using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Creature))]
public class CreatureAI : MonoBehaviour
{
    [Header("Ruch i Wedrowanie")]
    public float walkSpeed = 2f;
    public float minWanderTime = 1f;
    public float maxWanderTime = 3f;
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;

    [Header("Walka (Szarza)")]
    public float runSpeed = 6f;       // Predkosc biegu
    public float prepTime = 0.5f;     // Czas "ladowania" ataku
    public int attackDamage = 15;     // Obrazenia z szarzy

    // ===============================================================
    // NOWE: AGRO I GUBIENIE AGRO
    // Dzik, ktory sie rozwscieczyl, zglasza sie do CombatTracker (blokuje
    // wchodzenie do budynkow, tak jak BanditAI/ArcherAI). Gdy gracz ucieknie
    // dalej niz deaggroRange i utrzyma ten dystans przez loseAggroTime sekund,
    // dzik daje sobie spokoj: wypisuje sie z CombatTracker i - jesli z natury
    // byl Neutral/Peaceful - wraca do swojego pierwotnego nastawienia
    // (mozna go potem sprowokowac ponownie).
    // ===============================================================
    [Header("Agro")]
    [Tooltip("Tylko dla stworzen Z NATURY agresywnych: z jakiej odleglosci zauwazaja " +
             "gracza i zaczynaja atak. Sprowokowany (uderzony) Neutral atakuje zawsze, " +
             "bez wzgledu na odleglosc.")]
    public float aggroRange = 6f;

    [Tooltip("Gdy gracz jest dalej niz tyle, dzik zaczyna 'tracic zainteresowanie'.")]
    public float deaggroRange = 12f;

    [Tooltip("Ile sekund gracz musi pozostac poza deaggroRange, zeby dzik odpuscil. " +
             "Chroni przed natychmiastowym 'wybaczeniem' np. po strzale z daleka.")]
    public float loseAggroTime = 3f;

    private Disposition originalDisposition;
    private bool isEngaged;          // Czy aktywnie walczy z graczem (zgloszony w CombatTracker)
    private float outOfRangeTimer;   // Jak dlugo gracz jest juz poza deaggroRange

    private Rigidbody2D rb;
    private Animator anim;
    private Creature creature;
    private Transform player;

    // NOWE: statusy (Slow/Stun) - opcjonalne, moze nie byc podpiete na starszych
    // prefabach. Ogluszenie zamraza cala logike ponizej, spowolnienie mnozy predkosc.
    private StatusEffectController statusEffects;

    private enum State { Idle, Wander, PrepCharge, Charging }
    private State currentState;

    private float stateTimer;
    private Vector2 moveDirection;
    private Vector2 chargeTarget;
    private Vector2 lastFacingDirection = Vector2.down; // Domyslnie patrzy w dol

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        creature = GetComponent<Creature>();
        statusEffects = GetComponent<StatusEffectController>();

        // Zapamietujemy, jakie stworzenie bylo "z natury" - do tego wraca po zgubieniu agro
        originalDisposition = creature.disposition;

        FindPlayer();

        SetIdle();
    }

    private void FindPlayer()
    {
        // Szukamy gracza na mapie (Upewnij sie, ze Twoj gracz ma Tag "Player"!)
        if (PlayerStats.instance != null) { player = PlayerStats.instance.transform; return; }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        // Jesli nie zyje, zatrzymujemy logike (i od razu konczymy "walke" -
        // przy destroyDelay > 0 obiekt jeszcze chwile istnieje)
        if (creature.IsDead || creature.currentHealth <= 0)
        {
            SetEngaged(false);
            return;
        }

        if (player == null) FindPlayer();
        if (player == null) return;

        // Agro liczymy NAWET w trakcie ogluszenia - ogluszony dzik dalej
        // "walczy" (drzwi zostaja zablokowane), ale moze tez zgubic gracza.
        UpdateAggro();

        // Ogluszony - zamrazamy CALA logike (w tym timery), zeby po ockniciu
        // sie stworzenie doladowalo dokladnie tam, gdzie je przerwano.
        if (statusEffects != null && statusEffects.IsStunned) return;

        // Jesli walczy z graczem, a sobie tylko spacerowal -> zaczyna atak!
        if (isEngaged && (currentState == State.Idle || currentState == State.Wander))
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
                // W trakcie ladowania odwraca sie w strone gracza
                UpdateFacingDirection(player.position - transform.position);
                if (stateTimer <= 0) StartCharge();
                break;
            case State.Charging:
                // Konczy szarze, gdy dobiegnie do celu LUB jesli minie czas zabezpieczajacy (np. zablokuje sie o drzewo)
                if (Vector2.Distance(transform.position, chargeTarget) < 0.2f || stateTimer <= 0)
                {
                    anim.SetTrigger("Attack"); // Animacja ataku w powietrze
                    SetIdle();
                    stateTimer = 1.5f; // Chwila oddechu przed kolejna szarza
                }
                break;
        }

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        if (statusEffects != null && statusEffects.IsStunned) return;

        float slowMul = statusEffects != null ? statusEffects.MoveSpeedMultiplier : 1f;

        if (currentState == State.Wander)
        {
            rb.MovePosition(rb.position + moveDirection * walkSpeed * slowMul * Time.fixedDeltaTime);
        }
        else if (currentState == State.Charging)
        {
            Vector2 chargeDir = (chargeTarget - rb.position).normalized;
            rb.MovePosition(rb.position + chargeDir * runSpeed * slowMul * Time.fixedDeltaTime);
        }
    }

    // ===============================================================
    // AGRO
    // ===============================================================
    private void UpdateAggro()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (!isEngaged)
        {
            if (creature.disposition != Disposition.Aggressive) return;

            // Sprowokowany (z natury nie byl agresywny) -> atakuje od razu.
            // Z natury agresywny -> dopiero gdy gracz podejdzie w aggroRange.
            bool provoked = originalDisposition != Disposition.Aggressive;
            if (provoked || distance <= aggroRange)
            {
                SetEngaged(true);
                outOfRangeTimer = 0f;
            }
            return;
        }

        // Walczy - sprawdzamy, czy gracz nie uciekl wystarczajaco daleko
        if (distance > deaggroRange)
        {
            outOfRangeTimer += Time.deltaTime;
            if (outOfRangeTimer >= loseAggroTime) LoseAggro();
        }
        else
        {
            outOfRangeTimer = 0f;
        }
    }

    private void LoseAggro()
    {
        SetEngaged(false);
        outOfRangeTimer = 0f;

        // Sprowokowany Neutral/Peaceful sie uspokaja (mozna go znow sprowokowac).
        // Z natury agresywny zostaje agresywny - zaatakuje, gdy gracz wroci w aggroRange.
        creature.disposition = originalDisposition;

        // Przerywamy ewentualna szarze/ladowanie i wracamy do spacerowania
        SetIdle();
    }

    private void SetEngaged(bool engaged)
    {
        if (isEngaged == engaged) return;
        isEngaged = engaged;

        if (engaged) CombatTracker.Register(this);
        else CombatTracker.Unregister(this);
    }

    // Siatka bezpieczenstwa: zniszczony/wylaczony dzik (zmiana sceny, SpawnZone,
    // smierc) nie moze zostac na zawsze zarejestrowany w CombatTracker.
    void OnDisable()
    {
        CombatTracker.Unregister(this);
        isEngaged = false;
    }

    void OnDestroy()
    {
        CombatTracker.Unregister(this);
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
        moveDirection = Vector2.zero; // Zatrzymuje sie w miejscu
        stateTimer = prepTime; // laduje sie 0.5s
    }

    void StartCharge()
    {
        currentState = State.Charging;
        chargeTarget = player.position; // Zapisuje OSTATNIA pozycje gracza!
        UpdateFacingDirection(chargeTarget - (Vector2)transform.position);
        stateTimer = 2f; // Zabezpieczenie: przerywa szarze po 2 sek., jesli w cos utknie
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

            // Przekazujemy stan (0 = stoi, 1 = idzie, 2 = biegnie/szarzuje)
            float speedParam = 0f;
            if (currentState == State.Wander) speedParam = 1f;
            if (currentState == State.Charging) speedParam = 2f;

            anim.SetFloat("Speed", speedParam);
        }
    }

    // Zadawanie obrazen przy fizycznym zderzeniu!
    // 1. Zadziala, jesli dzik dobiegnie do Ciebie z dystansu
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HitPlayer(collision);
    }

    // 2. Zadziala, jesli wpadniesz na dzika podczas jego szarzy, LUB jesli juz sie stykaliscie
    private void OnCollisionStay2D(Collision2D collision)
    {
        HitPlayer(collision);
    }

    // Pomocnicza funkcja z logika uderzenia, zeby nie pisac kodu dwa razy
    private void HitPlayer(Collision2D collision)
    {
        if (currentState == State.Charging && collision.gameObject.CompareTag("Player"))
        {
            // Kierunek od Dzika do Gracza
            Vector2 hitDir = (collision.transform.position - transform.position).normalized;

            // Dziki nie uderzaja krytycznie, wiec isCrit = false
            PlayerStats.instance.TakeDamage(attackDamage, false, hitDir);

            // NOWE: szarza moze dodatkowo nalozyc status na gracza - bazowo 0%,
            // podnies na tym prefabie (Creature.onHitStatus), zeby zrobic np.
            // "specjalnego" dzika, ktory czasem oglusza przy trafieniu.
            creature.onHitStatus.TryProc(PlayerStats.instance.GetComponent<StatusEffectController>());

            if (anim != null) anim.SetTrigger("Attack");
            SetIdle();
            stateTimer = 1.5f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f);   // czerwony - zauwaza gracza (tylko z natury agresywne)
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = new Color(1f, 0.8f, 0.3f);   // zolty - za tym dystansem zaczyna odpuszczac
        Gizmos.DrawWireSphere(transform.position, deaggroRange);
    }
}
