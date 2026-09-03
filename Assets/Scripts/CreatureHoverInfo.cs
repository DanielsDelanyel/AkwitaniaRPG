using UnityEngine;

// DYMEK "NAZWA + POZIOM + PASEK ZDROWIA" NAD GLOWA - ukryty na starcie, pokazuje
// sie TYLKO gdy kursor myszy najedzie na to stworzenie, znika gdy kursor zjedzie.
//
// WYMAGANIA:
//   - zwykly Collider2D na TYM SAMYM obiekcie co ten skrypt (kazde stworzenie
//     juz go ma - to ten sam collider, ktorego uzywa np. CreatureAI do wykrywania
//     zderzenia z graczem) - to on generuje zdarzenia OnMouseEnter/OnMouseExit.
//   - dziecko z calą zawartoscia dymka (tekst + pasek), na starcie WYLACZONE
//     w Hierarchy (ten skrypt samo je wlacza/wylacza w runtime).
[RequireComponent(typeof(Creature))]
public class CreatureHoverInfo : MonoBehaviour
{
    [Header("Zawartosc dymka (dziecko postaci)")]
    [Tooltip("Caly obiekt dymka - ten skrypt sam go WLACZA/WYLACZA po najechaniu myszka. " +
             "W Hierarchy zostaw go WYLACZONYM (odznaczony checkbox przy nazwie).")]
    public GameObject infoRoot;

    [Tooltip("Zwykly TextMeshPro (NIE 'TextMeshPro - Text (UI)') - dziala w swiecie gry " +
             "bez Canvasu, dokladnie jak SpriteRenderer.")]
    public TMPro.TextMeshPro nameLevelText;

    [Tooltip("Transform PASKA WYPELNIENIA zdrowia (kolorowy prostokat, NIE tlo). " +
             "WAZNE: Sprite tego paska musi miec Pivot ustawiony na 'Left' (w ustawieniach " +
             "importu grafiki), inaczej pasek bedzie sie kurczyl z obu stron naraz zamiast " +
             "tylko z prawej.")]
    public Transform healthFillBar;

    private Creature creature;

    void Start()
    {
        creature = GetComponent<Creature>();
        if (infoRoot != null) infoRoot.SetActive(false);
    }

    void Update()
    {
        if (infoRoot == null || !infoRoot.activeSelf) return;

        // Stworzenie zdazylo umrzec, zanim kursor zdazyl zjechac - chowamy dymek recznie.
        if (creature.IsDead)
        {
            infoRoot.SetActive(false);
            return;
        }

        if (nameLevelText != null)
            nameLevelText.text = $"{creature.creatureName} Lv.{creature.level}";

        if (healthFillBar != null && creature.maxHealth > 0)
        {
            float pct = Mathf.Clamp01((float)creature.currentHealth / creature.maxHealth);
            Vector3 scale = healthFillBar.localScale;
            scale.x = pct;
            healthFillBar.localScale = scale;
        }
    }

    // Wymaga Collider2D na tym obiekcie i Camera gdziekolwiek w scenie - Unity samo
    // wysyla te zdarzenia, bez pisania wlasnego Raycasta.
    private void OnMouseEnter()
    {
        if (creature != null && creature.IsDead) return;
        if (infoRoot != null) infoRoot.SetActive(true);
    }

    private void OnMouseExit()
    {
        if (infoRoot != null) infoRoot.SetActive(false);
    }
}
