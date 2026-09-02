using System.Collections.Generic;
using UnityEngine;

// PUNKTY UMIEJETNOSCI, ODBLOKOWANE WEZLY I RZUCANIE UMIEJETNOSCI.
//
// Powies obok PlayerStats/PlayerEquipment/PlayerCombat na obiekcie gracza.
// Dziala jak QuestManager: punkty i odblokowane ID trzymane tu, dane
// (koszt/wymagania/efekt) leza w SkillData/SkillDatabase.
public class PlayerSkills : MonoBehaviour
{
    public static PlayerSkills instance;

    [Header("Punkty")]
    public int skillPoints = 0;

    [Tooltip("Co ile poziomow gracz dostaje 1 punkt umiejetnosci.")]
    public int levelsPerSkillPoint = 2;

    // Odblokowane umiejetnosci moga byc wydawane w DOWOLNEJ galezi - profesja
    // przelicza sie automatycznie z rozkladu statystyk (PlayerStats.DetermineProfession),
    // wiec blokowanie po aktualnej klasie odebraloby graczowi wlasne, oplacone umiejetnosci
    // przy kazdej zmianie builda.
    private readonly HashSet<string> unlockedSkillIds = new HashSet<string>();

    // Zywe przyzwania, pogrupowane po ID umiejetnosci ktora je stworzyla -
    // kazda umiejetnosc summonujaca ma WLASNY limit i WLASNA pule (dosumowywana
    // do maxSummonCount przy ponownym rzuceniu, zgodnie z ustaleniami).
    private readonly Dictionary<string, List<Creature>> summonsBySkill = new Dictionary<string, List<Creature>>();

    private readonly Dictionary<string, float> nextCastTime = new Dictionary<string, float>();

    public System.Action onSkillsChanged;

    // NOWE: kierunek dla umiejetnosci typu Active (np. Wir Powietrza Mnicha) -
    // lecą w strone kursora, tak samo jak atak lukiem/rozdzka w PlayerCombat.
    // Kamera lapana leniwie i re-fetchowana, gdyby zostala zniszczona/podmieniona
    // w trakcie gry (patrz identyczna poprawka w PlayerCombat.GetAngleToMouse).
    private Camera mainCam;

    [Header("DEBUG - test bez panelu UI (Faza 4 jeszcze nie istnieje)")]
    [Tooltip("Podepnij tu SkillData Przyzwania Nieumarlych, zeby przetestowac cala " +
             "sciezke bez budowania jeszcze panelu (G). Usun/wylacz, gdy panel bedzie gotowy.")]
    public SkillData debugTestSkill;
    public KeyCode debugCastKey = KeyCode.Alpha1;
    public bool debugAutoUnlock = true;

    void Awake()
    {
        instance = this;
    }

    void OnEnable()
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null) stats.onLevelUp += HandleLevelUp;
    }

    void OnDisable()
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null) stats.onLevelUp -= HandleLevelUp;
    }

    void Update()
    {
        // --- DEBUG: usun ten blok, gdy panel Umiejetnosci bedzie gotowy ---
        if (debugTestSkill != null && Input.GetKeyDown(debugCastKey))
        {
            if (debugAutoUnlock && !IsUnlocked(debugTestSkill))
            {
                unlockedSkillIds.Add(debugTestSkill.GetId());
                Debug.Log($"[DEBUG] Auto-odblokowano '{debugTestSkill.skillName}' do testow.");
            }

            TryCastSkill(debugTestSkill);
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        if (levelsPerSkillPoint <= 0) return;
        if (newLevel % levelsPerSkillPoint != 0) return;

        skillPoints++;
        Debug.Log($"Zdobyto punkt umiejetnosci! Masz ich teraz {skillPoints}.");
        onSkillsChanged?.Invoke();
    }

    // ===============================================================
    // ODBLOKOWYWANIE
    // ===============================================================
    public bool IsUnlocked(SkillData skill)
    {
        return skill != null && unlockedSkillIds.Contains(skill.GetId());
    }

    public bool CanUnlock(SkillData skill)
    {
        if (skill == null) return false;
        if (IsUnlocked(skill)) return false;
        if (skillPoints < skill.cost) return false;

        if (skill.requiredSkills != null)
        {
            foreach (SkillData req in skill.requiredSkills)
            {
                if (req != null && !IsUnlocked(req)) return false;
            }
        }

        return true;
    }

    public bool TryUnlock(SkillData skill)
    {
        if (!CanUnlock(skill)) return false;

        skillPoints -= skill.cost;
        unlockedSkillIds.Add(skill.GetId());

        Debug.Log($"Odblokowano umiejetnosc: {skill.skillName}");
        onSkillsChanged?.Invoke();
        return true;
    }

    // ===============================================================
    // RZUCANIE
    // ===============================================================
    public bool TryCastSkill(SkillData skill)
    {
        if (skill == null) return false;

        if (!IsUnlocked(skill))
        {
            Debug.Log($"{skill.skillName} nie jest jeszcze odblokowane.");
            return false;
        }

        float readyAt = nextCastTime.TryGetValue(skill.GetId(), out float t) ? t : 0f;
        if (Time.time < readyAt)
        {
            Debug.Log($"{skill.skillName} jeszcze sie odnawia ({(readyAt - Time.time):0.0}s).");
            return false;
        }

        PlayerStats stats = PlayerStats.instance;
        if (stats == null) return false;

        float manaCost = GetManaCost(skill, stats);
        float healthCost = GetHealthCost(skill, stats);

        if (manaCost > 0f && stats.currentMana < manaCost)
        {
            Debug.Log("Za malo many!");
            return false;
        }

        // Koszt zdrowia NIGDY nie moze zabic - gracz musi zostac z co najmniej 1 punktem.
        if (healthCost > 0f && stats.currentHealth <= Mathf.CeilToInt(healthCost))
        {
            Debug.Log("Za malo zdrowia na te umiejetnosc!");
            return false;
        }

        bool success = false;
        switch (skill.effectType)
        {
            case SkillEffectType.Summon:
                success = CastSummon(skill, stats);
                break;
            case SkillEffectType.Active:
                success = CastActiveEffect(skill, stats);
                break;
            default:
                Debug.LogWarning($"Typ umiejetnosci {skill.effectType} nie jest jeszcze obslugiwany.");
                break;
        }

        if (success)
        {
            if (manaCost > 0f) stats.currentMana -= manaCost;

            if (healthCost > 0f)
            {
                stats.currentHealth = Mathf.Max(1, stats.currentHealth - Mathf.RoundToInt(healthCost));
                stats.onHealthChangedCallback?.Invoke();
            }

            if (skill.cooldown > 0f) nextCastTime[skill.GetId()] = Time.time + skill.cooldown;
        }

        return success;
    }

    // Koszty procentowe licza sie z AKTUALNYCH maksimow gracza (rosna wraz z levelem/statystykami).
    // Gdy pole *Percent w SkillData jest > 0, ma pierwszenstwo nad statym polem *Cost.
    private float GetManaCost(SkillData skill, PlayerStats stats)
    {
        if (skill.manaCostPercent > 0f) return stats.GetMaxMana() * (skill.manaCostPercent / 100f);
        return skill.manaCost;
    }

    private float GetHealthCost(SkillData skill, PlayerStats stats)
    {
        if (skill.healthCostPercent > 0f) return stats.GetMaxHealth() * (skill.healthCostPercent / 100f);
        return 0f;
    }

    private bool CastSummon(SkillData skill, PlayerStats stats)
    {
        if (skill.summonPrefab == null)
        {
            Debug.LogWarning($"{skill.skillName}: brak przypisanego Summon Prefab!");
            return false;
        }

        string id = skill.GetId();
        if (!summonsBySkill.TryGetValue(id, out List<Creature> living))
        {
            living = new List<Creature>();
            summonsBySkill[id] = living;
        }

        // Sprzatamy martwych/zniszczonych z listy przed liczeniem, ile jeszcze wolno dosumowac
        living.RemoveAll(c => c == null || c.IsDead);

        int vit = stats.GetTotal(stats.baseWIT, stats.equipWIT);
        int intel = stats.GetTotal(stats.baseINT, stats.equipINT);

        int desiredCount = skill.baseSummonCount;
        if (skill.statPointsPerExtraSummon > 0f)
        {
            desiredCount += Mathf.FloorToInt((vit + intel) / skill.statPointsPerExtraSummon);
        }
        desiredCount = Mathf.Clamp(desiredCount, 0, skill.maxSummonCount);

        int toSpawn = Mathf.Max(0, desiredCount - living.Count);
        if (toSpawn <= 0)
        {
            Debug.Log($"{skill.skillName}: limit przyzwanych stworzen juz osiagniety ({living.Count}/{skill.maxSummonCount}).");
            return false;
        }

        float healthMult = 1f + (vit * skill.summonHealthPercentPerVitality / 100f);
        float damageMult = 1f + (intel * skill.summonDamagePercentPerIntelligence / 100f);

        for (int i = 0; i < toSpawn; i++)
        {
            Vector3 spawnPos = transform.position + (Vector3)(Random.insideUnitCircle * 1.5f);
            GameObject obj = Instantiate(skill.summonPrefab, spawnPos, Quaternion.identity);

            Creature creature = obj.GetComponent<Creature>();
            if (creature == null)
            {
                Debug.LogError($"{skill.skillName}: Summon Prefab nie ma komponentu Creature!");
                Destroy(obj);
                continue;
            }

            // Znacznik "to jest przyjazne stworzenie gracza" - chroni je przed
            // trafieniami PlayerMeleeAttack/Projectile i mowi jego wlasnemu AI,
            // kogo NIE atakowac.
            if (obj.GetComponent<SummonedCreature>() == null) obj.AddComponent<SummonedCreature>();

            // WAZNE: liczymy baze PRZED nadpisaniem maxHealth, bo Creature.Start()
            // (ktory jeszcze sie nie wykonal) inaczej nadpisalby zerem, gdyby
            // prefab mial maxHealth=0 (poleganie na auto-wyliczeniu z Witalnosci).
            int baseMaxHealth = creature.maxHealth > 0 ? creature.maxHealth : creature.baseWIT * creature.healthPerVitality;

            creature.maxHealth = Mathf.Max(1, Mathf.RoundToInt(baseMaxHealth * healthMult));
            creature.currentHealth = creature.maxHealth;
            creature.baseDmg = Mathf.Max(1, Mathf.RoundToInt(creature.baseDmg * damageMult));

            SummonedCreatureAI ai = obj.GetComponent<SummonedCreatureAI>();
            if (ai == null) ai = obj.AddComponent<SummonedCreatureAI>();
            ai.owner = transform;
            ai.attackDamage = creature.baseDmg;

            living.Add(creature);
        }

        Debug.Log($"{skill.skillName}: przyzwano {toSpawn} nowych. Razem: {living.Count}/{skill.maxSummonCount}.");
        onSkillsChanged?.Invoke();
        return true;
    }

    // NOWE: umiejetnosci typu Active - jednorazowy efekt (np. Wir Powietrza Mnicha),
    // ktory sam sobie zyje wlasnym zyciem po rzuceniu (patrz ActiveSkillEffect).
    // W odroznieniu od Summon nie trzeba tu niczego sledzic ani limitowac -
    // obiekt sam sie zniszczy po uplywie obliczonego "duration".
    private bool CastActiveEffect(SkillData skill, PlayerStats stats)
    {
        if (skill.activeEffectPrefab == null)
        {
            Debug.LogWarning($"{skill.skillName}: brak przypisanego Active Effect Prefab!");
            return false;
        }

        int intelligence = stats.GetTotal(stats.baseINT, stats.equipINT);
        int dexterity = stats.GetTotal(stats.baseZR, stats.equipZR);

        // Obrazenia i czas trwania rosna razem z Inteligencja I Zrecznoscia -
        // dokladnie jak prosiles, procenty z obu statystyk po prostu sie sumuja.
        float damageMult = 1f
            + (intelligence * skill.damagePercentPerIntelligence / 100f)
            + (dexterity * skill.damagePercentPerDexterity / 100f);
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(skill.baseDamage * damageMult));

        float durationMult = 1f
            + (intelligence * skill.durationPercentPerIntelligence / 100f)
            + (dexterity * skill.durationPercentPerDexterity / 100f);
        float finalDuration = Mathf.Max(0.1f, skill.baseDuration * durationMult);

        float angle = GetAimAngle();

        GameObject effectObj = Instantiate(skill.activeEffectPrefab, transform.position, Quaternion.identity);

        ActiveSkillEffect effect = effectObj.GetComponent<ActiveSkillEffect>();
        if (effect == null)
        {
            Debug.LogError($"{skill.skillName}: Active Effect Prefab nie ma komponentu implementujacego ActiveSkillEffect!");
            Destroy(effectObj);
            return false;
        }

        effect.Setup(finalDamage, finalDuration, skill.effectSpeed, angle);

        Debug.Log($"{skill.skillName}: rzucono - obrazenia {finalDamage}, czas trwania {finalDuration:0.0}s.");
        return true;
    }

    // Kierunek rzutu w strone kursora - tak samo jak atak lukiem/rozdzka w PlayerCombat.
    private float GetAimAngle()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return 0f;

        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = mousePos - transform.position;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    // ===============================================================
    // ODCZYT ODNOWIENIA (do paska umiejetnosci widocznego podczas rozgrywki)
    // ===============================================================
    public float GetCooldownRemainingSeconds(SkillData skill)
    {
        if (skill == null) return 0f;
        if (!nextCastTime.TryGetValue(skill.GetId(), out float readyAt)) return 0f;
        return Mathf.Max(0f, readyAt - Time.time);
    }

    // 1 = wlasnie uzyta (pelne odnowienie przed nami), 0 = gotowa do uzycia.
    // Dokladnie to, czego potrzeba jako fillAmount "szarej naklejki" na ikonie.
    public float GetCooldownFraction(SkillData skill)
    {
        if (skill == null || skill.cooldown <= 0f) return 0f;
        return Mathf.Clamp01(GetCooldownRemainingSeconds(skill) / skill.cooldown);
    }

    public int GetLivingSummonCount(SkillData skill)
    {
        if (skill == null) return 0;
        if (!summonsBySkill.TryGetValue(skill.GetId(), out List<Creature> living)) return 0;

        living.RemoveAll(c => c == null || c.IsDead);
        return living.Count;
    }

    // ===============================================================
    // ZAPIS / WCZYTYWANIE
    // ===============================================================
    public List<string> GetUnlockedForSave()
    {
        return new List<string>(unlockedSkillIds);
    }

    public void LoadFrom(int savedPoints, List<string> savedUnlocked)
    {
        skillPoints = savedPoints;
        unlockedSkillIds.Clear();

        if (savedUnlocked != null)
        {
            foreach (string id in savedUnlocked) unlockedSkillIds.Add(id);
        }

        onSkillsChanged?.Invoke();
    }

    // Wolane przy zaczynaniu nowej gry - patrz SaveManager.ResetSession()
    public void ClearAll()
    {
        skillPoints = 0;
        unlockedSkillIds.Clear();
        summonsBySkill.Clear();
        nextCastTime.Clear();

        onSkillsChanged?.Invoke();
    }
}
