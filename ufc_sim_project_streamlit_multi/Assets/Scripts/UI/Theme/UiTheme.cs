using UnityEngine;
using UnityEngine.UI;

namespace UFC.UI.Theme
{
    public static class UiTheme
    {
        public static readonly Color Background = new Color(14f / 255f, 17f / 255f, 23f / 255f, 1f);
        public static readonly Color Panel = new Color(38f / 255f, 39f / 255f, 48f / 255f, 1f);
        public static readonly Color Accent = new Color(1f, 75f / 255f, 75f / 255f, 1f);
        public static readonly Color TextPrimary = new Color(250f / 255f, 250f / 255f, 250f / 255f, 1f);
        public static readonly Color TextMuted = new Color(160f / 255f, 164f / 255f, 175f / 255f, 1f);

        public static Font PrimaryFont { get; private set; }
        public static Sprite RoundedSquare { get; private set; }
        public static bool Initialized { get; private set; }

        public static void EnsureInitialized(Component context)
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (Initialized)
            {
                return;
            }

            PrimaryFont = Resources.Load<Font>("Fonts/UfcPrimary");
            if (PrimaryFont == null)
            {
                PrimaryFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            RoundedSquare = Resources.Load<Sprite>("UI/RoundedSquare");

            Initialized = true;
        }

        public static GameObject CreatePanel(Transform parent, Color color, float height)
        {
            Initialize();

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            MatchParentLayer(panel, parent);

            var image = panel.GetComponent<Image>();
            image.color = color;
            image.sprite = RoundedSquare;
            image.type = RoundedSquare != null ? Image.Type.Sliced : Image.Type.Simple;

            if (height > 0f)
            {
                var layoutElement = panel.AddComponent<LayoutElement>();
                layoutElement.minHeight = height;
                layoutElement.preferredHeight = height;
                layoutElement.flexibleHeight = 0f;
            }

            return panel;
        }

        public static GameObject CreateText(Transform parent, string content, int fontSize, Color color, bool isBold)
        {
            Initialize();

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            MatchParentLayer(textObject, parent);

            var text = textObject.GetComponent<Text>();
            text.text = content ?? string.Empty;
            text.font = PrimaryFont;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = isBold ? FontStyle.Bold : FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;

            return textObject;
        }

        public static GameObject CreateRow(Transform parent, RectOffset padding, float spacing)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            MatchParentLayer(row, parent);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            return row;
        }

        public static GameObject CreateColumn(Transform parent, float spacing)
        {
            var column = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup));
            column.transform.SetParent(parent, false);
            MatchParentLayer(column, parent);

            var layout = column.GetComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            return column;
        }

        public static void ApplyCardVisual(GameObject card, float preferredHeight)
        {
            if (card == null)
            {
                return;
            }

            var image = card.GetComponent<Image>();
            if (image == null)
            {
                image = card.AddComponent<Image>();
            }

            image.color = Panel;
            image.sprite = RoundedSquare;
            image.type = RoundedSquare != null ? Image.Type.Sliced : Image.Type.Simple;

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

            Initialize();

            text.font = PrimaryFont;
            text.color = isMuted ? TextMuted : TextPrimary;
            if (isHeading)
            {
                text.fontStyle = FontStyle.Bold;
            }
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

        private static void MatchParentLayer(GameObject child, Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            var targetLayer = parent.gameObject.layer;
            child.layer = targetLayer;
        }
    }
}
