using System.Collections.Generic;
using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("Referencje")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("Collider ciosu. MUSI siedziec na INNYM obiekcie niz grafika - " +
             "inaczej odziedziczy jej obrot i skale. Zostaw puste, a skrypt utworzy go sam.")]
    public BoxCollider2D hitCollider;

    [Header("Odczucie Ciosu")]
    [Tooltip("Tempo ciecia. Stroma na poczatku = szybki swist i miekkie wyhamowanie.")]
    public AnimationCurve swingCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2.6f),
        new Keyframe(1f, 1f, 0.2f, 0f));

    [Tooltip("Chwilowe powiekszenie broni w polowie zamachu (0 = wylaczone).")]
    public float scalePunch = 0.18f;

    [Tooltip("Czy bron ma zanikac pod koniec ciecia?")]
    public bool fadeOut = true;
    [Range(0f, 1f)] public float fadeStartAt = 0.6f;

    [Header("Dzwiek")]
    [Tooltip("Odglos trafienia gra tylko dla PIERWSZEGO celu, " +
             "zeby ciecie w tlum nie zrobilo kakofonii.")]
    public bool hitSoundOncePerSwing = true;

    // --- dane ciosu ---
    private int damage;
    private float swingDuration;
    private float timer;
    private float startAngle;
    private float endAngle;

    private ItemData weaponData;
    private Transform graphicsTransform;
    private Transform hitboxTransform;
    private Vector3 graphicsBaseScale;
    private Color baseColor = Color.white;
    private bool playedHitSound;

    // Pamietamy TRAFIONE STWORZENIA, nie collidery - dzieki temu boss
    // z dwoma colliderami nie oberwie dwa razy jednym machnieciem.
    private readonly List<Creature> alreadyHit = new List<Creature>();

    public void Setup(ItemData weapon, int dmg, float duration, float angleToMouse)
    {
        weaponData = weapon;
        damage = dmg;
        swingDuration = Mathf.Max(0.05f, duration);
        timer = 0f;
        playedHitSound = false;

        PrepareTransforms();
        ApplyWeaponShape(weapon);

        // --- LUK CIECIA (szerokosc bierze sie z broni) ---
        float halfArc = Mathf.Max(5f, weapon.swingArc) * 0.5f;
        startAngle = angleToMouse - halfArc;
        endAngle = angleToMouse + halfArc;

        transform.rotation = Quaternion.Euler(0f, 0f, startAngle);

        // --- SWIST ---
        SoundManager.Play(weapon.swingSounds, weapon.soundVolume);

        Destroy(gameObject, swingDuration);
    }

    private void PrepareTransforms()
    {
        if (spriteRenderer != null)
        {
            graphicsTransform = spriteRenderer.transform;
            baseColor = spriteRenderer.color;
        }

        // Jesli collider nie zostal przypisany ALBO wisi na tym samym obiekcie
        // co grafika, tworzymy dla niego osobne dziecko.
        bool needsOwnObject = hitCollider == null ||
                              (graphicsTransform != null && hitCollider.transform == graphicsTransform);

        if (needsOwnObject)
        {
            if (hitCollider != null) Destroy(hitCollider);

            GameObject hitboxObj = new GameObject("Hitbox");
            hitboxObj.transform.SetParent(transform, false);
            hitboxObj.layer = gameObject.layer;

            hitCollider = hitboxObj.AddComponent<BoxCollider2D>();
            hitCollider.isTrigger = true;

            // Przekazujemy zdarzenia triggera z dziecka do tego skryptu
            hitboxObj.AddComponent<MeleeHitboxRelay>().owner = this;
        }
        else
        {
            hitCollider.isTrigger = true;

            if (hitCollider.transform != transform &&
                hitCollider.GetComponent<MeleeHitboxRelay>() == null)
            {
                hitCollider.gameObject.AddComponent<MeleeHitboxRelay>().owner = this;
            }
        }

        hitboxTransform = hitCollider.transform;
    }

    private void ApplyWeaponShape(ItemData weapon)
    {
        // --- GRAFIKA ---
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = weapon.icon;

            float scale = Mathf.Max(0.05f, weapon.weaponVisualScale);
            graphicsBaseScale = Vector3.one * scale;

            graphicsTransform.localPosition = new Vector3(weapon.weaponReach, 0f, 0f);
            graphicsTransform.localRotation = Quaternion.Euler(0f, 0f, weapon.weaponSpriteAngle);
            graphicsTransform.localScale = graphicsBaseScale;
        }

        // --- HITBOX ---
        // Osobny obiekt, BEZ obrotu grafiki i BEZ jej skali,
        // wiec podane w ItemData jednostki to realne jednostki swiata.
        if (hitboxTransform != null && hitboxTransform != transform)
        {
            hitboxTransform.localPosition = new Vector3(weapon.weaponReach, 0f, 0f);
            hitboxTransform.localRotation = Quaternion.identity;
            hitboxTransform.localScale = Vector3.one;
        }

        hitCollider.offset = Vector2.zero;
        hitCollider.size = new Vector2(
            Mathf.Max(0.05f, weapon.weaponLength),
            Mathf.Max(0.05f, weapon.weaponWidth));
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / swingDuration);

        // Krzywa zamiast prostego Lerpa - stad wrazenie zamachu, a nie przesuwania
        float eased = swingCurve.Evaluate(t);
        float currentAngle = Mathf.LerpAngle(startAngle, endAngle, eased);
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);

        if (graphicsTransform == null) return;

        // Lekkie "pchniecie" skali w polowie ciecia
        if (scalePunch > 0f)
        {
            float punch = Mathf.Sin(t * Mathf.PI) * scalePunch;
            graphicsTransform.localScale = graphicsBaseScale * (1f + punch);
        }

        // Zanikanie na koncu
        if (fadeOut && spriteRenderer != null && t > fadeStartAt)
        {
            float fadeProgress = Mathf.InverseLerp(fadeStartAt, 1f, t);
            Color c = baseColor;
            c.a = Mathf.Lerp(baseColor.a, 0f, fadeProgress);
            spriteRenderer.color = c;
        }
    }

    // Wolane przez MeleeHitboxRelay z obiektu Hitbox
    public void HandleHit(Collider2D collision)
    {
        Creature creature = collision.GetComponentInParent<Creature>();
        if (creature == null || creature.IsDead) return;
        if (alreadyHit.Contains(creature)) return;

        alreadyHit.Add(creature);

        bool isCrit = Random.Range(0f, 100f) < PlayerStats.instance.critChance;
        int finalDmg = isCrit
            ? Mathf.RoundToInt(damage * PlayerStats.instance.critDamageMultiplier)
            : damage;

        Vector2 hitDir = (creature.transform.position - transform.position).normalized;
        creature.TakeDamage(finalDmg, isCrit, hitDir);

        // --- ODGLOS TRAFIENIA ---
        if (weaponData != null && (!hitSoundOncePerSwing || !playedHitSound))
        {
            SoundManager.Play(weaponData.hitSounds, weaponData.soundVolume);
            playedHitSound = true;
        }
    }

    // Gdyby collider mimo wszystko siedzial na tym samym obiekcie co skrypt
    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleHit(collision);
    }
}

// Maly pomocnik: obiekt Hitbox przekazuje swoje zdarzenia do glownego skryptu.
public class MeleeHitboxRelay : MonoBehaviour
{
    [HideInInspector] public PlayerMeleeAttack owner;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (owner != null) owner.HandleHit(collision);
    }
}