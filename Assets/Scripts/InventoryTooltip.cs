using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class InventoryTooltip : MonoBehaviour
{
    // ===============================================================
    // ODPORNY SINGLETON
    // Jesli obiekt byl wylaczony w Hierarchii, Awake() nigdy sie nie odpalil
    // i 'instance' bylo null -> najechanie myszka wywalalo gre.
    // Teraz w razie potrzeby odnajdujemy siebie sami, takze wsrod wylaczonych.
    // ===============================================================
    private static InventoryTooltip _instance;
    public static InventoryTooltip instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<InventoryTooltip>(FindObjectsInactive.Include);
                if (_instance != null && !_instance.gameObject.activeSelf)
                    _instance.gameObject.SetActive(true);
            }
            return _instance;
        }
    }

    [Header("Komponenty UI")]
    public TextMeshProUGUI headerField;
    public TextMeshProUGUI contentField;
    public TextMeshProUGUI rarityField;
    public Image rarityIconImage;
    public TextMeshProUGUI priceField;
    public TextMeshProUGUI statsField;
    public TextMeshProUGUI typeField;
    public TextMeshProUGUI weightField;

    [Header("Grafiki Rzadkosci")]
    public Sprite commonIcon;
    public Sprite rareIcon;
    public Sprite epicIcon;
    public Sprite legendaryIcon;

    [Header("Ustawienia")]
    public float offsetX = 15f;
    public float offsetY = -15f;

    [Tooltip("Zaznacz, jesli okno ma Vertical Layout Group + Content Size Fitter.")]
    public bool autoResize = false;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    // Tryb "Szczegoly" - okno stoi w miejscu, dopoki gracz go nie zamknie
    private bool isPinned;
    public bool IsPinned { get { return isPinned; } }

    void Awake()
    {
        _instance = this;
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetVisible(false);
    }

    void Update()
    {
        // Przypiete okno nie ucieka za myszka
        if (isPinned)
        {
            // Escape obsluguje UIEscapeHandler - tu reagujemy tylko na klikniecie,
            // zeby jedno nacisniecie klawisza nie zamknelo dwoch okien naraz.
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) HideTooltip();
            return;
        }

        if (canvasGroup == null || canvasGroup.alpha <= 0f) return; // ukryty - nie liczymy pozycji

        FollowMouse();
    }

    private void FollowMouse()
    {
        Vector2 mousePos = Input.mousePosition;

        // Przy prawej krawedzi ekranu okno rysuje sie w lewo, przy gornej - w dol
        float pivotX = mousePos.x / Screen.width > 0.5f ? 1f : 0f;
        float pivotY = mousePos.y / Screen.height > 0.5f ? 1f : 0f;

        rectTransform.pivot = new Vector2(pivotX, pivotY);

        float currentOffsetX = pivotX == 1f ? -Mathf.Abs(offsetX) : Mathf.Abs(offsetX);
        float currentOffsetY = pivotY == 1f ? -Mathf.Abs(offsetY) : Mathf.Abs(offsetY);

        transform.position = mousePos + new Vector2(currentOffsetX, currentOffsetY);
    }

    // Chowanie przez przezroczystosc, NIE przez SetActive - obiekt musi zyc,
    // inaczej singleton znowu przestanie dzialac.
    private void SetVisible(bool visible)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = false; // tooltip NIGDY nie lapie klikniec
        canvasGroup.interactable = false;
    }

    public void ShowTooltip(ItemData item)
    {
        if (isPinned) return; // przypiete szczegoly maja pierwszenstwo
        Fill(item);
        SetVisible(true);
    }

    // Wolane przez "Szczegoly" w menu kontekstowym
    public void ShowPinned(ItemData item, Vector2 screenPosition)
    {
        Fill(item);
        SetVisible(true);

        isPinned = true;
        rectTransform.pivot = new Vector2(0f, 1f);
        transform.position = ClampToScreen(screenPosition);
    }

    private Vector3 ClampToScreen(Vector2 pos)
    {
        Vector2 size = rectTransform.sizeDelta;
        float x = Mathf.Clamp(pos.x, 0f, Mathf.Max(0f, Screen.width - size.x));
        float y = Mathf.Clamp(pos.y, Mathf.Min(size.y, Screen.height), Screen.height);
        return new Vector3(x, y, 0f);
    }

    private void Fill(ItemData item)
    {
        if (item == null) return;

        if (headerField != null)
        {
            headerField.text = item.itemName;
            headerField.color = GetRarityColor(item.rarity);
        }

        if (rarityField != null)
        {
            rarityField.text = GetRarityName(item.rarity);
            rarityField.color = GetRarityColor(item.rarity);
        }

        if (rarityIconImage != null)
        {
            Sprite iconToShow = GetRaritySprite(item.rarity);
            rarityIconImage.sprite = iconToShow;
            rarityIconImage.enabled = iconToShow != null;
        }

        SetField(typeField, item.GetTypeName());

        if (statsField != null)
        {
            if (item.HasAnyStats()) SetField(statsField, item.GetStatsDescription());
            else statsField.gameObject.SetActive(false);
        }

        SetField(contentField, item.description);

        if (weightField != null)
        {
            if (item.weight > 0f) SetField(weightField, $"Waga: {item.weight}");
            else weightField.gameObject.SetActive(false);
        }

        if (priceField != null)
        {
            // GetEffectivePrice() zamiast price - udany egzemplarz jest wart wiecej
            if (item.price > 0) SetField(priceField, $"Cena: {item.GetEffectivePrice()} G");
            else priceField.gameObject.SetActive(false);
        }

        if (autoResize && rectTransform != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private void SetField(TextMeshProUGUI field, string text)
    {
        if (field == null) return;

        bool hasText = !string.IsNullOrWhiteSpace(text);
        field.gameObject.SetActive(hasText);
        if (hasText) field.text = text;
    }

    public void HideTooltip()
    {
        isPinned = false;
        SetVisible(false);
        if (rarityIconImage != null) rarityIconImage.enabled = false;
    }

    // Uzywane przy zamykaniu ekwipunku - chowa takze przypiete okno
    public void ForceHide()
    {
        isPinned = false;
        SetVisible(false);
    }

    string GetRarityName(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return "Pospolity";
            case ItemRarity.Rare: return "Rzadki";
            case ItemRarity.Epic: return "Epicki";
            case ItemRarity.Legendary: return "Legendarny";
            default: return "";
        }
    }

    Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Color.white;
            case ItemRarity.Rare: return Color.cyan;
            case ItemRarity.Epic: return new Color(0.6f, 0f, 1f);
            case ItemRarity.Legendary: return Color.yellow;
            default: return Color.white;
        }
    }

    Sprite GetRaritySprite(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return commonIcon;
            case ItemRarity.Rare: return rareIcon;
            case ItemRarity.Epic: return epicIcon;
            case ItemRarity.Legendary: return legendaryIcon;
            default: return null;
        }
    }
}