using System.Collections.Generic;
using UnityEngine;

// SPIS WSZYSTKICH ZADAN W GRZE.
// Dziala tak samo jak ItemDatabase: zapis trzyma tylko identyfikator zadania,
// a ta baza pozwala odnalezc po nim wlasciwy plik .asset.
//
// Utworz przez Create -> Zadania -> Baza Zadan i umiesc w Assets/Resources
// pod nazwa "QuestDatabase".
[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Zadania/Baza Zadan")]
public class QuestDatabase : ScriptableObject
{
    private static QuestDatabase cached;

    public static QuestDatabase Instance
    {
        get
        {
            if (cached == null)
            {
                cached = Resources.Load<QuestDatabase>("QuestDatabase");

                if (cached == null)
                    Debug.LogError("Nie znaleziono QuestDatabase! Utworz ja i umiesc " +
                                   "w folderze Assets/Resources pod nazwa 'QuestDatabase'.");
            }
            return cached;
        }
    }

    public Quest[] allQuests;

    private Dictionary<string, Quest> lookup;

    public Quest Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        BuildLookup();

        if (lookup.TryGetValue(id, out Quest result)) return result;

        Debug.LogWarning($"QuestDatabase: nie znam zadania o ID '{id}'.");
        return null;
    }

    private void BuildLookup()
    {
        if (lookup != null) return;

        lookup = new Dictionary<string, Quest>();
        if (allQuests == null) return;

        foreach (Quest q in allQuests)
        {
            if (q == null) continue;

            string id = q.GetId();
            if (string.IsNullOrEmpty(id)) continue;

            if (lookup.ContainsKey(id))
            {
                Debug.LogError($"QuestDatabase: DWA zadania maja ID '{id}'! " +
                               $"({lookup[id].name} oraz {q.name}). Zapis bedzie je mylil.");
                continue;
            }

            lookup[id] = q;
        }
    }

    public void Refresh()
    {
        lookup = null;
    }

#if UNITY_EDITOR
    [ContextMenu("Znajdz wszystkie zadania w projekcie")]
    private void FindAllQuests()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Quest");
        List<Quest> found = new List<Quest>();

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            Quest q = UnityEditor.AssetDatabase.LoadAssetAtPath<Quest>(path);
            if (q == null) continue;

            if (string.IsNullOrEmpty(q.questId))
            {
                q.questId = q.name;
                UnityEditor.EditorUtility.SetDirty(q);
            }

            found.Add(q);
        }

        allQuests = found.ToArray();
        lookup = null;

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();

        Debug.Log($"QuestDatabase: znaleziono {found.Count} zadan.");
    }
#endif
}
