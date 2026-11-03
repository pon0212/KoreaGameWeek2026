#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TastePassport.Gameplay;
using TastePassport.Localization;
using TastePassport.Save;
using TastePassport.Story;
using TastePassport.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TastePassport.Core
{
    // Activated only by the headless smoke command. Never included in a player build.
    public sealed class TastePassportSmokeDriver : MonoBehaviour
    {
        private static readonly string[] Ids = { LevelCatalog.HoiAn.Id, RecipeCatalog.CaoLau.Id, MatchCatalog.HoiAn.Id, LevelCatalog.Jeonju.Id, RecipeCatalog.Bibimbap.Id, MatchCatalog.Jeonju.Id };
        private static readonly string[] MapLabels = { "Hội An", "Cao Lầu", "Vị khách phương xa", "Jeonju", "Bibimbap", "Hành trình giao lưu" };
        private PassportApp app;
        private string testPath;
        private int stage, mission, searchIndex, pieceIndex, recipeStep;
        private bool replaying, hoianWorldChecked, jeonjuWorldChecked;
        private float started, nextTick;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TP_SMOKE_PROFILE"))) return;
            var target = FindAnyObjectByType<PassportApp>();
            if (target != null) target.gameObject.AddComponent<TastePassportSmokeDriver>();
        }

        private void Awake()
        {
            ResetRun();
        }

        private void OnEnable()
        {
            if (Application.isPlaying) ResetRun();
        }

        private void ResetRun()
        {
            app = GetComponent<PassportApp>();
            testPath = Environment.GetEnvironmentVariable("TP_SMOKE_PROFILE");
            stage = mission = searchIndex = pieceIndex = recipeStep = 0;
            replaying = hoianWorldChecked = jeonjuWorldChecked = false;
            started = Time.realtimeSinceStartup;
            nextTick = started + 0.3f;
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup < nextTick) return;
            nextTick = Time.realtimeSinceStartup + 0.16f;
            try
            {
                if (Time.realtimeSinceStartup - started > 240) throw new TimeoutException("Smoke test timed out at stage " + stage);
                switch (stage)
                {
                    case 0:
                        if (new ProfileStore(testPath).Load().language != "vi")
                            throw new InvalidDataException("A fresh profile did not default to Vietnamese");
                        if (GameText.Get("vi", "title") != "DẤU XỨ VIỆT HÀN" ||
                            GameText.Get("vi", "tagline") != "Mỗi vùng đất một câu chuyện – Mỗi hành trình một dấu ấn.")
                            throw new InvalidDataException("Vietnamese brand title or tagline is incorrect");
                        if (!app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", "title")) ||
                            !app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", "tagline")))
                            throw new InvalidOperationException("The main menu is missing the Vietnamese-first brand heading");
                        Click("Bắt đầu hành trình"); stage = 1; break;
                    case 1: Click("Tiếp theo"); stage = 2; break;
                    case 2:
                        var input = app.GetComponentInChildren<InputField>();
                        if (input == null) throw new InvalidOperationException("Name input missing; buttons=" + string.Join(",", app.GetComponentsInChildren<Button>(true).Select(b => b.name)));
                        input.text = "Smoke Player";
                        Click("Mây"); stage = 3; break;
                    case 3: Click("Tạo nhà thám hiểm"); stage = 4; break;
                    case 4:
                        var profile = new ProfileStore(testPath).Load();
                        if (!profile.created || profile.playerName != "Smoke Player" || profile.avatarIndex != 1)
                            throw new InvalidDataException("Profile creation was not saved");
                        VerifyBackupRecovery();
                        Click("Mở bản đồ"); stage = 5; break;
                    case 5:
                        if (mission == 0 && !hoianWorldChecked)
                        {
                            CheckWorldEntry("hoian", "explore_hoian", "prologue_nhung_first", "nhung-intro-seen", () => hoianWorldChecked = true);
                            break;
                        }
                        if (mission == 3 && !jeonjuWorldChecked)
                        {
                            CheckWorldEntry("jeonju", "explore_jeonju", "gabo_first", "gabo-met", () => jeonjuWorldChecked = true);
                            break;
                        }
                        Click(MapLabels[mission]); stage = 6; break;
                    case 6:
                        Click("Bắt đầu khám phá");
                        stage = mission == 0 || mission == 3 ? 7 : mission == 1 || mission == 4 ? 10 : 11;
                        break;
                    case 7:
                        if (Find("Mảnh 1") != null) { stage = 8; break; }
                        var objects = mission == 0 ? LevelCatalog.HoiAn.ObjectKeys : LevelCatalog.Jeonju.ObjectKeys;
                        if (searchIndex >= objects.Length) throw new InvalidOperationException("Search did not find three fragments");
                        Click(GameText.Get("vi", objects[searchIndex++])); break;
                    case 8: Click("Mảnh " + (pieceIndex + 1)); stage = 9; break;
                    case 9:
                        Click("Ô " + (pieceIndex + 1));
                        pieceIndex++;
                        stage = pieceIndex == 3 ? 12 : 8;
                        break;
                    case 10:
                        var recipe = mission == 1 ? RecipeCatalog.CaoLau : RecipeCatalog.Bibimbap;
                        if (recipeStep < recipe.Steps.Length) Click(GameText.Get("vi", recipe.Steps[recipeStep++]));
                        else if (mission == 4) { Click("Trộn đều"); stage = 12; }
                        else stage = 12;
                        break;
                    case 11:
                        var field = typeof(PassportApp).GetField("matchRun", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        var match = (MatchRun)field.GetValue(app);
                        if (match.Complete) { stage = 12; break; }
                        int requestIndex = Array.IndexOf(match.Definition.RequestKeys, match.CurrentRequestKey);
                        Click(GameText.Get("vi", match.Definition.AnswerKeys[requestIndex]));
                        break;
                    case 12:
                        var saved = new ProfileStore(testPath).Load();
                        if (!ProgressService.HasStamp(saved, Ids[mission]))
                            throw new InvalidDataException("Missing saved stamp: " + Ids[mission]);
                        if (mission == 1 && !saved.culturalCards.Contains("culture-hoian-caolau"))
                            throw new InvalidDataException("Completing Cao Lầu did not unlock its cultural card");
                        if (mission == 3 && !saved.culturalCards.Contains(InventoryService.HanokCardId))
                            throw new InvalidDataException("Completing the Jeonju Hanok mission did not unlock its cultural card");
                        if (mission == 4 && !saved.culturalCards.Contains(InventoryService.BibimbapCardId))
                            throw new InvalidDataException("Completing the Jeonju Bibimbap game did not unlock its cultural card");
                        if (replaying)
                        {
                            var best = ProgressService.Get(saved, LevelCatalog.HoiAn.Id);
                            if (best.playCount != 2 || best.bestScore < 100)
                                throw new InvalidDataException("Replay progress was not saved");
                            Click("Hộ chiếu của bạn");
                            stage = 13;
                            break;
                        }
                        if (++mission == Ids.Length)
                        {
                            if (saved.levels.Count != 6) throw new InvalidDataException("Expected six completed levels");
                            replaying = true;
                            mission = 0;
                            Click("Về bản đồ");
                            searchIndex = pieceIndex = recipeStep = 0;
                            stage = 5;
                            break;
                        }
                        Click("Về bản đồ");
                        searchIndex = pieceIndex = recipeStep = 0;
                        stage = 5;
                        break;
                    case 13:
                        int openPostcards = app.GetComponentsInChildren<Button>().Count(b => b.name.StartsWith("Button Xem bưu thiếp", StringComparison.Ordinal));
                        if (openPostcards != 3) throw new InvalidDataException("Expected three unlocked postcards, got " + openPostcards);
                        Click("Xem bưu thiếp");
                        stage = 14;
                        break;
                    case 14:
                        Click("Lật bưu thiếp");
                        nextTick = Time.realtimeSinceStartup + .5f;
                        stage = 15;
                        break;
                    case 15:
                        if (!app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", "postcard_1_story")))
                            throw new InvalidOperationException("Postcard back is missing story text");
                        Click("Hộ chiếu của bạn");
                        stage = 16;
                        break;
                    case 16: Click("Quay lại"); stage = 17; break;
                    case 17:
                        Click("Cài đặt");
                        if (app.GetComponentsInChildren<Slider>(true).Length != 3)
                            throw new InvalidOperationException("Settings did not render independent music, effects, and ambience sliders");
                        foreach (string key in new[] { "settings_music_volume", "settings_sfx_volume", "settings_ambience_volume" })
                            if (!app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", key)))
                                throw new InvalidOperationException("A Vietnamese audio control label is missing: " + key);
                        stage = 18;
                        break;
                    case 18: Click("한국어"); stage = 19; break;
                    case 19:
                        var translated = new ProfileStore(testPath).Load();
                        if (translated.language != "ko" || Find("뒤로") == null ||
                            app.GetComponentsInChildren<Slider>(true).Length != 3)
                            throw new InvalidDataException("Korean switch was not saved or rendered");
                        foreach (string key in new[] { "settings_music_volume", "settings_sfx_volume", "settings_ambience_volume" })
                            if (!app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("ko", key)))
                                throw new InvalidOperationException("A Korean audio control label is missing: " + key);
                        Click("Tiếng Việt"); stage = 20;
                        break;
                    case 20:
                        if (new ProfileStore(testPath).Load().language != "vi" || Find("Quay lại") == null ||
                            app.GetComponentsInChildren<Slider>(true).Length != 3)
                            throw new InvalidDataException("Vietnamese switch was not saved or rendered in settings");
                        foreach (string key in new[] { "settings_music_volume", "settings_sfx_volume", "settings_ambience_volume" })
                            if (!app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", key)))
                                throw new InvalidOperationException("A Vietnamese audio control label is missing after switching back: " + key);
                        Click("Quay lại"); stage = 21;
                        break;
                    case 21:
                        if (Find("Tiếp tục hành trình") == null)
                            throw new InvalidOperationException("The menu did not reopen after settings");
                        Click("Tiếp tục hành trình"); stage = 22;
                        break;
                    case 22:
                        string craftButton = GameText.Get("vi", "lantern_craft_map");
                        if (Find(craftButton) == null) throw new InvalidOperationException("Map lantern craft entry is missing");
                        Click(craftButton); stage = 23;
                        break;
                    case 23:
                        Click(GameText.Get("vi", "frame_hexagon")); stage = 24;
                        break;
                    case 24:
                        Click(GameText.Get("vi", "color_blue")); stage = 25; pieceIndex = 0;
                        break;
                    case 25:
                        if (pieceIndex == 0) { Click(GameText.Get("vi", "material_fabric")); pieceIndex++; break; }
                        if (pieceIndex == 1) { ClickPrefix(GameText.Get("vi", "craft_slot_fabric")); pieceIndex++; break; }
                        if (pieceIndex == 2) { Click(GameText.Get("vi", "material_tassel")); pieceIndex++; break; }
                        ClickPrefix(GameText.Get("vi", "craft_slot_tassel")); stage = 26;
                        break;
                    case 26:
                        var crafted = new ProfileStore(testPath).Load();
                        string craftedItemId = InventoryService.LanternProductPrefix + "hexagon-blue";
                        if (!crafted.inventoryItems.Contains(craftedItemId) ||
                            !crafted.culturalCards.Contains(InventoryService.LanternCardId) ||
                            !crafted.culturalCards.Contains(InventoryService.ChuaCauCardId) ||
                            !crafted.culturalCards.Contains(InventoryService.CaoLauCardId) ||
                            !crafted.worldImprints.Contains("lantern-display-ready") ||
                            !app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", "lantern_result_title")))
                            throw new InvalidDataException("Lantern craft result, inventory, or cultural cards were not saved");
                        Click(GameText.Get("vi", "inventory")); stage = 27;
                        break;
                    case 27:
                        string variantLabel = GameText.Get("vi", "frame_hexagon") + " · " + GameText.Get("vi", "color_blue");
                        if (!app.GetComponentsInChildren<Text>().Any(t => t.text == variantLabel))
                            throw new InvalidDataException("The saved lantern variant is not displayed in the inventory");
                        Click(GameText.Get("vi", "culture_cards")); stage = 28;
                        break;
                    case 28:
                        if (!app.GetComponentsInChildren<Text>().Any(t => t.text.Contains(GameText.Get("vi", "culture_lantern_title"))) ||
                            !app.GetComponentsInChildren<Text>().Any(t => t.text.Contains(GameText.Get("vi", "culture_chuacau_title"))) ||
                            !app.GetComponentsInChildren<Text>().Any(t => t.text.Contains(GameText.Get("vi", "culture_caolau_title"))))
                            throw new InvalidDataException("A discovered Chùa Cầu, Cao Lầu, or lantern cultural card is missing");
                        Click(GameText.Get("vi", "passport")); stage = 29;
                        break;
                    case 29:
                        Click(GameText.Get("vi", "open_map")); stage = 30;
                        break;
                    case 30:
                        if (Find(GameText.Get("vi", "finale_start")) != null ||
                            !app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", "finale_locked_hint")))
                            throw new InvalidOperationException("The festival unlocked before the Hanji craft was completed");
                        Click(GameText.Get("vi", "hanji_craft_map")); stage = 31;
                        break;
                    case 31:
                        Click(GameText.Get("vi", "hanji_color_jade")); stage = 32;
                        break;
                    case 32:
                        Click(GameText.Get("vi", "hanji_shape_bookmark")); stage = 33;
                        break;
                    case 33:
                        Click(GameText.Get("vi", "hanji_motif_cloud")); stage = 34;
                        break;
                    case 34:
                        Click(GameText.Get("vi", "hanji_cut")); stage = 35;
                        break;
                    case 35:
                        Click(GameText.Get("vi", "hanji_fold")); stage = 36;
                        break;
                    case 36:
                        Click(GameText.Get("vi", "hanji_decorate")); stage = 37;
                        break;
                    case 37:
                        var hanjiProfile = new ProfileStore(testPath).Load();
                        string hanjiId = InventoryService.HanjiProductPrefix + "bookmark-jade-cloud";
                        if (!hanjiProfile.inventoryItems.Contains(hanjiId) ||
                            !hanjiProfile.culturalCards.Contains(InventoryService.HanjiCardId) ||
                            !hanjiProfile.worldImprints.Contains("hanji-display-ready") ||
                            !app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", "hanji_result_title")))
                            throw new InvalidDataException("Hanji craft result, product, cultural card, or display imprint was not saved");
                        if (!hanjiProfile.culturalCards.Contains(InventoryService.HanokCardId) ||
                            !hanjiProfile.culturalCards.Contains(InventoryService.BibimbapCardId) ||
                            !hanjiProfile.culturalCards.Contains(InventoryService.GyeonggijeonCardId) ||
                            !hanjiProfile.culturalCards.Contains(InventoryService.PansoriCardId))
                            throw new InvalidDataException("One or more Jeonju cultural discoveries were not saved");
                        Click(GameText.Get("vi", "inventory")); stage = 38;
                        break;
                    case 38:
                        string hanjiVariant = GameText.Get("vi", "hanji_shape_bookmark") + " · " +
                            GameText.Get("vi", "hanji_color_jade") + " · " + GameText.Get("vi", "hanji_motif_cloud");
                        if (!app.GetComponentsInChildren<Text>().Any(t => t.text == hanjiVariant))
                            throw new InvalidDataException("The selected Hanji product variant is missing from the inventory");
                        Click(GameText.Get("vi", "culture_cards")); stage = 39;
                        break;
                    case 39:
                        VerifyJeonjuCultureCards();
                        Click(GameText.Get("vi", "passport")); stage = 40;
                        break;
                    case 40:
                        Click(GameText.Get("vi", "open_map")); stage = 41;
                        break;
                    case 41:
                        if (Find(GameText.Get("vi", "finale_start")) == null)
                            throw new InvalidOperationException("The festival did not unlock after the Hanji craft was completed");
                        Click(GameText.Get("vi", "explore_jeonju")); stage = 42;
                        break;
                    case 42:
                        var jeonjuSession = app.GetComponent<WalkableWorldSession>();
                        GameObject hanjiDisplay = GameObject.Find("World Target - hanji-display");
                        if (jeonjuSession == null || !jeonjuSession.IsOpen || hanjiDisplay == null ||
                            hanjiDisplay.GetComponent<SpriteRenderer>()?.sprite != Resources.Load<Sprite>("Art/Props/HanjiBookmark") ||
                            hanjiDisplay.GetComponent<SpriteRenderer>().color == Color.white)
                            throw new InvalidDataException("The saved Hanji product was not restored in Jeonju's world display");

                        var bkim = WorldCatalog.Get("jeonju").Interactables.First(i => i.Id == "bkim");
                        jeonjuSession.SetPlayerPosition(bkim.Position);
                        var bkimInteraction = jeonjuSession.InteractNearest();
                        if (bkimInteraction == null || bkimInteraction.DialogueKey != "bkim_first" ||
                            jeonjuSession.InteractionButtonLabel != GameText.Get("vi", "world_start_activity") + " · " + GameText.Get("vi", "activity_hanji"))
                            throw new InvalidOperationException("Bác Kim's Vietnamese Hanji activity entry is missing");
                        jeonjuSession.ContinueDialogue();
                        if (!app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", "hanji_craft_title")))
                            throw new InvalidOperationException("Bác Kim did not open the Vietnamese Hanji workshop");
                        Click(GameText.Get("vi", "back_map")); stage = 43;
                        break;
                    case 43:
                        var finalProfile = new ProfileStore(testPath).Load();
                        if (!finalProfile.inventoryItems.Contains(InventoryService.HanjiProductPrefix + "bookmark-jade-cloud") ||
                            !finalProfile.culturalCards.Contains(InventoryService.HanjiCardId) ||
                            !finalProfile.worldImprints.Contains("hanji-display-ready") ||
                            !PassportApp.IsFirstJourneyComplete(finalProfile))
                            throw new InvalidDataException("Hanji rewards changed after returning from the Jeonju display");
                        if (Find(GameText.Get("vi", "finale_start")) == null)
                            throw new InvalidOperationException("The festival did not unlock after six missions and both crafts");
                        Click(GameText.Get("vi", "finale_start")); stage = 44;
                        break;
                    case 44:
                        var invited = new ProfileStore(testPath).Load();
                        if (invited.chapterIndex != 4 || !invited.storyFlags.Contains("festival-invitation-received") ||
                            !app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", "finale_title")))
                            throw new InvalidDataException("The fourth chapter festival invitation did not open and save");
                        for (int mark = 0; mark < FestivalPreparationRun.MarkCount; mark++)
                            if (Find(GameText.Get("vi", "finale_mark_" + mark)) == null)
                                throw new InvalidOperationException("The festival is missing mark choice " + mark);
                        pieceIndex = 0;
                        stage = 45;
                        break;
                    case 45:
                        if (pieceIndex >= FestivalPreparationRun.StationCount)
                        {
                            recipeStep = 0;
                            stage = 47;
                            break;
                        }
                        int stationMark = FestivalPreparationRun.GetRequiredMarkIndex(pieceIndex);
                        Click(GameText.Get("vi", "finale_mark_" + stationMark));
                        Click(GameText.Get("vi", "festival_place_action"));
                        var afterPlacement = new ProfileStore(testPath).Load();
                        if (!StoryProgressService.HasImprint(afterPlacement, FestivalPreparationRun.GetPlacementImprintId(pieceIndex)))
                            throw new InvalidDataException("Festival station placement was not saved: " + pieceIndex);
                        stage = 46;
                        break;
                    case 46:
                        string festivalDecorationId = FestivalPreparationRun.GetDecorationId(pieceIndex % FestivalPreparationRun.DecorationCount);
                        Click(GameText.Get("vi", "festival_decoration_" + festivalDecorationId));
                        Click(GameText.Get("vi", "festival_decorate_action"));
                        var afterDecoration = new ProfileStore(testPath).Load();
                        if (!StoryProgressService.HasImprint(afterDecoration,
                                FestivalPreparationRun.GetDecorationImprintId(pieceIndex, festivalDecorationId)))
                            throw new InvalidDataException("Festival station decoration was not saved: " + pieceIndex);
                        if (pieceIndex == 0)
                        {
                            Click(GameText.Get("vi", "back_map"));
                            stage = 50;
                        }
                        else
                        {
                            pieceIndex++;
                            stage = 45;
                        }
                        break;
                    case 47:
                        if (recipeStep >= FestivalPreparationRun.LinkCount)
                        {
                            stage = 48;
                            break;
                        }
                        string linkId = FestivalPreparationRun.GetLinkId(recipeStep);
                        Click(GameText.Get("vi", "festival_activity_vi_" + FestivalPreparationRun.GetVietnamActivityId(recipeStep)));
                        Click(GameText.Get("vi", "festival_activity_ko_" + FestivalPreparationRun.GetKoreaActivityId(recipeStep)));
                        Click(GameText.Get("vi", "festival_connect"));
                        if (!StoryProgressService.HasImprint(new ProfileStore(testPath).Load(), FestivalPreparationRun.GetConnectionImprintId(linkId)))
                            throw new InvalidDataException("Cross-cultural activity link was not saved: " + linkId);
                        recipeStep++;
                        if (recipeStep == FestivalPreparationRun.LinkCount) stage = 48;
                        break;
                    case 48:
                        var completedStory = new ProfileStore(testPath).Load();
                        if (!completedStory.mainStoryCompleted || completedStory.chapterIndex != 4 ||
                            !StoryProgressService.HasFlag(completedStory, "main-story-completed") ||
                            !StoryProgressService.HasImprint(completedStory, "festival-completed"))
                            throw new InvalidDataException("Festival completion did not persist the main story ending");
                        string[] endingKeys = { "festival_scene_title", "festival_corners_complete", "festival_friends_title", "finale_reveal", "main_story_completed" };
                        foreach (string key in endingKeys)
                            if (!app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", key, "Smoke Player")))
                                throw new InvalidOperationException("The animated ending is missing text: " + key);
                        foreach (string spritePath in new[] { "Art/Characters/Nhung", "Art/Characters/GaBo", "Art/Characters/Lan", "Art/Characters/BKim" })
                        {
                            Sprite expected = Resources.Load<Sprite>(spritePath);
                            if (expected == null || !app.GetComponentsInChildren<Image>().Any(image => image.sprite == expected))
                                throw new InvalidOperationException("The ending is missing a returning friend portrait: " + spritePath);
                        }
                        if (Find(GameText.Get("vi", "read_story")) == null)
                            throw new InvalidOperationException("The ending is missing its narration control");
                        Click(GameText.Get("vi", "continue_free_exploration")); stage = 49;
                        break;
                    case 49:
                        if (!app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", "main_story_completed")) ||
                            Find(GameText.Get("vi", "view_ending")) == null ||
                            Find(GameText.Get("vi", "explore_hoian")) == null ||
                            Find(GameText.Get("vi", "explore_jeonju")) == null)
                            throw new InvalidOperationException("The completed map did not show ending access and both free-exploration destinations");
                        Click(GameText.Get("vi", "explore_hoian")); stage = 52;
                        break;
                    case 50:
                        var partialFestival = new ProfileStore(testPath).Load();
                        if (!StoryProgressService.HasImprint(partialFestival, FestivalPreparationRun.GetPlacementImprintId(0)) ||
                            !StoryProgressService.HasImprint(partialFestival, FestivalPreparationRun.GetDecorationImprintId(0, "lantern")))
                            throw new InvalidDataException("Leaving the festival lost a saved station or decoration");
                        Click(GameText.Get("vi", "finale_start")); stage = 51;
                        break;
                    case 51:
                        var resumedRun = new FestivalPreparationRun(new ProfileStore(testPath).Load());
                        if (!resumedRun.IsStationPlaced(0) || !resumedRun.IsStationDecorated(0) ||
                            !app.GetComponentsInChildren<Text>().Any(t => t.text == "◆  " + GameText.Get("vi", "finale_mark_0")) ||
                            !app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", "festival_station_decorated") + ": " + GameText.Get("vi", "festival_decoration_lantern")))
                            throw new InvalidOperationException("The festival did not restore its saved station and decoration");
                        pieceIndex = 1;
                        stage = 45;
                        break;
                    case 52:
                        var hoianFree = app.GetComponent<WalkableWorldSession>();
                        if (hoianFree == null || !hoianFree.IsOpen)
                            throw new InvalidOperationException("Free exploration did not open Hội An after the main story");
                        hoianFree.Close();
                        if (Find(GameText.Get("vi", "explore_jeonju")) == null)
                            throw new InvalidOperationException("Free Jeonju exploration was locked after the main story");
                        Click(GameText.Get("vi", "explore_jeonju")); stage = 53;
                        break;
                    case 53:
                        var jeonjuFree = app.GetComponent<WalkableWorldSession>();
                        if (jeonjuFree == null || !jeonjuFree.IsOpen)
                            throw new InvalidOperationException("Free exploration did not open Jeonju after the main story");
                        jeonjuFree.Close();
                        Finish(0, "DẤU XỨ smoke PASS: Vietnamese-first display, Korean switching, six missions, both crafts, seven saved festival stations, three shared activity links, animated ending, and free exploration");
                        break;
                }
            }
            catch (Exception error) { Finish(2, "Taste Passport smoke FAIL: " + error); }
        }

        private Button Find(string label) => app.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name.StartsWith("Button " + label, StringComparison.Ordinal));

        private void CheckWorldEntry(string worldId, string buttonKey, string firstDialogueKey, string expectedFlag, Action passed)
        {
            string buttonLabel = GameText.Get("vi", buttonKey);
            if (worldId == "hoian" && Find(buttonLabel) == null)
                throw new InvalidOperationException("The Hội An exploration entry is missing from the map");

            if (worldId == "jeonju")
            {
                var profileBeforeUnlock = new ProfileStore(testPath).Load();
                if (!profileBeforeUnlock.storyFlags.Contains("jeonju-unlocked"))
                    throw new InvalidDataException("Jeonju did not unlock after the Hội An story milestone");
                if (profileBeforeUnlock.chapterIndex != 3)
                    throw new InvalidDataException("The story chapter did not advance after the Hội An milestone");
                if (Find(buttonLabel) == null)
                    throw new InvalidOperationException("The Jeonju exploration entry is missing after unlock");
                if (Find(GameText.Get("vi", "hanji_craft_map")) == null)
                    throw new InvalidOperationException("The unlocked Hanji workshop map entry is missing");
            }

            Click(buttonLabel);
            var session = app.GetComponent<WalkableWorldSession>();
            if (session == null || !session.IsOpen) throw new InvalidOperationException("Map entry did not open " + worldId);
            if (GameText.Get("vi", "character_nhung") != "Nhung" || GameText.Get("vi", "character_gabo") != "Ga Bo (가보)")
                throw new InvalidDataException("The official companion names were changed");

            var definition = WorldCatalog.Get(worldId);
            var npc = definition.Interactables.First(i => i.IsNpc);
            session.SetPlayerPosition(npc.Position);
            string expectedAction = GameText.Get("vi", "world_action") + " · " + GameText.Get("vi", npc.NameKey);
            if (session.InteractionButtonLabel != expectedAction)
                throw new InvalidOperationException("The Vietnamese world action button did not name its companion");
            var interaction = session.InteractNearest();
            if (interaction == null || interaction.DialogueKey != firstDialogueKey)
                throw new InvalidDataException("The first companion dialogue is incorrect in " + worldId);
            if (worldId == "hoian" && !session.CurrentDialogueText.Contains("DẤU XỨ"))
                throw new InvalidDataException("The Vietnamese prologue did not mention the DẤU XỨ book");
            if (worldId == "jeonju" && GameObject.Find("World Target - hanji-display") != null)
                throw new InvalidDataException("The Hanji display appeared before the saved craft imprint");
            var profileAfterInteraction = new ProfileStore(testPath).Load();
            if (!profileAfterInteraction.storyFlags.Contains(expectedFlag))
                throw new InvalidDataException("Companion interaction was not saved: " + expectedFlag);
            if (worldId == "hoian" && profileAfterInteraction.chapterIndex != 2)
                throw new InvalidDataException("Meeting Nhung did not advance the story to chapter two");
            if (worldId == "jeonju")
            {
                session.DismissDialogue();
                string[] discoveries = { "hanok", "gyeonggijeon", "pansori" };
                string[] cardIds = { InventoryService.HanokCardId, InventoryService.GyeonggijeonCardId, InventoryService.PansoriCardId };
                for (int i = 0; i < discoveries.Length; i++)
                {
                    var discovery = definition.Interactables.First(target => target.Id == discoveries[i]);
                    session.SetPlayerPosition(discovery.Position);
                    var result = session.InteractNearest();
                    if (result == null || result.Target.Id != discoveries[i])
                        throw new InvalidOperationException("Jeonju discovery did not interact: " + discoveries[i]);
                    if (!new ProfileStore(testPath).Load().culturalCards.Contains(cardIds[i]))
                        throw new InvalidDataException("Jeonju discovery did not save its cultural card: " + cardIds[i]);
                    session.DismissDialogue();
                }
            }
            session.Close();
            if (Find(MapLabels[mission]) == null)
                throw new InvalidOperationException("Closing " + worldId + " did not return to the map");
            passed();
        }

        private void VerifyJeonjuCultureCards()
        {
            string[] keys =
            {
                "culture_hanok_title", "culture_hanji_title", "culture_bibimbap_title",
                "culture_gyeonggijeon_title", "culture_pansori_title"
            };
            foreach (string key in keys)
                if (!app.GetComponentsInChildren<Text>().Any(t => t.text.Contains(GameText.Get("vi", key))))
                    throw new InvalidDataException("A Jeonju cultural card is missing from the collection: " + key);
            if (!app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", "culture_hanji_source")) ||
                !GameText.Get("vi", "culture_hanji_source").Contains("chưa được ghi danh"))
                throw new InvalidDataException("Hanji source does not explain that the pending UNESCO nomination is not yet inscribed");
        }
        private void VerifyBackupRecovery()
        {
            string recoveryPath = testPath + ".recovery";
            var recoveryStore = new ProfileStore(recoveryPath);
            try
            {
                var original = new PlayerProfile { created = true, playerName = "Original" };
                if (!recoveryStore.TrySave(original)) throw new IOException("Could not create recovery profile");
                var newer = new PlayerProfile { created = true, playerName = "Newer" };
                if (!recoveryStore.TrySave(newer)) throw new IOException("Could not create backup");
                File.WriteAllText(recoveryPath, "damaged JSON");
                if (recoveryStore.Load().playerName != "Original") throw new InvalidDataException("Backup did not load");
                if (!recoveryStore.TrySave(newer)) throw new IOException("Could not save after recovery");
                File.WriteAllText(recoveryPath, "damaged again");
                if (recoveryStore.Load().playerName != "Original")
                    throw new InvalidDataException("A damaged primary replaced the valid backup");
            }
            finally
            {
                foreach (var file in new[] { recoveryPath, recoveryPath + ".bak", recoveryPath + ".tmp" })
                    if (File.Exists(file)) File.Delete(file);
            }
        }

        private void Click(string label)
        {
            var button = Find(label);
            if (button == null) throw new InvalidOperationException("Button missing: " + label + " at stage " + stage);
            button.onClick.Invoke();
        }

        private void ClickPrefix(string labelPrefix)
        {
            var button = app.GetComponentsInChildren<Button>().FirstOrDefault(b =>
                b.name.StartsWith("Button " + labelPrefix, StringComparison.Ordinal));
            if (button == null) throw new InvalidOperationException("Button missing for prefix: " + labelPrefix + " at stage " + stage);
            button.onClick.Invoke();
        }

        private void Finish(int code, string message)
        {
            enabled = false;
            if (code == 0) Debug.Log(message); else Debug.LogError(message);
            Environment.SetEnvironmentVariable("TP_SMOKE_PROFILE", null);
            foreach (var path in new[] { testPath, testPath + ".bak", testPath + ".tmp", testPath + ".old" })
                if (File.Exists(path)) File.Delete(path);
            EditorApplication.Exit(code);
        }
    }
}
#endif
