using UnityEngine;
using UnityEngine.UI; // Potrzebne do Image
using TMPro;

public class InventoryTooltip : MonoBehaviour
{
    public static InventoryTooltip instance;

    [Header("Komponenty UI")]
    public TextMeshProUGUI headerField;   // Nazwa
    public TextMeshProUGUI contentField;  // Opis
    public TextMeshProUGUI rarityField;   // Tekst rzadkoœci (np. "Epicki")

    // --- NOWOŒÆ: Obrazek na ikonê rzadkoœci ---
    public Image rarityIconImage;
    // -----------------------------------------

    public TextMeshProUGUI priceField;

    [Header("Grafiki Rzadkoœci (Przypisz swoje ikonki)")]
    // --- NOWOŒÆ: Miejsca na Twoje sprite'y ---
    public Sprite commonIcon;
    public Sprite rareIcon;
    public Sprite epicIcon;
    public Sprite legendaryIcon;
    // ----------------------------------------


    [Header("Ustawienia")]
    public float offsetX = 15f;
    public float offsetY = -15f;

    private RectTransform rectTransform;

    void Awake()
    {
        instance = this;
        // Pobieramy komponent raz, ¿eby nie obci¹¿aæ gry co klatkê
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        // Na starcie wy³¹czamy tooltip i ikonkê
        if (rarityIconImage != null) rarityIconImage.enabled = false;
        gameObject.SetActive(false);
    }

    void Update()
    {
        // --- INTELIGENTNE POZYCJONOWANIE W EKRANIE ---
        Vector2 mousePos = Input.mousePosition;

        // Sprawdzamy, w której po³ówce ekranu jest kursor (wynik to 0 albo 1)
        float pivotX = mousePos.x / Screen.width > 0.5f ? 1f : 0f;
        float pivotY = mousePos.y / Screen.height > 0.5f ? 1f : 0f;

        // Zmieniamy punkt zaczepienia (Pivot) ToolTipa w locie!
        // Jeœli myszka jest przy prawej krawêdzi, okienko rysuje siê w lewo.
        rectTransform.pivot = new Vector2(pivotX, pivotY);

        // Odwracamy te¿ Twoje offsety, ¿eby ToolTip odsuwa³ siê w odpowiedni¹ stronê i nie zas³ania³ myszki
        float currentOffsetX = pivotX == 1f ? -Mathf.Abs(offsetX) : Mathf.Abs(offsetX);
        float currentOffsetY = pivotY == 1f ? -Mathf.Abs(offsetY) : Mathf.Abs(offsetY);

        transform.position = mousePos + new Vector2(currentOffsetX, currentOffsetY);
    }

    public void ShowTooltip(ItemData item)
    {
        // Ustawiamy teksty
        headerField.text = item.itemName;
        contentField.text = item.description;

        if (rarityField != null)
        {
            rarityField.text = GetRarityName(item.rarity);
            rarityField.color = GetRarityColor(item.rarity);
        }

        // --- NOWOŒÆ: Ustawiamy ikonê rzadkoœci ---
        if (rarityIconImage != null)
        {
            // Pobieramy odpowiedni obrazek
            Sprite iconToShow = GetRaritySprite(item.rarity);

            if (iconToShow != null)
            {
                // Jeœli mamy grafikê, przypisujemy j¹ i w³¹czamy obrazek
                rarityIconImage.sprite = iconToShow;
                rarityIconImage.enabled = true;
            }
            else
            {
                // Jeœli nie przypisa³eœ grafiki dla tej rzadkoœci, ukrywamy obrazek
                rarityIconImage.enabled = false;
            }
        }
        // -----------------------------------------
        if (priceField != null)
        {
            if (item.price > 0)
            {
                priceField.text = $"Cena: {item.price} G";
                priceField.gameObject.SetActive(true);
            }
            else
            {
                priceField.gameObject.SetActive(false);
            }
        }
        gameObject.SetActive(true);
    }

    public void HideTooltip()
    {
        gameObject.SetActive(false);
        // Wy³¹czamy ikonkê przy chowaniu
        if (rarityIconImage != null) rarityIconImage.enabled = false;
    }

    // Pomocnicza funkcja do nazw
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

    // Pomocnicza funkcja do kolorów
    Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Color.white;
            case ItemRarity.Rare: return Color.cyan;
            case ItemRarity.Epic: return new Color(0.6f, 0f, 1f); // Fioletowy
            case ItemRarity.Legendary: return Color.yellow;
            default: return Color.white;
        }
    }

    // --- NOWOŒÆ: Pomocnicza funkcja do grafik ---
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