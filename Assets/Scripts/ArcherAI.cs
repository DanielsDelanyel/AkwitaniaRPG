using UnityEngine;

// BANDYTA LUCZNIK - przeciwnik dystansowy.
//
// Cykl walki:
//   Czeka -> (gracz w AggroRange) -> Ustawia sie na dystansie -> Celuje -> STRZAL
//         -> Przeladowanie (krazy bokiem) -> znowu Celuje
//
//   Gracz podszedl za blisko -> PANIKA: machniecie sztyletem -> Odskok
//         -> powrot do strzelania
//
// Kluczowa rzecz: lucznik AKTYWNIE utrzymuje dystans. Bez tego gracz po prostu
// podchodzi i przeciwnik staje sie workiem treningowym.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Creature))]
public class ArcherAI : MonoBehaviour
{
    private enum State
    {
        Idle, Reposition, Aim, Shoot, Reload,
        PanicWindUp, PanicSwing, PanicRetreat, Returning
    }

    [Header("Luk i Strzaly")]
    [Tooltip("Luk jako ItemData - stad bonus do obrazen i dzwiek cieciwy.")]
    public ItemData bow;

    [Tooltip("Prefab strzaly z komponentem EnemyProjectile.")]
    public GameObject arrowPrefab;

    [Tooltip("Skad wylatuje strzala. Zostaw puste - uzyjemy srodka lucznika.")]
    public Transform firePoint;

    public float arrowSpeed = 11f;

    [Tooltip("Mnoznik obrazen strzalu. 1 = Zrecznosc + bonus z luku.")]
    public float rangedDamageMultiplier = 1f;

    [Header("Bron Podreczna (na zwarcie)")]
    [Tooltip("Sztylet lub krotki miecz. Zostaw puste, jesli lucznik ma tylko uciekac.")]
    public ItemData meleeWeapon;

    [Tooltip("Prefab ciecia z komponentem EnemyMeleeAttack.")]
    public GameObject slashPrefab;

    public float meleeDamageMultiplier = 0.8f;
    public float meleeKnockback = 2f;

    [Header("Wykrywanie Gracza")]
    public float aggroRange = 11f;
    public float deaggroRange = 16f;

    [Header("Dystans Strzelania")]
    [Tooltip("Blizej niz to - lucznik sie cofa.")]
    public float preferredRangeMin = 5f;

    [Tooltip("Dalej niz to - lucznik podchodzi.")]
    public float preferredRangeMax = 8f;

    [Tooltip("Gracz blizej niz to - PANIKA, lucznik siega po sztylet.")]
    public float panicRange = 2f;

    [Header("Ruch")]
    public float moveSpeed = 3f;
    [Tooltip("Cofanie sie jest wolniejsze niz podchodzenie - inaczej lucznik " +
             "bylby nie do zlapania.")]
    public float backpedalSpeed = 2.4f;
    public float returnSpeed = 2.5f;

    [Tooltip("Predkosc bocznego krazenia w trakcie przeladowania.")]
    public float strafeSpeed = 1.8f;

    [Header("Strzelanie")]
    [Tooltip("Zastygniecie przed strzalem - to jest okno na unik dla gracza.")]
    public float aimTime = 0.6f;

    [Tooltip("Przerwa miedzy strzalami.")]
    public float shootCooldownMin = 1.4f;
    public float shootCooldownMax = 2.2f;

    [Header("Panika (zwarcie)")]
    public float panicWindUpTime = 0.3f;
    public float panicRetreatSpeed = 8f;
    public float panicRetreatDuration = 0.35f;

    [Tooltip("Przerwa, zanim lucznik znow siegnie po sztylet.")]
    public float panicCooldown = 2.5f;

    [Header("Animator (opcjonalne)")]
    public string speedParam = "Speed";
    public string moveXParam = "MoveX";
    public string moveYParam = "MoveY";
    public string shootTrigger = "Shoot";
    public string meleeTrigger = "Attack";

    [Header("Odwracanie Grafiki")]
    public SpriteRenderer spriteRenderer;
    public bool spriteFacesRight = true;
    public float flipDeadzone = 0.05f;

    [Header("Podglad (tylko do odczytu)")]
    [SerializeField] private State currentState = State.Idle;

    private Rigidbody2D rb;
    private Animator anim;
    private Creature creature;
    private Transform player;

    private Vector3 homePosition;
    private Vector2 facingDirection = Vector2.down;

    private float stateTimer;
    private float nextShotTime;
    private float nextPanicTime;
    private float strafeSign = 1f;

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
        strafeSign = Random.value < 0.5f ? -1f : 1f;

        ValidateRanges();
        FindPlayer();
        SetState(State.Idle);
    }

    // Zle ustawione zakresy potrafia zablokowac lucznika w miejscu
    private void ValidateRanges()
    {
        if (preferredRangeMin >= preferredRangeMax)
        {
            Debug.LogWarning($"{name}: Preferred Range Min musi byc MNIEJSZY niz Max. Poprawiam.");
            preferredRangeMax = preferredRangeMin + 2f;
        }

        if (panicRange >= preferredRangeMin)
        {
            Debug.LogWarning($"{name}: Panic Range powinien byc mniejszy niz Preferred Range Min, " +
                             "inaczej lucznik bedzie panikowal bez przerwy. Poprawiam.");
            panicRange = preferredRangeMin * 0.5f;
        }
    }

    private void FindPlayer()
    {
        if (PlayerStats.instance != null) { player = PlayerStats.instance.transform; return; }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (creature.IsDead) return;
        if (player == null) { FindPlayer(); return; }

        stateTimer -= Time.deltaTime;

        float distance = Vector2.Distance(transform.position, player.position);

        // PANIKA ma pierwszenstwo przed wszystkim poza samym cyklem paniki
        if (IsCombatState() && distance <= panicRange && Time.time >= nextPanicTime)
        {
            SetState(meleeWeapon != null ? State.PanicWindUp : State.PanicRetreat);
            return;
        }

        switch (currentState)
        {
            case State.Idle:
                if (distance <= aggroRange) SetState(State.Reposition);
                break;

            case State.Reposition:
                LookAt(player.position);

                if (distance > deaggroRange) { SetState(State.Returning); break; }

                // Jestesmy w dobrym pasie i strzal sie odnowil - celujemy
                if (IsInFiringBand(distance) && Time.time >= nextShotTime) SetState(State.Aim);
                break;

            case State.Aim:
                LookAt(player.position);

                // Gracz uciekl w trakcie celowania - poprawiamy pozycje
                if (distance > preferredRangeMax * 1.3f) { SetState(State.Reposition); break; }

                if (stateTimer <= 0f) SetState(State.Shoot);
                break;

            case State.Shoot:
                // Stan trwa jedna klatke - strzala powstaje przy wejsciu w stan
                SetState(State.Reload);
                break;

            case State.Reload:
                LookAt(player.position);

                if (distance > deaggroRange) { SetState(State.Returning); break; }
                if (stateTimer <= 0f) SetState(State.Reposition);
                break;

            case State.PanicWindUp:
                LookAt(player.position);
                if (stateTimer <= 0f) SetState(State.PanicSwing);
                break;

            case State.PanicSwing:
                if (stateTimer <= 0f) SetState(State.PanicRetreat);
                break;

            case State.PanicRetreat:
                if (stateTimer <= 0f) SetState(State.Reposition);
                break;

            case State.Returning:
                if (distance <= aggroRange) { SetState(State.Reposition); break; }
                if (Vector2.Distance(transform.position, homePosition) < 0.3f) SetState(State.Idle);
                break;
        }

        UpdateAnimator();
        UpdateSpriteFlip();
    }

    private bool IsCombatState()
    {
        return currentState == State.Reposition
            || currentState == State.Aim
            || currentState == State.Reload;
    }

    private bool IsInFiringBand(float distance)
    {
        return distance >= preferredRangeMin && distance <= preferredRangeMax;
    }

    void FixedUpdate()
    {
        if (creature.IsDead || player == null) return;

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float distance = toPlayer.magnitude;
        Vector2 dirToPlayer = distance > 0.001f ? toPlayer / distance : Vector2.right;

        switch (currentState)
        {
            case State.Reposition:
                MoveToFiringBand(dirToPlayer, distance);
                break;

            case State.Reload:
                // W trakcie przeladowania krazy bokiem - trudniej go trafic
                StrafeSideways(dirToPlayer, distance);
                break;

            case State.PanicRetreat:
                rb.MovePosition(rb.position - dirToPlayer * panicRetreatSpeed * Time.fixedDeltaTime);
                break;

            case State.Returning:
                Vector2 toHome = ((Vector2)homePosition - rb.position).normalized;
                rb.MovePosition(rb.position + toHome * returnSpeed * Time.fixedDeltaTime);
                break;

                // Idle, Aim, Shoot, PanicWindUp, PanicSwing = stoi w miejscu
        }
    }

    // Podchodzi albo sie cofa, zeby trafic w pas strzelania
    private void MoveToFiringBand(Vector2 dirToPlayer, float distance)
    {
        if (distance > preferredRangeMax)
        {
            // Za daleko - podchodzimy
            rb.MovePosition(rb.position + dirToPlayer * moveSpeed * Time.fixedDeltaTime);
        }
        else if (distance < preferredRangeMin)
        {
            // Za blisko - cofamy sie, ale wolniej niz podchodzimy
            rb.MovePosition(rb.position - dirToPlayer * backpedalSpeed * Time.fixedDeltaTime);
        }
    }

    // Ruch bokiem wokol gracza, z korekta dystansu
    private void StrafeSideways(Vector2 dirToPlayer, float distance)
    {
        Vector2 tangent = new Vector2(-dirToPlayer.y, dirToPlayer.x) * strafeSign;

        float middle = (preferredRangeMin + preferredRangeMax) * 0.5f;
        float error = distance - middle;
        Vector2 radial = dirToPlayer * Mathf.Clamp(error, -1f, 1f);

        Vector2 move = (tangent + radial * 0.7f).normalized;
        rb.MovePosition(rb.position + move * strafeSpeed * Time.fixedDeltaTime);
    }

    private void SetState(State newState)
    {
        currentState = newState;

        switch (newState)
        {
            case State.Idle:
            case State.Reposition:
            case State.Returning:
                stateTimer = 0f;
                break;

            case State.Aim:
                stateTimer = aimTime;
                break;

            case State.Shoot:
                FireArrow();
                stateTimer = 0f;
                break;

            case State.Reload:
                stateTimer = Random.Range(shootCooldownMin, shootCooldownMax);
                nextShotTime = Time.time + stateTimer;

                // Czasem zmienia strone krazenia, zeby nie byl przewidywalny
                if (Random.value < 0.4f) strafeSign = -strafeSign;
                break;

            case State.PanicWindUp:
                stateTimer = panicWindUpTime;
                break;

            case State.PanicSwing:
                PerformPanicSwing();
                stateTimer = CalculateMeleeSwingDuration();
                break;

            case State.PanicRetreat:
                stateTimer = panicRetreatDuration;
                nextPanicTime = Time.time + panicCooldown;

                // Po odskoku lucznik potrzebuje chwili, zanim znow wypusci strzale
                nextShotTime = Mathf.Max(nextShotTime, Time.time + 0.5f);
                break;
        }
    }

    // ===============================================================
    // STRZAL
    // ===============================================================
    private void FireArrow()
    {
        if (anim != null && HasParam(shootTrigger)) anim.SetTrigger(shootTrigger);

        if (arrowPrefab == null)
        {
            Debug.LogWarning($"{name}: brak Arrow Prefab - lucznik strzela powietrzem!");
            return;
        }

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector2 direction = ((Vector2)player.position - (Vector2)origin).normalized;

        GameObject arrow = Instantiate(arrowPrefab, origin, Quaternion.identity);

        EnemyProjectile projectile = arrow.GetComponent<EnemyProjectile>();
        if (projectile == null)
        {
            Debug.LogError($"{name}: Arrow Prefab nie ma komponentu EnemyProjectile!");
            return;
        }

        // Obrazenia nadaje STRZELEC - dzieki temu jeden prefab strzaly
        // obsluzy i slabego rekruta, i mistrza lucznictwa.
        projectile.Setup(direction, CalculateArrowDamage(), arrowSpeed);

        if (bow != null) SoundManager.Play(bow.swingSounds, bow.soundVolume);
    }

    // Lucznik czerpie sile strzalu ze ZRECZNOSCI, nie z sily miesni
    public int CalculateArrowDamage()
    {
        int dexterity = creature != null ? creature.baseZR : 0;
        int bowBonus = bow != null ? bow.GetDamageBonus() : 0;

        return Mathf.Max(1, Mathf.RoundToInt((dexterity + bowBonus) * rangedDamageMultiplier));
    }

    // ===============================================================
    // PANIKA - sztylet w zwarciu
    // ===============================================================
    private void PerformPanicSwing()
    {
        if (anim != null && HasParam(meleeTrigger)) anim.SetTrigger(meleeTrigger);

        if (slashPrefab == null || meleeWeapon == null) return;

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector2 toPlayer = (Vector2)player.position - (Vector2)origin;
        float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        GameObject slash = Instantiate(slashPrefab, origin, Quaternion.identity);
        slash.transform.SetParent(transform);

        EnemyMeleeAttack attack = slash.GetComponent<EnemyMeleeAttack>();
        if (attack == null)
        {
            Debug.LogError($"{name}: Slash Prefab nie ma komponentu EnemyMeleeAttack!");
            return;
        }

        attack.knockbackForce = meleeKnockback;
        attack.Setup(meleeWeapon, CalculateMeleeDamage(), CalculateMeleeSwingDuration(), angle);
    }

    public int CalculateMeleeDamage()
    {
        int strength = creature != null ? creature.baseSTR : 0;
        int weaponBonus = meleeWeapon != null ? meleeWeapon.GetDamageBonus() : 0;

        return Mathf.Max(1, Mathf.RoundToInt((strength + weaponBonus) * meleeDamageMultiplier));
    }

    public float CalculateMeleeSwingDuration()
    {
        if (meleeWeapon == null) return 0.3f;

        int strength = creature != null ? creature.baseSTR : 1;
        int dexterity = creature != null ? creature.baseZR : 1;

        float denominator = Mathf.Max(5f, (strength + dexterity) * 0.25f);
        return Mathf.Clamp(meleeWeapon.weight / denominator, 0.2f, 1.2f);
    }

    // ===============================================================
    // POMOCNICZE
    // ===============================================================
    private void LookAt(Vector3 target)
    {
        Vector2 dir = (Vector2)target - (Vector2)transform.position;
        if (dir.magnitude > 0.1f) facingDirection = dir.normalized;
    }

    private void CacheAnimatorParams()
    {
        animParams.Clear();
        if (anim == null || anim.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter p in anim.parameters) animParams.Add(p.name);
    }

    private bool HasParam(string name)
    {
        return !string.IsNullOrEmpty(name) && animParams.Contains(name);
    }

    private void UpdateAnimator()
    {
        if (anim == null) return;

        float speedValue = 0f;
        if (currentState == State.Reposition || currentState == State.Reload
            || currentState == State.Returning) speedValue = 1f;
        if (currentState == State.PanicRetreat) speedValue = 2f;

        if (HasParam(speedParam)) anim.SetFloat(speedParam, speedValue);
        if (HasParam(moveXParam)) anim.SetFloat(moveXParam, facingDirection.x);
        if (HasParam(moveYParam)) anim.SetFloat(moveYParam, facingDirection.y);
    }

    private void UpdateSpriteFlip()
    {
        if (spriteRenderer == null) return;
        if (Mathf.Abs(facingDirection.x) < flipDeadzone) return;

        bool lookingRight = facingDirection.x > 0f;
        spriteRenderer.flipX = spriteFacesRight ? !lookingRight : lookingRight;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, deaggroRange);

        // Pas strzelania - miedzy tymi okregami lucznik stoi i strzela
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, preferredRangeMin);
        Gizmos.DrawWireSphere(transform.position, preferredRangeMax);

        // Strefa paniki
        Gizmos.color = new Color(1f, 0.2f, 0.6f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, panicRange);
    }
}