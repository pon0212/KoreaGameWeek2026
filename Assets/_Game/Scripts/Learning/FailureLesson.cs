namespace TastePassport.Learning
{
    /// <summary>
    /// A short bilingual lesson shown after a failed activity. TargetWord and
    /// ExampleSentence use the language the player is learning; SourceWord is
    /// their current UI-language translation.
    /// </summary>
    public sealed class FailureLesson
    {
        public string SourceWord { get; }
        public string TargetWord { get; }
        public string Pronunciation { get; }
        public string MeaningKey { get; }
        public string ExampleSentence { get; }
        public string ExampleTranslationKey { get; }

        public FailureLesson(string sourceWord, string targetWord, string pronunciation,
            string meaningKey, string exampleSentence, string exampleTranslationKey)
        {
            SourceWord = sourceWord;
            TargetWord = targetWord;
            Pronunciation = pronunciation;
            MeaningKey = meaningKey;
            ExampleSentence = exampleSentence;
            ExampleTranslationKey = exampleTranslationKey;
        }
    }
}
