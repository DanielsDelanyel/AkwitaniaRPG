using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// MENU GLOWNE.
// Powies to na obiekcie Canvas w scenie "MainMenu".
//
// Wszystkie akcje przyciskow podpinane sa Z KODU (w Awake), a nie przez pole
// "On Click" w Inspektorze - dzieki temu nie da sie ich przypadkiem zle spiac.
public class MainMenuUI : MonoBehaviour
{
    [Header("Panele")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    [Header("Przyciski Glowne")]
    public Button newGameButton;
    public Button loadGameButton;
    public Button optionsButton;
    public Button creditsButton;
    public Button quitButton;

    [Header("Przyciski Powrotu")]
    [Tooltip("Przycisk 'Wstecz' w panelu opcji.")]
    public Button optionsBackButton;
    [Tooltip("Przycisk 'Wstecz' w panelu autorow.")]
    public Button creditsBackButton;

    // ===============================================================
    // WSPOLDZIELONY PANEL POTWIERDZENIA
    // Jeden panel obsluguje i wyjscie z gry, i nadpisanie zapisu.
    // Przycisk "Tak" dostaje swoje zadanie DOPIERO w chwili pokazania okna,
    // wiec nigdy nie zrobi czegos innego, niz mowi pytanie.
    // ===============================================================
    [Header("Panel Potwierdzenia")]
    [Tooltip("To samo okno obsluguje wyjscie z gry i nadpisanie zapisu.")]
    public GameObject confirmPanel;
    public Button confirmYesButton;
    public Button confirmNoButton;
    [Tooltip("Opcjonalny tekst pytania w tym oknie.")]
    public TMPro.TextMeshProUGUI confirmText;

    [Header("Tresc Pytan")]
    [TextArea] public string quitMessage = "Czy na pewno chcesz wyjsc z gry?";
    [TextArea] public string overwriteMessage = "Masz juz zapisana gre.\nRozpoczecie nowej ja nadpisze. Kontynuowac?";

    [Header("Scena Gry")]
    [Tooltip("Nazwa sceny startowej gry - u Ciebie 'Bootstrap'. " +
             "Musi byc dodana w File -> Build Settings.")]
    public string gameSceneName = "Bootstrap";

    [Header("Przejscie")]
    [Tooltip("Czarna plansza na caly ekran z komponentem CanvasGroup.")]
    public CanvasGroup fadeGroup;
    public float fadeTime = 0.6f;

    [Header("Dzwiek")]
    public AudioClip[] clickSounds;
    public AudioClip[] hoverSounds;
    [Range(0f, 1f)] public float uiVolume = 0.5f;

    private bool isLeaving;

    // Sciezka zapisu - na razie sluzy tylko do sprawdzenia, czy zapis istnieje
    public static string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, "save01.json"); }
    }

    public static bool HasSave()
    {
        return SaveManager.HasSave(GameSession.SaveSlot);
    }

    void Awake()
    {
        GameSettings.ApplyAll();

        WireButtons();
    }

    void Start()
    {
        ShowMainPanel();

        // Brak zapisu = przycisk wyszarzony, a nie klikalny w pustke
        if (loadGameButton != null) loadGameButton.interactable = HasSave();

        // Wjazd z czerni
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 1f;
            fadeGroup.blocksRaycasts = true;
            StartCoroutine(FadeTo(0f, () => fadeGroup.blocksRaycasts = false));
        }
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (isLeaving) return;

        // Escape cofa o jeden poziom, nigdy nie wychodzi od razu z gry
        if (confirmPanel != null && confirmPanel.activeSelf) { ShowMainPanel(); return; }
        if (optionsPanel != null && optionsPanel.activeSelf) { ShowMainPanel(); return; }
        if (creditsPanel != null && creditsPanel.activeSelf) { ShowMainPanel(); return; }
    }

    private void WireButtons()
    {
        Wire(newGameButton, OnNewGameClicked);
        Wire(loadGameButton, OnLoadGameClicked);
        Wire(optionsButton, ShowOptionsPanel);
        Wire(creditsButton, ShowCreditsPanel);
        Wire(quitButton, OnQuitClicked);

        Wire(optionsBackButton, ShowMainPanel);
        Wire(creditsBackButton, ShowMainPanel);

        // "Nie" zawsze wraca do menu. "Tak" wpinamy dopiero w ShowConfirm().
        Wire(confirmNoButton, ShowMainPanel);
    }

    private void Wire(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            SoundManager.Play(clickSounds, uiVolume);
            action();
        });

        AddHoverSound(button);
    }

    // Dzwiek przy najechaniu myszka - podpinany przez EventTrigger,
    // zeby nie trzeba bylo dodawac go recznie do kazdego przycisku.
    private void AddHoverSound(Button button)
    {
        if (hoverSounds == null || hoverSounds.Length == 0) return;

        var trigger = button.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        var entry = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
        };
        entry.callback.AddListener(_ =>
        {
            if (button.interactable) SoundManager.Play(hoverSounds, uiVolume * 0.6f);
        });

        trigger.triggers.Add(entry);
    }

    // ===============================================================
    // PRZELACZANIE PANELI
    // ===============================================================
    public void ShowMainPanel()
    {
        SetPanel(mainPanel, true);
        SetPanel(optionsPanel, false);
        SetPanel(creditsPanel, false);
        SetPanel(confirmPanel, false);
    }

    public void ShowOptionsPanel()
    {
        SetPanel(mainPanel, false);
        SetPanel(optionsPanel, true);
        SetPanel(creditsPanel, false);
    }

    public void ShowCreditsPanel()
    {
        SetPanel(mainPanel, false);
        SetPanel(optionsPanel, false);
        SetPanel(creditsPanel, true);
    }

    private void SetPanel(GameObject panel, bool visible)
    {
        if (panel != null) panel.SetActive(visible);
    }

    // ===============================================================
    // AKCJE
    // ===============================================================
    private void OnNewGameClicked()
    {
        // Ostrzegamy, zanim skasujemy czyjs postep
        if (HasSave() && confirmPanel != null)
        {
            ShowConfirm(overwriteMessage, StartNewGame);
            return;
        }

        StartNewGame();
    }

    // Pokazuje okno pytania i przypisuje przyciskowi "Tak" konkretne zadanie
    private void ShowConfirm(string message, System.Action onYes)
    {
        if (confirmPanel == null)
        {
            onYes();  // brak okna potwierdzenia - wykonujemy od razu
            return;
        }

        if (confirmText != null) confirmText.text = message;

        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveAllListeners();
            confirmYesButton.onClick.AddListener(() =>
            {
                SoundManager.Play(clickSounds, uiVolume);
                onYes();
            });
        }

        SetPanel(mainPanel, false);
        SetPanel(optionsPanel, false);
        SetPanel(creditsPanel, false);
        SetPanel(confirmPanel, true);
    }

    private void StartNewGame()
    {
        GameSession.IsLoadingSave = false;
        SaveManager.ResetSession();   // czysty licznik czasu gry
        EnterGame();
    }

    private void OnLoadGameClicked()
    {
        if (!HasSave())
        {
            Debug.Log("Brak zapisu do wczytania.");
            return;
        }

        GameSession.IsLoadingSave = true;
        EnterGame();
    }

    private void EnterGame()
    {
        if (isLeaving) return;
        isLeaving = true;

        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("MainMenuUI: nie podano nazwy sceny gry!");
            isLeaving = false;
            return;
        }

        StartCoroutine(EnterGameRoutine());
    }

    private IEnumerator EnterGameRoutine()
    {
        // Muzyka menu ustepuje miejsca muzyce gry
        if (MusicPlayer.instance != null) MusicPlayer.instance.PlayGameTheme();

        if (fadeGroup != null)
        {
            fadeGroup.blocksRaycasts = true;
            yield return FadeTo(1f, null);
        }

        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private void OnQuitClicked()
    {
        ShowConfirm(quitMessage, QuitGame);
    }

    private void QuitGame()
    {
        GameSettings.Save();

        Debug.Log("Wyjscie z gry.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ===============================================================
    // PRZYCIEMNIANIE
    // ===============================================================
    private IEnumerator FadeTo(float target, System.Action onDone)
    {
        if (fadeGroup == null) yield break;

        float start = fadeGroup.alpha;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime);
            fadeGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t));
            yield return null;
        }

        fadeGroup.alpha = target;
        if (onDone != null) onDone();
    }
}

// Przenosi informacje z menu do sceny gry.
// Statyczne pola przezywaja zmiane sceny.
public static class GameSession
{
    // Czy gracz kliknal "Wczytaj gre" zamiast "Nowa gra"?
    public static bool IsLoadingSave;

    // Miejsce na przyszly numer slotu zapisu
    public static int SaveSlot = 1;

    public static void Reset()
    {
        IsLoadingSave = false;
        SaveSlot = 1;
    }
}