using UnityEngine;

public class TopDownMovement : MonoBehaviour
{
    [Header("Ruch")]
    public float moveSpeed = 5f;

    // ===============================================================
    // DASH
    // Sila  -> DYSTANS skoku (jak daleko doskoczysz)
    // Zrecznosc -> PREDKOSC skoku i KROTSZY cooldown (jak czesto)
    // ===============================================================
    [Header("Dash - Sterowanie")]
    public KeyCode dashKey = KeyCode.Space;
    public bool allowDashWhileStanding = true; // dash w strone, w ktora patrzysz, gdy stoisz

    [Header("Dash - Dystans (Sila)")]
    public float dashDistanceBase = 1.5f;        // dystans przy 0 Sily
    public float dashDistancePerStrength = 0.1f; // ile metrow dodaje 1 pkt Sily
    public float dashDistanceMax = 6f;           // sufit, zeby Barbarzynca nie latal przez mape

    [Header("Dash - Predkosc (Zrecznosc)")]
    public float dashSpeedBase = 10f;
    public float dashSpeedPerDexterity = 0.4f;
    public float dashSpeedMax = 28f;

    [Header("Dash - Odnowienie (Zrecznosc)")]
    public float dashCooldownBase = 3f;
    public float dashCooldownPerDexterity = 0.08f;
    public float dashCooldownMin = 0.35f;

    [Header("Dash Bezpieczenstwo")]
    public bool invincibleDuringDash = true;

    [Header("Dash - Efekty (opcjonalne)")]
    public GameObject dashEffectPrefab;
    public string dashAnimatorTrigger = ""; // wpisz np. "Dash", jesli masz taka animacje

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.down; // pamiec, w ktora strone patrzymy

    private bool isDashing;
    private float dashTimeLeft;
    private float dashCooldownLeft;
    private float dashCooldownTotal;
    private Vector2 dashDirection;
    private float dashCurrentSpeed;

    // Do podpiecia pod UI (np. ikonka odnowienia dasha)
    public bool IsDashing { get { return isDashing; } }
    public float DashCooldownLeft { get { return dashCooldownLeft; } }
    public float DashCooldownTotal { get { return Mathf.Max(0.01f, dashCooldownTotal); } }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void OnDisable()
    {
        // Gdy skrypt wylaczy sie w trakcie skoku (np. otwarcie ekwipunku),
        // nie chcemy, by postac po powrocie dalej "leciala".
        isDashing = false;
        dashTimeLeft = 0f;
    }

    void Update()
    {
        // 1. Odczyt klawiszy
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        if (moveInput != Vector2.zero) facingDirection = moveInput;

        // 2. Cooldown dasha leci caly czas
        if (dashCooldownLeft > 0f) dashCooldownLeft -= Time.deltaTime;

        // 3. Proba dasha
        if (Input.GetKeyDown(dashKey)) TryDash();

        // 4. Animator
        if (anim != null)
        {
            anim.SetFloat("Speed", isDashing ? 1f : moveInput.sqrMagnitude);

            if (isDashing)
            {
                anim.SetFloat("Horizontal", dashDirection.x);
                anim.SetFloat("Vertical", dashDirection.y);
            }
            else if (moveInput != Vector2.zero)
            {
                anim.SetFloat("Horizontal", moveInput.x);
                anim.SetFloat("Vertical", moveInput.y);
            }
        }
    }

    void FixedUpdate()
    {
        // DASH ma pierwszenstwo przed zwyklym chodzeniem
        if (isDashing)
        {
            rb.MovePosition(rb.position + dashDirection * dashCurrentSpeed * Time.fixedDeltaTime);

            dashTimeLeft -= Time.fixedDeltaTime;
            if (dashTimeLeft <= 0f) isDashing = false;
            return;
        }

        float currentSpeed = moveSpeed;
        if (PlayerStats.instance != null) currentSpeed *= PlayerStats.instance.moveSpeedMultiplier;

        rb.MovePosition(rb.position + moveInput * currentSpeed * Time.fixedDeltaTime);
    }

    private void TryDash()
    {
        if (isDashing || dashCooldownLeft > 0f) return;

        Vector2 direction = moveInput != Vector2.zero ? moveInput : facingDirection;
        if (direction == Vector2.zero) return;
        if (moveInput == Vector2.zero && !allowDashWhileStanding) return;

        PlayerStats st = PlayerStats.instance;


        // --- MATEMATYKA SKOKU ---
        int strength = st != null ? st.GetTotal(st.baseSTR, st.equipSTR) : 0;
        int dexterity = st != null ? st.GetTotal(st.baseZR, st.equipZR) : 0;

        // SILA -> jak daleko
        float distance = Mathf.Clamp(
            dashDistanceBase + strength * dashDistancePerStrength,
            dashDistanceBase, dashDistanceMax);

        // ZRECZNOSC -> jak szybko
        float speed = Mathf.Clamp(
            dashSpeedBase + dexterity * dashSpeedPerDexterity,
            dashSpeedBase, dashSpeedMax);

        // ZRECZNOSC -> jak czesto
        dashCooldownTotal = Mathf.Max(
            dashCooldownMin,
            dashCooldownBase - dexterity * dashCooldownPerDexterity);

        dashDirection = direction;
        dashCurrentSpeed = speed;
        dashTimeLeft = distance / speed;   // czas trwania = droga / predkosc
        dashCooldownLeft = dashCooldownTotal;
        isDashing = true;
        facingDirection = direction;

        // Klatki nietykalnosci na czas skoku (unik przed ciosem)
        if (invincibleDuringDash && st != null) st.GrantInvincibility(dashTimeLeft + 0.05f);

        if (anim != null && !string.IsNullOrEmpty(dashAnimatorTrigger))
            anim.SetTrigger(dashAnimatorTrigger);

        if (dashEffectPrefab != null)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Instantiate(dashEffectPrefab, transform.position, Quaternion.Euler(0f, 0f, angle));
        }
    }
}