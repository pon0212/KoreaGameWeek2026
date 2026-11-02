using System;
using System.Collections.Generic;

namespace TastePassport.Save
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;
        public string playerName = "";
        public string language = "vi";
        public int avatarIndex;
        public int coins;
        public int outfitId;
        public List<int> ownedOutfits = new List<int> { 0, 1 };
        public bool created;
        public List<LevelProgress> levels = new List<LevelProgress>();
        public int chapterIndex = 1;
        public bool mainStoryCompleted;
        public List<string> storyFlags = new List<string>();
        public List<string> worldImprints = new List<string>();
        public string lastWorldId = "hoian";
        public float lastWorldX = -6f;
        public float lastWorldY = -2f;

        public void CopyFrom(PlayerProfile source)
        {
            if (source == null) return;
            schemaVersion = source.schemaVersion;
            playerName = source.playerName;
            language = source.language;
            avatarIndex = source.avatarIndex;
            coins = source.coins;
            outfitId = source.outfitId;
            ownedOutfits = source.ownedOutfits == null ? new List<int>() : new List<int>(source.ownedOutfits);
            created = source.created;
            levels = new List<LevelProgress>();
            if (source.levels != null)
            {
                foreach (LevelProgress level in source.levels)
                    if (level != null)
                        levels.Add(new LevelProgress { levelId = level.levelId, bestScore = level.bestScore, bestStars = level.bestStars, playCount = level.playCount });
            }
            chapterIndex = source.chapterIndex;
            mainStoryCompleted = source.mainStoryCompleted;
            storyFlags = source.storyFlags == null ? new List<string>() : new List<string>(source.storyFlags);
            worldImprints = source.worldImprints == null ? new List<string>() : new List<string>(source.worldImprints);
            lastWorldId = source.lastWorldId;
            lastWorldX = source.lastWorldX;
            lastWorldY = source.lastWorldY;
        }
    }

    public static class PlayerProfileMigration
    {
        private const int MaxStoryMarks = 128;
        private const int MaxStoryMarkLength = 64;

        public static void Normalize(PlayerProfile profile)
        {
            if (profile == null) return;

            if (profile.schemaVersion == 1)
            {
                profile.chapterIndex = 1;
                profile.mainStoryCompleted = false;
                profile.storyFlags = new List<string>();
                profile.worldImprints = new List<string>();
                profile.lastWorldId = "hoian";
                profile.lastWorldX = -6f;
                profile.lastWorldY = -2f;
            }

            if (profile.storyFlags == null) profile.storyFlags = new List<string>();
            if (profile.worldImprints == null) profile.worldImprints = new List<string>();
            profile.storyFlags = NormalizeIds(profile.storyFlags);
            profile.worldImprints = NormalizeIds(profile.worldImprints);
            profile.chapterIndex = profile.mainStoryCompleted ? 4 : Math.Max(1, Math.Min(4, profile.chapterIndex));
            profile.lastWorldId = profile.lastWorldId == "jeonju" ? "jeonju" : "hoian";
            if (!IsFinite(profile.lastWorldX)) profile.lastWorldX = -6f;
            if (!IsFinite(profile.lastWorldY)) profile.lastWorldY = -2f;
            profile.lastWorldX = Math.Max(-12f, Math.Min(12f, profile.lastWorldX));
            profile.lastWorldY = Math.Max(-5f, Math.Min(5f, profile.lastWorldY));
            profile.schemaVersion = PlayerProfile.CurrentSchemaVersion;
        }

        private static List<string> NormalizeIds(List<string> values)
        {
            var normalized = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string value in values)
            {
                if (normalized.Count >= MaxStoryMarks) break;
                if (string.IsNullOrWhiteSpace(value)) continue;
                string id = value.Trim();
                if (id.Length > MaxStoryMarkLength || !seen.Add(id)) continue;
                normalized.Add(id);
            }
            return normalized;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
