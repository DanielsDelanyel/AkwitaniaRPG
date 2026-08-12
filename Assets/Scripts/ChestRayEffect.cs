using System.Collections;
using UnityEngine;

// Promienie swiatla bijace ze srodka skrzyni.
// Powies to na prefabie, ktorego dzieckiem jest sprite promieni (gwiazda / wachlarz promieni).
public class ChestRayEffect : MonoBehaviour
{
    [Header("Grafika")]
    [Tooltip("Sprite promieni. Jesli zostawisz puste, skrypt poszuka go w dzieciach.")]
    public SpriteRenderer rayRenderer;

    [Tooltip("Opcjonalny drugi sprite - miekka poswiata pod promieniami.")]
    public SpriteRenderer glowRenderer;

    [Header("Ruch")]
    public float rotationSpeed = 20f;   // stopnie na sekunde
    public float startScale = 0.2f;
    public float peakScale = 1f;
    public float endScale = 1.15f;

    [Header("Czasy")]
    public float fadeInTime = 0.25f;
    public float holdTime = 0.9f;
    public float fadeOutTime = 0.8f;

    [Header("Sila")]
    [Range(0f, 1f)] public float maxAlpha = 0.85f;
    [Tooltip("Mnozniki jasnosci dla kolejnych rzadkosci - legendarna moze swiecic mocniej.")]
    public float commonIntensity = 0.6f;
    public float rareIntensity = 0.75f;
    public float epicIntensity = 0.9f;
    public float legendaryIntensity = 1f;

    private Color baseColor;
    private float intensity = 1f;

    void Awake()
    {
        if (rayRenderer == null) rayRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Play(ItemRarity rarity)
    {
        baseColor = RarityUtils.GetColor(rarity);
        intensity = GetIntensity(rarity);
        StartCoroutine(Lifecycle());
    }

    private float GetIntensity(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return commonIntensity;
            case ItemRarity.Rare: return rareIntensity;
            case ItemRarity.Epic: return epicIntensity;
            case ItemRarity.Legendary: return legendaryIntensity;
            default: return 1f;
        }
    }

    void Update()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    private IEnumerator Lifecycle()
    {
        float target = maxAlpha * intensity;

        // 1. Rozblysk
        yield return Animate(startScale, peakScale, 0f, target, fadeInTime);

        // 2. Utrzymanie
        float t = 0f;
        while (t < holdTime)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // 3. Wygaszanie z lekkim rozszerzeniem
        yield return Animate(peakScale, endScale, target, 0f, fadeOutTime);

        Destroy(gameObject);
    }

    private IEnumerator Animate(float scaleFrom, float scaleTo, float alphaFrom, float alphaTo, float time)
    {
        float t = 0f;
        time = Mathf.Max(0.01f, time);

        while (t < 1f)
        {
            t += Time.deltaTime / time;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

            transform.localScale = Vector3.one * Mathf.Lerp(scaleFrom, scaleTo, eased);
            ApplyAlpha(Mathf.Lerp(alphaFrom, alphaTo, eased));

            yield return null;
        }
    }

    private void ApplyAlpha(float alpha)
    {
        Color c = baseColor;
        c.a = alpha;

        if (rayRenderer != null) rayRenderer.color = c;

        if (glowRenderer != null)
        {
            Color g = baseColor;
            g.a = alpha * 0.6f;
            glowRenderer.color = g;
        }
    }
}
