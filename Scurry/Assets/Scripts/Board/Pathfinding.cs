using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;

namespace Scurry.Board
{
    public static class Pathfinding
    {
        private static readonly Vector2Int[] Directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        public static List<Vector2Int> FindPath(Tile[,] grid, Vector2Int start, Vector2Int goal, bool allowHazards = false)
        {
            Debug.Log($"[Pathfinding] FindPath: start={start}, goal={goal}, allowHazards={allowHazards}");
            int rows = grid.GetLength(0);
            int cols = grid.GetLength(1);

            var openSet = new List<Vector2Int> { start };
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float> { [start] = 0 };
            var fScore = new Dictionary<Vector2Int, float> { [start] = Heuristic(start, goal) };
            int iterations = 0;

            while (openSet.Count > 0)
            {
                iterations++;
                // Find node with lowest fScore
                Vector2Int current = openSet[0];
                float bestF = fScore.GetValueOrDefault(current, float.MaxValue);
                for (int i = 1; i < openSet.Count; i++)
                {
                    float f = fScore.GetValueOrDefault(openSet[i], float.MaxValue);
                    if (f < bestF)
                    {
                        bestF = f;
                        current = openSet[i];
                    }
                }

                if (current == goal)
                {
                    var path = ReconstructPath(cameFrom, current);
                    Debug.Log($"[Pathfinding] FindPath: path found in {iterations} iterations, length={path.Count}, path=[{string.Join(" -> ", path)}]");
                    return path;
                }

                openSet.Remove(current);

                foreach (var dir in Directions)
                {
                    Vector2Int neighbor = current + dir;
                    if (neighbor.x < 0 || neighbor.x >= rows || neighbor.y < 0 || neighbor.y >= cols)
                        continue;

                    Tile tile = grid[neighbor.x, neighbor.y];
                    // Hazard tiles are impassable unless allowHazards is true
                    if (tile.TileType == TileType.Hazard && !allowHazards)
                        continue;

                    float moveCost = 1f;
                    // Enemy patrol tiles cost more but are traversable
                    if (tile.TileType == TileType.EnemyPatrol && !tile.IsEnemyDefeated)
                        moveCost = 2f;
                    // Hazard tiles cost more when allowed (high cost to avoid unless necessary)
                    if (tile.TileType == TileType.Hazard && allowHazards)
                        moveCost = 3f;

                    float tentativeG = gScore.GetValueOrDefault(current, float.MaxValue) + moveCost;
                    if (tentativeG < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);
                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }

            Debug.LogWarning($"[Pathfinding] FindPath: NO PATH found from {start} to {goal} after {iterations} iterations");
            return null; // No path found
        }

        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            return path;
        }
    }
}
