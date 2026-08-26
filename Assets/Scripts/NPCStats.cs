using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GiftPreference
{
    public ItemData item;
    public int affinityModifier; // Ile punkt�w dodaje/odejmuje ten konkretny prezent
}

// Definiujemy mo�liwe statusy relacji
public enum RelationshipStatus
{
    Wrog,           // Poni�ej -50
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

    // U�ywamy stringa, a nie Enuma, �eby� m�g� wpisa� tu DOWOLN� profesj� (nawet tak�, kt�ra mechanicznie nie istnieje, np. "Zielarka", "�ebrak")
    public string profession = "Brak";

    [Header("Poziom i Ekonomia")]
    public int level = 1;
    public int money = 50;

    [Header("Sklep")]
    public ItemData[] shopItems; // Co ten kupiec ma na sprzeda�?

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

    [Header("Preferencje Prezent�w")]
    // Zmiana sympatii dla przedmiot�w, kt�rych NIE MA na li�cie (np. 0, bo NPC jest oboj�tny na losowe �mieci)
    public int defaultGiftAffinity = 0;
    // Pe�na lista gust�w (kochane i znienawidzone przedmioty)
    public GiftPreference[] giftPreferences;

    [Header("Reakcje na prezenty")]
    public DialogueNode reactionLove;    // Zachwyt (np. Truskawka: >= 10 pkt)
    public DialogueNode reactionNeutral; // Oboj�tno�� (np. Ciastko: od -9 do 9 pkt)
    public DialogueNode reactionHate;    // Odraza (np. Pier�cionek: <= -10 pkt)

    // ===============================================================
    // ZAPAS SKLEPU
    // Egzemplarze losujemy RAZ i zapamietujemy, zeby gracz nie mogl
    // zamykac i otwierac sklepu w kolko az do wymarzonego rzutu.
    // ===============================================================
    [System.NonSerialized] private List<ItemData> rolledStock;

    public List<ItemData> GetShopStock()
    {
        if (rolledStock != null) return rolledStock;

        rolledStock = new List<ItemData>();
        if (shopItems == null) return rolledStock;

        foreach (ItemData template in shopItems)
        {
            if (template == null) continue;

            // ItemFactory odda oryginal, jesli nie ma czego losowac
            rolledStock.Add(ItemFactory.Create(template));
        }

        return rolledStock;
    }

    // Wolaj po uplywie dnia w grze, by kupiec odswiezyl towar
    public void RefreshShopStock()
    {
        rolledStock = null;
    }

    // Kupiec traci sprzedany egzemplarz z polki
    public void RemoveFromStock(ItemData soldItem)
    {
        if (rolledStock != null && soldItem != null) rolledStock.Remove(soldItem);
    }

    // ===============================================================
    // ZAPIS STANU
    // ===============================================================
    private UniqueId uniqueId;
    private string SaveId
    {
        get { return uniqueId != null ? uniqueId.Id : null; }
    }

    // Wywoluje SaveManager tuz przed zapisem gry
    public void StoreStateToWorld()
    {
        if (uniqueId == null) uniqueId = GetComponent<UniqueId>();
        if (uniqueId == null) return;

        SavedNpc data = WorldState.GetOrCreateNpc(SaveId);
        data.affinity = affinity;

        // Zapas towaru zapisujemy TYLKO, jesli kupiec go juz wylosowal.
        // Inaczej nie odroznilibysmy pustej polki od nieodwiedzonego sklepu.
        data.hasStock = rolledStock != null;
        data.shopStock.Clear();

        if (rolledStock == null) return;

        foreach (ItemData item in rolledStock)
        {
            SavedItem entry = SaveManager.CaptureItem(item, 1);
            if (entry != null) data.shopStock.Add(entry);
        }
    }

    // Odtwarza sympatie i zapas z wczytanego zapisu
    private void RestoreStateFromWorld()
    {
        uniqueId = GetComponent<UniqueId>();

        if (uniqueId == null)
        {
            Debug.LogWarning($"NPC '{npcName}' nie ma komponentu UniqueId - " +
                             "jego sympatia i zapas towaru nie beda zapisywane.");
            return;
        }

        SavedNpc data = WorldState.GetNpc(SaveId);
        if (data == null) return;   // pierwsze spotkanie

        affinity = Mathf.Clamp(data.affinity, -100, 100);

        if (!data.hasStock) return;

        // Odtwarzamy dokladnie te egzemplarze, ktore zostaly na polce
        rolledStock = new List<ItemData>();

        foreach (SavedItem entry in data.shopStock)
        {
            ItemData item = SaveManager.RestoreItem(entry);
            if (item != null) rolledStock.Add(item);
        }
    }

    void Start()
    {
        RestoreStateFromWorld();

        // Upewniamy si�, �e status na starcie zgadza si� z suwakiem
        UpdateRelationshipStatus();
    }

    // --- FUNKCJE DLA PRZYSZ�EGO SYSTEMU DIALOG�W ---

    // T� funkcj� b�dziemy wywo�ywa�, gdy gracz powie co� mi�ego lub chamskiego w oknie dialogowym
    public void ModifyAffinity(int amount)
    {
        affinity += amount;

        // Zabezpieczenie, �eby sympatia nie przekroczy�a skrajnych warto�ci
        affinity = Mathf.Clamp(affinity, -100, 100);

        UpdateRelationshipStatus();
    }

public int ReceiveGift(ItemData gift)
    {
        if (gift == null) return 0;

        int finalAffinityChange = defaultGiftAffinity;
        if (giftPreferences == null) giftPreferences = new GiftPreference[0];

        foreach (GiftPreference pref in giftPreferences)
        {
            // POPRAWKA: 'pref.item == gift' nie dzialalo dla przedmiotow
            // z losowanymi statystykami - gracz wreczal KOPIE, a lista
            // gustow trzyma ORYGINALY. IsSameKindAs porownuje rodzaj.
            if (pref.item != null && pref.item.IsSameKindAs(gift))
            {
                finalAffinityChange = pref.affinityModifier;
                break; 
            }
        }

        Debug.Log($"{npcName} otrzymuje {gift.itemName}. Zmiana sympatii: {finalAffinityChange}");
        ModifyAffinity(finalAffinityChange);
        
        return finalAffinityChange; // <--- Zwracamy wynik!
    }

    // Automatycznie przypisuje status (np. "Wrog" lub "Przyjaciel") na podstawie punkt�w
    private void UpdateRelationshipStatus()
    {
        if (affinity <= -50) currentStatus = RelationshipStatus.Wrog;
        else if (affinity < 10) currentStatus = RelationshipStatus.Nieznajomy;
        else if (affinity < 50) currentStatus = RelationshipStatus.Znajomy;
        else if (affinity < 90) currentStatus = RelationshipStatus.Przyjaciel;
        else currentStatus = RelationshipStatus.BratniaDusza;
    }
}