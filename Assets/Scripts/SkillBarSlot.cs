using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// POJEDYNCZY SLOT PASKA UMIEJETNOSCI. Dwa tryby zaleznie od Is Palette Slot -
// dokladnie tak jak InventorySlot rozroznia isBackpackSlot:
//
//  - PALETA (prawa strona, Is Palette Slot = TRUE): statyczna lista WSZYSTKICH
//    odblokowanych umiejetnosci, wypelniana przez SkillBarUI.RefreshPalette().
//    To tylko ZRODLO - klikniecie NIGDY nie oprozni tego slotu, tylko kopiuje
//    (lub podmienia) umiejetnosc trzymana na kursorze.
//
//  - HOTBAR (lewa strona, Is Palette Slot = FALSE): 8 statycznych okienek
//    odpowiadajacych klawiszom (Bound Key, domyslnie 1-8). Klikniecie dziala
//    jak w ekwipunku: pusta reka podnosi przypisana umiejetnosc (i oproznia slot),
//    trzymana na kursorze umiejetnosc zostaje TU wlozona, a to co bylo w slocie
//    wczesniej (jesli cokolwiek) wraca na kursor - zamiana.
public class SkillBarSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image iconDisplay;
    public SkillData skill;

    [Header("Konfiguracja Slotu")]
    [Tooltip("Zaznacz dla slotow PO PRAWEJ (statyczna lista odblokowanych umiejetnosci). " +
             "Odznacz dla slotow HOTBARA po lewej (1-8).")]
    public bool isPaletteSlot = false;

    [Header("Tylko Hotbar (Is Palette Slot = OFF)")]
    [Tooltip("Klawisz, ktory wywola te umiejetnosc w grze (patrz SkillBarUI.HandleHotbarInput). " +
             "W przyszlosci, przy opcjach sterowania, to pole bedzie mozna przemapowac z poziomu UI - " +
             "na razie zmieniaj recznie w Inspektorze.")]
    public KeyCode boundKey = KeyCode.None;

    [Tooltip("Opcjonalne - podpisuje slot nazwa klawisza (np. '4'). Zostaw puste, jesli nie potrzebujesz.")]
    public TMPro.TextMeshProUGUI keyLabelText;

    void Awake()
    {
        UpdateKeyLabel();
    }

    void Start()
    {
        if (iconDisplay == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null) iconDisplay = iconTransform.GetComponent<Image>();
        }

        ClearSkill();
    }

    // Publiczne, zeby dalo sie odswiezyc podpis recznie po zmianie Bound Key z kodu
    // (np. przyszly ekran opcji sterowania).
    public void UpdateKeyLabel()
    {
        if (keyLabelText == null || isPaletteSlot) return;
        keyLabelText.text = FormatKeyName(boundKey);
    }

    private string FormatKeyName(KeyCode key)
    {
        if (key == KeyCode.None) return "";

        string keyName = key.ToString();
        // KeyCode.Alpha4 -> "4" (samo "AlphaX" ladnie sie skraca do cyfry)
        if (keyName.StartsWith("Alpha")) return keyName.Substring("Alpha".Length);
        return keyName;
    }

    public void AssignSkill(SkillData newSkill)
    {
        skill = newSkill;

        if (iconDisplay != null)
        {
            iconDisplay.sprite = skill != null ? skill.icon : null;
            iconDisplay.preserveAspect = true;
            iconDisplay.enabled = skill != null;
        }
    }

    public void ClearSkill()
    {
        AssignSkill(null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (SkillBarUI.instance == null) return;

        if (isPaletteSlot)
        {
            // Paleta jest tylko ZRODLEM - nigdy sie nie oprozni. Klikniecie po prostu
            // podpina (lub podmienia) te umiejetnosc na kursorze.
            if (skill != null) SkillBarUI.instance.SetDraggedSkill(skill);
            return;
        }

        // --- HOTBAR ---
        SkillData cursorSkill = SkillBarUI.instance.draggedSkill;

        if (cursorSkill == null)
        {
            // Pusta reka - podnosimy to, co jest w tym slocie (jesli cokolwiek)
            if (skill == null) return; // nic sie nie zmienia - pusty slot, pusta reka

            SkillBarUI.instance.SetDraggedSkill(skill);
            ClearSkill();
        }
        else
        {
            // Trzymamy cos na kursorze - wkladamy tutaj, a to co bylo w slocie
            // (jesli cokolwiek) wraca na kursor. Dokladnie ta sama zamiana,
            // co przy przedmiotach w ekwipunku.
            SkillData previous = skill;
            AssignSkill(cursorSkill);

            if (previous != null) SkillBarUI.instance.SetDraggedSkill(previous);
            else SkillBarUI.instance.ClearDraggedSkill();
        }

        // Przypisanie faktycznie sie zmienilo - powiadamiamy pasek widoczny
        // podczas rozgrywki (SkillHUD), zeby od razu pokazal nowa ikone/klawisz.
        SkillBarUI.instance.NotifyHotbarChanged();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (skill == null) return;
        if (SkillBarUI.instance != null && SkillBarUI.instance.draggedSkill != null) return;

        // Tu wyswietlana umiejetnosc jest juz z definicji odblokowana (i paleta,
        // i hotbar pokazuja tylko to, co gracz juz ma) - stad unlocked/canUnlock = true.
        if (InventoryTooltip.instance != null)
            InventoryTooltip.instance.ShowTooltip(skill, true, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (InventoryTooltip.instance != null)
            InventoryTooltip.instance.HideTooltip();
    }
}
