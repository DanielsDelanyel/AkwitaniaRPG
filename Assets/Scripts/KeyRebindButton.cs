using UnityEngine;
using UnityEngine.UI;

// POJEDYNCZY WIERSZ w zakladce Controls: z lewej (osobny tekst, ustawiasz go
// recznie w Hierarchy - ten skrypt go nie dotyka) nazwa funkcji, z prawej TEN
// przycisk pokazujacy aktualnie przypisany klawisz. Klikniecie otwiera WSPOLNY
// popup (KeyRebindPopup) z instrukcja - kolejny nacisniety klawisz staje sie
// nowym przypisaniem danej AKCJI (patrz GameSettings.GetKey/SetKey).
//
// PODPIECIE W EDYTORZE:
//   1. Zrob przycisk (Button - TextMeshPro) - to bedzie "okienko z klawiszem".
//   2. Dodaj do niego ten skrypt: Button = ten sam obiekt (mozna zostawic
//      puste), Key Label = jego wlasny tekst w srodku.
//   3. Action Name = DOKLADNIE jak w GameSettings.DefaultKeyBindings (np. "Dash").
//   4. Display Name = ladna nazwa PO POLSKU pokazywana w oknie popup (np.
//      "Dash" albo "Otworz pasek umiejetnosci") - zwykle to samo co napis
//      obok w kolumnie funkcji, ale to dwa OSOBNE miejsca do wpisania.
//   5. Gdziekolwiek w oknie Opcji musi istniec JEDEN KeyRebindPopup (patrz ten
//      plik) - bez niego klikniecie tylko wypisze ostrzezenie w konsoli.
//
// DODANIE NOWEJ PRZEMAPOWYWALNEJ AKCJI: dopisz jeden wpis w
// GameSettings.DefaultKeyBindings, podepnij cache/odswiezanie w docelowym
// skrypcie (patrz TopDownMovement.RefreshKeyBindings() jako gotowy wzor), a
// tutaj po prostu dolóż kolejny wiersz w Hierarchy z innym Action Name /
// Display Name. Zero zmian w kodzie tego skryptu.
public class KeyRebindButton : MonoBehaviour
{
    [Tooltip("Musi dokladnie pasowac do klucza uzytego w GameSettings.GetKey/SetKey.")]
    public string actionName;

    [Tooltip("Nazwa pokazywana w oknie popup przy przypisywaniu, np. \"Dash\".")]
    public string displayName;

    public Button button;
    public TMPro.TextMeshProUGUI keyLabel;

    void Start()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(BeginRebind);

        RefreshLabel();
    }

    private void BeginRebind()
    {
        if (KeyRebindPopup.instance == null)
        {
            Debug.LogWarning("KeyRebindButton: brak KeyRebindPopup w scenie - dodaj go w oknie Opcji.");
            return;
        }

        string shown = string.IsNullOrEmpty(displayName) ? actionName : displayName;
        KeyRebindPopup.instance.Show(shown, OnKeyChosen);
    }

    // Wywolywane przez KeyRebindPopup PO wybraniu klawisza (nie wywolasz tego recznie).
    private void OnKeyChosen(KeyCode key)
    {
        // SetKey samo odpala GameSettings.OnKeyBindingsChanged, wiec kazdy skrypt
        // trzymajacy cache tego klawisza (np. TopDownMovement) odswiezy sie automatycznie.
        GameSettings.SetKey(actionName, key);
        RefreshLabel();
    }

    public void RefreshLabel()
    {
        if (keyLabel != null) keyLabel.text = GameSettings.GetKey(actionName).ToString();
    }
}
