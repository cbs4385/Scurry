using System.Collections.Generic;
using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewMapConfig", menuName = "Scurry/Map Config")]
    public class MapConfigSO : ScriptableObject
    {
        public string levelName;
        [Tooltip("Localization key prefix, e.g. 'level.wilderness'. Name = key+'.name'")]
        public string localizationKey;

        [Header("Level")]
        public int levelNumber = 1;

        [Header("Map Shape")]
        [Tooltip("Number of rows (depth) in the branching map")]
        public int numRows = 8;
        public int minNodesPerRow = 2;
        public int maxNodesPerRow = 3;

        [Header("Node Types")]
        public List<NodeTypeWeight> nodeTypeWeights = new List<NodeTypeWeight>();
        [Tooltip("First row always uses this node type")]
        public NodeType firstRowType = NodeType.ResourceEncounter;

        [Header("Boss")]
        public BossDefinitionSO bossDefinition;

        [Header("Encounter Pools")]
        public List<EncounterDefinitionSO> encounterPool = new List<EncounterDefinitionSO>();
        public List<EncounterDefinitionSO> eliteEncounterPool = new List<EncounterDefinitionSO>();

        [Header("Shop")]
        public List<CardDefinitionSO> shopCardPool = new List<CardDefinitionSO>();

        [Header("Events")]
        public List<EventDefinitionSO> eventPool = new List<EventDefinitionSO>();

        [Header("Board & Colony Sizes")]
        [Tooltip("Encounter board grid size (4=Wilderness, 5=Village, 6=Town)")]
        public int boardSize = 4;
        [Tooltip("Colony board grid size (3=Wilderness, 4=Village, 5=Town)")]
        public int colonyBoardSize = 3;
    }

    [System.Serializable]
    public class NodeTypeWeight
    {
        public NodeType nodeType;
        [Range(0f, 10f)]
        public float weight = 1f;
    }
}
