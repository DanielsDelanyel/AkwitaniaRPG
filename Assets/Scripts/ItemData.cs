using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Ekwipunek/Przedmiot")]
public class ItemData : ScriptableObject
{
    public string itemName = "Nowy Przedmiot";
    public Sprite icon;
    [TextArea] public string description = "Opis przedmiotu...";

    public ItemType itemType;

    public ItemRarity rarity;

    [Header("Logika Gry")]
    public GameObject itemPrefab;
    public bool isStackable = false;
    public float weight = 10f;

    [Header("Ekonomia")]
    public int price = 10; // Cena kupna (sprzeda¿ to np. po³owa tej kwoty)

    [Header("Konsumpcja")]
    public int healAmount = 0;

    [Header("Relacje (Prezenty)")]
    // NOWOŒÆ: Ile punktów sympatii zyska (lub straci) NPC po otrzymaniu tego przedmiotu
    public int affinityBonus = 0;

    [Header("Bonusy do Statystyk")]
    public int damageBonus = 0;
    public int magicDamageBonus = 0;

    public int defenseBonus = 0;        
    public int magicDefenseBonus = 0;

    public int vitalityBonus = 0;

    public int strengthBonus = 0;
    public int dexterityBonus = 0;
    public int intellegenceBonus = 0;
    public int charismaBonus = 0;
}

public enum ItemType
{
    General,
    Consumable,
    Ring,
    Necklace,
    Weapon1h,
    Weapon2h,
    Bow,
    Second_Hand,
    Helmet,
    Armor,
    Legs,
    Boots,
    Ammo,
    Gift
}

// --- NOWY ENUM ---
public enum ItemRarity
{
    Common,     // Pospolity
    Rare,       // Rzadki
    Epic,       // Epicki
    Legendary   // Legendarny
}