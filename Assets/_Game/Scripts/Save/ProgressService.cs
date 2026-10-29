using System;

namespace TastePassport.Save
{
    public static class ProgressService
    {
        public static LevelProgress Get(PlayerProfile profile, string levelId)
        {
            if (profile?.levels == null || string.IsNullOrEmpty(levelId)) return null;
            return profile.levels.Find(entry => entry != null && entry.levelId == levelId);
        }

        public static bool HasStamp(PlayerProfile profile, string levelId) => Get(profile, levelId) != null;

        public static void Record(PlayerProfile profile, string levelId, int score, int stars)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (string.IsNullOrEmpty(levelId)) throw new ArgumentException("Level ID is required", nameof(levelId));
            if (profile.levels == null) profile.levels = new System.Collections.Generic.List<LevelProgress>();
            var progress = Get(profile, levelId);
            if (progress == null)
            {
                progress = new LevelProgress { levelId = levelId };
                profile.levels.Add(progress);
            }
            progress.playCount++;
            progress.bestScore = Math.Max(progress.bestScore, score);
            progress.bestStars = Math.Max(progress.bestStars, stars);
        }
    }
}
