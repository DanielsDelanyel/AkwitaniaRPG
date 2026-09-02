using System.Collections.Generic;
using UnityEngine;

// UMIEJETNOSC MNICHA: Wir Powietrza (male tornado).
//
// - Po rzuceniu leci WLASNYM torem w strone kursora - gracz go juz nie prowadzi.
// - POPRAWKA: to NIE sam sprite ma sie obracac w miejscu (tak bylo wczesniej -
//   wygladalo dziwnie na wydluzonej, skosnej grafice tornada). Zamiast tego
//   zakrzywiamy sam WEKTOR PREDKOSCI w czasie (patrz spiralTurnSpeedDegreesPerSecond),
//   dzieki czemu caly obiekt fizycznie KRAZY PO PLANSZY PO SPIRALACH/PETLACH,
//   a grafika zostaje w jednej, stalej orientacji (bez wirowania w miejscu).
// - Odbija sie od przeszkod oznaczonych tagiem "Obstacle"/"Wall" (tak samo jak
//   strzaly/Projectile.cs), zamiast przez nie przelatywac.
// - KAZDY przeciwnik moze zostac trafiony WIELOKROTNIE, ale dopiero po tym jak
//   wir FIZYCZNIE opusci jego collider (OnTriggerExit2D). Dopoki go dotyka,
//   nie doda kolejnych obrazen w kolko - to celowa roznica wzgledem iskier
//   z rozdzki (FireSparkProjectile), ktore traktuja kazdego wroga tylko raz
//   na cale zycie pocisku.
//
// WYMAGANIA W EDYTORZE: Rigidbody2D na tym obiekcie ustawiony jako Kinematic
// (Gravity Scale niepotrzebny), Collider2D jako Is Trigger. Przeszkody/sciany
// w scenie musza miec collider Is Trigger + tag "Obstacle" lub "Wall" (tak jak
// juz maja dla istniejacych pociskow).
[RequireComponent(typeof(Rigidbody2D))]
public class WindVortexEffect : ActiveSkillEffect
{
    [Header("Ruch po spirali (dziala bez animacji)")]
    [Tooltip("Odznaczone = STALE skrecanie (dawne zachowanie, przewidywalna, rowna spirala). " +
             "Zaznaczone = skret ZMIENNY w czasie (patrz pola nizej) - kazdy rzut wyglada inaczej " +
             "i tor jest mniej przewidywalny.")]
    public bool randomizeTurn = false;

    [Tooltip("Uzywane, gdy Randomize Turn jest WYLACZONE. O ile stopni na sekunde skreca " +
             "sam TOR LOTU (wektor predkosci). Wieksza wartosc = ciasniejsze petle. " +
             "Znak (+/-) okresla kierunek (dodatnia = przeciwnie do wskazowek zegara, ujemna = zgodnie).")]
    public float spiralTurnSpeedDegreesPerSecond = 140f;

    [Tooltip("Uzywane, gdy Randomize Turn jest WLACZONE. Skret w kazdej chwili miesci sie " +
             "gdzies pomiedzy tymi dwiema wartosciami (stopni/sek) - ujemna dolna granica " +
             "pozwala wirowi skrecac w OBIE strony, a nie tylko coraz mocniej w jedna.")]
    public float minTurnSpeed = -220f;
    public float maxTurnSpeed = 220f;

    [Tooltip("Jak szybko zmienia sie 'nastrój' skretu w czasie, gdy Randomize Turn jest wlaczone. " +
             "Mala wartosc = powolne, plynne fale skretu (dalej wyglada jak spirala, tylko mniej " +
             "regularna). Duza wartosc = szybkie, chaotyczne miotanie sie wiru.")]
    public float turnNoiseFrequency = 0.6f;

    // Losowy przesuniecie wejscia Perlin Noise, ustawiane raz w Setup() - dzieki temu
    // kilka wirow rzuconych jeden po drugim NIE skreca identycznie w tym samym momencie.
    private float noiseSeed;

    [Header("Dzwieki/Efekty (opcjonalne)")]
    public AudioClip[] hitSounds;
    [Range(0f, 1f)] public float soundVolume = 0.8f;
    public GameObject hitEffectPrefab;

    private Rigidbody2D rb;
    private int damage;
    private float speed;

    // Wrogowie, ktorych wir AKTUALNIE dotyka. To NIE jest "juz trafieni na zawsze"
    // (jak alreadyHit w FireSparkProjectile) - usuwani stad w OnTriggerExit2D,
    // wiec ponowne wejscie w kolizje zawsze zadaje obrazenia od nowa.
    private readonly HashSet<Creature> currentlyTouching = new HashSet<Creature>();

    public override void Setup(int dmg, float duration, float flightSpeed, float aimAngleDegrees)
    {
        damage = Mathf.Max(1, dmg);
        speed = Mathf.Max(0.1f, flightSpeed);

        rb = GetComponent<Rigidbody2D>();
        noiseSeed = Random.Range(0f, 1000f);

        float rad = aimAngleDegrees * Mathf.Deg2Rad;
        Vector2 flyDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        rb.linearVelocity = flyDirection * speed;

        Destroy(gameObject, duration);
    }

    void FixedUpdate()
    {
        if (rb == null || rb.linearVelocity.sqrMagnitude < 0.0001f) return;

        // SEDNO SPIRALI: co klatke fizyki obracamy sam wektor predkosci o kawalek -
        // stala predkosc + skrecanie kierunku = tor lotu w ksztalcie petli/spirali
        // po calej planszy. Grafika (transform.rotation) w ogole tu nie wchodzi w gre.
        float turnSpeed;
        if (randomizeTurn)
        {
            // Perlin Noise zamiast czystego Random.Range() w kazdej klatce - ten drugi
            // dawalby brzydkie, "trzesace sie" szarpniecia. Noise plynnie przechodzi
            // miedzy wartosciami, wiec tor dalej wyglada jak spirala/petla, tylko
            // nieregularna i za kazdym razem inna (dzieki noiseSeed).
            float noise = Mathf.PerlinNoise(noiseSeed, Time.time * turnNoiseFrequency);
            turnSpeed = Mathf.Lerp(minTurnSpeed, maxTurnSpeed, noise);
        }
        else
        {
            turnSpeed = spiralTurnSpeedDegreesPerSecond;
        }

        float turn = turnSpeed * Time.fixedDeltaTime;
        rb.linearVelocity = (Vector2)(Quaternion.Euler(0f, 0f, turn) * rb.linearVelocity);

        // Odbicia (Vector2.Reflect) i zaokraglenia fizyki moga z czasem lekko
        // zmienic dlugosc wektora predkosci - pilnujemy, zeby wir zawsze leciał
        // ze STALA predkoscia ustawiona w Setup().
        rb.linearVelocity = rb.linearVelocity.normalized * speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // --- ODBICIE OD PRZESZKODY ---
        if (collision.CompareTag("Obstacle") || collision.CompareTag("Wall"))
        {
            // Trigger collidery nie daja gotowego punktu/normalnej kolizji (tak jak
            // OnCollisionEnter2D), wiec przyblizamy normalna jako kierunek od
            // najblizszego punktu na przeszkodzie do srodka wiru.
            Vector2 closest = collision.ClosestPoint(transform.position);
            Vector2 normal = (Vector2)transform.position - closest;

            // Wir trafil dokladnie w rog/naroznik - normalna wychodzi zerowa,
            // wiec awaryjnie odbijamy po prostu do tylu, zeby nie utknal w scianie.
            if (normal.sqrMagnitude < 0.0001f) normal = -rb.linearVelocity.normalized;
            normal.Normalize();

            rb.linearVelocity = Vector2.Reflect(rb.linearVelocity, normal).normalized * speed;
            return;
        }

        // --- TRAFIENIE PRZECIWNIKA ---
        Creature creature = collision.GetComponentInParent<Creature>();

        // Wir omija wlasne przyzwane stworzenia (Szkielety itd.), tak samo jak
        // strzaly i iskry z rozdzki.
        bool isFriendlySummon = creature != null && creature.GetComponent<SummonedCreature>() != null;

        if (creature != null && !creature.IsDead && !isFriendlySummon && !currentlyTouching.Contains(creature))
        {
            currentlyTouching.Add(creature);

            bool isCrit = Random.Range(0f, 100f) < PlayerStats.instance.critChance;
            int finalDmg = isCrit
                ? Mathf.RoundToInt(damage * PlayerStats.instance.critDamageMultiplier)
                : damage;

            Vector2 hitDir = (collision.transform.position - transform.position).normalized;
            creature.TakeDamage(finalDmg, isCrit, hitDir);

            SoundManager.Play(hitSounds, soundVolume);
            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, collision.transform.position, Quaternion.identity);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Wir "wyszedl" z kolizji tego przeciwnika - od teraz wolno go zranic ponownie.
        Creature creature = collision.GetComponentInParent<Creature>();
        if (creature != null) currentlyTouching.Remove(creature);
    }
}
