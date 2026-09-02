using System.Collections;
using UnityEngine;

public enum Disposition
{
    Peaceful,   // Pokojowy (ucieka lub ignoruje atak)
    Neutral,    // Neutralny (atakuje tylko, gdy sam zostanie zaatakowany)
    Aggressive  // Agresywny (atakuje gracza, gdy tylko go zobaczy)
}

public class Creature : MonoBehaviour
{
    [Header("Identyfikacja")]
    public string creatureName = "Owieczka";
    public Disposition disposition = Disposition.Peaceful;

    [Header("Rozwoj i Nagrody")]
    public int level = 1;
    public int expReward = 15;
    public int moneyReward = 0;

    [Header("Statystyki Bazowe")]
    public int baseSTR = 2;
    public int baseWIT = 2;
    public int baseINT = 1;
    public int baseZR = 2;
    public int baseCHAR = 1;

    public int baseDmg = 2;
    public int baseMagicDmg = 0;

    public int baseDef = 1;
    public int baseMagicDef = 0;

    [Header("Stan Zycia")]
    public int maxHealth;
    public int currentHealth;
    [Tooltip("Ile HP daje 1 punkt Witalnosci. Boss moze miec wiecej niz zwykly dzik.")]
    public int healthPerVitality = 10;

    [Header("UI Walki")]
    public GameObject damagePopupPrefab;

    // ===============================================================
    // NOWE: miganie przy trafieniu (biel -> czern -> biel -> powrot do normy)
    // Dziala dla KAZDEGO stworzenia (Creature, CreatureAI, BossController itd.),
    // bo TakeDamage() jest wspolne dla wszystkich.
    //
    // UWAGA TECHNICZNA: domyslny material sprite'ow (Sprite-Lit-Default/Sprites-Default)
    // tylko MNOZY kolor tekstury przez SpriteRenderer.color - ustawienie tego koloru
    // na bialy WIZUALNIE NIC NIE ZMIENIA (mnozenie przez 1 = bez zmian), dziala
    // tylko czern (mnozenie przez 0). Dlatego prawdziwy "flash na bialo" wymaga
    // osobnego shadera, ktory podmienia kolor zamiast go mnozyc - patrz
    // SpriteHitFlash.shader (dolaczony obok) i komentarz przy Hit Flash Material.
    // ===============================================================
    [Header("Miganie przy Trafieniu")]
    [Tooltip("Material korzystajacy z shadera 'Custom/SpriteHitFlash' (plik SpriteHitFlash.shader). " +
             "Jeden wspolny material wystarczy dla wszystkich stworzen - kazde miganie i tak " +
             "tworzy sobie wlasna, tymczasowa kopie, wiec kilka trafionych naraz nie miesza sie " +
             "ze soba. Zostaw puste, zeby CALKOWICIE wylaczyc miganie.")]
    public Material hitFlashMaterial;

    [Tooltip("Ile sekund trwa KAZDA z trzech faz (bialy/czarny/bialy). Cale miganie trwa 3x tyle - " +
             "przy domyslnych 0.05s to 0.15s, czyli bardzo szybko.")]
    public float hitFlashPhaseDuration = 0.05f;

    private SpriteRenderer[] flashRenderers;
    private Material[] flashOriginalMaterials;
    private Coroutine hitFlashRoutine;

    // ===============================================================
    // NOWE: obsluga smierci
    // ===============================================================
    [Header("Smierc")]
    [Tooltip("Odznacz dla bossa - wtedy o zniknieciu decyduje DeathFadeEffect.")]
    public bool destroyOnDeath = true;

    [Tooltip("Opoznienie zniszczenia obiektu (sekundy).")]
    public float destroyDelay = 0f;

    // Czy juz nie zyje? Chroni przed podwojnym zabiciem i podwojnym lupem.
    public bool IsDead { get; private set; }

    // Tu podpinaja sie CreatureLoot i DeathFadeEffect
    public System.Action<Creature> onDeath;

    // Przydaje sie paskowi zycia bossa
    public System.Action<Creature> onHealthChanged;

    void Start()
    {
        if (maxHealth <= 0) maxHealth = baseWIT * healthPerVitality;
        currentHealth = maxHealth;

        // Zbieramy WSZYSTKIE SpriteRenderery (np. osobna grafika + akcesoria),
        // zeby miganie objelo cala postac, a nie tylko jeden fragment.
        flashRenderers = GetComponentsInChildren<SpriteRenderer>();
        flashOriginalMaterials = new Material[flashRenderers.Length];
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            flashOriginalMaterials[i] = flashRenderers[i].sharedMaterial;
        }
    }

    public void TakeDamage(int damage, bool isCrit, Vector2 hitDirection)
    {
        if (IsDead) return; // trup nie krwawi dwa razy

        int finalDamage = damage - baseDef;
        if (finalDamage < 1) finalDamage = 1;

        currentHealth -= finalDamage;

        PlayHitFlash();

        if (damagePopupPrefab != null)
        {
            GameObject popup = Instantiate(damagePopupPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            popup.GetComponent<DamagePopup>().Setup(finalDamage, isCrit, hitDirection);
        }

        if (onHealthChanged != null) onHealthChanged.Invoke(this);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            if (disposition == Disposition.Neutral) disposition = Disposition.Aggressive;
        }
    }

    // Bialy -> czarny -> bialy -> powrot do oryginalnego materialu. Restartuje sie
    // od zera przy kazdym kolejnym trafieniu (StopCoroutine), zeby szybkie combo
    // ciosow nie nakladalo na siebie kilku rownoleglych sekwencji migania.
    private void PlayHitFlash()
    {
        if (hitFlashMaterial == null) return; // miganie wylaczone - nikt nie podpial materialu
        if (flashRenderers == null || flashRenderers.Length == 0) return;

        if (hitFlashRoutine != null) StopCoroutine(hitFlashRoutine);
        hitFlashRoutine = StartCoroutine(HitFlashSequence());
    }

    private IEnumerator HitFlashSequence()
    {
        // Swieza kopia materialu NA POTRZEBY TEGO JEDNEGO migniecia - dzieki temu
        // dwa stworzenia trafione w tej samej klatce nie nadpisuja sobie nawzajem
        // koloru na WSPOLNYM shared materiale.
        Material[] flashInstances = new Material[flashRenderers.Length];
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] == null) continue;
            flashInstances[i] = new Material(hitFlashMaterial);
            flashRenderers[i].material = flashInstances[i];
        }

        Color[] sequence = { Color.white, Color.black, Color.white };
        foreach (Color flashColor in sequence)
        {
            foreach (Material mat in flashInstances)
            {
                if (mat != null) mat.SetColor("_FlashColor", flashColor);
            }
            yield return new WaitForSeconds(hitFlashPhaseDuration);
        }

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null) flashRenderers[i].sharedMaterial = flashOriginalMaterials[i];
            if (flashInstances[i] != null) Destroy(flashInstances[i]);
        }

        hitFlashRoutine = null;
    }

    protected virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;

        Debug.Log($"{creatureName} umiera. Gracz zyskuje {expReward} EXP i {moneyReward} monet.");

        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.currentMoney += moneyReward;
            PlayerStats.instance.AddExp(expReward);
        }

        // Zadania dowiaduja sie o zabiciu ZANIM obiekt zniknie -
        // menedzer potrzebuje jeszcze odczytac creatureName i UniqueId.
        QuestManager.ReportKill(this);

        // WAZNE: lup wypada TERAZ, zanim obiekt zniknie
        if (onDeath != null) onDeath.Invoke(this);

        if (destroyOnDeath) Destroy(gameObject, destroyDelay);
    }
}
