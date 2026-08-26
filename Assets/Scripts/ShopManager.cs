using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    // ODPORNY SINGLETON.
    // Awake() nie odpala sie na obiekcie wylaczonym w Hierarchii, a ShopPanel
    // startuje wylaczony. Jesli ktos zapomni przypisac go w DialogueManagerze,
    // odnajdujemy sie sami - takze wsrod obiektow nieaktywnych.
    private static ShopManager _instance;
    public static ShopManager instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);

            return _instance;
        }
    }

    [Header("Oferta Kupca (Lewa Strona)")]
    public Transform shopSlotsParent;   // NOWE: przeciagnij tu obiekt "ShopSlots" (opcjonalnie, jako zabezpieczenie)
    public ShopItemSlot[] shopSlots;    // Kwadraty z lewej strony
    public TextMeshProUGUI pageText;
    private int currentPage = 0;
    private List<ItemData> currentShopItems = new List<ItemData>();

    [Header("Stol Handlowy (Srodek)")]
    public Image centerIcon;
    public TextMeshProUGUI centerAmountText;
    public TextMeshProUGUI priceText;
    public Button buyButton;
    public Button sellButton;

    [Header("Pieniadze Gracza")]
    public TextMeshProUGUI playerMoneyText;

    private NPCStats currentMerchant;
    private ItemData stagedItem;
    private int stagedAmount;
    private bool isSelling; // true = gracz cos polozyl na stol, false = gracz wybral towar kupca

    void Awake()
    {
        _instance = this;
        AutoFillSlots();
    }

    // Znajduje sloty sam, gdy tablica jest pusta ALBO ma dziury.
    // Niekompletna tablica (czesc pozycji "None") zalewala konsole bledami.
    private void AutoFillSlots()
    {
        if (!NeedsAutoFill()) return;

        // 1. Najpierw probujemy wskazanego rodzica
        Transform searchRoot = shopSlotsParent;

        // 2. Jesli go nie ma - szukamy wsrod wlasnych dzieci
        if (searchRoot == null) searchRoot = transform;

        ShopItemSlot[] found = searchRoot.GetComponentsInChildren<ShopItemSlot>(true);

        if (found.Length == 0)
        {
            Debug.LogError("ShopManager: nie znalazlem ZADNEGO ShopItemSlot! " +
                           "Przeciagnij obiekt 'ShopSlots' w pole 'Shop Slots Parent'.");
            return;
        }

        shopSlots = found;
        Debug.Log($"ShopManager: automatycznie znalazl {shopSlots.Length} slotow kupca.");
    }

    private bool NeedsAutoFill()
    {
        if (shopSlots == null || shopSlots.Length == 0) return true;

        // Dziura w tablicy = ktos zmienil rozmiar i nie dokonczyl
        foreach (ShopItemSlot slot in shopSlots)
        {
            if (slot == null) return true;
        }

        return false;
    }

    public void OpenShop(NPCStats npc)
    {
        gameObject.SetActive(true); // najpierw wlaczamy, potem rysujemy
        AutoFillSlots();
        currentMerchant = npc;

        if (npc == null)
        {
            Debug.LogError("BLAD: OpenShop dostal pustego NPC! Sprawdz, czy DialogueManager zna currentNPC.");
            currentShopItems = new List<ItemData>();
        }
        else if (npc.shopItems == null || npc.shopItems.Length == 0)
        {
            Debug.LogWarning($"UWAGA: Kupiec {npc.npcName} nie ma nic w tablicy ShopItems!");
            currentShopItems = new List<ItemData>();
        }
        else
        {
            // ZMIANA: bierzemy GOTOWE EGZEMPLARZE z NPC, a nie surowe szablony.
            // Dzieki temu tooltip w sklepie pokazuje dokladnie te statystyki,
            // ktore gracz dostanie po zakupie - koniec kupowania kota w worku.
            currentShopItems = new List<ItemData>(npc.GetShopStock());
            Debug.Log($"Sklep otwarty! Zaladowano {currentShopItems.Count} przedmiotow kupca {npc.npcName}.");
        }

        currentPage = 0;
        ClearStage();
        UpdatePage();
        UpdateMoneyText();
    }

    public void CloseShop()
    {
        // Jesli gracz polozyl na stol swoj przedmiot, ale wylaczyl sklep, oddajemy mu go do plecaka!
        if (isSelling && stagedItem != null && InventoryUI.instance != null)
        {
            InventoryUI.instance.Add(stagedItem, stagedAmount);
        }
        ClearStage();
        gameObject.SetActive(false);
    }

    // --- PAGINACJA ---
    public void UpdatePage()
    {
        if (shopSlots == null || shopSlots.Length == 0)
        {
            Debug.LogError("BLAD: Tablica 'Shop Slots' w ShopManagerze jest PUSTA! Przeciagnij tam 20 slotow (albo obiekt-rodzica w pole 'Shop Slots Parent').");
            if (pageText != null) pageText.text = "Strona 1 / 1";
            return;
        }

        int maxPages = Mathf.Max(1, Mathf.CeilToInt((float)currentShopItems.Count / shopSlots.Length));
        if (pageText != null) pageText.text = $"Strona {currentPage + 1} / {maxPages}";

        for (int i = 0; i < shopSlots.Length; i++)
        {
            if (shopSlots[i] == null)
            {
                Debug.LogError($"BLAD: Slot nr {i} w tablicy 'Shop Slots' jest PUSTY! Sprawdz Inspektor.");
                continue;
            }

            int itemIndex = (currentPage * shopSlots.Length) + i;
            if (itemIndex < currentShopItems.Count) shopSlots[i].Setup(currentShopItems[itemIndex]);
            else shopSlots[i].Clear();
        }
    }

    public void NextPage()
    {
        if (shopSlots == null || shopSlots.Length == 0) return;
        int maxPages = Mathf.CeilToInt((float)currentShopItems.Count / shopSlots.Length);
        if (currentPage < maxPages - 1) { currentPage++; UpdatePage(); }
    }

    public void PrevPage()
    {
        if (currentPage > 0) { currentPage--; UpdatePage(); }
    }

    // --- WYKLADANIE NA STOL ---
    public void StageForBuy(ItemData item)
    {
        if (isSelling && stagedItem != null && InventoryUI.instance != null)
            InventoryUI.instance.Add(stagedItem, stagedAmount); // zwrot poprzedniego towaru gracza

        stagedItem = item;
        stagedAmount = 1;
        isSelling = false;

        centerIcon.sprite = item.icon;
        centerIcon.preserveAspect = true;
        centerIcon.enabled = true;
        centerAmountText.text = "";

        int price = GetBuyPrice(item);
        priceText.text = $"Koszt: {price} G";

        buyButton.interactable = PlayerStats.instance.currentMoney >= price;
        sellButton.interactable = false;
    }

    public void StageForSell(ItemData item, int amount)
    {
        if (isSelling && stagedItem != null && InventoryUI.instance != null)
            InventoryUI.instance.Add(stagedItem, stagedAmount);

        stagedItem = item;
        stagedAmount = amount;
        isSelling = true;

        centerIcon.sprite = item.icon;
        centerIcon.preserveAspect = true;
        centerIcon.enabled = true;
        centerAmountText.text = amount > 1 ? amount.ToString() : "";

        int profit = GetSellPrice(item) * amount;
        priceText.text = $"Zysk: {profit} G";

        buyButton.interactable = false;
        sellButton.interactable = true;
    }

    // --- NOWOSC: ZDEJMOWANIE PRZEDMIOTU ZE STOLU ---
    // Wywolywane klikiem w srodkowe pole PUSTA reka.
    public void TakeBackFromStage()
    {
        if (stagedItem == null) return;

        // Towar kupca po prostu znika ze stolu (nie jest wlasnoscia gracza)
        if (isSelling && InventoryUI.instance != null && InventoryUI.instance.draggedItem == null)
        {
            // Bierzemy wlasny przedmiot z powrotem "na myszke" - mozesz go wlozyc w dowolny slot plecaka
            InventoryUI.instance.SetDraggedItem(stagedItem, stagedAmount);
        }

        ClearStage();
    }

    public bool HasStagedItem()
    {
        return stagedItem != null;
    }

    public void ClearStage()
    {
        stagedItem = null;
        stagedAmount = 0;
        centerIcon.sprite = null;
        centerIcon.enabled = false;
        centerAmountText.text = "";
        priceText.text = "";
        buyButton.interactable = false;
        sellButton.interactable = false;
    }

    // --- TRANSAKCJE ---
    public void OnBuyClicked()
    {
        if (stagedItem != null && !isSelling)
        {
            int price = GetBuyPrice(stagedItem);
            if (PlayerStats.instance.currentMoney >= price)
            {
                // Egzemplarz zostal wylosowany juz przy otwarciu sklepu,
                // wiec ItemFactory odda go bez zmian - gracz dostaje to, co widzial.
                ItemData purchased = ItemFactory.Create(stagedItem);

                int leftovers = InventoryUI.instance.Add(purchased, 1);
                if (leftovers == 0)
                {
                    PlayerStats.instance.currentMoney -= price;
                    UpdateMoneyText();

                    // Unikatowy egzemplarz schodzi z polki - nie da sie kupic
                    // dwoch identycznych mieczy z tym samym rzutem.
                    if (purchased != null && purchased.isRuntimeInstance)
                    {
                        if (currentMerchant != null) currentMerchant.RemoveFromStock(stagedItem);
                        currentShopItems.Remove(stagedItem);
                        ClearStage();
                        UpdatePage();
                    }
                    else
                    {
                        StageForBuy(stagedItem); // zwykly towar zostaje na polce
                    }

                    InventoryUI.instance.UpdatePlayerInfoUI();
                }
                else Debug.Log("Brak miejsca w plecaku!");
            }
        }
    }

    public void OnSellClicked()
    {
        if (stagedItem != null && isSelling)
        {
            int profit = GetSellPrice(stagedItem) * stagedAmount;
            PlayerStats.instance.currentMoney += profit;

            UpdateMoneyText();
            ClearStage();
            InventoryUI.instance.UpdatePlayerInfoUI();
        }
    }

    private void UpdateMoneyText()
    {
        if (playerMoneyText != null && PlayerStats.instance != null)
            playerMoneyText.text = PlayerStats.instance.currentMoney.ToString();
    }

    // --- MATEMATYKA (CHARYZMA I RABATY) ---
    private int GetBuyPrice(ItemData item)
    {
        if (item == null) return 1;
        float discount = PlayerStats.instance != null ? PlayerStats.instance.discount : 0f;
        // GetEffectivePrice() uwzglednia jakosc TEGO egzemplarza,
        // wiec miecz z +39% kosztuje wiecej niz zwykly.
        int finalPrice = Mathf.RoundToInt(item.GetEffectivePrice() * (1f - discount));
        return Mathf.Max(1, finalPrice);
    }

    private int GetSellPrice(ItemData item)
    {
        if (item == null) return 1;
        float discount = PlayerStats.instance != null ? PlayerStats.instance.discount : 0f;
        // Udany rzut podnosi tez kwote sprzedazy - dobry lup oplaca sie
        // nawet wtedy, gdy gracz go nie zaloz.
        int finalPrice = Mathf.RoundToInt((item.GetEffectivePrice() * 0.5f) * (1f + discount));
        return Mathf.Max(1, finalPrice);
    }
}