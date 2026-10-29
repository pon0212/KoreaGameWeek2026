using System;

namespace TastePassport.Gameplay
{
    public enum InspectResult { Fragment, Empty, AlreadyInspected }

    public sealed class ExplorationRun
    {
        private readonly bool[] fragments;
        private readonly bool[] inspected;
        public LevelDefinition Level { get; }
        public int FoundCount { get; private set; }
        public int MissCount { get; private set; }
        public bool SearchComplete => FoundCount == 3;
        public int Score => Math.Max(100, 300 - MissCount * 25);
        public int Stars => MissCount == 0 ? 3 : MissCount <= 2 ? 2 : 1;

        public ExplorationRun(LevelDefinition level, int seed)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            if (level.ObjectKeys == null || level.ObjectKeys.Length < 3)
                throw new ArgumentException("An exploration level needs at least three objects.", nameof(level));
            fragments = new bool[level.ObjectKeys.Length];
            inspected = new bool[level.ObjectKeys.Length];
            var indices = new int[fragments.Length];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            var random = new Random(seed);
            for (int i = indices.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int swap = indices[i]; indices[i] = indices[j]; indices[j] = swap;
            }
            for (int i = 0; i < 3; i++) fragments[indices[i]] = true;
        }

        public bool IsInspected(int index) => index >= 0 && index < inspected.Length && inspected[index];

        public InspectResult Inspect(int index)
        {
            if (index < 0 || index >= inspected.Length) throw new ArgumentOutOfRangeException(nameof(index));
            if (inspected[index]) return InspectResult.AlreadyInspected;
            inspected[index] = true;
            if (fragments[index]) { FoundCount++; return InspectResult.Fragment; }
            MissCount++;
            return InspectResult.Empty;
        }
    }
}
