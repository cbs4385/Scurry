using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewBoss", menuName = "Scurry/Boss Definition")]
    public class BossDefinitionSO : ScriptableObject
    {
        public string bossName;
        [Tooltip("Localization key prefix, e.g. 'boss.eldersilas'. Name = key+'.name', desc = key+'.desc'")]
        public string localizationKey;
        public int maxHP;
        public int baseAttack;
        public BossPhase[] phases;
        public CardDefinitionSO[] rewardCards;
        public RelicDefinitionSO rewardRelic;
        public Sprite artwork;
        public Color placeholderColor = new Color(0.6f, 0.2f, 0.8f);
    }

    [System.Serializable]
    public class BossPhase
    {
        [Tooltip("Phase activates when boss HP drops to or below this value")]
        public int hpThreshold;
        public BossAbility ability;
        public int abilityValue;
        [Tooltip("Localization key for phase description, e.g. 'boss.eldersilas.phase1'")]
        public string localizationKey;
    }
}
