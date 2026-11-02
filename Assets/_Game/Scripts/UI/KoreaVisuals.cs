using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TastePassport.UI
{
    /// <summary>Small, non-interactive Korean scene accents for existing artwork panels.</summary>
    public static class KoreaVisuals
    {
        private static Sprite softDisc;

        /// <summary>
        /// Adds a single animated, raycast-transparent overlay. Safe to call more than once
        /// on a panel; repeated calls will not duplicate decorations.
        /// </summary>
        public static void Attach(RectTransform panel, TravelArtworkKind kind)
        {
            if (panel == null || !IsKorean(kind) || panel.Find("Korean scene details") != null) return;

            var layerObject = new GameObject("Korean scene details", typeof(RectTransform), typeof(KoreaVisualLayer));
            var layer = (RectTransform)layerObject.transform;
            layer.SetParent(panel, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = layer.offsetMax = Vector2.zero;
            layer.SetAsLastSibling();

            var motion = layerObject.GetComponent<KoreaVisualLayer>();
            switch (kind)
            {
                case TravelArtworkKind.Jeonju:
                case TravelArtworkKind.KoreaJourney:
                    AddLantern(layer, motion, .82f, .74f, .075f);
                    AddPetal(layer, motion, .18f, .74f, .035f, 0f);
                    AddPetal(layer, motion, .69f, .42f, .025f, 1.7f);
                    AddPetal(layer, motion, .38f, .19f, .022f, 3.2f);
                    break;
                case TravelArtworkKind.KoreaMap:
                    AddMapPulse(layer, motion, .53f, .35f);
                    AddPetal(layer, motion, .76f, .75f, .024f, .8f);
                    break;
                case TravelArtworkKind.Bibimbap:
                    AddSteam(layer, motion, .43f, .84f, 0f);
                    AddSteam(layer, motion, .51f, .88f, .8f);
                    AddSteam(layer, motion, .59f, .83f, 1.6f);
                    break;
            }
            motion.StartMotion();
        }

        /// <summary>Styles a tappable recipe choice as a distinct hanji ingredient card.</summary>
        public static void StyleIngredientCard(RectTransform card, string key)
        {
            if (card == null || card.Find("Hanji ingredient card") != null) return;

            var layer = CreateOverlay(card, "Hanji ingredient card");
            layer.SetAsFirstSibling();
            var background = card.GetComponent<Image>();
            if (background != null) background.color = ColorOf("#FBF4E7");

            Color accent;
            Color secondary;
            switch (key)
            {
                case "bibi_egg":
                    accent = ColorOf("#B65343");
                    secondary = ColorOf("#DFA957");
                    break;
                case "bibi_vegetables":
                    accent = ColorOf("#557A58");
                    secondary = ColorOf("#B86645");
                    break;
                case "decoy_cheese":
                    accent = ColorOf("#BB873B");
                    secondary = ColorOf("#8C5745");
                    break;
                case "decoy_noodles":
                    accent = ColorOf("#9A6846");
                    secondary = ColorOf("#D0A75D");
                    break;
                case "decoy_kimchi":
                    accent = ColorOf("#A8493C");
                    secondary = ColorOf("#D58C54");
                    break;
                default:
                    accent = ColorOf("#496E70");
                    secondary = ColorOf("#C49A58");
                    break;
            }

            // A broad, quiet side band and woven lower rules give each ingredient a
            // cultural color identity while leaving the existing label unobstructed.
            Shape(layer, "Hanji color wash", new Color(accent.r, accent.g, accent.b, .075f), .5f, .5f, .88f, .82f);
            Shape(layer, "Dancheong side band", accent, .055f, .5f, .055f, .82f);
            Shape(layer, "Dancheong inner line", secondary, .105f, .5f, .012f, .72f);
            Shape(layer, "Hanji upper rule", new Color(.49f, .37f, .27f, .42f), .54f, .88f, .78f, .009f);
            Shape(layer, "Hanji lower rule", new Color(.49f, .37f, .27f, .48f), .54f, .12f, .78f, .012f);
            Shape(layer, "Woven color thread", secondary, .54f, .095f, .78f, .012f);

            var motion = layer.gameObject.AddComponent<KoreaVisualLayer>();
            var thread = Shape(layer, "Floating thread detail", new Color(secondary.r, secondary.g, secondary.b, .72f), .92f, .76f, .035f, .035f, true);
            motion.Add(thread, KoreaVisualLayer.Motion.Float, .003f, .006f, IngredientPhase(key));
            motion.StartMotion();
        }

        /// <summary>Warm hanji paper and subtle dancheong-inspired rules for the recipe prompt.</summary>
        public static void DecorateRecipePanel(RectTransform panel)
        {
            if (panel == null || panel.Find("Hanji recipe frame") != null) return;
            var layer = CreateOverlay(panel, "Hanji recipe frame");
            layer.SetAsFirstSibling();
            Shape(layer, "Top teal rule", ColorOf("#496E70"), .06f, .91f, .88f, .014f);
            Shape(layer, "Bottom vermilion rule", ColorOf("#A94A42"), .06f, .08f, .88f, .014f);
            Shape(layer, "Left gold mark", ColorOf("#C49A58"), .045f, .34f, .01f, .32f);
            Shape(layer, "Right gold mark", ColorOf("#C49A58"), .945f, .34f, .01f, .32f);
        }

        /// <summary>Adds quiet hanji trim and hanging lantern details around a Jeonju location card.</summary>
        public static void DecorateLocationCard(RectTransform card)
        {
            if (card == null || card.Find("Jeonju location trim") != null) return;

            var layer = CreateOverlay(card, "Jeonju location trim");
            Shape(layer, "Hanji left edge", new Color(.67f, .46f, .34f, .22f), .008f, .02f, .008f, .96f);
            Shape(layer, "Hanji right edge", new Color(.67f, .46f, .34f, .22f), .984f, .02f, .008f, .96f);
            AddLantern(layer, null, .035f, .94f, .028f);
            AddLantern(layer, null, .965f, .94f, .028f);

            var motion = layer.gameObject.AddComponent<KoreaVisualLayer>();
            AddPetal(layer, motion, .07f, .82f, .022f, .3f);
            AddPetal(layer, motion, .93f, .68f, .02f, 1.9f);
            motion.StartMotion();
        }

        private static RectTransform CreateOverlay(RectTransform parent, string name)
        {
            var layerObject = new GameObject(name, typeof(RectTransform));
            layerObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var layer = (RectTransform)layerObject.transform;
            layer.SetParent(parent, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = layer.offsetMax = Vector2.zero;
            layer.SetAsLastSibling();
            return layer;
        }

        private static float IngredientPhase(string key)
        {
            if (key == "bibi_vegetables") return 1.1f;
            if (key == "bibi_egg") return 2.2f;
            return 0f;
        }

        private static bool IsKorean(TravelArtworkKind kind) =>
            kind == TravelArtworkKind.Jeonju || kind == TravelArtworkKind.KoreaJourney ||
            kind == TravelArtworkKind.KoreaMap || kind == TravelArtworkKind.Bibimbap;

        private static void AddLantern(RectTransform layer, KoreaVisualLayer motion, float x, float y, float size)
        {
            var stringRect = Shape(layer, "Lantern thread", ColorOf("#805A45"), x, y + size * 1.55f, .006f, size * 1.55f);
            if (motion != null) motion.Add(stringRect, KoreaVisualLayer.Motion.Float, .035f, 0f, .7f);
            var lantern = Shape(layer, "Warm hanji lantern", ColorOf("#E9A75C"), x - size * .42f, y - size * .52f, size * .84f, size * 1.18f);
            var glow = Shape(layer, "Lantern glow", new Color(1f, .73f, .37f, .18f), x - size * .85f, y - size * .94f, size * 1.7f, size * 1.7f, true);
            glow.SetAsFirstSibling();
            if (motion != null) motion.Add(lantern, KoreaVisualLayer.Motion.Pulse, .08f, .06f, 1.4f);
        }

        private static void AddPetal(RectTransform layer, KoreaVisualLayer motion, float x, float y, float size, float phase)
        {
            var petal = Shape(layer, "Falling plum petal", ColorOf("#D98A81"), x, y, size, size * .62f, true);
            motion.Add(petal, KoreaVisualLayer.Motion.Float, .11f, .13f, phase);
        }

        private static void AddMapPulse(RectTransform layer, KoreaVisualLayer motion, float x, float y)
        {
            var pulse = Shape(layer, "Jeonju map marker pulse", new Color(.83f, .48f, .25f, .28f), x - .055f, y - .055f, .11f, .11f, true);
            motion.Add(pulse, KoreaVisualLayer.Motion.Pulse, .24f, .24f, 0f);
        }

        private static void AddSteam(RectTransform layer, KoreaVisualLayer motion, float x, float y, float phase)
        {
            var wisp = Shape(layer, "Bibimbap steam", new Color(.98f, .95f, .86f, .62f), x, y, .014f, .07f, true);
            motion.Add(wisp, KoreaVisualLayer.Motion.Steam, .07f, .16f, phase);
        }

        private static RectTransform Shape(RectTransform parent, string name, Color color,
            float x, float y, float width, float height, bool round = false)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = new Vector2(x - width * .5f, y - height * .5f);
            rect.anchorMax = new Vector2(x + width * .5f, y + height * .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (round) image.sprite = Disc();
            return rect;
        }

        private static Sprite Disc()
        {
            if (softDisc != null) return softDisc;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Korean UI soft circle" };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                var alpha = Mathf.Clamp01((15.5f - distance) * .8f + .5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            softDisc = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
            softDisc.name = "Korean UI soft circle";
            return softDisc;
        }

        private static Color ColorOf(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }

    [DisallowMultipleComponent]
    internal sealed class KoreaVisualLayer : MonoBehaviour
    {
        internal enum Motion { Float, Pulse, Steam }
        private readonly List<AnimatedDetail> details = new List<AnimatedDetail>(8);
        private bool running;

        internal void Add(RectTransform rect, Motion motion, float amplitudeX, float amplitudeY, float phase)
        {
            details.Add(new AnimatedDetail(rect, motion, amplitudeX, amplitudeY, phase));
        }

        internal void StartMotion() => running = true;

        private void Update()
        {
            if (!running) return;
            var time = Time.unscaledTime;
            var rect = (RectTransform)transform;
            var size = rect.rect.size;
            for (var i = 0; i < details.Count; i++) details[i].Tick(time, size);
        }

        private sealed class AnimatedDetail
        {
            private readonly RectTransform rect;
            private readonly Motion motion;
            private readonly Vector2 basePosition;
            private readonly float amplitudeX;
            private readonly float amplitudeY;
            private readonly float phase;

            internal AnimatedDetail(RectTransform rect, Motion motion, float amplitudeX, float amplitudeY, float phase)
            {
                this.rect = rect;
                this.motion = motion;
                basePosition = rect.anchoredPosition;
                this.amplitudeX = amplitudeX;
                this.amplitudeY = amplitudeY;
                this.phase = phase;
            }

            internal void Tick(float time, Vector2 panelSize)
            {
                if (rect == null) return;
                var t = time + phase;
                switch (motion)
                {
                    case Motion.Float:
                        rect.anchoredPosition = basePosition + new Vector2(Mathf.Sin(t * 1.1f) * amplitudeX * panelSize.x,
                            Mathf.Sin(t * .8f) * amplitudeY * panelSize.y);
                        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t) * 9f);
                        break;
                    case Motion.Pulse:
                        var scale = 1f + (Mathf.Sin(t * 2f) + 1f) * .5f * amplitudeX;
                        rect.localScale = Vector3.one * scale;
                        break;
                    case Motion.Steam:
                        var cycle = Mathf.Repeat(t * .32f, 1f);
                        rect.anchoredPosition = basePosition + new Vector2(Mathf.Sin(t * 1.7f) * amplitudeX * panelSize.x,
                            cycle * amplitudeY * panelSize.y);
                        rect.localScale = new Vector3(1f - cycle * .35f, 1f + cycle * .2f, 1f);
                        var image = rect.GetComponent<Graphic>();
                        if (image != null)
                        {
                            var color = image.color;
                            color.a = (1f - cycle) * .62f;
                            image.color = color;
                        }
                        break;
                }
            }
        }
    }

}
