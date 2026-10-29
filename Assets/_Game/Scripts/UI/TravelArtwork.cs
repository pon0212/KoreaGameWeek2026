using UnityEngine;
using UnityEngine.UI;

namespace TastePassport.UI
{
    public enum TravelArtworkKind { HoiAn, Jeonju, VietnamJourney, KoreaJourney, CaoLau, Bibimbap, Passport, Friendship, VietnamMap, KoreaMap }

    // Original, resolution-independent illustrations built from UI shapes.
    // All children ignore raycasts so artwork never blocks the game controls.
    public static class TravelArtwork
    {
        private static Sprite disc;
        private static readonly Color Ink = ColorOf("#43372F");
        private static readonly Color Wood = ColorOf("#704936");
        private static readonly Color Tile = ColorOf("#7C4139");
        private static readonly Color Gold = ColorOf("#E6A14D");
        private static readonly Color Jade = ColorOf("#357E84");

        public static void Draw(Transform parent, TravelArtworkKind kind)
        {
            if (kind == TravelArtworkKind.HoiAn || kind == TravelArtworkKind.Jeonju || kind == TravelArtworkKind.VietnamJourney || kind == TravelArtworkKind.KoreaJourney)
            {
                string path = kind == TravelArtworkKind.HoiAn ? "Art/HoiAn" : kind == TravelArtworkKind.Jeonju ? "Art/Jeonju" : kind == TravelArtworkKind.VietnamJourney ? "Art/VietnamJourney" : "Art/KoreaJourney";
                Sprite painting = Resources.Load<Sprite>(path);
                if (painting != null)
                {
                    var frame = parent.gameObject.AddComponent<RectMask2D>();
                    frame.padding = Vector4.zero;
                    var picture = Box(parent, "Original location painting", Color.white, 0, 0, 1, 1).GetComponent<Image>();
                    picture.sprite = painting;
                    picture.type = Image.Type.Simple;
                    var fit = picture.gameObject.AddComponent<AspectRatioFitter>();
                    fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    fit.aspectRatio = painting.rect.width / painting.rect.height;
                    return;
                }
            }
            switch (kind)
            {
                case TravelArtworkKind.HoiAn: DrawHoiAn(parent); break;
                case TravelArtworkKind.Jeonju: DrawJeonju(parent); break;
                case TravelArtworkKind.CaoLau: DrawBowl(parent, false); break;
                case TravelArtworkKind.Bibimbap: DrawBowl(parent, true); break;
                case TravelArtworkKind.VietnamMap: DrawCountryMap(parent, false); break;
                case TravelArtworkKind.KoreaMap: DrawCountryMap(parent, true); break;
                case TravelArtworkKind.Passport: DrawPassport(parent); break;
                default: DrawFriendship(parent); break;
            }
        }

        private static void DrawHoiAn(Transform p)
        {
            Box(p, "Warm sky", ColorOf("#F8D6A4"), 0, 0, 1, 1);
            Box(p, "Distant river", ColorOf("#87B5AF"), 0, 0, 1, .19f);
            Box(p, "Quay", ColorOf("#CC9A68"), 0, .17f, 1, .23f);
            Box(p, "Left house", ColorOf("#E5AA5C"), .03f, .22f, .33f, .78f);
            Box(p, "Middle house", ColorOf("#F1C877"), .34f, .22f, .7f, .9f);
            Box(p, "Right house", ColorOf("#E8B966"), .71f, .22f, .98f, .72f);
            Box(p, "Left roof", Tile, .015f, .75f, .35f, .83f);
            Box(p, "Middle roof", Tile, .32f, .87f, .72f, .95f);
            Box(p, "Right roof", Tile, .69f, .69f, .995f, .77f);
            for (int i = 0; i < 3; i++)
            {
                float x = .09f + i * .35f;
                Box(p, "Wooden door", Wood, x, .23f, x + .1f, .49f);
                Box(p, "Door panel", ColorOf("#AF7447"), x + .015f, .24f, x + .085f, .46f);
            }
            Box(p, "Gold cord", Ink, 0, .82f, 1, .83f);
            for (int i = 0; i < 5; i++)
            {
                float x = .12f + .19f * i;
                Box(p, "Gold string", Ink, x, .79f, x + .004f, .83f);
                Circle(p, "Gold", i % 2 == 0 ? ColorOf("#B54F41") : Gold, x - .028f, .68f, x + .032f, .79f);
            }
        }

        private static void DrawJeonju(Transform p)
        {
            Box(p, "Hanji sky", ColorOf("#DDEBE7"), 0, 0, 1, 1);
            Circle(p, "Soft sun", ColorOf("#F6E5BB"), .76f, .69f, .92f, .93f);
            Box(p, "Ground", ColorOf("#B7BDAA"), 0, 0, 1, .25f);
            Box(p, "Hanok wall", ColorOf("#F5E8CD"), .11f, .24f, .88f, .66f);
            Box(p, "Foundation", Wood, .07f, .21f, .92f, .27f);
            Box(p, "Roof underside", Wood, .06f, .63f, .94f, .72f);
            Box(p, "Tiled roof", ColorOf("#464C4B"), .14f, .72f, .86f, .84f);
            Box(p, "Left eave", ColorOf("#464C4B"), .02f, .69f, .19f, .76f);
            Box(p, "Right eave", ColorOf("#464C4B"), .81f, .69f, .98f, .76f);
            Box(p, "Roof ridge", ColorOf("#626866"), .3f, .82f, .7f, .86f);
            for (int i = 0; i < 4; i++)
            {
                float x = .19f + i * .16f;
                Box(p, "Timber post", Wood, x, .27f, x + .025f, .66f);
                Box(p, "Paper screen", ColorOf("#E9DAC0"), x + .03f, .32f, x + .15f, .61f);
                Box(p, "Screen crossbar", Wood, x + .03f, .45f, x + .15f, .465f);
            }
            Box(p, "Threshold", Wood, .11f, .28f, .88f, .3f);
        }

        private static void DrawBowl(Transform p, bool bibimbap)
        {
            Box(p, "Table", bibimbap ? ColorOf("#D8E6DD") : ColorOf("#F4D5A8"), 0, 0, 1, 1);
            Circle(p, "Plate shadow", ColorOf("#BFAE92"), .22f, .03f, .8f, .92f);
            Circle(p, "Ceramic rim", bibimbap ? ColorOf("#495D58") : ColorOf("#E9E3D3"), .2f, .08f, .78f, .96f);
            Circle(p, "Bowl center", bibimbap ? ColorOf("#F5EEE0") : ColorOf("#8D6B47"), .25f, .15f, .73f, .89f);
            Circle(p, "Greens", ColorOf("#6E9B63"), .32f, .5f, .47f, .76f);
            Circle(p, "Carrot", ColorOf("#D98145"), .48f, .54f, .64f, .76f);
            Circle(p, "Rice or noodles", ColorOf("#F6E4B9"), .37f, .24f, .62f, .56f);
            Circle(p, bibimbap ? "Egg" : "Pork", bibimbap ? ColorOf("#F5C849") : ColorOf("#B87758"), .48f, .37f, .65f, .6f);
            Box(p, "Chopsticks", Wood, .72f, .28f, .74f, .91f);
            Box(p, "Second chopstick", Wood, .77f, .25f, .79f, .88f);
        }

        private static void DrawPassport(Transform p)
        {
            Box(p, "Cover", Jade, 0, 0, 1, 1);
            Box(p, "Spine", ColorOf("#255E63"), .04f, .04f, .11f, .96f);
            Box(p, "Cover frame", Gold, .17f, .13f, .83f, .15f);
            Box(p, "Cover top", Gold, .17f, .83f, .83f, .85f);
            Circle(p, "Seal", Gold, .37f, .37f, .63f, .71f);
            Circle(p, "Seal center", Jade, .42f, .43f, .58f, .65f);
            Box(p, "Journey line", Gold, .23f, .24f, .76f, .26f);
            Circle(p, "Journey point one", Gold, .23f, .21f, .29f, .29f);
            Circle(p, "Journey point two", Gold, .7f, .21f, .76f, .29f);
        }

        private static void DrawFriendship(Transform p)
        {
            Box(p, "Memory paper", ColorOf("#F2E9D8"), 0, 0, 1, 1);
            Circle(p, "Vietnam memory", Gold, .14f, .23f, .5f, .79f);
            Circle(p, "Korea memory", Jade, .5f, .23f, .86f, .79f);
            Box(p, "Meeting path", ColorOf("#A94A42"), .27f, .14f, .73f, .17f);
            Circle(p, "Meeting point", ColorOf("#A94A42"), .46f, .1f, .54f, .21f);
        }

        // Stylized schematic only. Markers are placed approximately; the screen labels it as an illustration.
        private static void DrawCountryMap(Transform p, bool korea)
        {
            Box(p, "Hanji map paper", korea ? ColorOf("#E8EEE6") : ColorOf("#F4E8CF"), 0, 0, 1, 1);
            Box(p, "Sea", korea ? ColorOf("#C9DFDD") : ColorOf("#C5DEDC"), .68f, 0, 1, 1);
            if (!korea)
            {
                // Simple north-to-south Vietnam silhouette assembled from overlapping soft shapes.
                Circle(p, "Northern Vietnam", ColorOf("#76A98E"), .32f, .72f, .57f, .96f);
                Box(p, "Vietnam spine", ColorOf("#76A98E"), .38f, .24f, .57f, .82f);
                Circle(p, "Central Vietnam", ColorOf("#76A98E"), .34f, .31f, .58f, .54f);
                Circle(p, "Southern Vietnam", ColorOf("#76A98E"), .29f, .08f, .55f, .32f);
                Marker(p, "Hoi An marker", Gold, .48f, .42f);
            }
            else
            {
                // Korea peninsula schematic, with Jeonju in the southwest interior.
                Circle(p, "Northern peninsula", ColorOf("#729A83"), .39f, .64f, .67f, .96f);
                Box(p, "Korean peninsula", ColorOf("#729A83"), .46f, .22f, .62f, .78f);
                Circle(p, "Southern peninsula", ColorOf("#729A83"), .42f, .06f, .68f, .39f);
                Marker(p, "Jeonju marker", Gold, .49f, .34f);
            }
            Box(p, "Map legend line", ColorOf("#D3C3A6"), .08f, .12f, .28f, .135f);
        }

        private static void Marker(Transform p, string name, Color color, float x, float y)
        {
            Circle(p, name + " halo", new Color(color.r, color.g, color.b, .3f), x - .09f, y - .09f, x + .09f, y + .09f);
            Circle(p, name, color, x - .045f, y - .045f, x + .045f, y + .045f);
        }

        private static RectTransform Box(Transform parent, string name, Color color, float x0, float y0, float x1, float y1)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static void Circle(Transform parent, string name, Color color, float x0, float y0, float x1, float y1)
        {
            Box(parent, name, color, x0, y0, x1, y1).GetComponent<Image>().sprite = Disc();
        }

        private static Sprite Disc()
        {
            if (disc != null) return disc;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = new Color32(255, 255, 255,
                    (byte)(Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) < 30f ? 255 : 0));
            texture.SetPixels32(pixels);
            texture.Apply();
            disc = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
            return disc;
        }

        private static Color ColorOf(string hex) { ColorUtility.TryParseHtmlString(hex, out var color); return color; }
    }
}
