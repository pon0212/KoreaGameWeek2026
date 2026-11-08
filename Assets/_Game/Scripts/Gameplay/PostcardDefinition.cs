using TastePassport.Save;

namespace TastePassport.Gameplay
{
    public sealed class PostcardDefinition
    {
        public readonly string Id;
        public readonly string TitleKey;
        public readonly string FrontLeftKey;
        public readonly string FrontRightKey;
        public readonly string StoryKey;
        public readonly string FactKey;
        public readonly string[] RequiredLevelIds;
        public readonly string[] RequiredCultureCardIds;

        public PostcardDefinition(string id, string titleKey, string frontLeftKey, string frontRightKey, string storyKey, string factKey, params string[] requiredLevelIds)
            : this(id, titleKey, frontLeftKey, frontRightKey, storyKey, factKey, requiredLevelIds, new string[0])
        {
        }

        public PostcardDefinition(string id, string titleKey, string frontLeftKey, string frontRightKey, string storyKey,
            string factKey, string[] requiredLevelIds, string[] requiredCultureCardIds)
        {
            Id = id;
            TitleKey = titleKey;
            FrontLeftKey = frontLeftKey;
            FrontRightKey = frontRightKey;
            StoryKey = storyKey;
            FactKey = factKey;
            RequiredLevelIds = requiredLevelIds;
            RequiredCultureCardIds = requiredCultureCardIds;
        }

        public bool IsUnlocked(PlayerProfile profile)
        {
            foreach (var levelId in RequiredLevelIds)
                if (!ProgressService.HasStamp(profile, levelId)) return false;
            foreach (var cardId in RequiredCultureCardIds)
                if (!InventoryService.HasCultureCard(profile, cardId)) return false;
            return true;
        }
    }

    public static class PostcardCatalog
    {
        public static readonly PostcardDefinition[] All =
        {
            new PostcardDefinition("architecture", "postcard_1", "hoian", "jeonju", "postcard_1_story", "postcard_1_fact", LevelCatalog.HoiAn.Id, LevelCatalog.Jeonju.Id),
            new PostcardDefinition("flavors", "postcard_2", "cao_lau", "bibimbap", "postcard_2_story", "postcard_2_fact", RecipeCatalog.CaoLau.Id, RecipeCatalog.Bibimbap.Id),
            new PostcardDefinition("friendship", "postcard_3", "hoian", "jeonju", "postcard_3_story", "postcard_3_fact", "hoian-culture", "jeonju-culture"),
            new PostcardDefinition("chuacau", "postcard_chuacau", "landmark_chuacau", "hoian", "postcard_chuacau_story",
                "postcard_chuacau_fact", new string[0], new[] { InventoryService.ChuaCauCardId })
        };
    }
}
