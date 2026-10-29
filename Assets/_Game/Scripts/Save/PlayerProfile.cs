using System;
using System.Collections.Generic;

namespace TastePassport.Save
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public int schemaVersion = 1;
        public string playerName = "";
        public string language = "vi";
        public int avatarIndex;
        public int coins;
        public int outfitId;
        public List<int> ownedOutfits = new List<int> { 0, 1 };
        public bool created;
        public List<LevelProgress> levels = new List<LevelProgress>();
    }
}
