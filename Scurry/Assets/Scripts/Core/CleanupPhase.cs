using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scurry.Data;
using Scurry.Map;
using Scurry.Colony;
using Scurry.Logistics;


namespace Scurry.Core
{
    public static class CleanupPhase
    {
        /// <summary>
        /// Executes the cleanup phase:
        /// 1. Food consumption (1 per deployed hero)
        /// 2. Starvation for unfed heroes
        /// 3. Injury recovery tick
        /// 4. Hero deposit at colony
        /// 5. Enemy respawn timer tick
        /// </summary>
        public static CleanupResult Execute(
            List<HeroToken> deployedHeroes,
            List<EnemyToken> enemies,
            ResourceManager resources,
            ColonyGraph colony,
            MapGraph graph,
            bool emergencyRations)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: starting cleanup " +
                      $"(deployedHeroes={deployedHeroes?.Count ?? 0}, enemies={enemies?.Count ?? 0}, " +
                      $"emergencyRations={emergencyRations})");

            var result = new CleanupResult
            {
                foodConsumed = 0,
                foodDeficit = 0,
                foodHealing = 0,
                unfedHeroIds = new List<int>(),
                respawnedEnemyIds = new List<int>(),
                recoveredHeroIds = new List<int>(),
                healedHeroes = new List<(int tokenId, int healed)>()
            };

            if (deployedHeroes == null || resources == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning("[CleanupPhase] Execute: null references — returning empty result");
                return result;
            }

            // ── Step 1: Food consumption ─────────────────────────────────
            // Heroes at the colony node don't consume food — they eat directly from the colony
            int colonyNodeId = graph?.ColonyNodeId ?? -1;
            int heroesRequiringFood = deployedHeroes.Count(h => !h.isInjured && h.currentNodeId != colonyNodeId);
            int heroesAtColony = deployedHeroes.Count(h => !h.isInjured && h.currentNodeId == colonyNodeId);
            if (!SimulationFlags.SuppressLogging && heroesAtColony > 0) Debug.Log($"[CleanupPhase] Execute: {heroesAtColony} heroes at colony skip food consumption");
            int foodRequired = heroesRequiringFood;

            // Emergency rations reduce consumption
            if (emergencyRations)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: emergencyRations active — food spoilage immunity");
            }

            int foodAvailable = resources.FoodStockpile;
            int foodToConsume = Mathf.Min(foodRequired, foodAvailable);
            int deficit = foodRequired - foodToConsume;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: food consumption " +
                      $"(heroesRequiringFood={heroesRequiringFood}, foodRequired={foodRequired}, " +
                      $"foodAvailable={foodAvailable}, foodToConsume={foodToConsume}, deficit={deficit})");

            if (foodToConsume > 0)
            {
                bool consumed = resources.ConsumeFromStockpile(ResourceType.Food, foodToConsume);
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: consumed food (amount={foodToConsume}, success={consumed})");
            }

            result.foodConsumed = foodToConsume;
            result.foodDeficit = deficit;

            // ── Step 1b: Heal damaged heroes with surplus food (1 food per HP) ──
            if (deficit == 0)
            {
                int surplusFood = resources.FoodStockpile; // food remaining after feeding
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: checking healing — surplusFood={surplusFood}");

                if (surplusFood > 0)
                {
                    // Heal heroes with lowest HP first (most in need)
                    var damagedHeroes = deployedHeroes
                        .Where(h => !h.isInjured && h.currentHP < h.EffectiveHP)
                        .OrderBy(h => h.currentHP)
                        .ThenByDescending(h => h.EffectiveCombat)
                        .ToList();

                    int totalFoodSpentOnHealing = 0;

                    foreach (var hero in damagedHeroes)
                    {
                        if (surplusFood <= 0) break;

                        int hpMissing = hero.EffectiveHP - hero.currentHP;
                        int healAmount = Mathf.Min(hpMissing, surplusFood);

                        if (healAmount > 0)
                        {
                            hero.Heal(healAmount);
                            surplusFood -= healAmount;
                            totalFoodSpentOnHealing += healAmount;
                            result.healedHeroes.Add((hero.tokenId, healAmount));

                            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: healed hero with food " +
                                      $"(tokenId={hero.tokenId}, name={hero.cardDef?.cardName ?? "unknown"}, " +
                                      $"healed={healAmount}, hp={hero.currentHP}/{hero.EffectiveHP}, " +
                                      $"foodRemaining={surplusFood})");
                        }
                    }

                    if (totalFoodSpentOnHealing > 0)
                    {
                        resources.ConsumeFromStockpile(ResourceType.Food, totalFoodSpentOnHealing);
                        result.foodHealing = totalFoodSpentOnHealing;

                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: healing complete " +
                                  $"(totalFoodSpent={totalFoodSpentOnHealing}, heroesHealed={result.healedHeroes.Count}, " +
                                  $"foodRemaining={resources.FoodStockpile})");
                    }
                }
            }

            // ── Step 2: Determine unfed heroes ───────────────────────────
            if (deficit > 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: food deficit={deficit}, determining unfed heroes");

                // Unfed heroes: those with lowest combat value starve first (excluding colony heroes)
                var fedCandidates = deployedHeroes
                    .Where(h => !h.isInjured && h.currentNodeId != colonyNodeId)
                    .OrderByDescending(h => h.EffectiveCombat)
                    .ThenByDescending(h => h.EffectiveInitiative)
                    .ToList();

                // The first (heroesRequiringFood - deficit) heroes are fed, the rest are unfed
                int fedCount = heroesRequiringFood - deficit;
                for (int i = fedCount; i < fedCandidates.Count; i++)
                {
                    result.unfedHeroIds.Add(fedCandidates[i].tokenId);
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: hero unfed — will starve " +
                              $"(tokenId={fedCandidates[i].tokenId}, " +
                              $"name={fedCandidates[i].cardDef?.cardName ?? "unknown"}, " +
                              $"combat={fedCandidates[i].EffectiveCombat})");
                }
            }

            // ── Step 3: Injury recovery ──────────────────────────────────
            if (!SimulationFlags.SuppressLogging) Debug.Log("[CleanupPhase] Execute: processing injury recovery");

            // Check colony healing effects
            bool hasCampfire = colony != null && colony.HasEffect(ColonyEffect.HealInjured);
            int healingBonus = hasCampfire ? colony.GetEffectValue(ColonyEffect.HealInjured) : 0;
            bool hasShrineOfHeroes = colony != null && colony.HasEffect(ColonyEffect.ImmediateHealReturn);

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: healing effects " +
                      $"(hasCampfire={hasCampfire}, healingBonus={healingBonus}, " +
                      $"hasShrineOfHeroes={hasShrineOfHeroes})");

            // We need to check ALL heroes (not just deployed) for injury recovery
            // Injured heroes are not in deployedHeroes list, so we check all heroes via enemies list trick
            // Actually, the caller (TurnManager) passes deployedHeroes. Injured heroes were removed from deployed.
            // We need a way to find injured heroes. The TurnManager's allHeroes list has them.
            // Since we only have deployedHeroes here, we check each for injury timers.
            // NOTE: Injured heroes are tracked by TurnManager.allHeroes — the cleanup result returns
            // recoveredHeroIds so TurnManager can handle the actual recovery.

            // For now, we can't directly access injured heroes from here.
            // We'll process enemy respawns and return the result.
            // TurnManager will handle recovery by iterating allHeroes.

            // ── Step 4: Hero deposit at colony ───────────────────────────
            if (graph != null)
            {
                foreach (var hero in deployedHeroes)
                {
                    if (hero.isInjured) continue;

                    if (hero.currentNodeId == colonyNodeId && hero.TotalCarried > 0)
                    {
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: hero at colony depositing resources " +
                                  $"(tokenId={hero.tokenId}, name={hero.cardDef?.cardName ?? "unknown"}, " +
                                  $"totalCarried={hero.TotalCarried})");

                        resources.DepositHeroResources(hero);
                    }
                }
            }

            // ── Step 5: Enemy respawn timer tick ─────────────────────────
            if (enemies != null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: processing enemy respawn timers " +
                          $"(totalEnemies={enemies.Count})");

                foreach (var enemy in enemies)
                {
                    if (!enemy.isDefeated) continue;

                    enemy.respawnTimer--;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: enemy respawn timer tick " +
                              $"(tokenId={enemy.tokenId}, name={enemy.enemyName}, " +
                              $"respawnTimer={enemy.respawnTimer + 1}->{enemy.respawnTimer})");

                    if (enemy.respawnTimer <= 0)
                    {
                        result.respawnedEnemyIds.Add(enemy.tokenId);
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: enemy ready to respawn " +
                                  $"(tokenId={enemy.tokenId}, name={enemy.enemyName})");
                    }
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] Execute: cleanup complete " +
                      $"(foodConsumed={result.foodConsumed}, foodDeficit={result.foodDeficit}, " +
                      $"unfedHeroes={result.unfedHeroIds.Count}, " +
                      $"respawnedEnemies={result.respawnedEnemyIds.Count}, " +
                      $"recoveredHeroes={result.recoveredHeroIds.Count})");

            return result;
        }

        /// <summary>
        /// Processes injury recovery for all heroes. Called by TurnManager with its allHeroes list.
        /// Returns list of hero token IDs that have fully recovered.
        /// </summary>
        public static List<int> ProcessInjuryRecovery(List<HeroToken> allHeroes, ColonyGraph colony)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] ProcessInjuryRecovery: checking {allHeroes?.Count ?? 0} heroes");

            var recovered = new List<int>();

            if (allHeroes == null) return recovered;

            bool hasCampfire = colony != null && colony.HasEffect(ColonyEffect.HealInjured);
            int healingBonus = hasCampfire ? colony.GetEffectValue(ColonyEffect.HealInjured) : 0;
            bool hasShrineOfHeroes = colony != null && colony.HasEffect(ColonyEffect.ImmediateHealReturn);

            foreach (var hero in allHeroes)
            {
                if (!hero.isInjured) continue;

                if (hasShrineOfHeroes)
                {
                    // Shrine of Heroes: injured heroes return immediately
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] ProcessInjuryRecovery: ShrineOfHeroes — immediate recovery " +
                              $"(tokenId={hero.tokenId}, name={hero.cardDef?.cardName ?? "unknown"})");
                    recovered.Add(hero.tokenId);
                    continue;
                }

                // Normal recovery: decrement timer, with bonus healing
                int recoveryTick = 1 + healingBonus;
                hero.turnsUntilRecovery -= recoveryTick;

                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] ProcessInjuryRecovery: recovery tick " +
                          $"(tokenId={hero.tokenId}, name={hero.cardDef?.cardName ?? "unknown"}, " +
                          $"recoveryTick={recoveryTick}, turnsRemaining={hero.turnsUntilRecovery})");

                if (hero.turnsUntilRecovery <= 0)
                {
                    recovered.Add(hero.tokenId);
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] ProcessInjuryRecovery: hero fully recovered " +
                              $"(tokenId={hero.tokenId}, name={hero.cardDef?.cardName ?? "unknown"})");
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CleanupPhase] ProcessInjuryRecovery: complete (recoveredCount={recovered.Count})");
            return recovered;
        }
    }

    public struct CleanupResult
    {
        public int foodConsumed;
        public int foodDeficit;
        public int foodHealing;
        public List<int> unfedHeroIds;
        public List<int> respawnedEnemyIds;
        public List<int> recoveredHeroIds;
        public List<(int tokenId, int healed)> healedHeroes;
    }
}
