using System.Collections.Generic;
using UnityEngine;

namespace Scurry.Data
{
    /// <summary>
    /// Runtime enemy database that loads all enemy definitions from EnemyDatabase.json.
    /// Use EnemyDatabase.Instance to access enemies by ID.
    /// </summary>
    public class EnemyDatabase
    {
        private static EnemyDatabase _instance;
        public static EnemyDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EnemyDatabase();
                    _instance.Load();
                }
                return _instance;
            }
        }

        private Dictionary<int, EnemyDefinitionSO> _enemies = new Dictionary<int, EnemyDefinitionSO>();
        private List<EnemyDefinitionSO> _allEnemies = new List<EnemyDefinitionSO>();

        public IReadOnlyDictionary<int, EnemyDefinitionSO> Enemies => _enemies;
        public IReadOnlyList<EnemyDefinitionSO> AllEnemies => _allEnemies;

        /// <summary>
        /// Gets an enemy definition by its index in the database.
        /// </summary>
        public EnemyDefinitionSO GetEnemy(int enemyId)
        {
            if (_enemies.TryGetValue(enemyId, out var enemy))
            {
                Debug.Log($"[EnemyDatabase] GetEnemy: found (enemyId={enemyId}, name={enemy.enemyName})");
                return enemy;
            }
            Debug.LogWarning($"[EnemyDatabase] GetEnemy: enemyId={enemyId} not found");
            return null;
        }

        /// <summary>
        /// Gets all enemies belonging to a specific zone.
        /// </summary>
        public List<EnemyDefinitionSO> GetEnemiesByZone(NodeType zone)
        {
            var result = new List<EnemyDefinitionSO>();
            foreach (var enemy in _allEnemies)
            {
                if (enemy.homeZone == zone)
                    result.Add(enemy);
            }
            Debug.Log($"[EnemyDatabase] GetEnemiesByZone: zone={zone}, found={result.Count}");
            return result;
        }

        /// <summary>
        /// Gets all enemies with a specific behavior type.
        /// </summary>
        public List<EnemyDefinitionSO> GetEnemiesByBehavior(EnemyBehavior behavior)
        {
            var result = new List<EnemyDefinitionSO>();
            foreach (var enemy in _allEnemies)
            {
                if (enemy.behavior == behavior)
                    result.Add(enemy);
            }
            Debug.Log($"[EnemyDatabase] GetEnemiesByBehavior: behavior={behavior}, found={result.Count}");
            return result;
        }

        /// <summary>
        /// Gets all boss enemies (enemies with homeZone matching their zone but high stats).
        /// Bosses are identified by having strength >= 8.
        /// </summary>
        public List<EnemyDefinitionSO> GetBosses()
        {
            var result = new List<EnemyDefinitionSO>();
            foreach (var enemy in _allEnemies)
            {
                if (enemy.strength >= 8)
                    result.Add(enemy);
            }
            Debug.Log($"[EnemyDatabase] GetBosses: found={result.Count}");
            return result;
        }

        /// <summary>
        /// Gets regular (non-boss) enemies for a zone.
        /// </summary>
        public List<EnemyDefinitionSO> GetRegularEnemiesByZone(NodeType zone)
        {
            var result = new List<EnemyDefinitionSO>();
            foreach (var enemy in _allEnemies)
            {
                if (enemy.homeZone == zone && enemy.strength < 8)
                    result.Add(enemy);
            }
            Debug.Log($"[EnemyDatabase] GetRegularEnemiesByZone: zone={zone}, found={result.Count}");
            return result;
        }

        private void Load()
        {
            Debug.Log("[EnemyDatabase] Load: loading EnemyDatabase.json from Resources");
            var json = Resources.Load<TextAsset>("EnemyDatabase");
            if (json == null)
            {
                Debug.LogError("[EnemyDatabase] Load: EnemyDatabase.json not found in Resources!");
                return;
            }

            var db = JsonUtility.FromJson<EnemyDatabaseJson>(json.text);
            if (db == null)
            {
                Debug.LogError("[EnemyDatabase] Load: failed to parse EnemyDatabase.json");
                return;
            }

            if (db.enemies != null)
            {
                for (int i = 0; i < db.enemies.Length; i++)
                {
                    var e = db.enemies[i];
                    var so = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
                    so.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    so.enemyName = e.enemyName;
                    so.localizationKey = $"enemy.{e.enemyName.ToLower().Replace(" ", "_").Replace("&", "and")}";
                    so.strength = e.strength;
                    so.hp = e.hp;
                    so.speed = e.speed;
                    so.behavior = ParseBehavior(e.behavior);
                    so.homeZone = ParseZone(e.homeZone);
                    so.description = e.description;
                    so.tokenColor = ParseColor(e.tokenColor);

                    int id = e.enemyId >= 0 ? e.enemyId : i;
                    _enemies[id] = so;
                    _allEnemies.Add(so);

                    Debug.Log($"[EnemyDatabase] Load: loaded '{e.enemyName}' (id={id}, str={e.strength}, hp={e.hp}, spd={e.speed}, behavior={e.behavior}, zone={e.homeZone})");
                }
            }

            Debug.Log($"[EnemyDatabase] Load: complete. Enemies={_enemies.Count}");
        }

        private static EnemyBehavior ParseBehavior(string s)
        {
            return s switch
            {
                "Patrol" => EnemyBehavior.Patrol,
                "Chase" => EnemyBehavior.Chase,
                "Ambush" => EnemyBehavior.Ambush,
                "Guard" => EnemyBehavior.Guard,
                _ => EnemyBehavior.Patrol
            };
        }

        private static NodeType ParseZone(string s)
        {
            return s switch
            {
                "Wilderness" => NodeType.Wilderness,
                "Farmland" => NodeType.Farmland,
                "Town" => NodeType.Town,
                "Colony" => NodeType.Colony,
                "PiedPiper" => NodeType.PiedPiper,
                _ => NodeType.Wilderness
            };
        }

        private static Color ParseColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return new Color(1f, 0.2f, 0.2f);
            if (ColorUtility.TryParseHtmlString(hex, out var color))
                return color;
            Debug.LogWarning($"[EnemyDatabase] ParseColor: invalid hex '{hex}', using default red");
            return new Color(1f, 0.2f, 0.2f);
        }

        /// <summary>
        /// Resets the singleton instance. Useful for testing.
        /// </summary>
        public static void ResetInstance()
        {
            Debug.Log("[EnemyDatabase] ResetInstance: clearing singleton");
            _instance = null;
        }

        // --- JSON data classes ---

        [System.Serializable]
        private class EnemyDatabaseJson
        {
            public EnemyEntryJson[] enemies;
        }

        [System.Serializable]
        private class EnemyEntryJson
        {
            public int enemyId;
            public string enemyName;
            public int strength;
            public int hp;
            public int speed;
            public string behavior;
            public string homeZone;
            public string description;
            public string tokenColor;
        }
    }
}
