using UnityEngine;

// PUSTY ZNACZNIK: obiekt z tym komponentem to PRZYJAZNE stworzenie gracza
// (np. przyzwany Szkielet), a NIE wrogi mob.
//
// Sprawdzany przez:
//  - PlayerMeleeAttack i Projectile - zeby gracz nie ranil wlasnych sluig
//    swoim mieczem/strzalami,
//  - SummonedCreatureAI - zeby jedno przyzwanie nie atakowalo drugiego.
//
// Nie ma tu zadnej logiki - to czysty "tag" w postaci komponentu,
// bo TagManager w Unity wymagalby recznej konfiguracji nowego Taga
// w kazdym projekcie, do ktorego trafi ten kod.
public class SummonedCreature : MonoBehaviour
{
}
