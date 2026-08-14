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
    public Vector2 shockwaveOffset = new Vector2(0f, -0.2f);

    [Header("Animator (opcjonalne - zostaw puste, jesli nie masz)")]
    public string speedParam = "Speed";
    public string moveXParam = "MoveX";
    public string moveYParam = "MoveY";
    public string attackTrigger = "Attack";
    public string dashTrigger = "";

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        creature = GetComponent<Creature>();

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
                if (stateTimer <= 0f) SetState(State.Approach);
                break;

            case State.Returning:
                // Gracz wrocil w poblize - z powrotem do walki
                if (distanceToPlayer <= aggroRange) { SetState(State.Approach); break; }

                if (Vector2.Distance(transform.position, homePosition) < 0.3f) SetState(State.Sleeping);
                break;
        }

        UpdateAnimator();
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

                if (anim != null && !string.IsNullOrEmpty(dashTrigger)) anim.SetTrigger(dashTrigger);
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
        if (anim != null && !string.IsNullOrEmpty(attackTrigger)) anim.SetTrigger(attackTrigger);

        if (shockwavePrefab == null)
        {
            Debug.LogWarning($"{name}: brak przypisanego Shockwave Prefab - atak nic nie zrobi!");
            return;
        }

        Instantiate(shockwavePrefab, transform.position + (Vector3)shockwaveOffset, Quaternion.identity);
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

        if (!string.IsNullOrEmpty(speedParam)) anim.SetFloat(speedParam, speedValue);
        if (!string.IsNullOrEmpty(moveXParam)) anim.SetFloat(moveXParam, facingDirection.x);
        if (!string.IsNullOrEmpty(moveYParam)) anim.SetFloat(moveYParam, facingDirection.y);
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
