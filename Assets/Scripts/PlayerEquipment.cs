using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [Header("Wizualizacja")]
    public SpriteRenderer weaponRenderer;
    public SpriteRenderer helmetRenderer;
    public SpriteRenderer chestRenderer;
    public SpriteRenderer pantsRenderer;
    public SpriteRenderer bootsRenderer;
    public SpriteRenderer offhandRenderer;
    public SpriteRenderer ring1Renderer;
    public SpriteRenderer ring2Renderer;
    public SpriteRenderer necklaceRenderer;

    public ItemData currentWeapon;
    public ItemData currentAmmo;

    PlayerStats stats;

    void Start()
    {
        stats = GetComponent<PlayerStats>();
    }

    public void UpdateEquipment(ItemData weapon, ItemData helmet, ItemData armor, ItemData legs, ItemData boots, ItemData shield, ItemData ring1, ItemData ring2, ItemData necklace, ItemData ammo)
    {
        if (stats == null) stats = GetComponent<PlayerStats>();

        this.currentWeapon = weapon;
        this.currentAmmo = ammo;

        SetRenderer(weaponRenderer, weapon);
        SetRenderer(helmetRenderer, helmet);
        SetRenderer(chestRenderer, armor);
        SetRenderer(pantsRenderer, legs);
        SetRenderer(bootsRenderer, boots);
        SetRenderer(offhandRenderer, shield);
        SetRenderer(ring1Renderer, ring1);
        SetRenderer(ring2Renderer, ring2);
        SetRenderer(necklaceRenderer, necklace);

        // --- Zerowanie przed przeliczeniem ---
        stats.equipDmg = 0;
        stats.equipMagicDmg = 0;
        stats.equipDmgPercent = 0f;   // NOWE

        stats.equipSTR = 0;
        stats.equipWIT = 0;
        stats.equipZR = 0;
        stats.equipINT = 0;
        stats.equipCHAR = 0;

        stats.equipDef = 0;
        stats.equipMagicDef = 0;

        // NOWE: zerowanie pasywnej regeneracji z ekwipunku - bez tego stary bonus
        // z poprzednio zalozonego przedmiotu nigdy by nie znikal po zdjeciu.
        stats.equipHealthRegenPerSecond = 0f;
        stats.equipManaRegenPerSecond = 0f;

        // --- Sumowanie ---
        AddBonuses(weapon);
        AddBonuses(helmet);
        AddBonuses(armor);
        AddBonuses(legs);
        AddBonuses(boots);
        AddBonuses(shield);
        AddBonuses(ring1);      // pierscienie i naszyjnik tez licza sie do statystyk!
        AddBonuses(ring2);
        AddBonuses(necklace);
        AddBonuses(ammo);

        stats.RecalculateStats();
    }

    void SetRenderer(SpriteRenderer sr, ItemData item)
    {
        if (sr == null) return;

        if (item != null)
        {
            sr.sprite = item.icon;
            sr.enabled = true;
        }
        else
        {
            sr.sprite = null;
            sr.enabled = false;
        }
    }

    void AddBonuses(ItemData item)
    {
        if (item == null) return;

        // UWAGA: uzywamy GETTEROW, nie surowych pol!
        // Dzieki temu wchodza tu tez wartosci wylosowane dla tego egzemplarza.
        stats.equipSTR += item.GetStrengthBonus();
        stats.equipWIT += item.GetVitalityBonus();
        stats.equipZR += item.GetDexterityBonus();
        stats.equipINT += item.GetIntelligenceBonus();
        stats.equipCHAR += item.GetCharismaBonus();

        stats.equipDmg += item.GetDamageBonus();
        stats.equipMagicDmg += item.GetMagicDamageBonus();

        stats.equipDef += item.GetDefenseBonus();
        stats.equipMagicDef += item.GetMagicDefenseBonus();

        // NOWE: procenty z kilku przedmiotow sumuja sie
        // (miecz +30% i pierscien +10% dadza razem +40%)
        stats.equipDmgPercent += item.GetDamagePercent();

        // NOWE: pasywna regeneracja - to bylo dodane w ItemData/PlayerStats,
        // ale nigdy nie sumowane tutaj, wiec zalozony przedmiot nie dawal
        // zadnego realnego efektu mimo poprawnego opisu w tooltipie.
        stats.equipHealthRegenPerSecond += item.healthRegenPerSecond;
        stats.equipManaRegenPerSecond += item.manaRegenPerSecond;
    }
}
