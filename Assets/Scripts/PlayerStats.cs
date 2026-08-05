using UnityEngine;

public enum CharacterClass
{
    Traveler,
    Assassin,
    Mage,
    Barbarian,
    Juggernaut,
    Bard,
    Paladin,
    Nekromancer,
    Ilusionist,
    Monk,
    Hunter
}

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats instance;

    [Header("Dane Gracza")]
    public string playerName = "Bezi";

    [Header("Klasa i Bonusy Pasywne")]
    public CharacterClass currentProfession = CharacterClass.Traveler;
    public float moveSpeedMultiplier = 1f;      // W³óczêga: 1.1f (+10%)
    public float discount = 0f;                 // bard: 0.2f (+20%) iluzjonista: 0.1f (+10%)
    public float persuasionChance = 0.1f;       // Bard: 0.75f (+65%) Iluzjinista: 0.3f (+30%)
    public float additionalMana = 1f;       // Mag: +10%, Paladyn: +5%,
    public float additionalHealth = 1f;       // Paladyn: +5% Mag_bitewny: +15%


    public float damageMultiplier = 1f;         // Mag: 1.2f (+20%)
    public float defenseMultiplier = 1f;
    public float critChance = 10f;              // Bazowo 10%
    public float critDamageMultiplier = 2f;     // Skrytobójca x3



    [Header("Rozwój Postaci")]
    public int level = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100;
    public float levelScaling = 1.1f;

    public int attributePoints = 0;


    [Header("Ekonomia")]
    public int currentMoney = 23; // Z³oto na start

    [Header("Statystyki Bazowe")]
    public int baseSTR = 5; //Si³a
    public int baseWIT = 5; //Witalnoœæ
    public int baseINT = 5; //Inteligencja
    public int baseZR = 5; //Zrêcznoœæ
    public int baseCHAR = 5; //Charyzma

    public int baseDmg = 5;
    public int baseMagicDmg = 5;


    public int baseDef = 5;
    public int baseMagicDef = 5;

    [Header("Bonusy z Ekwipunku")]
    public int equipSTR = 0;
    public int equipWIT = 0;
    public int equipINT = 0;
    public int equipZR = 0;
    public int equipCHAR = 0;


    public int equipDmg = 0;
    public int equipMagicDmg = 0;

    public int equipDef = 0;
    public int equipMagicDef = 0;


    [Header("Statystyki Koñcowe")]
    public int totalDamage;
    public int totalMagicDamage;

    public int maxHealth;
    public int currentHealth;

    public float maxStamina;
    public float currentStamina;

    public float maxMana;
    public float currentMana;

    public int defense;
    public int magicDefense;

    public float staminaRegen = 3f;

    [Header("Walka i Popupy")]
    public GameObject damagePopupPrefab;
    public float invincibilityTime = 1f;
    private float invincibilityTimer;

    public delegate void OnStatsChanged();
    public OnStatsChanged onStatsChangedCallback;

    public delegate void OnHealthChanged();
    public OnHealthChanged onHealthChangedCallback;

    void Awake()
    {
        instance = this;
    }

    public int GetMaxHealth()
    {
        return Mathf.RoundToInt(maxHealth * additionalHealth);
    }

    public float GetMaxMana()
    {
        return maxMana * additionalMana;
    }

    void Start()
    {
        // NAJPIERW okreœlamy profesjê, ¿eby zdobyæ poprawne mno¿niki przed przypisaniem ¿ycia!
        DetermineProfession();

        currentHealth = GetMaxHealth();
        currentMana = GetMaxMana();
        currentStamina = maxStamina;
    }

    void Update()
    {
        if (invincibilityTimer > 0) invincibilityTimer -= Time.deltaTime;

        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegen * Time.deltaTime;
            if (currentStamina > maxStamina) currentStamina = maxStamina;
        }
    }

    public int GetTotal(int baseStat, int equipStat)
    {
        return baseStat + equipStat;
    }

    public void TakeDamage(int damage, bool isCrit, Vector2 hitDirection)
    {
        if (invincibilityTimer > 0) return;

        int totalDefense = Mathf.RoundToInt(GetTotal(baseDef, equipDef) * defenseMultiplier);
        int finalDamage = damage - totalDefense;
        if (finalDamage < 1) finalDamage = 1;

        currentHealth -= finalDamage;
        invincibilityTimer = invincibilityTime;

        // --- TWORZENIE NAPISU ---
        if (damagePopupPrefab != null)
        {
            GameObject popup = Instantiate(damagePopupPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            popup.GetComponent<DamagePopup>().Setup(finalDamage, isCrit, hitDirection);
        }

        if (currentHealth <= 0) Debug.Log("Gracz umiera!");
    }
    public void RecalculateStats()
    {
        DetermineProfession(); // Odœwie¿a klasê po ka¿dym dodanym punkcie!
        // Ta funkcja wywo³ywana jest po zmianie sprzêtu i ka¿e odœwie¿yæ panel tekstu

        // Zabezpieczenie przed b³êdem z hp 
        // Pilnujemy, by po zmianie klasy i stracie bonusów (np. zmiana z Paladyna na Mnicha), 
        // obecne HP/Mana nie przekroczy³y nowego, ni¿szego limitu!
        if (currentHealth > GetMaxHealth()) currentHealth = GetMaxHealth();
        if (currentMana > GetMaxMana()) currentMana = GetMaxMana();

        if (InventoryUI.instance != null)
        {
            InventoryUI.instance.UpdatePlayerInfoUI();
        }
    }
    public void DetermineProfession()
    {
        int totalStats = baseSTR + baseWIT + baseINT + baseZR + baseCHAR;
        if (totalStats == 0) return;

        // Liczymy udzia³ procentowy najwa¿niejszych statystyk (od 0.0 do 1.0)
        float strPct = (float)baseSTR / totalStats;
        float vitPct = (float)baseWIT / totalStats;
        float charPct = (float)baseCHAR / totalStats;
        float dexPct = (float)baseZR / totalStats;
        float intPct = (float)baseINT / totalStats;

        // Zawsze resetujemy bonusy do domyœlnych (na wypadek utraty klasy)
        moveSpeedMultiplier = 1f;
        damageMultiplier = 1f;
        defenseMultiplier = 1f;
        critChance = 10f;
        critDamageMultiplier = 2f;


        // £owca: Specjalista (Ponad 50% Zrêcznoœci)
        if (dexPct > 0.50f)
        {
            currentProfession = CharacterClass.Hunter;
            critChance += 40f; // Daje 50% szansy
            damageMultiplier = 1.10f; // +10% do ka¿dych obra¿eñ

        }
        // MAG: Specjalista (Ponad 40% Inteligencji)
        if (intPct > 0.40f)
        {
            currentProfession = CharacterClass.Mage;
            critChance += 7.5f; // Daje 17.5% szansy
            damageMultiplier = 1.20f; // +20% do ka¿dych obra¿eñ

        }
        // BARBARZYÑCA: Specjalista (Ponad 45% Si³y)
        else if (strPct > 0.45f) 
        {
            currentProfession = CharacterClass.Barbarian;
            damageMultiplier = 1.50f; // +50% do ka¿dych obra¿eñ
        }
        // Obroñca: Specjalista (Ponad 45% Witalnoœci)
        else if (vitPct > 0.45f)
        {
            currentProfession = CharacterClass.Juggernaut;
            defenseMultiplier = 1.50f; // +50% do pancerza
        }
        // Bard: Specjalista (Ponad 40% Charyzmy)
        else if (charPct > 0.40f)
        {
            currentProfession = CharacterClass.Bard;
            discount = 0.3f;
            persuasionChance = 0.75f;
        }

        // SKRYTOBÓJCA: Hybryda (Si³a + Zrêcznoœæ ponad 65%, przy czym s¹ w miarê równe)
        else if ((strPct + dexPct) > 0.65f && Mathf.Abs(strPct - dexPct) <= 0.15f)
        {
            currentProfession = CharacterClass.Assassin;
            critChance += 20f; // Daje 30% szansy
            critDamageMultiplier = 3f; // Zmienia obra¿enia krytyczne na x3
        }
        // PALADYN: Hybryda (Si³a + Inteligencja ponad 65%, przy czym s¹ w miarê równe)
        else if ((strPct + intPct) > 0.65f && Mathf.Abs(strPct - intPct) <= 0.15f)
        {
            currentProfession = CharacterClass.Paladin;
            damageMultiplier = 1.20f;
            additionalMana = 0.05f;
            additionalHealth = 0.05f;
        }
        // NEKROMANTA: Hybryda (Inteligencja + Witalnoœæ ponad 65%, przy czym s¹ w miarê równe)
        else if ((strPct + intPct) > 0.65f && Mathf.Abs(strPct - intPct) <= 0.15f)
        {
            currentProfession = CharacterClass.Nekromancer;
            damageMultiplier = 0.8f;
            additionalMana = 1.1f;
            additionalHealth = 1.1f;
        } 
        // ILUZJONISTA: Hybryda (Inteligencja + Charyzma ponad 65%, przy czym s¹ w miarê równe)
        else if ((charPct + intPct) > 0.65f && Mathf.Abs(charPct - intPct) <= 0.15f)
        {
            currentProfession = CharacterClass.Ilusionist;
            damageMultiplier = 0.8f;
            additionalMana = 1.2f;
            additionalHealth = 0.8f;
            persuasionChance = 0.3f;
            discount = 0.15f;
        }
        // MNICH: Hybryda (Inteligencja + Zrêcznoœæ ponad 65%, przy czym s¹ w miarê równe)
        else if ((dexPct + intPct) > 0.65f && Mathf.Abs(dexPct - intPct) <= 0.15f)
        {
            currentProfession = CharacterClass.Monk;
            damageMultiplier = 0.9f;
            additionalMana = 1.05f;
            additionalHealth = 0.95f;
            critChance += 7.5f;
            moveSpeedMultiplier = 1.2f;
        }
        // W£ÓCZÊGA: Baza (¯aden kierunek nie dominuje)
        else
        {
            currentProfession = CharacterClass.Traveler;
            moveSpeedMultiplier = 1.1f; // +10% szybkoœci biegania
        }
    }

    public void AddExp(int amount)
    {
        currentExp += amount;
        bool leveledUp = false;

        // U¿ywamy pêtli while, na wypadek gdyby gracz zdoby³ wystarczaj¹co expa na kilka poziomów naraz
        while (currentExp >= expToNextLevel)
        {
            // 1. Zdejmujemy z paska tylko tyle expa, ile kosztowa³ poziom (reszta przechodzi na kolejny!)
            currentExp -= expToNextLevel;

            // 2. Dodajemy poziom i nagrodê
            level++;
            attributePoints += 2;

            // 3. Zwiêkszamy koszt KOLEJNEGO poziomu o 10% i zaokr¹glamy do pe³nych liczb
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * levelScaling);

            leveledUp = true;
        }

        if (leveledUp)
        {
            Debug.Log($"AWANS! Osi¹gniêto {level} poziom. Punkty do wydania: {attributePoints}");
            RecalculateStats();

            // Jeœli okno statystyk jest obecnie otwarte, odœwie¿amy je, by pokazaæ nowe plusiki!
            StatsUI statsUI = Object.FindFirstObjectByType<StatsUI>();
            if (statsUI != null && statsUI.gameObject.activeInHierarchy)
            {
                statsUI.UpdateUI();
            }
        }
    }

    // --- NOWOŒÆ: Uniwersalny system pobierania opisów klas ---
    public string GetProfessionDescription()
    {
        switch (currentProfession)
        {
            case CharacterClass.Traveler:
                return "<color=#FFD700>W£ÓCZÊGA</color>\n\n+10% do prêdkoœci poruszania siê.";
            case CharacterClass.Hunter:
                return "<color=#FFD700>£OWCA</color>\n\n+40% szansy na cios krytyczny.\n+10% do zadawanych obra¿eñ.";
            case CharacterClass.Mage:
                return "<color=#FFD700>MAG</color>\n\n+7.5% szansy na cios krytyczny.\n+20% do zadawanych obra¿eñ.";
            case CharacterClass.Barbarian:
                return "<color=#FFD700>BARBARZYÑCA</color>\n\n+50% do zadawanych obra¿eñ.";
            case CharacterClass.Juggernaut:
                return "<color=#FFD700>OBROÑCA</color>\n\n+50% do ca³kowitego pancerza.";
            case CharacterClass.Bard:
                return "<color=#FFD700>BARD</color>\n\n+75% szansy na perswazjê w dialogach.\n-30% cen we wszystkich sklepach.";
            case CharacterClass.Assassin:
                return "<color=#FFD700>SKRYTOBÓJCA</color>\n\n+20% szansy na cios krytyczny.\nObra¿enia krytyczne mno¿one x3.";
            case CharacterClass.Paladin:
                return "<color=#FFD700>PALADYN</color>\n\n+20% do zadawanych obra¿eñ.\n+5% Maksymalnego Zdrowia i Many.";
            case CharacterClass.Nekromancer:
                return "<color=#FFD700>NEKROMANTA</color>\n\n+10% Maksymalnego Zdrowia i Many.\n-20% do zadawanych obra¿eñ.";
            case CharacterClass.Ilusionist:
                return "<color=#FFD700>ILUZJONISTA</color>\n\n+20% Maksymalnej Many.\n-20% Maksymalnego Zdrowia.\n-20% do zadawanych obra¿eñ.\n+30% szansy na perswazjê.\n-15% cen w sklepach.";
            case CharacterClass.Monk:
                return "<color=#FFD700>MNICH</color>\n\n+20% do prêdkoœci poruszania siê.\n+7.5% szansy na cios krytyczny.\n+5% Maksymalnej Many.\n-5% Maksymalnego Zdrowia.\n-10% do zadawanych obra¿eñ.";
            default:
                return "Brak dodatkowych bonusów.";
        }
    }
}
