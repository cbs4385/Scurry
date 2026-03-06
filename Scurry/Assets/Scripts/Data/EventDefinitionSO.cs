using System.Collections.Generic;
using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewEvent", menuName = "Scurry/Event Definition")]
    public class EventDefinitionSO : ScriptableObject
    {
        public string eventName;
        [Tooltip("Localization key prefix, e.g. 'event.wanderer'. Name = key+'.name', desc = key+'.desc'")]
        public string localizationKey;
        [TextArea(3, 6)] public string description;

        public List<EventChoice> choices = new List<EventChoice>();
    }

    [System.Serializable]
    public class EventChoice
    {
        [Tooltip("Localization key for choice button text")]
        public string choiceTextKey;
        public EventOutcomeType outcomeType;
        public int outcomeValue;
        [Tooltip("Localization key for outcome description shown after selection")]
        public string outcomeDescriptionKey;
    }

    public enum EventOutcomeType
    {
        GainFood,
        GainMaterials,
        GainCurrency,
        LoseFood,
        LoseMaterials,
        LoseCurrency,
        GainColonyHP,
        LoseColonyHP,
        GainCard,
        LoseCard,
        GainRelic,
        WoundRandomHero
    }
}
