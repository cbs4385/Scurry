using System.Collections.Generic;

namespace Scurry.Data
{
    [System.Serializable]
    public class RunSaveData
    {
        // Run state
        public int currentLevel;
        public int runState; // RunState enum as int
        public int nodesVisited;

        // Colony HP
        public int colonyHP;
        public int colonyMaxHP;

        // Stockpiles
        public int foodStockpile;
        public int materialsStockpile;
        public int currencyStockpile;

        // Decks (stored as card names for SO lookup)
        public List<string> heroDeckCardNames = new List<string>();
        public List<string> colonyDeckCardNames = new List<string>();

        // Wound tracking
        public List<string> woundedHeroNames = new List<string>();
        public List<string> exhaustedHeroNames = new List<string>();

        // Map state
        public int mapCurrentRow;
        public int mapCurrentCol;
        public List<MapNodeSaveData> mapNodes = new List<MapNodeSaveData>();

        // Colony config (cached from colony management)
        public ColonyConfigSaveData colonyConfig;

        // Relics
        public List<string> activeRelicNames = new List<string>();

        // Score
        public int encountersCompleted;
        public int totalResourcesGathered;
        public int enemiesDefeated;
        public int bossesKilled;
    }

    [System.Serializable]
    public class MapNodeSaveData
    {
        public int row;
        public int col;
        public int nodeType; // NodeType enum as int
        public bool visited;
        public List<int> connectedIndices = new List<int>();
        public int difficulty;
        public string encounterName; // for SO lookup
    }

    [System.Serializable]
    public class ColonyConfigSaveData
    {
        public int maxHeroDeckSize;
        public int foodConsumptionPerNode;
        public int heroCombatBonus;
        public int heroMoveBonus;
        public int heroCarryBonus;
        public int totalPopulation;
        public int bonusStartingFood;
    }
}
