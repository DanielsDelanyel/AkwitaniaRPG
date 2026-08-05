using UnityEngine;

// Ten fragment definiuje, jak wygl¹da pojedyncza odpowiedŸ gracza
[System.Serializable]
public class DialogueOption
{
    [TextArea(1, 2)]
    public string text; // Co powie gracz
    public DialogueNode nextNode; // Do jakiego wêz³a to prowadzi (jeœli puste = zamyka okno)
    public int affinityChange = 0; // Zmiana sympatii po wyborze tej opcji (np. +10 za komplement, -10 za obrazê)
}

// Ten fragment tworzy pozycjê w menu Unity, by ³atwo tworzyæ nowe dialogi!
[CreateAssetMenu(fileName = "Nowy Dialog", menuName = "System Dialogowy/Wêze³ Rozmowy")]
public class DialogueNode : ScriptableObject
{
    [TextArea(3, 10)]
    public string npcText; // Co mówi NPC w tym kroku
    public DialogueOption[] options; // Lista odpowiedzi gracza
}