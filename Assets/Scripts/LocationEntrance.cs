using UnityEngine;
using UnityEngine.EventSystems;

// Powies to na domku, jaskini albo na drzwiach w srodku (jako wyjscie).
// Dziala tak samo jak Twoja skrzynia: podejdz -> podswietla sie -> kliknij.
public class LocationEntrance : MonoBehaviour
{
    [Header("Dokad prowadzi")]
    [Tooltip("Nazwa sceny docelowej - dokladnie jak w Build Settings.")]
    public string targetLocation = "House_Interior";

    [Tooltip("ID punktu odrodzenia W SCENIE DOCELOWEJ, np. 'FrontDoor'.")]
    public string targetSpawnId = "FrontDoor";

    [Header("Interakcja")]
    public float interactionRange = 2f;
    public bool allowKeyEnter = true;
    public KeyCode enterKey = KeyCode.E;

    [Header("Podswietlenie (opcjonalne)")]
    [Tooltip("Dziecko: kopia sprite'a lub ramka, rysowana pod obiektem.")]
    public SpriteRenderer highlightRenderer;
    public Color highlightColor = new Color(1f, 0.95f, 0.6f);
    public float highlightMinAlpha = 0.1f;
    public float highlightMaxAlpha = 0.45f;
    public float highlightPulseSpeed = 2.5f;

    [Header("Dymek / Napis (opcjonalne)")]
    [Tooltip("Np. mala ikonka klawisza E nad drzwiami.")]
    public GameObject prompt;

    [Header("Blokada (np. zamkniete drzwi)")]
    public bool isLocked = false;
    [Tooltip("Klucz w ekwipunku, ktory otwiera te drzwi. Zostaw puste = nie da sie otworzyc.")]
    public ItemData requiredKey;
    public bool consumeKey = false;
    public string lockedMessage = "Drzwi sa zamkniete.";

    private Transform playerTransform;
    private bool isPlayerClose;

    void Start()
    {
        if (highlightRenderer != null) highlightRenderer.gameObject.SetActive(false);
        if (prompt != null) prompt.SetActive(false);
    }

    void Update()
    {
        if (playerTransform == null) FindPlayer();
        if (playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        bool wasClose = isPlayerClose;
        isPlayerClose = distance <= interactionRange;

        UpdateVisuals();

        if (allowKeyEnter && isPlayerClose && Input.GetKeyDown(enterKey)) TryEnter();
    }

    private void FindPlayer()
    {
        if (PlayerStats.instance != null)
        {
            playerTransform = PlayerStats.instance.transform;
            return;
        }
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) playerTransform = go.transform;
    }

    private void UpdateVisuals()
    {
        if (prompt != null && prompt.activeSelf != isPlayerClose) prompt.SetActive(isPlayerClose);

        if (highlightRenderer == null) return;

        if (!isPlayerClose)
        {
            if (highlightRenderer.gameObject.activeSelf) highlightRenderer.gameObject.SetActive(false);
            return;
        }

        if (!highlightRenderer.gameObject.activeSelf) highlightRenderer.gameObject.SetActive(true);

        float pulse = (Mathf.Sin(Time.time * highlightPulseSpeed) + 1f) * 0.5f;
        Color c = highlightColor;
        c.a = Mathf.Lerp(highlightMinAlpha, highlightMaxAlpha, pulse);
        highlightRenderer.color = c;
    }

    private void OnMouseDown()
    {
        // Nie reagujemy przez otwarty ekwipunek, sklep czy dialog
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (!isPlayerClose)
        {
            Debug.Log("Musisz podejsc blizej!");
            return;
        }

        TryEnter();
    }

    public void TryEnter()
    {
        if (LocationManager.instance == null)
        {
            Debug.LogError("Brak LocationManagera! Czy scena Bootstrap jest zaladowana?");
            return;
        }

        if (LocationManager.instance.IsTransitioning) return;

        // Blokada na klucz
        if (isLocked)
        {
            if (requiredKey == null || !PlayerHasKey())
            {
                Debug.Log(lockedMessage);
                return;
            }

            if (consumeKey && InventoryUI.instance != null)
                InventoryUI.instance.RemoveItem(requiredKey);

            isLocked = false;
        }

        LocationManager.instance.GoTo(targetLocation, targetSpawnId);
    }

    private bool PlayerHasKey()
    {
        if (InventoryUI.instance == null) return false;

        foreach (ItemData item in InventoryUI.instance.GetAllItems())
        {
            if (item == requiredKey) return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isLocked ? new Color(1f, 0.4f, 0.3f) : new Color(0.4f, 0.8f, 1f);
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}