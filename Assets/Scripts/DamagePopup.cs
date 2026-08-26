using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private Color outlineColor;

    [Header("Ruch (Lob)")]
    public float initialYVelocity = 6f;  // Si�a wyskoku w g�r�
    public float minXVelocity = 2f;      // Minimalny odskok w bok
    public float maxXVelocity = 4f;      // Maksymalny odskok w bok
    public float gravity = 15f;          // Symulowana grawitacja

    private Vector3 moveVector;

    [Header("Skalowanie")]
    public Vector3 normalScale = new Vector3(1f, 1f, 1f);
    public Vector3 critScale = new Vector3(1.5f, 1.5f, 1.5f); // Krytyk jest wi�kszy!
    public float animSpeed = 10f;

    private Vector3 targetScale;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();

        if (textMesh == null)
            Debug.LogError($"DamagePopup '{name}': brak komponentu TextMeshPro! " +
                           "Prefab musi uzywac TextMeshPro (3D), nie TextMeshProUGUI.");
    }

    void OnDestroy()
    {
        // WYCIEK PAMIECI: TMP tworzy kopie materialu przy pierwszym siegnieciu
        // po 'fontMaterial'. Material NIE jest sprzatany przez garbage collector,
        // wiec kazdy napis zostawal w pamieci do konca sceny.
        if (textMesh != null && textMesh.fontMaterial != null)
            Destroy(textMesh.fontMaterial);
    }

    // Nowa funkcja Setup przyjmuj�ca czy to CRIT i kierunek uderzenia
    public void Setup(int damageAmount, bool isCrit, Vector2 hitDirection)
    {
        if (textMesh == null) { Destroy(gameObject); return; }

        // Samo odczytanie 'fontMaterial' (zamiast 'fontSharedMaterial') sprawia,
        // ze TMP robi kopie dla tego obiektu. Reczne tworzenie Material bylo zbedne.
        Material _ = textMesh.fontMaterial;


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
        textMesh.outlineWidth = 0.25f; // Grubo�� obrysu
        textMesh.outlineColor = outlineColor;

        // Zaczynamy od po�owy rozmiaru, �eby fajnie "wyskoczy�" (puchni�cie)
        transform.localScale = targetScale * 0.5f;

        // 2. KIERUNEK ODSKOKU (LOB)
        // Je�li cios przyszed� z lewej (X > 0), to dirX = 1 (napis leci w prawo)
        float dirX = hitDirection.x > 0 ? 1f : -1f;
        float xVelocity = Random.Range(minXVelocity, maxXVelocity) * dirX;

        moveVector = new Vector3(xVelocity, initialYVelocity);
        disappearTimer = 1f; // Napis �yje 1 sekund�
    }

    void Update()
    {
        // 1. RUCH
        transform.position += moveVector * Time.deltaTime;
        moveVector.y -= gravity * Time.deltaTime; // Grawitacja �ci�ga go w d�

        // 2. SKALOWANIE (Puchni�cie)
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