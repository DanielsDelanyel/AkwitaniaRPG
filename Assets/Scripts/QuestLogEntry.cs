using UnityEngine;
using UnityEngine.UI;
using TMPro;

// POJEDYNCZY WIERSZ na liscie w Dzienniku Zadan.
// Zawiesic na prefabie przycisku z podpiedzieckiem TextMeshProUGUI na tytul.
public class QuestLogEntry : MonoBehaviour
{
    [Header("Referencje")]
    public Button button;
    public TextMeshProUGUI titleText;

    [Tooltip("Opcjonalne: maly tekst postepu, np. '2/5'. Moze zostac puste.")]
    public TextMeshProUGUI progressText;

    private QuestProgress progress;

    public void Setup(QuestProgress progress, QuestLogUI owner)
    {
        this.progress = progress;

        Quest def = progress.Definition;
        if (titleText != null) titleText.text = def != null ? def.title : "???";

        if (progressText != null)
        {
            string tag = "";
            if (progress.state == QuestState.ReadyToTurnIn) tag = "Gotowe do oddania";
            else if (progress.state == QuestState.Completed) tag = "Ukonczone";

            progressText.gameObject.SetActive(!string.IsNullOrEmpty(tag));
            progressText.text = tag;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => owner.ShowDetails(this.progress));
        }
    }
}
