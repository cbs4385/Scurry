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
        public bool IsHealing { get; private set; }
        public int ResourcesCollected { get; private set; }
        private bool hazardIgnored;

        public void ResetForNextTurn()
        {
            RemainingMoves = CardData.movement;
            CurrentCombat = CardData.combat;
            RemainingCarry = CardData.carryCapacity;
            ResourcesCollected = 0;
            hazardIgnored = false;

            if (CardData.specialAbility == SpecialAbility.ExtraCarry)
            {
                RemainingCarry += 1;
                Debug.Log($"[HeroAgent] ResetForNextTurn: ExtraCarry ability — carry increased to {RemainingCarry}");
            }

            Debug.Log($"[HeroAgent] ResetForNextTurn: hero='{CardData.cardName}' reset — moves={RemainingMoves}, combat={CurrentCombat}, carry={RemainingCarry}");
        }

        public void ApplyWound()
        {
            IsWounded = true;
            Debug.Log($"[HeroAgent] ApplyWound: hero='{CardData.cardName}' is now wounded");
        }

        public void SetHealing()
        {
            IsWounded = true;
            IsHealing = true;
            Debug.Log($"[HeroAgent] SetHealing: hero='{CardData.cardName}' is wounded and healing (will sit out this gathering)");
        }

        public void Initialize(CardDefinitionSO card, Vector2Int startPos)
        {
            CardData = card;
            GridPosition = startPos;
            RemainingMoves = card.movement;
            CurrentCombat = card.combat;
            RemainingCarry = card.carryCapacity;
            hazardIgnored = false;
            Debug.Log($"[HeroAgent] Initialize: hero='{card.cardName}', ability={card.specialAbility}, gridPos={startPos}, moves={RemainingMoves}, combat={CurrentCombat}, carry={RemainingCarry}");

            // ExtraCarry ability (Pack Rat): +1 carry capacity
            if (card.specialAbility == SpecialAbility.ExtraCarry)
            {
                RemainingCarry += 1;
                Debug.Log($"[HeroAgent] Initialize: ExtraCarry ability — carry increased to {RemainingCarry}");
            }
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
                ResourceType collectedType = tile.StoredResourceType;
                int collectedValue = tile.StoredResourceValue;
                tile.HasResource = false;
                tile.StoredResourceType = default;
                tile.StoredResourceValue = 0;
                RemainingCarry--;
                ResourcesCollected++;
                Debug.Log($"[HeroAgent] CollectResourceAtCurrentTile: hero='{CardData.cardName}' collected {collectedType} (value={collectedValue}) at ({GridPosition}) (remainingCarry={RemainingCarry}, totalCollected={ResourcesCollected})");
                if (collectedType == ResourceType.Equipment)
                {
                    int prevCombat = CurrentCombat;
                    CurrentCombat += collectedValue;
                    Debug.Log($"[HeroAgent] CollectResourceAtCurrentTile: Equipment buff — combat {prevCombat} -> {CurrentCombat} (+{collectedValue})");
                }
                EventBus.OnResourceCollected?.Invoke(collectedType, collectedValue);
                // BonusFood ability (Scout Rat): +1 bonus Food on Food collection
                if (CardData.specialAbility == SpecialAbility.BonusFood && collectedType == ResourceType.Food)
                {
                    Debug.Log($"[HeroAgent] CollectResourceAtCurrentTile: BonusFood ability — granting +1 bonus Food");
                    EventBus.OnResourceCollected?.Invoke(ResourceType.Food, 1);
                }
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
                        // Calculate shelter adjacency defense
                        int shelterDefense = 0;
                        int shelterMultiplier = (CardData.specialAbility == SpecialAbility.ShelterBoost) ? 3 : 1;
                        var adjacentTiles = board.GetAdjacentTiles(nextPos);
                        foreach (var adjTile in adjacentTiles)
                        {
                            if (adjTile.HasResource && adjTile.StoredResourceType == ResourceType.Shelter)
                            {
                                int defValue = adjTile.StoredResourceValue * shelterMultiplier;
                                shelterDefense += defValue;
                                Debug.Log($"[HeroAgent] MoveAlongPath: shelter defense from ({adjTile.GridPosition}) — baseValue={adjTile.StoredResourceValue}, multiplier={shelterMultiplier}, effective={defValue}");
                            }
                        }
                        int effectiveEnemyStrength = Mathf.Max(0, tile.EnemyStrength - shelterDefense);
                        Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' engaging enemy (heroCombat={CurrentCombat}, baseEnemyStr={tile.EnemyStrength}, shelterDef={shelterDefense}, effectiveEnemyStr={effectiveEnemyStrength})");

                        bool won = CombatResolver.Resolve(CurrentCombat, effectiveEnemyStrength);
                        EventBus.OnCombatResolved?.Invoke(CurrentCombat, effectiveEnemyStrength, won);

                        if (won)
                        {
                            DestroyEnemyOnTile(board, nextPos);
                            Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' defeated enemy at ({nextPos}) — tile will transition in resolve phase");
                        }
                        else
                        {
                            // NoDamageOnWin ability (Brawler Rat): survives combat loss without wounding
                            if (CardData.specialAbility == SpecialAbility.NoDamageOnWin)
                            {
                                Debug.Log($"[HeroAgent] MoveAlongPath: NoDamageOnWin ability — hero='{CardData.cardName}' lost combat but takes no wound, continuing movement");
                            }
                            else
                            {
                                int damage = effectiveEnemyStrength - CurrentCombat;
                                Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' LOST combat — damage to colony={damage}");
                                EventBus.OnColonyHPChanged?.Invoke(-damage, 0);
                                IsWounded = true;
                                Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' is wounded, stopping movement");
                                yield break; // Hero stops moving
                            }
                        }
                    }

                    // Hazard tile handling
                    if (tile.TileType == TileType.Hazard)
                    {
                        if (CardData.specialAbility == SpecialAbility.IgnoreFirstHazard && !hazardIgnored)
                        {
                            hazardIgnored = true;
                            Debug.Log($"[HeroAgent] MoveAlongPath: IgnoreFirstHazard ability — hero='{CardData.cardName}' ignoring hazard at ({nextPos})");
                        }
                        else
                        {
                            Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' hit hazard at ({nextPos}), damage={tile.HazardDamage}");
                            EventBus.OnColonyHPChanged?.Invoke(-tile.HazardDamage, 0);
                            IsWounded = true;
                            Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' is wounded by hazard, stopping movement");
                            yield break;
                        }
                    }

                    // Collect resource
                    if (tile.HasResource && RemainingCarry > 0)
                    {
                        ResourceType collectedType = tile.StoredResourceType;
                        int collectedValue = tile.StoredResourceValue;
                        tile.HasResource = false;
                        tile.StoredResourceType = default;
                        tile.StoredResourceValue = 0;
                        RemainingCarry--;
                        ResourcesCollected++;
                        Debug.Log($"[HeroAgent] MoveAlongPath: hero='{CardData.cardName}' collected {collectedType} (value={collectedValue}) at ({nextPos}) (remainingCarry={RemainingCarry}, totalCollected={ResourcesCollected})");
                        if (collectedType == ResourceType.Equipment)
                        {
                            int prevCombat = CurrentCombat;
                            CurrentCombat += collectedValue;
                            Debug.Log($"[HeroAgent] MoveAlongPath: Equipment buff — combat {prevCombat} -> {CurrentCombat} (+{collectedValue})");
                        }
                        EventBus.OnResourceCollected?.Invoke(collectedType, collectedValue);
                        // BonusFood ability (Scout Rat): +1 bonus Food on Food collection
                        if (CardData.specialAbility == SpecialAbility.BonusFood && collectedType == ResourceType.Food)
                        {
                            Debug.Log($"[HeroAgent] MoveAlongPath: BonusFood ability — granting +1 bonus Food");
                            EventBus.OnResourceCollected?.Invoke(ResourceType.Food, 1);
                        }
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
                        EventBus.OnResourceTokenCollected?.Invoke(child.name);
                        Destroy(child.gameObject);
                        break;
                    }
                }
            }
        }
    }
}
