using Scurry.Data;

namespace Scurry.Interfaces
{
    public interface IBalanceConfig
    {
        int StartingFood { get; }
        int StartingMaterials { get; }
        int StartingCurrency { get; }
        int StarvationDamagePerFood { get; }
        int BaseColonyHP { get; }
        int BaseColonyMaxHP { get; }
        int BaseHeroDeckSize { get; }
        int PriceCommon { get; }
        int PriceUncommon { get; }
        int PriceRare { get; }
        int PriceLegendary { get; }
        int ShopRerollCost { get; }
        int ShopCardCount { get; }
        int UpgradeCostCommon { get; }
        int UpgradeCostUncommon { get; }
        int UpgradeCostRare { get; }
        int MinorHealCost { get; }
        int MinorHealAmount { get; }
        int MajorHealCost { get; }
        int MajorHealAmount { get; }
        int ResupplyCost { get; }
        int RestHealPercent { get; }
        float DifficultyScalingFactor { get; }
        int BossFailureDamage { get; }
        int DraftCardCount { get; }
        int ColonyDraftOfferCount { get; }
        int ColonyDraftPickCount { get; }
        int EliteBonusCurrency { get; }
        int GetShopPrice(CardRarity rarity);
        int GetUpgradeCost(CardRarity rarity);
    }
}
