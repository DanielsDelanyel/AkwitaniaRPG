using UnityEngine;

// PASEK IKON STATUSOW NAD SPRITEM - wersja PROSTA: zadnego Canvasu, zadnego UI,
// tylko zwykle SpriteRenderery (dokladnie tak jak grafika calej reszty gry).
// Dziala identycznie dla gracza i kazdego przeciwnika, bo szuka
// StatusEffectController u RODZICA (GetComponentInParent) - jeden skrypt na
// wszystkich.
//
// NOWE: ikonki NIE siedza na sztywnych, wlasnych miejscach (przez co np. Podpalenie
// zawsze ladowaloby w tym samym miejscu, nawet gdy inne statusy sa akurat puste).
// Zamiast tego kazda klatka liczy, ILE statusow jest TERAZ aktywnych, i rozstawia
// TYLKO widoczne ikonki w rzedzie WYSRODKOWANYM na X = 0 tego obiektu - stad
// wrazenie, ze pasek "rosnie od srodka" w miare doklejania kolejnych statusow.
// Kolejnosc w rzedzie jest zawsze taka sama (Poison/Burn/Bleed/Slow/Stun), zeby
// caly uklad nie "skakal" losowo przy kazdej zmianie.
//
// PODPIECIE (patrz pelna instrukcja w rozmowie) - w skrocie:
//   1. Puste dziecko postaci "StatusIcons", pozycja kawalek NAD sprite'em.
//   2. Pod nim 5 pustych dzieci, kazde z komponentem Sprite Renderer i
//      WLASNA ikonka wpisana na stale we wlasciwosc "Sprite".
//   3. Ten skrypt na obiekcie "StatusIcons", przypisz mu tych 5 SpriteRenderer.
public class CreatureStatusIndicator : MonoBehaviour
{
    [Header("Ikony (kolejnosc = kolejnosc w rzedzie)")]
    public SpriteRenderer poisonIcon;
    public SpriteRenderer burnIcon;
    public SpriteRenderer bleedIcon;
    public SpriteRenderer slowIcon;
    public SpriteRenderer stunIcon;

    [Header("Ulozenie")]
    [Tooltip("Odstep miedzy srodkami sasiadujacych ikon (jednostki swiata).")]
    public float iconSpacing = 0.35f;

    private StatusEffectController statusEffects;

    // Dwie rownolegle tablice zamiast krotek - prostszy, bardziej czytelny kod.
    private SpriteRenderer[] orderedIcons;
    private StatusEffectType[] orderedTypes;

    void Awake()
    {
        orderedIcons = new[] { poisonIcon, burnIcon, bleedIcon, slowIcon, stunIcon };
        orderedTypes = new[]
        {
            StatusEffectType.Poison, StatusEffectType.Burn, StatusEffectType.Bleed,
            StatusEffectType.Slow, StatusEffectType.Stun
        };
    }

    void Start()
    {
        // WAZNE: ikonki chowamy na starcie BEZWARUNKOWO, zanim jeszcze sprawdzimy,
        // czy w ogole znalezlismy StatusEffectController. Bez tego kazda ikonka,
        // ktora w Scenie ma domyslnie zaznaczone "widoczna" (Sprite Renderer
        // wlaczony), swieci sie od pierwszej klatki i NIGDY sie nie wylacza,
        // jesli np. rodzic nie ma jeszcze dodanego StatusEffectController.
        HideAll();

        statusEffects = GetComponentInParent<StatusEffectController>();

        if (statusEffects == null)
        {
            string parentName = transform.parent != null ? transform.parent.name : "(brak rodzica)";
            Debug.LogWarning($"{name}: nie znalazlem StatusEffectController u rodzica " +
                              $"({parentName}) - dodaj ten komponent na glownym obiekcie " +
                              "postaci (obok Creature/PlayerStats). Do tego czasu pasek " +
                              "statusow zostanie schowany.");
        }
    }

    private void HideAll()
    {
        foreach (SpriteRenderer icon in orderedIcons)
        {
            if (icon != null) icon.enabled = false;
        }
    }

    void Update()
    {
        if (statusEffects == null) return;

        // 1. Ustal, ktore ikony sa TERAZ aktywne (w stalej kolejnosci typow),
        // od razu wlaczajac/wylaczajac ich widocznosc.
        int activeCount = 0;
        for (int i = 0; i < orderedIcons.Length; i++)
        {
            if (orderedIcons[i] == null) continue;

            bool active = statusEffects.IsActive(orderedTypes[i]);
            orderedIcons[i].enabled = active;
            if (active) activeCount++;
        }

        // 2. Rozstaw TYLKO aktywne ikony w rzedzie wysrodkowanym na X = 0.
        float startX = -(activeCount - 1) * iconSpacing * 0.5f;
        int placed = 0;
        for (int i = 0; i < orderedIcons.Length; i++)
        {
            SpriteRenderer icon = orderedIcons[i];
            if (icon == null || !icon.enabled) continue;

            Vector3 pos = icon.transform.localPosition;
            pos.x = startX + placed * iconSpacing;
            icon.transform.localPosition = pos;
            placed++;
        }
    }
}
