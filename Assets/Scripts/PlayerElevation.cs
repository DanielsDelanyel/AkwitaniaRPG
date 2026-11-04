using UnityEngine;

// POZIOM GRACZA: "na ziemi" albo "na murach".
//
// Jak to dziala:
//   - Mury maja DWA zestawy kolizji na osobnych warstwach fizyki:
//       MuryDol  - caly obrys muru widziany z dolu (sciana + chodnik + blanki),
//                  blokuje gracza chodzacego po ziemi.
//       MuryGora - krawedzie chodnika na gorze (blanki, konce muru),
//                  nie pozwalaja zejsc z muru inaczej niz schodami.
//   - Ten skrypt przelacza warstwe fizyki gracza: "Player" (koliduje z MuryDol)
//     albo "PlayerGora" (koliduje z MuryGora). Co z czym koliduje, ustawiasz
//     w Project Settings -> Physics 2D -> Layer Collision Matrix.
//   - Wejscie na mur: strefa ElevationZone na gorze schodow.
//   - Zejscie: jesli gracz stanie poza obrysem muru i poza strefa schodow
//     (czyli zszedl schodami, zginal i odrodzil sie na ziemi, przeszedl do
//     innej lokacji) - automatycznie wraca na poziom ziemi.
//   - Na mur da sie wejsc TYLKO przez strefe schodow (wyjatek: start gry
//     w obrysie muru, np. wczytany zapis - wtedy gracz od razu jest na murach).
//   - WAZNE: Composite Collider 2D na tilemapie MuryDol musi miec
//     Geometry Type = Polygons, inaczej "srodek" muru jest pusty.
//
// Powies na obiekcie gracza (tym z Rigidbody2D i colliderem ciala).
public class PlayerElevation : MonoBehaviour
{
    public static PlayerElevation instance;

    [Header("Warstwy fizyki gracza")]
    public string groundLayerName = "Player";
    public string wallTopLayerName = "PlayerGora";

    [Header("Obrys murow (warstwa MuryDol)")]
    [Tooltip("Zaznacz warstwe MuryDol. Uzywane do sprawdzania, czy gracz stoi w obrysie muru.\n" +
             "WAZNE: Composite Collider 2D na tilemapie kolizji musi miec Geometry Type = Polygons.")]
    public LayerMask lowerWallMask;

    [Tooltip("Collider ciala gracza (ten, ktory zderza sie ze scianami). Zostaw puste - " +
             "skrypt znajdzie sam pierwszy collider, ktory nie jest triggerem.")]
    public Collider2D bodyCollider;

    [Tooltip("Przesuniecie punktu sprawdzania od SRODKA collidera ciala. Zwykle zostaw 0 - " +
             "srodek collidera nigdy nie wchodzi w mur przy zwyklym zderzeniu.")]
    public Vector2 feetOffset = Vector2.zero;

    public bool IsOnWall { get; private set; }

    private int groundLayer;
    private int wallTopLayer;

    void Awake()
    {
        instance = this;
        groundLayer = LayerMask.NameToLayer(groundLayerName);
        wallTopLayer = LayerMask.NameToLayer(wallTopLayerName);

        if (bodyCollider == null)
        {
            foreach (Collider2D c in GetComponents<Collider2D>())
                if (!c.isTrigger) { bodyCollider = c; break; }
        }

        if (groundLayer < 0 || wallTopLayer < 0)
            Debug.LogWarning($"PlayerElevation: brak warstwy '{groundLayerName}' lub '{wallTopLayerName}' - " +
                             "dodaj je w Project Settings -> Tags and Layers.");
    }

    void Start()
    {
        // Jednorazowo na starcie: jesli gracz pojawil sie w obrysie muru
        // (np. wczytany zapis zrobiony na murach) - stawiamy go na murach.
        if (IsInsideWall(CheckPoint())) SetOnWall(true);
    }

    void FixedUpdate()
    {
        Vector2 point = CheckPoint();

        if (!IsOnWall)
        {
            // Z ziemi na mur prowadzi TYLKO strefa schodow. Samo wejscie w obrys
            // (np. miedzy czubki tylnych blankow od strony dziedzinca) nie wystarcza.
            if (ElevationZone.IsInsideAny(point)) SetOnWall(true);
        }
        else
        {
            // Na murach, ale poza obrysem i poza strefa schodow -> zszedl na ziemie
            // (zszedl schodami, odrodzil sie po smierci, przeniosl sie do innej lokacji).
            if (!IsInsideWall(point) && !ElevationZone.IsInsideAny(point)) SetOnWall(false);
        }
    }

    private bool IsInsideWall(Vector2 point)
    {
        return lowerWallMask.value != 0 && Physics2D.OverlapPoint(point, lowerWallMask) != null;
    }

    public void SetOnWall(bool onWall)
    {
        if (IsOnWall == onWall) return;
        IsOnWall = onWall;

        int layer = onWall ? wallTopLayer : groundLayer;
        if (layer >= 0) gameObject.layer = layer;

        Debug.Log(onWall ? "Gracz wszedl na mury." : "Gracz zszedl na ziemie.");
    }

    // Srodek collidera ciala (a nie jego krawedz) - przy zderzeniu z murem
    // krawedz dotyka obrysu, ale srodek zawsze zostaje na zewnatrz.
    private Vector2 CheckPoint()
    {
        Vector2 basePoint = bodyCollider != null ? (Vector2)bodyCollider.bounds.center : (Vector2)transform.position;
        return basePoint + feetOffset;
    }

    void OnDrawGizmosSelected()
    {
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(CheckPoint(), 0.08f);
    }
}
