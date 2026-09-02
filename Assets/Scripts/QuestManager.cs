using System;
using System.Collections.Generic;
using UnityEngine;

// MENEDZER ZADAN.
//
// Reszta gry tylko ZGLASZA zdarzenia ("zabito dzika", "rozmawiano z Lassi"),
// a menedzer sam sprawdza, ktore zadania to interesuje. Dzieki temu dodanie
// nowego zadania nie wymaga dotykania kodu walki czy dialogow.
//
// Powies to na pustym obiekcie w scenie Bootstrap.
public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    [Header("Ustawienia")]
    [Tooltip("Wypisuje w konsoli kazda zmiane postepu - przydatne przy testach.")]
    public bool logProgress = true;

    [Header("Dzwieki")]
    public AudioClip[] questStartedSounds;
    public AudioClip[] objectiveCompleteSounds;
    public AudioClip[] questCompleteSounds;
    [Range(0f, 1f)] public float soundVolume = 0.6f;

    // Wszystkie znane zadania, po identyfikatorze
    private static readonly Dictionary<string, QuestProgress> quests
        = new Dictionary<string, QuestProgress>();

    // ===============================================================
    // ZDARZENIA DLA UI
    // ===============================================================
    public static Action<QuestProgress> onQuestStarted;
    public static Action<QuestProgress> onQuestUpdated;
    public static Action<QuestProgress> onQuestReadyToTurnIn;
    public static Action<QuestProgress> onQuestCompleted;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ===============================================================
    // ODCZYT STANU
    // ===============================================================
    public static QuestState GetState(Quest quest)
    {
        if (quest == null) return QuestState.NotStarted;
        return GetState(quest.GetId());
    }

    public static QuestState GetState(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return QuestState.NotStarted;

        return quests.TryGetValue(questId, out QuestProgress p)
            ? p.state
            : QuestState.NotStarted;
    }

    public static QuestProgress GetProgress(Quest quest)
    {
        if (quest == null) return null;
        quests.TryGetValue(quest.GetId(), out QuestProgress p);
        return p;
    }

    public static bool IsActive(Quest quest)
    {
        QuestState s = GetState(quest);
        return s == QuestState.Active || s == QuestState.ReadyToTurnIn;
    }

    public static bool IsCompleted(Quest quest)
    {
        return GetState(quest) == QuestState.Completed;
    }

    // Lista dla panelu zadan
    public static List<QuestProgress> GetActiveQuests()
    {
        List<QuestProgress> result = new List<QuestProgress>();

        foreach (QuestProgress p in quests.Values)
        {
            if (p.state == QuestState.Active || p.state == QuestState.ReadyToTurnIn)
                result.Add(p);
        }
        return result;
    }

    public static List<QuestProgress> GetCompletedQuests()
    {
        List<QuestProgress> result = new List<QuestProgress>();

        foreach (QuestProgress p in quests.Values)
        {
            if (p.state == QuestState.Completed) result.Add(p);
        }
        return result;
    }

    // ===============================================================
    // ROZPOCZECIE
    // ===============================================================
    public static bool StartQuest(Quest quest)
    {
        if (quest == null) return false;

        string id = quest.GetId();

        if (quests.ContainsKey(id))
        {
            Log($"Zadanie '{quest.title}' jest juz znane.");
            return false;
        }

        if (!quest.CanBeStarted())
        {
            Log($"Zadanie '{quest.title}' nie jest jeszcze dostepne.");
            return false;
        }

        QuestProgress progress = new QuestProgress(quest);
        quests[id] = progress;

        Log($"NOWE ZADANIE: {quest.title}");
        PlaySound(instance != null ? instance.questStartedSounds : null);

        // Cele typu "miej X w plecaku" mogly byc juz spelnione
        RefreshInventoryObjectives();

        onQuestStarted?.Invoke(progress);
        onQuestUpdated?.Invoke(progress);

        CheckCompletion(progress);
        return true;
    }

    // ===============================================================
    // ZGLOSZENIA ZDARZEN
    // Wolane z roznych miejsc gry. Menedzer sam decyduje, co je obchodzi.
    // ===============================================================

    public static void ReportKill(Creature creature)
    {
        if (creature == null) return;

        // Gatunek - np. "Dzik"
        Report(ObjectiveType.KillCreature, creature.creatureName);

        // Konkretny osobnik - boss z UniqueId
        UniqueId uid = creature.GetComponent<UniqueId>();
        if (uid != null) Report(ObjectiveType.KillNamed, uid.Id);
    }

    public static void ReportTalk(string npcId)
    {
        Report(ObjectiveType.TalkToNpc, npcId);
    }

    public static void ReportLocation(string sceneName)
    {
        Report(ObjectiveType.ReachLocation, sceneName);
    }

    public static void ReportChestOpened(string chestId)
    {
        Report(ObjectiveType.OpenChest, chestId);
    }

    public static void ReportCustom(string customId)
    {
        Report(ObjectiveType.Custom, customId);
    }

    // Dostarczenie przedmiotu konkretnej postaci
    public static void ReportDelivery(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return;

        foreach (QuestProgress progress in quests.Values)
        {
            if (progress.state != QuestState.Active) continue;

            Quest quest = progress.Definition;
            if (quest == null) continue;

            for (int i = 0; i < quest.ObjectiveCount; i++)
            {
                QuestObjective obj = quest.objectives[i];

                if (obj.type != ObjectiveType.DeliverItem) continue;
                if (obj.deliverToNpcId != npcId) continue;
                if (!IsObjectiveReachable(progress, quest, i)) continue;

                // Czy gracz ma przy sobie to, co ma dostarczyc?
                int held = CountInInventory(obj.targetId);
                if (held < obj.requiredAmount) continue;

                if (quest.consumeQuestItems) RemoveFromInventory(obj.targetId, obj.requiredAmount);

                if (progress.SetProgress(i, obj.requiredAmount)) NotifyObjectiveDone(progress, i);
            }

            CheckCompletion(progress);
        }
    }

    // Wspolna sciezka dla prostych celow licznikowych
    private static void Report(ObjectiveType type, string targetId)
    {
        if (string.IsNullOrEmpty(targetId)) return;

        foreach (QuestProgress progress in quests.Values)
        {
            if (progress.state != QuestState.Active) continue;

            Quest quest = progress.Definition;
            if (quest == null) continue;

            bool changed = false;

            for (int i = 0; i < quest.ObjectiveCount; i++)
            {
                QuestObjective obj = quest.objectives[i];

                if (obj.type != type) continue;
                if (obj.targetId != targetId) continue;
                if (!IsObjectiveReachable(progress, quest, i)) continue;

                if (progress.AddProgress(i, 1))
                {
                    changed = true;
                    if (progress.IsObjectiveComplete(i)) NotifyObjectiveDone(progress, i);
                }
            }

            if (changed)
            {
                onQuestUpdated?.Invoke(progress);
                CheckCompletion(progress);
            }
        }
    }

    // Przy celach "po kolei" liczy sie tylko pierwszy niewykonany
    private static bool IsObjectiveReachable(QuestProgress progress, Quest quest, int index)
    {
        if (!quest.sequentialObjectives) return true;
        return progress.GetActiveObjectiveIndex() == index;
    }

    // ===============================================================
    // CELE ZALEZNE OD PLECAKA
    // Sprawdzane na zadanie, bo przedmioty mozna tez WYRZUCIC.
    // ===============================================================
    public static void RefreshInventoryObjectives()
    {
        if (InventoryUI.instance == null) return;

        foreach (QuestProgress progress in quests.Values)
        {
            if (progress.state != QuestState.Active && progress.state != QuestState.ReadyToTurnIn) continue;

            Quest quest = progress.Definition;
            if (quest == null) continue;

            bool changed = false;

            for (int i = 0; i < quest.ObjectiveCount; i++)
            {
                QuestObjective obj = quest.objectives[i];
                if (obj.type != ObjectiveType.CollectItem) continue;

                int held = CountInInventory(obj.targetId);

                if (progress.SetProgress(i, held))
                {
                    changed = true;
                    if (progress.IsObjectiveComplete(i)) NotifyObjectiveDone(progress, i);
                }
            }

            if (changed)
            {
                onQuestUpdated?.Invoke(progress);
                CheckCompletion(progress);
            }
        }
    }

    private static int CountInInventory(string itemId)
    {
        if (InventoryUI.instance == null || string.IsNullOrEmpty(itemId)) return 0;

        int total = 0;

        foreach (InventorySlot slot in InventoryUI.instance.GetBackpackSlots())
        {
            if (slot == null || slot.item == null) continue;
            if (slot.item.GetTemplateId() == itemId) total += Mathf.Max(1, slot.amount);
        }

        return total;
    }

    private static void RemoveFromInventory(string itemId, int amount)
    {
        if (InventoryUI.instance == null) return;

        int left = amount;

        foreach (InventorySlot slot in InventoryUI.instance.GetBackpackSlots())
        {
            if (left <= 0) break;
            if (slot == null || slot.item == null) continue;
            if (slot.item.GetTemplateId() != itemId) continue;

            int taken = Mathf.Min(left, slot.amount);
            slot.amount -= taken;
            left -= taken;

            if (slot.amount <= 0) slot.ClearSlot();
            else slot.UpdateSlotUI();
        }
    }

    // ===============================================================
    // ZAKONCZENIE
    // ===============================================================
    private static void CheckCompletion(QuestProgress progress)
    {
        if (progress.state != QuestState.Active) return;
        if (!progress.AreAllObjectivesComplete()) return;

        Quest quest = progress.Definition;
        if (quest == null) return;

        // Trzeba wrocic do zleceniodawcy?
        if (!string.IsNullOrEmpty(quest.turnInNpcId))
        {
            progress.state = QuestState.ReadyToTurnIn;
            Log($"Zadanie '{quest.title}' gotowe do oddania.");

            onQuestReadyToTurnIn?.Invoke(progress);
            onQuestUpdated?.Invoke(progress);
            return;
        }

        // Rozlicza sie samo
        CompleteQuest(quest);
    }

    // Oddanie zadania - wolane przez opcje dialogowa albo automatycznie
    public static bool CompleteQuest(Quest quest)
    {
        if (quest == null) return false;

        QuestProgress progress = GetProgress(quest);
        if (progress == null) return false;

        if (progress.state == QuestState.Completed) return false;
        if (!progress.AreAllObjectivesComplete())
        {
            Log($"Zadanie '{quest.title}' nie jest jeszcze wykonane.");
            return false;
        }

        // Zabieramy przedmioty wymagane przez cele zbierania
        if (quest.consumeQuestItems) ConsumeCollectedItems(quest);

        progress.state = QuestState.Completed;
        GrantReward(quest);

        Log($"ZADANIE UKONCZONE: {quest.title}");
        PlaySound(instance != null ? instance.questCompleteSounds : null);

        onQuestCompleted?.Invoke(progress);
        onQuestUpdated?.Invoke(progress);

        return true;
    }

    private static void ConsumeCollectedItems(Quest quest)
    {
        for (int i = 0; i < quest.ObjectiveCount; i++)
        {
            QuestObjective obj = quest.objectives[i];
            if (obj.type != ObjectiveType.CollectItem) continue;

            RemoveFromInventory(obj.targetId, obj.requiredAmount);
        }
    }

    private static void GrantReward(Quest quest)
    {
        QuestReward reward = quest.reward;
        if (reward == null) return;

        if (PlayerStats.instance != null)
        {
            if (reward.gold != 0) PlayerStats.instance.currentMoney += reward.gold;
            if (reward.experience > 0) PlayerStats.instance.AddExp(reward.experience);
        }

        if (reward.items != null && InventoryUI.instance != null)
        {
            for (int i = 0; i < reward.items.Length; i++)
            {
                ItemData template = reward.items[i];
                if (template == null) continue;

                int amount = 1;
                if (reward.itemAmounts != null && i < reward.itemAmounts.Length)
                    amount = Mathf.Max(1, reward.itemAmounts[i]);

                // Nagroda tez moze miec losowe statystyki
                ItemData given = ItemFactory.Create(template);

                int leftovers = InventoryUI.instance.Add(given, amount);
                if (leftovers > 0) InventoryUI.instance.ThrowItemStack(given, leftovers);
            }
        }

        if (InventoryUI.instance != null) InventoryUI.instance.UpdatePlayerInfoUI();
    }

    private static void NotifyObjectiveDone(QuestProgress progress, int index)
    {
        Log($"Cel wykonany: {progress.GetObjectiveText(index)}");
        PlaySound(instance != null ? instance.objectiveCompleteSounds : null);
    }

    // ===============================================================
    // ZAPIS I WCZYTYWANIE
    // ===============================================================
    public static List<QuestProgress> GetAllForSave()
    {
        return new List<QuestProgress>(quests.Values);
    }

    public static void LoadFrom(List<QuestProgress> saved)
    {
        quests.Clear();
        if (saved == null) return;

        foreach (QuestProgress p in saved)
        {
            if (p == null || string.IsNullOrEmpty(p.questId)) continue;

            // Zadanie moglo dostac nowe cele od czasu zapisu
            Quest def = p.Definition;
            if (def != null) p.EnsureSize(def.ObjectiveCount);

            quests[p.questId] = p;
        }

        Debug.Log($"Wczytano {quests.Count} zadan.");
    }

    public static void Clear()
    {
        quests.Clear();
    }

    // ===============================================================
    // POMOCNICZE
    // ===============================================================
    private static void Log(string message)
    {
        if (instance == null || instance.logProgress) Debug.Log($"[Zadania] {message}");
    }

    private static void PlaySound(AudioClip[] clips)
    {
        if (instance != null) SoundManager.Play(clips, instance.soundVolume);
    }
}
