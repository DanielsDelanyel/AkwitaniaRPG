using UnityEngine;
using UnityEngine.EventSystems;

public class TradeCenterSlot : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (ShopManager.instance == null || InventoryUI.instance == null) return;

        // SCENARIUSZ A: cos wisi na myszce -> kladziemy to na stol
        if (InventoryUI.instance.draggedItem != null)
        {
            ItemData droppedItem = InventoryUI.instance.draggedItem;
            int droppedAmount = InventoryUI.instance.draggedAmount;

            ShopManager.instance.StageForSell(droppedItem, droppedAmount);
            InventoryUI.instance.ClearDraggedItem();
            return;
        }

        // SCENARIUSZ B (NOWOSC): pusta reka -> zabieramy przedmiot ze stolu z powrotem na myszke
        ShopManager.instance.TakeBackFromStage();
    }
}