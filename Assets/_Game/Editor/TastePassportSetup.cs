using System.IO;
using TastePassport.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TastePassport.EditorTools
{
    public static class TastePassportSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/TastePassport_MVP.unity";
        private static string FullScenePath => Path.Combine(Application.dataPath, "_Game/Scenes/TastePassport_MVP.unity");

        [InitializeOnLoadMethod]
        private static void CreateInitialSceneWhenReady()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(FullScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode)
                    SetupMvp();
            };
        }

        [MenuItem("Tools/Taste Passport/Setup MVP")]
        public static void SetupMvp()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FullScenePath));
            Scene scene = File.Exists(FullScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var app = GameObject.Find("Taste Passport");
            if (app == null) app = new GameObject("Taste Passport");
            if (app.GetComponent<PassportApp>() == null) app.AddComponent<PassportApp>();

            var canvas = app.transform.Find("Passport Canvas");
            if (canvas == null)
            {
                var obj = new GameObject("Passport Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                obj.transform.SetParent(app.transform, false);
                canvas = obj.transform;
            }
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                events.AddComponent<StandaloneInputModule>();
#endif
            }

            if (Object.FindAnyObjectByType<Camera>() == null)
            {
                var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
                camera.tag = "MainCamera";
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.965f, 0.914f, 0.831f);
                camera.orthographic = true;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save " + ScenePath);

            var scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes;
            AssetDatabase.SaveAssets();
            Debug.Log("Taste Passport MVP scene ready: " + ScenePath);
        }

        [MenuItem("Tools/Taste Passport/Validate Phase 1")]
        public static void ValidatePhase1()
        {
            bool sceneExists = File.Exists(FullScenePath);
            bool buildReady = EditorBuildSettings.scenes.Length > 0 &&
                EditorBuildSettings.scenes[0].path == ScenePath && EditorBuildSettings.scenes[0].enabled;
            Scene scene = sceneExists ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single) : default;
            bool hasApp = false;
            if (sceneExists)
                foreach (var root in scene.GetRootGameObjects())
                    if (root.GetComponentInChildren<PassportApp>() != null) hasApp = true;
            Debug.Log($"Taste Passport Phase 1: scene={sceneExists}, build entry={buildReady}, bootstrap={hasApp}");
            if (!sceneExists || !buildReady || !hasApp) Debug.LogWarning("Run Tools > Taste Passport > Setup MVP, then validate again.");
        }
    }
}
