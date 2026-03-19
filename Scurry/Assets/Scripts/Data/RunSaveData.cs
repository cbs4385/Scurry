using System.Collections.Generic;

namespace Scurry.Data
{
    [System.Serializable]
    public class RunSaveData
    {
        // Run metadata
        public int randomSeed;
        public int currentTurn;
        public int runState;
        public int difficulty; // DifficultyLevel as int

        // Deck (card IDs)
        public List<int> deckCardIds = new List<int>();
        public List<int> colonyDeckCardIds = new List<int>();

        // Deployed heroes
        public List<HeroSaveData> deployedHeroes = new List<HeroSaveData>();

        // Injured heroes (card IDs)
        public List<int> injuredHeroCardIds = new List<int>();

        // Available equipment (card IDs not yet deployed)
        public List<int> availableEquipmentIds = new List<int>();

        // Deployed equipment (card IDs on heroes)
        public List<int> deployedEquipmentIds = new List<int>();

        // Available tactical cards
        public List<int> availableTacticalIds = new List<int>();

        // Used tactical cards (removed from game)
        public List<int> usedTacticalIds = new List<int>();

        // Colony
        public List<ColonyCardSaveData> placedColonyCards = new List<ColonyCardSaveData>();
        public List<int> availableColonyCardIds = new List<int>();

        // Stockpile
        public int foodStockpile;
        public int materialsStockpile;
        public int currencyStockpile;

        // Map state (node states)
        public List<MapNodeSaveData> mapNodes = new List<MapNodeSaveData>();

        // Enemy state
        public List<EnemySaveData> enemies = new List<EnemySaveData>();

        // Mid-run card rewards (Phase 3)
        public List<int> acquiredCardIds = new List<int>();

        // Stats
        public int totalResourcesGathered;
        public int totalEnemiesDefeated;
        public int zoneBossesDefeated;
        public bool piedPiperDefeated;
        public int colonyCardsPlayed;
        public List<int> heroesEverInjuredIds = new List<int>();
    }

    [System.Serializable]
    public class HeroSaveData
    {
        public int cardId;
        public int tokenId;
        public int currentNodeId;
        public int targetNodeId;
        public int currentHP;
        public int offensiveEquipId;  // -1 if none
        public int defensiveEquipId;  // -1 if none
        public int utilityEquipId;    // -1 if none
        public List<ResourceAmount> carriedResources = new List<ResourceAmount>();

        public HeroSaveData()
        {
            offensiveEquipId = -1;
            defensiveEquipId = -1;
            utilityEquipId = -1;
        }
    }

    [System.Serializable]
    public class ResourceAmount
    {
        public int resourceType; // ResourceType as int
        public int amount;

        public ResourceAmount() { }

        public ResourceAmount(ResourceType type, int amt)
        {
            resourceType = (int)type;
            amount = amt;
        }
    }

    [System.Serializable]
    public class ColonyCardSaveData
    {
        public int cardId;
        public int placedId;
        public float posX;
        public float posY;
        public List<int> adjacentPlacedIds = new List<int>();
    }

    [System.Serializable]
    public class MapNodeSaveData
    {
        public int nodeId;
        public int zone; // NodeType as int
        public bool visited;
        public int fogState; // FogState as int
        public List<ResourceAmount> resources = new List<ResourceAmount>();
        public float posX;
        public float posY;
        public List<int> neighborIds = new List<int>();
    }

    [System.Serializable]
    public class EnemySaveData
    {
        public int tokenId;
        public int enemyDefId;
        public string enemyName;
        public int currentNodeId;
        public int currentHP;
        public bool isDefeated;
        public int respawnTimer;
    }
}
