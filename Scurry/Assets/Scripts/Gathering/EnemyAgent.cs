using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Board;
using Scurry.Core;

namespace Scurry.Gathering
{
    public class EnemyAgent : MonoBehaviour
    {
        public Vector2Int GridPosition { get; private set; }
        public int Strength { get; private set; }
        public int Speed { get; private set; }
        public bool IsDefeated { get; private set; }

        private const int ChaseRange = 2;

        public void Initialize(Vector2Int startPos, int strength, int speed = 2)
        {
            GridPosition = startPos;
            Strength = strength;
            Speed = speed;
            IsDefeated = false;
            Debug.Log($"[EnemyAgent] Initialize: gridPos={startPos}, strength={strength}, speed={speed}");
        }

        /// <summary>
        /// Decide the enemy's next move. Sets chaseTarget to the hero being chased (or null if patrolling).
        /// </summary>
        public Vector2Int? DecideMove(BoardManager board, List<HeroAgent> heroes, out HeroAgent chaseTarget)
        {
            Debug.Log($"[EnemyAgent] DecideMove: enemy at {GridPosition}, checking {heroes.Count} heroes");
            chaseTarget = null;

            // Find nearest non-wounded hero within chase range
            HeroAgent closestHero = null;
            int closestDist = int.MaxValue;

            foreach (var hero in heroes)
            {
                if (hero == null || hero.IsWounded) continue;
                int dist = ManhattanDistance(GridPosition, hero.GridPosition);
                Debug.Log($"[EnemyAgent] DecideMove: hero '{hero.CardData.cardName}' at {hero.GridPosition}, dist={dist}");
                if (dist <= ChaseRange && dist < closestDist)
                {
                    closestDist = dist;
                    closestHero = hero;
                }
            }

            if (closestHero != null)
            {
                Debug.Log($"[EnemyAgent] DecideMove: CHASE — targeting hero '{closestHero.CardData.cardName}' at {closestHero.GridPosition} (dist={closestDist})");
                chaseTarget = closestHero;
                return GetStepToward(board, closestHero.GridPosition);
            }
            else
            {
                Debug.Log($"[EnemyAgent] DecideMove: PATROL — no hero in range ({ChaseRange}), moving randomly");
                return GetRandomAdjacentWalkable(board);
            }
        }

        private Vector2Int? GetStepToward(BoardManager board, Vector2Int target)
        {
            var adjacent = board.GetAdjacentTiles(GridPosition);
            Vector2Int? best = null;
            int bestDist = ManhattanDistance(GridPosition, target);

            foreach (var tile in adjacent)
            {
                if (tile.TileType == TileType.Hazard) continue;
                int dist = ManhattanDistance(tile.GridPosition, target);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = tile.GridPosition;
                }
            }

            Debug.Log($"[EnemyAgent] GetStepToward: from={GridPosition}, target={target}, bestStep={best?.ToString() ?? "NONE"}, bestDist={bestDist}");
            return best;
        }

        private Vector2Int? GetRandomAdjacentWalkable(BoardManager board)
        {
            var adjacent = board.GetAdjacentTiles(GridPosition);
            var walkable = new List<Tile>();
            foreach (var tile in adjacent)
            {
                if (tile.TileType != TileType.Hazard)
                    walkable.Add(tile);
            }

            if (walkable.Count == 0)
            {
                Debug.Log($"[EnemyAgent] GetRandomAdjacentWalkable: no walkable tiles from {GridPosition}");
                return null;
            }

            var chosen = walkable[Random.Range(0, walkable.Count)];
            Debug.Log($"[EnemyAgent] GetRandomAdjacentWalkable: chose {chosen.GridPosition} from {walkable.Count} options");
            return chosen.GridPosition;
        }

        public IEnumerator MoveToTile(Vector2Int targetPos, BoardManager board, float stepDelay)
        {
            Debug.Log($"[EnemyAgent] MoveToTile: from={GridPosition} to={targetPos}, stepDelay={stepDelay}");
            GridPosition = targetPos;

            Vector3 worldTarget = board.GetWorldPosition(targetPos);
            worldTarget.z = -0.08f;

            Vector3 startPos = transform.position;
            float elapsed = 0f;
            while (elapsed < stepDelay)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, worldTarget, elapsed / stepDelay);
                yield return null;
            }
            transform.position = worldTarget;

            Debug.Log($"[EnemyAgent] MoveToTile: arrived at {targetPos}, worldPos={worldTarget}");
            EventBus.OnEnemyMoved?.Invoke(targetPos);
        }

        public bool EngageHero(HeroAgent hero, BoardManager board)
        {
            Debug.Log($"[EnemyAgent] EngageHero: enemy(str={Strength}) at {GridPosition} vs hero '{hero.CardData.cardName}'(combat={hero.CurrentCombat}) at {hero.GridPosition}");

            // Shelter adjacency defense
            int shelterDefense = 0;
            int shelterMultiplier = (hero.CardData.specialAbility == SpecialAbility.ShelterBoost) ? 3 : 1;
            var adjacentTiles = board.GetAdjacentTiles(hero.GridPosition);
            foreach (var adjTile in adjacentTiles)
            {
                if (adjTile.HasResource && adjTile.StoredResourceType == ResourceType.Shelter)
                {
                    int defValue = adjTile.StoredResourceValue * shelterMultiplier;
                    shelterDefense += defValue;
                    Debug.Log($"[EnemyAgent] EngageHero: shelter from ({adjTile.GridPosition}) — base={adjTile.StoredResourceValue}, multiplier={shelterMultiplier}, effective={defValue}");
                }
            }

            int effectiveStrength = Mathf.Max(0, Strength - shelterDefense);
            Debug.Log($"[EnemyAgent] EngageHero: effectiveStrength={effectiveStrength} (base={Strength}, shelterDef={shelterDefense})");

            bool heroWins = CombatResolver.Resolve(hero.CurrentCombat, effectiveStrength);
            EventBus.OnCombatResolved?.Invoke(hero.CurrentCombat, effectiveStrength, heroWins);

            if (heroWins)
            {
                Debug.Log($"[EnemyAgent] EngageHero: hero '{hero.CardData.cardName}' WINS — enemy defeated");
                Defeat();
                return false;
            }
            else
            {
                if (hero.CardData.specialAbility == SpecialAbility.NoDamageOnWin)
                {
                    Debug.Log($"[EnemyAgent] EngageHero: hero '{hero.CardData.cardName}' lost but NoDamageOnWin — no wound");
                    return false;
                }
                int damage = effectiveStrength - hero.CurrentCombat;
                Debug.Log($"[EnemyAgent] EngageHero: hero '{hero.CardData.cardName}' LOSES — damage={damage}, wounding hero");
                EventBus.OnColonyHPChanged?.Invoke(-damage, 0);
                hero.ApplyWound();
                return true;
            }
        }

        public void Defeat()
        {
            IsDefeated = true;
            Debug.Log($"[EnemyAgent] Defeat: enemy at {GridPosition} defeated — destroying gameObject");
            Destroy(gameObject);
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
