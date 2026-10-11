using System.Collections;
using TMPro;
using UnityEngine;

// KOMUNIKAT NAD GLOWA GRACZA - krotki napis, ktory wyskakuje, chwile stoi,
// delikatnie unosi sie w gore i znika. Uzywany np. przez LocationEntrance,
// gdy gracz probuje uciec do budynku w trakcie walki
// ("Nie mozesz teraz tam wejsc - najpierw zgub albo pokonaj przeciwnika!").
//
// Dziala jak pojedynczy, wspoldzielony "slot" na komunikat (jeden na raz) -
// to NIE jest system numerkow obrazen (od tego jest osobny DamagePopup),
// tylko krotkie teksty informacyjne/ostrzegawcze.
//
// PODPIECIE W UNITY:
//   1. W Hierarchy, jako DZIECKO obiektu Player, stworz pusty obiekt
//      "NoticeAnchor" i ustaw mu pozycje np. (0, 1.6, 0) - kawalek NAD glowa
//      gracza (wyzej niz pasek zycia/dymek dialogowy, jesli takie masz).
//   2. Pod "NoticeAnchor" dodaj (Add Component) zwykly komponent
//      "TextMeshPro - Text" (UWAGA: NIE "TextMeshPro - Text (UI)" - chcemy
//      wersje bez Canvasu, dzialajaca w swiecie gry, dokladnie tak jak
//      CreatureHoverInfo.nameLevelText).
//        - Alignment: Center/Center
//        - Font Size: dobierz pod swoja rozdzielczosc (np. 3-4 dla pixel-artu)
//        - Kolor: np. jaskrawy zolty/czerwony, zeby ostrzezenie rzucalo sie w oczy
//   3. Ten skrypt (PlayerNotice) powies na obiekcie "NoticeAnchor" (albo na
//      samym Playerze - wtedy w polu "Text" wskaz ten TextMeshPro recznie).
//   4. Przypisz w Inspektorze pole "Text" na ten TextMeshPro.
//   5. Gotowe - z dowolnego skryptu wywoluj: PlayerNotice.Show("Tresc...");
public class PlayerNotice : MonoBehaviour
{
    public static PlayerNotice instance;

    [Header("Tekst")]
    [Tooltip("Zwykly TextMeshPro (world space) - NIE 'TextMeshPro - Text (UI)'.")]
    public TextMeshPro text;

    [Header("Czas")]
    [Tooltip("Jak dlugo napis stoi w pelni widoczny, zanim zacznie znikac.")]
    public float holdTime = 1.6f;
    [Tooltip("Czas plynnego znikania po holdTime.")]
    public float fadeTime = 0.5f;

    [Header("Ruch")]
    [Tooltip("O ile jednostek napis unosi sie w gore w trakcie calego wyswietlania.")]
    public float floatDistance = 0.3f;

    [Header("Ograniczenie Spamu")]
    [Tooltip("Ten sam komunikat, wywolany ponownie w tym oknie czasu (np. gracz " +
             "trzyma klawisz pod zablokowanymi drzwiami), nie restartuje animacji " +
             "od zera - po prostu przedluza to, co juz widac na ekranie.")]
    public float sameMessageCooldown = 0.5f;

    private Coroutine activeRoutine;
    private Vector3 baseLocalPos;
    private string lastMessage;
    private float lastShowTime = -999f;

    void Awake()
    {
        instance = this;

        if (text != null)
        {
            baseLocalPos = text.transform.localPosition;
            SetAlpha(0f);
        }
        else
        {
            Debug.LogWarning("PlayerNotice: nie przypisano pola 'Text' w Inspektorze - " +
                             "komunikaty nie beda sie wyswietlac.");
        }
    }

    // Wolaj to z dowolnego miejsca w kodzie: PlayerNotice.Show("...");
    public static void Show(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        if (instance == null)
        {
            Debug.LogWarning($"PlayerNotice: brak instancji w scenie - dodaj ten " +
                             $"komponent do gracza. Komunikat pominiety: \"{message}\"");
            return;
        }

        instance.ShowInternal(message);
    }

    private void ShowInternal(string message)
    {
        if (text == null) return;

        // Ten sam komunikat, wywolany za czesto - nie restartujemy animacji,
        // tylko przedluzamy biezacy pokaz (patrz komentarz przy polu wyzej).
        if (message == lastMessage
            && activeRoutine != null
            && Time.unscaledTime - lastShowTime < sameMessageCooldown)
        {
            lastShowTime = Time.unscaledTime;
            return;
        }

        lastMessage = message;
        lastShowTime = Time.unscaledTime;

        text.text = message;

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(ShowRoutine());
    }

    // unscaledDeltaTime - komunikat ma dzialac tak samo, nawet gdyby kiedys
    // odpalil sie w trakcie pauzy (Time.timeScale = 0), tak jak inne animacje
    // fade w tym projekcie (ScreenFader, PauseMenuUI.saveStatusTimer).
    private IEnumerator ShowRoutine()
    {
        text.transform.localPosition = baseLocalPos;
        SetAlpha(1f);

        float totalTime = holdTime + fadeTime;
        float t = 0f;

        // Faza 1: pelna widocznosc, napis juz zaczyna powoli unosic sie w gore
        while (t < holdTime)
        {
            t += Time.unscaledDeltaTime;
            ApplyFloatPosition(t, totalTime);
            yield return null;
        }

        // Faza 2: zanikanie, unoszenie leci dalej do konca
        float fadeStart = t;
        while (t - fadeStart < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float fadeProgress = Mathf.Clamp01((t - fadeStart) / fadeTime);

            SetAlpha(1f - fadeProgress);
            ApplyFloatPosition(t, totalTime);

            yield return null;
        }

        SetAlpha(0f);
        activeRoutine = null;
    }

    private void ApplyFloatPosition(float elapsed, float totalTime)
    {
        float progress = Mathf.Clamp01(elapsed / totalTime);
        text.transform.localPosition = baseLocalPos + Vector3.up * floatDistance * progress;
    }

    private void SetAlpha(float alpha)
    {
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }
}
