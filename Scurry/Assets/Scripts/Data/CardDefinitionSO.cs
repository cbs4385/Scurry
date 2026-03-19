using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "Scurry/Card Definition")]
    public class CardDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public int cardId;
        public string cardName;
        [Tooltip("Localization key prefix, e.g. 'card.scout'. Name = key+'.name', ability = key+'.ability'")]
        public string localizationKey;
        public CardType cardType;
        public CardRarity rarity = CardRarity.Common;
        [Tooltip("Deck cost: 1=up to 3 copies, 2=up to 2 copies, 3+=singleton")]
        public int deckCost = 1;

        [Header("Visuals")]
        public Sprite artwork;
        public Color placeholderColor = Color.white;

        [Header("Hero Stats (cardType=Hero)")]
        public HeroRole heroRole;
        public int combat;
        public int move;
        public int hp;
        public int carry;
        [Tooltip("Tie-breaker for damage: lower initiative takes damage first")]
        public int initiative;
        public SpecialAbility specialAbility;
        [TextArea] public string specialAbilityDescription;

        [Header("Equipment Stats (cardType=Equipment)")]
        public EquipmentSlot equipmentSlot;
        [TextArea] public string equipmentEffectDescription;

        [Header("Tactical Stats (cardType=Tactical)")]
        public TacticalType tacticalType;
        [TextArea] public string tacticalEffectDescription;

        [Header("Shared Effect Values")]
        [Tooltip("Primary numeric effect (e.g. +N Combat, +N HP, heal N)")]
        public int effectValue1;
        [Tooltip("Secondary numeric effect (e.g. +N Move alongside +N Combat)")]
        public int effectValue2;
        [Tooltip("Tertiary numeric effect")]
        public int effectValue3;
    }
}
