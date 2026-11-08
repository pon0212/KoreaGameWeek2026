using UnityEngine;

namespace TastePassport.World
{
    public sealed class WorldInteractableDefinition
    {
        public readonly string Id;
        public readonly string NameKey;
        public readonly string SpeakerKey;
        public readonly string FirstDialogueKey;
        public readonly string RepeatDialogueKey;
        public readonly string StoryFlag;
        public readonly string ImprintId;
        public readonly string SpriteResourcePath;
        public readonly Vector2 Position;
        public readonly float InteractionRadius;
        public readonly bool IsNpc;
        public readonly string ActivityId;
        public readonly string RequiredImprintId;
        public readonly string OptionalDialogueKey;

        public WorldInteractableDefinition(string id, string nameKey, string speakerKey, string firstDialogueKey,
            string repeatDialogueKey, string storyFlag, string imprintId, string spriteResourcePath,
            Vector2 position, float interactionRadius, bool isNpc, string activityId = null, string requiredImprintId = null,
            string optionalDialogueKey = null)
        {
            Id = id;
            NameKey = nameKey;
            SpeakerKey = speakerKey;
            FirstDialogueKey = firstDialogueKey;
            RepeatDialogueKey = repeatDialogueKey;
            StoryFlag = storyFlag;
            ImprintId = imprintId;
            SpriteResourcePath = spriteResourcePath;
            Position = position;
            InteractionRadius = interactionRadius;
            IsNpc = isNpc;
            ActivityId = activityId;
            RequiredImprintId = requiredImprintId;
            OptionalDialogueKey = optionalDialogueKey;
        }
    }

    public sealed class WorldDefinition
    {
        public readonly string Id;
        public readonly string NameKey;
        public readonly string BackgroundResourcePath;
        public readonly Vector2 MinimumPosition;
        public readonly Vector2 MaximumPosition;
        public readonly Vector2 SpawnPoint;
        public readonly WorldInteractableDefinition[] Interactables;

        public WorldDefinition(string id, string nameKey, string backgroundResourcePath,
            Vector2 minimumPosition, Vector2 maximumPosition, Vector2 spawnPoint,
            WorldInteractableDefinition[] interactables)
        {
            Id = id;
            NameKey = nameKey;
            BackgroundResourcePath = backgroundResourcePath;
            MinimumPosition = minimumPosition;
            MaximumPosition = maximumPosition;
            SpawnPoint = spawnPoint;
            Interactables = interactables;
        }
    }

    public static class WorldCatalog
    {
        private static readonly WorldDefinition HoiAn = new WorldDefinition(
            "hoian", "hoian", "Art/HoiAn",
            new Vector2(-6.7f, -2.65f), new Vector2(6.7f, 0.25f), new Vector2(-5.1f, -1.8f),
            new[]
            {
                new WorldInteractableDefinition("nhung", "character_nhung", "character_nhung", "prologue_nhung_first", "nhung_repeat",
                    "nhung-intro-seen", "dau-xu-book-discovered", "Art/Characters/Nhung", new Vector2(-3.7f, -1.72f), 1.35f, true,
                    optionalDialogueKey: "nhung_optional"),
                new WorldInteractableDefinition("lan", "character_lan", "character_lan", "lan_first", "lan_repeat",
                    "lan-met", "lantern-maker-met", "Art/Characters/Lan", new Vector2(-0.75f, -1.72f), 1.35f, true, "lantern-craft",
                    optionalDialogueKey: "lan_optional"),
                new WorldInteractableDefinition("chuacau", "landmark_chuacau", "dauxu_speaker", "chuacau_discovery_first", "chuacau_discovery_repeat",
                    "chuacau-observed", "stamp-chuacau", null, new Vector2(1.45f, -1.7f), 1.25f, false,
                    "hoian-chuacau"),
                new WorldInteractableDefinition("lantern-display", "lantern_display", "dauxu_speaker", "lantern_display_first", "lantern_display_repeat",
                    "lantern-display-seen", "lantern-display-visited", "Art/Props/HoiAnLantern", new Vector2(4.65f, -1.82f), 1.2f, false,
                    null, "lantern-display-ready")
            });

        private static readonly WorldDefinition Jeonju = new WorldDefinition(
            "jeonju", "jeonju", "Art/Jeonju",
            new Vector2(-6.7f, -2.65f), new Vector2(6.7f, 0.25f), new Vector2(-5.1f, -1.8f),
            new[]
            {
                new WorldInteractableDefinition("gabo", "character_gabo", "character_gabo", "gabo_first", "gabo_repeat",
                    "gabo-met", "jeonju-postcard-opened", "Art/Characters/GaBo", new Vector2(-3.7f, -1.72f), 1.35f, true,
                    optionalDialogueKey: "gabo_optional"),
                new WorldInteractableDefinition("bkim", "character_kim", "character_kim", "bkim_first", "bkim_repeat",
                    "bkim-met", "hanji-workshop-opened", "Art/Characters/BKim", new Vector2(-0.75f, -1.72f), 1.35f, true, "hanji-craft",
                    optionalDialogueKey: "bkim_optional"),
                new WorldInteractableDefinition("hanok", "landmark_hanok", "dauxu_speaker", "hanok_discovery_first", "hanok_discovery_repeat",
                    "hanok-observed", "stamp-hanok", null, new Vector2(1.05f, -1.7f), 1.25f, false,
                    "jeonju-hanok"),
                new WorldInteractableDefinition("gyeonggijeon", "landmark_gyeonggijeon", "dauxu_speaker", "gyeonggijeon_first", "gyeonggijeon_repeat",
                    "gyeonggijeon-discovered", "stamp-gyeonggijeon", null, new Vector2(2.65f, -1.7f), 1.2f, false),
                new WorldInteractableDefinition("pansori", "landmark_pansori", "dauxu_speaker", "pansori_first", "pansori_repeat",
                    "pansori-discovered", "stamp-pansori", null, new Vector2(4.15f, -1.7f), 1.2f, false),
                new WorldInteractableDefinition("hanji-display", "hanji_display", "dauxu_speaker", "hanji_display_first", "hanji_display_repeat",
                    "hanji-display-seen", "hanji-display-visited", "Art/Props/HanjiFan", new Vector2(5.55f, -1.7f), 1.15f, false,
                    null, "hanji-display-ready")
            });

        public static WorldDefinition Get(string worldId) => worldId == "jeonju" ? Jeonju : HoiAn;
    }
}
