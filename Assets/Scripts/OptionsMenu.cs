using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Panel opcji. Powies to na obiekcie OptionsPanel.
// Wszystkie pola sa opcjonalne - podepnij tylko te, ktore faktycznie masz.
public class OptionsMenu : MonoBehaviour
{
    [Header("Dzwiek")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Tooltip("Opcjonalne etykiety pokazujace procent obok suwakow.")]
    public TextMeshProUGUI masterLabel;
    public TextMeshProUGUI musicLabel;
    public TextMeshProUGUI sfxLabel;

    [Header("Ekran")]
    public Toggle fullscreenToggle;
    public Toggle vsyncToggle;
    public TMP_Dropdown resolutionDropdown;

    [Header("Przyciski")]
    public Button resetButton;

    private Resolution[] resolutions;
    private bool isInitializing;

    void OnEnable()
    {
        RefreshFromSettings();
    }

    void Start()
    {
        BuildResolutionDropdown();
        WireControls();
        RefreshFromSettings();
    }

    private void WireControls()
    {
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveAllListeners();
            masterSlider.onValueChanged.AddListener(v =>
            {
                if (isInitializing) return;
                GameSettings.MasterVolume = v;
                UpdateLabels();
            });
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(v =>
            {
                if (isInitializing) return;
                GameSettings.MusicVolume = v;
                UpdateLabels();
            });
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(v =>
            {
                if (isInitializing) return;
                GameSettings.SfxVolume = v;
                UpdateLabels();
            });
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.AddListener(v =>
            {
                if (isInitializing) return;
                GameSettings.Fullscreen = v;
            });
        }

        if (vsyncToggle != null)
        {
            vsyncToggle.onValueChanged.RemoveAllListeners();
            vsyncToggle.onValueChanged.AddListener(v =>
            {
                if (isInitializing) return;
                GameSettings.VSync = v;
            });
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(ApplyResolution);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(() =>
            {
                GameSettings.ResetToDefaults();
                RefreshFromSettings();
            });
        }
    }

    // Ustawia kontrolki wedlug zapisanych wartosci, bez odpalania zdarzen
    private void RefreshFromSettings()
    {
        isInitializing = true;

        if (masterSlider != null) masterSlider.value = GameSettings.MasterVolume;
        if (musicSlider != null) musicSlider.value = GameSettings.MusicVolume;
        if (sfxSlider != null) sfxSlider.value = GameSettings.SfxVolume;

        if (fullscreenToggle != null) fullscreenToggle.isOn = GameSettings.Fullscreen;
        if (vsyncToggle != null) vsyncToggle.isOn = GameSettings.VSync;

        UpdateLabels();

        isInitializing = false;
    }

    private void UpdateLabels()
    {
        if (masterLabel != null) masterLabel.text = Mathf.RoundToInt(GameSettings.MasterVolume * 100f) + "%";
        if (musicLabel != null) musicLabel.text = Mathf.RoundToInt(GameSettings.MusicVolume * 100f) + "%";
        if (sfxLabel != null) sfxLabel.text = Mathf.RoundToInt(GameSettings.SfxVolume * 100f) + "%";
    }

    private void BuildResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;

        List<string> labels = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            labels.Add($"{resolutions[i].width} x {resolutions[i].height}");

            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                currentIndex = i;
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(labels);
        resolutionDropdown.SetValueWithoutNotify(currentIndex);
        resolutionDropdown.RefreshShownValue();
    }

    private void ApplyResolution(int index)
    {
        if (isInitializing) return;
        if (resolutions == null || index < 0 || index >= resolutions.Length) return;

        Resolution r = resolutions[index];
        Screen.SetResolution(r.width, r.height, GameSettings.Fullscreen);
    }

    void OnDisable()
    {
        // Zapisujemy na dysk dopiero przy wyjsciu z panelu,
        // zeby nie pisac do pliku przy kazdym ruchu suwaka
        GameSettings.Save();
    }
}