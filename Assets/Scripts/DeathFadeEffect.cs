using System.Collections;
using UnityEngine;

// Po smierci stworzenie traci kolory, a potem powoli znika.
// Wymaga, by w Creature ODZNACZYC "Destroy On Death" - to ten skrypt
// decyduje, kiedy obiekt ma zniknac.
[RequireComponent(typeof(Creature))]
public class DeathFadeEffect : MonoBehaviour
{
    [Header("Faza 1: Szarzenie")]
    public float grayscaleTime = 0.8f;
    [Tooltip("Docelowy kolor. Ciemniejszy szary = bardziej 'martwy' wyglad.")]
    public Color deadColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Faza 2: Znikanie")]
    public float holdTime = 0.6f;   // chwila zastygniecia w szarosci
    public float fadeOutTime = 1.2f;

    [Header("Faza 3: Opadanie (opcjonalne)")]
    [Tooltip("O ile jednostek cialo osunie sie w dol podczas znikania.")]
    public float sinkDistance = 0.2f;

    [Header("Co wylaczyc od razu po smierci")]
    [Tooltip("Skrypty AI, ktore maja przestac dzialac. Zostaw puste - znajdzie sam.")]
    public MonoBehaviour[] scriptsToDisable;

    private Creature creature;
    private SpriteRenderer[] renderers;
    private Color[] startColors;

    void Awake()
    {
        creature = GetComponent<Creature>();
        creature.onDeath += HandleDeath;

        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) startColors[i] = renderers[i].color;
    }

    void OnDestroy()
    {
        if (creature != null) creature.onDeath -= HandleDeath;
    }

    private void HandleDeath(Creature c)
    {
        // Zatrzymujemy AI, fizyke i kolizje - trup nie goni i nie blokuje drogi
        DisableBehaviours();
        StartCoroutine(FadeRoutine());
    }

    private void DisableBehaviours()
    {
        // Recznie wskazane skrypty
        if (scriptsToDisable != null)
        {
            foreach (MonoBehaviour mb in scriptsToDisable)
            {
                if (mb != null) mb.enabled = false;
            }
        }

        // Automatycznie: wszystko, co steruje ruchem
        BossController boss = GetComponent<BossController>();
        if (boss != null) boss.enabled = false;

        CreatureAI ai = GetComponent<CreatureAI>();
        if (ai != null) ai.enabled = false;

        CreatureWander wander = GetComponent<CreatureWander>();
        if (wander != null) wander.enabled = false;

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.speed = 0f; // zastyga w ostatniej klatce

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            if (col != null) col.enabled = false;
        }
    }

    private IEnumerator FadeRoutine()
    {
        // --- FAZA 1: kolory blakna do szarosci ---
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, grayscaleTime);
            float eased = Mathf.Clamp01(t);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;

                Color target = new Color(
                    deadColor.r, deadColor.g, deadColor.b, startColors[i].a);

                renderers[i].color = Color.Lerp(startColors[i], target, eased);
            }
            yield return null;
        }

        // --- FAZA 2: chwila ciszy ---
        yield return new WaitForSeconds(holdTime);

        // --- FAZA 3: rozplyniecie sie ---
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.down * sinkDistance;

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, fadeOutTime);
            float eased = Mathf.Clamp01(t);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;

                Color c = renderers[i].color;
                c.a = Mathf.Lerp(startColors[i].a, 0f, eased);
                renderers[i].color = c;
            }

            if (sinkDistance > 0f) transform.position = Vector3.Lerp(startPos, endPos, eased);

            yield return null;
        }

        Destroy(gameObject);
    }
}
