using System.Collections.Generic;

namespace Scurry.Data
{
    [System.Serializable]
    public class MetaProgressionData
    {
        // Colony Deck Adjustment
        public int totalLevelsCleared;
        public int totalRunsCompleted;
        public int totalRunsFailed;
        public int totalResourcesGathered;
        public int totalBossesDefeated;
        public int colonyDeckSizeBonus; // Earned through performance

        // Colony Reputation
        public int reputation;
        public List<string> unlockedStartingRelics = new List<string>();
        public List<string> unlockedColonyCards = new List<string>();

        // The Rattery (Card Unlocks)
        public List<string> discoveredHeroCards = new List<string>();
        public List<string> upgradedHeroUnlocks = new List<string>(); // Heroes used in winning runs
        public List<string> discoveredEnemies = new List<string>(); // Bestiary
        public int bestiaryCompletion; // Percentage (0-100)

        // The Scrapbook
        public List<string> discoveredCards = new List<string>();
        public List<string> discoveredRelics = new List<string>();
        public List<string> discoveredEvents = new List<string>();
        public List<string> discoveredBosses = new List<string>();
        public List<string> discoveredLore = new List<string>();
        public int scrapbookCompletion; // Percentage (0-100)

        // Best Run Stats
        public int bestLevelReached;
        public int bestResourcesInSingleRun;
        public int bestBossesKilledInSingleRun;
        public int fastestRunNodes; // Fewest nodes to complete a run
    }
}
