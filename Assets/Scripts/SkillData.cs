using UnityEngine;

// DO JAKIEGO EFEKTU SLUZY UMIEJETNOSC.
// Na razie obslugiwany jest tylko Summon (przyzwanie). Passive/Active to
// zaslepki pod przyszle typy umiejetnosci (staly bonus / jednorazowe zaklecie) -
// dodane juz teraz, zeby nie trzeba bylo migrowac zapisanych ID pozniej.
public enum SkillEffectType
{
    Summon,
    Passive,
    Active
}

// POJEDYNCZY WEZEL W DRZEWKU UMIEJETNOSCI.
// Dziala jak Quest/ItemData: jeden ScriptableObject na kazda umiejetnosc,
// zarejestrowany w SkillDatabase.
[CreateAssetMenu(fileName = "Nowa Umiejetnosc", menuName = "Umiejetnosci/Umiejetnosc")]
public class SkillData : ScriptableObject
{
    [Header("Identyfikacja")]
    [Tooltip("Unikalny identyfikator do zapisu gry. Zostaw puste - wypelni sie nazwa pliku. " +
             "Po pierwszym zapisie NIE zmieniaj.")]
    public string skillId = "";

    public string skillName = "Nowa Umiejetnosc";
    public Sprite icon;

    [TextArea(2, 6)]
    public string description = "Opis umiejetnosci...";

    [Header("Drzewko")]
    [Tooltip("Do ktorej galezi (profesji) nalezy ten wezel. Decyduje, w ktorym " +
             "ramieniu panelu Umiejetnosci (G) sie pokaze.")]
    public CharacterClass profession = CharacterClass.Nekromancer;

    [Tooltip("Umiejetnosci, ktore gracz musi miec juz odblokowane, zanim ta stanie sie dostepna. " +
             "Puste = wezel startowy tej galezi (dostepny od razu, jesli sa punkty).")]
    public SkillData[] requiredSkills;

    [Tooltip("Ile punktow umiejetnosci kosztuje odblokowanie.")]
    [Min(1)] public int cost = 1;

    [Header("Efekt")]
    public SkillEffectType effectType = SkillEffectType.Summon;

    [Header("Koszt Rzucenia (Summon / Active)")]
    [Tooltip("Ile many zabiera UZYCIE umiejetnosci (nie odblokowanie). 0 = za darmo. " +
             "Ignorowane, jesli Mana Cost Percent ponizej jest > 0.")]
    public float manaCost = 0f;

    [Tooltip("Alternatywa dla Mana Cost: ile PROCENT maksymalnej many gracza kosztuje uzycie (0-100). " +
             "Gdy > 0, MA PIERWSZENSTWO nad Mana Cost powyzej - ustaw wtedy Mana Cost na 0.")]
    [Range(0f, 100f)] public float manaCostPercent = 0f;

    [Tooltip("Ile PROCENT maksymalnego zdrowia gracza kosztuje uzycie (0-100). 0 = brak kosztu zdrowia. " +
             "Ten koszt NIGDY nie zabija - gracz zawsze zostaje z co najmniej 1 punktem zdrowia.")]
    [Range(0f, 100f)] public float healthCostPercent = 0f;

    [Tooltip("Czas odnowienia w sekundach miedzy kolejnymi uzyciami. 0 = bez odnowienia.")]
    public float cooldown = 0f;

    // ===============================================================
    // PRZYZWANIE - pola uzywane tylko gdy effectType == Summon.
    // Ilosc i sila slug skaluja sie z WIT + INT gracza w momencie rzucenia.
    // ===============================================================
    [Header("Przyzwanie (tylko SkillEffectType.Summon)")]
    [Tooltip("Prefab przyzywanego stworzenia. Musi miec na sobie komponent Creature.")]
    public GameObject summonPrefab;

    [Tooltip("Ile stworzen przyzywa sie przy zerowym bonusie ze statystyk.")]
    [Min(0)] public int baseSummonCount = 1;

    [Tooltip("Ile PUNKTOW (Witalnosc + Inteligencja razem) trzeba, zeby dostac " +
             "JEDNEGO dodatkowego sluge ponad baseSummonCount.")]
    public float statPointsPerExtraSummon = 5f;

    [Tooltip("Twardy limit zywych stworzen przyzwanych TA umiejetnoscia naraz, " +
             "niezaleznie od tego, ile wyszloby ze statystyk.")]
    [Min(1)] public int maxSummonCount = 8;

    [Tooltip("O ile % rosnie zdrowie przyzwanego stworzenia za KAZDY punkt Witalnosci gracza.")]
    public float summonHealthPercentPerVitality = 5f;

    [Tooltip("O ile % rosnie obrazenia przyzwanego stworzenia za KAZDY punkt Inteligencji gracza.")]
    public float summonDamagePercentPerIntelligence = 5f;

    // ===============================================================
    // AKTYWNY EFEKT - pola uzywane tylko gdy effectType == Active.
    // Np. Wir Powietrza Mnicha (WindVortexEffect). Obrazenia i czas trwania
    // skaluja sie z INT + ZR gracza w momencie rzucenia, predkosc jest stala
    // (czysto konfiguracyjna, nie zalezy od statystyk).
    // ===============================================================
    [Header("Aktywny Efekt (tylko SkillEffectType.Active)")]
    [Tooltip("Prefab efektu (np. Wir Powietrza). Musi miec komponent implementujacy ActiveSkillEffect.")]
    public GameObject activeEffectPrefab;

    [Tooltip("Obrazenia przy zerowym bonusie ze statystyk.")]
    [Min(1)] public int baseDamage = 5;

    [Tooltip("O ile % rosna obrazenia za KAZDY punkt Inteligencji gracza.")]
    public float damagePercentPerIntelligence = 3f;

    [Tooltip("O ile % rosna obrazenia za KAZDY punkt Zrecznosci gracza.")]
    public float damagePercentPerDexterity = 3f;

    [Tooltip("Czas zycia efektu w sekundach przy zerowym bonusie ze statystyk.")]
    public float baseDuration = 3f;

    [Tooltip("O ile % rosnie czas trwania za KAZDY punkt Inteligencji gracza.")]
    public float durationPercentPerIntelligence = 2f;

    [Tooltip("O ile % rosnie czas trwania za KAZDY punkt Zrecznosci gracza.")]
    public float durationPercentPerDexterity = 2f;

    [Tooltip("Predkosc lotu efektu. NIE zalezy od statystyk - czysto konfiguracyjne pole.")]
    public float effectSpeed = 6f;

    public string GetId()
    {
        return string.IsNullOrEmpty(skillId) ? name : skillId;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrEmpty(skillId)) skillId = name;
    }
#endif
}
