using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewColonyCard", menuName = "Scurry/Colony Card Definition")]
    public class ColonyCardDefinitionSO : ScriptableObject
    {
        public string cardName;
        [Tooltip("Localization key prefix, e.g. 'colonycard.burrow'. Name = key+'.name', desc = key+'.desc'")]
        public string localizationKey;
        [TextArea] public string description;

        public CardRarity rarity = CardRarity.Common;

        [Header("Placement")]
        public PlacementRequirement placementRequirement = PlacementRequirement.None;
        [Tooltip("Card name that must be adjacent (only used when placementRequirement is AdjacentTo)")]
        public string adjacencyCardName;

        [Header("Colony Effect")]
        public ColonyEffect colonyEffect;
        public int effectValue;

        [Header("Population")]
        [Tooltip("Food consumption contribution when placed")]
        public int populationCost = 1;

        [Header("Visuals")]
        public Sprite artwork;
        public Color placeholderColor = new Color(0.6f, 0.4f, 0.2f);

        [Header("Upgrade State")]
        public bool upgraded;

        public void Upgrade()
        {
            if (upgraded)
            {
                Debug.Log($"[ColonyCardDefinitionSO] Upgrade: '{cardName}' already upgraded — skipping");
                return;
            }
            upgraded = true;
            effectValue += 1;
            Debug.Log($"[ColonyCardDefinitionSO] Upgrade: '{cardName}' effectValue {effectValue - 1} -> {effectValue}");
        }
    }
}
