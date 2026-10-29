namespace TastePassport.Gameplay
{
    public sealed class RecipeDefinition
    {
        public readonly string Id;
        public readonly string PlaceKey;
        public readonly string DishKey;
        public readonly string GuideKey;
        public readonly string[] Steps;
        public readonly string[] Decoys;

        public RecipeDefinition(string id, string placeKey, string dishKey, string guideKey, string[] steps, string[] decoys)
        {
            Id = id;
            PlaceKey = placeKey;
            DishKey = dishKey;
            GuideKey = guideKey;
            Steps = steps;
            Decoys = decoys;
        }
    }

    public static class RecipeCatalog
    {
        public static readonly RecipeDefinition CaoLau = new RecipeDefinition(
            "hoian-cao-lau", "hoian", "cao_lau", "lan_mission",
            new[] { "cao_lau_noodles", "cao_lau_greens", "cao_lau_pork" },
            new[] { "decoy_cheese", "decoy_kimchi" });

        public static readonly RecipeDefinition Bibimbap = new RecipeDefinition(
            "jeonju-bibimbap", "jeonju", "bibimbap", "kim_mission",
            new[] { "bibi_rice", "bibi_vegetables", "bibi_egg" },
            new[] { "decoy_cheese", "decoy_noodles" });
    }
}
