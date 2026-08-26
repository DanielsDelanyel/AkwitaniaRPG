using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Czarna plansza przykrywajaca ekran na czas ladowania lokacji.
// Powies to na czarnym Image rozciagnietym na caly Canvas (Raycast Target: OFF).
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    private CanvasGroup group;

    // Pobiera CanvasGroup na zadanie.
    // Awake() NIE odpala sie na obiekcie wylaczonym w Hierarchii, wiec 'group'
    // bywalo puste i SetBlack() rzucalo NullReferenceException.
    private CanvasGroup Group
    {
        get
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
                if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            }
            return group;
        }
    }

    void Awake()
    {
        Group.blocksRaycasts = false;
        Group.interactable = false;
        Group.alpha = 0f;
    }

    public void SetBlack()
    {
        Group.alpha = 1f;
        Group.blocksRaycasts = true;
    }

    public void SetClear()
    {
        Group.alpha = 0f;
        Group.blocksRaycasts = false;
    }

    public IEnumerator FadeOut(float duration)
    {
        Group.blocksRaycasts = true; // blokujemy klikanie w trakcie przejscia
        yield return Fade(Group.alpha, 1f, duration);
    }

    public IEnumerator FadeIn(float duration)
    {
        yield return Fade(Group.alpha, 0f, duration);
        Group.blocksRaycasts = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            Group.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            // unscaledDeltaTime - dziala nawet przy zatrzymanym czasie (pauza)
            t += Time.unscaledDeltaTime / duration;
            Group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t));
            yield return null;
        }

        Group.alpha = to;
    }
}