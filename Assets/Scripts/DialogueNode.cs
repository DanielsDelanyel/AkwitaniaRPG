using UnityEngine;

// Pojedyncza odpowiedz gracza w rozmowie
[System.Serializable]
public class DialogueOption
{
    [TextArea(1, 2)]
    public string text;              // Co powie gracz

    public DialogueNode nextNode;    // Dokad prowadzi (puste = zamyka okno)

    public int affinityChange = 0;   // Zmiana sympatii po wyborze tej opcji

    // ===============================================================
    // ZADANIA
    // ===============================================================
    [Header("Zadania")]
    [Tooltip("Wybranie tej opcji ROZPOCZYNA to zadanie.")]
    public Quest questToStart;

    [Tooltip("Wybranie tej opcji ODDAJE to zadanie (jesli cele sa wykonane).")]
    public Quest questToComplete;

    [Header("Warunek pokazania opcji")]
    [Tooltip("Opcja pojawi sie TYLKO wtedy, gdy to zadanie ma stan podany nizej. " +
             "Zostaw puste, jesli opcja ma byc zawsze widoczna.\n\n" +
             "Przyklad: opcja 'Mam twoje truskawki!' widoczna tylko przy " +
             "zadaniu w stanie ReadyToTurnIn.")]
    public Quest requiredQuest;

    public QuestState requiredState = QuestState.Active;

    // Czy ta opcja ma sie teraz pokazac graczowi?
    public bool IsVisible()
    {
        if (requiredQuest == null) return true;

        return QuestManager.GetState(requiredQuest) == requiredState;
    }
}

// Tworzy pozycje w menu Unity, by latwo dodawac nowe wezly rozmowy
[CreateAssetMenu(fileName = "Nowy Dialog", menuName = "System Dialogowy/Wezel Rozmowy")]
public class DialogueNode : ScriptableObject
{
    [TextArea(3, 10)]
    public string npcText;           // Co mowi NPC w tym kroku

    public DialogueOption[] options; // Lista odpowiedzi gracza
}
