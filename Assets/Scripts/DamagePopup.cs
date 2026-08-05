using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private Color outlineColor;

    [Header("Ruch (Lob)")]
    public float initialYVelocity = 6f;  // Si³a wyskoku w górê
    public float minXVelocity = 2f;      // Minimalny odskok w bok
    public float maxXVelocity = 4f;      // Maksymalny odskok w bok
    public float gravity = 15f;          // Symulowana grawitacja

    private Vector3 moveVector;

    [Header("Skalowanie")]
    public Vector3 normalScale = new Vector3(1f, 1f, 1f);
    public Vector3 critScale = new Vector3(1.5f, 1.5f, 1.5f); // Krytyk jest wiêkszy!
    public float animSpeed = 10f;

    private Vector3 targetScale;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    // Nowa funkcja Setup przyjmuj¹ca czy to CRIT i kierunek uderzenia
    public void Setup(int damageAmount, bool isCrit, Vector2 hitDirection)
    {
        // 1. USTAWIENIA WYGL¥DU (Kolory i Outline)
        // Klonujemy materia³, by zmiana koloru jednego napisu nie zmieni³a wszystkich w grze
        textMesh.fontMaterial = new Material(textMesh.fontSharedMaterial);

        if (isCrit)
        {
            textColor = Color.yellow;
            outlineColor = Color.black;
            targetScale = critScale;
            textMesh.text = damageAmount.ToString() + "!"; // Dodajemy wykrzyknik
        }
        else
        {
            textColor = Color.red;
            outlineColor = Color.yellow;
            targetScale = normalScale;
            textMesh.text = damageAmount.ToString();
        }

        textMesh.color = textColor;
        textMesh.outlineWidth = 0.25f; // Gruboœæ obrysu
        textMesh.outlineColor = outlineColor;

        // Zaczynamy od po³owy rozmiaru, ¿eby fajnie "wyskoczy³" (puchniêcie)
        transform.localScale = targetScale * 0.5f;

        // 2. KIERUNEK ODSKOKU (LOB)
        // Jeœli cios przyszed³ z lewej (X > 0), to dirX = 1 (napis leci w prawo)
        float dirX = hitDirection.x > 0 ? 1f : -1f;
        float xVelocity = Random.Range(minXVelocity, maxXVelocity) * dirX;

        moveVector = new Vector3(xVelocity, initialYVelocity);
        disappearTimer = 1f; // Napis ¿yje 1 sekundê
    }

    void Update()
    {
        // 1. RUCH
        transform.position += moveVector * Time.deltaTime;
        moveVector.y -= gravity * Time.deltaTime; // Grawitacja œci¹ga go w dó³

        // 2. SKALOWANIE (Puchniêcie)
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animSpeed);

        // 3. ZNIKANIE
        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            float fadeSpeed = 3f;
            textColor.a -= fadeSpeed * Time.deltaTime;
            outlineColor.a -= fadeSpeed * Time.deltaTime;

            textMesh.color = textColor;
            textMesh.outlineColor = outlineColor;

            if (textColor.a <= 0) Destroy(gameObject);
        }
    }
}