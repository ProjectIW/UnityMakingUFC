using UnityEngine;
using UnityEngine.UI;
using UFC.UI.Theme;

namespace UFC.UI.Screens
{
    [ExecuteAlways]
    public class MainTabsScreen : MonoBehaviour
    {
        public GameObject RankingTab;
        public GameObject EventsTab;
        public GameObject PastEventsTab;
        public Button RankingButton;
        public Button EventsButton;
        public Button PastEventsButton;

        private void OnEnable()
        {
            UiTheme.EnsureInitialized(this);
            CacheButtons();
            ApplySceneChrome();
        }

        private void Awake()
        {
            UiTheme.EnsureInitialized(this);
            CacheButtons();
            ApplySceneChrome();
        }

        private void OnValidate()
        {
            UiTheme.EnsureInitialized(this);
            CacheButtons();
            ApplySceneChrome();
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

            UiTheme.ApplyTabButtonStyle(RankingButton, target == RankingTab);
            UiTheme.ApplyTabButtonStyle(EventsButton, target == EventsTab);
            UiTheme.ApplyTabButtonStyle(PastEventsButton, target == PastEventsTab);
        }

        private void CacheButtons()
        {
            if (RankingButton == null)
            {
                RankingButton = FindButton("RankingButton");
            }
            if (EventsButton == null)
            {
                EventsButton = FindButton("EventsButton");
            }
            if (PastEventsButton == null)
            {
                PastEventsButton = FindButton("PastButton");
            }
        }

        private Button FindButton(string name)
        {
            var buttonTransform = transform.Find(name);
            return buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
        }

        private void ApplySceneChrome()
        {
            var background = GameObject.Find("Background");
            if (background != null)
            {
                var image = background.GetComponent<Image>();
                if (image != null)
                {
                    image.color = UiTheme.Background;
                }
            }

            var nextWeek = GameObject.Find("NextWeekButton");
            if (nextWeek != null)
            {
                UiTheme.ApplyPrimaryButtonStyle(nextWeek.GetComponent<Button>());
            }
        }
    }
}
