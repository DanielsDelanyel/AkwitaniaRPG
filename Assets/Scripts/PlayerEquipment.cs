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
        // 1. Zapamiêtujemy broñ (Dla PlayerMovement i logów)
        this.currentWeapon = weapon; 
        this.currentAmmo = ammo; 

        // 2. Ustawiamy Wygl¹d
        SetRenderer(weaponRenderer, weapon);
        SetRenderer(helmetRenderer, helmet);
        SetRenderer(chestRenderer, armor);
        SetRenderer(pantsRenderer, legs);
        SetRenderer(bootsRenderer, boots);
        SetRenderer(offhandRenderer, shield);
        SetRenderer(ring1Renderer, ring1);
        SetRenderer(ring2Renderer, ring2);
        SetRenderer(necklaceRenderer, necklace);

        stats.equipDmg = 0;         
        stats.equipMagicDmg = 0;    

        stats.equipSTR = 0;
        stats.equipWIT = 0;
        stats.equipZR = 0;
        stats.equipINT = 0;
        stats.equipCHAR = 0;

        stats.equipDef = 0;
        stats.equipMagicDef = 0;

        // 4. Sumujemy bonusy
        AddBonuses(weapon);
        AddBonuses(helmet);
        AddBonuses(armor);
        AddBonuses(legs);
        AddBonuses(boots);
        AddBonuses(shield);
        AddBonuses(ammo);

        // 5. Przeliczamy
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

        stats.equipSTR += item.strengthBonus;
        stats.equipWIT += item.vitalityBonus;
        stats.equipZR += item.dexterityBonus;
        stats.equipINT += item.intellegenceBonus; 
        stats.equipCHAR += item.charismaBonus;

        stats.equipDmg += item.damageBonus;            
        stats.equipMagicDmg += item.magicDamageBonus;   

        stats.equipDef += item.defenseBonus;
        stats.equipMagicDef += item.magicDefenseBonus;
    }
}