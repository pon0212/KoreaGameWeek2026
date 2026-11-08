using System.Collections;
using TastePassport.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TastePassport.Core
{
    public sealed partial class PassportApp
    {
        private enum IntroVisual { Cover, Places, People, Food, Today, Guide }

        private sealed class IntroLeafDefinition
        {
            public readonly string EraKey;
            public readonly string TitleKey;
            public readonly string BodyKey;
            public readonly string NoteKey;
            public readonly string CaptionKey;
            public readonly IntroVisual Visual;

            public IntroLeafDefinition(int number, IntroVisual visual)
            {
                string suffix = number.ToString();
                EraKey = "intro_era_" + suffix;
                TitleKey = "intro_title_" + suffix;
                BodyKey = "intro_body_" + suffix;
                NoteKey = "intro_note_" + suffix;
                CaptionKey = "intro_caption_" + suffix;
                Visual = visual;
            }
        }

        private static readonly IntroLeafDefinition[] IntroLeaves =
        {
            new IntroLeafDefinition(0, IntroVisual.Cover),
            new IntroLeafDefinition(1, IntroVisual.Places),
            new IntroLeafDefinition(2, IntroVisual.People),
            new IntroLeafDefinition(3, IntroVisual.Food),
            new IntroLeafDefinition(4, IntroVisual.Today),
            new IntroLeafDefinition(5, IntroVisual.Guide)
        };

        private int introPageIndex;
        private bool introBookOpened;
        private bool bookTurning;
        private RectTransform bookLeaf;
        private RectTransform bookSurface;
        private Image bookVisual;
        private Coroutine bookTurnRoutine;
        private Coroutine bookAutoOpenRoutine;
        private bool bookEntrancePending;

        private void OnDisable()
        {
            if (bookTurnRoutine != null) StopCoroutine(bookTurnRoutine);
            if (bookAutoOpenRoutine != null) StopCoroutine(bookAutoOpenRoutine);
            bookTurnRoutine = null;
            bookAutoOpenRoutine = null;
            bookTurning = false;
        }

        private void OpenIntroBook()
        {
            introPageIndex = 0;
            introBookOpened = false;
            bookTurning = false;
            bookEntrancePending = true;
            Navigate(Page.IntroBook);
            ScheduleBookOpening();
        }

        private void ScheduleBookOpening()
        {
            if (bookAutoOpenRoutine != null) StopCoroutine(bookAutoOpenRoutine);
            bookAutoOpenRoutine = StartCoroutine(OpenBookAfterPause());
        }

        private IEnumerator OpenBookAfterPause()
        {
            yield return null;
            if (bookSurface != null)
            {
                var entrance = bookSurface.GetComponent<CanvasGroup>();
                if (entrance == null) entrance = bookSurface.gameObject.AddComponent<CanvasGroup>();
                bookSurface.localScale = Vector3.one * .08f;
                entrance.alpha = 0f;
                float elapsed = 0f;
                const float duration = .55f;
                while (bookSurface != null && elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                    bookSurface.localScale = Vector3.one * Mathf.Lerp(.08f, 1f, t);
                    entrance.alpha = t;
                    yield return null;
                }
                if (bookSurface != null) bookSurface.localScale = Vector3.one;
                if (entrance != null) entrance.alpha = 1f;
                bookEntrancePending = false;
            }
            yield return new WaitForSecondsRealtime(.18f);
            bookAutoOpenRoutine = null;
            OpenClosedBook();
        }

        private void DrawIntroBook(Transform parent)
        {
            introPageIndex = Mathf.Clamp(introPageIndex, 0, IntroLeaves.Length - 1);
            var stage = NewRect("Antique book stage", parent);
            SetHeight(stage, Mathf.Max(1050f, safeRoot == null ? 1050f : safeRoot.rect.height - 24f));
            var stageLayout = stage.gameObject.AddComponent<VerticalLayoutGroup>();
            stageLayout.spacing = 20f;
            stageLayout.childAlignment = TextAnchor.MiddleCenter;
            stageLayout.childControlWidth = true;
            stageLayout.childControlHeight = true;
            stageLayout.childForceExpandWidth = true;
            stageLayout.childForceExpandHeight = false;
            bookSurface = NewRect(introBookOpened ? "Open antique storybook" : "Closed antique storybook", stage);
            SetHeight(bookSurface, 880);
            if (bookEntrancePending && !introBookOpened) bookSurface.localScale = Vector3.one * .08f;
            bookVisual = bookSurface.gameObject.AddComponent<Image>();
            bookVisual.sprite = Resources.Load<Sprite>(introBookOpened ? "Art/StorybookOpen" : "Art/StorybookClosed");
            bookVisual.type = Image.Type.Simple;
            bookVisual.preserveAspect = true;
            bookVisual.color = Color.white;
            bookVisual.raycastTarget = true;
            if (bookVisual.sprite == null)
                Debug.LogError("Taste Passport: Storybook sprite is missing or not imported as Sprite.");

            if (!introBookOpened)
            {
                bookSurface.gameObject.AddComponent<BookSwipeTurn>().Initialize(OpenClosedBook);
                BookCopy(stage, T("intro_tap_open"), 27, Paper, TextAnchor.MiddleCenter, true);
                return;
            }

            bookSurface.gameObject.AddComponent<BookSwipeTurn>().Initialize(BackIntroBook);
            var leaf = NewRect("Opening storybook leaf " + introPageIndex, bookSurface);
            leaf.anchorMin = new Vector2(.23f, .14f);
            leaf.anchorMax = new Vector2(.94f, .92f);
            leaf.offsetMin = Vector2.zero;
            leaf.offsetMax = Vector2.zero;
            leaf.pivot = new Vector2(0f, .5f);
            var pageHitArea = leaf.gameObject.AddComponent<Image>();
            pageHitArea.color = new Color(1f, 1f, 1f, .001f);
            pageHitArea.raycastTarget = true;
            leaf.gameObject.AddComponent<RectMask2D>();
            var leafContent = NewRect("Storybook text and illustrations", leaf);
            leafContent.anchorMin = new Vector2(0f, 1f);
            leafContent.anchorMax = new Vector2(1f, 1f);
            leafContent.pivot = new Vector2(.5f, 1f);
            leafContent.offsetMin = Vector2.zero;
            leafContent.offsetMax = Vector2.zero;
            var leafLayout = leafContent.gameObject.AddComponent<VerticalLayoutGroup>();
            leafLayout.padding = new RectOffset(16, 12, 10, 8);
            leafLayout.spacing = 4;
            leafLayout.childAlignment = TextAnchor.UpperCenter;
            leafLayout.childControlWidth = true;
            leafLayout.childControlHeight = true;
            leafLayout.childForceExpandWidth = true;
            leafLayout.childForceExpandHeight = false;
            leafContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var leafScroll = leaf.gameObject.AddComponent<ScrollRect>();
            leafScroll.viewport = leaf;
            leafScroll.content = leafContent;
            leafScroll.horizontal = false;
            leafScroll.vertical = true;
            leafScroll.movementType = ScrollRect.MovementType.Clamped;
            leaf.gameObject.AddComponent<CanvasGroup>();
            leaf.gameObject.AddComponent<BookSwipeTurn>().Initialize(AdvanceIntroBook, FlipIntroBook);
            bookLeaf = leaf;

            IntroLeafDefinition definition = IntroLeaves[introPageIndex];
            if (introPageIndex == 0)
                BookCopy(leafContent, T("intro_series"), 22, Vermilion, TextAnchor.MiddleCenter, true);
            BookCopy(leafContent, "◆ " + T(definition.EraKey) + " ◆", 22, Jade, TextAnchor.MiddleCenter, true);
            BookCopy(leafContent, T(definition.TitleKey), 28, HeaderWood, TextAnchor.MiddleCenter, true);
            Rule(leafContent);
            DrawIntroIllustration(leafContent, definition.Visual);
            BookCopy(leafContent, T(definition.CaptionKey), 22, Muted, TextAnchor.MiddleCenter, true);
            BookCopy(leafContent, T(definition.BodyKey), 24, Ink, TextAnchor.MiddleLeft, true);
            if (!string.IsNullOrEmpty(definition.NoteKey))
                BookCopy(leafContent, T(definition.NoteKey), 21, Vermilion, TextAnchor.MiddleLeft, false);

            string count = T("intro_page_count")
                .Replace("{current}", (introPageIndex + 1).ToString())
                .Replace("{total}", IntroLeaves.Length.ToString());
            BookCopy(leafContent, count, 21, Muted, TextAnchor.MiddleCenter, true);

            // Responsive Navigation Bar at the bottom of the book stage
            var navBar = NewRect("Book navigation bar", stage);
            SetHeight(navBar, 52);
            var navLayout = navBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            navLayout.spacing = 10;
            navLayout.childAlignment = TextAnchor.MiddleCenter;
            navLayout.childControlWidth = true;
            navLayout.childControlHeight = true;
            navLayout.childForceExpandWidth = true;
            navLayout.childForceExpandHeight = false;

            if (introPageIndex > 0)
            {
                var backBtn = CompactButton(navBar, "◄ " + T("nav_back_previous"), Paper, BackIntroBook, Ink);
                SetHeight(backBtn.GetComponent<RectTransform>(), 48);
            }

            var indicator = Panel("Page badge", navBar, Paper).rectTransform;
            SetHeight(indicator, 48);
            var indicatorLabel = Label(indicator, count, 22, 48, HeaderWood, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(indicatorLabel.rectTransform);

            string nextText = introPageIndex == IntroLeaves.Length - 1 ? ("★ " + T("start")) : (T("card_continue") + " ►");
            var nextBtn = CompactButton(navBar, nextText, introPageIndex == IntroLeaves.Length - 1 ? Jade : Lantern, AdvanceIntroBook, Color.white);
            SetHeight(nextBtn.GetComponent<RectTransform>(), 48);

            if (introPageIndex < IntroLeaves.Length - 1)
            {
                var skipBtn = CompactButton(navBar, T("world_controls_skip_save"), Paper, () => Navigate(profile.created ? Page.Map : Page.Language), Muted);
                SetHeight(skipBtn.GetComponent<RectTransform>(), 48);
            }

            BookCopy(stage, T(introPageIndex == IntroLeaves.Length - 1 ? "intro_tap_finish" : "intro_tap_next"),
                24, Paper, TextAnchor.MiddleCenter, true);
        }

        private Text BookCopy(Transform parent, string value, int size, Color color, TextAnchor alignment, bool classic)
        {
            var label = Label(parent, value, size, 0, color, alignment, FontStyle.Normal, classic);
            label.lineSpacing = 1.12f;
            var layout = label.GetComponent<LayoutElement>();
            layout.minHeight = -1;
            layout.preferredHeight = -1;
            return label;
        }

        private void DrawIntroIllustration(Transform parent, IntroVisual visual)
        {
            switch (visual)
            {
                case IntroVisual.Cover:
                    Artwork(parent, TravelArtworkKind.Passport, 100);
                    break;
                case IntroVisual.Places:
                    DrawBookPair(parent, TravelArtworkKind.HoiAn, TravelArtworkKind.Jeonju, 112);
                    break;
                case IntroVisual.People:
                    var portraits = NewRect("Two guides in the storybook", parent);
                    SetHeight(portraits, 112);
                    AddHorizontal(portraits);
                    DrawBookPortrait(portraits, "Art/Characters/Nhung", TravelArtworkKind.HoiAn);
                    DrawBookPortrait(portraits, "Art/Characters/GaBo", TravelArtworkKind.Jeonju);
                    break;
                case IntroVisual.Food:
                    DrawBookPair(parent, TravelArtworkKind.CaoLau, TravelArtworkKind.Bibimbap, 112);
                    break;
                case IntroVisual.Today:
                    Artwork(parent, TravelArtworkKind.Friendship, 112);
                    break;
                case IntroVisual.Guide:
                    Artwork(parent, TravelArtworkKind.Passport, 80);
                    break;
            }
        }

        private void DrawBookPair(Transform parent, TravelArtworkKind left, TravelArtworkKind right, float height)
        {
            var row = NewRect("Two places on one spread", parent);
            SetHeight(row, height);
            AddHorizontal(row);
            Artwork(row, left, height);
            Artwork(row, right, height);
        }

        private void DrawBookPortrait(Transform parent, string resourcePath, TravelArtworkKind fallback)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Artwork(parent, fallback, 112);
                return;
            }
            var frame = Panel("Story guide portrait", parent, Paper).rectTransform;
            SetHeight(frame, 112);
            var image = frame.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var outline = frame.gameObject.AddComponent<Outline>();
            outline.effectColor = Border;
            outline.effectDistance = new Vector2(2, -2);
        }

        private void OpenClosedBook()
        {
            if (bookTurning || bookEntrancePending || introBookOpened || page != Page.IntroBook) return;
            bookTurnRoutine = StartCoroutine(OpenBookAnimation());
        }

        private void BackIntroBook()
        {
            if (bookTurning || !introBookOpened || page != Page.IntroBook) return;
            if (introPageIndex > 0) FlipIntroBook(-1);
            else Navigate(Page.Menu);
        }

        private void AdvanceIntroBook()
        {
            if (bookTurning || !introBookOpened || page != Page.IntroBook) return;
            if (introPageIndex == IntroLeaves.Length - 1)
                Navigate(profile.created ? Page.Map : Page.Language);
            else FlipIntroBook(1);
        }

        private IEnumerator OpenBookAnimation()
        {
            bookTurning = true;
            audioManager?.PlayCue(GameSoundCue.BookOpen);
            yield return AnimateBookVisual(bookSurface, bookVisual, 1f, .78f, 1f, 0f, .27f);
            introBookOpened = true;
            Show();
            if (bookSurface != null && bookVisual != null)
            {
                bookSurface.localScale = new Vector3(.78f, 1f, 1f);
                bookVisual.color = new Color(1f, 1f, 1f, 0f);
                if (bookLeaf != null) bookLeaf.GetComponent<CanvasGroup>().alpha = 0f;
                yield return AnimateBookVisual(bookSurface, bookVisual, .78f, 1f, 0f, 1f, .34f);
                if (bookLeaf != null) bookLeaf.GetComponent<CanvasGroup>().alpha = 1f;
            }
            bookTurning = false;
            bookTurnRoutine = null;
        }

        private void FlipIntroBook(int direction)
        {
            if (bookTurning || !introBookOpened || page != Page.IntroBook) return;
            int next = introPageIndex + direction;
            if (next < 0 || next >= IntroLeaves.Length) return;
            bookTurnRoutine = StartCoroutine(TurnIntroBook(next));
        }

        private IEnumerator TurnIntroBook(int next)
        {
            bookTurning = true;
            audioManager?.PlayCue(GameSoundCue.BookOpen);
            yield return AnimateBookLeaf(bookLeaf, 1f, .08f, 1f, 0f, .16f);
            introPageIndex = next;
            Show();
            if (page == Page.IntroBook)
            {
                if (bookLeaf != null)
                {
                    bookLeaf.localScale = new Vector3(.08f, 1f, 1f);
                    bookLeaf.GetComponent<CanvasGroup>().alpha = 0f;
                }
                yield return AnimateBookLeaf(bookLeaf, .08f, 1f, 0f, 1f, .2f);
            }
            bookTurning = false;
            bookTurnRoutine = null;
        }

        private static IEnumerator AnimateBookLeaf(RectTransform leaf, float fromScale, float toScale,
            float fromAlpha, float toAlpha, float seconds)
        {
            if (leaf == null) yield break;
            var group = leaf.GetComponent<CanvasGroup>();
            float elapsed = 0f;
            while (leaf != null && elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
                leaf.localScale = new Vector3(Mathf.Lerp(fromScale, toScale, progress), 1f, 1f);
                if (group != null) group.alpha = Mathf.Lerp(fromAlpha, toAlpha, progress);
                yield return null;
            }
            if (leaf != null)
            {
                leaf.localScale = new Vector3(toScale, 1f, 1f);
                if (group != null) group.alpha = toAlpha;
            }
        }

        private static IEnumerator AnimateBookVisual(RectTransform surface, Image visual,
            float fromScale, float toScale, float fromAlpha, float toAlpha, float seconds)
        {
            if (surface == null || visual == null) yield break;
            float elapsed = 0f;
            while (surface != null && visual != null && elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
                surface.localScale = new Vector3(Mathf.Lerp(fromScale, toScale, progress), 1f, 1f);
                visual.color = new Color(1f, 1f, 1f, Mathf.Lerp(fromAlpha, toAlpha, progress));
                yield return null;
            }
            if (surface != null && visual != null)
            {
                surface.localScale = new Vector3(toScale, 1f, 1f);
                visual.color = new Color(1f, 1f, 1f, toAlpha);
            }
        }
    }
}
