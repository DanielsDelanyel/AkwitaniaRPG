using UnityEngine;

// BANDYTA - przeciwnik walczacy wrecz.
//
// Cykl walki:
//   Czeka -> (gracz w AggroRange) -> Podbiega -> Zamach -> CIECIE -> CZEKA
//         -> Odskok do tylu -> Krazenie wokol gracza -> znowu Podbiega
//
// Bron to zwykly ItemData. Dwoch bandytow z roznymi mieczami bedzie mialo
// inne obrazenia, zasieg, szybkosc ciosu i dzwieki - bez zmian w kodzie.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Creature))]
public class BanditAI : MonoBehaviour
{
    private enum State { Idle, Chase, WindUp, Attack, WaitAfterAttack, Retreat, Circle, Returning }

    [Header("Uzbrojenie")]
    [Tooltip("Miecz bandyty. Stad biora sie obrazenia, zasieg ciosu, luk i dzwieki.")]
    public ItemData weapon;

    [Tooltip("Prefab ciecia z komponentem EnemyMeleeAttack.")]
    public GameObject slashPrefab;

    [Tooltip("Punkt, z ktorego wychodzi cios. Zostaw puste - uzyjemy srodka bandyty.")]
    public Transform attackOrigin;

    [Tooltip("Opcjonalnie: SpriteRenderer pokazujacy bron w rece bandyty.")]
    public SpriteRenderer weaponRenderer;

    [Header("Obrazenia")]
    [Tooltip("Mnoznik obrazen bandyty. 1 = tyle, ile daje Sila + bron.")]
    public float damageMultiplier = 1f;

    [Tooltip("Odrzut gracza po trafieniu. 0 = brak.")]
    public float knockbackForce = 2f;

    [Header("Wykrywanie Gracza")]
    public float aggroRange = 8f;
    [Tooltip("Gdy gracz oddali sie bardziej, bandyta rezygnuje i wraca na miejsce.")]
    public float deaggroRange = 14f;

    [Header("Ruch")]
    public float chaseSpeed = 3.2f;
    public float returnSpeed = 2.5f;
    [Tooltip("Z tej odleglosci bandyta zaczyna zamach. Dobierz do dlugosci miecza.")]
    public float attackRange = 1.4f;

    [Header("Zamach")]
    [Tooltip("Zastygniecie przed ciosem - to jest okno na unik dla gracza.")]
    public float windUpTime = 0.45f;

    [Tooltip("Przerwa miedzy kolejnymi ciosami.")]
    public float attackCooldown = 1.8f;

    [Header("Odskok po ciosie")]
    [Tooltip("Jaka czesc ciecia bandyta stoi nieruchomo. 1 = czeka, az ostrze " +
             "przejdzie caly luk. Dzieki temu ciezki topor trzyma go dluzej niz sztylet.")]
    [Range(0f, 1f)] public float swingFollowThrough = 1f;

    [Tooltip("DODATKOWA chwila bezruchu po zakonczeniu ciecia, zanim bandyta odskoczy.")]
    public float postAttackDelay = 0.25f;
    public float retreatSpeed = 7f;
    public float retreatDuration = 0.3f;

    [Header("Krazenie")]
    [Tooltip("Ile sekund bandyta krazy wokol gracza, zanim znow zaatakuje.")]
    public float circleTimeMin = 0.8f;
    public float circleTimeMax = 2f;
    public float circleSpeed = 2.6f;

    [Tooltip("Na jakim dystansie krazy. Powinien byc wiekszy niz Attack Range.")]
    public float circleDistance = 2.8f;

    [Tooltip("Szansa na zmiane kierunku krazenia przy kazdym podejsciu.")]
    [Range(0f, 1f)] public float circleDirectionChangeChance = 0.5f;

    [Header("Animator (opcjonalne)")]
    public string speedParam = "Speed";
    public string moveXParam = "MoveX";
    public string moveYParam = "MoveY";
    public string attackTrigger = "Attack";

    [Header("Odwracanie Grafiki")]
    [Tooltip("Zostaw puste - skrypt znajdzie SpriteRenderer sam.")]
    public SpriteRenderer spriteRenderer;
    [Tooltip("Zaznacz, jesli postac w pliku graficznym patrzy w PRAWO.")]
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
    private float nextAttackTime;
    private float circleSign = 1f;   // 1 = w prawo wokol gracza, -1 = w lewo

    private readonly System.Collections.Generic.HashSet<string> animParams
        = new System.Collections.Generic.HashSet<string>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        creature = GetComponent<Creature>();

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        CacheAnimatorParams();
        ShowWeaponInHand();

        homePosition = transform.position;
        circleSign = Random.value < 0.5f ? -1f : 1f;

        FindPlayer();
        SetState(State.Idle);
    }

    // Pokazuje miecz w rece, jesli podpiales osobny SpriteRenderer
    private void ShowWeaponInHand()
    {
        if (weaponRenderer == null) return;

        if (weapon != null && weapon.icon != null)
        {
            weaponRenderer.sprite = weapon.icon;
            weaponRenderer.enabled = true;
        }
        else weaponRenderer.enabled = false;
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

        switch (currentState)
        {
            case State.Idle:
                if (distance <= aggroRange) SetState(State.Chase);
                break;

            case State.Chase:
                LookAt(player.position);

                if (distance > deaggroRange) { SetState(State.Returning); break; }

                // Dosc blisko i cios sie odnowil - zamach!
                if (distance <= attackRange && Time.time >= nextAttackTime)
                {
                    SetState(State.WindUp);
                }
                break;

            case State.WindUp:
                // Bandyta stoi i celuje - gracz ma czas uskoczyc
                LookAt(player.position);
                if (stateTimer <= 0f) SetState(State.Attack);
                break;

            case State.Attack:
                // Stan trwa jedna klatke - ciecie powstaje przy wejsciu w stan
                SetState(State.WaitAfterAttack);
                break;

            case State.WaitAfterAttack:
                // Bandyta po ataku odczekuje chwile przed odskokiem
                LookAt(player.position);
                if (stateTimer <= 0f) SetState(State.Retreat);
                break;

            case State.Retreat:
                if (stateTimer <= 0f) SetState(State.Circle);
                break;

            case State.Circle:
                LookAt(player.position);

                if (distance > deaggroRange) { SetState(State.Returning); break; }
                if (stateTimer <= 0f) SetState(State.Chase);
                break;

            case State.Returning:
                if (distance <= aggroRange) { SetState(State.Chase); break; }
                if (Vector2.Distance(transform.position, homePosition) < 0.3f) SetState(State.Idle);
                break;
        }

        UpdateAnimator();
        UpdateSpriteFlip();
    }

    void FixedUpdate()
    {
        if (creature.IsDead || player == null) return;

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float distance = toPlayer.magnitude;
        Vector2 dirToPlayer = distance > 0.001f ? toPlayer / distance : Vector2.right;

        switch (currentState)
        {
            case State.Chase:
                // Podbiega, ale nie wchodzi graczowi w plecy
                if (distance > attackRange * 0.9f)
                    rb.MovePosition(rb.position + dirToPlayer * chaseSpeed * Time.fixedDeltaTime);
                break;

            case State.Retreat:
                // Skok DO TYLU - to jest ta chwila oddechu dla gracza
                rb.MovePosition(rb.position - dirToPlayer * retreatSpeed * Time.fixedDeltaTime);
                break;

            case State.Circle:
                MoveInCircle(dirToPlayer, distance);
                break;

            case State.Returning:
                Vector2 toHome = ((Vector2)homePosition - rb.position).normalized;
                rb.MovePosition(rb.position + toHome * returnSpeed * Time.fixedDeltaTime);
                break;

                // Idle, WindUp, Attack, WaitAfterAttack = bandyta stoi w miejscu
        }
    }

    // Krazy wokol gracza, jednoczesnie korygujac dystans
    private void MoveInCircle(Vector2 dirToPlayer, float distance)
    {
        // Wektor prostopadly do kierunku na gracza = ruch po okregu
        Vector2 tangent = new Vector2(-dirToPlayer.y, dirToPlayer.x) * circleSign;

        // Korekta dystansu: za blisko - odsun sie, za daleko - podejdz
        float distanceError = distance - circleDistance;
        Vector2 radial = dirToPlayer * Mathf.Clamp(distanceError, -1f, 1f);

        Vector2 move = (tangent + radial * 0.6f).normalized;
        rb.MovePosition(rb.position + move * circleSpeed * Time.fixedDeltaTime);
    }

    private void SetState(State newState)
    {
        currentState = newState;

        switch (newState)
        {
            case State.Idle:
            case State.Chase:
            case State.Returning:
                stateTimer = 0f;
                break;

            case State.WindUp:
                stateTimer = windUpTime;
                break;

            case State.Attack:
                PerformSwing();
                stateTimer = 0f;
                break;

            case State.WaitAfterAttack:
                // TU BYL BLAD: sztywne 0.5 s nie zalezalo od broni, wiec przy
                // ciezkim toporze bandyta odskakiwal w POLOWIE wlasnego ciecia.
                // Teraz czekamy, az ostrze faktycznie przejdzie luk.
                stateTimer = CalculateSwingDuration() * swingFollowThrough + postAttackDelay;
                break;

            case State.Retreat:
                stateTimer = retreatDuration;
                nextAttackTime = Time.time + attackCooldown;
                break;

            case State.Circle:
                stateTimer = Random.Range(circleTimeMin, circleTimeMax);

                // Czasem zmienia strone, zeby nie byl przewidywalny
                if (Random.value < circleDirectionChangeChance) circleSign = -circleSign;
                break;
        }
    }

    // ===============================================================
    // CIOS
    // ===============================================================
    private void PerformSwing()
    {
        if (anim != null && HasParam(attackTrigger)) anim.SetTrigger(attackTrigger);

        if (slashPrefab == null)
        {
            Debug.LogWarning($"{name}: brak Slash Prefab - bandyta macha powietrzem!");
            return;
        }

        if (weapon == null)
        {
            Debug.LogWarning($"{name}: brak przypisanej broni (Weapon)!");
            return;
        }

        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;

        Vector2 toPlayer = (Vector2)player.position - (Vector2)origin;
        float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        GameObject slash = Instantiate(slashPrefab, origin, Quaternion.identity);
        slash.transform.SetParent(transform);   // ciecie podaza za bandyta

        EnemyMeleeAttack attack = slash.GetComponent<EnemyMeleeAttack>();
        if (attack == null)
        {
            Debug.LogError($"{name}: Slash Prefab nie ma komponentu EnemyMeleeAttack!");
            return;
        }

        attack.knockbackForce = knockbackForce;
        attack.Setup(weapon, CalculateDamage(), CalculateSwingDuration(), angle);
    }

    // Obrazenia: Sila bandyty + bonus z miecza, przemnozone przez mnoznik
    public int CalculateDamage()
    {
        int strength = creature != null ? creature.baseSTR : 0;
        int weaponBonus = weapon != null ? weapon.GetDamageBonus() : 0;

        return Mathf.Max(1, Mathf.RoundToInt((strength + weaponBonus) * damageMultiplier));
    }

    // Ciezki miecz tnie wolniej - ten sam wzor co u gracza
    public float CalculateSwingDuration()
    {
        if (weapon == null) return 0.4f;

        int strength = creature != null ? creature.baseSTR : 1;
        int dexterity = creature != null ? creature.baseZR : 1;

        float denominator = Mathf.Max(5f, (strength + dexterity) * 0.25f);
        return Mathf.Clamp(weapon.weight / denominator, 0.2f, 1.6f);
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

    // Ustawia parametr TYLKO, jesli Animator go zna - inaczej konsola
    // zalewa sie bledami "Parameter does not exist" co klatke.
    private bool HasParam(string name)
    {
        return !string.IsNullOrEmpty(name) && animParams.Contains(name);
    }

    private void UpdateAnimator()
    {
        if (anim == null) return;

        float speedValue = 0f;
        if (currentState == State.Chase || currentState == State.Returning) speedValue = 1f;
        if (currentState == State.Circle) speedValue = 1f;
        if (currentState == State.Retreat) speedValue = 2f;

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

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, deaggroRange);

        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(0.4f, 0.6f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, circleDistance);
    }
}