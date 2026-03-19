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
            Debug.Log($"[SaveManager] Save: serializing save data (turn={data.currentTurn}, seed={data.randomSeed}, state={data.runState})");

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveManager] Save: saved to '{SavePath}' (turn={data.currentTurn}, " +
                      $"deck={data.deckCardIds.Count}, colonyDeck={data.colonyDeckCardIds.Count}, " +
                      $"heroes={data.deployedHeroes.Count}, enemies={data.enemies.Count}, " +
                      $"food={data.foodStockpile}, materials={data.materialsStockpile}, currency={data.currencyStockpile}, " +
                      $"mapNodes={data.mapNodes.Count})");

            // Sync to Steam Cloud
            try
            {
                Steam.SteamManager.CloudSave("run_save.json", json);
                Debug.Log("[SaveManager] Save: synced to Steam Cloud");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveManager] Save: Steam Cloud sync failed — {e.Message}");
            }
        }

        public static RunSaveData Load()
        {
            Debug.Log($"[SaveManager] Load: attempting to load from '{SavePath}'");

            string json = null;

            if (File.Exists(SavePath))
            {
                json = File.ReadAllText(SavePath);
                Debug.Log($"[SaveManager] Load: loaded from local save at '{SavePath}' (length={json.Length})");
            }
            else
            {
                // Fallback to Steam Cloud if local save is missing
                Debug.Log($"[SaveManager] Load: no local save at '{SavePath}' — trying Steam Cloud");
                try
                {
                    json = Steam.SteamManager.CloudLoad("run_save.json");
                    if (json != null)
                    {
                        Debug.Log("[SaveManager] Load: restored from Steam Cloud");
                        // Write cloud save to local disk for future loads
                        File.WriteAllText(SavePath, json);
                        Debug.Log("[SaveManager] Load: wrote cloud save to local disk");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SaveManager] Load: Steam Cloud load failed — {e.Message}");
                }
            }

            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("[SaveManager] Load: no save found (local or cloud)");
                return null;
            }

            var data = JsonUtility.FromJson<RunSaveData>(json);
            Debug.Log($"[SaveManager] Load: parsed save (turn={data.currentTurn}, seed={data.randomSeed}, state={data.runState}, " +
                      $"deck={data.deckCardIds.Count}, colonyDeck={data.colonyDeckCardIds.Count}, " +
                      $"heroes={data.deployedHeroes.Count}, enemies={data.enemies.Count}, " +
                      $"mapNodes={data.mapNodes.Count})");
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
