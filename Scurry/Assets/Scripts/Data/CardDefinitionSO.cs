using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "Scurry/Card Definition")]
    public class CardDefinitionSO : ScriptableObject
    {
        public string cardName;
        [Tooltip("Localization key prefix, e.g. 'card.scoutrat'. Name = key+'.name', ability = key+'.ability'")]
        public string localizationKey;
        public CardType cardType;
        public CardRarity rarity = CardRarity.Common;
        public Sprite artwork;
        public Color placeholderColor = Color.white;

        [Header("Hero Stats")]
        public int movement;
        public int combat;
        public int carryCapacity;
        [TextArea] public string specialAbilityDescription;
        public SpecialAbility specialAbility;

        [Header("Resource Stats")]
        public ResourceType resourceType;
        public int value = 1;
    }
}