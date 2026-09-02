using UnityEngine;

// AI PRZYZWANEGO STWORZENIA (np. Szkieletu z "Przyzwania Nieumarlych").
//
// W odroznieniu od CreatureAI/BanditAI/ArcherAI/BossController (ktore ZAWSZE
// celuja w gracza po tagu "Player"), to stworzenie szuka NAJBLIZSZEGO wrogiego
// Creature w poblizu i je atakuje. Gdy nie ma wroga w zasiegu, podaza za
// wlascicielem (graczem), zeby nie zostac samo z tylu na mapie.
//
// UWAGA (swiadome uproszczenie v1): "wrogie" = KAZDY Creature bez komponentu
// SummonedCreature, wiec szkielet zaatakuje tez pokojowa owieczke, jesli
// znajdzie sie najblizej. Jesli chcesz, zeby ignorowal Disposition.Peaceful,
// daj znac - to jeden warunek do dodania w FindTarget().
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Creature))]
public class SummonedCreatureAI : MonoBehaviour
{
    private enum State { FollowOwner, Chase, Attack }

    [Header("Wlasciciel")]
    [Tooltip("Ustawiane automatycznie przez PlayerSkills w momencie przyzwania. " +
             "Nie trzeba tego wypelniac recznie w prefabie.")]
    public Transform owner;

    [Header("Namierzanie Wrogow")]
    public float detectionRange = 8f;
    public float loseTargetRange = 11f;
    public float retargetInterval = 0.5f;

    [Header("Podazanie za Wlascicielem")]
    [Tooltip("Powyzej tego dystansu od gracza, przy braku wroga, sluga zaczyna wracac.")]
    public float followTriggerDistance = 3f;
    public float followSpeed = 3.5f;

    [Header("Atak")]
    public float attackRange = 1.1f;
    public float chaseSpeed = 3.6f;
    public float attackCooldown = 1.2f;

    [Tooltip("Nadpisywane przez PlayerSkills wedlug Inteligencji gracza w chwili przyzwania.")]
    public int attackDamage = 5;

    private Rigidbody2D rb;
    private Creature creature;
    private Creature target;
    private State currentState;
    private float retargetTimer;
    private float nextAttackTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        creature = GetComponent<Creature>();
        currentState = State.FollowOwner;
    }

    void Update()
    {
        if (creature.IsDead) return;

        retargetTimer -= Time.deltaTime;

        bool needsNewTarget = target == null || target.IsDead ||
            Vector2.Distance(transform.position, target.transform.position) > loseTargetRange;

        if (needsNewTarget && retargetTimer <= 0f)
        {
            FindTarget();
            retargetTimer = retargetInterval;
        }

        if (target != null && !target.IsDead)
        {
            float dist = Vector2.Distance(transform.position, target.transform.position);
            currentState = dist <= attackRange ? State.Attack : State.Chase;

            if (currentState == State.Attack && Time.time >= nextAttackTime)
            {
                PerformAttack();
            }
        }
        else
        {
            currentState = State.FollowOwner;
        }
    }

    void FixedUpdate()
    {
        if (creature.IsDead) return;

        switch (currentState)
        {
            case State.Chase:
                if (target == null) break;
                Vector2 toTarget = ((Vector2)target.transform.position - rb.position).normalized;
                rb.MovePosition(rb.position + toTarget * chaseSpeed * Time.fixedDeltaTime);
                break;

            case State.FollowOwner:
                if (owner == null) break;
                float distToOwner = Vector2.Distance(rb.position, owner.position);
                if (distToOwner > followTriggerDistance)
                {
                    Vector2 toOwner = ((Vector2)owner.position - rb.position).normalized;
                    rb.MovePosition(rb.position + toOwner * followSpeed * Time.fixedDeltaTime);
                }
                break;

                // Attack = stoi w miejscu i okresowo bije
        }
    }

    // Szuka najblizszego wrogiego Creature w promieniu detectionRange.
    private void FindTarget()
    {
        Creature[] all = FindObjectsByType<Creature>(FindObjectsSortMode.None);

        Creature best = null;
        float bestDist = detectionRange;

        foreach (Creature c in all)
        {
            if (c == null || c == creature || c.IsDead) continue;
            if (c.GetComponent<SummonedCreature>() != null) continue; // nie atakujemy innych przyzwan

            float d = Vector2.Distance(transform.position, c.transform.position);
            if (d <= bestDist)
            {
                bestDist = d;
                best = c;
            }
        }

        target = best;
    }

    private void PerformAttack()
    {
        nextAttackTime = Time.time + attackCooldown;

        Vector2 hitDir = (target.transform.position - transform.position).normalized;
        target.TakeDamage(attackDamage, false, hitDir);
    }
}
