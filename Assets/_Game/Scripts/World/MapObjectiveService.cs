using TastePassport.Gameplay;
using TastePassport.Save;
using TastePassport.Story;

namespace TastePassport.World
{
    public enum MapObjectiveAction
    {
        ExploreWorld,
        OpenPassport,
        HoiAnArchitecture,
        CaoLau,
        HoiAnExchange,
        JeonjuArchitecture,
        Bibimbap,
        JeonjuExchange,
        LanternCraft,
        HanjiCraft,
        Festival,
        StoryCardTarget
    }

    public sealed class MapObjectiveDefinition
    {
        public readonly string TitleKey;
        public readonly string DescriptionKey;
        public readonly string ActionLabelKey;
        public readonly MapObjectiveAction Action;
        public readonly string WorldId;
        public readonly string TargetId;

        public MapObjectiveDefinition(string titleKey, string descriptionKey, string actionLabelKey,
            MapObjectiveAction action, string worldId = null, string targetId = null)
        {
            TitleKey = titleKey;
            DescriptionKey = descriptionKey;
            ActionLabelKey = actionLabelKey;
            Action = action;
            WorldId = worldId;
            TargetId = targetId;
        }
    }

    public static class MapObjectiveService
    {
        private static readonly MapObjectiveDefinition MeetNhung =
            World("world_objective_find_nhung", "world_hint_find_nhung", "hoian", "nhung");
        private static readonly MapObjectiveDefinition MeetLan =
            World("world_objective_find_lan", "world_hint_find_lan", "hoian", "lan");
        private static readonly MapObjectiveDefinition CraftLantern =
            World("world_objective_craft_lantern", "world_hint_craft_lantern", "hoian", "lan");
        private static readonly MapObjectiveDefinition SeeLantern =
            World("world_objective_see_lantern", "world_hint_see_lantern", "hoian", "lantern-display");
        private static readonly MapObjectiveDefinition ViewPassport =
            new MapObjectiveDefinition("world_objective_view_passport", "world_hint_view_passport",
                "map_do_next", MapObjectiveAction.OpenPassport);
        private static readonly MapObjectiveDefinition HoianMission = Mission("chuacau_mission", MapObjectiveAction.HoiAnArchitecture);
        private static readonly MapObjectiveDefinition CaoLauMission = Mission("cao_lau", MapObjectiveAction.CaoLau);
        private static readonly MapObjectiveDefinition HoianExchange = Mission("hoian_exchange", MapObjectiveAction.HoiAnExchange);
        private static readonly MapObjectiveDefinition JeonjuMission = Mission("joon_mission", MapObjectiveAction.JeonjuArchitecture);
        private static readonly MapObjectiveDefinition BibimbapMission = Mission("bibimbap", MapObjectiveAction.Bibimbap);
        private static readonly MapObjectiveDefinition JeonjuExchange = Mission("jeonju_exchange", MapObjectiveAction.JeonjuExchange);
        private static readonly MapObjectiveDefinition LanternCraftMission =
            new MapObjectiveDefinition("world_objective_craft_lantern", "world_hint_craft_lantern",
                "lantern_craft_map", MapObjectiveAction.LanternCraft);
        private static readonly MapObjectiveDefinition HanjiCraftMission =
            new MapObjectiveDefinition("world_objective_craft_hanji", "world_hint_craft_hanji",
                "hanji_craft_map", MapObjectiveAction.HanjiCraft);
        private static readonly MapObjectiveDefinition Festival =
            new MapObjectiveDefinition("finale_start", "finale_clue", "finale_start", MapObjectiveAction.Festival);
        private static readonly MapObjectiveDefinition FreeExploreHoiAn = FreeExplore("hoian");
        private static readonly MapObjectiveDefinition FreeExploreJeonju = FreeExplore("jeonju");

        public static MapObjectiveDefinition GetCurrent(PlayerProfile profile, bool jeonjuUnlocked)
        {
            if (profile == null) return MeetNhung;
            if (profile.mainStoryCompleted) return jeonjuUnlocked ? FreeExploreJeonju : FreeExploreHoiAn;

            // Older saves can have completed Hoi An missions without the later onboarding flags.
            bool hoiAnCompleted = ProgressService.HasStamp(profile, LevelCatalog.HoiAn.Id) &&
                ProgressService.HasStamp(profile, RecipeCatalog.CaoLau.Id) &&
                ProgressService.HasStamp(profile, MatchCatalog.HoiAn.Id);
            bool legacySaveMissingNewCrafts = hoiAnCompleted &&
                !StoryProgressService.HasImprint(profile, "lantern-display-ready");
            if (!hoiAnCompleted)
            {
                if (!StoryProgressService.HasFlag(profile, "nhung-intro-seen")) return MeetNhung;
                if (!StoryProgressService.HasFlag(profile, "lan-met")) return MeetLan;
                if (!StoryProgressService.HasImprint(profile, "lantern-display-ready")) return CraftLantern;
                if (!StoryProgressService.HasFlag(profile, "lantern-display-seen")) return SeeLantern;
                if (!StoryProgressService.HasFlag(profile, "tutorial-passport-viewed")) return ViewPassport;
            }

            if (!ProgressService.HasStamp(profile, LevelCatalog.HoiAn.Id)) return HoianMission;
            if (!legacySaveMissingNewCrafts && !StoryProgressService.HasImprint(profile, "lantern-display-ready"))
                return LanternCraftMission;
            if (!ProgressService.HasStamp(profile, RecipeCatalog.CaoLau.Id)) return CaoLauMission;
            if (!ProgressService.HasStamp(profile, MatchCatalog.HoiAn.Id)) return HoianExchange;
            if (!jeonjuUnlocked) return MeetNhung;
            if (!ProgressService.HasStamp(profile, LevelCatalog.Jeonju.Id)) return JeonjuMission;
            if (!legacySaveMissingNewCrafts && !StoryProgressService.HasImprint(profile, "hanji-display-ready"))
                return HanjiCraftMission;
            if (!ProgressService.HasStamp(profile, RecipeCatalog.Bibimbap.Id)) return BibimbapMission;
            if (!ProgressService.HasStamp(profile, MatchCatalog.Jeonju.Id)) return JeonjuExchange;
            if (!StoryProgressService.HasImprint(profile, "lantern-display-ready")) return LanternCraftMission;
            if (!StoryProgressService.HasImprint(profile, "hanji-display-ready")) return HanjiCraftMission;
            return Festival;
        }

        private static MapObjectiveDefinition World(string title, string description, string worldId, string targetId) =>
            new MapObjectiveDefinition(title, description, "map_do_next", MapObjectiveAction.StoryCardTarget, worldId, targetId);

        private static MapObjectiveDefinition Mission(string title, MapObjectiveAction action) =>
            new MapObjectiveDefinition(title, "map_goal_mission_hint", "map_do_next", action);

        private static MapObjectiveDefinition FreeExplore(string worldId) =>
            new MapObjectiveDefinition("world_objective_free_explore", "world_hint_free_explore",
                "map_do_next", MapObjectiveAction.ExploreWorld, worldId);
    }
}
