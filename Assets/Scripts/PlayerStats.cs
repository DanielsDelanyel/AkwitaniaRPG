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

    // ---------------------------------------------------------------
    // WARTOSCI DOMYSLNE - do nich wracamy PRZED przydzieleniem klasy.
    // Dzieki temu bonusy nigdy sie nie "zapetlaja" po zmianie profesji.
    // ---------------------------------------------------------------
    [Header("Wartosci Domyslne (bez klasy)")]
    public float defaultCritChance = 10f;
    public float defaultCritDamage = 2f;
    public float defaultPersuasion = 0.1f;

    [Header("Klasa i Bonusy Pasywne (LICZONE AUTOMATYCZNIE - nie ruszaj w Inspektorze)")]
    public CharacterClass currentProfession = CharacterClass.Traveler;
    public float moveSpeedMultiplier = 1f;
    public float discount = 0f;
    public float persuasionChance = 0.1f;
    public float additionalMana = 1f;      // 1.0 = 100%, 1.05 = +5%
    public float additionalHealth = 1f;    // 1.0 = 100%, 0.8  = -20%
    public float damageMultiplier = 1f;
    public float defenseMultiplier = 1f;
    public float critChance = 10f;
    public float critDamageMultiplier = 2f;

    [Header("Rozwoj Postaci")]
    public int level = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100;
    public float levelScaling = 1.1f;
    public int attributePoints = 0;

    [Header("Ekonomia")]
    public int currentMoney = 23;

    [Header("Statystyki Bazowe")]
    public int baseSTR = 5;  // Sila
    public int baseWIT = 5;  // Witalnosc
    public int baseINT = 5;  // Inteligencja
    public int baseZR = 5;   // Zrecznosc
    public int baseCHAR = 5; // Charyzma

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

    // ---------------------------------------------------------------
    // WZORY NA ZASOBY - tu ustawiasz balans gry.
    // Zdrowie rosnie z Witalnosci, Mana z Inteligencji, Stamina ze Zrecznosci.
    // ---------------------------------------------------------------
    [Header("Wzory na Zasoby")]
    public int healthBase = 100;
    public int healthPerVitality = 10;
    public int healthPerLevel = 5;

    public float manaBase = 50f;
    public float manaPerIntelligence = 10f;

    public float staminaBase = 100f;
    public float staminaPerDexterity = 5f;
    public float staminaRegen = 15f;

    [Header("Statystyki Koncowe (podglad - liczone automatycznie)")]
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

    void Start()
    {
        RecalculateStats();          // wyznacza klase i maksymalne zasoby
        currentHealth = GetMaxHealth();
        currentMana = GetMaxMana();
        currentStamina = GetMaxStamina();
    }

    void Update()
    {
        // TU BYL BLAD: puste Update() = timer nietykalnosci nigdy nie schodzil,
        // wiec po pierwszym trafieniu gracz byl niesmiertelny do konca gry.
        if (invincibilityTimer > 0f) invincibilityTimer -= Time.deltaTime;

        float maxStam = GetMaxStamina();
        if (currentStamina < maxStam)
        {
            currentStamina += staminaRegen * Time.deltaTime;
            if (currentStamina > maxStam) currentStamina = maxStam;
        }
    }

    // --- ZASOBY ---
    public int GetMaxHealth()
    {
        int vit = GetTotal(baseWIT, equipWIT);
        float raw = (healthBase + vit * healthPerVitality + (level - 1) * healthPerLevel) * additionalHealth;
        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    public float GetMaxMana()
    {
        int inteligence = GetTotal(baseINT, equipINT);
        float raw = (manaBase + inteligence * manaPerIntelligence) * additionalMana;
        return Mathf.Max(1f, raw);
    }

    public float GetMaxStamina()
    {
        int dex = GetTotal(baseZR, equipZR);
        return Mathf.Max(1f, staminaBase + dex * staminaPerDexterity);
    }

    public int GetTotal(int baseStat, int equipStat)
    {
        return baseStat + equipStat;
    }

    // Zuzycie staminy (np. przez dash). Zwraca false, jesli zabraklo si.
    public bool UseStamina(float amount)
    {
        if (amount <= 0f) return true;
        if (currentStamina < amount) return false;
        currentStamina -= amount;
        return true;
    }

    // Pozwala innym skryptom (np. dashowi) nadac chwilowa nietykalnosc
    public void GrantInvincibility(float duration)
    {
        if (duration > invincibilityTimer) invincibilityTimer = duration;
    }

    public bool IsInvincible()
    {
        return invincibilityTimer > 0f;
    }

    // --- WALKA ---
    public void TakeDamage(int damage, bool isCrit, Vector2 hitDirection)
    {
        if (invincibilityTimer > 0f) return;

        int totalDefense = Mathf.RoundToInt(GetTotal(baseDef, equipDef) * defenseMultiplier);
        int finalDamage = damage - totalDefense;
        if (finalDamage < 1) finalDamage = 1;

        currentHealth -= finalDamage;
        invincibilityTimer = invincibilityTime;

        if (damagePopupPrefab != null)
        {
            GameObject popup = Instantiate(damagePopupPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            popup.GetComponent<DamagePopup>().Setup(finalDamage, isCrit, hitDirection);
        }

        if (onHealthChangedCallback != null) onHealthChangedCallback.Invoke();
        if (currentHealth <= 0) Debug.Log("Gracz umiera!");
    }

    // --- PRZELICZANIE ---
    public void RecalculateStats()
    {
        DetermineProfession();

        // Odswiezamy maksymalne zasoby JUZ Z uwzglednieniem mnoznikow klasy
        maxHealth = GetMaxHealth();
        maxMana = GetMaxMana();
        maxStamina = GetMaxStamina();

        totalDamage = Mathf.RoundToInt(GetTotal(baseDmg, equipDmg) * damageMultiplier);
        totalMagicDamage = Mathf.RoundToInt(GetTotal(baseMagicDmg, equipMagicDmg) * damageMultiplier);
        defense = Mathf.RoundToInt(GetTotal(baseDef, equipDef) * defenseMultiplier);
        magicDefense = GetTotal(baseMagicDef, equipMagicDef);

        // Po zmianie klasy na slabsza (np. Paladyn -> Iluzjonista) obcinamy nadmiar
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        if (currentMana > maxMana) currentMana = maxMana;
        if (currentStamina > maxStamina) currentStamina = maxStamina;

        if (InventoryUI.instance != null) InventoryUI.instance.UpdatePlayerInfoUI();
        if (onStatsChangedCallback != null) onStatsChangedCallback.Invoke();
    }

    // TU BYL GLOWNY BLAD: te wartosci nie wracaly do domyslnych,
    // wiec bonusy poprzedniej klasy zostawaly na zawsze.
    private void ResetClassBonuses()
    {
        moveSpeedMultiplier = 1f;
        damageMultiplier = 1f;
        defenseMultiplier = 1f;
        additionalHealth = 1f;
        additionalMana = 1f;
        discount = 0f;
        persuasionChance = defaultPersuasion;
        critChance = defaultCritChance;
        critDamageMultiplier = defaultCritDamage;
    }

    public void DetermineProfession()
    {
        ResetClassBonuses();

        int totalStats = baseSTR + baseWIT + baseINT + baseZR + baseCHAR;
        if (totalStats <= 0)
        {
            currentProfession = CharacterClass.Traveler;
            moveSpeedMultiplier = 1.1f;
            return;
        }

        float strPct = (float)baseSTR / totalStats;
        float vitPct = (float)baseWIT / totalStats;
        float intPct = (float)baseINT / totalStats;
        float dexPct = (float)baseZR / totalStats;
        float charPct = (float)baseCHAR / totalStats;

        // ================= SPECJALISCI =================

        // LOWCA: ponad 50% Zrecznosci
        if (dexPct > 0.50f)
        {
            currentProfession = CharacterClass.Hunter;
            critChance = defaultCritChance + 40f;   // 50%
            damageMultiplier = 1.10f;
        }
        // MAG: ponad 40% Inteligencji
        else if (intPct > 0.40f)
        {
            currentProfession = CharacterClass.Mage;
            critChance = defaultCritChance + 7.5f;  // 17.5%
            damageMultiplier = 1.20f;
        }
        // BARBARZYNCA: ponad 45% Sily
        else if (strPct > 0.45f)
        {
            currentProfession = CharacterClass.Barbarian;
            damageMultiplier = 1.50f;
        }
        // OBRONCA: ponad 45% Witalnosci
        else if (vitPct > 0.45f)
        {
            currentProfession = CharacterClass.Juggernaut;
            defenseMultiplier = 1.50f;
        }
        // BARD: ponad 40% Charyzmy
        else if (charPct > 0.40f)
        {
            currentProfession = CharacterClass.Bard;
            persuasionChance = 0.75f;
            discount = 0.30f;
        }

        // ================= HYBRYDY =================

        // SKRYTOBOJCA: Sila + Zrecznosc
        else if ((strPct + dexPct) > 0.65f && Mathf.Abs(strPct - dexPct) <= 0.15f)
        {
            currentProfession = CharacterClass.Assassin;
            critChance = defaultCritChance + 20f;   // 30%
            critDamageMultiplier = 3f;
        }
        // PALADYN: Sila + Inteligencja
        else if ((strPct + intPct) > 0.65f && Mathf.Abs(strPct - intPct) <= 0.15f)
        {
            currentProfession = CharacterClass.Paladin;
            damageMultiplier = 1.20f;
            additionalHealth = 1.05f;   // TU BYLO 0.05f -> stad "5 / 5" HP!
            additionalMana = 1.05f;     // TU BYLO 0.05f
        }
        // NEKROMANTA: Inteligencja + Witalnosc
        // (wczesniej mial SKOPIOWANY warunek Paladyna, wiec nie dalo sie go zdobyc)
        else if ((intPct + vitPct) > 0.65f && Mathf.Abs(intPct - vitPct) <= 0.15f)
        {
            currentProfession = CharacterClass.Nekromancer;
            additionalHealth = 1.10f;
            additionalMana = 1.10f;
            damageMultiplier = 0.80f;
        }
        // ILUZJONISTA: Inteligencja + Charyzma
        else if ((charPct + intPct) > 0.65f && Mathf.Abs(charPct - intPct) <= 0.15f)
        {
            currentProfession = CharacterClass.Ilusionist;
            additionalMana = 1.20f;
            additionalHealth = 0.80f;
            damageMultiplier = 0.80f;
            persuasionChance = 0.30f;
            discount = 0.15f;
        }
        // MNICH: Inteligencja + Zrecznosc
        else if ((dexPct + intPct) > 0.65f && Mathf.Abs(dexPct - intPct) <= 0.15f)
        {
            currentProfession = CharacterClass.Monk;
            moveSpeedMultiplier = 1.20f;
            critChance = defaultCritChance + 7.5f;  // 17.5%
            additionalMana = 1.05f;
            additionalHealth = 0.95f;
            damageMultiplier = 0.90f;
        }
        // WLOCZEGA: zaden kierunek nie dominuje
        else
        {
            currentProfession = CharacterClass.Traveler;
            moveSpeedMultiplier = 1.10f;
        }
    }

    public void AddExp(int amount)
    {
        currentExp += amount;
        bool leveledUp = false;

        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            level++;
            attributePoints += 2;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * levelScaling);
            leveledUp = true;
        }

        if (leveledUp)
        {
            Debug.Log($"AWANS! Osiagnieto {level} poziom. Punkty do wydania: {attributePoints}");
            RecalculateStats();

            StatsUI statsUI = Object.FindFirstObjectByType<StatsUI>();
            if (statsUI != null && statsUI.gameObject.activeInHierarchy) statsUI.UpdateUI();
        }
    }

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