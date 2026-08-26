using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// EKRAN SMIERCI.
//
// Powies ten skrypt na obiekcie CANVAS (nie na samym panelu!), a w pole
// 'Death Root' wpnij nakladke. Dzieki temu skrypt zyje zawsze i moze
// zareagowac na smierc, nawet gdy nakladka jest schowana.
public class DeathScreenUI : MonoBehaviour
{
    public static DeathScreenUI instance;

    [Header("Nakladka")]
    [Tooltip("Obiekt z czerwonym tlem i panelem. Startuje wylaczony.")]
    public GameObject deathRoot;

    [Tooltip("Czerwone tlo na caly ekran - Image z komponentem CanvasGroup.")]
    public CanvasGroup bloodOverlay;

    [Tooltip("Panel z napisem i przyciskami.")]
    public GameObject deathPanel;

    [Header("Teksty")]
    public TMPro.TextMeshProUGUI titleText;
    public TMPro.TextMeshProUGUI messageText;

    [TextArea] public string titleMessage = "Zginales!";
    [TextArea] public string deathMessage = "Mozesz sie ratowac w najblizszym miescie.";

    [Tooltip("Uzywane, gdy gracz nie odwiedzil zadnego punktu odrodzenia.")]
    [TextArea] public string noRespawnMessage = "Nie znasz jeszcze zadnego bezpiecznego miejsca.";

    [Header("Przyciski")]
    public Button reviveButton;
    public Button mainMenuButton;

    [Header("HUD do wygaszenia")]
    [Tooltip("Paski zycia, plecak, minimapa - wszystko, co ma zniknac po smierci.")]
    public GameObject[] hudToHide;

    [Header("Wyglad")]
    public Color bloodColor = new Color(0.5f, 0.02f, 0.02f, 0.88f);
    [Tooltip("Jak dlugo czerwien narasta.")]
    public float fadeInTime = 1.2f;

    [Tooltip("Opoznienie, zanim pojawi sie panel - daje chwile na przetrawienie smierci.")]
    public float panelDelay = 0.8f;

    [Header("Zasady Odrodzenia")]
    [Tooltip("Ile procent maksymalnego zdrowia dostaje gracz po ratunku.")]
    [Range(0.1f, 1f)] public float reviveHealthPercent = 0.5f;

    [Tooltip("Jaka czesc zlota gracz traci przy smierci. 0 = nic nie traci.")]
    [Range(0f, 1f)] public float goldLossPercent = 0.1f;

    [Header("Sceny")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Dzwiek")]
    public AudioClip[] deathSounds;
    public AudioClip[] clickSounds;
    [Range(0f, 1f)] public float uiVolume = 0.6f;

    private bool isBusy;

    // Stan kazdego obiektu z listy SPRZED smierci - zeby odtworzyc
    // dokladnie to, co bylo, a nie wlaczyc wszystkiego na raz.
    private bool[] hudPreviousStates;

    public bool IsShowing { get { return deathRoot != null && deathRoot.activeSelf; } }

    void Awake()
    {
        instance = this;

        WireButton(reviveButton, OnReviveClicked);
        WireButton(mainMenuButton, OnMainMenuClicked);
    }

    void OnEnable()
    {
        // Podpinamy sie do gracza. Moze go jeszcze nie byc - stad ponawianie.
        StartCoroutine(SubscribeWhenReady());
    }

    void OnDisable()
    {
        if (PlayerStats.instance != null) PlayerStats.instance.onPlayerDied -= HandlePlayerDeath;
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (PlayerStats.instance == null) yield return null;

        PlayerStats.instance.onPlayerDied -= HandlePlayerDeath;   // bez duplikatow
        PlayerStats.instance.onPlayerDied += HandlePlayerDeath;
    }

    void Start()
    {
        if (deathRoot != null) deathRoot.SetActive(false);

        CheckHudList();
    }

    // Niektore obiekty MUSZA zostac wlaczone, bo ich Awake ustawia singleton.
    // Wylaczenie ich psuje tooltip albo menu kontekstowe.
    private void CheckHudList()
    {
        if (hudToHide == null) return;

        foreach (GameObject obj in hudToHide)
        {
            if (obj == null) continue;

            if (obj.GetComponent<InventoryTooltip>() != null ||
                obj.GetComponent<ContextMenuUI>() != null ||
                obj.GetComponent<PauseMenuUI>() != null ||
                obj.GetComponent<DeathScreenUI>() != null)
            {
                Debug.LogWarning($"DeathScreenUI: '{obj.name}' NIE powinien byc na liscie " +
                                 "'Hud To Hide' - te okna chowaja sie same i musza zostac " +
                                 "wlaczone, inaczej ich singletony przestana dzialac.");
            }
        }
    }

    private void WireButton(Button button, UnityEngine.Events.UnityAction action)
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
    // SMIERC
    // ===============================================================
    private void HandlePlayerDeath()
    {
        if (IsShowing) return;
        StartCoroutine(ShowDeathScreen());
    }

    private IEnumerator ShowDeathScreen()
    {
        isBusy = true;

        // 1. Zatrzymujemy swiat i blokujemy ruch
        UILock.Set("Death", true);
        Time.timeScale = 0f;

        // 2. Chowamy wszystkie inne okna, zeby nie wisialy nad ekranem smierci
        CloseOtherWindows();
        HideHud(true);

        SoundManager.Play(deathSounds, uiVolume);

        // 3. Czerwien narasta
        if (deathRoot != null) deathRoot.SetActive(true);
        if (deathPanel != null) deathPanel.SetActive(false);

        yield return FadeBlood(0f, 1f, fadeInTime);

        // 4. Dopiero teraz panel z wyborem
        yield return WaitUnscaled(panelDelay);

        UpdateTexts();
        if (deathPanel != null) deathPanel.SetActive(true);

        isBusy = false;
    }

    private void UpdateTexts()
    {
        if (titleText != null) titleText.text = titleMessage;

        bool canRevive = RespawnPoint.HasRespawnPoint || LocationManager.instance != null;

        if (messageText != null)
        {
            if (RespawnPoint.HasRespawnPoint && !string.IsNullOrEmpty(RespawnPoint.CurrentName))
                messageText.text = $"{deathMessage}\n({RespawnPoint.CurrentName})";
            else if (canRevive)
                messageText.text = deathMessage;
            else
                messageText.text = noRespawnMessage;
        }

        if (reviveButton != null) reviveButton.interactable = canRevive;
    }

    // Ekran smierci ma byc na samym wierzchu
    private void CloseOtherWindows()
    {
        if (PauseMenuUI.instance != null && PauseMenuUI.instance.IsOpen) PauseMenuUI.instance.Close();
        if (ContextMenuUI.instance != null) ContextMenuUI.instance.CloseMenu();
        if (InventoryTooltip.instance != null) InventoryTooltip.instance.ForceHide();

        if (InventoryUI.instance != null && InventoryUI.instance.IsOpen)
            InventoryUI.instance.CloseInventory();

        if (StatsUI.instance != null && StatsUI.instance.IsOpen)
            StatsUI.instance.CloseStatsWindow();
    }

    // TU BYL BLAD: przy odrodzeniu wszystko wracalo jako WLACZONE.
    // Jesli w liscie byly okna normalnie schowane (opcje, dialog, potwierdzenia),
    // to po ratunku wyskakiwaly wszystkie naraz.
    private void HideHud(bool hidden)
    {
        if (hudToHide == null) return;

        if (hidden)
        {
            // Zapamietujemy, co bylo wlaczone, i dopiero potem chowamy
            hudPreviousStates = new bool[hudToHide.Length];

            for (int i = 0; i < hudToHide.Length; i++)
            {
                if (hudToHide[i] == null) continue;

                hudPreviousStates[i] = hudToHide[i].activeSelf;
                hudToHide[i].SetActive(false);
            }
            return;
        }

        // Odtwarzamy DOKLADNIE poprzedni stan
        if (hudPreviousStates == null) return;

        for (int i = 0; i < hudToHide.Length && i < hudPreviousStates.Length; i++)
        {
            if (hudToHide[i] == null) continue;
            hudToHide[i].SetActive(hudPreviousStates[i]);
        }

        hudPreviousStates = null;
    }

    // ===============================================================
    // RATUNEK
    // ===============================================================
    private void OnReviveClicked()
    {
        if (isBusy) return;
        StartCoroutine(ReviveRoutine());
    }

    private IEnumerator ReviveRoutine()
    {
        isBusy = true;

        if (deathPanel != null) deathPanel.SetActive(false);

        // Kara za smierc - czesc zlota zostaje na polu bitwy
        ApplyDeathPenalty();

        // Czas musi ruszyc PRZED przenosinami, bo LocationManager
        // uzywa korutyn z WaitForSeconds.
        Time.timeScale = 1f;

        // Przenosimy gracza do ostatniego bezpiecznego miejsca
        yield return TeleportToRespawn();

        // Dopiero teraz stawiamy go na nogi
        if (PlayerStats.instance != null) PlayerStats.instance.Revive(reviveHealthPercent);

        if (InventoryUI.instance != null) InventoryUI.instance.UpdatePlayerInfoUI();

        // Czerwien opada
        yield return FadeBlood(1f, 0f, fadeInTime * 0.6f);

        if (deathRoot != null) deathRoot.SetActive(false);
        HideHud(false);

        UILock.Set("Death", false);
        isBusy = false;
    }

    private void ApplyDeathPenalty()
    {
        if (goldLossPercent <= 0f || PlayerStats.instance == null) return;

        int lost = Mathf.RoundToInt(PlayerStats.instance.currentMoney * goldLossPercent);
        if (lost <= 0) return;

        PlayerStats.instance.currentMoney -= lost;
        Debug.Log($"Strata przy smierci: {lost} zlota.");
    }

    private IEnumerator TeleportToRespawn()
    {
        if (LocationManager.instance == null) yield break;

        string scene = RespawnPoint.CurrentScene;
        string spawnId = RespawnPoint.CurrentSpawnId;

        // Gracz nie odwiedzil zadnego punktu - wracamy na start gry
        if (string.IsNullOrEmpty(scene))
        {
            scene = LocationManager.instance.startingLocation;
            spawnId = LocationManager.instance.startingSpawnId;
        }

        LocationManager.instance.GoTo(scene, spawnId);

        // Czekamy, az przejscie miedzy lokacjami sie skonczy
        while (LocationManager.instance.IsTransitioning) yield return null;
    }

    // ===============================================================
    // MENU GLOWNE
    // ===============================================================
    private void OnMainMenuClicked()
    {
        if (isBusy) return;
        StartCoroutine(QuitToMenuRoutine());
    }

    private IEnumerator QuitToMenuRoutine()
    {
        isBusy = true;

        Time.timeScale = 1f;
        UILock.ClearAll();

        if (LocationManager.instance != null)
            yield return LocationManager.instance.Shutdown();

        if (MusicPlayer.instance != null) MusicPlayer.instance.PlayMenuTheme();

        RespawnPoint.Clear();

        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    // ===============================================================
    // POMOCNICZE
    // ===============================================================
    private IEnumerator FadeBlood(float from, float to, float duration)
    {
        if (bloodOverlay == null) yield break;

        Image img = bloodOverlay.GetComponent<Image>();
        if (img != null) img.color = bloodColor;

        bloodOverlay.blocksRaycasts = true;

        float t = 0f;
        while (t < 1f)
        {
            // unscaledDeltaTime - czas gry stoi, a ekran i tak ma dzialac
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration);
            bloodOverlay.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t));
            yield return null;
        }

        bloodOverlay.alpha = to;
    }

    private IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            Time.timeScale = 1f;
            instance = null;
        }
    }
}