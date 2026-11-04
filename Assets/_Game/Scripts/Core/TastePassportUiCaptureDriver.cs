#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TastePassport.Gameplay;
using TastePassport.Save;
using TastePassport.Story;
using TastePassport.World;
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
        private static readonly string[] Pages =
        {
            "Menu", "Map", "Passport", "Dialogue", "Explore", "RecipeGame", "Postcard", "Settings", "Finale", "FinaleResult",
            "WorldGuide", "WorldHud", "WorldDialogue", "WorldJournal", "WorldPassport", "WorldHudKo", "WorldDialogueKo", "WorldReturn"
        };

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
            captureCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            captureCanvas = app.GetComponentInChildren<Canvas>();
            navigate = typeof(PassportApp).GetMethod("Navigate", BindingFlags.Instance | BindingFlags.NonPublic);
            pageType = navigate.GetParameters()[0].ParameterType;
            captureCanvas.worldCamera = captureCamera;
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
                if (index == Pages.Length * 2)
                {
                    Finish(0, "Taste Passport UI capture PASS: eighteen pages, including world HUD/dialogue/journal/passport/return, at 720x1280 and 1080x2400");
                    return;
                }
                int width = index < Pages.Length ? 720 : 1080;
                int height = index < Pages.Length ? 1280 : 2400;
                if (!prepared)
                {
                    target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                    target.Create();
                    captureCamera.targetTexture = target;
                    captureCanvas.worldCamera = captureCamera;
                    string page = Pages[index % Pages.Length];
                    if (page.StartsWith("World", StringComparison.Ordinal))
                    {
                        PrepareWorldCapture(page);
                    }
                    else
                    {
                        if (page == "Dialogue" || page == "Explore")
                            typeof(PassportApp).GetMethod("StartLevel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(app, new object[] { LevelCatalog.HoiAn });
                        if (page == "RecipeGame")
                        {
                            typeof(PassportApp).GetMethod("StartRecipe", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(app, new object[] { RecipeCatalog.CaoLau });
                            typeof(PassportApp).GetField("recipeDeadline", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(app, Time.time + 90f);
                        }
                        if (page == "Finale" || page == "FinaleResult")
                        {
                            var festivalProfile = CreateFestivalCaptureProfile(page == "FinaleResult");
                            typeof(PassportApp).GetField("profile", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(app, festivalProfile);
                        }
                        navigate.Invoke(app, new[] { Enum.Parse(pageType, page) });
                    }
                    VerifyCaptureSafeArea(page);
                    if (page == "Menu" || page == "Passport" || page == "Settings") LogPageCanvasState(page);
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

        private static PlayerProfile CreateFestivalCaptureProfile(bool completed)
        {
            var profile = new PlayerProfile { created = true, playerName = "Nhung", language = "vi", avatarIndex = 1, chapterIndex = 4 };
            string[] missions =
            {
                LevelCatalog.HoiAn.Id, RecipeCatalog.CaoLau.Id, MatchCatalog.HoiAn.Id,
                LevelCatalog.Jeonju.Id, RecipeCatalog.Bibimbap.Id, MatchCatalog.Jeonju.Id
            };
            foreach (string mission in missions) ProgressService.Record(profile, mission, 300, 3);
            StoryProgressService.AddImprint(profile, "lantern-display-ready");
            StoryProgressService.AddImprint(profile, "hanji-display-ready");
            StoryProgressService.RecordInteraction(profile, "festival-invitation-received", "festival-invitation");
            int stations = completed ? FestivalPreparationRun.StationCount : 2;
            for (int station = 0; station < stations; station++)
            {
                StoryProgressService.AddImprint(profile, FestivalPreparationRun.GetPlacementImprintId(station));
                string decoration = FestivalPreparationRun.GetDecorationId(station % FestivalPreparationRun.DecorationCount);
                StoryProgressService.AddImprint(profile, FestivalPreparationRun.GetDecorationImprintId(station, decoration));
            }
            int links = completed ? FestivalPreparationRun.LinkCount : 1;
            for (int i = 0; i < links; i++)
                StoryProgressService.AddImprint(profile, FestivalPreparationRun.GetConnectionImprintId(FestivalPreparationRun.GetLinkId(i)));
            if (completed) StoryProgressService.CompleteMainStory(profile);
            return profile;
        }

        private void LogPageCanvasState(string page)
        {
            var scroll = app.GetComponentInChildren<ScrollRect>(true);
            var viewport = scroll != null ? scroll.viewport : null;
            var content = scroll != null ? scroll.content : null;
            var canvasRect = captureCanvas.transform as RectTransform;
            var scaler = captureCanvas.GetComponent<CanvasScaler>();
            string firstText = "none";
            Vector3 firstTextViewport = Vector3.zero;
            bool firstTextCulled = false;
            float firstTextAlpha = 0f;
            foreach (Text label in app.GetComponentsInChildren<Text>(true))
            {
                if (!label.isActiveAndEnabled || string.IsNullOrWhiteSpace(label.text)) continue;
                firstText = label.text.Replace('\n', ' ');
                firstTextViewport = captureCamera.WorldToViewportPoint(label.transform.position);
                firstTextCulled = label.canvasRenderer.cull;
                firstTextAlpha = label.color.a * label.canvasRenderer.GetAlpha();
                break;
            }
            Debug.Log("UI capture layout " + page + ": mode=" + captureCanvas.renderMode +
                ", canvasDepth=" + Vector3.Dot(captureCanvas.transform.position - captureCamera.transform.position, captureCamera.transform.forward).ToString("F3") +
                ", cameraPixels=" + captureCamera.pixelWidth + "x" + captureCamera.pixelHeight +
                ", target=" + (captureCamera.targetTexture != null ? captureCamera.targetTexture.width + "x" + captureCamera.targetTexture.height : "none") +
                ", screen=" + Screen.width + "x" + Screen.height +
                ", canvasRect=" + (canvasRect != null ? canvasRect.rect.size.ToString() : "none") +
                ", canvasScale=" + (scaler != null ? scaler.scaleFactor.ToString("F3") : "none") +
                ", firstText='" + firstText + "' viewport=" + firstTextViewport + " culled=" + firstTextCulled + " alpha=" + firstTextAlpha.ToString("F3") +
                ", viewportSize=" + (viewport != null ? viewport.rect.size.ToString() : "none") +
                ", contentSize=" + (content != null ? content.rect.size.ToString() : "none") +
                ", scrollY=" + (scroll != null ? scroll.verticalNormalizedPosition.ToString("F3") : "none"));
        }

        private void VerifyCaptureSafeArea(string page)
        {
            if (captureCamera.targetTexture == null) return;
            var safeRoot = (RectTransform)typeof(PassportApp)
                .GetField("safeRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(app);
            if (safeRoot == null || !safeRoot.gameObject.activeInHierarchy) return;
            if ((safeRoot.anchorMin - Vector2.zero).sqrMagnitude > 0.0001f ||
                (safeRoot.anchorMax - Vector2.one).sqrMagnitude > 0.0001f)
                throw new InvalidOperationException("The " + page + " page does not fill the portrait render texture safe area: " +
                    safeRoot.anchorMin + ".." + safeRoot.anchorMax + " for " + captureCamera.targetTexture.width + "x" + captureCamera.targetTexture.height);
        }

        private void PrepareWorldCapture(string page)
        {
            if (page == "WorldGuide" || page == "WorldHudKo")
            {
                bool korean = page == "WorldHudKo";
                var profile = new PlayerProfile
                {
                    created = true,
                    playerName = "Bảo",
                    language = korean ? "ko" : "vi",
                    lastWorldId = korean ? "jeonju" : "hoian",
                    lastWorldX = korean ? WorldCatalog.Get("jeonju").SpawnPoint.x : WorldCatalog.Get("hoian").SpawnPoint.x,
                    lastWorldY = korean ? WorldCatalog.Get("jeonju").SpawnPoint.y : WorldCatalog.Get("hoian").SpawnPoint.y
                };
                if (korean) StoryProgressService.AddFlag(profile, "world-controls-guide-seen");
                if (korean) StoryProgressService.AddFlag(profile, "jeonju-unlocked");
                var store = (ProfileStore)typeof(PassportApp).GetField("store", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(app);
                if (!store.TrySave(profile)) throw new InvalidOperationException("Could not seed the world capture profile");
                typeof(PassportApp).GetField("profile", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(app, profile);
                string worldId = korean ? "jeonju" : "hoian";
                typeof(PassportApp).GetMethod("OpenWorld", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(app, new object[] { worldId });
            }

            var session = app.GetComponent<WalkableWorldSession>();
            if (session == null || !session.IsOpen)
                throw new InvalidOperationException("The walkable world did not open for capture page " + page);

            if (page == "WorldGuide" || page == "WorldHudKo")
            {
                var canvas = (Canvas)typeof(WalkableWorldSession)
                    .GetField("worldHudCanvas", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
                var runtimeRoot = (GameObject)typeof(WalkableWorldSession)
                    .GetField("runtimeRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
                float canvasDepth = Vector3.Dot(canvas.transform.position - captureCamera.transform.position, captureCamera.transform.forward);
                var nearMeshes = new List<string>();
                foreach (var renderer in runtimeRoot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    Bounds bounds = renderer.bounds;
                    float centerDepth = Vector3.Dot(bounds.center - captureCamera.transform.position, captureCamera.transform.forward);
                    Vector3 forward = captureCamera.transform.forward;
                    float radius = Mathf.Abs(forward.x) * bounds.extents.x + Mathf.Abs(forward.y) * bounds.extents.y + Mathf.Abs(forward.z) * bounds.extents.z;
                    float nearestDepth = centerDepth - radius;
                    if (nearestDepth <= canvasDepth + 0.08f && centerDepth + radius >= captureCamera.nearClipPlane)
                        nearMeshes.Add(renderer.name + "=" + nearestDepth.ToString("F3"));
                }
                Debug.Log("World capture depth " + page + ": camera=" + captureCamera.transform.position +
                    ", near=" + captureCamera.nearClipPlane.ToString("F3") +
                    ", canvas=" + canvasDepth.ToString("F3") +
                    ", meshes=" + string.Join("; ", nearMeshes));
            }

            if (page == "WorldHud")
            {
                var closeGuide = (UnityEngine.UI.Button)typeof(WalkableWorldSession)
                    .GetField("controlsGuideCloseButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
                closeGuide.onClick.Invoke();
            }
            else if (page == "WorldDialogue")
            {
                session.SetPlayerPosition(WorldCatalog.Get("hoian").Interactables[0].Position);
                session.InteractNearest();
            }
            else if (page == "WorldJournal")
            {
                session.DismissDialogue();
                typeof(WalkableWorldSession).GetMethod("ToggleQuestJournal", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, null);
            }
            else if (page == "WorldPassport")
            {
                session.DismissDialogue();
                var journal = (GameObject)typeof(WalkableWorldSession)
                    .GetField("questJournalPanel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
                if (journal.activeSelf)
                    typeof(WalkableWorldSession).GetMethod("ToggleQuestJournal", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, null);
                var hudCanvas = (Canvas)typeof(WalkableWorldSession)
                    .GetField("worldHudCanvas", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
                Button passportButton = null;
                foreach (Button candidate in hudCanvas.GetComponentsInChildren<Button>(true))
                    if (candidate.name == "World Passport") { passportButton = candidate; break; }
                if (passportButton == null) throw new InvalidOperationException("The world HUD passport button is missing");
                passportButton.onClick.Invoke();
                if (session.IsOpen) throw new InvalidOperationException("The world HUD passport button did not close the walkable world");
            }
            else if (page == "WorldDialogueKo")
            {
                session.SetPlayerPosition(WorldCatalog.Get("jeonju").Interactables[0].Position);
                session.InteractNearest();
            }
            else if (page == "WorldReturn")
            {
                session.DismissDialogue();
                var journal = (GameObject)typeof(WalkableWorldSession)
                    .GetField("questJournalPanel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
                if (journal.activeSelf)
                    typeof(WalkableWorldSession).GetMethod("ToggleQuestJournal", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, null);
                var hudCanvas = (Canvas)typeof(WalkableWorldSession)
                    .GetField("worldHudCanvas", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
                Button backButton = null;
                foreach (Button candidate in hudCanvas.GetComponentsInChildren<Button>(true))
                    if (candidate.name == "World Back") { backButton = candidate; break; }
                if (backButton == null) throw new InvalidOperationException("The world HUD back button is missing");
                backButton.onClick.Invoke();
                if (session.IsOpen) throw new InvalidOperationException("The world HUD back button did not close the walkable world");
            }
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
