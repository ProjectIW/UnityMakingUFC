using UnityEngine;
using UnityEngine.UI;

namespace UFC.UI.Theme
{
    public static class UiTheme
    {
        public static readonly Color Background = new Color(0.05f, 0.05f, 0.07f, 1f);
        public static readonly Color Panel = new Color(0.12f, 0.12f, 0.15f, 0.95f);
        public static readonly Color PanelAlt = new Color(0.14f, 0.14f, 0.18f, 0.95f);
        public static readonly Color Accent = new Color(0.86f, 0.24f, 0.24f, 1f);
        public static readonly Color TextPrimary = new Color(0.95f, 0.95f, 0.96f, 1f);
        public static readonly Color TextMuted = new Color(0.67f, 0.69f, 0.72f, 1f);

        public static Font PrimaryFont { get; private set; }
        public static Font HeadingFont { get; private set; }
        public static bool Initialized { get; private set; }

        public static void EnsureInitialized(Component context)
        {
            var canvas = context != null ? context.GetComponentInParent<Canvas>() : Object.FindObjectOfType<Canvas>();
            if (!Initialized)
            {
                Initialize(canvas);
            }

            if (canvas != null)
            {
                ApplyGlobal(canvas);
            }
        }

        public static void Initialize(Canvas canvas)
        {
            if (Initialized)
            {
                return;
            }

            PrimaryFont = Resources.Load<Font>("Fonts/UfcPrimary");
            HeadingFont = Resources.Load<Font>("Fonts/UfcHeading");

            if (PrimaryFont == null)
            {
                PrimaryFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (HeadingFont == null)
            {
                HeadingFont = PrimaryFont;
            }

            if (canvas != null)
            {
                ApplyGlobal(canvas);
            }

            Initialized = true;
        }

        public static void ApplyGlobal(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            var root = canvas.transform;

            var background = FindByName(root, "Background");
            if (background != null)
            {
                var image = background.GetComponent<Image>();
                if (image != null)
                {
                    image.color = Background;
                }
            }

            ApplyPanelColor(root, "RankingTab");
            ApplyPanelColor(root, "EventsTab");
            ApplyPanelColor(root, "PastEventsTab");

            ApplyTextStyles(root);
            ApplyButtonStyles(root);

            EnsureListLayout(FindByName(root, "ListRoot"));
            EnsureListLayout(FindByName(root, "EventsListRoot"));
            EnsureListLayout(FindByName(root, "FightListRoot"));
            EnsureListLayout(FindByName(root, "ResultsListRoot"));
        }

        public static void ApplyCardVisual(GameObject card, float preferredHeight)
        {
            if (card == null)
            {
                return;
            }

            var image = card.GetComponent<Image>();
            if (image != null)
            {
                image.color = PanelAlt;
            }

            var layoutElement = card.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = card.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = preferredHeight;
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleHeight = 0f;
        }

        public static void ApplyTextStyle(Text text, bool isMuted, bool isHeading)
        {
            if (text == null)
            {
                return;
            }

            if (isHeading && HeadingFont != null)
            {
                text.font = HeadingFont;
            }
            else if (PrimaryFont != null)
            {
                text.font = PrimaryFont;
            }

            text.color = isMuted ? TextMuted : TextPrimary;
        }

        public static void EnsureListLayout(Transform listRoot)
        {
            if (listRoot == null)
            {
                return;
            }

            var rect = listRoot as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
            }

            var layout = listRoot.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = listRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.spacing = 12f;
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = listRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = listRoot.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void ApplyPanelColor(Transform root, string objectName)
        {
            var panel = FindByName(root, objectName);
            if (panel == null)
            {
                return;
            }

            var image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.color = Panel;
            }
        }

        private static void ApplyTextStyles(Transform root)
        {
            var texts = root.GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                var lowerName = text.name.ToLowerInvariant();
                var isMuted = lowerName.Contains("subtitle")
                    || lowerName.Contains("hint")
                    || lowerName.Contains("desc");
                var isHeading = lowerName.Contains("title")
                    || lowerName.Contains("header")
                    || text.fontSize >= 18;

                ApplyTextStyle(text, isMuted, isHeading);
            }
        }

        private static void ApplyButtonStyles(Transform root)
        {
            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                var colors = button.colors;
                colors.normalColor = PanelAlt;
                colors.highlightedColor = new Color(PanelAlt.r + 0.05f, PanelAlt.g + 0.05f, PanelAlt.b + 0.05f, PanelAlt.a);
                colors.pressedColor = Accent;
                colors.selectedColor = Accent;
                colors.disabledColor = new Color(PanelAlt.r, PanelAlt.g, PanelAlt.b, 0.5f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.15f;
                button.colors = colors;
            }
        }

        private static Transform FindByName(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindByName(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
