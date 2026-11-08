using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TastePassport.EditorTools
{
    public static class TastePassportSmoke
    {
        [MenuItem("Tools/Taste Passport/Run Full Runtime Smoke")]
        // Also run with -executeMethod TastePassport.EditorTools.TastePassportSmoke.Run (without -quit).
        public static void Run()
        {
            string path = Path.Combine(Path.GetTempPath(), "taste_passport_smoke_" + Guid.NewGuid().ToString("N") + ".json");
            Environment.SetEnvironmentVariable("TP_SMOKE_PROFILE", path);
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/TastePassport_MVP.unity");
            EditorApplication.isPlaying = true;
        }
    }
}
