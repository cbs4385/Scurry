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
        public EnemyDefinitionSO Definition { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public int Strength { get; private set; }
        public int Speed { get; private set; }
        public bool IsDefeated { get; private set; }
        public EnemyBehavior Behavior { get; private set; }

        private const int PatrolChaseRange = 2;
        private const int ChaseChaseRange = 3;
        private bool ambushTriggered;
        private Vector2Int guardTile;

        public string DisplayName
        {
            get
            {
                if (Definition != null && !string.IsNullOrEmpty(Definition.localizationKey))
                    return Loc.Get(Definition.localizationKey + ".name");
                if (Definition != null)
                    return Definition.enemyName;
                return $"Enemy at {GridPosition}";
            }
        }

        public void Initialize(EnemyDefinitionSO def, Vector2Int startPos)
        {
            Definition = def;
            GridPosition = startPos;
            Strength = def.strength;
            Speed = def.speed;
            Behavior = def.behavior;
            IsDefeated = false;
            ambushTriggered = false;
            guardTile = startPos;
            Debug.Log($"[EnemyAgent] Initialize: name='{def.enemyName}', gridPos={startPos}, strength={Strength}, speed={Speed}, behavior={Behavior}");
        }

        public void Initialize(Vector2Int startPos, int strength, int speed = 2)
        {
            GridPosition = startPos;
            Strength = strength;
            Speed = speed;
            Behavior = EnemyBehavior.Patrol;
            IsDefeated = false;
            ambushTriggered = false;
            guardTile = startPos;
            Debug.Log($"[EnemyAgent] Initialize: gridPos={startPos}, strength={strength}, speed={speed}, behavior=Patrol (legacy)");
        }

        public Vector2Int? DecideMove(BoardManager board, List<HeroAgent> heroes, out HeroAgent chaseTarget)
        {
            Debug.Log($"[EnemyAgent] DecideMove: enemy at {GridPosition}, behavior={Behavior}, checking {heroes.Count} heroes");
            chaseTarget = null;

            if (Behavior == EnemyBehavior.Guard)
            {
                Debug.Log($"[EnemyAgent] DecideMove: GUARD — staying on guard tile {guardTile}");
                return null;
            }

            int chaseRange = Behavior == EnemyBehavior.Chase ? ChaseChaseRange : PatrolChaseRange;

            if (Behavior == EnemyBehavior.Ambush && !ambushTriggered)
            {
                // Check if any hero is adjacent
                bool heroAdjacent = false;
                foreach (var hero in heroes)
                {
                    if (hero == null || hero.IsWounded) continue;
                    int dist = ManhattanDistance(GridPosition, hero.GridPosition);
                    Debug.Log($"[EnemyAgent] DecideMove: (ambush check) hero '{hero.CardData.cardName}' at {hero.GridPosition}, dist={dist}");
                    if (dist <= 1)
                    {
                        heroAdjacent = true;
                        break;
                    }
                }
                if (!heroAdjacent)
                {
                    Debug.Log($"[EnemyAgent] DecideMove: AMBUSH — no hero adjacent, staying hidden");
                    return null;
                }
                ambushTriggered = true;
                Debug.Log($"[EnemyAgent] DecideMove: AMBUSH TRIGGERED — hero adjacent, switching to chase");
                chaseRange = ChaseChaseRange;
            }

            // Find nearest non-wounded hero within chase range
            HeroAgent closestHero = null;
            int closestDist = int.MaxValue;

            foreach (var hero in heroes)
            {
                if (hero == null || hero.IsWounded) continue;
                int dist = ManhattanDistance(GridPosition, hero.GridPosition);
                Debug.Log($"[EnemyAgent] DecideMove: hero '{hero.CardData.cardName}' at {hero.GridPosition}, dist={dist}");
                if (dist <= chaseRange && dist < closestDist)
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
                if (Behavior == EnemyBehavior.Ambush)
                {
                    Debug.Log($"[EnemyAgent] DecideMove: AMBUSH (triggered but no hero in range) — staying put");
                    return null;
                }
                Debug.Log($"[EnemyAgent] DecideMove: PATROL — no hero in range ({chaseRange}), moving randomly");
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

            var chosen = walkable[SeededRandom.Range(0, walkable.Count)];
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
