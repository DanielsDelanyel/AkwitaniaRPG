using UnityEngine;
using UnityEngine.EventSystems;

public class GiftSlot : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (InventoryUI.instance == null) return;

        if (DialogueManager.instance == null)
        {
            Debug.LogError("GiftSlot: brak DialogueManagera w scenie!");
            return;
        }

        // Pusta reka - nie ma czego wreczac
        if (InventoryUI.instance.draggedItem == null)
        {
            Debug.Log("Myszka jest pusta! Przeciagnij tu przedmiot z ekwipunku.");
            return;
        }

        NPCStats npc = DialogueManager.instance.GetCurrentNPC();
        if (npc == null)
        {
            // ZABEZPIECZENIE: wczesniej przedmiot zostawal na kursorze bez slowa
            // wyjasnienia i gracz nie wiedzial, dlaczego nic sie nie stalo.
            Debug.LogWarning("GiftSlot: nie wiadomo, komu wreczamy prezent - " +
                             "rozmowa zostala zakonczona przedwczesnie.");
            return;
        }

        ItemData giftItem = InventoryUI.instance.draggedItem;
        int giftAmount = InventoryUI.instance.draggedAmount;

        // 1. Zmieniamy sympatie i pobieramy wynik
        int affinityChange = npc.ReceiveGift(giftItem);

        // 2. Zabieramy graczowi JEDNA sztuke z kursora
        if (giftAmount > 1) InventoryUI.instance.SetDraggedItem(giftItem, giftAmount - 1);
        else InventoryUI.instance.ClearDraggedItem();

        // 3. Odpalamy galaz dialogowa (Zachwyt, Obojetnosc, Odraza)
        DialogueManager.instance.TriggerGiftReaction(affinityChange);
    }
}