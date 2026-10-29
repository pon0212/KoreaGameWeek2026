namespace TastePassport.Gameplay
{
    public sealed class LevelDefinition
    {
        public readonly string Id;
        public readonly string PlaceKey;
        public readonly string GuideKey;
        public readonly string[] ObjectKeys;

        public LevelDefinition(string id, string placeKey, string guideKey, string[] objectKeys)
        {
            Id = id;
            PlaceKey = placeKey;
            GuideKey = guideKey;
            ObjectKeys = objectKeys;
        }
    }

    public static class LevelCatalog
    {
        public static readonly LevelDefinition HoiAn = new LevelDefinition(
            "hoian-architecture", "hoian", "mai_mission",
            new[] { "lantern", "old_house", "boat", "bridge", "window", "market" });

        public static readonly LevelDefinition Jeonju = new LevelDefinition(
            "jeonju-architecture", "jeonju", "joon_mission",
            new[] { "hanok_roof", "hanok_gate", "courtyard", "tea_house", "wall", "tree" });
    }
}
