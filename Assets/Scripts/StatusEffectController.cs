using System.Collections.Generic;
using UnityEngine;

// Typy statusow w grze. Trzy pierwsze to "kolory" tego samego mechanizmu (obrazenia
// co interval, przez okreslony czas) - rozne tylko z nazwy/ikonki w UI. Slow i Stun
// dzialaja inaczej (nie zadaja obrazen, tylko ograniczaja gracza/stworzenie).
public enum StatusEffectType
{
    Poison,
    Burn,
    Bleed,
    Slow,
    Stun
}

// KONTROLER STATUSOW (buffow/debuffow). Powies OBOK kazdego IDamageable, ktory ma
// miec mozliwosc zostania zatrutym/podpalonym/pokrwawionym/spowolnionym/ogluszonym:
//   - gracz: obok PlayerStats, na tym samym obiekcie co Rigidbody2D/Collider2D gracza.
//   - kazdy przeciwnik/boss: obok Creature, na tym samym obiekcie co jego Collider2D.
//
// DZIALA W OBIE STRONY: skoro zarowno PlayerStats jak i Creature implementuja
// IDamageable, DOKLADNIE TEN SAM skrypt obsluguje zatrucie gracza przez wroga
// i zatrucie wroga przez gracza - bez zadnego "if (to gracz) ... else ...".
//
// PROSTA ZASADA NAKLADANIA: TEN SAM typ statusu nie stackuje sie - ponowne
// nalozenie odswieza/wzmacnia juz istniejacy (patrz komentarze przy Apply...).
// To swiadomy wybor prostoty, nie ograniczenie techniczne.
public class StatusEffectController : MonoBehaviour
{
    // Jeden tik DoT-a (zatrucie/podpalenie/krwawienie) - identyczna mechanika,
    // rozna tylko etykieta (StatusEffectType) do UI.
    private class DotEffect
    {
        public int tickDamage;
        public float tickInterval;
        public float tickTimer;
        public float remaining;
    }

    private IDamageable target;

    // NOWE: poswiata przy trafieniu - kolorowa dla tiku statusu (patrz TickDots)
    // i dla samego nalozenia Spowolnienia (patrz ApplySlow). Opcjonalny - jesli
    // ktos nie doda tego komponentu, po prostu nie bedzie migania, bez bledow.
    private HitFlashController hitFlash;

    // Najwyzej JEDNA aktywna instancja DoT na typ naraz.
    private readonly Dictionary<StatusEffectType, DotEffect> activeDots = new Dictionary<StatusEffectType, DotEffect>();

    private float slowRemaining;
    private float slowMultiplier = 1f;

    private float stunRemaining;

    // Odpalane przy KAZDEJ zmianie (nalozenie lub wygasniecie efektu) - PlayerStatusUI
    // moze na to nasluchiwac zamiast odpytywac co klatke, jesli kiedys zechcesz.
    public System.Action onStatusesChanged;

    public bool IsStunned { get { return stunRemaining > 0f; } }

    // 1 = normalna predkosc, mniej = spowolniony. Kazdy skrypt ruchu (CreatureAI,
    // CreatureWander, BossController, ruch gracza) mnozy przez to swoja predkosc.
    public float MoveSpeedMultiplier { get { return slowRemaining > 0f ? slowMultiplier : 1f; } }

    void Awake()
    {
        target = GetComponent<IDamageable>();
        if (target == null)
        {
            Debug.LogWarning($"{name}: StatusEffectController nie znalazl komponentu IDamageable " +
                              "(Creature/PlayerStats) na tym samym obiekcie - statusy zadajace obrazenia " +
                              "(zatrucie/podpalenie/krwawienie) nie beda dzialac na tym obiekcie.");
        }

        hitFlash = GetComponent<HitFlashController>();
    }

    void Update()
    {
        bool changed = false;

        if (stunRemaining > 0f)
        {
            stunRemaining -= Time.deltaTime;
            if (stunRemaining <= 0f) { stunRemaining = 0f; changed = true; }
        }

        if (slowRemaining > 0f)
        {
            slowRemaining -= Time.deltaTime;
            if (slowRemaining <= 0f) { slowRemaining = 0f; slowMultiplier = 1f; changed = true; }
        }

        if (activeDots.Count > 0) changed |= TickDots();

        if (changed) onStatusesChanged?.Invoke();
    }

    private bool TickDots()
    {
        bool changed = false;
        List<StatusEffectType> expired = null;

        foreach (KeyValuePair<StatusEffectType, DotEffect> pair in activeDots)
        {
            DotEffect dot = pair.Value;

            dot.remaining -= Time.deltaTime;
            if (dot.remaining <= 0f)
            {
                if (expired == null) expired = new List<StatusEffectType>();
                expired.Add(pair.Key);
                changed = true;
                continue;
            }

            dot.tickTimer -= Time.deltaTime;
            if (dot.tickTimer <= 0f)
            {
                dot.tickTimer = Mathf.Max(0.05f, dot.tickInterval);

                // NOWE: kazdy typ DoT-a ma wlasny kolor poswiaty (patrz HitFlashController).
                if (target != null) target.TakeDamage(dot.tickDamage, false, Vector2.zero, TypeToFlashColor(pair.Key));
            }
        }

        if (expired != null)
        {
            foreach (StatusEffectType type in expired) activeDots.Remove(type);
        }

        return changed;
    }

    // Poison/Burn/Bleed maja wlasny kolor poswiaty - reszta typow tu nie trafia
    // (Slow/Stun nie sa DoT-ami, wiec nigdy nie przechodza przez TickDots()).
    private static HitFlashColor TypeToFlashColor(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Poison: return HitFlashColor.Poison;
            case StatusEffectType.Burn: return HitFlashColor.Burn;
            case StatusEffectType.Bleed: return HitFlashColor.Bleed;
            default: return HitFlashColor.Normal;
        }
    }

    // ===============================================================
    // NAKLADANIE STATUSOW
    // ===============================================================

    // Wspolny mechanizm zatrucia/podpalenia/krwawienia - rozny tylko typem (ikonka w UI).
    private void ApplyDot(StatusEffectType type, int tickDamage, float tickInterval, float duration)
    {
        if (activeDots.TryGetValue(type, out DotEffect existing))
        {
            // Ponowne trafienie NIGDY nie oslabia juz trwajacego statusu - bierzemy
            // silniejsze obrazenia na tik i dluzszy pozostaly czas z dwoch nalozen.
            existing.tickDamage = Mathf.Max(existing.tickDamage, Mathf.Max(1, tickDamage));
            existing.tickInterval = Mathf.Max(0.05f, tickInterval);
            existing.remaining = Mathf.Max(existing.remaining, duration);
        }
        else
        {
            activeDots[type] = new DotEffect
            {
                tickDamage = Mathf.Max(1, tickDamage),
                tickInterval = Mathf.Max(0.05f, tickInterval),
                tickTimer = Mathf.Max(0.05f, tickInterval), // pierwszy tik dopiero po pierwszym interwale, nie od razu
                remaining = duration
            };
        }

        onStatusesChanged?.Invoke();
    }

    public void ApplyPoison(int tickDamage, float tickInterval, float duration) =>
        ApplyDot(StatusEffectType.Poison, tickDamage, tickInterval, duration);

    public void ApplyBurn(int tickDamage, float tickInterval, float duration) =>
        ApplyDot(StatusEffectType.Burn, tickDamage, tickInterval, duration);

    public void ApplyBleed(int tickDamage, float tickInterval, float duration) =>
        ApplyDot(StatusEffectType.Bleed, tickDamage, tickInterval, duration);

    // Slabszy/krotszy Slow NIGDY nie oslabia juz trwajacego spowolnienia - silniejszy
    // (nizszy) mnoznik i dluzszy pozostaly czas licza sie NIEZALEZNIE od siebie.
    public void ApplySlow(float speedMultiplier, float duration)
    {
        speedMultiplier = Mathf.Clamp01(speedMultiplier);

        slowMultiplier = slowRemaining > 0f ? Mathf.Min(slowMultiplier, speedMultiplier) : speedMultiplier;
        slowRemaining = Mathf.Max(slowRemaining, duration);

        // NOWE: Spowolnienie nie zadaje obrazen (wiec nigdy nie przejdzie przez
        // TickDots/TakeDamage), ale wciaz chcemy wizualny sygnal - stad osobny,
        // bezposredni Play() zamiast kolorowania tiku obrazen.
        if (hitFlash != null) hitFlash.Play(HitFlashColor.Frost);

        onStatusesChanged?.Invoke();
    }

    public void ApplyStun(float duration)
    {
        stunRemaining = Mathf.Max(stunRemaining, duration);
        onStatusesChanged?.Invoke();
    }

    // ===============================================================
    // ODCZYT (do UI - patrz PlayerStatusUI/StatusEffectIconSlot)
    // ===============================================================
    public bool IsActive(StatusEffectType type)
    {
        if (type == StatusEffectType.Slow) return slowRemaining > 0f;
        if (type == StatusEffectType.Stun) return stunRemaining > 0f;
        return activeDots.ContainsKey(type);
    }

    public float GetRemaining(StatusEffectType type)
    {
        if (type == StatusEffectType.Slow) return slowRemaining;
        if (type == StatusEffectType.Stun) return stunRemaining;
        return activeDots.TryGetValue(type, out DotEffect dot) ? dot.remaining : 0f;
    }
}
