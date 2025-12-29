using System.Globalization;
using UFC.Core.Models;
using UFC.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace UFC.UI.Widgets
{
    public class RankingEntryWidget : MonoBehaviour
    {
        private static readonly Color ChampionGold = new Color(212f / 255f, 175f / 255f, 55f / 255f, 1f);
        private const float RatingMin = 1300f;
        private const float RatingMax = 2200f;

        private bool _isBuilt;
        private Text _rankText;
        private Text _nameText;
        private Text _recordText;
        private Text _ratingValueText;
        private Text _ratingLabelText;
        private Image _rankBadgeImage;
        private Image _ratingBarFill;
        private Image _avatarImage;

        public void Bind(Fighter fighter, string rank)
        {
            if (!_isBuilt)
            {
                BuildVisualHierarchy();
            }

            if (fighter == null)
            {
                return;
            }

            _rankText.text = rank ?? string.Empty;
            bool isChampion = fighter.IsChamp == 1 || rank == "C";
            _rankBadgeImage.color = isChampion ? ChampionGold : UiTheme.Accent;
            _rankText.color = UiTheme.Background;

            _nameText.text = fighter.Name ?? string.Empty;
            _recordText.text = $"{fighter.Age} лет • {fighter.Wins}-{fighter.Losses}-{fighter.Draws}";
            var ratingValue = Mathf.RoundToInt(fighter.Rating);
            _ratingValueText.text = ratingValue.ToString(CultureInfo.InvariantCulture);
            _ratingLabelText.text = "Рейтинг";

            var ratingColor = UiTheme.GetRatingColor(fighter.Rating);
            _ratingValueText.color = ratingColor;
            if (_ratingBarFill != null)
            {
                _ratingBarFill.color = ratingColor;
                _ratingBarFill.fillAmount = Mathf.Clamp01((fighter.Rating - RatingMin) / (RatingMax - RatingMin));
            }

            if (_avatarImage != null && _avatarImage.sprite == null)
            {
                _avatarImage.color = UiTheme.Panel;
            }
        }

        private void BuildVisualHierarchy()
        {
            UiTheme.Initialize();

            var rootRect = gameObject.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = gameObject.AddComponent<RectTransform>();
            }

            ClearExistingChildren();

            var layoutElement = gameObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 108f;
            layoutElement.preferredHeight = 108f;
            layoutElement.flexibleHeight = 0f;
            layoutElement.flexibleWidth = 1f;

            var panel = UiTheme.CreatePanel(transform, UiTheme.PanelElevated, 0f);
            panel.name = "Card";
            UiTheme.StyleCardSurface(panel, 0f, addAccent: true, applyLayout: false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var row = UiTheme.CreateRow(panel.transform, new RectOffset(18, 18, 16, 16), 16f);
            row.name = "Row";
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = Vector2.zero;
            rowRect.anchorMax = Vector2.one;
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            var rankBadge = UiTheme.CreatePanel(row.transform, UiTheme.Accent, 44f);
            rankBadge.name = "RankBadge";
            _rankBadgeImage = rankBadge.GetComponent<Image>();
            var rankLayout = rankBadge.AddComponent<LayoutElement>();
            rankLayout.minWidth = 52f;
            rankLayout.preferredWidth = 52f;
            rankLayout.minHeight = 44f;
            rankLayout.preferredHeight = 44f;

            var rankObject = UiTheme.CreateText(rankBadge.transform, "#1", 16, UiTheme.Background, true);
            rankObject.name = "Rank";
            _rankText = rankObject.GetComponent<Text>();
            _rankText.alignment = TextAnchor.MiddleCenter;

            var avatarRoot = new GameObject("Avatar", typeof(RectTransform), typeof(Image), typeof(Mask));
            avatarRoot.transform.SetParent(row.transform, false);
            UiTheme.ApplyLayerFromParent(avatarRoot, row.transform);
            var avatarImage = avatarRoot.GetComponent<Image>();
            avatarImage.sprite = UiTheme.RoundedSquare;
            avatarImage.type = UiTheme.RoundedSquare != null ? Image.Type.Sliced : Image.Type.Simple;
            avatarImage.color = UiTheme.Panel;
            var avatarMask = avatarRoot.GetComponent<Mask>();
            avatarMask.showMaskGraphic = true;
            var avatarLayout = avatarRoot.AddComponent<LayoutElement>();
            avatarLayout.minWidth = 56f;
            avatarLayout.minHeight = 56f;
            avatarLayout.preferredWidth = 56f;
            avatarLayout.preferredHeight = 56f;

            var avatarContent = new GameObject("AvatarImage", typeof(RectTransform), typeof(Image));
            avatarContent.transform.SetParent(avatarRoot.transform, false);
            UiTheme.ApplyLayerFromParent(avatarContent, avatarRoot.transform);
            _avatarImage = avatarContent.GetComponent<Image>();
            _avatarImage.preserveAspect = true;
            _avatarImage.color = UiTheme.Panel;

            var infoColumn = UiTheme.CreateColumn(row.transform, 4f);
            infoColumn.name = "Info";
            var infoLayout = infoColumn.AddComponent<LayoutElement>();
            infoLayout.flexibleWidth = 1f;

            var nameObject = UiTheme.CreateText(infoColumn.transform, "Name", 20, UiTheme.TextPrimary, true);
            nameObject.name = "Name";
            _nameText = nameObject.GetComponent<Text>();
            UiTheme.ApplyTextStyle(_nameText, false, true);

            var recordObject = UiTheme.CreateText(infoColumn.transform, "Возраст | Record", 13, UiTheme.TextMuted, false);
            recordObject.name = "Record";
            _recordText = recordObject.GetComponent<Text>();

            var ratingColumn = UiTheme.CreateColumn(row.transform, 6f);
            ratingColumn.name = "RatingColumn";
            var ratingLayout = ratingColumn.AddComponent<LayoutElement>();
            ratingLayout.minWidth = 120f;
            ratingLayout.preferredWidth = 120f;

            var ratingLabel = UiTheme.CreateText(ratingColumn.transform, "Рейтинг", 12, UiTheme.TextMuted, false);
            ratingLabel.name = "RatingLabel";
            _ratingLabelText = ratingLabel.GetComponent<Text>();
            _ratingLabelText.alignment = TextAnchor.UpperRight;

            var ratingValue = UiTheme.CreateText(ratingColumn.transform, "1500", 20, UiTheme.TextHeading, true);
            ratingValue.name = "RatingValue";
            _ratingValueText = ratingValue.GetComponent<Text>();
            _ratingValueText.alignment = TextAnchor.UpperRight;
            UiTheme.ApplyTextStyle(_ratingValueText, false, true);

            var barRoot = new GameObject("RatingBar", typeof(RectTransform), typeof(Image));
            barRoot.transform.SetParent(ratingColumn.transform, false);
            UiTheme.ApplyLayerFromParent(barRoot, ratingColumn.transform);
            var barImage = barRoot.GetComponent<Image>();
            barImage.color = UiTheme.Panel;
            barImage.sprite = UiTheme.RoundedSquare;
            barImage.type = UiTheme.RoundedSquare != null ? Image.Type.Sliced : Image.Type.Simple;
            var barLayout = barRoot.AddComponent<LayoutElement>();
            barLayout.minHeight = 8f;
            barLayout.preferredHeight = 8f;

            var barFill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barRoot.transform, false);
            UiTheme.ApplyLayerFromParent(barFill, barRoot.transform);
            var barFillRect = barFill.GetComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;
            _ratingBarFill = barFill.GetComponent<Image>();
            _ratingBarFill.sprite = UiTheme.RoundedSquare;
            _ratingBarFill.type = Image.Type.Filled;
            _ratingBarFill.fillMethod = Image.FillMethod.Horizontal;
            _ratingBarFill.fillOrigin = 0;
            _ratingBarFill.fillAmount = 0.5f;

            _isBuilt = true;
        }

        private void ClearExistingChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
