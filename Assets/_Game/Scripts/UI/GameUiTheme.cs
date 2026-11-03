using UnityEngine;

namespace TastePassport.UI
{
    public static class GameUiTheme
    {
        public static readonly Color Cream = Hex("#F6E9D4");
        public static readonly Color Paper = Hex("#FFF9EE");
        public static readonly Color Ink = Hex("#352820");
        public static readonly Color Jade = Hex("#357E84");
        public static readonly Color Lantern = Hex("#E6A14D");
        public static readonly Color Vermilion = Hex("#A94A42");
        public static readonly Color Border = Hex("#D9C6A7");
        public static readonly Color Muted = Hex("#51463D");

        // Minimum control size in the 720 x 1280 reference space.
        public const float MinimumTapTarget = 64f;

        private static Color Hex(string code)
        {
            ColorUtility.TryParseHtmlString(code, out var result);
            return result;
        }
    }
}
