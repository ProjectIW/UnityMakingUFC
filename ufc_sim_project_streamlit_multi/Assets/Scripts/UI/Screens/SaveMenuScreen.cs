using UnityEngine;
using UFC.Infrastructure.Save;

namespace UFC.UI.Screens
{
    public class SaveMenuScreen : MonoBehaviour
    {
        public int DefaultSlot = 1;

        public void CreateSlot()
        {
            SaveSlotsService.CreateSlot(DefaultSlot, overwrite: true);
        }

        public bool SlotExists()
        {
            return SaveSlotsService.SlotExists(DefaultSlot);
        }
    }
}
