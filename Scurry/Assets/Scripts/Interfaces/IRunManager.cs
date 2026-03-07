using System.Collections.Generic;
using Scurry.Data;

namespace Scurry.Interfaces
{
    public interface IRunManager
    {
        int CurrentLevel { get; }
        int FoodStockpile { get; }
        int MaterialsStockpile { get; }
        int CurrencyStockpile { get; }
        List<ColonyCardDefinitionSO> ColonyCardPool { get; }
        ZoneSO CurrentZone { get; }
        int CurrentStageIndex { get; }
        int CurrentStepIndex { get; }
        RunState CurrentRunState { get; }
        void StartRun();
        void ContinueRun();
    }
}
