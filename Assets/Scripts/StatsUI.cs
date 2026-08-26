using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatsUI : MonoBehaviour
{
    // Singleton, zeby UIEscapeHandler mogl zamknac to okno
    public static StatsUI instance;

    public bool IsOpen { get { return statsWindow != null && statsWindow.activeSelf; } }

    void Awake() { instance = this; }

    [Header("Zarz�dzanie Oknem")]
    public GameObject statsWindow; // <--- Referencja do graficznego okna
    public TopDownMovement playerMovement; // <--- Do zatrzymywania gracza

    [Header("Nowe Elementy UI")]
    public TextMeshProUGUI playerNameText;
    public GameObject professionTooltipWindow;
    public TextMeshProUGUI professionTooltipText;

    [Header("Teksty Warto�ci")]
    public TextMeshProUGUI strText;
    public TextMeshProUGUI dexText;
    public TextMeshProUGUI vitText;
    public TextMeshProUGUI intText;
    public TextMeshProUGUI charText;

    [Header("Og�lne Informacje")]
    public TextMeshProUGUI availablePointsText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI professionText;

    [Header("Przyciski (Plusiki)")]
    public Button strButton;
    public Button dexButton;
    public Button vitButton;
    public Button intButton;
    public Button charButton;

    public Image expBar;

    // Od�wie�a UI, je�li okno by�o w��czone z poziomu Inspektora
    void OnEnable()
    {
        if (statsWindow != null && statsWindow.activeSelf)
        {
            UpdateUI();
        }
    }

    void Update()
    {
        // UWAGA: Escape NIE jest tu obslugiwany - zajmuje sie nim UIEscapeHandler.
        if (Input.GetKeyDown(KeyCode.V)) TryToggleStatsWindow();

        //Tooltip pod��a za myszk�
        if (professionTooltipWindow != null && professionTooltipWindow.activeSelf)
        {
            professionTooltipWindow.transform.position = Input.mousePosition + new Vector3(150f, 44f, 0f);
        }
    }

    // Klawisz "V" - nie otwiera okna w trakcie rozmowy ani zakupow
    public void TryToggleStatsWindow()
    {
        if (!IsOpen && IsBlockedByAnotherWindow())
        {
            Debug.Log("Nie mozna teraz otworzyc okna statystyk.");
            return;
        }

        ToggleStatsWindow();
    }

    private bool IsBlockedByAnotherWindow()
    {
        if (PauseMenuUI.instance != null && PauseMenuUI.instance.IsOpen) return true;
        if (DeathScreenUI.instance != null && DeathScreenUI.instance.IsShowing) return true;

        if (DialogueManager.instance != null)
        {
            if (DialogueManager.instance.IsDialogueOpen) return true;
            if (DialogueManager.instance.IsShopOpen) return true;
            if (DialogueManager.instance.IsGiftPanelOpen) return true;
        }

        if (InventoryUI.instance != null && InventoryUI.instance.IsOpen) return true;

        return false;
    }

    // Dla UIEscapeHandler - zamyka, ale nigdy nie otwiera
    public void CloseStatsWindow()
    {
        if (IsOpen) ToggleStatsWindow();
    }

    public void ToggleStatsWindow()
    {
        // Zamieniamy stan na przeciwny (w��czone -> wy��czone i na odwr�t)
        bool isActive = !statsWindow.activeSelf;
        statsWindow.SetActive(isActive);

        // Od�wie�amy liczby przy otwieraniu okna
        if (isActive) UpdateUI();
        else HideProfessionTooltip();
        // Blokada ruchu przez UILock - ruch wroci dopiero, gdy zamkna sie
        // WSZYSTKIE okna, ktore go blokuja (np. trwajaca rozmowa).
        UILock.Set("Stats", isActive);
    }

    public void UpdateUI()
    {
        if (PlayerStats.instance == null) return;

        PlayerStats stats = PlayerStats.instance;
        expBar.fillAmount = (float)stats.currentExp / stats.expToNextLevel; 
        if (playerNameText != null) playerNameText.text = stats.playerName;
        // 1. Aktualizacja tekst�w
        strText.text = stats.baseSTR.ToString();
        dexText.text = stats.baseZR.ToString();
        vitText.text = stats.baseWIT.ToString();
        intText.text = stats.baseINT.ToString();
        charText.text = stats.baseCHAR.ToString();

        availablePointsText.text = stats.attributePoints.ToString();
        levelText.text = stats.level.ToString();

        if (professionText != null)
        {
            switch (stats.currentProfession)
            {
                case CharacterClass.Traveler: professionText.text = "WLOCZEGA"; break;
                case CharacterClass.Assassin: professionText.text = "SKRYTOBOJCA"; break;
                case CharacterClass.Mage: professionText.text = "MAG"; break;
                case CharacterClass.Barbarian: professionText.text = "BARBARZYNCA"; break;
                case CharacterClass.Juggernaut: professionText.text = "OBRONCA"; break;
                case CharacterClass.Bard: professionText.text = "BARD"; break;
                case CharacterClass.Paladin: professionText.text = "PALADYN"; break;
                case CharacterClass.Nekromancer: professionText.text = "NEKROMANTA"; break;
                case CharacterClass.Ilusionist: professionText.text = "ILUZJONISTA"; break;
                case CharacterClass.Monk: professionText.text = "MNICH"; break;
                case CharacterClass.Hunter: professionText.text = "LOWCA"; break;
            }
        }

        // 2. Blokowanie przycisk�w
        bool hasPoints = stats.attributePoints > 0;
        strButton.interactable = hasPoints;
        dexButton.interactable = hasPoints;
        vitButton.interactable = hasPoints;
        intButton.interactable = hasPoints;
        charButton.interactable = hasPoints;
    }

    // --- FUNKCJE DLA PRZYCISK�W ---
    public void IncreaseSTR() { TryIncreaseStat(ref PlayerStats.instance.baseSTR); }
    public void IncreaseDEX() { TryIncreaseStat(ref PlayerStats.instance.baseZR); }
    public void IncreaseVIT() { TryIncreaseStat(ref PlayerStats.instance.baseWIT); }
    public void IncreaseINT() { TryIncreaseStat(ref PlayerStats.instance.baseINT); }
    public void IncreaseCHAR() { TryIncreaseStat(ref PlayerStats.instance.baseCHAR); }

    private void TryIncreaseStat(ref int statToIncrease)
    {
        if (PlayerStats.instance.attributePoints > 0)
        {
            PlayerStats.instance.attributePoints--;
            statToIncrease++;

            PlayerStats.instance.RecalculateStats();
            UpdateUI();
        }
    }

    public void ShowProfessionTooltip()
    {
        if (PlayerStats.instance == null || professionTooltipWindow == null) return;

        professionTooltipText.text = PlayerStats.instance.GetProfessionDescription();
        professionTooltipWindow.SetActive(true);
    }

    public void HideProfessionTooltip()
    {
        if (professionTooltipWindow != null) professionTooltipWindow.SetActive(false);
    }
}