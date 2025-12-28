using UnityEngine;
using UnityEngine.UI;
using UFC.UI.Theme;

namespace UFC.UI.Widgets
{
    public class RankingEntryWidget : MonoBehaviour
    {
        public Text Title;
        public Text Subtitle;

        public void Bind(string title, string subtitle)
        {
            UiTheme.EnsureInitialized(this);
            UiTheme.ApplyCardVisual(gameObject, 68f);

            if (Title != null)
            {
                Title.text = title ?? string.Empty;
                UiTheme.ApplyTextStyle(Title, false, true);
            }
            if (Subtitle != null)
            {
                Subtitle.text = subtitle ?? string.Empty;
                UiTheme.ApplyTextStyle(Subtitle, true, false);
            }
        }
    }
}
