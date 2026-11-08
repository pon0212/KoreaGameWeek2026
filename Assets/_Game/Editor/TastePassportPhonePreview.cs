using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TastePassport.EditorTools
{
    public static class TastePassportPhonePreview
    {
        private const string MainScene = "Assets/_Game/Scenes/TastePassport_MVP.unity";

        [MenuItem("Tools/Taste Passport/Start Phone Preview")]
        public static void StartPhonePreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.Log("Taste Passport phone preview is already running.");
                return;
            }

            if (EditorSceneManager.GetActiveScene().path != MainScene)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;

                EditorSceneManager.OpenScene(MainScene);
            }

            if (!EditorApplication.ExecuteMenuItem("Window/General/Device Simulator"))
            {
                Debug.LogError("Could not open Device Simulator. Open Window > General > Device Simulator in Unity.");
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.isPlaying = true;
            };
        }
    }
}
