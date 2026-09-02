using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// MENU PAUZY (w trakcie gry).
// Powies to na obiekcie PauseRoot wewnatrz Canvas w scenie Bootstrap.
//
// Otwiera je UIEscapeHandler, ale tylko wtedy, gdy zadne inne okno
// nie jest aktywne - Escape najpierw zamyka to, co masz przed oczami.
public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI instance;

    [Header("Korzen Nakladki")]
    [Tooltip("Obiekt zawierajacy CALE menu pauzy razem z przyciemnieniem tla. " +
             "Zostaw puste, a skrypt uzyje wlasnego obiektu.")]
    public GameObject pauseRoot;

    [Header("Panele")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject feedbackPanel;
    [Tooltip("Wspoldzielone okno 'Czy na pewno?'")]
    public GameObject confirmPanel;
    public TMPro.TextMeshProUGUI confirmText;

    [Header("Przyciski Glowne")]
    public Button resumeButton;
    public Button saveGameButton;
    public Button loadGameButton;
    public Button optionsButton;
    public Button feedbackButton;
    public Button quitToMenuButton;

    [Header("Przyciski Powrotu")]
    public Button optionsBackButton;
    public Button feedbackBackButton;

    [Header("Panel Potwierdzenia")]
    public Button confirmYesButton;
    public Button confirmNoButton;
    [TextArea] public string quitToMenuMessage = "Wyjsc do menu glownego?\\nNiezapisany postep zostanie utracony.";
    [TextArea] public string loadGameMessage = "Wczytac zapis?\\nObecny postep zostanie utracony.";

    [Header("Diagnostyka")]
    [Tooltip("Wypisuje przy starcie, czy menu pauzy jest poprawnie skonfigurowane.")]
    public bool logSetupCheck = true;

    [Header("Zachowanie")]
    [Tooltip("Zatrzymuje CZAS gry na pauzie. Odznacz, jesli gra ma toczyc sie dalej w tle.")]
    public bool pauseTimeScale = true;

    [Tooltip("Nazwa sceny menu glownego - musi byc w Build Settings.")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Komunikat Zapisu")]
    [Tooltip("Napis 'Gra zapisana' pojawiajacy sie na chwile po zapisie.")]
    public TMPro.TextMeshProUGUI saveStatusText;
    public float saveStatusTime = 2.5f;
    [TextArea] public string saveSuccessMessage = "Gra zapisana.";
    [TextArea] public string saveFailMessage = "Nie udalo sie zapisac gry.";

    [Header("Dzwiek")]
    public AudioClip[] clickSounds;
    public AudioClip[] saveSounds;
    public AudioClip[] openSounds;
    [Range(0f, 1f)] public float uiVolume = 0.5f;

    private bool isQuitting;
    private float saveStatusTimer;
    private bool warnedAboutHandler;

    public bool IsOpen { get { return Root != null && Root.activeSelf; } }

    private GameObject Root { get { return pauseRoot != null ? pauseRoot : gameObject; } }

    void Awake()
    {
        instance = this;
        WireButtons();
    }

    void Update()
    {
        HandleEscapeFallback();

        if (saveStatusTimer <= 0f) return;

        // unscaledDeltaTime - na pauzie zwykly czas stoi
        saveStatusTimer -= Time.unscaledDeltaTime;
        if (saveStatusTimer <= 0f) SetSaveStatus("");
    }

    // ZABEZPIECZENIE: normalnie Escape obsluguje UIEscapeHandler.
    // Jesli go w scenie nie ma, przejmujemy klawisz tutaj, zeby menu pauzy
    // dzialalo mimo wszystko - i mowimy o tym raz w konsoli.
    private void HandleEscapeFallback()
    {
        if (UIEscapeHandler.instance != null) return;

        if (!warnedAboutHandler)
        {
            warnedAboutHandler = true;
            Debug.LogWarning("Nie znaleziono UIEscapeHandler w scenie! " +
                             "Dodaj ten komponent do obiektu Canvas w scenie Bootstrap. " +
                             "Bez niego Escape nie zamyka ekwipunku, sklepu ani rozmow - " +
                             "menu pauzy dziala teraz w trybie awaryjnym.");
        }

        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (IsOpen) HandleEscape();
        else Open();
    }

    void Start()
    {
        // Menu pauzy startuje schowane, ale sam skrypt musi zostac wlaczony,
        // wiec chowamy dopiero jego korzen - nie ten obiekt.
        if (pauseRoot != null) pauseRoot.SetActive(false);

        if (logSetupCheck) RunSetupCheck();
    }

    // Sprawdza najczestsze bledy konfiguracji i wypisuje je w konsoli
    private void RunSetupCheck()
    {
        if (mainPanel == null)
            Debug.LogError("PauseMenuUI: nie przypisano Main Panel - menu bedzie puste!");

        if (resumeButton == null)
            Debug.LogWarning("PauseMenuUI: brak Resume Button - pauze zamkniesz tylko Escapem.");

        Debug.Log($"PauseMenuUI gotowe. Escape obsluguje: " +
                  $"{(UIEscapeHandler.instance != null ? "UIEscapeHandler" : "TRYB AWARYJNY (brak handlera!)")}");
    }

    private void WireButtons()
    {
        Wire(resumeButton, Close);
        Wire(saveGameButton, OnSaveGameClicked);
        Wire(loadGameButton, OnLoadGameClicked);
        Wire(optionsButton, ShowOptions);
        Wire(feedbackButton, ShowFeedback);
        Wire(quitToMenuButton, OnQuitToMenuClicked);

        Wire(optionsBackButton, ShowMainPanel);
        Wire(feedbackBackButton, ShowMainPanel);

        // "Tak" wpinane jest dopiero w ShowConfirm - patrz komentarz tam
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
    }

    // ===============================================================
    // OTWIERANIE I ZAMYKANIE
    // ===============================================================
    public void Open()
    {
        if (IsOpen) return;

        // Nie otwieramy w trakcie przechodzenia miedzy lokacjami
        if (LocationManager.instance != null && LocationManager.instance.IsTransitioning) return;

        Root.SetActive(true);
        ShowMainPanel();

        if (loadGameButton != null) loadGameButton.interactable = MainMenuUI.HasSave();
        SetSaveStatus("");

        UILock.Set("Pause", true);
        if (pauseTimeScale) Time.timeScale = 0f;

        SoundManager.Play(openSounds, uiVolume);
    }

    public void Close()
    {
        if (!IsOpen) return;

        Root.SetActive(false);

        UILock.Set("Pause", false);
        if (pauseTimeScale) Time.timeScale = 1f;
    }

    // Wolane przez UIEscapeHandler, gdy menu pauzy jest otwarte.
    // Escape cofa o jeden poziom, a z panelu glownego zamyka pauze.
    public void HandleEscape()
    {
        if (confirmPanel != null && confirmPanel.activeSelf) { ShowMainPanel(); return; }
        if (optionsPanel != null && optionsPanel.activeSelf) { ShowMainPanel(); return; }
        if (feedbackPanel != null && feedbackPanel.activeSelf) { ShowMainPanel(); return; }

        Close();
    }

    // ===============================================================
    // PANELE
    // ===============================================================
    public void ShowMainPanel()
    {
        SetPanel(mainPanel, true);
        SetPanel(optionsPanel, false);
        SetPanel(feedbackPanel, false);
        SetPanel(confirmPanel, false);
    }

    public void ShowOptions()
    {
        SetPanel(mainPanel, false);
        SetPanel(optionsPanel, true);
        SetPanel(feedbackPanel, false);
    }

    public void ShowFeedback()
    {
        SetPanel(mainPanel, false);
        SetPanel(optionsPanel, false);
        SetPanel(feedbackPanel, true);
    }

    private void SetPanel(GameObject panel, bool visible)
    {
        if (panel != null) panel.SetActive(visible);
    }

    // Przycisk "Tak" dostaje zadanie DOPIERO teraz, wiec jedno okno
    // moze obslugiwac rozne pytania bez ryzyka pomylki.
    private void ShowConfirm(string message, System.Action onYes)
    {
        if (confirmPanel == null) { onYes(); return; }

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
        SetPanel(feedbackPanel, false);
        SetPanel(confirmPanel, true);
    }

    // ===============================================================
    // AKCJE
    // ===============================================================
    private void OnSaveGameClicked()
    {
        bool ok = SaveManager.SaveGame(GameSession.SaveSlot);

        SetSaveStatus(ok ? saveSuccessMessage : saveFailMessage);
        if (ok) SoundManager.Play(saveSounds, uiVolume);

        // Po zapisie "Wczytaj gre" ma juz co wczytywac
        if (loadGameButton != null) loadGameButton.interactable = SaveManager.HasSave(GameSession.SaveSlot);
    }

    private void SetSaveStatus(string text)
    {
        if (saveStatusText == null) return;

        saveStatusText.text = text;
        saveStatusText.gameObject.SetActive(!string.IsNullOrEmpty(text));
        saveStatusTimer = string.IsNullOrEmpty(text) ? 0f : saveStatusTime;
    }

    private void OnLoadGameClicked()
    {
        if (!MainMenuUI.HasSave())
        {
            Debug.Log("Brak zapisu do wczytania.");
            return;
        }

        ShowConfirm(loadGameMessage, () =>
        {
            GameSession.IsLoadingSave = true;
            QuitToMainMenu();   // menu od razu wystartuje gre z zapisu
        });
    }

    private void OnQuitToMenuClicked()
    {
        ShowConfirm(quitToMenuMessage, () =>
        {
            GameSession.IsLoadingSave = false;
            QuitToMainMenu();
        });
    }

    public void QuitToMainMenu()
    {
        if (isQuitting) return;
        isQuitting = true;

        // Korutyna ginie razem z wylaczonym obiektem, wiec upewniamy sie,
        // ze korzen jest wlaczony, zanim ja odpalimy.
        if (!Root.activeSelf) Root.SetActive(true);

        StartCoroutine(QuitRoutine());
    }

    private IEnumerator QuitRoutine()
    {
        // 1. Czas musi wrocic do normy PRZED zmiana sceny,
        //    inaczej menu glowne obudzi sie zamrozone.
        Time.timeScale = 1f;

        // 2. Zdejmujemy wszystkie blokady ruchu - nowa sesja zaczyna od zera
        UILock.ClearAll();

        // Stan swiata zostaje w pamieci tylko wtedy, gdy zaraz wczytamy zapis.
        // Przy zwyklym wyjsciu do menu czyscimy go, zeby nowa gra nie
        // odziedziczyla otwartych skrzyn po poprzedniej rozgrywce.
        if (!GameSession.IsLoadingSave)
        {
            WorldState.Clear();
            RespawnPoint.Clear();
            QuestManager.Clear();
        }

        // 3. TU BYL BLAD: wolalismy Close(), ktore wylacza PauseRoot.
        //    A PauseRoot to obiekt, na ktorym dziala TA korutyna - Unity
        //    przerywalo ja natychmiast i scena nigdy sie nie zmieniala.
        //    Chowamy wiec tylko panele w srodku, a korzen zostaje zywy
        //    az do przeladowania sceny.
        SetPanel(mainPanel, false);
        SetPanel(optionsPanel, false);
        SetPanel(feedbackPanel, false);
        SetPanel(confirmPanel, false);

        // 4. Sprzatamy obiekty z DontDestroyOnLoad.
        //    One NIE gina przy zmianie sceny, wiec bez tego druga rozgrywka
        //    wystartowalaby ze starym LocationManagerem i jego stanem.
        if (LocationManager.instance != null)
        {
            yield return LocationManager.instance.Shutdown();
        }

        // 5. Muzyka wraca do motywu menu
        if (MusicPlayer.instance != null) MusicPlayer.instance.PlayMenuTheme();

        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        // Gdyby scena zniknela z otwarta pauza - nie zostawiamy zamrozonego czasu
        if (instance == this)
        {
            Time.timeScale = 1f;
            instance = null;
        }
    }
}