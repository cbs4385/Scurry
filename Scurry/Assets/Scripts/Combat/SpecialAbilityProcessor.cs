using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scurry.Data;
using Scurry.Map;
using Scurry.Core;

namespace Scurry.Combat
{
    /// <summary>
    /// Processes hero special abilities at various combat and game phases.
    /// Each hero has at most one SpecialAbility from their CardDefinitionSO.
    /// </summary>
    public static class SpecialAbilityProcessor
    {
        // ===================================================================
        // Pre-Combat Abilities
        // ===================================================================

        /// <summary>
        /// Processes a hero's special ability at the start of combat (before any rounds).
        /// </summary>
        public static void ProcessPreCombatAbility(
            HeroToken hero,
            List<HeroToken> allHeroes,
            List<EnemyToken> enemies,
            CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: ENTER (hero={hero.cardDef.cardName}, ability={hero.cardDef.specialAbility})");

            if (!hero.IsAlive)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: hero={hero.cardDef.cardName} is dead, skipping");
                return;
            }

            switch (hero.cardDef.specialAbility)
            {
                case SpecialAbility.Rally:
                    // +1 Combat to all heroes on same node
                    int rallyCount = 0;
                    foreach (var ally in allHeroes.Where(h => h.IsAlive && h.tokenId != hero.tokenId))
                    {
                        ctx.AddPermanentCombatBuff(ally.tokenId, 1);
                        rallyCount++;
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: Rally — hero={hero.cardDef.cardName} buffed ally={ally.cardDef.cardName} +1 Combat");
                    }
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: Rally — buffed {rallyCount} allies");
                    break;

                case SpecialAbility.BattleCry:
                    // +1 Move to all heroes on same node (applied to base move for this turn)
                    int bcCount = 0;
                    foreach (var ally in allHeroes.Where(h => h.IsAlive && h.tokenId != hero.tokenId))
                    {
                        ally.baseMove += 1;
                        bcCount++;
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: BattleCry — hero={hero.cardDef.cardName} gave ally={ally.cardDef.cardName} +1 Move (now={ally.baseMove})");
                    }
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: BattleCry — buffed {bcCount} allies with +1 Move");
                    break;

                case SpecialAbility.WiseCounsel:
                    // Reveals enemy stats before combat on this node (informational)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: WiseCounsel — revealing enemy stats on node={ctx.nodeId}");
                    foreach (var enemy in enemies.Where(e => e.IsAlive))
                    {
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: WiseCounsel reveal — enemy={enemy.definition.enemyName}, str={enemy.strength}, hp={enemy.currentHP}/{enemy.maxHP}, behavior={enemy.behavior}");
                    }
                    break;

                case SpecialAbility.Inspire:
                    // Inspire: +1 Combat to lowest-combat ally on same node
                    var lowestAlly = allHeroes
                        .Where(h => h.IsAlive && h.tokenId != hero.tokenId)
                        .OrderBy(h => h.EffectiveCombat)
                        .FirstOrDefault();
                    if (lowestAlly != null)
                    {
                        ctx.AddPermanentCombatBuff(lowestAlly.tokenId, 1);
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: Inspire — hero={hero.cardDef.cardName} inspired ally={lowestAlly.cardDef.cardName} +1 Combat");
                    }
                    else
                    {
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: Inspire — no ally to inspire");
                    }
                    break;

                case SpecialAbility.FirstStrike:
                    // FirstStrike is handled directly by CombatResolver.ProcessPreCombat
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: FirstStrike — handled by CombatResolver");
                    break;

                case SpecialAbility.Taunt:
                    // Taunt: enemies must target this hero first (handled in damage targeting)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: Taunt — hero={hero.cardDef.cardName} will be targeted first by enemies");
                    break;

                case SpecialAbility.Fortify:
                    // +2 HP when stationary for a full turn (check if hero hasn't moved)
                    // This is a pre-combat check: if the hero is on the same node as last turn, gain +2 HP
                    // For now, we assume stationary heroes get the buff (caller tracks movement)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: Fortify — hero={hero.cardDef.cardName} (stationary check handled by TurnManager)");
                    break;

                case SpecialAbility.None:
                    break;

                default:
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: hero={hero.cardDef.cardName}, ability={hero.cardDef.specialAbility} — no pre-combat effect");
                    break;
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPreCombatAbility: EXIT (hero={hero.cardDef.cardName})");
        }

        // ===================================================================
        // Per-Round Abilities
        // ===================================================================

        /// <summary>
        /// Processes a hero's special ability each combat round.
        /// </summary>
        public static void ProcessPerRoundAbility(
            HeroToken hero,
            List<HeroToken> allHeroes,
            List<EnemyToken> enemies,
            CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPerRoundAbility: ENTER (hero={hero.cardDef.cardName}, ability={hero.cardDef.specialAbility}, round={ctx.roundNumber})");

            if (!hero.IsAlive)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPerRoundAbility: hero={hero.cardDef.cardName} is dead, skipping");
                return;
            }

            switch (hero.cardDef.specialAbility)
            {
                case SpecialAbility.FieldMedic:
                    // Heal 1 HP to lowest-HP ally on same node each round
                    var lowestHP = allHeroes
                        .Where(h => h.IsAlive && h.tokenId != hero.tokenId && h.currentHP < h.maxHP)
                        .OrderBy(h => h.currentHP)
                        .FirstOrDefault();
                    if (lowestHP != null)
                    {
                        lowestHP.Heal(1);
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPerRoundAbility: FieldMedic — hero={hero.cardDef.cardName} healed ally={lowestHP.cardDef.cardName} for 1 HP (now={lowestHP.currentHP}/{lowestHP.maxHP})");
                    }
                    else
                    {
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPerRoundAbility: FieldMedic — no ally needs healing");
                    }
                    break;

                case SpecialAbility.Intercept:
                    // Takes damage instead of lowest-combat ally (handled in CombatResolver.ApplyDamageToHeroes)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPerRoundAbility: Intercept — hero={hero.cardDef.cardName} ready to intercept (handled by damage targeting)");
                    break;

                case SpecialAbility.Riposte:
                    // Deals 1 damage to attacker when hit (handled in CombatResolver.ApplyDamageToHeroes)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPerRoundAbility: Riposte — hero={hero.cardDef.cardName} ready to riposte (handled by damage application)");
                    break;

                case SpecialAbility.Cleave:
                    // 1 splash damage to all enemies (handled in CombatResolver.ApplyCleaveAndSplash)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPerRoundAbility: Cleave — hero={hero.cardDef.cardName} (handled by round resolution)");
                    break;

                case SpecialAbility.DualWield:
                    // +1 Combat per offensive item (handled in EquipmentEffectProcessor.GetCombatBonus)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPerRoundAbility: DualWield — hero={hero.cardDef.cardName} (passive, handled by equipment processor)");
                    break;

                case SpecialAbility.RangedStrike:
                    // Ranged strike is handled in pre-combat; no per-round effect
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPerRoundAbility: RangedStrike — hero={hero.cardDef.cardName} (handled in pre-combat)");
                    break;

                case SpecialAbility.Tinker:
                    // Utility items grant double effect (handled in EquipmentEffectProcessor)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPerRoundAbility: Tinker — hero={hero.cardDef.cardName} (passive, handled by equipment processor)");
                    break;

                case SpecialAbility.None:
                    break;

                default:
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPerRoundAbility: hero={hero.cardDef.cardName}, ability={hero.cardDef.specialAbility} — no per-round combat effect");
                    break;
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPerRoundAbility: EXIT (hero={hero.cardDef.cardName})");
        }

        // ===================================================================
        // Post-Combat Abilities
        // ===================================================================

        /// <summary>
        /// Processes a hero's special ability after combat ends.
        /// </summary>
        public static void ProcessPostCombatAbility(HeroToken hero, CombatResult result)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPostCombatAbility: ENTER (hero={hero.cardDef.cardName}, ability={hero.cardDef.specialAbility}, heroesWon={result.heroesWon})");

            if (!hero.IsAlive)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPostCombatAbility: hero={hero.cardDef.cardName} is dead, skipping");
                return;
            }

            switch (hero.cardDef.specialAbility)
            {
                case SpecialAbility.ScoutReport:
                    // Reveals enemy count on adjacent nodes (informational, post-combat)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPostCombatAbility: ScoutReport — hero={hero.cardDef.cardName} reveals enemy counts on adjacent nodes (handled by MapManager)");
                    break;

                case SpecialAbility.None:
                    break;

                default:
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPostCombatAbility: hero={hero.cardDef.cardName}, ability={hero.cardDef.specialAbility} — no post-combat effect");
                    break;
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessPostCombatAbility: EXIT (hero={hero.cardDef.cardName})");
        }

        // ===================================================================
        // Movement Abilities
        // ===================================================================

        /// <summary>
        /// Processes movement-related abilities when a hero moves between nodes.
        /// </summary>
        public static void ProcessMovementAbility(HeroToken hero, MapGraph graph, int fromNode, int toNode)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessMovementAbility: ENTER (hero={hero.cardDef.cardName}, ability={hero.cardDef.specialAbility}, from={fromNode}, to={toNode})");

            if (!hero.IsAlive)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessMovementAbility: hero={hero.cardDef.cardName} is dead, skipping");
                return;
            }

            switch (hero.cardDef.specialAbility)
            {
                case SpecialAbility.ExtendedFogReveal:
                    // Reveals 1 extra node beyond normal visibility
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessMovementAbility: ExtendedFogReveal — hero={hero.cardDef.cardName} reveals +1 extra node from node={toNode}");
                    if (graph != null)
                    {
                        var neighbors = graph.GetNeighbors(toNode);
                        foreach (var neighbor in neighbors)
                        {
                            // Reveal the neighbor's neighbors too (1 extra depth)
                            var extended = graph.GetNeighbors(neighbor.nodeId);
                            foreach (var ext in extended)
                            {
                                if (ext.fogState == FogState.Hidden)
                                {
                                    ext.fogState = FogState.Remembered;
                                    Scurry.Core.EventBus.OnNodeRevealed?.Invoke(ext.nodeId);
                                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessMovementAbility: ExtendedFogReveal — revealed node={ext.nodeId} (zone={ext.zone})");
                                }
                            }
                        }
                    }
                    break;

                case SpecialAbility.MapOnMove:
                    // Reveals all nodes along path when moving
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessMovementAbility: MapOnMove — hero={hero.cardDef.cardName} reveals path from={fromNode} to={toNode}");
                    if (graph != null)
                    {
                        var path = graph.ShortestPath(fromNode, toNode);
                        foreach (int nodeId in path)
                        {
                            var node = graph.GetNode(nodeId);
                            if (node != null && node.fogState == FogState.Hidden)
                            {
                                node.fogState = FogState.Visible;
                                Scurry.Core.EventBus.OnNodeRevealed?.Invoke(nodeId);
                                if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessMovementAbility: MapOnMove — revealed node={nodeId} along path");
                            }
                            // Also reveal neighbors of path nodes
                            var neighbors = graph.GetNeighbors(nodeId);
                            foreach (var neighbor in neighbors)
                            {
                                if (neighbor.fogState == FogState.Hidden)
                                {
                                    neighbor.fogState = FogState.Remembered;
                                    Scurry.Core.EventBus.OnNodeRevealed?.Invoke(neighbor.nodeId);
                                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessMovementAbility: MapOnMove — revealed adjacent node={neighbor.nodeId}");
                                }
                            }
                        }
                    }
                    break;

                case SpecialAbility.Torchlight:
                    // Reveals fog 2 nodes in all directions (BFS depth 2)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessMovementAbility: Torchlight — hero={hero.cardDef.cardName} reveals 2-depth from node={toNode}");
                    if (graph != null)
                    {
                        RevealNodesInRadius(graph, toNode, 2);
                    }
                    break;

                case SpecialAbility.SwiftDelivery:
                    // Can deposit resources at colony without ending movement (handled by TurnManager)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessMovementAbility: SwiftDelivery — hero={hero.cardDef.cardName} passes through colony (handled by TurnManager)");
                    break;

                case SpecialAbility.None:
                    break;

                default:
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessMovementAbility: hero={hero.cardDef.cardName}, ability={hero.cardDef.specialAbility} — no movement effect");
                    break;
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessMovementAbility: EXIT (hero={hero.cardDef.cardName})");
        }

        // ===================================================================
        // Gather Abilities
        // ===================================================================

        /// <summary>
        /// Processes gathering-related abilities and returns bonus resources gathered.
        /// </summary>
        public static int ProcessGatherAbility(HeroToken hero, MapNode node)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessGatherAbility: ENTER (hero={hero.cardDef.cardName}, ability={hero.cardDef.specialAbility}, nodeId={node.nodeId}, zone={node.zone})");

            if (!hero.IsAlive)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessGatherAbility: hero={hero.cardDef.cardName} is dead, returning 0");
                return 0;
            }

            int bonus = 0;

            switch (hero.cardDef.specialAbility)
            {
                case SpecialAbility.EfficientGather:
                    // +1 resource gathered per node
                    bonus = 1;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessGatherAbility: EfficientGather — hero={hero.cardDef.cardName} gathers +{bonus} extra resource");
                    break;

                case SpecialAbility.BulkHaul:
                    // +1 Carry on Wilderness nodes
                    if (node.zone == NodeType.Wilderness)
                    {
                        bonus = 1;
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessGatherAbility: BulkHaul — hero={hero.cardDef.cardName} gets +{bonus} Carry on Wilderness node={node.nodeId}");
                    }
                    else
                    {
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessGatherAbility: BulkHaul — not on Wilderness (zone={node.zone}), no bonus");
                    }
                    break;

                case SpecialAbility.None:
                    break;

                default:
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessGatherAbility: hero={hero.cardDef.cardName}, ability={hero.cardDef.specialAbility} — no gather effect");
                    break;
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] ProcessGatherAbility: EXIT (hero={hero.cardDef.cardName}, bonus={bonus})");
            return bonus;
        }

        // ===================================================================
        // Helpers
        // ===================================================================

        /// <summary>
        /// Reveals all nodes within a given BFS depth radius from a center node.
        /// </summary>
        private static void RevealNodesInRadius(MapGraph graph, int centerNodeId, int radius)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] RevealNodesInRadius: ENTER (center={centerNodeId}, radius={radius})");

            var visited = new HashSet<int>();
            var queue = new Queue<(int nodeId, int depth)>();
            queue.Enqueue((centerNodeId, 0));
            visited.Add(centerNodeId);

            while (queue.Count > 0)
            {
                var (nodeId, depth) = queue.Dequeue();
                var node = graph.GetNode(nodeId);
                if (node == null) continue;

                if (node.fogState == FogState.Hidden)
                {
                    node.fogState = depth == 0 ? FogState.Visible : FogState.Remembered;
                    Scurry.Core.EventBus.OnNodeRevealed?.Invoke(nodeId);
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] RevealNodesInRadius: revealed node={nodeId} at depth={depth}");
                }

                if (depth < radius)
                {
                    foreach (int neighborId in node.neighborIds)
                    {
                        if (!visited.Contains(neighborId))
                        {
                            visited.Add(neighborId);
                            queue.Enqueue((neighborId, depth + 1));
                        }
                    }
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[SpecialAbilityProcessor] RevealNodesInRadius: EXIT (revealed {visited.Count} nodes in radius={radius})");
        }
    }
}
