using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Serce systemu lokacji. Zyje w scenie "Bootstrap" i nigdy nie ginie.
// Lokacje (swiat, domki, jaskinie) sa ladowane DODATKOWO (additive),
// dzieki czemu Gracz, Kamera i Canvas istnieja tylko w jednym egzemplarzu.
public class LocationManager : MonoBehaviour
{
    public static LocationManager instance;

    [Header("Start Gry")]
    [Tooltip("Nazwa sceny z otwartym swiatem - dokladnie tak, jak w Build Settings.")]
    public string startingLocation = "World";
    public string startingSpawnId = "Start";

    [Header("Przejscie")]
    public ScreenFader fader;
    public float fadeOutTime = 0.35f;
    public float fadeInTime = 0.35f;

    [Header("Referencje")]
    [Tooltip("Zostaw puste - znajdzie gracza po PlayerStats.instance.")]
    public Transform player;

    public string CurrentLocation { get; private set; }
    public bool IsTransitioning { get; private set; }

    // Scena, w ktorej ten obiekt sie urodzil. MUSI byc zapisana przed DontDestroyOnLoad,
    // bo ta funkcja przenosi obiekt do wewnetrznej sceny systemowej Unity.
    private Scene bootstrapScene;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        bootstrapScene = gameObject.scene; // <- najpierw zapamietaj...
        DontDestroyOnLoad(gameObject);     // <- ...a dopiero potem przenies
    }

    void Start()
    {
        StartCoroutine(BootRoutine());
    }

    private IEnumerator BootRoutine()
    {
        // KROK 1: sprzatanie po edytorze.
        // Unity laduje WSZYSTKIE sceny otwarte w Hierarchii, gdy wciskasz Play.
        // Jesli obok Bootstrapa wisial World albo wnetrze domku, trzeba je zamknac,
        // inaczej dwie lokacje beda narysowane jedna na drugiej.
        yield return CleanupStrayScenes();

        // KROK 2: wejscie do gry.
        // Jesli gracz wybral "Wczytaj gre", idziemy do lokacji Z ZAPISU,
        // a nie do domyslnej sceny startowej.
        string targetLocation = startingLocation;
        string targetSpawn = startingSpawnId;
        bool loadingSave = false;

        if (GameSession.IsLoadingSave && SaveManager.LoadFromDisk(GameSession.SaveSlot))
        {
            SaveData save = SaveManager.PendingLoad;

            if (save != null && !string.IsNullOrEmpty(save.player.locationScene))
            {
                targetLocation = save.player.locationScene;
                targetSpawn = "";   // pozycje ustawi SaveManager, nie punkt odrodzenia
                loadingSave = true;
            }
        }

        GameSession.IsLoadingSave = false;

        if (!string.IsNullOrEmpty(targetLocation))
            yield return LoadRoutine(targetLocation, targetSpawn, true);

        // KROK 3: dane gracza nakladamy DOPIERO, gdy lokacja stoi
        if (loadingSave)
        {
            yield return null;   // jedna klatka na obudzenie obiektow sceny
            SaveManager.ApplyPendingLoad();
        }
        else
        {
            SaveManager.ResetSession();
        }

        SaveManager.StartSession();
    }

    private IEnumerator CleanupStrayScenes()
    {
        // Aktywna scena nie moze byc ta, ktora zaraz zamykamy.
        // Uwaga: sceny systemowej (DontDestroyOnLoad, buildIndex = -1) NIE wolno aktywowac.
        if (bootstrapScene.IsValid() && bootstrapScene.isLoaded && bootstrapScene.buildIndex >= 0)
            SceneManager.SetActiveScene(bootstrapScene);

        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene s = SceneManager.GetSceneAt(i);

            if (!s.isLoaded) continue;
            if (s == bootstrapScene) continue;
            if (s.buildIndex < 0) continue;                    // scena systemowa - nie ruszamy
            if (s.name == bootstrapScene.name) continue;        // zapasowe zabezpieczenie

            Debug.LogWarning($"Scena '{s.name}' byla otwarta obok Bootstrapa w edytorze - zamykam ja. " +
                             "Na przyszlosc: przed wcisnieciem Play zostaw w Hierarchii tylko Bootstrap.");

            yield return SceneManager.UnloadSceneAsync(s);
        }
    }

    // Glowna funkcja - wywoluje ja drzwi domku albo wejscie do jaskini
    public void GoTo(string locationScene, string spawnId)
    {
        if (IsTransitioning)
        {
            Debug.Log("Przejscie juz trwa - ignoruje.");
            return;
        }

        if (string.IsNullOrEmpty(locationScene))
        {
            Debug.LogError("Nie podano nazwy sceny docelowej!");
            return;
        }

        StartCoroutine(LoadRoutine(locationScene, spawnId, false));
    }

    private IEnumerator LoadRoutine(string locationScene, string spawnId, bool isFirstLoad)
    {
        IsTransitioning = true;
        SetPlayerFrozen(true);

        // 1. Ciemnosc
        if (fader != null && !isFirstLoad) yield return fader.FadeOut(fadeOutTime);
        else if (fader != null) fader.SetBlack();

        // 2. Wyrzucamy stara lokacje z pamieci
        if (!string.IsNullOrEmpty(CurrentLocation))
        {
            Scene old = SceneManager.GetSceneByName(CurrentLocation);
            if (old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
        }

        // 3. Wczytujemy nowa (o ile juz nie siedzi w pamieci)
        Scene existing = SceneManager.GetSceneByName(locationScene);
        if (!existing.isLoaded)
            yield return SceneManager.LoadSceneAsync(locationScene, LoadSceneMode.Additive);

        Scene loaded = SceneManager.GetSceneByName(locationScene);
        if (!loaded.IsValid() || !loaded.isLoaded)
        {
            Debug.LogError($"Nie udalo sie wczytac sceny '{locationScene}'. " +
                           "Czy dodales ja w File -> Build Settings?");
            IsTransitioning = false;
            SetPlayerFrozen(false);
            if (fader != null) yield return fader.FadeIn(fadeInTime);
            yield break;
        }

        // Wazne: nowe obiekty (np. wyrzucone przedmioty) maja powstawac w lokacji,
        // a nie w scenie Bootstrap, ktora nigdy sie nie czysci.
        SceneManager.SetActiveScene(loaded);
        CurrentLocation = locationScene;

        // Zadania typu "dotrzyj do jaskini"
        QuestManager.ReportLocation(locationScene);

        // 4. Stawiamy gracza na wlasciwym punkcie
        yield return null; // jedna klatka, by obiekty sceny zdazyly sie obudzic
        PlacePlayer(spawnId);

        // 5. Rozjasniamy
        if (fader != null) yield return fader.FadeIn(fadeInTime);

        SetPlayerFrozen(false);
        IsTransitioning = false;
    }

    // ===============================================================
    // SPRZATANIE PRZY POWROCIE DO MENU GLOWNEGO
    // Ten obiekt ma DontDestroyOnLoad, wiec NIE zniknie sam przy zmianie
    // sceny. Bez tego druga rozgrywka wystartowalaby ze starym menedzerem
    // i wskaznikiem na nieistniejaca juz lokacje.
    // ===============================================================
    public IEnumerator Shutdown()
    {
        StopAllCoroutines();

        if (!string.IsNullOrEmpty(CurrentLocation))
        {
            Scene loc = SceneManager.GetSceneByName(CurrentLocation);
            if (loc.isLoaded) yield return SceneManager.UnloadSceneAsync(loc);
        }

        CurrentLocation = null;
        IsTransitioning = false;
        instance = null;

        Destroy(gameObject);
    }

    private void PlacePlayer(string spawnId)
    {
        Transform p = GetPlayer();
        if (p == null)
        {
            Debug.LogError("Nie znaleziono gracza! Czy Player jest w scenie Bootstrap?");
            return;
        }

        // Pusty spawnId = pozycje ustawi zapis gry, nie szukamy punktu
        if (string.IsNullOrEmpty(spawnId)) return;

        LocationSpawnPoint target = LocationSpawnPoint.Find(spawnId);
        if (target == null)
        {
            Debug.LogWarning($"Brak punktu odrodzenia o ID '{spawnId}' w tej lokacji. " +
                             "Gracz zostaje tam, gdzie byl.");
            return;
        }

        Vector3 pos = target.transform.position;
        pos.z = p.position.z; // nie ruszamy glebokosci

        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = pos;          // teleport zgodny z fizyka
            rb.linearVelocity = Vector2.zero;
        }
        p.position = pos;

        // Kamera ma przeskoczyc natychmiast, a nie plynnie lecieć przez pol mapy
        SnapCamera(pos);
    }

    private void SnapCamera(Vector3 pos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 camPos = cam.transform.position;
        cam.transform.position = new Vector3(pos.x, pos.y, camPos.z);

        // Jesli uzywasz Cinemachine, odpal tez jego "teleport"
        // (dziala przez refleksje, wiec kod kompiluje sie takze bez Cinemachine)
        foreach (MonoBehaviour mb in cam.GetComponentsInChildren<MonoBehaviour>())
        {
            if (mb == null) continue;
            var method = mb.GetType().GetMethod("OnTargetObjectWarped");
            if (method != null) { /* opcjonalne - zostawione swiadomie puste */ }
        }
    }

    private Transform GetPlayer()
    {
        if (player != null) return player;
        if (PlayerStats.instance != null)
        {
            player = PlayerStats.instance.transform;
            return player;
        }

        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go.transform;
        return player;
    }

    private void SetPlayerFrozen(bool frozen)
    {
        // Przez UILock, a nie recznie - inaczej koniec przejscia miedzy lokacjami
        // odblokowalby ruch nawet wtedy, gdy otwarty jest ekwipunek.
        UILock.Set("Transition", frozen);

        Transform p = GetPlayer();
        if (p == null) return;

        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
        if (rb != null && frozen) rb.linearVelocity = Vector2.zero;
    }
}
