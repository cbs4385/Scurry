using System.Collections.Generic;
using Scurry.Data;

namespace Scurry.Interfaces
{
    public interface IRelicManager
    {
        IReadOnlyList<RelicDefinitionSO> ActiveRelics { get; }
        int RelicCount { get; }
        void AddRelic(RelicDefinitionSO relic);
        bool HasRelic(string relicName);
        int GetShopDiscount();
        int GetCombatBonus();
        int GetMoveBonus();
        int GetHPBonus();
    }
}
