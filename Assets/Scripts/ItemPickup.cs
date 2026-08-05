using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Dane Przedmiotu")]
    public ItemData itemData;

    [Header("Iloœæ (np. dla strza³)")]
    public int amount = 1; // Ile sztuk le¿y w tej paczce na trawie

    [Header("Ustawienia")]
    public GameObject promptE;
    private bool isPlayerClose = false;
    private GameObject activePrompt;

    void Update()
    {
        if (isPlayerClose && Input.GetKeyDown(KeyCode.E))
        {
            PickUp();
        }
    }

    void PickUp()
    {
        // Add zwraca nam, ile przedmiotów NIE ZMIEŒCI£O SIÊ do plecaka
        int leftovers = InventoryUI.instance.Add(itemData, amount);

        if (leftovers == 0)
        {
            // Podnieœliœmy wszystko!
            Debug.Log($"Podniesiono: {itemData.itemName} ({amount} szt.)");
            if (activePrompt != null) Destroy(activePrompt);
            Destroy(gameObject); // Niszczymy obiekt na trawie
        }
        else if (leftovers < amount)
        {
            // Zmieœci³a siê tylko czêœæ
            Debug.Log($"Plecak pe³ny! Na ziemi zosta³o {leftovers} szt.");
            amount = leftovers; // Redukujemy stos na ziemi do samej resztki
        }
        else
        {
            Debug.Log("Ca³kowity brak miejsca w plecaku!");
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