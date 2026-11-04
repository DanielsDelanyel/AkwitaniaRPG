using System.Collections.Generic;
using UnityEngine;

// STREFA NA GORZE SCHODOW. Gdy stopy gracza sa w tej strefie, gracz jest
// na poziomie murow.
//
// Ustawienie: pusty obiekt z BoxCollider2D (Is Trigger = ON) postawiony na
// gornym koncu schodow, w przerwie miedzy blankami, tam gdzie schody lacza sie
// z chodnikiem. Warstwa tego obiektu nie ma znaczenia.
//
// UWAGA: strefa NIE korzysta z OnTriggerEnter/Exit - zmiana warstwy fizyki
// gracza powodowala w Unity falszywe "wyjscie" ze strefy i miganie miedzy
// poziomami. PlayerElevation sam co klatke fizyki sprawdza, czy stopy gracza
// leza w ktorejs strefie (IsInsideAny).
[RequireComponent(typeof(Collider2D))]
public class ElevationZone : MonoBehaviour
{
    private static readonly List<ElevationZone> all = new List<ElevationZone>();
    private Collider2D col;

    void Awake() { col = GetComponent<Collider2D>(); }
    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    public static bool IsInsideAny(Vector2 point)
    {
        for (int i = 0; i < all.Count; i++)
        {
            ElevationZone z = all[i];
            if (z != null && z.col != null && z.col.enabled && z.col.OverlapPoint(point)) return true;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
        Gizmos.DrawCube(c.bounds.center, c.bounds.size);
    }
}
