using System.Collections.Generic;
using UnityEngine;

// ===================================================================
// RODZAJE CELOW
// ===================================================================
public enum ObjectiveType
{
    TalkToNpc,       // porozmawiaj z postacia
    KillCreature,    // zabij X sztuk danego gatunku
    KillNamed,       // pokonaj KONKRETNEGO przeciwnika (boss)
    CollectItem,     // miej X sztuk przedmiotu w plecaku
    DeliverItem,     // zanies przedmiot do postaci
    ReachLocation,   // wejdz do lokacji
    OpenChest,       // otworz konkretna skrzynie
    Custom           // wlasny znacznik, zglaszany recznie z kodu
}

// Pojedynczy cel zadania
[System.Serializable]
public class QuestObjective
{
    [Tooltip("Tekst pokazywany graczowi, np. 'Ubij 5 dzikow'.")]
    public string description = "Nowy cel";

    public ObjectiveType type = ObjectiveType.TalkToNpc;

    [Tooltip("Kogo/czego dotyczy:\n" +
             "TalkToNpc, DeliverItem - Unique Id postaci\n" +
             "KillCreature - Creature Name (np. 'Dzik')\n" +
             "KillNamed - Unique Id przeciwnika\n" +
             "CollectItem, DeliverItem - Item Id przedmiotu\n" +
             "ReachLocation - nazwa sceny\n" +
             "OpenChest - Unique Id skrzyni")]
    public string targetId = "";

    [Tooltip("Uzywane tylko przy DeliverItem: Unique Id postaci, ktorej niesiemy przedmiot. " +
             "Wtedy Target Id to identyfikator PRZEDMIOTU.")]
    public string deliverToNpcId = "";

    [Tooltip("Ile sztuk trzeba. Dla celow typu 'porozmawiaj' zostaw 1.")]
    [Min(1)] public int requiredAmount = 1;

    [Tooltip("Cel ukryty nie pojawia sie na liscie, dopoki nie zostanie odblokowany. " +
             "Przydatne przy zwrotach akcji.")]
    public bool hidden = false;

    // Czy ten cel pokazuje pasek postepu? Tylko te liczone maja sens.
    public bool ShowsProgressBar
    {
        get { return requiredAmount > 1; }
    }
}

// ===================================================================
// NAGRODA
// ===================================================================
[System.Serializable]
public class QuestReward
{
    public int experience = 0;
    public int gold = 0;

    [Tooltip("Przedmioty wreczane po oddaniu zadania.")]
    public ItemData[] items;

    [Tooltip("Ilosci odpowiadajace tablicy powyzej. Puste = po jednej sztuce.")]
    public int[] itemAmounts;

    [Tooltip("Zmiana sympatii u zleceniodawcy.")]
    public int affinityChange = 0;
}

public enum QuestState
{
    NotStarted,   // gracz jeszcze o nim nie wie
    Active,       // w toku
    ReadyToTurnIn,// wszystkie cele zrobione, trzeba wrocic do zleceniodawcy
    Completed,    // oddane i rozliczone
    Failed        // nieudane (na przyszlosc)
}

// ===================================================================
// KATEGORIA - decyduje, w ktorej zakladce Dziennika Zadan sie pokaze.
// Dopisujac tu nowa wartosc, pamietaj o dodaniu jej tez jako zakladki
// (CategoryTab) w Inspectorze na obiekcie QuestLogUI.
// ===================================================================
public enum QuestCategory
{
    MainQuest,   // Zadania glowne
    SideQuest,   // Zadania poboczne
    Contract     // Zlecenia
}

// ===================================================================
// ZADANIE
// ===================================================================
[CreateAssetMenu(fileName = "Nowe Zadanie", menuName = "Zadania/Zadanie")]
public class Quest : ScriptableObject
{
    [Header("Identyfikacja")]
    [Tooltip("Unikalny identyfikator do zapisu gry. Zostaw puste - wypelni sie nazwa pliku. " +
             "Po pierwszym zapisie NIE zmieniaj.")]
    public string questId = "";

    public string title = "Nowe zadanie";

    [TextArea(3, 6)]
    public string description = "Opis zadania...";

    [Header("Zleceniodawca")]
    [Tooltip("Unique Id postaci, ktorej trzeba oddac zadanie. " +
             "Puste = zadanie rozlicza sie samo po wykonaniu celow.")]
    public string turnInNpcId = "";

    [Header("Cele")]
    [Tooltip("Wszystkie musza zostac wykonane. Kolejnosc na liscie = kolejnosc wyswietlania.")]
    public QuestObjective[] objectives;

    [Tooltip("Zaznacz, jesli cele maja byc wykonywane PO KOLEI. " +
             "Odznaczone = gracz moze je robic w dowolnej kolejnosci.")]
    public bool sequentialObjectives = false;

    [Header("Nagroda")]
    public QuestReward reward;

    [Tooltip("Zabiera z plecaka przedmioty wymagane przez cele typu Collect/Deliver.")]
    public bool consumeQuestItems = true;

    [Header("Wymagania")]
    [Tooltip("Zadania, ktore trzeba ukonczyc, zanim to stanie sie dostepne.")]
    public Quest[] requiredQuests;

    [Tooltip("Minimalny poziom gracza. 0 = bez ograniczen.")]
    public int requiredLevel = 0;

    [Header("Wyglad")]
    [Tooltip("Do jakiej zakladki w Dzienniku Zadan trafia to zadanie.")]
    public QuestCategory category = QuestCategory.SideQuest;

    public string GetId()
    {
        return string.IsNullOrEmpty(questId) ? name : questId;
    }

    public int ObjectiveCount
    {
        get { return objectives != null ? objectives.Length : 0; }
    }

    // Czy gracz spelnia warunki, by w ogole dostac to zadanie?
    public bool CanBeStarted()
    {
        if (PlayerStats.instance != null && requiredLevel > 0
            && PlayerStats.instance.level < requiredLevel) return false;

        if (requiredQuests == null) return true;

        foreach (Quest required in requiredQuests)
        {
            if (required == null) continue;
            if (QuestManager.GetState(required) != QuestState.Completed) return false;
        }

        return true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrEmpty(questId)) questId = name;
    }
#endif
}
