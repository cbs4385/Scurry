using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewTile", menuName = "Scurry/Tile Definition")]
    public class TileDefinitionSO : ScriptableObject
    {
        public TileType tileType;
        public Color tileColor = Color.white;
        public int enemyStrength;
        public int hazardDamage;
    }
}