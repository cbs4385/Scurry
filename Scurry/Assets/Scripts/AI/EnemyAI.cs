using System.Collections.Generic;
using UnityEngine;
using Scurry.Core;
using Scurry.Data;
using Scurry.Map;


namespace Scurry.AI
{
    public struct EnemyMoveResult
    {
        public int enemyTokenId;
        public int fromNodeId;
        public int toNodeId;

        public override string ToString()
        {
            return $"EnemyMoveResult(enemyId={enemyTokenId}, from={fromNodeId}, to={toNodeId})";
        }
    }

    public static class EnemyAI
    {
        /// <summary>
        /// Decides the target node for a single enemy based on its behavior.
        /// Returns the target nodeId, or -1 if the enemy stays put.
        /// </summary>
        public static int DecideMove(EnemyToken enemy, List<MapNode> allNodes, List<HeroToken> heroes)
        {
            if (enemy == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning("[EnemyAI] DecideMove: null enemy passed");
                return -1;
            }

            if (enemy.isDefeated)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecideMove: enemy is defeated, skipping (tokenId={enemy.tokenId}, name={enemy.enemyName})");
                return -1;
            }

            if (enemy.speed <= 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecideMove: enemy has speed 0, staying put (tokenId={enemy.tokenId}, name={enemy.enemyName}, behavior={enemy.behavior})");
                return -1;
            }

            MapNode currentNode = FindNode(allNodes, enemy.currentNodeId);
            if (currentNode == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[EnemyAI] DecideMove: could not find current node (tokenId={enemy.tokenId}, name={enemy.enemyName}, nodeId={enemy.currentNodeId})");
                return -1;
            }

            if (currentNode.neighborIds == null || currentNode.neighborIds.Count == 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecideMove: no neighbors available (tokenId={enemy.tokenId}, name={enemy.enemyName}, nodeId={enemy.currentNodeId})");
                return -1;
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecideMove: evaluating (tokenId={enemy.tokenId}, name={enemy.enemyName}, behavior={enemy.behavior}, nodeId={enemy.currentNodeId}, neighborCount={currentNode.neighborIds.Count})");

            switch (enemy.behavior)
            {
                case EnemyBehavior.Guard:
                    return DecideGuard(enemy);

                case EnemyBehavior.Ambush:
                    return DecideAmbush(enemy, currentNode, allNodes, heroes);

                case EnemyBehavior.Chase:
                    return DecideChase(enemy, currentNode, allNodes, heroes);

                case EnemyBehavior.Patrol:
                    return DecidePatrol(enemy, currentNode, allNodes);

                default:
                    if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[EnemyAI] DecideMove: unknown behavior {enemy.behavior} (tokenId={enemy.tokenId}, name={enemy.enemyName})");
                    return -1;
            }
        }

        private static int DecideGuard(EnemyToken enemy)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecideGuard: staying on node (tokenId={enemy.tokenId}, name={enemy.enemyName}, nodeId={enemy.currentNodeId})");
            return -1;
        }

        private static int DecideAmbush(EnemyToken enemy, MapNode currentNode, List<MapNode> allNodes, List<HeroToken> heroes)
        {
            // Ambush: Stay put unless a hero is on an adjacent node, then move to attack
            int adjacentHeroNode = FindAdjacentHeroNode(currentNode, heroes);
            if (adjacentHeroNode >= 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecideAmbush: hero found on adjacent node — moving to attack (tokenId={enemy.tokenId}, name={enemy.enemyName}, targetNode={adjacentHeroNode})");
                return adjacentHeroNode;
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecideAmbush: no adjacent heroes — staying put (tokenId={enemy.tokenId}, name={enemy.enemyName}, nodeId={enemy.currentNodeId})");
            return -1;
        }

        private static int DecideChase(EnemyToken enemy, MapNode currentNode, List<MapNode> allNodes, List<HeroToken> heroes)
        {
            // Chase: If colony node is adjacent, move to colony
            foreach (int neighborId in currentNode.neighborIds)
            {
                MapNode neighbor = FindNode(allNodes, neighborId);
                if (neighbor != null && neighbor.zone == NodeType.Colony)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecideChase: colony node adjacent — moving to colony (tokenId={enemy.tokenId}, name={enemy.enemyName}, targetNode={neighborId})");
                    return neighborId;
                }
            }

            // If any hero on adjacent node, move to that node
            int adjacentHeroNode = FindAdjacentHeroNode(currentNode, heroes);
            if (adjacentHeroNode >= 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecideChase: hero found on adjacent node — chasing (tokenId={enemy.tokenId}, name={enemy.enemyName}, targetNode={adjacentHeroNode})");
                return adjacentHeroNode;
            }

            // Otherwise, patrol
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecideChase: no targets — falling back to patrol (tokenId={enemy.tokenId}, name={enemy.enemyName})");
            return DecidePatrol(enemy, currentNode, allNodes);
        }

        private static int DecidePatrol(EnemyToken enemy, MapNode currentNode, List<MapNode> allNodes)
        {
            // Patrol: Move to random neighbor, prefer same-zone nodes (70% same zone, 30% cross)
            var sameZoneNeighbors = new List<int>();
            var crossZoneNeighbors = new List<int>();

            foreach (int neighborId in currentNode.neighborIds)
            {
                MapNode neighbor = FindNode(allNodes, neighborId);
                if (neighbor == null) continue;

                if (neighbor.zone == enemy.homeZone || neighbor.zone == currentNode.zone)
                {
                    sameZoneNeighbors.Add(neighborId);
                }
                else
                {
                    crossZoneNeighbors.Add(neighborId);
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecidePatrol: evaluating neighbors (tokenId={enemy.tokenId}, name={enemy.enemyName}, sameZone={sameZoneNeighbors.Count}, crossZone={crossZoneNeighbors.Count})");

            // If only one type of neighbor exists, use that
            if (sameZoneNeighbors.Count == 0 && crossZoneNeighbors.Count == 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecidePatrol: no valid neighbors — staying put (tokenId={enemy.tokenId}, name={enemy.enemyName})");
                return -1;
            }

            if (sameZoneNeighbors.Count == 0)
            {
                int targetId = crossZoneNeighbors[SeededRandom.Range(0, crossZoneNeighbors.Count)];
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecidePatrol: no same-zone neighbors — moving cross-zone (tokenId={enemy.tokenId}, name={enemy.enemyName}, targetNode={targetId})");
                return targetId;
            }

            if (crossZoneNeighbors.Count == 0)
            {
                int targetId = sameZoneNeighbors[SeededRandom.Range(0, sameZoneNeighbors.Count)];
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecidePatrol: no cross-zone neighbors — moving same-zone (tokenId={enemy.tokenId}, name={enemy.enemyName}, targetNode={targetId})");
                return targetId;
            }

            // 70% chance same zone, 30% cross zone
            float roll = SeededRandom.Value;
            if (roll < 0.7f)
            {
                int targetId = sameZoneNeighbors[SeededRandom.Range(0, sameZoneNeighbors.Count)];
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecidePatrol: rolled same-zone (roll={roll:F2}, tokenId={enemy.tokenId}, name={enemy.enemyName}, targetNode={targetId})");
                return targetId;
            }
            else
            {
                int targetId = crossZoneNeighbors[SeededRandom.Range(0, crossZoneNeighbors.Count)];
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] DecidePatrol: rolled cross-zone (roll={roll:F2}, tokenId={enemy.tokenId}, name={enemy.enemyName}, targetNode={targetId})");
                return targetId;
            }
        }

        /// <summary>
        /// Finds the first adjacent node that has a hero on it. Returns nodeId or -1.
        /// </summary>
        private static int FindAdjacentHeroNode(MapNode currentNode, List<HeroToken> heroes)
        {
            if (heroes == null) return -1;

            foreach (int neighborId in currentNode.neighborIds)
            {
                foreach (var hero in heroes)
                {
                    if (!hero.isInjured && hero.currentNodeId == neighborId)
                    {
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] FindAdjacentHeroNode: found hero '{(hero.cardDef != null ? hero.cardDef.cardName : "unknown")}' on adjacent node {neighborId}");
                        return neighborId;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Finds a MapNode by nodeId in the list.
        /// </summary>
        private static MapNode FindNode(List<MapNode> allNodes, int nodeId)
        {
            if (allNodes == null) return null;
            foreach (var node in allNodes)
            {
                if (node.nodeId == nodeId)
                    return node;
            }
            return null;
        }

        /// <summary>
        /// Processes movement for all active enemies. Returns a list of move results.
        /// </summary>
        public static List<EnemyMoveResult> ProcessAllEnemies(List<EnemyToken> enemies, List<MapNode> allNodes, List<HeroToken> heroes)
        {
            var results = new List<EnemyMoveResult>();

            if (enemies == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning("[EnemyAI] ProcessAllEnemies: null enemy list");
                return results;
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] ProcessAllEnemies: processing {enemies.Count} enemies");

            foreach (var enemy in enemies)
            {
                if (enemy.isDefeated)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] ProcessAllEnemies: skipping defeated enemy (tokenId={enemy.tokenId}, name={enemy.enemyName}, respawnTimer={enemy.respawnTimer})");
                    continue;
                }

                int targetNode = DecideMove(enemy, allNodes, heroes);
                int fromNode = enemy.currentNodeId;

                if (targetNode >= 0 && targetNode != fromNode)
                {
                    enemy.currentNodeId = targetNode;

                    // Update node tracking lists
                    MapNode oldNode = FindNode(allNodes, fromNode);
                    MapNode newNode = FindNode(allNodes, targetNode);
                    if (oldNode != null)
                    {
                        oldNode.enemyTokenIds.Remove(enemy.tokenId);
                    }
                    if (newNode != null)
                    {
                        if (!newNode.enemyTokenIds.Contains(enemy.tokenId))
                        {
                            newNode.enemyTokenIds.Add(enemy.tokenId);
                        }
                    }

                    var result = new EnemyMoveResult
                    {
                        enemyTokenId = enemy.tokenId,
                        fromNodeId = fromNode,
                        toNodeId = targetNode
                    };
                    results.Add(result);

                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] ProcessAllEnemies: enemy moved (tokenId={enemy.tokenId}, name={enemy.enemyName}, from={fromNode}, to={targetNode})");
                }
                else
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] ProcessAllEnemies: enemy stays (tokenId={enemy.tokenId}, name={enemy.enemyName}, nodeId={enemy.currentNodeId})");
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyAI] ProcessAllEnemies: complete — {results.Count} enemies moved out of {enemies.Count} total");
            return results;
        }
    }
}
