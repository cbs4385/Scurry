using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewZone", menuName = "Scurry/Zone Definition")]
    public class ZoneSO : ScriptableObject
    {
        public string zoneName;
        [Tooltip("Localization key prefix, e.g. 'zone.wilds'. Name = key+'.name', desc = key+'.desc'")]
        public string localizationKey;

        [Header("Board")]
        public int gridSize = 4;
        public BoardLayoutSO[] stageLayouts;

        [Header("Stage Structure")]
        public int stagesPerZone = 3;
        public int minStepsPerStage = 3;
        public int maxStepsPerStage = 5;
        public StepType[] stepPool;
        [Tooltip("Parallel array with stepPool — relative weights for procedural step selection")]
        public float[] stepWeights;

        [Header("Cards & Enemies")]
        public CardDefinitionSO[] zoneCardPool;
        public BossDefinitionSO zoneBoss;
    }
}
