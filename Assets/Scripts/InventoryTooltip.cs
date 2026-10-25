using System.Collections.Generic;
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

    [Header("Kolory Stanu Umiejetnosci (panel Umiejetnosci - klawisz G)")]
    public Color skillLockedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color skillAvailableColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color skillUnlockedColor = new Color(0.3f, 1f, 0.4f, 1f);

    [Header("Ustawienia")]
    public float offsetX = 15f;
    public float offsetY = -15f;

    [Tooltip("Zaznacz, jesli okno ma Vertical Layout Group + Content Size Fitter.")]
    public bool autoResize = false;

    [Tooltip("Minimalny odstep (w pikselach ekranu) miedzy oknem a krawedzia ekranu.")]
    public float screenMargin = 4f;

    // Bufor na rogi okna - zeby nie tworzyc nowej tablicy co klatke
    private readonly Vector3[] corners = new Vector3[4];

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

        // Okno podaza za myszka, ale zatrzymuje sie na krawedzi ekranu
        KeepOnScreen();
    }

    // ===============================================================
    // NOWE: TRZYMANIE OKNA W GRANICACH EKRANU
    // Sprawdzamy, gdzie FAKTYCZNIE leza rogi okna (juz po przeskalowaniu
    // przez Canvas Scaler), i jesli ktorys wystaje za ekran - przesuwamy
    // cale okno z powrotem. Dziala niezaleznie od rozdzielczosci, pivotu
    // i tego, jak duze jest okno w danej chwili (np. dlugi opis).
    // Canvas jest w trybie Screen Space - Overlay, wiec rogi w "world space"
    // to po prostu piksele ekranu.
    // ===============================================================
    private void KeepOnScreen()
    {
        rectTransform.GetWorldCorners(corners); // [0] = lewy dol, [2] = prawy gora

        float left = corners[0].x;
        float bottom = corners[0].y;
        float right = corners[2].x;
        float top = corners[2].y;

        float minX = screenMargin;
        float maxX = Screen.width - screenMargin;
        float minY = screenMargin;
        float maxY = Screen.height - screenMargin;

        Vector3 shift = Vector3.zero;

        // Poziomo: jesli okno jest szersze niz ekran, przyklejamy je do lewej krawedzi
        if (right - left > maxX - minX) shift.x = minX - left;
        else if (left < minX) shift.x = minX - left;
        else if (right > maxX) shift.x = maxX - right;

        // Pionowo: jesli okno jest wyzsze niz ekran, przyklejamy je do gornej krawedzi
        // (tam jest nazwa przedmiotu - najwazniejsza czesc)
        if (top - bottom > maxY - minY) shift.y = maxY - top;
        else if (top > maxY) shift.y = maxY - top;
        else if (bottom < minY) shift.y = minY - bottom;

        if (shift != Vector3.zero) transform.position += shift;
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
        transform.position = screenPosition;

        // Stary ClampToScreen liczyl rozmiar w jednostkach Canvasu (sizeDelta),
        // a nie w pikselach ekranu - przy skalowanym Canvasie okno i tak wystawalo.
        KeepOnScreen();
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

    // ===============================================================
    // WARIANT DLA UMIEJETNOSCI (panel Umiejetnosci - klawisz G)
    // Uzywa tych samych pol UI co ekwipunek, tylko wypelnia je inaczej -
    // dzieki temu nie trzeba budowac osobnego okienka tooltipa.
    // ===============================================================
    public void ShowTooltip(SkillData skill, bool unlocked, bool canUnlock)
    {
        if (isPinned) return; // przypiete szczegoly (ekwipunek) maja pierwszenstwo
        Fill(skill, unlocked, canUnlock);
        SetVisible(true);
    }

    private void Fill(SkillData skill, bool unlocked, bool canUnlock)
    {
        if (skill == null) return;

        Color statusColor = unlocked ? skillUnlockedColor : (canUnlock ? skillAvailableColor : skillLockedColor);

        if (headerField != null)
        {
            headerField.text = skill.skillName;
            headerField.color = statusColor;
        }

        if (rarityField != null)
        {
            rarityField.text = unlocked ? "Odblokowana" : (canUnlock ? "Mozna odblokowac" : "Zablokowana");
            rarityField.color = statusColor;
        }

        // Pole rarityIcon przy umiejetnosciach pokazuje po prostu ikone umiejetnosci.
        if (rarityIconImage != null)
        {
            rarityIconImage.sprite = skill.icon;
            rarityIconImage.enabled = skill.icon != null;
        }

        SetField(typeField, $"{GetProfessionLabel(skill.profession)} - {GetEffectLabel(skill.effectType)}");
        SetField(statsField, BuildSkillStatsText(skill));
        SetField(contentField, skill.description);

        if (weightField != null) weightField.gameObject.SetActive(false);

        if (priceField != null) SetField(priceField, $"Koszt: {skill.cost} pkt umiejetnosci");

        if (autoResize && rectTransform != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private string BuildSkillStatsText(SkillData skill)
    {
        List<string> parts = new List<string>();

        if (skill.manaCostPercent > 0f) parts.Add($"Koszt many: {skill.manaCostPercent:0.#}% maks. many");
        else if (skill.manaCost > 0f) parts.Add($"Koszt many: {skill.manaCost}");

        if (skill.healthCostPercent > 0f) parts.Add($"Koszt zdrowia: {skill.healthCostPercent:0.#}% maks. zdrowia");

        if (skill.cooldown > 0f) parts.Add($"Odnowienie: {skill.cooldown:0.#}s");

        if (skill.effectType == SkillEffectType.Summon)
        {
            parts.Add($"Przyzywa: {skill.baseSummonCount}-{skill.maxSummonCount} stworzen " +
                      "(ilosc i sila zaleza od Witalnosci i Inteligencji)");
        }

        if (skill.effectType == SkillEffectType.Active)
        {
            parts.Add($"Obrazenia: {skill.baseDamage}+ (rosna z Inteligencja i Zrecznoscia)");
            parts.Add($"Czas trwania: {skill.baseDuration:0.#}s+ (rosnie z Inteligencja i Zrecznoscia)");
        }

        if (skill.requiredSkills != null && skill.requiredSkills.Length > 0)
        {
            List<string> reqNames = new List<string>();
            foreach (SkillData req in skill.requiredSkills)
            {
                if (req != null) reqNames.Add(req.skillName);
            }
            if (reqNames.Count > 0) parts.Add("Wymaga: " + string.Join(", ", reqNames));
        }

        return string.Join("\n", parts);
    }

    private string GetProfessionLabel(CharacterClass profession)
    {
        switch (profession)
        {
            case CharacterClass.Nekromancer: return "Nekromanta";
            case CharacterClass.Hunter: return "Lowca";
            case CharacterClass.Mage: return "Mag";
            case CharacterClass.Barbarian: return "Barbarzynca";
            case CharacterClass.Juggernaut: return "Obronca";
            case CharacterClass.Bard: return "Bard";
            case CharacterClass.Assassin: return "Skrytobojca";
            case CharacterClass.Paladin: return "Paladyn";
            case CharacterClass.Ilusionist: return "Iluzjonista";
            case CharacterClass.Monk: return "Mnich";
            default: return "Wloczega";
        }
    }

    private string GetEffectLabel(SkillEffectType effectType)
    {
        switch (effectType)
        {
            case SkillEffectType.Summon: return "Przyzwanie";
            case SkillEffectType.Passive: return "Pasywna";
            case SkillEffectType.Active: return "Aktywna";
            default: return "";
        }
    }
}
