using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace TastePassport.Save
{
    public sealed class ProfileStore
    {
        private readonly string path;
        public string FilePath => path;

        public ProfileStore(string path = null)
        {
            this.path = path ?? Path.Combine(Application.persistentDataPath, "taste_passport_profile.json");
        }

        public PlayerProfile Load()
        {
            var current = Read(path);
            if (current != null) return current;
            var backup = Read(path + ".bak");
            return backup ?? new PlayerProfile();
        }

        private static PlayerProfile Read(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                var profile = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(file));
                if (profile == null || profile.schemaVersion != 1) throw new InvalidDataException("Unsupported profile version");
                profile.language = profile.language == "ko" ? "ko" : "vi";
                profile.playerName = profile.playerName == null ? "" : profile.playerName.Trim();
                profile.avatarIndex = Mathf.Clamp(profile.avatarIndex, 0, 2);
                if (profile.created && profile.playerName.Length == 0) profile.created = false;
                if (profile.levels == null) profile.levels = new List<LevelProgress>();
                if (profile.ownedOutfits == null) profile.ownedOutfits = new List<int> { 0, 1 };
                if (profile.ownedOutfits.Count == 0) profile.ownedOutfits.AddRange(new[] { 0, 1 });
                profile.coins = Math.Max(0, profile.coins);
                profile.outfitId = profile.ownedOutfits.Contains(profile.outfitId) ? profile.outfitId : 0;
                return profile;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Taste Passport: Could not read profile " + file + ": " + error.Message);
                return null;
            }
        }

        public bool TrySave(PlayerProfile profile)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(profile, true));
                // Never replace a valid backup with a damaged primary save.
                if (Read(temporary) == null) throw new InvalidDataException("Temporary profile is invalid");
                if (Read(path) != null) File.Copy(path, path + ".bak", true);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temporary, path);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError("Taste Passport: Could not save profile: " + error.Message);
                return false;
            }
        }
    }
}
