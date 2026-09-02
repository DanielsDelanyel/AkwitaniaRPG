using System.Collections.Generic;
using UnityEngine;

// CHMURA TRUJACEGO GAZU - rozdzka nekromanty. W odroznieniu od iskier (fala LECACA,
// przebijajaca wszystko) to STATYCZNY obszar: pojawia sie w miejscu wyznaczonym przez
// zasieg rzutu i zostaje tam przez pewien czas, zadajac obrazenia w regularnych odstepach
// kazdemu, kto stoi w srodku - w tym takze graczowi, jesli sam w nia wejdzie.
//
// UWAGA: to pierwsza wersja - zanim bedziesz stroil promien/czas trwania/obrazenia na tik,
// polecam najpierw dokonczyc iskry ognia. Numery ponizej sa punktem startowym, nie balansem.
[RequireComponent(typeof(CircleCollider2D))]
public class PoisonCloudSpell : WandSpell
{
    [Header("Chmura")]
    [Tooltip("Jak dlugo chmura wisi w powietrzu, zanim zniknie.")]
    public float cloudDuration = 4f;

    [Tooltip("Co ile sekund chmura zadaje obrazenia kazdemu, kto w niej stoi.")]
    public float tickInterval = 0.5f;

    [Tooltip("Promien chmury. Ustawiany TU, a nie recznie na CircleCollider2D - " +
             "Start() nadpisze Radius kolidera ta wartoscia.")]
    public float cloudRadius = 1.5f;

    [Header("Wyglad")]
    [Tooltip("Obrot WLASNEJ grafiki, jesli chmura nie jest symetryczna/okragla. 0 = bez obrotu.")]
    public float spriteAngleOffset = 0f;

    [Header("Dzwieki (opcjonalne)")]
    public AudioClip[] tickSounds;
    [Range(0f, 1f)] public float soundVolume = 0.5f;

    [Header("Efekty (opcjonalne)")]
    [Tooltip("Np. wizualny puf/rozwianie gazu przy zniknieciu chmury.")]
    public GameObject dissipateEffectPrefab;

    private int damagePerTick;
    private float tickTimer;

    private readonly List<Creature> creaturesInside = new List<Creature>();

    public override void Setup(int damage, float maxRange, float aimAngleDegrees)
    {
        damagePerTick = Mathf.Max(1, damage);

        // Chmura "emituje sie" NA KONCU zasiegu rzutu, nie pod nogami gracza.
        float rad = aimAngleDegrees * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        transform.position += (Vector3)(direction * maxRange);

        transform.rotation = Quaternion.Euler(0f, 0f, aimAngleDegrees + spriteAngleOffset);
    }

    void Start()
    {
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = cloudRadius;

        Destroy(gameObject, cloudDuration);
    }

    void OnDestroy()
    {
        if (dissipateEffectPrefab != null)
            Instantiate(dissipateEffectPrefab, transform.position, Quaternion.identity);
    }

    void Update()
    {
        tickTimer -= Time.deltaTime;
        if (tickTimer <= 0f)
        {
            tickTimer = Mathf.Max(0.05f, tickInterval);
            ApplyTick();
        }
    }

    private void ApplyTick()
    {
        creaturesInside.RemoveAll(c => c == null || c.IsDead);
        if (creaturesInside.Count == 0) return;

        bool hitAnything = false;
        foreach (Creature creature in creaturesInside)
        {
            // Obrazenia "od trucizny" - bez szansy na trafienie krytyczne, to nie jest cios.
            creature.TakeDamage(damagePerTick, false, Vector2.zero);
            hitAnything = true;
        }

        if (hitAnything) SoundManager.Play(tickSounds, soundVolume);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Creature creature = collision.GetComponentInParent<Creature>();

        // Wlasne przyzwane stworzenia (Szkielety) sa odporne na wlasna trucizne gracza.
        bool isFriendlySummon = creature != null && creature.GetComponent<SummonedCreature>() != null;

        if (creature != null && !creature.IsDead && !isFriendlySummon && !creaturesInside.Contains(creature))
        {
            creaturesInside.Add(creature);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Creature creature = collision.GetComponentInParent<Creature>();
        if (creature != null) creaturesInside.Remove(creature);
    }
}
