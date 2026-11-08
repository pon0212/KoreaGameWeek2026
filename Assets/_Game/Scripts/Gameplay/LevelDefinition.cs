using System;

namespace TastePassport.Gameplay
{
    public sealed class LevelDefinition
    {
        public readonly string Id;
        public readonly string PlaceKey;
        public readonly string GuideKey;
        public readonly string[] ObjectKeys;
        public int DifficultyTier => 0;
        public string DifficultyTierKey => "journey_tier_familiar";
        public readonly int[] RequiredObjectIndices;
        public readonly string[] AssemblySlotKeys;
        public readonly int[] RequiredRotations;

        public LevelDefinition(string id, string placeKey, string guideKey, string[] objectKeys,
            int[] requiredObjectIndices = null, string[] assemblySlotKeys = null, int[] requiredRotations = null)
        {
            Id = id;
            PlaceKey = placeKey;
            GuideKey = guideKey;
            ObjectKeys = objectKeys;
            RequiredObjectIndices = requiredObjectIndices != null && requiredObjectIndices.Length == 3
                ? requiredObjectIndices : new[] { 0, 1, 2 };
            AssemblySlotKeys = assemblySlotKeys != null && assemblySlotKeys.Length == 3
                ? assemblySlotKeys : new[] { "slot", "slot", "slot" };
            RequiredRotations = requiredRotations != null && requiredRotations.Length == 3
                ? requiredRotations : new[] { 0, 0, 0 };
            if (RequiredObjectIndices[0] == RequiredObjectIndices[1] ||
                RequiredObjectIndices[0] == RequiredObjectIndices[2] ||
                RequiredObjectIndices[1] == RequiredObjectIndices[2])
                throw new ArgumentException("An architecture puzzle needs three distinct details.", nameof(requiredObjectIndices));
            for (int i = 0; i < RequiredRotations.Length; i++)
            {
                if (RequiredRotations[i] % 90 != 0)
                    throw new ArgumentException("Architecture model rotations must use quarter turns.", nameof(requiredRotations));
                RequiredRotations[i] = ((RequiredRotations[i] % 360) + 360) % 360;
            }
            if (AssemblySlotKeys[0] == AssemblySlotKeys[1] ||
                AssemblySlotKeys[0] == AssemblySlotKeys[2] ||
                AssemblySlotKeys[1] == AssemblySlotKeys[2])
                throw new ArgumentException("An architecture puzzle needs three distinct assembly slots.", nameof(assemblySlotKeys));
        }
    }

    public static class LevelCatalog
    {
        public static readonly LevelDefinition HoiAn = new LevelDefinition(
            "hoian-architecture", "hoian", "chuacau_mission",
            new[] { "chuacau_roof", "chuacau_covered_walkway", "chuacau_yinyang_tiles", "chuacau_shrine", "chuacau_wooden_arch", "chuacau_lai_vien" },
            new[] { 0, 1, 3 }, new[] { "chuacau_slot_roof", "chuacau_slot_walkway", "chuacau_slot_shrine" });

        public static readonly LevelDefinition Jeonju = new LevelDefinition(
            "jeonju-architecture", "jeonju", "joon_mission",
            new[] { "hanok_roof_tiles", "hanok_wooden_door", "hanji_windows", "hanok_courtyard", "hanok_eaves", "hanok_stone_wall" },
            new[] { 0, 1, 2 }, new[] { "hanok_slot_roof", "hanok_slot_door", "hanok_slot_windows" }, new[] { 180, 90, 0 });
    }
}
