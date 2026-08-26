using System.Collections.Generic;
using UnityEngine;

// EXPOWISKO - strefa, w ktorej sama odradza sie okreslona populacja mobow.
//
// Postaw pusty obiekt na mapie, powies ten skrypt, ustaw ksztalt strefy
// i wrzuc prefaby przeciwnikow. Reszta dzieje sie sama.
//
// Strefa pilnuje LICZBY zywych, a nie liczby zabitych - dzieki temu dziala
// tak samo, gdy przeciwnik zginie, jak i gdy zniknie z innego powodu.
public class SpawnZone : MonoBehaviour
{
    public enum ZoneShape { Circle, Rectangle }

    // Pojedyncza pozycja na liscie "co tu mieszka"
    [System.Serializable]
    public class SpawnEntry
    {
        [Tooltip("Prefab przeciwnika. Powinien miec komponent Creature.")]
        public GameObject prefab;

        [Tooltip("Waga losowania. 100 wypada 10x czesciej niz 10.")]
        [Min(0f)] public float weight = 10f;

        [Tooltip("Ilu takich moze zyc naraz. 0 = bez ograniczen.")]
        [Min(0)] public int maxOfThisType = 0;

        [HideInInspector] public int aliveCount;
    }

    [Header("Opis")]
    public string zoneName = "Expowisko";

    [Header("Kto tu mieszka")]
    public SpawnEntry[] creatures;

    [Header("Ksztalt Strefy")]
    public ZoneShape shape = ZoneShape.Circle;
    public float radius = 6f;
    public Vector2 rectSize = new Vector2(10f, 6f);

    [Header("Populacja")]
    [Tooltip("Ilu przeciwnikow moze zyc w strefie naraz.")]
    public int maxAlive = 5;

    [Tooltip("Ilu pojawia sie od razu przy pierwszej aktywacji.")]
    public int initialSpawnCount = 3;

    [Tooltip("Ilu odradza sie za jednym razem.")]
    public int spawnBatchSize = 1;

    [Header("Czasy Odradzania")]
    public float firstSpawnDelay = 0.5f;
    public float respawnDelayMin = 4f;
    public float respawnDelayMax = 9f;

    [Header("Aktywacja")]
    [Tooltip("Strefa spawnuje tylko, gdy gracz jest w poblizu. " +
             "Oszczedza wydajnosc na duzej mapie.")]
    public bool onlyWhenPlayerNear = true;
    public float activationRange = 22f;

    [Tooltip("Nie tworzymy moba tak blisko gracza - nie chcemy, by wyrastal mu przed nosem.")]
    public float minDistanceFromPlayer = 4f;

    [Tooltip("Usuwa zywych przeciwnikow, gdy gracz odejdzie bardzo daleko.")]
    public bool despawnWhenPlayerFar = false;
    public float despawnRange = 40f;

    [Header("Przeszkody")]
    [Tooltip("Warstwy, na ktorych mob NIE moze sie pojawic (sciany, skaly, woda).")]
    public LayerMask obstacleLayers;

    [Tooltip("Promien sprawdzany przy szukaniu wolnego miejsca.")]
    public float spawnClearRadius = 0.4f;

    [Tooltip("Ile razy probowac znalezc wolne miejsce, zanim odpuscimy.")]
    public int positionAttempts = 12;

    [Header("Poziom Przeciwnikow")]
    [Tooltip("Nadpisuje poziom z prefabu i skaluje zycie oraz doswiadczenie.")]
    public bool overrideLevel = false;
    public int levelMin = 1;
    public int levelMax = 1;

    [Tooltip("O ile procent rosnie zycie za kazdy poziom powyzej pierwszego.")]
    public float healthBonusPerLevel = 15f;

    [Tooltip("O ile procent rosnie doswiadczenie za kazdy poziom powyzej pierwszego.")]
    public float expBonusPerLevel = 20f;

    [Header("Efekty (opcjonalne)")]
    public GameObject spawnEffectPrefab;
    public AudioClip[] spawnSounds;
    [Range(0f, 1f)] public float spawnVolume = 0.4f;

    [Header("Podglad w Edytorze")]
    public bool alwaysShowGizmo = true;
    public Color zoneColor = new Color(1f, 0.4f, 0.1f, 0.9f);

    // --- stan wewnetrzny ---
    private readonly List<GameObject> alive = new List<GameObject>();
    private readonly List<Creature> aliveCreatures = new List<Creature>();
    private readonly List<SpawnEntry> aliveOrigin = new List<SpawnEntry>();

    private float respawnTimer;
    private bool hasDoneInitialSpawn;
    private Transform player;

    public int AliveCount { get { return alive.Count; } }

    void Start()
    {
        respawnTimer = firstSpawnDelay;
        FindPlayer();
    }

    private void FindPlayer()
    {
        if (PlayerStats.instance != null) { player = PlayerStats.instance.transform; return; }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) FindPlayer();

        PruneDead();

        bool playerNear = IsPlayerNear();

        if (despawnWhenPlayerFar && player != null &&
            Vector2.Distance(transform.position, player.position) > despawnRange)
        {
            DespawnAll();
            return;
        }

        if (onlyWhenPlayerNear && !playerNear) return;

        // Pierwsze zaludnienie strefy
        if (!hasDoneInitialSpawn)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer > 0f) return;

            hasDoneInitialSpawn = true;
            int count = Mathf.Min(initialSpawnCount, maxAlive);
            for (int i = 0; i < count; i++) SpawnOne();

            respawnTimer = Random.Range(respawnDelayMin, respawnDelayMax);
            return;
        }

        // Uzupelnianie ubytkow
        if (alive.Count >= maxAlive) return;

        respawnTimer -= Time.deltaTime;
        if (respawnTimer > 0f) return;

        int batch = Mathf.Min(spawnBatchSize, maxAlive - alive.Count);
        for (int i = 0; i < batch; i++) SpawnOne();

        respawnTimer = Random.Range(respawnDelayMin, respawnDelayMax);
    }

    private bool IsPlayerNear()
    {
        if (player == null) return false;
        return Vector2.Distance(transform.position, player.position) <= activationRange;
    }

    // Sprzatanie listy: martwi i zniszczeni znikaja z ewidencji
    private void PruneDead()
    {
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            bool gone = alive[i] == null;

            // Boss szarzeje przez chwile po smierci - liczymy go juz jako martwego,
            // zeby strefa nie czekala z odrodzeniem do konca animacji.
            if (!gone && aliveCreatures[i] != null && aliveCreatures[i].IsDead) gone = true;

            if (!gone) continue;

            if (aliveOrigin[i] != null) aliveOrigin[i].aliveCount--;

            alive.RemoveAt(i);
            aliveCreatures.RemoveAt(i);
            aliveOrigin.RemoveAt(i);
        }
    }

    public void SpawnOne()
    {
        SpawnEntry entry = PickEntry();
        if (entry == null || entry.prefab == null) return;

        if (!TryFindSpawnPosition(out Vector3 pos))
        {
            // Cala strefa zastawiona albo gracz stoi w srodku - sprobujemy za chwile
            return;
        }

        GameObject obj = Instantiate(entry.prefab, pos, Quaternion.identity);
        obj.transform.SetParent(null); // mob zyje wlasnym zyciem, nie przesuwa sie ze strefa

        Creature creature = obj.GetComponent<Creature>();
        if (creature != null && overrideLevel) ApplyLevel(creature);

        alive.Add(obj);
        aliveCreatures.Add(creature);
        aliveOrigin.Add(entry);
        entry.aliveCount++;

        if (spawnEffectPrefab != null) Instantiate(spawnEffectPrefab, pos, Quaternion.identity);
        SoundManager.Play(spawnSounds, spawnVolume);
    }

    // Skalowanie mocy przeciwnika do poziomu strefy
    private void ApplyLevel(Creature creature)
    {
        int level = Random.Range(Mathf.Min(levelMin, levelMax), Mathf.Max(levelMin, levelMax) + 1);
        creature.level = level;

        int stepsAboveOne = Mathf.Max(0, level - 1);
        if (stepsAboveOne == 0) return;

        float healthMult = 1f + (healthBonusPerLevel / 100f) * stepsAboveOne;
        float expMult = 1f + (expBonusPerLevel / 100f) * stepsAboveOne;

        // maxHealth liczy sie w Start() Creature, wiec podbijamy witalnosc,
        // z ktorej to zycie powstaje - dziala niezaleznie od kolejnosci wywolan.
        creature.baseWIT = Mathf.Max(1, Mathf.RoundToInt(creature.baseWIT * healthMult));
        creature.expReward = Mathf.RoundToInt(creature.expReward * expMult);
        creature.baseDmg = Mathf.RoundToInt(creature.baseDmg * healthMult);
    }

    private SpawnEntry PickEntry()
    {
        if (creatures == null || creatures.Length == 0) return null;

        // Odsiewamy tych, ktorzy osiagneli wlasny limit
        float total = 0f;
        foreach (SpawnEntry e in creatures)
        {
            if (e == null || e.prefab == null) continue;
            if (e.maxOfThisType > 0 && e.aliveCount >= e.maxOfThisType) continue;
            total += Mathf.Max(0f, e.weight);
        }

        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        foreach (SpawnEntry e in creatures)
        {
            if (e == null || e.prefab == null) continue;
            if (e.maxOfThisType > 0 && e.aliveCount >= e.maxOfThisType) continue;

            roll -= Mathf.Max(0f, e.weight);
            if (roll <= 0f) return e;
        }

        return null;
    }

    // Szuka wolnego miejsca: nie w scianie i nie na glowie gracza
    private bool TryFindSpawnPosition(out Vector3 position)
    {
        for (int i = 0; i < Mathf.Max(1, positionAttempts); i++)
        {
            Vector3 candidate = RandomPointInZone();

            if (player != null &&
                Vector2.Distance(candidate, player.position) < minDistanceFromPlayer) continue;

            if (obstacleLayers.value != 0 &&
                Physics2D.OverlapCircle(candidate, spawnClearRadius, obstacleLayers) != null) continue;

            position = candidate;
            return true;
        }

        position = transform.position;
        return false;
    }

    private Vector3 RandomPointInZone()
    {
        if (shape == ZoneShape.Circle)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            return transform.position + new Vector3(offset.x, offset.y, 0f);
        }

        return transform.position + new Vector3(
            Random.Range(-rectSize.x * 0.5f, rectSize.x * 0.5f),
            Random.Range(-rectSize.y * 0.5f, rectSize.y * 0.5f),
            0f);
    }

    // --- STEROWANIE Z ZEWNATRZ (np. z questa) ---

    public void DespawnAll()
    {
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            if (alive[i] != null) Destroy(alive[i]);
        }

        alive.Clear();
        aliveCreatures.Clear();
        aliveOrigin.Clear();

        foreach (SpawnEntry e in creatures)
        {
            if (e != null) e.aliveCount = 0;
        }

        hasDoneInitialSpawn = false;
        respawnTimer = firstSpawnDelay;
    }

    public void ForceRefill()
    {
        PruneDead();
        while (alive.Count < maxAlive)
        {
            int before = alive.Count;
            SpawnOne();
            if (alive.Count == before) break; // nie znalazlo miejsca - koniec prob
        }
    }

    // --- PODGLAD W EDYTORZE ---

    void OnDrawGizmos()
    {
        if (alwaysShowGizmo) DrawZone(0.35f);
    }

    void OnDrawGizmosSelected()
    {
        DrawZone(1f);

        // Zasieg aktywacji
        if (onlyWhenPlayerNear)
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, activationRange);
        }

        // Martwa strefa wokol gracza
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, minDistanceFromPlayer);
    }

    private void DrawZone(float alphaMult)
    {
        Color c = zoneColor;
        c.a *= alphaMult;
        Gizmos.color = c;

        if (shape == ZoneShape.Circle) Gizmos.DrawWireSphere(transform.position, radius);
        else Gizmos.DrawWireCube(transform.position, new Vector3(rectSize.x, rectSize.y, 0.1f));

#if UNITY_EDITOR
        if (alphaMult >= 1f)
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
                $"{zoneName}  ({AliveCount}/{maxAlive})");
#endif
    }
}