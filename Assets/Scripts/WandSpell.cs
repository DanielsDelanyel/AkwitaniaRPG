using UnityEngine;

// WSPOLNY KONTRAKT dla wszystkich efektow rzucanych rozdzka (iskry ognia, chmura
// trucizny, i cokolwiek dojdzie pozniej). PlayerCombat.PerformWandAttack() zna TYLKO
// te jedna metode - nie musi wiedziec, czy to lecacy pocisk, czy stacjonarna chmura.
//
// Zeby dodac nowy rodzaj zaklecia: stworz skrypt : WandSpell, zaimplementuj Setup(),
// przypisz go do prefabu i wskaz ten prefab w polu ItemData.spellPrefab danej rozdzki.
public abstract class WandSpell : MonoBehaviour
{
    // Wywolywane RAZ, zaraz po Instantiate.
    // damage        - juz policzone (Inteligencja gracza + bonus rozdzki)
    // maxRange      - ItemData.spellRange
    // aimAngleDegrees - SUROWY kat w strone kursora (bez zadnych korekt graficznych) -
    //                   uzyj go do policzenia kierunku lotu/pozycji. Wlasny obrot
    //                   SPRITE'A dobierz osobno, przez wlasne pole w swoim skrypcie
    //                   (patrz Sprite Angle Offset w FireSparkProjectile) - dzieki temu
    //                   rotacja obiektu nigdy nie miesza sie z jego gameplayowym kierunkiem.
    public abstract void Setup(int damage, float maxRange, float aimAngleDegrees);

    // NOWE: kto rzucil to zaklecie - PlayerCombat.PerformWandAttack() wywoluje to
    // zaraz po Setup(). Zaklecia, ktore "lapia" cele bardzo szeroko (np. cala
    // rosnaca chmura, a nie tylko pierwszy trafiony wrog - patrz PoisonCloudSpell)
    // powinny to wykorzystac, zeby NIE trafiac wlasnego rzucajacego. Domyslna,
    // pusta implementacja - zaklecia, ktore i tak nigdy nie mogą trafic wlasnego
    // rzucajacego (np. FireSparkProjectile dziala tylko na Creature, a gracz go
    // nie ma), moga to po prostu zignorowac.
    public virtual void SetCaster(Transform casterTransform) { }
}
