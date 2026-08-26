using UnityEngine;

// PUNKT ODRODZENIA (ognisko, karczma, kapliczka w miescie).
//
// Gracz musi go raz "aktywowac", wchodzac w jego obszar. Od tej chwili
// to tutaj wraca po smierci - dokladnie tak dziala system ognisk w Dark Souls.
//
// Wymaga LocationSpawnPoint, bo odrodzenie w innej lokacji korzysta
// z tego samego mechanizmu co przechodzenie przez drzwi.
[RequireComponent(typeof(LocationSpawnPoint))]
public class RespawnPoint : MonoBehaviour
{
    [Header("Opis")]
    [Tooltip("Nazwa pokazywana graczowi, np. 'Wioska Akwitania'.")]
    public string displayName = "Bezpieczne miejsce";

    [Header("Aktywacja")]
    [Tooltip("Z jakiej odleglosci gracz aktywuje ten punkt.")]
    public float activationRange = 2f;

    [Tooltip("Odznacz, jesli punkt ma byc aktywny od poczatku gry, " +
             "bez koniecznosci odwiedzenia go.")]
    public bool requiresVisit = true;

    [Header("Efekty (opcjonalne)")]
    public GameObject activatedEffectPrefab;
    public AudioClip[] activationSounds;
    [Range(0f, 1f)] public float soundVolume = 0.6f;

    [Header("Podglad")]
    public Color gizmoColor = new Color(0.4f, 1f, 0.6f, 0.9f);

    // ===============================================================
    // PAMIEC MIEDZY SCENAMI
    // Statyczne pola przezywaja zaladowanie innej lokacji.
    // ===============================================================
    public static string CurrentScene { get; private set; }
    public static string CurrentSpawnId { get; private set; }
    public static string CurrentName { get; private set; }

    public static bool HasRespawnPoint
    {
        get { return !string.IsNullOrEmpty(CurrentScene) && !string.IsNullOrEmpty(CurrentSpawnId); }
    }

    // Wolane przy wczytywaniu zapisu
    public static void SetCurrent(string scene, string spawnId, string displayName)
    {
        CurrentScene = scene;
        CurrentSpawnId = spawnId;
        CurrentName = displayName;
    }

    public static void Clear()
    {
        CurrentScene = null;
        CurrentSpawnId = null;
        CurrentName = null;
    }

    private LocationSpawnPoint spawnPoint;
    private Transform player;
    private bool wasActivated;

    void Start()
    {
        spawnPoint = GetComponent<LocationSpawnPoint>();

        if (spawnPoint == null || string.IsNullOrEmpty(spawnPoint.spawnId))
        {
            Debug.LogError($"RespawnPoint '{name}': brak LocationSpawnPoint albo pustego Spawn Id! " +
                           "Bez tego gracz nie bedzie mial gdzie wrocic.");
            return;
        }

        // Punkt startowy moze byc aktywny od razu
        if (!requiresVisit && !HasRespawnPoint) Activate(false);
    }

    void Update()
    {
        if (wasActivated || spawnPoint == null) return;

        if (player == null)
        {
            if (PlayerStats.instance != null) player = PlayerStats.instance.transform;
            else return;
        }

        if (Vector2.Distance(transform.position, player.position) <= activationRange)
            Activate(true);
    }

    public void Activate(bool withEffects)
    {
        wasActivated = true;

        string scene = GetSceneName();
        SetCurrent(scene, spawnPoint.spawnId, displayName);

        Debug.Log($"Punkt odrodzenia aktywowany: {displayName} ({scene} / {spawnPoint.spawnId})");

        if (!withEffects) return;

        if (activatedEffectPrefab != null)
            Instantiate(activatedEffectPrefab, transform.position, Quaternion.identity);

        SoundManager.Play(activationSounds, soundVolume);
    }

    // Nazwa sceny, w ktorej ten punkt sie znajduje
    private string GetSceneName()
    {
        return gameObject.scene.IsValid() ? gameObject.scene.name : "";
    }

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, activationRange);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.9f, $"Respawn: {displayName}");
#endif
    }
}
