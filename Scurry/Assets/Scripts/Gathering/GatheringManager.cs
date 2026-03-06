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

                        SpriteHelper.AddOutline(enemy, 3);

                        // Attach EnemyAgent for AI behavior
                        var enemyAgent = enemy.GetComponent<EnemyAgent>();
                        if (enemyAgent == null)
                            enemyAgent = enemy.AddComponent<EnemyAgent>();
                        enemyAgent.Initialize(new Vector2Int(r, c), tile.EnemyStrength);
                        Debug.Log($"[GatheringManager] SpawnEnemies: spawned enemy with EnemyAgent at gridPos=({r},{c}), strength={tile.EnemyStrength}, speed={enemyAgent.Speed}");

                        spawnCount++;
                    }
                }
            }
            Debug.Log($"[GatheringManager] SpawnEnemies: complete — spawned {spawnCount} enemies");
        }

        public void SpawnEnemyAt(Vector2Int gridPos, int strength)
        {
            Debug.Log($"[GatheringManager] SpawnEnemyAt: spawning enemy at gridPos={gridPos}, strength={strength}");
            Vector3 worldPos = boardManager.GetWorldPosition(gridPos);
            worldPos.z = -0.08f;
            GameObject enemy = Instantiate(enemyTokenPrefab, worldPos, Quaternion.identity, boardManager.transform);
            enemy.name = $"Enemy_{gridPos.x}_{gridPos.y}";
            var sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                SpriteHelper.EnsureSprite(sr);
                sr.color = new Color(1f, 0.2f, 0.2f);
                sr.sortingOrder = 3;
            }
            SpriteHelper.AddOutline(enemy, 3);
            var enemyAgent = enemy.GetComponent<EnemyAgent>();
            if (enemyAgent == null)
                enemyAgent = enemy.AddComponent<EnemyAgent>();
            enemyAgent.Initialize(gridPos, strength);
            Debug.Log($"[GatheringManager] SpawnEnemyAt: spawned enemy at gridPos={gridPos}, strength={strength}, speed={enemyAgent.Speed}");
        }

        public void SpawnEnemyFromDefinition(EnemyDefinitionSO def, Vector2Int gridPos)
        {
            Debug.Log($"[GatheringManager] SpawnEnemyFromDefinition: spawning '{def.enemyName}' at gridPos={gridPos}, strength={def.strength}, speed={def.speed}, behavior={def.behavior}");
            Vector3 worldPos = boardManager.GetWorldPosition(gridPos);
            worldPos.z = -0.08f;
            GameObject enemy = Instantiate(enemyTokenPrefab, worldPos, Quaternion.identity, boardManager.transform);
            enemy.name = $"Enemy_{def.enemyName.Replace(" ", "")}_{gridPos.x}_{gridPos.y}";
            var sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                SpriteHelper.EnsureSprite(sr);
                sr.color = def.tokenColor;
                sr.sortingOrder = 3;
            }
            SpriteHelper.AddOutline(enemy, 3);
            var enemyAgent = enemy.GetComponent<EnemyAgent>();
            if (enemyAgent == null)
                enemyAgent = enemy.AddComponent<EnemyAgent>();
            enemyAgent.Initialize(def, gridPos);
            Debug.Log($"[GatheringManager] SpawnEnemyFromDefinition: spawned '{def.enemyName}' at gridPos={gridPos}");
        }

        public IEnumerator RunGathering()
        {
            Debug.Log("[GatheringManager] RunGathering: starting gathering phase");
            float speedMult = Core.GameSettings.Instance != null ? Core.GameSettings.Instance.BattleWaitMultiplier : 1f;

            // Gather all actors
            var heroes = new List<HeroAgent>(boardManager.GetComponentsInChildren<HeroAgent>());
            var enemies = new List<EnemyAgent>(boardManager.GetComponentsInChildren<EnemyAgent>());
            Debug.Log($"[GatheringManager] RunGathering: found {heroes.Count} heroes and {enemies.Count} enemies");

            // Build unified initiative list: (speed, isHero, index)
            // Heroes use CardData.movement; enemies use Speed property
            // Sort descending by speed, heroes win ties
            var initiative = new List<(int speed, bool isHero, int index)>();

            for (int i = 0; i < heroes.Count; i++)
                initiative.Add((heroes[i].CardData.movement, true, i));
            for (int i = 0; i < enemies.Count; i++)
                initiative.Add((enemies[i].Speed, false, i));

            initiative.Sort((a, b) =>
            {
                int cmp = b.speed.CompareTo(a.speed);
                if (cmp != 0) return cmp;
                return b.isHero.CompareTo(a.isHero); // heroes win ties
            });

            Debug.Log($"[GatheringManager] RunGathering: initiative order ({initiative.Count} actors):");
            for (int i = 0; i < initiative.Count; i++)
            {
                var entry = initiative[i];
                string actorName = entry.isHero
                    ? heroes[entry.index].CardData.cardName
                    : $"Enemy at {enemies[entry.index].GridPosition} (str={enemies[entry.index].Strength})";
                Debug.Log($"[GatheringManager]   [{i}] {(entry.isHero ? "HERO" : "ENEMY")} '{actorName}' speed={entry.speed}");
            }

            // Process each actor in initiative order
            foreach (var entry in initiative)
            {
                if (entry.isHero)
                {
                    // --- HERO TURN ---
                    var hero = heroes[entry.index];
                    if (hero == null || hero.IsWounded)
                    {
                        Debug.Log($"[GatheringManager] RunGathering: skipping hero '{hero?.CardData?.cardName ?? "NULL"}' (wounded={hero?.IsWounded}, healing={hero?.IsHealing})");
                        if (hero != null && hero.IsHealing)
                        {
                            string healName = !string.IsNullOrEmpty(hero.CardData.localizationKey) ? Loc.Get(hero.CardData.localizationKey + ".name") : hero.CardData.cardName;
                            EventBus.OnGatheringNotification?.Invoke(Loc.Format("gather.hero.healing", healName), new Color(0.4f, 0.8f, 1f));
                        }
                        continue;
                    }

                    // RallyAll ability (Elder Rat): buff all non-wounded heroes +1 combat
                    if (hero.CardData.specialAbility == SpecialAbility.RallyAll)
                    {
                        string rallyName = !string.IsNullOrEmpty(hero.CardData.localizationKey) ? Loc.Get(hero.CardData.localizationKey + ".name") : hero.CardData.cardName;
                        Debug.Log($"[GatheringManager] RunGathering: RallyAll ability — '{hero.CardData.cardName}' buffing all allies +1 combat");
                        foreach (var ally in heroes)
                        {
                            if (ally == null || ally.IsWounded) continue;
                            ally.AddCombatBonus(1);
                            Debug.Log($"[GatheringManager] RunGathering: RallyAll — '{ally.CardData.cardName}' combat now {ally.CurrentCombat}");
                        }
                        EventBus.OnGatheringNotification?.Invoke(Loc.Format("gather.hero.rally", rallyName), new Color(0.9f, 0.8f, 0.3f));
                    }

                    Debug.Log($"[GatheringManager] RunGathering: HERO TURN — '{hero.CardData.cardName}' (moves={hero.RemainingMoves}, carry={hero.RemainingCarry})");
                    Color heroColor = new Color(0.4f, 0.9f, 0.4f);
                    string heroDisplayName = !string.IsNullOrEmpty(hero.CardData.localizationKey) ? Loc.Get(hero.CardData.localizationKey + ".name") : hero.CardData.cardName;
                    EventBus.OnGatheringNotification?.Invoke(Loc.Format("gather.hero.begin", heroDisplayName, hero.CardData.movement), heroColor);

                    int startMoves = hero.RemainingMoves;
                    int startCarry = hero.RemainingCarry;

                    // Loop: keep seeking resources while hero has moves and carry capacity
                    while (hero.RemainingMoves > 0 && hero.RemainingCarry > 0 && !hero.IsWounded)
                    {
                        Vector2Int? target = FindNearestResource(hero.GridPosition);
                        if (!target.HasValue)
                        {
                            Debug.Log($"[GatheringManager] RunGathering: no more resources for hero '{hero.CardData.cardName}' — ending turn");
                            break;
                        }

                        Debug.Log($"[GatheringManager] RunGathering: hero '{hero.CardData.cardName}' targeting resource at {target.Value} (moves={hero.RemainingMoves}, carry={hero.RemainingCarry})");

                        if (target.Value == hero.GridPosition)
                        {
                            Debug.Log($"[GatheringManager] RunGathering: hero '{hero.CardData.cardName}' resource on same tile — collecting directly");
                            hero.CollectResourceAtCurrentTile(boardManager);
                        }
                        else
                        {
                            bool allowHazards = hero.CardData.specialAbility == SpecialAbility.IgnoreFirstHazard;
                            List<Vector2Int> path = Pathfinding.FindPath(boardManager.Grid, hero.GridPosition, target.Value, allowHazards);
                            if (path == null || path.Count <= 1)
                            {
                                Debug.Log($"[GatheringManager] RunGathering: no valid path for hero '{hero.CardData.cardName}' — ending turn");
                                break;
                            }

                            Debug.Log($"[GatheringManager] RunGathering: hero '{hero.CardData.cardName}' moving along path of length {path.Count}");
                            yield return StartCoroutine(hero.MoveAlongPath(path, boardManager, moveStepDelay));
                        }
                    }

                    // Post-move abilities (HealAlly, TrapDisarm)
                    hero.ExecutePostMoveAbilities(boardManager, heroes);

                    // Summary notification
                    int movesMade = startMoves - hero.RemainingMoves;
                    int resourcesGathered = startCarry - hero.RemainingCarry;
                    string heroSummary = Loc.Format("gather.hero.summary", heroDisplayName, movesMade, resourcesGathered);
                    if (hero.IsWounded) heroSummary += Loc.Get("gather.hero.wounded");
                    EventBus.OnGatheringNotification?.Invoke(heroSummary, heroColor);
                    Debug.Log($"[GatheringManager] RunGathering: hero '{hero.CardData.cardName}' turn done (wounded={hero.IsWounded}, moves={hero.RemainingMoves}, carry={hero.RemainingCarry}, collected={hero.ResourcesCollected})");
                }
                else
                {
                    // --- ENEMY TURN ---
                    var enemy = enemies[entry.index];
                    if (enemy == null || enemy.IsDefeated)
                    {
                        Debug.Log("[GatheringManager] RunGathering: skipping defeated/destroyed enemy");
                        continue;
                    }

                    Debug.Log($"[GatheringManager] RunGathering: ENEMY TURN — enemy at {enemy.GridPosition} (str={enemy.Strength})");
                    Color enemyColor = new Color(1f, 0.4f, 0.4f);

                    var liveHeroes = heroes.FindAll(h => h != null && !h.IsWounded);
                    HeroAgent chaseTarget;
                    Vector2Int? moveTarget = enemy.DecideMove(boardManager, liveHeroes, out chaseTarget);

                    if (!moveTarget.HasValue)
                    {
                        EventBus.OnGatheringNotification?.Invoke(Loc.Format("gather.enemy.holds", enemy.Strength), enemyColor);
                        Debug.Log($"[GatheringManager] RunGathering: enemy at {enemy.GridPosition} has no valid move — staying put");
                        yield return new WaitForSeconds(0.2f * speedMult);
                        continue;
                    }

                    yield return StartCoroutine(enemy.MoveToTile(moveTarget.Value, boardManager, moveStepDelay));

                    if (chaseTarget != null)
                    {
                        string chaseName = !string.IsNullOrEmpty(chaseTarget.CardData.localizationKey) ? Loc.Get(chaseTarget.CardData.localizationKey + ".name") : chaseTarget.CardData.cardName;
                        EventBus.OnGatheringNotification?.Invoke(Loc.Format("gather.enemy.chases", chaseName), enemyColor);
                    }
                    else
                        EventBus.OnGatheringNotification?.Invoke(Loc.Format("gather.enemy.patrols", moveTarget.Value), enemyColor);

                    // Check if enemy landed on a hero's tile
                    foreach (var hero in heroes)
                    {
                        if (hero == null || hero.IsWounded) continue;
                        if (hero.GridPosition == enemy.GridPosition)
                        {
                            Debug.Log($"[GatheringManager] RunGathering: enemy reached hero '{hero.CardData.cardName}' at {enemy.GridPosition} — initiating combat");
                            bool heroDefeated = enemy.EngageHero(hero, boardManager);

                            if (enemy.IsDefeated)
                            {
                                string defenderName = !string.IsNullOrEmpty(hero.CardData.localizationKey) ? Loc.Get(hero.CardData.localizationKey + ".name") : hero.CardData.cardName;
                                EventBus.OnGatheringNotification?.Invoke(Loc.Format("gather.enemy.defeated", defenderName), new Color(1f, 1f, 0.4f));
                                Debug.Log($"[GatheringManager] RunGathering: enemy defeated at {enemy.GridPosition} — tile will transition in resolve phase");
                            }
                            else if (heroDefeated)
                            {
                                string woundedName = !string.IsNullOrEmpty(hero.CardData.localizationKey) ? Loc.Get(hero.CardData.localizationKey + ".name") : hero.CardData.cardName;
                                EventBus.OnGatheringNotification?.Invoke(Loc.Format("gather.hero.woundedby", woundedName), new Color(1f, 0.2f, 0.2f));
                            }
                            break; // enemy fights one hero per turn
                        }
                    }
                }

                yield return new WaitForSeconds(0.2f * speedMult);
            }

            Debug.Log("[GatheringManager] RunGathering: all actors processed — invoking OnGatheringComplete");
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
