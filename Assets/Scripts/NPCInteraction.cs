using UnityEngine;
using UnityEngine.EventSystems;
public class NPCInteraction : MonoBehaviour
{
    [Header("System Dialogowy")]
    public DialogueNode startingNode;

    [Header("Elementy Dialogowe")]
    public GameObject dialogueBubble; // Nasz dymek
    public SpriteRenderer bubbleSpriteRenderer; // �eby odwraca� ogonek dymku

    [Header("Ustawienia Pozycji")]
    public float bubbleHeight = 1.2f; // Jak wysoko nad NPC ma by� dymek
    public float bubbleOffset = 0.8f; // Jak bardzo na boki ma odskakiwa� dymek

    private Transform playerTransform;
    private bool isPlayerInRange = false;

    // ===============================================================
    // NOWE: NPC, ktore jest jednoczesnie stworzeniem (np. Ent/drzewiec) - taki
    // obiekt ma OBOK tego skryptu takze komponent Creature. Dopoki jego
    // Disposition to Peaceful/Neutral, mozna z nim gadac normalnie. Ale gdy
    // ktos go uderzy, Creature.TakeDamage() samo przelacza Neutral->Aggressive
    // - a od tego momentu rozmowa ma byc zablokowana (jest zbyt wsciekle, zeby
    // gadac). Zwykli NPC (kupiec, kowal itd.) nie maja Creature, wiec creature
    // zostanie null i caly ten mechanizm po prostu sie nie wlacza.
    private Creature creature;

    // NOWE: kara do sympatii za sprowokowanie (uderzenie) takiego NPC.
    // Naliczana RAZ na kazda prowokacje (Creature.onProvoked), a nie za
    // kazdy cios. Dziala tylko, gdy obok sa komponenty Creature i NPCStats.
    [Header("Prowokacja (tylko NPC-stworzenia, np. Ent)")]
    [Tooltip("O ile spada sympatia, gdy gracz uderzy tego NPC i go rozwscieczy. 0 = bez kary.")]
    public int affinityPenaltyOnProvoke = 15;

    private bool CanTalkNow()
    {
        return creature == null || creature.disposition != Disposition.Aggressive;
    }

    void Start()
    {
        creature = GetComponent<Creature>();
        if (creature != null) creature.onProvoked += OnProvoked;

        // Na start ukrywamy dymek, zeby nie wisial w powietrzu
        if (dialogueBubble != null)
        {
            dialogueBubble.SetActive(false);
        }
    }

    void Update()
    {
        // Stworzenie zdazylo sie rozwscieczyc (np. gracz je uderzyl) - chowamy
        // dymek i nie aktualizujemy juz jego pozycji, dopoki tak zostanie.
        if (!CanTalkNow())
        {
            if (dialogueBubble != null && dialogueBubble.activeSelf) dialogueBubble.SetActive(false);
            return;
        }

        // Jesli gracz jest blisko, dymek na biezaco sprawdza pozycje
        if (isPlayerInRange && playerTransform != null && dialogueBubble != null)
        {
            UpdateBubblePosition();
        }
    }

    private void UpdateBubblePosition()
    {
        // Sprawdzamy, z kt�rej strony jest gracz w stosunku do NPC
        if (playerTransform.position.x < transform.position.x)
        {
            // Gracz jest po LEWEJ stronie -> dymek skacze na PRAW� stron�
            dialogueBubble.transform.localPosition = new Vector3(bubbleOffset, bubbleHeight, 0f);

            // Domy�lnie ogonek na Twoim obrazku jest po lewej stronie, wi�c nie musimy go odwraca� (celuje w d�-lewo do NPC)
            if (bubbleSpriteRenderer != null) bubbleSpriteRenderer.flipX = false;
        }
        else
        {
            // Gracz jest po PRAWEJ stronie -> dymek skacze na LEW� stron�
            dialogueBubble.transform.localPosition = new Vector3(-bubbleOffset, bubbleHeight, 0f);

            // Odwracamy obrazek lustrzanie, �eby ogonek celowa� w d�-prawo do NPC
            if (bubbleSpriteRenderer != null) bubbleSpriteRenderer.flipX = true;
        }
    }

    private void OnProvoked(Creature c)
    {
        if (affinityPenaltyOnProvoke == 0) return;

        NPCStats stats = GetComponent<NPCStats>();
        if (stats == null) return;

        stats.ModifyAffinity(-affinityPenaltyOnProvoke);
        Debug.Log($"{stats.npcName} zostal sprowokowany! Sympatia -{affinityPenaltyOnProvoke} (teraz {stats.affinity}).");
    }

    void OnDestroy()
    {
        if (creature != null) creature.onProvoked -= OnProvoked;
    }

    // Ta funkcja odpala sie automatycznie, gdy w strefe wejdzie inny obiekt
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Upewniamy sie, ze to gracz, a nie np. dzik
        if (collision.CompareTag("Player"))
        {
            playerTransform = collision.transform;
            isPlayerInRange = true;

            // Nie pokazujemy dymka "mozna pogadac", jesli stworzenie jest juz Aggressive
            if (dialogueBubble != null) dialogueBubble.SetActive(CanTalkNow());
        }
    }

    // Ta funkcja odpala si�, gdy gracz wyjdzie ze strefy
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
        // Je�li myszka znajduje si� nad jakimkolwiek elementem Canvasu, ca�kowicie ignorujemy klikni�cie w budynek!
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // --- ZABEZPIECZENIE 2: Sklep lub ekwipunek jest otwarty ---
        // Jesli okno plecaka (ktore otwiera sie tez w sklepie) jest aktywne, nie pozwalamy na nowa rozmowe.
        if (InventoryUI.instance != null && InventoryUI.instance.inventoryWindow.activeSelf)
        {
            return;
        }

        // --- ZABEZPIECZENIE 3: Stworzenie jest wsciekle (Disposition.Aggressive) ---
        // Dotyczy tylko NPC, ktore sa jednoczesnie Creature (np. Ent) - patrz komentarz
        // przy polu "creature" wyzej. Po sprowokowaniu nie da sie juz z nim pogadac.
        if (!CanTalkNow())
        {
            Debug.Log($"{name} jest zbyt wsciekle, zeby teraz rozmawiac!");
            return;
        }

        if (isPlayerInRange)
        {
            StartDialogue();
        }
        else
        {
            Debug.Log("Musisz podejsc blizej, aby porozmawiac!");
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
            Debug.LogWarning("Posta� nie ma przypisanego dialogu!");
        }
    }
}