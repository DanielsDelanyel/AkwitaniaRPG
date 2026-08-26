using System.Collections.Generic;
using UnityEngine;

// SPIS WSZYSTKICH PRZEDMIOTOW W GRZE.
//
// Po co: zapis gry przechowuje tylko IDENTYFIKATOR przedmiotu (tekst).
// Przy wczytywaniu trzeba po tym tekscie odnalezc wlasciwy plik .asset -
// i wlasnie do tego sluzy ta baza.
//
// Utworz ja przez Create -> Ekwipunek -> Baza Przedmiotow, a potem kliknij
// prawym na komponencie i wybierz "Znajdz wszystkie przedmioty w projekcie".
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Ekwipunek/Baza Przedmiotow")]
public class ItemDatabase : ScriptableObject
{
    private static ItemDatabase cached;

    // Znajduje baze automatycznie - musi lezec w folderze o nazwie "Resources"
    public static ItemDatabase Instance
    {
        get
        {
            if (cached == null)
            {
                cached = Resources.Load<ItemDatabase>("ItemDatabase");

                if (cached == null)
                    Debug.LogError("Nie znaleziono ItemDatabase! Utworz ja i umiesc " +
                                   "w folderze Assets/Resources pod nazwa 'ItemDatabase'.");
            }
            return cached;
        }
    }

    [Tooltip("Wszystkie przedmioty w grze. Uzyj przycisku w menu kontekstowym, by wypelnic automatycznie.")]
    public ItemData[] allItems;

    private Dictionary<string, ItemData> lookup;

    public ItemData Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        BuildLookup();

        ItemData result;
        if (lookup.TryGetValue(id, out result)) return result;

        Debug.LogWarning($"ItemDatabase: nie znam przedmiotu o ID '{id}'. " +
                         "Czy zostal usuniety albo przemianowany po zapisaniu gry?");
        return null;
    }

    private void BuildLookup()
    {
        if (lookup != null) return;

        lookup = new Dictionary<string, ItemData>();
        if (allItems == null) return;

        foreach (ItemData item in allItems)
        {
            if (item == null) continue;

            string id = item.GetId();
            if (string.IsNullOrEmpty(id)) continue;

            if (lookup.ContainsKey(id))
            {
                Debug.LogError($"ItemDatabase: DWA przedmioty maja to samo ID '{id}'! " +
                               $"({lookup[id].name} oraz {item.name}). Zapis gry bedzie je mylil.");
                continue;
            }

            lookup[id] = item;
        }
    }

    // Czysci pamiec podreczna - przydaje sie po recznej zmianie listy
    public void Refresh()
    {
        lookup = null;
    }

#if UNITY_EDITOR
    [ContextMenu("Znajdz wszystkie przedmioty w projekcie")]
    private void FindAllItems()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
        List<ItemData> found = new List<ItemData>();

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (item == null) continue;

            // Uzupelniamy puste identyfikatory nazwa pliku
            if (string.IsNullOrEmpty(item.itemId))
            {
                item.itemId = item.name;
                UnityEditor.EditorUtility.SetDirty(item);
            }

            found.Add(item);
        }

        allItems = found.ToArray();
        lookup = null;

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();

        Debug.Log($"ItemDatabase: znaleziono {found.Count} przedmiotow.");
    }
#endif
}