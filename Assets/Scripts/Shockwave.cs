using UnityEngine;

// Fala uderzeniowa okrazajaca wlasciciela.
//
// Pozycja sierpa jest LICZONA W KODZIE:
//     pozycja sierpa = srodek fali + kierunek_do_gracza * promien
//
// Nic nie zalezy od obrotu ani skali w prefabie - skrypt ustawia
// dziecko 'Graphics' recznie co klatke. Dzieki temu sierp zawsze
// ladauje po tej stronie, po ktorej stoi gracz.
//
// BUDOWA PREFABU:
//   Shockwave          <- ten skrypt, powstaje na bossie
//     └── Graphics     <- sprite sierpa (pozycja i obrot dowolne, skrypt je nadpisze)
public class Shockwave : MonoBehaviour
{
    [Header("Grafika")]
    [Tooltip("Dziecko ze sprite'em sierpa. Zostaw puste - skrypt znajdzie je sam.")]
    public Transform graphicsTransform;
    public SpriteRenderer waveRenderer;

    [Tooltip("Obrot sierpa wzgledem kierunku ataku. Jesli sierp jest odwrocony " +
             "wypuklascia do srodka, wpisz 180. Jesli lezy bokiem: 90 lub -90.")]
    public float spriteAngleOffset = 0f;

    [Header("Rozmiar sierpa")]
    [Tooltip("Skala sprite'a na starcie.")]
    public float spriteScaleStart = 1f;

    [Tooltip("Skala sprite'a na koncu. Rowna poczatkowej = staly rozmiar.")]
    public float spriteScaleEnd = 1.4f;

    [Header("Zasieg")]
    [Tooltip("Jak daleko od bossa sierp startuje.")]
    public float startRadius = 0.6f;

    [Tooltip("Jak daleko odlatuje, zanim zniknie.")]
    public float maxRadius = 3.5f;
    public float expandTime = 0.45f;

    [Header("Razenie")]
    [Tooltip("Szerokosc razenia w stopniach od kierunku ataku. 360 = dookola.")]
    [Range(10f, 360f)] public float arcDegrees = 150f;

    public int damage = 15;
    [Tooltip("Grubosc czola fali. Wieksza wartosc = trudniej sie wyminac.")]
    public float waveThickness = 0.6f;
    public float knockbackForce = 0f;

    [Header("Wyglad")]
    public float startAlpha = 0.9f;
    public float endAlpha = 0f;
    public Color waveColor = new Color(1f, 0.75f, 0.3f);

    [Header("Czas zycia")]
    public float lingerTime = 0.1f;

    [Header("Diagnostyka")]
    [Tooltip("Wypisze w konsoli, w ktora strone poleciala fala.")]
    public bool logDirection = false;

    private float timer;
    private bool hasHitPlayer;
    private Transform player;
    private Vector2 forward = Vector2.right;
    private bool isReady;

    void Awake()
    {
        if (graphicsTransform == null && transform.childCount > 0)
            graphicsTransform = transform.GetChild(0);

        if (waveRenderer == null) waveRenderer = GetComponentInChildren<SpriteRenderer>();
        if (waveRenderer != null) waveRenderer.color = waveColor;

        // Rodzic zostaje BEZ obrotu i BEZ skali - caly ruch robimy na dziecku
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    // Wywolywane przez BossControllera zaraz po Instantiate
    public void Setup(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;

        forward = direction.normalized;
        isReady = true;

        if (logDirection)
        {
            float a = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            Debug.Log($"Fala: kierunek ({forward.x:0.00}, {forward.y:0.00}), kat {a:0}st.");
        }

        Place(startRadius, 0f);
    }

    void Start()
    {
        if (PlayerStats.instance != null) player = PlayerStats.instance.transform;
        else
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Gdyby ktos stworzyl fale bez Setup - lecimy w prawo, byle nie w miejscu
        if (!isReady) Setup(Vector2.right);

        Destroy(gameObject, expandTime + lingerTime);
    }

    // SERCE CALEJ MECHANIKI: sierp ladauje dokladnie tam, gdzie kaze wektor
    private void Place(float radius, float t)
    {
        if (graphicsTransform == null) return;

        // Pozycja lokalna wzgledem srodka fali - czyli wzgledem bossa
        graphicsTransform.localPosition = new Vector3(forward.x * radius, forward.y * radius, 0f);

        // Sierp patrzy na zewnatrz, w strone lotu
        float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        graphicsTransform.localRotation = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset);

        float scale = Mathf.Lerp(spriteScaleStart, spriteScaleEnd, t);
        graphicsTransform.localScale = new Vector3(scale, scale, 1f);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, expandTime));

        float radius = Mathf.Lerp(startRadius, maxRadius, t);
        Place(radius, t);

        if (waveRenderer != null)
        {
            Color c = waveColor;
            c.a = Mathf.Lerp(startAlpha, endAlpha, t);
            waveRenderer.color = c;
        }

        // --- Obrazenia ---
        if (hasHitPlayer || player == null) return;

        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        float distance = toPlayer.magnitude;

        // Gracz poza wycinkiem, ktory sierp obejmuje?
        if (arcDegrees < 359f && distance > 0.01f)
        {
            if (Vector2.Angle(forward, toPlayer.normalized) > arcDegrees * 0.5f) return;
        }

        // Czolo fali wlasnie do niego doszlo?
        if (distance <= radius && distance >= radius - waveThickness)
        {
            hasHitPlayer = true;

            PlayerStats ps = PlayerStats.instance;
            if (ps == null) return;

            if (ps.IsInvincible())
            {
                Debug.Log("Fala uderzeniowa unikniela!");
                return;
            }

            Vector2 hitDir = toPlayer.normalized;
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

        Vector3 dir = Application.isPlaying ? new Vector3(forward.x, forward.y, 0f) : Vector3.right;
        float half = Mathf.Min(arcDegrees, 359f) * 0.5f;

        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0f, 0f, half) * dir * maxRadius);
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0f, 0f, -half) * dir * maxRadius);
    }
}