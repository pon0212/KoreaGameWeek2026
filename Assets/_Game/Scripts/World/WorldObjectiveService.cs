using TastePassport.Save;
using TastePassport.Story;

namespace TastePassport.World
{
    public sealed class WorldObjectiveDefinition
    {
        public readonly string Id;
        public readonly string TitleKey;
        public readonly string DescriptionKey;
        public readonly string TargetId;

        public WorldObjectiveDefinition(string id, string titleKey, string descriptionKey, string targetId)
        {
            Id = id;
            TitleKey = titleKey;
            DescriptionKey = descriptionKey;
            TargetId = targetId;
        }
    }

    public static class WorldObjectiveService
    {
        private static readonly WorldObjectiveDefinition FindNhung =
            new WorldObjectiveDefinition("hoian-find-nhung", "world_objective_find_nhung", "world_hint_find_nhung", "nhung");
        private static readonly WorldObjectiveDefinition TalkToNhung =
            new WorldObjectiveDefinition("hoian-talk-nhung", "world_objective_talk_nhung", "world_hint_talk_nhung", "nhung");
        private static readonly WorldObjectiveDefinition FindLan =
            new WorldObjectiveDefinition("hoian-find-lan", "world_objective_find_lan", "world_hint_find_lan", "lan");
        private static readonly WorldObjectiveDefinition CraftLantern =
            new WorldObjectiveDefinition("hoian-craft-lantern", "world_objective_craft_lantern", "world_hint_craft_lantern", "lan");
        private static readonly WorldObjectiveDefinition SeeLantern =
            new WorldObjectiveDefinition("hoian-see-lantern", "world_objective_see_lantern", "world_hint_see_lantern", "lantern-display");
        private static readonly WorldObjectiveDefinition ViewPassport =
            new WorldObjectiveDefinition("hoian-view-passport", "world_objective_view_passport", "world_hint_view_passport", null);
        private static readonly WorldObjectiveDefinition FindGaBo =
            new WorldObjectiveDefinition("jeonju-find-gabo", "world_objective_find_gabo", "world_hint_find_gabo", "gabo");
        private static readonly WorldObjectiveDefinition FindKim =
            new WorldObjectiveDefinition("jeonju-find-kim", "world_objective_find_kim", "world_hint_find_kim", "bkim");
        private static readonly WorldObjectiveDefinition CraftHanji =
            new WorldObjectiveDefinition("jeonju-craft-hanji", "world_objective_craft_hanji", "world_hint_craft_hanji", "bkim");
        private static readonly WorldObjectiveDefinition SeeHanji =
            new WorldObjectiveDefinition("jeonju-see-hanji", "world_objective_see_hanji", "world_hint_see_hanji", "hanji-display");
        private static readonly WorldObjectiveDefinition Explore =
            new WorldObjectiveDefinition("world-free-exploration", "world_objective_free_explore", "world_hint_free_explore", null);

        public static WorldObjectiveDefinition GetCurrent(string worldId, PlayerProfile profile)
        {
            if (worldId == "jeonju")
            {
                if (!StoryProgressService.HasFlag(profile, "gabo-met")) return FindGaBo;
                if (!StoryProgressService.HasFlag(profile, "bkim-met")) return FindKim;
                if (!StoryProgressService.HasImprint(profile, "hanji-display-ready")) return CraftHanji;
                if (!StoryProgressService.HasFlag(profile, "hanji-display-seen")) return SeeHanji;
                return Explore;
            }

            if (!StoryProgressService.HasFlag(profile, "nhung-intro-seen"))
                return StoryProgressService.HasFlag(profile, "tutorial-nhung-reached") ? TalkToNhung : FindNhung;
            if (!StoryProgressService.HasFlag(profile, "lan-met")) return FindLan;
            if (!StoryProgressService.HasImprint(profile, "lantern-display-ready")) return CraftLantern;
            if (!StoryProgressService.HasFlag(profile, "lantern-display-seen")) return SeeLantern;
            if (!StoryProgressService.HasFlag(profile, "tutorial-passport-viewed")) return ViewPassport;
            return Explore;
        }
    }
}
