using System;
using System.Collections;
using TastePassport.Gameplay;
using TastePassport.Localization;
using TastePassport.Save;
using TastePassport.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TastePassport.Core
{
    public sealed class PassportApp : MonoBehaviour
    {
        private enum Page { Menu, Language, Create, Passport, Postcard, Map, Wardrobe, Settings, Dialogue, Explore, Assembly, Result, RecipeDialogue, RecipeGame, RecipeMix, RecipeFail, MatchDialogue, MatchGame, Finale, FinaleResult }
        private static readonly Color Cream = Hex("#F6E9D4");
        private static readonly Color Lantern = Hex("#E6A14D");
        private static readonly Color Jade = Hex("#357E84");
        private static readonly Color Ink = Hex("#43372F");
        private static readonly Color Paper = Hex("#FFF9EE");
        private static readonly Color Vermilion = Hex("#A94A42");
        private static readonly Color Border = Hex("#D9C6A7");
        private static readonly Color Muted = Hex("#6B6258");
        private readonly Color[] avatarColors = { Lantern, Jade, Vermilion };
        private static Sprite roundedSprite;
        private static Sprite circleSprite;

        private ProfileStore store;
        private PlayerProfile profile;
        private Canvas canvas;
        private RectTransform safeRoot;
        private Font font;
        private UiFeedbackSound feedbackSound;
        private Page page;
        private Page settingsReturnPage;
        private string draftName = "";
        private int draftAvatar;
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
        private float finaleDeadline;
        private Text finaleTimer;
        private int finaleTarget;
        private int[] finaleOrder;

        private string T(string key, string place = null) => GameText.Get(profile.language, key, profile.playerName, place);

        private bool FirstJourneyComplete() =>
            ProgressService.HasStamp(profile, LevelCatalog.HoiAn.Id) && ProgressService.HasStamp(profile, LevelCatalog.Jeonju.Id) &&
            ProgressService.HasStamp(profile, RecipeCatalog.CaoLau.Id) && ProgressService.HasStamp(profile, RecipeCatalog.Bibimbap.Id) &&
            ProgressService.HasStamp(profile, MatchCatalog.HoiAn.Id) && ProgressService.HasStamp(profile, MatchCatalog.Jeonju.Id);

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
            AudioListener.volume = PlayerPrefs.GetInt("tp_sound", 1) == 1 ? 1 : 0;
            feedbackSound = GetComponent<UiFeedbackSound>();
            if (feedbackSound == null) feedbackSound = gameObject.AddComponent<UiFeedbackSound>();
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

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea) UpdateSafeArea();
            if (page == Page.RecipeGame && recipeRun != null && !recipeRun.Complete)
            {
                int remaining = Mathf.Max(0, Mathf.CeilToInt(recipeDeadline - Time.time));
                if (recipeTimer != null) recipeTimer.text = T("time_left") + ": " + remaining + "s";
                if (remaining == 0) Navigate(Page.RecipeFail);
            }
            if (page == Page.Finale && finaleOrder != null)
            {
                int remaining = Mathf.Max(0, Mathf.CeilToInt(finaleDeadline - Time.time));
                if (finaleTimer != null) finaleTimer.text = T("time_left") + ": " + remaining + "s";
                if (remaining == 0) { finaleOrder = null; Navigate(Page.Map); notice = T("finale_timeout"); Show(); }
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
            page = target;
            resetScroll = true;
            Show();
        }

        private void Show()
        {
            EnsureFont();
            var previousScroll = safeRoot.Find("Page Scroll")?.GetComponent<ScrollRect>();
            float savedScroll = resetScroll || previousScroll == null ? 1f : previousScroll.verticalNormalizedPosition;
            for (int i = safeRoot.childCount - 1; i >= 0; i--) Destroy(safeRoot.GetChild(i).gameObject);
            var backdrop = Panel("Destination Background", safeRoot, Color.white, true);
            string backdropPath = koreanBackdrop ? "Art/KoreaJourney" : "Art/VietnamJourney";
            backdrop.sprite = Resources.Load<Sprite>(backdropPath);
            backdrop.type = Image.Type.Simple;
            // Keep the supplied destination illustration recognizable between cards.
            // Cards stay opaque so the foreground copy and controls remain crisp.
            backdrop.color = new Color(1f, 1f, 1f, backdrop.sprite == null ? 1f : .82f);
            Panel("Paper wash", safeRoot, new Color(Cream.r, Cream.g, Cream.b, backdrop.sprite == null ? 1f : .24f), true);
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
            Label(parent, T("brand_kicker"), 22, 38, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(parent, T("title"), 43, 72, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(parent, subtitle, 27, 56, Muted, TextAnchor.MiddleCenter);
            Rule(parent);
        }

        private void DrawMenu(Transform parent)
        {
            Header(parent, T("subtitle"));
            var spread = Card(parent, 330);
            Label(spread, T("living_passport"), 30, 48, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            var destinations = NewRect("Two culture destinations", spread);
            SetHeight(destinations, 190);
            AddHorizontal(destinations);
            Artwork(destinations, TravelArtworkKind.HoiAn, 184);
            Artwork(destinations, TravelArtworkKind.Jeonju, 184);
            AddJourneyThread(spread);
            var card = Card(parent, 118);
            Label(card, T("welcome"), 31, 78, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(parent, "◆  " + T("tomorrow_note"), 23, 76, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button(parent, profile.created ? T("continue") : T("start"), Jade, () => Navigate(profile.created ? Page.Map : Page.Language));
            Button(parent, T("language"), Lantern, () => Navigate(Page.Language));
            if (profile.created) Button(parent, T("passport"), Paper, () => Navigate(Page.Passport), Ink);
            Button(parent, T("settings"), Paper, () => { settingsReturnPage = Page.Menu; Navigate(Page.Settings); }, Ink);
        }

        private void DrawLanguage(Transform parent)
        {
            Header(parent, T("choose_language"));
            Artwork(parent, TravelArtworkKind.Friendship, 180);
            Button(parent, "Tiếng Việt" + (profile.language == "vi" ? "  ✓" : ""), profile.language == "vi" ? Jade : Paper, () => ChangeLanguage("vi"), profile.language == "vi" ? Color.white : Ink);
            Button(parent, "한국어" + (profile.language == "ko" ? "  ✓" : ""), profile.language == "ko" ? Jade : Paper, () => ChangeLanguage("ko"), profile.language == "ko" ? Color.white : Ink);
            Spacer(parent, 28);
            Button(parent, T("next"), Lantern, () => Navigate(profile.created ? Page.Map : Page.Create));
            Button(parent, T("back"), Paper, () => Navigate(Page.Menu), Ink);
        }

        private void DrawCreate(Transform parent)
        {
            Header(parent, T("create"));
            Artwork(parent, TravelArtworkKind.Passport, 124);
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
            Button(parent, T("create"), Jade, CreateProfile);
            Button(parent, T("back"), Paper, () => Navigate(Page.Language), Ink);
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
            Navigate(Page.Passport);
        }

        private void DrawPassport(Transform parent)
        {
            Header(parent, T("passport"));
            Artwork(parent, TravelArtworkKind.Passport, 200);
            var card = Card(parent, 210);
            Label(card, profile.playerName, 42, 65, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(card, T("passport_intro"), 26, 52, Muted, TextAnchor.MiddleCenter);
            Label(card, T("avatar_" + profile.avatarIndex), 26, 36, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(parent, T("stamps"), 30, 62, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(parent, T("coins") + ": " + profile.coins + " ◆", 27, 50, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            StampRow(parent, LevelCatalog.HoiAn.Id, "hoian", LevelCatalog.Jeonju.Id, "jeonju");
            StampRow(parent, RecipeCatalog.CaoLau.Id, "cao_lau", RecipeCatalog.Bibimbap.Id, "bibimbap");
            StampRow(parent, MatchCatalog.HoiAn.Id, "hoian_exchange", MatchCatalog.Jeonju.Id, "jeonju_exchange");
            if (FirstJourneyComplete()) Label(parent, T("achievement_explorer"), 27, 72, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int i = 0; i < PostcardCatalog.All.Length; i++)
            {
                int index = i;
                var definition = PostcardCatalog.All[i];
                bool unlocked = definition.IsUnlocked(profile);
                var postcard = Card(parent, unlocked ? 184 : 170);
                Label(postcard, "◆  " + T(definition.TitleKey), 28, 55, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                if (unlocked) CompactButton(postcard, T("view_postcard"), Lantern, () => { postcardIndex = index; postcardBack = false; Navigate(Page.Postcard); });
                else Label(postcard, "○  " + T("locked"), 25, 60, Muted, TextAnchor.MiddleCenter);
            }
            Button(parent, T("open_map"), Jade, () => Navigate(Page.Map));
            Button(parent, T("wardrobe"), Lantern, () => Navigate(Page.Wardrobe));
            Button(parent, T("edit_profile"), Paper, () => { draftName = profile.playerName; draftAvatar = profile.avatarIndex; Navigate(Page.Create); }, Ink);
            Button(parent, T("back"), Paper, () => Navigate(Page.Menu), Ink);
        }

        private void DrawPostcard(Transform parent)
        {
            var definition = PostcardCatalog.All[Mathf.Clamp(postcardIndex, 0, PostcardCatalog.All.Length - 1)];
            if (!definition.IsUnlocked(profile)) { Navigate(Page.Passport); return; }
            Header(parent, T(definition.TitleKey));
            Label(parent, T(postcardBack ? "back_side" : "front"), 29, 65, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            var card = Card(parent, postcardBack ? 460 : 535);
            if (!postcardBack)
            {
                Label(card, T(definition.TitleKey), 31, 85, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                var pair = NewRect("Two cultures", card);
                SetHeight(pair, 235);
                AddHorizontal(pair);
                var left = NewRect("Vietnam picture", pair);
                SetHeight(left, 220);
                TravelArtwork.Draw(left, postcardIndex == 1 ? TravelArtworkKind.CaoLau : TravelArtworkKind.HoiAn);
                var right = NewRect("Korea picture", pair);
                SetHeight(right, 220);
                TravelArtwork.Draw(right, postcardIndex == 1 ? TravelArtworkKind.Bibimbap : TravelArtworkKind.Jeonju);
                AddJourneyThread(card);
                Label(card, T(definition.FrontLeftKey) + "  •  " + T(definition.FrontRightKey), 26, 58, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                Label(card, "◆  ◆  ◆", 28, 44, Vermilion, TextAnchor.MiddleCenter);
            }
            else
            {
                Label(card, "◆  " + T("passport"), 25, 45, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
                Label(card, T(definition.StoryKey), 29, 165, Ink, TextAnchor.MiddleCenter);
                Rule(card);
                Label(card, T("culture_note"), 27, 54, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
                Label(card, T(definition.FactKey), 26, 115, Ink, TextAnchor.MiddleCenter);
                Label(card, T("tomorrow_clue_" + (postcardIndex + 1)), 24, 82, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            Button(parent, T("flip_postcard"), Jade, () => { postcardBack = !postcardBack; Show(); });
            Button(parent, T("passport"), Paper, () => Navigate(Page.Passport), Ink);
        }

        private void DrawMap(Transform parent)
        {
            Header(parent, T("map"));
            if (FirstJourneyComplete()) Label(parent, T("first_journey_complete"), 29, 110, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (FirstJourneyComplete()) CompactButton(parent, T("finale_start"), Vermilion, StartFinale, Color.white);
            Label(parent, T("map_intro"), 27, 54, Ink, TextAnchor.MiddleCenter);
            Label(parent, T("map_illustration"), 22, 42, Muted, TextAnchor.MiddleCenter);
            var hoian = Card(parent, 790);
            Artwork(hoian, TravelArtworkKind.VietnamJourney, 220);
            Label(hoian, T("hoian"), 38, 48, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(hoian, T("hoian_desc"), 25, 40, Muted, TextAnchor.MiddleCenter);
            Label(hoian, HasAnyStamp(LevelCatalog.Jeonju.Id, RecipeCatalog.Bibimbap.Id, MatchCatalog.Jeonju.Id) ? "◆  " + T("echo_from_jeonju") : "○  " + T("echo_waiting"), 22, 34, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            Artwork(hoian, TravelArtworkKind.CaoLau, 84);
            Label(hoian, T("location_hoian_food"), 23, 36, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            CompactButton(hoian, MissionLabel("hoian", LevelCatalog.HoiAn.Id), Lantern, () => StartLevel(LevelCatalog.HoiAn));
            CompactButton(hoian, MissionLabel("cao_lau", RecipeCatalog.CaoLau.Id), Jade, () => StartRecipe(RecipeCatalog.CaoLau));
            CompactButton(hoian, MissionLabel("hoian_exchange", MatchCatalog.HoiAn.Id), Paper, () => StartMatch(MatchCatalog.HoiAn), Ink);
            AddJourneyThread(parent);
            var jeonju = Card(parent, 790);
            Artwork(jeonju, TravelArtworkKind.KoreaJourney, 220);
            Label(jeonju, T("jeonju"), 38, 48, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(jeonju, T("jeonju_desc"), 25, 40, Muted, TextAnchor.MiddleCenter);
            Label(jeonju, HasAnyStamp(LevelCatalog.HoiAn.Id, RecipeCatalog.CaoLau.Id, MatchCatalog.HoiAn.Id) ? "◆  " + T("echo_from_hoian") : "○  " + T("echo_waiting"), 22, 34, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            Artwork(jeonju, TravelArtworkKind.Bibimbap, 84);
            Label(jeonju, T("location_jeonju_food"), 23, 36, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            CompactButton(jeonju, MissionLabel("jeonju", LevelCatalog.Jeonju.Id), Lantern, () => StartLevel(LevelCatalog.Jeonju));
            CompactButton(jeonju, MissionLabel("bibimbap", RecipeCatalog.Bibimbap.Id), Jade, () => StartRecipe(RecipeCatalog.Bibimbap));
            CompactButton(jeonju, MissionLabel("jeonju_exchange", MatchCatalog.Jeonju.Id), Paper, () => StartMatch(MatchCatalog.Jeonju), Ink);
            Button(parent, T("passport"), Jade, () => Navigate(Page.Passport));
            Button(parent, T("settings"), Paper, () => { settingsReturnPage = Page.Map; Navigate(Page.Settings); }, Ink);
        }

        private void DrawWardrobe(Transform parent)
        {
            Header(parent, T("wardrobe"));
            Label(parent, T("coins") + ": " + profile.coins + " ◆", 30, 55, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int i = 0; i < OutfitCatalog.All.Length; i++)
            {
                var outfit = OutfitCatalog.All[i];
                var card = Card(parent, 190);
                var preview = NewRect("Outfit preview", card);
                SetHeight(preview, 76);
                DrawAvatar(preview, OutfitCatalog.Get(profile.outfitId).Avatar, outfit.Id);
                Label(card, T(outfit.NameKey), 29, 55, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                Label(card, outfit.IsOwned(profile) ? T("outfit_owned") : T("outfit_price") + ": " + outfit.Price + " ◆", 24, 42, Muted, TextAnchor.MiddleCenter);
                if (outfit.IsOwned(profile)) CompactButton(card, profile.outfitId == outfit.Id ? T("outfit_selected") : T("outfit_wear"), Jade, () => SetOutfit(outfit.Id));
                else CompactButton(card, T("outfit_unlock"), outfit.Price <= profile.coins ? Lantern : Border, () => UnlockOutfit(outfit.Id));
            }
            Button(parent, T("back"), Paper, () => Navigate(Page.Passport), Ink);
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
            Header(parent, T("settings"));
            Artwork(parent, TravelArtworkKind.Passport, 150);
            Label(parent, T("settings_language"), 29, 58, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            Button(parent, "Tiếng Việt" + (profile.language == "vi" ? "  ✓" : ""), profile.language == "vi" ? Jade : Paper, () => ChangeLanguage("vi"), profile.language == "vi" ? Color.white : Ink);
            Button(parent, "한국어" + (profile.language == "ko" ? "  ✓" : ""), profile.language == "ko" ? Jade : Paper, () => ChangeLanguage("ko"), profile.language == "ko" ? Color.white : Ink);
            Label(parent, T("settings_sound"), 29, 58, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            Button(parent, T(PlayerPrefs.GetInt("tp_sound", 1) == 1 ? "on" : "off"), Lantern, () =>
            {
                int state = PlayerPrefs.GetInt("tp_sound", 1) == 1 ? 0 : 1;
                PlayerPrefs.SetInt("tp_sound", state);
                PlayerPrefs.Save();
                AudioListener.volume = state;
                Show();
            });
            Spacer(parent, 28);
            Button(parent, T("back"), Paper, () => Navigate(settingsReturnPage), Ink);
        }

        private void StartFinale()
        {
            if (!FirstJourneyComplete()) { Navigate(Page.Map); return; }
            finaleTarget = 6;
            finaleOrder = new int[] { 0, 1, 2, 3, 4, 5, 6 };
            for (int i = finaleOrder.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int value = finaleOrder[i]; finaleOrder[i] = finaleOrder[j]; finaleOrder[j] = value;
            }
            finaleDeadline = Time.time + 60f;
            Navigate(Page.Finale);
        }

        private void DrawFinale(Transform parent)
        {
            Header(parent, T("finale_title"));
            Artwork(parent, TravelArtworkKind.Passport, 180);
            Label(parent, T("finale_clue"), 29, 120, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            finaleTimer = Label(parent, T("time_left") + ": 60s", 34, 64, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int row = 0; row < 4; row++)
            {
                var choices = NewRect("Finale stamp choices", parent);
                SetHeight(choices, 86);
                AddHorizontal(choices);
                for (int col = 0; col < 2; col++)
                {
                    int index = row * 2 + col;
                    if (index >= finaleOrder.Length) break;
                    int mark = finaleOrder[index];
                    Button(choices, T("finale_mark_" + mark), Paper, () => ChooseFinaleMark(mark), Ink);
                }
            }
            Button(parent, T("back_map"), Paper, () => { finaleOrder = null; Navigate(Page.Map); }, Ink);
        }

        private void ChooseFinaleMark(int mark)
        {
            if (mark == finaleTarget)
            {
                finaleOrder = null;
                Navigate(Page.FinaleResult);
            }
            else
            {
                notice = T("finale_wrong");
            }
        }

        private void DrawFinaleResult(Transform parent)
        {
            Header(parent, T("finale_reveal_title"));
            Artwork(parent, TravelArtworkKind.Friendship, 240);
            var card = Card(parent, 320);
            Label(card, T("finale_reveal"), 29, 245, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button(parent, T("replay"), Jade, StartFinale);
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void StartLevel(LevelDefinition level)
        {
            koreanBackdrop = level != LevelCatalog.HoiAn;
            recipeResult = false;
            matchResult = false;
            run = new ExplorationRun(level, Environment.TickCount);
            placedFragments = new bool[3];
            selectedFragment = -1;
            Navigate(Page.Dialogue);
        }

        private void DrawDialogue(Transform parent)
        {
            Header(parent, T(run.Level.PlaceKey));
            Artwork(parent, run.Level == LevelCatalog.HoiAn ? TravelArtworkKind.HoiAn : TravelArtworkKind.Jeonju, 230);
            SpeakerBadge(parent, run.Level == LevelCatalog.HoiAn ? "Mai" : "Joon");
            var card = Card(parent, 220);
            Label(card, T(run.Level.GuideKey), 31, 172, Ink, TextAnchor.MiddleCenter);
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
            SetHeight(row, 150);
            AddHorizontal(row);
            StampTile(row, firstId, firstKey);
            StampTile(row, secondId, secondKey);
        }

        private void StampTile(Transform parent, string levelId, string titleKey)
        {
            bool earned = ProgressService.HasStamp(profile, levelId);
            var card = Card(parent, 148);
            Label(card, earned ? "◆  " + T("stamp") : "○  " + T("locked"), 22, 38,
                earned ? Vermilion : Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(card, T(titleKey), 25, 64, Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            var saved = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
            ProgressService.Record(saved, run.Level.Id, run.Score, run.Stars);
            saved.coins += 20 + run.Stars * 5;
            if (!store.TrySave(saved)) { notice = T("save_error"); Show(); return; }
            profile = saved;
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
            var stamp = Card(parent, 108);
            Label(stamp, "◆  " + T("stamp") + ": " + T(subjectKey), 28, 66, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            string speaker = koreanBackdrop ? "Joon" : "Mai";
            var letter = Card(parent, 142);
            Label(letter, "◆  " + speaker, 25, 36, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(letter, T(koreanBackdrop ? "stamp_message_joon" : "stamp_message_mai", T(subjectKey)), 24, 58, Ink, TextAnchor.MiddleCenter);
            Button(parent, T("passport"), Jade, () => Navigate(Page.Passport));
            Button(parent, T("replay"), Lantern, () => { if (matchResult) StartMatch(matchRun.Definition); else if (recipeResult) StartRecipe(recipeRun.Recipe); else StartLevel(run.Level); });
            Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
        }

        private void StartRecipe(RecipeDefinition recipe)
        {
            koreanBackdrop = recipe == RecipeCatalog.Bibimbap;
            matchResult = false;
            recipeRun = new RecipeRun(recipe, Environment.TickCount);
            Navigate(Page.RecipeDialogue);
        }

        private void DrawRecipeDialogue(Transform parent)
        {
            Header(parent, T(recipeRun.Recipe.DishKey));
            Artwork(parent, recipeRun.Recipe == RecipeCatalog.Bibimbap ? TravelArtworkKind.Bibimbap : TravelArtworkKind.CaoLau, 230);
            SpeakerBadge(parent, recipeRun.Recipe == RecipeCatalog.Bibimbap ? "Kim" : "Lan");
            var card = Card(parent, 218);
            Label(card, T(recipeRun.Recipe.GuideKey), 30, 170, Ink, TextAnchor.MiddleCenter);
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
            Header(parent, T(recipeRun.Recipe.DishKey));
            Artwork(parent, recipeRun.Recipe == RecipeCatalog.Bibimbap ? TravelArtworkKind.Bibimbap : TravelArtworkKind.CaoLau, 220);
            if (recipeRun.Complete)
            {
                Label(parent, T("save_result"), 32, 180, Jade, TextAnchor.MiddleCenter);
                Button(parent, T("save_result"), Jade, CompleteRecipe);
                Button(parent, T("back_map"), Paper, () => Navigate(Page.Map), Ink);
                return;
            }
            recipeTimer = Label(parent, "", 32, 70, Vermilion, TextAnchor.MiddleCenter, FontStyle.Bold);
            ProgressStrip(parent, recipeRun.StepIndex, recipeRun.Recipe.Steps.Length, T("recipe_step"));
            var guide = Card(parent, 178);
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
                    var ingredient = Button(ingredients, T(recipeRun.Choices[index]), index % 2 == 0 ? Lantern : Jade, () => ChooseIngredient(index));
                    SetHeight(ingredient.GetComponent<RectTransform>(), 98);
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
            var saved = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
            ProgressService.Record(saved, recipeRun.Recipe.Id, recipeRun.Score, recipeRun.Stars);
            saved.coins += 20 + recipeRun.Stars * 5;
            if (!store.TrySave(saved)) { notice = T("save_error"); Show(); return; }
            profile = saved;
            recipeResult = true;
            matchResult = false;
            Navigate(Page.Result);
        }

        private void StartMatch(MatchDefinition definition)
        {
            koreanBackdrop = definition == MatchCatalog.Jeonju;
            matchRun = new MatchRun(definition, Environment.TickCount);
            Navigate(Page.MatchDialogue);
        }

        private void DrawMatchDialogue(Transform parent)
        {
            Header(parent, T(matchRun.Definition.TitleKey));
            Artwork(parent, TravelArtworkKind.Friendship, 200);
            SpeakerBadge(parent, matchRun.Definition == MatchCatalog.HoiAn ? "Mai" : "Joon");
            var card = Card(parent, 230);
            Label(card, T(matchRun.Definition.GuideKey), 29, 180, Ink, TextAnchor.MiddleCenter);
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
            var saved = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
            ProgressService.Record(saved, matchRun.Definition.Id, matchRun.Score, matchRun.Stars);
            saved.coins += 20 + matchRun.Stars * 5;
            if (!store.TrySave(saved)) { notice = T("save_error"); Show(); return; }
            profile = saved;
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
        private Text Label(Transform parent, string value, int size, float height, Color color, TextAnchor alignment, FontStyle style = FontStyle.Normal)
        {
            EnsureFont();
            var rect = NewRect("Text", parent);
            SetHeight(rect, height);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = value;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            // Keep glyph rasterization at the authored size; Best Fit used to shrink
            // short labels to arbitrary intermediate sizes and made them look soft.
            label.resizeTextForBestFit = false;
            label.resizeTextMinSize = size;
            label.resizeTextMaxSize = size;
            label.raycastTarget = false;
            return label;
        }

        private void EnsureFont()
        {
            if (font != null) return;
            font = Resources.Load<Font>("TastePassportFont");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) Debug.LogError("Taste Passport: No UI font is available. Check Resources/TastePassportFont.ttf import.");
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
