using UnityEngine;
using UnityEngine.UI;

// ZAKLADKI "General" / "Controls" w oknie Opcji - dwa przyciski u gory, dwa
// panele pod spodem, zawsze widoczny dokladnie jeden. Ten sam patent co
// PauseMenuUI.ShowMainPanel()/SetPanel() - zwykle wlaczanie/wylaczanie obiektow,
// bez zadnej dodatkowej biblioteki UI.
//
// PODPIECIE W EDYTORZE:
//   1. W OptionsPanel zrob dwa przyciski "General"/"Controls" (juz je masz na
//      screenie) i dwa osobne obiekty-panele pod nimi: np. "GeneralPanel"
//      (Twoje dzisiejsze suwaki/toggle/rozdzielczosc) i "ControlsPanel" (nowa
//      lista wierszy KeyRebindButton).
//   2. Powies ten skrypt na OptionsPanel (obok OptionsMenu).
//   3. Podepnij oba przyciski i oba panele w Inspektorze.
// WAZNE: OptionsMenu.cs (suwaki/toggle) i KeyRebindButton (wiersze klawiszy)
// dzialaja tak samo, niezaleznie od tego, czy ich panel jest akurat widoczny -
// nic tam nie trzeba zmieniac.
public class OptionsTabsUI : MonoBehaviour
{
    [Header("Przyciski zakladek")]
    public Button generalTabButton;
    public Button controlsTabButton;

    [Header("Panele zakladek")]
    public GameObject generalPanel;
    public GameObject controlsPanel;

    void Start()
    {
        if (generalTabButton != null) generalTabButton.onClick.AddListener(ShowGeneral);
        if (controlsTabButton != null) controlsTabButton.onClick.AddListener(ShowControls);

        ShowGeneral(); // zakladka startowa przy kazdym otwarciu Opcji
    }

    void OnEnable()
    {
        ShowGeneral();
    }

    public void ShowGeneral()
    {
        if (generalPanel != null) generalPanel.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    public void ShowControls()
    {
        if (generalPanel != null) generalPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);
    }
}
