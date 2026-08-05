using TMPro;
using UnityEngine;
using UnityEngine.UI; // Wymagane do obs³ugi UI!

public class PlayerHUD : MonoBehaviour
{
    [Header("Paski Wype³nienia")]
    public Image healthBar;
    public Image manaBar; // Tu przypiszemy niebieski pasek (Stamina)
    public Image expBar;

    [Header("Wyskakuj¹ce Informacje")]
    public GameObject infoTextObject; // G³ówny obiekt tekstu (¿eby go w³¹czaæ/wy³¹czaæ)
    public TextMeshProUGUI infoText;  // Komponent do podmiany cyferek

    private string currentHoveredBar = "";

    void Update()
    {
        // Sprawdzamy czy statystyki gracza istniej¹, by nie wyrzucaæ b³êdów
        if (PlayerStats.instance != null)
        {
            PlayerStats stats = PlayerStats.instance;

            // Pasek ¯ycia (Czerwony)
            // (float) zamienia int na u³amek, ¿eby wynik dzielenia nie wychodzi³ zawsze jako 0
            healthBar.fillAmount = (float)stats.currentHealth / stats.GetMaxHealth();

            // Pasek Many/Staminy (Niebieski)
            manaBar.fillAmount = stats.currentMana / stats.GetMaxMana();

            // Pasek Doœwiadczenia (Zielony)
            expBar.fillAmount = (float)stats.currentExp / stats.expToNextLevel;

            // Aktualizacja tekstu NA ¯YWO (jeœli na coœ patrzymy)
            if (currentHoveredBar != "")
            {
                if (currentHoveredBar == "Health")
                {
                    infoText.text = $"{stats.currentHealth} / {stats.GetMaxHealth()}";
                }
                else if (currentHoveredBar == "Mana")
                {
                    // U¿ywamy RoundToInt, ¿eby nie wyœwietlaæ u³amków podczas regeneracji staminy/many!
                    infoText.text = $"{Mathf.RoundToInt(stats.currentMana)} / {Mathf.RoundToInt(stats.GetMaxMana())}";
                }
                else if (currentHoveredBar == "Exp")
                {
                    infoText.text = $"{stats.currentExp} / {stats.expToNextLevel}";
                }
            }
        }

    }
    // --- FUNKCJE DLA MYSZKI (EVENT TRIGGERS) ---
    public void ShowHealthInfo()
    {
        currentHoveredBar = "Health";
        if (infoTextObject != null) infoTextObject.SetActive(true);
    }

    public void ShowManaInfo()
    {
        currentHoveredBar = "Mana";
        if (infoTextObject != null) infoTextObject.SetActive(true);
    }

    public void ShowExpInfo()
    {
        currentHoveredBar = "Exp";
        if (infoTextObject != null) infoTextObject.SetActive(true);
    }

    public void HideInfo()
    {
        currentHoveredBar = ""; // Resetujemy pamiêæ myszki
        if (infoTextObject != null) infoTextObject.SetActive(false);
    }
}