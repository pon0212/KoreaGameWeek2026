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
    public sealed class PassportApp : MonoBehaviour
    {
        private enum Page { Menu, Language, Create, Passport, Postcard, Map, Wardrobe, Settings, Dialogue, Explore, Assembly, Result, RecipeDialogue, RecipeGame, RecipeMix, RecipeFail, MatchDialogue, MatchGame, Finale, FinaleResult, LanternCraft, LanternResult, HanjiCraft, HanjiResult, Inventory, CultureCards }
        private static readonly Color Cream = GameUiTheme.Cream;
        private static readonly Color Lantern = GameUiTheme.Lantern;
        private static readonly Color Jade = GameUiTheme.Jade;
        private static readonly Color Ink = GameUiTheme.Ink;
        private static readonly Color Paper = GameUiTheme.Paper;
        private static readonly Color Vermilion = GameUiTheme.Vermilion;
        private static readonly Color Border = GameUiTheme.Border;
        private static readonly Color Muted = GameUiTheme.Muted;
        private readonly Color[] avatarColors = { Lantern, Jade, Vermilion };
        private static Sprite roundedSprite;
        private static Sprite circleSprite;

        private ProfileStore store;
        private PlayerProfile profile;
        private Canvas canvas;
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
        private bool editingProfile;
        private string notice;
        private Rect lastSafeArea;
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
        private Coroutine pageFade;
        private bool resetScroll = true;
        private bool koreanBackdrop;
        private bool resultCommitted;
        private FestivalPreparationRun festivalRun;
        private Coroutine finaleReveal;
        private WalkableWorldSession worldSession;
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
            page = Page.Menu;
            Show();
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
            if (Screen.safeArea != lastSafeArea) UpdateSafeArea();
            if (page == Page.RecipeGame && recipeRun != null && !recipeRun.Complete)
            {
                int remaining = Mathf.Max(0, Mathf.CeilToInt(recipeDeadline - Time.time));
                if (recipeTimer != null) recipeTimer.text = T("time_left") + ": " + remaining + "s";
                if (remaining == 0) Navigate(Page.RecipeFail);
            }
        }

        private void EnsureCanvas()
        {
            canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                var obj = new GameObject("Passport Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                obj.transform.SetParent(transform, false);
                canvas = obj.GetComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();

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
            Rect area = Screen.safeArea;
            lastSafeArea = area;
            safeRoot.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
        }

        private void Navigate(Page target)
        {
            notice = null;
            if (target == Page.Passport) audioManager?.PlayCue(GameSoundCue.BookOpen);
            page = target;
            resetScroll = true;
            Show();
        }

        private void Show()
        {
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
            var previousScroll = safeRoot.Find("Page Scroll")?.GetComponent<ScrollRect>();
            float savedScroll = resetScroll || previousScroll == null ? 1f : previousScroll.verticalNormalizedPosition;
            for (int i = safeRoot.childCount - 1; i >= 0; i--) Destroy(safeRoot.GetChild(i).gameObject);
            var backdrop = Panel("Destination Background", safeRoot, Color.white, true);
            string backdropPath = koreanBackdrop ? "Art/KoreaJourney" : "Art/VietnamJourney";
            backdrop.sprite = Resources.Load<Sprite>(backdropPath);
            backdrop.type = Image.Type.Simple;
            if (koreanBackdrop) KoreaVisuals.Attach(backdrop.rectTransform, TravelArtworkKind.KoreaJourney);
            else HoiAnVisuals.Attach(backdrop.rectTransform, TravelArtworkKind.VietnamJourney);
            // Keep the supplied destination illustration recognizable between cards.
            // Cards stay opaque so the foreground copy and controls remain crisp.
            backdrop.color = new Color(1f, 1f, 1f, backdrop.sprite == null ? 1f : .82f);
            Panel("Paper wash", safeRoot, new Color(Cream.r, Cream.g, Cream.b, backdrop.sprite == null ? 1f : .38f), true);
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
            scroll.vertical = true;
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
            layout.padding = new RectOffset(36, 36, 32, 64);
            layout.spacing = 18;
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
                case Page.Menu: DrawMenu(content); break;
                case Page.Language: DrawLanguage(content); break;
                case Page.Create: DrawCreate(content); break;
                case Page.Passport: DrawPassport(content); break;
                case Page.Postcard: DrawPostcard(content); break;
                case Page.Map: DrawMap(content); break;
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
            if (!string.IsNullOrEmpty(notice))
            {
                Toast(notice);
                notice = null;
            }
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = savedScroll;
            resetScroll = false;
            if (pageFade != null) StopCoroutine(pageFade);
            pageFade = StartCoroutine(FadePage(fadeGroup));
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
            Header(parent, T("subtitle"));
            var welcome = JourneyCard(parent);
            Label(welcome, T(profile.created ? "journey_welcome_back" : "journey_first_run"), 30, 66, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(welcome, T(profile.created ? "journey_resume_hint" : "journey_setup_hint"), 26, 92, Muted, TextAnchor.MiddleCenter);
            Button(welcome, profile.created ? T("continue") : T("start"), Jade, () =>
            {
                if (!profile.created)
                {
                    Navigate(Page.Language);
                    return;
                }

                string lastWorld = profile.lastWorldId;
                if (lastWorld == "hoian" || (lastWorld == "jeonju" && IsJeonjuUnlocked(profile)))
                    OpenWorld(lastWorld);
                else Navigate(Page.Map);
            });
            JourneyNavigation(parent, Page.Menu);
            var spread = JourneyCard(parent);
            Label(spread, T("living_passport"), 26, 84, Jade, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            var destinations = NewRect("Two culture destinations", spread);
            SetHeight(destinations, 184);
            AddHorizontal(destinations);
            Artwork(destinations, TravelArtworkKind.HoiAn, 184);
            Artwork(destinations, TravelArtworkKind.Jeonju, 184);
            AddJourneyThread(spread);
            Label(spread, T("tagline"), 26, 92, Ink, TextAnchor.MiddleCenter);
            NarrationButtons(parent, T("story_full"));
            CompactButton(parent, T("language"), Paper, () => Navigate(Page.Language), Ink);
        }

        private void DrawLanguage(Transform parent)
        {
            Header(parent, T("choose_language"));
            Label(parent, T(profile.created ? "journey_language_hint" : "journey_language_step"), 26, 92, Muted, TextAnchor.MiddleCenter);
            var languages = JourneyCard(parent);
            Button(languages, T("language_vi") + (profile.language == "vi" ? "  ✓" : ""), profile.language == "vi" ? Lantern : Paper, () => ChangeLanguage("vi"), Ink);
            Button(languages, T("language_ko") + (profile.language == "ko" ? "  ✓" : ""), profile.language == "ko" ? Lantern : Paper, () => ChangeLanguage("ko"), Ink);
            Button(parent, T(profile.created ? "open_map" : "journey_to_profile"), Jade, () => Navigate(profile.created ? Page.Map : Page.Create));
            CompactButton(parent, T("back"), Paper, () => Navigate(Page.Menu), Ink);
        }

        private void DrawCreate(Transform parent)
        {
            Header(parent, editingProfile ? T("edit_profile") : T("create"));
            Label(parent, T(editingProfile ? "journey_profile_hint" : "journey_profile_step"), 26, 92, Muted, TextAnchor.MiddleCenter);
            Label(parent, T("name_label"), 29, 50, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var field = Input(parent, draftName, T("name_hint"));
            field.onValueChanged.AddListener(value => draftName = value);
            Label(parent, T("avatar_label"), 29, 58, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var row = NewRect("Avatar choices", parent);
            SetHeight(row, 184);
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
                var avatar = Button(row, T("avatar_" + i) + (draftAvatar == i ? " ✓" : ""), draftAvatar == i ? Lantern : Paper, () => { draftAvatar = chosen; Show(); }, Ink);
                SetHeight(avatar.GetComponent<RectTransform>(), 174);
                var caption = avatar.transform.Find("Text") as RectTransform;
                caption.anchorMin = new Vector2(0, 0);
                caption.anchorMax = new Vector2(1, 0);
                caption.pivot = new Vector2(0.5f, 0);
                caption.offsetMin = Vector2.zero;
                caption.offsetMax = new Vector2(0, 55);
                DrawAvatar(avatar.transform, i);
            }
            Spacer(parent, 20);
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
            Navigate(Page.Create);
        }

        private void CreateProfile()
        {
            string clean = (draftName ?? "").Trim();
            if (clean.Length < 1 || clean.Length > 20) { notice = T("name_error"); Show(); return; }
            var candidate = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
            candidate.playerName = clean;
            candidate.avatarIndex = draftAvatar;
            candidate.created = true;
            if (!store.TrySave(candidate)) { notice = T("save_error"); Show(); return; }
            profile = candidate;
            if (editingProfile)
            {
                editingProfile = false;
                Navigate(profileEditReturnPage);
                return;
            }
            OpenWorld("hoian");
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
            TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var label = Label(parent, value, size, 0, color, alignment, style);
            var layout = label.GetComponent<LayoutElement>();
            layout.minHeight = -1;
            layout.preferredHeight = -1;
            return label;
        }

        private void DrawPassport(Transform parent)
        {
            RecordTutorialPassportView();
            Header(parent, T("passport"));
            var identity = JourneyCard(parent);
            Artwork(identity, TravelArtworkKind.Passport, 160);
            CollectionCopy(identity, profile.playerName, 36, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(identity, T("passport_intro"), 26, Muted, TextAnchor.MiddleCenter);
            CollectionCopy(identity, T("avatar_" + profile.avatarIndex), 26, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(identity, T("coins") + ": " + profile.coins + " ◆", 28, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
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
                Label(parent, T("craft_completed_pending_save"), 27, 112, Ink, TextAnchor.MiddleCenter);
                Button(parent, T("save_result"), Jade, CompleteLanternCraft);
                Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
                return;
            }
            Label(parent, T("lantern_craft_intro"), 27, 108, Ink, TextAnchor.MiddleCenter);
            DrawLanternPreview(parent, 230);
            if (!string.IsNullOrEmpty(lanternCraftRun.FrameId))
                Label(parent, T("frame_" + lanternCraftRun.FrameId) +
                    (string.IsNullOrEmpty(lanternCraftRun.ColorId) ? "" : " · " + T("color_" + lanternCraftRun.ColorId)),
                    25, 48, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);

            var frameCard = Card(parent, 245);
            Label(frameCard, T("craft_choose_frame"), 28, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            var frameRow = NewRect("Lantern frame choices", frameCard);
            SetHeight(frameRow, 104);
            AddHorizontal(frameRow);
            foreach (string frameId in new[] { "round", "diamond", "hexagon" })
            {
                string chosenFrame = frameId;
                Button(frameRow, T("frame_" + frameId) + (lanternCraftRun.FrameId == frameId ? " ✓" : ""),
                    lanternCraftRun.FrameId == frameId ? Jade : Paper,
                    () => SelectLanternFrame(chosenFrame), lanternCraftRun.FrameId == frameId ? Color.white : Ink);
            }

            var colorCard = Card(parent, 350);
            Label(colorCard, T("craft_choose_color"), 28, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
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
                    Button(colorRow, T("color_" + colorId) + (isSelected ? " ✓" : ""),
                        isSelected ? ColorForLantern(colorId) : Paper,
                        () => SelectLanternColor(chosenColor), isSelected ? Color.white : Ink);
                }
            }

            var materialsCard = Card(parent, 380);
            Label(materialsCard, T("craft_materials"), 28, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(materialsCard, T("craft_drag_hint"), 23, 55, Muted, TextAnchor.MiddleCenter);
            var materialRow = NewRect("Lantern craft materials", materialsCard);
            SetHeight(materialRow, 112);
            AddHorizontal(materialRow);
            AddLanternMaterial(materialRow, LanternCraftRun.FabricMaterialId, "material_fabric");
            AddLanternMaterial(materialRow, LanternCraftRun.TasselMaterialId, "material_tassel");
            lanternMaterialStatus = Label(materialsCard,
                string.IsNullOrEmpty(selectedLanternMaterialId) ? T("craft_material_unselected") : T("craft_selected_material") + ": " + T("material_" + selectedLanternMaterialId),
                23, 42, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);

            var slotsRow = NewRect("Lantern craft drop zones", parent);
            SetHeight(slotsRow, 230);
            AddHorizontal(slotsRow);
            AddLanternDropZone(slotsRow, LanternCraftRun.FabricSlotId, "craft_slot_fabric", lanternCraftRun.FabricPlaced);
            AddLanternDropZone(slotsRow, LanternCraftRun.TasselSlotId, "craft_slot_tassel", lanternCraftRun.TasselPlaced);

            var processCard = Card(parent, 190);
            Label(processCard, T("lantern_craft_process_title"), 26, 43, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(processCard, T("lantern_craft_process"), 24, 125, Ink, TextAnchor.MiddleCenter);
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
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
            string slotLabel = T(labelKey) + (filled ? " ✓" : "\n" + T("craft_drop_here"));
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
            DrawLanternPreview(parent, 300, InventoryService.GetLanternColorId(lanternResultItemId));
            Label(parent, T("lantern_result_copy"), 28, 90, Ink, TextAnchor.MiddleCenter);
            Label(parent, T("lantern_item") + ": " + LanternVariantLabel(lanternResultItemId), 27, 74, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button(parent, T("inventory"), Jade, () => Navigate(Page.Inventory));
            Button(parent, T("culture_cards"), Lantern, () => Navigate(Page.CultureCards));
            Button(parent, T("continue_free_exploration"), Jade, () => OpenWorld("hoian"));
            Button(parent, T("craft_again"), Paper, StartLanternCraft, Ink);
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
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
            Label(parent, T("hanji_craft_intro"), 27, 86, Ink, TextAnchor.MiddleCenter);
            DrawHanjiPreview(parent, 250, hanjiCraftRun.ShapeId, hanjiCraftRun.PaperColorId);
            if (!string.IsNullOrEmpty(hanjiCraftRun.ShapeId))
                Label(parent, HanjiVariantLabel(hanjiCraftRun.ShapeId, hanjiCraftRun.PaperColorId, hanjiCraftRun.MotifId),
                    25, 52, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);

            var colorCard = Card(parent, 300);
            Label(colorCard, T("hanji_choose_color"), 28, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int rowIndex = 0; rowIndex < 2; rowIndex++)
            {
                var row = NewRect("Hanji paper color choices", colorCard);
                SetHeight(row, 82);
                AddHorizontal(row);
                for (int column = 0; column < 2; column++)
                {
                    string colorId = new[] { "indigo", "vermilion", "gold", "jade" }[rowIndex * 2 + column];
                    bool selected = hanjiCraftRun.PaperColorId == colorId;
                    Button(row, T("hanji_color_" + colorId) + (selected ? " ✓" : ""),
                        selected ? ColorForHanji(colorId) : Paper,
                        () => SelectHanjiColor(colorId), selected ? Color.white : Ink);
                }
            }

            var shapeCard = Card(parent, 250);
            Label(shapeCard, T("hanji_choose_shape"), 28, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            var shapeRow = NewRect("Hanji product shape choices", shapeCard);
            SetHeight(shapeRow, 132);
            AddHorizontal(shapeRow);
            foreach (string shapeId in new[] { "fan", "bookmark", "flower" })
            {
                string chosenShape = shapeId;
                bool selected = hanjiCraftRun.ShapeId == shapeId;
                Button(shapeRow, T("hanji_shape_" + shapeId) + (selected ? " ✓" : ""),
                    selected ? Jade : Paper, () => SelectHanjiShape(chosenShape), selected ? Color.white : Ink);
            }

            var motifCard = Card(parent, 210);
            Label(motifCard, T("hanji_choose_motif"), 28, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            var motifRow = NewRect("Hanji motif choices", motifCard);
            SetHeight(motifRow, 108);
            AddHorizontal(motifRow);
            foreach (string motifId in new[] { "flower", "cloud", "stripe" })
            {
                string chosenMotif = motifId;
                bool selected = hanjiCraftRun.MotifId == motifId;
                Button(motifRow, T("hanji_motif_" + motifId) + (selected ? " ✓" : ""),
                    selected ? Lantern : Paper, () => SelectHanjiMotif(chosenMotif), selected ? Color.white : Ink);
            }

            var processCard = Card(parent, 360);
            Label(processCard, T("hanji_process_title"), 27, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(processCard, T("hanji_process_copy"), 24, 88, Ink, TextAnchor.MiddleCenter);
            Button(processCard, T("hanji_cut") + StepMark(hanjiCraftRun.CutApplied),
                hanjiCraftRun.CutApplied ? Jade : Paper, () => ApplyHanjiAction("cut"), hanjiCraftRun.CutApplied ? Color.white : Ink);
            Button(processCard, T("hanji_fold") + StepMark(hanjiCraftRun.FoldApplied),
                hanjiCraftRun.FoldApplied ? Jade : Paper, () => ApplyHanjiAction("fold"), hanjiCraftRun.FoldApplied ? Color.white : Ink);
            Button(processCard, T("hanji_decorate") + StepMark(hanjiCraftRun.DecorationApplied),
                hanjiCraftRun.DecorationApplied ? Jade : Paper, () => ApplyHanjiAction("decorate"), hanjiCraftRun.DecorationApplied ? Color.white : Ink);
            if (hanjiCraftRun.Complete)
            {
                Label(processCard, T("hanji_completed_pending_save"), 23, 54, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
                Button(processCard, T("save_result"), Jade, CompleteHanjiCraft);
            }
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private static string StepMark(bool applied) => applied ? "  ✓" : "";

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
            DrawHanjiPreview(parent, 300, InventoryService.GetHanjiShapeId(hanjiResultItemId),
                InventoryService.GetHanjiColorId(hanjiResultItemId));
            Label(parent, T("hanji_result_copy"), 28, 88, Ink, TextAnchor.MiddleCenter);
            Label(parent, T("hanji_item") + ": " + HanjiVariantLabel(hanjiResultItemId),
                27, 74, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button(parent, T("inventory"), Jade, () => Navigate(Page.Inventory));
            Button(parent, T("culture_cards"), Lantern, () => Navigate(Page.CultureCards));
            Button(parent, T("hanji_again"), Paper, StartHanjiCraft, Ink);
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
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
                Label(card, "◆  ◆  ◆", 28, 44, Vermilion, TextAnchor.MiddleCenter);
            }
            else
            {
                Label(card, "◆  " + T("passport"), 25, 45, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            var heading = JourneyCard(parent);
            Label(heading, T("map"), 38, 60, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Label(heading, T("journey_progress") + "  " + JourneyStampCount(LevelCatalog.HoiAn.Id, RecipeCatalog.CaoLau.Id, MatchCatalog.HoiAn.Id,
                LevelCatalog.Jeonju.Id, RecipeCatalog.Bibimbap.Id, MatchCatalog.Jeonju.Id) + "/6", 26, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            MapObjectiveDefinition nextObjective = MapObjectiveService.GetCurrent(profile, IsJeonjuUnlocked(profile));
            var objectiveCard = JourneyCard(parent);
            Label(objectiveCard, T("map_next_step"), 24, 42, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            var objectiveTitle = Label(objectiveCard, T(nextObjective.TitleKey), 30, 86, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            // Some existing objective keys contain a full mission introduction.
            // Measure that text at the available width instead of truncating it.
            var titleSize = objectiveTitle.GetComponent<LayoutElement>();
            titleSize.minHeight = -1;
            titleSize.preferredHeight = -1;
            Label(objectiveCard, T(nextObjective.DescriptionKey), 26, 112, Muted, TextAnchor.MiddleCenter);
            Button(objectiveCard, T(nextObjective.ActionLabelKey), Jade, () => RunMapObjective(nextObjective));
            JourneyNavigation(parent, Page.Map);
            Label(parent, T("journey_destinations"), 30, 54, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var hoian = JourneyCard(parent);
            Artwork(hoian, TravelArtworkKind.VietnamJourney, 150);
            Label(hoian, T("hoian"), 38, 48, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Label(hoian, T("hoian_desc"), 26, 42, Muted, TextAnchor.MiddleCenter);
            DestinationProgress(hoian, LevelCatalog.HoiAn.Id, RecipeCatalog.CaoLau.Id, MatchCatalog.HoiAn.Id);
            CompactButton(hoian, T(showHoiAnActivities ? "journey_hide_activities" : "journey_show_activities"), Paper,
                () => { showHoiAnActivities = !showHoiAnActivities; Show(); }, Ink);
            if (showHoiAnActivities)
            {
                Label(hoian, T("journey_explore_craft"), 28, 52, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                CompactButton(hoian, T("explore_hoian"), Paper, () => OpenWorld("hoian"), Ink);
                CompactButton(hoian, MissionLabel("journey_hoian_architecture", LevelCatalog.HoiAn.Id), Paper, () => StartLevel(LevelCatalog.HoiAn), Ink);
                CompactButton(hoian, T("lantern_craft_map"), Paper, StartLanternCraft, Ink);
                Label(hoian, T("journey_food_exchange"), 28, 52, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                JourneyMissionButton(hoian, "cao_lau", RecipeCatalog.CaoLau.Id, LevelCatalog.HoiAn.Id, "journey_hoian_architecture", () => StartRecipe(RecipeCatalog.CaoLau));
                JourneyMissionButton(hoian, "hoian_exchange", MatchCatalog.HoiAn.Id, RecipeCatalog.CaoLau.Id, "cao_lau", () => StartMatch(MatchCatalog.HoiAn));
            }
            HoiAnVisuals.DecorateLocationCard(hoian);
            var jeonju = JourneyCard(parent);
            Artwork(jeonju, TravelArtworkKind.KoreaJourney, 150);
            Label(jeonju, T("jeonju"), 38, 48, Ink, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Label(jeonju, T("jeonju_desc"), 26, 42, Muted, TextAnchor.MiddleCenter);
            DestinationProgress(jeonju, LevelCatalog.Jeonju.Id, RecipeCatalog.Bibimbap.Id, MatchCatalog.Jeonju.Id);
            bool jeonjuUnlocked = IsJeonjuUnlocked(profile);
            if (!jeonjuUnlocked) Label(jeonju, T("locked") + " · " + T("journey_jeonju_requirement"), 26, 122, Muted, TextAnchor.MiddleCenter);
            CompactButton(jeonju, T(showJeonjuActivities ? "journey_hide_activities" : "journey_show_activities"), Paper,
                () => { showJeonjuActivities = !showJeonjuActivities; Show(); }, Ink);
            if (showJeonjuActivities)
            {
                Label(jeonju, T("journey_explore_craft"), 28, 52, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                CompactButton(jeonju, T("explore_jeonju") + (jeonjuUnlocked ? "" : " · " + T("locked")), jeonjuUnlocked ? Paper : Border,
                    jeonjuUnlocked ? () => OpenWorld("jeonju") : () => Toast(T("jeonju_not_unlocked")), Ink);
                JourneyMissionButton(jeonju, "journey_jeonju_architecture", LevelCatalog.Jeonju.Id, MatchCatalog.HoiAn.Id, "hoian_exchange", () => StartLevel(LevelCatalog.Jeonju));
                CompactButton(jeonju, T("hanji_craft_map") + (jeonjuUnlocked ? "" : " · " + T("locked")), jeonjuUnlocked ? Paper : Border,
                    jeonjuUnlocked ? StartHanjiCraft : () => Toast(T("jeonju_not_unlocked")), Ink);
                Label(jeonju, T("journey_food_exchange"), 28, 52, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                JourneyMissionButton(jeonju, "bibimbap", RecipeCatalog.Bibimbap.Id, LevelCatalog.Jeonju.Id, "journey_jeonju_architecture", () => StartRecipe(RecipeCatalog.Bibimbap));
                JourneyMissionButton(jeonju, "jeonju_exchange", MatchCatalog.Jeonju.Id, RecipeCatalog.Bibimbap.Id, "bibimbap", () => StartMatch(MatchCatalog.Jeonju));
            }
            KoreaVisuals.DecorateLocationCard(jeonju);
            var storyCard = JourneyCard(parent);
            Label(storyCard, profile.mainStoryCompleted ? T("main_story_completed") : T("chapter_title_4"),
                29, 86, profile.mainStoryCompleted ? Jade : Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (profile.mainStoryCompleted)
            {
                Label(storyCard, T("main_story_complete_map"), 24, 66, Ink, TextAnchor.MiddleCenter);
                Button(storyCard, T("view_ending"), Jade, () => Navigate(Page.FinaleResult));
            }
            else if (FirstJourneyComplete())
            {
                Label(storyCard, T("festival_ready_intro"), 26, 112, Ink, TextAnchor.MiddleCenter);
                Button(storyCard, T("finale_start"), Vermilion, StartFinale);
            }
            else Label(storyCard, T("finale_locked_hint"), 26, 144, Muted, TextAnchor.MiddleCenter);
        }

        private int JourneyStampCount(params string[] missionIds)
        {
            int count = 0;
            foreach (string id in missionIds)
                if (ProgressService.HasStamp(profile, id)) count++;
            return count;
        }

        private void DestinationProgress(Transform parent, params string[] missionIds)
        {
            Label(parent, T("journey_destination_progress") + "  " + JourneyStampCount(missionIds) + "/3", 26, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private void JourneyMissionButton(Transform parent, string titleKey, string missionId, string prerequisiteId, string prerequisiteTitleKey, Action action)
        {
            MissionButton(parent, titleKey, missionId, prerequisiteId, Paper, action, Ink, prerequisiteTitleKey);
        }

        private void RunMapObjective(MapObjectiveDefinition objective)
        {
            if (objective == null) return;
            switch (objective.Action)
            {
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
                worldSession.Open(worldId, profile, store, ReturnFromWorld, HandleWorldActivity, OpenPassportFromWorld);
                bool koreanWorld = worldId == "jeonju";
                audioManager?.SetEnvironment(koreanWorld);
                PlayStageAudio(Mathf.Clamp(profile.chapterIndex, 1, 3), koreanWorld);
            }
            catch (Exception error)
            {
                safeRoot.gameObject.SetActive(true);
                Debug.LogException(error);
                notice = T("world_open_error");
                Show();
            }
        }

        private void ReturnFromWorld()
        {
            if (safeRoot != null) safeRoot.gameObject.SetActive(true);
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
                CollectionCopy(card, "◆  " + T(ImprintNameKey(imprint)), 26, Ink, TextAnchor.MiddleCenter);
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
            CollectionHeader(parent, "wardrobe", "collection_wardrobe_hint", T("coins") + ": " + profile.coins + " ◆");
            for (int i = 0; i < OutfitCatalog.All.Length; i++)
            {
                var outfit = OutfitCatalog.All[i];
                var card = JourneyCard(parent);
                var preview = NewRect("Outfit preview", card);
                SetHeight(preview, 150);
                DrawAvatar(preview, OutfitCatalog.Get(profile.outfitId).Avatar, outfit.Id);
                CollectionCopy(card, T(outfit.NameKey), 29, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                CollectionCopy(card, outfit.IsOwned(profile) ? T("outfit_owned") : T("outfit_price") + ": " + outfit.Price + " ◆", 26, Muted, TextAnchor.MiddleCenter);
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
            CollectionHeader(parent, "settings", "settings_intro");
            var language = JourneyCard(parent);
            CollectionCopy(language, T("settings_language"), 29, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            CollectionCopy(language, T("settings_language_hint"), 26, Muted);
            Button(language, T("language_vi") + (profile.language == "vi" ? "  ✓" : ""), profile.language == "vi" ? Lantern : Paper, () => ChangeLanguage("vi"), Ink);
            Button(language, T("language_ko") + (profile.language == "ko" ? "  ✓" : ""), profile.language == "ko" ? Lantern : Paper, () => ChangeLanguage("ko"), Ink);
            var sound = JourneyCard(parent);
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
            Artwork(parent, TravelArtworkKind.Friendship, 150);
            Label(parent, T("finale_clue"), 27, 112, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(parent, T("festival_progress") + ": " + festivalRun.PlacedCount + "/" + FestivalPreparationRun.StationCount +
                "  ·  " + festivalRun.DecoratedCount + "/" + FestivalPreparationRun.StationCount +
                "  ·  " + festivalRun.ConnectedCount + "/" + FestivalPreparationRun.LinkCount,
                25, 62, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);

            var markCard = Card(parent, 380);
            Label(markCard, T("festival_marks_title"), 28, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
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
                    var choice = Button(choices, T("finale_mark_" + mark) + (selected ? " ✓" : ""),
                        selected ? Jade : Paper, () => SelectFestivalMark(selectedMark), selected ? Color.white : Ink);
                    choice.GetComponent<UnityEngine.UI.Button>().interactable = !festivalRun.IsMarkPlaced(mark);
                }
            }

            var decorationCard = Card(parent, 220);
            Label(decorationCard, T("festival_decor_title"), 28, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(decorationCard, T("festival_decor_hint"), 23, 56, Muted, TextAnchor.MiddleCenter);
            var decorations = NewRect("Festival decoration choices", decorationCard);
            SetHeight(decorations, 92);
            AddHorizontal(decorations);
            for (int i = 0; i < FestivalPreparationRun.DecorationCount; i++)
            {
                string decorationId = FestivalPreparationRun.GetDecorationId(i);
                bool selected = festivalRun.SelectedDecorationId == decorationId;
                Button(decorations, T("festival_decoration_" + decorationId) + (selected ? " ✓" : ""),
                    selected ? Lantern : Paper, () => SelectFestivalDecoration(decorationId), selected ? Color.white : Ink);
            }

            for (int station = 0; station < FestivalPreparationRun.StationCount; station++)
                DrawFestivalStation(parent, station);

            if (festivalRun.PlacedCount == FestivalPreparationRun.StationCount &&
                festivalRun.DecoratedCount == FestivalPreparationRun.StationCount)
                DrawFestivalConnections(parent);
            else
                Label(parent, T("festival_finish_stations_hint"), 24, 72, Muted, TextAnchor.MiddleCenter);
            if (festivalRun.Complete)
                Button(parent, T("festival_finish_action"), Jade, CompleteFestival);
            Button(parent, T("back_map"), Paper, () => { festivalRun = null; Navigate(Page.Map); }, Ink);
        }

        private void DrawFestivalStation(Transform parent, int stationIndex)
        {
            string stationId = FestivalPreparationRun.GetStationId(stationIndex);
            var card = Card(parent, 210);
            Label(card, T("festival_station_" + stationId), 28, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (festivalRun.IsStationPlaced(stationIndex))
            {
                int mark = festivalRun.GetPlacedMarkForStation(stationIndex);
                Label(card, "◆  " + T("finale_mark_" + mark), 24, 44, Ink, TextAnchor.MiddleCenter);
            }
            else Label(card, T("festival_station_empty"), 23, 40, Muted, TextAnchor.MiddleCenter);

            if (!festivalRun.IsStationPlaced(stationIndex))
            {
                var place = Button(card, T("festival_place_action"), Paper,
                    () => PlaceFestivalMark(stationIndex), Ink);
                place.GetComponent<UnityEngine.UI.Button>().interactable = festivalRun.SelectedMarkIndex >= 0;
            }
            else if (festivalRun.IsStationDecorated(stationIndex))
            {
                Label(card, T("festival_station_decorated") + ": " +
                    T("festival_decoration_" + festivalRun.GetDecorationForStation(stationIndex)),
                    23, 48, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            Artwork(parent, TravelArtworkKind.Friendship, 190);
            var festivalCard = Card(parent, 150);
            var festivalGroup = festivalCard.gameObject.AddComponent<CanvasGroup>();
            festivalGroup.alpha = 0f;
            Label(festivalCard, T("festival_scene_title"), 31, 58, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(festivalCard, T("festival_corners_complete"), 25, 68, Ink, TextAnchor.MiddleCenter);

            var friendsCard = Card(parent, 220);
            var friendsGroup = friendsCard.gameObject.AddComponent<CanvasGroup>();
            friendsGroup.alpha = 0f;
            Label(friendsCard, T("festival_friends_title"), 28, 46, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            var guests = NewRect("Festival returning friends", friendsCard);
            SetHeight(guests, 148);
            AddHorizontal(guests);
            AddFestivalFriend(guests, "character_nhung", "Art/Characters/Nhung");
            AddFestivalFriend(guests, "character_gabo", "Art/Characters/GaBo");
            AddFestivalFriend(guests, "character_lan", "Art/Characters/Lan");
            AddFestivalFriend(guests, "character_kim", "Art/Characters/BKim");

            var endingCard = Card(parent, 560);
            var endingGroup = endingCard.gameObject.AddComponent<CanvasGroup>();
            endingGroup.alpha = 0f;
            Label(endingCard, T("finale_reveal"), 29, 300, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            NarrationButtons(endingCard, T("finale_reveal"));
            Label(parent, T("main_story_completed"), 27, 58, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button(parent, T("continue_free_exploration"), Jade, () => Navigate(Page.Map));
            Button(parent, T("passport"), Paper, () => Navigate(Page.Passport), Ink);
            finaleReveal = StartCoroutine(RevealFestivalEnding(festivalGroup, friendsGroup, endingGroup));
        }

        private void AddFestivalFriend(Transform parent, string nameKey, string spritePath)
        {
            var friend = NewRect("Festival friend - " + nameKey, parent);
            var size = friend.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = 130;
            size.minWidth = 100;
            size.preferredHeight = 144;
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
            Label(friend, T(nameKey), 22, 34, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            var card = Card(parent, 680);
            Label(card, T("festival_link_title"), 29, 52, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(card, T("festival_link_hint"), 24, 68, Ink, TextAnchor.MiddleCenter);
            Label(card, T("festival_links_progress") + ": " + festivalRun.ConnectedCount + "/" + FestivalPreparationRun.LinkCount,
                25, 46, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            var vietnam = NewRect("Vietnam activity link choices", card);
            SetHeight(vietnam, 98);
            AddHorizontal(vietnam);
            for (int i = 0; i < FestivalPreparationRun.LinkCount; i++)
            {
                string activityId = FestivalPreparationRun.GetVietnamActivityId(i);
                bool connected = festivalRun.IsLinkConnected(FestivalPreparationRun.GetLinkId(i));
                bool selected = festivalRun.SelectedVietnamActivityId == activityId;
                var choice = Button(vietnam, T("festival_activity_vi_" + activityId) + (selected ? " ✓" : ""),
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
                var choice = Button(korea, T("festival_activity_ko_" + activityId) + (selected ? " ✓" : ""),
                    selected ? Lantern : Paper, () => SelectFestivalActivity(false, activityId), selected ? Color.white : Ink);
                choice.GetComponent<UnityEngine.UI.Button>().interactable = !connected;
            }
            for (int i = 0; i < FestivalPreparationRun.LinkCount; i++)
            {
                string pairId = FestivalPreparationRun.GetLinkId(i);
                Label(card, (festivalRun.IsLinkConnected(pairId) ? "◆  " : "○  ") + T("festival_pair_" + pairId),
                    23, 40, festivalRun.IsLinkConnected(pairId) ? Jade : Muted, TextAnchor.MiddleCenter);
            }
            Button(card, T("festival_connect"), Vermilion, ConnectFestivalActivities);
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
            Button(parent, T("read_story"), Jade, () => StoryNarration.Speak(profile.language, text));
            Button(parent, T("stop_voice"), Paper, StoryNarration.Stop, Ink);
        }

        private void DrawDialogue(Transform parent)
        {
            Header(parent, T(run.Level.PlaceKey));
            Artwork(parent, run.Level == LevelCatalog.HoiAn ? TravelArtworkKind.HoiAn : TravelArtworkKind.Jeonju, 230);
            SpeakerBadge(parent, T(run.Level == LevelCatalog.HoiAn ? "character_nhung" : "character_gabo"));
            var card = Card(parent, 220);
            Label(card, T(run.Level.GuideKey), 31, 172, Ink, TextAnchor.MiddleCenter);
            NarrationButtons(parent, T(run.Level.GuideKey));
            Label(parent, "◆  " + T("tomorrow_note"), 23, 96, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button(parent, T("mission_start"), Jade, () => Navigate(Page.Explore));
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void DrawExplore(Transform parent)
        {
            Header(parent, T(run.Level.PlaceKey));
            Artwork(parent, run.Level == LevelCatalog.HoiAn ? TravelArtworkKind.HoiAn : TravelArtworkKind.Jeonju, 170);
            Label(parent, T("search_hint"), 27, 75, Ink, TextAnchor.MiddleCenter);
            ProgressStrip(parent, run.FoundCount, 3, T("fragments"));
            for (int row = 0; row < 3; row++)
            {
                var rowRect = NewRect("Places", parent);
                SetHeight(rowRect, 145);
                var layout = rowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 14;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                for (int column = 0; column < 2; column++)
                {
                    int index = row * 2 + column;
                    bool inspected = run.IsInspected(index);
                    var place = Button(rowRect, T(run.Level.ObjectKeys[index]) + (inspected ? " ✓" : " ?"), inspected ? Jade : Lantern, () => InspectPlace(index));
                    SetHeight(place.GetComponent<RectTransform>(), 137);
                }
            }
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void InspectPlace(int index)
        {
            InspectResult result = run.Inspect(index);
            if (run.SearchComplete) { Navigate(Page.Assembly); return; }
            notice = T(result == InspectResult.Fragment ? "fragment_found" : result == InspectResult.Empty ? "empty_spot" : "already_searched");
            Show();
        }

        private void DrawAssembly(Transform parent)
        {
            Header(parent, T("assemble_title"));
            Artwork(parent, run.Level == LevelCatalog.HoiAn ? TravelArtworkKind.HoiAn : TravelArtworkKind.Jeonju, 150);
            Label(parent, T("assemble_hint"), 26, 120, Ink, TextAnchor.MiddleCenter);
            ProgressStrip(parent, (placedFragments[0] ? 1 : 0) + (placedFragments[1] ? 1 : 0) + (placedFragments[2] ? 1 : 0), 3, T("fragments"));
            pieceViews = new FragmentDrag[3];
            slotViews = new Transform[3];
            var pieces = NewRect("Fragments", parent);
            SetHeight(pieces, 128);
            AddHorizontal(pieces);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                if (placedFragments[i]) { Spacer(pieces, 112); continue; }
                var piece = Button(pieces, T("piece") + " " + (i + 1) + (selectedFragment == i ? " ✓" : ""), Lantern, () => { selectedFragment = index; Show(); });
                SetHeight(piece.GetComponent<RectTransform>(), 112);
                pieceViews[i] = piece.AddComponent<FragmentDrag>();
                pieceViews[i].PieceIndex = i;
            }
            var slots = NewRect("Postcard slots", parent);
            SetHeight(slots, 128);
            AddHorizontal(slots);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var slot = Button(slots, placedFragments[i] ? "✓" : T("slot") + " " + (i + 1), placedFragments[i] ? Jade : Paper,
                    () => TryPlace(index, null), placedFragments[i] ? Color.white : Ink);
                slotViews[i] = slot.transform;
                SetHeight(slot.GetComponent<RectTransform>(), 112);
                var drop = slot.AddComponent<FragmentDropSlot>();
                drop.SlotIndex = i;
                drop.TryDrop = TryPlace;
            }
            if (placedFragments[0] && placedFragments[1] && placedFragments[2])
                Button(parent, T("save_result"), Jade, CompleteRun);
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
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
            if (kind == TravelArtworkKind.HoiAn || kind == TravelArtworkKind.VietnamJourney || kind == TravelArtworkKind.CaoLau)
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
            T(titleKey) + (ProgressService.HasStamp(profile, levelId) ? "  ✓" : "");

        private void MissionButton(Transform parent, string titleKey, string levelId, string prerequisiteId,
            Color color, Action action, Color? foreground = null, string prerequisiteTitleKey = null)
        {
            bool unlocked = ProgressService.HasStamp(profile, prerequisiteId) || ProgressService.HasStamp(profile, levelId);
            string requirement = prerequisiteTitleKey == null ? T("unlock_previous")
                : T("journey_requires_mission").Replace("{mission}", T(prerequisiteTitleKey));
            CompactButton(parent, unlocked ? MissionLabel(titleKey, levelId) : "○  " + T(titleKey) + " · " + T("locked"),
                unlocked ? color : Border, unlocked ? action : () => Toast(requirement), unlocked ? foreground : Ink);
            if (!unlocked && prerequisiteTitleKey != null)
                Label(parent, requirement, 26, 88, Muted, TextAnchor.MiddleLeft);
        }

        private void ProgressStrip(Transform parent, int done, int total, string title)
        {
            var card = Card(parent, 110);
            Label(card, title + "  " + done + "/" + total, 26, 36, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            CollectionCopy(card, earned ? "◆  " + T("stamp") : "○  " + T("locked"), 26,
                earned ? Vermilion : Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            CollectionCopy(card, T(titleKey), 26, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            Label(badge, "●  " + speaker, 28, 42, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            Header(parent, T("result_title"));
            if (FirstJourneyComplete()) Label(parent, T("first_journey_complete"), 28, 105, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            Artwork(parent, matchResult ? TravelArtworkKind.Friendship : recipeResult ? (recipeRun.Recipe == RecipeCatalog.Bibimbap ? TravelArtworkKind.Bibimbap : TravelArtworkKind.CaoLau) : run.Level == LevelCatalog.HoiAn ? TravelArtworkKind.HoiAn : TravelArtworkKind.Jeonju, 210);
            var card = Card(parent, 326);
            Label(card, T("result_intro", T(subjectKey)), 29, 135, Ink, TextAnchor.MiddleCenter);
            Label(card, T("score") + ": " + score, 35, 75, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(card, T("stars") + ": " + new string('★', stars), 30, 60, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            var stamp = Card(parent, 128);
            Label(stamp, "◆  " + T("stamp") + ": " + T(subjectKey), 28, 80, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            string speaker = T(koreanBackdrop ? "character_gabo" : "character_nhung");
            var letter = Card(parent, 174);
            Label(letter, "◆  " + speaker, 25, 36, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(letter, T(koreanBackdrop ? "stamp_message_gabo" : "stamp_message_nhung", T(subjectKey)), 24, 90, Ink, TextAnchor.MiddleCenter);
            Button(parent, T("passport"), Jade, () => Navigate(Page.Passport));
            Button(parent, T("replay"), Lantern, () => { if (matchResult) StartMatch(matchRun.Definition); else if (recipeResult) StartRecipe(recipeRun.Recipe); else StartLevel(run.Level); });
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void StartRecipe(RecipeDefinition recipe)
        {
            StoryNarration.Stop();
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
            Artwork(parent, recipeRun.Recipe == RecipeCatalog.Bibimbap ? TravelArtworkKind.Bibimbap : TravelArtworkKind.CaoLau, 230);
            SpeakerBadge(parent, T(recipeRun.Recipe == RecipeCatalog.Bibimbap ? "character_kim" : "character_lan"));
            var card = Card(parent, 218);
            Label(card, T(recipeRun.Recipe.GuideKey), 30, 170, Ink, TextAnchor.MiddleCenter);
            NarrationButtons(parent, T(recipeRun.Recipe.GuideKey));
            if (recipeRun.Recipe == RecipeCatalog.Bibimbap) KoreaVisuals.DecorateRecipePanel(card);
            else HoiAnVisuals.DecorateRecipePanel(card);
            Label(parent, T("recipe_hint"), 25, 110, Jade, TextAnchor.MiddleCenter);
            Label(parent, "◆  " + T("tomorrow_note"), 23, 96, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button(parent, T("mission_start"), Jade, () =>
            {
                recipeDeadline = Time.time + 90f;
                Navigate(Page.RecipeGame);
            });
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void DrawRecipeGame(Transform parent)
        {
            bool bibimbap = recipeRun.Recipe == RecipeCatalog.Bibimbap;
            Header(parent, T(recipeRun.Recipe.DishKey));
            Artwork(parent, bibimbap ? TravelArtworkKind.Bibimbap : TravelArtworkKind.CaoLau, 270);
            if (recipeRun.Complete)
            {
                Label(parent, T("save_result"), 32, 180, Jade, TextAnchor.MiddleCenter);
                Button(parent, T("save_result"), Jade, CompleteRecipe);
                Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
                return;
            }
            recipeTimer = Label(parent, "", 32, bibimbap ? 56 : 70, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            ProgressStrip(parent, recipeRun.StepIndex, recipeRun.Recipe.Steps.Length, T("recipe_step"));
            var guide = Card(parent, 178);
            if (bibimbap) KoreaVisuals.DecorateRecipePanel(guide);
            else HoiAnVisuals.DecorateRecipePanel(guide);
            Label(guide, T("recipe_step") + " " + (recipeRun.StepIndex + 1) + "/" + recipeRun.Recipe.Steps.Length, 29, 58, Jade, TextAnchor.MiddleCenter);
            Label(guide, T(recipeRun.Recipe.Steps[recipeRun.StepIndex]), 37, 70, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int row = 0; row < (recipeRun.Choices.Length + 1) / 2; row++)
            {
                var ingredients = NewRect("Ingredients", parent);
                SetHeight(ingredients, 104);
                AddHorizontal(ingredients);
                for (int column = 0; column < 2; column++)
                {
                    int index = row * 2 + column;
                    if (index >= recipeRun.Choices.Length) break;
                    var ingredient = Button(ingredients, T(recipeRun.Choices[index]), Paper, () => ChooseIngredient(index), Ink);
                    SetHeight(ingredient.GetComponent<RectTransform>(), 104);
                    if (recipeRun.Recipe == RecipeCatalog.CaoLau)
                        HoiAnVisuals.StyleIngredientCard(ingredient.GetComponent<RectTransform>(), recipeRun.Choices[index]);
                    else if (bibimbap)
                        KoreaVisuals.StyleIngredientCard(ingredient.GetComponent<RectTransform>(), recipeRun.Choices[index]);
                }
            }
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void ChooseIngredient(int index)
        {
            if (Time.time >= recipeDeadline) { Navigate(Page.RecipeFail); return; }
            IngredientResult result = recipeRun.Choose(index);
            if (result == IngredientResult.Complete)
            {
                if (recipeRun.Recipe == RecipeCatalog.Bibimbap) Navigate(Page.RecipeMix);
                else CompleteRecipe();
                return;
            }
            notice = T(result == IngredientResult.Correct ? "recipe_correct" : "recipe_wrong");
            Show();
        }

        private void DrawRecipeMix(Transform parent)
        {
            Header(parent, T("bibimbap"));
            Artwork(parent, TravelArtworkKind.Bibimbap, 280);
            Button(parent, T("mix"), Jade, CompleteRecipe);
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void DrawRecipeFail(Transform parent)
        {
            Header(parent, T(recipeRun.Recipe.DishKey));
            Artwork(parent, recipeRun.Recipe == RecipeCatalog.Bibimbap ? TravelArtworkKind.Bibimbap : TravelArtworkKind.CaoLau, 210);
            Label(parent, T("time_out"), 32, 200, Vermilion, TextAnchor.MiddleCenter);
            Button(parent, T("replay"), Jade, () => StartRecipe(recipeRun.Recipe));
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void CompleteRecipe()
        {
            if (resultCommitted) return;
            resultCommitted = true;
            var saved = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
            ProgressService.Record(saved, recipeRun.Recipe.Id, recipeRun.Score, recipeRun.Stars);
            RecordMissionWorldMark(saved, recipeRun.Recipe.Id);
            saved.coins += 20 + recipeRun.Stars * 5;
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
            Artwork(parent, TravelArtworkKind.Friendship, 200);
            SpeakerBadge(parent, T(matchRun.Definition == MatchCatalog.HoiAn ? "character_nhung" : "character_gabo"));
            var card = Card(parent, 230);
            Label(card, T(matchRun.Definition.GuideKey), 29, 180, Ink, TextAnchor.MiddleCenter);
            NarrationButtons(parent, T(matchRun.Definition.GuideKey));
            Label(parent, "◆  " + T("tomorrow_note"), 23, 96, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button(parent, T("mission_start"), Jade, () => Navigate(Page.MatchGame));
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void DrawMatchGame(Transform parent)
        {
            Header(parent, T(matchRun.Definition.TitleKey));
            Artwork(parent, TravelArtworkKind.Friendship, 140);
            if (matchRun.Complete)
            {
                Button(parent, T("save_result"), Jade, CompleteMatch);
                return;
            }
            Label(parent, T("match_hint"), 25, 80, Ink, TextAnchor.MiddleCenter);
            ProgressStrip(parent, matchRun.StepIndex, matchRun.Definition.RequestKeys.Length, T("recipe_step"));
            Label(parent, T("recipe_step") + " " + (matchRun.StepIndex + 1) + "/" + matchRun.Definition.RequestKeys.Length, 28, 58, Jade, TextAnchor.MiddleCenter);
            var request = Card(parent, 210);
            Label(request, T(matchRun.CurrentRequestKey), 31, 165, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int i = 0; i < matchRun.AnswerCount; i++)
            {
                int choice = i;
                Button(parent, T(matchRun.AnswerKey(i)), i == 1 ? Jade : Lantern, () => ChooseMatch(choice));
            }
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void ChooseMatch(int choice)
        {
            MatchResult result = matchRun.Choose(choice);
            if (result == MatchResult.Complete) { CompleteMatch(); return; }
            bool hoiAn = matchRun.Definition == MatchCatalog.HoiAn;
            notice = T(result == MatchResult.Correct
                ? (hoiAn ? "match_correct_hoian" : "match_correct_jeonju")
                : (hoiAn ? "match_wrong_hoian" : "match_wrong_jeonju"));
            Show();
        }

        private void CompleteMatch()
        {
            if (resultCommitted) return;
            resultCommitted = true;
            var saved = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
            ProgressService.Record(saved, matchRun.Definition.Id, matchRun.Score, matchRun.Stars);
            RecordMissionWorldMark(saved, matchRun.Definition.Id);
            saved.coins += 20 + matchRun.Stars * 5;
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
        private void DrawAvatar(Transform parent, int index) => DrawAvatar(parent, index, profile == null ? 0 : profile.outfitId);

        private void DrawAvatar(Transform parent, int index, int outfitId)
        {
            Color[] skin = { Hex("#EAB998"), Hex("#D69B75"), Hex("#F0C4A5") };
            Color[] hair = { Hex("#3B302C"), Hex("#674534"), Hex("#302F3B") };
            Color outfitColor = outfitId == 1 ? Hex("#A94A42") : outfitId == 2 ? Hex("#357E84") : outfitId == 3 ? Hex("#E6A14D") : outfitId == 4 ? Hex("#788B70") : avatarColors[index];
            PortraitPart(parent, "Outfit silhouette", outfitColor, new Vector2(0, 0), new Vector2(outfitId == 1 || outfitId == 2 ? 106 : 98, outfitId == 1 || outfitId == 2 ? 86 : 70), CircleSprite());
            if (outfitId == 1) PortraitPart(parent, "Ao dai panel", Hex("#FFF1D5"), new Vector2(0, -12), new Vector2(10, 50), RoundedSprite());
            if (outfitId == 2) PortraitPart(parent, "Hanbok collar", Hex("#F7E8D0"), new Vector2(0, -5), new Vector2(44, 15), RoundedSprite());
            PortraitPart(parent, "Hair back", hair[index], new Vector2(0, 27), new Vector2(80, 91), CircleSprite());
            PortraitPart(parent, "Face", skin[index], new Vector2(0, 19), new Vector2(65, 74), CircleSprite());
            PortraitPart(parent, "Fringe", hair[index], new Vector2(index == 1 ? 12 : -8, 48), new Vector2(index == 2 ? 61 : 49, 25), RoundedSprite());
            if (outfitId == 3) PortraitPart(parent, "Lantern hair pin", Lantern, new Vector2(36, 49), new Vector2(16, 22), CircleSprite());
            if (outfitId == 4) PortraitPart(parent, "Hanji hair pin", Paper, new Vector2(-36, 49), new Vector2(18, 18), CircleSprite());
            PortraitPart(parent, "Left ink eye", Hex("#43372F"), new Vector2(-14, 19), new Vector2(5, 7), CircleSprite());
            PortraitPart(parent, "Right ink eye", Hex("#43372F"), new Vector2(14, 19), new Vector2(5, 7), CircleSprite());
            PortraitPart(parent, "Smile", Hex("#A94A42"), new Vector2(0, 3), new Vector2(11, 3), RoundedSprite());
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
        private RectTransform Card(Transform parent, float height)
        {
            var card = Panel("Card", parent, Paper).rectTransform;
            SetHeight(card, height);
            var border = card.gameObject.AddComponent<Outline>();
            border.effectColor = Border;
            border.effectDistance = new Vector2(2, -2);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 18, 18);
            layout.spacing = 6;
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
            int readableSize = Mathf.Max(26, height >= 55f && size < 32 ? size + 2 : size);
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
            SetHeight(rect, 92);
            rect.GetComponent<Image>().raycastTarget = true;
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.26f, 0.21f, 0.17f, 0.2f);
            shadow.effectDistance = new Vector2(0, -3);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(background, Ink, 0.15f);
            button.colors = colors;
            button.onClick.AddListener(() => { feedbackSound?.PlayTap(); action(); });
            var label = Label(rect, text, 29, 90, foreground ?? (background == Lantern || background == Paper ? Ink : Color.white), TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label.rectTransform);
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
}
