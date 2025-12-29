using UnityEngine;
using UnityEngine.UI;
using UFC.UI.Theme;

namespace UFC.UI.Widgets
{
    public class FightCardWidget : MonoBehaviour
    {
        public Text Title;
        public Text Subtitle;

        public void Bind(string title, string subtitle)
        {
            UiTheme.EnsureInitialized(this);
            UiTheme.ApplyCardVisual(gameObject, 86f);

            if (Title != null)
            {
                Title.text = title ?? string.Empty;
                Title.fontSize = 18;
                UiTheme.ApplyTextStyle(Title, false, true);
            }
            if (Subtitle != null)
            {
                Subtitle.text = subtitle ?? string.Empty;
                Subtitle.fontSize = 14;
                UiTheme.ApplyTextStyle(Subtitle, true, false);
            }
        }
    }
}
