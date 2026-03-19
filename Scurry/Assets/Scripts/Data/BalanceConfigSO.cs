using UnityEngine;
using Scurry.Interfaces;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "Scurry/Balance Config")]
    public class BalanceConfigSO : ScriptableObject, IBalanceConfig
    {
        private static BalanceConfigSO _instance;
        public static BalanceConfigSO Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<BalanceConfigSO>("BalanceConfig");
#if UNITY_EDITOR
                    if (_instance == null)
                    {
                        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:BalanceConfigSO"))
                        {
                            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                            _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<BalanceConfigSO>(path);
                            if (_instance != null) break;
                        }
                    }
#endif
                    if (_instance == null)
                        Debug.LogWarning("[BalanceConfigSO] Instance: no BalanceConfig asset found — using defaults");
                    else
                        Scurry.Core.ServiceLocator.Register<IBalanceConfig>(_instance);
                }
                return _instance;
            }
        }

        [Header("Economy")]
        [Tooltip("Base food stockpile at run start")]
        public int startingFood = 25;
        [Tooltip("Base materials stockpile at run start")]
        public int startingMaterials = 8;
        [Tooltip("Base currency stockpile at run start")]
        public int startingCurrency = 5;

        [Header("Starvation")]
        [Tooltip("HP damage per unpaid food unit")]
        public int starvationDamagePerFood = 2;

        [Header("Colony")]
        [Tooltip("Base colony HP")]
        public int baseColonyHP = 30;
        [Tooltip("Base colony max HP")]
        public int baseColonyMaxHP = 30;

        [Header("Combat")]
        [Tooltip("Difficulty scaling multiplier per difficulty point")]
        public float difficultyScalingFactor = 0.15f;
        [Tooltip("Per-hero combat bonus when 2 heroes fight together")]
        public int groupCombatBonus2Heroes = 2;
        [Tooltip("Per-hero combat bonus when 3 heroes fight together")]
        public int groupCombatBonus3Heroes = 3;
        [Tooltip("Per-hero combat bonus when 4+ heroes fight together")]
        public int groupCombatBonus4PlusHeroes = 4;

        [Header("Enemies")]
        [Tooltip("Base chance (0-1) that a non-boss enemy spawns per node")]
        public float enemySpawnChance = 0.65f;
        [Tooltip("Number of zone bosses that must be defeated before Pied Piper node is accessible")]
        public int bossesRequiredForPiper = 2;

        [Header("Colony Card Costs")]
        [Tooltip("Food cost to place a Food & Storage tier colony card")]
        public int colonyFoodStorageFoodCost = 1;
        [Tooltip("Materials cost to place a Food & Storage tier colony card")]
        public int colonyFoodStorageMaterialsCost = 1;
        [Tooltip("Food cost to place a Structure & Defense tier colony card")]
        public int colonyStructureDefenseFoodCost = 1;
        [Tooltip("Materials cost to place a Structure & Defense tier colony card")]
        public int colonyStructureDefenseMaterialsCost = 2;
        [Tooltip("Food cost to place an Advanced tier colony card")]
        public int colonyAdvancedFoodCost = 1;
        [Tooltip("Materials cost to place an Advanced tier colony card")]
        public int colonyAdvancedMaterialsCost = 2;

        [Header("Deployment")]
        [Tooltip("Max hero deploys per turn per difficulty: Easy, Normal, Hard")]
        public int[] maxHeroDeploysPerDifficulty = { 4, 3, 2 };

        [Header("Food Economy")]
        [Tooltip("Base food production per turn from colony")]
        public int baseFoodProduction = 5;
        [Tooltip("Food bonus per N heroes per difficulty: Easy, Normal, Hard (1 food per this many heroes)")]
        public int[] foodBonusPerNHeroesPerDifficulty = { 2, 3, 5 };
        [Tooltip("Heroes at colony node do not consume food")]
        public bool heroesAtColonyFreeFood = true;

        [Header("Difficulty")]
        [Tooltip("Current difficulty level")]
        public DifficultyLevel difficulty = DifficultyLevel.Normal;
        [Tooltip("Enemy strength multiplier per difficulty: Easy, Normal, Hard")]
        public float[] enemyStrengthMultipliers = { 0.5f, 1.0f, 1.5f };
        [Tooltip("Enemy spawn chance per difficulty: Easy, Normal, Hard")]
        public float[] enemySpawnChances = { 0.25f, 0.40f, 0.65f };
        [Tooltip("Easy mode: bonus starting food")]
        public int easyBonusFood = 10;
        [Tooltip("Easy mode: bonus starting materials")]
        public int easyBonusMaterials = 5;
        [Tooltip("Easy mode: bonus HP for all heroes")]
        public int easyBonusHeroHP = 2;
        [Tooltip("Easy mode: bonus combat for all heroes")]
        public int easyBonusHeroCombat = 1;

        /// <summary>Returns the max hero deploys per turn for the current difficulty.</summary>
        public int GetMaxHeroDeploysPerTurn()
        {
            int idx = (int)difficulty;
            if (idx >= 0 && idx < maxHeroDeploysPerDifficulty.Length)
                return maxHeroDeploysPerDifficulty[idx];
            return 2;
        }

        /// <summary>Returns the food bonus divisor (1 food per N heroes) for the current difficulty.</summary>
        public int GetFoodBonusPerNHeroes()
        {
            int idx = (int)difficulty;
            if (idx >= 0 && idx < foodBonusPerNHeroesPerDifficulty.Length)
                return foodBonusPerNHeroesPerDifficulty[idx];
            return 3;
        }

        /// <summary>Returns the enemy strength multiplier for the current difficulty.</summary>
        public float GetEnemyStrengthMultiplier()
        {
            int idx = (int)difficulty;
            if (idx >= 0 && idx < enemyStrengthMultipliers.Length)
                return enemyStrengthMultipliers[idx];
            return 1f;
        }

        /// <summary>Returns the enemy spawn chance for the current difficulty.</summary>
        public float GetEnemySpawnChance()
        {
            int idx = (int)difficulty;
            if (idx >= 0 && idx < enemySpawnChances.Length)
                return enemySpawnChances[idx];
            return enemySpawnChance;
        }

        // IBalanceConfig implementation
        int IBalanceConfig.StartingFood => startingFood;
        int IBalanceConfig.StartingMaterials => startingMaterials;
        int IBalanceConfig.StartingCurrency => startingCurrency;
        int IBalanceConfig.StarvationDamagePerFood => starvationDamagePerFood;
        int IBalanceConfig.BaseColonyHP => baseColonyHP;
        int IBalanceConfig.BaseColonyMaxHP => baseColonyMaxHP;
        float IBalanceConfig.DifficultyScalingFactor => difficultyScalingFactor;
        float IBalanceConfig.EnemySpawnChance => enemySpawnChance;
        int IBalanceConfig.BossesRequiredForPiper => bossesRequiredForPiper;
        int IBalanceConfig.GroupCombatBonus2 => groupCombatBonus2Heroes;
        int IBalanceConfig.GroupCombatBonus3 => groupCombatBonus3Heroes;
        int IBalanceConfig.GroupCombatBonus4Plus => groupCombatBonus4PlusHeroes;
        int IBalanceConfig.BaseFoodProduction => baseFoodProduction;
    }
}
