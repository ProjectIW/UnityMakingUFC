using UnityEngine;
using UnityEngine.UI;

namespace UFC.UI.Widgets
{
    public class RankingEntryWidget : MonoBehaviour
    {
        public Text Title;
        public Text Subtitle;

        public void Bind(string title, string subtitle)
        {
            if (Title != null)
            {
                Title.text = title ?? string.Empty;
            }
            if (Subtitle != null)
            {
                Subtitle.text = subtitle ?? string.Empty;
            }
        }
    }
}
