using UnityEngine;
using UnityEngine.EventSystems;

// PRZECIAGANIE (PAN) I PRZYBLIZANIE (ZOOM) DLA PANELU DRZEWKA UMIEJETNOSCI.
//
// Powies na obiekcie "Content" - tym samym, ktory trzyma awatar gracza oraz
// wszystkie wezly/strzalki drzewka (SkillTreeUI.treeContent).
//
// WAZNE: zeby przeciaganie/scrollowanie dzialalo w KAZDYM miejscu okna (nie
// tylko nad samymi ikonkami), rodzic tego obiektu (samo okno drzewka) musi miec
// Image (moze byc niemal przezroczysty, np. alpha 0.01) z "Raycast Target"
// zaznaczonym - inaczej klikniecie/scroll w puste miejsce nie trafi w nic i
// UI go zignoruje. Nie dodawaj wbudowanego ScrollRect na ten sam obiekt -
// ten skrypt robi to samo recznie, zeby latwiej bylo trzymac awatar dokladnie
// na srodku i swobodnie ustawic zakres zoomu.
[RequireComponent(typeof(RectTransform))]
public class PanZoomContent : MonoBehaviour, IDragHandler, IScrollHandler
{
    [Header("Zoom")]
    public float minZoom = 0.5f;
    public float maxZoom = 2f;
    public float zoomStep = 0.1f;

    [Header("Pan")]
    [Tooltip("Jak daleko (w pikselach, przy zoomie x1) wolno odjechac od pozycji startowej. " +
             "0 = bez ograniczen.")]
    public float maxPanDistance = 0f;

    private RectTransform rt;
    private Canvas parentCanvas;
    private Vector2 homePosition; // pozycja startowa - zwykle taka, ze awatar wypada na srodku okna

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        homePosition = rt.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float scaleFactor = (parentCanvas != null && parentCanvas.scaleFactor > 0f) ? parentCanvas.scaleFactor : 1f;

        rt.anchoredPosition += eventData.delta / scaleFactor;
        ClampPan();
    }

    public void OnScroll(PointerEventData eventData)
    {
        float delta = eventData.scrollDelta.y;
        if (Mathf.Approximately(delta, 0f)) return;

        float newScale = Mathf.Clamp(rt.localScale.x + delta * zoomStep, minZoom, maxZoom);
        rt.localScale = new Vector3(newScale, newScale, 1f);
    }

    // Wolaj przy otwieraniu panelu, jesli chcesz, zeby zawsze startowal wysrodkowany i bez zoomu.
    public void ResetView()
    {
        rt.anchoredPosition = homePosition;
        rt.localScale = Vector3.one;
    }

    private void ClampPan()
    {
        if (maxPanDistance <= 0f) return;

        Vector2 offset = rt.anchoredPosition - homePosition;
        offset = Vector2.ClampMagnitude(offset, maxPanDistance);
        rt.anchoredPosition = homePosition + offset;
    }
}
