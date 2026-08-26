using System.Collections.Generic;
using UnityEngine;

// PAMIEC STANU SWIATA.
//
// Zyje w pamieci przez cala rozgrywke i przezywa zmiane lokacji.
// Obiekty same siebie sprawdzaja przy starcie - skrzynia pyta "czy bylem
// juz otwarty?", NPC pyta "jaka mam sympatie?".
//
// SaveManager tylko zrzuca to do pliku i odczytuje z powrotem.
public static class WorldState
{
    // Zdarzenia jednorazowe: otwarte skrzynie, zabrane przedmioty, ukonczone zadania
    private static readonly HashSet<string> flags = new HashSet<string>();

    // Stan NPC: sympatia i pozostaly towar
    private static readonly Dictionary<string, SavedNpc> npcs = new Dictionary<string, SavedNpc>();

    // ===============================================================
    // ZNACZNIKI (tak/nie)
    // ===============================================================
    public static void SetFlag(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        flags.Add(id);
    }

    public static bool HasFlag(string id)
    {
        return !string.IsNullOrEmpty(id) && flags.Contains(id);
    }

    public static void ClearFlag(string id)
    {
        if (!string.IsNullOrEmpty(id)) flags.Remove(id);
    }

    // ===============================================================
    // NPC
    // ===============================================================
    public static SavedNpc GetNpc(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        npcs.TryGetValue(id, out SavedNpc data);
        return data;
    }

    public static SavedNpc GetOrCreateNpc(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (!npcs.TryGetValue(id, out SavedNpc data))
        {
            data = new SavedNpc { npcId = id };
            npcs[id] = data;
        }
        return data;
    }

    public static void StoreNpc(SavedNpc data)
    {
        if (data == null || string.IsNullOrEmpty(data.npcId)) return;
        npcs[data.npcId] = data;
    }

    // ===============================================================
    // ZRZUT DO ZAPISU I ODCZYT
    // ===============================================================
    public static List<string> GetFlagsForSave()
    {
        return new List<string>(flags);
    }

    public static List<SavedNpc> GetNpcsForSave()
    {
        return new List<SavedNpc>(npcs.Values);
    }

    public static void LoadFrom(List<string> savedFlags, List<SavedNpc> savedNpcs)
    {
        Clear();

        if (savedFlags != null)
        {
            foreach (string f in savedFlags)
            {
                if (!string.IsNullOrEmpty(f)) flags.Add(f);
            }
        }

        if (savedNpcs != null)
        {
            foreach (SavedNpc n in savedNpcs)
            {
                if (n != null && !string.IsNullOrEmpty(n.npcId)) npcs[n.npcId] = n;
            }
        }

        Debug.Log($"Stan swiata wczytany: {flags.Count} znacznikow, {npcs.Count} postaci.");
    }

    // Nowa gra zaczyna od czystej karty
    public static void Clear()
    {
        flags.Clear();
        npcs.Clear();
    }
}
