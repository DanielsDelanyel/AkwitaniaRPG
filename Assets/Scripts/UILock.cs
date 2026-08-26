using System.Collections.Generic;
using UnityEngine;

// JEDEN wlasciciel blokady ruchu gracza.
//
// Problem, ktory to rozwiazuje:
// Kazde okno UI samo ustawialo playerMovement.enabled. Gdy dialog zablokowal
// ruch, a gracz otworzyl i zamknal ekwipunek, ekwipunek przy zamykaniu
// odblokowywal ruch - mimo ze rozmowa wciaz trwala.
//
// Teraz kazde okno zglasza swoj POWOD blokady. Ruch wraca dopiero wtedy,
// gdy zniknie OSTATNI powod.
public static class UILock
{
    private static readonly HashSet<string> reasons = new HashSet<string>();

    public static bool IsLocked { get { return reasons.Count > 0; } }

    // reason: dowolna nazwa, np. "Dialogue", "Inventory", "Stats", "Transition"
    public static void Set(string reason, bool locked)
    {
        if (string.IsNullOrEmpty(reason)) return;

        if (locked) reasons.Add(reason);
        else reasons.Remove(reason);

        Apply();
    }

    public static bool Has(string reason)
    {
        return reasons.Contains(reason);
    }

    // Awaryjne zdjecie wszystkich blokad - np. przy zmianie lokacji
    public static void ClearAll()
    {
        reasons.Clear();
        Apply();
    }

    private static void Apply()
    {
        if (PlayerStats.instance == null) return;

        TopDownMovement movement = PlayerStats.instance.GetComponent<TopDownMovement>();
        if (movement == null) return;

        movement.enabled = !IsLocked;

        if (IsLocked)
        {
            Rigidbody2D rb = movement.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }
}