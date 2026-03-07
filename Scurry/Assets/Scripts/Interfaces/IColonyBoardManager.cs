using System.Collections.Generic;
using UnityEngine;
using Scurry.Colony;
using Scurry.Data;

namespace Scurry.Interfaces
{
    public interface IColonyBoardManager
    {
        int BoardSize { get; }
        int CurrentHandIndex { get; }
        int TotalHands { get; }
        bool HasCardsInHand { get; }
        bool HasMoreHands { get; }
        List<ColonyCardDefinitionSO> CurrentHand { get; }
        ColonyConfig CalculatedConfig { get; }
        ColonyCardDefinitionSO GetCardAt(Vector2Int pos);
        bool TryPlaceCard(ColonyCardDefinitionSO card, Vector2Int pos);
        ColonyCardDefinitionSO RemoveCard(Vector2Int pos);
        bool IsValidPlacement(ColonyCardDefinitionSO card, Vector2Int pos);
        ColonyConfig CalculateColonyEffects();
        List<ColonyCardDefinitionSO> DrawHand();
        void FinishColonyManagement();
    }
}
