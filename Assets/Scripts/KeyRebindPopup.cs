using UnityEngine;

// WSPOLNE okienko "wcisnij klawisz" dla WSZYSTKICH wierszy w zakladce Controls -
// jeden popup na cala gre. Wywolywany przez KeyRebindButton.BeginRebind(), nie
// trzeba go recznie odpalac.
//
// PODPIECIE W EDYTORZE:
//   1. W oknie Opcji (najlepiej jako dziecko OptionsPanel, PONIZEJ zakladek
//      General/Controls w Hierarchy - dzieki temu rysuje sie NA WIERZCHU) zrob
//      caly obiekt popupu: tlo (Image) + tekst (TextMeshPro - Text (UI)).
//   2. Powies TEN skrypt na tym obiekcie (albo na jego rodzicu).
//   3. Popup Root = caly ten obiekt (tlo+tekst razem) - zostaw go WYLACZONYM
//      w Hierarchy (odznaczony checkbox), skrypt sam go wlacza/wylacza.
//   4. Message Text = ten tekst w srodku.
public class KeyRebindPopup : MonoBehaviour
{
    public static KeyRebindPopup instance;

    [Header("Okno")]
    [Tooltip("Caly obiekt popupu (tlo + tekst) - w Hierarchy zostaw go WYLACZONYM.")]
    public GameObject popupRoot;

    public TMPro.TextMeshProUGUI messageText;

    [TextArea]
    [Tooltip("{0} zostanie zastapione nazwa funkcji, ktora akurat przypisujesz " +
             "(pole Display Name w KeyRebindButton danego wiersza).")]
    public string messageFormat = "Zmiana sterowania\nWcisnij przycisk, pod ktory chcesz przypisac funkcje \"{0}\"\n\n(Escape - anuluj)";

    private System.Action<KeyCode> onKeyChosen;
    private bool waiting;

    void Awake()
    {
        instance = this;
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    // Wywolywane przez KeyRebindButton - callback dostaje wybrany klawisz.
    public void Show(string displayName, System.Action<KeyCode> callback)
    {
        onKeyChosen = callback;
        waiting = true;

        if (messageText != null) messageText.text = string.Format(messageFormat, displayName);
        if (popupRoot != null) popupRoot.SetActive(true);
    }

    private void Cancel()
    {
        waiting = false;
        onKeyChosen = null;
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    void Update()
    {
        if (!waiting) return;

        // Escape anuluje - nic sie nie zmienia.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
            return;
        }

        foreach (KeyCode code in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (code == KeyCode.None) continue;

            // Na razie tylko klawiatura - myszka pomijana.
            if (code >= KeyCode.Mouse0 && code <= KeyCode.Mouse6) continue;

            if (Input.GetKeyDown(code))
            {
                System.Action<KeyCode> callback = onKeyChosen;
                waiting = false;
                onKeyChosen = null;
                if (popupRoot != null) popupRoot.SetActive(false);

                callback?.Invoke(code);
                return;
            }
        }
    }
}
