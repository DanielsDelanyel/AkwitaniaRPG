using UnityEngine;

// Granica regionu: gracz, ktory w nia wejdzie, przechodzi do innej sceny
// (bez klawisza E, w odroznieniu od LocationEntrance - do drog i krawedzi mapy).
// Ustawienie: pusty GameObject na krawedzi regionu + BoxCollider2D (Is Trigger)
// + ten skrypt. W scenie docelowej musi byc LocationSpawnPoint o ID = targetSpawnId,
// postawiony KILKA KAFLI od tamtejszej granicy (inaczej gracz od razu wroci).
[RequireComponent(typeof(Collider2D))]
public class RegionBorder : MonoBehaviour
{
    [Tooltip("Nazwa sceny docelowej, dokladnie jak w Build Settings, np. 'Forest'.")]
    public string targetLocation = "Forest";

    [Tooltip("ID punktu LocationSpawnPoint w scenie docelowej, np. 'ZWioski'.")]
    public string targetSpawnId = "ZWioski";

    [Tooltip("Czy blokowac przejscie, gdy gracza ktos goni (CombatTracker).")]
    public bool blockDuringCombat = true;
    public string combatBlockedMessage = "Nie mozesz teraz odejsc - najpierw zgub albo pokonaj przeciwnika!";

    [Tooltip("Co ile sekund najwyzej pokazywac komunikat o blokadzie.")]
    public float messageCooldown = 2f;

    private float nextMessageTime;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // Stay zamiast Enter: gdy gracz stoi na granicy w trakcie walki i walka sie
    // skonczy, przejscie zadziala bez wychodzenia i wchodzenia jeszcze raz.
    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (LocationManager.instance == null || LocationManager.instance.IsTransitioning) return;

        if (blockDuringCombat && CombatTracker.IsPlayerInCombat)
        {
            if (Time.time >= nextMessageTime)
            {
                PlayerNotice.Show(combatBlockedMessage);
                nextMessageTime = Time.time + messageCooldown;
            }
            return;
        }

        LocationManager.instance.GoTo(targetLocation, targetSpawnId);
    }

    void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(col.bounds.center, "-> " + targetLocation + " / " + targetSpawnId);
#endif
    }
}
