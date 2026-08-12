using System.Collections.Generic;
using UnityEngine;

// Pojedyncza pozycja w tabeli lupow
[System.Serializable]
public class LootEntry
{
    public ItemData item;

    [Header("Ilosc")]
    public int minAmount = 1;
    public int maxAmount = 1;

    [Header("Szansa")]
    [Tooltip("Waga w losowaniu. Im wieksza liczba, tym czesciej wypada. 100 wypada 10x czesciej niz 10.")]
    [Min(0f)] public float weight = 10f;

    [Tooltip("Gwarantowany lup - omija losowanie i wypada zawsze (z podana ponizej szansa).")]
    public bool guaranteed = false;

    [Tooltip("Dotyczy tylko gwarantowanych. 1 = zawsze, 0.5 = w polowie przypadkow.")]
    [Range(0f, 1f)] public float guaranteedChance = 1f;
}

// Wynik pojedynczego losowania
public struct LootResult
{
    public ItemData item;
    public int amount;

    public LootResult(ItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}

[CreateAssetMenu(fileName = "Nowa Tabela Lupow", menuName = "Ekwipunek/Tabela Lupow")]
public class LootTable : ScriptableObject
{
    [Header("Ile razy losujemy")]
    public int minRolls = 1;
    public int maxRolls = 3;

    [Header("Zasady")]
    [Tooltip("Czy ten sam przedmiot moze wypasc w kilku losowaniach?")]
    public bool allowDuplicates = true;

    [Tooltip("Laczy powtorzone przedmioty w jeden stos (np. 3x strzala zamiast trzech paczek).")]
    public bool mergeStacks = true;

    [Tooltip("Waga 'pustego losu'. Ustaw > 0, jesli czasem ma nie wypasc nic.")]
    [Min(0f)] public float emptyWeight = 0f;

    [Header("Zawartosc")]
    public LootEntry[] entries;

    public List<LootResult> Roll()
    {
        List<LootResult> results = new List<LootResult>();
        if (entries == null || entries.Length == 0) return results;

        // 1. Lupy gwarantowane (omijaja losowanie wag)
        List<LootEntry> randomPool = new List<LootEntry>();
        foreach (LootEntry entry in entries)
        {
            if (entry == null || entry.item == null) continue;

            if (entry.guaranteed)
            {
                if (Random.value <= entry.guaranteedChance)
                    results.Add(new LootResult(entry.item, RollAmount(entry)));
            }
            else
            {
                randomPool.Add(entry);
            }
        }

        // 2. Losowania wazone
        int rolls = Random.Range(Mathf.Min(minRolls, maxRolls), Mathf.Max(minRolls, maxRolls) + 1);

        for (int i = 0; i < rolls; i++)
        {
            if (randomPool.Count == 0) break;

            LootEntry picked = PickWeighted(randomPool);
            if (picked == null) continue; // trafil sie "pusty los"

            results.Add(new LootResult(picked.item, RollAmount(picked)));

            if (!allowDuplicates) randomPool.Remove(picked);
        }

        if (mergeStacks) results = MergeResults(results);
        return results;
    }

    private int RollAmount(LootEntry entry)
    {
        int min = Mathf.Max(1, Mathf.Min(entry.minAmount, entry.maxAmount));
        int max = Mathf.Max(1, Mathf.Max(entry.minAmount, entry.maxAmount));
        return Random.Range(min, max + 1);
    }

    private LootEntry PickWeighted(List<LootEntry> pool)
    {
        float total = emptyWeight;
        foreach (LootEntry e in pool) total += Mathf.Max(0f, e.weight);
        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);

        foreach (LootEntry e in pool)
        {
            roll -= Mathf.Max(0f, e.weight);
            if (roll <= 0f) return e;
        }

        return null; // zostala tylko waga "nic nie wypadlo"
    }

    // Skleja powtorki tego samego przedmiotu, jesli da sie go stackowac
    private List<LootResult> MergeResults(List<LootResult> input)
    {
        List<LootResult> output = new List<LootResult>();

        foreach (LootResult r in input)
        {
            if (r.item == null) continue;

            if (r.item.isStackable)
            {
                bool merged = false;
                for (int i = 0; i < output.Count; i++)
                {
                    if (output[i].item == r.item)
                    {
                        output[i] = new LootResult(r.item, output[i].amount + r.amount);
                        merged = true;
                        break;
                    }
                }
                if (!merged) output.Add(r);
            }
            else
            {
                // Przedmioty nie do stackowania: kazda sztuka to osobna paczka na ziemi
                for (int i = 0; i < r.amount; i++) output.Add(new LootResult(r.item, 1));
            }
        }

        return output;
    }
}
