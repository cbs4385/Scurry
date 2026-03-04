using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Board;
using Scurry.Core;

namespace Scurry.Gathering
{
    public class GatheringManager : MonoBehaviour
    {
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private float moveStepDelay = 0.3f;
        [SerializeField] private GameObject enemyTokenPrefab;

        private void Awake()
        {
            Debug.Log($"[GatheringManager] Awake: boardManager={boardManager?.name ?? "NULL"}, moveStepDelay={moveStepDelay}, enemyTokenPrefab={enemyTokenPrefab?.name ?? "NULL"}");
        }

        public void SpawnEnemies()
        {
            Debug.Log($"[GatheringManager] SpawnEnemies: scanning {boardManager.Rows}x{boardManager.Cols} grid for EnemyPatrol tiles");
            int spawnCount = 0;
            for (int r = 0; r < boardManager.Rows; r++)
            {
                for (int c = 0; c < boardManager.Cols; c++)
                {
                    Tile tile = boardManager.Grid[r, c];
                    if (tile.TileType == TileType.EnemyPatrol && !tile.IsEnemyDefeated)
                    {
                        Vector3 worldPos = boardManager.GetWorldPosition(new Vector2Int(r, c));
                        worldPos.z = -0.08f;
                        GameObject enemy = Instantiate(enemyTokenPrefab, worldPos, Quaternion.identity, boardManager.transform);
                        enemy.name = $"Enemy_{r}_{c}";
                        var sr = enemy.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            SpriteHelper.EnsureSprite(sr);
                            sr.color = new Color(1f, 0.2f, 0.2f);
                            sr.sortingOrder = 3;
                        }
                        spawnCount++;
                        Debug.Log($"[GatheringManager] SpawnEnemies: spawned enemy at gridPos=({r},{c}), worldPos={worldPos}");
                    }
                }
            }
            Debug.Log($"[GatheringManager] SpawnEnemies: complete — spawned {spawnCount} enemies");
        }

        public IEnumerator RunGathering()
        {
            Debug.Log("[GatheringManager] RunGathering: starting gathering phase");
            var heroes = new List<HeroAgent>(boardManager.GetComponentsInChildren<HeroAgent>());
            Debug.Log($"[GatheringManager] RunGathering: found {heroes.Count} heroes on the board");

            heroes.Sort((a, b) => b.CardData.movement.CompareTo(a.CardData.movement));
            for (int i = 0; i < heroes.Count; i++)
                Debug.Log($"[GatheringManager] RunGathering: hero[{i}]='{heroes[i].CardData.cardName}' (move={heroes[i].CardData.movement}, combat={heroes[i].CardData.combat}, carry={heroes[i].CardData.carryCapacity}) at gridPos={heroes[i].GridPosition}");

            foreach (var hero in heroes)
            {
                if (hero.IsWounded)
                {
                    Debug.Log($"[GatheringManager] RunGathering: skipping wounded hero '{hero.CardData.cardName}'");
                    continue;
                }

                Vector2Int? target = FindNearestResource(hero.GridPosition);
                if (!target.HasValue)
                {
                    Debug.Log($"[GatheringManager] RunGathering: no resource target found for hero '{hero.CardData.cardName}'");
                    continue;
                }

                Debug.Log($"[GatheringManager] RunGathering: hero '{hero.CardData.cardName}' targeting resource at {target.Value}");

                // If resource is on the same tile, collect it directly
                if (target.Value == hero.GridPosition)
                {
                    Debug.Log($"[GatheringManager] RunGathering: hero '{hero.CardData.cardName}' resource is on same tile — collecting directly");
                    hero.CollectResourceAtCurrentTile(boardManager);
                }
                else
                {
                    List<Vector2Int> path = Pathfinding.FindPath(boardManager.Grid, hero.GridPosition, target.Value);
                    if (path == null || path.Count <= 1)
                    {
                        Debug.Log($"[GatheringManager] RunGathering: no valid path for hero '{hero.CardData.cardName}' from {hero.GridPosition} to {target.Value}");
                        continue;
                    }

                    Debug.Log($"[GatheringManager] RunGathering: hero '{hero.CardData.cardName}' moving along path of length {path.Count}");
                    yield return StartCoroutine(hero.MoveAlongPath(path, boardManager, moveStepDelay));
                }
                Debug.Log($"[GatheringManager] RunGathering: hero '{hero.CardData.cardName}' finished moving (wounded={hero.IsWounded}, resourcesCollected={hero.ResourcesCollected})");
                yield return new WaitForSeconds(0.2f);
            }

            Debug.Log("[GatheringManager] RunGathering: all heroes processed — invoking OnGatheringComplete");
            EventBus.OnGatheringComplete?.Invoke();
        }

        private Vector2Int? FindNearestResource(Vector2Int from)
        {
            Vector2Int? nearest = null;
            float bestDist = float.MaxValue;

            for (int r = 0; r < boardManager.Rows; r++)
            {
                for (int c = 0; c < boardManager.Cols; c++)
                {
                    Tile tile = boardManager.Grid[r, c];
                    if (tile.HasResource)
                    {
                        float dist = Mathf.Abs(from.x - r) + Mathf.Abs(from.y - c);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            nearest = new Vector2Int(r, c);
                        }
                    }
                }
            }

            Debug.Log($"[GatheringManager] FindNearestResource: from={from}, nearest={nearest?.ToString() ?? "NONE"}, distance={bestDist}");
            return nearest;
        }
    }
}
