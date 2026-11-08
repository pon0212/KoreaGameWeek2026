using UnityEngine;

namespace TastePassport.UI
{
    /// <summary>
    /// Unified Design System for Dấu Xứ Việt Hàn – Tales of Two Lands.
    /// Premium Cozy Storybook Adventure aesthetic inspired by Hội An & Jeonju.
    /// </summary>
    public static class GameUiTheme
    {
        // Core Cultural Palette
        public static readonly Color Cream = Hex("#FBF4E8");
        public static readonly Color Paper = Hex("#FFFDF8");
        public static readonly Color Ink = Hex("#2D211A");
        public static readonly Color Jade = Hex("#2D6E78");
        public static readonly Color Lantern = Hex("#E89F3C");
        public static readonly Color Vermilion = Hex("#B84B3D");
        public static readonly Color Border = Hex("#CFBC9E");
        public static readonly Color Muted = Hex("#594B40");

        // Rich Accent Palette
        public static readonly Color GoldGlow = Hex("#FCE6A4");
        public static readonly Color Terracotta = Hex("#B8533D");
        public static readonly Color SoftSage = Hex("#6F8B75");
        public static readonly Color HanjiGauze = Hex("#EFE5D3");
        public static readonly Color HeaderWood = Hex("#3D2D24");
        public static readonly Color DeepTeal = Hex("#23545D");

        // Semantic Roles
        public static readonly Color Surface = Paper;
        public static readonly Color Primary = Jade;
        public static readonly Color Reward = Vermilion;
        public static readonly Color Secondary = Cream;
        public static readonly Color TextPrimary = Ink;
        public static readonly Color TextSecondary = Muted;
        public static readonly Color OnPrimary = Color.white;
        public static readonly Color Disabled = new Color(.78f, .76f, .72f, 1f);
        public static readonly Color PaperShadow = new Color(0.18f, 0.13f, 0.10f, 0.14f);
        public static readonly Color HeavyShadow = new Color(0.14f, 0.10f, 0.08f, 0.32f);

        // Responsive Reference Coordinates
        public static readonly Vector2 ReferenceResolution = new Vector2(720f, 1280f);

        // Spacing & Padding
        public const int SpaceMicro = 4;
        public const int SpaceSmall = 8;
        public const int SpaceMedium = 16;
        public const int SpaceLarge = 24;
        public const int SpaceXLarge = 32;
        public const int CardPaddingVertical = 20;

        // Typography Hierarchy
        public const int FontSizeHero = 42;
        public const int FontSizeTitle = 34;
        public const int FontSizeHeading = 28;
        public const int FontSizeBody = 24;
        public const int FontSizeCaption = 20;
        public const int FontSizeSmall = 18;
        public const int MinimumBodyFontSize = 24;

        // Component Metrics
        public const float ButtonHeightHero = 92f;
        public const float ButtonHeight = 84f;
        public const float ButtonHeightCompact = 70f;
        public const float ButtonHeightIcon = 64f;
        public const float MinimumTapTarget = 64f;

        // Motion & Interaction Speeds
        public const float PressedVisualScale = .96f;
        public const float HighlightedVisualScale = 1.018f;
        public const float ButtonMotionSeconds = .09f;

        public static Color Hex(string code)
        {
            ColorUtility.TryParseHtmlString(code, out var result);
            return result;
        }
    }
}
