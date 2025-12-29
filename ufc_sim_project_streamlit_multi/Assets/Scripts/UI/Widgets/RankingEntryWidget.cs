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

        private bool _isBuilt;
        private Text _rankText;
        private Text _nameText;
        private Text _recordText;
        private Text _eloText;
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
            _rankText.color = fighter.IsChamp == 1 || rank == "C" ? ChampionGold : UiTheme.Accent;

            _nameText.text = fighter.Name ?? string.Empty;
            _recordText.text = $"{fighter.Age} лет | {fighter.Wins}-{fighter.Losses}-{fighter.Draws}";
            _eloText.text = Mathf.RoundToInt(fighter.Rating).ToString(CultureInfo.InvariantCulture);

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

            var layoutElement = gameObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 76f;
            layoutElement.preferredHeight = 76f;
            layoutElement.flexibleHeight = 0f;
            layoutElement.flexibleWidth = 1f;

            var panel = UiTheme.CreatePanel(transform, UiTheme.Panel, 0f);
            panel.name = "Card";
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var row = UiTheme.CreateRow(panel.transform, new RectOffset(15, 15, 10, 10), 12f);
            row.name = "Row";
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = Vector2.zero;
            rowRect.anchorMax = Vector2.one;
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            var rankObject = UiTheme.CreateText(row.transform, "#1", 16, UiTheme.Accent, true);
            rankObject.name = "Rank";
            _rankText = rankObject.GetComponent<Text>();
            _rankText.alignment = TextAnchor.MiddleCenter;
            var rankLayout = rankObject.AddComponent<LayoutElement>();
            rankLayout.minWidth = 40f;
            rankLayout.preferredWidth = 40f;

            var avatarRoot = new GameObject("Avatar", typeof(RectTransform), typeof(Image), typeof(Mask));
            avatarRoot.transform.SetParent(row.transform, false);
            var avatarImage = avatarRoot.GetComponent<Image>();
            avatarImage.sprite = UiTheme.RoundedSquare;
            avatarImage.type = UiTheme.RoundedSquare != null ? Image.Type.Sliced : Image.Type.Simple;
            avatarImage.color = UiTheme.Panel;
            var avatarMask = avatarRoot.GetComponent<Mask>();
            avatarMask.showMaskGraphic = true;
            var avatarLayout = avatarRoot.AddComponent<LayoutElement>();
            avatarLayout.minWidth = 50f;
            avatarLayout.minHeight = 50f;
            avatarLayout.preferredWidth = 50f;
            avatarLayout.preferredHeight = 50f;

            var avatarContent = new GameObject("AvatarImage", typeof(RectTransform), typeof(Image));
            avatarContent.transform.SetParent(avatarRoot.transform, false);
            _avatarImage = avatarContent.GetComponent<Image>();
            _avatarImage.preserveAspect = true;
            _avatarImage.color = UiTheme.Panel;

            var infoColumn = UiTheme.CreateColumn(row.transform, 2f);
            infoColumn.name = "Info";
            var infoLayout = infoColumn.AddComponent<LayoutElement>();
            infoLayout.flexibleWidth = 1f;

            var nameObject = UiTheme.CreateText(infoColumn.transform, "Name", 18, UiTheme.TextPrimary, true);
            nameObject.name = "Name";
            _nameText = nameObject.GetComponent<Text>();

            var recordObject = UiTheme.CreateText(infoColumn.transform, "Возраст | Record", 14, UiTheme.TextMuted, false);
            recordObject.name = "Record";
            _recordText = recordObject.GetComponent<Text>();

            var eloBadge = UiTheme.CreatePanel(row.transform, UiTheme.Accent, 30f);
            eloBadge.name = "EloBadge";
            var eloLayout = eloBadge.AddComponent<LayoutElement>();
            eloLayout.minWidth = 60f;
            eloLayout.preferredWidth = 60f;

            var eloTextObject = UiTheme.CreateText(eloBadge.transform, "1500", 14, UiTheme.TextPrimary, true);
            _eloText = eloTextObject.GetComponent<Text>();
            _eloText.alignment = TextAnchor.MiddleCenter;

            _isBuilt = true;
        }
    }
}
