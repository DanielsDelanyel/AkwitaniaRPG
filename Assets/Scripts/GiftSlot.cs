using UnityEngine;
using UnityEngine.EventSystems; // Biblioteka do klikania myszk¹

public class GiftSlot : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // Sprawdzamy, czy gracz faktycznie "przyklei³" coœ do myszki z poziomu InventoryUI
        if (InventoryUI.instance != null && InventoryUI.instance.draggedItem != null)
        {
            ItemData giftItem = InventoryUI.instance.draggedItem;
            int giftAmount = InventoryUI.instance.draggedAmount;

            NPCStats npc = DialogueManager.instance.GetCurrentNPC();

            if (npc != null)
            {
                // 1. Zmieniamy sympatiê i pobieramy wynik
                int affinityChange = npc.ReceiveGift(giftItem);

                // 2. Zabieramy graczowi JEDN¥ sztukê z myszki
                if (giftAmount > 1)
                {
                    InventoryUI.instance.SetDraggedItem(giftItem, giftAmount - 1);
                }
                else
                {
                    InventoryUI.instance.ClearDraggedItem();
                }

                // 3. Odpalamy ga³¹Ÿ dialogow¹ (Zachwyt, Obojêtnoœæ, Odraza)
                DialogueManager.instance.TriggerGiftReaction(affinityChange);
            }
        }
        else
        {
            Debug.Log("Myszka jest pusta! Przeci¹gnij tu przedmiot z ekwipunku.");
        }
    }
}