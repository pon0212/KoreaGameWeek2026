using UnityEditor;
using UnityEngine;

namespace TastePassport.EditorTools
{
    /// <summary>Applies mobile-friendly Sprite and compressed AudioClip import settings to the original generated MVP assets.</summary>
    public static class GeneratedAssetImportSetup
    {
        private const string HoiAnArt = "Assets/_Game/Resources/Art/LocationHoiAnCaoLau.png";
        private const string JeonjuArt = "Assets/_Game/Resources/Art/LocationJeonjuBibimbap.png";
        private const string Theme = "Assets/_Game/Resources/Audio/TastePassportTheme.wav";

        [MenuItem("Tools/Taste Passport/Prepare Original Travel Art and Music")]
        public static void Prepare()
        {
            PrepareSprite(HoiAnArt);
            PrepareSprite(JeonjuArt);
            PrepareAudio(Theme);
            AssetDatabase.SaveAssets();
            Debug.Log("Taste Passport original travel illustrations and background track are imported and ready.");
        }

        private static void PrepareSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("Generated travel illustration is missing or has the wrong importer: " + path);
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 85;
            importer.SaveAndReimport();
        }

        private static void PrepareAudio(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogError("Generated background track is missing or has the wrong importer: " + path);
                return;
            }
            importer.forceToMono = true;
            importer.loadInBackground = false;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .72f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
    }
}
