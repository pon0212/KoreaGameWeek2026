using System.Collections.Generic;
using TastePassport.Story;

namespace TastePassport.Save
{
    public static class InventoryService
    {
        public const string ChuaCauCardId = "culture-hoian-chuacau";
        public const string LanternCardId = "culture-hoian-lantern";
        public const string CaoLauCardId = "culture-hoian-caolau";
        public const string HanokCardId = "culture-jeonju-hanok";
        public const string HanjiCardId = "culture-jeonju-hanji";
        public const string BibimbapCardId = "culture-jeonju-bibimbap";
        public const string GyeonggijeonCardId = "culture-jeonju-gyeonggijeon";
        public const string PansoriCardId = "culture-jeonju-pansori";
        public const string LanternProductPrefix = "crafted-lantern-";
        public const string HanjiProductPrefix = "crafted-hanji-";
        private const int MaximumCollectionSize = 128;
        private static readonly HashSet<string> LanternFrames = new HashSet<string> { "round", "diamond", "hexagon" };
        private static readonly HashSet<string> LanternColors = new HashSet<string> { "red", "gold", "blue", "green" };
        private static readonly HashSet<string> HanjiShapes = new HashSet<string> { "fan", "bookmark", "flower" };
        private static readonly HashSet<string> HanjiColors = new HashSet<string> { "indigo", "vermilion", "gold", "jade" };
        private static readonly HashSet<string> HanjiMotifs = new HashSet<string> { "flower", "cloud", "stripe" };

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

        public static bool RecordHanjiCraft(PlayerProfile profile, string shapeId, string colorId, string motifId)
        {
            if (profile == null || !IsHanjiShape(shapeId) || !IsHanjiColor(colorId) || !IsHanjiMotif(motifId)) return false;
            string itemId = HanjiProductPrefix + shapeId + "-" + colorId + "-" + motifId;
            bool hasProduct = HasItem(profile, itemId);
            bool hasCard = HasCultureCard(profile, HanjiCardId);
            if (!hasProduct && (profile.inventoryItems == null || profile.inventoryItems.Count >= MaximumCollectionSize)) return false;
            if (!hasCard && (profile.culturalCards == null || profile.culturalCards.Count >= MaximumCollectionSize)) return false;
            if (!hasProduct) AddItem(profile, itemId);
            if (!hasCard) AddCultureCard(profile, HanjiCardId);
            StoryProgressService.RecordInteraction(profile, "jeonju-hanji-crafted", "hanji-display-ready");
            return true;
        }

        public static string GetLatestCraftedHanji(PlayerProfile profile)
        {
            if (profile?.inventoryItems == null) return null;
            for (int i = profile.inventoryItems.Count - 1; i >= 0; i--)
            {
                string item = profile.inventoryItems[i];
                if (GetHanjiShapeId(item) != null) return item;
            }
            return null;
        }

        public static string GetHanjiShapeId(string itemId) => GetHanjiVariantPart(itemId, 0);
        public static string GetHanjiColorId(string itemId) => GetHanjiVariantPart(itemId, 1);
        public static string GetHanjiMotifId(string itemId) => GetHanjiVariantPart(itemId, 2);
        public static bool IsHanjiShape(string shapeId) => HanjiShapes.Contains(shapeId);
        public static bool IsHanjiColor(string colorId) => HanjiColors.Contains(colorId);
        public static bool IsHanjiMotif(string motifId) => HanjiMotifs.Contains(motifId);

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

        private static string GetHanjiVariantPart(string itemId, int partIndex)
        {
            if (string.IsNullOrEmpty(itemId) || !itemId.StartsWith(HanjiProductPrefix, System.StringComparison.Ordinal)) return null;
            string[] parts = itemId.Substring(HanjiProductPrefix.Length).Split('-');
            if (parts.Length != 3) return null;
            string value = parts[partIndex];
            if (partIndex == 0) return IsHanjiShape(value) ? value : null;
            if (partIndex == 1) return IsHanjiColor(value) ? value : null;
            return IsHanjiMotif(value) ? value : null;
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
