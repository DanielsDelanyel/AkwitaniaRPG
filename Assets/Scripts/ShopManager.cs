using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public static ShopManager instance;

    [Header("Oferta Kupca (Lewa Strona)")]
    public ShopItemSlot[] shopSlots; // Kwadraty z lewej strony
    public TextMeshProUGUI pageText;
    private int currentPage = 0;
    private List<ItemData> currentShopItems = new List<ItemData>();

    [Header("Stół Handlowy (Środek)")]
    public Image centerIcon;
    public TextMeshProUGUI centerAmountText;
    public TextMeshProUGUI priceText;
    public Button buyButton;
    public Button sellButton;

    [Header("Pieniądze Gracza")]
    public TextMeshProUGUI playerMoneyText;

    private ItemData stagedItem;
    private int stagedAmount;
    private bool isSelling; // Zmienna mówiąca, czy gracz coś tu położył, czy wybrał towar kupca

    void Awake() { instance = this; }

    public void OpenShop(NPCStats npc)
    {
        // --- DETEKTYW 1: Sprawdzamy, czy kupiec w ogóle ma towar! ---
        if (npc.shopItems == null || npc.shopItems.Length == 0)
        {
            Debug.LogWarning($"UWAGA: Kupiec {npc.npcName} nie ma nic w tablicy ShopItems!");
            currentShopItems = new List<ItemData>();
        }
        else
        {
            currentShopItems = new List<ItemData>(npc.shopItems);
            Debug.Log($"Sklep otwarty! Załadowano {currentShopItems.Count} przedmiotów do sprzedaży.");
        }

        currentPage = 0;
        ClearStage();
        UpdatePage();
        UpdateMoneyText();
        gameObject.SetActive(true);
    }

    public void CloseShop()
    {
        // Jeśli gracz położył na stół swój przedmiot, ale wyłączył sklep, oddajemy mu go do plecaka!
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
        int maxPages = Mathf.Max(1, Mathf.CeilToInt((float)currentShopItems.Count / shopSlots.Length));
        if (pageText != null) pageText.text = $"Strona {currentPage + 1} / {maxPages}";

        for (int i = 0; i < shopSlots.Length; i++)
        {
            // --- DETEKTYW 2: Zabezpieczenie przed pustymi miejscami w tablicy ---
            if (shopSlots[i] == null)
            {
                Debug.LogError($"BŁĄD: Slot nr {i} w tablicy 'Shop Slots' w ShopManagerze jest PUSTY! Sprawdź Inspektor.");
                continue; // Przeskakujemy zepsuty slot, żeby reszta sklepu mogła się załadować!
            }

            int itemIndex = (currentPage * shopSlots.Length) + i;
            if (itemIndex < currentShopItems.Count)
            {
                shopSlots[i].Setup(currentShopItems[itemIndex]);
            }
            else
            {
                shopSlots[i].Clear();
            }
        }
    }

    public void NextPage() 
    { 
        int maxPages = Mathf.CeilToInt((float)currentShopItems.Count / shopSlots.Length);
        if (currentPage < maxPages - 1) { currentPage++; UpdatePage(); }
    }

    public void PrevPage() 
    { 
        if (currentPage > 0) { currentPage--; UpdatePage(); }
    }

    // --- WYKŁADANIE NA STÓŁ ---
    public void StageForBuy(ItemData item)
    {
        if (isSelling && stagedItem != null) InventoryUI.instance.Add(stagedItem, stagedAmount); // Zwrot z powrotem

        stagedItem = item;
        stagedAmount = 1;
        isSelling = false;

        centerIcon.sprite = item.icon;
        centerIcon.enabled = true;
        centerAmountText.text = "";

        int price = GetBuyPrice(item);
        priceText.text = $"Koszt: {price} G";

        buyButton.interactable = PlayerStats.instance.currentMoney >= price;
        sellButton.interactable = false;
    }

    public void StageForSell(ItemData item, int amount)
    {
        if (isSelling && stagedItem != null) InventoryUI.instance.Add(stagedItem, stagedAmount);

        stagedItem = item;
        stagedAmount = amount;
        isSelling = true;

        centerIcon.sprite = item.icon;
        centerIcon.enabled = true;
        centerAmountText.text = amount > 1 ? amount.ToString() : "";

        int profit = GetSellPrice(item) * amount;
        priceText.text = $"Zysk: {profit} G";

        buyButton.interactable = false;
        sellButton.interactable = true;
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
                int leftovers = InventoryUI.instance.Add(stagedItem, 1);
                if (leftovers == 0) // Zmieściło się w plecaku!
                {
                    PlayerStats.instance.currentMoney -= price;
                    UpdateMoneyText();
                    StageForBuy(stagedItem); // Odśwież (zablokuje przycisk, jeśli zabrakło kasy na kolejne)
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
            ClearStage(); // Po sprzedaży stół staje się pusty
            InventoryUI.instance.UpdatePlayerInfoUI();
        }
    }

    private void UpdateMoneyText()
    {
        if (playerMoneyText != null) playerMoneyText.text = PlayerStats.instance.currentMoney.ToString();
    }

    // --- MATEMATYKA (CHARYZMA I RABATY) ---
    private int GetBuyPrice(ItemData item)
    {
        float discount = PlayerStats.instance != null ? PlayerStats.instance.discount : 0f;
        int finalPrice = Mathf.RoundToInt(item.price * (1f - discount));
        return Mathf.Max(1, finalPrice); // Zawsze kosztuje minimum 1G
    }

    private int GetSellPrice(ItemData item)
    {
        float discount = PlayerStats.instance != null ? PlayerStats.instance.discount : 0f;
        // Gracz sprzedaje za 50% ceny. Rabat (np. 0.3 od Barda) ZWIĘKSZA ten zysk!
        int finalPrice = Mathf.RoundToInt((item.price * 0.5f) * (1f + discount));
        return Mathf.Max(1, finalPrice); 
    }
}