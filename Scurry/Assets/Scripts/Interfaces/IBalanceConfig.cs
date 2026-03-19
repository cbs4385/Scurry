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
        float DifficultyScalingFactor { get; }
        float EnemySpawnChance { get; }
        int BossesRequiredForPiper { get; }
        int GroupCombatBonus2 { get; }
        int GroupCombatBonus3 { get; }
        int GroupCombatBonus4Plus { get; }
        int BaseFoodProduction { get; }
    }
}
