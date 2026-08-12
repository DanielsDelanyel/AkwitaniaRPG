using UnityEditor;
using UnityEngine;

// UWAGA: ten plik MUSI lezec w folderze o nazwie "Editor"
// (np. Assets/Scripts/Editor/PixelArtImporterTool.cs), inaczej gra sie nie zbuduje.
public class PixelArtImporterTool
{
    private const int PIXELS_PER_UNIT = 32; // <- Twoj bazowy rozmiar kafelka

    [MenuItem("Tools/Pixel Art/Ustaw jako KAFELEK (siatka 32x32)")]
    private static void SetupTileset()
    {
        ApplyToSelection(true, SpriteAlignment.Center);
    }

    [MenuItem("Tools/Pixel Art/Ustaw jako OBIEKT (pivot na dole)")]
    private static void SetupObject()
    {
        ApplyToSelection(false, SpriteAlignment.BottomCenter);
    }

    [MenuItem("Tools/Pixel Art/Ustaw jako IKONA (ekwipunek)")]
    private static void SetupIcon()
    {
        ApplyToSelection(false, SpriteAlignment.Center);
    }

    private static void ApplyToSelection(bool isTileset, SpriteAlignment pivot)
    {
        Object[] textures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);

        if (textures.Length == 0)
        {
            Debug.LogWarning("Zaznacz najpierw jakies tekstury w oknie Project!");
            return;
        }

        int changed = 0;

        foreach (Object obj in textures)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            Texture2D tex = obj as Texture2D;

            // --- Podstawy pixel artu ---
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PIXELS_PER_UNIT;
            importer.filterMode = FilterMode.Point;          // ostre piksele
            importer.mipmapEnabled = false;                  // brak rozmycia z oddali
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spriteImportMode = isTileset ? SpriteImportMode.Multiple : SpriteImportMode.Single;

            // UWAGA: meshType, extrude i pivot NIE sa polami TextureImportera.
            // Siedza w osobnym obiekcie ustawien, ktory trzeba odczytac i zapisac.
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = (uint)(isTileset ? 1 : 0); // 1 px marginesu = koniec szczelin
            settings.spriteBorder = Vector4.zero;
            settings.spriteGenerateFallbackPhysicsShape = false;

            if (!isTileset)
            {
                settings.spriteAlignment = (int)pivot;
            }

            importer.SetTextureSettings(settings);

            // --- Zero kompresji, zeby piksele nie "brudzily sie" ---
            TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            platform.maxTextureSize = 8192;
            platform.format = TextureImporterFormat.RGBA32;
            importer.SetPlatformTextureSettings(platform);

            // --- Ostrzezenie o zlych wymiarach ---
            if (isTileset && tex != null)
            {
                if (tex.width % PIXELS_PER_UNIT != 0 || tex.height % PIXELS_PER_UNIT != 0)
                {
                    Debug.LogWarning(
                        $"'{tex.name}' ma {tex.width}x{tex.height} px - to NIE dzieli sie przez {PIXELS_PER_UNIT}. " +
                        "Kafelki beda sie rozjezdzac. Sprawdz, czy to na pewno tileset.");
                }
                else
                {
                    Debug.Log($"'{tex.name}': siatka {tex.width / PIXELS_PER_UNIT} x {tex.height / PIXELS_PER_UNIT} kafelkow. OK!");
                }
            }

            importer.SaveAndReimport();
            changed++;
        }

        Debug.Log($"Zaktualizowano {changed} tekstur. Pamietaj: tilesety trzeba jeszcze POCIAC (Sprite Editor -> Slice -> Grid By Cell Size 32x32).");
    }
}