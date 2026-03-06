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

        [Header("Resource Stats (legacy M0 — used for tile resource data)")]
        public ResourceType resourceType;
        public int value = 1;

        [Header("Equipment Stats (M1)")]
        public EquipmentSlot equipmentSlot;
        public int equipmentBonusValue;

        [Header("Hero Benefit Stats (M1)")]
        public BenefitTrigger benefitTrigger;
        public int benefitValue;
        [TextArea] public string benefitDescription;

        [Header("Upgrade State")]
        public bool upgraded;

        public void Upgrade()
        {
            if (upgraded)
            {
                Debug.Log($"[CardDefinitionSO] Upgrade: '{cardName}' already upgraded — skipping");
                return;
            }
            upgraded = true;
            switch (cardType)
            {
                case CardType.Hero:
                    combat += 1;
                    Debug.Log($"[CardDefinitionSO] Upgrade: '{cardName}' hero combat {combat - 1} -> {combat}");
                    break;
                case CardType.Equipment:
                    equipmentBonusValue += 1;
                    Debug.Log($"[CardDefinitionSO] Upgrade: '{cardName}' equipment bonus {equipmentBonusValue - 1} -> {equipmentBonusValue}");
                    break;
                case CardType.HeroBenefit:
                    benefitValue += 1;
                    Debug.Log($"[CardDefinitionSO] Upgrade: '{cardName}' benefit value {benefitValue - 1} -> {benefitValue}");
                    break;
                default:
                    Debug.Log($"[CardDefinitionSO] Upgrade: '{cardName}' type={cardType} — no stat to upgrade");
                    break;
            }
        }
    }
}
