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

    // Wywolywane przez PlayerCombat - przekazuje obrazenia policzone
    // ze Zrecznosci gracza i statystyk luku.
    // Gdy nikt tego nie wywola, zostaje wartosc z prefabu.
    public void SetDamage(int dmg)
    {
        damage = Mathf.Max(1, dmg);
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

        if (creature != null && !creature.IsDead)
        {
            bool isCrit = Random.Range(0f, 100f) < PlayerStats.instance.critChance;

            int finalDmg = isCrit
                ? Mathf.RoundToInt(damage * PlayerStats.instance.critDamageMultiplier)
                : damage;

            Vector2 hitDir = (collision.transform.position - transform.position).normalized;

            creature.TakeDamage(finalDmg, isCrit, hitDir);

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