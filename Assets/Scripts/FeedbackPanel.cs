using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// PANEL FEEDBACKU - wersja robocza.
//
// Na razie zapisuje zgloszenia do pliku tekstowego na dysku gracza
// i opcjonalnie otwiera formularz w przegladarce. Gdy bedziesz mial
// serwer albo Formularz Google, podmienimy tylko metode Send().
public class FeedbackPanel : MonoBehaviour
{
    public enum SendMode
    {
        SaveToFile,      // zapis lokalny - dziala od reki, bez internetu
        OpenUrl,         // otwiera formularz w przegladarce
        Both
    }

    [Header("Sposob Wysylki")]
    public SendMode mode = SendMode.SaveToFile;

    [Tooltip("Adres formularza (np. Google Forms). Uzywany w trybie OpenUrl.")]
    public string feedbackUrl = "";

    [Header("Pola UI")]
    public TMP_InputField messageInput;
    public TMP_Dropdown categoryDropdown;
    public Button sendButton;
    public Button clearButton;

    [Tooltip("Napis potwierdzajacy wyslanie.")]
    public TextMeshProUGUI statusText;

    [Header("Teksty")]
    public string successMessage = "Dziekujemy! Zgloszenie zostalo zapisane.";
    public string emptyMessage = "Napisz cos, zanim wyslesz.";
    public float statusVisibleTime = 3f;

    [Header("Dzwiek")]
    public AudioClip[] sendSounds;
    [Range(0f, 1f)] public float uiVolume = 0.5f;

    private float statusTimer;

    // Plik ze zgloszeniami - w folderze danych gry, obok zapisow
    public static string FeedbackPath
    {
        get { return Path.Combine(Application.persistentDataPath, "feedback.txt"); }
    }

    void Awake()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(Send);
        }

        if (clearButton != null)
        {
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(ClearForm);
        }

        SetupCategories();
    }

    void OnEnable()
    {
        SetStatus("");
    }

    void Update()
    {
        if (statusTimer <= 0f) return;

        // unscaledDeltaTime - panel dziala takze przy zatrzymanym czasie (pauza)
        statusTimer -= Time.unscaledDeltaTime;
        if (statusTimer <= 0f) SetStatus("");
    }

    private void SetupCategories()
    {
        if (categoryDropdown == null) return;
        if (categoryDropdown.options.Count > 0) return; // juz wypelnione w Inspektorze

        categoryDropdown.ClearOptions();
        categoryDropdown.AddOptions(new System.Collections.Generic.List<string>
        {
            "Blad w grze",
            "Propozycja",
            "Problem z wydajnoscia",
            "Literowka lub tlumaczenie",
            "Inne"
        });
    }

    public void Send()
    {
        string message = messageInput != null ? messageInput.text : "";

        if (string.IsNullOrWhiteSpace(message))
        {
            SetStatus(emptyMessage);
            return;
        }

        string category = "Inne";
        if (categoryDropdown != null && categoryDropdown.options.Count > 0)
            category = categoryDropdown.options[categoryDropdown.value].text;

        if (mode == SendMode.SaveToFile || mode == SendMode.Both) SaveToFile(category, message);
        if (mode == SendMode.OpenUrl || mode == SendMode.Both) OpenUrl();

        SoundManager.Play(sendSounds, uiVolume);
        SetStatus(successMessage);
        ClearForm();
    }

    private void SaveToFile(string category, string message)
    {
        try
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("===================================");
            sb.AppendLine($"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Kategoria: {category}");
            sb.AppendLine($"Wersja gry: {Application.version}");
            sb.AppendLine($"System: {SystemInfo.operatingSystem}");
            sb.AppendLine($"Lokacja: {GetCurrentLocation()}");
            sb.AppendLine($"Poziom gracza: {GetPlayerLevel()}");
            sb.AppendLine("--- Tresc ---");
            sb.AppendLine(message);
            sb.AppendLine();

            File.AppendAllText(FeedbackPath, sb.ToString());

            Debug.Log($"Zgloszenie zapisane: {FeedbackPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Nie udalo sie zapisac zgloszenia: {e.Message}");
            SetStatus("Nie udalo sie zapisac zgloszenia.");
        }
    }

    private void OpenUrl()
    {
        if (string.IsNullOrEmpty(feedbackUrl))
        {
            Debug.LogWarning("FeedbackPanel: tryb OpenUrl, ale nie podano adresu!");
            return;
        }

        Application.OpenURL(feedbackUrl);
    }

    // Kontekst zgloszenia - bardzo pomaga przy szukaniu bledu
    private string GetCurrentLocation()
    {
        if (LocationManager.instance == null) return "nieznana";

        string loc = LocationManager.instance.CurrentLocation;
        return string.IsNullOrEmpty(loc) ? "nieznana" : loc;
    }

    private string GetPlayerLevel()
    {
        if (PlayerStats.instance == null) return "-";
        return PlayerStats.instance.level.ToString();
    }

    public void ClearForm()
    {
        if (messageInput != null) messageInput.text = "";
    }

    private void SetStatus(string text)
    {
        if (statusText == null) return;

        statusText.text = text;
        statusText.gameObject.SetActive(!string.IsNullOrEmpty(text));
        statusTimer = string.IsNullOrEmpty(text) ? 0f : statusVisibleTime;
    }
}