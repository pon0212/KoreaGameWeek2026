using System;
using TastePassport.Localization;

namespace TastePassport.Learning
{
    /// <summary>Provides compact VI/KO vocabulary related to the activity that was retried.</summary>
    public static class LearningService
    {
        private sealed class LessonPair
        {
            public readonly string Id;
            public readonly string Vietnamese;
            public readonly string Korean;
            public readonly string KoreanPronunciation;
            public readonly string VietnamesePronunciation;
            public readonly string MeaningKey;
            public readonly string KoreanExample;
            public readonly string VietnameseExampleKey;
            public readonly string ExampleTranslationKey;

            public LessonPair(string id, string vietnamese, string korean, string koreanPronunciation,
                string vietnamesePronunciation, string meaningKey, string koreanExample,
                string vietnameseExampleKey, string exampleTranslationKey)
            {
                Id = id;
                Vietnamese = vietnamese;
                Korean = korean;
                KoreanPronunciation = koreanPronunciation;
                VietnamesePronunciation = vietnamesePronunciation;
                MeaningKey = meaningKey;
                KoreanExample = koreanExample;
                VietnameseExampleKey = vietnameseExampleKey;
                ExampleTranslationKey = exampleTranslationKey;
            }
        }

        private static readonly LessonPair[] Lessons =
        {
            new LessonPair("hoian-architecture", "Chùa Cầu", "쭈어꺼우", "jjueo-keu-o", "chùa cầu",
                "lesson_meaning_hoian_architecture", "쭈어꺼우를 찾아보세요.", "lesson_example_hoian_architecture", "lesson_example_translation_hoian_architecture"),
            new LessonPair("jeonju-architecture", "mái Hanok", "한옥 지붕", "hanok jibung", "mái ha-nốc",
                "lesson_meaning_jeonju_architecture", "한옥의 지붕을 살펴보세요.", "lesson_example_jeonju_architecture", "lesson_example_translation_jeonju_architecture"),
            new LessonPair("hoian-cao-lau", "Cao Lầu", "까오러우", "ka-o-reo-u", "cao lầu",
                "lesson_meaning_cao_lau", "까오러우를 맛보고 싶어요.", "lesson_example_cao_lau", "lesson_example_translation_cao_lau"),
            new LessonPair("hoian_caolau", "Cao Lầu", "까오러우", "ka-o-reo-u", "cao lầu",
                "lesson_meaning_cao_lau", "까오러우를 맛보고 싶어요.", "lesson_example_cao_lau", "lesson_example_translation_cao_lau"),
            new LessonPair("jeonju-bibimbap", "Bibimbap", "비빔밥", "bi-bim-bap", "bi-bim-bap",
                "lesson_meaning_bibimbap", "비빔밥을 함께 먹어요.", "lesson_example_bibimbap", "lesson_example_translation_bibimbap"),
            new LessonPair("jeonju_bibimbap", "Bibimbap", "비빔밥", "bi-bim-bap", "bi-bim-bap",
                "lesson_meaning_bibimbap", "비빔밥을 함께 먹어요.", "lesson_example_bibimbap", "lesson_example_translation_bibimbap"),
            new LessonPair("hoian-culture", "Xin chào", "안녕하세요", "an-nyeong-ha-se-yo", "xin chào",
                "lesson_meaning_hoian_exchange", "안녕하세요, 반가워요.", "lesson_example_hoian_exchange", "lesson_example_translation_hoian_exchange"),
            new LessonPair("jeonju-culture", "Cảm ơn", "감사합니다", "gam-sa-ham-ni-da", "cảm ơn",
                "lesson_meaning_jeonju_exchange", "도와주셔서 감사합니다.", "lesson_example_jeonju_exchange", "lesson_example_translation_jeonju_exchange")
        };

        /// <summary>Returns null for unknown activities so the UI can safely omit the lesson card.</summary>
        public static FailureLesson GetFailureLesson(string activityId, string playerLanguage, int failureContext = 0)
        {
            if (string.IsNullOrWhiteSpace(activityId)) return null;
            string id = activityId.Trim();
            LessonPair pair = null;
            for (int i = 0; i < Lessons.Length; i++)
            {
                if (string.Equals(Lessons[i].Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    pair = Lessons[i];
                    break;
                }
            }
            if (pair == null) return null;

            bool playerUsesKorean = string.Equals(playerLanguage, "ko", StringComparison.OrdinalIgnoreCase);
            return playerUsesKorean
                ? new FailureLesson(pair.Korean, pair.Vietnamese, pair.VietnamesePronunciation, pair.MeaningKey,
                    GameText.Get("ko", pair.VietnameseExampleKey), pair.ExampleTranslationKey)
                : new FailureLesson(pair.Vietnamese, pair.Korean, pair.KoreanPronunciation, pair.MeaningKey,
                    pair.KoreanExample, pair.ExampleTranslationKey);
        }
    }
}
