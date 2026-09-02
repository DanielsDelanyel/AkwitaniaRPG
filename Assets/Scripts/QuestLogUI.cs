using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// PANEL DZIENNIKA ZADAN.
//
// Gorny rzad zakladek (Glowne / Poboczne / Zlecenia / ...) filtruje liste
// po Quest.category. Klikniecie wpisu na liscie pokazuje pelny opis,
// cele i nagrode w panelu szczegolow.
//
// Powies to na obiekcie w Canvasie (np. obok InventoryUI/StatsUI) i podepnij
// wszystkie pola w Inspectorze - patrz instrukcja konfiguracji.
public class QuestLogUI : MonoBehaviour
{
    public static QuestLogUI instance;

    // Jedna zakladka = jeden przycisk + kategoria, ktora ma pokazywac.
    [System.Serializable]
    public class CategoryTab
    {
        [Tooltip("Ignorowane, jesli zaznaczone jest 'Show Completed Instead' ponizej.")]
        public QuestCategory category;

        [Tooltip("Ta zakladka pokazuje WSZYSTKIE ukonczone zadania (ze wszystkich kategorii razem), " +
                 "zamiast filtrowac aktywne zadania po polu Category powyzej.")]
        public bool showCompletedInstead = false;

        public Button tabButton;

        [Tooltip("Tekst pokazywany, gdy w tej zakladce nie ma zadnego zadania. " +
                 "Np. 'Brak aktywnych zadan glownych'.")]
        public string emptyMessage = "Brak zadan w tej kategorii";

        [Tooltip("Opcjonalne: obiekt podswietlajacy aktywna zakladke (np. podkreslenie). Moze zostac puste.")]
        public GameObject highlightWhenActive;
    }

    [Header("Okno")]
    public GameObject questLogWindow;

    [Header("Sterowanie")]
    [Tooltip("Klawisz otwierajacy/zamykajacy dziennik. Jesli masz juz wlasny " +
             "przycisk HUD albo menedzer wejscia, olej to pole i wywoluj " +
             "OpenQuestLog()/CloseQuestLog() stamtad.")]
    public KeyCode toggleKey = KeyCode.L;

    [Header("Zakladki")]
    public CategoryTab[] tabs;

    [Header("Lista zadan")]
    [Tooltip("Parent z Vertical Layout Group, do ktorego trafiaja wpisy listy.")]
    public Transform listContent;

    public QuestLogEntry entryPrefab;

    [Tooltip("Tekst pokazywany, gdy w danej zakladce nie ma zadnego zadania.")]
    public TextMeshProUGUI emptyStateText;

    [Header("Szczegoly wybranego zadania")]
    public GameObject detailsPanel;
    public TextMeshProUGUI detailsTitle;
    public TextMeshProUGUI detailsDescription;
    public TextMeshProUGUI detailsObjectives;
    public TextMeshProUGUI detailsReward;

    private CategoryTab currentTab;
    private readonly List<QuestLogEntry> spawnedEntries = new List<QuestLogEntry>();

    public bool IsOpen { get { return questLogWindow != null && questLogWindow.activeSelf; } }

    void Awake()
    {
        instance = this;
    }

    void OnEnable()
    {
        QuestManager.onQuestStarted += OnQuestsChanged;
        QuestManager.onQuestUpdated += OnQuestsChanged;
        QuestManager.onQuestCompleted += OnQuestsChanged;
    }

    void OnDisable()
    {
        QuestManager.onQuestStarted -= OnQuestsChanged;
        QuestManager.onQuestUpdated -= OnQuestsChanged;
        QuestManager.onQuestCompleted -= OnQuestsChanged;
    }

    void Start()
    {
        if (tabs != null)
        {
            foreach (CategoryTab tab in tabs)
            {
                if (tab == null || tab.tabButton == null) continue;

                CategoryTab captured = tab; // kazdy przycisk pamieta SWOJA zakladke
                tab.tabButton.onClick.AddListener(() => SelectTab(captured));
            }
        }

        if (questLogWindow != null) questLogWindow.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(toggleKey)) return;

        if (IsOpen)
        {
            CloseQuestLog();
            return;
        }

        // Nie otwieraj na sile, jesli inne okno (dialog, sklep, ekwipunek...) juz trzyma blokade
        if (UILock.IsLocked) return;

        OpenQuestLog();
    }

    // ===============================================================
    // OTWIERANIE / ZAMYKANIE
    // ===============================================================
    public void OpenQuestLog()
    {
        if (questLogWindow == null) return;

        questLogWindow.SetActive(true);
        UILock.Set("QuestLog", true);

        CategoryTab startTab = (tabs != null && tabs.Length > 0) ? tabs[0] : currentTab;
        SelectTab(startTab);
    }

    public void CloseQuestLog()
    {
        if (questLogWindow != null) questLogWindow.SetActive(false);
        UILock.Set("QuestLog", false);
    }

    private void OnQuestsChanged(QuestProgress progress)
    {
        // Odswiez liste na biezaco, jesli okno jest akurat otwarte
        if (IsOpen) RefreshList();
    }

    // ===============================================================
    // ZAKLADKI I LISTA
    // ===============================================================
    public void SelectTab(CategoryTab tab)
    {
        if (tab == null) return;
        currentTab = tab;

        if (tabs != null)
        {
            foreach (CategoryTab t in tabs)
            {
                if (t == null || t.highlightWhenActive == null) continue;
                t.highlightWhenActive.SetActive(t == tab);
            }
        }

        RefreshList();
    }

    private void RefreshList()
    {
        foreach (QuestLogEntry entry in spawnedEntries)
        {
            if (entry != null) Destroy(entry.gameObject);
        }
        spawnedEntries.Clear();

        if (currentTab == null) return;

        List<QuestProgress> matching = new List<QuestProgress>();

        if (currentTab.showCompletedInstead)
        {
            // Ukonczone zadania ze WSZYSTKICH kategorii razem, bez filtrowania po Category.
            matching.AddRange(QuestManager.GetCompletedQuests());
        }
        else
        {
            foreach (QuestProgress progress in QuestManager.GetActiveQuests())
            {
                Quest def = progress.Definition;
                if (def != null && def.category == currentTab.category) matching.Add(progress);
            }
        }

        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(matching.Count == 0);
            if (matching.Count == 0) emptyStateText.text = currentTab.emptyMessage;
        }

        foreach (QuestProgress progress in matching)
        {
            if (entryPrefab == null || listContent == null) break;

            QuestLogEntry entry = Instantiate(entryPrefab, listContent);
            entry.Setup(progress, this);
            spawnedEntries.Add(entry);
        }

        if (matching.Count > 0) ShowDetails(matching[0]);
        else ClearDetails();
    }

    // ===============================================================
    // SZCZEGOLY
    // ===============================================================
    public void ShowDetails(QuestProgress progress)
    {
        Quest def = progress != null ? progress.Definition : null;
        if (def == null)
        {
            ClearDetails();
            return;
        }

        if (detailsPanel != null) detailsPanel.SetActive(true);
        if (detailsTitle != null) detailsTitle.text = def.title;
        if (detailsDescription != null) detailsDescription.text = def.description;

        if (detailsObjectives != null)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < def.ObjectiveCount; i++)
            {
                if (!progress.IsObjectiveVisible(i)) continue;

                string line = progress.GetObjectiveText(i);
                bool done = progress.IsObjectiveComplete(i);

                // <s> = przekreslenie tekstu we wbudowanym rich texcie TextMeshPro
                sb.AppendLine(done ? $"<s>{line}</s>" : line);
            }
            detailsObjectives.text = sb.ToString().TrimEnd('\n');
        }

        if (detailsReward != null) detailsReward.text = BuildRewardText(def.reward);
    }

    private string BuildRewardText(QuestReward reward)
    {
        if (reward == null) return "";

        List<string> parts = new List<string>();
        if (reward.experience > 0) parts.Add($"{reward.experience} exp");
        if (reward.gold > 0) parts.Add($"{reward.gold} zlota");

        if (reward.items != null)
        {
            foreach (ItemData item in reward.items)
            {
                if (item != null) parts.Add(item.itemName);
            }
        }

        return string.Join("   ", parts);
    }

    private void ClearDetails()
    {
        if (detailsPanel != null) detailsPanel.SetActive(false);

        // Kasujemy tez same teksty - inaczej zostaja widoczne "pod" pustym stanem,
        // skoro Details Panel moze byc niepodpiety (u Ciebie tak wlasnie jest).
        if (detailsTitle != null) detailsTitle.text = "";
        if (detailsDescription != null) detailsDescription.text = "";
        if (detailsObjectives != null) detailsObjectives.text = "";
        if (detailsReward != null) detailsReward.text = "";
    }
}
