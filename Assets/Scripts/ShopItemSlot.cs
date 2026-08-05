using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopItemSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image iconDisplay;
    private ItemData currentItem;

    public void Setup(ItemData item)
    {
        currentItem = item;
        iconDisplay.sprite = item.icon;

        // 1. W³¹czamy sam komponent rysuj¹cy
        iconDisplay.enabled = true;

        // 2. Wymuszamy fizyczne w³¹czenie obiektu w hierarchii (ZABEZPIECZENIE)
        iconDisplay.gameObject.SetActive(true);

        // 3. Upewniamy siê, ¿e kolor nie jest czarny ani przezroczysty (ZABEZPIECZENIE)
        iconDisplay.color = Color.white;
    }

    public void Clear()
    {
        currentItem = null;
        iconDisplay.sprite = null;
        iconDisplay.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem != null) ShopManager.instance.StageForBuy(currentItem);
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