#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// UWAGA: ten plik MUSI lezec w folderze o nazwie "Editor"!
//
// Po co to jest:
// Przycisk Play w edytorze uruchamia scene AKTUALNIE OTWARTA w Hierarchii,
// a nie te z numerem 0 w Build Settings. Kolejnosc w Build Settings liczy sie
// dopiero w zbudowanej grze.
//
// To narzedzie sprawia, ze Play zawsze startuje od menu glownego -
// niezaleznie od tego, ktora scene masz otwarta do edycji.
[InitializeOnLoad]
public static class PlayFromMainMenu
{
    private const string MENU_PATH = "Tools/Zawsze startuj od MainMenu";
    private const string PREF_KEY = "playFromMainMenu_enabled";
    private const string SCENE_NAME = "MainMenu";

    private static bool IsEnabled
    {
        get { return EditorPrefs.GetBool(PREF_KEY, false); }
        set { EditorPrefs.SetBool(PREF_KEY, value); }
    }

    static PlayFromMainMenu()
    {
        // Odswiezamy ustawienie po kazdym przeladowaniu skryptow
        EditorApplication.delayCall += Apply;
    }

    [MenuItem(MENU_PATH)]
    private static void Toggle()
    {
        IsEnabled = !IsEnabled;
        Apply();

        Debug.Log(IsEnabled
            ? $"Play bedzie teraz startowal od sceny '{SCENE_NAME}'."
            : "Play startuje od sceny otwartej w Hierarchii.");
    }

    [MenuItem(MENU_PATH, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MENU_PATH, IsEnabled);
        return true;
    }

    private static void Apply()
    {
        if (!IsEnabled)
        {
            EditorSceneManager.playModeStartScene = null;
            return;
        }

        SceneAsset scene = FindScene(SCENE_NAME);

        if (scene == null)
        {
            Debug.LogWarning($"Nie znaleziono sceny '{SCENE_NAME}' w projekcie.");
            EditorSceneManager.playModeStartScene = null;
            return;
        }

        EditorSceneManager.playModeStartScene = scene;
    }

    private static SceneAsset FindScene(string sceneName)
    {
        string[] guids = AssetDatabase.FindAssets($"t:SceneAsset {sceneName}");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Dopasowanie DOKLADNE - "MainMenu" nie ma byc mylone z "MainMenuOld"
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        return null;
    }
}
#endif