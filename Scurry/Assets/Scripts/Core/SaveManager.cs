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
            Debug.Log($"[SaveManager] Save: saved run data to '{SavePath}' (turn={data.turnNumber}, colonyHP={data.colonyHP}, drawPile={data.drawPileCardNames.Count}, discardPile={data.discardPileCardNames.Count}, wounded={data.woundedHeroCardNames.Count}, livingEnemies={data.livingEnemyPositions.Count})");
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
            Debug.Log($"[SaveManager] Load: loaded run data from '{SavePath}' (turn={data.turnNumber}, colonyHP={data.colonyHP}, drawPile={data.drawPileCardNames.Count}, discardPile={data.discardPileCardNames.Count}, wounded={data.woundedHeroCardNames.Count}, livingEnemies={data.livingEnemyPositions.Count})");
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
