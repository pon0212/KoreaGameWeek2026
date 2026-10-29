using System;

namespace TastePassport.Save
{
    [Serializable]
    public sealed class LevelProgress
    {
        public string levelId;
        public int bestScore;
        public int bestStars;
        public int playCount;
    }
}
