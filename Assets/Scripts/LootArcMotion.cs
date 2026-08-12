using UnityEngine;

// Przenosi swiezo wyrzucony przedmiot po luku ze skrzyni na ziemie.
// Dodawany automatycznie przez TreasureChest - nie musisz go wieszac recznie.
public class LootArcMotion : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 endPos;
    private float arcHeight;
    private float duration;
    private float timer;
    private bool isFlying;

    private Rigidbody2D rb;
    private Collider2D[] colliders;
    private bool[] colliderStates;

    [Header("Dodatkowy sznyt")]
    public float spinDegrees = 0f;     // ustaw np. 180, jesli przedmiot ma sie obracac w locie
    public float landSquash = 0.15f;   // lekkie "plasniecie" przy ladowaniu

    public void Launch(Vector3 from, Vector3 to, float height, float time)
    {
        startPos = from;
        endPos = to;
        arcHeight = height;
        duration = Mathf.Max(0.05f, time);
        timer = 0f;
        isFlying = true;

        transform.position = from;

        // Na czas lotu wylaczamy fizyke, zeby nie walczyla z nasza animacja
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // Wylaczamy triggery, zeby gracz nie podniosl przedmiotu w powietrzu
        colliders = GetComponents<Collider2D>();
        colliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderStates[i] = colliders[i].enabled;
            colliders[i].enabled = false;
        }
    }

    void Update()
    {
        if (!isFlying) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        // Ruch poziomy: prosta linia od skrzyni do miejsca ladowania
        Vector3 pos = Vector3.Lerp(startPos, endPos, t);

        // Ruch pionowy: parabola. 4*t*(1-t) daje 0 na starcie, 1 w polowie, 0 na koncu.
        pos.y += arcHeight * 4f * t * (1f - t);

        transform.position = pos;

        if (spinDegrees != 0f)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, spinDegrees, t));

        if (t >= 1f) Land();
    }

    private void Land()
    {
        isFlying = false;
        transform.position = endPos;
        transform.rotation = Quaternion.identity;

        // Przywracamy wszystko, czym byl przedmiot przed lotem
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = colliderStates[i];
            }
        }

        if (landSquash > 0f) StartCoroutine(SquashEffect());
        else Destroy(this);
    }

    private System.Collections.IEnumerator SquashEffect()
    {
        Vector3 normal = transform.localScale;
        Vector3 squashed = new Vector3(normal.x * (1f + landSquash), normal.y * (1f - landSquash), normal.z);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 8f;
            transform.localScale = Vector3.Lerp(squashed, normal, t);
            yield return null;
        }

        transform.localScale = normal;
        Destroy(this); // skrypt znika, przedmiot zostaje zwyklym ItemPickupem
    }
}
