using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// PASEK UMIEJETNOSCI: panel przypisywania odblokowanych umiejetnosci pod klawisze.
//
// Dziala na tej samej zasadzie co ekwipunek (patrz InventoryUI/InventorySlot):
// klikasz ikone umiejetnosci na liscie po prawej (paleta), "przykleja" sie do
// kursora, klikniecie na okienko hotbara po lewej (domyslnie klawisze 1-8)
// przypisuje ja pod dany klawisz - a nastepnie NACISNIECIE tego klawisza w grze
// wywoluje PlayerSkills.TryCastSkill() na tej umiejetnosci.
//
// ZGODNIE Z USTALENIAMI: panel i jego sloty sa na razie STATYCZNE (zbudowane
// recznie w Hierarchii, tak jak sloty ekwipunku) - nic sie tu nie generuje
// w runtime. Docelowy pasek widoczny NA EKRANIE GRY (poza tym panelem
// przypisywania) zrobimy w kolejnym kroku.
public class SkillBarUI : MonoBehaviour
{
    public static SkillBarUI instance;

    [Header("Panel")]
    public GameObject skillBarWindow;

    [Tooltip("Klawisz otwierajacy/zamykajacy ten panel. W przyszlosci, przy opcjach " +
             "sterowania, bedzie mozna to przemapowac z poziomu UI - na razie zmieniaj " +
             "recznie tutaj.")]
    public KeyCode togglePanelKey = KeyCode.H;

    [Header("Paleta - WSZYSTKIE odblokowane umiejetnosci (prawa strona)")]
    [Tooltip("Rodzic zawierajacy statyczne sloty palety (SkillBarSlot z Is Palette Slot = true).")]
    public Transform paletteArea;

    [Header("Hotbar - sloty przypisane do klawiszy (lewa strona, domyslnie 8 sztuk)")]
    [Tooltip("Rodzic zawierajacy statyczne sloty hotbara (SkillBarSlot z Is Palette Slot = false).")]
    public Transform hotbarArea;

    [Header("Kursor")]
    public Image dragImage;

    // To, co gracz aktualnie trzyma "przyklejone" do kursora. Ustawiaj WYLACZNIE
    // przez SetDraggedSkill/ClearDraggedSkill, zeby dragImage zawsze byl spojny.
    public SkillData draggedSkill;

    [Header("Stronicowanie Palety")]
    [Tooltip("Przycisk 'poprzednia strona' (strzalka w lewo pod/obok palety).")]
    public Button prevPageButton;
    [Tooltip("Przycisk 'nastepna strona' (strzalka w prawo pod/obok palety).")]
    public Button nextPageButton;
    [Tooltip("Tekst pokazujacy np. '1/3'. Opcjonalny - zostaw puste, jesli nie potrzebujesz.")]
    public TMPro.TextMeshProUGUI pageIndicatorText;

    private int currentPage = 0;

    private SkillBarSlot[] paletteSlots;
    private SkillBarSlot[] hotbarSlots;

    // Odczytywane przez SkillHUD (pasek widoczny podczas rozgrywki) - ten panel
    // przypisywania pozostaje jedynym zrodlem prawdy o tym, co jest pod jakim klawiszem.
    public SkillBarSlot[] HotbarSlots { get { return hotbarSlots; } }

    // Odpalane przez SkillBarSlot za kazdym razem, gdy zmieni sie przypisanie
    // ktoregokolwiek slotu hotbara (podniesienie/wlozenie/zamiana) - SkillHUD
    // nasluchuje tego, zeby pasek na dole ekranu zawsze byl aktualny.
    public System.Action onHotbarChanged;

    public void NotifyHotbarChanged()
    {
        onHotbarChanged?.Invoke();
    }

    void Awake()
    {
        instance = this;

        // WAZNE: cachujemy sloty JUZ TUTAJ, nie w Start(). SkillHUD (pasek na
        // dole ekranu) odczytuje HotbarSlots we WLASNYM Start(), a Unity nie
        // gwarantuje kolejnosci Start() miedzy roznymi obiektami - gwarantuje
        // tylko, ze WSZYSTKIE Awake() w scenie odpala sie przed KTORYMKOLWIEK
        // Start(). Trzymanie tego w Start() dawalo czasem pusta tablice
        // SkillHUD-owi (gdy jego Start() trafil sie pierwszy) i pasek zostawal
        // z placeholderami z Edytora az do pierwszej zmiany przypisania.
        paletteSlots = paletteArea != null
            ? paletteArea.GetComponentsInChildren<SkillBarSlot>(true)
            : new SkillBarSlot[0];

        hotbarSlots = hotbarArea != null
            ? hotbarArea.GetComponentsInChildren<SkillBarSlot>(true)
            : new SkillBarSlot[0];
    }

    void Start()
    {
        if (dragImage != null) dragImage.enabled = false;
        if (skillBarWindow != null) skillBarWindow.SetActive(false);

        // Wpiete przez kod, tak jak wszystkie inne przyciski w tym projekcie
        // (patrz ContextMenuUI/SplitAmountUI) - w Inspektorze zostawiamy OnClick puste.
        if (prevPageButton != null) prevPageButton.onClick.AddListener(PreviousPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);

        // Paleta ma sie sama odswiezyc, gdy gracz odblokuje nowa umiejetnosc,
        // zamiast czekac az sam zamknie i otworzy panel jeszcze raz.
        if (PlayerSkills.instance != null) PlayerSkills.instance.onSkillsChanged += RefreshPalette;
    }

    void OnDestroy()
    {
        if (PlayerSkills.instance != null) PlayerSkills.instance.onSkillsChanged -= RefreshPalette;
    }

    public bool IsOpen { get { return skillBarWindow != null && skillBarWindow.activeSelf; } }

    void Update()
    {
        if (Input.GetKeyDown(togglePanelKey)) TryTogglePanel();

        if (draggedSkill != null && dragImage != null)
        {
            dragImage.transform.position = Input.mousePosition;
        }

        HandleHotbarInput();
    }

    // Wywoluje przypisane umiejetnosci PO NACISNIECIU ich klawisza - dziala
    // NIEZALEZNIE od tego, czy ten panel przypisywania jest akurat otwarty,
    // bo o to w hotbarze chodzi (przypisujesz raz, potem grasz z panelem zamknietym).
    private void HandleHotbarInput()
    {
        if (hotbarSlots == null || hotbarSlots.Length == 0) return;
        if (IsBlockedByAnotherWindow()) return;

        foreach (SkillBarSlot slot in hotbarSlots)
        {
            if (slot == null || slot.skill == null) continue;
            if (slot.boundKey == KeyCode.None) continue;

            if (Input.GetKeyDown(slot.boundKey))
            {
                if (PlayerSkills.instance != null) PlayerSkills.instance.TryCastSkill(slot.skill);
            }
        }
    }

    // Ten sam zestaw blokad co w InventoryUI - rozmowa/sklep/pauza/ekran smierci
    // maja pierwszenstwo, zarowno przy otwieraniu panelu, jak i przy odpalaniu
    // umiejetnosci klawiszem.
    private bool IsBlockedByAnotherWindow()
    {
        if (PauseMenuUI.instance != null && PauseMenuUI.instance.IsOpen) return true;
        if (DeathScreenUI.instance != null && DeathScreenUI.instance.IsShowing) return true;

        // Wczytywanie zapisu jeszcze trwa - SaveManager.ApplyPendingLoad() (ktore
        // woła PlayerSkills.LoadFrom) odpala sie dopiero PO zaladowaniu lokacji,
        // czyli PO starcie tej sceny. Do tego momentu PlayerSkills moze jeszcze
        // NIE miec odblokowanych umiejetnosci z zapisu - bez tej blokady panel
        // otwarty za wczesnie pokazywalby falszywie pusta/niepelna palete.
        if (SaveManager.PendingLoad != null) return true;

        if (DialogueManager.instance == null) return false;

        return DialogueManager.instance.IsDialogueOpen
            || DialogueManager.instance.IsShopOpen
            || DialogueManager.instance.IsGiftPanelOpen;
    }

    public void TryTogglePanel()
    {
        if (!IsOpen && IsBlockedByAnotherWindow())
        {
            Debug.Log("Nie mozna teraz otworzyc paska umiejetnosci.");
            return;
        }

        TogglePanel();
    }

    public void TogglePanel()
    {
        if (skillBarWindow == null) return;

        bool isActive = !skillBarWindow.activeSelf;
        skillBarWindow.SetActive(isActive);

        if (isActive)
        {
            // Zawsze zaczynamy od pierwszej strony palety przy otwarciu panelu.
            currentPage = 0;
            RefreshPalette();
        }
        else
        {
            // Umiejetnosci to nie fizyczny przedmiot do wyrzucenia na ziemie jak
            // w ekwipunku - przy zamknieciu po prostu puszczamy to, co bylo
            // przyklejone do kursora (i tak nic sie nie traci, paleta ja pamieta).
            ClearDraggedSkill();

            // Zamkniecie panelu (np. Escapem) nie generuje OnPointerExit na slocie,
            // pod ktorym akurat stoi kursor - bez tego tooltip zostawalby zawieszony
            // na ekranie. Ten sam patent co InventoryUI.ForceHide() przy zamknieciu ekwipunku.
            if (InventoryTooltip.instance != null) InventoryTooltip.instance.ForceHide();
        }
    }

    public void ClosePanel()
    {
        if (IsOpen) TogglePanel();
    }

    // Wypelnia sloty palety odblokowanymi umiejetnosciami NALEZACYMI DO AKTUALNEJ
    // STRONY (statyczna liczba slotow palety = rozmiar jednej strony), dokladnie tak
    // jak plecak wypelnia sie przedmiotami od pierwszego pustego slotu - z tym, ze
    // tutaj "pierwszy pusty" przesuwa sie o currentPage * (liczba slotow palety).
    public void RefreshPalette()
    {
        if (paletteSlots == null) return;

        List<SkillData> unlocked = GetAllUnlockedSkills();

        int slotsPerPage = Mathf.Max(1, paletteSlots.Length);
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(unlocked.Count / (float)slotsPerPage));
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        int startIndex = currentPage * slotsPerPage;

        for (int i = 0; i < paletteSlots.Length; i++)
        {
            if (paletteSlots[i] == null) continue;

            int unlockedIndex = startIndex + i;
            paletteSlots[i].AssignSkill(unlockedIndex < unlocked.Count ? unlocked[unlockedIndex] : null);
        }

        UpdatePageUI(totalPages);
    }

    public void NextPage()
    {
        currentPage++;
        RefreshPalette();
    }

    public void PreviousPage()
    {
        currentPage--;
        RefreshPalette();
    }

    // Chowa strzalki na krancach (nie ma gdzie dalej/wstecz przewinac) i uzupelnia
    // wskaznik strony w stylu "1/3" - dokladnie ten sam pomysl co ContextMenuUI
    // chowajace przyciski, ktore akurat nie maja zastosowania.
    private void UpdatePageUI(int totalPages)
    {
        if (prevPageButton != null) prevPageButton.gameObject.SetActive(currentPage > 0);
        if (nextPageButton != null) nextPageButton.gameObject.SetActive(currentPage < totalPages - 1);
        if (pageIndicatorText != null) pageIndicatorText.text = $"{currentPage + 1}/{totalPages}";
    }

    private List<SkillData> GetAllUnlockedSkills()
    {
        List<SkillData> result = new List<SkillData>();

        if (PlayerSkills.instance == null) return result;

        SkillDatabase db = SkillDatabase.Instance;
        if (db == null || db.allSkills == null) return result;

        foreach (SkillData skill in db.allSkills)
        {
            if (skill != null && PlayerSkills.instance.IsUnlocked(skill)) result.Add(skill);
        }
        return result;
    }

    public void SetDraggedSkill(SkillData skill)
    {
        draggedSkill = skill;

        if (dragImage != null)
        {
            dragImage.sprite = skill != null ? skill.icon : null;
            dragImage.preserveAspect = true;
            dragImage.enabled = skill != null;
        }
    }

    public void ClearDraggedSkill()
    {
        SetDraggedSkill(null);
    }
}
