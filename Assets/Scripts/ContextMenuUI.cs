using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContextMenuUI : MonoBehaviour
{
    // Ten sam odporny singleton co w tooltipie - dziala nawet,
    // gdy obiekt zostal wylaczony w Hierarchii.
    private static ContextMenuUI _instance;
    public static ContextMenuUI instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ContextMenuUI>(FindObjectsInactive.Include);
                if (_instance != null && !_instance.gameObject.activeSelf)
                    _instance.gameObject.SetActive(true);
            }
            return _instance;
        }
    }

    [Header("Okno")]
    [Tooltip("Panel z przyciskami (u Ciebie: MenuBox).")]
    public GameObject menuPanel;

    [Header("Przyciski - wszystkie opcjonalne")]
    [Tooltip("Uzyj / Zjedz / Zaloz - etykieta zmienia sie sama.")]
    public Button useButton;
    public TextMeshProUGUI useButtonText;

    [Tooltip("Dzieli stos na pol i bierze polowe na kursor.")]
    public Button splitButton;
    public TextMeshProUGUI splitButtonText;

    [Tooltip("Przypina okno z pelnym opisem przedmiotu.")]
    public Button detailsButton;
    public TextMeshProUGUI detailsButtonText;

    [Tooltip("Wyrzuca przedmiot na ziemie.")]
    public Button dropButton;
    public TextMeshProUGUI dropButtonText;

    [Tooltip("Zamyka menu bez akcji.")]
    public Button cancelButton;
    public TextMeshProUGUI cancelButtonText;

    [Header("Zachowanie")]
    [Tooltip("Zamykaj menu, gdy gracz kliknie gdziekolwiek poza nim.")]
    public bool closeOnOutsideClick = true;

    private InventorySlot currentSlot;
    private RectTransform panelRect;

    public bool IsOpen { get { return menuPanel != null && menuPanel.activeSelf; } }

    void Awake()
    {
        _instance = this;
        if (menuPanel != null) panelRect = menuPanel.GetComponent<RectTransform>();

        // WAZNE: podpinamy akcje Z KODU, a nie przez pole "On Click" w Inspektorze.
        // Dzieki temu nie da sie przypadkiem podac zlego argumentu
        // (tak jak w przypadku UseItem z pustym slotem, ktory wywalal gre).
        Wire(useButton, OnUseClicked);
        Wire(splitButton, OnSplitClicked);
        Wire(detailsButton, OnDetailsClicked);
        Wire(dropButton, OnDropClicked);
        Wire(cancelButton, CloseMenu);
    }

    private void Wire(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    void Start()
    {
        SetDefaultLabels();
        CloseMenu();
    }

    private void SetDefaultLabels()
    {
        if (splitButtonText != null) splitButtonText.text = "Podziel";
        if (detailsButtonText != null) detailsButtonText.text = "Szczegoly";
        if (dropButtonText != null) dropButtonText.text = "Wyrzuc";
        if (cancelButtonText != null) cancelButtonText.text = "Anuluj";
    }

    void Update()
    {
        if (!IsOpen) return;

        // Escape obsluguje UIEscapeHandler - tu tylko klikniecie poza menu,
        // zeby jedno nacisniecie klawisza nie zamknelo dwoch okien naraz.
        if (!closeOnOutsideClick) return;

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (panelRect == null) return;

            bool overMenu = RectTransformUtility.RectangleContainsScreenPoint(
                panelRect, Input.mousePosition, GetCanvasCamera());

            if (!overMenu) CloseMenu();
        }
    }

    private Camera GetCanvasCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        // Overlay nie uzywa kamery przy przeliczaniu punktow
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    public void OpenMenu(InventorySlot slot, Vector2 mousePos)
    {
        if (slot == null || slot.item == null) return;
        if (menuPanel == null)
        {
            Debug.LogError("ContextMenuUI: nie przypisano Menu Panel!");
            return;
        }

        currentSlot = slot;
        ItemData item = slot.item;

        menuPanel.SetActive(true);
        ConfigureButtons(slot, item);
        PlaceMenu(mousePos);
    }

    private void ConfigureButtons(InventorySlot slot, ItemData item)
    {
        // --- UZYJ: etykieta zalezna od tego, co trzymamy ---
        bool canUse = true;
        string useLabel;

        switch (item.itemType)
        {
            case ItemType.Consumable:
                useLabel = "Zjedz / Wypij";
                break;
            case ItemType.General:
            case ItemType.Gift:
                useLabel = "Uzyj";
                canUse = false; // nie ma czego zakladac ani zjadac
                break;
            default:
                useLabel = "Zaloz";
                break;
        }

        SetButton(useButton, useButtonText, canUse, useLabel);

        // --- PODZIEL: tylko stosy wieksze niz 1, i tylko z pusta reka ---
        bool handsFree = InventoryUI.instance == null || InventoryUI.instance.draggedItem == null;
        bool canSplit = slot.isBackpackSlot && item.isStackable && slot.amount > 1 && handsFree;

        int half = slot.amount / 2;
        SetButton(splitButton, splitButtonText, canSplit,
                  canSplit ? $"Podziel ({half})" : "Podziel");

        // --- SZCZEGOLY: zawsze dostepne ---
        SetButton(detailsButton, detailsButtonText, true, "Szczegoly");

        // --- WYRZUC: tylko z plecaka, i tylko gdy przedmiot ma model na ziemi ---
        bool canDrop = slot.isBackpackSlot && item.itemPrefab != null;
        SetButton(dropButton, dropButtonText, canDrop,
                  slot.amount > 1 ? $"Wyrzuc ({slot.amount})" : "Wyrzuc");

        SetButton(cancelButton, cancelButtonText, true, "Anuluj");
    }

    // Niedostepna opcja znika zamiast wisiec wyszarzona - menu jest wtedy krotsze
    private void SetButton(Button button, TextMeshProUGUI label, bool available, string text)
    {
        if (button == null) return;

        button.gameObject.SetActive(available);
        if (available && label != null) label.text = text;
    }

    // Pilnuje, by menu nie wyszlo poza ekran
    private void PlaceMenu(Vector2 mousePos)
    {
        if (panelRect == null)
        {
            menuPanel.transform.position = mousePos;
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

        Vector2 size = panelRect.rect.size * panelRect.lossyScale;

        // Domyslnie menu rozwija sie w prawo i w dol od kursora
        panelRect.pivot = new Vector2(
            mousePos.x + size.x > Screen.width ? 1f : 0f,
            mousePos.y - size.y < 0f ? 0f : 1f);

        menuPanel.transform.position = mousePos;
    }

    public void CloseMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        currentSlot = null;
    }

    // ===============================================================
    // AKCJE
    // ===============================================================

    public void OnUseClicked()
    {
        if (currentSlot != null && currentSlot.item != null && InventoryUI.instance != null)
            InventoryUI.instance.UseItem(currentSlot);

        CloseMenu();
    }

    public void OnSplitClicked()
    {
        if (currentSlot != null && InventoryUI.instance != null)
            InventoryUI.instance.SplitStack(currentSlot);

        CloseMenu();
    }

    public void OnDetailsClicked()
    {
        if (currentSlot == null || currentSlot.item == null) { CloseMenu(); return; }

        ItemData item = currentSlot.item;
        Vector2 pos = menuPanel != null ? (Vector2)menuPanel.transform.position : (Vector2)Input.mousePosition;

        CloseMenu();

        if (InventoryTooltip.instance != null)
            InventoryTooltip.instance.ShowPinned(item, pos);
    }

    public void OnDropClicked()
    {
        if (currentSlot != null && InventoryUI.instance != null)
            InventoryUI.instance.DropFromSlot(currentSlot);

        CloseMenu();
    }
}