using UnityEngine;

// AZTEKOWIEC - pierwszy boss.
//
// Cykl walki:
//   Spi -> (gracz wchodzi w AggroRange) -> Podbiega -> Nawijka -> FALA -> Odpoczynek
//                                              \-> od czasu do czasu DASH
//   Gracz ucieka poza DeaggroRange -> Powrot na pozycje startowa -> Spi
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Creature))]
public class BossController : MonoBehaviour
{
    private enum State { Sleeping, Approach, Dash, WindUp, Attack, Recover, Returning }

    [Header("Wykrywanie Gracza")]
    [Tooltip("Z tej odleglosci boss budzi sie i rusza do walki.")]
    public float aggroRange = 7f;
    [Tooltip("Gdy gracz oddali sie bardziej, boss przerywa walke i wraca na miejsce.")]
    public float deaggroRange = 12f;

    [Header("Ruch")]
    public float moveSpeed = 4.5f;
    public float returnSpeed = 3f;
    [Tooltip("Na tej odleglosci boss przestaje podchodzic i zaczyna atak.")]
    public float attackRange = 2.2f;
    [Tooltip("Blizej niz to - boss sie cofa, zeby nie wlazic graczowi w plecy.")]
    public float tooCloseRange = 1.0f;

    [Header("Dash")]
    public bool dashEnabled = true;
    public float dashSpeed = 16f;
    public float dashDistance = 4f;
    [Tooltip("Minimalna przerwa miedzy skokami.")]
    public float dashCooldownMin = 3f;
    public float dashCooldownMax = 6f;
    [Tooltip("Krotkie zastygniecie przed skokiem - gracz ma szanse zareagowac.")]
    public float dashTelegraphTime = 0.25f;
    [Tooltip("Boss dashuje tylko, gdy gracz jest dalej niz to.")]
    public float dashMinDistance = 3f;

    [Header("Atak: Fala Uderzeniowa")]
    public GameObject shockwavePrefab;
    [Tooltip("Zamach przed uderzeniem - czas na ucieczke.")]
    public float windUpTime = 0.55f;
    [Tooltip("Przerwa po ataku, zanim boss zrobi cokolwiek innego.")]
    public float recoverTime = 1.1f;
    public float attackCooldown = 2.5f;
    [Tooltip("Przesuniecie fali w PIONIE, np. do stop bossa. " +
             "Celowo nie ma tu osi X - przesuniecie w bok wyrwaloby os obrotu " +
             "spod Aztekowca i sierp orbitowalby wokol pustego miejsca.")]
    public float shockwaveHeightOffset = -0.2f;

    [Header("Animator (opcjonalne - zostaw puste, jesli nie masz)")]
    public string speedParam = "Speed";
    public string moveXParam = "MoveX";
    public string moveYParam = "MoveY";
    public string attackTrigger = "Attack";
    public string dashTrigger = "";

    // ===============================================================
    // ODWRACANIE GRAFIKI
    // Uzywamy flipX na SpriteRenderze, a NIE ujemnej skali obiektu.
    // Ujemna skala odwrocilaby tez dzieci: collidery, pasek zycia,
    // punkt tworzenia fali - i wszystko wyladowaloby po zlej stronie.
    // ===============================================================
    [Header("Odwracanie Grafiki")]
    [Tooltip("Zostaw puste - skrypt znajdzie SpriteRenderer sam.")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("Zaznacz, jesli posta w pliku graficznym patrzy w PRAWO. " +
             "Odznacz, jesli patrzy w LEWO. Jesli boss odwraca sie odwrotnie - zmien to pole.")]
    public bool spriteFacesRight = false;

    [Tooltip("Martwa strefa. Gdy gracz stoi dokladnie nad lub pod bossem, " +
             "drobne ruchy nie beda powodowaly migotania grafiki.")]
    public float flipDeadzone = 0.05f;

    [Header("Efekty (opcjonalne)")]
    public GameObject dashEffectPrefab;
    public GameObject windUpEffectPrefab;

    [Header("Podglad (tylko do odczytu)")]
    [SerializeField] private State currentState = State.Sleeping;

    private Rigidbody2D rb;
    private Animator anim;
    private Creature creature;
    private Transform player;

    private Vector3 homePosition;
    private Vector2 facingDirection = Vector2.down;

    private float stateTimer;
    private float nextDashTime;
    private float nextAttackTime;

    private Vector2 dashDirection;
    private float dashTimeLeft;

    // Nazwy parametrow, ktore Animator FAKTYCZNIE posiada.
    // Bez tego kazda klatka wypluwala blad "Parameter 'Speed' does not exist".
    private readonly System.Collections.Generic.HashSet<string> animParams
        = new System.Collections.Generic.HashSet<string>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        creature = GetComponent<Creature>();

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        CacheAnimatorParams();

        homePosition = transform.position;

        FindPlayer();
        SetState(State.Sleeping);
    }

    private void FindPlayer()
    {
        if (PlayerStats.instance != null)
        {
            player = PlayerStats.instance.transform;
            return;
        }
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (creature.IsDead) return;
        if (player == null) { FindPlayer(); return; }

        stateTimer -= Time.deltaTime;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Sleeping:
                if (distanceToPlayer <= aggroRange) SetState(State.Approach);
                break;

            case State.Approach:
                LookAt(player.position);

                // Gracz uciekl za daleko - koniec walki
                if (distanceToPlayer > deaggroRange) { SetState(State.Returning); break; }

                // Dosc blisko - zamach!
                if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
                {
                    SetState(State.WindUp);
                    break;
                }

                // Za daleko na cios, ale pora na skok
                if (dashEnabled && Time.time >= nextDashTime && distanceToPlayer >= dashMinDistance)
                {
                    SetState(State.Dash);
                    break;
                }
                break;

            case State.Dash:
                // Faza telegrafu: boss stoi i celuje
                if (stateTimer > 0f)
                {
                    LookAt(player.position);
                    break;
                }

                // Faza lotu
                dashTimeLeft -= Time.deltaTime;
                if (dashTimeLeft <= 0f)
                {
                    nextDashTime = Time.time + Random.Range(dashCooldownMin, dashCooldownMax);
                    SetState(State.Approach);
                }
                break;

            case State.WindUp:
                LookAt(player.position);
                if (stateTimer <= 0f) SetState(State.Attack);
                break;

            case State.Attack:
                // Stan trwa jedna klatke - fala powstaje przy wejsciu w stan
                SetState(State.Recover);
                break;

            case State.Recover:
                LookAt(player.position); // nie spuszcza gracza z oczu
                if (stateTimer <= 0f) SetState(State.Approach);
                break;

            case State.Returning:
                // Gracz wrocil w poblize - z powrotem do walki
                if (distanceToPlayer <= aggroRange) { SetState(State.Approach); break; }

                if (Vector2.Distance(transform.position, homePosition) < 0.3f) SetState(State.Sleeping);
                break;
        }

        UpdateAnimator();
        UpdateSpriteFlip();
    }

    // Obraca grafike w strone, w ktora boss aktualnie patrzy
    private void CacheAnimatorParams()
    {
        animParams.Clear();
        if (anim == null || anim.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter p in anim.parameters) animParams.Add(p.name);
    }

    // Ustawia parametr TYLKO, jesli Animator go zna
    private bool HasParam(string name)
    {
        return !string.IsNullOrEmpty(name) && animParams.Contains(name);
    }

    private void UpdateSpriteFlip()
    {
        if (spriteRenderer == null) return;

        // Gdy gracz stoi niemal dokladnie nad lub pod bossem, skladowa X
        // skacze wokol zera - bez tej strefy grafika by migotala.
        if (Mathf.Abs(facingDirection.x) < flipDeadzone) return;

        bool lookingRight = facingDirection.x > 0f;
        spriteRenderer.flipX = spriteFacesRight ? !lookingRight : lookingRight;
    }

    void FixedUpdate()
    {
        if (creature.IsDead) return;
        if (player == null) return;

        switch (currentState)
        {
            case State.Approach:
                MoveTowardsPlayer();
                break;

            case State.Dash:
                if (stateTimer <= 0f && dashTimeLeft > 0f)
                    rb.MovePosition(rb.position + dashDirection * dashSpeed * Time.fixedDeltaTime);
                break;

            case State.Returning:
                Vector2 toHome = ((Vector2)homePosition - rb.position).normalized;
                rb.MovePosition(rb.position + toHome * returnSpeed * Time.fixedDeltaTime);
                break;

                // WindUp, Attack, Recover, Sleeping = boss stoi w miejscu
        }
    }

    private void MoveTowardsPlayer()
    {
        float distance = Vector2.Distance(rb.position, player.position);
        Vector2 toPlayer = ((Vector2)player.position - rb.position).normalized;

        if (distance > attackRange)
        {
            rb.MovePosition(rb.position + toPlayer * moveSpeed * Time.fixedDeltaTime);
        }
        else if (distance < tooCloseRange)
        {
            // Za blisko - odsuwa sie, zeby fala miala sens
            rb.MovePosition(rb.position - toPlayer * moveSpeed * 0.6f * Time.fixedDeltaTime);
        }
    }

    private void SetState(State newState)
    {
        currentState = newState;

        switch (newState)
        {
            case State.Sleeping:
                stateTimer = 0f;
                break;

            case State.Approach:
                stateTimer = 0f;
                break;

            case State.Dash:
                dashDirection = ((Vector2)player.position - rb.position).normalized;
                facingDirection = dashDirection;
                dashTimeLeft = dashDistance / Mathf.Max(0.1f, dashSpeed);
                stateTimer = dashTelegraphTime; // najpierw telegraf, potem lot

                if (anim != null && HasParam(dashTrigger)) anim.SetTrigger(dashTrigger);
                if (dashEffectPrefab != null) Instantiate(dashEffectPrefab, transform.position, Quaternion.identity);
                break;

            case State.WindUp:
                stateTimer = windUpTime;
                if (windUpEffectPrefab != null)
                    Instantiate(windUpEffectPrefab, transform.position, Quaternion.identity, transform);
                break;

            case State.Attack:
                FireShockwave();
                stateTimer = 0f;
                break;

            case State.Recover:
                stateTimer = recoverTime;
                nextAttackTime = Time.time + attackCooldown;
                break;

            case State.Returning:
                stateTimer = 0f;
                break;
        }
    }

    private void FireShockwave()
    {
        if (anim != null && HasParam(attackTrigger)) anim.SetTrigger(attackTrigger);

        if (shockwavePrefab == null)
        {
            Debug.LogWarning($"{name}: brak przypisanego Shockwave Prefab - atak nic nie zrobi!");
            return;
        }

        // KIERUNEK ATAKU: prosto w gracza, a jesli go nie ma - tam, gdzie boss patrzy
        Vector2 attackDir = facingDirection;
        if (player != null)
        {
            Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
            if (toPlayer.sqrMagnitude > 0.0001f) attackDir = toPlayer.normalized;
        }

        // WAZNE: fala powstaje DOKLADNIE na bossie, bo to on jest osia obrotu.
        // Odsuniecie sierpa zalatwia dziecko 'Graphics' wewnatrz prefabu.
        Vector3 offset = new Vector3(0f, shockwaveHeightOffset, 0f);

        GameObject waveObj = Instantiate(
            shockwavePrefab,
            transform.position + offset,
            Quaternion.identity);

        // TU BYL BRAK: fala nigdy nie dostawala kierunku, wiec sierp
        // zawsze wisial po tej samej stronie niezaleznie od pozycji gracza.
        Shockwave wave = waveObj.GetComponent<Shockwave>();
        if (wave != null) wave.Setup(attackDir);
        else Debug.LogWarning($"{name}: prefab fali nie ma komponentu Shockwave!");
    }

    private void LookAt(Vector3 target)
    {
        Vector2 dir = ((Vector2)target - (Vector2)transform.position);
        if (dir.magnitude > 0.1f) facingDirection = dir.normalized;
    }

    private void UpdateAnimator()
    {
        if (anim == null) return;

        float speedValue = 0f;
        if (currentState == State.Approach || currentState == State.Returning) speedValue = 1f;
        if (currentState == State.Dash && stateTimer <= 0f) speedValue = 2f;

        if (HasParam(speedParam)) anim.SetFloat(speedParam, speedValue);
        if (HasParam(moveXParam)) anim.SetFloat(moveXParam, facingDirection.x);
        if (HasParam(moveYParam)) anim.SetFloat(moveYParam, facingDirection.y);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);   // agresja
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);   // rezygnacja
        Gizmos.DrawWireSphere(transform.position, deaggroRange);

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);   // zasieg ataku
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}