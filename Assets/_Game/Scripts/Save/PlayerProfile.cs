using System;
using System.Collections.Generic;

namespace TastePassport.Save
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public const int CurrentSchemaVersion = 4;

        public int schemaVersion = CurrentSchemaVersion;
        public string playerName = "";
        public string language = "vi";
        public int avatarIndex;
        public int coins;
        public int outfitId;
        public List<int> ownedOutfits = new List<int> { 0, 1 };
        public bool created;
        public List<LevelProgress> levels = new List<LevelProgress>();
        public int chapterIndex = 1;
        public bool mainStoryCompleted;
        public List<string> storyFlags = new List<string>();
        public List<string> worldImprints = new List<string>();
        public List<string> inventoryItems = new List<string>();
        public List<string> culturalCards = new List<string>();
        public List<MemoryPhotoRecord> memoryPhotos = new List<MemoryPhotoRecord>();
        public List<JourneyEventRecord> journeyEvents = new List<JourneyEventRecord>();
        public List<JournalPageState> journalPages = new List<JournalPageState>();
        public List<FestivalDecorationRecord> festivalDecorations = new List<FestivalDecorationRecord>();
        public string lastWorldId = "hoian";
        public float lastWorldX = -5.1f;
        public float lastWorldY = -1.8f;

        public void CopyFrom(PlayerProfile source)
        {
            if (source == null) return;
            schemaVersion = source.schemaVersion;
            playerName = source.playerName;
            language = source.language;
            avatarIndex = source.avatarIndex;
            coins = source.coins;
            outfitId = source.outfitId;
            ownedOutfits = source.ownedOutfits == null ? new List<int>() : new List<int>(source.ownedOutfits);
            created = source.created;
            levels = new List<LevelProgress>();
            if (source.levels != null)
            {
                foreach (LevelProgress level in source.levels)
                    if (level != null)
                        levels.Add(new LevelProgress { levelId = level.levelId, bestScore = level.bestScore, bestStars = level.bestStars, playCount = level.playCount });
            }
            chapterIndex = source.chapterIndex;
            mainStoryCompleted = source.mainStoryCompleted;
            storyFlags = source.storyFlags == null ? new List<string>() : new List<string>(source.storyFlags);
            worldImprints = source.worldImprints == null ? new List<string>() : new List<string>(source.worldImprints);
            inventoryItems = source.inventoryItems == null ? new List<string>() : new List<string>(source.inventoryItems);
            culturalCards = source.culturalCards == null ? new List<string>() : new List<string>(source.culturalCards);
            memoryPhotos = new List<MemoryPhotoRecord>();
            if (source.memoryPhotos != null)
                foreach (MemoryPhotoRecord photo in source.memoryPhotos)
                    if (photo != null) memoryPhotos.Add(new MemoryPhotoRecord
                    {
                        photoId = photo.photoId, worldId = photo.worldId, locationId = photo.locationId,
                        capturedAtUtc = photo.capturedAtUtc
                    });
            journeyEvents = new List<JourneyEventRecord>();
            if (source.journeyEvents != null)
                foreach (JourneyEventRecord entry in source.journeyEvents)
                    if (entry != null) journeyEvents.Add(new JourneyEventRecord
                    {
                        eventId = entry.eventId, worldId = entry.worldId, createdAtUtc = entry.createdAtUtc
                    });
            journalPages = new List<JournalPageState>();
            if (source.journalPages != null)
                foreach (JournalPageState page in source.journalPages)
                    if (page != null) journalPages.Add(new JournalPageState
                    {
                        pageId = page.pageId, photoId = page.photoId, imprintId = page.imprintId,
                        itemId = page.itemId, layoutSlot = page.layoutSlot
                    });
            festivalDecorations = new List<FestivalDecorationRecord>();
            if (source.festivalDecorations != null)
                foreach (FestivalDecorationRecord decoration in source.festivalDecorations)
                    if (decoration != null) festivalDecorations.Add(new FestivalDecorationRecord
                    {
                        stationId = decoration.stationId, decorationId = decoration.decorationId, itemId = decoration.itemId
                    });
            lastWorldId = source.lastWorldId;
            lastWorldX = source.lastWorldX;
            lastWorldY = source.lastWorldY;
        }

    }

    [Serializable]
    public sealed class MemoryPhotoRecord
    {
        public string photoId = "";
        public string worldId = "";
        public string locationId = "";
        public string capturedAtUtc = "";
    }

    [Serializable]
    public sealed class JourneyEventRecord
    {
        public string eventId = "";
        public string worldId = "";
        public string createdAtUtc = "";
    }

    [Serializable]
    public sealed class JournalPageState
    {
        public string pageId = "";
        public string photoId = "";
        public string imprintId = "";
        public string itemId = "";
        public int layoutSlot;
    }

    [Serializable]
    public sealed class FestivalDecorationRecord
    {
        public string stationId = "";
        public string decorationId = "";
        public string itemId = "";
    }

    public static class PlayerProfileMigration
    {
        private const int MaxStoryMarks = 128;
        private const int MaxStoryMarkLength = 64;

        public static void Normalize(PlayerProfile profile)
        {
            if (profile == null) return;

            if (profile.schemaVersion == 1)
            {
                profile.chapterIndex = 1;
                profile.mainStoryCompleted = false;
                profile.storyFlags = new List<string>();
                profile.worldImprints = new List<string>();
                profile.lastWorldId = "hoian";
                profile.lastWorldX = -5.1f;
                profile.lastWorldY = -1.8f;
            }

            if (profile.storyFlags == null) profile.storyFlags = new List<string>();
            if (profile.worldImprints == null) profile.worldImprints = new List<string>();
            if (profile.inventoryItems == null) profile.inventoryItems = new List<string>();
            if (profile.culturalCards == null) profile.culturalCards = new List<string>();
            if (profile.memoryPhotos == null) profile.memoryPhotos = new List<MemoryPhotoRecord>();
            if (profile.journeyEvents == null) profile.journeyEvents = new List<JourneyEventRecord>();
            if (profile.journalPages == null) profile.journalPages = new List<JournalPageState>();
            if (profile.festivalDecorations == null) profile.festivalDecorations = new List<FestivalDecorationRecord>();
            profile.storyFlags = NormalizeIds(profile.storyFlags);
            profile.worldImprints = NormalizeIds(profile.worldImprints);
            profile.inventoryItems = NormalizeIds(profile.inventoryItems);
            profile.culturalCards = NormalizeIds(profile.culturalCards);
            NormalizePhotos(profile);
            NormalizeEvents(profile);
            NormalizeJournalPages(profile);
            NormalizeFestivalDecorations(profile);
            profile.chapterIndex = profile.mainStoryCompleted ? 4 : Math.Max(1, Math.Min(4, profile.chapterIndex));
            profile.lastWorldId = profile.lastWorldId == "jeonju" ? "jeonju" : "hoian";
            if (!IsFinite(profile.lastWorldX)) profile.lastWorldX = -5.1f;
            if (!IsFinite(profile.lastWorldY)) profile.lastWorldY = -1.8f;
            profile.lastWorldX = Math.Max(-12f, Math.Min(12f, profile.lastWorldX));
            profile.lastWorldY = Math.Max(-5f, Math.Min(5f, profile.lastWorldY));
            profile.schemaVersion = PlayerProfile.CurrentSchemaVersion;
        }

        private static void NormalizePhotos(PlayerProfile profile)
        {
            var normalized = new List<MemoryPhotoRecord>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (MemoryPhotoRecord photo in profile.memoryPhotos)
            {
                if (photo == null || string.IsNullOrWhiteSpace(photo.photoId) || normalized.Count >= 48) continue;
                string id = photo.photoId.Trim();
                if (id.Length > 64 || !seen.Add(id)) continue;
                normalized.Add(new MemoryPhotoRecord
                {
                    photoId = id,
                    worldId = photo.worldId == "jeonju" ? "jeonju" : "hoian",
                    locationId = SafeId(photo.locationId),
                    capturedAtUtc = SafeTimestamp(photo.capturedAtUtc)
                });
            }
            profile.memoryPhotos = normalized;
        }

        private static void NormalizeEvents(PlayerProfile profile)
        {
            var normalized = new List<JourneyEventRecord>();
            foreach (JourneyEventRecord entry in profile.journeyEvents)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.eventId) || normalized.Count >= 128) continue;
                normalized.Add(new JourneyEventRecord
                {
                    eventId = SafeId(entry.eventId),
                    worldId = entry.worldId == "jeonju" ? "jeonju" : entry.worldId == "hoian" ? "hoian" : "shared",
                    createdAtUtc = SafeTimestamp(entry.createdAtUtc)
                });
            }
            profile.journeyEvents = normalized;
        }

        private static void NormalizeJournalPages(PlayerProfile profile)
        {
            var normalized = new List<JournalPageState>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (JournalPageState page in profile.journalPages)
            {
                if (page == null || normalized.Count >= 3) continue;
                string id = page.pageId == "hoian" || page.pageId == "jeonju" || page.pageId == "shared" ? page.pageId : "";
                if (id.Length == 0 || !seen.Add(id)) continue;
                normalized.Add(new JournalPageState
                {
                    pageId = id,
                    photoId = SafeId(page.photoId),
                    imprintId = SafeId(page.imprintId),
                    itemId = SafeId(page.itemId),
                    layoutSlot = Math.Max(0, Math.Min(2, page.layoutSlot))
                });
            }
            profile.journalPages = normalized;
        }

        private static void NormalizeFestivalDecorations(PlayerProfile profile)
        {
            var normalized = new List<FestivalDecorationRecord>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (FestivalDecorationRecord decoration in profile.festivalDecorations)
            {
                if (decoration == null || normalized.Count >= 7) continue;
                string station = SafeId(decoration.stationId);
                string kind = SafeId(decoration.decorationId);
                string item = SafeId(decoration.itemId);
                if (station.Length == 0 || kind.Length == 0 || item.Length == 0 || !seen.Add(station)) continue;
                normalized.Add(new FestivalDecorationRecord { stationId = station, decorationId = kind, itemId = item });
            }
            profile.festivalDecorations = normalized;
        }

        private static string SafeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            string id = value.Trim();
            return id.Length <= MaxStoryMarkLength ? id : "";
        }

        private static string SafeTimestamp(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            string timestamp = value.Trim();
            return timestamp.Length <= 40 ? timestamp : "";
        }

        private static List<string> NormalizeIds(List<string> values)
        {
            var normalized = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string value in values)
            {
                if (normalized.Count >= MaxStoryMarks) break;
                if (string.IsNullOrWhiteSpace(value)) continue;
                string id = value.Trim();
                if (id.Length > MaxStoryMarkLength || !seen.Add(id)) continue;
                normalized.Add(id);
            }
            return normalized;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
