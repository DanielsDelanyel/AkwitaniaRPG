using UnityEngine;

// EFEKT DODATKOWY PRZY TRAFIENIU - szansa, z jaka ZWYKLY atak (miecz, luk, szarza
// wroga...) dodatkowo nakłada status (zatrucie/podpalenie/krwawienie/spowolnienie/
// ogluszenie), NIEZALEZNIE od zwyklych obrazen z ciosu.
//
// BAZOWO KAZDY MA 0% (pole chancePercent = 0) - patrz ItemData.onHitStatus (bronie
// gracza) i Creature.onHitStatus (kazdy przeciwnik/boss). Chcesz "zatruty sztylet"
// albo "pajaka, ktory czasem ogłusza"? Po prostu podnies chancePercent na TYM
// KONKRETNYM przedmiocie/prefabie w Inspektorze - reszta gry zostaje bez zmian.
//
// Umiejetnosci (Wir Powietrza, Chmura Trucizny, Iskry) NIE korzystaja z tego -
// one juz maja wlasny, w pelni skonfigurowany efekt. To pole jest TYLKO dla
// zwyklych atakow bronia/szarzy, zgodnie z ustaleniami.
[System.Serializable]
public class OnHitStatusData
{
    [Tooltip("Szansa w % na nalozenie efektu przy KAZDYM trafieniu. 0 = nigdy (wartosc bazowa " +
             "dla wszystkich broni/przeciwnikow, chyba ze celowo ja podniesiesz).")]
    [Range(0f, 100f)] public float chancePercent = 0f;

    public StatusEffectType statusType = StatusEffectType.Poison;

    [Header("Tylko Zatrucie / Podpalenie / Krwawienie")]
    public int tickDamage = 2;
    public float tickInterval = 0.5f;

    [Header("Tylko Spowolnienie")]
    [Tooltip("0.5 = -50% predkosci.")]
    [Range(0.05f, 1f)] public float slowMultiplier = 0.5f;

    [Header("Czas trwania")]
    [Tooltip("Dla Spowolnienia/Ogluszenia to caly czas trwania efektu. Dla Zatrucia/Podpalenia/" +
             "Krwawienia to LACZNY czas dzialania (Tick Interval mowi, co ile sekund tyka w tym czasie).")]
    public float duration = 3f;

    private bool Roll()
    {
        return chancePercent > 0f && Random.Range(0f, 100f) < chancePercent;
    }

    private void ApplyTo(StatusEffectController target)
    {
        switch (statusType)
        {
            case StatusEffectType.Poison: target.ApplyPoison(tickDamage, tickInterval, duration); break;
            case StatusEffectType.Burn: target.ApplyBurn(tickDamage, tickInterval, duration); break;
            case StatusEffectType.Bleed: target.ApplyBleed(tickDamage, tickInterval, duration); break;
            case StatusEffectType.Slow: target.ApplySlow(slowMultiplier, duration); break;
            case StatusEffectType.Stun: target.ApplyStun(duration); break;
        }
    }

    // Wygodny skrot - rzut kostka + nalozenie w jednej linii. Kazdy skrypt ataku
    // (PlayerMeleeAttack, Projectile, CreatureAI...) wola to RAZ, zaraz po udanym
    // TakeDamage(), z komponentem StatusEffectController trafionego celu.
    public void TryProc(StatusEffectController target)
    {
        if (target == null) return;
        if (Roll()) ApplyTo(target);
    }
}
