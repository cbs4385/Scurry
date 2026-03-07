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
            string json = null;

            if (File.Exists(SavePath))
            {
                json = File.ReadAllText(SavePath);
                Debug.Log($"[SaveManager] Load: loaded from local save at '{SavePath}'");
            }
            else
            {
                // Fallback to Steam Cloud if local save is missing
                Debug.Log($"[SaveManager] Load: no local save at '{SavePath}' — trying Steam Cloud");
                json = Steam.SteamManager.CloudLoad("run_save.json");
                if (json != null)
                {
                    Debug.Log("[SaveManager] Load: restored from Steam Cloud");
                    // Write cloud save to local disk for future loads
                    File.WriteAllText(SavePath, json);
                }
            }

            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("[SaveManager] Load: no save found (local or cloud)");
                return null;
            }

            var data = JsonUtility.FromJson<RunSaveData>(json);
            Debug.Log($"[SaveManager] Load: parsed save (level={data.currentLevel}, colonyHP={data.colonyHP}, " +
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
