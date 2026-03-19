using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewColonyCard", menuName = "Scurry/Colony Card Definition")]
    public class ColonyCardDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public int cardId;
        public string cardName;
        [Tooltip("Localization key prefix, e.g. 'colony.underground_storage'")]
        public string localizationKey;
        [TextArea] public string description;
        public CardRarity rarity = CardRarity.Common;
        [Tooltip("Deck cost: 1=up to 3 copies, 2=up to 2 copies, 3+=singleton")]
        public int deckCost = 1;

        [Header("Colony Tier")]
        public ColonyTier colonyTier;

        [Header("Placement")]
        public PlacementRequirement placementRequirement = PlacementRequirement.None;
        [Tooltip("Card name that must be adjacent (only used when placementRequirement is AdjacentTo)")]
        public string adjacencyCardName;

        [Header("Colony Effect")]
        public ColonyEffect colonyEffect;
        public int effectValue;
        [Tooltip("Whether this is a starter card (free, always in deck)")]
        public bool isStarter;

        [Header("Visuals")]
        public Sprite artwork;
        public Color placeholderColor = new Color(0.6f, 0.4f, 0.2f);

    }
}
