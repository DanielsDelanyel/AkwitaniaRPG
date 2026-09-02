using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ZAPIS I WCZYTYWANIE GRY.
//
// Zapisuje: pozycje i lokacje gracza, jego statystyki, zloto, doswiadczenie,
// caly plecak i wyposazenie - razem z wylosowanymi statystykami przedmiotow,
// oraz punkty i odblokowane umiejetnosci.
public static class SaveManager
{
    public const int CURRENT_VERSION = 1;

    // Dane wczytane z pliku, czekajace na zastosowanie po zaladowaniu lokacji
    public static SaveData PendingLoad { get; private set; }

    private static float sessionStartTime;
    private static float carriedPlayTime;

    public static string GetPath(int slot = 1)
    {
        return Path.Combine(Application.persistentDataPath, $"save{slot:00}.json");
    }

    public static bool HasSave(int slot = 1)
    {
        return File.Exists(GetPath(slot));
    }

    public static void DeleteSave(int slot = 1)
    {
        string path = GetPath(slot);
        if (File.Exists(path)) File.Delete(path);
    }

    // ===============================================================
    // ZAPIS
    // ===============================================================
    public static bool SaveGame(int slot = 1)
    {
        if (PlayerStats.instance == null)
        {
            Debug.LogError("Zapis nieudany: brak gracza w scenie.");
            return false;
        }

        try
        {
            SaveData data = CaptureCurrentState();

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetPath(slot), json);

            Debug.Log($"Gra zapisana: {GetPath(slot)}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Zapis nieudany: {e.Message}");
            return false;
        }
    }

    private static SaveData CaptureCurrentState()
    {
        SaveData data = new SaveData();
        data.saveVersion = CURRENT_VERSION;
        data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        data.playTimeSeconds = GetPlayTime();

        PlayerStats ps = PlayerStats.instance;

        // --- GRACZ ---
        SavedPlayer p = data.player;
        p.playerName = ps.playerName;

        Vector3 pos = ps.transform.position;
        p.posX = pos.x;
        p.posY = pos.y;
        p.locationScene = LocationManager.instance != null
            ? LocationManager.instance.CurrentLocation
            : "";

        p.level = ps.level;
        p.currentExp = ps.currentExp;
        p.expToNextLevel = ps.expToNextLevel;
        p.attributePoints = ps.attributePoints;
        p.currentMoney = ps.currentMoney;

        p.baseSTR = ps.baseSTR;
        p.baseWIT = ps.baseWIT;
        p.baseINT = ps.baseINT;
        p.baseZR = ps.baseZR;
        p.baseCHAR = ps.baseCHAR;

        p.baseDmg = ps.baseDmg;
        p.baseMagicDmg = ps.baseMagicDmg;
        p.baseDef = ps.baseDef;
        p.baseMagicDef = ps.baseMagicDef;

        p.respawnScene = RespawnPoint.CurrentScene;
        p.respawnSpawnId = RespawnPoint.CurrentSpawnId;
        p.respawnName = RespawnPoint.CurrentName;

        p.currentHealth = ps.currentHealth;
        p.currentMana = ps.currentMana;
        p.currentStamina = ps.currentStamina;

        // --- PLECAK I WYPOSAZENIE ---
        if (InventoryUI.instance != null)
        {
            InventorySlot[] backpack = InventoryUI.instance.GetBackpackSlots();
            if (backpack != null)
            {
                for (int i = 0; i < backpack.Length; i++)
                {
                    SavedItem entry = CaptureSlot(backpack[i], i);
                    if (entry != null) data.backpack.Add(entry);
                }
            }

            InventorySlot[] equipment = InventoryUI.instance.GetEquipmentSlots();
            for (int i = 0; i < equipment.Length; i++)
            {
                SavedItem entry = CaptureSlot(equipment[i], i);
                if (entry != null) data.equipment.Add(entry);
            }
        }

        // --- STAN SWIATA ---
        // Zbieramy najpierw zywe postacie z aktualnej lokacji, bo ich sympatia
        // mogla sie zmienic od ostatniego wejscia do WorldState.
        CollectActiveNpcs();

        data.worldFlags = WorldState.GetFlagsForSave();
        data.npcs = WorldState.GetNpcsForSave();
        data.quests = QuestManager.GetAllForSave();

        // --- UMIEJETNOSCI ---
        if (PlayerSkills.instance != null)
        {
            data.skills.skillPoints = PlayerSkills.instance.skillPoints;
            data.skills.unlockedSkillIds = PlayerSkills.instance.GetUnlockedForSave();
        }

        return data;
    }

    // Kazdy NPC obecny w scenie zapisuje swoj biezacy stan do WorldState
    private static void CollectActiveNpcs()
    {
        // Pelna nazwa UnityEngine.Object jest tu KONIECZNA: plik ma "using System;",
        // wiec samo "Object" bylo niejednoznaczne z System.Object.
        NPCStats[] npcsInScene = UnityEngine.Object.FindObjectsByType<NPCStats>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (NPCStats npc in npcsInScene)
        {
            if (npc != null) npc.StoreStateToWorld();
        }
    }

    // Zamienia dowolny przedmiot na wpis w zapisie - razem z jego rzutami.
    // Uzywane przez ekwipunek gracza ORAZ przez zapas towaru u kupca.
    public static SavedItem CaptureItem(ItemData item, int amount, int index = 0)
    {
        if (item == null) return null;

        SavedItem entry = new SavedItem();
        entry.slotIndex = index;
        entry.itemId = item.GetTemplateId();
        entry.amount = Mathf.Max(1, amount);

        if (item.damageBonusPercent != null && item.damageBonusPercent.HasRolled)
        {
            entry.hasRolledPercent = true;
            entry.rolledPercent = item.damageBonusPercent.RolledValue;
        }

        if (item.randomBonuses != null)
        {
            foreach (RandomizableBonus b in item.randomBonuses)
            {
                entry.rolledBonuses.Add(b != null ? b.Value : 0);
            }
        }

        return entry;
    }

    // Zamienia zawartosc jednego okienka na wpis w zapisie
    private static SavedItem CaptureSlot(InventorySlot slot, int index)
    {
        if (slot == null || slot.item == null) return null;

        return CaptureItem(slot.item, slot.amount, index);
    }

    // ===============================================================
    // WCZYTYWANIE
    // ===============================================================

    // Krok 1: odczyt pliku. Wolane z menu, ZANIM powstanie scena gry.
    public static bool LoadFromDisk(int slot = 1)
    {
        if (!HasSave(slot))
        {
            Debug.LogWarning("Brak pliku zapisu.");
            return false;
        }

        try
        {
            string json = File.ReadAllText(GetPath(slot));
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (data == null)
            {
                Debug.LogError("Plik zapisu jest uszkodzony.");
                return false;
            }

            if (data.saveVersion > CURRENT_VERSION)
            {
                Debug.LogWarning($"Zapis pochodzi z nowszej wersji gry (v{data.saveVersion}). " +
                                 "Moze wczytac sie niepoprawnie.");
            }

            PendingLoad = data;
            carriedPlayTime = data.playTimeSeconds;

            // WAZNE: stan swiata nakladamy JUZ TERAZ, zanim powstanie lokacja.
            // Skrzynie i NPC sprawdzaja WorldState w swoim Start(), wiec dane
            // musza tam byc, zanim obiekty sie obudza.
            WorldState.LoadFrom(data.worldFlags, data.npcs);
            QuestManager.LoadFrom(data.quests);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Nie udalo sie odczytac zapisu: {e.Message}");
            return false;
        }
    }

    // Krok 2: nakladanie danych. Wolane przez LocationManager PO zaladowaniu lokacji.
    public static void ApplyPendingLoad()
    {
        if (PendingLoad == null) return;

        SaveData data = PendingLoad;
        PendingLoad = null;   // zuzywamy dane tylko raz

        ApplyPlayer(data.player);
        ApplyInventory(data);

        // Umiejetnosci nakladamy tu, obok reszty postepu gracza - nie zaleza
        // od kolejnosci ladowania lokacji tak jak WorldState/QuestManager.
        if (PlayerSkills.instance != null)
        {
            PlayerSkills.instance.LoadFrom(data.skills.skillPoints, data.skills.unlockedSkillIds);
        }

        // Statystyki koncowe przeliczamy DOPIERO po zalozeniu ekwipunku,
        // inaczej maksymalne zycie nie uwzglednialoby bonusow z przedmiotow.
        if (InventoryUI.instance != null) InventoryUI.instance.OnEquipmentChanged();

        // Zycie i mane przywracamy na koncu - RecalculateStats mogl je przyciac
        if (PlayerStats.instance != null)
        {
            PlayerStats ps = PlayerStats.instance;
            ps.currentHealth = Mathf.Clamp(data.player.currentHealth, 1, ps.GetMaxHealth());
            ps.currentMana = Mathf.Clamp(data.player.currentMana, 0f, ps.GetMaxMana());
            ps.currentStamina = Mathf.Clamp(data.player.currentStamina, 0f, ps.GetMaxStamina());
        }

        if (InventoryUI.instance != null) InventoryUI.instance.UpdatePlayerInfoUI();

        // Cele typu "miej X w plecaku" sprawdzamy PO odtworzeniu ekwipunku
        QuestManager.RefreshInventoryObjectives();

        Debug.Log($"Wczytano zapis z {data.savedAt}.");
    }

    private static void ApplyPlayer(SavedPlayer p)
    {
        PlayerStats ps = PlayerStats.instance;
        if (ps == null || p == null) return;

        ps.playerName = p.playerName;

        ps.level = p.level;
        ps.currentExp = p.currentExp;
        ps.expToNextLevel = Mathf.Max(1, p.expToNextLevel);
        ps.attributePoints = p.attributePoints;
        ps.currentMoney = p.currentMoney;

        ps.baseSTR = p.baseSTR;
        ps.baseWIT = p.baseWIT;
        ps.baseINT = p.baseINT;
        ps.baseZR = p.baseZR;
        ps.baseCHAR = p.baseCHAR;

        ps.baseDmg = p.baseDmg;
        ps.baseMagicDmg = p.baseMagicDmg;
        ps.baseDef = p.baseDef;
        ps.baseMagicDef = p.baseMagicDef;

        // Punkt odrodzenia z zapisu
        if (!string.IsNullOrEmpty(p.respawnScene))
            RespawnPoint.SetCurrent(p.respawnScene, p.respawnSpawnId, p.respawnName);

        // Pozycja: przez Rigidbody, zeby fizyka nie cofnela gracza
        Vector3 target = new Vector3(p.posX, p.posY, ps.transform.position.z);

        Rigidbody2D rb = ps.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = target;
            rb.linearVelocity = Vector2.zero;
        }
        ps.transform.position = target;

        ps.RecalculateStats();
    }

    private static void ApplyInventory(SaveData data)
    {
        if (InventoryUI.instance == null) return;

        ItemDatabase db = ItemDatabase.Instance;
        if (db == null) return;

        InventorySlot[] backpack = InventoryUI.instance.GetBackpackSlots();
        InventorySlot[] equipment = InventoryUI.instance.GetEquipmentSlots();

        // Czyscimy wszystko przed wlozeniem zapisanej zawartosci
        if (backpack != null) foreach (var s in backpack) { if (s != null) s.ClearSlot(); }
        foreach (var s in equipment) { if (s != null) s.ClearSlot(); }

        RestoreInto(backpack, data.backpack, db);
        RestoreInto(equipment, data.equipment, db);
    }

    private static void RestoreInto(InventorySlot[] slots, List<SavedItem> entries, ItemDatabase db)
    {
        if (slots == null || entries == null) return;

        foreach (SavedItem entry in entries)
        {
            if (entry.slotIndex < 0 || entry.slotIndex >= slots.Length) continue;
            if (slots[entry.slotIndex] == null) continue;

            ItemData item = RestoreItem(entry, db);
            if (item == null) continue;

            slots[entry.slotIndex].AddItem(item, entry.amount);
        }
    }

    // Odtwarza KONKRETNY egzemplarz przedmiotu razem z jego rzutami.
    // Publiczne, bo korzysta z tego takze NPCStats przy odtwarzaniu towaru.
    public static ItemData RestoreItem(SavedItem entry)
    {
        return RestoreItem(entry, ItemDatabase.Instance);
    }

    public static ItemData RestoreItem(SavedItem entry, ItemDatabase db)
    {
        ItemData template = db.Find(entry.itemId);
        if (template == null) return null;

        // Przedmiot bez losowych statystyk - oddajemy wspoldzielony szablon
        if (!entry.HasAnyRoll) return template;

        ItemData copy = UnityEngine.Object.Instantiate(template);
        copy.name = template.name;
        copy.isRuntimeInstance = true;
        copy.sourceTemplate = template;

        copy.ClearAllRolls();

        if (entry.hasRolledPercent && copy.damageBonusPercent != null)
            copy.damageBonusPercent.ForceRoll(entry.rolledPercent);

        if (copy.randomBonuses != null && entry.rolledBonuses != null)
        {
            int count = Mathf.Min(copy.randomBonuses.Length, entry.rolledBonuses.Count);
            for (int i = 0; i < count; i++)
            {
                if (copy.randomBonuses[i] != null)
                    copy.randomBonuses[i].ForceRoll(entry.rolledBonuses[i]);
            }
        }

        return copy;
    }

    // ===============================================================
    // CZAS GRY
    // ===============================================================
    public static void StartSession()
    {
        sessionStartTime = Time.realtimeSinceStartup;
    }

    public static float GetPlayTime()
    {
        return carriedPlayTime + (Time.realtimeSinceStartup - sessionStartTime);
    }

    public static void ResetSession()
    {
        carriedPlayTime = 0f;
        sessionStartTime = Time.realtimeSinceStartup;
        PendingLoad = null;

        // Nowa gra zaczyna od pelnych skrzyn, NPC bez wspomnien, pustego dziennika
        // i bez odblokowanych umiejetnosci.
        WorldState.Clear();
        RespawnPoint.Clear();
        QuestManager.Clear();
        if (PlayerSkills.instance != null) PlayerSkills.instance.ClearAll();
    }

    // Krotki opis zapisu - do pokazania na przycisku "Wczytaj gre"
    public static string GetSaveSummary(int slot = 1)
    {
        if (!HasSave(slot)) return "Brak zapisu";

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(GetPath(slot)));
            if (data == null || data.player == null) return "Uszkodzony zapis";

            int minutes = Mathf.FloorToInt(data.playTimeSeconds / 60f);
            return $"{data.player.playerName} - poziom {data.player.level}\n{data.savedAt} ({minutes} min)";
        }
        catch
        {
            return "Uszkodzony zapis";
        }
    }
}
