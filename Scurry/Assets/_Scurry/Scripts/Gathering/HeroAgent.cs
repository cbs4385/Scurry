using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Board;
using Scurry.Core;

namespace Scurry.Gathering
{
    public class HeroAgent : MonoBehaviour
    {
        public CardDefinitionSO CardData { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public int RemainingMoves { get; private set; }
        public int CurrentCombat { get; private set; }
        public int RemainingCarry { get; private set; }
        public bool IsWounded { get; private set; }
        public int ResourcesCollected { get; private set; }

        public void Initialize(CardDefinitionSO card, Vector2Int startPos)
        {
            CardData = card;
            GridPosition = startPos;
            RemainingMoves = card.movement;
            CurrentCombat = card.combat;
            RemainingCarry = card.carryCapacity;
            Debug.Log($"[HeroAgent] Initialize: hero='{card.cardName}', gridPos={startPos}, moves={RemainingMoves}, combat={CurrentCombat}, carry={RemainingCarry}");
        }

        public void CollectResourceAtCurrentTile(BoardManager board)
        {
            Tile tile = board.GetTile(GridPosition);
            if (tile == null)
            {
                Debug.LogWarning($"[HeroAgent] CollectResourceAtCurrentTile: hero='{CardData.cardName}' tile at ({GridPosition}) is null");
                return;
            }

            Debug.Log($"[HeroAgent] CollectResourceAtCurrentTile: hero='{CardData.cardName}' at ({GridPosition}), hasResource={tile.HasResource}, remainingCarry={RemainingCarry}");

            if (tile.HasResource && RemainingCarry > 0)
            {
                tile.HasResource = false;
                RemainingCarry--;
                ResourcesCollected++;
                Debug.Log($"[HeroAgent] CollectResourceAtCurrentTile: hero='{CardData.cardName}' collected resource at ({GridPosition}) (remainingCarry={RemainingCarry}, totalCollected={ResourcesCollected})");
                EventBus.OnResourceCollected?.Invoke(ResourceType.Food, 1);
                DestroyResourceOnTile(board, GridPosition);
            }
            else
            {
                Debug.Log($"[HeroAgent] CollectResourceAtCurrentTile: hero='{CardData.cardName}' nothing to collect (hasResource={tile.HasResource}, carry={RemainingCarry})");
            }
        }

        public IEnumerator MoveAlongPath(List<Vector2Int> path, BoardManager board, float stepDelay)
        {
            Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}', pathLength={path.Count}, remainingMoves={RemainingMoves}, stepDelay={stepDelay}");

            // Check starting tile for resources before moving
            CollectResourceAtCurrentTile(board);

            for (int i = 1; i < path.Count && RemainingMoves > 0; i++)
            {
                Vector2Int nextPos = path[i];
                Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' step {i}/{path.Count - 1}, from={GridPosition} to={nextPos}, remainingMoves={RemainingMoves}");
                GridPosition = nextPos;
                RemainingMoves--;

                Vector3 worldTarget = board.GetWorldPosition(nextPos);
                worldTarget.z = -0.1f;

                // Lerp movement
                Vector3 startPos = transform.position;
                float elapsed = 0;
                while (elapsed < stepDelay)
                {
                    elapsed += Time.deltaTime;
                    transform.position = Vector3.Lerp(startPos, worldTarget, elapsed / stepDelay);
                    yield return null;
                }
                transform.position = worldTarget;

                // Check tile effects
                Tile tile = board.GetTile(nextPos);
                if (tile != null)
                {
                    Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' arrived at tile ({nextPos}), type={tile.TileType}, hasResource={tile.HasResource}, enemyStr={tile.EnemyStrength}, enemyDefeated={tile.IsEnemyDefeated}");

                    if (tile.TileType == TileType.EnemyPatrol && !tile.IsEnemyDefeated)
                    {
                        Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' engaging enemy (heroCombat={CurrentCombat}, enemyStr={tile.EnemyStrength})");
                        bool won = CombatResolver.Resolve(CurrentCombat, tile.EnemyStrength);
                        EventBus.OnCombatResolved?.Invoke(CurrentCombat, tile.EnemyStrength, won);

                        if (won)
                        {
                            tile.IsEnemyDefeated = true;
                            DestroyEnemyOnTile(board, nextPos);
                            Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' defeated enemy at ({nextPos})");
                        }
                        else
                        {
                            int damage = tile.EnemyStrength - CurrentCombat;
                            Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' LOST combat — damage to colony={damage}");
                            EventBus.OnColonyHPChanged?.Invoke(-damage, 0);
                            IsWounded = true;
                            Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' is wounded, stopping movement");
                            yield break; // Hero stops moving
                        }
                    }

                    // Collect resource
                    if (tile.HasResource && RemainingCarry > 0)
                    {
                        tile.HasResource = false;
                        RemainingCarry--;
                        ResourcesCollected++;
                        Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' collected resource at ({nextPos}) (remainingCarry={RemainingCarry}, totalCollected={ResourcesCollected})");
                        EventBus.OnResourceCollected?.Invoke(ResourceType.Food, 1);
                        DestroyResourceOnTile(board, nextPos);
                    }
                }
                else
                {
                    Debug.LogWarning($"[HeroAgent] MoveAlongPath: tile at ({nextPos}) is null!");
                }

                EventBus.OnHeroMoved?.Invoke(nextPos);
            }

            Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' finished — finalPos={GridPosition}, remainingMoves={RemainingMoves}, resourcesCollected={ResourcesCollected}, wounded={IsWounded}");
        }

        private void DestroyEnemyOnTile(BoardManager board, Vector2Int pos)
        {
            foreach (Transform child in board.transform)
            {
                if (child.name.StartsWith("Enemy_"))
                {
                    Vector2Int? childGrid = board.GetGridPosition(child.position);
                    if (childGrid.HasValue && childGrid.Value == pos)
                    {
                        Debug.Log($"[HeroAgent] DestroyEnemyOnTile: destroying '{child.name}' at ({pos})");
                        Destroy(child.gameObject);
                        break;
                    }
                }
            }
        }

        private void DestroyResourceOnTile(BoardManager board, Vector2Int pos)
        {
            foreach (Transform child in board.transform)
            {
                if (child.name.StartsWith("Resource_"))
                {
                    Vector2Int? childGrid = board.GetGridPosition(child.position);
                    if (childGrid.HasValue && childGrid.Value == pos)
                    {
                        Debug.Log($"[HeroAgent] DestroyResourceOnTile: destroying '{child.name}' at ({pos})");
                        Destroy(child.gameObject);
                        break;
                    }
                }
            }
        }
    }
}
