using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI instance;

    [Header("UI")]
    public GameObject inventoryWindow;
    [Tooltip("NIE jest juz wylaczane przez SetActive - tooltip chowa sie sam przez CanvasGroup.")]
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

    [Header("Ustawienia")]
    public int maxStackSize = 100;

    InventorySlot[] slots;

    public TextMeshProUGUI statsText;

    void Awake() { instance = this; }

    void Start()
    {
        if (backpackArea != null) slots = backpackArea.GetComponentsInChildren<InventorySlot>(true);
        else slots = new InventorySlot[0];

        if (dragImage != null) dragImage.enabled = false;
        foreach (var slot in slots) slot.ClearSlot();

        if (inventoryWindow != null) inventoryWindow.SetActive(false);

        // Tooltip chowamy przez jego wlasna metode - obiekt ma zostac WLACZONY,
        // inaczej jego Awake sie nie odpali i singleton bedzie pusty.
        if (InventoryTooltip.instance != null) InventoryTooltip.instance.ForceHide();

        UpdatePlayerInfoUI();
    }

    // Czy okno ekwipunku jest otwarte?
    public bool IsOpen { get { return inventoryWindow != null && inventoryWindow.activeSelf; } }

    void Update()
    {
        // UWAGA: Escape NIE jest tu obslugiwany. Zajmuje sie nim UIEscapeHandler,
        // zeby jedno nacisniecie nie zamykalo jednego okna i nie otwieralo drugiego.
        if (Input.GetKeyDown(KeyCode.I)) TryToggleInventory();

        if (draggedItem != null && dragImage != null)
        {
            dragImage.transform.position = Input.mousePosition;
        }
    }

    // Klawisz "I" - otwiera tylko wtedy, gdy nic wazniejszego nie jest otwarte
    public void TryToggleInventory()
    {
        if (!IsOpen && IsBlockedByAnotherWindow())
        {
            Debug.Log("Nie mozna teraz otworzyc ekwipunku.");
            return;
        }

        ToggleInventory();
    }

    // Rozmowa, sklep i prezenty maja pierwszenstwo przed plecakiem
    private bool IsBlockedByAnotherWindow()
    {
        // Pauza zaslania wszystko - pod nia nie otwieramy plecaka
        if (PauseMenuUI.instance != null && PauseMenuUI.instance.IsOpen) return true;

        // Martwy gracz nie grzebie w plecaku
        if (DeathScreenUI.instance != null && DeathScreenUI.instance.IsShowing) return true;

        if (DialogueManager.instance == null) return false;

        return DialogueManager.instance.IsDialogueOpen
            || DialogueManager.instance.IsShopOpen
            || DialogueManager.instance.IsGiftPanelOpen;
    }

    // Wygodne dla UIEscapeHandler - zamyka, ale nigdy nie otwiera
    public void CloseInventory()
    {
        if (IsOpen) ToggleInventory();
    }

    public void ToggleInventory()
    {
        if (inventoryWindow == null) return;

        bool isActive = !inventoryWindow.activeSelf;
        inventoryWindow.SetActive(isActive);

        if (isActive) UpdatePlayerInfoUI();

        if (!isActive)
        {
            if (InventoryTooltip.instance != null) InventoryTooltip.instance.ForceHide();
            if (ContextMenuUI.instance != null) ContextMenuUI.instance.CloseMenu();

            if (draggedItem != null)
            {
                int leftovers = Add(draggedItem, draggedAmount);
                if (leftovers > 0) ThrowItem(draggedItem);
                ClearDraggedItem();
            }
        }

        // TU BYL BLAD: zamkniecie ekwipunku wlaczalo ruch bezwarunkowo,
        // kasujac blokade zalozona przez trwajaca rozmowe. UILock pilnuje,
        // by ruch wrocil dopiero, gdy zniknie OSTATNI powod blokady.
        UILock.Set("Inventory", isActive);
    }

    public void CloseInventoryForTrade()
    {
        UILock.Set("Inventory", false);
    }

    // ===============================================================
    // DOSTEP DLA ZAPISU GRY
    // Kolejnosc slotow wyposazenia MUSI byc stala - zapis zapamietuje
    // pozycje w tej tablicy, wiec przestawienie ich zepsulo by stare zapisy.
    // ===============================================================
    public InventorySlot[] GetBackpackSlots()
    {
        return slots;
    }

    public InventorySlot[] GetEquipmentSlots()
    {
        return new InventorySlot[]
        {
            weaponSlot,    // 0
            helmetSlot,    // 1
            armorSlot,     // 2
            legsSlot,      // 3
            bootsSlot,     // 4
            offhandSlot,   // 5
            ring1Slot,     // 6
            ring2Slot,     // 7
            necklaceSlot,  // 8
            ammoSlot       // 9
        };
    }

    public List<ItemData> GetAllItems()
    {
        List<ItemData> items = new List<ItemData>();
        if (slots == null) return items;

        foreach (var slot in slots)
        {
            if (slot != null && slot.item != null) items.Add(slot.item);
        }
        return items;
    }

    public void RemoveItem(ItemData itemToRemove)
    {
        if (slots == null) return;

        foreach (var slot in slots)
        {
            if (slot != null && slot.item == itemToRemove)
            {
                slot.ClearSlot();
                break;
            }
        }
    }

    // ===============================================================
    // NOWE: PODZIAL STOSU
    // Bierze polowe stosu "na kursor", reszte zostawia w slocie.
    // ===============================================================
    public void SplitStack(InventorySlot slot)
    {
        if (slot == null || slot.item == null) return;
        SplitStackAmount(slot, slot.amount / 2); // przy nieparzystej liczbie wieksza czesc zostaje w slocie
    }

    // NOWE: jak SplitStack, ale z DOWOLNA iloscia (wybierana suwakiem w SplitAmountUI)
    // zamiast sztywnej polowy. amount jest przycinane do bezpiecznego zakresu
    // [1, slot.amount - 1], zeby w oryginalnym slocie zawsze zostala co najmniej 1 sztuka.
    public void SplitStackAmount(InventorySlot slot, int amount)
    {
        if (slot == null || slot.item == null) return;

        if (draggedItem != null)
        {
            Debug.Log("Najpierw odloz to, co trzymasz na kursorze.");
            return;
        }

        if (!slot.item.isStackable || slot.amount < 2)
        {
            Debug.Log("Tego przedmiotu nie da sie podzielic.");
            return;
        }

        int taken = Mathf.Clamp(amount, 1, slot.amount - 1);
        int left = slot.amount - taken;

        ItemData item = slot.item;

        slot.amount = left;
        slot.UpdateSlotUI();

        SetDraggedItem(item, taken);
    }

    // ===============================================================
    // NOWE: WYRZUCANIE ZE SLOTU
    // ===============================================================
    public void DropFromSlot(InventorySlot slot)
    {
        if (slot == null || slot.item == null) return;

        ItemData item = slot.item;
        int amount = slot.amount;

        slot.ClearSlot();
        ThrowItemStack(item, amount);

        if (!slot.isBackpackSlot) OnEquipmentChanged();
    }

    public void OnEquipmentChanged()
    {
        HandleTwoHandedWeapons();

        if (playerMovement == null) return;

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

        if (weaponSlot.item != null)
        {
            if (weaponSlot.item.itemType == ItemType.Weapon2h || weaponSlot.item.itemType == ItemType.Bow
                || weaponSlot.item.itemType == ItemType.Wand2h)
                isTwoHanded = true;
        }

        if (isTwoHanded)
        {
            if (offhandSlot.item != null)
            {
                ItemData itemToMove = offhandSlot.item;
                int amountToMove = offhandSlot.amount;
                offhandSlot.ClearSlot();

                if (Add(itemToMove, amountToMove) > 0) ThrowItem(itemToMove);
            }

            offhandSlot.SetBlocked(true);
        }
        else
        {
            offhandSlot.SetBlocked(false);
        }
    }

    public int Add(ItemData item, int amountToAdd = 1)
    {
        if (item == null || slots == null) return amountToAdd;

        int maxStack = maxStackSize;

        if (item.isStackable)
        {
            foreach (var slot in slots)
            {
                if (slot == null) continue;

                if (slot.item == item && slot.amount < maxStack)
                {
                    int spaceLeft = maxStack - slot.amount;
                    if (spaceLeft >= amountToAdd)
                    {
                        slot.amount += amountToAdd;
                        slot.UpdateSlotUI();
                        return 0;
                    }
                    else
                    {
                        slot.amount = maxStack;
                        slot.UpdateSlotUI();
                        amountToAdd -= spaceLeft;
                    }
                }
            }
        }

        while (amountToAdd > 0)
        {
            bool foundEmpty = false;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].item == null)
                {
                    foundEmpty = true;
                    int amountToPlace = item.isStackable ? Mathf.Min(amountToAdd, maxStack) : 1;

                    slots[i].AddItem(item, amountToPlace);
                    amountToAdd -= amountToPlace;
                    break;
                }
            }

            if (!foundEmpty) break;
        }

        // Zadania typu "zbierz 5 truskawek" sprawdzaja plecak po kazdej zmianie
        QuestManager.RefreshInventoryObjectives();

        return amountToAdd;
    }

    public bool ConsumeAmmo()
    {
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
        if (item == null) { ClearDraggedItem(); return; }

        draggedItem = item;
        draggedAmount = amount;

        if (dragImage != null)
        {
            dragImage.sprite = item.icon;
            dragImage.preserveAspect = true;
            dragImage.enabled = true;
        }
    }

    public void ClearDraggedItem()
    {
        draggedItem = null;
        draggedAmount = 0;
        if (dragImage != null) dragImage.enabled = false;
    }

    public void ThrowItem(ItemData item)
    {
        ThrowItemStack(item, Mathf.Max(1, draggedAmount));
        ClearDraggedItem();
        OnEquipmentChanged();
    }

    // Wyrzuca konkretna ilosc, zachowujac wylosowane statystyki egzemplarza
    public void ThrowItemStack(ItemData item, int amount)
    {
        if (item == null) return;
        if (item.itemPrefab == null || playerMovement == null)
        {
            Debug.LogWarning($"'{item.itemName}' nie ma Item Prefab - nie da sie go wyrzucic.");
            return;
        }

        Vector3 spawnPos = playerMovement.transform.position + new Vector3(0f, -0.5f, 0f);
        GameObject droppedItem = Instantiate(item.itemPrefab, spawnPos, Quaternion.identity);

        ItemPickup pickup = droppedItem.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.itemData = item;
            pickup.amount = Mathf.Max(1, amount);
        }

        Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 randomSlide = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            rb.linearVelocity = randomSlide * 2f;
        }
    }

    public void UpdatePlayerInfoUI()
    {
        if (PlayerStats.instance == null) return;

        PlayerStats ps = PlayerStats.instance;

        if (playerNameText != null) playerNameText.text = ps.playerName;
        if (playerMoneyText != null) playerMoneyText.text = ps.currentMoney.ToString();

        if (statsText != null)
        {
            string info = $"Sila: {ps.GetTotal(ps.baseSTR, ps.equipSTR)}\n" +
                          $"Inteligencja: {ps.GetTotal(ps.baseINT, ps.equipINT)}\n" +
                          $"Zrecznosc: {ps.GetTotal(ps.baseZR, ps.equipZR)}\n" +
                          $"Charyzma: {ps.GetTotal(ps.baseCHAR, ps.equipCHAR)}\n\n" +
                          $"Obrazenia: {ps.GetTotal(ps.baseDmg, ps.equipDmg)}\n" +
                          $"Obrona: {ps.GetTotal(ps.baseDef, ps.equipDef)}\n" +
                          $"Obrona Magiczna: {ps.GetTotal(ps.baseMagicDef, ps.equipMagicDef)}";

            statsText.text = info;
        }
    }

    public void UseItem(InventorySlot slot)
    {
        // ZABEZPIECZENIE: przycisk w Inspektorze potrafil wolac to z pustym slotem
        if (slot == null || slot.item == null) return;

        // NOWE: menu kontekstowe otwarte na slocie EKWIPUNKU wysyla tu przycisk
        // "Zdejmij" (patrz ContextMenuUI.ConfigureButtons) - przedmiot jest juz
        // zalozony, wiec zamiast probowac go "zalozyc jeszcze raz" po prostu
        // wracamy nim do plecaka.
        if (!slot.isBackpackSlot)
        {
            ItemData equippedItem = slot.item;
            int equippedAmount = slot.amount;

            int leftovers = Add(equippedItem, equippedAmount);
            if (leftovers > 0)
            {
                // Plecak pelny - zostawiamy przedmiot tam, gdzie byl, zamiast go gubic
                Debug.Log("Brak miejsca w plecaku - nie mozna zdjac przedmiotu.");
                return;
            }

            slot.ClearSlot();
            OnEquipmentChanged();
            return;
        }

        ItemData item = slot.item;

        if (item.itemType == ItemType.Consumable)
        {
            // POPRAWKA: GetHealAmount() zamiast healAmount -
            // inaczej wylosowany bonus leczenia byl ignorowany
            int heal = item.GetHealAmount();

            if (PlayerStats.instance != null && heal > 0)
            {
                PlayerStats.instance.currentHealth += heal;

                if (PlayerStats.instance.currentHealth > PlayerStats.instance.GetMaxHealth())
                    PlayerStats.instance.currentHealth = PlayerStats.instance.GetMaxHealth();

                Debug.Log($"Skonsumowano: {item.itemName}. Odzyskano {heal} HP.");
            }

            slot.amount--;
            if (slot.amount <= 0) slot.ClearSlot();
            else slot.UpdateSlotUI();

            return;
        }

        // --- ZAKLADANIE EKWIPUNKU ---
        InventorySlot targetSlot = null;

        switch (item.itemType)
        {
            case ItemType.Weapon1h:
            case ItemType.Weapon2h:
            case ItemType.Bow:
            case ItemType.Wand1h:
            case ItemType.Wand2h: targetSlot = weaponSlot; break;
            case ItemType.Helmet: targetSlot = helmetSlot; break;
            case ItemType.Armor: targetSlot = armorSlot; break;
            case ItemType.Legs: targetSlot = legsSlot; break;
            case ItemType.Boots: targetSlot = bootsSlot; break;
            case ItemType.Second_Hand: targetSlot = offhandSlot; break;
            case ItemType.Ring:
                // Wolny palec ma pierwszenstwo przed zamiana
                targetSlot = (ring1Slot != null && ring1Slot.item == null) ? ring1Slot : ring2Slot;
                if (targetSlot == null) targetSlot = ring1Slot;
                break;
            case ItemType.Necklace: targetSlot = necklaceSlot; break;
            case ItemType.Ammo: targetSlot = ammoSlot; break;
        }

        if (targetSlot != null && !targetSlot.isBlocked)
        {
            ItemData tempItem = targetSlot.item;
            int tempAmount = targetSlot.amount;

            targetSlot.AddItem(item, slot.amount);

            if (tempItem != null) slot.AddItem(tempItem, tempAmount);
            else slot.ClearSlot();

            OnEquipmentChanged();
        }
    }
}
