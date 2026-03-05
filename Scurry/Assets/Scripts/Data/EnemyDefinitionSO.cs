using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Scurry/Enemy Definition")]
    public class EnemyDefinitionSO : ScriptableObject
    {
        public string enemyName;
        [Tooltip("Localization key prefix, e.g. 'enemy.fieldmouse'. Name = key+'.name'")]
        public string localizationKey;
        public int strength;
        public int speed;
        public EnemyBehavior behavior;
        public Color tokenColor = new Color(1f, 0.2f, 0.2f);
        public Sprite artwork;
    }
}
