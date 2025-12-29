using UnityEngine;
using UnityEngine.UI;

namespace UFC.UI.Theme
{
    public static class UiTheme
    {
        public static readonly Color Background = new Color(12f / 255f, 15f / 255f, 22f / 255f, 1f);
        public static readonly Color Panel = new Color(26f / 255f, 30f / 255f, 40f / 255f, 1f);
        public static readonly Color PanelElevated = new Color(34f / 255f, 39f / 255f, 52f / 255f, 1f);
        public static readonly Color Accent = new Color(237f / 255f, 71f / 255f, 60f / 255f, 1f);
        public static readonly Color AccentSoft = new Color(255f / 255f, 140f / 255f, 92f / 255f, 1f);
        public static readonly Color TextPrimary = new Color(246f / 255f, 247f / 255f, 251f / 255f, 1f);
        public static readonly Color TextMuted = new Color(169f / 255f, 176f / 255f, 190f / 255f, 1f);
        public static readonly Color TextHeading = new Color(255f / 255f, 255f / 255f, 255f / 255f, 1f);
        public static readonly Color Border = new Color(58f / 255f, 65f / 255f, 82f / 255f, 1f);
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.45f);

        public static Font PrimaryFont { get; private set; }
        public static Font HeadingFont { get; private set; }
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
            HeadingFont = Resources.Load<Font>("Fonts/UfcHeading");

            var fallbackFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (PrimaryFont == null || !SupportsReadableGlyphs(PrimaryFont))
            {
                PrimaryFont = fallbackFont;
            }
            if (HeadingFont == null || !SupportsReadableGlyphs(HeadingFont))
            {
                HeadingFont = PrimaryFont ?? fallbackFont;
            }

            RoundedSquare = Resources.Load<Sprite>("UI/RoundedSquare");

            Initialized = true;
        }

        public static GameObject CreatePanel(Transform parent, Color color, float height)
        {
            Initialize();

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            ApplyLayerFromParent(panel, parent);

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
            ApplyLayerFromParent(textObject, parent);

            var text = textObject.GetComponent<Text>();
            text.text = content ?? string.Empty;
            text.font = PrimaryFont;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = isBold ? FontStyle.Bold : FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = TextAnchor.MiddleLeft;
            text.lineSpacing = 1.1f;
            text.raycastTarget = false;

            return textObject;
        }

        public static GameObject CreateRow(Transform parent, RectOffset padding, float spacing)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            ApplyLayerFromParent(row, parent);

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
            ApplyLayerFromParent(column, parent);

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
            StyleCardSurface(card, preferredHeight, addAccent: true, applyLayout: true);
        }

        public static void StyleCardSurface(GameObject card, float preferredHeight, bool addAccent, bool applyLayout)
        {
            if (card == null)
            {
                return;
            }

            if (card.transform.parent != null)
            {
                ApplyLayerFromParent(card, card.transform.parent);
            }

            var image = card.GetComponent<Image>();
            if (image == null)
            {
                image = card.AddComponent<Image>();
            }

            image.color = PanelElevated;
            image.sprite = RoundedSquare;
            image.type = RoundedSquare != null ? Image.Type.Sliced : Image.Type.Simple;

            ApplyShadow(card, Shadow, new Vector2(0f, -4f));
            ApplyOutline(card, Border, new Vector2(1f, -1f));

            if (addAccent)
            {
                EnsureAccentStrip(card, Accent);
            }

            if (applyLayout && preferredHeight > 0f)
            {
                var layoutElement = card.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = card.AddComponent<LayoutElement>();
                }

                layoutElement.minHeight = preferredHeight;
                layoutElement.preferredHeight = preferredHeight;
                layoutElement.flexibleHeight = 0f;
            }
        }

        public static void ApplyTextStyle(Text text, bool isMuted, bool isHeading)
        {
            if (text == null)
            {
                return;
            }

            Initialize();

            text.font = isHeading ? HeadingFont : PrimaryFont;
            text.color = isMuted ? TextMuted : (isHeading ? TextHeading : TextPrimary);
            text.fontStyle = isHeading ? FontStyle.Bold : text.fontStyle;
        }

        public static void ApplyTabButtonStyle(Button button, bool isActive)
        {
            if (button == null)
            {
                return;
            }

            var image = button.targetGraphic as Image;
            if (image != null)
            {
                image.sprite = RoundedSquare;
                image.type = RoundedSquare != null ? Image.Type.Sliced : Image.Type.Simple;
                image.color = isActive ? Accent : Panel;
            }

            var colors = button.colors;
            colors.normalColor = isActive ? Accent : Panel;
            colors.highlightedColor = isActive ? AccentSoft : PanelElevated;
            colors.pressedColor = isActive ? AccentSoft : Panel;
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(colors.normalColor.r, colors.normalColor.g, colors.normalColor.b, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontSize = 16;
                label.alignment = TextAnchor.MiddleCenter;
                label.fontStyle = FontStyle.Bold;
                ApplyTextStyle(label, isMuted: !isActive, isHeading: true);
                label.color = isActive ? TextHeading : TextMuted;
            }
        }

        public static void ApplyPrimaryButtonStyle(Button button)
        {
            if (button == null)
            {
                return;
            }

            var image = button.targetGraphic as Image;
            if (image != null)
            {
                image.sprite = RoundedSquare;
                image.type = RoundedSquare != null ? Image.Type.Sliced : Image.Type.Simple;
                image.color = Accent;
            }

            var colors = button.colors;
            colors.normalColor = Accent;
            colors.highlightedColor = AccentSoft;
            colors.pressedColor = new Color(Accent.r * 0.9f, Accent.g * 0.9f, Accent.b * 0.9f, 1f);
            colors.selectedColor = Accent;
            colors.disabledColor = new Color(Accent.r, Accent.g, Accent.b, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontSize = 16;
                label.alignment = TextAnchor.MiddleCenter;
                ApplyTextStyle(label, isMuted: false, isHeading: true);
                label.color = TextHeading;
            }
        }

        public static void ApplyCardButtonStyle(Button button)
        {
            if (button == null)
            {
                return;
            }

            var colors = button.colors;
            colors.normalColor = PanelElevated;
            colors.highlightedColor = new Color(48f / 255f, 55f / 255f, 72f / 255f, 1f);
            colors.pressedColor = new Color(26f / 255f, 30f / 255f, 40f / 255f, 1f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(PanelElevated.r, PanelElevated.g, PanelElevated.b, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
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

            layout.spacing = 16f;
            layout.padding = new RectOffset(20, 20, 20, 20);
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

        public static void ApplyLayerFromParent(GameObject target, Transform parent)
        {
            if (target == null || parent == null)
            {
                return;
            }

            ApplyLayerRecursive(target, parent.gameObject.layer);
        }

        public static void ApplyLayerRecursive(GameObject target, int layer)
        {
            if (target == null)
            {
                return;
            }

            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                if (child != null)
                {
                    ApplyLayerRecursive(child.gameObject, layer);
                }
            }
        }

        private static void ApplyShadow(GameObject target, Color color, Vector2 distance)
        {
            var shadow = target.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = target.AddComponent<Shadow>();
            }
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private static void ApplyOutline(GameObject target, Color color, Vector2 distance)
        {
            var outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void EnsureAccentStrip(GameObject target, Color color)
        {
            var accentTransform = target.transform.Find("AccentStrip");
            if (accentTransform == null)
            {
                var accent = new GameObject("AccentStrip", typeof(RectTransform), typeof(Image));
                accent.transform.SetParent(target.transform, false);
                ApplyLayerFromParent(accent, target.transform);
                accentTransform = accent.transform;

                var rect = accentTransform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    rect.sizeDelta = new Vector2(4f, 0f);
                    rect.anchoredPosition = Vector2.zero;
                }
            }

            var image = accentTransform.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private static bool SupportsReadableGlyphs(Font font)
        {
            if (font == null)
            {
                return false;
            }

            return font.HasCharacter('A') && font.HasCharacter('0') && font.HasCharacter('А');
        }
    }
}
