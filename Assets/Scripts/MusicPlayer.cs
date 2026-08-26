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

    [Header("Przejscia")]
    public float fadeTime = 1.2f;

    [Tooltip("Zaznacz, jesli muzyka ma grac od razu po uruchomieniu.")]
    public bool playMenuOnStart = true;

    private AudioSource source;
    private Coroutine fadeRoutine;

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

    public void PlayMenuTheme() { Play(menuTheme); }
    public void PlayGameTheme() { Play(gameTheme); }

    public void Play(AudioClip clip)
    {
        if (clip == null) return;
        if (source.clip == clip && source.isPlaying) return; // juz gra to samo

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(CrossFade(clip));
    }

    public void StopMusic()
    {
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