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

    [Header("Blokada Slotu (np. na broñ 2H)")]
    public bool isBlocked = false;
    private Image slotBackground;
    private Color normalColor;
    private Color blockedColor = new Color(0.2f, 0.2f, 0.2f, 1f); // Ciemnoszary, "wy³¹czony" kolor

    void Awake()
    {
        // Pobieramy t³o slota, by móc je przyciemniaæ
        slotBackground = GetComponent<Image>();
        if (slotBackground != null)
        {
            normalColor = slotBackground.color;
        }
    }

    void Start()
    {
        if (iconDisplay == null || iconDisplay.gameObject.activeInHierarchy == false)
        {
            iconDisplay = transform.Find("Icon").GetComponent<Image>();
        }
        if (item == null) ClearSlot();
    }

    // Funkcja blokuj¹ca/odblokowuj¹ca slot
    public void SetBlocked(bool blocked)
    {
        isBlocked = blocked;
        if (slotBackground != null)
        {
            slotBackground.color = blocked ? blockedColor : normalColor;
        }
    }

    public void AddItem(ItemData newItem, int newAmount = 1)
    {
        item = newItem;
        amount = newAmount;
        iconDisplay.sprite = item.icon;
        iconDisplay.preserveAspect = true;
        iconDisplay.enabled = true;
        UpdateSlotUI();
    }

    public void ClearSlot()
    {
        item = null;
        amount = 0;
        iconDisplay.sprite = null;
        if (iconDisplay != null) iconDisplay.enabled = false;
        UpdateSlotUI();
    }

    // NOWA FUNKCJA: Odœwie¿a napis w prawym dolnym rogu
    public void UpdateSlotUI()
    {
        if (amountText != null)
        {
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
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isBlocked) return;

        // --- NOWOŒÆ: PRAWY PRZYCISK MYSZY (Otwiera menu) ---
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Otwieramy menu tylko, jeœli w slocie coœ jest i jesteœmy w plecaku (a nie na slocie zbroi)
            if (item != null && isBackpackSlot)
            {
                ContextMenuUI.instance.OpenMenu(this, Input.mousePosition);
            }
            return; // Przerywamy kod, ¿eby nie podnieœæ przedmiotu!
        }

        ItemData mouseItem = InventoryUI.instance.draggedItem;
        int mouseAmount = InventoryUI.instance.draggedAmount; // Pamiêtamy iloœæ na myszce!

        // --- INTELIGENTNE PRZEKIEROWANIE BRONI 2H I £UKU ---
        if (allowedType1 == ItemType.Second_Hand && mouseItem != null)
        {
            if (mouseItem.itemType == ItemType.Weapon2h || mouseItem.itemType == ItemType.Bow)
            {
                InventoryUI.instance.weaponSlot.OnPointerClick(null);
                return;
            }
        }

        // SCENARIUSZ A: Podnoszenie (Nic na myszce)
        if (mouseItem == null)
        {
            if (item != null)
            {
                InventoryUI.instance.SetDraggedItem(item, amount); // Podnosimy z iloœci¹!
                ClearSlot();
            }
        }
        // SCENARIUSZ B: K³adzenie / Zamiana / £¥CZENIE STOSÓW (Coœ na myszce)
        else
        {
            if (CheckIfItemFits(mouseItem))
            {
                ItemData itemInSlot = item;
                int amountInSlot = amount; // Zapamiêtujemy iloœæ w slocie 

                // --- NOWOŒÆ: RÊCZNE £¥CZENIE STOSÓW (STACKOWANIE) ---
                // Jeœli w slocie jest ten sam przedmiot co na myszce i mo¿na go stackowaæ:
                if (itemInSlot == mouseItem && mouseItem.isStackable)
                {
                    int maxStack = 100; // Limit stosu (taki sam jak w InventoryUI)
                    int totalAmount = amountInSlot + mouseAmount;

                    if (totalAmount <= maxStack)
                    {
                        // Jeœli suma zmieœci siê w slocie, wk³adamy wszystko i czyœcimy myszkê
                        AddItem(mouseItem, totalAmount);
                        InventoryUI.instance.ClearDraggedItem();
                    }
                    else
                    {
                        // Jeœli suma przekracza limit, wype³niamy slot do pe³na (100)
                        AddItem(mouseItem, maxStack);

                        // Obliczamy resztê i zostawiamy j¹ "przyklejon¹" do myszki!
                        int leftovers = totalAmount - maxStack;
                        InventoryUI.instance.SetDraggedItem(mouseItem, leftovers);
                    }
                }
                // ZWYK£A ZAMIANA (Przedmioty s¹ ró¿ne LUB nie da siê ich stackowaæ)
                else
                {
                    // K³adziemy myszkê do slota z w³aœciw¹ iloœci¹
                    AddItem(mouseItem, mouseAmount);

                    // Jeœli coœ tu by³o, weŸ to na myszkê z w³aœciw¹ iloœci¹
                    if (itemInSlot != null) InventoryUI.instance.SetDraggedItem(itemInSlot, amountInSlot);
                    else InventoryUI.instance.ClearDraggedItem();
                }
            }
            else
            {
                Debug.Log("Ten przedmiot tu nie pasuje!");
            }
        }

        if (isBackpackSlot == false)
        {
            InventoryUI.instance.OnEquipmentChanged();
        }
    }
    bool CheckIfItemFits(ItemData itemToCheck)
    {
        if (isBackpackSlot) return true;
        if (itemToCheck.itemType == allowedType1) return true;
        if (itemToCheck.itemType == allowedType2) return true;
        if (itemToCheck.itemType == allowedType3) return true;
        return false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item != null && InventoryUI.instance.draggedItem == null)
        {
            InventoryTooltip.instance.ShowTooltip(item);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InventoryTooltip.instance.HideTooltip();
    }
}