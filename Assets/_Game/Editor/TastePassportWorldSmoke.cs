using System;
using System.IO;
using System.Reflection;
using TastePassport.Core;
using TastePassport.Gameplay;
using TastePassport.Localization;
using TastePassport.Save;
using TastePassport.Story;
using TastePassport.UI;
using TastePassport.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TastePassport.EditorTools
{
    public static class TastePassportWorldSmoke
    {
        [MenuItem("Tools/Taste Passport/Validate Shared UI")]
        public static void RunSharedUiChecks()
        {
            var root = new GameObject("Shared UI smoke", typeof(RectTransform), typeof(Canvas));
            var owner = new GameObject("UI builder smoke");
            owner.SetActive(false); // Exercise builders without loading a player profile or audio.
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var panel = new GameObject("Reference safe area", typeof(RectTransform)).GetComponent<RectTransform>();
                panel.SetParent(root.transform, false);
                panel.sizeDelta = new Vector2(720f, 1280f);
                var app = owner.AddComponent<PassportApp>();
                var appButton = (GameObject)typeof(PassportApp).GetMethod("Button", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(app, new object[] { panel, "Tiếp tục hành trình · 여행 계속하기", GameUiTheme.Jade, (Action)(() => { }), null });
                var appRect = appButton.GetComponent<RectTransform>();
                appRect.sizeDelta = new Vector2(640f, appRect.GetComponent<LayoutElement>().preferredHeight);
                var session = owner.AddComponent<WalkableWorldSession>();
                int clicks = 0;
                var worldButton = (Button)typeof(WalkableWorldSession).GetMethod("CreateButton", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(session, new object[] { panel, "Smoke world button", "Nhung · 이야기", new Vector2(.5f, .5f),
                        new Vector2(.5f, .5f), new Vector2(0f, -180f), new Vector2(280f, 32f),
                        (Action)(() => clicks++), GameUiTheme.Paper, GameUiTheme.Ink, 26 });
                Canvas.ForceUpdateCanvases();
                foreach (var button in new[] { appButton.GetComponent<Button>(), worldButton })
                {
                    var rect = button.GetComponent<RectTransform>();
                    Require(rect.rect.width >= 64f && rect.rect.height >= 64f, "shared button builder permits a touch target below 64 units");
                    var corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, corner);
                        Require(screen.x >= 0f && screen.x <= Screen.width && screen.y >= 0f && screen.y <= Screen.height,
                            "shared control extends outside screen-space bounds");
                    }
                    var motionType = Type.GetType("TastePassport.UI.GameUiButtonMotion, Assembly-CSharp");
                    Require(motionType != null && button.GetComponent(motionType) != null, "shared button has no visual-state motion");
                    var motion = button.GetComponent(motionType);
                    // ExecuteMethod runs outside play mode, so initialize the runtime lifecycle explicitly.
                    motionType.GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(motion, null);
                    var pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Left };
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                    motionType.GetMethod("Animate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(motion, new object[] { 1f / 60f });
                    float partialPressScale = button.GetComponentInChildren<Text>().transform.localScale.x;
                    Require(partialPressScale > .97f && partialPressScale < 1f,
                        "press motion snaps to its target in one 60 Hz frame");
                    motionType.GetMethod("Animate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(motion, new object[] { 1f / 30f });
                    Require(Mathf.Abs(button.GetComponentInChildren<Text>().transform.localScale.x - .985f) < .0001f,
                        "press motion does not follow its configured 0.1-second transition duration");
                    motionType.GetMethod("Animate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(motion, new object[] { .2f });
                    Require(button.GetComponentInChildren<Text>().transform.localScale.x < 1f, "press does not produce restrained visual response");
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
                    motionType.GetMethod("Animate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(motion, new object[] { 1f / 60f });
                    float partialHighlightScale = button.GetComponentInChildren<Text>().transform.localScale.x;
                    Require(partialHighlightScale > .97f && partialHighlightScale < 1.015f,
                        "highlight motion snaps to its target in one 60 Hz frame");
                    motionType.GetMethod("Animate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(motion, new object[] { .2f });
                    Require(button.GetComponentInChildren<Text>().transform.localScale.x > 1f, "highlight does not produce visual response");
                    button.interactable = false;
                    motionType.GetMethod("Animate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(motion, new object[] { .2f });
                    Require(button.GetComponentInChildren<Text>().transform.localScale == Vector3.one, "disabled button retains motion state");
                    Require(rect.localScale == Vector3.one, "button motion changes its touch surface");
                    button.interactable = true;
                }
                worldButton.onClick.Invoke();
                Require(clicks == 1, "button motion replaced the existing click action");
                Debug.Log("Dấu Xứ shared UI smoke PASS: minimum targets, screen bounds, press/highlight/disabled response, stable hit surface, and click preservation");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [MenuItem("Tools/Taste Passport/Validate Phase Four")]
        public static void RunPhaseFourChecks()
        {
            RunJeonjuInventoryChecks();
            RunJeonjuWorldChecks();
            RunHanjiCraftChecks();
            Debug.Log("Dấu Xứ phase four smoke PASS: Jeonju collection, localized culture, Hanji workshop, and persistent world display");
        }

        [MenuItem("Tools/Taste Passport/Validate Phase Five")]
        public static void RunPhaseFiveChecks()
        {
            RunPhaseFiveStoryChecks();
            RunFestivalPreparationChecks();
            RunFestivalLocalizationChecks();
            Debug.Log("Dấu Xứ phase five smoke PASS: four chapters, resumable festival, ending state, and free exploration");
        }

        public static void RunFestivalLocalizationChecks()
        {
            string[] keys =
            {
                "finale_title", "finale_reveal_title", "finale_reveal",
                "main_story_completed", "main_story_complete_map", "view_ending", "festival_ready_intro", "finale_locked_hint",
                "festival_progress", "festival_marks_title", "festival_decor_title", "festival_decor_hint",
                "festival_decoration_lantern", "festival_decoration_paper-flower", "festival_decoration_postcard",
                "festival_station_hoian", "festival_station_jeonju", "festival_station_food", "festival_station_craft",
                "festival_station_arts", "festival_station_exchange", "festival_station_memory", "festival_station_empty",
                "festival_place_action", "festival_station_decorated", "festival_decorate_action", "festival_finish_stations_hint",
                "festival_mark_placed", "festival_select_mark_first", "festival_station_already_ready", "festival_decor_placed",
                "festival_place_mark_first", "festival_select_decor_first", "festival_save_error", "festival_link_title",
                "festival_link_hint", "festival_links_progress", "festival_activity_vi_chuacau", "festival_activity_vi_caolau",
                "festival_activity_vi_lantern", "festival_activity_ko_hanok", "festival_activity_ko_bibimbap",
                "festival_activity_ko_hanji", "festival_pair_architecture", "festival_pair_food", "festival_pair_craft",
                "festival_connect", "festival_select_both_activities", "festival_wrong_pair", "festival_pair_already_connected",
                "festival_pair_connected", "festival_scene_title", "festival_corners_complete", "festival_friends_title",
                "festival_finish_action", "continue_free_exploration"
            };
            foreach (string key in keys)
            {
                string vietnamese = GameText.Get("vi", key, "Bảo");
                string korean = GameText.Get("ko", key, "바오");
                Require(vietnamese != key, "Vietnamese festival text is missing: " + key);
                Require(korean != key && korean != vietnamese, "Korean festival text is missing or falls back to Vietnamese: " + key);
            }
        }

        [MenuItem("Tools/Taste Passport/Validate Phase Six Audio")]
        public static void RunPhaseSixAudioChecks()
        {
            Type settingsType = Type.GetType("TastePassport.UI.GameAudioSettings, Assembly-CSharp");
            Require(settingsType != null, "separate music, effects, and ambience settings are missing");
            var musicVolume = settingsType.GetProperty("MusicVolume");
            var sfxVolume = settingsType.GetProperty("SfxVolume");
            var ambienceVolume = settingsType.GetProperty("AmbienceVolume");
            var muted = settingsType.GetProperty("Muted");
            var setMusic = settingsType.GetMethod("SetMusicVolume");
            var setSfx = settingsType.GetMethod("SetSfxVolume");
            var setAmbience = settingsType.GetMethod("SetAmbienceVolume");
            var setMuted = settingsType.GetMethod("SetMuted");
            Require(musicVolume != null && sfxVolume != null && ambienceVolume != null && muted != null &&
                    setMusic != null && setSfx != null && setAmbience != null && setMuted != null,
                "audio settings do not expose saved controls for all three channels and mute");

            string[] keys = { "tp_music_volume", "tp_sfx_volume", "tp_ambience_volume", "tp_sound" };
            bool[] hadValue = new bool[keys.Length];
            float[] oldFloat = new float[3];
            int oldMute = 1;
            for (int i = 0; i < keys.Length; i++) hadValue[i] = PlayerPrefs.HasKey(keys[i]);
            oldFloat[0] = PlayerPrefs.GetFloat(keys[0], .4f);
            oldFloat[1] = PlayerPrefs.GetFloat(keys[1], .5f);
            oldFloat[2] = PlayerPrefs.GetFloat(keys[2], .25f);
            oldMute = PlayerPrefs.GetInt(keys[3], 1);
            try
            {
                setMusic.Invoke(null, new object[] { .72f });
                setSfx.Invoke(null, new object[] { .43f });
                setAmbience.Invoke(null, new object[] { .18f });
                setMuted.Invoke(null, new object[] { true });
                PlayerPrefs.Save();
                Require(Mathf.Abs((float)musicVolume.GetValue(null) - .72f) < .001f &&
                        Mathf.Abs((float)sfxVolume.GetValue(null) - .43f) < .001f &&
                        Mathf.Abs((float)ambienceVolume.GetValue(null) - .18f) < .001f && (bool)muted.GetValue(null),
                    "audio settings did not persist independently");
                setMusic.Invoke(null, new object[] { -2f });
                setSfx.Invoke(null, new object[] { 2f });
                Require(Mathf.Abs((float)musicVolume.GetValue(null)) < .001f &&
                        Mathf.Abs((float)sfxVolume.GetValue(null) - 1f) < .001f,
                    "audio channel values were not clamped to the supported range");
            }
            finally
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    if (!hadValue[i]) PlayerPrefs.DeleteKey(keys[i]);
                    else if (i < 3) PlayerPrefs.SetFloat(keys[i], oldFloat[i]);
                    else PlayerPrefs.SetInt(keys[i], oldMute);
                }
                PlayerPrefs.Save();
            }
            Debug.Log("Dấu Xứ phase six audio settings smoke PASS: three saved channels, mute, and clamped values");
        }

        [MenuItem("Tools/Taste Passport/Validate Android Gradle Temp")]
        public static void RunAndroidGradleTempChecks()
        {
            Require(Application.platform == RuntimePlatform.WindowsEditor,
                "Android Gradle temp validation requires the Windows Editor");
            bool preparesShutdown = false;
            foreach (object attribute in typeof(TastePassportBuild).GetCustomAttributes(false))
                if (attribute.GetType().FullName == "UnityEditor.InitializeOnLoadAttribute") preparesShutdown = true;
            Require(preparesShutdown, "Unity does not prepare the safe Gradle temp directory when the Editor starts");
            var helper = typeof(TastePassportBuild).GetMethod("EnsureAndroidGradleTemp",
                BindingFlags.Static | BindingFlags.NonPublic);
            Require(helper != null, "Android builds do not retain the safe Gradle temp directory for Unity shutdown");

            string windowsTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
            Require(Directory.Exists(windowsTemp), "Windows temp directory is missing: " + windowsTemp);
            try
            {
                helper.Invoke(null, null);
            }
            catch (TargetInvocationException error)
            {
                throw error.InnerException ?? error;
            }
            Require(Environment.GetEnvironmentVariable("TEMP") == windowsTemp &&
                    Environment.GetEnvironmentVariable("TMP") == windowsTemp,
                "Unity did not retain the safe TEMP/TMP values after configuring Android builds");
            Debug.Log("Dấu Xứ phase seven Android temp smoke PASS: safe temp retained for Gradle shutdown within the Unity process");
        }

        [MenuItem("Tools/Taste Passport/Validate Phase Three")]
        public static void RunPhaseThreeChecks()
        {
            RunSharedUiChecks();
            RunProfileAndStoryChecks();
            RunWorldBoundsChecks();
            RunCharacterSpriteChecks();
            RunLanternSpriteChecks();
            RunLanternDragDropChecks();
            RunWorldSessionChecks();
            Debug.Log("Dấu Xứ phase three smoke PASS: save, cultural collections, lantern craft model, localized world activity, and art imports");
        }

        [MenuItem("Tools/Taste Passport/Validate Jeonju Inventory")]
        public static void RunJeonjuInventoryChecks()
        {
            var profile = new PlayerProfile();
            var recordHanji = typeof(InventoryService).GetMethod("RecordHanjiCraft");
            Require(recordHanji != null, "Hanji product recording is not implemented");
            string itemId = "crafted-hanji-fan-indigo-flower";
            Require((bool)recordHanji.Invoke(null, new object[] { profile, "fan", "indigo", "flower" }),
                "valid Hanji craft was rejected");
            Require(profile.inventoryItems.Contains(itemId) && profile.culturalCards.Contains("culture-jeonju-hanji") &&
                    StoryProgressService.HasImprint(profile, "hanji-display-ready"),
                "Hanji craft did not grant its product, cultural card, and display imprint");

            Require((bool)recordHanji.Invoke(null, new object[] { profile, "fan", "indigo", "flower" }) &&
                    profile.inventoryItems.FindAll(id => id == itemId).Count == 1,
                "repeated Hanji craft duplicated the same product");
            Require(!(bool)recordHanji.Invoke(null, new object[] { profile, "triangle", "indigo", "flower" }) &&
                    !(bool)recordHanji.Invoke(null, new object[] { profile, "fan", "violet", "flower" }),
                "invalid Hanji variants were accepted");
            var latestHanji = typeof(InventoryService).GetMethod("GetLatestCraftedHanji");
            Require(latestHanji != null && (string)latestHanji.Invoke(null, new object[] { profile }) == itemId,
                "latest crafted Hanji product was not returned");

            var fullProfile = new PlayerProfile();
            for (int i = 0; i < 128; i++) fullProfile.inventoryItems.Add("full-item-" + i);
            Require(!(bool)recordHanji.Invoke(null, new object[] { fullProfile, "fan", "indigo", "flower" }) &&
                    !fullProfile.culturalCards.Contains("culture-jeonju-hanji") &&
                    !StoryProgressService.HasImprint(fullProfile, "hanji-display-ready"),
                "full inventory partially recorded a Hanji craft");

            string path = Path.Combine(Path.GetTempPath(), "taste_passport_hanji_inventory_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var store = new ProfileStore(path);
                Require(store.TrySave(profile), "Hanji profile could not be saved");
                var reloaded = new ProfileStore(path).Load();
                Require(reloaded.inventoryItems.Contains(itemId) && reloaded.culturalCards.Contains("culture-jeonju-hanji") &&
                        StoryProgressService.HasImprint(reloaded, "hanji-display-ready"),
                    "Hanji product, card, or imprint did not survive reload");
            }
            finally
            {
                foreach (string file in new[] { path, path + ".bak", path + ".tmp", path + ".old" })
                    if (File.Exists(file)) File.Delete(file);
            }
            Debug.Log("Dấu Xứ Jeonju inventory smoke PASS: Hanji product variants, idempotency, capacity, card, imprint, and reload");
        }

        [MenuItem("Tools/Taste Passport/Validate Jeonju World")]
        public static void RunJeonjuWorldChecks()
        {
            WorldDefinition world = WorldCatalog.Get("jeonju");
            WorldInteractableDefinition kim = FindTarget(world, "bkim");
            Require(kim != null && kim.IsNpc && kim.ActivityId == "hanji-craft" &&
                    kim.SpriteResourcePath == "Art/Characters/BKim",
                "Bác Kim or his Hanji activity is missing from Jeonju");
            Require(FindTarget(world, "gyeonggijeon") != null && FindTarget(world, "pansori") != null,
                "Gyeonggijeon or Pansori discovery is missing from Jeonju");
            WorldInteractableDefinition display = FindTarget(world, "hanji-display");
            Require(display != null && display.RequiredImprintId == "hanji-display-ready" &&
                    display.SpriteResourcePath == "Art/Props/HanjiFan" && Resources.Load<Sprite>(display.SpriteResourcePath) != null,
                "the Jeonju Hanji display is not gated by its saved craft imprint or its default sprite is missing");

            string[] expectedObjects = { "hanok_roof_tiles", "hanok_wooden_door", "hanji_windows", "hanok_courtyard", "hanok_eaves", "hanok_stone_wall" };
            for (int i = 0; i < expectedObjects.Length; i++)
                Require(LevelCatalog.Jeonju.ObjectKeys[i] == expectedObjects[i] &&
                        GameText.Get("vi", expectedObjects[i]) != expectedObjects[i] &&
                        GameText.Get("ko", expectedObjects[i]) != expectedObjects[i],
                    "Hanok detail or Vietnamese/Korean text is missing: " + expectedObjects[i]);
            Require(GameText.Get("vi", "bkim_first").Contains("Hanji") &&
                    GameText.Get("ko", "bkim_first").Contains("한지"),
                "Bác Kim's Hanji introduction is missing in Vietnamese or Korean");
            Require(GameText.Get("vi", "culture_gyeonggijeon_body").Contains("1410") &&
                    GameText.Get("ko", "culture_pansori_body").Contains("유네스코"),
                "Gyeonggijeon or Pansori cultural copy is missing");
            string[] cultureCopyKeys =
            {
                "culture_hanok_title", "culture_hanji_title", "culture_hanji_body", "culture_hanji_source",
                "culture_bibimbap_title", "culture_bibimbap_body", "culture_bibimbap_source",
                "culture_gyeonggijeon_title", "culture_gyeonggijeon_source", "culture_pansori_title", "culture_pansori_source"
            };
            foreach (string key in cultureCopyKeys)
                Require(GameText.Get("vi", key) != key && GameText.Get("ko", key) != key,
                    "Jeonju culture card copy is missing in Vietnamese or Korean: " + key);
            Require(GameText.Get("vi", "culture_hanji_source").Contains("chưa được ghi danh") &&
                    GameText.Get("ko", "culture_hanji_source").Contains("아직 등재되지 않음"),
                "The Hanji nomination was incorrectly described as inscribed or lacks its pending note");

            CheckDiscoveryCard(FindTarget(world, "hanok"), InventoryService.HanokCardId);
            CheckDiscoveryCard(FindTarget(world, "gyeonggijeon"), InventoryService.GyeonggijeonCardId);
            CheckDiscoveryCard(FindTarget(world, "pansori"), InventoryService.PansoriCardId);
            Sprite kimSprite = Resources.Load<Sprite>(kim.SpriteResourcePath);
            Require(kimSprite != null, "Bác Kim sprite did not load from Resources");
            Debug.Log("Dấu Xứ Jeonju world smoke PASS: Bác Kim activity, Hanok details, Gyeonggijeon/Pansori cards, VI/KO copy, and character art");
        }

        [MenuItem("Tools/Taste Passport/Validate Hanji Craft")]
        public static void RunHanjiCraftChecks()
        {
            RunHanjiSpriteChecks();
            var assembly = typeof(InventoryService).Assembly;
            var runType = assembly.GetType("TastePassport.Gameplay.HanjiCraftRun");
            Require(runType != null, "Hanji craft state model is missing");
            object run = Activator.CreateInstance(runType);
            var completeProperty = runType.GetProperty("Complete");
            var selectColor = runType.GetMethod("SelectPaperColor");
            var selectShape = runType.GetMethod("SelectShape");
            var selectMotif = runType.GetMethod("SelectMotif");
            var applyAction = runType.GetMethod("ApplyAction");
            Require(completeProperty != null && selectColor != null && selectShape != null &&
                    selectMotif != null && applyAction != null,
                "Hanji craft model is missing a public selection, action, or completion contract");
            Require(!((bool)selectColor.Invoke(run, new object[] { "violet" })) &&
                    !((bool)selectShape.Invoke(run, new object[] { "triangle" })) &&
                    !((bool)selectMotif.Invoke(run, new object[] { "dragon" })),
                "invalid Hanji color, shape, or decoration was accepted");
            Require(applyAction.Invoke(run, new object[] { "fold" }).ToString() == "MissingDesign",
                "Hanji action was accepted before the design was selected");
            Require((bool)selectColor.Invoke(run, new object[] { "indigo" }) &&
                    (bool)selectShape.Invoke(run, new object[] { "fan" }) &&
                    (bool)selectMotif.Invoke(run, new object[] { "flower" }),
                "valid Hanji color, shape, or decoration was rejected");
            Require(applyAction.Invoke(run, new object[] { "fold" }).ToString() == "OutOfOrder",
                "Hanji folding was allowed before cutting");
            Require(applyAction.Invoke(run, new object[] { "cut" }).ToString() == "Applied" &&
                    applyAction.Invoke(run, new object[] { "cut" }).ToString() == "AlreadyApplied",
                "Hanji cutting did not enforce one application per step");
            Require(applyAction.Invoke(run, new object[] { "fold" }).ToString() == "Applied" &&
                    !(bool)completeProperty.GetValue(run),
                "Hanji craft completed before decoration");
            Require(applyAction.Invoke(run, new object[] { "decorate" }).ToString() == "Completed" &&
                    (bool)completeProperty.GetValue(run),
                "Hanji craft did not complete after cutting, folding, and decorating");
            Require(!(bool)selectColor.Invoke(run, new object[] { "jade" }),
                "completed Hanji product design remained mutable");

            string path = Path.Combine(Path.GetTempPath(), "taste_passport_hanji_display_" + Guid.NewGuid().ToString("N") + ".json");
            GameObject cameraObject = null;
            GameObject sessionObject = null;
            WalkableWorldSession session = null;
            try
            {
                var profile = new PlayerProfile { playerName = "Nhà thám hiểm", created = true, language = "vi", lastWorldId = "jeonju" };
                profile.lastWorldX = WorldCatalog.Get("jeonju").SpawnPoint.x;
                profile.lastWorldY = WorldCatalog.Get("jeonju").SpawnPoint.y;
                var store = new ProfileStore(path);
                Require(store.TrySave(profile), "could not seed a Jeonju Hanji display profile");
                cameraObject = new GameObject("Hanji Display Smoke Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<Camera>();
                sessionObject = new GameObject("Hanji Display Smoke Session");
                session = sessionObject.AddComponent<WalkableWorldSession>();
                session.Open("jeonju", profile, store, null);
                Require(GameObject.Find("World Target - hanji-display") == null,
                    "Hanji display appeared before the craft imprint was saved");
                session.Close();

                var saved = store.Load();
                Require(InventoryService.RecordHanjiCraft(saved, "fan", "indigo", "flower") && store.TrySave(saved),
                    "completed Hanji craft could not be saved before display");
                session.Open("jeonju", store.Load(), store, null);
                GameObject display = GameObject.Find("World Target - hanji-display");
                Require(display != null && display.GetComponent<SpriteRenderer>() != null &&
                        display.GetComponent<SpriteRenderer>().sprite == Resources.Load<Sprite>("Art/Props/HanjiFan") &&
                        display.GetComponent<SpriteRenderer>().color != Color.white,
                    "saved Hanji product did not appear as a styled Jeonju display after reload");
                session.Close();

                var secondSaved = store.Load();
                Require(InventoryService.RecordHanjiCraft(secondSaved, "bookmark", "vermilion", "cloud") && store.TrySave(secondSaved),
                    "second Hanji shape could not be saved for the Jeonju display");
                session.Open("jeonju", store.Load(), store, null);
                display = GameObject.Find("World Target - hanji-display");
                Require(display != null && display.GetComponent<SpriteRenderer>().sprite == Resources.Load<Sprite>("Art/Props/HanjiBookmark"),
                    "Jeonju display did not update to the latest crafted Hanji shape");
                session.Close();
                var reloaded = store.Load();
                Require(reloaded.inventoryItems.Contains("crafted-hanji-fan-indigo-flower") &&
                        reloaded.inventoryItems.Contains("crafted-hanji-bookmark-vermilion-cloud") &&
                        InventoryService.HasCultureCard(reloaded, InventoryService.HanjiCardId) &&
                        StoryProgressService.HasImprint(reloaded, "hanji-display-ready"),
                    "Hanji product, culture card, or display imprint did not survive world reload");
            }
            finally
            {
                if (session != null && session.IsOpen) session.Close();
                if (sessionObject != null) UnityEngine.Object.DestroyImmediate(sessionObject);
                if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                foreach (string file in new[] { path, path + ".bak", path + ".tmp", path + ".old" })
                    if (File.Exists(file)) File.Delete(file);
            }
            Debug.Log("Dấu Xứ Hanji craft smoke PASS: validated choices, cut/fold/decorate order, product save, and Jeonju display reload");
        }

        [MenuItem("Tools/Taste Passport/Validate Hanji Sprites")]
        public static void RunHanjiSpriteChecks()
        {
            string[] names = { "HanjiFan", "HanjiBookmark", "HanjiFlower" };
            foreach (string name in names)
            {
                Sprite sprite = Resources.Load<Sprite>("Art/Props/" + name);
                Require(sprite != null, "Hanji product sprite did not load from Resources: " + name);
                Require(Mathf.Abs(sprite.rect.width / sprite.rect.height - (2f / 3f)) < 0.01f,
                    name + " sprite rectangle does not match the 2:3 source aspect");
                string assetPath = "Assets/_Game/Resources/Art/Props/" + name + ".png";
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Require(importer != null && importer.spriteImportMode == SpriteImportMode.Single &&
                        Mathf.Abs(importer.spritePixelsPerUnit - 200f) < 0.01f,
                    name + " sprite import mode or pixels-per-unit setting is incorrect");
            }
            Require(GameText.Get("vi", "hanji_craft_title").Contains("Xưởng") &&
                    GameText.Get("vi", "hanji_shape_bookmark").Contains("sách") &&
                    GameText.Get("ko", "hanji_craft_title").Contains("한지"),
                "Vietnamese or Korean Hanji craft copy is missing");
            Debug.Log("Dấu Xứ Hanji sprite smoke PASS: three product shapes load as 2:3 single sprites with VI/KO labels");
        }

        [MenuItem("Tools/Taste Passport/Validate Main Story Progression")]
        public static void RunPhaseFiveStoryChecks()
        {
            var profile = new PlayerProfile { playerName = "Nhà thám hiểm", created = true, language = "vi" };
            WorldInteractableDefinition nhungDefinition = FindTarget(WorldCatalog.Get("hoian"), "nhung");
            Require(nhungDefinition != null, "Nhung's opening story interaction is missing");
            var nhungObject = new GameObject("Main story chapter one smoke");
            try
            {
                WorldInteractable nhung = nhungObject.AddComponent<WorldInteractable>();
                nhung.Configure(nhungDefinition);
                Require(nhung.Interact(profile).DialogueKey == "prologue_nhung_first" && profile.chapterIndex == 2 &&
                        StoryProgressService.HasFlag(profile, "nhung-intro-seen"),
                    "meeting Nhung did not record the book and advance from chapter one to chapter two");
            }
            finally { UnityEngine.Object.DestroyImmediate(nhungObject); }

            var completedMainStory = typeof(StoryProgressService).GetMethod("CompleteMainStory");
            Require(completedMainStory != null, "persistent main story completion is not implemented");
            var endingProfile = new PlayerProfile { chapterIndex = 3 };
            Require((bool)completedMainStory.Invoke(null, new object[] { endingProfile }) &&
                    endingProfile.mainStoryCompleted && endingProfile.chapterIndex == 4 &&
                    StoryProgressService.HasFlag(endingProfile, "main-story-completed") &&
                    StoryProgressService.HasImprint(endingProfile, "festival-completed"),
                "main story completion did not save chapter four, completion state, and festival imprint");
            Require(!(bool)completedMainStory.Invoke(null, new object[] { endingProfile }) &&
                    !StoryProgressService.AdvanceToChapter(endingProfile, 2) && endingProfile.chapterIndex == 4,
                "completed main story was not idempotent or moved backwards");

            var eligibility = typeof(PassportApp).GetMethod("IsFirstJourneyComplete",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            Require(eligibility != null, "the six missions and two craft requirements are not checked as one story gate");
            var journey = new PlayerProfile();
            Require(!(bool)eligibility.Invoke(null, new object[] { journey }), "an empty profile unlocked the festival");
            foreach (string id in new[] { LevelCatalog.HoiAn.Id, RecipeCatalog.CaoLau.Id, MatchCatalog.HoiAn.Id,
                         LevelCatalog.Jeonju.Id, RecipeCatalog.Bibimbap.Id, MatchCatalog.Jeonju.Id })
                ProgressService.Record(journey, id, 100, 3);
            Require(!(bool)eligibility.Invoke(null, new object[] { journey }), "mission stamps alone skipped the two main craft activities");
            Require(InventoryService.RecordLanternCraft(journey, "round", "blue") &&
                    !(bool)eligibility.Invoke(null, new object[] { journey }), "festival unlocked before Hanji was completed");
            Require(InventoryService.RecordHanjiCraft(journey, "fan", "indigo", "flower") &&
                    (bool)eligibility.Invoke(null, new object[] { journey }),
                "six mission stamps plus saved lantern and Hanji crafts did not unlock the festival");

            string path = Path.Combine(Path.GetTempPath(), "taste_passport_main_story_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var store = new ProfileStore(path);
                Require(store.TrySave(endingProfile), "completed main story did not save");
                var reloaded = new ProfileStore(path).Load();
                Require(reloaded.mainStoryCompleted && reloaded.chapterIndex == 4 &&
                        StoryProgressService.HasFlag(reloaded, "main-story-completed") &&
                        StoryProgressService.HasImprint(reloaded, "festival-completed"),
                    "completed main story flag, chapter, or festival imprint did not survive reload");
            }
            finally
            {
                foreach (string file in new[] { path, path + ".bak", path + ".tmp", path + ".old" })
                    if (File.Exists(file)) File.Delete(file);
            }
            Debug.Log("Dấu Xứ main story smoke PASS: chapter one to four, craft gating, idempotent completion, and reload");
        }

        [MenuItem("Tools/Taste Passport/Validate Festival Preparation")]
        public static void RunFestivalPreparationChecks()
        {
            var assembly = typeof(InventoryService).Assembly;
            var runType = assembly.GetType("TastePassport.Gameplay.FestivalPreparationRun");
            Require(runType != null, "festival preparation model is missing");
            object run = Activator.CreateInstance(runType, new object[] { new PlayerProfile() });
            var selectMark = runType.GetMethod("SelectMark");
            var placeAt = runType.GetMethod("TryPlaceAt");
            var isStationPlaced = runType.GetMethod("IsStationPlaced");
            var selectDecoration = runType.GetMethod("SelectDecoration");
            var decorate = runType.GetMethod("TryDecorateAt");
            var isStationDecorated = runType.GetMethod("IsStationDecorated");
            var selectVietnamActivity = runType.GetMethod("SelectVietnamActivity");
            var selectKoreaActivity = runType.GetMethod("SelectKoreaActivity");
            var connect = runType.GetMethod("TryConnectActivities");
            var complete = runType.GetProperty("Complete");
            var placedCount = runType.GetProperty("PlacedCount");
            var decoratedCount = runType.GetProperty("DecoratedCount");
            var connectedCount = runType.GetProperty("ConnectedCount");
            Require(selectMark != null && placeAt != null && isStationPlaced != null && selectDecoration != null &&
                    decorate != null && isStationDecorated != null && selectVietnamActivity != null &&
                    selectKoreaActivity != null && connect != null && complete != null &&
                    placedCount != null && decoratedCount != null && connectedCount != null,
                "festival model is missing a placement, decoration, connection, or completion contract");
            Require(!(bool)selectMark.Invoke(run, new object[] { -1 }) && !(bool)selectMark.Invoke(run, new object[] { 7 }) &&
                    placeAt.Invoke(run, new object[] { 0 }).ToString() == "MissingSelection",
                "festival accepted an invalid mark or placement before selecting an item");
            Require((bool)selectMark.Invoke(run, new object[] { 4 }) &&
                    placeAt.Invoke(run, new object[] { 0 }).ToString() == "WrongStation" &&
                    !(bool)isStationPlaced.Invoke(run, new object[] { 0 }),
                "wrong festival station placement changed saved activity state");
            Require((bool)selectMark.Invoke(run, new object[] { 0 }) &&
                    placeAt.Invoke(run, new object[] { 0 }).ToString() == "Placed" &&
                    placeAt.Invoke(run, new object[] { 0 }).ToString() == "AlreadyPlaced" &&
                    !(bool)selectMark.Invoke(run, new object[] { 0 }),
                "festival accepted a repeated station or duplicate mark");
            Require(!(bool)selectDecoration.Invoke(run, new object[] { "unlisted" }) &&
                    decorate.Invoke(run, new object[] { 0 }).ToString() == "MissingSelection" &&
                    (bool)selectDecoration.Invoke(run, new object[] { "paper-flower" }) &&
                    decorate.Invoke(run, new object[] { 0 }).ToString() == "Decorated" &&
                    decorate.Invoke(run, new object[] { 0 }).ToString() == "AlreadyDecorated" &&
                    (bool)isStationDecorated.Invoke(run, new object[] { 0 }),
                "festival station decoration validation or duplicate guard failed");

            var partialProfile = new PlayerProfile();
            StoryProgressService.AddImprint(partialProfile, "festival-corner-hoian-ready");
            StoryProgressService.AddImprint(partialProfile, "festival-decor-hoian-paper-flower");
            object restored = Activator.CreateInstance(runType, new object[] { partialProfile });
            Require((bool)isStationPlaced.Invoke(restored, new object[] { 0 }) &&
                    (bool)isStationDecorated.Invoke(restored, new object[] { 0 }) &&
                    (int)placedCount.GetValue(restored) == 1 && (int)decoratedCount.GetValue(restored) == 1,
                "festival placement or decoration did not restore from saved imprints");

            var fullRun = Activator.CreateInstance(runType, new object[] { new PlayerProfile() });
            int[] markByStation = { 0, 4, 1, 3, 2, 5, 6 };
            string[] stationIds = { "hoian", "jeonju", "food", "craft", "arts", "exchange", "memory" };
            for (int station = 0; station < markByStation.Length; station++)
            {
                Require((bool)selectMark.Invoke(fullRun, new object[] { markByStation[station] }) &&
                        placeAt.Invoke(fullRun, new object[] { station }).ToString() == "Placed",
                    "festival could not place its matching mark at station " + station);
                string decorationId = station % 3 == 0 ? "lantern" : station % 3 == 1 ? "paper-flower" : "postcard";
                Require((bool)selectDecoration.Invoke(fullRun, new object[] { decorationId }) &&
                        decorate.Invoke(fullRun, new object[] { station }).ToString() == "Decorated",
                    "festival could not decorate station " + station);
            }
            Require(selectVietnamActivity.Invoke(fullRun, new object[] { "chuacau" }).ToString() == "Selected" &&
                    selectKoreaActivity.Invoke(fullRun, new object[] { "bibimbap" }).ToString() == "Selected" &&
                    connect.Invoke(fullRun, null).ToString() == "WrongPair" &&
                    (int)connectedCount.GetValue(fullRun) == 0,
                "an incorrect Vietnam–Korea activity pair was accepted");
            string[] vietnam = { "chuacau", "caolau", "lantern" };
            string[] korea = { "hanok", "bibimbap", "hanji" };
            for (int i = 0; i < vietnam.Length; i++)
            {
                Require(selectVietnamActivity.Invoke(fullRun, new object[] { vietnam[i] }).ToString() == "Selected" &&
                        selectKoreaActivity.Invoke(fullRun, new object[] { korea[i] }).ToString() == "Selected" &&
                        connect.Invoke(fullRun, null).ToString() == "Connected",
                    "festival could not connect the matching cultural activities: " + vietnam[i] + "/" + korea[i]);
            }
            Require((int)placedCount.GetValue(fullRun) == 7 && (int)decoratedCount.GetValue(fullRun) == 7 &&
                    (int)connectedCount.GetValue(fullRun) == 3 && (bool)complete.GetValue(fullRun),
                "festival did not complete after seven placements, seven decorations, and three cultural links");
            Debug.Log("Dấu Xứ festival model smoke PASS: seven stations, decoration, unique marks, cross-cultural links, and restoration");
        }

        private static WorldInteractableDefinition FindTarget(WorldDefinition world, string id)
        {
            if (world?.Interactables == null) return null;
            foreach (WorldInteractableDefinition target in world.Interactables)
                if (target.Id == id) return target;
            return null;
        }

        private static void CheckDiscoveryCard(WorldInteractableDefinition definition, string cardId)
        {
            Require(definition != null, "Missing world discovery for card " + cardId);
            var gameObject = new GameObject("Jeonju discovery test - " + definition.Id);
            try
            {
                WorldInteractable target = gameObject.AddComponent<WorldInteractable>();
                target.Configure(definition);
                var profile = new PlayerProfile();
                target.Interact(profile);
                Require(InventoryService.HasCultureCard(profile, cardId), "Discovery did not unlock cultural card " + cardId);
            }
            finally { UnityEngine.Object.DestroyImmediate(gameObject); }
        }

        [MenuItem("Tools/Taste Passport/Validate Story Save")]
        public static void RunProfileAndStoryChecks()
        {
            string path = Path.Combine(Path.GetTempPath(), "taste_passport_world_smoke_" + Guid.NewGuid().ToString("N") + ".json");
            string schemaTwoPath = path + ".schema2";
            try
            {
                File.WriteAllText(path,
                    "{\"schemaVersion\":1,\"playerName\":\"Legacy Explorer\",\"language\":\"ko\",\"avatarIndex\":2,\"created\":true," +
                    "\"levels\":[{\"levelId\":\"hoian-architecture\",\"bestScore\":240,\"bestStars\":2,\"playCount\":3}]} ");

                var store = new ProfileStore(path);
                var migrated = store.Load();
                Require(migrated.schemaVersion == 3, "schema-1 profile did not migrate to schema 3");
                Require(migrated.playerName == "Legacy Explorer" && migrated.language == "ko", "migration lost profile identity or language");
                var oldProgress = ProgressService.Get(migrated, "hoian-architecture");
                Require(oldProgress != null && oldProgress.bestScore == 240 && oldProgress.playCount == 3, "migration lost existing mission progress");
                Require(migrated.inventoryItems.Count == 0 && migrated.culturalCards.Count == 0,
                    "schema-1 migration did not initialize empty collections");
                Require(GameText.Get("vi", "tomorrow_note").Contains("Hãy gặp Nhung") &&
                        GameText.Get("ko", "lantern_craft_title").Contains("등불"),
                    "Vietnamese default text or Korean lantern localization is missing");

                bool first = StoryProgressService.RecordInteraction(migrated, "nhung-intro-seen", "daushu-book-discovered");
                bool repeat = StoryProgressService.RecordInteraction(migrated, "nhung-intro-seen", "daushu-book-discovered");
                Require(first && !repeat, "repeated interaction was not idempotent");
                Require(StoryProgressService.HasFlag(migrated, "nhung-intro-seen"), "story flag was not recorded");
                Require(StoryProgressService.HasImprint(migrated, "daushu-book-discovered"), "world imprint was not recorded");
                Require(migrated.storyFlags.Count == 1 && migrated.worldImprints.Count == 1, "repeated interaction duplicated saved marks");
                Require(InventoryService.AddItem(migrated, "lantern-test") && !InventoryService.AddItem(migrated, "lantern-test"),
                    "inventory item grant was not idempotent");
                Require(InventoryService.AddCultureCard(migrated, InventoryService.ChuaCauCardId) &&
                        !InventoryService.AddCultureCard(migrated, InventoryService.ChuaCauCardId),
                    "cultural card grant was not idempotent");
                var caoLauDiscovery = new PlayerProfile();
                Require(InventoryService.RecordCaoLauDiscovery(caoLauDiscovery) &&
                        !InventoryService.RecordCaoLauDiscovery(caoLauDiscovery) &&
                        InventoryService.HasCultureCard(caoLauDiscovery, InventoryService.CaoLauCardId),
                    "Cao Lầu cultural card was not granted exactly once");
                Require(GameText.Get("vi", "culture_caolau_body").Contains("thịt xá xíu") &&
                        GameText.Get("ko", "culture_caolau_title").Contains("까오러우"),
                    "Cao Lầu cultural card content is missing in Vietnamese or Korean");
                Require(InventoryService.RecordLanternCraft(migrated, "diamond", "blue"),
                    "valid lantern craft was not saved to inventory and story progress");
                Require(InventoryService.RecordLanternCraft(migrated, "diamond", "blue") &&
                        migrated.inventoryItems.FindAll(id => id == InventoryService.LanternProductPrefix + "diamond-blue").Count == 1,
                    "repeated lantern craft duplicated its inventory entry or failed");
                Require(!InventoryService.RecordLanternCraft(migrated, "pyramid", "violet"),
                    "invalid lantern design was accepted");
                var fullInventory = new PlayerProfile();
                for (int i = 0; i < 128; i++) fullInventory.inventoryItems.Add("item-" + i);
                Require(!InventoryService.RecordLanternCraft(fullInventory, "round", "red") &&
                        !InventoryService.HasCultureCard(fullInventory, InventoryService.LanternCardId) &&
                        !StoryProgressService.HasImprint(fullInventory, "lantern-display-ready"),
                    "a full inventory partially saved a lantern craft");

                var craft = new LanternCraftRun();
                Require(craft.TryPlaceMaterial(LanternCraftRun.FabricSlotId, LanternCraftRun.FabricMaterialId) == LanternCraftInputResult.MissingDesign,
                    "materials were accepted before a frame and fabric color were selected");
                Require(craft.SelectFrame("hexagon") && craft.SelectColor("green"), "valid lantern design choices were rejected");
                Require(craft.TryPlaceMaterial(LanternCraftRun.FabricSlotId, LanternCraftRun.TasselMaterialId) == LanternCraftInputResult.WrongMaterial,
                    "a tassel was accepted in the fabric slot");
                Require(craft.TryPlaceMaterial(LanternCraftRun.FabricSlotId, LanternCraftRun.FabricMaterialId) == LanternCraftInputResult.Placed,
                    "fabric was not accepted in the fabric slot");
                Require(craft.TryPlaceMaterial(LanternCraftRun.FabricSlotId, LanternCraftRun.FabricMaterialId) == LanternCraftInputResult.AlreadyFilled,
                    "a filled fabric slot accepted a duplicate material");
                Require(craft.TryPlaceMaterial(LanternCraftRun.TasselSlotId, LanternCraftRun.TasselMaterialId) == LanternCraftInputResult.Completed && craft.Complete,
                    "the lantern did not complete after both correct materials were placed");
                Require(!craft.SelectColor("red"), "a completed lantern design was mutable");
                Require(StoryProgressService.AdvanceToChapter(migrated, 2), "chapter did not advance after the prologue interaction");
                Require(!StoryProgressService.AdvanceToChapter(migrated, 1) && migrated.chapterIndex == 2, "chapter progress moved backward");
                Require(store.TrySave(migrated), "could not save the migrated story profile");

                var reloaded = store.Load();
                Require(reloaded.schemaVersion == 3 && StoryProgressService.HasFlag(reloaded, "nhung-intro-seen"), "story flag did not survive reload");
                Require(StoryProgressService.HasImprint(reloaded, "daushu-book-discovered"), "world imprint did not survive reload");
                Require(ProgressService.HasStamp(reloaded, "hoian-architecture"), "existing stamp did not survive reload");
                Require(InventoryService.HasItem(reloaded, "lantern-test") &&
                        InventoryService.HasCultureCard(reloaded, InventoryService.ChuaCauCardId) &&
                        InventoryService.HasItem(reloaded, InventoryService.LanternProductPrefix + "diamond-blue") &&
                        InventoryService.HasCultureCard(reloaded, InventoryService.LanternCardId) &&
                        StoryProgressService.HasImprint(reloaded, "lantern-display-ready") &&
                        InventoryService.GetLanternFrameId(InventoryService.GetCraftedLantern(reloaded)) == "diamond" &&
                        InventoryService.GetLanternColorId(InventoryService.GetCraftedLantern(reloaded)) == "blue",
                    "inventory item or cultural card did not survive reload");

                var chuaCauDiscovery = new PlayerProfile();
                Require(InventoryService.RecordChuaCauDiscovery(chuaCauDiscovery) &&
                        !InventoryService.RecordChuaCauDiscovery(chuaCauDiscovery) &&
                        InventoryService.HasCultureCard(chuaCauDiscovery, InventoryService.ChuaCauCardId),
                    "Chùa Cầu discovery card was not granted exactly once");

                File.WriteAllText(schemaTwoPath,
                    "{\"schemaVersion\":2,\"playerName\":\"Schema Two Explorer\",\"language\":\"vi\",\"chapterIndex\":3," +
                    "\"storyFlags\":[\"jeonju-unlocked\"],\"worldImprints\":[\"old-imprint\"],\"lastWorldId\":\"jeonju\",\"lastWorldX\":4.25,\"lastWorldY\":-1.5}");
                var migratedSchemaTwo = new ProfileStore(schemaTwoPath).Load();
                Require(migratedSchemaTwo.schemaVersion == 3 && migratedSchemaTwo.chapterIndex == 3 &&
                        StoryProgressService.HasFlag(migratedSchemaTwo, "jeonju-unlocked") &&
                        StoryProgressService.HasImprint(migratedSchemaTwo, "old-imprint") &&
                        migratedSchemaTwo.inventoryItems.Count == 0 && migratedSchemaTwo.culturalCards.Count == 0,
                    "schema-2 migration lost story progress or did not initialize collections");
                Debug.Log("Dấu Xứ story save smoke PASS: schema 1/2 migration, legacy progress, inventory/cards, idempotency, save and reload");
            }
            catch (Exception error)
            {
                Debug.LogError("Dấu Xứ story save smoke FAIL: " + error);
                throw;
            }
            finally
            {
                foreach (string file in new[] { path, path + ".bak", path + ".tmp", path + ".old", schemaTwoPath, schemaTwoPath + ".bak", schemaTwoPath + ".tmp", schemaTwoPath + ".old" })
                    if (File.Exists(file)) File.Delete(file);
            }
        }

        [MenuItem("Tools/Taste Passport/Validate World Bounds")]
        public static void RunWorldBoundsChecks()
        {
            Vector2 minimum = new Vector2(-6f, -2f);
            Vector2 maximum = new Vector2(6f, 2f);
            Require(WorldPlayerController.ClampToBounds(new Vector2(-99f, -99f), minimum, maximum) == minimum,
                "player movement passed the lower world boundary");
            Require(WorldPlayerController.ClampToBounds(new Vector2(99f, 99f), minimum, maximum) == maximum,
                "player movement passed the upper world boundary");
            Vector2 pointInside = new Vector2(-1.25f, 0.5f);
            Require(WorldPlayerController.ClampToBounds(pointInside, minimum, maximum) == pointInside,
                "player movement changed an in-bounds position");

            MethodInfo keyboardMovement = typeof(WorldPlayerController).GetMethod("GetKeyboardMovement",
                BindingFlags.Public | BindingFlags.Static);
            Require(keyboardMovement != null,
                "keyboard movement mapping is not exposed for deterministic WASD/arrow verification");
            Require((Vector2)keyboardMovement.Invoke(null, new object[] { true, false, false, false }) == Vector2.left &&
                    (Vector2)keyboardMovement.Invoke(null, new object[] { false, true, false, false }) == Vector2.right &&
                    (Vector2)keyboardMovement.Invoke(null, new object[] { false, false, true, false }) == Vector2.down &&
                    (Vector2)keyboardMovement.Invoke(null, new object[] { false, false, false, true }) == Vector2.up,
                "keyboard movement did not map all four directions to the matching world axis");
            Require(Vector2.Distance((Vector2)keyboardMovement.Invoke(null, new object[] { false, true, false, true }),
                    new Vector2(1f, 1f).normalized) < 0.001f &&
                    (Vector2)keyboardMovement.Invoke(null, new object[] { true, true, false, false }) == Vector2.zero,
                "diagonal keyboard movement was not normalized or opposing keys did not cancel");

            var hoiAn = WorldCatalog.Get("hoian");
            var jeonju = WorldCatalog.Get("jeonju");
            Require(hoiAn != null && jeonju != null && hoiAn.BackgroundResourcePath != jeonju.BackgroundResourcePath,
                "the two destinations do not use separate backgrounds");
            Debug.Log("Dấu Xứ world bounds smoke PASS: movement clamps and destination art remains distinct");
        }

        [MenuItem("Tools/Taste Passport/Repair Character Sprite Imports")]
        public static void RepairCharacterSpriteImports()
        {
            string[] assetPaths =
            {
                "Assets/_Game/Resources/Art/Characters/Lan.png",
                "Assets/_Game/Resources/Art/Characters/BKim.png",
                "Assets/_Game/Resources/Art/Props/HoiAnLantern.png",
                "Assets/_Game/Resources/Art/Props/HanjiFan.png",
                "Assets/_Game/Resources/Art/Props/HanjiBookmark.png",
                "Assets/_Game/Resources/Art/Props/HanjiFlower.png"
            };
            foreach (string assetPath in assetPaths)
            {
                string name = Path.GetFileNameWithoutExtension(assetPath);
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing sprite texture importer: " + assetPath);

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 200f;
                importer.spritePivot = new Vector2(0.5f, 0f);
                ReadPngDimensions(assetPath, out int width, out int height);
                if (width <= 0 || height <= 0) throw new InvalidOperationException("Invalid dimensions for " + assetPath);

                var importerData = new SerializedObject(importer);
                SerializedProperty spriteRects = importerData.FindProperty("m_SpriteSheet.m_Sprites");
                if (spriteRects == null)
                    throw new InvalidOperationException("Sprite rectangle data is unavailable for " + assetPath);
                if (spriteRects.arraySize != 1) spriteRects.arraySize = 1;
                SerializedProperty spriteRect = spriteRects.GetArrayElementAtIndex(0);
                spriteRect.FindPropertyRelative("m_Name").stringValue = name;
                spriteRect.FindPropertyRelative("m_Rect").rectValue = new Rect(0f, 0f, width, height);
                spriteRect.FindPropertyRelative("m_Alignment").intValue = (int)SpriteAlignment.Custom;
                spriteRect.FindPropertyRelative("m_Pivot").vector2Value = new Vector2(0.5f, 0f);
                importerData.ApplyModifiedPropertiesWithoutUndo();
                importer.SaveAndReimport();
            }

            RunCharacterSpriteChecks();
            RunLanternSpriteChecks();
            RunHanjiSpriteChecks();
            Debug.Log("Dấu Xứ character, lantern, and Hanji sprite metadata repaired and verified");
        }

        [MenuItem("Tools/Taste Passport/Validate Character Sprites")]
        public static void RunCharacterSpriteChecks()
        {
            string[] names = { "Traveler", "Nhung", "GaBo", "Lan", "BKim" };
            foreach (string name in names)
            {
                Sprite sprite = Resources.Load<Sprite>("Art/Characters/" + name);
                Require(sprite != null, "character sprite did not load: " + name);
                float aspect = sprite.rect.width / sprite.rect.height;
                Require(Mathf.Abs(aspect - (2f / 3f)) < 0.01f, name + " sprite rectangle does not match its 2:3 source aspect");
                string assetPath = "Assets/_Game/Resources/Art/Characters/" + name + ".png";
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Require(importer != null && importer.spriteImportMode == SpriteImportMode.Single &&
                        Mathf.Abs(importer.spritePixelsPerUnit - 200f) < 0.01f,
                    name + " sprite import mode or pixels-per-unit setting is incorrect");
            }
            Debug.Log("Dấu Xứ character sprite smoke PASS: all five 2:3 sprites load with single-sprite imports at 200 pixels per unit");
        }

        [MenuItem("Tools/Taste Passport/Validate Lantern Sprite")]
        public static void RunLanternSpriteChecks()
        {
            Sprite sprite = Resources.Load<Sprite>("Art/Props/HoiAnLantern");
            Require(sprite != null, "the Hoi An lantern sprite did not load from Resources");
            Require(Mathf.Abs(sprite.rect.width / sprite.rect.height - (2f / 3f)) < 0.01f,
                "the Hoi An lantern sprite rectangle does not match the 2:3 source aspect");
            var importer = AssetImporter.GetAtPath("Assets/_Game/Resources/Art/Props/HoiAnLantern.png") as TextureImporter;
            Require(importer != null && importer.spriteImportMode == SpriteImportMode.Single &&
                    Mathf.Abs(importer.spritePixelsPerUnit - 200f) < 0.01f,
                "the lantern sprite import mode or pixels-per-unit setting is incorrect");
            byte[] png = File.ReadAllBytes("Assets/_Game/Resources/Art/Props/HoiAnLantern.png");
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Require(decoded.LoadImage(png, false), "the lantern art could not be decoded as PNG");
                bool hasTransparentPixel = false;
                bool hasVisiblePixel = false;
                foreach (Color32 pixel in decoded.GetPixels32())
                {
                    if (pixel.a == 0) hasTransparentPixel = true;
                    if (pixel.a > 0) hasVisiblePixel = true;
                    if (hasTransparentPixel && hasVisiblePixel) break;
                }
                Require(hasTransparentPixel && hasVisiblePixel, "the lantern PNG does not preserve transparent cutout pixels");
            }
            finally { UnityEngine.Object.DestroyImmediate(decoded); }
            Debug.Log("Dấu Xứ lantern sprite smoke PASS: transparent 2:3 lantern art loads as a single 200 PPU sprite");
        }

        [MenuItem("Tools/Taste Passport/Validate Lantern Drag And Drop")]
        public static void RunLanternDragDropChecks()
        {
            GameObject canvasObject = null;
            GameObject eventSystemObject = null;
            GameObject materialObject = null;
            GameObject slotObject = null;
            GameObject rejectedMaterialObject = null;
            try
            {
                canvasObject = new GameObject("Lantern Drag Smoke Canvas", typeof(RectTransform), typeof(Canvas));
                eventSystemObject = new GameObject("Lantern Drag Smoke Event System", typeof(EventSystem));
                EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
                materialObject = CreateDragTestMaterial(canvasObject.transform, LanternCraftRun.FabricMaterialId);
                slotObject = new GameObject("Lantern Drag Smoke Slot", typeof(RectTransform), typeof(Image));
                slotObject.transform.SetParent(canvasObject.transform, false);
                bool acceptedByDropZone = false;
                var zone = slotObject.AddComponent<LanternCraftDropZone>();
                zone.Initialize(LanternCraftRun.FabricSlotId, (slotId, materialId, drag, target) =>
                {
                    if (slotId != LanternCraftRun.FabricSlotId || materialId != LanternCraftRun.FabricMaterialId) return;
                    drag.Accept(target);
                    acceptedByDropZone = true;
                });

                var pointer = new PointerEventData(eventSystem) { position = new Vector2(100f, 100f) };
                LanternCraftDrag drag = materialObject.GetComponent<LanternCraftDrag>();
                Transform originalParent = materialObject.transform.parent;
                drag.OnBeginDrag(pointer);
                pointer.pointerDrag = materialObject;
                zone.OnDrop(pointer);
                drag.OnEndDrag(pointer);
                Require(acceptedByDropZone && drag.Accepted && materialObject.transform.parent == slotObject.transform &&
                        !materialObject.GetComponent<CanvasGroup>().blocksRaycasts,
                    "a correctly dragged fabric did not land in its slot");

                rejectedMaterialObject = CreateDragTestMaterial(canvasObject.transform, "beads");
                LanternCraftDrag rejectedDrag = rejectedMaterialObject.GetComponent<LanternCraftDrag>();
                pointer.pointerDrag = rejectedMaterialObject;
                Transform rejectedOriginalParent = rejectedMaterialObject.transform.parent;
                rejectedDrag.OnBeginDrag(pointer);
                zone.OnDrop(pointer);
                rejectedDrag.OnEndDrag(pointer);
                Require(!rejectedDrag.Accepted && rejectedMaterialObject.transform.parent == rejectedOriginalParent &&
                        rejectedMaterialObject.GetComponent<CanvasGroup>().blocksRaycasts,
                    "an invalid material did not return to its source after a rejected drop");
                Debug.Log("Dấu Xứ lantern drag/drop smoke PASS: accepted drops occupy the target and rejected drops return to source");
            }
            finally
            {
                foreach (GameObject target in new[] { canvasObject, eventSystemObject, materialObject, slotObject, rejectedMaterialObject })
                    if (target != null) UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static GameObject CreateDragTestMaterial(Transform parent, string materialId)
        {
            var material = new GameObject("Lantern Drag Smoke Material", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            material.transform.SetParent(parent, false);
            material.AddComponent<LanternCraftDrag>().Initialize(materialId);
            return material;
        }

        [MenuItem("Tools/Taste Passport/Validate Walkable World Session")]
        public static void RunWorldSessionChecks()
        {
            string path = Path.Combine(Path.GetTempPath(), "taste_passport_session_smoke_" + Guid.NewGuid().ToString("N") + ".json");
            string failedSavePath = null;
            GameObject cameraObject = null;
            GameObject sessionObject = null;
            WalkableWorldSession session = null;
            int closeCount = 0;
            try
            {
                var store = new ProfileStore(path);
                var profile = new PlayerProfile
                {
                    playerName = "Nhà thám hiểm",
                    created = true,
                    language = "vi",
                    lastWorldId = "hoian",
                    lastWorldX = -3.7f,
                    lastWorldY = -1.72f
                };
                Require(store.TrySave(profile), "could not seed the world-session profile");

                cameraObject = new GameObject("World Session Smoke Camera");
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = false;
                camera.orthographicSize = 5.5f;
                camera.transform.position = new Vector3(4f, 2f, -9f);
                camera.transform.rotation = Quaternion.Euler(0f, 0f, 3f);
                var originalPosition = camera.transform.position;
                var originalRotation = camera.transform.rotation;

                sessionObject = new GameObject("World Session Smoke");
                session = sessionObject.AddComponent<WalkableWorldSession>();
                session.Open("hoian", profile, store, () => closeCount++);
                Require(session.IsOpen, "world session did not open");
                Require(session.InteractionButtonLabel == "Tương tác · Nhung", "world interaction button did not name Nhung in Vietnamese");

                WorldInteractionResult first = session.InteractNearest();
                Require(first != null && first.DialogueKey == "prologue_nhung_first" && first.IsFirstVisit,
                    "Nhung's first interaction did not use the opening dialogue");
                Require(StoryProgressService.HasFlag(profile, "nhung-intro-seen"), "Nhung's story flag was not recorded");
                Require(StoryProgressService.HasImprint(profile, "dau-xu-book-discovered"), "the story imprint was not recorded");
                var afterFirst = store.Load();
                Require(StoryProgressService.HasFlag(afterFirst, "nhung-intro-seen") &&
                        StoryProgressService.HasImprint(afterFirst, "dau-xu-book-discovered"),
                    "the first interaction was not saved immediately");

                session.DismissDialogue();
                WorldInteractionResult repeat = session.InteractNearest();
                Require(repeat != null && repeat.DialogueKey == "nhung_repeat" && !repeat.IsFirstVisit,
                    "Nhung's repeat interaction did not use the repeat dialogue");
                var afterRepeat = store.Load();
                Require(afterRepeat.storyFlags.Count == 1 && afterRepeat.worldImprints.Count == 1,
                    "repeated interaction duplicated a persistent mark");

                session.DismissDialogue();
                session.SetPlayerPosition(WorldCatalog.Get("hoian").Interactables[2].Position);
                Require(session.InteractionButtonLabel == "Tương tác · Chùa Cầu", "world interaction button did not name Chùa Cầu");
                WorldInteractionResult landmark = session.InteractNearest();
                Require(landmark != null && landmark.Target.Id == "chuacau" &&
                        StoryProgressService.HasImprint(profile, "stamp-chuacau") &&
                        InventoryService.HasCultureCard(profile, InventoryService.ChuaCauCardId),
                    "Chùa Cầu interaction did not save its exploration imprint and culture card");

                session.SetPlayerPosition(new Vector2(0f, -1.5f));
                session.OnApplicationPause(true);
                Require(Mathf.Abs(store.Load().lastWorldY + 1.5f) < 0.001f,
                    "pausing the app did not save the current world position");
                session.SetPlayerPosition(new Vector2(0f, -1.25f));
                session.OnApplicationQuit();
                Require(Mathf.Abs(store.Load().lastWorldY + 1.25f) < 0.001f,
                    "quitting the app did not save the current world position");
                session.SetPlayerPosition(new Vector2(0f, -1.5f));
                session.Close();
                Require(!session.IsOpen && closeCount == 1, "closing the world did not return exactly once");
                Require(camera.orthographic == false && Mathf.Approximately(camera.orthographicSize, 5.5f),
                    "closing the world did not restore the original camera projection");
                Require(Vector3.Distance(camera.transform.position, originalPosition) < 0.001f &&
                        Quaternion.Angle(camera.transform.rotation, originalRotation) < 0.001f,
                    "closing the world did not restore the original camera transform");

                var checkpoint = store.Load();
                Require(checkpoint.lastWorldId == "hoian" && Mathf.Abs(checkpoint.lastWorldX) < 0.001f &&
                        Mathf.Abs(checkpoint.lastWorldY + 1.5f) < 0.001f,
                    "closing the world did not save its checkpoint");

                checkpoint.language = "ko";
                Require(store.TrySave(checkpoint), "could not seed a Korean world-session profile");
                session.Open("hoian", checkpoint, store, () => closeCount++);
                Require(Vector2.Distance(session.PlayerPosition, new Vector2(0f, -1.5f)) < 0.001f,
                    "reopening the world did not restore its checkpoint");
                session.SetPlayerPosition(WorldCatalog.Get("hoian").Interactables[0].Position);
                Require(session.InteractionButtonLabel == "상호작용 · Nhung", "Korean world interaction button did not include its target");
                var koreanRepeat = session.InteractNearest();
                Require(koreanRepeat != null && koreanRepeat.DialogueKey == "nhung_repeat" &&
                        session.CurrentDialogueText == "DẤU XỨ 수첩의 첫 페이지를 열었어. 호이안을 함께 둘러보자!",
                    "Korean repeat dialogue was not displayed");
                session.Close();
                profile = store.Load();
                profile.lastWorldX = 99f;
                profile.lastWorldY = 99f;
                Require(store.TrySave(profile), "could not seed an out-of-bounds checkpoint");
                session.Open("hoian", store.Load(), store, () => closeCount++);
                WorldDefinition world = WorldCatalog.Get("hoian");
                Require(session.PlayerPosition.x <= world.MaximumPosition.x - 0.28f + 0.001f &&
                        session.PlayerPosition.y <= world.MaximumPosition.y - 0.28f + 0.001f,
                    "an out-of-bounds checkpoint was not clamped to the walkable area");
                session.Close();
                Require(closeCount == 3, "world session close callback count was incorrect");

                profile = store.Load();
                profile.lastWorldId = "jeonju";
                profile.lastWorldX = WorldCatalog.Get("jeonju").Interactables[0].Position.x;
                profile.lastWorldY = WorldCatalog.Get("jeonju").Interactables[0].Position.y;
                profile.language = "vi";
                Require(store.TrySave(profile), "could not seed a Jeonju world-session profile");
                session.Open("jeonju", profile, store, () => closeCount++);
                Require(session.InteractionButtonLabel == "Tương tác · Ga Bo (가보)", "Jeonju interaction prompt did not name Ga Bo");
                session.SetPlayerPosition(FindTarget(WorldCatalog.Get("jeonju"), "hanok").Position);
                Require(session.InteractionButtonLabel == "Tương tác · Làng Hanok", "Jeonju landmark prompt did not name the Hanok village");
                session.Close();
                Require(closeCount == 4, "Jeonju world session did not close exactly once");

                failedSavePath = Path.Combine(Path.GetTempPath(), "taste_passport_failed_save_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(failedSavePath);
                var failingStore = new ProfileStore(failedSavePath);
                var unsavedProfile = new PlayerProfile
                {
                    playerName = "Nhà thám hiểm",
                    created = true,
                    language = "vi",
                    lastWorldId = "hoian",
                    lastWorldX = -3.7f,
                    lastWorldY = -1.72f
                };
                Require(!failingStore.TrySave(unsavedProfile), "the intentionally invalid directory save path unexpectedly succeeded");
                session.Open("hoian", unsavedProfile, failingStore, () => closeCount++);
                Require(session.InteractionButtonLabel == "Tương tác · Nhung", "failed-save test did not reach Nhung");
                WorldInteractionResult unsavedInteraction = session.InteractNearest();
                Require(unsavedInteraction == null && !StoryProgressService.HasFlag(unsavedProfile, "nhung-intro-seen") &&
                        !StoryProgressService.HasImprint(unsavedProfile, "dau-xu-book-discovered"),
                    "failed interaction save mutated the in-memory profile");
                Require(!session.LastSaveSucceeded && session.CurrentDialogueText == "Không lưu được hồ sơ. Hãy kiểm tra dung lượng thiết bị.",
                    "failed interaction save did not show the Vietnamese save error");
                session.Close();
                Require(!session.LastSaveSucceeded, "world close reported success after a failed checkpoint save");
                Directory.Delete(failedSavePath, true);
                Require(closeCount == 5, "failed-save world session did not close exactly once");

                profile = store.Load();
                profile.language = "vi";
                profile.lastWorldId = "hoian";
                profile.lastWorldX = WorldCatalog.Get("hoian").Interactables[1].Position.x;
                profile.lastWorldY = WorldCatalog.Get("hoian").Interactables[1].Position.y;
                Require(InventoryService.RecordLanternCraft(profile, "diamond", "red") && store.TrySave(profile),
                    "could not seed a crafted lantern for the world activity check");
                string requestedActivity = null;
                session.Open("hoian", profile, store, () => closeCount++, activityId => requestedActivity = activityId);
                Require(GameObject.Find("World Target - lantern-display") != null,
                    "the crafted lantern was not placed in the walkable world");
                Require(session.InteractionButtonLabel == "Tương tác · Cô Lan", "lantern artisan is missing from the walkable world");
                WorldInteractionResult lan = session.InteractNearest();
                Require(lan != null && lan.DialogueKey == "lan_first" &&
                        session.CurrentDialogueText.Contains("Khung tre") &&
                        session.InteractionButtonLabel == "Bắt đầu hoạt động · Làm đèn lồng cùng cô Lan",
                    "Cô Lan did not open the localized lantern craft activity");
                session.ContinueDialogue();
                Require(!session.IsOpen && requestedActivity == "lantern-craft" && closeCount == 6,
                    "starting the lantern activity did not close the world and invoke its activity callback");

                profile = store.Load();
                profile.language = "ko";
                profile.lastWorldId = "hoian";
                profile.lastWorldX = WorldCatalog.Get("hoian").Interactables[1].Position.x;
                profile.lastWorldY = WorldCatalog.Get("hoian").Interactables[1].Position.y;
                Require(store.TrySave(profile), "could not seed the Korean lantern activity check");
                requestedActivity = null;
                session.Open("hoian", profile, store, () => closeCount++, activityId => requestedActivity = activityId);
                session.InteractNearest();
                Require(session.InteractionButtonLabel == "활동 시작 · Lan 아주머니와 등불 만들기",
                    "the lantern activity prompt was not localized in Korean");
                session.ContinueDialogue();
                Require(!session.IsOpen && requestedActivity == "lantern-craft" && closeCount == 7,
                    "the Korean lantern activity prompt did not continue");
                Debug.Log("Dấu Xứ world session smoke PASS: Vietnamese target labels, first/repeat dialogue, Chùa Cầu culture card, transactional save failure, lantern display/activity, camera restore and checkpoint resume");
            }
            catch (Exception error)
            {
                Debug.LogError("Dấu Xứ world session smoke FAIL: " + error);
                throw;
            }
            finally
            {
                if (session != null && session.IsOpen) session.Close();
                if (sessionObject != null) UnityEngine.Object.DestroyImmediate(sessionObject);
                if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                foreach (string file in new[] { path, path + ".bak", path + ".tmp", path + ".old" })
                    if (File.Exists(file)) File.Delete(file);
                if (!string.IsNullOrEmpty(failedSavePath) && Directory.Exists(failedSavePath)) Directory.Delete(failedSavePath, true);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void ReadPngDimensions(string assetPath, out int width, out int height)
        {
            using (FileStream stream = File.OpenRead(assetPath))
            {
                byte[] header = new byte[24];
                if (stream.Read(header, 0, header.Length) != header.Length || header[0] != 137 || header[1] != 80 ||
                    header[2] != 78 || header[3] != 71 || header[4] != 13 || header[5] != 10 || header[6] != 26 || header[7] != 10)
                    throw new InvalidDataException("Character art is not a valid PNG: " + assetPath);
                width = ReadBigEndianInt(header, 16);
                height = ReadBigEndianInt(header, 20);
            }
        }

        private static int ReadBigEndianInt(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }
    }
}
