using System;
using System.Collections;
using TastePassport.Gameplay;
using TastePassport.Localization;
using TastePassport.Save;
using TastePassport.Story;
using TastePassport.UI;
using TastePassport.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TastePassport.Core
{
    public sealed partial class PassportApp : MonoBehaviour
    {
        private enum Page { IntroBook, Menu, Language, Create, Passport, Postcard, Map, Wardrobe, Settings, Dialogue, Explore, Assembly, Result, RecipeDialogue, RecipeGame, RecipeMix, RecipeFail, MatchDialogue, MatchGame, Finale, FinaleResult, LanternCraft, LanternResult, HanjiCraft, HanjiResult, Inventory, CultureCards, StoryCard, StoryTarget }
        private static readonly Color Cream = GameUiTheme.Cream;
        private static readonly Color Lantern = GameUiTheme.Lantern;
        private static readonly Color Jade = GameUiTheme.Primary;
        private static readonly Color Ink = GameUiTheme.TextPrimary;
        private static readonly Color Paper = GameUiTheme.Surface;
        private static readonly Color Vermilion = GameUiTheme.Vermilion;
        private static readonly Color Border = GameUiTheme.Border;
        private static readonly Color Muted = GameUiTheme.TextSecondary;
        private static readonly Color HeaderWood = GameUiTheme.HeaderWood;
        private static readonly Color GoldGlow = GameUiTheme.GoldGlow;
        private static readonly Color PaperShadow = GameUiTheme.PaperShadow;
        private readonly Color[] avatarColors = { Lantern, Jade, Vermilion };
        private static Sprite roundedSprite;
        private static Sprite circleSprite;

        private ProfileStore store;
        private PlayerProfile profile;
        private Canvas canvas;
        private GameUiDioramaStage dioramaStage;
        private GameUiActivityDiorama activityDiorama;
        private RectTransform safeRoot;
        private Font font;
        private Font displayFont;
        private UiFeedbackSound feedbackSound;
        private Page page;
        private Page settingsReturnPage;
        private Page worldReturnPage = Page.Map;
        private Page profileEditReturnPage = Page.Menu;
        private string draftName = "";
        private int draftAvatar;
        private int draftFaceStyle;
        private int draftHairStyle;
        private int draftBodyStyle;
        private int draftAccessoryStyle;
        private int draftOutfit;
        private bool editingProfile;
        private string notice;
        private Rect lastSafeArea;
        private Rect lastCameraViewport;
        private ExplorationRun run;
        private bool[] placedFragments = new bool[3];
        private int selectedFragment = -1;
        private FragmentDrag[] pieceViews = new FragmentDrag[3];
        private Transform[] slotViews = new Transform[3];
        private RecipeRun recipeRun;
        private float recipeDeadline;
        private Text recipeTimer;
        private bool recipeResult;
        private MatchRun matchRun;
        private bool matchResult;
        private int postcardIndex;
        private bool postcardBack;
        private bool helpOpen;
        private string interactionFeedbackKey;
        private Coroutine pageFade;
        private bool resetScroll = true;
        private bool koreanBackdrop;
        private bool selectedMapKorea;
        private bool resultCommitted;
        private FestivalPreparationRun festivalRun;
        private Coroutine finaleReveal;
        private WalkableWorldSession worldSession;
        private WorldInteractionResult storyCardInteraction;
        private WorldInteractableDefinition storyTarget;
        private string storyWorldId = "hoian";
        private bool storyAskMore;
        private LanternCraftRun lanternCraftRun;
        private string selectedLanternMaterialId;
        private string lanternResultItemId;
        private Text lanternMaterialStatus;
        private HanjiCraftRun hanjiCraftRun;
        private string hanjiResultItemId;
        private GameAudioManager audioManager;
        private RectTransform postcardCard;
        private Coroutine postcardFlip;
        private bool rebuildingPostcardForFlip;

        private string T(string key, string place = null) => GameText.Get(profile.language, key, profile.playerName, place);

        private bool FirstJourneyComplete() => IsFirstJourneyComplete(profile);

        public static bool IsFirstJourneyComplete(PlayerProfile target) =>
            ProgressService.HasStamp(target, LevelCatalog.HoiAn.Id) && ProgressService.HasStamp(target, LevelCatalog.Jeonju.Id) &&
            ProgressService.HasStamp(target, RecipeCatalog.CaoLau.Id) && ProgressService.HasStamp(target, RecipeCatalog.Bibimbap.Id) &&
            ProgressService.HasStamp(target, MatchCatalog.HoiAn.Id) && ProgressService.HasStamp(target, MatchCatalog.Jeonju.Id) &&
            StoryProgressService.HasImprint(target, "lantern-display-ready") && StoryProgressService.HasImprint(target, "hanji-display-ready");

        private void Awake()
        {
            InitializeApp();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (store == null || profile == null || canvas == null || safeRoot == null || font == null)
                InitializeApp();
            else Show();
        }

        private void InitializeApp()
        {
#if UNITY_ANDROID
            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;
#endif
#if UNITY_EDITOR
            string isolatedProfile = Environment.GetEnvironmentVariable("TP_SMOKE_PROFILE");
            store = new ProfileStore(string.IsNullOrEmpty(isolatedProfile) ? null : isolatedProfile);
#else
            store = new ProfileStore();
#endif
            profile = store.Load();
            MigrateWorldUnlockFromExistingProgress(profile);
            selectedMapKorea = koreanBackdrop = profile.lastWorldId == "jeonju";
            AudioListener.volume = PlayerPrefs.GetInt("tp_sound", 1) == 1 ? 1 : 0;
            feedbackSound = GetComponent<UiFeedbackSound>();
            if (feedbackSound == null) feedbackSound = gameObject.AddComponent<UiFeedbackSound>();
            StageAudioDirector.For(gameObject);
            audioManager = GameAudioManager.For(gameObject);
            audioManager.SetEnvironment(false);
            PlayStageAudio(1, false);
            EnsureFont();
            EnsureCanvas();
            EnsureEventSystem();
            introPageIndex = 0;
            introBookOpened = false;
            bookEntrancePending = true;
            page = Page.IntroBook;
            showHoiAnActivities = showJeonjuActivities = false;
            Show();
            ScheduleBookOpening();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) UpdateSafeArea();
        }

        private void PlayStageAudio(int stage, bool korean)
        {
            StageAudioDirector.For(gameObject).PlayStage(stage, korean);
            audioManager?.NotifyMusicTransition();
        }

        private void Update()
        {
            if (GetSafeAreaRect() != lastSafeArea) UpdateSafeArea();
            if (page == Page.RecipeGame && recipeRun != null && !recipeRun.Complete)
            {
                int remaining = Mathf.Max(0, Mathf.CeilToInt(recipeDeadline - Time.time));
                if (recipeTimer != null) recipeTimer.text = T("time_left") + ": " + remaining + "s";
                if (remaining == 0) Navigate(Page.RecipeFail);
            }
        }

        private void LateUpdate()
        {
            // Follow viewport/camera changes without reparenting the Canvas away from the app.
            if (canvas == null || safeRoot == null || !safeRoot.gameObject.activeSelf) return;
            var mainCamera = Camera.main;
            if (mainCamera != null && WorldSpaceCanvasFitter.Configure(canvas, mainCamera) &&
                (GetSafeAreaRect() != lastSafeArea || GetViewportRect() != lastCameraViewport))
                UpdateSafeArea();
        }

        private void EnsureCanvas()
        {
            canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                var obj = new GameObject("Passport Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                obj.transform.SetParent(transform, false);
                canvas = obj.GetComponent<Canvas>();
            }
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = GameUiTheme.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.dynamicPixelsPerUnit = 1f;
            if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();

            WorldSpaceCanvasFitter.Configure(canvas, Camera.main);
            dioramaStage = GameUiDioramaStage.Ensure(transform);

            safeRoot = canvas.transform.Find("Safe Area") as RectTransform;
            if (safeRoot == null) safeRoot = NewRect("Safe Area", canvas.transform);
            UpdateSafeArea();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var obj = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            obj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            obj.AddComponent<StandaloneInputModule>();
#endif
        }

        private void UpdateSafeArea()
        {
            if (safeRoot == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect area = GetSafeAreaRect();
            lastSafeArea = area;
            Rect viewport = GetViewportRect();
            if (viewport.width <= 0f || viewport.height <= 0f) return;
            lastCameraViewport = viewport;
            safeRoot.anchorMin = new Vector2(Mathf.Clamp01((area.xMin - viewport.xMin) / viewport.width),
                Mathf.Clamp01((area.yMin - viewport.yMin) / viewport.height));
            safeRoot.anchorMax = new Vector2(Mathf.Clamp01((area.xMax - viewport.xMin) / viewport.width),
                Mathf.Clamp01((area.yMax - viewport.yMin) / viewport.height));
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
        }

        private Rect GetSafeAreaRect()
        {
            var target = canvas != null && canvas.worldCamera != null ? canvas.worldCamera.targetTexture : null;
            // RenderTexture captures use their own portrait viewport, not the Editor window's safe area.
            return target != null ? new Rect(0, 0, target.width, target.height) : Screen.safeArea;
        }

        private Rect GetViewportRect()
        {
            var camera = canvas != null ? canvas.worldCamera : null;
            var target = camera != null ? camera.targetTexture : null;
            if (target != null) return new Rect(0, 0, target.width, target.height);
            return camera != null ? camera.pixelRect : new Rect(0, 0, Screen.width, Screen.height);
        }

        private void Navigate(Page target)
        {
            notice = null;
            helpOpen = false;
            StoryNarration.Stop();
            if (target == Page.Map) selectedMapKorea = koreanBackdrop;
            if (target == Page.Passport) audioManager?.PlayCue(GameSoundCue.BookOpen);
            page = target;
            resetScroll = true;
            Show();
        }

        private void Show()
        {
            bookLeaf = null;
            bookSurface = null;
            bookVisual = null;
            if (postcardFlip != null && !rebuildingPostcardForFlip)
            {
                StopCoroutine(postcardFlip);
                postcardFlip = null;
            }
            postcardCard = null;
            if (finaleReveal != null)
            {
                StopCoroutine(finaleReveal);
                finaleReveal = null;
            }
            EnsureFont();
            WorldSpaceCanvasFitter.Configure(canvas, Camera.main);
            UpdateSafeArea();
            bool introBookPage = page == Page.IntroBook;
            bool mapPage = page == Page.Map;
            if (!introBookPage && dioramaStage == null) dioramaStage = GameUiDioramaStage.Ensure(transform);
            dioramaStage?.SetDestination(koreanBackdrop);
            string activityDioramaId = ResolveActivityDioramaId();
            bool activityPage = !string.IsNullOrEmpty(activityDioramaId);
            if (activityPage)
            {
                if (activityDiorama == null) activityDiorama = GameUiActivityDiorama.Ensure(transform);
                activityDiorama?.SetActivity(activityDioramaId, koreanBackdrop);
            }
            bool pageVisible = safeRoot.gameObject.activeSelf;
            bool useActivityDiorama = activityPage && activityDiorama != null && activityDiorama.StageTexture != null;
            activityDiorama?.SetVisible(pageVisible && useActivityDiorama && !introBookPage);
            bool culturalArtworkPage = page == Page.Menu || page == Page.Language || page == Page.Create || mapPage || page == Page.Passport || page == Page.Postcard || page == Page.StoryCard;
            dioramaStage?.SetStageVisible(pageVisible && !useActivityDiorama && !introBookPage && !culturalArtworkPage);
            var previousScroll = safeRoot.Find("Page Scroll")?.GetComponent<ScrollRect>();
            float savedScroll = resetScroll || previousScroll == null ? 1f : previousScroll.verticalNormalizedPosition;
            for (int i = safeRoot.childCount - 1; i >= 0; i--)
            {
                // Destroy is deferred; disable old raycast targets immediately during rebuilds.
                var oldPage = safeRoot.GetChild(i).gameObject;
                oldPage.SetActive(false);
                Destroy(oldPage);
            }
            Panel("Destination Background", safeRoot, introBookPage ? Hex("#35271F") : Cream, true);
            if (culturalArtworkPage)
            {
                string artworkPath = selectedMapKorea ? "Art/LocationJeonjuBibimbap" : "Art/LocationHoiAnCaoLau";
                Sprite artwork = Resources.Load<Sprite>(artworkPath);
                if (artwork == null)
                    Debug.LogWarning("Taste Passport location background is missing: Resources/" + artworkPath + ".png");
                else
                {
                    var background = NewRect("Selected location background", safeRoot);
                    Stretch(background);
                    var picture = background.gameObject.AddComponent<Image>();
                    picture.sprite = artwork;
                    picture.type = Image.Type.Simple;
                    picture.preserveAspect = false;
                    picture.raycastTarget = false;
                    background.gameObject.AddComponent<LocationPaintingMotion>().Initialize(selectedMapKorea);
                    background.gameObject.AddComponent<LocationBackdropTransition>();
                }
            }
            Texture backdropTexture = introBookPage || culturalArtworkPage ? null : useActivityDiorama ? activityDiorama.StageTexture : dioramaStage?.StageTexture;
            if (backdropTexture != null)
            {
                var surface = NewRect("3D destination stage", safeRoot);
                Stretch(surface);
                var image = surface.gameObject.AddComponent<RawImage>();
                image.texture = backdropTexture;
                image.raycastTarget = false;
                // Fill the viewport without stretching the portrait stage on taller displays.
                float viewportAspect = safeRoot.rect.width / Mathf.Max(1f, safeRoot.rect.height);
                float textureAspect = (float)backdropTexture.width / backdropTexture.height;
                float uvWidth = Mathf.Min(1f, viewportAspect / textureAspect);
                float uvHeight = Mathf.Min(1f, textureAspect / viewportAspect);
                image.uvRect = new Rect((1f - uvWidth) * .5f, (1f - uvHeight) * .5f, uvWidth, uvHeight);
            }
            if (!introBookPage)
                Panel("Paper wash", safeRoot, new Color(Cream.r, Cream.g, Cream.b, mapPage ? .10f : .22f), true);
            if (page == Page.Menu || page == Page.Map || page == Page.Passport || page == Page.Postcard)
            {
                var petals = NewRect("Floating cherry blossoms", safeRoot);
                Stretch(petals);
                petals.gameObject.AddComponent<PetalDriftEffect>().Initialize(CircleSprite());
            }
            var scrollObject = new GameObject("Page Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(safeRoot, false);
            var fadeGroup = scrollObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            var scrollRect = scrollObject.GetComponent<RectTransform>();
            Stretch(scrollRect);
            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = !introBookPage;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = NewRect("Viewport", scrollRect);
            Stretch(viewport);
            var viewportGraphic = viewport.gameObject.AddComponent<Image>();
            viewportGraphic.color = new Color(1f, 1f, 1f, 0f);
            viewportGraphic.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = introBookPage ? new RectOffset(36, 36, 12, 12) :
                new RectOffset(36, 36, HasContextHelp() ? 108 : 32, 64);
            layout.spacing = introBookPage ? 4 : 18;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            switch (page)
            {
                case Page.IntroBook: DrawIntroBook(content); break;
                case Page.Menu: DrawMenu(content); break;
                case Page.Language: DrawLanguage(content); break;
                case Page.Create: DrawCreate(content); break;
                case Page.Passport: DrawPassport(content); break;
                case Page.Postcard: DrawPostcard(content); break;
                case Page.Map: DrawMap(content); break;
                case Page.StoryTarget: DrawStoryTarget(content); break;
                case Page.StoryCard: DrawStoryCard(content); break;
                case Page.Wardrobe: DrawWardrobe(content); break;
                case Page.Settings: DrawSettings(content); break;
                case Page.Dialogue: DrawDialogue(content); break;
                case Page.Explore: DrawExplore(content); break;
                case Page.Assembly: DrawAssembly(content); break;
                case Page.Result: DrawResult(content); break;
                case Page.RecipeDialogue: DrawRecipeDialogue(content); break;
                case Page.RecipeGame: DrawRecipeGame(content); break;
                case Page.RecipeMix: DrawRecipeMix(content); break;
                case Page.RecipeFail: DrawRecipeFail(content); break;
                case Page.MatchDialogue: DrawMatchDialogue(content); break;
                case Page.MatchGame: DrawMatchGame(content); break;
                case Page.Finale: DrawFinale(content); break;
                case Page.FinaleResult: DrawFinaleResult(content); break;
                case Page.LanternCraft: DrawLanternCraft(content); break;
                case Page.LanternResult: DrawLanternResult(content); break;
                case Page.HanjiCraft: DrawHanjiCraft(content); break;
                case Page.HanjiResult: DrawHanjiResult(content); break;
                case Page.Inventory: DrawInventory(content); break;
                case Page.CultureCards: DrawCultureCards(content); break;
            }
            if (HasContextHelp())
            {
                BuildContextHelpButton();
                if (helpOpen) DrawContextHelpOverlay();
            }
            if (!string.IsNullOrEmpty(notice))
            {
                Toast(notice);
                notice = null;
            }
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = savedScroll;
            resetScroll = false;
            if (pageFade != null) StopCoroutine(pageFade);
            if (page == Page.IntroBook && bookTurning)
            {
                fadeGroup.alpha = 1f;
                pageFade = null;
            }
            else pageFade = StartCoroutine(FadePage(fadeGroup));
        }

        private bool HasContextHelp() => page != Page.IntroBook && page != Page.FinaleResult && page != Page.LanternResult && page != Page.HanjiResult;

        private string ContextHelpKey()
        {
            switch (page)
            {
                case Page.Map: return "map";
                case Page.Create:
                case Page.Wardrobe: return "character";
                case Page.Passport:
                case Page.Postcard:
                case Page.Inventory:
                case Page.CultureCards: return "passport";
                case Page.RecipeDialogue:
                case Page.RecipeGame:
                case Page.RecipeFail: return "recipe";
                case Page.RecipeMix: return "mix";
                case Page.MatchDialogue:
                case Page.MatchGame: return "match";
                case Page.Dialogue:
                    return "dialogue";
                case Page.Explore: return "explore";
                case Page.Assembly: return "assembly";
                case Page.Result: return "result";
                case Page.Settings:
                case Page.Language: return "settings";
                default: return "general";
            }
        }

        private void BuildContextHelpButton()
        {
            var control = Button(safeRoot, "?", Paper, () => { helpOpen = true; Show(); }, Jade);
            var rect = control.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(52f, 52f);
            rect.anchoredPosition = new Vector2(-20f, -20f);
            var image = control.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = CircleSprite();
                image.type = Image.Type.Simple;
                image.color = Paper;
            }
            var border = control.GetComponent<Outline>();
            if (border != null)
            {
                border.effectColor = GoldGlow;
                border.effectDistance = new Vector2(2f, -2f);
            }
            var shadow = control.GetComponent<Shadow>();
            if (shadow != null)
            {
                shadow.effectColor = GameUiTheme.PaperShadow;
                shadow.effectDistance = new Vector2(0f, -3f);
            }
            var label = control.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                label.fontSize = 28;
                label.color = Jade;
                label.fontStyle = FontStyle.Bold;
            }
        }

        private void DrawContextHelpOverlay()
        {
            var dimImage = Panel("Context help backdrop", safeRoot, new Color(.12f, .10f, .08f, .64f), true);
            dimImage.raycastTarget = true;
            var dim = dimImage.rectTransform;
            Stretch(dim);
            var dismiss = dim.gameObject.AddComponent<UnityEngine.UI.Button>();
            dismiss.transition = Selectable.Transition.None;
            dismiss.onClick.AddListener(() => { helpOpen = false; Show(); });

            var sheet = Card(safeRoot, 540f);
            sheet.name = "Context help sheet";
            sheet.GetComponent<Image>().raycastTarget = true;
            sheet.anchorMin = sheet.anchorMax = new Vector2(.5f, .5f);
            sheet.pivot = new Vector2(.5f, .5f);
            sheet.sizeDelta = new Vector2(628f, 540f);
            sheet.SetAsLastSibling();
            string topic = ContextHelpKey();
            CollectionCopy(sheet, T("help_" + topic + "_title"), 35, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Rule(sheet);
            CollectionCopy(sheet, T("help_" + topic + "_body"), 28, Ink, TextAnchor.MiddleLeft);
            Button(sheet, T("help_close"), Jade, () => { helpOpen = false; Show(); });
        }

        private string ResolveActivityDioramaId()
        {
            switch (page)
            {
                case Page.Passport:
                case Page.Inventory:
                case Page.CultureCards:
                    return "passport";
                case Page.Postcard:
                    return "postcard";
                case Page.Dialogue:
                case Page.Explore:
                    return "architecture";
                case Page.Assembly:
                    return "architecture-assembly";
                case Page.Result:
                    if (matchResult && matchRun != null) return "matching-result";
                    if (recipeResult && recipeRun != null) return "recipe-result";
                    return "architecture-result";
                case Page.RecipeDialogue:
                case Page.RecipeGame:
                    return "recipe";
                case Page.RecipeMix:
                    return "recipe-mix";
                case Page.RecipeFail:
                    return "recipe-fail";
                case Page.MatchDialogue:
                case Page.MatchGame:
                    return "matching";
                case Page.LanternCraft:
                    return "lantern-craft";
                case Page.LanternResult:
                    return "lantern-result";
                case Page.HanjiCraft:
                    return "hanji-craft";
                case Page.HanjiResult:
                    return "hanji-result";
                case Page.Finale:
                    return "festival";
                case Page.FinaleResult:
                    return "festival-result";
                default:
                    return null;
            }
        }

        private IEnumerator FadePage(CanvasGroup group)
        {
            float elapsed = 0f;
            while (group != null && elapsed < .18f)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(elapsed / .18f);
                yield return null;
            }
            if (group != null) group.alpha = 1f;
            pageFade = null;
        }

        private void Header(Transform parent, string subtitle)
        {
            var plate = Card(parent, 270);
            CountryFlags(plate);
            Label(plate, T("title"), 44, 72, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Label(plate, subtitle, 30, 66, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Rule(plate);
        }

        private void CountryFlags(Transform parent)
        {
            var vietnam = Resources.Load<Sprite>("Art/FlagVietnam");
            var korea = Resources.Load<Sprite>("Art/FlagKorea");
            if (vietnam == null || korea == null)
            {
                Label(parent, T("brand_kicker"), 26, 70, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
                return;
            }

            var row = NewRect("Vietnam and Korea flags", parent);
            SetHeight(row, 70);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            AddCountryFlag(row, "Vietnam flag", vietnam);
            var route = NewRect("Passport route", row);
            var routeImage = route.gameObject.AddComponent<Image>();
            routeImage.color = Vermilion;
            routeImage.raycastTarget = false;
            var routeSize = route.gameObject.AddComponent<LayoutElement>();
            routeSize.preferredWidth = 44;
            routeSize.preferredHeight = 3;
            AddCountryFlag(row, "Korea flag", korea);
        }

        private static void AddCountryFlag(Transform parent, string name, Sprite sprite)
        {
            var flag = NewRect(name, parent);
            var size = flag.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = 100;
            size.preferredHeight = 66;
            var image = flag.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var border = flag.gameObject.AddComponent<Outline>();
            border.effectColor = Border;
            border.effectDistance = new Vector2(2, -2);
        }

        private void DestinationFlag(Transform parent, string spritePath, string fallbackKey)
        {
            var sprite = Resources.Load<Sprite>(spritePath);
            if (sprite == null)
            {
                Label(parent, T(fallbackKey), 26, 40, Muted, TextAnchor.MiddleCenter);
                return;
            }

            var row = NewRect("Destination country flag", parent);
            SetHeight(row, 40);
            var flag = NewRect("Country flag", row);
            flag.anchorMin = flag.anchorMax = new Vector2(.5f, .5f);
            flag.sizeDelta = new Vector2(54, 36);
            flag.anchoredPosition = Vector2.zero;
            var image = flag.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var border = flag.gameObject.AddComponent<Outline>();
            border.effectColor = Border;
            border.effectDistance = new Vector2(1, -1);
        }

        private void DrawMenu(Transform parent)
        {
            Spacer(parent, GameUiTheme.SpaceMedium);

            // Cultural Grand Header Plaque (Gỗ trầm, viền chỉ vàng)
            var plaque = Card(parent, 250f);
            plaque.name = "Menu cultural plaque";
            var plaqueImage = plaque.GetComponent<Image>();
            if (plaqueImage != null) plaqueImage.color = GameUiTheme.HeaderWood;
            var plaqueBorder = plaque.GetComponent<Outline>();
            if (plaqueBorder != null)
            {
                plaqueBorder.effectColor = GameUiTheme.GoldGlow;
                plaqueBorder.effectDistance = new Vector2(2, -2);
            }
            CountryFlags(plaque);
            CollectionCopy(plaque, T("title"), 44, GameUiTheme.GoldGlow, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            CollectionCopy(plaque, T("subtitle"), 24, GameUiTheme.Surface, TextAnchor.MiddleCenter, FontStyle.Bold);
            Rule(plaque);
            CollectionCopy(plaque, T("tagline"), 22, GameUiTheme.SoftSage, TextAnchor.MiddleCenter, FontStyle.Italic);

            Spacer(parent, GameUiTheme.SpaceMedium);

            // Primary Hero Journey Surface
            var welcome = JourneyCard(parent);
            welcome.name = "Journey start surface";
            CollectionCopy(welcome, T(profile.created ? "journey_welcome_back" : "journey_first_run"), 30, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (profile.created)
            {
                string regionSubtitle = profile.language == "ko" ? "호이안 & 전주 · 문화 탐험 여행" : "Hội An & Jeonju · Hành trình văn hóa";
                CollectionCopy(welcome, regionSubtitle, 22, Jade, TextAnchor.MiddleCenter, FontStyle.Normal);
            }
            Button(welcome, T(profile.created ? "continue" : "start"), Jade, () =>
            {
                if (!profile.created)
                {
                    Navigate(Page.Language);
                    return;
                }
                Navigate(Page.Map);
            });

            // Secondary Navigation Bar
            var row = NewRect("Menu secondary navigation", parent);
            SetHeight(row, 84);
            AddHorizontal(row);
            if (profile.created)
                CompactButton(row, T("nav_passport"), Paper, () => Navigate(Page.Passport), Ink);
            CompactButton(row, T("settings"), Paper, () => { settingsReturnPage = Page.Menu; Navigate(Page.Settings); }, Ink);
            CompactButton(row, T("language"), Paper, () => Navigate(Page.Language), Ink);
            CompactButton(parent, T("intro_reopen"), Paper, OpenIntroBook, Ink);
        }

        private void DrawLanguage(Transform parent)
        {
            Header(parent, T("choose_language"));
            Label(parent, T(profile.created ? "journey_language_hint" : "journey_language_step"), 26, 92, Muted, TextAnchor.MiddleCenter);
            var languages = JourneyCard(parent);
            Button(languages, T("language_vi") + (profile.language == "vi" ? " · " + T("selected_state") : ""), profile.language == "vi" ? Lantern : Paper, () => ChangeLanguage("vi"), Ink);
            Button(languages, T("language_ko") + (profile.language == "ko" ? " · " + T("selected_state") : ""), profile.language == "ko" ? Lantern : Paper, () => ChangeLanguage("ko"), Ink);
            Button(parent, T(profile.created ? "open_map" : "journey_to_profile"), Jade, () => Navigate(profile.created ? Page.Map : Page.Create));
            CompactButton(parent, T("back"), Paper, () => Navigate(Page.Menu), Ink);
        }

        private void DrawCreate(Transform parent)
        {
            CollectionCopy(parent, T(editingProfile ? "edit_profile" : "create"), 38, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(parent, T(editingProfile ? "journey_profile_hint" : "journey_profile_step"), 26, Muted, TextAnchor.MiddleCenter);

            // Full-body traveler live preview showcase
            var showcaseCard = JourneyCard(parent);
            showcaseCard.name = "Full-body traveler showcase";
            Label(showcaseCard, profile != null && profile.language == "ko" ? "전신 미리보기 · 여행자 모습" : "Xem trước toàn thân · Nhân vật du ký", 26, 44, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            int previewOutfitId = draftOutfit < 0 ? (profile == null ? 0 : profile.outfitId) : draftOutfit;
            string outfitName = previewOutfitId == 1 ? (profile != null && profile.language == "ko" ? "베트남 전통 아오자이" : "Áo dài Việt Nam") :
                previewOutfitId == 2 ? (profile != null && profile.language == "ko" ? "한국 전통 한복" : "Hanbok Hàn Quốc") :
                (profile != null && profile.language == "ko" ? "현대 여행자 복장" : "Trang phục lữ hành");
            Label(showcaseCard, outfitName, 22, 34, Vermilion, TextAnchor.MiddleCenter, FontStyle.Italic);
            var showcaseStage = NewRect("Showcase stage", showcaseCard);
            SetHeight(showcaseStage, 330);
            DrawFullBodyTraveler(showcaseStage, draftAvatar, previewOutfitId, draftFaceStyle, draftHairStyle, draftBodyStyle, draftAccessoryStyle);

            var profileStep = JourneyCard(parent);
            profileStep.name = "Profile identity step";
            Label(profileStep, T("name_label"), 29, 50, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var field = Input(profileStep, draftName, T("name_hint"));
            field.onValueChanged.AddListener(value => draftName = value);
            Label(profileStep, T("avatar_label"), 29, 58, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var row = NewRect("Avatar choices", profileStep);
            SetHeight(row, 196);
            var horizontal = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 12;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = false;
            for (int i = 0; i < 3; i++)
            {
                int chosen = i;
                var avatar = Button(row, T("avatar_" + i) + (draftAvatar == i ? " · " + T("selected_state") : ""), draftAvatar == i ? Lantern : Paper, () => { draftAvatar = chosen; Show(); }, Ink);
                SetHeight(avatar.GetComponent<RectTransform>(), 188);
                var caption = avatar.transform.Find("Text") as RectTransform;
                if (caption != null)
                {
                    caption.anchorMin = new Vector2(0f, 0f);
                    caption.anchorMax = new Vector2(1f, 0.26f);
                    caption.pivot = new Vector2(0.5f, 0.5f);
                    caption.offsetMin = new Vector2(4f, 2f);
                    caption.offsetMax = new Vector2(-4f, -2f);
                    var captionText = caption.GetComponent<Text>();
                    if (captionText != null)
                    {
                        captionText.fontSize = 18;
                        captionText.alignment = TextAnchor.MiddleCenter;
                    }
                }
                int previewOutfit = draftOutfit < 0 ? profile.outfitId : draftOutfit;
                DrawAvatar(avatar.transform, i, previewOutfit, draftFaceStyle, draftHairStyle, draftBodyStyle, draftAccessoryStyle);
            }
            AppearanceChoices(profileStep, "appearance_face", new[] { "face_0", "face_1", "face_2" }, draftFaceStyle,
                selected => { draftFaceStyle = selected; Show(); });
            AppearanceChoices(profileStep, "appearance_hair", new[] { "hair_0", "hair_1", "hair_2" }, draftHairStyle,
                selected => { draftHairStyle = selected; Show(); });
            AppearanceChoices(profileStep, "appearance_silhouette", new[] { "silhouette_0", "silhouette_1", "silhouette_2" }, draftBodyStyle,
                selected => { draftBodyStyle = selected; Show(); });
            AppearanceChoices(profileStep, "appearance_clothing", new[] { "outfit_traveler", "outfit_ao_dai", "outfit_hanbok" }, draftOutfit,
                selected => { draftOutfit = selected; Show(); });
            if (draftOutfit < 0)
                CollectionCopy(profileStep, T("current_outfit") + ": " + T(OutfitCatalog.Get(profile.outfitId).NameKey), 24, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            AppearanceChoices(profileStep, "appearance_accessory", new[] { "accessory_0", "accessory_1", "accessory_2" }, draftAccessoryStyle,
                selected => { draftAccessoryStyle = selected; Show(); });
            CollectionCopy(profileStep, T("outfit_review_note"), 22, Muted, TextAnchor.MiddleCenter);
            Spacer(parent, GameUiTheme.SpaceSmall);
            Button(parent, T(editingProfile ? "save_profile" : "journey_enter_hoian"), Jade, CreateProfile);
            CompactButton(parent, T("back"), Paper, () =>
            {
                bool wasEditing = editingProfile;
                editingProfile = false;
                Navigate(wasEditing ? profileEditReturnPage : Page.Language);
            }, Ink);
        }

        // Let each card's layout report its preferred height, including wrapped
        // helper copy and nested controls, to the page's vertical layout.
        private RectTransform JourneyCard(Transform parent)
        {
            var card = Card(parent, 0);
            var size = card.GetComponent<LayoutElement>();
            size.minHeight = -1;
            size.preferredHeight = -1;
            return card;
        }

        private void JourneyNavigation(Transform parent, Page returnPage)
        {
            var row = NewRect("Journey navigation", parent);
            SetHeight(row, 84);
            AddHorizontal(row);
            if (profile.created)
            {
                CompactButton(row, T("nav_profile"), Paper, () => OpenCharacterEditor(returnPage), Ink);
                CompactButton(row, T("nav_passport"), Paper, () => Navigate(Page.Passport), Ink);
            }
            CompactButton(row, T("settings"), Paper, () => { settingsReturnPage = returnPage; Navigate(Page.Settings); }, Ink);
        }

        private void OpenCharacterEditor(Page returnPage)
        {
            editingProfile = true;
            profileEditReturnPage = returnPage;
            draftName = profile.playerName;
            draftAvatar = profile.avatarIndex;
            draftFaceStyle = profile.faceStyle;
            draftHairStyle = profile.hairStyle;
            draftBodyStyle = profile.bodyStyle;
            draftAccessoryStyle = profile.accessoryStyle;
            draftOutfit = profile.outfitId <= 2 ? profile.outfitId : -1;
            Navigate(Page.Create);
        }

        private void CreateProfile()
        {
            string clean = (draftName ?? "").Trim();
            if (clean.Length < 1 || clean.Length > 20) { notice = T("name_error"); Show(); return; }
            var candidate = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
            candidate.playerName = clean;
            candidate.avatarIndex = draftAvatar;
            candidate.faceStyle = draftFaceStyle;
            candidate.hairStyle = draftHairStyle;
            candidate.bodyStyle = draftBodyStyle;
            candidate.accessoryStyle = draftAccessoryStyle;
            candidate.outfitId = draftOutfit < 0 ? profile.outfitId : draftOutfit;
            if (candidate.ownedOutfits == null) candidate.ownedOutfits = new System.Collections.Generic.List<int>();
            if (!candidate.ownedOutfits.Contains(draftOutfit)) candidate.ownedOutfits.Add(draftOutfit);
            candidate.created = true;
            if (!store.TrySave(candidate)) { notice = T("save_error"); Show(); return; }
            profile = candidate;
            if (editingProfile)
            {
                editingProfile = false;
                Navigate(profileEditReturnPage);
                return;
            }
            Navigate(Page.Map);
        }

        private void AppearanceChoices(Transform parent, string titleKey, string[] optionKeys, int selected, Action<int> choose)
        {
            var section = NewRect("Appearance selector " + titleKey, parent);
            var sectionLayout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            sectionLayout.padding = new RectOffset(0, 0, 4, 4);
            sectionLayout.spacing = 6;
            sectionLayout.childAlignment = TextAnchor.UpperLeft;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            SetHeight(section, 116f);

            var title = Label(section, T(titleKey), 25, 30f, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetHeight(title.rectTransform, 30f);

            var row = NewRect("Appearance options " + titleKey, section);
            SetHeight(row, 66f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < optionKeys.Length; i++)
            {
                int option = i;
                bool isSelected = selected == i;
                var button = Button(row, T(optionKeys[i]) + (isSelected ? " · " + T("selected_state") : ""),
                    isSelected ? Lantern : Paper, () => choose(option), Ink);
                SetHeight(button.GetComponent<RectTransform>(), 62f);
                var btnTxt = button.GetComponentInChildren<Text>();
                if (btnTxt != null)
                {
                    btnTxt.fontSize = 20;
                    btnTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
                    btnTxt.rectTransform.offsetMin = new Vector2(4f, 0f);
                    btnTxt.rectTransform.offsetMax = new Vector2(-4f, 0f);
                }
            }
        }

        private void CollectionHeader(Transform parent, string titleKey, string hintKey, string status = null)
        {
            Header(parent, T(titleKey));
            var summary = JourneyCard(parent);
            if (!string.IsNullOrEmpty(status))
                CollectionCopy(summary, status, 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(summary, T(hintKey), 26, Muted, TextAnchor.MiddleCenter);
        }

        // Let translated copy grow with its content rather than clipping at a fixed height.
        private Text CollectionCopy(Transform parent, string value, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal, bool classic = false)
        {
            var label = Label(parent, value, size, 0, color, alignment, style, classic);
            var layout = label.GetComponent<LayoutElement>();
            layout.minHeight = -1;
            layout.preferredHeight = -1;
            return label;
        }

        private void DrawPassport(Transform parent)
        {
            RecordTutorialPassportView();
            var title = JourneyCard(parent);
            CollectionCopy(title, T("passport"), 38, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            // Cultural Category Quick-Nav Ribbon
            var navRow = NewRect("Passport cultural tabs", parent);
            SetHeight(navRow, 56);
            AddHorizontal(navRow);
            CompactButton(navRow, "👤 " + (profile != null && profile.language == "ko" ? "여행자" : "Lữ khách"), Paper, () => {}, Ink);
            CompactButton(navRow, "🔖 " + T("stamps"), Paper, () => {}, Ink);
            CompactButton(navRow, "✉️ " + (profile != null && profile.language == "ko" ? "엽서" : "Bưu thiếp"), Paper, () => {}, Ink);
            CompactButton(navRow, "🎒 " + (profile != null && profile.language == "ko" ? "소지품" : "Kho đồ"), Paper, () => Navigate(Page.Inventory), Ink);

            Spacer(parent, 20);
            var book = JourneyCard(parent);
            book.name = "Open passport book";
            book.GetComponent<Image>().color = Jade;
            var bookBorder = book.gameObject.AddComponent<Outline>();
            bookBorder.effectColor = GameUiTheme.GoldGlow;
            bookBorder.effectDistance = new Vector2(2, -2);
            book.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(14, 14, 14, 14);
            var spread = NewRect("Open book spread", book);
            SetHeight(spread, 330f);
            var identity = JourneyCard(spread);
            identity.name = "Passport left leaf";
            identity.anchorMin = Vector2.zero;
            identity.anchorMax = new Vector2(.5f, 1f);
            identity.offsetMin = Vector2.zero;
            identity.offsetMax = new Vector2(-5f, 0f);
            var avatarFrame = Panel("Traveler Avatar Frame", identity, Paper).rectTransform;
            SetHeight(avatarFrame, 130f);
            var avatarOutline = avatarFrame.gameObject.AddComponent<Outline>();
            avatarOutline.effectColor = GoldGlow;
            avatarOutline.effectDistance = new Vector2(1.5f, -1.5f);
            DrawAvatar(avatarFrame, profile.avatarIndex, profile.outfitId, profile.faceStyle, profile.hairStyle, profile.bodyStyle, profile.accessoryStyle);
            CollectionCopy(identity, profile.playerName, 28, HeaderWood, TextAnchor.MiddleCenter, FontStyle.Bold);
            string outfitName = profile.outfitId == 1 ? (profile.language == "ko" ? "Áo dài" : "Áo dài Việt Nam") :
                profile.outfitId == 2 ? (profile.language == "ko" ? "Hanbok" : "Hanbok Hàn Quốc") :
                (profile.language == "ko" ? "여행자 복장" : "Trang phục lữ hành");
            CollectionCopy(identity, "★ " + outfitName, 22, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);

            // Traveler Passport Account Actions
            var passAccRow = NewRect("Passport account action row", identity);
            SetHeight(passAccRow, 44);
            AddHorizontal(passAccRow);
            CompactButton(passAccRow, "✏️ " + T("edit_profile"), Paper, () => OpenCharacterEditor(Page.Passport), Ink);
            CompactButton(passAccRow, "🚪 " + (profile.language == "ko" ? "로그아웃" : "Đăng xuất"), new Color(0.95f, 0.90f, 0.88f), () =>
            {
                if (profile != null && !string.IsNullOrEmpty(profile.playerName)) store.TrySave(profile);
                string currentLang = profile != null ? profile.language : "vi";
                profile = new PlayerProfile { language = currentLang, created = false };
                draftName = "";
                editingProfile = false;
                Navigate(Page.Create);
                Toast(currentLang == "ko" ? "로그아웃되었습니다." : "Đã đăng xuất.");
            }, Lantern);
            var greeting = JourneyCard(spread);
            greeting.name = "Passport right leaf";
            greeting.anchorMin = new Vector2(.5f, 0f);
            greeting.anchorMax = Vector2.one;
            greeting.offsetMin = new Vector2(5f, 0f);
            greeting.offsetMax = Vector2.zero;
            CollectionCopy(greeting, "◆ " + T("passport_intro") + " ◆", 24, HeaderWood, TextAnchor.MiddleCenter, FontStyle.Bold);
            Rule(greeting);
            CollectionCopy(greeting, "★ " + (profile.language == "ko" ? "총 별점: " : "Tổng số sao: ") + profile.GetTotalStars() + " / 24", 24, Lantern, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(greeting, "🪙 " + T("coins") + ": " + profile.coins, 24, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            int vocabLearned = profile.learnedVocabulary == null ? 0 : profile.learnedVocabulary.Count;
            CollectionCopy(greeting, "📖 " + (profile.language == "ko" ? "학습한 어휘: " : "Từ vựng đã học: ") + vocabLearned + " / " + TastePassport.Learning.VocabularyCatalog.AllWords.Length, 22, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            var spine = Panel("Book binding", spread, Border).rectTransform;
            spine.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            spine.anchorMin = new Vector2(.5f, 0f);
            spine.anchorMax = new Vector2(.5f, 1f);
            spine.sizeDelta = new Vector2(6f, 0f);
            spine.anchoredPosition = Vector2.zero;
            Button(parent, T("open_map"), Jade, () => Navigate(Page.Map));
            var stamps = JourneyCard(parent);
            CollectionCopy(stamps, T("stamps"), 30, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            StampRow(stamps, LevelCatalog.HoiAn.Id, "hoian", LevelCatalog.Jeonju.Id, "jeonju");
            StampRow(stamps, RecipeCatalog.CaoLau.Id, "cao_lau", RecipeCatalog.Bibimbap.Id, "bibimbap");
            StampRow(stamps, MatchCatalog.HoiAn.Id, "hoian_exchange", MatchCatalog.Jeonju.Id, "jeonju_exchange");
            DrawWorldImprints(parent);
            if (profile.mainStoryCompleted)
                CollectionCopy(parent, T("main_story_completed"), 28, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (FirstJourneyComplete())
                CollectionCopy(parent, T("achievement_explorer"), 27, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            var collections = JourneyCard(parent);
            CollectionCopy(collections, T("collection_heading"), 30, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            CompactButton(collections, T("inventory") + " · " + (profile.inventoryItems == null ? 0 : profile.inventoryItems.Count), Paper, () => Navigate(Page.Inventory), Ink);
            CompactButton(collections, T("culture_cards") + " · " + (profile.culturalCards == null ? 0 : profile.culturalCards.Count), Paper, () => Navigate(Page.CultureCards), Ink);
            CompactButton(collections, T("wardrobe"), Paper, () => Navigate(Page.Wardrobe), Ink);

            // Cultural Vocabulary Notebook in Passport
            var vocabCard = JourneyCard(parent);
            vocabCard.name = "Passport vocabulary notebook";
            var vocabBorder = vocabCard.gameObject.AddComponent<Outline>();
            vocabBorder.effectColor = Border;
            vocabBorder.effectDistance = new Vector2(1.5f, -1.5f);
            CollectionCopy(vocabCard, "📖 " + (profile != null && profile.language == "ko" ? "한-베 문화 어휘 수첩" : "Sổ tay từ vựng văn hóa Việt - Hàn"), 28, HeaderWood, TextAnchor.MiddleCenter, FontStyle.Bold);
            int learnedCount = profile != null && profile.learnedVocabulary != null ? profile.learnedVocabulary.Count : 0;
            CollectionCopy(vocabCard, (profile != null && profile.language == "ko" ? "수집한 단어: " : "Đã mở khóa: ") + learnedCount + " / " + TastePassport.Learning.VocabularyCatalog.AllWords.Length, 22, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            int displayCount = Mathf.Min(6, TastePassport.Learning.VocabularyCatalog.AllWords.Length);
            for (int w = 0; w < displayCount; w++)
            {
                var word = TastePassport.Learning.VocabularyCatalog.AllWords[w];
                bool isLearned = profile != null && profile.learnedVocabulary != null && profile.learnedVocabulary.Contains(word.Id);
                var wordRow = Panel("VocabRow_" + word.Id, vocabCard, isLearned ? Paper : new Color(0.95f, 0.93f, 0.89f)).rectTransform;
                SetHeight(wordRow, 48);
                var wOutline = wordRow.gameObject.AddComponent<Outline>();
                wOutline.effectColor = isLearned ? Border : new Color(0.85f, 0.82f, 0.78f);
                wOutline.effectDistance = new Vector2(1f, -1f);
                var rowLayout = wordRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                rowLayout.padding = new RectOffset(14, 14, 4, 4);
                rowLayout.spacing = 8;
                rowLayout.childAlignment = TextAnchor.MiddleCenter;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = true;
                rowLayout.childForceExpandHeight = false;

                string leftText = isLearned ? (word.Vietnamese + " (" + word.KoreanPronunciation + ")") : "???";
                string rightText = isLearned ? word.Korean : "🔒 " + (profile != null && profile.language == "ko" ? "잠김" : "Chưa mở");
                var wLeft = Label(wordRow, leftText, 20, 44, isLearned ? HeaderWood : Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
                var wRight = Label(wordRow, rightText, 20, 44, isLearned ? Jade : Muted, TextAnchor.MiddleRight, FontStyle.Bold);
            }
            CollectionCopy(parent, T("collection_postcards"), 30, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int i = 0; i < PostcardCatalog.All.Length; i++)
            {
                int index = i;
                var definition = PostcardCatalog.All[i];
                bool unlocked = definition.IsUnlocked(profile);
                var postcard = JourneyCard(parent);
                CollectionCopy(postcard, T(unlocked ? "collection_unlocked" : "locked"), 26, unlocked ? Jade : Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
                CollectionCopy(postcard, T(definition.TitleKey), 28, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                if (unlocked) CompactButton(postcard, T("view_postcard"), Lantern, () => { postcardIndex = index; postcardBack = false; Navigate(Page.Postcard); }, Ink);
                else CollectionCopy(postcard, T("collection_postcard_hint"), 26, Muted, TextAnchor.MiddleCenter);
            }
            CompactButton(parent, T("edit_profile"), Paper, () => OpenCharacterEditor(Page.Passport), Ink);
            CompactButton(parent, T("nav_back_menu"), Paper, () => Navigate(Page.Menu), Ink);
        }

        private void StartLanternCraft()
        {
            StoryNarration.Stop();
            lanternCraftRun = new LanternCraftRun();
            selectedLanternMaterialId = null;
            lanternResultItemId = null;
            lanternMaterialStatus = null;
            koreanBackdrop = false;
            audioManager?.SetEnvironment(false);
            PlayStageAudio(2, false);
            Navigate(Page.LanternCraft);
        }

        private void DrawLanternCraft(Transform parent)
        {
            Header(parent, T("lantern_craft_title"));
            if (lanternCraftRun != null && lanternCraftRun.Complete)
            {
                ActivityGuide(parent, "activity_complete", "craft_completed_pending_save");
                Button(parent, T("save_result"), Jade, CompleteLanternCraft);
                CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
                return;
            }
            ActivityGuide(parent, "activity_lantern_phase", "lantern_craft_intro");
            int completed = (string.IsNullOrEmpty(lanternCraftRun.FrameId) ? 0 : 1) +
                (string.IsNullOrEmpty(lanternCraftRun.ColorId) ? 0 : 1) +
                (lanternCraftRun.FabricPlaced ? 1 : 0) + (lanternCraftRun.TasselPlaced ? 1 : 0);
            ProgressStrip(parent, completed, 4, T("activity_craft_progress"));
            DrawLanternPreview(parent, 170);
            if (!string.IsNullOrEmpty(lanternCraftRun.FrameId))
                CollectionCopy(parent, T("frame_" + lanternCraftRun.FrameId) +
                    (string.IsNullOrEmpty(lanternCraftRun.ColorId) ? "" : " · " + T("color_" + lanternCraftRun.ColorId)), 25, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);

            var frameCard = JourneyCard(parent);
            CollectionCopy(frameCard, T("craft_choose_frame"), 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            var frameRow = NewRect("Lantern frame choices", frameCard);
            SetHeight(frameRow, 104);
            AddHorizontal(frameRow);
            foreach (string frameId in new[] { "round", "diamond", "hexagon" })
            {
                string chosenFrame = frameId;
                Button(frameRow, T("frame_" + frameId) + (lanternCraftRun.FrameId == frameId ? " · " + T("selected_state") : ""),
                    lanternCraftRun.FrameId == frameId ? Jade : Paper,
                    () => SelectLanternFrame(chosenFrame), lanternCraftRun.FrameId == frameId ? Color.white : Ink);
            }

            var colorCard = JourneyCard(parent);
            CollectionCopy(colorCard, T("craft_choose_color"), 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int rowIndex = 0; rowIndex < 2; rowIndex++)
            {
                var colorRow = NewRect("Lantern color choices", colorCard);
                SetHeight(colorRow, 98);
                AddHorizontal(colorRow);
                for (int column = 0; column < 2; column++)
                {
                    string colorId = new[] { "red", "gold", "blue", "green" }[rowIndex * 2 + column];
                    string chosenColor = colorId;
                    bool isSelected = lanternCraftRun.ColorId == colorId;
                    Button(colorRow, T("color_" + colorId) + (isSelected ? " · " + T("selected_state") : ""),
                        isSelected ? ColorForLantern(colorId) : Paper,
                        () => SelectLanternColor(chosenColor), isSelected && colorId != "gold" ? Color.white : Ink);
                }
            }

            var materialsCard = JourneyCard(parent);
            CollectionCopy(materialsCard, T("craft_materials"), 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(materialsCard, T("craft_drag_hint"), 23, Muted, TextAnchor.MiddleCenter);
            var materialRow = NewRect("Lantern craft materials", materialsCard);
            SetHeight(materialRow, 112);
            AddHorizontal(materialRow);
            AddLanternMaterial(materialRow, LanternCraftRun.FabricMaterialId, "material_fabric");
            AddLanternMaterial(materialRow, LanternCraftRun.TasselMaterialId, "material_tassel");
            lanternMaterialStatus = CollectionCopy(materialsCard, string.IsNullOrEmpty(selectedLanternMaterialId) ? T("craft_material_unselected") : T("craft_selected_material") + ": " + T("material_" + selectedLanternMaterialId), 23, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);

            var slotsRow = NewRect("Lantern craft drop zones", parent);
            SetHeight(slotsRow, 230);
            AddHorizontal(slotsRow);
            AddLanternDropZone(slotsRow, LanternCraftRun.FabricSlotId, "craft_slot_fabric", lanternCraftRun.FabricPlaced);
            AddLanternDropZone(slotsRow, LanternCraftRun.TasselSlotId, "craft_slot_tassel", lanternCraftRun.TasselPlaced);

            var processCard = JourneyCard(parent);
            CollectionCopy(processCard, T("lantern_craft_process_title"), 26, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(processCard, T("lantern_craft_process"), 24, Ink, TextAnchor.MiddleCenter);
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void AddLanternMaterial(Transform parent, string materialId, string labelKey)
        {
            GameObject material = Button(parent, T(labelKey), selectedLanternMaterialId == materialId ? Lantern : Paper,
                () => SelectLanternMaterial(materialId), Ink);
            var group = material.GetComponent<CanvasGroup>();
            if (group == null) group = material.AddComponent<CanvasGroup>();
            var drag = material.AddComponent<LanternCraftDrag>();
            drag.Initialize(materialId);
        }

        private void AddLanternDropZone(Transform parent, string slotId, string labelKey, bool filled)
        {
            string slotLabel = T(labelKey) + (filled ? " · " + T("completed_state") : "\n" + T("craft_drop_here"));
            GameObject slot = Button(parent, slotLabel, filled ? new Color(0.84f, 0.91f, 0.81f) : Paper,
                () => PlaceLanternMaterial(slotId, selectedLanternMaterialId, null, null), Ink);
            SetHeight(slot.GetComponent<RectTransform>(), 190);
            slot.GetComponent<LanternCraftDropZone>()?.Initialize(slotId, PlaceLanternMaterial);
            if (slot.GetComponent<LanternCraftDropZone>() == null)
                slot.AddComponent<LanternCraftDropZone>().Initialize(slotId, PlaceLanternMaterial);
        }

        private void SelectLanternFrame(string frameId)
        {
            if (lanternCraftRun != null && lanternCraftRun.SelectFrame(frameId)) Show();
        }

        private void SelectLanternColor(string colorId)
        {
            if (lanternCraftRun != null && lanternCraftRun.SelectColor(colorId)) Show();
        }

        private void SelectLanternMaterial(string materialId)
        {
            selectedLanternMaterialId = materialId;
            if (lanternMaterialStatus != null)
                lanternMaterialStatus.text = T("craft_selected_material") + ": " + T("material_" + materialId);
            feedbackSound?.PlayTap();
        }

        private void PlaceLanternMaterial(string slotId, string materialId, LanternCraftDrag drag, Transform slot)
        {
            if (lanternCraftRun == null) return;
            if (string.IsNullOrEmpty(materialId))
            {
                notice = T("craft_material_unselected");
                StartCoroutine(RefreshAfterLanternInput(false));
                return;
            }
            LanternCraftInputResult result = lanternCraftRun.TryPlaceMaterial(slotId, materialId);
            if (result == LanternCraftInputResult.MissingDesign) notice = T("craft_choose_design_first");
            else if (result == LanternCraftInputResult.WrongMaterial) notice = T("craft_wrong_material");
            else if (result == LanternCraftInputResult.AlreadyFilled) notice = T("craft_slot_filled");
            else
            {
                selectedLanternMaterialId = null;
                if (drag != null && slot != null) drag.Accept(slot);
            }
            StartCoroutine(RefreshAfterLanternInput(result == LanternCraftInputResult.Completed));
        }

        private IEnumerator RefreshAfterLanternInput(bool completed)
        {
            yield return null;
            if (completed) CompleteLanternCraft();
            else Show();
        }

        private void CompleteLanternCraft()
        {
            var candidate = new PlayerProfile();
            candidate.CopyFrom(profile);
            if (lanternCraftRun == null || !lanternCraftRun.Complete)
            {
                notice = T("craft_save_error");
                Show();
                return;
            }
            if (!InventoryService.RecordLanternCraft(candidate, lanternCraftRun.FrameId, lanternCraftRun.ColorId))
            {
                notice = T("inventory_full");
                Show();
                return;
            }
            TastePassport.Learning.VocabularyCatalog.UnlockStageWords(candidate, 2);
            if (!store.TrySave(candidate))
            {
                notice = T("save_error");
                Show();
                return;
            }
            profile = candidate;
            audioManager?.PlayCue(GameSoundCue.Craft);
            lanternResultItemId = InventoryService.LanternProductPrefix + lanternCraftRun.FrameId + "-" + lanternCraftRun.ColorId;
            Navigate(Page.LanternResult);
        }

        private void DrawLanternPreview(Transform parent, float height)
        {
            DrawLanternPreview(parent, height, lanternCraftRun == null ? null : lanternCraftRun.ColorId);
        }

        private void DrawLanternPreview(Transform parent, float height, string colorId)
        {
            RectTransform preview = NewRect("Hoi An Lantern Preview", parent);
            SetHeight(preview, height);
            var image = preview.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>("Art/Props/HoiAnLantern");
            image.color = string.IsNullOrEmpty(colorId) ? Color.white : Color.Lerp(Color.white, ColorForLantern(colorId), 0.32f);
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (image.sprite == null) Debug.LogError("DẤU XỨ: Missing lantern sprite Resources/Art/Props/HoiAnLantern");
        }

        private void DrawLanternResult(Transform parent)
        {
            Header(parent, T("lantern_result_title"));
            ActivityGuide(parent, "activity_saved", "lantern_result_copy");
            DrawLanternPreview(parent, 210, InventoryService.GetLanternColorId(lanternResultItemId));
            var earned = JourneyCard(parent);
            CollectionCopy(earned, T("activity_earned"), 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(earned, T("lantern_item") + ": " + LanternVariantLabel(lanternResultItemId), 27, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (InventoryService.HasCultureCard(profile, InventoryService.LanternCardId))
                CollectionCopy(earned, T("culture_cards") + ": " + T("culture_lantern_title"), 26, Ink, TextAnchor.MiddleCenter);
            CollectionCopy(parent, T("activity_lantern_next"), 26, Muted, TextAnchor.MiddleCenter);
            Button(parent, T("card_continue"), Jade, () => Navigate(Page.Map));
            CompactButton(parent, T("inventory"), Paper, () => Navigate(Page.Inventory), Ink);
            CompactButton(parent, T("culture_cards"), Paper, () => Navigate(Page.CultureCards), Ink);
            CompactButton(parent, T("craft_again"), Paper, StartLanternCraft, Ink);
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void StartHanjiCraft()
        {
            StoryNarration.Stop();
            hanjiCraftRun = new HanjiCraftRun();
            hanjiResultItemId = null;
            koreanBackdrop = true;
            audioManager?.SetEnvironment(true);
            PlayStageAudio(2, true);
            Navigate(Page.HanjiCraft);
        }

        private void DrawHanjiCraft(Transform parent)
        {
            Header(parent, T("hanji_craft_title"));
            if (hanjiCraftRun == null) hanjiCraftRun = new HanjiCraftRun();
            ActivityGuide(parent, "activity_hanji_phase", "hanji_craft_intro");
            int completed = (string.IsNullOrEmpty(hanjiCraftRun.PaperColorId) ? 0 : 1) +
                (string.IsNullOrEmpty(hanjiCraftRun.ShapeId) ? 0 : 1) +
                (string.IsNullOrEmpty(hanjiCraftRun.MotifId) ? 0 : 1) +
                (hanjiCraftRun.CutApplied ? 1 : 0) + (hanjiCraftRun.FoldApplied ? 1 : 0) +
                (hanjiCraftRun.DecorationApplied ? 1 : 0);
            ProgressStrip(parent, completed, 6, T("activity_craft_progress"));
            DrawHanjiPreview(parent, 170, hanjiCraftRun.ShapeId, hanjiCraftRun.PaperColorId);
            if (!string.IsNullOrEmpty(hanjiCraftRun.ShapeId))
                CollectionCopy(parent, HanjiVariantLabel(hanjiCraftRun.ShapeId, hanjiCraftRun.PaperColorId, hanjiCraftRun.MotifId), 25, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);

            var colorCard = JourneyCard(parent);
            CollectionCopy(colorCard, T("hanji_choose_color"), 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int rowIndex = 0; rowIndex < 2; rowIndex++)
            {
                var row = NewRect("Hanji paper color choices", colorCard);
                SetHeight(row, 82);
                AddHorizontal(row);
                for (int column = 0; column < 2; column++)
                {
                    string colorId = new[] { "indigo", "vermilion", "gold", "jade" }[rowIndex * 2 + column];
                    bool selected = hanjiCraftRun.PaperColorId == colorId;
                    Button(row, T("hanji_color_" + colorId) + (selected ? " · " + T("selected_state") : ""),
                        selected ? ColorForHanji(colorId) : Paper,
                        () => SelectHanjiColor(colorId), selected && colorId != "gold" ? Color.white : Ink);
                }
            }

            var shapeCard = JourneyCard(parent);
            CollectionCopy(shapeCard, T("hanji_choose_shape"), 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            var shapeRow = NewRect("Hanji product shape choices", shapeCard);
            SetHeight(shapeRow, 132);
            AddHorizontal(shapeRow);
            foreach (string shapeId in new[] { "fan", "bookmark", "flower" })
            {
                string chosenShape = shapeId;
                bool selected = hanjiCraftRun.ShapeId == shapeId;
                Button(shapeRow, T("hanji_shape_" + shapeId) + (selected ? " · " + T("selected_state") : ""),
                    selected ? Jade : Paper, () => SelectHanjiShape(chosenShape), selected ? Color.white : Ink);
            }

            var motifCard = JourneyCard(parent);
            CollectionCopy(motifCard, T("hanji_choose_motif"), 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            var motifRow = NewRect("Hanji motif choices", motifCard);
            SetHeight(motifRow, 108);
            AddHorizontal(motifRow);
            foreach (string motifId in new[] { "flower", "cloud", "stripe" })
            {
                string chosenMotif = motifId;
                bool selected = hanjiCraftRun.MotifId == motifId;
                Button(motifRow, T("hanji_motif_" + motifId) + (selected ? " · " + T("selected_state") : ""),
                    selected ? Lantern : Paper, () => SelectHanjiMotif(chosenMotif), Ink);
            }

            var processCard = JourneyCard(parent);
            CollectionCopy(processCard, T("hanji_process_title"), 27, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(processCard, T("hanji_process_copy"), 24, Ink, TextAnchor.MiddleCenter);
            bool designReady = !string.IsNullOrEmpty(hanjiCraftRun.PaperColorId) &&
                !string.IsNullOrEmpty(hanjiCraftRun.ShapeId) && !string.IsNullOrEmpty(hanjiCraftRun.MotifId);
            bool cutNext = designReady && !hanjiCraftRun.CutApplied;
            bool foldNext = hanjiCraftRun.CutApplied && !hanjiCraftRun.FoldApplied;
            bool decorateNext = hanjiCraftRun.FoldApplied && !hanjiCraftRun.DecorationApplied;
            Button(processCard, T("hanji_cut") + StepMark(hanjiCraftRun.CutApplied),
                cutNext ? Jade : Paper, () => ApplyHanjiAction("cut"), cutNext ? Color.white : Ink);
            Button(processCard, T("hanji_fold") + StepMark(hanjiCraftRun.FoldApplied),
                foldNext ? Jade : Paper, () => ApplyHanjiAction("fold"), foldNext ? Color.white : Ink);
            Button(processCard, T("hanji_decorate") + StepMark(hanjiCraftRun.DecorationApplied),
                decorateNext ? Jade : Paper, () => ApplyHanjiAction("decorate"), decorateNext ? Color.white : Ink);
            if (hanjiCraftRun.Complete)
            {
                CollectionCopy(processCard, T("hanji_completed_pending_save"), 23, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
                Button(processCard, T("save_result"), Jade, CompleteHanjiCraft);
            }
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private string StepMark(bool applied) => applied ? " · " + T("completed_state") : "";

        private void SelectHanjiColor(string colorId)
        {
            if (hanjiCraftRun != null && hanjiCraftRun.SelectPaperColor(colorId)) Show();
        }

        private void SelectHanjiShape(string shapeId)
        {
            if (hanjiCraftRun != null && hanjiCraftRun.SelectShape(shapeId)) Show();
        }

        private void SelectHanjiMotif(string motifId)
        {
            if (hanjiCraftRun != null && hanjiCraftRun.SelectMotif(motifId)) Show();
        }

        private void ApplyHanjiAction(string actionId)
        {
            if (hanjiCraftRun == null) return;
            HanjiCraftActionResult result = hanjiCraftRun.ApplyAction(actionId);
            if (result == HanjiCraftActionResult.MissingDesign) notice = T("hanji_missing_design");
            else if (result == HanjiCraftActionResult.OutOfOrder) notice = T("hanji_out_of_order");
            else if (result == HanjiCraftActionResult.AlreadyApplied) notice = T("hanji_already_applied");
            else if (result == HanjiCraftActionResult.Applied) notice = T("hanji_step_applied");
            Show();
            if (result == HanjiCraftActionResult.Completed) CompleteHanjiCraft();
        }

        private void CompleteHanjiCraft()
        {
            if (hanjiCraftRun == null || !hanjiCraftRun.Complete)
            {
                notice = T("hanji_save_error");
                Show();
                return;
            }
            var candidate = new PlayerProfile();
            candidate.CopyFrom(profile);
            if (!InventoryService.RecordHanjiCraft(candidate, hanjiCraftRun.ShapeId,
                    hanjiCraftRun.PaperColorId, hanjiCraftRun.MotifId))
            {
                notice = T("inventory_full");
                Show();
                return;
            }
            TastePassport.Learning.VocabularyCatalog.UnlockStageWords(candidate, 6);
            if (!store.TrySave(candidate))
            {
                notice = T("save_error");
                Show();
                return;
            }
            profile = candidate;
            audioManager?.PlayCue(GameSoundCue.Craft);
            hanjiResultItemId = InventoryService.HanjiProductPrefix + hanjiCraftRun.ShapeId + "-" +
                                hanjiCraftRun.PaperColorId + "-" + hanjiCraftRun.MotifId;
            Navigate(Page.HanjiResult);
        }

        private void DrawHanjiPreview(Transform parent, float height, string shapeId, string colorId)
        {
            RectTransform preview = NewRect("Hanji Product Preview", parent);
            SetHeight(preview, height);
            var image = preview.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>(HanjiSpritePath(shapeId));
            image.color = string.IsNullOrEmpty(colorId) ? Color.white : Color.Lerp(Color.white, ColorForHanji(colorId), 0.22f);
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (image.sprite == null) Debug.LogError("DẤU XỨ: Missing Hanji sprite for " + (shapeId ?? "default"));
        }

        private static string HanjiSpritePath(string shapeId)
        {
            if (shapeId == "bookmark") return "Art/Props/HanjiBookmark";
            if (shapeId == "flower") return "Art/Props/HanjiFlower";
            return "Art/Props/HanjiFan";
        }

        private void DrawHanjiResult(Transform parent)
        {
            Header(parent, T("hanji_result_title"));
            ActivityGuide(parent, "activity_saved", "hanji_result_copy");
            DrawHanjiPreview(parent, 210, InventoryService.GetHanjiShapeId(hanjiResultItemId),
                InventoryService.GetHanjiColorId(hanjiResultItemId));
            var earned = JourneyCard(parent);
            CollectionCopy(earned, T("activity_earned"), 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(earned, T("hanji_item") + ": " + HanjiVariantLabel(hanjiResultItemId), 27, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(earned, T("culture_cards") + ": " + T("culture_hanji_title"), 26, Ink, TextAnchor.MiddleCenter);
            CollectionCopy(parent, T("activity_hanji_next"), 26, Muted, TextAnchor.MiddleCenter);
            Button(parent, T("inventory"), Jade, () => Navigate(Page.Inventory));
            CompactButton(parent, T("culture_cards"), Paper, () => Navigate(Page.CultureCards), Ink);
            CompactButton(parent, T("hanji_again"), Paper, StartHanjiCraft, Ink);
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void DrawInventory(Transform parent)
        {
            int count = profile.inventoryItems == null ? 0 : profile.inventoryItems.Count;
            CollectionHeader(parent, "inventory", "collection_inventory_hint", T("collection_owned") + ": " + count);
            if (count == 0)
                CollectionCopy(JourneyCard(parent), T("inventory_empty"), 27, Muted, TextAnchor.MiddleCenter);
            else
            {
                foreach (string itemId in profile.inventoryItems)
                {
                    var card = JourneyCard(parent);
                    CollectionCopy(card, T("collection_owned"), 26, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
                    string hanjiShape = InventoryService.GetHanjiShapeId(itemId);
                    if (!string.IsNullOrEmpty(hanjiShape))
                    {
                        CollectionCopy(card, T("hanji_item"), 29, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                        CollectionCopy(card, HanjiVariantLabel(itemId), 26, Ink, TextAnchor.MiddleCenter);
                        DrawHanjiPreview(card, 120, hanjiShape, InventoryService.GetHanjiColorId(itemId));
                    }
                    else if (!string.IsNullOrEmpty(InventoryService.GetLanternFrameId(itemId)))
                    {
                        CollectionCopy(card, T("lantern_item"), 29, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                        CollectionCopy(card, LanternVariantLabel(itemId), 26, Ink, TextAnchor.MiddleCenter);
                        DrawLanternPreview(card, 120, InventoryService.GetLanternColorId(itemId));
                    }
                    else CollectionCopy(card, T("inventory_unknown_item"), 29, Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
                }
            }
            CompactButton(parent, T("culture_cards"), Paper, () => Navigate(Page.CultureCards), Ink);
            CompactButton(parent, T("nav_back_passport"), Paper, () => Navigate(Page.Passport), Ink);
        }

        private void DrawCultureCards(Transform parent)
        {
            CollectionHeader(parent, "culture_cards", "collection_culture_hint",
                T("collection_owned") + ": " + (profile.culturalCards == null ? 0 : profile.culturalCards.Count));
            DrawCultureCard(parent, InventoryService.ChuaCauCardId, "culture_chuacau_title", "culture_chuacau_body", "culture_chuacau_source");
            DrawCultureCard(parent, InventoryService.CaoLauCardId, "culture_caolau_title", "culture_caolau_body", "culture_caolau_source");
            DrawCultureCard(parent, InventoryService.LanternCardId, "culture_lantern_title", "culture_lantern_body", "culture_lantern_source");
            DrawCultureCard(parent, InventoryService.HanokCardId, "culture_hanok_title", "culture_hanok_body", "culture_hanok_source");
            DrawCultureCard(parent, InventoryService.HanjiCardId, "culture_hanji_title", "culture_hanji_body", "culture_hanji_source");
            DrawCultureCard(parent, InventoryService.BibimbapCardId, "culture_bibimbap_title", "culture_bibimbap_body", "culture_bibimbap_source");
            DrawCultureCard(parent, InventoryService.GyeonggijeonCardId, "culture_gyeonggijeon_title", "culture_gyeonggijeon_body", "culture_gyeonggijeon_source");
            DrawCultureCard(parent, InventoryService.PansoriCardId, "culture_pansori_title", "culture_pansori_body", "culture_pansori_source");
            CompactButton(parent, T("inventory"), Paper, () => Navigate(Page.Inventory), Ink);
            CompactButton(parent, T("nav_back_passport"), Paper, () => Navigate(Page.Passport), Ink);
        }

        private void DrawCultureCard(Transform parent, string cardId, string titleKey, string bodyKey, string sourceKey)
        {
            bool unlocked = InventoryService.HasCultureCard(profile, cardId);
            var card = JourneyCard(parent);
            CollectionCopy(card, T(unlocked ? "collection_owned" : "locked"), 26, unlocked ? Jade : Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(card, T(titleKey), 29, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (unlocked)
            {
                CollectionCopy(card, T(bodyKey), 26, Ink);
                Rule(card);
                CollectionCopy(card, T(sourceKey), 26, Muted);
            }
            else CollectionCopy(card, T("culture_card_locked"), 26, Muted, TextAnchor.MiddleCenter);
        }

        private Color ColorForLantern(string colorId)
        {
            if (colorId == "red") return Hex("#B7473D");
            if (colorId == "gold") return Hex("#C89136");
            if (colorId == "blue") return Hex("#3D6F91");
            return Hex("#56785E");
        }

        private Color ColorForHanji(string colorId)
        {
            if (colorId == "indigo") return Hex("#49658A");
            if (colorId == "vermilion") return Hex("#B7473D");
            if (colorId == "gold") return Hex("#C89136");
            return Hex("#4F806C");
        }

        private string HanjiVariantLabel(string itemId)
        {
            return HanjiVariantLabel(InventoryService.GetHanjiShapeId(itemId),
                InventoryService.GetHanjiColorId(itemId), InventoryService.GetHanjiMotifId(itemId));
        }

        private string HanjiVariantLabel(string shapeId, string colorId, string motifId)
        {
            if (string.IsNullOrEmpty(shapeId) || string.IsNullOrEmpty(colorId) || string.IsNullOrEmpty(motifId))
                return T("inventory_unknown_item");
            return T("hanji_shape_" + shapeId) + " · " + T("hanji_color_" + colorId) + " · " + T("hanji_motif_" + motifId);
        }

        private string LanternVariantLabel(string itemId)
        {
            string frameId = InventoryService.GetLanternFrameId(itemId);
            string colorId = InventoryService.GetLanternColorId(itemId);
            return !string.IsNullOrEmpty(frameId) && !string.IsNullOrEmpty(colorId)
                ? T("frame_" + frameId) + " · " + T("color_" + colorId)
                : T("inventory_unknown_item");
        }

        private void DrawPostcard(Transform parent)
        {
            var definition = PostcardCatalog.All[Mathf.Clamp(postcardIndex, 0, PostcardCatalog.All.Length - 1)];
            if (!definition.IsUnlocked(profile)) { Navigate(Page.Passport); return; }
            Header(parent, T(definition.TitleKey));
            CollectionCopy(parent, T("collection_unlocked") + " · " + T(postcardBack ? "back_side" : "front"), 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            var card = JourneyCard(parent);
            postcardCard = card as RectTransform;
            if (!postcardBack)
            {
                CollectionCopy(card, T(definition.TitleKey), 31, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                var pair = NewRect("Two cultures", card);
                SetHeight(pair, 235);
                AddHorizontal(pair);
                var left = NewRect("Vietnam picture", pair);
                SetHeight(left, 220);
                TravelArtwork.Draw(left, postcardIndex == 1 ? TravelArtworkKind.CaoLau : TravelArtworkKind.HoiAn);
                HoiAnVisuals.Attach(left, postcardIndex == 1 ? TravelArtworkKind.CaoLau : TravelArtworkKind.HoiAn);
                var right = NewRect("Korea picture", pair);
                SetHeight(right, 220);
                TravelArtwork.Draw(right, postcardIndex == 1 ? TravelArtworkKind.Bibimbap : TravelArtworkKind.Jeonju);
                KoreaVisuals.Attach(right, postcardIndex == 1 ? TravelArtworkKind.Bibimbap : TravelArtworkKind.Jeonju);
                AddJourneyThread(card);
                CollectionCopy(card, T(definition.FrontLeftKey) + "  •  " + T(definition.FrontRightKey), 26, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                Label(card, T("collection_postcard_hint"), 22, 44, Vermilion, TextAnchor.MiddleCenter);
            }
            else
            {
                Label(card, T("passport"), 25, 45, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
                CollectionCopy(card, T(definition.StoryKey), 29, Ink);
                Rule(card);
                Label(card, T("culture_note"), 27, 54, Jade, TextAnchor.MiddleCenter, FontStyle.Bold, true);
                CollectionCopy(card, T(definition.FactKey), 26, Ink);
                CollectionCopy(card, T("tomorrow_clue_" + (postcardIndex + 1)), 26, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
                NarrationButtons(parent, T(definition.StoryKey));
            }
            Button(parent, T("flip_postcard"), Jade, StartPostcardFlip);
            CompactButton(parent, T("nav_back_passport"), Paper, () => Navigate(Page.Passport), Ink);
        }

        private void StartPostcardFlip()
        {
            if (postcardFlip != null) return;
            audioManager?.PlayCue(GameSoundCue.PageFlip);
            postcardFlip = StartCoroutine(AnimatePostcardFlip());
        }

        private IEnumerator AnimatePostcardFlip()
        {
            if (postcardCard == null)
            {
                postcardBack = !postcardBack;
                postcardFlip = null;
                Show();
                yield break;
            }

            yield return ScalePostcard(postcardCard, 1f, 0f);
            postcardBack = !postcardBack;
            rebuildingPostcardForFlip = true;
            Show();
            rebuildingPostcardForFlip = false;
            if (postcardCard != null)
            {
                postcardCard.localScale = new Vector3(0f, 1f, 1f);
                yield return ScalePostcard(postcardCard, 0f, 1f);
            }
            postcardFlip = null;
        }

        private IEnumerator ScalePostcard(RectTransform card, float from, float to)
        {
            const float duration = .16f;
            float elapsed = 0f;
            while (card != null && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float scale = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                card.localScale = new Vector3(scale, 1f, 1f);
                yield return null;
            }
            if (card != null) card.localScale = new Vector3(to, 1f, 1f);
        }

        private bool showHoiAnActivities;
        private bool showJeonjuActivities;

        private void DrawMap(Transform parent)
        {
            MapObjectiveDefinition nextObjective = MapObjectiveService.GetCurrent(profile, IsJeonjuUnlocked(profile));
            var objectiveCard = JourneyCard(parent);
            objectiveCard.name = "Next objective surface";
            var objImage = objectiveCard.GetComponent<Image>();
            if (objImage != null) objImage.color = new Color(0.99f, 0.97f, 0.92f, 0.95f);
            var objBorder = objectiveCard.GetComponent<Outline>();
            if (objBorder != null)
            {
                objBorder.effectColor = GoldGlow;
                objBorder.effectDistance = new Vector2(2f, -2f);
            }
            CollectionCopy(objectiveCard, "◆ " + T("map") + " ◆", 36, HeaderWood, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Label(objectiveCard, T("map_next_step"), 22, 36, Lantern, TextAnchor.MiddleCenter, FontStyle.Bold);
            var objectiveTitle = Label(objectiveCard, T(nextObjective.TitleKey), 32, 86, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            // Some existing objective keys contain a full mission introduction.
            // Measure that text at the available width instead of truncating it.
            var titleSize = objectiveTitle.GetComponent<LayoutElement>();
            titleSize.minHeight = -1;
            titleSize.preferredHeight = -1;
            string objectiveHint = nextObjective.Action == MapObjectiveAction.LanternCraft || nextObjective.Action == MapObjectiveAction.HanjiCraft
                ? "card_craft_hint" : nextObjective.Action == MapObjectiveAction.StoryCardTarget ? "card_next_hint" : nextObjective.DescriptionKey;
            Rule(objectiveCard);
            CollectionCopy(objectiveCard, T(objectiveHint), 25, Ink, TextAnchor.MiddleCenter);
            StoryPrimary(objectiveCard, T(profile.mainStoryCompleted ? "view_ending" : nextObjective.ActionLabelKey),
                profile.mainStoryCompleted ? () => Navigate(Page.FinaleResult) : () => RunMapObjective(nextObjective));

            var journeyProgress = JourneyCard(parent);
            var progImage = journeyProgress.GetComponent<Image>();
            if (progImage != null) progImage.color = new Color(0.98f, 0.96f, 0.91f, 0.96f);
            var progBorder = journeyProgress.GetComponent<Outline>();
            if (progBorder != null)
            {
                progBorder.effectColor = GoldGlow;
                progBorder.effectDistance = new Vector2(1.5f, -1.5f);
            }
            int totalStamps = JourneyStampCount(LevelCatalog.HoiAn.Id, RecipeCatalog.CaoLau.Id, MatchCatalog.HoiAn.Id,
                LevelCatalog.Jeonju.Id, RecipeCatalog.Bibimbap.Id, MatchCatalog.Jeonju.Id);
            CollectionCopy(journeyProgress, "🔖 " + T("journey_destinations") + " · " + T("journey_progress") + " " + totalStamps + "/6",
                26, HeaderWood, TextAnchor.MiddleCenter, FontStyle.Bold);

            // Visual Cultural Stamp Strip
            var stampStrip = NewRect("Cultural stamp track", journeyProgress);
            SetHeight(stampStrip, 52);
            AddHorizontal(stampStrip);
            string[] trackMissions = {
                LevelCatalog.HoiAn.Id, "lantern-display-ready", RecipeCatalog.CaoLau.Id,
                LevelCatalog.Jeonju.Id, "hanji-display-ready", RecipeCatalog.Bibimbap.Id
            };
            string[] trackIcons = { "🏛️", "🏮", "🍜", "🏯", "📜", "🍚" };
            for (int i = 0; i < 6; i++)
            {
                bool earned = i == 1 ? StoryProgressService.HasImprint(profile, "lantern-display-ready")
                    : i == 4 ? StoryProgressService.HasImprint(profile, "hanji-display-ready")
                    : ProgressService.HasStamp(profile, trackMissions[i]);
                var badge = NewRect("StampToken_" + i, stampStrip);
                SetHeight(badge, 44);
                var tokenBg = badge.gameObject.AddComponent<Image>();
                tokenBg.sprite = CircleSprite();
                tokenBg.color = earned ? (i < 3 ? Lantern : Jade) : new Color(0.85f, 0.82f, 0.77f, 0.5f);
                var tokenBorder = badge.gameObject.AddComponent<Outline>();
                tokenBorder.effectColor = earned ? GoldGlow : Border;
                tokenBorder.effectDistance = new Vector2(1f, -1f);
                var tokenLabel = Label(badge, trackIcons[i] + (earned ? "✓" : ""), 20, 44, earned ? Color.white : Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
                Stretch(tokenLabel.rectTransform);
                var lyt = badge.gameObject.AddComponent<LayoutElement>();
                lyt.flexibleWidth = 1f;
            }

            DrawVisualJourneyMap(parent);

            MapLocationSwitcher(parent);

            var hoian = JourneyCard(parent);
            hoian.name = "Hoi An destination surface";
            var hoianBorder = hoian.GetComponent<Outline>();
            if (hoianBorder != null)
            {
                hoianBorder.effectColor = Lantern;
                hoianBorder.effectDistance = new Vector2(2f, -2f);
            }
            Artwork(hoian, TravelArtworkKind.HoiAn, 110);
            // The 38pt display face needs a 55-unit line box plus room for descenders.
            Label(hoian, "🏮 " + T("hoian"), 38, 64, HeaderWood, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            CollectionCopy(hoian, T("hoian_desc"), 24, Muted, TextAnchor.MiddleCenter, FontStyle.Italic);
            DestinationProgress(hoian, LevelCatalog.HoiAn.Id, RecipeCatalog.CaoLau.Id, MatchCatalog.HoiAn.Id);
            CompactButton(hoian, T(showHoiAnActivities ? "journey_hide_activities" : "journey_show_activities"), Paper,
                () => { showHoiAnActivities = !showHoiAnActivities; Show(); }, Ink);
            if (showHoiAnActivities)
            {
                Label(hoian, "🏮 " + T("journey_explore_craft"), 28, 52, HeaderWood, TextAnchor.MiddleLeft, FontStyle.Bold);
                DrawLocationTargets(hoian, "hoian");
                if (profile.mainStoryCompleted) CompactButton(hoian, T("explore_hoian"), Paper, () => OpenWorld("hoian"), Ink);
                var hoianTierTag = NewRect("Mission tier tag hoian_architecture", hoian);
                SetHeight(hoianTierTag, 26);
                var hoianTierLabel = Label(hoianTierTag, "◆ " + T("journey_tier_familiar_short"), 20, 26, Jade, TextAnchor.MiddleLeft, FontStyle.Bold);
                Stretch(hoianTierLabel.rectTransform);
                hoianTierLabel.rectTransform.offsetMin = new Vector2(12, 0);
                CompactButton(hoian, MissionLabel("journey_hoian_architecture", LevelCatalog.HoiAn.Id), Paper, () => StartLevel(LevelCatalog.HoiAn), Ink);
                CompactButton(hoian, T("lantern_craft_map"), Paper, StartLanternCraft, Ink);
                Label(hoian, "🍜 " + T("journey_food_exchange"), 28, 52, HeaderWood, TextAnchor.MiddleLeft, FontStyle.Bold);
                Artwork(hoian, TravelArtworkKind.CaoLau, 126);
                JourneyMissionButton(hoian, "cao_lau", RecipeCatalog.CaoLau.Id, LevelCatalog.HoiAn.Id, "journey_hoian_architecture", () => StartRecipe(RecipeCatalog.CaoLau));
                JourneyMissionButton(hoian, "hoian_exchange", MatchCatalog.HoiAn.Id, RecipeCatalog.CaoLau.Id, "cao_lau", () => StartMatch(MatchCatalog.HoiAn));
            }
            HoiAnVisuals.DecorateLocationCard(hoian);

            var jeonju = JourneyCard(parent);
            jeonju.name = "Jeonju destination surface";
            var jeonjuBorder = jeonju.GetComponent<Outline>();
            if (jeonjuBorder != null)
            {
                jeonjuBorder.effectColor = Jade;
                jeonjuBorder.effectDistance = new Vector2(2f, -2f);
            }
            Artwork(jeonju, TravelArtworkKind.Jeonju, 110);
            Label(jeonju, "🏯 " + T("jeonju"), 38, 64, HeaderWood, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            CollectionCopy(jeonju, T("jeonju_desc"), 24, Muted, TextAnchor.MiddleCenter, FontStyle.Italic);
            DestinationProgress(jeonju, LevelCatalog.Jeonju.Id, RecipeCatalog.Bibimbap.Id, MatchCatalog.Jeonju.Id);
            bool jeonjuUnlocked = IsJeonjuUnlocked(profile);
            if (!jeonjuUnlocked) CollectionCopy(jeonju, "🔒 " + T("locked") + " · " + T("journey_jeonju_requirement"), 26, Muted, TextAnchor.MiddleCenter);
            CompactButton(jeonju, T(showJeonjuActivities ? "journey_hide_activities" : "journey_show_activities"), Paper,
                () => { showJeonjuActivities = !showJeonjuActivities; Show(); }, Ink);
            if (showJeonjuActivities)
            {
                Label(jeonju, "🏯 " + T("journey_explore_craft"), 28, 52, HeaderWood, TextAnchor.MiddleLeft, FontStyle.Bold);
                DrawLocationTargets(jeonju, "jeonju");
                if (profile.mainStoryCompleted)
                    CompactButton(jeonju, T("explore_jeonju"), Paper, () => OpenWorld("jeonju"), Ink);
                JourneyMissionButton(jeonju, "journey_jeonju_architecture", LevelCatalog.Jeonju.Id, MatchCatalog.HoiAn.Id, "hoian_exchange", () => StartLevel(LevelCatalog.Jeonju));
                CompactButton(jeonju, T("hanji_craft_map") + (jeonjuUnlocked ? "" : " · " + T("locked")), jeonjuUnlocked ? Paper : Border,
                    jeonjuUnlocked ? StartHanjiCraft : () => Toast(T("jeonju_not_unlocked")), Ink)
                    .GetComponent<UnityEngine.UI.Button>().interactable = jeonjuUnlocked;
                Label(jeonju, "🍚 " + T("journey_food_exchange"), 28, 52, HeaderWood, TextAnchor.MiddleLeft, FontStyle.Bold);
                Artwork(jeonju, TravelArtworkKind.Bibimbap, 126);
                JourneyMissionButton(jeonju, "bibimbap", RecipeCatalog.Bibimbap.Id, LevelCatalog.Jeonju.Id, "journey_jeonju_architecture", () => StartRecipe(RecipeCatalog.Bibimbap));
                JourneyMissionButton(jeonju, "jeonju_exchange", MatchCatalog.Jeonju.Id, RecipeCatalog.Bibimbap.Id, "bibimbap", () => StartMatch(MatchCatalog.Jeonju));
            }
            KoreaVisuals.DecorateLocationCard(jeonju);

            DrawJourneyTiers(parent);
            JourneyNavigation(parent, Page.Map);

            var storyCard = JourneyCard(parent);
            var storyBorder = storyCard.GetComponent<Outline>();
            if (storyBorder != null)
            {
                storyBorder.effectColor = GoldGlow;
                storyBorder.effectDistance = new Vector2(2f, -2f);
            }
            Label(storyCard, profile.mainStoryCompleted ? T("main_story_completed") : "📜 " + T("chapter_title_4"),
                30, 86, profile.mainStoryCompleted ? Jade : Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            if (profile.mainStoryCompleted)
            {
                Label(storyCard, T("main_story_complete_map"), 24, 66, Ink, TextAnchor.MiddleCenter);
                CompactButton(storyCard, T("view_ending"), Paper, () => Navigate(Page.FinaleResult), Ink);
            }
            else if (FirstJourneyComplete())
            {
                Label(storyCard, T("festival_ready_intro"), 26, 112, Ink, TextAnchor.MiddleCenter);
                if (nextObjective.Action != MapObjectiveAction.Festival)
                    CompactButton(storyCard, T("finale_start"), Paper, StartFinale, Ink);
            }
            else Label(storyCard, T("finale_locked_hint"), 26, 144, Muted, TextAnchor.MiddleCenter);
        }

        private void DrawVisualJourneyMap(Transform parent)
        {
            var mapCard = JourneyCard(parent);
            mapCard.name = "Expedition cultural map board";
            var cardImg = mapCard.GetComponent<Image>();
            if (cardImg != null) cardImg.color = new Color(0.99f, 0.97f, 0.92f, 0.98f);
            var cardBorder = mapCard.GetComponent<Outline>();
            if (cardBorder != null)
            {
                cardBorder.effectColor = GoldGlow;
                cardBorder.effectDistance = new Vector2(2f, -2f);
            }

            // Map Header
            CollectionCopy(mapCard, "🧭 " + (profile.language == "ko" ? "한-베 문화 탐험 지도" : "BẢN ĐỒ KHÁM PHÁ VĂN HÓA VIỆT — HÀN"), 30, HeaderWood, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Label(mapCard, profile.language == "ko" ? "8개 문화 여정 및 유산 거점" : "Hành trình 8 chặng văn hóa & di sản hai miền đất", 22, 32, Lantern, TextAnchor.MiddleCenter, FontStyle.Italic);

            // Geographic Map Silhouette with City Pin
            Artwork(mapCard, selectedMapKorea ? TravelArtworkKind.KoreaMap : TravelArtworkKind.VietnamMap, 150);

            // Region Title: Hội An - Việt Nam (Chặng 1 -> 4)
            Label(mapCard, "🏮 " + (profile.language == "ko" ? "베트남 호이안 여정 (1~4단계)" : "VIỆT NAM — HỘI AN (CHẶNG 1 — 4)"), 24, 38, HeaderWood, TextAnchor.MiddleLeft, FontStyle.Bold);

            // 4 Waypoint Cards for Hoi An
            DrawWaypointRow(mapCard, 1, "🏛️ Chùa Cầu", LevelCatalog.HoiAn.Id, () => StartLevel(LevelCatalog.HoiAn), true);
            DrawWaypointRow(mapCard, 2, "🏮 Đèn Lồng", "lantern-display-ready", StartLanternCraft, StoryProgressService.HasImprint(profile, "lantern-display-ready") || ProgressService.HasStamp(profile, LevelCatalog.HoiAn.Id));
            DrawWaypointRow(mapCard, 3, "🍜 Cao Lầu", RecipeCatalog.CaoLau.Id, () => StartRecipe(RecipeCatalog.CaoLau), StoryProgressService.HasImprint(profile, "lantern-display-ready"));
            DrawWaypointRow(mapCard, 4, "🎴 Bài Chòi", MatchCatalog.HoiAn.Id, () => StartMatch(MatchCatalog.HoiAn), ProgressService.HasStamp(profile, RecipeCatalog.CaoLau.Id));

            // Cultural Bridge Connector
            var bridge = NewRect("Cultural route connector", mapCard);
            SetHeight(bridge, 34);
            var bridgeLabel = Label(bridge, "✈️ ────── " + (profile.language == "ko" ? "우정의 문화 교류 다리" : "CẦU NỐI VĂN HÓA VIỆT — HÀN") + " ────── ✈️", 20, 34, GoldGlow, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(bridgeLabel.rectTransform);

            // Region Title: Jeonju - Hàn Quốc (Chặng 5 -> 8)
            Label(mapCard, "🏯 " + (profile.language == "ko" ? "한국 전주 여정 (5~8단계)" : "HÀN QUỐC — JEONJU (CHẶNG 5 — 8)"), 24, 38, Jade, TextAnchor.MiddleLeft, FontStyle.Bold);

            bool jeonjuUnlocked = IsJeonjuUnlocked(profile);
            DrawWaypointRow(mapCard, 5, "🏯 Làng Hanok", LevelCatalog.Jeonju.Id, () => { if (jeonjuUnlocked) StartLevel(LevelCatalog.Jeonju); else Toast(T("jeonju_not_unlocked")); }, jeonjuUnlocked);
            DrawWaypointRow(mapCard, 6, "📜 Giấy Hanji", "hanji-display-ready", () => { if (jeonjuUnlocked) StartHanjiCraft(); else Toast(T("jeonju_not_unlocked")); }, jeonjuUnlocked && ProgressService.HasStamp(profile, LevelCatalog.Jeonju.Id));
            DrawWaypointRow(mapCard, 7, "🍚 Bibimbap", RecipeCatalog.Bibimbap.Id, () => { if (jeonjuUnlocked) StartRecipe(RecipeCatalog.Bibimbap); else Toast(T("jeonju_not_unlocked")); }, jeonjuUnlocked && StoryProgressService.HasImprint(profile, "hanji-display-ready"));
            DrawWaypointRow(mapCard, 8, "🎊 Ngày Hội Lớn", MatchCatalog.Jeonju.Id, () => { if (FirstJourneyComplete()) StartFinale(); else Toast(T("finale_locked_hint")); }, jeonjuUnlocked && ProgressService.HasStamp(profile, RecipeCatalog.Bibimbap.Id));
        }

        private void DrawWaypointRow(Transform parent, int stageNum, string title, string missionOrImprint, Action onAction, bool unlocked)
        {
            bool earned = missionOrImprint == "lantern-display-ready" || missionOrImprint == "hanji-display-ready"
                ? StoryProgressService.HasImprint(profile, missionOrImprint)
                : ProgressService.HasStamp(profile, missionOrImprint);

            var row = NewRect("Waypoint_" + stageNum, parent);
            SetHeight(row, 56);
            AddHorizontal(row);

            // Stage Number Badge
            var numBox = NewRect("StageNumBox", row);
            SetHeight(numBox, 48);
            var numImg = numBox.gameObject.AddComponent<Image>();
            numImg.sprite = CircleSprite();
            numImg.color = earned ? Jade : unlocked ? Lantern : Border;
            var numTxt = Label(numBox, stageNum.ToString(), 20, 48, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(numTxt.rectTransform);
            var numLyt = numBox.gameObject.AddComponent<LayoutElement>();
            numLyt.minWidth = 48;
            numLyt.preferredWidth = 48;

            // Title & Status
            string statusStr = earned ? " ★★★ " + (profile.language == "ko" ? "완료" : "Đã xong ✓")
                : unlocked ? (profile.language == "ko" ? "탐험 가능 ▶" : "Sẵn sàng ▶")
                : (profile.language == "ko" ? "잠김 🔒" : "Khóa 🔒");
            Color titleColor = earned ? Jade : unlocked ? Ink : Muted;

            var actionBtn = CompactButton(row, title + "  " + statusStr, unlocked ? Paper : new Color(0.92f, 0.90f, 0.86f),
                unlocked ? onAction : () => Toast(T("locked")), titleColor);
            var btnLyt = actionBtn.GetComponent<LayoutElement>();
            if (btnLyt != null) btnLyt.flexibleWidth = 1f;
            var btnBorder = actionBtn.GetComponent<Outline>();
            if (btnBorder != null)
            {
                btnBorder.effectColor = earned ? GoldGlow : unlocked ? Border : new Color(0.7f, 0.68f, 0.65f, 0.5f);
                btnBorder.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        private void MapLocationSwitcher(Transform parent)
        {
            var row = NewRect("Location destination selector", parent);
            SetHeight(row, 86);
            AddHorizontal(row);
            var hoiAn = CompactButton(row, T("map_tab_hoian") + (selectedMapKorea ? "" : " · " + T("selected_state")),
                selectedMapKorea ? Paper : Lantern, () => SelectMapDestination(false), selectedMapKorea ? Ink : Color.white);
            var jeonju = CompactButton(row, T("map_tab_jeonju") + (selectedMapKorea ? " · " + T("selected_state") : ""),
                selectedMapKorea ? Jade : Paper, () => SelectMapDestination(true), selectedMapKorea ? Color.white : Ink);
            foreach (var button in new[] { hoiAn, jeonju })
            {
                var layout = button.GetComponent<LayoutElement>();
                if (layout != null) layout.flexibleWidth = 1f;
            }
            var hoiBorder = hoiAn.GetComponent<Outline>();
            if (hoiBorder != null)
            {
                hoiBorder.effectColor = selectedMapKorea ? Border : GoldGlow;
                hoiBorder.effectDistance = new Vector2(1.5f, -1.5f);
            }
            var jeonBorder = jeonju.GetComponent<Outline>();
            if (jeonBorder != null)
            {
                jeonBorder.effectColor = selectedMapKorea ? GoldGlow : Border;
                jeonBorder.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        private void SelectMapDestination(bool korean)
        {
            selectedMapKorea = korean;
            koreanBackdrop = korean;
            audioManager?.SetEnvironment(korean);
            Show();
        }

        private int JourneyStampCount(params string[] missionIds)
        {
            int count = 0;
            foreach (string id in missionIds)
                if (ProgressService.HasStamp(profile, id)) count++;
            return count;
        }

        private void DrawJourneyTiers(Transform parent)
        {
            var intro = JourneyCard(parent);
            var introBorder = intro.GetComponent<Outline>();
            if (introBorder != null)
            {
                introBorder.effectColor = Border;
                introBorder.effectDistance = new Vector2(1.5f, -1.5f);
            }
            CollectionCopy(intro, "◆ " + T("journey_tiers") + " · " + T("journey_tier_unlock_note") + " ◆", 24, HeaderWood,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            var row = NewRect("Journey tier progress", parent);
            SetHeight(row, 84);
            AddHorizontal(row);
            JourneyTierPill(row, "journey_tier_familiar_short", JourneyStampCount(LevelCatalog.HoiAn.Id, LevelCatalog.Jeonju.Id));
            JourneyTierPill(row, "journey_tier_challenge_short", JourneyStampCount(RecipeCatalog.CaoLau.Id, RecipeCatalog.Bibimbap.Id));
            JourneyTierPill(row, "journey_tier_mastery_short", JourneyStampCount(MatchCatalog.HoiAn.Id, MatchCatalog.Jeonju.Id));
        }

        private void JourneyTierPill(Transform parent, string titleKey, int completed)
        {
            var tile = Panel("Journey tier " + titleKey, parent, completed == 2 ? new Color(.91f,.96f,.92f) : Paper).rectTransform;
            SetHeight(tile, 84);
            var border = tile.gameObject.AddComponent<Outline>();
            border.effectColor = completed == 2 ? GoldGlow : Border;
            border.effectDistance = new Vector2(1.5f, -1.5f);
            var shadow = tile.gameObject.AddComponent<Shadow>();
            shadow.effectColor = GameUiTheme.PaperShadow;
            shadow.effectDistance = new Vector2(0f, -2f);
            var label = Label(tile, (completed == 2 ? "★ " : "◇ ") + T(titleKey) + "\n" + completed + "/2", 22, 84,
                completed == 2 ? Jade : Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label.rectTransform);
        }

        private void DestinationProgress(Transform parent, params string[] missionIds)
        {
            Label(parent, "🔖 " + T("journey_destination_progress") + "  " + JourneyStampCount(missionIds) + "/3", 26, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private void JourneyMissionButton(Transform parent, string titleKey, string missionId, string prerequisiteId, string prerequisiteTitleKey, Action action)
        {
            int tier = missionId.Contains("cao_lau") || missionId.Contains("bibimbap") ? 2
                : missionId.Contains("exchange") ? 3 : 1;
            string tierKey = tier == 1 ? "journey_tier_familiar_short"
                : tier == 2 ? "journey_tier_challenge_short" : "journey_tier_mastery_short";
            var badgeRow = NewRect("Mission tier tag " + missionId, parent);
            SetHeight(badgeRow, 26);
            var tierText = Label(badgeRow, "◆ " + T(tierKey), 20, 26,
                tier == 1 ? Jade : tier == 2 ? Lantern : Vermilion, TextAnchor.MiddleLeft, FontStyle.Bold);
            Stretch(tierText.rectTransform);
            tierText.rectTransform.offsetMin = new Vector2(12, 0);

            MissionButton(parent, titleKey, missionId, prerequisiteId, Paper, action, Ink, prerequisiteTitleKey);
        }

        private void RunMapObjective(MapObjectiveDefinition objective)
        {
            if (objective == null) return;
            switch (objective.Action)
            {
                case MapObjectiveAction.StoryCardTarget: OpenStoryCard(objective.WorldId, objective.TargetId); break;
                case MapObjectiveAction.ExploreWorld: OpenWorld(objective.WorldId); break;
                case MapObjectiveAction.OpenPassport: Navigate(Page.Passport); break;
                case MapObjectiveAction.HoiAnArchitecture: StartLevel(LevelCatalog.HoiAn); break;
                case MapObjectiveAction.CaoLau: StartRecipe(RecipeCatalog.CaoLau); break;
                case MapObjectiveAction.HoiAnExchange: StartMatch(MatchCatalog.HoiAn); break;
                case MapObjectiveAction.JeonjuArchitecture: StartLevel(LevelCatalog.Jeonju); break;
                case MapObjectiveAction.Bibimbap: StartRecipe(RecipeCatalog.Bibimbap); break;
                case MapObjectiveAction.JeonjuExchange: StartMatch(MatchCatalog.Jeonju); break;
                case MapObjectiveAction.LanternCraft: StartLanternCraft(); break;
                case MapObjectiveAction.HanjiCraft: StartHanjiCraft(); break;
                case MapObjectiveAction.Festival: StartFinale(); break;
            }
        }

        private GameObject StoryPrimary(Transform parent, string label, Action action)
        {
            var btn = Button(parent, label, Jade, action);
            var border = btn.GetComponent<Outline>();
            if (border != null)
            {
                border.effectColor = GoldGlow;
                border.effectDistance = new Vector2(2f, -2f);
            }
            return btn;
        }

        private void DrawLocationTargets(Transform parent, string worldId)
        {
            CollectionCopy(parent, T("card_location_hint"), 26, Muted, TextAnchor.MiddleCenter);
            foreach (var definition in WorldCatalog.Get(worldId).Interactables)
            {
                var target = definition;
                bool available = (worldId != "jeonju" || IsJeonjuUnlocked(profile)) &&
                    (string.IsNullOrEmpty(target.RequiredImprintId) || StoryProgressService.HasImprint(profile, target.RequiredImprintId));
                CompactButton(parent, T(target.NameKey) + (available ? "" : " · " + T("locked")), available ? Paper : Border,
                    () => OpenTargetCard(worldId, target), Ink).GetComponent<UnityEngine.UI.Button>().interactable = available;
            }
        }

        private void OpenTargetCard(string worldId, WorldInteractableDefinition target)
        {
            if (target == null || (worldId == "jeonju" && !IsJeonjuUnlocked(profile)) ||
                (!string.IsNullOrEmpty(target.RequiredImprintId) && !StoryProgressService.HasImprint(profile, target.RequiredImprintId))) return;
            storyWorldId = worldId;
            storyTarget = target;
            koreanBackdrop = worldId == "jeonju";
            Navigate(Page.StoryTarget);
        }

        private void StoryPortrait(Transform parent, WorldInteractableDefinition target)
        {
            var portrait = NewRect("Story portrait", parent);
            SetHeight(portrait, target.IsNpc ? 210 : 170);
            var image = portrait.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>(target.SpriteResourcePath ?? WorldCatalog.Get(storyWorldId).BackgroundResourcePath);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void DrawStoryTarget(Transform parent)
        {
            if (storyTarget == null) return;
            var card = JourneyCard(parent);
            card.name = "Story target surface";
            StoryPortrait(card, storyTarget);
            CollectionCopy(card, T(storyTarget.NameKey), 36, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(card, T("card_location_hint"), 26, Muted, TextAnchor.MiddleCenter);
            StoryPrimary(card, T("card_open_target"), () => OpenStoryCard(storyWorldId, storyTarget.Id));
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void DrawStoryCard(Transform parent)
        {
            if (storyCardInteraction == null) return;
            var target = storyCardInteraction.Target;
            var card = JourneyCard(parent);
            card.name = "Story dialogue surface";
            StoryPortrait(card, target);
            CollectionCopy(card, T(target.NameKey), 34, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (target.SpeakerKey != target.NameKey)
                CollectionCopy(card, T(target.SpeakerKey), 24, Jade, TextAnchor.MiddleCenter);
            CollectionCopy(card, T(storyAskMore ? target.OptionalDialogueKey : storyCardInteraction.DialogueKey), 27, Ink);
            bool activity = !string.IsNullOrEmpty(target.ActivityId);
            StoryPrimary(card, activity ? T(target.ActivityId == "lantern-craft" ? "lantern_craft_map" : "hanji_craft_map") : T("card_continue"),
                activity ? () => HandleWorldActivity(target.ActivityId) : () => Navigate(Page.Map));
            if (!string.IsNullOrEmpty(target.OptionalDialogueKey) && !storyAskMore)
                CompactButton(card, T("card_ask_more"), Paper, () => { storyAskMore = true; Show(); }, Ink);
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void OpenStoryCard(string worldId, string targetId)
        {
            if (profile == null || store == null || string.IsNullOrEmpty(targetId)) return;
            // WorldCatalog.Get falls back to Hoi An for unknown IDs; card routes must resolve exactly.
            WorldDefinition world = WorldCatalog.Get(worldId);
            if (world.Id != worldId) return;
            if (worldId == "jeonju" && !IsJeonjuUnlocked(profile))
            {
                Toast(T("jeonju_not_unlocked"));
                return;
            }
            WorldInteractableDefinition target = null;
            foreach (var definition in world.Interactables)
                if (definition.Id == targetId) { target = definition; break; }
            if (target == null) return;

            var candidate = new PlayerProfile();
            candidate.CopyFrom(profile);
            WorldInteractionResult interaction = WorldInteractionService.Interact(candidate, target);
            if (interaction == null || !TryPersistCandidate(candidate)) return;
            storyCardInteraction = interaction;
            storyTarget = target;
            storyWorldId = worldId;
            storyAskMore = false;
            koreanBackdrop = worldId == "jeonju";
            Navigate(Page.StoryCard);
        }

        private bool TryPersistCandidate(PlayerProfile candidate)
        {
            if (!store.TrySave(candidate))
            {
                notice = T("save_error");
                Show();
                return false;
            }
            profile.CopyFrom(candidate);
            return true;
        }

        private void OpenWorld(string worldId)
        {
            if (worldId == "jeonju" && !IsJeonjuUnlocked(profile))
            {
                Toast(T("jeonju_not_unlocked"));
                return;
            }

            try
            {
                worldReturnPage = Page.Map;
                if (worldSession == null) worldSession = GetComponent<WalkableWorldSession>();
                if (worldSession == null) worldSession = gameObject.AddComponent<WalkableWorldSession>();
                safeRoot.gameObject.SetActive(false);
                dioramaStage?.SetStageVisible(false);
                activityDiorama?.SetVisible(false);
                worldSession.Open(worldId, profile, store, ReturnFromWorld, HandleWorldActivity, OpenPassportFromWorld);
                bool koreanWorld = worldId == "jeonju";
                audioManager?.SetEnvironment(koreanWorld);
                PlayStageAudio(Mathf.Clamp(profile.chapterIndex, 1, 3), koreanWorld);
            }
            catch (Exception error)
            {
                safeRoot.gameObject.SetActive(true);
                dioramaStage?.SetStageVisible(true);
                activityDiorama?.SetVisible(false);
                Debug.LogException(error);
                notice = T("world_open_error");
                Show();
            }
        }

        private void ReturnFromWorld()
        {
            if (safeRoot != null) safeRoot.gameObject.SetActive(true);
            dioramaStage?.SetStageVisible(true);
            page = worldReturnPage;
            worldReturnPage = Page.Map;
            notice = worldSession != null && !worldSession.LastSaveSucceeded ? T("save_error") : null;
            Show();
        }

        private void OpenPassportFromWorld()
        {
            worldReturnPage = Page.Passport;
            if (worldSession != null && worldSession.IsOpen) worldSession.Close();
        }

        private void RecordTutorialPassportView()
        {
            if (profile == null || store == null ||
                StoryProgressService.HasFlag(profile, "tutorial-passport-viewed") ||
                !StoryProgressService.HasFlag(profile, "lantern-display-seen")) return;

            var candidate = new PlayerProfile();
            candidate.CopyFrom(profile);
            StoryProgressService.AddFlag(candidate, "tutorial-passport-viewed");
            if (!store.TrySave(candidate))
            {
                notice = T("save_error");
                return;
            }
            profile.CopyFrom(candidate);
        }

        private void HandleWorldActivity(string activityId)
        {
            if (activityId == "lantern-craft") StartLanternCraft();
            else if (activityId == "hanji-craft") StartHanjiCraft();
        }

        private bool IsJeonjuUnlocked(PlayerProfile target) =>
            StoryProgressService.HasFlag(target, "jeonju-unlocked") || HasHoiAnStoryMilestone(target);

        private static bool HasHoiAnStoryMilestone(PlayerProfile target) =>
            ProgressService.HasStamp(target, LevelCatalog.HoiAn.Id) &&
            ProgressService.HasStamp(target, RecipeCatalog.CaoLau.Id) &&
            ProgressService.HasStamp(target, MatchCatalog.HoiAn.Id);

        private void MigrateWorldUnlockFromExistingProgress(PlayerProfile target)
        {
            if (!HasHoiAnStoryMilestone(target)) return;
            bool changed = StoryProgressService.AddFlag(target, "jeonju-unlocked");
            changed |= StoryProgressService.AdvanceToChapter(target, 3);
            if (changed && !store.TrySave(target)) Debug.LogError("DẤU XỨ: Could not save the Jeonju story unlock migration.");
        }

        private void RecordMissionWorldMark(PlayerProfile target, string missionId)
        {
            if (missionId == LevelCatalog.HoiAn.Id)
            {
                StoryProgressService.RecordInteraction(target, "hoian-architecture-seen", "stamp-hoian-architecture");
                InventoryService.RecordChuaCauDiscovery(target);
            }
            else if (missionId == RecipeCatalog.CaoLau.Id)
            {
                StoryProgressService.RecordInteraction(target, "cao-lau-discovered", "stamp-cao-lau");
                InventoryService.RecordCaoLauDiscovery(target);
            }
            else if (missionId == MatchCatalog.HoiAn.Id)
                StoryProgressService.RecordInteraction(target, "hoian-cultural-exchange-complete", "stamp-hoian-exchange");
            else if (missionId == LevelCatalog.Jeonju.Id)
            {
                StoryProgressService.RecordInteraction(target, "hanok-village-observed", "stamp-jeonju-hanok");
                InventoryService.AddCultureCard(target, InventoryService.HanokCardId);
            }
            else if (missionId == RecipeCatalog.Bibimbap.Id)
            {
                StoryProgressService.RecordInteraction(target, "bibimbap-discovered", "stamp-bibimbap");
                InventoryService.AddCultureCard(target, InventoryService.BibimbapCardId);
            }
            else if (missionId == MatchCatalog.Jeonju.Id)
                StoryProgressService.RecordInteraction(target, "jeonju-cultural-exchange-complete", "stamp-jeonju-exchange");

            if (HasHoiAnStoryMilestone(target))
            {
                StoryProgressService.AddFlag(target, "jeonju-unlocked");
                StoryProgressService.AdvanceToChapter(target, 3);
            }
        }

        private void DrawWorldImprints(Transform parent)
        {
            int count = profile.worldImprints == null ? 0 : profile.worldImprints.Count;
            var card = JourneyCard(parent);
            CollectionCopy(card, T("current_chapter"), 27, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(card, T("chapter_title_" + Mathf.Clamp(profile.chapterIndex, 1, 4)), 30, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(card, T("world_imprints") + ": " + count, 26, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (count == 0)
            {
                CollectionCopy(card, T("no_world_imprints"), 26, Muted, TextAnchor.MiddleCenter);
                return;
            }
            foreach (string imprint in profile.worldImprints)
                CollectionCopy(card, T("stamp") + " · " + T(ImprintNameKey(imprint)), 26, Ink, TextAnchor.MiddleCenter);
        }

        private static string ImprintNameKey(string id)
        {
            if (id == "dau-xu-book-discovered") return "imprint_book";
            if (id == "stamp-chuacau") return "imprint_chuacau";
            if (id == "stamp-hanok") return "imprint_hanok";
            if (id == "stamp-hoian-architecture") return "imprint_hoian_architecture";
            if (id == "stamp-cao-lau") return "imprint_cao_lau";
            if (id == "stamp-hoian-exchange") return "imprint_hoian_exchange";
            if (id == "stamp-jeonju-hanok") return "imprint_jeonju_hanok";
            if (id == "stamp-bibimbap") return "imprint_bibimbap";
            if (id == "stamp-jeonju-exchange") return "imprint_jeonju_exchange";
            return "imprint_unknown";
        }

        private void DrawWardrobe(Transform parent)
        {
            CollectionHeader(parent, "wardrobe", "collection_wardrobe_hint", T("coins") + ": " + profile.coins);
            for (int i = 0; i < OutfitCatalog.All.Length; i++)
            {
                var outfit = OutfitCatalog.All[i];
                var card = JourneyCard(parent);
                var preview = NewRect("Outfit preview", card);
                SetHeight(preview, 150);
                DrawAvatar(preview, OutfitCatalog.Get(profile.outfitId).Avatar, outfit.Id);
                CollectionCopy(card, T(outfit.NameKey), 29, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                CollectionCopy(card, outfit.IsOwned(profile) ? T("outfit_owned") : T("outfit_price") + ": " + outfit.Price, 26, Muted, TextAnchor.MiddleCenter);
                if (outfit.IsOwned(profile)) CompactButton(card, profile.outfitId == outfit.Id ? T("outfit_selected") : T("outfit_wear"), Jade, () => SetOutfit(outfit.Id));
                else CompactButton(card, T("outfit_unlock"), outfit.Price <= profile.coins ? Lantern : Border, () => UnlockOutfit(outfit.Id), Ink);
            }
            CompactButton(parent, T("nav_back_passport"), Paper, () => Navigate(Page.Passport), Ink);
        }

        private void SetOutfit(int id)
        {
            profile.outfitId = id;
            profile.avatarIndex = OutfitCatalog.Get(id).Avatar;
            if (store.TrySave(profile)) Show(); else { notice = T("save_error"); Show(); }
        }

        private void UnlockOutfit(int id)
        {
            if (!OutfitCatalog.TryUnlock(profile, id)) { notice = T("outfit_not_enough"); Show(); return; }
            SetOutfit(id);
        }

        private void DrawSettings(Transform parent)
        {
            CollectionCopy(parent, T("settings"), 38, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(parent, T("settings_intro"), 26, Muted, TextAnchor.MiddleCenter);
            var language = JourneyCard(parent);
            language.name = "Settings language group";
            CollectionCopy(language, T("settings_language"), 29, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            CollectionCopy(language, T("settings_language_hint"), 26, Muted);
            var languages = NewRect("Settings language choices", language);
            SetHeight(languages, GameUiTheme.ButtonHeight);
            AddHorizontal(languages);
            Button(languages, T("language_vi") + (profile.language == "vi" ? " · " + T("selected_state") : ""), profile.language == "vi" ? Lantern : Paper, () => ChangeLanguage("vi"), Ink);
            Button(languages, T("language_ko") + (profile.language == "ko" ? " · " + T("selected_state") : ""), profile.language == "ko" ? Lantern : Paper, () => ChangeLanguage("ko"), Ink);
            var sound = JourneyCard(parent);
            sound.name = "Settings audio group";
            CollectionCopy(sound, T("settings_sound"), 29, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            if (audioManager == null) audioManager = GameAudioManager.For(gameObject);
            CollectionCopy(sound, T("settings_sound_state") + ": " + T(audioManager.Muted ? "off" : "on"), 26, Jade);
            AudioVolumeSlider(sound, T("settings_music_volume"), audioManager.MusicVolume, audioManager.SetMusicVolume);
            AudioVolumeSlider(sound, T("settings_sfx_volume"), audioManager.SfxVolume, audioManager.SetSfxVolume);
            AudioVolumeSlider(sound, T("settings_ambience_volume"), audioManager.AmbienceVolume, audioManager.SetAmbienceVolume);
            CompactButton(sound, T(audioManager.Muted ? "sound_muted" : "sound_unmuted"), Lantern, () =>
            {
                bool mute = !audioManager.Muted;
                audioManager.SetMuted(mute);
                if (mute) StoryNarration.Stop();
                Show();
            }, Ink);

            // Traveler Account Management Group (Đăng xuất & Đăng nhập lại)
            var account = JourneyCard(parent);
            account.name = "Settings account group";
            var accBorder = account.GetComponent<Outline>();
            if (accBorder != null)
            {
                accBorder.effectColor = GoldGlow;
                accBorder.effectDistance = new Vector2(1.5f, -1.5f);
            }
            CollectionCopy(account, "👤 " + (profile != null && profile.language == "ko" ? "여행자 계정 관리" : "Tài khoản nhà du hành"), 29, HeaderWood, TextAnchor.MiddleLeft, FontStyle.Bold);
            string curName = profile != null && !string.IsNullOrEmpty(profile.playerName) ? profile.playerName : (profile != null && profile.language == "ko" ? "새 여행자" : "Lữ khách mới");
            CollectionCopy(account, (profile != null && profile.language == "ko" ? "현재 계정: " : "Lữ khách hiện tại: ") + curName + (profile != null && profile.created ? "  (✓ Đã lưu)" : ""), 25, Jade);
            CollectionCopy(account, profile != null && profile.language == "ko" ? "새로운 여행자로 다시 시작하거나 프로필을 변경할 수 있습니다." : "Bạn có thể đăng xuất để tạo hành trình mới hoặc đổi thông tin lữ khách.", 22, Muted);

            var accButtons = NewRect("Settings account action row", account);
            SetHeight(accButtons, GameUiTheme.ButtonHeight);
            AddHorizontal(accButtons);

            // Nút 1: Đổi tài khoản / Đăng nhập lại
            var reloginBtn = CompactButton(accButtons, "🔄 " + (profile != null && profile.language == "ko" ? "계정 전환 / 다시 로그인" : "Đổi tài khoản / Đăng nhập"), Paper, () =>
            {
                editingProfile = true;
                profileEditReturnPage = Page.Settings;
                draftName = profile != null ? profile.playerName : "";
                draftAvatar = profile != null ? profile.avatarIndex : 0;
                draftFaceStyle = profile != null ? profile.faceStyle : 0;
                draftHairStyle = profile != null ? profile.hairStyle : 0;
                draftBodyStyle = profile != null ? profile.bodyStyle : 0;
                draftAccessoryStyle = profile != null ? profile.accessoryStyle : 0;
                draftOutfit = profile != null ? profile.outfitId : 0;
                Navigate(Page.Create);
            }, Ink);
            var reloginLyt = reloginBtn.GetComponent<LayoutElement>();
            if (reloginLyt != null) reloginLyt.flexibleWidth = 1f;

            // Nút 2: Đăng xuất (Tạo lữ khách mới)
            var logoutBtn = CompactButton(accButtons, "🚪 " + (profile != null && profile.language == "ko" ? "로그아웃" : "Đăng xuất"), new Color(0.95f, 0.90f, 0.88f), () =>
            {
                if (profile != null && !string.IsNullOrEmpty(profile.playerName)) store.TrySave(profile);
                string currentLang = profile != null ? profile.language : "vi";
                profile = new PlayerProfile { language = currentLang, created = false };
                draftName = "";
                editingProfile = false;
                Navigate(Page.Create);
                Toast(currentLang == "ko" ? "성공적으로 로그아웃되었습니다." : "Đã đăng xuất tài khoản thành công.");
            }, Lantern);
            var logoutLyt = logoutBtn.GetComponent<LayoutElement>();
            if (logoutLyt != null) logoutLyt.flexibleWidth = 1f;

            CompactButton(parent, T("nav_back_previous"), Paper, () => Navigate(settingsReturnPage), Ink);
        }

        private void AudioVolumeSlider(Transform parent, string label, float value, UnityAction<float> changed)
        {
            Label(parent, label, 25, 42, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var root = NewRect("Audio volume slider - " + label, parent);
            SetHeight(root, 76);

            var track = NewRect("Track", root);
            track.anchorMin = new Vector2(.045f, .42f);
            track.anchorMax = new Vector2(.955f, .58f);
            track.offsetMin = track.offsetMax = Vector2.zero;
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = Border;
            trackImage.raycastTarget = true;

            var fillArea = NewRect("Fill Area", root);
            fillArea.anchorMin = track.anchorMin;
            fillArea.anchorMax = track.anchorMax;
            fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
            var fill = NewRect("Fill", fillArea);
            Stretch(fill);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = Jade;
            fillImage.raycastTarget = false;

            var handleArea = NewRect("Handle Area", root);
            handleArea.anchorMin = new Vector2(.045f, .05f);
            handleArea.anchorMax = new Vector2(.955f, .95f);
            handleArea.offsetMin = handleArea.offsetMax = Vector2.zero;
            var handle = NewRect("Handle", handleArea);
            handle.anchorMin = handle.anchorMax = new Vector2(.5f, .5f);
            handle.sizeDelta = new Vector2(34, 34);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.sprite = CircleSprite();
            handleImage.color = Lantern;
            handleImage.raycastTarget = true;

            var slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            slider.onValueChanged.AddListener(changed);
        }

        private void StartFinale()
        {
            if (profile.mainStoryCompleted)
            {
                Navigate(Page.FinaleResult);
                return;
            }
            if (!FirstJourneyComplete())
            {
                notice = T("finale_locked_hint");
                Navigate(Page.Map);
                return;
            }

            var candidate = new PlayerProfile();
            candidate.CopyFrom(profile);
            StoryProgressService.AdvanceToChapter(candidate, 4);
            StoryProgressService.RecordInteraction(candidate, "festival-invitation-received", "festival-invitation");
            if (!store.TrySave(candidate))
            {
                notice = T("save_error");
                Show();
                return;
            }
            profile = candidate;
            festivalRun = new FestivalPreparationRun(profile);
            koreanBackdrop = false;
            audioManager?.SetEnvironment(false);
            PlayStageAudio(3, false);
            audioManager?.PlayCue(GameSoundCue.Unlock);
            StoryNarration.Stop();
            Navigate(Page.Finale);
        }

        private void DrawFinale(Transform parent)
        {
            if (festivalRun == null) festivalRun = new FestivalPreparationRun(profile);
            Header(parent, T("finale_title"));
            ActivityGuide(parent, "activity_festival_phase", "activity_festival_hint");
            ProgressStrip(parent, festivalRun.PlacedCount, FestivalPreparationRun.StationCount, T("activity_marks_placed"));
            ProgressStrip(parent, festivalRun.DecoratedCount, FestivalPreparationRun.StationCount, T("activity_stations_decorated"));
            ProgressStrip(parent, festivalRun.ConnectedCount, FestivalPreparationRun.LinkCount, T("festival_links_progress"));
            Artwork(parent, TravelArtworkKind.Friendship, 120);

            var markCard = JourneyCard(parent);
            CollectionCopy(markCard, T("festival_marks_title"), 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            int markRows = (FestivalPreparationRun.MarkCount + 2) / 3;
            for (int rowIndex = 0; rowIndex < markRows; rowIndex++)
            {
                var choices = NewRect("Festival cultural memory choices", markCard);
                SetHeight(choices, 82);
                AddHorizontal(choices);
                for (int column = 0; column < 3; column++)
                {
                    int mark = rowIndex * 3 + column;
                    if (mark >= FestivalPreparationRun.MarkCount) break;
                    int selectedMark = mark;
                    bool selected = festivalRun.SelectedMarkIndex == mark;
                    var choice = Button(choices, T("finale_mark_" + mark) + (selected ? " · " + T("selected_state") : ""),
                        selected ? Jade : Paper, () => SelectFestivalMark(selectedMark), selected ? Color.white : Ink);
                    choice.GetComponent<UnityEngine.UI.Button>().interactable = !festivalRun.IsMarkPlaced(mark);
                }
            }

            var decorationCard = JourneyCard(parent);
            CollectionCopy(decorationCard, T("festival_decor_title"), 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(decorationCard, T("festival_decor_hint"), 23, Muted, TextAnchor.MiddleCenter);
            var decorations = NewRect("Festival decoration choices", decorationCard);
            SetHeight(decorations, 92);
            AddHorizontal(decorations);
            for (int i = 0; i < FestivalPreparationRun.DecorationCount; i++)
            {
                string decorationId = FestivalPreparationRun.GetDecorationId(i);
                bool selected = festivalRun.SelectedDecorationId == decorationId;
                Button(decorations, T("festival_decoration_" + decorationId) + (selected ? " · " + T("selected_state") : ""),
                    selected ? Lantern : Paper, () => SelectFestivalDecoration(decorationId), Ink);
            }

            for (int station = 0; station < FestivalPreparationRun.StationCount; station++)
                DrawFestivalStation(parent, station);

            if (festivalRun.PlacedCount == FestivalPreparationRun.StationCount &&
                festivalRun.DecoratedCount == FestivalPreparationRun.StationCount)
                DrawFestivalConnections(parent);
            else
                CollectionCopy(parent, T("festival_finish_stations_hint"), 24, Muted, TextAnchor.MiddleCenter);
            if (festivalRun.Complete)
                Button(parent, T("festival_finish_action"), Jade, CompleteFestival);
            CompactButton(parent, T("back_map"), Paper, () => { festivalRun = null; Navigate(Page.Map); }, Ink);
        }

        private void DrawFestivalStation(Transform parent, int stationIndex)
        {
            string stationId = FestivalPreparationRun.GetStationId(stationIndex);
            var card = JourneyCard(parent);
            CollectionCopy(card, T("festival_station_" + stationId), 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (festivalRun.IsStationPlaced(stationIndex))
            {
                int mark = festivalRun.GetPlacedMarkForStation(stationIndex);
                CollectionCopy(card, T("stamp") + " · " + T("finale_mark_" + mark), 24, Ink, TextAnchor.MiddleCenter);
            }
            else CollectionCopy(card, T("festival_station_empty"), 23, Muted, TextAnchor.MiddleCenter);

            if (!festivalRun.IsStationPlaced(stationIndex))
            {
                var place = Button(card, T("festival_place_action"), Paper,
                    () => PlaceFestivalMark(stationIndex), Ink);
                place.GetComponent<UnityEngine.UI.Button>().interactable = festivalRun.SelectedMarkIndex >= 0;
            }
            else if (festivalRun.IsStationDecorated(stationIndex))
            {
                CollectionCopy(card, T("festival_station_decorated") + ": " +
                    T("festival_decoration_" + festivalRun.GetDecorationForStation(stationIndex)), 23, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            else
            {
                var decorate = Button(card, T("festival_decorate_action"), Lantern,
                    () => DecorateFestivalStation(stationIndex), Ink);
                decorate.GetComponent<UnityEngine.UI.Button>().interactable = !string.IsNullOrEmpty(festivalRun.SelectedDecorationId);
            }
        }

        private void DrawFinaleResult(Transform parent)
        {
            Header(parent, T("finale_reveal_title"));
            ActivityGuide(parent, "activity_saved", "activity_finale_result_hint");
            Artwork(parent, TravelArtworkKind.Friendship, 190);
            var festivalCard = JourneyCard(parent);
            var festivalGroup = festivalCard.gameObject.AddComponent<CanvasGroup>();
            festivalGroup.alpha = 0f;
            CollectionCopy(festivalCard, T("festival_scene_title"), 31, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(festivalCard, T("festival_corners_complete"), 25, Ink, TextAnchor.MiddleCenter);

            var friendsCard = JourneyCard(parent);
            var friendsGroup = friendsCard.gameObject.AddComponent<CanvasGroup>();
            friendsGroup.alpha = 0f;
            CollectionCopy(friendsCard, T("festival_friends_title"), 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            var guests = NewRect("Festival returning friends", friendsCard);
            // The row inherits the tallest measured caption, including wrapped Korean names.
            AddHorizontal(guests);
            AddFestivalFriend(guests, "character_nhung", "Art/Characters/Nhung");
            AddFestivalFriend(guests, "character_gabo", "Art/Characters/GaBo");
            AddFestivalFriend(guests, "character_lan", "Art/Characters/Lan");
            AddFestivalFriend(guests, "character_kim", "Art/Characters/BKim");

            var endingCard = JourneyCard(parent);
            var endingGroup = endingCard.gameObject.AddComponent<CanvasGroup>();
            endingGroup.alpha = 0f;
            CollectionCopy(endingCard, T("finale_reveal"), 29, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            ActivityNarration(endingCard, T("finale_reveal"));
            CollectionCopy(parent, T("main_story_completed"), 27, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button(parent, T("continue_free_exploration"), Jade, () => Navigate(Page.Map));
            CompactButton(parent, T("passport"), Paper, () => Navigate(Page.Passport), Ink);
            finaleReveal = StartCoroutine(RevealFestivalEnding(festivalGroup, friendsGroup, endingGroup));
        }

        private void AddFestivalFriend(Transform parent, string nameKey, string spritePath)
        {
            var friend = NewRect("Festival friend - " + nameKey, parent);
            var size = friend.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = 130;
            size.minWidth = 100;
            var layout = friend.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var portrait = NewRect("Portrait", friend);
            SetHeight(portrait, 102);
            var image = portrait.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>(spritePath);
            image.preserveAspect = true;
            image.raycastTarget = false;
            var caption = CollectionCopy(friend, T(nameKey), 26, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            // One line measures 37 units; wrapped VI/KO names measure 75 at phone widths.
            caption.GetComponent<LayoutElement>().minHeight = 44;
        }

        private IEnumerator RevealFestivalEnding(CanvasGroup festival, CanvasGroup friends, CanvasGroup ending)
        {
            yield return RevealFestivalGroup(festival, 0.18f);
            yield return RevealFestivalGroup(friends, 0.22f);
            yield return RevealFestivalGroup(ending, 0.28f);
            finaleReveal = null;
        }

        private IEnumerator RevealFestivalGroup(CanvasGroup group, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (group == null) yield break;
            RectTransform rect = group.transform as RectTransform;
            Vector3 startScale = Vector3.one * 0.96f;
            rect.localScale = startScale;
            float elapsed = 0f;
            while (group != null && elapsed < 0.45f)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / 0.45f);
                group.alpha = progress;
                if (rect != null) rect.localScale = Vector3.Lerp(startScale, Vector3.one, progress);
                yield return null;
            }
            if (group != null) group.alpha = 1f;
            if (rect != null) rect.localScale = Vector3.one;
        }

        private void DrawFestivalConnections(Transform parent)
        {
            var card = JourneyCard(parent);
            CollectionCopy(card, T("festival_link_title"), 29, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(card, T("festival_link_hint"), 24, Ink, TextAnchor.MiddleCenter);
            CollectionCopy(card, T("festival_links_progress") + ": " + festivalRun.ConnectedCount + "/" + FestivalPreparationRun.LinkCount, 25, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            var vietnam = NewRect("Vietnam activity link choices", card);
            SetHeight(vietnam, 98);
            AddHorizontal(vietnam);
            for (int i = 0; i < FestivalPreparationRun.LinkCount; i++)
            {
                string activityId = FestivalPreparationRun.GetVietnamActivityId(i);
                bool connected = festivalRun.IsLinkConnected(FestivalPreparationRun.GetLinkId(i));
                bool selected = festivalRun.SelectedVietnamActivityId == activityId;
                var choice = Button(vietnam, T("festival_activity_vi_" + activityId) + (selected ? " · " + T("selected_state") : ""),
                    selected ? Jade : Paper, () => SelectFestivalActivity(true, activityId), selected ? Color.white : Ink);
                choice.GetComponent<UnityEngine.UI.Button>().interactable = !connected;
            }
            var korea = NewRect("Korea activity link choices", card);
            SetHeight(korea, 98);
            AddHorizontal(korea);
            for (int i = 0; i < FestivalPreparationRun.LinkCount; i++)
            {
                string activityId = FestivalPreparationRun.GetKoreaActivityId(i);
                bool connected = festivalRun.IsLinkConnected(FestivalPreparationRun.GetLinkId(i));
                bool selected = festivalRun.SelectedKoreaActivityId == activityId;
                var choice = Button(korea, T("festival_activity_ko_" + activityId) + (selected ? " · " + T("selected_state") : ""),
                    selected ? Lantern : Paper, () => SelectFestivalActivity(false, activityId), Ink);
                choice.GetComponent<UnityEngine.UI.Button>().interactable = !connected;
            }
            for (int i = 0; i < FestivalPreparationRun.LinkCount; i++)
            {
                string pairId = FestivalPreparationRun.GetLinkId(i);
                CollectionCopy(card, T("festival_pair_" + pairId) + " · " + T(festivalRun.IsLinkConnected(pairId) ? "completed_state" : "pending_state"), 23, festivalRun.IsLinkConnected(pairId) ? Jade : Muted, TextAnchor.MiddleCenter);
            }
            Button(card, T("festival_connect"), festivalRun.Complete ? Paper : Jade,
                ConnectFestivalActivities, festivalRun.Complete ? Ink : Color.white);
        }

        private void SelectFestivalMark(int markIndex)
        {
            if (festivalRun != null && festivalRun.SelectMark(markIndex)) Show();
        }

        private void PlaceFestivalMark(int stationIndex)
        {
            if (festivalRun == null) return;
            FestivalPlacementResult result = festivalRun.TryPlaceAt(stationIndex);
            if (result == FestivalPlacementResult.MissingSelection) { notice = T("festival_select_mark_first"); Show(); return; }
            if (result == FestivalPlacementResult.WrongStation) { notice = T("finale_wrong"); Show(); return; }
            if (result != FestivalPlacementResult.Placed) { notice = T("festival_station_already_ready"); Show(); return; }
            if (!SaveFestivalProgress(FestivalPreparationRun.GetPlacementStoryFlagId(stationIndex),
                    FestivalPreparationRun.GetPlacementImprintId(stationIndex))) return;
            notice = T("festival_mark_placed");
            Show();
        }

        private void SelectFestivalDecoration(string decorationId)
        {
            if (festivalRun != null && festivalRun.SelectDecoration(decorationId)) Show();
        }

        private void DecorateFestivalStation(int stationIndex)
        {
            if (festivalRun == null) return;
            FestivalDecorationResult result = festivalRun.TryDecorateAt(stationIndex);
            if (result == FestivalDecorationResult.StationNotReady) { notice = T("festival_place_mark_first"); Show(); return; }
            if (result == FestivalDecorationResult.MissingSelection) { notice = T("festival_select_decor_first"); Show(); return; }
            if (result != FestivalDecorationResult.Decorated) { notice = T("festival_station_already_ready"); Show(); return; }
            string decorationId = festivalRun.GetDecorationForStation(stationIndex);
            if (!SaveFestivalProgress(FestivalPreparationRun.GetDecorationStoryFlagId(stationIndex, decorationId),
                    FestivalPreparationRun.GetDecorationImprintId(stationIndex, decorationId))) return;
            notice = T("festival_decor_placed");
            Show();
        }

        private void SelectFestivalActivity(bool vietnam, string activityId)
        {
            if (festivalRun == null) return;
            FestivalSelectionResult result = vietnam
                ? festivalRun.SelectVietnamActivity(activityId)
                : festivalRun.SelectKoreaActivity(activityId);
            if (result == FestivalSelectionResult.Selected) Show();
        }

        private void ConnectFestivalActivities()
        {
            if (festivalRun == null) return;
            string pairId = null;
            for (int i = 0; i < FestivalPreparationRun.LinkCount; i++)
            {
                if (festivalRun.SelectedVietnamActivityId == FestivalPreparationRun.GetVietnamActivityId(i) &&
                    festivalRun.SelectedKoreaActivityId == FestivalPreparationRun.GetKoreaActivityId(i))
                {
                    pairId = FestivalPreparationRun.GetLinkId(i);
                    break;
                }
            }
            FestivalConnectionResult result = festivalRun.TryConnectActivities();
            if (result == FestivalConnectionResult.MissingVietnamActivity || result == FestivalConnectionResult.MissingKoreaActivity)
            {
                notice = T("festival_select_both_activities");
                Show();
                return;
            }
            if (result == FestivalConnectionResult.WrongPair)
            {
                notice = T("festival_wrong_pair");
                Show();
                return;
            }
            if (result != FestivalConnectionResult.Connected)
            {
                notice = T("festival_pair_already_connected");
                Show();
                return;
            }

            if (string.IsNullOrEmpty(pairId))
            {
                notice = T("festival_save_error");
                festivalRun = new FestivalPreparationRun(profile);
                Show();
                return;
            }
            if (!SaveFestivalProgress(FestivalPreparationRun.GetConnectionStoryFlagId(pairId),
                    FestivalPreparationRun.GetConnectionImprintId(pairId))) return;
            if (festivalRun.Complete) CompleteFestival();
            else
            {
                notice = T("festival_pair_connected");
                Show();
            }
        }

        private bool SaveFestivalProgress(string storyFlag, string imprint)
        {
            var candidate = new PlayerProfile();
            candidate.CopyFrom(profile);
            StoryProgressService.RecordInteraction(candidate, storyFlag, imprint);
            if (!store.TrySave(candidate))
            {
                festivalRun = new FestivalPreparationRun(profile);
                notice = T("festival_save_error");
                Show();
                return false;
            }
            profile = candidate;
            festivalRun = new FestivalPreparationRun(profile);
            return true;
        }

        private void CompleteFestival()
        {
            if (festivalRun == null || !festivalRun.Complete) return;
            var candidate = new PlayerProfile();
            candidate.CopyFrom(profile);
            StoryProgressService.CompleteMainStory(candidate);
            if (!store.TrySave(candidate))
            {
                notice = T("save_error");
                Show();
                return;
            }
            profile = candidate;
            audioManager?.PlayCue(GameSoundCue.FestivalComplete);
            Navigate(Page.FinaleResult);
        }

        private void StartLevel(LevelDefinition level)
        {
            StoryNarration.Stop();
            resultCommitted = false;
            koreanBackdrop = level != LevelCatalog.HoiAn;
            recipeResult = false;
            matchResult = false;
            run = new ExplorationRun(level, Environment.TickCount);
            audioManager?.SetEnvironment(koreanBackdrop);
            PlayStageAudio(1, koreanBackdrop);
            placedFragments = new bool[3];
            selectedFragment = -1;
            Navigate(Page.Dialogue);
        }

        private void NarrationButtons(Transform parent, string text)
        {
            SpeakerControl(parent, text);
        }

        private RectTransform ActivityGuide(Transform parent, string phaseKey, string hintKey)
        {
            var card = JourneyCard(parent);
            CollectionCopy(card, T(phaseKey), 28, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(card, T(hintKey), 26, Ink, TextAnchor.MiddleCenter);
            return card;
        }

        private void ActivityNarration(Transform parent, string text)
        {
            SpeakerControl(parent, text);
        }

        private void SpeakerControl(Transform parent, string text)
        {
            var row = NewRect("Dialogue narration control", parent);
            SetHeight(row, 74f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            bool speaking = StoryNarration.IsSpeaking;
            var control = Button(row, speaking ? "♫" : "♪", speaking ? Jade : Paper, () =>
            {
                if (StoryNarration.IsSpeaking) StoryNarration.Stop();
                else StoryNarration.Speak(profile.language, text);
                Show();
            }, Ink);
            var element = control.GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = 76f;
            element.flexibleWidth = 0f;
            SetHeight(control.GetComponent<RectTransform>(), 68f);
        }

        private void DrawDialogue(Transform parent)
        {
            Header(parent, T(run.Level.PlaceKey));
            ActivityGuide(parent, "activity_ready", "activity_explore_intro");
            Artwork(parent, run.Level == LevelCatalog.HoiAn ? TravelArtworkKind.HoiAn : TravelArtworkKind.Jeonju, 150);
            SpeakerBadge(parent, T(run.Level == LevelCatalog.HoiAn ? "character_nhung" : "character_gabo"));
            var card = JourneyCard(parent);
            CollectionCopy(card, T(run.Level.GuideKey), 31, Ink, TextAnchor.MiddleCenter);
            ActivityNarration(card, T(run.Level.GuideKey));
            Button(parent, T("mission_start"), Jade, () => Navigate(Page.Explore));
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void DrawExplore(Transform parent)
        {
            Header(parent, T(run.Level.PlaceKey));
            ActivityGuide(parent, "activity_search_phase", "search_hint");
            ProgressStrip(parent, run.FoundCount, 3, T("fragments"));
            var scene = Artwork(parent, run.Level == LevelCatalog.HoiAn ? TravelArtworkKind.HoiAn : TravelArtworkKind.Jeonju, 430);
            if (!string.IsNullOrEmpty(interactionFeedbackKey))
            {
                var foundFeedback = CollectionCopy(parent, T(interactionFeedbackKey), 25, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
                if (interactionFeedbackKey == "fragment_found") StartCoroutine(PopFoundFragment(foundFeedback.rectTransform));
            }
            for (int index = 0; index < run.Level.ObjectKeys.Length; index++)
            {
                int spotIndex = index;
                bool inspected = run.IsInspected(spotIndex);
                string spotKey = (run.Level == LevelCatalog.HoiAn ? "search_hoian_" : "search_jeonju_") + spotIndex;
                var spot = Button(scene, T(spotKey) + (inspected ? "\n" + T("inspected_state") : ""),
                    inspected ? Lantern : new Color(Paper.r, Paper.g, Paper.b, .96f), () => InspectPlace(spotIndex), Ink);
                spot.name = "Search spot " + spotIndex;
                var rect = spot.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(.17f + (spotIndex % 3) * .33f, spotIndex < 3 ? .28f : .72f);
                rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(168f, 82f);
                rect.anchoredPosition = Vector2.zero;
                spot.GetComponentInChildren<Text>().fontSize = 24;
                if (run.Level == LevelCatalog.HoiAn) HoiAnVisuals.DecorateLocationCard(rect);
                else KoreaVisuals.DecorateLocationCard(rect);
            }
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void InspectPlace(int index)
        {
            InspectResult result = run.Inspect(index);
            if (run.SearchComplete) { Navigate(Page.Assembly); return; }
            interactionFeedbackKey = result == InspectResult.Fragment ? "fragment_found" : result == InspectResult.Empty ? "empty_spot" : "already_searched";
            notice = T(interactionFeedbackKey);
            Show();
        }

        private static IEnumerator PopFoundFragment(RectTransform target)
        {
            if (target == null) yield break;
            var group = target.GetComponent<CanvasGroup>();
            if (group == null) group = target.gameObject.AddComponent<CanvasGroup>();
            Vector2 start = target.anchoredPosition;
            float elapsed = 0f;
            const float duration = .32f;
            while (target != null && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float punch = t < .45f ? Mathf.Lerp(.72f, 1.12f, t / .45f) : Mathf.Lerp(1.12f, 1f, (t - .45f) / .55f);
                target.localScale = Vector3.one * punch;
                target.anchoredPosition = start + Vector2.up * (24f * t);
                group.alpha = Mathf.Lerp(.7f, 1f, t);
                yield return null;
            }
            if (target != null) { target.localScale = Vector3.one; target.anchoredPosition = start + Vector2.up * 24f; }
        }

        private void DrawAssembly(Transform parent)
        {
            Header(parent, T(run.Level == LevelCatalog.HoiAn ? "assemble_hoian_title" : "assemble_jeonju_title"));
            ActivityGuide(parent, "activity_assemble_phase", "assemble_hint");
            ProgressStrip(parent, (placedFragments[0] ? 1 : 0) + (placedFragments[1] ? 1 : 0) + (placedFragments[2] ? 1 : 0), 3, T("activity_pieces_placed"));
            Artwork(parent, run.Level == LevelCatalog.HoiAn ? TravelArtworkKind.HoiAn : TravelArtworkKind.Jeonju, 190);
            CollectionCopy(parent, selectedFragment >= 0 ? T("activity_selected_piece") + " " + (selectedFragment + 1) : T("activity_select_piece"), 26, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            pieceViews = new FragmentDrag[3];
            slotViews = new Transform[3];
            CollectionCopy(parent, profile != null && profile.language == "ko" ? "◆ 수집한 조각 (아래 틀로 끌어놓으세요) ◆" : "◆ Mảnh ghép đã tìm được (Kéo vào khung bên dưới) ◆", 22, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            var pieces = NewRect("Fragments", parent);
            SetHeight(pieces, 128);
            AddHorizontal(pieces);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                if (placedFragments[i]) { Spacer(pieces, 112); continue; }
                var pieceImage = Panel("Architecture fragment " + (i + 1), pieces, selectedFragment == i ? Lantern : Paper, true);
                var piece = pieceImage.gameObject;
                Label(piece.transform, T(run.Level == LevelCatalog.HoiAn ? "hoian_arch_piece_" + i : "jeonju_arch_piece_" + i), 23, 0, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                SetHeight(piece.GetComponent<RectTransform>(), 112);
                if (run.Level == LevelCatalog.HoiAn) HoiAnVisuals.DecorateLocationCard(piece.GetComponent<RectTransform>());
                else KoreaVisuals.DecorateLocationCard(piece.GetComponent<RectTransform>());
                pieceViews[i] = piece.AddComponent<FragmentDrag>();
                pieceViews[i].PieceIndex = i;
            }
            CollectionCopy(parent, profile != null && profile.language == "ko" ? "◆ 완성할 엽서 틀 ◆" : "◆ Khung bưu thiếp cần ghép ◆", 22, GameUiTheme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            var slots = NewRect("Postcard slots", parent);
            SetHeight(slots, 128);
            AddHorizontal(slots);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var slot = Panel("Architecture target " + (i + 1), slots, placedFragments[i] ? Jade : Paper, true).gameObject;
                Label(slot.transform, placedFragments[i] ? T("slot_filled_state") : T(run.Level == LevelCatalog.HoiAn ? "hoian_arch_piece_" + i : "jeonju_arch_piece_" + i), 23, 0, placedFragments[i] ? Color.white : Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                slotViews[i] = slot.transform;
                SetHeight(slot.GetComponent<RectTransform>(), 112);
                if (run.Level == LevelCatalog.HoiAn) HoiAnVisuals.DecorateLocationCard(slot.GetComponent<RectTransform>());
                else KoreaVisuals.DecorateLocationCard(slot.GetComponent<RectTransform>());
                var drop = slot.AddComponent<FragmentDropSlot>();
                drop.SlotIndex = i;
                drop.TryDrop = TryPlace;
            }
            if (placedFragments[0] && placedFragments[1] && placedFragments[2])
                Button(parent, T("save_result"), Jade, CompleteRun);
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private static void AddHorizontal(RectTransform rect)
        {
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private RectTransform Artwork(Transform parent, TravelArtworkKind kind, float height)
        {
            var panel = Panel("Original travel illustration", parent, Paper).rectTransform;
            SetHeight(panel, height);
            TravelArtwork.Draw(panel, kind);
            if (kind == TravelArtworkKind.LocationHoiAnCaoLau || kind == TravelArtworkKind.LocationJeonjuBibimbap)
            {
                // The combined paintings own their contained light, water and blossom motion.
            }
            else if (kind == TravelArtworkKind.HoiAn || kind == TravelArtworkKind.VietnamJourney || kind == TravelArtworkKind.CaoLau)
                HoiAnVisuals.Attach(panel, kind);
            else KoreaVisuals.Attach(panel, kind);
            return panel;
        }

        private void Rule(Transform parent)
        {
            var line = Panel("Page rule", parent, Border).rectTransform;
            SetHeight(line, 3);
            line.GetComponent<Image>().raycastTarget = false;
        }

        private void Toast(string message)
        {
            var panel = Panel("Feedback toast", safeRoot, Paper).rectTransform;
            panel.anchorMin = new Vector2(.06f, 1f);
            panel.anchorMax = new Vector2(.94f, 1f);
            panel.pivot = new Vector2(.5f, 1f);
            panel.sizeDelta = new Vector2(0, 92);
            panel.anchoredPosition = new Vector2(0, -12);
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = Vermilion;
            outline.effectDistance = new Vector2(2, -2);
            var label = Label(panel, message, 26, 80, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(15, 0);
            label.rectTransform.offsetMax = new Vector2(-15, 0);
            StartCoroutine(HideToast(panel));
        }

        private static IEnumerator HideToast(RectTransform panel)
        {
            yield return new WaitForSecondsRealtime(2.2f);
            if (panel != null) Destroy(panel.gameObject);
        }

        private GameObject CompactButton(Transform parent, string text, Color color, Action action, Color? foreground = null)
        {
            var result = Button(parent, text, color, action, foreground);
            SetHeight(result.GetComponent<RectTransform>(), 74);
            return result;
        }

        private string MissionLabel(string titleKey, string levelId) =>
            T(titleKey) + (ProgressService.HasStamp(profile, levelId) ? " · " + T("completed_state") : "");

        private void MissionButton(Transform parent, string titleKey, string levelId, string prerequisiteId,
            Color color, Action action, Color? foreground = null, string prerequisiteTitleKey = null)
        {
            bool unlocked = ProgressService.HasStamp(profile, prerequisiteId) || ProgressService.HasStamp(profile, levelId);
            string requirement = prerequisiteTitleKey == null ? T("unlock_previous")
                : T("journey_requires_mission").Replace("{mission}", T(prerequisiteTitleKey));
            CompactButton(parent, unlocked ? MissionLabel(titleKey, levelId) : T(titleKey) + " · " + T("locked"),
                unlocked ? color : Border, unlocked ? action : () => Toast(requirement), unlocked ? foreground : Ink)
                .GetComponent<UnityEngine.UI.Button>().interactable = unlocked;
            if (!unlocked && prerequisiteTitleKey != null)
                Label(parent, requirement, 26, 88, Muted, TextAnchor.MiddleLeft);
        }

        private void ProgressStrip(Transform parent, int done, int total, string title)
        {
            var card = JourneyCard(parent);
            CollectionCopy(card, title + "  " + done + "/" + total, 26, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            var row = NewRect("Progress marks", card);
            SetHeight(row, 24);
            AddHorizontal(row);
            for (int i = 0; i < total; i++)
            {
                var mark = Panel("Step " + (i + 1), row, i < done ? Jade : Border).rectTransform;
                SetHeight(mark, 20);
                mark.GetComponent<Image>().raycastTarget = false;
            }
        }

        private void StampRow(Transform parent, string firstId, string firstKey, string secondId, string secondKey)
        {
            var row = NewRect("Passport stamp row", parent);
            AddHorizontal(row);
            StampTile(row, firstId, firstKey);
            StampTile(row, secondId, secondKey);
        }

        private void StampTile(Transform parent, string levelId, string titleKey)
        {
            bool earned = ProgressService.HasStamp(profile, levelId);
            var card = JourneyCard(parent);
            if (earned)
            {
                var border = card.gameObject.AddComponent<Outline>();
                border.effectColor = Vermilion;
                border.effectDistance = new Vector2(2, -2);
                CollectionCopy(card, "◆ " + T("stamp") + " ◆", 24, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
                CollectionCopy(card, T(titleKey), 26, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                CollectionCopy(card, profile != null && profile.language == "ko" ? "★ 공식 인증 발자취 ★" : "★ DẤU ẤN CHÍNH THỨC ★", 18, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            else
            {
                CollectionCopy(card, "🔒 " + T("locked"), 24, Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
                CollectionCopy(card, T(titleKey), 26, Muted, TextAnchor.MiddleCenter, FontStyle.Normal);
            }
        }

        private bool HasAnyStamp(params string[] levelIds)
        {
            foreach (string id in levelIds)
                if (ProgressService.HasStamp(profile, id)) return true;
            return false;
        }

        // Red stitched route used as a visual link between the two passport pages.
        private void AddJourneyThread(Transform parent)
        {
            var row = NewRect("Red passport thread", parent);
            SetHeight(row, 34);
            var line = Panel("Journey thread", row, Vermilion).rectTransform;
            line.anchorMin = new Vector2(.08f, .45f);
            line.anchorMax = new Vector2(.92f, .55f);
            line.offsetMin = line.offsetMax = Vector2.zero;
            for (int i = 0; i < 7; i++)
            {
                var stitch = Panel("Thread stitch " + i, row, Paper).rectTransform;
                float x = .18f + i * .107f;
                stitch.anchorMin = stitch.anchorMax = new Vector2(x, .5f);
                stitch.sizeDelta = new Vector2(4, 11);
                stitch.anchoredPosition = Vector2.zero;
            }
            ThreadSeal(row, .08f);
            ThreadSeal(row, .92f);
        }

        private void ThreadSeal(Transform parent, float x)
        {
            var seal = NewRect("Route seal", parent);
            seal.anchorMin = seal.anchorMax = new Vector2(x, .5f);
            seal.sizeDelta = new Vector2(25, 25);
            seal.anchoredPosition = Vector2.zero;
            var image = seal.gameObject.AddComponent<Image>();
            image.sprite = CircleSprite();
            image.color = Vermilion;
            image.raycastTarget = false;
        }

        private void SpeakerBadge(Transform parent, string speaker)
        {
            var badge = Card(parent, 82);
            Label(badge, speaker, 28, 42, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private void TryPlace(int slotIndex, FragmentDrag dragged)
        {
            int pieceIndex = dragged == null ? selectedFragment : dragged.PieceIndex;
            if (placedFragments[slotIndex] || pieceIndex < 0) return;
            if (pieceIndex != slotIndex)
            {
                notice = T("wrong_slot");
                StartCoroutine(RefreshAfterInput(false));
                return;
            }
            placedFragments[slotIndex] = true;
            selectedFragment = -1;
            (dragged == null ? pieceViews[pieceIndex] : dragged)?.Accept(slotViews[slotIndex]);
            StartCoroutine(RefreshAfterInput(true));
        }

        private IEnumerator RefreshAfterInput(bool checkCompletion)
        {
            yield return null;
            if (checkCompletion && placedFragments[0] && placedFragments[1] && placedFragments[2]) CompleteRun();
            else Show();
        }

        private void CompleteRun()
        {
            if (resultCommitted) return;
            resultCommitted = true;
            var saved = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
            ProgressService.Record(saved, run.Level.Id, run.Score, run.Stars);
            RecordMissionWorldMark(saved, run.Level.Id);
            saved.coins += 20 + run.Stars * 5;
            TastePassport.Learning.VocabularyCatalog.UnlockStageWords(saved, run.Level == LevelCatalog.HoiAn ? 1 : 5);
            if (!store.TrySave(saved)) { resultCommitted = false; notice = T("save_error"); Show(); return; }
            profile = saved;
            audioManager?.PlayCue(GameSoundCue.Stamp);
            recipeResult = false;
            matchResult = false;
            Navigate(Page.Result);
        }

        private void DrawResult(Transform parent)
        {
            string subjectKey = matchResult ? matchRun.Definition.TitleKey : recipeResult ? recipeRun.Recipe.DishKey : run.Level.PlaceKey;
            int score = matchResult ? matchRun.Score : recipeResult ? recipeRun.Score : run.Score;
            int stars = matchResult ? matchRun.Stars : recipeResult ? recipeRun.Stars : run.Stars;

            // Grand Celebration Plaque (Yêu cầu 8)
            var celebrationPlaque = JourneyCard(parent);
            var plaqueBorder = celebrationPlaque.gameObject.AddComponent<Outline>();
            plaqueBorder.effectColor = GoldGlow;
            plaqueBorder.effectDistance = new Vector2(2f, -2f);
            CollectionCopy(celebrationPlaque, "★ " + (profile != null && profile.language == "ko" ? "여정 완료 · 축하합니다!" : "CHÚC MỪNG HOÀN THÀNH CHẶNG!") + " ★", 30, HeaderWood, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            string starGraphic = stars >= 3 ? "★★★ " + (profile != null && profile.language == "ko" ? "(완벽한 성공!)" : "(Xuất sắc!)") :
                stars == 2 ? "★★☆ " + (profile != null && profile.language == "ko" ? "(능숙한 솜씨)" : "(Thành thạo)") :
                "★☆☆ " + (profile != null && profile.language == "ko" ? "(첫 발걸음)" : "(Làm quen)");
            CollectionCopy(celebrationPlaque, starGraphic, 28, Lantern, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(celebrationPlaque, T("score") + ": " + score + "   ·   🪙 +" + (20 + stars * 5), 24, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);

            if (FirstJourneyComplete()) CollectionCopy(parent, T("first_journey_complete"), 28, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            Artwork(parent, matchResult
                ? (matchRun.Definition == MatchCatalog.HoiAn ? TravelArtworkKind.HoiAn : TravelArtworkKind.Jeonju)
                : recipeResult ? (recipeRun.Recipe == RecipeCatalog.Bibimbap ? TravelArtworkKind.Bibimbap : TravelArtworkKind.CaoLau)
                : run.Level == LevelCatalog.HoiAn ? TravelArtworkKind.HoiAn : TravelArtworkKind.Jeonju, 190);

            // Cultural Seal Stamp Card with Impact Animation
            var stamp = JourneyCard(parent);
            var sBorder = stamp.gameObject.AddComponent<Outline>();
            sBorder.effectColor = Vermilion;
            sBorder.effectDistance = new Vector2(2f, -2f);
            CollectionCopy(stamp, "★ " + T("stamp") + ": " + T(subjectKey) + " ★", 28, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            StartCoroutine(StampImpact(stamp));

            // Companion Narrative Letter Card
            string speaker = T(koreanBackdrop ? "character_gabo" : "character_nhung");
            var letter = JourneyCard(parent);
            var lBorder = letter.gameObject.AddComponent<Outline>();
            lBorder.effectColor = Border;
            lBorder.effectDistance = new Vector2(1.5f, -1.5f);
            CollectionCopy(letter, "💌 " + speaker, 25, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(letter, T(koreanBackdrop ? "stamp_message_gabo" : "stamp_message_nhung", T(subjectKey)), 24, Ink, TextAnchor.MiddleCenter);

            // Newly Unlocked Cultural Vocabulary Card (Yêu cầu 5)
            int stageIdx = matchResult ? (matchRun.Definition == MatchCatalog.HoiAn ? 4 : 8) :
                recipeResult ? (recipeRun.Recipe == RecipeCatalog.CaoLau ? 3 : 7) :
                (run.Level == LevelCatalog.HoiAn ? 1 : 5);
            var stageWords = TastePassport.Learning.VocabularyCatalog.GetWordsForStage(stageIdx);
            if (stageWords != null && stageWords.Count > 0)
            {
                var vocabCard = JourneyCard(parent);
                var vBorder = vocabCard.gameObject.AddComponent<Outline>();
                vBorder.effectColor = GoldGlow;
                vBorder.effectDistance = new Vector2(1.5f, -1.5f);
                CollectionCopy(vocabCard, "📖 " + (profile != null && profile.language == "ko" ? "새로 획득한 문화 어휘:" : "Từ vựng văn hóa vừa thu thập:"), 26, HeaderWood, TextAnchor.MiddleCenter, FontStyle.Bold);
                foreach (var word in stageWords)
                {
                    var row = Panel("RewardWord_" + word.Id, vocabCard, Paper).rectTransform;
                    SetHeight(row, 44);
                    var rLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                    rLayout.padding = new RectOffset(12, 12, 4, 4);
                    rLayout.spacing = 8;
                    rLayout.childAlignment = TextAnchor.MiddleCenter;
                    rLayout.childControlWidth = true;
                    rLayout.childControlHeight = true;
                    rLayout.childForceExpandWidth = true;
                    rLayout.childForceExpandHeight = false;
                    Label(row, "★ " + word.Vietnamese + " (" + word.KoreanPronunciation + ")", 20, 40, HeaderWood, TextAnchor.MiddleLeft, FontStyle.Bold);
                    Label(row, word.Korean + " · " + word.MeaningVi, 18, 40, Jade, TextAnchor.MiddleRight, FontStyle.Italic);
                }
            }

            // Next Stage Waypoint Preview (Yêu cầu 8)
            var nextObj = MapObjectiveService.GetCurrent(profile, IsJeonjuUnlocked(profile));
            if (nextObj != null && !profile.mainStoryCompleted)
            {
                var nextCard = JourneyCard(parent);
                var nBorder = nextCard.gameObject.AddComponent<Outline>();
                nBorder.effectColor = Lantern;
                nBorder.effectDistance = new Vector2(1.5f, -1.5f);
                CollectionCopy(nextCard, "— " + (profile != null && profile.language == "ko" ? "다음 여정 안내" : "Hành trình tiếp theo đang chờ bạn") + " —", 22, Lantern, TextAnchor.MiddleCenter, FontStyle.Bold);
                CollectionCopy(nextCard, "❖ " + T(nextObj.TitleKey), 28, HeaderWood, TextAnchor.MiddleCenter, FontStyle.Bold);
                string hintKey = nextObj.Action == MapObjectiveAction.LanternCraft || nextObj.Action == MapObjectiveAction.HanjiCraft
                    ? "card_craft_hint" : nextObj.Action == MapObjectiveAction.StoryCardTarget ? "card_next_hint" : nextObj.DescriptionKey;
                CollectionCopy(nextCard, T(hintKey), 23, Muted, TextAnchor.MiddleCenter);
                StoryPrimary(nextCard, (profile != null && profile.language == "ko" ? "다음 여정 시작 ►" : "Tiến vào chặng kế tiếp ►"), () => RunMapObjective(nextObj));
            }

            Button(parent, T("passport"), Jade, () => Navigate(Page.Passport));
            CompactButton(parent, T("replay"), Paper, () => { if (matchResult) StartMatch(matchRun.Definition); else if (recipeResult) StartRecipe(recipeRun.Recipe); else StartLevel(run.Level); }, Ink);
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private static IEnumerator StampImpact(RectTransform stamp)
        {
            if (stamp == null) yield break;
            const float impactTime = .13f;
            const float settleTime = .19f;
            stamp.localScale = Vector3.one * .72f;
            stamp.localRotation = Quaternion.Euler(0f, 0f, -6f);
            float elapsed = 0f;
            while (stamp != null && elapsed < impactTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / impactTime);
                stamp.localScale = Vector3.Lerp(Vector3.one * .72f, Vector3.one * 1.06f, progress);
                stamp.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-6f, 1.5f, progress));
                yield return null;
            }
            elapsed = 0f;
            while (stamp != null && elapsed < settleTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / settleTime);
                stamp.localScale = Vector3.Lerp(Vector3.one * 1.06f, Vector3.one, progress);
                stamp.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(1.5f, 0f, progress));
                yield return null;
            }
            if (stamp != null)
            {
                stamp.localScale = Vector3.one;
                stamp.localRotation = Quaternion.identity;
            }
        }

        private void StartRecipe(RecipeDefinition recipe)
        {
            StoryNarration.Stop();
            interactionFeedbackKey = null;
            resultCommitted = false;
            koreanBackdrop = recipe == RecipeCatalog.Bibimbap;
            matchResult = false;
            recipeRun = new RecipeRun(recipe, Environment.TickCount);
            audioManager?.SetEnvironment(koreanBackdrop);
            PlayStageAudio(2, koreanBackdrop);
            Navigate(Page.RecipeDialogue);
        }

        private void DrawRecipeDialogue(Transform parent)
        {
            Header(parent, T(recipeRun.Recipe.DishKey));
            ActivityGuide(parent, "activity_ready", "activity_recipe_intro");
            Artwork(parent, recipeRun.Recipe == RecipeCatalog.Bibimbap ? TravelArtworkKind.Bibimbap : TravelArtworkKind.CaoLau, 200);
            SpeakerBadge(parent, T(recipeRun.Recipe == RecipeCatalog.Bibimbap ? "character_kim" : "character_lan"));
            var card = JourneyCard(parent);
            CollectionCopy(card, T(recipeRun.Recipe.GuideKey), 30, Ink, TextAnchor.MiddleCenter);
            ActivityNarration(card, T(recipeRun.Recipe.GuideKey));
            if (recipeRun.Recipe == RecipeCatalog.Bibimbap) KoreaVisuals.DecorateRecipePanel(card);
            else HoiAnVisuals.DecorateRecipePanel(card);
            CollectionCopy(parent, T("recipe_hint"), 25, Jade, TextAnchor.MiddleCenter);
            Button(parent, T("activity_start_recipe"), Jade, () =>
            {
                recipeDeadline = Time.time + 90f;
                Navigate(Page.RecipeGame);
            });
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void DrawRecipeGame(Transform parent)
        {
            bool bibimbap = recipeRun.Recipe == RecipeCatalog.Bibimbap;
            Header(parent, T(recipeRun.Recipe.DishKey));
            if (recipeRun.Complete)
            {
                ActivityGuide(parent, "activity_complete", "activity_pending_save");
                Button(parent, T("save_result"), Jade, CompleteRecipe);
                CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
                return;
            }
            var status = ActivityGuide(parent, "activity_recipe_phase", "activity_recipe_drop");
            recipeTimer = CollectionCopy(status, "", 32, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            ProgressStrip(parent, recipeRun.StepIndex, recipeRun.Recipe.Steps.Length, T("recipe_step"));
            Artwork(parent, bibimbap ? TravelArtworkKind.Bibimbap : TravelArtworkKind.CaoLau, 205);
            var guide = JourneyCard(parent);
            if (bibimbap) KoreaVisuals.DecorateRecipePanel(guide);
            else HoiAnVisuals.DecorateRecipePanel(guide);
            CollectionCopy(guide, T("recipe_step") + " " + (recipeRun.StepIndex + 1) + "/" + recipeRun.Recipe.Steps.Length, 29, Jade, TextAnchor.MiddleCenter);
            CollectionCopy(guide, T(recipeRun.Recipe.Steps[recipeRun.StepIndex]), 37, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            var recipeDrop = guide.gameObject.AddComponent<FragmentDropSlot>();
            recipeDrop.SlotIndex = -1;
            recipeDrop.TryDrop = TryDropIngredient;
            if (!string.IsNullOrEmpty(interactionFeedbackKey))
                CollectionCopy(guide, T(interactionFeedbackKey), 23, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(parent, profile != null && profile.language == "ko" ? "◆ 재료 선택 (알맞은 재료를 위 단계로 끌어놓으세요) ◆" : "◆ Chọn nguyên liệu (Kéo nguyên liệu vào bước nấu ăn ở trên) ◆", 22, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int row = 0; row < (recipeRun.Choices.Length + 1) / 2; row++)
            {
                var ingredients = NewRect("Ingredients", parent);
                SetHeight(ingredients, 104);
                AddHorizontal(ingredients);
                for (int column = 0; column < 2; column++)
                {
                    int index = row * 2 + column;
                    if (index >= recipeRun.Choices.Length) break;
                    var ingredient = Panel("Ingredient card " + index, ingredients, Paper, true).gameObject;
                    Label(ingredient.transform, T(recipeRun.Choices[index]), 24, 0, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                    SetHeight(ingredient.GetComponent<RectTransform>(), 112);
                    if (recipeRun.Recipe == RecipeCatalog.CaoLau)
                        HoiAnVisuals.StyleIngredientCard(ingredient.GetComponent<RectTransform>(), recipeRun.Choices[index]);
                    else if (bibimbap)
                        KoreaVisuals.StyleIngredientCard(ingredient.GetComponent<RectTransform>(), recipeRun.Choices[index]);
                    ingredient.AddComponent<FragmentDrag>().PieceIndex = index;
                }
            }
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void ChooseIngredient(int index)
        {
            if (Time.time >= recipeDeadline) { Navigate(Page.RecipeFail); return; }
            IngredientResult result = recipeRun.Choose(index);
            interactionFeedbackKey = result == IngredientResult.Wrong ? "recipe_wrong" : "recipe_correct";
            if (result == IngredientResult.Complete)
            {
                if (recipeRun.Recipe == RecipeCatalog.Bibimbap) Navigate(Page.RecipeMix);
                else CompleteRecipe();
                return;
            }
            if (result == IngredientResult.Wrong)
                StartCoroutine(RefreshAfterInput(false));
            else Show();
        }

        private void TryDropIngredient(int targetIndex, FragmentDrag dragged)
        {
            if (dragged != null) ChooseIngredient(dragged.PieceIndex);
        }

        private void DrawRecipeMix(Transform parent)
        {
            Header(parent, T("bibimbap"));
            ActivityGuide(parent, "activity_mix_phase", "activity_mix_hint");
            ProgressStrip(parent, recipeRun.StepIndex, recipeRun.Recipe.Steps.Length, T("activity_ingredients_ready"));
            Artwork(parent, TravelArtworkKind.Bibimbap, 210);
            var mixZone = JourneyCard(parent);
            mixZone.name = "Bibimbap swipe mixing area";
            Label(mixZone, T("mix_gesture_target"), 28, 0, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            mixZone.gameObject.AddComponent<MixGestureHandler>().Initialize(CompleteRecipe);
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void DrawRecipeFail(Transform parent)
        {
            TryPlayLearningAudioMood();
            Header(parent, T(recipeRun.Recipe.DishKey));
            ActivityGuide(parent, "activity_retry", "activity_recipe_retry_hint");
            ProgressStrip(parent, recipeRun.StepIndex, recipeRun.Recipe.Steps.Length, T("activity_ingredients_ready"));
            Artwork(parent, recipeRun.Recipe == RecipeCatalog.Bibimbap ? TravelArtworkKind.Bibimbap : TravelArtworkKind.CaoLau, 190);
            CollectionCopy(parent, T("time_out"), 32, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            DrawFailureLearningCard(parent, recipeRun.Recipe == RecipeCatalog.Bibimbap ? "jeonju_bibimbap" : "hoian_caolau");
            Spacer(parent, GameUiTheme.SpaceSmall);
            Button(parent, T("replay"), Jade, () => StartRecipe(recipeRun.Recipe));
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void TryPlayLearningAudioMood()
        {
            try
            {
                var director = GetComponent<TastePassport.UI.StageAudioDirector>();
                if (director != null)
                {
                    var method = director.GetType().GetMethod("PlayLearningMusic") ?? director.GetType().GetMethod("SetMood");
                    if (method != null)
                    {
                        var parameters = method.GetParameters();
                        if (parameters.Length == 0) method.Invoke(director, null);
                        else if (parameters.Length == 1 && parameters[0].ParameterType.IsEnum)
                        {
                            var enumValue = Enum.Parse(parameters[0].ParameterType, "Learning");
                            method.Invoke(director, new[] { enumValue });
                        }
                    }
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("Learning audio mood hook skipped: " + error.Message);
            }
        }

        private void DrawFailureLearningCard(Transform parent, string activityTopic)
        {
            var card = JourneyCard(parent);
            card.name = "Cultural failure lesson notebook";

            bool isKo = profile != null && profile.language == "ko";
            bool isBibimbap = activityTopic == "jeonju_bibimbap";

            Label(card, isKo ? "여행 노트 · 음식 & 문화 배움" : "Ghi chú du ký · Từ vựng & Văn hóa", 28, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);

            string vocabWord = isBibimbap
                ? (isKo ? "비빔밥 · Cơm trộn Jeonju" : "Bibimbap (비빔밥) · Cơm trộn Jeonju")
                : (isKo ? "카오라우 · Cao Lầu Hội An" : "Cao Lầu (비빔 쌀국수) · Cao Lầu Hội An");

            string pronunciation = isBibimbap
                ? (isKo ? "발음: [bi-bim-bap]" : "Phát âm: [Bi-bim-bap]")
                : (isKo ? "발음: [kao-lau]" : "Phát âm: [Kao-lau / Bibim ssal-guksu]");

            string meaning = isBibimbap
                ? (isKo ? "전주 대표 전통 음식. 오방색(청·적·황·백·흑) 나물과 고추장의 조화로 맛과 건강을 담은 밥 요리."
                        : "Món cơm biểu tượng của Jeonju. Sự phối hợp hài hòa giữa ngũ sắc rau củ và tương ớt Gochujang thơm lừng.")
                : (isKo ? "호이안의 대표 전통 비빔 쌀국수. 바레 고대 우물물과 참섬 나무 재를 사용해 특유 của 쫄깃한 식감을 냅니다."
                        : "Món mì độc nhất vô nhị của Hội An, dùng nước giếng Bá Lễ và tro củi Cù Lao Chàm để tạo sợi mì dẻo giòn đặc trưng.");

            string example = isBibimbap
                ? (isKo ? "예문: \"따뜻한 놋그릇에 담긴 전주 비빔밥을 맛있게 비벼 드세요.\" (Hãy thưởng thức tô cơm trộn Jeonju nóng hổi.)"
                        : "Ví dụ: \"Vuốt nhẹ qua lại để trộn đều các nguyên liệu ngũ sắc.\" (재료가 잘 어우러지도록 골고루 비벼주세요.)")
                : (isKo ? "예문: \"호아이 강변에서 향긋한 채소와 함께 카오라우를 맛보세요.\" (Hãy thưởng thức Cao Lầu cùng rau sống sông Hoài.)"
                        : "Ví dụ: \"Cao Lầu ăn kèm rau đắng Trà Quế và bánh tráng mè nướng giòn.\" (신선한 채소와 바삭한 라이스페이퍼를 곁들여 먹습니다.)");

            var wordBox = Panel("Vocabulary highlight box", card, new Color(1f, 0.98f, 0.92f));
            wordBox.rectTransform.gameObject.AddComponent<Outline>().effectColor = Border;
            var wordLayout = wordBox.rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            wordLayout.padding = new RectOffset(16, 16, 12, 12);
            wordLayout.spacing = 6;
            wordLayout.childAlignment = TextAnchor.MiddleCenter;
            wordLayout.childControlWidth = true;
            wordLayout.childControlHeight = true;
            wordLayout.childForceExpandWidth = true;
            wordLayout.childForceExpandHeight = false;

            Label(wordBox.transform, vocabWord, 29, 0, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(wordBox.transform, pronunciation, 22, 0, Muted, TextAnchor.MiddleCenter);

            Spacer(card, 6);
            CollectionCopy(card, meaning, 24, Ink, TextAnchor.MiddleLeft);
            Spacer(card, 4);
            CollectionCopy(card, example, 22, Jade, TextAnchor.MiddleLeft, FontStyle.Italic);
        }

        private void CompleteRecipe()
        {
            if (resultCommitted) return;
            resultCommitted = true;
            var saved = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
            ProgressService.Record(saved, recipeRun.Recipe.Id, recipeRun.Score, recipeRun.Stars);
            RecordMissionWorldMark(saved, recipeRun.Recipe.Id);
            saved.coins += 20 + recipeRun.Stars * 5;
            TastePassport.Learning.VocabularyCatalog.UnlockStageWords(saved, recipeRun.Recipe == RecipeCatalog.CaoLau ? 3 : 7);
            if (!store.TrySave(saved)) { resultCommitted = false; notice = T("save_error"); Show(); return; }
            profile = saved;
            audioManager?.PlayCue(GameSoundCue.Stamp);
            recipeResult = true;
            matchResult = false;
            Navigate(Page.Result);
        }

        private void StartMatch(MatchDefinition definition)
        {
            StoryNarration.Stop();
            interactionFeedbackKey = null;
            resultCommitted = false;
            koreanBackdrop = definition == MatchCatalog.Jeonju;
            matchRun = new MatchRun(definition, Environment.TickCount);
            audioManager?.SetEnvironment(koreanBackdrop);
            PlayStageAudio(3, koreanBackdrop);
            Navigate(Page.MatchDialogue);
        }

        private void DrawMatchDialogue(Transform parent)
        {
            Header(parent, T(matchRun.Definition.TitleKey));
            ActivityGuide(parent, "activity_ready", "activity_match_intro");
            Artwork(parent, matchRun.Definition == MatchCatalog.HoiAn ? TravelArtworkKind.HoiAn : TravelArtworkKind.Jeonju, 190);
            SpeakerBadge(parent, T(matchRun.Definition == MatchCatalog.HoiAn ? "character_nhung" : "character_gabo"));
            var card = JourneyCard(parent);
            CollectionCopy(card, T(matchRun.Definition.GuideKey), 29, Ink, TextAnchor.MiddleCenter);
            ActivityNarration(card, T(matchRun.Definition.GuideKey));
            Button(parent, T("activity_start_match"), Jade, () => Navigate(Page.MatchGame));
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void DrawMatchGame(Transform parent)
        {
            Header(parent, T(matchRun.Definition.TitleKey));
            if (matchRun.Complete)
            {
                ActivityGuide(parent, "activity_complete", "activity_pending_save");
                Button(parent, T("save_result"), Jade, CompleteMatch);
                return;
            }
            ActivityGuide(parent, "activity_match_phase", "activity_match_intro");
            ProgressStrip(parent, matchRun.StepIndex, matchRun.Definition.RequestKeys.Length, T("activity_guests_helped"));
            Artwork(parent, matchRun.Definition == MatchCatalog.HoiAn ? TravelArtworkKind.HoiAn : TravelArtworkKind.Jeonju, 190);
            CollectionCopy(parent, T("recipe_step") + " " + (matchRun.StepIndex + 1) + "/" + matchRun.Definition.RequestKeys.Length, 28, Jade, TextAnchor.MiddleCenter);
            var request = JourneyCard(parent);
            CollectionCopy(request, T(matchRun.CurrentRequestKey), 31, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            bool hoiAn = matchRun.Definition == MatchCatalog.HoiAn;
            if (hoiAn) HoiAnVisuals.DecorateLocationCard(request);
            else KoreaVisuals.DecorateLocationCard(request);
            var target = request.gameObject.AddComponent<FragmentDropSlot>();
            target.SlotIndex = -1;
            target.TryDrop = TryDropMatch;
            if (!string.IsNullOrEmpty(interactionFeedbackKey))
                CollectionCopy(request, T(interactionFeedbackKey), 23, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(parent, profile != null && profile.language == "ko" ? "◆ 알맞은 경험을 골라 위 손님 카드에 전달하세요 ◆" : "◆ Chọn trải nghiệm thích hợp kéo vào thẻ du khách ở trên ◆", 22, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int i = 0; i < matchRun.AnswerCount; i++)
            {
                int choice = i;
                var answer = Panel("Experience card " + (i + 1), parent, Paper, true).gameObject;
                Label(answer.transform, T(matchRun.AnswerKey(i)), 24, 0, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                SetHeight(answer.GetComponent<RectTransform>(), 105);
                if (hoiAn) HoiAnVisuals.StyleIngredientCard(answer.GetComponent<RectTransform>(), matchRun.AnswerKey(i));
                else KoreaVisuals.StyleIngredientCard(answer.GetComponent<RectTransform>(), matchRun.AnswerKey(i));
                answer.AddComponent<FragmentDrag>().PieceIndex = choice;
            }
            CompactButton(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void ChooseMatch(int choice)
        {
            MatchResult result = matchRun.Choose(choice);
            interactionFeedbackKey = result == MatchResult.Wrong
                ? (matchRun.Definition == MatchCatalog.HoiAn ? "match_wrong_hoian" : "match_wrong_jeonju")
                : (matchRun.Definition == MatchCatalog.HoiAn ? "match_correct_hoian" : "match_correct_jeonju");
            if (result == MatchResult.Complete) { CompleteMatch(); return; }
            Show();
        }

        private void TryDropMatch(int targetIndex, FragmentDrag dragged)
        {
            if (dragged != null) ChooseMatch(dragged.PieceIndex);
        }

        private void CompleteMatch()
        {
            if (resultCommitted) return;
            resultCommitted = true;
            var saved = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
            ProgressService.Record(saved, matchRun.Definition.Id, matchRun.Score, matchRun.Stars);
            RecordMissionWorldMark(saved, matchRun.Definition.Id);
            saved.coins += 20 + matchRun.Stars * 5;
            TastePassport.Learning.VocabularyCatalog.UnlockStageWords(saved, matchRun.Definition == MatchCatalog.HoiAn ? 4 : 8);
            if (!store.TrySave(saved)) { resultCommitted = false; notice = T("save_error"); Show(); return; }
            profile = saved;
            audioManager?.PlayCue(GameSoundCue.Stamp);
            recipeResult = false;
            matchResult = true;
            Navigate(Page.Result);
        }

        private void ChangeLanguage(string language)
        {
            string previous = profile.language;
            profile.language = language;
            if (!store.TrySave(profile)) { profile.language = previous; notice = T("save_error"); }
            Show();
        }

        private static Color Hex(string code) { ColorUtility.TryParseHtmlString(code, out var result); return result; }
        private static RectTransform NewRect(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return (RectTransform)obj.transform;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
        private static void SetHeight(RectTransform rect, float height)
        {
            var element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
        }
        private static void Spacer(Transform parent, float height) => SetHeight(NewRect("Space", parent), height);
        private static Image Panel(string name, Transform parent, Color color, bool stretch = false)
        {
            var rect = NewRect(name, parent);
            if (stretch) Stretch(rect);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (!stretch)
            {
                image.sprite = RoundedSprite();
                image.type = Image.Type.Sliced;
            }
            return image;
        }
        private static Sprite RoundedSprite()
        {
            if (roundedSprite != null) return roundedSprite;
            const int size = 64;
            const float radius = 13;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, Mathf.Abs(x - (size - 1) * 0.5f) - (size * 0.5f - radius));
                float dy = Mathf.Max(0, Mathf.Abs(y - (size - 1) * 0.5f) - (size * 0.5f - radius));
                byte alpha = (byte)(Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy)) * 255);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
            return roundedSprite;
        }
        private static Sprite CircleSprite()
        {
            if (circleSprite != null) return circleSprite;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                byte alpha = (byte)(Mathf.Clamp01(31.5f - distance) * 255);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return circleSprite;
        }
        private void DrawAvatar(Transform parent, int index) => DrawAvatar(parent, index,
            profile == null ? 0 : profile.outfitId,
            profile == null ? 0 : profile.faceStyle,
            profile == null ? 0 : profile.hairStyle,
            profile == null ? 0 : profile.bodyStyle,
            profile == null ? 0 : profile.accessoryStyle);

        private void DrawAvatar(Transform parent, int index, int outfitId) => DrawAvatar(parent, index, outfitId,
            profile == null ? 0 : profile.faceStyle,
            profile == null ? 0 : profile.hairStyle,
            profile == null ? 0 : profile.bodyStyle,
            profile == null ? 0 : profile.accessoryStyle);

        private void DrawAvatar(Transform parent, int index, int outfitId, int faceStyle, int hairStyle, int bodyStyle, int accessoryStyle)
        {
            index = Mathf.Clamp(index, 0, 2);
            string[] characterPaths = { "Art/Characters/Traveler", "Art/Characters/Nhung", "Art/Characters/GaBo" };
            var charSprite = Resources.Load<Sprite>(characterPaths[index]);
            if (charSprite != null)
            {
                var portraitBox = NewRect("Face", parent);
                portraitBox.anchorMin = new Vector2(0.04f, 0.28f);
                portraitBox.anchorMax = new Vector2(0.96f, 0.98f);
                portraitBox.offsetMin = Vector2.zero;
                portraitBox.offsetMax = Vector2.zero;
                var charImg = portraitBox.gameObject.AddComponent<Image>();
                charImg.sprite = charSprite;
                charImg.preserveAspect = true;
                charImg.raycastTarget = false;

                // Cultural outfit badge overlay
                if (outfitId == 1)
                {
                    var badge = NewRect("Avatar outfit tag", parent);
                    badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 0.28f);
                    badge.sizeDelta = new Vector2(76, 20);
                    badge.anchoredPosition = new Vector2(0, 10);
                    var bImg = badge.gameObject.AddComponent<Image>();
                    bImg.color = Hex("#A94A42");
                    var bTxt = Label(badge, "Áo dài", 12, 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    Stretch(bTxt.rectTransform);
                }
                else if (outfitId == 2)
                {
                    var badge = NewRect("Avatar outfit tag", parent);
                    badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 0.28f);
                    badge.sizeDelta = new Vector2(76, 20);
                    badge.anchoredPosition = new Vector2(0, 10);
                    var bImg = badge.gameObject.AddComponent<Image>();
                    bImg.color = Hex("#357E84");
                    var bTxt = Label(badge, "Hanbok", 12, 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    Stretch(bTxt.rectTransform);
                }
                return;
            }

            Color[] skin = { Hex("#EAB998"), Hex("#D69B75"), Hex("#F0C4A5") };
            Color[] hair = { Hex("#3B302C"), Hex("#674534"), Hex("#302F3B") };
            index = Mathf.Clamp(index, 0, 2);
            faceStyle = Mathf.Clamp(faceStyle, 0, 2);
            hairStyle = Mathf.Clamp(hairStyle, 0, 2);
            bodyStyle = Mathf.Clamp(bodyStyle, 0, 2);
            accessoryStyle = Mathf.Clamp(accessoryStyle, 0, 2);
            Color outfitColor = outfitId == 1 ? Hex("#A94A42") : outfitId == 2 ? Hex("#357E84") : outfitId == 3 ? Hex("#E6A14D") : outfitId == 4 ? Hex("#788B70") : avatarColors[index];
            float[] bodyWidths = { 98f, 84f, 110f };
            float[] bodyHeights = { 70f, 82f, 74f };
            PortraitPart(parent, "Outfit silhouette", outfitColor, new Vector2(0, 0),
                new Vector2(bodyWidths[bodyStyle] + (outfitId == 1 || outfitId == 2 ? 8f : 0f), bodyHeights[bodyStyle] + (outfitId == 1 || outfitId == 2 ? 10f : 0f)), CircleSprite());
            if (outfitId == 1) PortraitPart(parent, "Ao dai panel", Hex("#FFF1D5"), new Vector2(0, -12), new Vector2(10, 50), RoundedSprite());
            if (outfitId == 2) PortraitPart(parent, "Hanbok collar", Hex("#F7E8D0"), new Vector2(0, -5), new Vector2(44, 15), RoundedSprite());
            float[] hairWidths = { 80f, 92f, 74f };
            float[] hairHeights = { 91f, 77f, 102f };
            float[] faceWidths = { 65f, 72f, 60f };
            float[] faceHeights = { 74f, 70f, 80f };
            PortraitPart(parent, "Hair back", hair[index], new Vector2(0, 27), new Vector2(hairWidths[hairStyle], hairHeights[hairStyle]), CircleSprite());
            PortraitPart(parent, "Face", skin[index], new Vector2(0, 19), new Vector2(faceWidths[faceStyle], faceHeights[faceStyle]), CircleSprite());
            float fringeX = hairStyle == 1 ? 12f : hairStyle == 2 ? 0f : -8f;
            float fringeWidth = hairStyle == 2 ? 68f : hairStyle == 1 ? 52f : 49f;
            PortraitPart(parent, "Fringe", hair[index], new Vector2(fringeX, 48), new Vector2(fringeWidth, hairStyle == 2 ? 31f : 25f), RoundedSprite());
            float eyeY = faceStyle == 2 ? 22f : 19f;
            float eyeWidth = faceStyle == 1 ? 6f : 5f;
            PortraitPart(parent, "Left ink eye", Hex("#43372F"), new Vector2(-14, eyeY), new Vector2(eyeWidth, 7), CircleSprite());
            PortraitPart(parent, "Right ink eye", Hex("#43372F"), new Vector2(14, eyeY), new Vector2(eyeWidth, 7), CircleSprite());
            PortraitPart(parent, "Nose detail", Hex("#A86F58"), new Vector2(0, 12), new Vector2(4, 4), CircleSprite());
            PortraitPart(parent, "Smile", Hex("#A94A42"), new Vector2(0, faceStyle == 2 ? 1 : 3), new Vector2(faceStyle == 1 ? 13f : 11f, 3), RoundedSprite());
            if (outfitId == 1)
            {
                PortraitPart(parent, "Ao dai collar", Hex("#FFF1D5"), new Vector2(0, 31), new Vector2(20, 10), RoundedSprite());
                PortraitPart(parent, "Ao dai piping", Hex("#E6A14D"), new Vector2(0, -8), new Vector2(3, 29), RoundedSprite());
            }
            else if (outfitId == 2)
            {
                PortraitPart(parent, "Hanbok collar left", Hex("#F7E8D0"), new Vector2(-9, 27), new Vector2(22, 9), RoundedSprite());
                PortraitPart(parent, "Hanbok collar right", Hex("#A94A42"), new Vector2(9, 27), new Vector2(22, 9), RoundedSprite());
            }
            else
            {
                PortraitPart(parent, "Classic coat button", Hex("#E6A14D"), new Vector2(0, -18), new Vector2(7, 7), CircleSprite());
            }
            int accessory = outfitId == 3 ? 1 : outfitId == 4 ? 2 : accessoryStyle;
            if (accessory == 1)
            {
                PortraitPart(parent, "Hoi An lantern hair ornament", Lantern, new Vector2(34, 47), new Vector2(14, 18), CircleSprite());
                PortraitPart(parent, "Lantern ornament tassel", Vermilion, new Vector2(34, 34), new Vector2(3, 10), RoundedSprite());
            }
            else if (accessory == 2)
            {
                PortraitPart(parent, "Hanji flower hair ornament", Paper, new Vector2(-34, 47), new Vector2(18, 18), CircleSprite());
                PortraitPart(parent, "Flower center", Vermilion, new Vector2(-34, 47), new Vector2(6, 6), CircleSprite());
            }
        }
        private static void PortraitPart(Transform parent, string name, Color color, Vector2 position, Vector2 size, Sprite sprite)
        {
            var rect = NewRect(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
        }

        private void DrawFullBodyTraveler(Transform parent, int avatarIndex, int outfitId, int faceStyle, int hairStyle, int bodyStyle, int accessoryStyle)
        {
            avatarIndex = Mathf.Clamp(avatarIndex, 0, 2);
            faceStyle = Mathf.Clamp(faceStyle, 0, 2);
            hairStyle = Mathf.Clamp(hairStyle, 0, 2);
            bodyStyle = Mathf.Clamp(bodyStyle, 0, 2);
            accessoryStyle = Mathf.Clamp(accessoryStyle, 0, 2);
            outfitId = Mathf.Clamp(outfitId, 0, 4);

            float scaleX = bodyStyle == 1 ? 0.90f : bodyStyle == 2 ? 1.10f : 1.0f;
            float scaleY = bodyStyle == 1 ? 1.05f : bodyStyle == 2 ? 0.95f : 1.0f;

            string[] characterPaths = { "Art/Characters/Traveler", "Art/Characters/Nhung", "Art/Characters/GaBo" };
            var charSprite = Resources.Load<Sprite>(characterPaths[avatarIndex]);
            if (charSprite != null)
            {
                // Shadow underneath
                PortraitPart(parent, "Traveler pedestal shadow", new Color(0.28f, 0.22f, 0.18f, 0.28f), new Vector2(0, -125), new Vector2(170 * scaleX, 24), CircleSprite());

                var charRect = NewRect("High res traveler showcase", parent);
                charRect.anchorMin = charRect.anchorMax = new Vector2(0.5f, 0.5f);
                charRect.pivot = new Vector2(0.5f, 0.5f);
                charRect.sizeDelta = new Vector2(250, 280);
                charRect.anchoredPosition = new Vector2(0, 10);
                var img = charRect.gameObject.AddComponent<Image>();
                img.sprite = charSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
                charRect.localScale = new Vector3(scaleX, scaleY, 1f);
                charRect.gameObject.AddComponent<CharacterPreviewBobbing>();

                // Outfit badge
                if (outfitId == 1)
                {
                    var badge = NewRect("Outfit badge", parent);
                    badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 0.5f);
                    badge.sizeDelta = new Vector2(130, 28);
                    badge.anchoredPosition = new Vector2(0, -135);
                    var bImg = badge.gameObject.AddComponent<Image>();
                    bImg.color = Hex("#A94A42");
                    var bOutline = badge.gameObject.AddComponent<Outline>();
                    bOutline.effectColor = GoldGlow;
                    bOutline.effectDistance = new Vector2(1, -1);
                    var bTxt = Label(badge, "🇻🇳 Áo dài", 16, 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    Stretch(bTxt.rectTransform);
                }
                else if (outfitId == 2)
                {
                    var badge = NewRect("Outfit badge", parent);
                    badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 0.5f);
                    badge.sizeDelta = new Vector2(130, 28);
                    badge.anchoredPosition = new Vector2(0, -135);
                    var bImg = badge.gameObject.AddComponent<Image>();
                    bImg.color = Hex("#357E84");
                    var bOutline = badge.gameObject.AddComponent<Outline>();
                    bOutline.effectColor = GoldGlow;
                    bOutline.effectDistance = new Vector2(1, -1);
                    var bTxt = Label(badge, "🇰🇷 Hanbok", 16, 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    Stretch(bTxt.rectTransform);
                }
                return;
            }

            Color[] skin = { Hex("#EAB998"), Hex("#D69B75"), Hex("#F0C4A5") };
            Color[] hair = { Hex("#3B302C"), Hex("#674534"), Hex("#302F3B") };

            Color outfitColor = outfitId == 1 ? Hex("#A94A42") : outfitId == 2 ? Hex("#357E84") : outfitId == 3 ? Hex("#E6A14D") : outfitId == 4 ? Hex("#788B70") : avatarColors[avatarIndex];

            // 1. Floor shadow / pedestal
            PortraitPart(parent, "Traveler pedestal shadow", new Color(0.28f, 0.22f, 0.18f, 0.22f), new Vector2(0, -135), new Vector2(170 * scaleX, 24), CircleSprite());

            // 2. Shoes
            if (outfitId == 1) // Áo dài traditional silk shoes / wooden clogs
            {
                PortraitPart(parent, "Left shoe sole", Hex("#94633E"), new Vector2(-19 * scaleX, -132), new Vector2(28, 9), RoundedSprite());
                PortraitPart(parent, "Left shoe silk", Hex("#A94A42"), new Vector2(-19 * scaleX, -127), new Vector2(24, 11), CircleSprite());
                PortraitPart(parent, "Right shoe sole", Hex("#94633E"), new Vector2(19 * scaleX, -132), new Vector2(28, 9), RoundedSprite());
                PortraitPart(parent, "Right shoe silk", Hex("#A94A42"), new Vector2(19 * scaleX, -127), new Vector2(24, 11), CircleSprite());
            }
            else if (outfitId == 2) // Hanbok kipsin embroidered shoes
            {
                PortraitPart(parent, "Left kipsin", Hex("#2A474D"), new Vector2(-19 * scaleX, -130), new Vector2(26, 13), RoundedSprite());
                PortraitPart(parent, "Right kipsin", Hex("#2A474D"), new Vector2(19 * scaleX, -130), new Vector2(26, 13), RoundedSprite());
                PortraitPart(parent, "Left kipsin tip", Hex("#FFF5E4"), new Vector2(-19 * scaleX, -127), new Vector2(10, 6), CircleSprite());
                PortraitPart(parent, "Right kipsin tip", Hex("#FFF5E4"), new Vector2(19 * scaleX, -127), new Vector2(10, 6), CircleSprite());
            }
            else // Travel boots
            {
                PortraitPart(parent, "Left boot", Hex("#463428"), new Vector2(-19 * scaleX, -129), new Vector2(28, 15), RoundedSprite());
                PortraitPart(parent, "Right boot", Hex("#463428"), new Vector2(19 * scaleX, -129), new Vector2(28, 15), RoundedSprite());
                PortraitPart(parent, "Left boot cuff", Hex("#61493A"), new Vector2(-19 * scaleX, -122), new Vector2(24, 8), RoundedSprite());
                PortraitPart(parent, "Right boot cuff", Hex("#61493A"), new Vector2(19 * scaleX, -122), new Vector2(24, 8), RoundedSprite());
            }

            // 3. Lower garment / legs
            if (outfitId == 1) // Áo dài: white silk trousers + crimson front flap
            {
                PortraitPart(parent, "Silk trousers left", Hex("#FFF8F0"), new Vector2(-17 * scaleX, -78), new Vector2(26 * scaleX, 98), RoundedSprite());
                PortraitPart(parent, "Silk trousers right", Hex("#FFF8F0"), new Vector2(17 * scaleX, -78), new Vector2(26 * scaleX, 98), RoundedSprite());
                PortraitPart(parent, "Ao dai front flap", Hex("#A94A42"), new Vector2(0, -62), new Vector2(56 * scaleX, 126), RoundedSprite());
                PortraitPart(parent, "Ao dai hem border", Hex("#E6A14D"), new Vector2(0, -122), new Vector2(50 * scaleX, 5), RoundedSprite());
            }
            else if (outfitId == 2) // Hanbok: voluminous chima skirt
            {
                PortraitPart(parent, "Hanbok chima skirt", Hex("#2C4D57"), new Vector2(0, -70), new Vector2(124 * scaleX, 134), RoundedSprite());
                PortraitPart(parent, "Skirt pleat left", Hex("#24414A"), new Vector2(-36 * scaleX, -72), new Vector2(6, 122), RoundedSprite());
                PortraitPart(parent, "Skirt pleat right", Hex("#24414A"), new Vector2(36 * scaleX, -72), new Vector2(6, 122), RoundedSprite());
                PortraitPart(parent, "Skirt pleat center", new Color(1f, 1f, 1f, 0.08f), new Vector2(0, -72), new Vector2(10 * scaleX, 126), RoundedSprite());
                PortraitPart(parent, "Skirt hem border", Hex("#5A8794"), new Vector2(0, -134), new Vector2(112 * scaleX, 6), RoundedSprite());
            }
            else // Travel trousers + coat flap
            {
                PortraitPart(parent, "Travel trousers left", Hex("#463B34"), new Vector2(-16 * scaleX, -76), new Vector2(26 * scaleX, 96), RoundedSprite());
                PortraitPart(parent, "Travel trousers right", Hex("#463B34"), new Vector2(16 * scaleX, -76), new Vector2(26 * scaleX, 96), RoundedSprite());
                PortraitPart(parent, "Coat flap", outfitColor, new Vector2(0, -36), new Vector2(76 * scaleX, 62), RoundedSprite());
                PortraitPart(parent, "Coat hem trim", Hex("#D49B55"), new Vector2(0, -65), new Vector2(70 * scaleX, 6), RoundedSprite());
            }

            // 4. Upper body & Torso
            if (outfitId == 1) // Áo dài torso
            {
                PortraitPart(parent, "Ao dai torso", Hex("#A94A42"), new Vector2(0, 10), new Vector2(72 * scaleX, 74 * scaleY), RoundedSprite());
                PortraitPart(parent, "Ao dai collar", Hex("#FFF1D5"), new Vector2(0, 48), new Vector2(24, 14), RoundedSprite());
                PortraitPart(parent, "Ao dai piping", Hex("#E6A14D"), new Vector2(6 * scaleX, 26), new Vector2(2, 34), RoundedSprite());
                PortraitPart(parent, "Ao dai buttons", Hex("#E6A14D"), new Vector2(10 * scaleX, 20), new Vector2(4, 26), RoundedSprite());
            }
            else if (outfitId == 2) // Hanbok jeogori + dongjeong + otgoreum bow
            {
                PortraitPart(parent, "Hanbok jeogori", Hex("#438C88"), new Vector2(0, 18), new Vector2(78 * scaleX, 60 * scaleY), RoundedSprite());
                PortraitPart(parent, "Hanbok dongjeong", Hex("#FFFFFF"), new Vector2(0, 44), new Vector2(34, 12), RoundedSprite());
                PortraitPart(parent, "Hanbok ribbon knot", Hex("#A94A42"), new Vector2(10 * scaleX, 24), new Vector2(14, 12), CircleSprite());
                PortraitPart(parent, "Hanbok ribbon long", Hex("#A94A42"), new Vector2(14 * scaleX, -2), new Vector2(9, 46), RoundedSprite());
                PortraitPart(parent, "Hanbok ribbon short", Hex("#A94A42"), new Vector2(8 * scaleX, 4), new Vector2(9, 30), RoundedSprite());
            }
            else // Travel coat + leather belt + brass buttons + satchel
            {
                PortraitPart(parent, "Travel coat", outfitColor, new Vector2(0, 10), new Vector2(76 * scaleX, 74 * scaleY), RoundedSprite());
                PortraitPart(parent, "Coat lapel", Hex("#D49B55"), new Vector2(0, 36), new Vector2(38 * scaleX, 22), RoundedSprite());
                PortraitPart(parent, "Brass button 1", Hex("#F2BC56"), new Vector2(0, 20), new Vector2(7, 7), CircleSprite());
                PortraitPart(parent, "Brass button 2", Hex("#F2BC56"), new Vector2(0, 2), new Vector2(7, 7), CircleSprite());
                PortraitPart(parent, "Leather belt", Hex("#4C372A"), new Vector2(0, -14), new Vector2(72 * scaleX, 10), RoundedSprite());
                PortraitPart(parent, "Belt buckle", Hex("#F2BC56"), new Vector2(0, -14), new Vector2(12, 12), RoundedSprite());
                PortraitPart(parent, "Satchel strap", Hex("#583C2A"), new Vector2(2, 12), new Vector2(7, 74), RoundedSprite());
                PortraitPart(parent, "Travel satchel", Hex("#704C33"), new Vector2(-38 * scaleX, -16), new Vector2(26, 32), RoundedSprite());
                PortraitPart(parent, "Satchel lock", Hex("#F2BC56"), new Vector2(-38 * scaleX, -16), new Vector2(7, 7), CircleSprite());
            }

            // 5. Arms & Hands
            PortraitPart(parent, "Left sleeve", outfitColor, new Vector2(-42 * scaleX, 16), new Vector2(20 * scaleX, 62), RoundedSprite());
            PortraitPart(parent, "Right sleeve", outfitColor, new Vector2(42 * scaleX, 16), new Vector2(20 * scaleX, 62), RoundedSprite());
            PortraitPart(parent, "Left hand", skin[avatarIndex], new Vector2(-44 * scaleX, -18), new Vector2(15, 15), CircleSprite());
            PortraitPart(parent, "Right hand", skin[avatarIndex], new Vector2(44 * scaleX, -18), new Vector2(15, 15), CircleSprite());

            // 6. Neck, Head, Hair & Face
            PortraitPart(parent, "Neck", skin[avatarIndex], new Vector2(0, 48), new Vector2(18, 16), RoundedSprite());

            float[] hairW = { 68f, 80f, 66f };
            float[] hairH = { 72f, 94f, 76f };
            PortraitPart(parent, "Hair back", hair[avatarIndex], new Vector2(0, 72), new Vector2(hairW[hairStyle], hairH[hairStyle]), CircleSprite());
            if (hairStyle == 2)
                PortraitPart(parent, "Hair bun top", hair[avatarIndex], new Vector2(0, 104), new Vector2(28, 28), CircleSprite());

            float[] faceW = { 56f, 62f, 52f };
            float[] faceH = { 64f, 60f, 70f };
            PortraitPart(parent, "Face", skin[avatarIndex], new Vector2(0, 70), new Vector2(faceW[faceStyle], faceH[faceStyle]), CircleSprite());

            float fringeX = hairStyle == 1 ? 10f : hairStyle == 2 ? 0f : -7f;
            float fringeWidth = hairStyle == 2 ? 58f : hairStyle == 1 ? 46f : 43f;
            PortraitPart(parent, "Fringe", hair[avatarIndex], new Vector2(fringeX, 89), new Vector2(fringeWidth, hairStyle == 2 ? 26f : 22f), RoundedSprite());

            float eyeY = faceStyle == 2 ? 73f : 70f;
            float eyeW = faceStyle == 1 ? 6f : 5f;
            PortraitPart(parent, "Left eye", Hex("#3B2F2A"), new Vector2(-11, eyeY), new Vector2(eyeW, 7), CircleSprite());
            PortraitPart(parent, "Right eye", Hex("#3B2F2A"), new Vector2(11, eyeY), new Vector2(eyeW, 7), CircleSprite());
            PortraitPart(parent, "Nose detail", Hex("#A86F58"), new Vector2(0, 64), new Vector2(4, 4), CircleSprite());
            PortraitPart(parent, "Smile", Hex("#A94A42"), new Vector2(0, faceStyle == 2 ? 56 : 58), new Vector2(faceStyle == 1 ? 12f : 10f, 3), RoundedSprite());

            // 7. Accessories
            int acc = outfitId == 3 ? 1 : outfitId == 4 ? 2 : accessoryStyle;
            if (acc == 1) // Lantern hairpin & tassel
            {
                PortraitPart(parent, "Lantern hairpin", Lantern, new Vector2(28, 94), new Vector2(14, 18), CircleSprite());
                PortraitPart(parent, "Lantern tassel", Vermilion, new Vector2(28, 81), new Vector2(3, 14), RoundedSprite());
            }
            else if (acc == 2) // Hanji flower
            {
                PortraitPart(parent, "Hanji flower ornament", Hex("#FFF5F7"), new Vector2(-28, 94), new Vector2(18, 18), CircleSprite());
                PortraitPart(parent, "Flower center", Vermilion, new Vector2(-28, 94), new Vector2(6, 6), CircleSprite());
            }
        }
        private RectTransform Card(Transform parent, float height)
        {
            var card = Panel("Card", parent, GameUiTheme.Surface).rectTransform;
            SetHeight(card, height);
            // Use the Canvas plane for every nested paper surface; UI shadows provide its depth cue.
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = GameUiTheme.PaperShadow;
            shadow.effectDistance = new Vector2(0f, -4f);
            var border = card.gameObject.AddComponent<Outline>();
            border.effectColor = Border;
            border.effectDistance = new Vector2(1.5f, -1.5f);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(GameUiTheme.SpaceLarge, GameUiTheme.SpaceLarge, GameUiTheme.CardPaddingVertical, GameUiTheme.CardPaddingVertical);
            layout.spacing = GameUiTheme.SpaceSmall;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return card;
        }
        private Text Label(Transform parent, string value, int size, float height, Color color, TextAnchor alignment, FontStyle style = FontStyle.Normal, bool classic = false)
        {
            EnsureFont();
            var rect = NewRect("Text", parent);
            SetHeight(rect, height);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = classic ? displayFont : font;
            label.text = value;
            // The narrowest secondary labels used to render at 22-23 reference
            // pixels, which became hard to read in a phone-width Game view.
            int readableSize = Mathf.Max(GameUiTheme.MinimumBodyFontSize, height >= 55f && size < 32 ? size + 2 : size);
            label.fontSize = readableSize;
            label.fontStyle = classic && displayFont != font ? FontStyle.Normal : style;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            // Keep glyph rasterization at the authored size; Best Fit used to shrink
            // short labels to arbitrary intermediate sizes and made them look soft.
            label.resizeTextForBestFit = false;
            label.resizeTextMinSize = readableSize;
            label.resizeTextMaxSize = readableSize;
            label.raycastTarget = false;
            return label;
        }

        private void EnsureFont()
        {
            if (font == null)
            {
                font = Resources.Load<Font>("TastePassportFont");
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) Debug.LogError("Taste Passport: No UI font is available. Check Resources/TastePassportFont.ttf import.");
            }
            if (displayFont == null)
            {
                displayFont = Resources.Load<Font>("TastePassportSerif");
                if (displayFont == null)
                {
                    displayFont = font;
                    Debug.LogWarning("Taste Passport: Classic display font is unavailable; using the body font.");
                }
            }
        }
        private GameObject Button(Transform parent, string text, Color background, Action action, Color? foreground = null)
        {
            var rect = Panel("Button " + text, parent, background).rectTransform;
            SetHeight(rect, GameUiTheme.ButtonHeight);
            rect.GetComponent<Image>().raycastTarget = true;
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.26f, 0.21f, 0.17f, 0.2f);
            shadow.effectDistance = new Vector2(0, -4);
            var border = rect.gameObject.AddComponent<Outline>();
            border.effectColor = (background == Jade || background == Lantern) ? GoldGlow : new Color(0.35f, 0.26f, 0.20f, 0.24f);
            border.effectDistance = new Vector2(1.5f, -1.5f);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(Color.white, GameUiTheme.Secondary, .12f);
            colors.pressedColor = Color.Lerp(Color.white, GameUiTheme.Ink, .12f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = GameUiTheme.Disabled;
            button.colors = colors;
            button.onClick.AddListener(() => { feedbackSound?.PlayTap(); action(); });
            var label = Label(rect, text, 29, 90, foreground ?? (background == Lantern || background == Paper ? Ink : Color.white), TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(GameUiTheme.SpaceMedium, 0f);
            label.rectTransform.offsetMax = new Vector2(-GameUiTheme.SpaceMedium, 0f);
            rect.gameObject.AddComponent<GameUiButtonMotion>();
            return rect.gameObject;
        }
        private InputField Input(Transform parent, string value, string hint)
        {
            var rect = Panel("Name input", parent, Paper).rectTransform;
            SetHeight(rect, 94);
            rect.GetComponent<Image>().raycastTarget = true;
            var input = rect.gameObject.AddComponent<InputField>();
            input.characterLimit = 20;
            input.contentType = InputField.ContentType.Standard;
            var placeholder = Label(rect, hint, 27, 90, Muted, TextAnchor.MiddleLeft);
            var text = Label(rect, value, 30, 90, Ink, TextAnchor.MiddleLeft);
            foreach (var child in new[] { placeholder.rectTransform, text.rectTransform })
            {
                Stretch(child);
                child.offsetMin = new Vector2(24, 0);
                child.offsetMax = new Vector2(-24, 0);
            }
            input.placeholder = placeholder;
            input.textComponent = text;
            input.text = value;
            return input;
        }
    }

    /// <summary>Subtle breathing and idle micro-animation for the full-body character creator preview.</summary>
    public sealed class CharacterPreviewBobbing : MonoBehaviour
    {
        private RectTransform target;
        private Vector2 origin;
        private Vector3 baseScale = Vector3.one;

        private void Start()
        {
            target = transform as RectTransform;
            if (target != null)
            {
                origin = target.anchoredPosition;
                baseScale = target.localScale;
            }
        }

        private void Update()
        {
            if (target == null) return;
            float time = Time.unscaledTime;
            float breath = Mathf.Sin(time * 2.2f) * 3f;
            target.anchoredPosition = origin + new Vector2(0f, breath);
            float scaleBreath = 1f + Mathf.Sin(time * 2.2f) * 0.012f;
            target.localScale = new Vector3(baseScale.x * scaleBreath, baseScale.y * scaleBreath, 1f);
        }
    }
}
