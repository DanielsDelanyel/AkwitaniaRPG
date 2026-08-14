using UnityEngine;

// Rozchodzaca sie fala uderzeniowa. Trafia gracza raz - w momencie,
// gdy czolo fali przez niego przechodzi. Da sie ja przeskoczyc dashem,
// bo dash daje klatki nietykalnosci.
public class Shockwave : MonoBehaviour
{
    [Header("Zasieg")]
    public float startRadius = 0.2f;
    public float maxRadius = 3.5f;
    public float expandTime = 0.45f;

    [Header("Obrazenia")]
    public int damage = 15;
    [Tooltip("Grubosc czola fali. Wieksza wartosc = trudniej sie wyminac.")]
    public float waveThickness = 0.6f;

    [Header("Odrzut gracza")]
    public float knockbackForce = 0f;

    [Header("Wyglad")]
    public SpriteRenderer waveRenderer;
    [Tooltip("Ile jednostek swiata zajmuje sprite przy skali 1. Zwykle 1.")]
    public float spriteUnitSize = 1f;
    public float startAlpha = 0.9f;
    public float endAlpha = 0f;
    public Color waveColor = new Color(1f, 0.75f, 0.3f);

    [Header("Czas zycia")]
    [Tooltip("Ile jeszcze zyje po osiagnieciu maksimum.")]
    public float lingerTime = 0.1f;

    private float timer;
    private bool hasHitPlayer;
    private Transform player;

    void Awake()
    {
        if (waveRenderer == null) waveRenderer = GetComponentInChildren<SpriteRenderer>();
        if (waveRenderer != null) waveRenderer.color = waveColor;
    }

    void Start()
    {
        if (PlayerStats.instance != null) player = PlayerStats.instance.transform;
        else
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        Destroy(gameObject, expandTime + lingerTime);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, expandTime));

        float radius = Mathf.Lerp(startRadius, maxRadius, t);

        // --- Wyglad ---
        if (waveRenderer != null)
        {
            // srednica = 2 * promien, przeliczona na skale sprite'a
            float scale = (radius * 2f) / Mathf.Max(0.0001f, spriteUnitSize);
            transform.localScale = new Vector3(scale, scale, 1f);

            Color c = waveColor;
            c.a = Mathf.Lerp(startAlpha, endAlpha, t);
            waveRenderer.color = c;
        }

        // --- Obrazenia ---
        if (hasHitPlayer || player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Czolo fali wlasnie doszlo do gracza?
        if (distance <= radius && distance >= radius - waveThickness)
        {
            hasHitPlayer = true; // niezaleznie od wyniku - fala mija gracza tylko raz

            PlayerStats ps = PlayerStats.instance;
            if (ps == null) return;

            // Uniknieta dashem? Nie zabieramy zycia.
            if (ps.IsInvincible())
            {
                Debug.Log("Fala uderzeniowa uniknieta!");
                return;
            }

            Vector2 hitDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            ps.TakeDamage(damage, false, hitDir);

            if (knockbackForce > 0f)
            {
                Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
                if (prb != null) prb.AddForce(hitDir * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, maxRadius);
    }
}
