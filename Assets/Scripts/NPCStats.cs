using UnityEngine;

[System.Serializable]
public class GiftPreference
{
    public ItemData item;
    public int affinityModifier; // Ile punktów dodaje/odejmuje ten konkretny prezent
}

// Definiujemy mo¿liwe statusy relacji
public enum RelationshipStatus
{
    Wrog,           // Poni¿ej -50
    Nieznajomy,     // Od -49 do 9
    Znajomy,        // Od 10 do 49
    Przyjaciel,     // Od 50 do 89
    BratniaDusza    // Od 90 do 100
}

public class NPCStats : MonoBehaviour
{
    [Header("Identyfikacja")]
    public string npcName = "Lassi";
    public Sprite portrait;

    // U¿ywamy stringa, a nie Enuma, ¿ebyœ móg³ wpisaæ tu DOWOLN¥ profesjê (nawet tak¹, która mechanicznie nie istnieje, np. "Zielarka", "¯ebrak")
    public string profession = "Brak";

    [Header("Poziom i Ekonomia")]
    public int level = 1;
    public int money = 50;

    [Header("Sklep")]
    public ItemData[] shopItems; // Co ten kupiec ma na sprzeda¿?

    [Header("Statystyki Bazowe")]
    public int baseSTR = 5;
    public int baseWIT = 5;
    public int baseINT = 5;
    public int baseZR = 5;
    public int baseCHAR = 5;

    [Header("Relacje z Graczem")]
    [Range(-100, 100)] // Ten atrybut tworzy fajny suwak w Inspektorze Unity!
    public int affinity = 0;

    // Zmienna widoczna w Inspektorze (tylko do odczytu), aktualizowana automatycznie
    public RelationshipStatus currentStatus = RelationshipStatus.Nieznajomy;

    [Header("Preferencje Prezentów")]
    // Zmiana sympatii dla przedmiotów, których NIE MA na liœcie (np. 0, bo NPC jest obojêtny na losowe œmieci)
    public int defaultGiftAffinity = 0;
    // Pe³na lista gustów (kochane i znienawidzone przedmioty)
    public GiftPreference[] giftPreferences;

    [Header("Reakcje na prezenty")]
    public DialogueNode reactionLove;    // Zachwyt (np. Truskawka: >= 10 pkt)
    public DialogueNode reactionNeutral; // Obojêtnoœæ (np. Ciastko: od -9 do 9 pkt)
    public DialogueNode reactionHate;    // Odraza (np. Pierœcionek: <= -10 pkt)

    void Start()
    {
        // Upewniamy siê, ¿e status na starcie zgadza siê z suwakiem
        UpdateRelationshipStatus();
    }

    // --- FUNKCJE DLA PRZYSZ£EGO SYSTEMU DIALOGÓW ---

    // Tê funkcjê bêdziemy wywo³ywaæ, gdy gracz powie coœ mi³ego lub chamskiego w oknie dialogowym
    public void ModifyAffinity(int amount)
    {
        affinity += amount;

        // Zabezpieczenie, ¿eby sympatia nie przekroczy³a skrajnych wartoœci
        affinity = Mathf.Clamp(affinity, -100, 100);

        UpdateRelationshipStatus();
    }

public int ReceiveGift(ItemData gift)
    {
        int finalAffinityChange = defaultGiftAffinity; 

        foreach (GiftPreference pref in giftPreferences)
        {
            if (pref.item == gift)
            {
                finalAffinityChange = pref.affinityModifier;
                break; 
            }
        }

        Debug.Log($"{npcName} otrzymuje {gift.itemName}. Zmiana sympatii: {finalAffinityChange}");
        ModifyAffinity(finalAffinityChange);
        
        return finalAffinityChange; // <--- Zwracamy wynik!
    }

    // Automatycznie przypisuje status (np. "Wrog" lub "Przyjaciel") na podstawie punktów
    private void UpdateRelationshipStatus()
    {
        if (affinity <= -50) currentStatus = RelationshipStatus.Wrog;
        else if (affinity < 10) currentStatus = RelationshipStatus.Nieznajomy;
        else if (affinity < 50) currentStatus = RelationshipStatus.Znajomy;
        else if (affinity < 90) currentStatus = RelationshipStatus.Przyjaciel;
        else currentStatus = RelationshipStatus.BratniaDusza;
    }
}