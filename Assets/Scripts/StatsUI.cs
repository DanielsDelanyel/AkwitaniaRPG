using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatsUI : MonoBehaviour
{
    [Header("Zarz¹dzanie Oknem")]
    public GameObject statsWindow; // <--- Referencja do graficznego okna
    public TopDownMovement playerMovement; // <--- Do zatrzymywania gracza

    [Header("Nowe Elementy UI")]
    public TextMeshProUGUI playerNameText;
    public GameObject professionTooltipWindow;
    public TextMeshProUGUI professionTooltipText;

    [Header("Teksty Wartoœci")]
    public TextMeshProUGUI strText;
    public TextMeshProUGUI dexText;
    public TextMeshProUGUI vitText;
    public TextMeshProUGUI intText;
    public TextMeshProUGUI charText;

    [Header("Ogólne Informacje")]
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

    // Odœwie¿a UI, jeœli okno by³o w³¹czone z poziomu Inspektora
    void OnEnable()
    {
        if (statsWindow != null && statsWindow.activeSelf)
        {
            UpdateUI();
        }
    }

    void Update()
    {
        // --- NOWOŒÆ: Zabezpieczenie przed otwieraniem okien w trakcie rozmowy ---
        if (DialogueManager.instance != null && DialogueManager.instance.dialogueWindow.activeSelf)
            return; // Przerwij czytanie klawiszy, jeœli dialog jest otwarty!

        if (Input.GetKeyDown(KeyCode.V)) ToggleStatsWindow();
        else if (Input.GetKeyDown(KeyCode.Escape) && statsWindow.activeSelf) ToggleStatsWindow();

        //Tooltip pod¹¿a za myszk¹
        if (professionTooltipWindow != null && professionTooltipWindow.activeSelf)
        {
            professionTooltipWindow.transform.position = Input.mousePosition + new Vector3(150f, 44f, 0f);
        }
    }

    public void ToggleStatsWindow()
    {
        // Zamieniamy stan na przeciwny (w³¹czone -> wy³¹czone i na odwrót)
        bool isActive = !statsWindow.activeSelf;
        statsWindow.SetActive(isActive);

        // Odœwie¿amy liczby przy otwieraniu okna
        if (isActive) UpdateUI();
        else HideProfessionTooltip();
        // Zatrzymujemy gracza, gdy czyta statystyki (tak jak w Inventory)
        if (playerMovement != null)
        {
            playerMovement.enabled = !isActive;
            if (isActive) playerMovement.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }
    }

    public void UpdateUI()
    {
        if (PlayerStats.instance == null) return;

        PlayerStats stats = PlayerStats.instance;
        expBar.fillAmount = (float)stats.currentExp / stats.expToNextLevel; 
        if (playerNameText != null) playerNameText.text = stats.playerName;
        // 1. Aktualizacja tekstów
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

        // 2. Blokowanie przycisków
        bool hasPoints = stats.attributePoints > 0;
        strButton.interactable = hasPoints;
        dexButton.interactable = hasPoints;
        vitButton.interactable = hasPoints;
        intButton.interactable = hasPoints;
        charButton.interactable = hasPoints;
    }

    // --- FUNKCJE DLA PRZYCISKÓW ---
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