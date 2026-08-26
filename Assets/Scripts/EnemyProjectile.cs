using UnityEngine;

// POCISK PRZECIWNIKA (strzala, belt, kamien).
// Lustrzane odbicie Projectile - ta sama idea, ale trafia GRACZA.
//
// Obrazenia nadaje STRZELEC przez Setup(). Pole 'damage' ponizej to tylko
// wartosc awaryjna, gdyby ktos wypuscil pocisk bez inicjalizacji.
public class EnemyProjectile : MonoBehaviour
{
    [Header("Lot")]
    public float speed = 11f;
    public float lifeTime = 4f;

    [Tooltip("Obrot grafiki, jesli strzala w pliku nie lezy poziomo. " +
             "Twoje strzaly rysowane po skosie potrzebuja -45.")]
    public float spriteAngleOffset = -45f;

    [Header("Obrazenia (wartosc awaryjna)")]
    [Tooltip("Uzywane TYLKO, gdy strzelec nie poda wlasnych obrazen przez Setup().")]
    public int damage = 5;

    [Tooltip("Odrzut gracza po trafieniu. 0 = brak.")]
    public float knockbackForce = 0f;

    [Header("Kolizje")]
    [Tooltip("Tagi, na ktorych pocisk sie rozbija.")]
    public string[] obstacleTags = { "Obstacle", "Wall" };

    [Header("Dzwieki")]
    public AudioClip[] hitSounds;
    public AudioClip[] missSounds;
    [Range(0f, 1f)] public float soundVolume = 0.7f;

    [Header("Efekty (opcjonalne)")]
    public GameObject hitEffectPrefab;

    private Rigidbody2D rb;
    private Vector2 flyDirection = Vector2.right;
    private bool wasSetUp;

    // Wywolywane przez lucznika zaraz po Instantiate
    public void Setup(Vector2 direction, int dmg, float projectileSpeed = -1f)
    {
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;

        flyDirection = direction.normalized;
        damage = Mathf.Max(1, dmg);
        if (projectileSpeed > 0f) speed = projectileSpeed;

        wasSetUp = true;

        // Ustawiamy obrot grafiki zgodnie z kierunkiem lotu
        float angle = Mathf.Atan2(flyDirection.y, flyDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset);

        ApplyVelocity();
    }

    void Start()
    {
        // Awaryjnie: pocisk bez Setup() leci tam, gdzie patrzy jego transform
        if (!wasSetUp)
        {
            float rawAngle = (transform.eulerAngles.z - spriteAngleOffset) * Mathf.Deg2Rad;
            flyDirection = new Vector2(Mathf.Cos(rawAngle), Mathf.Sin(rawAngle)).normalized;
            ApplyVelocity();
        }

        Destroy(gameObject, lifeTime);
    }

    private void ApplyVelocity()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = flyDirection * speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HitPlayer(collision);
            return;
        }

        // Rozbicie o przeszkode
        foreach (string tag in obstacleTags)
        {
            if (string.IsNullOrEmpty(tag)) continue;

            if (collision.CompareTag(tag))
            {
                SoundManager.Play(missSounds, soundVolume);
                SpawnEffect();
                Destroy(gameObject);
                return;
            }
        }
    }

    private void HitPlayer(Collider2D collision)
    {
        PlayerStats ps = PlayerStats.instance;
        if (ps == null) return;

        // Dash daje klatki nietykalnosci - strzala wtedy przelatuje obok
        if (ps.IsInvincible())
        {
            Debug.Log("Strzala unikniela dashem!");
            Destroy(gameObject);
            return;
        }

        ps.TakeDamage(damage, false, flyDirection);

        SoundManager.Play(hitSounds, soundVolume);
        SpawnEffect();

        if (knockbackForce > 0f)
        {
            Rigidbody2D prb = collision.GetComponent<Rigidbody2D>();
            if (prb != null) prb.AddForce(flyDirection * knockbackForce, ForceMode2D.Impulse);
        }

        Destroy(gameObject);
    }

    private void SpawnEffect()
    {
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
    }
}