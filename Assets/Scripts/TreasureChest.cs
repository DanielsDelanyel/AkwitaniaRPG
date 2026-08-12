using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer))]
public class TreasureChest : MonoBehaviour
{
    [Header("Rzadkosc i Lup")]
    public ItemRarity rarity = ItemRarity.Common;
    public LootTable lootTable;

    [Header("Interakcja")]
    [Tooltip("Z jakiej odleglosci gracz moze otworzyc skrzynie (w jednostkach Unity).")]
    public float interactionRange = 2f;
    public string playerTag = "Player";
    [Tooltip("Dodatkowo: otwieranie klawiszem, gdy stoisz blisko.")]
    public bool allowKeyOpen = true;
    public KeyCode openKey = KeyCode.E;

    [Header("Poswiata (gdy gracz blisko)")]
    [Tooltip("Dziecko skrzyni: kopia sprite'a, troche wieksza, rysowana POD skrzynia.")]
    public SpriteRenderer glowRenderer;
    public bool tintGlowByRarity = true;
    public float glowMinAlpha = 0.15f;
    public float glowMaxAlpha = 0.55f;
    public float glowPulseSpeed = 2.5f;

    [Header("Animacja Otwierania")]
    [Tooltip("Sposob A: Animator z triggerem. Zostaw puste, jesli uzywasz klatek ponizej.")]
    public Animator animator;
    public string openTrigger = "Open";

    [Tooltip("Sposob B: klatki wprost ze spritesheeta (prostsze, bez Animatora).")]
    public Sprite[] openFrames;
    public float framesPerSecond = 12f;

    [Header("Promienie Rzadkosci")]
    public ChestRayEffect rayPrefab;
    public Vector2 rayOffset = new Vector2(0f, 0.35f);

    [Header("Wyrzucanie Lupu")]
    [Tooltip("Uzywany, gdy ItemData nie ma wlasnego Item Prefab.")]
    public GameObject genericPickupPrefab;
    public Vector2 spawnOffset = new Vector2(0f, 0.3f); // skad wylatuje przedmiot
    public float lootDelay = 0.4f;          // opoznienie po kliknieciu (czas na animacje)
    public float delayBetweenItems = 0.12f;
    public float popHeight = 1.1f;          // jak wysoko podskakuje po luku
    public float popDistanceMin = 0.5f;
    public float popDistanceMax = 1.4f;
    public float popDuration = 0.55f;
    [Tooltip("O ile nizej od srodka skrzyni ladunek ma wyladowac (0 = dokladnie ten sam Y).")]
    public float landOffsetY = 0f;

    [Header("Dzwiek (opcjonalnie)")]
    public AudioClip openSound;
    [Range(0f, 1f)] public float openVolume = 0.7f;

    private SpriteRenderer sr;
    private Transform playerTransform;
    private bool isPlayerClose;
    private bool isOpened;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (glowRenderer != null)
        {
            if (tintGlowByRarity)
            {
                Color c = RarityUtils.GetColor(rarity);
                c.a = 0f;
                glowRenderer.color = c;
            }
            glowRenderer.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (isOpened) return;

        if (playerTransform == null) FindPlayer();
        if (playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        isPlayerClose = distance <= interactionRange;

        UpdateGlow();

        if (allowKeyOpen && isPlayerClose && Input.GetKeyDown(openKey)) Open();
    }

    private void FindPlayer()
    {
        if (PlayerStats.instance != null)
        {
            playerTransform = PlayerStats.instance.transform;
            return;
        }

        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) playerTransform = p.transform;
    }

    private void UpdateGlow()
    {
        if (glowRenderer == null) return;

        if (!isPlayerClose)
        {
            if (glowRenderer.gameObject.activeSelf) glowRenderer.gameObject.SetActive(false);
            return;
        }

        if (!glowRenderer.gameObject.activeSelf) glowRenderer.gameObject.SetActive(true);

        // Plynne pulsowanie (sinus zamieniony na zakres 0-1)
        float pulse = (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f;
        Color c = glowRenderer.color;
        c.a = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, pulse);
        glowRenderer.color = c;
    }

    // Klikniecie myszka w skrzynie
    private void OnMouseDown()
    {
        // Nie reagujemy, jesli kursor jest nad UI (ekwipunek, sklep, dialog)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (isOpened) return;

        if (!isPlayerClose)
        {
            Debug.Log("Musisz podejsc blizej do skrzyni!");
            return;
        }

        Open();
    }

    public void Open()
    {
        if (isOpened) return;
        isOpened = true;

        // Gasimy poswiate - skrzynia jest juz "zuzyta"
        if (glowRenderer != null) glowRenderer.gameObject.SetActive(false);

        if (openSound != null)
            AudioSource.PlayClipAtPoint(openSound, transform.position, openVolume);

        // Animacja: Animator ma pierwszenstwo, w przeciwnym razie lecimy klatkami
        if (animator != null && !string.IsNullOrEmpty(openTrigger))
            animator.SetTrigger(openTrigger);
        else if (openFrames != null && openFrames.Length > 0)
            StartCoroutine(PlayOpenFrames());

        StartCoroutine(SpawnLootRoutine());
    }

    private IEnumerator PlayOpenFrames()
    {
        float frameTime = 1f / Mathf.Max(1f, framesPerSecond);

        for (int i = 0; i < openFrames.Length; i++)
        {
            if (openFrames[i] != null) sr.sprite = openFrames[i];
            yield return new WaitForSeconds(frameTime);
        }
        // Ostatnia klatka zostaje na stale = otwarta skrzynia
    }

    private IEnumerator SpawnLootRoutine()
    {
        yield return new WaitForSeconds(lootDelay);

        // Promienie rzadkosci ze srodka skrzyni
        if (rayPrefab != null)
        {
            ChestRayEffect rays = Instantiate(
                rayPrefab,
                transform.position + (Vector3)rayOffset,
                Quaternion.identity);

            rays.Play(rarity);
        }

        if (lootTable == null)
        {
            Debug.LogWarning($"Skrzynia '{name}' nie ma przypisanej Loot Table!");
            yield break;
        }

        List<LootResult> loot = lootTable.Roll();

        if (loot.Count == 0)
        {
            Debug.Log($"Skrzynia '{name}' okazala sie pusta.");
            yield break;
        }

        foreach (LootResult result in loot)
        {
            SpawnItem(result);
            if (delayBetweenItems > 0f) yield return new WaitForSeconds(delayBetweenItems);
        }
    }

    private void SpawnItem(LootResult result)
    {
        if (result.item == null) return;

        GameObject prefab = result.item.itemPrefab != null ? result.item.itemPrefab : genericPickupPrefab;
        if (prefab == null)
        {
            Debug.LogError($"Przedmiot '{result.item.itemName}' nie ma Item Prefab, a skrzynia nie ma Generic Pickup Prefab!");
            return;
        }

        Vector3 start = transform.position + (Vector3)spawnOffset;
        GameObject obj = Instantiate(prefab, start, Quaternion.identity);

        // Wpisujemy dane do paczki lezacej na ziemi
        ItemPickup pickup = obj.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.itemData = result.item;
            pickup.amount = result.amount;
        }

        // Miejsce ladowania: ten sam poziom Y co skrzynia, losowo w lewo lub w prawo
        float direction = Random.value < 0.5f ? -1f : 1f;
        float distance = Random.Range(popDistanceMin, popDistanceMax);

        Vector3 landing = new Vector3(
            transform.position.x + direction * distance,
            transform.position.y + landOffsetY,
            transform.position.z);

        LootArcMotion motion = obj.AddComponent<LootArcMotion>();
        motion.Launch(start, landing, popHeight, popDuration);
    }

    // Zasieg widoczny w edytorze po zaznaczeniu skrzyni
    void OnDrawGizmosSelected()
    {
        Gizmos.color = RarityUtils.GetColor(rarity);
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
