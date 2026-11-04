using UnityEngine;

// "DACH" TUNELU POD MUREM (np. przejscie przez brame).
//
// Powies na Tilemapie 'Tunel_wierzch', na ktorej lezy KOPIA kafli muru nad
// przejsciem (chodnik i czubki blankow w kolumnie bramy). Ta Tilemapa jest
// rysowana NAD graczem (Sorting Layer 'Envoirment(in front)'), wiec gracz
// idacy brama po ziemi chowa sie pod murem.
//
// Gdy gracz jest na murach (PlayerElevation.IsOnWall), kopia znika - inaczej
// zaslanialaby go, kiedy przechodzi po murze nad brama. Pod spodem i tak sa
// te same kafle na zwyklej Tilemapie muru, wiec nic nie "mignie".
public class TunnelOverlay : MonoBehaviour
{
    private Renderer[] renderers;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    void LateUpdate()
    {
        bool playerOnWall = PlayerElevation.instance != null && PlayerElevation.instance.IsOnWall;

        foreach (Renderer r in renderers)
            if (r != null && r.enabled == playerOnWall) r.enabled = !playerOnWall;
    }
}
