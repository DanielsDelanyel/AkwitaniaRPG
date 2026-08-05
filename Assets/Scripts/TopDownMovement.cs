using UnityEngine;

public class TopDownMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. Odczyt klawiszy
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // Normalizacja, aby postaæ nie porusza³a siê szybciej id¹c po skosie
        moveInput = moveInput.normalized;

        // 2. Przekazanie danych do Animatora
        if (anim != null)
        {
            // Speed zawsze odœwie¿amy (¿eby wiedzieæ, czy stoimy, czy biegniemy)
            anim.SetFloat("Speed", moveInput.sqrMagnitude);

            // Kierunek (Horizontal/Vertical) aktualizujemy TYLKO gdy gracz siê rusza.
            // Dziêki temu po puszczeniu klawiszy postaæ nadal "pamiêta", gdzie patrzy³a
            // i odtwarza odpowiedni¹ animacjê Idle.
            if (moveInput != Vector2.zero)
            {
                anim.SetFloat("Horizontal", moveInput.x);
                anim.SetFloat("Vertical", moveInput.y);
            }
        }
    }

    void FixedUpdate()
    {
        // 1. Domyœlna prêdkoœæ
        float currentSpeed = moveSpeed;

        // 2. Jeœli statystyki istniej¹, aplikujemy mno¿nik (np. x 1.1f dla W³óczêgi)
        if (PlayerStats.instance != null)
        {
            currentSpeed *= PlayerStats.instance.moveSpeedMultiplier;
        }

        // Fizyczne przemieszczanie postaci w przestrzeni z now¹ prêdkoœci¹
        rb.MovePosition(rb.position + moveInput * currentSpeed * Time.fixedDeltaTime);
    }
}