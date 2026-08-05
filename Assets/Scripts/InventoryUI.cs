using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI instance;

    [Header("UI")]
    public GameObject inventoryWindow;
    public GameObject tooltipWindow;
    public Transform backpackArea;

    public TopDownMovement playerMovement;

    [Header("Sloty Ekwipunku (Equipment Area)")]
    public InventorySlot weaponSlot;
    public InventorySlot helmetSlot;
    public InventorySlot armorSlot;
    public InventorySlot legsSlot;
    public InventorySlot bootsSlot;
    public InventorySlot offhandSlot;
    public InventorySlot ring1Slot;
    public InventorySlot ring2Slot;
    public InventorySlot necklaceSlot; 
    public InventorySlot ammoSlot;

    [Header("Drag & Drop")]
    public Image dragImage;
    public ItemData draggedItem;
    public int draggedAmount = 0;

    [Header("Sklep")]
    public GameObject equipmentPanel;

    [Header("Informacje Gracza (Teksty)")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI playerMoneyText;

    InventorySlot[] slots;

    public TextMeshProUGUI statsText;

    void Awake() { instance = this; }

    void Start()
    {
        playerNameText.text = "Maniek";
        slots = backpackArea.GetComponentsInChildren<InventorySlot>();
        if (dragImage != null) dragImage.enabled = false;
        foreach (var slot in slots) slot.ClearSlot();
        inventoryWindow.SetActive(false);
        if (tooltipWindow != null) tooltipWindow.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Escape))
        {
            // --- NOWOŒÆ: Zabezpieczenie dla prezentów ---
            if (DialogueManager.instance != null && DialogueManager.instance.giftOverlay != null && DialogueManager.instance.giftOverlay.activeSelf)
            {
                // Jeœli jesteœmy w trybie wrêczania prezentu, zamykamy okno przez DialogueManager
                DialogueManager.instance.CloseGiftPanel();
            }
            else
            {
                // Normalne otwieranie/zamykanie ekwipunku
                ToggleInventory();
            }
        }

        if (draggedItem != null && dragImage != null)
        {
            dragImage.transform.position = Input.mousePosition;
        }
    }

    public void ToggleInventory()
    {
        bool isActive = !inventoryWindow.activeSelf;

        inventoryWindow.SetActive(isActive);

        if (isActive) UpdatePlayerInfoUI();

        if (!isActive)
        {
            if (tooltipWindow != null) tooltipWindow.SetActive(false);
            if (ContextMenuUI.instance != null) ContextMenuUI.instance.CloseMenu();
            if (draggedItem != null)
            {
                int leftovers = Add(draggedItem, draggedAmount); // Próbujemy zrzuciæ to co mamy na myszce
                if (leftovers > 0) ThrowItem(draggedItem); // Brak miejsca? Wyrzucamy na ziemiê!
                ClearDraggedItem();
            }
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = !isActive;
            if (isActive) playerMovement.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }
    }

    public void CloseInventoryForTrade()
    {
        if (playerMovement != null) playerMovement.enabled = true;
    }

    public List<ItemData> GetAllItems()
    {
        List<ItemData> items = new List<ItemData>();
        foreach (var slot in slots)
        {
            if (slot.item != null) items.Add(slot.item);
        }
        return items;
    }

    public void RemoveItem(ItemData itemToRemove)
    {
        foreach (var slot in slots)
        {
            if (slot.item == itemToRemove)
            {
                slot.ClearSlot();
                break;
            }
        }
    }

    public void OnEquipmentChanged()
    {
        HandleTwoHandedWeapons();

        PlayerEquipment playerEq = playerMovement.GetComponent<PlayerEquipment>();
        if (playerEq != null)
        {
            ItemData w = (weaponSlot != null) ? weaponSlot.item : null;
            ItemData h = (helmetSlot != null) ? helmetSlot.item : null;
            ItemData a = (armorSlot != null) ? armorSlot.item : null;
            ItemData l = (legsSlot != null) ? legsSlot.item : null;
            ItemData b = (bootsSlot != null) ? bootsSlot.item : null;
            ItemData s = (offhandSlot != null) ? offhandSlot.item : null;
            ItemData r1 = (ring1Slot != null) ? ring1Slot.item : null;
            ItemData r2 = (ring2Slot != null) ? ring2Slot.item : null;
            ItemData n = (necklaceSlot != null) ? necklaceSlot.item : null;
            ItemData am = (ammoSlot != null) ? ammoSlot.item : null;

            playerEq.UpdateEquipment(w, h, a, l, b, s, r1, r2, n, am);
        }
    }

    void HandleTwoHandedWeapons()
    {
        if (weaponSlot == null || offhandSlot == null) return;

        bool isTwoHanded = false;

        // Sprawdzamy czy g³ówna broñ jest 2H lub £ukiem
        if (weaponSlot.item != null)
        {
            if (weaponSlot.item.itemType == ItemType.Weapon2h || weaponSlot.item.itemType == ItemType.Bow)
            {
                isTwoHanded = true;
            }
        }

        if (isTwoHanded)
        {
            // Jeœli trzymamy ³uk/2H, a w drugiej rêce coœ jest (np. tarcza) -> Zdejmujemy to!
            if (offhandSlot.item != null)
            {
                ItemData itemToMove = offhandSlot.item;
                offhandSlot.ClearSlot(); // Czyœcimy drug¹ rêkê

                // Próbujemy wrzuciæ tarczê do plecaka
                if (Add(itemToMove) > 0)
                {
                    // Jeœli plecak jest pe³ny (coœ zosta³o z Add), wyrzucamy na ziemiê
                    ThrowItem(itemToMove);
                }
            }

            // Blokujemy i przyciemniamy slot drugiej rêki
            offhandSlot.SetBlocked(true);
        }
        else
        {
            // Jeœli mamy woln¹ rêkê lub broñ 1H, odblokowujemy slot na tarcze
            offhandSlot.SetBlocked(false);
        }
    }

    public int Add(ItemData item, int amountToAdd = 1)
    {
        int maxStack = 100; // Limit stosu

        if (item.isStackable)
        {
            // Faza 1: Uzupe³niamy ju¿ istniej¹ce stosy tego przedmiotu
            foreach (var slot in slots)
            {
                if (slot.item == item && slot.amount < maxStack)
                {
                    int spaceLeft = maxStack - slot.amount;
                    if (spaceLeft >= amountToAdd)
                    {
                        // Ca³oœæ mieœci siê w tym slocie
                        slot.amount += amountToAdd;
                        slot.UpdateSlotUI();
                        return 0;
                    }
                    else
                    {
                        // Zape³niamy ten slot do oporu i szukamy miejsca dla reszty!
                        slot.amount = maxStack;
                        slot.UpdateSlotUI();
                        amountToAdd -= spaceLeft;
                    }
                }
            }
        }

        // Faza 2: Szukamy pustych slotów na to, co nam jeszcze zosta³o
        while (amountToAdd > 0)
        {
            bool foundEmpty = false;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].item == null) // Pusty slot!
                {
                    foundEmpty = true;
                    // Bierzemy maksymalnie 100 (lub 1 jeœli to niereplikowalny miecz)
                    int amountToPlace = item.isStackable ? Mathf.Min(amountToAdd, maxStack) : 1;

                    slots[i].AddItem(item, amountToPlace);
                    amountToAdd -= amountToPlace;
                    break; // Przerywamy pêtlê "for" i znowu krêcimy "while", ¿eby znaleŸæ kolejny pusty slot
                }
            }

            // Jeœli przeszukaliœmy wszystkie okienka i nie ma pustych -> koniec miejsca
            if (!foundEmpty) break;
        }

        return amountToAdd; // Zwracamy to, na co ostatecznie zabrak³o plecaka
    }

    public bool ConsumeAmmo()
    {
        // Sprawdzamy slot amunicji
        if (ammoSlot != null && ammoSlot.item != null && ammoSlot.item.itemType == ItemType.Ammo)
        {
            ammoSlot.amount--;
            if (ammoSlot.amount <= 0) ammoSlot.ClearSlot();
            else ammoSlot.UpdateSlotUI();

            return true;
        }
        return false;
    }

    public void SetDraggedItem(ItemData item, int amount)
    {
        draggedItem = item;
        draggedAmount = amount;
        dragImage.sprite = item.icon;
        dragImage.preserveAspect = true;
        dragImage.enabled = true;
    }

    public void ClearDraggedItem()
    {
        draggedItem = null;
        draggedAmount = 0;
        dragImage.enabled = false;
    }

    public void ThrowItem(ItemData item)
    {
        if (item == null) return;
        if (item.itemPrefab != null && playerMovement != null)
        {
            // Upuszczamy nieco pod nogami gracza
            Vector3 spawnPos = playerMovement.transform.position + new Vector3(0f, -0.5f, 0f);
            GameObject droppedItem = Instantiate(item.itemPrefab, spawnPos, Quaternion.identity);

            Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Nadajemy przedmiotowi lekki, losowy "œlizg" po trawie
                Vector2 randomSlide = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
                rb.linearVelocity = randomSlide * 2f;
            }
        }
        ClearDraggedItem();
        OnEquipmentChanged();
    }

    public void UpdatePlayerInfoUI()
    {
        if (PlayerStats.instance != null)
        {
            PlayerStats ps = PlayerStats.instance;

            // 1. Aktualizacja osobnych okienek (Imiê i Z³oto)
            if (playerNameText != null) playerNameText.text = ps.playerName;
            if (playerMoneyText != null) playerMoneyText.text = ps.currentMoney.ToString();

            // 2. Aktualizacja g³ównego okna ze statystykami
            if (statsText != null)
            {
                string info = $"Si³a: {ps.GetTotal(ps.baseSTR, ps.equipSTR)}\n" +
                              $"Inteligencja: {ps.GetTotal(ps.baseINT, ps.equipINT)}\n" +
                              $"Zrêcznoœæ: {ps.GetTotal(ps.baseZR, ps.equipZR)}\n" +
                              $"Charyzma: {ps.GetTotal(ps.baseCHAR, ps.equipCHAR)}\n\n" +
                              $"Obra¿enia: {ps.GetTotal(ps.baseDmg, ps.equipDmg)}\n" +
                              $"Obrona: {ps.GetTotal(ps.baseDef, ps.equipDef)}\n" +
                              $"Obrona Magiczna: {ps.GetTotal(ps.baseMagicDef, ps.equipMagicDef)}";

                statsText.text = info;
            }
        }
    }

    // --- NOWOŒÆ: Szybkie u¿ywanie przedmiotów z prawego przycisku ---
    public void UseItem(InventorySlot slot)
    {
        ItemData item = slot.item;

        // 1. KONSUMPCJA (Jedzenie, mikstury)
        if (item.itemType == ItemType.Consumable)
        {
            // Tutaj leczymy gracza! (Odwo³anie do Twojego PlayerStats)
            if (PlayerStats.instance != null && item.healAmount > 0)
            {
                PlayerStats.instance.currentHealth += item.healAmount;
                // Upewniamy siê, ¿e zdrowie nie przekracza maksa
                if (PlayerStats.instance.currentHealth > PlayerStats.instance.GetMaxHealth())
                    PlayerStats.instance.currentHealth = PlayerStats.instance.GetMaxHealth();

                Debug.Log($"Skonsumowano: {item.itemName}. Odzyskano {item.healAmount} HP.");
            }

            // Zabieramy jedn¹ sztukê ze stosu
            slot.amount--;
            if (slot.amount <= 0) slot.ClearSlot();
            else slot.UpdateSlotUI();
        }

        // 2. SZYBKIE ZAK£ADANIE EKWIPUNKU (Zbroje, bronie)
        else
        {
            InventorySlot targetSlot = null;

            // Szukamy odpowiedniego okienka zbroi na podstawie typu przedmiotu
            switch (item.itemType)
            {
                case ItemType.Weapon1h:
                case ItemType.Weapon2h:
                case ItemType.Bow: targetSlot = weaponSlot; break;
                case ItemType.Helmet: targetSlot = helmetSlot; break;
                case ItemType.Armor: targetSlot = armorSlot; break;
                case ItemType.Legs: targetSlot = legsSlot; break;
                case ItemType.Boots: targetSlot = bootsSlot; break;
                case ItemType.Second_Hand: targetSlot = offhandSlot; break;
                case ItemType.Ring: targetSlot = ring1Slot; break; // Domyœlnie zak³adamy na palec nr 1
                case ItemType.Necklace: targetSlot = necklaceSlot; break;
                case ItemType.Ammo: targetSlot = ammoSlot; break;
            }

            // Jeœli znaleŸliœmy miejsce i nie jest zablokowane (np. przez broñ 2H)
            if (targetSlot != null && !targetSlot.isBlocked)
            {
                ItemData tempItem = targetSlot.item;
                int tempAmount = targetSlot.amount;

                // Wk³adamy nowy przedmiot na postaæ
                targetSlot.AddItem(item, slot.amount);

                // Jeœli postaæ mia³a ju¿ coœ na sobie, wraca to do plecaka (do tego samego slota!)
                if (tempItem != null) slot.AddItem(tempItem, tempAmount);
                else slot.ClearSlot();

                // Obliczamy statystyki i odœwie¿amy wygl¹d postaci na mapie
                OnEquipmentChanged();
            }
        }
    }
}