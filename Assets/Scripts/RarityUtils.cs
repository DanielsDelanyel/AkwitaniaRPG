using UnityEngine;

// Wspolne kolory i nazwy rzadkosci - uzywane przez skrzynie, promienie i (opcjonalnie) tooltip.
public static class RarityUtils
{
    public static Color GetColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return new Color(0.78f, 0.80f, 0.85f); // szary
            case ItemRarity.Rare: return new Color(0.25f, 0.60f, 1.00f);   // niebieski
            case ItemRarity.Epic: return new Color(0.65f, 0.25f, 1.00f);   // fioletowy
            case ItemRarity.Legendary: return new Color(1.00f, 0.80f, 0.15f); // zloty
            default: return Color.white;
        }
    }

    public static string GetName(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return "Pospolita";
            case ItemRarity.Rare: return "Rzadka";
            case ItemRarity.Epic: return "Epicka";
            case ItemRarity.Legendary: return "Legendarna";
            default: return "";
        }
    }
}
