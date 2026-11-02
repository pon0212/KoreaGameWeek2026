using System.Collections.Generic;
using TastePassport.Save;

namespace TastePassport.Story
{
    public static class StoryProgressService
    {
        public static bool HasFlag(PlayerProfile profile, string flagId) => Contains(profile?.storyFlags, flagId);

        public static bool AddFlag(PlayerProfile profile, string flagId) => AddUnique(profile, flagId, true);

        public static bool HasImprint(PlayerProfile profile, string imprintId) => Contains(profile?.worldImprints, imprintId);

        public static bool AddImprint(PlayerProfile profile, string imprintId) => AddUnique(profile, imprintId, false);

        public static bool RecordInteraction(PlayerProfile profile, string storyFlag, string imprintId)
        {
            if (profile == null) return false;
            bool changed = false;
            if (!string.IsNullOrWhiteSpace(storyFlag)) changed |= AddFlag(profile, storyFlag);
            if (!string.IsNullOrWhiteSpace(imprintId)) changed |= AddImprint(profile, imprintId);
            return changed;
        }

        public static bool AdvanceToChapter(PlayerProfile profile, int chapterIndex)
        {
            if (profile == null || profile.mainStoryCompleted) return false;
            int target = chapterIndex < 1 ? 1 : chapterIndex > 4 ? 4 : chapterIndex;
            if (target <= profile.chapterIndex) return false;
            profile.chapterIndex = target;
            return true;
        }

        public static bool CompleteMainStory(PlayerProfile profile)
        {
            if (profile == null) return false;
            bool changed = false;
            if (profile.chapterIndex < 4)
            {
                profile.chapterIndex = 4;
                changed = true;
            }
            if (!profile.mainStoryCompleted)
            {
                profile.mainStoryCompleted = true;
                changed = true;
            }
            changed |= AddFlag(profile, "main-story-completed");
            changed |= AddImprint(profile, "festival-completed");
            return changed;
        }

        private static bool Contains(List<string> values, string id) =>
            values != null && !string.IsNullOrWhiteSpace(id) && values.Contains(id.Trim());

        private static bool AddUnique(PlayerProfile profile, string id, bool storyFlag)
        {
            if (profile == null || string.IsNullOrWhiteSpace(id)) return false;
            id = id.Trim();
            List<string> values = storyFlag ? profile.storyFlags : profile.worldImprints;
            if (values == null)
            {
                values = new List<string>();
                if (storyFlag) profile.storyFlags = values;
                else profile.worldImprints = values;
            }
            if (values.Contains(id)) return false;
            values.Add(id);
            return true;
        }
    }
}
