using System;
using System.Collections.Generic;
using UnityEngine;

// POSTEP POJEDYNCZEGO ZADANIA.
// Ta klasa trafia prosto do zapisu gry, wiec sklada sie z pol publicznych.
[Serializable]
public class QuestProgress
{
    public string questId;
    public QuestState state = QuestState.NotStarted;

    [Tooltip("Licznik dla kazdego celu - w tej samej kolejnosci co tablica objectives.")]
    public List<int> counts = new List<int>();

    [Tooltip("Ktore cele zostaly odkryte (dotyczy tych oznaczonych jako hidden).")]
    public List<bool> revealed = new List<bool>();

    // --- Referencja na definicje. Nie zapisujemy jej - odnajdujemy po questId. ---
    [NonSerialized] private Quest cachedQuest;

    public Quest Definition
    {
        get
        {
            if (cachedQuest == null) cachedQuest = QuestDatabase.Instance?.Find(questId);
            return cachedQuest;
        }
        set { cachedQuest = value; }
    }

    public QuestProgress() { }

    public QuestProgress(Quest quest)
    {
        Definition = quest;
        questId = quest.GetId();
        state = QuestState.Active;

        EnsureSize(quest.ObjectiveCount);

        // Cele ukryte startuja jako nieodkryte
        for (int i = 0; i < quest.ObjectiveCount; i++)
            revealed[i] = !quest.objectives[i].hidden;
    }

    // Dopasowuje dlugosc list do liczby celow - chroni przed bledami,
    // gdy dodasz cel do zadania juz po zapisaniu gry.
    public void EnsureSize(int count)
    {
        while (counts.Count < count) counts.Add(0);
        while (revealed.Count < count) revealed.Add(true);
    }

    // ===============================================================
    // ODCZYT POSTEPU
    // ===============================================================
    public int GetCount(int objectiveIndex)
    {
        if (objectiveIndex < 0 || objectiveIndex >= counts.Count) return 0;
        return counts[objectiveIndex];
    }

    public bool IsObjectiveComplete(int objectiveIndex)
    {
        Quest q = Definition;
        if (q == null || objectiveIndex >= q.ObjectiveCount) return false;

        return GetCount(objectiveIndex) >= q.objectives[objectiveIndex].requiredAmount;
    }

    public bool IsObjectiveVisible(int objectiveIndex)
    {
        if (objectiveIndex < 0 || objectiveIndex >= revealed.Count) return true;
        return revealed[objectiveIndex];
    }

    public bool AreAllObjectivesComplete()
    {
        Quest q = Definition;
        if (q == null) return false;

        for (int i = 0; i < q.ObjectiveCount; i++)
        {
            if (!IsObjectiveComplete(i)) return false;
        }
        return true;
    }

    // Przy celach po kolei liczy sie tylko pierwszy niewykonany
    public int GetActiveObjectiveIndex()
    {
        Quest q = Definition;
        if (q == null) return -1;

        for (int i = 0; i < q.ObjectiveCount; i++)
        {
            if (!IsObjectiveComplete(i)) return i;
        }
        return -1;
    }

    // Ulamek 0-1 dla calego zadania - przyda sie paskowi postepu
    public float GetOverallProgress()
    {
        Quest q = Definition;
        if (q == null || q.ObjectiveCount == 0) return 0f;

        float sum = 0f;
        for (int i = 0; i < q.ObjectiveCount; i++)
        {
            int required = Mathf.Max(1, q.objectives[i].requiredAmount);
            sum += Mathf.Clamp01((float)GetCount(i) / required);
        }

        return sum / q.ObjectiveCount;
    }

    // Gotowy tekst dla panelu zadan, np. "Ubij dziki  3/5"
    public string GetObjectiveText(int objectiveIndex)
    {
        Quest q = Definition;
        if (q == null || objectiveIndex >= q.ObjectiveCount) return "";

        QuestObjective obj = q.objectives[objectiveIndex];

        if (!obj.ShowsProgressBar) return obj.description;

        int current = Mathf.Min(GetCount(objectiveIndex), obj.requiredAmount);
        return $"{obj.description}  {current}/{obj.requiredAmount}";
    }

    // ===============================================================
    // ZMIANA POSTEPU
    // ===============================================================

    // Zwraca true, jesli licznik faktycznie sie zmienil
    public bool AddProgress(int objectiveIndex, int amount = 1)
    {
        Quest q = Definition;
        if (q == null || objectiveIndex < 0 || objectiveIndex >= q.ObjectiveCount) return false;

        EnsureSize(q.ObjectiveCount);

        int required = q.objectives[objectiveIndex].requiredAmount;
        if (counts[objectiveIndex] >= required) return false;   // juz zrobione

        counts[objectiveIndex] = Mathf.Min(required, counts[objectiveIndex] + amount);
        revealed[objectiveIndex] = true;   // postep odkrywa ukryty cel

        return true;
    }

    // Ustawia licznik wprost - uzywane przy celach typu "miej X w plecaku"
    public bool SetProgress(int objectiveIndex, int value)
    {
        Quest q = Definition;
        if (q == null || objectiveIndex < 0 || objectiveIndex >= q.ObjectiveCount) return false;

        EnsureSize(q.ObjectiveCount);

        int required = q.objectives[objectiveIndex].requiredAmount;
        int clamped = Mathf.Clamp(value, 0, required);

        if (counts[objectiveIndex] == clamped) return false;

        counts[objectiveIndex] = clamped;
        return true;
    }

    public void Reveal(int objectiveIndex)
    {
        EnsureSize(objectiveIndex + 1);
        revealed[objectiveIndex] = true;
    }
}
