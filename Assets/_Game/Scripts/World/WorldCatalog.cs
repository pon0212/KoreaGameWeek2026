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

        public WorldInteractableDefinition(string id, string nameKey, string speakerKey, string firstDialogueKey,
            string repeatDialogueKey, string storyFlag, string imprintId, string spriteResourcePath,
            Vector2 position, float interactionRadius, bool isNpc)
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
                    "nhung-intro-seen", "dau-xu-book-discovered", "Art/Characters/Nhung", new Vector2(-3.7f, -1.72f), 1.35f, true),
                new WorldInteractableDefinition("chuacau", "landmark_chuacau", "dauxu_speaker", "chuacau_discovery_first", "chuacau_discovery_repeat",
                    "chuacau-observed", "stamp-chuacau", null, new Vector2(1.45f, -1.7f), 1.25f, false)
            });

        private static readonly WorldDefinition Jeonju = new WorldDefinition(
            "jeonju", "jeonju", "Art/Jeonju",
            new Vector2(-6.7f, -2.65f), new Vector2(6.7f, 0.25f), new Vector2(-5.1f, -1.8f),
            new[]
            {
                new WorldInteractableDefinition("gabo", "character_gabo", "character_gabo", "gabo_first", "gabo_repeat",
                    "gabo-met", "jeonju-postcard-opened", "Art/Characters/GaBo", new Vector2(-3.7f, -1.72f), 1.35f, true),
                new WorldInteractableDefinition("hanok", "landmark_hanok", "dauxu_speaker", "hanok_discovery_first", "hanok_discovery_repeat",
                    "hanok-observed", "stamp-hanok", null, new Vector2(1.45f, -1.7f), 1.25f, false)
            });

        public static WorldDefinition Get(string worldId) => worldId == "jeonju" ? Jeonju : HoiAn;
    }
}
