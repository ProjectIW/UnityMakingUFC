using System;
using UnityEngine;
using UnityEngine.UI;

namespace UFC.UI.Widgets
{
    public class EventCardWidget : MonoBehaviour
    {
        public Text Title;
        public Text Subtitle;
        public Button Button;

        public void Bind(string title, string subtitle, Action onClick = null)
        {
            if (Title != null)
            {
                Title.text = title ?? string.Empty;
            }
            if (Subtitle != null)
            {
                Subtitle.text = subtitle ?? string.Empty;
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
