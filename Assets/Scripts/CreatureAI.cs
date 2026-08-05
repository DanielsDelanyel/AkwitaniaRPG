using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Creature))]
public class CreatureAI : MonoBehaviour
{
    [Header("Ruch i Wêdrowanie")]
    public float walkSpeed = 2f;
    public float minWanderTime = 1f;
    public float maxWanderTime = 3f;
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;

    [Header("Walka (Szar¿a)")]
    public float runSpeed = 6f;       // Prêdkoœæ biegu
    public float prepTime = 0.5f;     // Czas "³adowania" ataku
    public int attackDamage = 15;     // Obra¿enia z szar¿y

    private Rigidbody2D rb;
    private Animator anim;
    private Creature creature;
    private Transform player;

    private enum State { Idle, Wander, PrepCharge, Charging }
    private State currentState;

    private float stateTimer;
    private Vector2 moveDirection;
    private Vector2 chargeTarget;
    private Vector2 lastFacingDirection = Vector2.down; // Domyœlnie patrzy w dó³

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        creature = GetComponent<Creature>();

        // Szukamy gracza na mapie (Upewnij siê, ¿e Twój gracz ma Tag "Player"!)
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        SetIdle();
    }

    void Update()
    {
        if (creature.currentHealth <= 0) return; // Jeœli nie ¿yje, zatrzymujemy logikê

        // Jeœli dosta³ cios (sta³ siê agresywny), a sobie tylko spacerowa³ -> zaczyna atak!
        if (creature.disposition == Disposition.Aggressive &&
           (currentState == State.Idle || currentState == State.Wander))
        {
            StartPrepCharge();
        }

        stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case State.Idle:
                if (stateTimer <= 0) SetWander();
                break;
            case State.Wander:
                if (stateTimer <= 0) SetIdle();
                break;
            case State.PrepCharge:
                // W trakcie ³adowania odwraca siê w stronê gracza
                UpdateFacingDirection(player.position - transform.position);
                if (stateTimer <= 0) StartCharge();
                break;
            case State.Charging:
                // Koñczy szar¿ê, gdy dobiegnie do celu LUB jeœli minie czas zabezpieczaj¹cy (np. zablokuje siê o drzewo)
                if (Vector2.Distance(transform.position, chargeTarget) < 0.2f || stateTimer <= 0)
                {
                    anim.SetTrigger("Attack"); // Animacja ataku w powietrze
                    SetIdle();
                    stateTimer = 1.5f; // Chwila oddechu przed kolejn¹ szar¿¹
                }
                break;
        }

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        if (currentState == State.Wander)
        {
            rb.MovePosition(rb.position + moveDirection * walkSpeed * Time.fixedDeltaTime);
        }
        else if (currentState == State.Charging)
        {
            Vector2 chargeDir = (chargeTarget - rb.position).normalized;
            rb.MovePosition(rb.position + chargeDir * runSpeed * Time.fixedDeltaTime);
        }
    }

    void SetIdle()
    {
        currentState = State.Idle;
        moveDirection = Vector2.zero;
        stateTimer = Random.Range(minIdleTime, maxIdleTime);
    }

    void SetWander()
    {
        currentState = State.Wander;
        moveDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        UpdateFacingDirection(moveDirection);
        stateTimer = Random.Range(minWanderTime, maxWanderTime);
    }

    void StartPrepCharge()
    {
        currentState = State.PrepCharge;
        moveDirection = Vector2.zero; // Zatrzymuje siê w miejscu
        stateTimer = prepTime; // £aduje siê 0.5s
    }

    void StartCharge()
    {
        currentState = State.Charging;
        chargeTarget = player.position; // Zapisuje OSTATNI¥ pozycjê gracza!
        UpdateFacingDirection(chargeTarget - (Vector2)transform.position);
        stateTimer = 2f; // Zabezpieczenie: przerywa szar¿ê po 2 sek., jeœli w coœ utknie
    }

    void UpdateFacingDirection(Vector2 dir)
    {
        if (dir.magnitude > 0.1f)
        {
            lastFacingDirection = dir.normalized;
        }
    }

    void UpdateAnimator()
    {
        if (anim != null)
        {
            // Przekazujemy kierunek
            anim.SetFloat("MoveX", lastFacingDirection.x);
            anim.SetFloat("MoveY", lastFacingDirection.y);

            // Przekazujemy stan (0 = stoi, 1 = idzie, 2 = biegnie/szar¿uje)
            float speedParam = 0f;
            if (currentState == State.Wander) speedParam = 1f;
            if (currentState == State.Charging) speedParam = 2f;

            anim.SetFloat("Speed", speedParam);
        }
    }

    // Zadawanie obra¿eñ przy fizycznym zderzeniu!
    // 1. Zadzia³a, jeœli dzik dobiegnie do Ciebie z dystansu
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HitPlayer(collision);
    }

    // 2. Zadzia³a, jeœli wpadniesz na dzika podczas jego szar¿y, LUB jeœli ju¿ siê stykaliœcie
    private void OnCollisionStay2D(Collision2D collision)
    {
        HitPlayer(collision);
    }

    // Pomocnicza funkcja z logik¹ uderzenia, ¿eby nie pisaæ kodu dwa razy
    private void HitPlayer(Collision2D collision)
    {
        if (currentState == State.Charging && collision.gameObject.CompareTag("Player"))
        {
            // Kierunek od Dzika do Gracza
            Vector2 hitDir = (collision.transform.position - transform.position).normalized;

            // Dziki nie uderzaj¹ krytycznie, wiêc isCrit = false
            PlayerStats.instance.TakeDamage(attackDamage, false, hitDir);

            if (anim != null) anim.SetTrigger("Attack");
            SetIdle();
            stateTimer = 1.5f;
        }
    }   
}