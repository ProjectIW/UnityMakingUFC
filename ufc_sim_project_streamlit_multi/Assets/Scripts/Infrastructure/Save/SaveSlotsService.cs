using System.IO;
using System.Linq;
using UnityEngine;

namespace UFC.Infrastructure.Save
{
    public static class SaveSlotsService
    {
        public static string SavesRoot => Path.Combine(Application.persistentDataPath, "saves");

        public static string SlotPath(int slotId)
        {
            return Path.Combine(SavesRoot, $"slot_{slotId}");
        }

        public static string SlotDataPath(int slotId)
        {
            return Path.Combine(SlotPath(slotId), "Data");
        }

        public static bool SlotExists(int slotId)
        {
            return File.Exists(Path.Combine(SlotDataPath(slotId), "_global", "save_game.csv"));
        }

        public static void EnsureSlotData(int slotId)
        {
            string slot = SlotPath(slotId);
            string dataPath = SlotDataPath(slotId);
            string savePath = Path.Combine(dataPath, "_global", "save_game.csv");

            if (Directory.Exists(dataPath) && File.Exists(savePath))
            {
                return;
            }

            if (Directory.Exists(dataPath) && Directory.EnumerateFileSystemEntries(dataPath).Any())
            {
                return;
            }

            Directory.CreateDirectory(slot);
            CopyDirectory(Application.streamingAssetsPath + "/BaseData", dataPath);
        }

        public static void CreateSlot(int slotId, bool overwrite = false)
        {
            string slot = SlotPath(slotId);
            if (Directory.Exists(slot))
            {
                if (!overwrite)
                {
                    throw new IOException($"Save slot {slotId} already exists.");
                }
                Directory.Delete(slot, true);
            }
            Directory.CreateDirectory(slot);
            CopyDirectory(Application.streamingAssetsPath + "/BaseData", Path.Combine(slot, "Data"));
        }

        public static void CopySlot(int sourceSlot, int targetSlot, bool overwrite = false)
        {
            string source = SlotPath(sourceSlot);
            string target = SlotPath(targetSlot);
            if (!Directory.Exists(source))
            {
                throw new IOException($"Source save slot {sourceSlot} missing.");
            }
            if (Directory.Exists(target))
            {
                if (!overwrite)
                {
                    throw new IOException($"Target save slot {targetSlot} already exists.");
                }
                Directory.Delete(target, true);
            }
            Directory.CreateDirectory(target);
            CopyDirectory(Path.Combine(source, "Data"), Path.Combine(target, "Data"));
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string dest = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }
            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string dest = Path.Combine(targetDir, Path.GetFileName(directory));
                CopyDirectory(directory, dest);
            }
        }
    }
}
