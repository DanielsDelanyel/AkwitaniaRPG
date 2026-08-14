using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryTooltip : MonoBehaviour
{
    public static InventoryTooltip instance;

    [Header("Komponenty UI")]
    public TextMeshProUGUI headerField;   // Nazwa
    public TextMeshProUGUI contentField;  // Opis fabularny
    public TextMeshProUGUI rarityField;   // Tekst rzadkosci
    public Image rarityIconImage;
    public TextMeshProUGUI priceField;

    [Header("NOWE POLA (utworz je w TooltipWindow)")]
    [Tooltip("Tu trafiaja bonusy: +5 Sila, Przywraca 20 pkt zdrowia itd.")]
    public TextMeshProUGUI statsField;

    [Tooltip("Opcjonalnie: typ przedmiotu, np. 'Bron jednoreczna'.")]
    public TextMeshProUGUI typeField;

    [Tooltip("Opcjonalnie: waga przedmiotu.")]
    public TextMeshProUGUI weightField;

    [Header("Grafiki Rzadkosci")]
    public Sprite commonIcon;
    public Sprite rareIcon;
    public Sprite epicIcon;
    public Sprite legendaryIcon;

    [Header("Ustawienia")]
    public float offsetX = 15f;
    public float offsetY = -15f;

    [Tooltip("Zaznacz, jesli TooltipWindow ma Vertical Layout Group + Content Size Fitter. " +
             "Okno bedzie sie wtedy samo kurczyc i rozciagac pod zawartosc.")]
    public bool autoResize = false;

    private RectTransform rectTransform;

    void Awake()
    {
        instance = this;
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (rarityIconImage != null) rarityIconImage.enabled = false;
        gameObject.SetActive(false);
    }

    void Update()
    {
        Vector2 mousePos = Input.mousePosition;

        float pivotX = mousePos.x / Screen.width > 0.5f ? 1f : 0f;
        float pivotY = mousePos.y / Screen.height > 0.5f ? 1f : 0f;

        rectTransform.pivot = new Vector2(pivotX, pivotY);

        float currentOffsetX = pivotX == 1f ? -Mathf.Abs(offsetX) : Mathf.Abs(offsetX);
        float currentOffsetY = pivotY == 1f ? -Mathf.Abs(offsetY) : Mathf.Abs(offsetY);

        transform.position = mousePos + new Vector2(currentOffsetX, currentOffsetY);
    }

    public void ShowTooltip(ItemData item)
    {
        if (item == null) return;

        gameObject.SetActive(true);

        // --- NAZWA (w kolorze rzadkosci - czytelniej niz sama nazwa na bialo) ---
        if (headerField != null)
        {
            headerField.text = item.itemName;
            headerField.color = GetRarityColor(item.rarity);
        }

        // --- RZADKOSC ---
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

        // --- TYP PRZEDMIOTU ---
        SetField(typeField, item.GetTypeName());

        // --- STATYSTYKI: TU DZIEJE SIE CALA MAGIA ---
        // Klucz z healAmount = 0 i zerowymi bonusami -> pole znika calkowicie.
        // Mikstura z healAmount = 20 -> "Przywraca 20 pkt zdrowia".
        if (statsField != null)
        {
            if (item.HasAnyStats()) SetField(statsField, item.GetStatsDescription());
            else statsField.gameObject.SetActive(false);
        }

        // --- OPIS FABULARNY (chowamy, jesli pusty) ---
        SetField(contentField, item.description);

        // --- WAGA ---
        if (weightField != null)
        {
            if (item.weight > 0f) SetField(weightField, $"Waga: {item.weight}");
            else weightField.gameObject.SetActive(false);
        }

        // --- CENA ---
        if (priceField != null)
        {
            if (item.price > 0) SetField(priceField, $"Cena: {item.price} G");
            else priceField.gameObject.SetActive(false);
        }

        // Przeliczenie wysokosci okna, gdy czesc pol zniknela
        if (autoResize)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    // Wpisuje tekst i wlacza pole; pusty tekst = pole znika, zeby nie zostawialo dziury
    private void SetField(TextMeshProUGUI field, string text)
    {
        if (field == null) return;

        bool hasText = !string.IsNullOrWhiteSpace(text);
        field.gameObject.SetActive(hasText);
        if (hasText) field.text = text;
    }

    public void HideTooltip()
    {
        gameObject.SetActive(false);
        if (rarityIconImage != null) rarityIconImage.enabled = false;
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