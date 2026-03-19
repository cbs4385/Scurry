using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;

namespace Scurry.Core
{
    public enum AchievementId
    {
        FirstVictory,
        DefeatElderSilas,
        DefeatTobiasDuchess,
        DefeatAldricFenn,
        DefeatPiedPiper,
        FullRunComplete,
        Scrapbook25,
        Scrapbook50,
        Scrapbook75,
        Scrapbook100,
        Bestiary25,
        Bestiary50,
        Bestiary100,
        Collect100Resources,
        Collect500Resources,
        Defeat50Enemies,
        Defeat200Enemies,
        Complete10Runs,
        NoStarvationRun,
        PerfectBossKill,
        AllRelicsCollected,
    }

    public class AchievementManager : MonoBehaviour
    {
        private static AchievementManager _instance;
        public static AchievementManager Instance => _instance;

        private HashSet<string> unlockedAchievements = new HashSet<string>();

        // Cumulative stats for achievement tracking
        private int totalResourcesCollected;
        private int totalEnemiesDefeated;
        private int totalShopPurchases;
        private int totalUpgrades;
        private int totalRunsCompleted;
        private bool currentRunNoStarvation;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Log("[AchievementManager] Awake: duplicate instance — destroying component only");
                Destroy(this);
                return;
            }
            _instance = this;
            // Only DontDestroyOnLoad if on dedicated Bootstrap GO, not a shared scene GO
            if (gameObject.name == "PersistentManagers")
                DontDestroyOnLoad(gameObject);
            LoadAchievements();
            Debug.Log($"[AchievementManager] Awake: loaded {unlockedAchievements.Count} unlocked achievements");
        }

        private void OnEnable()
        {
            Debug.Log("[AchievementManager] OnEnable: subscribing to events");
            EventBus.OnRunComplete += OnRunComplete;
        }

        private void OnDisable()
        {
            Debug.Log("[AchievementManager] OnDisable: unsubscribing from events");
            EventBus.OnRunComplete -= OnRunComplete;
        }

        public bool IsUnlocked(AchievementId id)
        {
            return unlockedAchievements.Contains(id.ToString());
        }

        public void TryUnlock(AchievementId id)
        {
            string key = id.ToString();
            if (unlockedAchievements.Contains(key)) return;

            unlockedAchievements.Add(key);
            Debug.Log($"[AchievementManager] TryUnlock: ACHIEVEMENT UNLOCKED — {key}");

            SaveAchievements();

            // Push to Steam
            Steam.SteamManager.UnlockAchievement(key);

            EventBus.OnAchievementUnlocked?.Invoke(key);

            // Route achievement notification through the per-scene NotificationStack
            string displayName = FormatAchievementName(key);
            EventBus.OnNotification?.Invoke($"Achievement Unlocked: {displayName}", new Color(1f, 0.85f, 0.3f));
        }

        private string FormatAchievementName(string key)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < key.Length; i++)
            {
                if (i > 0 && char.IsUpper(key[i]) && !char.IsUpper(key[i - 1]))
                    sb.Append(' ');
                sb.Append(key[i]);
            }
            return sb.ToString();
        }

        public void OnRunStarted()
        {
            currentRunNoStarvation = true;
            Debug.Log("[AchievementManager] OnRunStarted: resetting per-run tracking (noStarvation=true)");
        }

        public void OnBossDefeatedByName(string bossName)
        {
            Debug.Log($"[AchievementManager] OnBossDefeatedByName: bossName='{bossName}'");
            switch (bossName)
            {
                case "Elder Silas":
                    TryUnlock(AchievementId.DefeatElderSilas);
                    break;
                case "Tobias & Duchess":
                    TryUnlock(AchievementId.DefeatTobiasDuchess);
                    break;
                case "Guildmaster Aldric Fenn":
                    TryUnlock(AchievementId.DefeatAldricFenn);
                    break;
                case "The Pied Piper":
                    TryUnlock(AchievementId.DefeatPiedPiper);
                    TryUnlock(AchievementId.FullRunComplete);
                    break;
            }
        }

        private void OnRunComplete(bool victory)
        {
            if (!victory) return;

            totalRunsCompleted++;
            Debug.Log($"[AchievementManager] OnRunComplete: totalRunsCompleted={totalRunsCompleted}, noStarvation={currentRunNoStarvation}");

            if (totalRunsCompleted >= 10) TryUnlock(AchievementId.Complete10Runs);
            if (currentRunNoStarvation) TryUnlock(AchievementId.NoStarvationRun);

            SaveStats();
        }

        public void OnResourceDeposited(int amount)
        {
            totalResourcesCollected += amount;
            Debug.Log($"[AchievementManager] OnResourceDeposited: amount={amount}, totalCollected={totalResourcesCollected}");
            if (totalResourcesCollected >= 100) TryUnlock(AchievementId.Collect100Resources);
            if (totalResourcesCollected >= 500) TryUnlock(AchievementId.Collect500Resources);
            SaveStats();
        }

        public void OnEnemyDefeated()
        {
            totalEnemiesDefeated++;
            Debug.Log($"[AchievementManager] OnEnemyDefeated: totalDefeated={totalEnemiesDefeated}");
            if (totalEnemiesDefeated >= 50) TryUnlock(AchievementId.Defeat50Enemies);
            if (totalEnemiesDefeated >= 200) TryUnlock(AchievementId.Defeat200Enemies);
            SaveStats();
        }

        public void OnStarvationOccurred()
        {
            currentRunNoStarvation = false;
            Debug.Log("[AchievementManager] OnStarvationOccurred: noStarvation now false");
        }

        public void CheckScrapbookCompletion(float percent)
        {
            Debug.Log($"[AchievementManager] CheckScrapbookCompletion: {percent:F1}%");
            if (percent >= 25f) TryUnlock(AchievementId.Scrapbook25);
            if (percent >= 50f) TryUnlock(AchievementId.Scrapbook50);
            if (percent >= 75f) TryUnlock(AchievementId.Scrapbook75);
            if (percent >= 100f) TryUnlock(AchievementId.Scrapbook100);
        }

        public void CheckBestiaryCompletion(float percent)
        {
            Debug.Log($"[AchievementManager] CheckBestiaryCompletion: {percent:F1}%");
            if (percent >= 25f) TryUnlock(AchievementId.Bestiary25);
            if (percent >= 50f) TryUnlock(AchievementId.Bestiary50);
            if (percent >= 100f) TryUnlock(AchievementId.Bestiary100);
        }

        public void OnPerfectBossKill()
        {
            Debug.Log("[AchievementManager] OnPerfectBossKill: no heroes wounded during boss fight");
            TryUnlock(AchievementId.PerfectBossKill);
        }

        private void LoadAchievements()
        {
            string json = PlayerPrefs.GetString("Scurry_Achievements", "");
            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<AchievementSaveData>(json);
                if (data != null && data.unlocked != null)
                {
                    foreach (var id in data.unlocked)
                        unlockedAchievements.Add(id);
                }
            }

            totalResourcesCollected = PlayerPrefs.GetInt("Scurry_TotalResources", 0);
            totalEnemiesDefeated = PlayerPrefs.GetInt("Scurry_TotalEnemies", 0);
            totalShopPurchases = PlayerPrefs.GetInt("Scurry_TotalPurchases", 0);
            totalUpgrades = PlayerPrefs.GetInt("Scurry_TotalUpgrades", 0);
            totalRunsCompleted = PlayerPrefs.GetInt("Scurry_TotalRuns", 0);

            Debug.Log($"[AchievementManager] LoadAchievements: unlocked={unlockedAchievements.Count}, resources={totalResourcesCollected}, enemies={totalEnemiesDefeated}, purchases={totalShopPurchases}, upgrades={totalUpgrades}, runs={totalRunsCompleted}");
        }

        private void SaveAchievements()
        {
            var data = new AchievementSaveData();
            data.unlocked = new List<string>(unlockedAchievements);
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("Scurry_Achievements", json);
            PlayerPrefs.Save();
            Debug.Log($"[AchievementManager] SaveAchievements: saved {unlockedAchievements.Count} achievements");
        }

        private void SaveStats()
        {
            PlayerPrefs.SetInt("Scurry_TotalResources", totalResourcesCollected);
            PlayerPrefs.SetInt("Scurry_TotalEnemies", totalEnemiesDefeated);
            PlayerPrefs.SetInt("Scurry_TotalPurchases", totalShopPurchases);
            PlayerPrefs.SetInt("Scurry_TotalUpgrades", totalUpgrades);
            PlayerPrefs.SetInt("Scurry_TotalRuns", totalRunsCompleted);
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                Debug.Log($"[AchievementManager] OnDestroy: duplicate instance destroyed, skipping unregister (self={GetInstanceID()})");
                return;
            }
            _instance = null;
            Debug.Log("[AchievementManager] OnDestroy: singleton instance cleared");
        }

        [System.Serializable]
        private class AchievementSaveData
        {
            public List<string> unlocked = new List<string>();
        }
    }
}
