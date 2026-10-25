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

    // ===============================================================
    // NOWE: PAMIEC ROZMOWY (znaczniki w WorldState)
    // Znaczniki to zwykle napisy, np. "Lassi_przedstawiony". Trafiaja do
    // zapisu gry razem z otwartymi skrzyniami, wiec postac pamieta je
    // takze po zmianie lokacji i po wczytaniu gry.
    // ===============================================================
    [Header("Pamiec (znaczniki swiata)")]
    [Tooltip("Wybranie tej opcji USTAWIA ten znacznik, np. 'Lassi_przedstawiony'. " +
             "Zostaw puste, jesli opcja nie ma niczego zapamietywac.")]
    public string setFlag;

    [Tooltip("Opcja ZNIKA, gdy ten znacznik jest ustawiony. Wpisz tu ten sam znacznik " +
             "co w 'Set Flag', zeby opcja byla jednorazowa (np. przedstawienie sie).")]
    public string hideIfFlag;

    [Tooltip("Opcja pojawia sie TYLKO, gdy ten znacznik jest ustawiony " +
             "(np. 'Pamietasz mnie?' dopiero po przedstawieniu sie).")]
    public string showOnlyIfFlag;

    // Czy ta opcja ma sie teraz pokazac graczowi?
    public bool IsVisible()
    {
        if (!string.IsNullOrEmpty(hideIfFlag) && WorldState.HasFlag(hideIfFlag)) return false;
        if (!string.IsNullOrEmpty(showOnlyIfFlag) && !WorldState.HasFlag(showOnlyIfFlag)) return false;

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
