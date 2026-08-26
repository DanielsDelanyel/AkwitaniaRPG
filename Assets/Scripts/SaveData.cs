using System;
using System.Collections.Generic;
using UnityEngine;

// Struktury zapisu. Wszystko musi byc [Serializable] i skladac sie z pol
// publicznych - JsonUtility Unity nie radzi sobie z wlasciwosciami ani slownikami.

[Serializable]
public class SavedItem
{
    public int slotIndex;        // ktore okienko w plecaku
    public string itemId;        // identyfikator SZABLONU przedmiotu
    public int amount = 1;

    // --- Wyniki losowania tego konkretnego egzemplarza ---
    public bool hasRolledPercent;
    public float rolledPercent;

    [Tooltip("Wartosci w tej samej kolejnosci co tablica randomBonuses w przedmiocie.")]
    public List<int> rolledBonuses = new List<int>();

    public bool HasAnyRoll
    {
        get { return hasRolledPercent || (rolledBonuses != null && rolledBonuses.Count > 0); }
    }
}

[Serializable]
public class SavedPlayer
{
    public string playerName;

    // Gdzie stoi
    public string locationScene;
    public float posX;
    public float posY;

    // Rozwoj
    public int level;
    public int currentExp;
    public int expToNextLevel;
    public int attributePoints;
    public int currentMoney;

    // Statystyki bazowe (te przydzielane punktami)
    public int baseSTR;
    public int baseWIT;
    public int baseINT;
    public int baseZR;
    public int baseCHAR;

    public int baseDmg;
    public int baseMagicDmg;
    public int baseDef;
    public int baseMagicDef;

    // Ostatni aktywowany punkt odrodzenia
    public string respawnScene;
    public string respawnSpawnId;
    public string respawnName;

    // Stan zasobow
    public int currentHealth;
    public float currentMana;
    public float currentStamina;
}

// Stan pojedynczej postaci niezaleznej
[Serializable]
public class SavedNpc
{
    public string npcId;

    [Tooltip("Sympatia do gracza w chwili zapisu.")]
    public int affinity;

    [Tooltip("Czy kupiec ma juz wylosowany towar? Bez tego nie odroznimy " +
             "pustej polki od sklepu, ktorego gracz jeszcze nie odwiedzil.")]
    public bool hasStock;

    [Tooltip("Towar, ktory ZOSTAL na polce - razem z wylosowanymi statystykami.")]
    public List<SavedItem> shopStock = new List<SavedItem>();
}

[Serializable]
public class SaveData
{
    public int saveVersion = 1;
    public string savedAt;
    public float playTimeSeconds;

    public SavedPlayer player = new SavedPlayer();

    public List<SavedItem> backpack = new List<SavedItem>();

    // Wyposazenie w STALEJ kolejnosci - patrz SaveManager.EquipmentOrder
    public List<SavedItem> equipment = new List<SavedItem>();

    // ===============================================================
    // STAN SWIATA
    // ===============================================================
    [Tooltip("Otwarte skrzynie, zabrane przedmioty - identyfikatory z UniqueId.")]
    public List<string> worldFlags = new List<string>();

    [Tooltip("Sympatia i zapas towaru u postaci niezaleznych.")]
    public List<SavedNpc> npcs = new List<SavedNpc>();
}
