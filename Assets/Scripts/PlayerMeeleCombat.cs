using UnityEngine;
using System.Collections.Generic;

public class PlayerMeleeAttack : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Collider2D hitCollider;

    private int damage;
    private float swingDuration;
    private float timer;
    private float startAngle;
    private float endAngle;

    // Lista, ¿eby nie zadaæ obra¿eñ tej samej owcy 5 razy podczas jednego machniêcia!
    private List<Collider2D> alreadyHit = new List<Collider2D>();

    public void Setup(ItemData weapon, int dmg, float duration, float angleToMouse)
    {
        spriteRenderer.sprite = weapon.icon;
        damage = dmg;
        swingDuration = duration;

        // Tworzymy ³uk o szerokoœci 65 stopni
        startAngle = angleToMouse - 32.5f;
        endAngle = angleToMouse + 32.5f;

        // Ustawiamy miecz na pozycji startowej
        transform.rotation = Quaternion.Euler(0, 0, startAngle);

        // Miecz zniknie sam po zakoñczeniu animacji ciêcia
        Destroy(gameObject, swingDuration);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / swingDuration;

        // P³ynny obrót z punktu A (startAngle) do punktu B (endAngle)
        float currentAngle = Mathf.Lerp(startAngle, endAngle, progress);
        transform.rotation = Quaternion.Euler(0, 0, currentAngle);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (alreadyHit.Contains(collision)) return;

        Creature creature = collision.GetComponent<Creature>();
        if (creature != null)
        {
            // Szansa na krytyk pobierana z postaci
            bool isCrit = Random.Range(0f, 100f) < PlayerStats.instance.critChance;

            // Mno¿nik krytyka pobierany z postaci
            int finalDmg = isCrit ? Mathf.RoundToInt(damage * PlayerStats.instance.critDamageMultiplier) : damage;

            Vector2 hitDir = (collision.transform.position - transform.position).normalized;

            creature.TakeDamage(finalDmg, isCrit, hitDir);
            alreadyHit.Add(collision);
        }
    }
}