using System.Collections.Generic;
using Scurry.Colony;
using Scurry.Data;
using Scurry.Map;

namespace Scurry.Interfaces
{
    public interface IColonyGraph
    {
        IReadOnlyDictionary<int, PlacedColonyCard> PlacedCards { get; }
        int CardCount { get; }
        void Initialize();
        void Initialize(ColonyCardDefinitionSO entranceDef, ColonyCardDefinitionSO basicBurrowDef);
        void Initialize(MapGraph mapGraph);
        int AddCard(ColonyCardDefinitionSO card, int targetNodeId);
        IReadOnlyDictionary<int, PlacedColonyCard> GetPlacedCards();
        List<int> GetAdjacentCards(int nodeId);
        int CalculateFoodProduction();
        Dictionary<ColonyEffect, int> GetActiveEffects();
        bool HasEffect(ColonyEffect effect);
        int GetEffectValue(ColonyEffect effect);
        bool CanPlayCard();
        int CardsPlayedThisTurn { get; }
        void ResetTurnCardCount();
    }
}
