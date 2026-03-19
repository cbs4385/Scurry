using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Core;
using Scurry.Interfaces;


namespace Scurry.Map
{
    /// <summary>
    /// Lightweight struct representing a hero's position and equipment effects for fog calculation.
    /// </summary>
    public struct HeroFogInfo
    {
        public int nodeId;
        public int bonusRevealRange; // e.g., Bead Lantern gives +1
        public bool revealsEntireZone; // e.g., Lantern of the Deep
    }

    public class FogOfWar : IFogOfWar
    {
        /// <summary>
        /// Recalculates fog state for all nodes in the graph based on hero positions and colony effects.
        /// Fires EventBus events for nodes that change visibility.
        /// </summary>
        public void RecalculateVisibility(MapGraph graph, List<HeroFogInfo> heroes, HashSet<ColonyEffect> activeEffects)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[FogOfWar] RecalculateVisibility: starting (heroCount={heroes.Count}, activeEffects={activeEffects.Count})");

            var allNodes = graph.GetAllNodes();
            var newVisibleNodes = new HashSet<int>();
            int colonyNodeId = graph.ColonyNodeId;

            // Rule: Colony node is always visible
            if (colonyNodeId >= 0)
            {
                newVisibleNodes.Add(colonyNodeId);
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[FogOfWar] RecalculateVisibility: Colony node always visible (nodeId={colonyNodeId})");
            }

            // Rule: Watchtower colony effect -- nodes within 2 edges of colony are Visible
            if (activeEffects.Contains(ColonyEffect.FogReveal) && colonyNodeId >= 0)
            {
                var watchtowerNodes = PathfindingService.GetNodesWithinRange(graph, colonyNodeId, 2);
                foreach (int id in watchtowerNodes)
                    newVisibleNodes.Add(id);
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[FogOfWar] RecalculateVisibility: Watchtower (FogReveal) revealed {watchtowerNodes.Count} nodes near colony");
            }

            // Process each hero
            foreach (var hero in heroes)
            {
                if (graph.GetNode(hero.nodeId) == null)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[FogOfWar] RecalculateVisibility: hero on invalid node (nodeId={hero.nodeId}), skipping");
                    continue;
                }

                // Rule: Nodes with heroes = Visible
                newVisibleNodes.Add(hero.nodeId);

                // Rule: Nodes adjacent to heroes = Visible (base range = 1)
                int revealRange = 1 + hero.bonusRevealRange;

                // Rule: Signal Tower colony effect -- nodes within 3 edges of any hero = Visible
                if (activeEffects.Contains(ColonyEffect.FogRevealHeroes))
                {
                    revealRange = Mathf.Max(revealRange, 3);
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[FogOfWar] RecalculateVisibility: Signal Tower (FogRevealHeroes) boosted reveal range to {revealRange} for hero at node {hero.nodeId}");
                }

                var revealedNodes = PathfindingService.GetNodesWithinRange(graph, hero.nodeId, revealRange);
                foreach (int id in revealedNodes)
                    newVisibleNodes.Add(id);

                if (!SimulationFlags.SuppressLogging) Debug.Log($"[FogOfWar] RecalculateVisibility: hero reveal (heroNode={hero.nodeId}, revealRange={revealRange}, nodesRevealed={revealedNodes.Count})");

                // Rule: Lantern of the Deep -- entire zone visible
                if (hero.revealsEntireZone)
                {
                    MapNode heroNode = graph.GetNode(hero.nodeId);
                    if (heroNode != null)
                    {
                        var zoneNodes = graph.GetNodesInZone(heroNode.zone);
                        foreach (var zoneNode in zoneNodes)
                            newVisibleNodes.Add(zoneNode.nodeId);
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[FogOfWar] RecalculateVisibility: Lantern of the Deep revealed entire zone (zone={heroNode.zone}, count={zoneNodes.Count})");
                    }
                }
            }

            // Apply fog states and track changes
            int nodesRevealed = 0;
            int nodesHidden = 0;
            int nodesRemembered = 0;

            foreach (MapNode node in allNodes)
            {
                FogState previousState = node.fogState;
                FogState newState;

                if (newVisibleNodes.Contains(node.nodeId))
                {
                    newState = FogState.Visible;
                    node.visited = true; // Mark as visited when seen
                }
                else if (node.visited)
                {
                    // Rule: Previously visited nodes with no hero nearby = Remembered
                    newState = FogState.Remembered;
                }
                else
                {
                    // Rule: Everything else = Hidden
                    newState = FogState.Hidden;
                }

                node.fogState = newState;

                // Fire events for state changes
                if (previousState != newState)
                {
                    if (newState == FogState.Visible && previousState != FogState.Visible)
                    {
                        nodesRevealed++;
                        EventBus.OnNodeRevealed?.Invoke(node.nodeId);
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[FogOfWar] RecalculateVisibility: node revealed (nodeId={node.nodeId}, zone={node.zone}, previousState={previousState})");
                    }
                    else if (newState == FogState.Hidden && previousState != FogState.Hidden)
                    {
                        nodesHidden++;
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[FogOfWar] RecalculateVisibility: node hidden (nodeId={node.nodeId}, zone={node.zone}, previousState={previousState})");
                    }
                    else if (newState == FogState.Remembered)
                    {
                        nodesRemembered++;
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[FogOfWar] RecalculateVisibility: node remembered (nodeId={node.nodeId}, zone={node.zone}, previousState={previousState})");
                    }
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[FogOfWar] RecalculateVisibility: complete (totalVisible={newVisibleNodes.Count}, nodesRevealed={nodesRevealed}, nodesHidden={nodesHidden}, nodesRemembered={nodesRemembered})");
        }

        /// <summary>
        /// Returns true if enemies on the given node should be visible to the player.
        /// Enemies are only visible on Visible nodes, not Remembered ones.
        /// </summary>
        public bool AreEnemiesVisible(MapNode node)
        {
            bool visible = node.fogState == FogState.Visible;
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[FogOfWar] AreEnemiesVisible: (nodeId={node.nodeId}, fogState={node.fogState}, enemiesVisible={visible})");
            return visible;
        }

        /// <summary>
        /// Returns true if resources on the given node should be visible to the player.
        /// Resources are visible on Visible and Remembered nodes.
        /// </summary>
        public bool AreResourcesVisible(MapNode node)
        {
            bool visible = node.fogState == FogState.Visible || node.fogState == FogState.Remembered;
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[FogOfWar] AreResourcesVisible: (nodeId={node.nodeId}, fogState={node.fogState}, resourcesVisible={visible})");
            return visible;
        }

        /// <summary>
        /// Returns the set of node IDs currently visible.
        /// </summary>
        public HashSet<int> GetVisibleNodeIds(MapGraph graph)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log("[FogOfWar] GetVisibleNodeIds: collecting visible nodes");

            var result = new HashSet<int>();
            foreach (MapNode node in graph.GetAllNodes())
            {
                if (node.fogState == FogState.Visible)
                    result.Add(node.nodeId);
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[FogOfWar] GetVisibleNodeIds: (visibleCount={result.Count})");
            return result;
        }

        /// <summary>
        /// Returns the set of node IDs that are at least Remembered (Visible or Remembered).
        /// </summary>
        public HashSet<int> GetKnownNodeIds(MapGraph graph)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log("[FogOfWar] GetKnownNodeIds: collecting known nodes");

            var result = new HashSet<int>();
            foreach (MapNode node in graph.GetAllNodes())
            {
                if (node.fogState != FogState.Hidden)
                    result.Add(node.nodeId);
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[FogOfWar] GetKnownNodeIds: (knownCount={result.Count})");
            return result;
        }
    }
}
