using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "Scurry/Card Definition")]
    public class CardDefinitionSO : ScriptableObject
    {
        public string cardName;
        public CardType cardType;
        public Sprite artwork;
        public Color placeholderColor = Color.white;

        [Header("Hero Stats")]
        public int movement;
        public int combat;
        public int carryCapacity;
        [TextArea] public string specialAbilityDescription;

        [Header("Resource Stats")]
        public ResourceType resourceType;
        public int value = 1;
    }
}