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
        private bool patrolIgnored;
        private bool healUsed;
        private bool disarmUsed;

        public void ResetForNextTurn()
        {
            RemainingMoves = CardData.movement;
            CurrentCombat = CardData.combat;
            RemainingCarry = CardData.carryCapacity;
            ResourcesCollected = 0;
            hazardIgnored = false;
            patrolIgnored = false;
            healUsed = false;
            disarmUsed = false;

            if (CardData.specialAbility == SpecialAbility.ExtraCarry)
            {
                RemainingCarry += 1;
                Debug.Log($"[HeroAgent] ResetForNextTurn: ExtraCarry ability — carry increased to {RemainingCarry}");
            }

            if (CardData.specialAbility == SpecialAbility.Frenzy)
            {
                CurrentCombat += 2;
                RemainingCarry = 0;
                Debug.Log($"[HeroAgent] ResetForNextTurn: Frenzy ability — combat +2 to {CurrentCombat}, carry=0");
            }

            Debug.Log($"[HeroAgent] ResetForNextTurn: hero='{CardData.cardName}' reset — moves={RemainingMoves}, combat={CurrentCombat}, carry={RemainingCarry}");
        }

        public void ApplyWound()
        {
            IsWounded = true;
            Debug.Log($"[HeroAgent] ApplyWound: hero='{CardData.cardName}' is now wounded");
        }

        public void HealWound()
        {
            IsWounded = false;
            IsHealing = false;
            Debug.Log($"[HeroAgent] HealWound: hero='{CardData.cardName}' has been healed");
        }

        public void SetHealing()
        {
            IsWounded = true;
            IsHealing = true;
            Debug.Log($"[HeroAgent] SetHealing: hero='{CardData.cardName}' is wounded and healing (will sit out this gathering)");
        }

        public void AddCombatBonus(int amount)
        {
            int prev = CurrentCombat;
            CurrentCombat += amount;
            Debug.Log($"[HeroAgent] AddCombatBonus: hero='{CardData.cardName}' combat {prev} -> {CurrentCombat} (+{amount})");
        }

        public void Initialize(CardDefinitionSO card, Vector2Int startPos)
        {
            CardData = card;
            GridPosition = startPos;
            RemainingMoves = card.movement;
            CurrentCombat = card.combat;
            RemainingCarry = card.carryCapacity;
            hazardIgnored = false;
            patrolIgnored = false;
            healUsed = false;
            disarmUsed = false;
            Debug.Log($"[HeroAgent] Initialize: hero='{card.cardName}', ability={card.specialAbility}, gridPos={startPos}, moves={RemainingMoves}, combat={CurrentCombat}, carry={RemainingCarry}");

            // ExtraCarry ability (Pack Rat, Forager Rat): +1 carry capacity
            if (card.specialAbility == SpecialAbility.ExtraCarry)
            {
                RemainingCarry += 1;
                Debug.Log($"[HeroAgent] Initialize: ExtraCarry ability — carry increased to {RemainingCarry}");
            }

            // Frenzy ability (Berserker Rat): +2 combat, carry=0
            if (card.specialAbility == SpecialAbility.Frenzy)
            {
                CurrentCombat += 2;
                RemainingCarry = 0;
                Debug.Log($"[HeroAgent] Initialize: Frenzy ability — combat +2 to {CurrentCombat}, carry=0");
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
                    string heroName = !string.IsNullOrEmpty(CardData.localizationKey) ? Loc.Get(CardData.localizationKey + ".name") : CardData.cardName;
                    EventBus.OnGatheringNotification?.Invoke(Loc.Format("gather.equip.bonus", heroName, collectedType, collectedValue), new Color(0.5f, 0.5f, 0.9f));
                }

                EventBus.OnResourceCollected?.Invoke(collectedType, collectedValue);

                // BonusFood ability (Scout Rat): +1 bonus Food on Food collection
                if (CardData.specialAbility == SpecialAbility.BonusFood && collectedType == ResourceType.Food)
                {
                    Debug.Log($"[HeroAgent] CollectResourceAtCurrentTile: BonusFood ability — granting +1 bonus Food");
                    EventBus.OnResourceCollected?.Invoke(ResourceType.Food, 1);
                }

                // ShelterBoost ability (Guard Rat): +1 bonus Shelter on Shelter collection
                if (CardData.specialAbility == SpecialAbility.ShelterBoost && collectedType == ResourceType.Shelter)
                {
                    Debug.Log($"[HeroAgent] CollectResourceAtCurrentTile: ShelterBoost ability — granting +1 bonus Shelter");
                    EventBus.OnResourceCollected?.Invoke(ResourceType.Shelter, 1);
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
                        // StealthMove ability (Shadow Rat): ignore first enemy patrol
                        if (CardData.specialAbility == SpecialAbility.StealthMove && !patrolIgnored)
                        {
                            patrolIgnored = true;
                            string heroName = !string.IsNullOrEmpty(CardData.localizationKey) ? Loc.Get(CardData.localizationKey + ".name") : CardData.cardName;
                            Debug.Log($"[HeroAgent] MoveAlongPath: StealthMove ability — hero='{CardData.cardName}' ignoring enemy at ({nextPos})");
                            EventBus.OnGatheringNotification?.Invoke(Loc.Format("gather.hero.stealth", heroName), new Color(0.5f, 0.3f, 0.7f));
                        }
                        else
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
                            string heroName = !string.IsNullOrEmpty(CardData.localizationKey) ? Loc.Get(CardData.localizationKey + ".name") : CardData.cardName;
                            EventBus.OnGatheringNotification?.Invoke(Loc.Format("gather.equip.bonus", heroName, collectedType, collectedValue), new Color(0.5f, 0.5f, 0.9f));
                        }

                        EventBus.OnResourceCollected?.Invoke(collectedType, collectedValue);

                        // BonusFood ability (Scout Rat): +1 bonus Food on Food collection
                        if (CardData.specialAbility == SpecialAbility.BonusFood && collectedType == ResourceType.Food)
                        {
                            Debug.Log($"[HeroAgent] MoveAlongPath: BonusFood ability — granting +1 bonus Food");
                            EventBus.OnResourceCollected?.Invoke(ResourceType.Food, 1);
                        }

                        // ShelterBoost ability (Guard Rat): +1 bonus Shelter on Shelter collection
                        if (CardData.specialAbility == SpecialAbility.ShelterBoost && collectedType == ResourceType.Shelter)
                        {
                            Debug.Log($"[HeroAgent] MoveAlongPath: ShelterBoost ability — granting +1 bonus Shelter");
                            EventBus.OnResourceCollected?.Invoke(ResourceType.Shelter, 1);
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

        /// <summary>
        /// Post-movement abilities: HealAlly and TrapDisarm. Called by GatheringManager after MoveAlongPath completes.
        /// </summary>
        public void ExecutePostMoveAbilities(BoardManager board, List<HeroAgent> allHeroes)
        {
            if (IsWounded) return;

            string heroName = !string.IsNullOrEmpty(CardData.localizationKey) ? Loc.Get(CardData.localizationKey + ".name") : CardData.cardName;

            // HealAlly ability (Healer Rat): heal one adjacent wounded ally
            if (CardData.specialAbility == SpecialAbility.HealAlly && !healUsed)
            {
                var adjacentTiles = board.GetAdjacentTiles(GridPosition);
                foreach (var hero in allHeroes)
                {
                    if (hero == null || hero == this || !hero.IsWounded) continue;
                    foreach (var adjTile in adjacentTiles)
                    {
                        if (hero.GridPosition == adjTile.GridPosition)
                        {
                            string targetName = !string.IsNullOrEmpty(hero.CardData.localizationKey) ? Loc.Get(hero.CardData.localizationKey + ".name") : hero.CardData.cardName;
                            hero.HealWound();
                            healUsed = true;
                            Debug.Log($"[HeroAgent] ExecutePostMoveAbilities: HealAlly ability — hero='{CardData.cardName}' healed '{hero.CardData.cardName}' at ({hero.GridPosition})");
                            EventBus.OnGatheringNotification?.Invoke(Loc.Format("gather.hero.healally", heroName, targetName), new Color(0.4f, 0.9f, 0.7f));
                            goto doneHeal;
                        }
                    }
                }
                doneHeal:;
            }

            // TrapDisarm ability (Sapper Rat): convert adjacent hazard tiles to normal
            if (CardData.specialAbility == SpecialAbility.TrapDisarm && !disarmUsed)
            {
                var adjacentTiles = board.GetAdjacentTiles(GridPosition);
                foreach (var adjTile in adjacentTiles)
                {
                    if (adjTile.TileType == TileType.Hazard)
                    {
                        adjTile.SetAsNormal(new Color(0.3f, 0.7f, 0.3f));
                        disarmUsed = true;
                        Debug.Log($"[HeroAgent] ExecutePostMoveAbilities: TrapDisarm ability — hero='{CardData.cardName}' disarmed hazard at ({adjTile.GridPosition})");
                        EventBus.OnGatheringNotification?.Invoke(Loc.Format("gather.hero.disarm", heroName, adjTile.GridPosition), new Color(0.7f, 0.5f, 0.2f));
                    }
                }
            }
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
