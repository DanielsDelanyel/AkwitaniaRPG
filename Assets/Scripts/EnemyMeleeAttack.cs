using UnityEngine;

// CIECIE BRONIA PRZECIWNIKA.
// To lustrzane odbicie PlayerMeleeAttack - ta sama mechanika luku i hitboxa,
// ale trafia GRACZA, a nie stworzenia.
//
// Prefab budujesz tak samo jak gracza:
//   EnemySlash            <- ten skrypt
//     └── Graphics        <- SpriteRenderer (sprite podmienia skrypt)
// Hitbox powstaje sam jako osobne dziecko.
public class EnemyMeleeAttack : MonoBehaviour
{
    [Header("Referencje")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("Zostaw puste - skrypt utworzy hitbox sam, jako osobne dziecko.")]
    public BoxCollider2D hitCollider;

    [Header("Odczucie Ciosu")]
    public AnimationCurve swingCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2.6f),
        new Keyframe(1f, 1f, 0.2f, 0f));

    public float scalePunch = 0.18f;
    public bool fadeOut = true;
    [Range(0f, 1f)] public float fadeStartAt = 0.6f;

    // ===============================================================
    // DOPASOWANIE HITBOXA DO OSTRZA
    // Grafika broni jest obracana o weaponSpriteAngle, a collider zostawal
    // rownolegly do kierunku zamachu - stad rozjazd przy ikonach rysowanych
    // po skosie. Tryb FollowSprite naklada pudelko dokladnie na sprite.
    // ===============================================================
    public enum HitboxMode
    {
        FollowSprite,   // pudelko przyjmuje obrot i rozmiar grafiki (zalecane)
        Manual          // pudelko wedlug weaponLength i weaponWidth z ItemData
    }

    [Header("Hitbox")]
    public HitboxMode hitboxMode = HitboxMode.FollowSprite;

    [Tooltip("Ikony maja przezroczysty margines. 0.75 przycina pudelko " +
             "do samego ostrza. Dotyczy trybu FollowSprite.")]
    [Range(0.2f, 1.2f)] public float spriteFitScale = 0.75f;

    [Tooltip("Rysuje hitbox w Scene View podczas gry - do strojenia.")]
    public bool drawHitboxGizmo = true;

    [Header("Odrzut Gracza")]
    public float knockbackForce = 0f;

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

    // Gracz obrywa od jednego machniecia tylko RAZ
    private bool hasHitPlayer;

    // Czy AI faktycznie uruchomilo to ciecie?
    private bool wasSetUp;

    public void Setup(ItemData weapon, int dmg, float duration, float angleToTarget)
    {
        weaponData = weapon;
        damage = dmg;
        swingDuration = Mathf.Max(0.05f, duration);
        timer = 0f;
        hasHitPlayer = false;
        wasSetUp = true;

        PrepareTransforms();
        ApplyWeaponShape(weapon);

        float halfArc = Mathf.Max(5f, weapon != null ? weapon.swingArc : 65f) * 0.5f;
        startAngle = angleToTarget - halfArc;
        endAngle = angleToTarget + halfArc;

        transform.rotation = Quaternion.Euler(0f, 0f, startAngle);

        if (weapon != null) SoundManager.Play(weapon.swingSounds, weapon.soundVolume);

        Destroy(gameObject, swingDuration);
    }

    private void PrepareTransforms()
    {
        if (spriteRenderer != null)
        {
            graphicsTransform = spriteRenderer.transform;
            baseColor = spriteRenderer.color;
        }

        // Hitbox MUSI byc osobnym obiektem - inaczej odziedziczy obrot
        // i skale grafiki, przez co nie pokrywalby sie z ostrzem.
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

            hitboxObj.AddComponent<EnemyHitboxRelay>().owner = this;
        }
        else
        {
            hitCollider.isTrigger = true;

            if (hitCollider.transform != transform &&
                hitCollider.GetComponent<EnemyHitboxRelay>() == null)
            {
                hitCollider.gameObject.AddComponent<EnemyHitboxRelay>().owner = this;
            }
        }

        hitboxTransform = hitCollider.transform;
    }

    private void ApplyWeaponShape(ItemData weapon)
    {
        float reach = weapon != null ? weapon.weaponReach : 0.85f;
        float length = weapon != null ? weapon.weaponLength : 0.8f;
        float width = weapon != null ? weapon.weaponWidth : 0.28f;

        if (spriteRenderer != null && weapon != null)
        {
            spriteRenderer.sprite = weapon.icon;

            float scale = Mathf.Max(0.05f, weapon.weaponVisualScale);
            graphicsBaseScale = Vector3.one * scale;

            graphicsTransform.localPosition = new Vector3(reach, 0f, 0f);
            graphicsTransform.localRotation = Quaternion.Euler(0f, 0f, weapon.weaponSpriteAngle);
            graphicsTransform.localScale = graphicsBaseScale;
        }

        if (hitboxTransform == null || hitboxTransform == transform) return;

        hitboxTransform.localScale = Vector3.one;
        hitCollider.offset = Vector2.zero;

        bool canFollowSprite = hitboxMode == HitboxMode.FollowSprite
                               && spriteRenderer != null
                               && spriteRenderer.sprite != null;

        if (canFollowSprite)
        {
            FitHitboxToSprite(weapon);
            return;
        }

        // Tryb reczny: pudelko lezy wzdluz kierunku zamachu
        hitboxTransform.localPosition = new Vector3(reach, 0f, 0f);
        hitboxTransform.localRotation = Quaternion.identity;
        hitCollider.size = new Vector2(Mathf.Max(0.05f, length), Mathf.Max(0.05f, width));
    }

    // Naklada pudelko dokladnie na widoczne ostrze:
    // ta sama pozycja, ten sam obrot, rozmiar wziety z granic sprite'a.
    private void FitHitboxToSprite(ItemData weapon)
    {
        float visualScale = weapon != null ? Mathf.Max(0.05f, weapon.weaponVisualScale) : 1f;

        // Granice sprite'a sa juz w jednostkach swiata (uwzgledniaja PPU)
        Bounds spriteBounds = spriteRenderer.sprite.bounds;

        // Pudelko dziedziczy pozycje i obrot grafiki...
        hitboxTransform.localPosition = graphicsTransform.localPosition;
        hitboxTransform.localRotation = graphicsTransform.localRotation;

        // ...a rozmiar bierze z niej, przyciety o przezroczysty margines ikony
        Vector2 size = (Vector2)spriteBounds.size * visualScale * spriteFitScale;

        hitCollider.size = new Vector2(
            Mathf.Max(0.05f, size.x),
            Mathf.Max(0.05f, size.y));

        // Ikona rzadko jest wysrodkowana idealnie - korygujemy przesuniecie
        hitCollider.offset = (Vector2)spriteBounds.center * visualScale;
    }

    void Start()
    {
        // ZABEZPIECZENIE: jesli ktos przeciagnal ten prefab jako STALE dziecko
        // przeciwnika, Setup() nigdy sie nie wywola. Bez tego swingDuration
        // wynosi 0, a timer/0 daje NaN i obiekt wiruje w nieskonczonosc.
        if (!wasSetUp)
        {
            Debug.LogWarning($"'{name}': ten obiekt ciecia nie zostal poprawnie " +
                             "uruchomiony przez AI. Usun go z hierarchii przeciwnika - " +
                             "ciecie ma powstawac dopiero w trakcie walki.");
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!wasSetUp) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / swingDuration);

        float eased = swingCurve.Evaluate(t);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(startAngle, endAngle, eased));

        if (graphicsTransform == null) return;

        if (scalePunch > 0f)
        {
            float punch = Mathf.Sin(t * Mathf.PI) * scalePunch;
            graphicsTransform.localScale = graphicsBaseScale * (1f + punch);
        }

        if (fadeOut && spriteRenderer != null && t > fadeStartAt)
        {
            float fadeProgress = Mathf.InverseLerp(fadeStartAt, 1f, t);
            Color c = baseColor;
            c.a = Mathf.Lerp(baseColor.a, 0f, fadeProgress);
            spriteRenderer.color = c;
        }
    }

    public void HandleHit(Collider2D collision)
    {
        if (hasHitPlayer) return;
        if (!collision.CompareTag("Player")) return;

        PlayerStats ps = PlayerStats.instance;
        if (ps == null) return;

        hasHitPlayer = true;   // machniecie zuzyte, nawet jesli gracz je uniknal

        // Dash daje klatki nietykalnosci - TakeDamage sam to sprawdza,
        // ale chcemy tez pominac dzwiek i odrzut przy udanym uniku.
        if (ps.IsInvincible())
        {
            Debug.Log("Cios bandyty unikniety!");
            return;
        }

        Vector2 hitDir = ((Vector2)collision.transform.position - (Vector2)transform.position).normalized;
        ps.TakeDamage(damage, false, hitDir);

        if (weaponData != null) SoundManager.Play(weaponData.hitSounds, weaponData.soundVolume);

        if (knockbackForce > 0f)
        {
            Rigidbody2D prb = collision.GetComponent<Rigidbody2D>();
            if (prb != null) prb.AddForce(hitDir * knockbackForce, ForceMode2D.Impulse);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleHit(collision);
    }

    // Podglad hitboxa w Scene View - wlacz Gizmos, by go zobaczyc w trakcie gry
    void OnDrawGizmos()
    {
        if (!drawHitboxGizmo || hitCollider == null) return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);

        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(
            hitCollider.transform.TransformPoint(hitCollider.offset),
            hitCollider.transform.rotation,
            hitCollider.transform.lossyScale);

        Gizmos.DrawWireCube(Vector3.zero, hitCollider.size);
        Gizmos.matrix = old;
    }
}

// Przekazuje zdarzenia z obiektu Hitbox do glownego skryptu ciecia
public class EnemyHitboxRelay : MonoBehaviour
{
    [HideInInspector] public EnemyMeleeAttack owner;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (owner != null) owner.HandleHit(collision);
    }
}