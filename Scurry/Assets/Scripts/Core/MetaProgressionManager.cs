using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;

namespace Scurry.Core
{
    public class MetaProgressionManager : MonoBehaviour
    {
        private const string SAVE_KEY = "MetaProgression";

        private MetaProgressionData data;

        // Reputation costs
        private const int RELIC_UNLOCK_COST = 10;
        private const int COLONY_CARD_UNLOCK_COST = 5;

        // Colony deck adjustment thresholds
        private const int LEVELS_PER_DECK_BONUS = 3;
        private const int MAX_DECK_BONUS = 5;

        public MetaProgressionData Data => data;
        public int ColonyDeckSizeBonus => data?.colonyDeckSizeBonus ?? 0;
        public int Reputation => data?.reputation ?? 0;
        public int ScrapbookCompletion => data?.scrapbookCompletion ?? 0;
        public int BestiaryCompletion => data?.bestiaryCompletion ?? 0;

        private void Awake()
        {
            Debug.Log("[MetaProgressionManager] Awake: loading meta-progression data");
            Load();
        }

        private void OnEnable()
        {
            Debug.Log("[MetaProgressionManager] OnEnable: subscribing to events");
            EventBus.OnRunComplete += OnRunComplete;
            EventBus.OnRunFailed += OnRunFailed;
            EventBus.OnBossDefeated += OnBossDefeated;
        }

        private void OnDisable()
        {
            Debug.Log("[MetaProgressionManager] OnDisable: unsubscribing from events");
            EventBus.OnRunComplete -= OnRunComplete;
            EventBus.OnRunFailed -= OnRunFailed;
            EventBus.OnBossDefeated -= OnBossDefeated;
        }

        // --- Run End Processing ---

        public void ProcessRunEnd(bool victory, int levelReached, int resourcesGathered,
            int bossesKilled, int nodesVisited, List<CardDefinitionSO> heroDeck,
            List<string> enemiesEncountered, List<string> eventsEncountered,
            List<string> bossesEncountered)
        {
            Debug.Log($"[MetaProgressionManager] ProcessRunEnd: victory={victory}, level={levelReached}, resources={resourcesGathered}, bosses={bossesKilled}, nodes={nodesVisited}");

            // Update totals
            data.totalResourcesGathered += resourcesGathered;
            data.totalBossesDefeated += bossesKilled;

            if (victory)
            {
                data.totalRunsCompleted++;
                data.totalLevelsCleared += levelReached;

                // Rattery: mark heroes used in winning run for upgraded unlocks
                if (heroDeck != null)
                {
                    foreach (var card in heroDeck)
                    {
                        if (card != null && card.cardType == CardType.Hero && !data.upgradedHeroUnlocks.Contains(card.cardName))
                        {
                            data.upgradedHeroUnlocks.Add(card.cardName);
                            Debug.Log($"[MetaProgressionManager] ProcessRunEnd: unlocked upgraded version of '{card.cardName}'");
                        }
                    }
                }
            }
            else
            {
                data.totalRunsFailed++;
            }

            // Earn reputation
            int repEarned = CalculateReputation(victory, levelReached, bossesKilled);
            data.reputation += repEarned;
            Debug.Log($"[MetaProgressionManager] ProcessRunEnd: earned {repEarned} reputation (total={data.reputation})");

            // Update best stats
            if (levelReached > data.bestLevelReached)
                data.bestLevelReached = levelReached;
            if (resourcesGathered > data.bestResourcesInSingleRun)
                data.bestResourcesInSingleRun = resourcesGathered;
            if (bossesKilled > data.bestBossesKilledInSingleRun)
                data.bestBossesKilledInSingleRun = bossesKilled;
            if (victory && (data.fastestRunNodes == 0 || nodesVisited < data.fastestRunNodes))
                data.fastestRunNodes = nodesVisited;

            // Register discoveries for Scrapbook
            if (heroDeck != null)
            {
                foreach (var card in heroDeck)
                {
                    if (card != null) DiscoverCard(card.cardName);
                }
            }
            if (enemiesEncountered != null)
            {
                foreach (var enemy in enemiesEncountered)
                    DiscoverEnemy(enemy);
            }
            if (eventsEncountered != null)
            {
                foreach (var evt in eventsEncountered)
                    DiscoverEvent(evt);
            }
            if (bossesEncountered != null)
            {
                foreach (var boss in bossesEncountered)
                    DiscoverBoss(boss);
            }

            // Colony deck adjustment
            RecalculateColonyDeckBonus();

            // Recalculate completion percentages
            RecalculateCompletions();

            Save();
        }

        private int CalculateReputation(bool victory, int levelReached, int bossesKilled)
        {
            int rep = levelReached * 2; // 2 per level reached
            rep += bossesKilled * 3;    // 3 per boss killed
            if (victory) rep += 10;     // 10 bonus for completing a run
            return rep;
        }

        // --- Colony Deck Adjustment ---

        private void RecalculateColonyDeckBonus()
        {
            int newBonus = Mathf.Min(data.totalLevelsCleared / LEVELS_PER_DECK_BONUS, MAX_DECK_BONUS);
            if (newBonus != data.colonyDeckSizeBonus)
            {
                Debug.Log($"[MetaProgressionManager] RecalculateColonyDeckBonus: {data.colonyDeckSizeBonus} -> {newBonus}");
                data.colonyDeckSizeBonus = newBonus;
            }
        }

        // --- Colony Reputation Spending ---

        public bool TryUnlockStartingRelic(string relicName)
        {
            if (data.reputation < RELIC_UNLOCK_COST)
            {
                Debug.Log($"[MetaProgressionManager] TryUnlockStartingRelic: insufficient reputation ({data.reputation} < {RELIC_UNLOCK_COST})");
                return false;
            }
            if (data.unlockedStartingRelics.Contains(relicName))
            {
                Debug.Log($"[MetaProgressionManager] TryUnlockStartingRelic: '{relicName}' already unlocked");
                return false;
            }

            data.reputation -= RELIC_UNLOCK_COST;
            data.unlockedStartingRelics.Add(relicName);
            Debug.Log($"[MetaProgressionManager] TryUnlockStartingRelic: unlocked '{relicName}' (reputation remaining={data.reputation})");
            Save();
            return true;
        }

        public bool TryUnlockColonyCard(string cardName)
        {
            if (data.reputation < COLONY_CARD_UNLOCK_COST)
            {
                Debug.Log($"[MetaProgressionManager] TryUnlockColonyCard: insufficient reputation ({data.reputation} < {COLONY_CARD_UNLOCK_COST})");
                return false;
            }
            if (data.unlockedColonyCards.Contains(cardName))
            {
                Debug.Log($"[MetaProgressionManager] TryUnlockColonyCard: '{cardName}' already unlocked");
                return false;
            }

            data.reputation -= COLONY_CARD_UNLOCK_COST;
            data.unlockedColonyCards.Add(cardName);
            Debug.Log($"[MetaProgressionManager] TryUnlockColonyCard: unlocked '{cardName}' (reputation remaining={data.reputation})");
            Save();
            return true;
        }

        // --- Discovery (Scrapbook / Rattery / Bestiary) ---

        public void DiscoverCard(string cardName)
        {
            if (!data.discoveredCards.Contains(cardName))
            {
                data.discoveredCards.Add(cardName);
                Debug.Log($"[MetaProgressionManager] DiscoverCard: discovered '{cardName}' (total={data.discoveredCards.Count})");
            }
        }

        public void DiscoverEnemy(string enemyName)
        {
            if (!data.discoveredEnemies.Contains(enemyName))
            {
                data.discoveredEnemies.Add(enemyName);
                Debug.Log($"[MetaProgressionManager] DiscoverEnemy: discovered '{enemyName}' (total={data.discoveredEnemies.Count})");
            }
        }

        public void DiscoverEvent(string eventName)
        {
            if (!data.discoveredEvents.Contains(eventName))
            {
                data.discoveredEvents.Add(eventName);
                Debug.Log($"[MetaProgressionManager] DiscoverEvent: discovered '{eventName}' (total={data.discoveredEvents.Count})");
            }
        }

        public void DiscoverBoss(string bossName)
        {
            if (!data.discoveredBosses.Contains(bossName))
            {
                data.discoveredBosses.Add(bossName);
                Debug.Log($"[MetaProgressionManager] DiscoverBoss: discovered '{bossName}' (total={data.discoveredBosses.Count})");
            }
        }

        public void DiscoverRelic(string relicName)
        {
            if (!data.discoveredRelics.Contains(relicName))
            {
                data.discoveredRelics.Add(relicName);
                Debug.Log($"[MetaProgressionManager] DiscoverRelic: discovered '{relicName}' (total={data.discoveredRelics.Count})");
            }
        }

        public void DiscoverLore(string loreKey)
        {
            if (!data.discoveredLore.Contains(loreKey))
            {
                data.discoveredLore.Add(loreKey);
                Debug.Log($"[MetaProgressionManager] DiscoverLore: discovered '{loreKey}' (total={data.discoveredLore.Count})");
            }
        }

        // --- Queries ---

        public bool IsHeroUpgradeUnlocked(string heroName)
        {
            return data.upgradedHeroUnlocks.Contains(heroName);
        }

        public bool IsRelicUnlocked(string relicName)
        {
            return data.unlockedStartingRelics.Contains(relicName);
        }

        public bool IsColonyCardUnlocked(string cardName)
        {
            return data.unlockedColonyCards.Contains(cardName);
        }

        public bool IsEnemyDiscovered(string enemyName)
        {
            return data.discoveredEnemies.Contains(enemyName);
        }

        // --- Completion Calculation ---

        private void RecalculateCompletions()
        {
            // Scrapbook: total discoverable items (approximate counts for now)
            int totalDiscoverable = 80; // ~20 heroes + 12 equip + 20 colony + 10 benefits + 10 relics + 15 events + 4 bosses + ~8 enemies
            int discovered = data.discoveredCards.Count + data.discoveredRelics.Count +
                             data.discoveredEvents.Count + data.discoveredBosses.Count;
            data.scrapbookCompletion = Mathf.Min(100, Mathf.RoundToInt((float)discovered / totalDiscoverable * 100f));

            // Bestiary: total enemies
            int totalEnemies = 12; // 4 per level
            data.bestiaryCompletion = Mathf.Min(100, Mathf.RoundToInt((float)data.discoveredEnemies.Count / totalEnemies * 100f));

            Debug.Log($"[MetaProgressionManager] RecalculateCompletions: scrapbook={data.scrapbookCompletion}%, bestiary={data.bestiaryCompletion}%");
        }

        // --- Event Handlers ---

        private void OnRunComplete()
        {
            Debug.Log("[MetaProgressionManager] OnRunComplete: run completed — will be processed by RunManager calling ProcessRunEnd");
        }

        private void OnRunFailed()
        {
            Debug.Log("[MetaProgressionManager] OnRunFailed: run failed — will be processed by RunManager calling ProcessRunEnd");
        }

        private void OnBossDefeated()
        {
            Debug.Log("[MetaProgressionManager] OnBossDefeated: boss defeated event received");
        }

        // --- Persistence ---

        private void Save()
        {
            string json = JsonUtility.ToJson(data, false);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            Debug.Log($"[MetaProgressionManager] Save: meta-progression saved ({json.Length} chars)");
        }

        private void Load()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                data = JsonUtility.FromJson<MetaProgressionData>(json);
                Debug.Log($"[MetaProgressionManager] Load: loaded meta-progression (runs={data.totalRunsCompleted}, rep={data.reputation}, scrapbook={data.scrapbookCompletion}%)");
            }
            else
            {
                data = new MetaProgressionData();
                Debug.Log("[MetaProgressionManager] Load: no existing data — starting fresh");
            }
        }

        public void ResetAllProgress()
        {
            Debug.Log("[MetaProgressionManager] ResetAllProgress: clearing all meta-progression data");
            data = new MetaProgressionData();
            Save();
        }
    }
}
