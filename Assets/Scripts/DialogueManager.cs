using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    private DialogueNode currentNode;
    private NPCStats currentNPC;

    [Header("Panel Prezentów")]
    public GameObject giftOverlay; // <-- Zmieniona nazwa. Tutaj przeci¹gniesz swój obiekt 'Gift' (Z³ote pude³ko)

    public GameObject[] elementsToHide;

    [Header("G³ówne Okno")]
    public GameObject dialogueWindow;
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI greetingText;

    [Header("Sklep")]
    public GameObject shopPanel;


    [Header("Opcje Dialogowe")]
    public GameObject[] optionButtons; // Przeci¹gnij tu wszystkie 4 przyciski opcji
    public TextMeshProUGUI[] optionTexts; // Przeci¹gnij tu teksty znajduj¹ce siê w tych przyciskach

    [Header("Przyciski Akcji")]
    public GameObject tradeButton;
    public GameObject giftButton;

    [Header("Blokady")]
    public TopDownMovement playerMovement;

    void Awake()
    {
        instance = this; // Singleton - ³atwy dostêp z ka¿dego innego skryptu!
    }

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

    // Ta funkcja jest wywo³ywana przez NPC po klikniêciu
    public void StartDialogue(NPCStats npc, DialogueNode startNode, bool canTrade)
    {
        currentNPC = npc;
        dialogueWindow.SetActive(true);

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            playerMovement.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }

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

        // Wyœwietlamy pierwszy klocek rozmowy
        DisplayNode(startNode);
    }

    public void CloseDialogue()
    {
        dialogueWindow.SetActive(false);
        currentNode = null;
        currentNPC = null;

        if (playerMovement != null) playerMovement.enabled = true;
    }

    // Tymczasowe funkcje dla przycisków, dopóki nie zbudujemy drzewka
    public void OnOptionClicked(int optionIndex)
    {
        if (currentNode == null || optionIndex >= currentNode.options.Length) return;

        DialogueOption selectedOption = currentNode.options[optionIndex];

        // 1. Zmiana Sympatii postaci!
        if (selectedOption.affinityChange != 0 && currentNPC != null)
        {
            currentNPC.ModifyAffinity(selectedOption.affinityChange);
            Debug.Log($"Sympatia Lassi zmienia siê o {selectedOption.affinityChange}. Wynosi teraz: {currentNPC.affinity}");
        }

        // 2. Skok do kolejnego dialogu lub zamkniêcie okna
        if (selectedOption.nextNode != null)
        {
            DisplayNode(selectedOption.nextNode); // Jeœli jest kolejny krok, ³adujemy go!
        }
        else
        {
            CloseDialogue(); // Jeœli pole 'nextNode' jest puste, koñczymy rozmowê!
        }
    }

    // Pozwala skryptowi z³otego pude³ka dowiedzieæ siê, komu dajemy prezent
    public NPCStats GetCurrentNPC()
    {
        return currentNPC;
    }

    public void OpenGiftPanel()
    {
        if (InventoryUI.instance != null) InventoryUI.instance.inventoryWindow.SetActive(true);
        if (giftOverlay != null) giftOverlay.SetActive(true);
        dialogueWindow.SetActive(false);

        // --- NOWOŒÆ: Wy³¹czamy wszystkie elementy z listy ---
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

        if (currentNPC != null)
        {
            dialogueWindow.SetActive(true);
        }

        // --- NOWOŒÆ: W³¹czamy je z powrotem, by zwyk³y ekwipunek dzia³a³ normalnie! ---
        foreach (GameObject obj in elementsToHide)
        {
            if (obj != null) obj.SetActive(true);
        }
    }

    // Ta funkcja odpala siê w momencie klikniêcia na z³ote pude³ko z przedmiotem w d³oni
    public void TriggerGiftReaction(int affinityChange)
    {
        CloseGiftPanel(); // Zamykamy plecak z prezentami

        DialogueNode nextNode = null;

        // Wybieramy reakcjê na podstawie punktów. Progi mo¿esz dowolnie modyfikowaæ!
        if (affinityChange >= 5) nextNode = currentNPC.reactionLove;
        else if (affinityChange <= -5) nextNode = currentNPC.reactionHate;
        else nextNode = currentNPC.reactionNeutral;

        if (nextNode != null)
        {
            DisplayNode(nextNode); // £adujemy nowy tekst do okna dialogowego!
        }
        else
        {
            Debug.LogWarning("Brakuje przypisanego wêz³a reakcji u NPC!");
            CloseDialogue();
        }
    }

    // Funkcja wywo³ywana po klikniêciu przycisku z monetami w oknie dialogowym
    public void OpenShopPanel()
    {
        // Pokazujemy przyciemniony ekran z kowalem
        if (shopPanel != null) shopPanel.SetActive(true);

        // Otwieramy prawdziwy ekwipunek gracza, by mia³ czym handlowaæ
        if (InventoryUI.instance != null) InventoryUI.instance.inventoryWindow.SetActive(true);

        // Tymczasowo ukrywamy dymek dialogowy
        dialogueWindow.SetActive(false);

        // Opcjonalnie: ukrywamy statystyki postaci (tak jak przy prezentach)
        foreach (GameObject obj in elementsToHide)
        {
            if (obj != null) obj.SetActive(false);
        }
    }

    // Funkcja wywo³ywana przyciskiem wyjœcia w nowym oknie sklepu
    public void CloseShopPanel()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (InventoryUI.instance != null) InventoryUI.instance.inventoryWindow.SetActive(false);

        // Zabezpieczenie zrzutu z myszki
        if (InventoryUI.instance != null && InventoryUI.instance.draggedItem != null)
        {
            InventoryUI.instance.Add(InventoryUI.instance.draggedItem, InventoryUI.instance.draggedAmount);
            InventoryUI.instance.ClearDraggedItem();
        }

        // Przywracamy okno dialogowe i ukryte elementy
        if (currentNPC != null) dialogueWindow.SetActive(true);

        foreach (GameObject obj in elementsToHide)
        {
            if (obj != null) obj.SetActive(true);
        }
        ShopManager.instance.CloseShop();
    }
}