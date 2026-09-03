using System.Collections.Generic;
using UnityEngine;

// CHMURA TRUJACEGO GAZU - rozdzka nekromanty.
//
// LECI w strone kursora dokladnie jak iskry ognia (FireSparkProjectile) - z tym,
// ze W TRAKCIE LOTU stopniowo ROSNIE (a razem z nia jej kolizja, bo obie sa tym
// samym obiektem - Collider2D skaluje sie automatycznie razem z transformem) i
// BLAKNIE (traci alpha), az calkiem zniknie. Zasieg rozdzki (ItemData.spellRange)
// dziala tak samo jak przy iskrach - okresla, jak dlugo chmura zyje/leci.
//
// Kazdego, kogo dotknie, ZATRUWA (StatusEffectController.ApplyPoison) zamiast
// zadawac obrazenia od razu - a poniewaz to ten sam mechanizm "wejscie/wyjscie z
// kolizji" co Wir Powietrza Mnicha, cel stojacy w rosnacej chmurze przez dluzszy
// czas moze zostac zatruty ponownie, jesli zdazy wyjsc i wrocic.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class PoisonCloudSpell : WandSpell
{
    [Header("Lot")]
    public float speed = 3f;

    [Header("Rozrost (kolizja rosnie RAZEM z grafika - to ten sam obiekt)")]
    [Tooltip("Skala chmury na starcie lotu.")]
    public float startScale = 0.4f;
    [Tooltip("Skala chmury pod koniec zycia.")]
    public float endScale = 2.5f;
    [Tooltip("Krzywa wzrostu w czasie (0 = start lotu, 1 = koniec zycia). " +
             "Zostaw domyslna (liniowa), jesli nie chcesz nic stroic.")]
    public AnimationCurve growthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Blaknienie")]
    [Range(0f, 1f)] public float startAlpha = 1f;
    [Tooltip("Od ktorego % zycia chmura zaczyna sie robic przezroczysta (0.6 = od 60% czasu).")]
    [Range(0f, 1f)] public float fadeStartAt = 0.5f;

    [Header("Zatrucie")]
    public float poisonTickInterval = 0.5f;
    public float poisonDuration = 3f;

    [Header("Dzwieki (opcjonalne)")]
    public AudioClip[] hitSounds;
    [Range(0f, 1f)] public float soundVolume = 0.5f;

    [Header("Efekty (opcjonalne)")]
    [Tooltip("Np. wizualny puf/rozwianie gazu przy zniknieciu chmury.")]
    public GameObject dissipateEffectPrefab;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private int poisonTickDamage;
    private float lifeTime;
    private float age;

    // NOWE: kto rzucil te chmure - zeby jej NIE zatruwac (patrz WandSpell.SetCaster,
    // wywolywane przez PlayerCombat.PerformWandAttack zaraz po Setup()).
    private Transform caster;

    // Ten sam patent co Wir Powietrza - "wyjscie z kolizji" pozwala ponownie
    // zatruc ten sam kolider, jesli nadal stoi w rosnacej chmurze.
    private readonly HashSet<Collider2D> currentlyTouching = new HashSet<Collider2D>();

    public override void Setup(int damage, float maxRange, float aimAngleDegrees)
    {
        // "damage" z WandSpell.Setup (Inteligencja gracza + bonus rozdzki) staje sie
        // obrazeniami NA TIK zatrucia - rosnie ze statystykami/ekwipunkiem gracza.
        poisonTickDamage = Mathf.Max(1, damage);

        rb = GetComponent<Rigidbody2D>();
        float rad = aimAngleDegrees * Mathf.Deg2Rad;
        Vector2 flyDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        rb.linearVelocity = flyDirection * speed;

        // Tak samo jak iskry ognia: Zasieg rozdzki okresla dlugosc lotu/zycia,
        // a nie sztywna liczba sekund wpisana w Inspektorze.
        lifeTime = maxRange > 0f ? maxRange / Mathf.Max(0.01f, speed) : 4f;
        Destroy(gameObject, lifeTime);
    }

    public override void SetCaster(Transform casterTransform)
    {
        caster = casterTransform;
    }

    void Start()
    {
        GetComponent<CircleCollider2D>().isTrigger = true;

        sprite = GetComponentInChildren<SpriteRenderer>();
        transform.localScale = Vector3.one * startScale;
        SetAlpha(startAlpha);
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / Mathf.Max(0.01f, lifeTime));

        float scale = Mathf.Lerp(startScale, endScale, growthCurve.Evaluate(t));
        transform.localScale = Vector3.one * scale;

        if (t >= fadeStartAt)
        {
            float fadeT = Mathf.InverseLerp(fadeStartAt, 1f, t);
            SetAlpha(Mathf.Lerp(startAlpha, 0f, fadeT));
        }
    }

    private void SetAlpha(float alpha)
    {
        if (sprite == null) return;
        Color c = sprite.color;
        c.a = alpha;
        sprite.color = c;
    }

    void OnDestroy()
    {
        if (dissipateEffectPrefab != null)
            Instantiate(dissipateEffectPrefab, transform.position, Quaternion.identity);
    }

    // Celowo BRAK Creature-specific checkow poza "wlasne przyzwanie" - szukamy
    // od razu StatusEffectController, wiec to samo dziala i na wrogach, i na
    // graczu, jesli sam wejdzie we wlasna chmure (a obiekty bez tego komponentu,
    // np. sciany/przeszkody, po prostu nigdy go nie znajda i sa ignorowane).
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (currentlyTouching.Contains(collision)) return;

        // NOWE: nigdy nie zatruwamy WLASNEGO rzucajacego (np. gracza, ktory rzucil
        // chmure tuz przed sobą i sam w niej stoi). IsChildOf zwraca true tez dla
        // DOKLADNIE TEGO SAMEGO transformu, wiec dziala niezaleznie od tego, czy
        // collider gracza siedzi na glownym obiekcie, czy na jego dziecku.
        if (caster != null && collision.transform.IsChildOf(caster)) return;

        Creature creature = collision.GetComponentInParent<Creature>();
        if (creature != null)
        {
            if (creature.IsDead) return;
            if (creature.GetComponent<SummonedCreature>() != null) return; // wlasne przyzwania odporne
        }

        StatusEffectController target = collision.GetComponentInParent<StatusEffectController>();
        if (target == null) return;

        currentlyTouching.Add(collision);
        target.ApplyPoison(poisonTickDamage, poisonTickInterval, poisonDuration);
        SoundManager.Play(hitSounds, soundVolume);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        currentlyTouching.Remove(collision);
    }
}
