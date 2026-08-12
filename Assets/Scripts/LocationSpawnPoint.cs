using System.Collections.Generic;
using UnityEngine;

// Znacznik miejsca, w ktorym gracz pojawia sie po wejsciu do lokacji.
// Powies to na pustym GameObjekcie tuz przed drzwiami / u wylotu jaskini.
public class LocationSpawnPoint : MonoBehaviour
{
    [Tooltip("Unikalna nazwa w obrebie tej lokacji, np. 'FrontDoor', 'CaveExit'. " +
             "Drzwi w drugiej scenie musza wpisac dokladnie to samo ID.")]
    public string spawnId = "Start";

    private static readonly List<LocationSpawnPoint> active = new List<LocationSpawnPoint>();

    void OnEnable()
    {
        if (!active.Contains(this)) active.Add(this);
    }

    void OnDisable()
    {
        active.Remove(this);
    }

    public static LocationSpawnPoint Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i] == null) { active.RemoveAt(i); continue; }
            if (active[i].spawnId == id) return active[i];
        }
        return null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.7f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, spawnId);
#endif
    }
}