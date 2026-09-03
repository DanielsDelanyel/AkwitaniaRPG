using System.Collections;
using UnityEngine;

// KOLOR POSWIATY PRZY TRAFIENIU - Normal to zwykle obrazenia fizyczne (miecz, luk,
// szarza), reszta to konkretny status zadajacy obrazenia w czasie (patrz
// StatusEffectController). Sam typ statusu Slow tez dostaje wlasny kolor (Frost),
// mimo ze nie zadaje obrazen - tam poswiate odpala samo NALOZENIE spowolnienia,
// nie tik obrazen (patrz StatusEffectController.ApplySlow).
public enum HitFlashColor
{
    Normal,
    Poison,
    Burn,
    Bleed,
    Frost
}

// WSPOLNY MECHANIZM MIGANIA PRZY TRAFIENIU - wydzielony z Creature, zeby PlayerStats
// mogl korzystac z DOKLADNIE TEGO SAMEGO kodu zamiast kopiowac go drugi raz. Dopnij
// ten komponent obok Creature (kazdy przeciwnik/boss) LUB obok PlayerStats (gracz) -
// obaj wywoluja Play() ze swojego TakeDamage().
//
// UWAGA TECHNICZNA (bez zmian od oryginalu): domyslny material sprite'ow tylko
// MNOZY kolor tekstury przez SpriteRenderer.color - ustawienie bialego WIZUALNIE
// NIC NIE ZMIENIA (mnozenie przez 1), dziala tylko czern (mnozenie przez 0).
// Dlatego prawdziwe kolorowe/biale miganie wymaga osobnego shadera, ktory PODMIENIA
// kolor zamiast go mnozyc - patrz SpriteHitFlash.shader i pole Hit Flash Material.
[DisallowMultipleComponent]
public class HitFlashController : MonoBehaviour
{
    [Tooltip("Material korzystajacy z shadera 'Custom/SpriteHitFlash'. Jeden wspolny " +
             "material wystarczy dla wszystkich - kazde miganie tworzy sobie wlasna, " +
             "tymczasowa kopie. Zostaw puste, zeby CALKOWICIE wylaczyc miganie.")]
    public Material hitFlashMaterial;

    [Tooltip("Ile sekund trwa KAZDA z trzech faz miniania. Cale miganie trwa 3x tyle.")]
    public float phaseDuration = 0.05f;

    [Header("Kolory Statusow (srodkowa faza migania)")]
    public Color poisonColor = new Color(0.25f, 0.95f, 0.3f);
    public Color burnColor = new Color(1f, 0.55f, 0.05f);
    public Color bleedColor = new Color(0.9f, 0.1f, 0.1f);
    public Color frostColor = new Color(0.35f, 0.75f, 1f);

    private SpriteRenderer[] flashRenderers;
    private Material[] flashOriginalMaterials;
    private Coroutine hitFlashRoutine;

    void Awake()
    {
        // Zbieramy WSZYSTKIE SpriteRenderery (np. osobna grafika + akcesoria),
        // zeby miganie objelo cala postac, a nie tylko jeden fragment.
        flashRenderers = GetComponentsInChildren<SpriteRenderer>();
        flashOriginalMaterials = new Material[flashRenderers.Length];
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            flashOriginalMaterials[i] = flashRenderers[i].sharedMaterial;
        }
    }

    // Bialo-czarne miganie dla zwyklych obrazen, kolorowo-czarne dla konkretnego
    // statusu. Restartuje sie od zera przy kazdym kolejnym wywolaniu (StopCoroutine),
    // zeby szybkie combo trafien nie nakladalo na siebie kilku sekwencji naraz.
    public void Play(HitFlashColor colorType = HitFlashColor.Normal)
    {
        if (hitFlashMaterial == null) return; // miganie wylaczone - nikt nie podpial materialu
        if (flashRenderers == null || flashRenderers.Length == 0) return;

        if (hitFlashRoutine != null) StopCoroutine(hitFlashRoutine);
        hitFlashRoutine = StartCoroutine(FlashSequence(BuildSequence(colorType)));
    }

    private Color[] BuildSequence(HitFlashColor type)
    {
        // Zwykle obrazenia: biel -> czern -> biel (jak dotychczas).
        if (type == HitFlashColor.Normal) return new[] { Color.white, Color.black, Color.white };

        // Status: czern -> KOLOR STATUSU -> czern.
        return new[] { Color.black, GetStatusColor(type), Color.black };
    }

    private Color GetStatusColor(HitFlashColor type)
    {
        switch (type)
        {
            case HitFlashColor.Poison: return poisonColor;
            case HitFlashColor.Burn: return burnColor;
            case HitFlashColor.Bleed: return bleedColor;
            case HitFlashColor.Frost: return frostColor;
            default: return Color.black;
        }
    }

    private IEnumerator FlashSequence(Color[] sequence)
    {
        // Swieza kopia materialu NA POTRZEBY TEGO JEDNEGO migniecia - dzieki temu
        // dwie postacie trafione w tej samej klatce nie nadpisuja sobie nawzajem
        // koloru na WSPOLNYM shared materiale.
        Material[] flashInstances = new Material[flashRenderers.Length];
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] == null) continue;
            flashInstances[i] = new Material(hitFlashMaterial);
            flashRenderers[i].material = flashInstances[i];
        }

        foreach (Color flashColor in sequence)
        {
            foreach (Material mat in flashInstances)
            {
                if (mat != null) mat.SetColor("_FlashColor", flashColor);
            }
            yield return new WaitForSeconds(phaseDuration);
        }

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null) flashRenderers[i].sharedMaterial = flashOriginalMaterials[i];
            if (flashInstances[i] != null) Destroy(flashInstances[i]);
        }

        hitFlashRoutine = null;
    }
}
