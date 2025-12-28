using System;
using UnityEngine;
using UnityEngine.UI;
using UFC.UI.Theme;

namespace UFC.UI.Widgets
{
    public class EventCardWidget : MonoBehaviour
    {
        public Text Title;
        public Text Subtitle;
        public Button Button;

        public void Bind(string title, string subtitle, Action onClick = null)
        {
            UiTheme.EnsureInitialized(this);
            UiTheme.ApplyCardVisual(gameObject, 84f);

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
            if (Button != null)
            {
                Button.onClick.RemoveAllListeners();
                if (onClick != null)
                {
                    Button.onClick.AddListener(() => onClick());
                }
            }
        }
    }
}
