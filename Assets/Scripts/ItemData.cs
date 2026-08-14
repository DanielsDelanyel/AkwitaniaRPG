using System.Text;
using UnityEngine;

// ===================================================================
// STATYSTYKA Z WYBOREM: stala wartosc ALBO losowany zakres
// ===================================================================
[System.Serializable]
public class RandomizableStat
{
    [Tooltip("Odznaczone = wpisujesz konkretna wartosc. Zaznaczone = gra losuje z zakresu.")]
    public bool useRandomRange = false;

    [Tooltip("Uzywane, gdy losowanie jest WYLACZONE.")]
    public float fixedValue = 0f;

    [Tooltip("Dolna granica losowania.")]
    public float minValue = -10f;

    [Tooltip("Gorna granica losowania.")]
    public float maxValue = 40f;

    [Tooltip("Zaokraglac wynik do pelnych liczb?")]
    public bool roundToWhole = false;

    // Wynik rzutu. Ukryty - nie edytuj tego recznie.
    [SerializeField, HideInInspector] private bool hasRolled;
    [SerializeField, HideInInspector] private float rolledValue;

    public float Value
    {
        get
        {
            if (!useRandomRange) return fixedValue;
            return hasRolled ? rolledValue : 0f;
        }
    }

    public bool IsActive { get { return Mathf.Abs(Value) > 0.0001f; } }

    public void Roll()
    {
        if (!useRandomRange) return;

        float lo = Mathf.Min(minValue, maxValue);
        float hi = Mathf.Max(minValue, maxValue);

        rolledValue = Random.Range(lo, hi);
        if (roundToWhole) rolledValue = Mathf.Round(rolledValue);
        else rolledValue = Mathf.Round(rolledValue * 10f) / 10f; // jedno miejsce po przecinku

        hasRolled = true;
    }

    public void ClearRoll()
    {
        hasRolled = false;
        rolledValue = 0f;
    }
}

// ===================================================================
// DODATKOWY LOSOWY BONUS do dowolnej istniejacej statystyki.
// Dziala jak DOPISEK do wartosci bazowej wpisanej w polach ItemData.
// ===================================================================
public enum BonusStatType
{
    Damage,
    MagicDamage,
    Defense,
    MagicDefense,
    Vitality,
    Strength,
    Dexterity,
    Intelligence,
    Charisma,
    Heal
}

[System.Serializable]
public class RandomizableBonus
{
    public BonusStatType stat = BonusStatType.Strength;
    public int minValue = 1;
    public int maxValue = 5;

    [SerializeField, HideInInspector] private bool hasRolled;
    [SerializeField, HideInInspector] private int rolledValue;

    public int Value { get { return hasRolled ? rolledValue : 0; } }

    public void Roll()
    {
        int lo = Mathf.Min(minValue, maxValue);
        int hi = Mathf.Max(minValue, maxValue);
        rolledValue = Random.Range(lo, hi + 1);
        hasRolled = true;
    }

    public void ClearRoll()
    {
        hasRolled = false;
        rolledValue = 0;
    }
}

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

    // ===============================================================
    // KSZTALT CIOSU - kazda bron macha inaczej
    // ===============================================================
    [Header("Walka Wrecz (ksztalt ciosu)")]
    [Tooltip("Jak daleko od gracza jest srodek ostrza. Noz ~0.55, miecz ~0.85, kij ~1.4")]
    public float weaponReach = 0.85f;

    [Tooltip("Dlugosc hitboxa WZDLUZ ostrza (w jednostkach swiata). Noz ~0.35, miecz ~0.7, kij ~1.3")]
    public float weaponLength = 0.7f;

    [Tooltip("Grubosc hitboxa w poprzek. Zwykle 0.2 - 0.35.")]
    public float weaponWidth = 0.28f;

    [Tooltip("Szerokosc luku ciecia w stopniach. Noz ~50, miecz ~65, kij ~110 (zamach nad glowa).")]
    public float swingArc = 65f;

    [Tooltip("Skala grafiki broni podczas ciecia.")]
    public float weaponVisualScale = 0.8f;

    [Tooltip("Obrot grafiki, by ostrze celowalo w kierunku ciosu. " +
             "Ikony rysowane po skosie potrzebuja zwykle -45 lub -55.")]
    public float weaponSpriteAngle = -55f;

    // ===============================================================
    // DZWIEKI
    // ===============================================================
    [Header("Dzwieki")]
    [Tooltip("Swist przy machnieciu (lub napiecie cieciwy dla luku). " +
             "Wrzuc kilka wariantow - gra wylosuje, by nie bylo monotonnie.")]
    public AudioClip[] swingSounds;

    [Tooltip("Odglos trafienia w cel.")]
    public AudioClip[] hitSounds;

    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("Ekonomia")]
    public int price = 10;

    [Header("Konsumpcja")]
    public int healAmount = 0;

    [Header("Relacje (Prezenty)")]
    public int affinityBonus = 0;

    [Header("Bonusy do Statystyk (wartosci bazowe)")]
    public int damageBonus = 0;
    public int magicDamageBonus = 0;
    public int defenseBonus = 0;
    public int magicDefenseBonus = 0;
    public int vitalityBonus = 0;
    public int strengthBonus = 0;
    public int dexterityBonus = 0;
    public int intellegenceBonus = 0;
    public int charismaBonus = 0;

    // ===============================================================
    // NOWA STATYSTYKA: procentowy bonus do obrazen
    // ===============================================================
    [Header("Bonus do Obrazen (%)")]
    [Tooltip("Mnoznik zadawanych obrazen. 25 = +25%, -10 = -10%. " +
             "Zaznacz 'Use Random Range', by gra losowala go przy zdobyciu przedmiotu.")]
    public RandomizableStat damageBonusPercent = new RandomizableStat
    {
        useRandomRange = false,
        fixedValue = 0f,
        minValue = -10f,
        maxValue = 40f
    };

    [Header("Dodatkowe Losowe Bonusy")]
    [Tooltip("Kazdy wpis dolosowuje wartosc do wybranej statystyki, PONAD wartosc bazowa powyzej.")]
    public RandomizableBonus[] randomBonuses;

    // --- Znaczniki kopii dzialajacej w pamieci ---
    [System.NonSerialized] public bool isRuntimeInstance;
    [System.NonSerialized] public ItemData sourceTemplate;

    // ===============================================================
    // LOSOWANIE
    // ===============================================================

    // Czy w ogole warto robic kopie tego przedmiotu?
    public bool NeedsRandomization()
    {
        if (isStackable) return false; // stosy musza pozostac wymienne
        if (damageBonusPercent != null && damageBonusPercent.useRandomRange) return true;
        return randomBonuses != null && randomBonuses.Length > 0;
    }

    public void RollAllStats()
    {
        if (damageBonusPercent != null) damageBonusPercent.Roll();

        if (randomBonuses != null)
        {
            foreach (RandomizableBonus b in randomBonuses)
            {
                if (b != null) b.Roll();
            }
        }
    }

    public void ClearAllRolls()
    {
        if (damageBonusPercent != null) damageBonusPercent.ClearRoll();

        if (randomBonuses != null)
        {
            foreach (RandomizableBonus b in randomBonuses)
            {
                if (b != null) b.ClearRoll();
            }
        }
    }

    private int GetRolledExtra(BonusStatType type)
    {
        if (randomBonuses == null) return 0;

        int sum = 0;
        foreach (RandomizableBonus b in randomBonuses)
        {
            if (b != null && b.stat == type) sum += b.Value;
        }
        return sum;
    }

    // ===============================================================
    // GETTERY - UZYWAJ ICH ZAMIAST SUROWYCH POL!
    // Zwracaja wartosc bazowa + to, co wylosowal ten konkretny egzemplarz.
    // ===============================================================
    public int GetDamageBonus() { return damageBonus + GetRolledExtra(BonusStatType.Damage); }
    public int GetMagicDamageBonus() { return magicDamageBonus + GetRolledExtra(BonusStatType.MagicDamage); }
    public int GetDefenseBonus() { return defenseBonus + GetRolledExtra(BonusStatType.Defense); }
    public int GetMagicDefenseBonus() { return magicDefenseBonus + GetRolledExtra(BonusStatType.MagicDefense); }
    public int GetVitalityBonus() { return vitalityBonus + GetRolledExtra(BonusStatType.Vitality); }
    public int GetStrengthBonus() { return strengthBonus + GetRolledExtra(BonusStatType.Strength); }
    public int GetDexterityBonus() { return dexterityBonus + GetRolledExtra(BonusStatType.Dexterity); }
    public int GetIntelligenceBonus() { return intellegenceBonus + GetRolledExtra(BonusStatType.Intelligence); }
    public int GetCharismaBonus() { return charismaBonus + GetRolledExtra(BonusStatType.Charisma); }
    public int GetHealAmount() { return healAmount + GetRolledExtra(BonusStatType.Heal); }

    public float GetDamagePercent()
    {
        return damageBonusPercent != null ? damageBonusPercent.Value : 0f;
    }

    // ===============================================================
    // OPIS DLA TOOLTIPA
    // ===============================================================
    private const string COLOR_GOOD = "#7CFF8A";
    private const string COLOR_BAD = "#FF7B7B";
    private const string COLOR_USE = "#8ED8FF";

    public bool HasAnyStats()
    {
        return GetHealAmount() != 0
            || GetDamageBonus() != 0 || GetMagicDamageBonus() != 0
            || GetDefenseBonus() != 0 || GetMagicDefenseBonus() != 0
            || GetVitalityBonus() != 0 || GetStrengthBonus() != 0
            || GetDexterityBonus() != 0 || GetIntelligenceBonus() != 0
            || GetCharismaBonus() != 0
            || Mathf.Abs(GetDamagePercent()) > 0.0001f;
    }

    public string GetStatsDescription()
    {
        StringBuilder sb = new StringBuilder();

        int heal = GetHealAmount();
        if (heal != 0)
            sb.Append($"<color={COLOR_USE}>Przywraca {Mathf.Abs(heal)} pkt zdrowia</color>\n");

        // Procentowy bonus - wyroznia sie, bo to mnoznik, a nie dodawanie
        float pct = GetDamagePercent();
        if (Mathf.Abs(pct) > 0.0001f)
        {
            string color = pct > 0f ? COLOR_GOOD : COLOR_BAD;
            string sign = pct > 0f ? "+" : "";
            sb.Append($"<color={color}>{sign}{pct:0.#}% do zadawanych obrazen</color>\n");
        }

        AppendLine(sb, "Obrazenia", GetDamageBonus());
        AppendLine(sb, "Obrazenia magiczne", GetMagicDamageBonus());
        AppendLine(sb, "Obrona", GetDefenseBonus());
        AppendLine(sb, "Obrona magiczna", GetMagicDefenseBonus());
        AppendLine(sb, "Sila", GetStrengthBonus());
        AppendLine(sb, "Witalnosc", GetVitalityBonus());
        AppendLine(sb, "Zrecznosc", GetDexterityBonus());
        AppendLine(sb, "Inteligencja", GetIntelligenceBonus());
        AppendLine(sb, "Charyzma", GetCharismaBonus());

        return sb.ToString().TrimEnd('\n');
    }

    private void AppendLine(StringBuilder sb, string label, int value)
    {
        if (value == 0) return;

        string color = value > 0 ? COLOR_GOOD : COLOR_BAD;
        string sign = value > 0 ? "+" : "";
        sb.Append($"<color={color}>{sign}{value} {label}</color>\n");
    }

    public string GetTypeName()
    {
        switch (itemType)
        {
            case ItemType.General: return "Przedmiot";
            case ItemType.Consumable: return "Konsumpcyjny";
            case ItemType.Ring: return "Pierscien";
            case ItemType.Necklace: return "Naszyjnik";
            case ItemType.Weapon1h: return "Bron jednoreczna";
            case ItemType.Weapon2h: return "Bron dwureczna";
            case ItemType.Bow: return "Luk";
            case ItemType.Second_Hand: return "Druga reka";
            case ItemType.Helmet: return "Helm";
            case ItemType.Armor: return "Zbroja";
            case ItemType.Legs: return "Spodnie";
            case ItemType.Boots: return "Buty";
            case ItemType.Ammo: return "Amunicja";
            case ItemType.Gift: return "Prezent";
            default: return "";
        }
    }

    // Czy to ten sam RODZAJ przedmiotu? Kopia i oryginal to wciaz "ten sam miecz".
    // Przydaje sie np. przy sprawdzaniu klucza do drzwi albo gustow NPC.
    public bool IsSameKindAs(ItemData other)
    {
        if (other == null) return false;
        ItemData a = sourceTemplate != null ? sourceTemplate : this;
        ItemData b = other.sourceTemplate != null ? other.sourceTemplate : other;
        return a == b;
    }
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

public enum ItemRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}