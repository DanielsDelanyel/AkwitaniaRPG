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
    }

    public void TakeDamage(int damage, bool isCrit, Vector2 hitDirection)
    {
        if (IsDead) return; // trup nie krwawi dwa razy

        int finalDamage = damage - baseDef;
        if (finalDamage < 1) finalDamage = 1;

        currentHealth -= finalDamage;

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

        // WAZNE: lup wypada TERAZ, zanim obiekt zniknie
        if (onDeath != null) onDeath.Invoke(this);

        if (destroyOnDeath) Destroy(gameObject, destroyDelay);
    }
}
