using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Dane Przedmiotu")]
    public ItemData itemData;

    [Header("Ilość (np. dla strza�)")]
    public int amount = 1; // Ile sztuk le�y w tej paczce na trawie

    [Header("Ustawienia")]
    public GameObject promptE;
    private bool isPlayerClose = false;
    private GameObject activePrompt;

    void Start()
    {
        // NOWE: paczka lezaca na ziemi dostaje wlasny, wylosowany egzemplarz przedmiotu.
        // Jesli itemData jest juz egzemplarzem (bo gracz wyrzucil miecz z plecaka),
        // fabryka odda go bez zmian i statystyki zostana zachowane.
        itemData = ItemFactory.Create(itemData);
    }

    void Update()
    {
        if (isPlayerClose && Input.GetKeyDown(KeyCode.E))
        {
            PickUp();
        }
    }

    void PickUp()
    {
        // Add zwraca nam, ile przedmiot�w NIE ZMIE�CI�O SI� do plecaka
        int leftovers = InventoryUI.instance.Add(itemData, amount);

        if (leftovers == 0)
        {
            // Podnie�li�my wszystko!
            Debug.Log($"Podniesiono: {itemData.itemName} ({amount} szt.)");
            if (activePrompt != null) Destroy(activePrompt);
            Destroy(gameObject); // Niszczymy obiekt na trawie
        }
        else if (leftovers < amount)
        {
            // Zmie�ci�a si� tylko cz��
            Debug.Log($"Plecak pełny! Na ziemi zostało {leftovers} szt.");
            amount = leftovers; // Redukujemy stos na ziemi do samej resztki
        }
        else
        {
            Debug.Log("Całkowity brak miejsca w plecaku!");
        }
    }

    // ... Reszta kodu OnTriggerEnter/Exit bez zmian ...
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerClose = true;
            if (activePrompt == null && promptE != null)
            {
                activePrompt = Instantiate(promptE, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                activePrompt.transform.SetParent(transform);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerClose = false;
            if (activePrompt != null) Destroy(activePrompt);
        }
    }
}