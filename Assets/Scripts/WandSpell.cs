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
}
