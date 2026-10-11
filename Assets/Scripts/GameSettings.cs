using System;
using System.Collections.Generic;
using UnityEngine;

// Ustawienia gracza (dzwiek, ekran, przypisania klawiszy) zapisywane w PlayerPrefs.
// Wywolaj GameSettings.ApplyAll() raz przy starcie gry - robi to MainMenuUI.
public static class GameSettings
{
    private const string KEY_MASTER = "opt_masterVolume";
    private const string KEY_MUSIC = "opt_musicVolume";
    private const string KEY_SFX = "opt_sfxVolume";
    private const string KEY_FULLSCREEN = "opt_fullscreen";
    private const string KEY_VSYNC = "opt_vsync";

    public static float MasterVolume
    {
        get { return PlayerPrefs.GetFloat(KEY_MASTER, 1f); }
        set { PlayerPrefs.SetFloat(KEY_MASTER, Mathf.Clamp01(value)); ApplyAudio(); }
    }

    public static float MusicVolume
    {
        get { return PlayerPrefs.GetFloat(KEY_MUSIC, 0.6f); }
        set { PlayerPrefs.SetFloat(KEY_MUSIC, Mathf.Clamp01(value)); ApplyAudio(); }
    }

    public static float SfxVolume
    {
        get { return PlayerPrefs.GetFloat(KEY_SFX, 0.8f); }
        set { PlayerPrefs.SetFloat(KEY_SFX, Mathf.Clamp01(value)); ApplyAudio(); }
    }

    public static bool Fullscreen
    {
        get { return PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1; }
        set { PlayerPrefs.SetInt(KEY_FULLSCREEN, value ? 1 : 0); ApplyScreen(); }
    }

    public static bool VSync
    {
        get { return PlayerPrefs.GetInt(KEY_VSYNC, 1) == 1; }
        set { PlayerPrefs.SetInt(KEY_VSYNC, value ? 1 : 0); ApplyScreen(); }
    }

    public static void ApplyAll()
    {
        ApplyAudio();
        ApplyScreen();
    }

    public static void ApplyAudio()
    {
        // Glosnosc glowna dziala na wszystko naraz
        AudioListener.volume = MasterVolume;

        // Efekty ida przez nasz SoundManager, wiec ustawiamy mu jego wlasny mnoznik
        if (SoundManager.instance != null) SoundManager.instance.masterVolume = SfxVolume;

        // Muzyka: MusicPlayer sam sie odswiezy, jesli istnieje
        if (MusicPlayer.instance != null) MusicPlayer.instance.RefreshVolume();
    }

    public static void ApplyScreen()
    {
        Screen.fullScreen = Fullscreen;
        QualitySettings.vSyncCount = VSync ? 1 : 0;
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    public static void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey(KEY_MASTER);
        PlayerPrefs.DeleteKey(KEY_MUSIC);
        PlayerPrefs.DeleteKey(KEY_SFX);
        PlayerPrefs.DeleteKey(KEY_FULLSCREEN);
        PlayerPrefs.DeleteKey(KEY_VSYNC);
        ResetKeyBindings();
        ApplyAll();
    }

    // ===============================================================
    // PRZYPISANIA KLAWISZY (przemapowywalne)
    // ===============================================================
    // ELASTYCZNOSC NA PRZYSZLOSC: to jedyne miejsce, ktore trzeba dotknac, zeby dodac
    // nowa przemapowywalna akcje - jeden wpis nizej, zero zmian gdziekolwiek indziej w
    // tej klasie. Skrypt, ktory dotychczas trzymal wlasne "public KeyCode xKey = ...",
    // zamiast tego pyta GameSettings.GetKey("NazwaAkcji") i cachuje wynik w polu
    // odswiezanym przez OnKeyBindingsChanged - dokladnie ten sam patent co
    // MusicPlayer.RefreshVolume() dla dzwieku. Patrz TopDownMovement.RefreshKeyBindings()
    // i SkillBarUI.RefreshKeyBindings() jako gotowy przyklad do skopiowania.
    private static readonly Dictionary<string, KeyCode> DefaultKeyBindings = new Dictionary<string, KeyCode>
    {
        { "Dash", KeyCode.Space },
        { "ToggleSkillBar", KeyCode.H },
        { "Interact", KeyCode.E },
        { "ToggleInventory", KeyCode.I },
        { "ToggleStats", KeyCode.V },
        { "ToggleQuestLog", KeyCode.L },
        { "ToggleSkillTree", KeyCode.G },
        { "Hotbar1", KeyCode.Alpha1 },
        { "Hotbar2", KeyCode.Alpha2 },
        { "Hotbar3", KeyCode.Alpha3 },
        { "Hotbar4", KeyCode.Alpha4 },
        { "Hotbar5", KeyCode.Alpha5 },
        { "Hotbar6", KeyCode.Alpha6 },
        { "Hotbar7", KeyCode.Alpha7 },
        { "Hotbar8", KeyCode.Alpha8 },
    };

    private const string KEY_BIND_PREFIX = "keybind_";

    // Subskrybuj to w kazdym skrypcie, ktory cachuje sobie ktorykolwiek KeyCode z
    // GameSettings - odpala sie po kazdej zmianie (SetKey) i po przywroceniu
    // domyslnych (ResetKeyBindings).
    public static event Action OnKeyBindingsChanged;

    public static KeyCode GetKey(string action)
    {
        if (!DefaultKeyBindings.TryGetValue(action, out KeyCode fallback))
        {
            Debug.LogWarning($"GameSettings.GetKey: nieznana akcja '{action}' - dopisz ja do DefaultKeyBindings.");
            fallback = KeyCode.None;
        }

        string saved = PlayerPrefs.GetString(KEY_BIND_PREFIX + action, "");
        if (!string.IsNullOrEmpty(saved) && Enum.TryParse(saved, out KeyCode parsed)) return parsed;

        return fallback;
    }

    public static void SetKey(string action, KeyCode key)
    {
        PlayerPrefs.SetString(KEY_BIND_PREFIX + action, key.ToString());
        OnKeyBindingsChanged?.Invoke();
    }

    public static void ResetKeyBindings()
    {
        foreach (KeyValuePair<string, KeyCode> pair in DefaultKeyBindings)
            PlayerPrefs.DeleteKey(KEY_BIND_PREFIX + pair.Key);

        OnKeyBindingsChanged?.Invoke();
    }

    // Do automatycznego wypelnienia listy wierszy w ekranie Opcji, bez recznego
    // przepisywania nazw akcji w kolejnym miejscu.
    public static IEnumerable<string> AllKeyBindingActions => DefaultKeyBindings.Keys;
}