using UnityEngine;
using UnityEngine.UI;
using TMPro;

// FUNDAMENT: male okienko z suwakiem do wyboru ILE sztuk odlaczyc od stosu
// (zamiast starego, sztywnego "zawsze polowa" w SplitStack).
//
// Cala logika ponizej jest juz kompletna i dziala - zeby zaczelo dzialac
// w grze, trzeba jeszcze w edytorze:
//   1. Zbudowac Panel z: Slider (Horizontal Slider) + TextMeshProUGUI na wartosc
//      + dwa przyciski (Potwierdz / Anuluj).
//   2. Powiesic ten skrypt na wspolnym rodzicu tego Panelu (moze byc ten sam
//      obiekt co Panel).
//   3. W Inspektorze podpiac pola: Panel, Amount Slider, Amount Text,
//      Confirm Button, Cancel Button.
//   4. Panel powinien startowo byc WYLACZONY w Hierarchii (Awake i tak go
//      chowa w Start(), ale wygodniej od razu widziec docelowy stan w edytorze).
//
// ContextMenuUI.OnSplitClicked() juz woła SplitAmountUI.instance.Open(slot),
// wiec samo pojawienie sie tego komponentu w scenie z podpietymi polami
// wystarczy, by przycisk "Podziel" zaczal otwierac to okno zamiast starego,
// natychmiastowego podzialu na pol.
public class SplitAmountUI : MonoBehaviour
{
    public static SplitAmountUI instance;

    [Header("Okno")]
    public GameObject panel;

    [Header("Suwak i podglad wartosci")]
    public Slider amountSlider;
    public TextMeshProUGUI amountText;

    [Header("Przyciski")]
    public Button confirmButton;
    public Button cancelButton;

    private InventorySlot targetSlot;

    void Awake()
    {
        instance = this;

        // Tak samo jak w ContextMenuUI - podpinamy akcje z kodu, zeby nie dalo
        // sie przypadkiem zle skonfigurowac "On Click" w Inspektorze.
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(Close);
        }
        if (amountSlider != null)
        {
            amountSlider.onValueChanged.RemoveAllListeners();
            amountSlider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    void Start()
    {
        Close();
    }

    public bool IsOpen { get { return panel != null && panel.activeSelf; } }

    // Otwiera okno dla danego slotu. Suwak: od 1 do (ilosc w slocie - 1),
    // zeby w oryginalnym stosie zawsze zostala co najmniej 1 sztuka.
    public void Open(InventorySlot slot)
    {
        if (slot == null || slot.item == null || !slot.item.isStackable || slot.amount < 2)
        {
            Debug.Log("Tego przedmiotu nie da sie podzielic.");
            return;
        }

        if (panel == null || amountSlider == null)
        {
            Debug.LogWarning("SplitAmountUI: brak podpietego Panel/Amount Slider w Inspektorze!");
            return;
        }

        targetSlot = slot;

        int maxSplit = slot.amount - 1;

        amountSlider.wholeNumbers = true;
        amountSlider.minValue = 1;
        amountSlider.maxValue = maxSplit;
        amountSlider.value = Mathf.Max(1, maxSplit / 2); // startowo polowa - jak stare zachowanie

        UpdateAmountText();
        panel.SetActive(true);
    }

    private void OnSliderChanged(float _)
    {
        UpdateAmountText();
    }

    private void UpdateAmountText()
    {
        if (amountText != null && amountSlider != null)
            amountText.text = Mathf.RoundToInt(amountSlider.value).ToString();
    }

    private void OnConfirmClicked()
    {
        if (targetSlot != null && amountSlider != null && InventoryUI.instance != null)
        {
            int chosenAmount = Mathf.RoundToInt(amountSlider.value);
            InventoryUI.instance.SplitStackAmount(targetSlot, chosenAmount);
        }

        Close();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        targetSlot = null;
    }
}
