using UnityEngine;

// PRZEZROCZYSTOSC OBIEKTU, GDY GRACZ ZA NIM STOI (drzewa, krzaki, dachy itp.).
//
// Jak dziala:
//   - Obiekt sprawdza, czy gracz znajduje sie w obszarze jego grafiki
//     (prostokat sprite'a, mozna go zwezic polem "Area Padding")
//     ORAZ czy stoi "za" nim, czyli wyzej niz jego podstawa (pivot).
//   - Jesli tak - obiekt plynnie przechodzi do przezroczystosci "Hidden Alpha".
//     Jesli nie - plynnie wraca do pelnej widocznosci.
//   - W trybie "Gradual By Depth" przezroczystosc rosnie stopniowo,
//     im glebiej gracz wejdzie za obiekt (np. pelny efekt dopiero przy pniu).
//
// PODPIECIE: wystarczy dodac ten skrypt (Add Component). Nie potrzebuje
// zadnego dodatkowego collidera - korzysta z rozmiaru grafiki. Najlepiej dodac
// go raz w PREFABIE (np. Trees_shadow_source_0) - wtedy dziala na kazdym drzewie.
public class FadeWhenPlayerBehind : MonoBehaviour
{
    [Header("Przezroczystosc")]
    [Tooltip("Do jakiej przezroczystosci schodzi obiekt (0 = niewidoczny, 1 = bez zmian).")]
    [Range(0f, 1f)] public float hiddenAlpha = 0.45f;

    [Tooltip("Jak szybko obiekt zanika i wraca (jednostki alfa na sekunde).")]
    public float fadeSpeed = 4f;

    [Header("Tryb stopniowy")]
    [Tooltip("Wylaczone: obiekt zanika do 'Hidden Alpha', gdy tylko gracz wejdzie za niego.\n" +
             "Wlaczone: im glebiej gracz wejdzie za obiekt, tym bardziej jest przezroczysty.")]
    public bool gradualByDepth = false;

    [Tooltip("Tylko tryb stopniowy: na jakiej glebokosci (w jednostkach swiata, mierzonej " +
             "od podstawy obiektu w gore) efekt osiaga pelna moc.")]
    public float fullEffectDepth = 1.5f;

    [Header("Obszar")]
    [Tooltip("Zmniejsza (wartosci ujemne) albo powieksza obszar sprawdzania wzgledem " +
             "grafiki - przydatne, gdy sprite ma przezroczyste marginesy. Zaznacz obiekt, " +
             "zeby zobaczyc obszar jako zolty prostokat w Scene.")]
    public Vector2 areaPadding = new Vector2(-0.1f, -0.1f);

    [Tooltip("O ile powyzej podstawy obiektu gracz musi stac, zeby liczylo sie, ze jest 'za' nim. " +
             "Podstawa = pozycja (pivot) obiektu, zwykle dol pnia.")]
    public float behindOffset = 0f;

    [Tooltip("Ktory punkt gracza sprawdzamy - przesuniecie od jego pozycji (zwykle stop) " +
             "w gore. 0.3-0.5 = mniej wiecej srodek postaci.")]
    public float playerPointOffsetY = 0.4f;

    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private float currentAlpha = 1f;
    private Transform player;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
    }

    void Update()
    {
        if (renderers == null || renderers.Length == 0) return;

        if (player == null) FindPlayer();

        float targetAlpha = player != null ? CalculateTargetAlpha() : 1f;

        if (!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            ApplyAlpha(currentAlpha);
        }
    }

    private float CalculateTargetAlpha()
    {
        Vector2 point = (Vector2)player.position + Vector2.up * playerPointOffsetY;

        Bounds area = GetArea();
        if (!area.Contains(new Vector3(point.x, point.y, area.center.z))) return 1f;

        float depth = point.y - (transform.position.y + behindOffset);
        if (depth <= 0f) return 1f; // gracz stoi PRZED obiektem - nic nie zaslania

        if (!gradualByDepth) return hiddenAlpha;

        float t = fullEffectDepth > 0f ? Mathf.Clamp01(depth / fullEffectDepth) : 1f;
        return Mathf.Lerp(1f, hiddenAlpha, t);
    }

    // Prostokat wszystkich grafik obiektu, zwezony/poszerzony o areaPadding
    private Bounds GetArea()
    {
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        b.Expand(new Vector3(areaPadding.x * 2f, areaPadding.y * 2f, 0f));
        return b;
    }

    private void ApplyAlpha(float alpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Color c = baseColors[i];
            c.a = baseColors[i].a * alpha;
            renderers[i].color = c;
        }
    }

    private void FindPlayer()
    {
        if (PlayerStats.instance != null) { player = PlayerStats.instance.transform; return; }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void OnDrawGizmosSelected()
    {
        SpriteRenderer[] rs = renderers != null && renderers.Length > 0
            ? renderers
            : GetComponentsInChildren<SpriteRenderer>(true);
        if (rs.Length == 0) return;

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        b.Expand(new Vector3(areaPadding.x * 2f, areaPadding.y * 2f, 0f));

        // Obszar sprawdzania
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(b.center, b.size);

        // Linia "podstawy" - gracz musi byc POWYZEJ niej, zeby obiekt zanikal
        float baseY = transform.position.y + behindOffset;
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
        Gizmos.DrawLine(new Vector3(b.min.x, baseY, 0f), new Vector3(b.max.x, baseY, 0f));
    }
}
