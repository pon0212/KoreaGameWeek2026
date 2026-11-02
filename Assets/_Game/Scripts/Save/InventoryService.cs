using System.Collections.Generic;
using TastePassport.Story;

namespace TastePassport.Save
{
    public static class InventoryService
    {
        public const string ChuaCauCardId = "culture-hoian-chuacau";
        public const string LanternCardId = "culture-hoian-lantern";
        public const string CaoLauCardId = "culture-hoian-caolau";
        public const string LanternProductPrefix = "crafted-lantern-";
        private const int MaximumCollectionSize = 128;
        private static readonly HashSet<string> LanternFrames = new HashSet<string> { "round", "diamond", "hexagon" };
        private static readonly HashSet<string> LanternColors = new HashSet<string> { "red", "gold", "blue", "green" };

        public static bool HasItem(PlayerProfile profile, string itemId) => Contains(profile?.inventoryItems, itemId);
        public static bool HasCultureCard(PlayerProfile profile, string cardId) => Contains(profile?.culturalCards, cardId);

        public static bool AddItem(PlayerProfile profile, string itemId) => AddUnique(profile?.inventoryItems, itemId);
        public static bool AddCultureCard(PlayerProfile profile, string cardId) => AddUnique(profile?.culturalCards, cardId);

        public static bool RecordChuaCauDiscovery(PlayerProfile profile)
        {
            if (profile == null) return false;
            bool changed = AddCultureCard(profile, ChuaCauCardId);
            changed |= StoryProgressService.RecordInteraction(profile, "hoian-chuacau-discovered", "stamp-chuacau");
            return changed;
        }

        public static bool RecordCaoLauDiscovery(PlayerProfile profile) => AddCultureCard(profile, CaoLauCardId);

        public static bool RecordLanternCraft(PlayerProfile profile, string frameId, string colorId)
        {
            if (profile == null || !LanternFrames.Contains(frameId) || !LanternColors.Contains(colorId)) return false;
            string itemId = LanternProductPrefix + frameId + "-" + colorId;
            if (!HasItem(profile, itemId) && (profile.inventoryItems == null || profile.inventoryItems.Count >= MaximumCollectionSize))
                return false;
            AddItem(profile, itemId);
            AddCultureCard(profile, LanternCardId);
            StoryProgressService.RecordInteraction(profile, "hoian-lantern-crafted", "lantern-display-ready");
            return true;
        }

        public static string GetCraftedLantern(PlayerProfile profile)
        {
            if (profile?.inventoryItems == null) return null;
            for (int i = profile.inventoryItems.Count - 1; i >= 0; i--)
            {
                string item = profile.inventoryItems[i];
                if (!string.IsNullOrEmpty(item) && item.StartsWith(LanternProductPrefix, System.StringComparison.Ordinal))
                    return item;
            }
            return null;
        }

        public static string GetLanternFrameId(string itemId) => GetLanternVariantPart(itemId, 0);
        public static string GetLanternColorId(string itemId) => GetLanternVariantPart(itemId, 1);

        public static bool IsLanternFrame(string frameId) => LanternFrames.Contains(frameId);
        public static bool IsLanternColor(string colorId) => LanternColors.Contains(colorId);

        private static bool Contains(List<string> values, string id) =>
            !string.IsNullOrWhiteSpace(id) && values != null && values.Contains(id);

        private static string GetLanternVariantPart(string itemId, int partIndex)
        {
            if (string.IsNullOrEmpty(itemId) || !itemId.StartsWith(LanternProductPrefix, System.StringComparison.Ordinal)) return null;
            string[] parts = itemId.Substring(LanternProductPrefix.Length).Split('-');
            if (parts.Length != 2) return null;
            string value = parts[partIndex];
            return partIndex == 0 ? (IsLanternFrame(value) ? value : null) : (IsLanternColor(value) ? value : null);
        }

        private static bool AddUnique(List<string> values, string id)
        {
            if (values == null || string.IsNullOrWhiteSpace(id) || id.Length > 64 || values.Contains(id) || values.Count >= MaximumCollectionSize)
                return false;
            values.Add(id);
            return true;
        }
    }
}
