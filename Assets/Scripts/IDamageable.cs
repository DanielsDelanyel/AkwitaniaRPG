using UnityEngine;

// Wspolny kontrakt dla wszystkiego, co moze oberwac obrazenia (gracz, kazde
// stworzenie) - dzieki temu StatusEffectController (tiki zatrucia/podpalenia/
// krwawienia) i systemy trafien (miecz, luk, szarza) obsluguja OBA rodzaje celu
// dokladnie tym samym kodem.
//
// flashColor: jakiego koloru poswiaty (patrz HitFlashController.cs) uzyc przy TYM
// KONKRETNYM trafieniu. Domyslnie Normal (biala/czarna) - dzieki wartosci domyslnej
// wiekszosc miejsc (miecz, luk, szarza) woła TakeDamage() bez zadnych zmian, jak
// dotychczas. Tylko StatusEffectController swiadomie podaje kolorowy tik (Poison/
// Burn/Bleed) przy kazdym tiku DoT-a.
public interface IDamageable
{
    void TakeDamage(int damage, bool isCrit, Vector2 hitDirection, HitFlashColor flashColor = HitFlashColor.Normal);
}
