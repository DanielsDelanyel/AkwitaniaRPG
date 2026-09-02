using System.Collections.Generic;
using UnityEngine;

// ISKRY OGNIA - podstawowy atak rozdzki maga ognia.
//
// W odroznieniu od strzaly z luku: PRZELATUJE przez wszystko (wrogow, przeszkody,
// sciany) i znika dopiero po wyczerpaniu zasiegu - nie po pierwszym trafieniu.
// Kazdego konkretnego wroga rani jednak TYLKO RAZ (patrz alreadyHit), nawet jesli
// fala przez niego "przechodzi" przez cala dlugosc jego hitboxa.
[RequireComponent(typeof(Rigidbody2D))]
public class FireSparkProjectile : WandSpell
{
    [Header("Lot")]
    public float speed = 12f;

    [Header("Wyglad")]
    [Tooltip("Obrot WLASNEJ grafiki wzgledem kierunku lotu (stopnie). 0 = grafika narysowana " +
             "domyslnie w prawo (Wschod) - dostosuj, jesli Twoja fala iskier ma inny domyslny kierunek. " +
             "To pole dziala NIEZALEZNIE od kierunku lotu, wiec samo NIE wplywa na trajektorie.")]
    public float spriteAngleOffset = 0f;

    [Header("Dzwieki")]
    [Tooltip("Odglos trafienia we wroga. Gra przy KAZDYM nowym trafionym celu.")]
    public AudioClip[] hitSounds;

    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("Efekty (opcjonalne)")]
    public GameObject hitEffectPrefab;

    private Rigidbody2D rb;
    private int damage;
    private float maxRange;
    private float aimAngle;

    // Kazdy wrog trafiony NAJWYZEJ RAZ, mimo ze fala go nie omija i leci dalej.
    private readonly HashSet<Creature> alreadyHit = new HashSet<Creature>();

    public override void Setup(int dmg, float range, float aimAngleDegrees)
    {
        damage = Mathf.Max(1, dmg);
        maxRange = range;
        aimAngle = aimAngleDegrees;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        float rad = aimAngle * Mathf.Deg2Rad;
        Vector2 flyDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        rb.linearVelocity = flyDirection * speed;

        // Obrot SPRITE'A liczymy osobno od kierunku lotu - dzieki temu dowolna
        // grafika (narysowana w dowolna strone) da sie dopasowac samym Sprite Angle Offset,
        // bez ryzyka rozjechania sie z rzeczywistym torem lotu.
        transform.rotation = Quaternion.Euler(0f, 0f, aimAngle + spriteAngleOffset);

        // Zasieg zamiast sztywnego czasu zycia - iskra gasnie po przebyciu Spell Range.
        float lifeTime = maxRange > 0f ? maxRange / Mathf.Max(0.01f, speed) : 1f;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Creature creature = collision.GetComponentInParent<Creature>();
        bool isFriendlySummon = creature != null && creature.GetComponent<SummonedCreature>() != null;

        // NOWE: brak niszczenia przy trafieniu - fala leci dalej. Ten sam wrog
        // dostaje obrazenia tylko przy PIERWSZYM zetknieciu, kolejne sa ignorowane.
        if (creature != null && !creature.IsDead && !isFriendlySummon && !alreadyHit.Contains(creature))
        {
            alreadyHit.Add(creature);

            bool isCrit = Random.Range(0f, 100f) < PlayerStats.instance.critChance;
            int finalDmg = isCrit
                ? Mathf.RoundToInt(damage * PlayerStats.instance.critDamageMultiplier)
                : damage;

            Vector2 hitDir = (collision.transform.position - transform.position).normalized;
            creature.TakeDamage(finalDmg, isCrit, hitDir);

            SoundManager.Play(hitSounds, soundVolume);
            if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, collision.transform.position, Quaternion.identity);
        }

        // Celowo BRAK obslugi "Obstacle"/"Wall" - fala ma przelatywac przez wszystko
        // i zniknac dopiero z konca zasiegu (patrz Start() -> Destroy po lifeTime).
    }
}
