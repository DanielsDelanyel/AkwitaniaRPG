using UnityEngine;

// Ustawienia gracza (dzwiek, ekran) zapisywane w PlayerPrefs.
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
        ApplyAll();
    }
}