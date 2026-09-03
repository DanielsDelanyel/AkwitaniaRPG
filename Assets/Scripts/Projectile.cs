using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Lot")]
    public float speed = 15f;
    public float lifeTime = 3f;

    [Header("Obrazenia (wartosc awaryjna)")]
    [Tooltip("Uzywane TYLKO, gdy strzelec nie poda wlasnych obrazen przez Setup(). " +
             "Dzieki temu jeden prefab strzaly dziala dla slabego i mocnego luku.")]
    public int damage = 10;

    [Header("Dzwieki")]
    [Tooltip("Odglos trafienia w cel (gluchy odglos strzaly w cialo).")]
    public AudioClip[] hitSounds;

    [Tooltip("Odglos uderzenia w sciane lub przeszkode.")]
    public AudioClip[] missSounds;

    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("Efekty (opcjonalne)")]
    public GameObject hitEffectPrefab;

    private Rigidbody2D rb;

    // NOWE: efekt dodatkowy przy trafieniu - MOZE pochodzic i z luku, i z zalozonej
    // strzaly (np. zwykly luk + "zatruta strzala" z kolczanu), wiec trzymamy oba
    // i przy trafieniu probujemy nalozyc KAZDY z osobna (dwie niezalezne szanse).
    private OnHitStatusData bowOnHitStatus;
    private OnHitStatusData ammoOnHitStatus;

    // Wywolywane przez PlayerCombat - przekazuje obrazenia policzone
    // ze Zrecznosci gracza i statystyk luku.
    // Gdy nikt tego nie wywola, zostaje wartosc z prefabu.
    public void SetDamage(int dmg)
    {
        damage = Mathf.Max(1, dmg);
    }

    // NOWE: wywolywane przez PlayerCombat razem z SetDamage() - przekazuje
    // ewentualne efekty dodatkowe zalozonego luku i strzaly.
    public void SetOnHitStatuses(OnHitStatusData bowStatus, OnHitStatusData ammoStatus)
    {
        bowOnHitStatus = bowStatus;
        ammoOnHitStatus = ammoStatus;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        float rawAngle = (transform.eulerAngles.z + 45f) * Mathf.Deg2Rad;
        Vector2 flyDirection = new Vector2(Mathf.Cos(rawAngle), Mathf.Sin(rawAngle)).normalized;

        rb.linearVelocity = flyDirection * speed;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // GetComponentInParent - trafienie w dziecko z colliderem tez sie liczy
        Creature creature = collision.GetComponentInParent<Creature>();

        // NOWE: strzala przelatuje przez wlasne przyzwane stworzenia (np. Szkielety),
        // zamiast je ranic - sprawdzamy to PRZED normalnym trafieniem.
        bool isFriendlySummon = creature != null && creature.GetComponent<SummonedCreature>() != null;

        if (creature != null && !creature.IsDead && !isFriendlySummon)
        {
            bool isCrit = Random.Range(0f, 100f) < PlayerStats.instance.critChance;

            int finalDmg = isCrit
                ? Mathf.RoundToInt(damage * PlayerStats.instance.critDamageMultiplier)
                : damage;

            Vector2 hitDir = (collision.transform.position - transform.position).normalized;

            creature.TakeDamage(finalDmg, isCrit, hitDir);

            // NOWE: strzala moze dodatkowo nalozyc status - osobna szansa z luku
            // i osobna z konkretnej strzaly, obie bazowo 0% (patrz OnHitStatusData.cs).
            StatusEffectController targetStatuses = creature.GetComponent<StatusEffectController>();
            if (bowOnHitStatus != null) bowOnHitStatus.TryProc(targetStatuses);
            if (ammoOnHitStatus != null) ammoOnHitStatus.TryProc(targetStatuses);

            SoundManager.Play(hitSounds, soundVolume);
            SpawnEffect();

            Destroy(gameObject);
        }
        else if (collision.CompareTag("Obstacle") || collision.CompareTag("Wall"))
        {
            SoundManager.Play(missSounds, soundVolume);
            SpawnEffect();
            Destroy(gameObject);
        }
    }

    private void SpawnEffect()
    {
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
    }
}
