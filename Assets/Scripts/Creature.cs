using UnityEngine;

// Definiujemy mo¿liwe nastawienia stworzenia (mo¿esz tego u¿ywaæ w innych skryptach!)
public enum Disposition
{
    Peaceful,   // Pokojowy (Ucieka lub ignoruje atak)
    Neutral,    // Neutralny (Atakuje tylko, gdy sam zostanie zaatakowany)
    Aggressive  // Agresywny (Atakuje gracza, gdy tylko go zobaczy)
}

public class Creature : MonoBehaviour
{
    [Header("Identyfikacja")]
    public string creatureName = "Owieczka";
    public Disposition disposition = Disposition.Peaceful;

    [Header("Rozwój i Nagrody")]
    public int level = 1;
    public int expReward = 15; // Ile punktów doœwiadczenia dostanie gracz za pokonanie go
    public int moneyReward = 0; // Ile monet dostanie gracz za pokonanie

    [Header("Statystyki Bazowe")]
    public int baseSTR = 2; // Si³a
    public int baseWIT = 2; // Witalnoœæ
    public int baseINT = 1; // Inteligencja
    public int baseZR = 2;  // Zrêcznoœæ
    public int baseCHAR = 1;// Charyzma

    public int baseDmg = 2;
    public int baseMagicDmg = 0;

    public int baseDef = 1;
    public int baseMagicDef = 0;

    [Header("Stan ¯ycia")]
    public int maxHealth;
    public int currentHealth;

    [Header("UI Walki")]
    public GameObject damagePopupPrefab;

    void Start()
    {
        // Inicjalizacja ¿ycia na podstawie Witalnoœci (np. 1 punkt WIT = 10 HP)
        maxHealth = baseWIT * 10;
        currentHealth = maxHealth;
    }

    // Funkcja wywo³ywana, gdy gracz uderzy stworzenie
    public void TakeDamage(int damage, bool isCrit, Vector2 hitDirection)
    {
        int finalDamage = damage - baseDef;
        if (finalDamage < 1) finalDamage = 1;

        currentHealth -= finalDamage;

        // --- TWORZENIE NAPISU ---
        if (damagePopupPrefab != null)
        {
            // Tworzymy napis lekko nad g³ow¹ postaci
            GameObject popup = Instantiate(damagePopupPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            popup.GetComponent<DamagePopup>().Setup(finalDamage, isCrit, hitDirection);
        }

        if (currentHealth <= 0) Die();
        else
        {
            if (disposition == Disposition.Neutral) disposition = Disposition.Aggressive;
        }
    }
    void Die()
    {
        Debug.Log($"{creatureName} umiera. Gracz zyskuje {expReward} EXP i {moneyReward} monet.");

        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.currentMoney += moneyReward;

            // --- ZMIANA: Zamiast po prostu dodawaæ expa, wywo³ujemy nasz¹ now¹ funkcjê! ---
            PlayerStats.instance.AddExp(expReward);

            // RecalculateStats() usuniête st¹d, bo AddExp wywo³a je samo w razie awansu
        }

        Destroy(gameObject);
    }
}