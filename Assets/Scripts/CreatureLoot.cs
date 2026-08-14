using System.Collections.Generic;
using UnityEngine;

// Uniwersalny lup z pokonanego stworzenia.
// Powies to na dziku, owcy albo bossie - dziala tak samo wszedzie.
[RequireComponent(typeof(Creature))]
public class CreatureLoot : MonoBehaviour
{
    public enum LootMode
    {
        DropItems,   // przedmioty wypadaja wprost na ziemie
        SpawnChest,  // w miejscu smierci pojawia sie skrzynia
        Both         // i to, i to
    }

    [Header("Sposob")]
    public LootMode mode = LootMode.DropItems;

    [Header("Losowy Lup")]
    [Tooltip("Tabela lupow - taka sama jak w skrzyniach. Zostaw puste, jesli chcesz tylko gwarantowane przedmioty.")]
    public LootTable lootTable;

    [Header("Gwarantowany Lup")]
    [Tooltip("Te przedmioty wypadaja ZAWSZE, niezaleznie od tabeli. Idealne dla bossa.")]
    public LootEntry[] alwaysDrop;

    [Header("Skrzynia (dla trybu SpawnChest)")]
    public GameObject chestPrefab;
    public Vector2 chestOffset = Vector2.zero;

    [Header("Wyrzucanie na ziemie")]
    [Tooltip("Uzywany, gdy ItemData nie ma wlasnego Item Prefab.")]
    public GameObject genericPickupPrefab;
    public Vector2 spawnOffset = new Vector2(0f, 0.3f);
    public float popHeight = 1.1f;
    public float popDistanceMin = 0.6f;
    public float popDistanceMax = 1.8f;
    public float popDuration = 0.55f;
    [Tooltip("O ile nizej od srodka stworzenia ma wyladowac lup (0 = ten sam poziom Y).")]
    public float landOffsetY = 0f;

    [Header("Zloto")]
    [Tooltip("Dodatkowe zloto ponad moneyReward z Creature. 0 = brak.")]
    public int bonusMoneyMin = 0;
    public int bonusMoneyMax = 0;

    private Creature creature;

    void Awake()
    {
        creature = GetComponent<Creature>();
        creature.onDeath += HandleDeath;
    }

    void OnDestroy()
    {
        if (creature != null) creature.onDeath -= HandleDeath;
    }

    private void HandleDeath(Creature c)
    {
        // Pozycja zapamietana TERAZ - obiekt zaraz moze zniknac
        Vector3 deathPos = transform.position;

        if (bonusMoneyMax > 0 && PlayerStats.instance != null)
        {
            int gold = Random.Range(Mathf.Min(bonusMoneyMin, bonusMoneyMax),
                                    Mathf.Max(bonusMoneyMin, bonusMoneyMax) + 1);
            if (gold > 0)
            {
                PlayerStats.instance.currentMoney += gold;
                if (InventoryUI.instance != null) InventoryUI.instance.UpdatePlayerInfoUI();
            }
        }

        if (mode == LootMode.SpawnChest || mode == LootMode.Both) SpawnChest(deathPos);
        if (mode == LootMode.DropItems || mode == LootMode.Both) DropItems(deathPos);
    }

    private void SpawnChest(Vector3 deathPos)
    {
        if (chestPrefab == null)
        {
            Debug.LogWarning($"{name}: tryb SpawnChest, ale nie przypisano Chest Prefab!");
            return;
        }

        // Skrzynia dostaje wlasna tabele lupow ze swojego prefabu -
        // albo nasza, jesli ja tu ustawiles.
        GameObject chest = Instantiate(chestPrefab, deathPos + (Vector3)chestOffset, Quaternion.identity);

        if (lootTable != null)
        {
            TreasureChest tc = chest.GetComponent<TreasureChest>();
            if (tc != null) tc.lootTable = lootTable;
        }
    }

    private void DropItems(Vector3 deathPos)
    {
        List<LootResult> loot = new List<LootResult>();

        // 1. Gwarantowane
        if (alwaysDrop != null)
        {
            foreach (LootEntry entry in alwaysDrop)
            {
                if (entry == null || entry.item == null) continue;

                int min = Mathf.Max(1, Mathf.Min(entry.minAmount, entry.maxAmount));
                int max = Mathf.Max(1, Mathf.Max(entry.minAmount, entry.maxAmount));
                loot.Add(new LootResult(entry.item, Random.Range(min, max + 1)));
            }
        }

        // 2. Losowe z tabeli
        if (lootTable != null) loot.AddRange(lootTable.Roll());

        if (loot.Count == 0) return;

        foreach (LootResult result in loot) SpawnOne(result, deathPos);
    }

    private void SpawnOne(LootResult result, Vector3 deathPos)
    {
        if (result.item == null) return;

        GameObject prefab = result.item.itemPrefab != null ? result.item.itemPrefab : genericPickupPrefab;
        if (prefab == null)
        {
            Debug.LogError($"Przedmiot '{result.item.itemName}' nie ma Item Prefab, " +
                           $"a {name} nie ma Generic Pickup Prefab!");
            return;
        }

        Vector3 start = deathPos + (Vector3)spawnOffset;
        GameObject obj = Instantiate(prefab, start, Quaternion.identity);

        ItemPickup pickup = obj.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.itemData = result.item;
            pickup.amount = result.amount;
        }

        // Rozrzut na boki, ladowanie na poziomie Y stworzenia
        float direction = Random.value < 0.5f ? -1f : 1f;
        float distance = Random.Range(popDistanceMin, popDistanceMax);

        Vector3 landing = new Vector3(
            deathPos.x + direction * distance,
            deathPos.y + landOffsetY,
            deathPos.z);

        LootArcMotion motion = obj.AddComponent<LootArcMotion>();
        motion.Launch(start, landing, popHeight, popDuration);
    }
}
