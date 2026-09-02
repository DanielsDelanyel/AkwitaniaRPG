using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Dane Przedmiotu")]
    public ItemData itemData;

    [Header("Ilosc (np. dla strzal)")]
    public int amount = 1; // Ile sztuk lezy w tej paczce na trawie

    [Header("Ustawienia")]
    public GameObject promptE;
    private bool isPlayerClose = false;
    private GameObject activePrompt;

    // Tylko przedmioty ROZSTAWIONE W EDYTORZE maja UniqueId.
    // Te wypadajace ze skrzyn i przeciwnikow go nie maja i nie sa zapisywane.
    private UniqueId uniqueId;
    private string SaveId
    {
        get { return uniqueId != null ? "pickup_" + uniqueId.Id : null; }
    }

    void Start()
    {
        uniqueId = GetComponent<UniqueId>();

        // Gracz zabral juz ten przedmiot w poprzedniej sesji
        if (uniqueId != null && WorldState.HasFlag(SaveId))
        {
            Destroy(gameObject);
            return;
        }

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
        // Add zwraca nam, ile przedmiotow NIE ZMIESCILO SIE do plecaka
        int leftovers = InventoryUI.instance.Add(itemData, amount);

        if (leftovers == 0)
        {
            // Podnieslismy wszystko!
            Debug.Log($"Podniesiono: {itemData.itemName} ({amount} szt.)");
            SoundManager.PlayPickupSound(itemData);

            // Rozstawiony w edytorze - zapamietujemy, ze juz go nie ma
            if (uniqueId != null) WorldState.SetFlag(SaveId);
            if (activePrompt != null) Destroy(activePrompt);
            Destroy(gameObject); // Niszczymy obiekt na trawie
        }
        else if (leftovers < amount)
        {
            // Zmiescila sie tylko czesc - i tak cos trafilo do plecaka, wiec dzwiek gra.
            Debug.Log($"Plecak pelny! Na ziemi zostalo {leftovers} szt.");
            SoundManager.PlayPickupSound(itemData);
            amount = leftovers; // Redukujemy stos na ziemi do samej resztki
        }
        else
        {
            Debug.Log("Calkowity brak miejsca w plecaku!");
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
