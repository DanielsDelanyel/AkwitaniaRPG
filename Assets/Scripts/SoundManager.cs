using UnityEngine;

// Prosty odtwarzacz efektow dzwiekowych.
//
// Dlaczego nie AudioSource.PlayClipAtPoint?
// Tamta metoda tworzy i niszczy obiekt przy KAZDYM dzwieku, a przy szybkiej
// walce to dziesiatki obiektow na sekunde. Tu mamy stala pule zrodel.
//
// Druga zaleta: losowa wysokosc tonu. Ten sam swist odtworzony 20 razy z rzedu
// brzmi sztucznie - drobna zmiana pitcha calkowicie to naprawia.
public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Pula Zrodel")]
    [Tooltip("Ile dzwiekow moze grac naraz. 12 spokojnie wystarcza.")]
    public int poolSize = 12;

    [Header("Glosnosc")]
    [Range(0f, 1f)] public float masterVolume = 1f;

    [Header("Urozmaicenie")]
    [Tooltip("Zakres losowej wysokosci tonu. 1 = bez zmian.")]
    public Vector2 pitchRange = new Vector2(0.92f, 1.08f);

    private AudioSource[] pool;
    private int nextIndex;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        pool = new AudioSource[Mathf.Max(1, poolSize)];
        for (int i = 0; i < pool.Length; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // dzwiek 2D - w grze z gory nie ma sensu przestrzenny
            pool[i] = src;
        }
    }

    // --- WERSJE STATYCZNE: wolaj SoundManager.Play(...) z dowolnego miejsca ---

    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (instance != null) instance.PlayClip(clip, volume);
        else if (clip != null) AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, volume);
    }

    // Losuje jeden klip z tablicy - dzieki temu ciosy nie brzmia identycznie
    public static void Play(AudioClip[] clips, float volume = 1f)
    {
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        Play(clip, volume);
    }

    // --- WLASCIWE ODTWARZANIE ---

    public void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (clip == null || pool == null) return;

        AudioSource src = GetFreeSource();
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume * masterVolume);
        src.pitch = Random.Range(pitchRange.x, pitchRange.y);
        src.Play();
    }

    private AudioSource GetFreeSource()
    {
        // Najpierw szukamy takiego, ktory nic nie gra
        for (int i = 0; i < pool.Length; i++)
        {
            int idx = (nextIndex + i) % pool.Length;
            if (!pool[idx].isPlaying)
            {
                nextIndex = (idx + 1) % pool.Length;
                return pool[idx];
            }
        }

        // Wszystkie zajete - przerywamy najstarszy
        AudioSource src = pool[nextIndex];
        nextIndex = (nextIndex + 1) % pool.Length;
        return src;
    }
}