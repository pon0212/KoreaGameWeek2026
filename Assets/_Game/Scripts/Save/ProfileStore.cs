using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace TastePassport.Save
{
    public sealed class ProfileStore
    {
        private const int MaxFileBytes = 1024 * 1024;
        private const int MaxLevels = 64;
        private const int MaxLevelIdLength = 64;
        private const int MaxCounter = 1000000;
        private const int MaxCoins = 1000000;
        private const int MaxPlayerNameTextElements = 20;

        private readonly string path;
        public string FilePath => path;

        public ProfileStore(string path = null)
        {
            this.path = path ?? Path.Combine(Application.persistentDataPath, "taste_passport_profile.json");
        }

        public PlayerProfile Load()
        {
            var profile = Read(path);
            if (profile != null) return profile;

            profile = Read(path + ".bak");
            if (profile != null) return profile;

            return new PlayerProfile();
        }

        private static PlayerProfile Read(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                var info = new FileInfo(file);
                if (info.Length <= 0 || info.Length > MaxFileBytes)
                    throw new InvalidDataException("Profile file size is outside the supported range");

                var profile = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(file));
                if (profile == null || profile.schemaVersion != 1)
                    throw new InvalidDataException("Unsupported profile version");

                Normalize(profile);
                return profile;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Taste Passport: Could not read profile " + file + ": " + error.Message);
                return null;
            }
        }

        public bool TrySave(PlayerProfile profile)
        {
            string temporary = path + ".tmp";
            string displaced = path + ".old";
            try
            {
                if (profile == null) throw new ArgumentNullException(nameof(profile));
                var snapshot = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
                if (snapshot == null || snapshot.schemaVersion != 1)
                    throw new InvalidDataException("Unsupported profile version");
                Normalize(snapshot);

                string directory = Path.GetDirectoryName(Path.GetFullPath(path));
                Directory.CreateDirectory(directory);
                string json = JsonUtility.ToJson(snapshot, true);
                if (Encoding.UTF8.GetByteCount(json) > MaxFileBytes)
                    throw new InvalidDataException("Profile exceeds the supported size");
                File.WriteAllText(temporary, json);

                // Verify the exact bytes before publishing them.
                if (Read(temporary) == null) throw new InvalidDataException("Temporary profile is invalid");

                if (!File.Exists(path))
                {
                    File.Move(temporary, path);
                }
                else if (Read(path) != null)
                {
                    // Replace is atomic on supported filesystems and preserves the last good primary.
                    File.Replace(temporary, path, path + ".bak");
                }
                else
                {
                    // Keep a valid recovery backup intact when replacing a corrupt primary.
                    if (File.Exists(displaced)) File.Delete(displaced);
                    File.Replace(temporary, path, displaced);
                    if (File.Exists(displaced)) File.Delete(displaced);
                }

                CopySnapshot(profile, snapshot);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError("Taste Passport: Could not save profile: " + error.Message);
                return false;
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (Exception cleanupError) { Debug.LogWarning("Taste Passport: Could not remove temporary profile: " + cleanupError.Message); }
            }
        }

        private static void Normalize(PlayerProfile profile)
        {
            profile.language = profile.language == "ko" ? "ko" : "vi";
            profile.playerName = profile.playerName == null ? "" : profile.playerName.Trim();
            profile.playerName = LimitTextElements(profile.playerName, MaxPlayerNameTextElements);
            profile.avatarIndex = Mathf.Clamp(profile.avatarIndex, 0, 2);
            if (profile.created && profile.playerName.Length == 0) profile.created = false;

            profile.coins = Mathf.Clamp(profile.coins, 0, MaxCoins);
            if (profile.ownedOutfits == null) profile.ownedOutfits = new List<int>();
            var outfits = new List<int> { 0, 1 };
            var seenOutfits = new HashSet<int>(outfits);
            foreach (int outfit in profile.ownedOutfits)
                if (outfit >= 0 && outfit <= 4 && seenOutfits.Add(outfit)) outfits.Add(outfit);
            profile.ownedOutfits = outfits;
            profile.outfitId = outfits.Contains(profile.outfitId) ? profile.outfitId : 0;

            if (profile.levels == null) profile.levels = new List<LevelProgress>();
            var normalizedLevels = new List<LevelProgress>();
            var byId = new Dictionary<string, LevelProgress>(StringComparer.Ordinal);
            foreach (var level in profile.levels)
            {
                if (level == null || string.IsNullOrWhiteSpace(level.levelId)) continue;
                string id = level.levelId.Trim();
                if (id.Length > MaxLevelIdLength) continue;

                if (!byId.TryGetValue(id, out var existing))
                {
                    if (normalizedLevels.Count >= MaxLevels) continue;
                    existing = new LevelProgress { levelId = id };
                    byId.Add(id, existing);
                    normalizedLevels.Add(existing);
                }

                existing.bestScore = Math.Max(existing.bestScore, Mathf.Clamp(level.bestScore, 0, MaxCounter));
                existing.bestStars = Math.Max(existing.bestStars, Mathf.Clamp(level.bestStars, 0, 3));
                existing.playCount = (int)Math.Min(MaxCounter, (long)existing.playCount + Mathf.Clamp(level.playCount, 0, MaxCounter));
            }
            profile.levels = normalizedLevels;
        }

        private static string LimitTextElements(string value, int limit)
        {
            int[] starts = StringInfo.ParseCombiningCharacters(value);
            return starts.Length <= limit ? value : value.Substring(0, starts[limit]);
        }

        private static void CopySnapshot(PlayerProfile destination, PlayerProfile source)
        {
            destination.schemaVersion = source.schemaVersion;
            destination.playerName = source.playerName;
            destination.language = source.language;
            destination.avatarIndex = source.avatarIndex;
            destination.coins = source.coins;
            destination.outfitId = source.outfitId;
            destination.ownedOutfits = source.ownedOutfits;
            destination.created = source.created;
            destination.levels = source.levels;
        }
    }
}
