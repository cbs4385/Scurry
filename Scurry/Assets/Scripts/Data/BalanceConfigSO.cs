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

        [Header("Economy - Food")]
        [Tooltip("Base food stockpile at run start")]
        public int startingFood = 15;
        [Tooltip("Base materials stockpile at run start")]
        public int startingMaterials = 5;
        [Tooltip("Base currency stockpile at run start")]
        public int startingCurrency = 5;

        [Header("Economy - Starvation")]
        [Tooltip("HP damage per unpaid food unit")]
        public int starvationDamagePerFood = 2;

        [Header("Colony")]
        [Tooltip("Base colony HP")]
        public int baseColonyHP = 30;
        [Tooltip("Base colony max HP")]
        public int baseColonyMaxHP = 30;
        [Tooltip("Base hero deck size before colony bonuses")]
        public int baseHeroDeckSize = 8;

        [Header("Shop Prices")]
        public int priceCommon = 2;
        public int priceUncommon = 4;
        public int priceRare = 7;
        public int priceLegendary = 12;
        [Tooltip("Currency cost to reroll shop")]
        public int shopRerollCost = 2;
        [Tooltip("Number of cards offered in shop")]
        public int shopCardCount = 5;

        [Header("Upgrade Costs (Materials)")]
        public int upgradeCostCommon = 2;
        public int upgradeCostUncommon = 4;
        public int upgradeCostRare = 7;

        [Header("Healing Costs (Food)")]
        public int minorHealCost = 2;
        public int minorHealAmount = 5;
        public int majorHealCost = 5;
        public int majorHealAmount = 15;
        public int resupplyCost = 3;

        [Header("Rest Site")]
        [Tooltip("Percentage of max HP restored at rest site (0-100)")]
        [Range(0, 100)]
        public int restHealPercent = 30;

        [Header("Combat")]
        [Tooltip("Difficulty scaling multiplier per difficulty point")]
        public float difficultyScalingFactor = 0.15f;
        [Tooltip("Colony HP damage on boss fight failure")]
        public int bossFailureDamage = 10;

        [Header("Draft")]
        [Tooltip("Number of cards offered in card draft")]
        public int draftCardCount = 3;

        [Header("Colony Draft")]
        [Tooltip("Number of colony cards offered to choose from at run start")]
        public int colonyDraftOfferCount = 12;
        [Tooltip("Number of colony cards the player picks to form their colony deck")]
        public int colonyDraftPickCount = 8;

        [Header("Encounter Rewards")]
        [Tooltip("Bonus currency for completing an elite encounter")]
        public int eliteBonusCurrency = 3;

        public int GetShopPrice(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return priceCommon;
                case CardRarity.Uncommon: return priceUncommon;
                case CardRarity.Rare: return priceRare;
                case CardRarity.Legendary: return priceLegendary;
                default: return priceCommon;
            }
        }

        public int GetUpgradeCost(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return upgradeCostCommon;
                case CardRarity.Uncommon: return upgradeCostUncommon;
                case CardRarity.Rare: return upgradeCostRare;
                default: return upgradeCostRare;
            }
        }

        // Explicit IBalanceConfig implementation (fields -> properties)
        int IBalanceConfig.StartingFood => startingFood;
        int IBalanceConfig.StartingMaterials => startingMaterials;
        int IBalanceConfig.StartingCurrency => startingCurrency;
        int IBalanceConfig.StarvationDamagePerFood => starvationDamagePerFood;
        int IBalanceConfig.BaseColonyHP => baseColonyHP;
        int IBalanceConfig.BaseColonyMaxHP => baseColonyMaxHP;
        int IBalanceConfig.BaseHeroDeckSize => baseHeroDeckSize;
        int IBalanceConfig.PriceCommon => priceCommon;
        int IBalanceConfig.PriceUncommon => priceUncommon;
        int IBalanceConfig.PriceRare => priceRare;
        int IBalanceConfig.PriceLegendary => priceLegendary;
        int IBalanceConfig.ShopRerollCost => shopRerollCost;
        int IBalanceConfig.ShopCardCount => shopCardCount;
        int IBalanceConfig.UpgradeCostCommon => upgradeCostCommon;
        int IBalanceConfig.UpgradeCostUncommon => upgradeCostUncommon;
        int IBalanceConfig.UpgradeCostRare => upgradeCostRare;
        int IBalanceConfig.MinorHealCost => minorHealCost;
        int IBalanceConfig.MinorHealAmount => minorHealAmount;
        int IBalanceConfig.MajorHealCost => majorHealCost;
        int IBalanceConfig.MajorHealAmount => majorHealAmount;
        int IBalanceConfig.ResupplyCost => resupplyCost;
        int IBalanceConfig.RestHealPercent => restHealPercent;
        float IBalanceConfig.DifficultyScalingFactor => difficultyScalingFactor;
        int IBalanceConfig.BossFailureDamage => bossFailureDamage;
        int IBalanceConfig.DraftCardCount => draftCardCount;
        int IBalanceConfig.ColonyDraftOfferCount => colonyDraftOfferCount;
        int IBalanceConfig.ColonyDraftPickCount => colonyDraftPickCount;
        int IBalanceConfig.EliteBonusCurrency => eliteBonusCurrency;
    }
}
