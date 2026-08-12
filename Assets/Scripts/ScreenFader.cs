using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Czarna plansza przykrywajaca ekran na czas ladowania lokacji.
// Powies to na czarnym Image rozciagnietym na caly Canvas (Raycast Target: OFF).
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    private CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;
    }

    public void SetBlack()
    {
        group.alpha = 1f;
        group.blocksRaycasts = true;
    }

    public void SetClear()
    {
        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    public IEnumerator FadeOut(float duration)
    {
        group.blocksRaycasts = true; // blokujemy klikanie w trakcie przejscia
        yield return Fade(group.alpha, 1f, duration);
    }

    public IEnumerator FadeIn(float duration)
    {
        yield return Fade(group.alpha, 0f, duration);
        group.blocksRaycasts = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            // unscaledDeltaTime - dziala nawet przy zatrzymanym czasie (pauza)
            t += Time.unscaledDeltaTime / duration;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t));
            yield return null;
        }

        group.alpha = to;
    }
}