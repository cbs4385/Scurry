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
        CompleteLevel1,
        CompleteLevel2,
        CompleteLevel3,
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
        Buy10ShopCards,
        Upgrade10Cards,
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
                Debug.Log("[AchievementManager] Awake: duplicate instance — destroying self");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            LoadAchievements();
            Debug.Log($"[AchievementManager] Awake: loaded {unlockedAchievements.Count} unlocked achievements");
        }

        private void OnEnable()
        {
            Debug.Log("[AchievementManager] OnEnable: subscribing to events");
            EventBus.OnBossDefeated += OnBossDefeated;
            EventBus.OnRunComplete_M1 += OnRunComplete;
            EventBus.OnStarvationDamage += OnStarvationDamage;
            EventBus.OnCardPurchased += OnCardPurchased;
            EventBus.OnUpgradeComplete += OnUpgradeComplete;
            EventBus.OnResourceCollected += OnResourceCollected;
        }

        private void OnDisable()
        {
            Debug.Log("[AchievementManager] OnDisable: unsubscribing from events");
            EventBus.OnBossDefeated -= OnBossDefeated;
            EventBus.OnRunComplete_M1 -= OnRunComplete;
            EventBus.OnStarvationDamage -= OnStarvationDamage;
            EventBus.OnCardPurchased -= OnCardPurchased;
            EventBus.OnUpgradeComplete -= OnUpgradeComplete;
            EventBus.OnResourceCollected -= OnResourceCollected;
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
        }

        public void OnRunStarted()
        {
            currentRunNoStarvation = true;
            Debug.Log("[AchievementManager] OnRunStarted: resetting per-run tracking (noStarvation=true)");
        }

        private void OnBossDefeated()
        {
            Debug.Log("[AchievementManager] OnBossDefeated: checking boss achievements");
            TryUnlock(AchievementId.FirstVictory);
        }

        public void OnBossDefeatedByName(string bossName)
        {
            Debug.Log($"[AchievementManager] OnBossDefeatedByName: bossName='{bossName}'");
            switch (bossName)
            {
                case "Elder Silas":
                    TryUnlock(AchievementId.DefeatElderSilas);
                    TryUnlock(AchievementId.CompleteLevel1);
                    break;
                case "Tobias & Duchess":
                    TryUnlock(AchievementId.DefeatTobiasDuchess);
                    TryUnlock(AchievementId.CompleteLevel2);
                    break;
                case "Guildmaster Aldric Fenn":
                    TryUnlock(AchievementId.DefeatAldricFenn);
                    TryUnlock(AchievementId.CompleteLevel3);
                    break;
                case "The Pied Piper":
                    TryUnlock(AchievementId.DefeatPiedPiper);
                    TryUnlock(AchievementId.FullRunComplete);
                    break;
            }
        }

        public void OnLevelCompleted(int level)
        {
            Debug.Log($"[AchievementManager] OnLevelCompleted: level={level}");
            switch (level)
            {
                case 1: TryUnlock(AchievementId.CompleteLevel1); break;
                case 2: TryUnlock(AchievementId.CompleteLevel2); break;
                case 3: TryUnlock(AchievementId.CompleteLevel3); break;
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

        private void OnStarvationDamage(int damage)
        {
            currentRunNoStarvation = false;
            Debug.Log($"[AchievementManager] OnStarvationDamage: damage={damage}, noStarvation now false");
        }

        private void OnCardPurchased(CardDefinitionSO card)
        {
            totalShopPurchases++;
            Debug.Log($"[AchievementManager] OnCardPurchased: card='{card.cardName}', totalPurchases={totalShopPurchases}");
            if (totalShopPurchases >= 10) TryUnlock(AchievementId.Buy10ShopCards);
            SaveStats();
        }

        private void OnUpgradeComplete()
        {
            totalUpgrades++;
            Debug.Log($"[AchievementManager] OnUpgradeComplete: totalUpgrades={totalUpgrades}");
            if (totalUpgrades >= 10) TryUnlock(AchievementId.Upgrade10Cards);
            SaveStats();
        }

        private void OnResourceCollected(ResourceType type, int value)
        {
            totalResourcesCollected += value;
            Debug.Log($"[AchievementManager] OnResourceCollected: type={type}, value={value}, totalCollected={totalResourcesCollected}");
            if (totalResourcesCollected >= 100) TryUnlock(AchievementId.Collect100Resources);
            if (totalResourcesCollected >= 500) TryUnlock(AchievementId.Collect500Resources);
            SaveStats();
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

        [System.Serializable]
        private class AchievementSaveData
        {
            public List<string> unlocked = new List<string>();
        }
    }
}
