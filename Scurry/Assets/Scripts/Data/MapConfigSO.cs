using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "MapConfig", menuName = "Scurry/Map Config")]
    public class MapConfigSO : ScriptableObject
    {
        [Header("Graph Structure")]
        [Tooltip("Number of nodes per zone (Wilderness, Farmland, Town)")]
        public int nodesPerZone = 10;

        [Tooltip("Minimum edges per node within its zone")]
        public int minEdgesPerNode = 2;

        [Tooltip("Maximum edges per node within its zone")]
        public int maxEdgesPerNode = 4;

        [Tooltip("Number of edges connecting adjacent zones")]
        public int crossZoneEdges = 3;

        [Tooltip("Number of edges from Colony to Wilderness")]
        public int colonyConnections = 3;

        [Tooltip("Number of edges from PiedPiper to Town")]
        public int piperConnections = 3;

        [Header("Resources - Wilderness")]
        [Tooltip("Minimum total resources per Wilderness node")]
        public int wildernessResourceMin = 2;
        [Tooltip("Maximum total resources per Wilderness node")]
        public int wildernessResourceMax = 4;

        [Header("Resources - Farmland")]
        [Tooltip("Minimum total resources per Farmland node")]
        public int farmlandResourceMin = 1;
        [Tooltip("Maximum total resources per Farmland node")]
        public int farmlandResourceMax = 3;

        [Header("Resources - Town")]
        [Tooltip("Minimum total resources per Town node")]
        public int townResourceMin = 0;
        [Tooltip("Maximum total resources per Town node")]
        public int townResourceMax = 2;

        [Header("Boss Definition")]
        public BossDefinitionSO bossDefinition;
    }
}
