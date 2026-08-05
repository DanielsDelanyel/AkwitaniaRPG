using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 10;
    public float lifeTime = 3f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        float rawAngle = (transform.eulerAngles.z + 45f) * Mathf.Deg2Rad;
        Vector2 flyDirection = new Vector2(Mathf.Cos(rawAngle), Mathf.Sin(rawAngle)).normalized;

        rb.linearVelocity = flyDirection * speed;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Creature creature = collision.GetComponent<Creature>();
        if (creature != null)
        {
            // Pobieramy szanse na krytyk i obra¿enia z gracza
            bool isCrit = Random.Range(0f, 100f) < PlayerStats.instance.critChance;

            // Strza³a ma swoje bazowe damage, ale mno¿nik bierze z klasy postaci
            int finalDmg = isCrit ? Mathf.RoundToInt(damage * PlayerStats.instance.critDamageMultiplier) : damage;

            Vector2 hitDir = (collision.transform.position - transform.position).normalized;

            creature.TakeDamage(finalDmg, isCrit, hitDir);
            Destroy(gameObject);
        }
        else if (collision.CompareTag("Obstacle") || collision.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}