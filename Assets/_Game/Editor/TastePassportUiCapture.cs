using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TastePassport.EditorTools
{
    public static class TastePassportUiCapture
    {
        // Run without -nographics or -quit; the play-mode driver exits the editor.
        public static void Run()
        {
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "UiPreview");
            Directory.CreateDirectory(folder);
            Environment.SetEnvironmentVariable("TP_UI_CAPTURE", folder);
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/TastePassport_MVP.unity");
            EditorApplication.isPlaying = true;
        }
    }
}
