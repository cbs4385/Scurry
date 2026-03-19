using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scurry.Data;
using Scurry.Map;
using Scurry.Colony;


namespace Scurry.Core
{
    public static class GatherPhase
    {
        /// <summary>
        /// Executes the gather phase for all deployed heroes. Each hero on a node with resources
        /// gathers up to their remaining carry capacity, with applicable bonuses.
        /// Heroes on the colony node auto-deposit resources.
        /// </summary>
        public static GatherResult Execute(List<HeroToken> heroes, MapGraph graph, ColonyGraph colony)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[GatherPhase] Execute: starting gather phase (heroCount={heroes?.Count ?? 0})");

            var result = new GatherResult
            {
                gatheredByHero = new Dictionary<int, Dictionary<ResourceType, int>>(),
                totalGathered = 0
            };

            if (heroes == null || graph == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning("[GatherPhase] Execute: heroes or graph is null — returning empty result");
                return result;
            }

            // Check colony effects
            bool hasGreatCity = colony != null && colony.HasEffect(ColonyEffect.DoubleProduction);

            foreach (var hero in heroes)
            {
                if (hero.isInjured)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[GatherPhase] Execute: skipping injured hero " +
                              $"(tokenId={hero.tokenId}, name={hero.cardDef?.cardName ?? "unknown"})");
                    continue;
                }

                MapNode node = graph.GetNode(hero.currentNodeId);
                if (node == null)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[GatherPhase] Execute: hero on null node " +
                                     $"(tokenId={hero.tokenId}, nodeId={hero.currentNodeId})");
                    continue;
                }

                int totalNodeResources = node.TotalResources();
                if (totalNodeResources <= 0)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[GatherPhase] Execute: no resources on node " +
                              $"(tokenId={hero.tokenId}, nodeId={hero.currentNodeId}, totalResources=0)");
                    continue;
                }

                int remainingCapacity = hero.EffectiveCarry - hero.TotalCarried;
                if (remainingCapacity <= 0)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[GatherPhase] Execute: hero at carry capacity " +
                              $"(tokenId={hero.tokenId}, name={hero.cardDef?.cardName ?? "unknown"}, " +
                              $"carry={hero.EffectiveCarry}, carried={hero.TotalCarried})");
                    continue;
                }

                // Calculate gather bonuses
                int gatherBonus = 0;
                string bonusSource = "";

                // EfficientGather ability: +1
                if (hero.cardDef != null && hero.cardDef.specialAbility == SpecialAbility.EfficientGather)
                {
                    gatherBonus += 1;
                    bonusSource += "EfficientGather(+1) ";
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[GatherPhase] Execute: EfficientGather bonus applied " +
                              $"(tokenId={hero.tokenId}, bonus=+1)");
                }

                // BulkHaul ability: already factored into EffectiveCarry via hero stats
                // but we log it for tracking
                if (hero.cardDef != null && hero.cardDef.specialAbility == SpecialAbility.BulkHaul)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[GatherPhase] Execute: BulkHaul hero noted " +
                              $"(tokenId={hero.tokenId}, effectiveCarry={hero.EffectiveCarry})");
                }

                if (!SimulationFlags.SuppressLogging) Debug.Log($"[GatherPhase] Execute: gathering for hero " +
                          $"(tokenId={hero.tokenId}, name={hero.cardDef?.cardName ?? "unknown"}, " +
                          $"nodeId={hero.currentNodeId}, nodeResources={totalNodeResources}, " +
                          $"remainingCapacity={remainingCapacity}, gatherBonus={gatherBonus}, " +
                          $"bonusSource=\"{bonusSource.Trim()}\")");

                var heroGathered = new Dictionary<ResourceType, int>();
                int heroTotalGathered = 0;

                // Gather each resource type from the node
                var resourceTypes = new List<ResourceType>(node.resources.Keys);
                foreach (var resType in resourceTypes)
                {
                    if (remainingCapacity <= 0) break;

                    int available = node.resources[resType];
                    if (available <= 0) continue;

                    int baseGather = Mathf.Min(available, remainingCapacity);
                    int bonusGather = Mathf.Min(gatherBonus, available - baseGather);
                    bonusGather = Mathf.Max(0, bonusGather);

                    int totalGather = baseGather + bonusGather;

                    // Apply Harvest Bounty (Great City doubles production, applied to gathering too)
                    if (hasGreatCity)
                    {
                        int doubled = Mathf.Min(totalGather * 2, available);
                        doubled = Mathf.Min(doubled, remainingCapacity + gatherBonus);
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[GatherPhase] Execute: GreatCity doubling " +
                                  $"(before={totalGather}, after={doubled}, tokenId={hero.tokenId})");
                        totalGather = doubled;
                    }

                    // Clamp to what's actually available and what hero can carry
                    totalGather = Mathf.Min(totalGather, available);
                    int actuallyCarriable = hero.EffectiveCarry - hero.TotalCarried;
                    totalGather = Mathf.Min(totalGather, Mathf.Max(0, actuallyCarriable));

                    if (totalGather <= 0) continue;

                    // Perform the gather
                    int actualGathered = hero.GatherResource(resType, totalGather);

                    // Remove from node
                    node.resources[resType] -= actualGathered;
                    if (node.resources[resType] <= 0)
                    {
                        node.resources.Remove(resType);
                    }

                    if (!heroGathered.ContainsKey(resType))
                    {
                        heroGathered[resType] = 0;
                    }
                    heroGathered[resType] += actualGathered;
                    heroTotalGathered += actualGathered;
                    remainingCapacity -= actualGathered;

                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[GatherPhase] Execute: gathered resource " +
                              $"(tokenId={hero.tokenId}, type={resType}, amount={actualGathered}, " +
                              $"nodeRemaining={node.resources.GetValueOrDefault(resType, 0)}, " +
                              $"heroCarried={hero.TotalCarried}/{hero.EffectiveCarry})");
                }

                if (heroTotalGathered > 0)
                {
                    result.gatheredByHero[hero.tokenId] = heroGathered;
                    result.totalGathered += heroTotalGathered;

                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[GatherPhase] Execute: hero gather complete " +
                              $"(tokenId={hero.tokenId}, name={hero.cardDef?.cardName ?? "unknown"}, " +
                              $"totalGathered={heroTotalGathered}, carried={hero.TotalCarried}/{hero.EffectiveCarry})");
                }
                else
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[GatherPhase] Execute: hero gathered nothing " +
                              $"(tokenId={hero.tokenId}, name={hero.cardDef?.cardName ?? "unknown"})");
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[GatherPhase] Execute: gather phase complete " +
                      $"(totalHeroesGathered={result.gatheredByHero.Count}, totalResourcesGathered={result.totalGathered})");

            return result;
        }
    }

    public struct GatherResult
    {
        /// <summary>heroTokenId -> (ResourceType -> amount gathered)</summary>
        public Dictionary<int, Dictionary<ResourceType, int>> gatheredByHero;
        public int totalGathered;
    }
}
