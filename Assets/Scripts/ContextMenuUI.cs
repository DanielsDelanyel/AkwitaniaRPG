using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContextMenuUI : MonoBehaviour
{
    public static ContextMenuUI instance;

    [Header("UI Elementy")]
    public GameObject menuPanel; // G³ówne okienko menu
    public Button useButton; // Przycisk "U¿yj"
    public TextMeshProUGUI useButtonText; // Tekst na przycisku (¿eby zmieniaæ "Zjedz" na "Za³ó¿")

    private InventorySlot currentSlot; // Zapamiêtuje, w który slot kliknêliœmy

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        CloseMenu(); // Na starcie gry menu musi byæ niewidoczne
    }

    public void OpenMenu(InventorySlot slot, Vector2 mousePos)
    {
        currentSlot = slot;

        // Ustawiamy okienko w miejscu kursora
        menuPanel.transform.position = mousePos;
        menuPanel.SetActive(true);

        // Inteligentna zmiana tekstu na przycisku!
        if (slot.item.itemType == ItemType.Consumable)
        {
            useButtonText.text = "Zjedz / Wypij";
        }
        else
        {
            useButtonText.text = "Za³ó¿";
        }
    }

    public void CloseMenu()
    {
        menuPanel.SetActive(false);
        currentSlot = null;
    }

    // Ta funkcja bêdzie przypisana do przycisku "U¿yj"
    public void OnUseClicked()
    {
        if (currentSlot != null && currentSlot.item != null)
        {
            // Przekazujemy rozkaz u¿ycia do g³ównego mened¿era ekwipunku
            InventoryUI.instance.UseItem(currentSlot);
        }
        CloseMenu();
    }
}