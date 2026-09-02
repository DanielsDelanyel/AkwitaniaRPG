using System.Collections.Generic;
using UnityEngine;

// SPIS WSZYSTKICH UMIEJETNOSCI W GRZE.
// Dziala dokladnie tak samo jak QuestDatabase/ItemDatabase: zapis trzyma tylko
// identyfikator umiejetnosci, a ta baza pozwala odnalezc po nim wlasciwy .asset.
//
// Utworz przez Create -> Umiejetnosci -> Baza Umiejetnosci i umiesc w Assets/Resources
// pod nazwa "SkillDatabase".
[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Umiejetnosci/Baza Umiejetnosci")]
public class SkillDatabase : ScriptableObject
{
    private static SkillDatabase cached;

    public static SkillDatabase Instance
    {
        get
        {
            if (cached == null)
            {
                cached = Resources.Load<SkillDatabase>("SkillDatabase");

                if (cached == null)
                    Debug.LogError("Nie znaleziono SkillDatabase! Utworz ja i umiesc " +
                                   "w folderze Assets/Resources pod nazwa 'SkillDatabase'.");
            }
            return cached;
        }
    }

    public SkillData[] allSkills;

    private Dictionary<string, SkillData> lookup;

    public SkillData Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        BuildLookup();

        if (lookup.TryGetValue(id, out SkillData result)) return result;

        Debug.LogWarning($"SkillDatabase: nie znam umiejetnosci o ID '{id}'.");
        return null;
    }

    // Wszystkie wezly nalezace do jednej galezi (profesji) - do wyswietlenia
    // w jednym ramieniu panelu Umiejetnosci.
    public List<SkillData> GetForProfession(CharacterClass profession)
    {
        List<SkillData> result = new List<SkillData>();
        if (allSkills == null) return result;

        foreach (SkillData skill in allSkills)
        {
            if (skill != null && skill.profession == profession) result.Add(skill);
        }
        return result;
    }

    private void BuildLookup()
    {
        if (lookup != null) return;

        lookup = new Dictionary<string, SkillData>();
        if (allSkills == null) return;

        foreach (SkillData s in allSkills)
        {
            if (s == null) continue;

            string id = s.GetId();
            if (string.IsNullOrEmpty(id)) continue;

            if (lookup.ContainsKey(id))
            {
                Debug.LogError($"SkillDatabase: DWIE umiejetnosci maja ID '{id}'! " +
                               $"({lookup[id].name} oraz {s.name}). Zapis bedzie je mylil.");
                continue;
            }

            lookup[id] = s;
        }
    }

    public void Refresh()
    {
        lookup = null;
    }

#if UNITY_EDITOR
    [ContextMenu("Znajdz wszystkie umiejetnosci w projekcie")]
    private void FindAllSkills()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SkillData");
        List<SkillData> found = new List<SkillData>();

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            SkillData s = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (s == null) continue;

            if (string.IsNullOrEmpty(s.skillId))
            {
                s.skillId = s.name;
                UnityEditor.EditorUtility.SetDirty(s);
            }

            found.Add(s);
        }

        allSkills = found.ToArray();
        lookup = null;

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();

        Debug.Log($"SkillDatabase: znaleziono {found.Count} umiejetnosci.");
    }
#endif
}
