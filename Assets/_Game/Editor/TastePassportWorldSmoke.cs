using System;
using System.IO;
using TastePassport.Save;
using TastePassport.Story;
using TastePassport.World;
using UnityEditor;
using UnityEngine;

namespace TastePassport.EditorTools
{
    public static class TastePassportWorldSmoke
    {
        [MenuItem("Tools/Taste Passport/Validate Story Save")]
        public static void RunProfileAndStoryChecks()
        {
            string path = Path.Combine(Path.GetTempPath(), "taste_passport_world_smoke_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(path,
                    "{\"schemaVersion\":1,\"playerName\":\"Legacy Explorer\",\"language\":\"ko\",\"avatarIndex\":2,\"created\":true," +
                    "\"levels\":[{\"levelId\":\"hoian-architecture\",\"bestScore\":240,\"bestStars\":2,\"playCount\":3}]} ");

                var store = new ProfileStore(path);
                var migrated = store.Load();
                Require(migrated.schemaVersion == 2, "schema-1 profile did not migrate to schema 2");
                Require(migrated.playerName == "Legacy Explorer" && migrated.language == "ko", "migration lost profile identity or language");
                var oldProgress = ProgressService.Get(migrated, "hoian-architecture");
                Require(oldProgress != null && oldProgress.bestScore == 240 && oldProgress.playCount == 3, "migration lost existing mission progress");

                bool first = StoryProgressService.RecordInteraction(migrated, "nhung-intro-seen", "daushu-book-discovered");
                bool repeat = StoryProgressService.RecordInteraction(migrated, "nhung-intro-seen", "daushu-book-discovered");
                Require(first && !repeat, "repeated interaction was not idempotent");
                Require(StoryProgressService.HasFlag(migrated, "nhung-intro-seen"), "story flag was not recorded");
                Require(StoryProgressService.HasImprint(migrated, "daushu-book-discovered"), "world imprint was not recorded");
                Require(migrated.storyFlags.Count == 1 && migrated.worldImprints.Count == 1, "repeated interaction duplicated saved marks");
                Require(StoryProgressService.AdvanceToChapter(migrated, 2), "chapter did not advance after the prologue interaction");
                Require(!StoryProgressService.AdvanceToChapter(migrated, 1) && migrated.chapterIndex == 2, "chapter progress moved backward");
                Require(store.TrySave(migrated), "could not save the migrated story profile");

                var reloaded = store.Load();
                Require(reloaded.schemaVersion == 2 && StoryProgressService.HasFlag(reloaded, "nhung-intro-seen"), "story flag did not survive reload");
                Require(StoryProgressService.HasImprint(reloaded, "daushu-book-discovered"), "world imprint did not survive reload");
                Require(ProgressService.HasStamp(reloaded, "hoian-architecture"), "existing stamp did not survive reload");
                Debug.Log("Dấu Xứ story save smoke PASS: schema migration, legacy progress, interaction idempotency, save and reload");
            }
            catch (Exception error)
            {
                Debug.LogError("Dấu Xứ story save smoke FAIL: " + error);
                throw;
            }
            finally
            {
                foreach (string file in new[] { path, path + ".bak", path + ".tmp", path + ".old" })
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

            var hoiAn = WorldCatalog.Get("hoian");
            var jeonju = WorldCatalog.Get("jeonju");
            Require(hoiAn != null && jeonju != null && hoiAn.BackgroundResourcePath != jeonju.BackgroundResourcePath,
                "the two destinations do not use separate backgrounds");
            Debug.Log("Dấu Xứ world bounds smoke PASS: movement clamps and destination art remains distinct");
        }

        [MenuItem("Tools/Taste Passport/Repair Character Sprite Imports")]
        public static void RepairCharacterSpriteImports()
        {
            string[] names = { "Traveler", "Nhung", "GaBo" };
            foreach (string name in names)
            {
                string assetPath = "Assets/_Game/Resources/Art/Characters/" + name + ".png";
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing character texture importer: " + assetPath);

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 200f;
                importer.spritePivot = new Vector2(0.5f, 0f);
                ReadPngDimensions(assetPath, out int width, out int height);
                if (width <= 0 || height <= 0) throw new InvalidOperationException("Invalid dimensions for " + assetPath);

                var importerData = new SerializedObject(importer);
                SerializedProperty spriteRects = importerData.FindProperty("m_SpriteSheet.m_Sprites");
                if (spriteRects == null || spriteRects.arraySize != 1)
                    throw new InvalidOperationException("Expected one persisted sprite rectangle for " + assetPath);
                SerializedProperty spriteRect = spriteRects.GetArrayElementAtIndex(0);
                spriteRect.FindPropertyRelative("m_Name").stringValue = name;
                spriteRect.FindPropertyRelative("m_Rect").rectValue = new Rect(0f, 0f, width, height);
                spriteRect.FindPropertyRelative("m_Alignment").intValue = (int)SpriteAlignment.Custom;
                spriteRect.FindPropertyRelative("m_Pivot").vector2Value = new Vector2(0.5f, 0f);
                importerData.ApplyModifiedPropertiesWithoutUndo();
                importer.SaveAndReimport();
            }

            RunCharacterSpriteChecks();
            Debug.Log("Dấu Xứ character sprite metadata repaired and verified");
        }

        [MenuItem("Tools/Taste Passport/Validate Character Sprites")]
        public static void RunCharacterSpriteChecks()
        {
            string[] names = { "Traveler", "Nhung", "GaBo" };
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
            Debug.Log("Dấu Xứ character sprite smoke PASS: all three 2:3 sprites load with single-sprite imports at 200 pixels per unit");
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
                session.SetPlayerPosition(WorldCatalog.Get("hoian").Interactables[1].Position);
                Require(session.InteractionButtonLabel == "Tương tác · Chùa Cầu", "world interaction button did not name Chùa Cầu");
                WorldInteractionResult landmark = session.InteractNearest();
                Require(landmark != null && landmark.Target.Id == "chuacau" &&
                        StoryProgressService.HasImprint(profile, "stamp-chuacau"),
                    "Chùa Cầu interaction did not save its exploration imprint");

                session.SetPlayerPosition(new Vector2(0f, -1.5f));
                sessionObject.SendMessage("OnApplicationPause", true, SendMessageOptions.RequireReceiver);
                Require(Mathf.Abs(store.Load().lastWorldY + 1.5f) < 0.001f,
                    "pausing the app did not save the current world position");
                session.SetPlayerPosition(new Vector2(0f, -1.25f));
                sessionObject.SendMessage("OnApplicationQuit", SendMessageOptions.RequireReceiver);
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
                session.SetPlayerPosition(WorldCatalog.Get("jeonju").Interactables[1].Position);
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
                Debug.Log("Dấu Xứ world session smoke PASS: Vietnamese target labels, first/repeat dialogue, transactional save failure, exploration marks, camera restore and checkpoint resume");
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
