#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using TastePassport.Gameplay;
using TastePassport.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TastePassport.Core
{
    public sealed class TastePassportUiCaptureDriver : MonoBehaviour
    {
        private string folder;
        private float started;
        private float nextFrame;
        private int index;
        private bool prepared;
        private bool profileInjected;
        private PassportApp app;
        private Camera captureCamera;
        private Canvas captureCanvas;
        private MethodInfo navigate;
        private Type pageType;
        private RenderTexture target;
        private static readonly string[] Pages = { "Menu", "Map", "Passport", "Dialogue", "Explore", "RecipeGame", "Postcard" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TP_UI_CAPTURE"))) return;
            var app = FindAnyObjectByType<PassportApp>();
            if (app != null) app.gameObject.AddComponent<TastePassportUiCaptureDriver>();
        }

        private void Start()
        {
            InitializeCapture();
        }

        private void OnEnable()
        {
            if (Application.isPlaying) InitializeCapture();
        }

        private void InitializeCapture()
        {
            folder = Environment.GetEnvironmentVariable("TP_UI_CAPTURE");
            started = Time.realtimeSinceStartup;
            nextFrame = started + .5f;
            index = 0;
            prepared = false;
            profileInjected = false;
            app = GetComponent<PassportApp>();
            captureCamera = FindAnyObjectByType<Camera>();
            captureCanvas = app.GetComponentInChildren<Canvas>();
            navigate = typeof(PassportApp).GetMethod("Navigate", BindingFlags.Instance | BindingFlags.NonPublic);
            pageType = navigate.GetParameters()[0].ParameterType;
            captureCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            captureCanvas.worldCamera = captureCamera;
            captureCanvas.planeDistance = 100f;
        }

        private void Update()
        {
            if (pageType == null) InitializeCapture();
            if (!profileInjected)
            {
                var profile = new PlayerProfile { created = true, playerName = "Nhung", language = "vi", avatarIndex = 1 };
                ProgressService.Record(profile, LevelCatalog.HoiAn.Id, 300, 3);
                ProgressService.Record(profile, LevelCatalog.Jeonju.Id, 260, 2);
                typeof(PassportApp).GetField("profile", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(app, profile);
                profileInjected = true;
            }
            if (Time.realtimeSinceStartup - started > 180f)
            {
                Finish(2, "Taste Passport UI capture timed out");
                return;
            }
            if (Time.realtimeSinceStartup < nextFrame) return;
            nextFrame = Time.realtimeSinceStartup + .35f;
            try
            {
                if (index == Pages.Length * 2) { Finish(0, "Taste Passport UI capture PASS: seven pages at 720x1280 and 1080x2400"); return; }
                int width = index < Pages.Length ? 720 : 1080;
                int height = index < Pages.Length ? 1280 : 2400;
                if (!prepared)
                {
                    target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                    target.Create();
                    captureCamera.targetTexture = target;
                    captureCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    captureCanvas.worldCamera = captureCamera;
                    string page = Pages[index % Pages.Length];
                    if (page == "Dialogue" || page == "Explore")
                        typeof(PassportApp).GetMethod("StartLevel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(app, new object[] { LevelCatalog.HoiAn });
                    if (page == "RecipeGame")
                    {
                        typeof(PassportApp).GetMethod("StartRecipe", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(app, new object[] { RecipeCatalog.CaoLau });
                        typeof(PassportApp).GetField("recipeDeadline", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(app, Time.time + 90f);
                    }
                    navigate.Invoke(app, new[] { Enum.Parse(pageType, page) });
                    prepared = true;
                    return;
                }
                foreach (var group in app.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1f;
                Canvas.ForceUpdateCanvases();
                var request = new RenderPipeline.StandardRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(captureCamera, request))
                    throw new InvalidOperationException("URP render request is unavailable");
                RenderPipeline.SubmitRenderRequest(captureCamera, request);
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
                screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshot.Apply();
                RenderTexture.active = previous;
                string path = Path.Combine(folder, Pages[index % Pages.Length] + "_" + width + "x" + height + ".png");
                File.WriteAllBytes(path, screenshot.EncodeToPNG());
                Destroy(screenshot);
                captureCamera.targetTexture = null;
                target.Release();
                Destroy(target);
                prepared = false;
                index++;
            }
            catch (Exception error) { Finish(2, "Taste Passport UI capture failed: " + error); }
        }

        private void Finish(int code, string message)
        {
            if (!enabled) return;
            enabled = false;
            if (code == 0) Debug.Log(message); else Debug.LogError(message);
            Environment.SetEnvironmentVariable("TP_UI_CAPTURE", null);
            EditorApplication.Exit(code);
        }
    }
}
#endif
