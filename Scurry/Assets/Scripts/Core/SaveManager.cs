using System.IO;
using UnityEngine;
using Scurry.Data;

namespace Scurry.Core
{
    public static class SaveManager
    {
        private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "run_save.json");

        public static void Save(RunSaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveManager] Save: saved to '{SavePath}' (level={data.currentLevel}, colonyHP={data.colonyHP}, " +
                      $"heroDeck={data.heroDeckCardNames.Count}, wounded={data.woundedHeroNames.Count}, " +
                      $"nodes={data.nodesVisited}, food={data.foodStockpile}, materials={data.materialsStockpile}, currency={data.currencyStockpile})");

            // Sync to Steam Cloud
            Steam.SteamManager.CloudSave("run_save.json", json);
        }

        public static RunSaveData Load()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log($"[SaveManager] Load: no save file found at '{SavePath}'");
                return null;
            }

            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<RunSaveData>(json);
            Debug.Log($"[SaveManager] Load: loaded from '{SavePath}' (level={data.currentLevel}, colonyHP={data.colonyHP}, " +
                      $"heroDeck={data.heroDeckCardNames.Count}, wounded={data.woundedHeroNames.Count}, nodes={data.nodesVisited})");
            return data;
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log($"[SaveManager] DeleteSave: deleted save file at '{SavePath}'");
            }
            else
            {
                Debug.Log($"[SaveManager] DeleteSave: no save file to delete at '{SavePath}'");
            }
        }

        public static bool HasSave()
        {
            bool exists = File.Exists(SavePath);
            Debug.Log($"[SaveManager] HasSave: {exists}");
            return exists;
        }
    }
}
