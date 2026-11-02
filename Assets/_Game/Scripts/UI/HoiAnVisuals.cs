using UnityEngine;
using UnityEngine.UI;

namespace TastePassport.UI
{
    // Lightweight motion layered over the existing Hội An and Cao Lầu artwork.
    // Every decoration ignores raycasts so exploration controls remain usable.
    public sealed class HoiAnVisuals : MonoBehaviour
    {
        private RectTransform[] accents;
        private Vector2[] positions;
        private bool cooking;
        private static Sprite disc;

        public static void Attach(RectTransform panel, TravelArtworkKind kind)
        {
            if (panel == null || panel.GetComponent<HoiAnVisuals>() != null) return;
            var motion = panel.gameObject.AddComponent<HoiAnVisuals>();
            motion.cooking = kind == TravelArtworkKind.CaoLau;
            motion.Build(panel);
        }

        public static void StyleIngredientCard(RectTransform card, string ingredientKey)
        {
            if (card == null || card.Find("Hội An card trim") != null) return;
            Color accent = ingredientKey == "cao_lau_greens" ? new Color(.31f, .48f, .30f) :
                ingredientKey == "cao_lau_pork" ? new Color(.59f, .34f, .24f) :
                ingredientKey == "cao_lau_noodles" ? new Color(.72f, .42f, .20f) :
                new Color(.60f, .52f, .39f);
            var image = card.GetComponent<Image>();
            if (image != null) image.color = new Color(1f, .97f, .91f);
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.57f, .40f, .27f, .65f);
            outline.effectDistance = new Vector2(2, -2);
            var trim = Shape(card, "Hội An card trim", accent, false);
            trim.anchorMin = new Vector2(.03f, .10f);
            trim.anchorMax = new Vector2(.03f, .90f);
            trim.sizeDelta = new Vector2(5, 0);
            var button = card.GetComponent<Button>();
            if (button != null)
            {
                var colors = button.colors;
                colors.highlightedColor = new Color(1f, .97f, .91f);
                colors.pressedColor = new Color(.95f, .84f, .69f);
                button.colors = colors;
            }
        }

        public static void DecorateLocationCard(RectTransform card)
        {
            if (card == null || card.Find("Hội An page trim") != null) return;
            var overlay = new GameObject("Hội An page trim", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            overlay.SetParent(card, false);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            overlay.GetComponent<LayoutElement>().ignoreLayout = true;
            var edge = Shape(overlay, "Lantern orange page edge", new Color(.88f, .49f, .20f, .48f), false);
            edge.anchorMin = new Vector2(.014f, .03f);
            edge.anchorMax = new Vector2(.014f, .97f);
            edge.sizeDelta = new Vector2(4, 0);
            var seal = Shape(overlay, "Old town seal", new Color(.67f, .29f, .21f, .7f), true);
            seal.anchorMin = seal.anchorMax = new Vector2(.95f, .96f);
            seal.sizeDelta = new Vector2(18, 18);
        }

        public static void DecorateRecipePanel(RectTransform panel)
        {
            if (panel == null || panel.Find("Hội An recipe frame") != null) return;
            var overlay = new GameObject("Hội An recipe frame", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            overlay.SetParent(panel, false);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            overlay.GetComponent<LayoutElement>().ignoreLayout = true;
            overlay.SetAsFirstSibling();
            var upper = Shape(overlay, "Old town ochre rule", new Color(.70f, .37f, .18f, .75f), false);
            upper.anchorMin = new Vector2(.06f, .91f);
            upper.anchorMax = new Vector2(.94f, .91f);
            upper.sizeDelta = new Vector2(0, 3);
            var lower = Shape(overlay, "Wooden table rule", new Color(.43f, .29f, .19f, .60f), false);
            lower.anchorMin = new Vector2(.06f, .09f);
            lower.anchorMax = new Vector2(.94f, .09f);
            lower.sizeDelta = new Vector2(0, 3);
        }

        private void Build(RectTransform panel)
        {
            accents = new RectTransform[cooking ? 3 : 2];
            positions = new Vector2[accents.Length];
            if (cooking)
            {
                for (int i = 0; i < accents.Length; i++)
                {
                    var wisp = Shape(panel, "Cao Lầu steam", new Color(1f, .97f, .83f, .58f), true);
                    wisp.anchorMin = wisp.anchorMax = new Vector2(.43f + i * .075f, .56f);
                    wisp.sizeDelta = new Vector2(12 + i * 4, 27 + i * 3);
                    accents[i] = wisp;
                    positions[i] = new Vector2(0, -5);
                }
                return;
            }

            for (int i = 0; i < accents.Length; i++)
            {
                float x = i == 0 ? .10f : .90f;
                var cord = Shape(panel, "Lantern cord", new Color(.34f, .19f, .12f, .8f), false);
                cord.anchorMin = cord.anchorMax = new Vector2(x, 1f);
                cord.pivot = new Vector2(.5f, 1f);
                cord.sizeDelta = new Vector2(3, 30);

                var lantern = Shape(panel, "Hội An lantern", new Color(.89f, .37f, .18f, .9f), true);
                lantern.anchorMin = lantern.anchorMax = new Vector2(x, 1f);
                lantern.pivot = new Vector2(.5f, 1f);
                lantern.sizeDelta = new Vector2(32, 42);
                lantern.anchoredPosition = new Vector2(0, -27);
                accents[i] = lantern;
                positions[i] = lantern.anchoredPosition;

                var stripe = Shape(lantern, "Lantern gold rib", new Color(1f, .77f, .34f, .9f), false);
                stripe.anchorMin = stripe.anchorMax = new Vector2(.5f, .5f);
                stripe.sizeDelta = new Vector2(3, 29);
            }
        }

        private void Update()
        {
            if (accents == null) return;
            float time = Time.unscaledTime;
            for (int i = 0; i < accents.Length; i++)
            {
                if (accents[i] == null) continue;
                if (cooking)
                {
                    float cycle = Mathf.Repeat(time * (.17f + i * .027f) + i * .31f, 1f);
                    accents[i].anchoredPosition = positions[i] + new Vector2(Mathf.Sin(time * 1.4f + i) * 5f, cycle * 35f);
                    var image = accents[i].GetComponent<Image>();
                    var tint = image.color;
                    tint.a = .48f * (1f - cycle);
                    image.color = tint;
                }
                else
                {
                    accents[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 1.25f + i * 1.6f) * 7f);
                    accents[i].anchoredPosition = positions[i] + new Vector2(Mathf.Sin(time * 1.1f + i) * 3f, 0);
                }
            }
        }

        private static RectTransform Shape(Transform parent, string name, Color color, bool round)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (round) image.sprite = Disc();
            return obj.GetComponent<RectTransform>();
        }

        private static Sprite Disc()
        {
            if (disc != null) return disc;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(16f - distance)));
                }
            texture.Apply();
            disc = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
            return disc;
        }
    }
}
