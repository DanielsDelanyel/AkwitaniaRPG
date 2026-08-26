using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopItemSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image iconDisplay;
    private ItemData currentItem;

    void Awake()
    {
        // Gdyby ktos zapomnial przeciagnac ikony w Inspektorze
        if (iconDisplay == null)
        {
            Transform icon = transform.Find("Icon");
            if (icon != null) iconDisplay = icon.GetComponent<Image>();
            if (iconDisplay == null) iconDisplay = GetComponentInChildren<Image>(true);
        }
    }

    public void Setup(ItemData item)
    {
        currentItem = item;

        if (iconDisplay == null)
        {
            Debug.LogError($"ShopItemSlot '{name}' nie ma przypisanego Icon Display!");
            return;
        }

        if (item == null) { Clear(); return; }

        iconDisplay.sprite = item.icon;
        iconDisplay.preserveAspect = true;
        iconDisplay.enabled = true;
        iconDisplay.gameObject.SetActive(true);
        iconDisplay.color = Color.white;
    }

    public void Clear()
    {
        currentItem = null;

        if (iconDisplay == null) return;

        iconDisplay.sprite = null;
        iconDisplay.enabled = false;

        // SYMETRIA: Setup wlacza obiekt, wiec Clear tez musi go zostawic
        // wlaczonego - inaczej kolejne Setup nie mialoby czego wlaczyc,
        // gdyby ktos w miedzyczasie ruszyl hierarchie.
        iconDisplay.gameObject.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null) return;

        if (ShopManager.instance == null)
        {
            Debug.LogError("ShopItemSlot: brak ShopManagera!");
            return;
        }

        ShopManager.instance.StageForBuy(currentItem);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null && InventoryTooltip.instance != null)
            InventoryTooltip.instance.ShowTooltip(currentItem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (InventoryTooltip.instance != null) InventoryTooltip.instance.HideTooltip();
    }
}