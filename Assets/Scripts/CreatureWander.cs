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

    [Header("Odwracanie Grafiki")]
    [Tooltip("Zostaw puste - skrypt znajdzie SpriteRenderer sam (na tym obiekcie albo w dziecku).")]
    public SpriteRenderer spriteRenderer;
    [Tooltip("Zaznacz, jesli postac w pliku graficznym patrzy w PRAWO.")]
    public bool spriteFacesRight = true;
    [Tooltip("Ponizej tej wartosci na osi X nie zmieniamy kierunku - zapobiega migotaniu przy ruchu prawie pionowym.")]
    public float flipDeadzone = 0.05f;

    private Rigidbody2D rb;
    private Animator anim;

    private Vector2 moveDirection;
    private bool isWandering = false;
    private float stateTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

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

        // 1. Wysylanie danych do Animatora
        if (anim != null)
        {
            anim.SetFloat("Speed", isWandering ? 1f : 0f);
        }

        // 2. ODWRACANIE GRAFIKI (FLIP)
        //
        // WAZNE: odwracamy TYLKO SpriteRenderer (flipX), a NIE cala skale obiektu
        // (transform.localScale). Kiedy odwracalismy cala skale, kazde dziecko tego
        // obiektu tez dostawalo ujemna skale - a takim dzieckiem jest np. dymek z nazwa
        // i paskiem zdrowia (CreatureHoverInfo.infoRoot) albo ikonki statusow
        // (CreatureStatusIndicator) - stad tekst i pasek wychodzily lustrzane/odwrocone
        // przy ruchu w lewo. flipX odwraca WYLACZNIE rysowanie tego jednego SpriteRenderera,
        // wiec dzieci zostaja nietkniete. Ten sam patent jest juz w BanditAI/ArcherAI.
        if (isWandering && spriteRenderer != null && Mathf.Abs(moveDirection.x) > flipDeadzone)
        {
            bool lookingRight = moveDirection.x > 0f;
            spriteRenderer.flipX = spriteFacesRight ? !lookingRight : lookingRight;
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
        // Losuje kierunek (np. skos w dol-lewo) i go normalizuje (zeby nie przyspieszal na skosach)
        moveDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        stateTimer = Random.Range(minWanderTime, maxWanderTime);
    }
}
