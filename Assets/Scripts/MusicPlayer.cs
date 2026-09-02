using System.Collections;
using UnityEngine;

// Muzyka tla, ktora przezywa zmiane scen i plynnie przechodzi miedzy utworami.
// Powies to na pustym obiekcie w scenie MainMenu.
[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer instance;

    [Header("Utwory")]
    public AudioClip menuTheme;
    public AudioClip gameTheme;

    // ===============================================================
    // NOWE: playlista muzyki swiata - gra losowy utwor, a po jego zakonczeniu
    // sama losuje kolejny. Docelowo (kolejny krok) da sie to zastapic muzyka
    // konkretnego obszaru - patrz PlayWorldPlaylist() / PlayAreaTheme() ponizej.
    // ===============================================================
    [Header("Muzyka Swiata (losowa playlista podczas eksploracji)")]
    [Tooltip("Utwory, ktore graja losowo podczas przemierzania swiata. Zawsze jeden na raz - " +
             "po zakonczeniu gra sama losuje kolejny z tej listy.")]
    public AudioClip[] worldThemes;

    [Tooltip("Jesli w liscie jest wiecej niz jeden utwor, ten sam nie zagra dwa razy z rzedu.")]
    public bool avoidImmediateRepeat = true;

    [Header("Przejscia")]
    public float fadeTime = 1.2f;

    [Tooltip("Zaznacz, jesli muzyka ma grac od razu po uruchomieniu.")]
    public bool playMenuOnStart = true;

    private AudioSource source;
    private Coroutine fadeRoutine;

    private bool isPlayingWorldPlaylist;
    private AudioClip lastWorldTrack;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    void Start()
    {
        if (playMenuOnStart && menuTheme != null) Play(menuTheme);
    }

    void Update()
    {
        // Playlista NIE zapetla pojedynczego utworu (patrz Play(..., loop:false) ponizej),
        // wiec kiedy skonczy sie grac, sami losujemy kolejny.
        if (isPlayingWorldPlaylist && fadeRoutine == null && source != null && !source.isPlaying)
        {
            PlayRandomWorldTrack();
        }
    }

    public void PlayMenuTheme()
    {
        isPlayingWorldPlaylist = false;
        Play(menuTheme);
    }

    public void PlayGameTheme()
    {
        isPlayingWorldPlaylist = false;
        Play(gameTheme);
    }

    // NOWE: wywolaj to zamiast/obok PlayGameTheme(), gdy gracz wchodzi do swiata gry -
    // od tego momentu muzyka sama losuje kolejne utwory z World Themes bez konca.
    public void PlayWorldPlaylist()
    {
        if (worldThemes == null || worldThemes.Length == 0)
        {
            Debug.LogWarning("MusicPlayer: World Themes jest puste - nie ma z czego losowac.");
            return;
        }

        isPlayingWorldPlaylist = true;
        PlayRandomWorldTrack();
    }

    public void StopWorldPlaylist()
    {
        isPlayingWorldPlaylist = false;
    }

    // Miejsce pod przyszla mechanike "muzyka po wejsciu w obszar": docelowo obszar wywola
    // np. StopWorldPlaylist() + Play(areaTheme, loop: true), a przy wyjsciu wroci PlayWorldPlaylist().
    private void PlayRandomWorldTrack()
    {
        if (worldThemes == null || worldThemes.Length == 0) return;

        AudioClip next = PickRandomWorldTrack();
        lastWorldTrack = next;

        // loop:false - dzieki temu source.isPlaying naturalnie spadnie na false po koncu utworu
        // i Update() zdazy wylosowac kolejny.
        Play(next, loop: false);
    }

    private AudioClip PickRandomWorldTrack()
    {
        if (worldThemes.Length == 1) return worldThemes[0];

        AudioClip pick;
        int guard = 0;
        do
        {
            pick = worldThemes[Random.Range(0, worldThemes.Length)];
            guard++;
        }
        while (avoidImmediateRepeat && pick == lastWorldTrack && guard < 8);

        return pick;
    }

    public void Play(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;
        if (source.clip == clip && source.isPlaying) return; // juz gra to samo

        source.loop = loop;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(CrossFade(clip));
    }

    public void StopMusic()
    {
        isPlayingWorldPlaylist = false;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutAndStop());
    }

    // Odswieza glosnosc po zmianie w opcjach
    public void RefreshVolume()
    {
        if (source != null && fadeRoutine == null) source.volume = GameSettings.MusicVolume;
    }

    private IEnumerator CrossFade(AudioClip next)
    {
        // Wyciszamy stary utwor
        if (source.isPlaying) yield return FadeVolume(source.volume, 0f);

        source.clip = next;
        source.Play();

        // Wprowadzamy nowy
        yield return FadeVolume(0f, GameSettings.MusicVolume);
        fadeRoutine = null;
    }

    private IEnumerator FadeOutAndStop()
    {
        yield return FadeVolume(source.volume, 0f);
        source.Stop();
        fadeRoutine = null;
    }

    private IEnumerator FadeVolume(float from, float to)
    {
        float t = 0f;
        float time = Mathf.Max(0.01f, fadeTime);

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / time; // dziala takze przy zatrzymanym czasie
            source.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t));
            yield return null;
        }

        source.volume = to;
    }
}
