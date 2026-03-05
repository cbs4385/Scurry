using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewRelic", menuName = "Scurry/Relic Definition")]
    public class RelicDefinitionSO : ScriptableObject
    {
        public string relicName;
        [Tooltip("Localization key prefix, e.g. 'relic.feather'. Name = key+'.name', desc = key+'.desc'")]
        public string localizationKey;
        public RelicEffect effect;
        public int effectValue = 1;
        public Sprite artwork;
        public Color placeholderColor = Color.cyan;
    }
}
