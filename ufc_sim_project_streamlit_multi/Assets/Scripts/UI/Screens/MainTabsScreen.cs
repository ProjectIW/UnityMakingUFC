using UnityEngine;
using UFC.UI.Theme;

namespace UFC.UI.Screens
{
    public class MainTabsScreen : MonoBehaviour
    {
        public GameObject RankingTab;
        public GameObject EventsTab;
        public GameObject PastEventsTab;

        private void Awake()
        {
            UiTheme.EnsureInitialized(this);
        }

        public void ShowRanking()
        {
            SetActiveTab(RankingTab);
        }

        public void ShowEvents()
        {
            SetActiveTab(EventsTab);
        }

        public void ShowPastEvents()
        {
            SetActiveTab(PastEventsTab);
        }

        private void SetActiveTab(GameObject target)
        {
            if (RankingTab != null) RankingTab.SetActive(target == RankingTab);
            if (EventsTab != null) EventsTab.SetActive(target == EventsTab);
            if (PastEventsTab != null) PastEventsTab.SetActive(target == PastEventsTab);
        }
    }
}
