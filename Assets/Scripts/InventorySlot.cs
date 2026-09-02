using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Image iconDisplay;
    public ItemData item;

    public int amount = 0;
    public TMPro.TextMeshProUGUI amountText;

    [Header("Konfiguracja Slotu")]
    public bool isBackpackSlot = true;
    public ItemType allowedType1;
    public ItemType allowedType2;
    public ItemType allowedType3;

    [Tooltip("NOWE: dla slotow ktore musza przyjmowac wiecej niz 3 typy (np. slot broni: " +
             "Weapon1h/Weapon2h/Bow juz zajmuja allowedType1-3, wiec Wand1h/Wand2h dopisz tutaj). " +
             "Zostaw puste, jesli 3 pola powyzej wystarczaja - nie trzeba nic tu zmieniac.")]
    public ItemType[] additionalAllowedTypes;

    [Header("Blokada Slotu (np. na bron 2H)")]
    public bool isBlocked = false;
    private Image slotBackground;
    private Color normalColor;
    private Color blockedColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    void Awake()
    {
        slotBackground = GetComponent<Image>();
        if (slotBackground != null) normalColor = slotBackground.color;
    }

    void Start()
    {
        if (iconDisplay == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null) iconDisplay = iconTransform.GetComponent<Image>();

            if (iconDisplay == null)
            {
                Debug.LogError($"Slot '{name}' nie ma przypisanego Icon Display ani dziecka o nazwie 'Icon'!");
                return;
            }
        }

        if (item == null) ClearSlot();
    }

    public void SetBlocked(bool blocked)
    {
        isBlocked = blocked;
        if (slotBackground != null)
            slotBackground.color = blocked ? blockedColor : normalColor;
    }

    public void AddItem(ItemData newItem, int newAmount = 1)
    {
        item = newItem;
        amount = newAmount;

        if (iconDisplay != null)
        {
            iconDisplay.sprite = item != null ? item.icon : null;
            iconDisplay.preserveAspect = true;
            iconDisplay.enabled = item != null;
        }
        UpdateSlotUI();
    }

    public void ClearSlot()
    {
        item = null;
        amount = 0;

        if (iconDisplay != null)
        {
            iconDisplay.sprite = null;
            iconDisplay.enabled = false;
        }
        UpdateSlotUI();
    }

    public void UpdateSlotUI()
    {
        if (amountText == null) return;

        if (item != null && amount > 1)
        {
            amountText.text = amount.ToString();
            amountText.gameObject.SetActive(true);
        }
        else
        {
            amountText.gameObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isBlocked) return;

        // --- PRAWY PRZYCISK: menu kontekstowe ---
        // UWAGA: eventData bywa NULL, bo jeden slot potrafi wolac ten kod
        // na drugim (przekierowanie broni 2H). Bez tej ochrony gra sie wywalala.
        if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
        {
            // NOWE: menu kontekstowe teraz otwiera sie takze na slotach EKWIPUNKU,
            // nie tylko w plecaku (ConfigureButtons samo dobiera odpowiednie przyciski
            // - dla zalozonego przedmiotu "Zaloz" zamienia sie na "Zdejmij").
            if (item != null && ContextMenuUI.instance != null)
            {
                if (InventoryTooltip.instance != null) InventoryTooltip.instance.HideTooltip();
                ContextMenuUI.instance.OpenMenu(this, Input.mousePosition);
            }
            return;
        }

        // Kliknięcie lewym zamyka otwarte menu
        if (ContextMenuUI.instance != null && ContextMenuUI.instance.IsOpen)
            ContextMenuUI.instance.CloseMenu();

        if (InventoryUI.instance == null) return;

        ItemData mouseItem = InventoryUI.instance.draggedItem;
        int mouseAmount = InventoryUI.instance.draggedAmount;

        // --- PRZEKIEROWANIE BRONI 2H, LUKU I ROZDZKI 2H DO GLOWNEJ REKI ---
        if (allowedType1 == ItemType.Second_Hand && mouseItem != null)
        {
            if (mouseItem.itemType == ItemType.Weapon2h || mouseItem.itemType == ItemType.Bow
                || mouseItem.itemType == ItemType.Wand2h)
            {
                if (InventoryUI.instance.weaponSlot != null)
                    InventoryUI.instance.weaponSlot.OnPointerClick(null);
                return;
            }
        }

        // SCENARIUSZ A: podnoszenie (pusta reka)
        if (mouseItem == null)
        {
            if (item != null)
            {
                InventoryUI.instance.SetDraggedItem(item, amount);
                ClearSlot();
            }
        }
        // SCENARIUSZ B: kladzenie / zamiana / laczenie stosow
        else
        {
            if (CheckIfItemFits(mouseItem))
            {
                ItemData itemInSlot = item;
                int amountInSlot = amount;

                if (itemInSlot == mouseItem && mouseItem.isStackable)
                {
                    int maxStack = InventoryUI.instance.maxStackSize;
                    int totalAmount = amountInSlot + mouseAmount;

                    if (totalAmount <= maxStack)
                    {
                        AddItem(mouseItem, totalAmount);
                        InventoryUI.instance.ClearDraggedItem();
                    }
                    else
                    {
                        AddItem(mouseItem, maxStack);
                        InventoryUI.instance.SetDraggedItem(mouseItem, totalAmount - maxStack);
                    }
                }
                else
                {
                    AddItem(mouseItem, mouseAmount);

                    if (itemInSlot != null) InventoryUI.instance.SetDraggedItem(itemInSlot, amountInSlot);
                    else InventoryUI.instance.ClearDraggedItem();
                }
            }
            else
            {
                Debug.Log("Ten przedmiot tu nie pasuje!");
            }
        }

        if (!isBackpackSlot) InventoryUI.instance.OnEquipmentChanged();
    }

    bool CheckIfItemFits(ItemData itemToCheck)
    {
        if (itemToCheck == null) return false;
        if (isBackpackSlot) return true;
        if (itemToCheck.itemType == allowedType1) return true;
        if (itemToCheck.itemType == allowedType2) return true;
        if (itemToCheck.itemType == allowedType3) return true;

        if (additionalAllowedTypes != null)
        {
            foreach (ItemType t in additionalAllowedTypes)
            {
                if (itemToCheck.itemType == t) return true;
            }
        }

        return false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item == null) return;
        if (InventoryUI.instance == null || InventoryUI.instance.draggedItem != null) return;

        // Nie zaslaniamy otwartego menu kontekstowego
        if (ContextMenuUI.instance != null && ContextMenuUI.instance.IsOpen) return;

        if (InventoryTooltip.instance != null)
            InventoryTooltip.instance.ShowTooltip(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (InventoryTooltip.instance != null)
            InventoryTooltip.instance.HideTooltip();
    }
}
