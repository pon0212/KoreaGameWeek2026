#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TastePassport.Gameplay;
using TastePassport.Localization;
using TastePassport.Save;
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
                if (Time.realtimeSinceStartup - started > 120) throw new TimeoutException("Smoke test timed out at stage " + stage);
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
                        stage = 15;
                        break;
                    case 15:
                        if (!app.GetComponentsInChildren<Text>().Any(t => t.text == GameText.Get("vi", "postcard_1_story")))
                            throw new InvalidOperationException("Postcard back is missing story text");
                        Click("Hộ chiếu của bạn");
                        stage = 16;
                        break;
                    case 16: Click("Quay lại"); stage = 17; break;
                    case 17: Click("Cài đặt"); stage = 18; break;
                    case 18: Click("한국어"); stage = 19; break;
                    case 19:
                        var translated = new ProfileStore(testPath).Load();
                        if (translated.language != "ko" || Find("뒤로") == null)
                            throw new InvalidDataException("Korean switch was not saved or rendered");
                        Finish(0, "DẤU XỨ smoke PASS: Vietnamese brand, Hội An/Jeonju exploration, six missions, saved marks, postcards and Korean switch");
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
            var profileAfterInteraction = new ProfileStore(testPath).Load();
            if (!profileAfterInteraction.storyFlags.Contains(expectedFlag))
                throw new InvalidDataException("Companion interaction was not saved: " + expectedFlag);
            session.Close();
            if (Find(MapLabels[mission]) == null)
                throw new InvalidOperationException("Closing " + worldId + " did not return to the map");
            passed();
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

        private void Finish(int code, string message)
        {
            enabled = false;
            if (code == 0) Debug.Log(message); else Debug.LogError(message);
            Environment.SetEnvironmentVariable("TP_SMOKE_PROFILE", null);
            foreach (var path in new[] { testPath, testPath + ".bak", testPath + ".tmp" })
                if (File.Exists(path)) File.Delete(path);
            EditorApplication.Exit(code);
        }
    }
}
#endif
