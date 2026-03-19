using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Interfaces;
using Scurry.Core;
using Scurry.Map;

namespace Scurry.Colony
{
    /// <summary>
    /// Thin adapter over MapGraph colony nodes. Colony cards are stored on MapNode.placedColonyCard.
    /// This class provides the IColonyGraph interface for effect calculations and turn management.
    /// </summary>
    public class ColonyGraph : IColonyGraph
    {
        private MapGraph mapGraph;
        private int cardsPlayedThisTurn = 0;

        public int CardCount
        {
            get
            {
                if (mapGraph == null) return 0;
                int count = 0;
                foreach (var node in mapGraph.GetColonyNodes())
                    if (node.HasColonyCard) count++;
                return count;
            }
        }

        public int CardsPlayedThisTurn => cardsPlayedThisTurn;

        public IReadOnlyDictionary<int, PlacedColonyCard> PlacedCards
        {
            get
            {
                var result = new Dictionary<int, PlacedColonyCard>();
                if (mapGraph == null) return result;
                foreach (var node in mapGraph.GetColonyNodes())
                {
                    if (node.HasColonyCard)
                    {
                        result[node.nodeId] = new PlacedColonyCard
                        {
                            placedId = node.nodeId,
                            definition = node.placedColonyCard,
                            position = node.worldPosition
                        };
                    }
                }
                return result;
            }
        }

        // ── Initialization ───────────────────────────────────────────────

        /// <summary>
        /// Initialize with MapGraph reference. Colony cards are stored on MapNode.placedColonyCard.
        /// </summary>
        public void Initialize(MapGraph graph)
        {
            Debug.Log($"[ColonyGraph] Initialize: setting MapGraph reference (colonyNodes={graph?.GetColonyNodes()?.Count ?? 0})");
            mapGraph = graph;
            cardsPlayedThisTurn = 0;
        }

        /// <summary>
        /// Legacy initialization — clears state. Call Initialize(MapGraph) first.
        /// </summary>
        public void Initialize()
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log("[ColonyGraph] Initialize: clearing state");
            cardsPlayedThisTurn = 0;
        }

        /// <summary>
        /// Places starter cards on the colony entrance node and an adjacent colony node.
        /// Requires Initialize(MapGraph) to have been called first.
        /// </summary>
        public void Initialize(ColonyCardDefinitionSO entranceDef, ColonyCardDefinitionSO basicBurrowDef)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] Initialize: placing starters " +
                      $"(entrance={entranceDef?.cardName ?? "NULL"}, burrow={basicBurrowDef?.cardName ?? "NULL"})");

            cardsPlayedThisTurn = 0;

            if (mapGraph == null)
            {
                Debug.LogError("[ColonyGraph] Initialize: mapGraph is null — cannot place starter cards");
                return;
            }

            // Place Entrance on the colony entrance node (ColonyNodeId)
            if (entranceDef != null)
            {
                var entranceNode = mapGraph.GetNode(mapGraph.ColonyNodeId);
                if (entranceNode != null && entranceNode.IsColonyNode)
                {
                    entranceNode.placedColonyCard = entranceDef;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] Initialize: placed Entrance on node {entranceNode.nodeId}");

                    // Place Basic Burrow on first adjacent colony node
                    if (basicBurrowDef != null)
                    {
                        var neighbors = mapGraph.GetNeighbors(entranceNode.nodeId);
                        foreach (var neighbor in neighbors)
                        {
                            if (neighbor.IsColonyNode && !neighbor.HasColonyCard)
                            {
                                neighbor.placedColonyCard = basicBurrowDef;
                                if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] Initialize: placed Basic Burrow on node {neighbor.nodeId}");
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[ColonyGraph] Initialize: entrance node not found or not colony type (id={mapGraph.ColonyNodeId})");
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] Initialize: starter colony complete (totalCards={CardCount})");
        }

        // ── Card placement ───────────────────────────────────────────────

        /// <summary>
        /// Places a colony card on an empty colony node. The target node must be an empty colony node
        /// adjacent to a colony node that already has a card placed.
        /// Returns the node ID where the card was placed, or -1 if invalid.
        /// </summary>
        public int AddCard(ColonyCardDefinitionSO card, int targetNodeId)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] AddCard: attempting placement " +
                      $"(card={card?.cardName ?? "NULL"}, targetNodeId={targetNodeId}, " +
                      $"cardsPlayedThisTurn={cardsPlayedThisTurn})");

            if (card == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning("[ColonyGraph] AddCard: card is null — returning -1");
                return -1;
            }

            if (mapGraph == null)
            {
                Debug.LogError("[ColonyGraph] AddCard: mapGraph is null");
                return -1;
            }

            if (!CanPlayCard())
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[ColonyGraph] AddCard: cannot play more cards this turn " +
                                 $"(cardsPlayedThisTurn={cardsPlayedThisTurn})");
                return -1;
            }

            // Validate target is an empty colony node
            var targetNode = mapGraph.GetNode(targetNodeId);
            if (targetNode == null || !targetNode.IsColonyNode || targetNode.HasColonyCard)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[ColonyGraph] AddCard: invalid target " +
                                 $"(nodeId={targetNodeId}, exists={targetNode != null}, " +
                                 $"isColony={targetNode?.IsColonyNode}, hasCard={targetNode?.HasColonyCard})");
                return -1;
            }

            // Validate target is adjacent to at least one occupied colony node
            bool adjacentToOccupied = false;
            var neighbors = mapGraph.GetNeighbors(targetNodeId);
            foreach (var neighbor in neighbors)
            {
                if (neighbor.IsColonyNode && neighbor.HasColonyCard)
                {
                    adjacentToOccupied = true;
                    break;
                }
            }
            if (!adjacentToOccupied)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[ColonyGraph] AddCard: target not adjacent to occupied colony node");
                return -1;
            }

            // Validate placement requirement (AdjacentTo specific card name)
            if (!ValidatePlacementRequirement(card, targetNodeId))
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[ColonyGraph] AddCard: placement requirement not met " +
                                 $"(card={card.cardName}, requirement={card.placementRequirement}, " +
                                 $"adjacencyCardName={card.adjacencyCardName})");
                return -1;
            }

            // Place the card
            targetNode.placedColonyCard = card;
            cardsPlayedThisTurn++;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] AddCard: card placed successfully " +
                      $"(card={card.cardName}, nodeId={targetNodeId}, " +
                      $"cardsPlayedThisTurn={cardsPlayedThisTurn}, totalCards={CardCount})");

            return targetNodeId;
        }

        private bool ValidatePlacementRequirement(ColonyCardDefinitionSO card, int targetNodeId)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] ValidatePlacementRequirement: checking " +
                      $"(card={card.cardName}, requirement={card.placementRequirement}, targetNodeId={targetNodeId})");

            switch (card.placementRequirement)
            {
                case PlacementRequirement.None:
                    return true;

                case PlacementRequirement.AdjacentTo:
                    if (string.IsNullOrEmpty(card.adjacencyCardName))
                        return true;

                    // Check if any neighbor of the target has the required card
                    var neighbors = mapGraph.GetNeighbors(targetNodeId);
                    foreach (var neighbor in neighbors)
                    {
                        if (neighbor.IsColonyNode && neighbor.HasColonyCard &&
                            neighbor.placedColonyCard.cardName == card.adjacencyCardName)
                        {
                            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] ValidatePlacementRequirement: AdjacentTo satisfied " +
                                      $"(required={card.adjacencyCardName}, foundAt node {neighbor.nodeId})");
                            return true;
                        }
                    }

                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] ValidatePlacementRequirement: AdjacentTo NOT satisfied " +
                              $"(required={card.adjacencyCardName})");
                    return false;

                default:
                    return true;
            }
        }

        // ── Accessors ────────────────────────────────────────────────────

        public IReadOnlyDictionary<int, PlacedColonyCard> GetPlacedCards()
        {
            return PlacedCards;
        }

        /// <summary>
        /// Returns adjacent colony node IDs that have placed cards.
        /// </summary>
        public List<int> GetAdjacentCards(int nodeId)
        {
            var result = new List<int>();
            if (mapGraph == null) return result;

            var neighbors = mapGraph.GetNeighbors(nodeId);
            foreach (var neighbor in neighbors)
            {
                if (neighbor.IsColonyNode && neighbor.HasColonyCard)
                    result.Add(neighbor.nodeId);
            }
            return result;
        }

        // ── Colony effects ───────────────────────────────────────────────

        public int CalculateFoodProduction()
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log("[ColonyGraph] CalculateFoodProduction: calculating total food production");

            int baseProduction = 0;
            bool hasBaseProduction = false;

            foreach (var node in GetOccupiedNodes())
            {
                if (node.placedColonyCard.colonyEffect == ColonyEffect.BaseProduction)
                {
                    baseProduction += node.placedColonyCard.effectValue;
                    hasBaseProduction = true;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] CalculateFoodProduction: base production card " +
                              $"(node={node.nodeId}, card={node.placedColonyCard.cardName}, value={node.placedColonyCard.effectValue})");
                }
            }

            if (!hasBaseProduction)
            {
                baseProduction = 3;
                if (!SimulationFlags.SuppressLogging) Debug.Log("[ColonyGraph] CalculateFoodProduction: using default base production (value=3)");
            }

            int foodEffects = 0;
            foreach (var node in GetOccupiedNodes())
            {
                if (node.placedColonyCard.colonyEffect == ColonyEffect.FoodProduction)
                {
                    foodEffects += node.placedColonyCard.effectValue;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] CalculateFoodProduction: food production card " +
                              $"(node={node.nodeId}, card={node.placedColonyCard.cardName}, value={node.placedColonyCard.effectValue})");
                }
            }

            int totalProduction = baseProduction + foodEffects;

            if (HasEffect(ColonyEffect.DoubleProduction))
            {
                int before = totalProduction;
                totalProduction *= 2;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] CalculateFoodProduction: DoubleProduction applied " +
                          $"(before={before}, after={totalProduction})");
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] CalculateFoodProduction: total " +
                      $"(base={baseProduction}, foodEffects={foodEffects}, total={totalProduction})");

            return totalProduction;
        }

        public Dictionary<ColonyEffect, int> GetActiveEffects()
        {
            var effects = new Dictionary<ColonyEffect, int>();
            foreach (var node in GetOccupiedNodes())
            {
                ColonyEffect effect = node.placedColonyCard.colonyEffect;
                int value = node.placedColonyCard.effectValue;
                if (!effects.ContainsKey(effect))
                    effects[effect] = 0;
                effects[effect] += value;
            }
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] GetActiveEffects: found {effects.Count} unique effects");
            return effects;
        }

        public bool HasEffect(ColonyEffect effect)
        {
            foreach (var node in GetOccupiedNodes())
            {
                if (node.placedColonyCard.colonyEffect == effect)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] HasEffect: found (effect={effect}, " +
                              $"card={node.placedColonyCard.cardName}, node={node.nodeId})");
                    return true;
                }
            }
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] HasEffect: not found (effect={effect})");
            return false;
        }

        public int GetEffectValue(ColonyEffect effect)
        {
            int total = 0;
            foreach (var node in GetOccupiedNodes())
            {
                if (node.placedColonyCard.colonyEffect == effect)
                    total += node.placedColonyCard.effectValue;
            }
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] GetEffectValue: (effect={effect}, totalValue={total})");
            return total;
        }

        // ── Turn management ──────────────────────────────────────────────

        public bool CanPlayCard()
        {
            int limit = 1;
            if (HasEffect(ColonyEffect.DoubleColonyCard))
            {
                limit = 2;
                if (!SimulationFlags.SuppressLogging) Debug.Log("[ColonyGraph] CanPlayCard: DoubleColonyCard effect — limit=2");
            }
            bool canPlay = cardsPlayedThisTurn < limit;
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] CanPlayCard: (cardsPlayedThisTurn={cardsPlayedThisTurn}, " +
                      $"limit={limit}, canPlay={canPlay})");
            return canPlay;
        }

        public void ResetTurnCardCount()
        {
            int previous = cardsPlayedThisTurn;
            cardsPlayedThisTurn = 0;
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ColonyGraph] ResetTurnCardCount: reset (previous={previous}, current=0)");
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private List<MapNode> GetOccupiedNodes()
        {
            if (mapGraph == null) return new List<MapNode>();
            return mapGraph.GetOccupiedColonyNodes();
        }
    }

    [System.Serializable]
    public class PlacedColonyCard
    {
        public int placedId; // now stores the MapNode.nodeId
        public ColonyCardDefinitionSO definition;
        public Vector2 position; // world position from MapNode

        public override string ToString()
        {
            return $"PlacedColonyCard(id={placedId}, name={definition?.cardName ?? "NULL"}, pos={position})";
        }
    }
}
