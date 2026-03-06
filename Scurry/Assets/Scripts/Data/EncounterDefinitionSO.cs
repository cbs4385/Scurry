using System.Collections.Generic;
using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewEncounter", menuName = "Scurry/Encounter Definition")]
    public class EncounterDefinitionSO : ScriptableObject
    {
        public string encounterName;
        [Tooltip("Localization key prefix, e.g. 'encounter.meadow1'. Name = key+'.name'")]
        public string localizationKey;

        public EncounterType encounterType = EncounterType.Resource;
        public BoardLayoutSO boardLayout;

        [Header("Enemies")]
        public List<EnemySpawnEntry> enemySpawns = new List<EnemySpawnEntry>();

        [Header("Resources")]
        public List<ResourceNodeEntry> resourceNodes = new List<ResourceNodeEntry>();

        [Header("Difficulty")]
        [Range(1, 10)]
        public int difficulty = 1;

        [Header("Rules")]
        public bool allowRecall = true;

        [Header("Rewards (elite/boss only)")]
        public List<CardDefinitionSO> rewardCards = new List<CardDefinitionSO>();
        public RelicDefinitionSO rewardRelic;
    }

    [System.Serializable]
    public class EnemySpawnEntry
    {
        public EnemyDefinitionSO enemyDefinition;
        public Vector2Int gridPosition;
    }

    [System.Serializable]
    public class ResourceNodeEntry
    {
        public ResourceType resourceType = ResourceType.Food;
        public int value = 1;
        public Vector2Int gridPosition;
    }
}
