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
            "hoian-architecture", "hoian", "chuacau_mission",
            new[] { "chuacau_roof", "chuacau_covered_walkway", "chuacau_yinyang_tiles", "chuacau_shrine", "chuacau_wooden_arch", "chuacau_lai_vien" });

        public static readonly LevelDefinition Jeonju = new LevelDefinition(
            "jeonju-architecture", "jeonju", "joon_mission",
            new[] { "hanok_roof_tiles", "hanok_wooden_door", "hanji_windows", "hanok_courtyard", "hanok_eaves", "hanok_stone_wall" });
    }
}
