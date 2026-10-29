using System;

namespace TastePassport.Gameplay
{
    public enum MatchResult { Correct, Wrong, Complete }

    public sealed class MatchRun
    {
        private readonly int[] questionOrder;
        private readonly int[] answerOrder;
        public MatchDefinition Definition { get; }
        public int StepIndex { get; private set; }
        public int Mistakes { get; private set; }
        public bool Complete => StepIndex == questionOrder.Length;
        public string CurrentRequestKey => Definition.RequestKeys[questionOrder[StepIndex]];
        public int AnswerCount => answerOrder.Length;
        public int Score => Math.Max(100, 300 - Mistakes * 30);
        public int Stars => Mistakes == 0 ? 3 : Mistakes <= 2 ? 2 : 1;

        public MatchRun(MatchDefinition definition, int seed)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (definition.RequestKeys == null || definition.AnswerKeys == null || definition.RequestKeys.Length != definition.AnswerKeys.Length || definition.RequestKeys.Length < 2)
                throw new ArgumentException("Match level needs equal request and answer counts.", nameof(definition));
            questionOrder = new int[definition.RequestKeys.Length];
            answerOrder = new int[definition.AnswerKeys.Length];
            for (int i = 0; i < questionOrder.Length; i++) questionOrder[i] = answerOrder[i] = i;
            var random = new Random(seed);
            Shuffle(questionOrder, random);
            Shuffle(answerOrder, random);
        }

        private static void Shuffle(int[] values, Random random)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int swap = values[i]; values[i] = values[j]; values[j] = swap;
            }
        }

        public string AnswerKey(int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex >= answerOrder.Length) throw new ArgumentOutOfRangeException(nameof(choiceIndex));
            return Definition.AnswerKeys[answerOrder[choiceIndex]];
        }

        public MatchResult Choose(int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex >= answerOrder.Length) throw new ArgumentOutOfRangeException(nameof(choiceIndex));
            if (Complete) return MatchResult.Complete;
            if (answerOrder[choiceIndex] != questionOrder[StepIndex]) { Mistakes++; return MatchResult.Wrong; }
            StepIndex++;
            return Complete ? MatchResult.Complete : MatchResult.Correct;
        }
    }
}
