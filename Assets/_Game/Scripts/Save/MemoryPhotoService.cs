using System;
using System.IO;
using TastePassport.Story;
using UnityEngine;

namespace TastePassport.Save
{
    /// <summary>Stores genuine game-camera captures as bounded local JPEG files referenced by the save.</summary>
    public static class MemoryPhotoService
    {
        public const int MaximumPhotos = 48;
        private const int MaximumPhotoBytes = 4 * 1024 * 1024;
        private const string FolderName = "TastePassportMemories";

        public static bool TryStoreJpeg(PlayerProfile profile, ProfileStore store, string worldId,
            string locationId, byte[] jpegBytes, out string photoId)
        {
            photoId = null;
            if (profile == null || store == null || (worldId != "hoian" && worldId != "jeonju") ||
                string.IsNullOrWhiteSpace(locationId) || locationId.Length > 64 || !IsJpeg(jpegBytes) ||
                profile.memoryPhotos == null || profile.memoryPhotos.Count >= MaximumPhotos)
                return false;

            string id = Guid.NewGuid().ToString("N");
            string directory = Path.Combine(Application.persistentDataPath, FolderName);
            string destination = Path.Combine(directory, id + ".jpg");
            string temporary = destination + ".tmp";
            var candidate = new PlayerProfile();
            candidate.CopyFrom(profile);
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(temporary, jpegBytes);
                if (!IsJpeg(File.ReadAllBytes(temporary))) throw new InvalidDataException("Captured image is not a JPEG.");
                File.Move(temporary, destination);
                candidate.memoryPhotos.Add(new MemoryPhotoRecord
                {
                    photoId = id,
                    worldId = worldId,
                    locationId = locationId.Trim(),
                    capturedAtUtc = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture)
                });
                candidate.journeyEvents.Add(new JourneyEventRecord
                {
                    eventId = "photo-captured",
                    worldId = worldId,
                    createdAtUtc = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture)
                });
                if (!store.TrySave(candidate))
                {
                    DeleteIfExists(destination);
                    return false;
                }
                profile.CopyFrom(candidate);
                photoId = id;
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Taste Passport: Could not store memory photo: " + error.Message);
                DeleteIfExists(temporary);
                DeleteIfExists(destination);
                return false;
            }
        }

        public static Texture2D LoadTexture(MemoryPhotoRecord photo)
        {
            if (photo == null || string.IsNullOrWhiteSpace(photo.photoId) || photo.photoId.Length > 64) return null;
            string path = Path.Combine(Application.persistentDataPath, FolderName, photo.photoId + ".jpg");
            try
            {
                if (!File.Exists(path)) return null;
                var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (texture.LoadImage(File.ReadAllBytes(path), true)) return texture;
                UnityEngine.Object.Destroy(texture);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Taste Passport: Could not load memory photo: " + error.Message);
            }
            return null;
        }

        public static bool Exists(MemoryPhotoRecord photo)
        {
            if (photo == null || string.IsNullOrWhiteSpace(photo.photoId) || photo.photoId.Length > 64) return false;
            return File.Exists(Path.Combine(Application.persistentDataPath, FolderName, photo.photoId + ".jpg"));
        }

        public static bool DeletePhotoFile(string photoId)
        {
            if (string.IsNullOrWhiteSpace(photoId) || photoId.Length != 32) return false;
            string path = Path.Combine(Application.persistentDataPath, FolderName, photoId + ".jpg");
            if (!File.Exists(path)) return false;
            try { File.Delete(path); return true; }
            catch (Exception error)
            {
                Debug.LogWarning("Taste Passport: Could not remove memory photo: " + error.Message);
                return false;
            }
        }

        private static bool IsJpeg(byte[] bytes) => bytes != null && bytes.Length >= 4 && bytes.Length <= MaximumPhotoBytes &&
            bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[bytes.Length - 2] == 0xff && bytes[bytes.Length - 1] == 0xd9;

        private static void DeleteIfExists(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception error) { Debug.LogWarning("Taste Passport: Could not clean up photo file: " + error.Message); }
        }
    }

    public enum JournalSaveResult
    {
        Saved,
        InvalidPage,
        MissingPhoto,
        MissingImprint,
        UnownedItem,
        InvalidLayout,
        SaveFailed
    }

    public static class JournalService
    {
        public static JournalPageState GetPage(PlayerProfile profile, string pageId) =>
            profile?.journalPages?.Find(page => page != null && page.pageId == pageId);

        public static JournalSaveResult TrySavePage(PlayerProfile profile, ProfileStore store, string pageId,
            string photoId, string imprintId, string itemId, int layoutSlot)
        {
            if (profile == null || store == null || (pageId != "hoian" && pageId != "jeonju" && pageId != "shared"))
                return JournalSaveResult.InvalidPage;
            MemoryPhotoRecord chosenPhoto = profile.memoryPhotos?.Find(photo => photo != null && photo.photoId == photoId);
            if (chosenPhoto == null || !MemoryPhotoService.Exists(chosenPhoto)) return JournalSaveResult.MissingPhoto;
            if ((pageId == "hoian" && chosenPhoto.worldId != "hoian") ||
                (pageId == "jeonju" && chosenPhoto.worldId != "jeonju")) return JournalSaveResult.InvalidPage;
            if (!string.IsNullOrEmpty(imprintId) && !StoryProgressService.HasImprint(profile, imprintId))
                return JournalSaveResult.MissingImprint;
            if (!string.IsNullOrEmpty(itemId) && !InventoryService.HasItem(profile, itemId))
                return JournalSaveResult.UnownedItem;
            if (layoutSlot < 0 || layoutSlot > 2) return JournalSaveResult.InvalidLayout;

            var candidate = new PlayerProfile();
            candidate.CopyFrom(profile);
            if (candidate.journalPages == null) candidate.journalPages = new System.Collections.Generic.List<JournalPageState>();
            JournalPageState page = GetPage(candidate, pageId);
            if (page == null)
            {
                page = new JournalPageState { pageId = pageId };
                candidate.journalPages.Add(page);
            }
            page.photoId = photoId;
            page.imprintId = imprintId ?? "";
            page.itemId = itemId ?? "";
            page.layoutSlot = layoutSlot;
            if (!store.TrySave(candidate)) return JournalSaveResult.SaveFailed;
            profile.CopyFrom(candidate);
            return JournalSaveResult.Saved;
        }
    }
}
