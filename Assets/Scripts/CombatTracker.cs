using System.Collections.Generic;
using UnityEngine;

// PROSTY, STATYCZNY LICZNIK "CZY GRACZ JEST TERAZ W AKTYWNEJ WALCE".
//
// Kazdy przeciwnik, ktory zaczyna aktywnie gonic/atakowac gracza (np. BanditAI
// wchodzi w State.Chase, ArcherAI w State.Reposition), zglasza sie tutaj przez
// Register(this). Gdy go zgubi (przekroczony deaggroRange -> State.Returning)
// albo zginie (Destroy wywoluje OnDestroy) - wypisuje sie przez Unregister(this).
//
// HashSet sprawia, ze wielokrotny Register tego samego przeciwnika (np. przy
// kazdym powrocie do State.Chase w petli Chase->WindUp->Attack->Retreat->
// Circle->Chase) jest bezpieczny - nie "podbija" licznika i jeden Unregister
// od razu go zdejmuje, bez wzgledu na to, ile razy wczesniej sie rejestrowal.
//
// UZYCIE: wszedzie tam, gdzie trzeba sprawdzic, czy gracz moze teraz np. wejsc
// do budynku/innej lokacji - CombatTracker.IsPlayerInCombat (patrz LocationEntrance).
public static class CombatTracker
{
    private static readonly HashSet<object> activeAggressors = new HashSet<object>();

    public static bool IsPlayerInCombat => activeAggressors.Count > 0;

    // Ile przeciwnikow aktualnie gonii/atakuje gracza - przydatne do debugowania
    // (np. wypisania w UI "Walczysz z 2 przeciwnikami!").
    public static int ActiveAggressorCount => activeAggressors.Count;

    public static void Register(object source)
    {
        if (source == null) return;
        activeAggressors.Add(source);
    }

    public static void Unregister(object source)
    {
        if (source == null) return;
        activeAggressors.Remove(source);
    }

    // Wolane przy powrocie do menu glownego (PauseMenuUI.QuitRoutine) - czysta
    // siatka bezpieczenstwa, gdyby jakis przeciwnik z jakiegos powodu nie zdazyl
    // sam sie wypisac (normalnie robi to jego OnDestroy przy zmianie sceny).
    public static void ClearAll()
    {
        activeAggressors.Clear();
    }
}
