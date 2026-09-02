using UnityEngine;

// CENTRALNY ODTWARZACZ DZWIEKOW GRACZA: kroki, dash, skok.
//
// Kroki dzialaja SAME - ten skrypt czyta predkosc z Rigidbody2D i sam decyduje,
// kiedy zagrac kolejny krok. Nie musisz nic wolac z zewnatrz, wystarczy podpiac
// klipy w Inspectorze.
//
// Dash i skok to zdarzenia JEDNORAZOWE (nie da sie ich wyczytac z samej predkosci),
// wiec wywolaj PlayerAudio.TryPlayDash() / TryPlayJump() z Twojego skryptu ruchu,
// dokladnie w momencie w ktorym dash/skok sie zaczyna.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAudio : MonoBehaviour
{
    public static PlayerAudio instance;

    [Header("Kroki")]
    [Tooltip("Wrzuc kilka wariantow - gra losuje, zeby nie brzmialo monotonnie.")]
    public AudioClip[] footstepSounds;

    [Tooltip("Ponizej tej predkosci (jednostki/s) gracz jest uznawany za stojacego w miejscu.")]
    public float minSpeedForFootsteps = 0.1f;

    [Tooltip("Odstep miedzy krokami PRZY predkosci rownej Reference Speed (sekundy).")]
    public float baseStepInterval = 0.35f;

    [Tooltip("Predkosc ruchu, dla ktorej Base Step Interval jest dokladny. Wolniejszy ruch " +
             "(np. skradanie) sam wydluza odstep, szybszy (sprint) - skraca.")]
    public float referenceSpeed = 4f;

    [Range(0f, 1f)] public float footstepVolume = 0.6f;

    [Header("Dash / Skok")]
    [Tooltip("Wywolaj PlayerAudio.TryPlayDash() z Twojego skryptu ruchu w momencie startu dasha.")]
    public AudioClip[] dashSounds;

    [Tooltip("Wywolaj PlayerAudio.TryPlayJump() z Twojego skryptu ruchu w momencie skoku.")]
    public AudioClip[] jumpSounds;

    [Range(0f, 1f)] public float actionVolume = 0.8f;

    private Rigidbody2D rb;
    private float stepTimer;

    void Awake()
    {
        instance = this;
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        if (footstepSounds == null || footstepSounds.Length == 0 || rb == null) return;

        float speed = rb.linearVelocity.magnitude;
        if (speed < minSpeedForFootsteps)
        {
            stepTimer = 0f; // stoimy w miejscu - kolejny krok zagra od razu po ruszeniu
            return;
        }

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            SoundManager.Play(footstepSounds, footstepVolume);

            // Szybszy ruch = czestsze kroki. Klamrujemy, zeby sprint nie zamienil sie w karabin maszynowy.
            float speedRatio = Mathf.Clamp(speed / Mathf.Max(0.01f, referenceSpeed), 0.5f, 2.5f);
            stepTimer = baseStepInterval / speedRatio;
        }
    }

    public void PlayDash()
    {
        SoundManager.Play(dashSounds, actionVolume);
    }

    public void PlayJump()
    {
        SoundManager.Play(jumpSounds, actionVolume);
    }

    // Wygodne statyczne skroty - bezpieczne do wolania nawet bez trzymania referencji do instancji.
    public static void TryPlayDash()
    {
        if (instance != null) instance.PlayDash();
    }

    public static void TryPlayJump()
    {
        if (instance != null) instance.PlayJump();
    }
}
