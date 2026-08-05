using UnityEngine;
using UnityEngine.EventSystems;

public class TradeCenterSlot : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // Jeœli gracz ma coœ "przyklejone" do myszki z plecaka, k³adziemy to na stó³
        if (InventoryUI.instance != null && InventoryUI.instance.draggedItem != null)
        {
            ItemData droppedItem = InventoryUI.instance.draggedItem;
            int droppedAmount = InventoryUI.instance.draggedAmount;

            ShopManager.instance.StageForSell(droppedItem, droppedAmount);

            InventoryUI.instance.ClearDraggedItem(); // Zdejmujemy z myszki
        }
    }
}