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

        // Semantic roles shared by screen and world builders.
        public static readonly Color Surface = Paper;
        public static readonly Color Primary = Jade;
        public static readonly Color Reward = Vermilion;
        public static readonly Color Secondary = Cream;
        public static readonly Color TextPrimary = Ink;
        public static readonly Color TextSecondary = Muted;
        public static readonly Color OnPrimary = Color.white;
        public static readonly Color Disabled = new Color(.78f, .76f, .72f, 1f);
        public static readonly Color PaperShadow = new Color(Ink.r, Ink.g, Ink.b, .12f);
        public static readonly Vector2 ReferenceResolution = new Vector2(720f, 1280f);
        public const int SpaceSmall = 6;
        public const int SpaceMedium = 12;
        public const int SpaceLarge = 24;
        public const int CardPaddingVertical = 18;
        public const int MinimumBodyFontSize = 26;
        public const float ButtonHeight = 88f;
        public const float PressedVisualScale = .97f;
        public const float HighlightedVisualScale = 1.015f;
        public const float ButtonMotionSeconds = .1f;

        // Minimum control size in the 720 x 1280 reference space.
        public const float MinimumTapTarget = 64f;

        private static Color Hex(string code)
        {
            ColorUtility.TryParseHtmlString(code, out var result);
            return result;
        }
    }
}
