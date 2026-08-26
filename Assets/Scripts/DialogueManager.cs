using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    private DialogueNode currentNode;
    private NPCStats currentNPC;

    [Header("Panel Prezentow")]
    public GameObject giftOverlay;

    public GameObject[] elementsToHide;

    [Header("Glowne Okno")]
    public GameObject dialogueWindow;
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI greetingText;

    [Header("Sklep")]
    public GameObject shopPanel;

    [Header("Opcje Dialogowe")]
    public GameObject[] optionButtons;
    public TextMeshProUGUI[] optionTexts;

    [Header("Przyciski Akcji")]
    public GameObject tradeButton;
    public GameObject giftButton;

    [Header("Blokady")]
    public TopDownMovement playerMovement;

    void Awake()
    {
        instance = this;
    }

    // ===============================================================
    // STAN OKIEN - czyta to UIEscapeHandler oraz InventoryUI i StatsUI,
    // zeby nie otwierac swoich okien w trakcie rozmowy.
    // ===============================================================
    public bool IsDialogueOpen { get { return dialogueWindow != null && dialogueWindow.activeSelf; } }
    public bool IsGiftPanelOpen { get { return giftOverlay != null && giftOverlay.activeSelf; } }
    public bool IsShopOpen { get { return shopPanel != null && shopPanel.activeSelf; } }

    private void DisplayNode(DialogueNode node)
    {
        currentNode = node;
        greetingText.text = node.npcText;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < node.options.Length)
            {
                optionButtons[i].SetActive(true);
                optionTexts[i].text = (i + 1) + ". " + node.options[i].text;
            }
            else
            {
                optionButtons[i].SetActive(false);
            }
        }
    }

    public void StartDialogue(NPCStats npc, DialogueNode startNode, bool canTrade)
    {
        currentNPC = npc;
        dialogueWindow.SetActive(true);

        // Rozmowa zaklada wlasna blokade ruchu. Zdejmie ja dopiero CloseDialogue,
        // wiec otwarcie i zamkniecie plecaka w miedzyczasie jej nie skasuje.
        UILock.Set("Dialogue", true);

        string npcName = npc != null ? npc.npcName : "Nieznajomy";
        nameText.text = npcName;

        if (npc != null && npc.portrait != null)
        {
            portraitImage.sprite = npc.portrait;
            portraitImage.gameObject.SetActive(true);
        }
        else
        {
            portraitImage.gameObject.SetActive(false);
        }

        tradeButton.SetActive(canTrade);
        giftButton.SetActive(true);

        DisplayNode(startNode);
    }

    public void CloseDialogue()
    {
        if (dialogueWindow != null) dialogueWindow.SetActive(false);
        currentNode = null;
        currentNPC = null;

        UILock.Set("Dialogue", false);
    }

    public void OnOptionClicked(int optionIndex)
    {
        if (currentNode == null || optionIndex >= currentNode.options.Length) return;

        DialogueOption selectedOption = currentNode.options[optionIndex];

        if (selectedOption.affinityChange != 0 && currentNPC != null)
        {
            currentNPC.ModifyAffinity(selectedOption.affinityChange);
            Debug.Log($"Sympatia zmienia sie o {selectedOption.affinityChange}. Wynosi teraz: {currentNPC.affinity}");
        }

        if (selectedOption.nextNode != null) DisplayNode(selectedOption.nextNode);
        else CloseDialogue();
    }

    public NPCStats GetCurrentNPC()
    {
        return currentNPC;
    }

    public void OpenGiftPanel()
    {
        if (InventoryUI.instance != null) InventoryUI.instance.inventoryWindow.SetActive(true);
        if (giftOverlay != null) giftOverlay.SetActive(true);
        dialogueWindow.SetActive(false);

        foreach (GameObject obj in elementsToHide)
        {
            if (obj != null) obj.SetActive(false);
        }
    }

    public void CloseGiftPanel()
    {
        if (giftOverlay != null) giftOverlay.SetActive(false);
        if (InventoryUI.instance != null) InventoryUI.instance.inventoryWindow.SetActive(false);

        if (InventoryUI.instance != null && InventoryUI.instance.draggedItem != null)
        {
            InventoryUI.instance.Add(InventoryUI.instance.draggedItem, InventoryUI.instance.draggedAmount);
            InventoryUI.instance.ClearDraggedItem();
        }

        if (currentNPC != null) dialogueWindow.SetActive(true);

        foreach (GameObject obj in elementsToHide)
        {
            if (obj != null) obj.SetActive(true);
        }
    }

    public void TriggerGiftReaction(int affinityChange)
    {
        CloseGiftPanel();

        DialogueNode nextNode = null;

        if (affinityChange >= 5) nextNode = currentNPC.reactionLove;
        else if (affinityChange <= -5) nextNode = currentNPC.reactionHate;
        else nextNode = currentNPC.reactionNeutral;

        if (nextNode != null)
        {
            DisplayNode(nextNode);
        }
        else
        {
            Debug.LogWarning("Brakuje przypisanego wezla reakcji u NPC!");
            CloseDialogue();
        }
    }

    // --- TU BYL BLAD: panel sie wlaczal, ale nikt nie mowil ShopManagerowi, z kim handlujemy ---
    public void OpenShopPanel()
    {
        // ShopManager sam sie odnajduje, nawet gdy jego obiekt jest wylaczony
        ShopManager shop = ShopManager.instance;

        if (shop == null)
        {
            Debug.LogError("Nie znaleziono ShopManagera w scenie! " +
                           "Sprawdz, czy skrypt ShopManager wisi na obiekcie ShopPanel.");
            return;
        }

        // TU BYL CICHY BLAD: gdy pole 'Shop Panel' bylo puste, kod po prostu
        // pomijal aktywacje i szedl dalej, a potem dziwil sie brakiem menedzera.
        if (shopPanel == null)
        {
            Debug.LogWarning("DialogueManager: pole 'Shop Panel' jest puste! " +
                             "Uzywam obiektu ShopManagera. Uzupelnij te referencje w Inspektorze.");
            shopPanel = shop.gameObject;
        }

        shopPanel.SetActive(true);
        shop.OpenShop(currentNPC);

        if (InventoryUI.instance != null) InventoryUI.instance.inventoryWindow.SetActive(true);

        dialogueWindow.SetActive(false);

        foreach (GameObject obj in elementsToHide)
        {
            if (obj != null) obj.SetActive(false);
        }
    }

    public void CloseShopPanel()
    {
        // Najpierw zamykamy sklep - odda gracz owi przedmiot lezacy na stole
        if (ShopManager.instance != null) ShopManager.instance.CloseShop();

        if (shopPanel != null) shopPanel.SetActive(false);

        if (InventoryUI.instance != null && InventoryUI.instance.draggedItem != null)
        {
            InventoryUI.instance.Add(InventoryUI.instance.draggedItem, InventoryUI.instance.draggedAmount);
            InventoryUI.instance.ClearDraggedItem();
        }

        if (InventoryUI.instance != null) InventoryUI.instance.inventoryWindow.SetActive(false);

        if (currentNPC != null) dialogueWindow.SetActive(true);

        foreach (GameObject obj in elementsToHide)
        {
            if (obj != null) obj.SetActive(true);
        }
    }
}