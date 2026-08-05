using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CreatureWander : MonoBehaviour
{
    [Header("Ustawienia Ruchu")]
    public float moveSpeed = 2f;
    public float minWanderTime = 1f;
    public float maxWanderTime = 3f;
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;

    private Rigidbody2D rb;
    private Animator anim;

    private Vector2 moveDirection;
    private bool isWandering = false;
    private float stateTimer;

    private Vector3 originalScale; // Zapamiêtuje oryginalny rozmiar, ¿eby go nie sp³aszczyæ

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        originalScale = transform.localScale;

        SetIdleState(); // Zaczynamy od stania w miejscu
    }

    void Update()
    {
        // Odliczanie czasu do zmiany stanu
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0)
        {
            if (isWandering) SetIdleState();
            else SetWanderState();
        }

        // 1. Wysy³anie danych do Animatora
        if (anim != null)
        {
            anim.SetFloat("Speed", isWandering ? 1f : 0f);
        }

        // 2. MAGICZNE OBRACANIE (FLIP)
        if (isWandering)
        {
            if (moveDirection.x > 0)
            {
                // Idzie w prawo -> Oryginalna skala
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
            }
            else if (moveDirection.x < 0)
            {
                // Idzie w lewo -> Lustrzane odbicie na osi X
                transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
            }
        }
    }

    void FixedUpdate()
    {
        if (isWandering)
        {
            // Fizyczne przesuwanie zwierzaka
            rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
        }
    }

    void SetIdleState()
    {
        isWandering = false;
        moveDirection = Vector2.zero;
        stateTimer = Random.Range(minIdleTime, maxIdleTime);
    }

    void SetWanderState()
    {
        isWandering = true;
        // Losuje kierunek (np. skos w dó³-lewo) i go normalizuje (¿eby nie przyspiesza³ na skosach)
        moveDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        stateTimer = Random.Range(minWanderTime, maxWanderTime);
    }
}