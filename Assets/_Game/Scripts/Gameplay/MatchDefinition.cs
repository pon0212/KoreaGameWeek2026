namespace TastePassport.Gameplay
{
    public sealed class MatchDefinition
    {
        public readonly string Id;
        public readonly string PlaceKey;
        public readonly string TitleKey;
        public readonly string GuideKey;
        public readonly string[] RequestKeys;
        public readonly string[] AnswerKeys;

        public MatchDefinition(string id, string placeKey, string titleKey, string guideKey, string[] requestKeys, string[] answerKeys)
        {
            Id = id;
            PlaceKey = placeKey;
            TitleKey = titleKey;
            GuideKey = guideKey;
            RequestKeys = requestKeys;
            AnswerKeys = answerKeys;
        }
    }

    public static class MatchCatalog
    {
        public static readonly MatchDefinition HoiAn = new MatchDefinition(
            "hoian-culture", "hoian", "hoian_exchange", "hoian_exchange_intro",
            new[] { "request_lantern", "request_cao_lau", "request_bridge" },
            new[] { "answer_old_town", "cao_lau", "answer_japanese_bridge" });

        public static readonly MatchDefinition Jeonju = new MatchDefinition(
            "jeonju-culture", "jeonju", "jeonju_exchange", "jeonju_exchange_intro",
            new[] { "request_hanok", "request_bibimbap", "request_hanbok" },
            new[] { "answer_hanok_village", "bibimbap", "answer_hanbok" });
    }
}
