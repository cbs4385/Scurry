using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Core;
using Scurry.Interfaces;

namespace Scurry.Colony
{
    public class ColonyBoardManager : MonoBehaviour, IColonyBoardManager
    {
        private int boardSize;
        private ColonyCardDefinitionSO[,] grid;
        private List<ColonyCardDefinitionSO> colonyDeck;
        private List<ColonyCardDefinitionSO> currentHand;
        private int currentHandIndex;
        private int totalHands;
        private ColonyConfig calculatedConfig;

        public int BoardSize => boardSize;
        public int CurrentHandIndex => currentHandIndex;
        public int TotalHands => totalHands;
        public ColonyConfig CalculatedConfig => calculatedConfig;

        public void StartColonyManagement(int level, MapConfigSO config, List<ColonyCardDefinitionSO> deck)
        {
            boardSize = config.colonyBoardSize;
            grid = new ColonyCardDefinitionSO[boardSize, boardSize];
            colonyDeck = new List<ColonyCardDefinitionSO>(deck);
            totalHands = level;
            currentHandIndex = 0;
            currentHand = new List<ColonyCardDefinitionSO>();

            Debug.Log($"[ColonyBoardManager] StartColonyManagement: level={level}, boardSize={boardSize}, deckSize={colonyDeck.Count}, hands={totalHands}");

            // Shuffle colony deck
            ShuffleDeck();

            // Draw first hand
            DrawHand();
        }

        private void ShuffleDeck()
        {
            for (int i = colonyDeck.Count - 1; i > 0; i--)
            {
                int j = SeededRandom.Range(0, i + 1);
                var temp = colonyDeck[i];
                colonyDeck[i] = colonyDeck[j];
                colonyDeck[j] = temp;
            }
            Debug.Log($"[ColonyBoardManager] ShuffleDeck: shuffled {colonyDeck.Count} cards");
        }

        public List<ColonyCardDefinitionSO> DrawHand()
        {
            currentHand.Clear();
            int cardsToDraw = Mathf.Min(5, colonyDeck.Count);
            for (int i = 0; i < cardsToDraw; i++)
            {
                currentHand.Add(colonyDeck[0]);
                colonyDeck.RemoveAt(0);
            }
            currentHandIndex++;
            Debug.Log($"[ColonyBoardManager] DrawHand: hand {currentHandIndex}/{totalHands}, drew {currentHand.Count} cards, remaining deck={colonyDeck.Count}");
            return new List<ColonyCardDefinitionSO>(currentHand);
        }

        public bool TryPlaceCard(ColonyCardDefinitionSO card, Vector2Int pos)
        {
            Debug.Log($"[ColonyBoardManager] TryPlaceCard: card='{card.cardName}', pos={pos}");

            if (pos.x < 0 || pos.x >= boardSize || pos.y < 0 || pos.y >= boardSize)
            {
                Debug.Log($"[ColonyBoardManager] TryPlaceCard: REJECTED — position out of bounds");
                return false;
            }

            if (grid[pos.x, pos.y] != null)
            {
                Debug.Log($"[ColonyBoardManager] TryPlaceCard: REJECTED — slot already occupied by '{grid[pos.x, pos.y].cardName}'");
                return false;
            }

            if (!IsValidPlacement(card, pos))
            {
                Debug.Log($"[ColonyBoardManager] TryPlaceCard: REJECTED — placement requirement not met");
                return false;
            }

            grid[pos.x, pos.y] = card;
            currentHand.Remove(card);
            Debug.Log($"[ColonyBoardManager] TryPlaceCard: PLACED '{card.cardName}' at ({pos.x},{pos.y}), hand remaining={currentHand.Count}");
            return true;
        }

        public ColonyCardDefinitionSO RemoveCard(Vector2Int pos)
        {
            if (pos.x < 0 || pos.x >= boardSize || pos.y < 0 || pos.y >= boardSize)
                return null;

            var card = grid[pos.x, pos.y];
            if (card == null) return null;

            grid[pos.x, pos.y] = null;
            currentHand.Add(card);
            Debug.Log($"[ColonyBoardManager] RemoveCard: removed '{card.cardName}' from ({pos.x},{pos.y}), returned to hand");
            return card;
        }

        public bool IsValidPlacement(ColonyCardDefinitionSO card, Vector2Int pos)
        {
            switch (card.placementRequirement)
            {
                case PlacementRequirement.None:
                    return true;

                case PlacementRequirement.AdjacentTo:
                    return HasAdjacentCard(pos, card.adjacencyCardName);

                case PlacementRequirement.Edge:
                    return pos.x == 0 || pos.x == boardSize - 1 || pos.y == 0 || pos.y == boardSize - 1;

                case PlacementRequirement.Corner:
                    return (pos.x == 0 || pos.x == boardSize - 1) && (pos.y == 0 || pos.y == boardSize - 1);

                case PlacementRequirement.Center:
                    int center = boardSize / 2;
                    return pos.x == center && pos.y == center;

                default:
                    Debug.LogWarning($"[ColonyBoardManager] IsValidPlacement: unknown requirement {card.placementRequirement}");
                    return true;
            }
        }

        private bool HasAdjacentCard(Vector2Int pos, string cardName)
        {
            Vector2Int[] offsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var offset in offsets)
            {
                Vector2Int adj = pos + offset;
                if (adj.x >= 0 && adj.x < boardSize && adj.y >= 0 && adj.y < boardSize)
                {
                    var adjCard = grid[adj.x, adj.y];
                    if (adjCard != null && adjCard.cardName == cardName)
                    {
                        Debug.Log($"[ColonyBoardManager] HasAdjacentCard: found '{cardName}' at ({adj.x},{adj.y}) adjacent to ({pos.x},{pos.y})");
                        return true;
                    }
                }
            }
            return false;
        }

        public ColonyConfig CalculateColonyEffects()
        {
            var config = new ColonyConfig();
            int baseDeckSize = 8;
            int totalDeckBonus = 0;
            int totalConsumptionReduction = 0;
            int totalPopulation = 0;

            Debug.Log($"[ColonyBoardManager] CalculateColonyEffects: scanning {boardSize}x{boardSize} grid");

            for (int r = 0; r < boardSize; r++)
            {
                for (int c = 0; c < boardSize; c++)
                {
                    var card = grid[r, c];
                    if (card == null) continue;

                    totalPopulation += card.populationCost;
                    Debug.Log($"[ColonyBoardManager] CalculateColonyEffects: card='{card.cardName}' at ({r},{c}), effect={card.colonyEffect}, value={card.effectValue}, pop={card.populationCost}");

                    switch (card.colonyEffect)
                    {
                        case ColonyEffect.IncreaseDeckSize:
                            totalDeckBonus += card.effectValue;
                            break;
                        case ColonyEffect.ReduceConsumption:
                            totalConsumptionReduction += card.effectValue;
                            break;
                        case ColonyEffect.HeroCombatBonus:
                            config.heroCombatBonus += card.effectValue;
                            break;
                        case ColonyEffect.HeroMoveBonus:
                            config.heroMoveBonus += card.effectValue;
                            break;
                        case ColonyEffect.HeroCarryBonus:
                            config.heroCarryBonus += card.effectValue;
                            break;
                        case ColonyEffect.BonusStartingFood:
                            config.bonusStartingFood += card.effectValue;
                            break;
                        case ColonyEffect.ReducePopulation:
                            totalPopulation = Mathf.Max(0, totalPopulation - card.effectValue);
                            break;
                    }
                }
            }

            config.maxHeroDeckSize = baseDeckSize + totalDeckBonus;
            config.totalPopulation = totalPopulation;

            // Food consumption = population / 2, rounded up, min 1, minus reductions
            int rawConsumption = Mathf.CeilToInt(totalPopulation / 2f);
            config.foodConsumptionPerNode = Mathf.Max(1, rawConsumption - totalConsumptionReduction);

            calculatedConfig = config;
            Debug.Log($"[ColonyBoardManager] CalculateColonyEffects: {config}");
            return config;
        }

        public void FinishColonyManagement()
        {
            var config = CalculateColonyEffects();
            Debug.Log($"[ColonyBoardManager] FinishColonyManagement: colony locked — {config}");
            EventBus.OnColonyManagementComplete?.Invoke(config);
        }

        public bool HasCardsInHand => currentHand != null && currentHand.Count > 0;
        public bool HasMoreHands => currentHandIndex < totalHands;
        public List<ColonyCardDefinitionSO> CurrentHand => currentHand != null ? new List<ColonyCardDefinitionSO>(currentHand) : new List<ColonyCardDefinitionSO>();

        public ColonyCardDefinitionSO GetCardAt(Vector2Int pos)
        {
            if (pos.x < 0 || pos.x >= boardSize || pos.y < 0 || pos.y >= boardSize)
                return null;
            return grid[pos.x, pos.y];
        }

        public void ResetColony()
        {
            grid = null;
            colonyDeck = null;
            currentHand = null;
            calculatedConfig = null;
            Debug.Log("[ColonyBoardManager] ResetColony: colony board cleared");
        }
    }
}
