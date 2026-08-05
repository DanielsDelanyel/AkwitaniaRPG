using UnityEngine;
using UnityEngine.EventSystems;
public class NPCInteraction : MonoBehaviour
{
    [Header("System Dialogowy")]
    public DialogueNode startingNode;

    [Header("Elementy Dialogowe")]
    public GameObject dialogueBubble; // Nasz dymek
    public SpriteRenderer bubbleSpriteRenderer; // ¯eby odwracaæ ogonek dymku

    [Header("Ustawienia Pozycji")]
    public float bubbleHeight = 1.2f; // Jak wysoko nad NPC ma byæ dymek
    public float bubbleOffset = 0.8f; // Jak bardzo na boki ma odskakiwaæ dymek

    private Transform playerTransform;
    private bool isPlayerInRange = false;

    void Start()
    {
        // Na start ukrywamy dymek, ¿eby nie wisia³ w powietrzu
        if (dialogueBubble != null)
        {
            dialogueBubble.SetActive(false);
        }
    }

    void Update()
    {
        // Jeœli gracz jest blisko, dymek na bie¿¹co sprawdza pozycjê
        if (isPlayerInRange && playerTransform != null && dialogueBubble != null)
        {
            UpdateBubblePosition();
        }
    }

    private void UpdateBubblePosition()
    {
        // Sprawdzamy, z której strony jest gracz w stosunku do NPC
        if (playerTransform.position.x < transform.position.x)
        {
            // Gracz jest po LEWEJ stronie -> dymek skacze na PRAW¥ stronê
            dialogueBubble.transform.localPosition = new Vector3(bubbleOffset, bubbleHeight, 0f);

            // Domyœlnie ogonek na Twoim obrazku jest po lewej stronie, wiêc nie musimy go odwracaæ (celuje w dó³-lewo do NPC)
            if (bubbleSpriteRenderer != null) bubbleSpriteRenderer.flipX = false;
        }
        else
        {
            // Gracz jest po PRAWEJ stronie -> dymek skacze na LEW¥ stronê
            dialogueBubble.transform.localPosition = new Vector3(-bubbleOffset, bubbleHeight, 0f);

            // Odwracamy obrazek lustrzanie, ¿eby ogonek celowa³ w dó³-prawo do NPC
            if (bubbleSpriteRenderer != null) bubbleSpriteRenderer.flipX = true;
        }
    }

    // Ta funkcja odpala siê automatycznie, gdy w strefê wejdzie inny obiekt
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Upewniamy siê, ¿e to gracz, a nie np. dzik
        if (collision.CompareTag("Player"))
        {
            playerTransform = collision.transform;
            isPlayerInRange = true;
            if (dialogueBubble != null) dialogueBubble.SetActive(true);
        }
    }

    // Ta funkcja odpala siê, gdy gracz wyjdzie ze strefy
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (dialogueBubble != null) dialogueBubble.SetActive(false);
        }
    }
        private void OnMouseDown()
    {
        // --- ZABEZPIECZENIE 1: Zablokowanie "klikania przez UI" ---
        // Jeœli myszka znajduje siê nad jakimkolwiek elementem Canvasu, ca³kowicie ignorujemy klikniêcie w budynek!
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // --- ZABEZPIECZENIE 2: Sklep lub ekwipunek jest otwarty ---
        // Jeœli okno plecaka (które otwiera siê te¿ w sklepie) jest aktywne, nie pozwalamy na now¹ rozmowê.
        if (InventoryUI.instance != null && InventoryUI.instance.inventoryWindow.activeSelf)
        {
            return;
        }

        if (isPlayerInRange)
        {
            StartDialogue();
        }
        else
        {
            Debug.Log("Musisz podejœæ bli¿ej, aby porozmawiaæ!");
        }
    }

    private void StartDialogue()
    {
        NPCStats stats = GetComponent<NPCStats>();
        bool isMerchant = stats != null && (stats.profession == "Kupiec" || stats.profession == "Kowal");

        if (startingNode != null)
        {
            DialogueManager.instance.StartDialogue(stats, startingNode, isMerchant);
        }
        else
        {
            Debug.LogWarning("Postaæ nie ma przypisanego dialogu!");
        }
    }
}