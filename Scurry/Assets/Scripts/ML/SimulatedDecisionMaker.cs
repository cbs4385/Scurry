using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scurry.Data;
using Scurry.Map;
using Scurry.Colony;
using Scurry.Combat;

namespace Scurry.ML
{
    /// <summary>
    /// Makes all in-game decisions using DecisionWeights. Replaces the hardcoded
    /// strategy switch statements in AutoPlayController with parameterized heuristics.
    /// </summary>
    public class SimulatedDecisionMaker
    {
        private readonly DecisionWeights weights;

        public SimulatedDecisionMaker(DecisionWeights w)
        {
            weights = w;
        }

        // ── Colony Phase ────────────────────────────────────────────────

        /// <summary>
        /// Chooses the best colony card to play based on weights and available resources.
        /// Returns null if no card should be played or no card is affordable.
        /// </summary>
        public ColonyCardDefinitionSO ChooseColonyCard(
            List<ColonyCardDefinitionSO> available, ColonyGraph colony, int deployedCount, int turn,
            int foodStockpile = int.MaxValue, int materialsStockpile = int.MaxValue,
            GameSimulator simulator = null)
        {
            if (available.Count == 0) return null;

            int foodProd = colony.CalculateFoodProduction();
            bool needFood = foodProd < weights.FoodThreshold;

            // Timing multipliers for colony priorities
            float foodMult = turn <= 3 ? Mathf.Max(1f, weights.genes[DecisionWeights.COLONY_EARLY_FOOD_MULT]) : 1f;
            float combatMult = turn >= 4 && turn <= 8 ? Mathf.Max(1f, weights.genes[DecisionWeights.COLONY_MID_COMBAT_MULT]) : 1f;

            ColonyCardDefinitionSO best = null;
            float bestScore = -1f;

            foreach (var card in available)
            {
                // Check if we can afford this card
                var (foodCost, matCost) = GetColonyCardCostStatic(card.colonyTier);
                if (foodStockpile < foodCost || materialsStockpile < matCost)
                    continue;

                float score = ScoreColonyCardForPlay(card, needFood);

                // Apply timing multipliers
                if (card.colonyEffect == ColonyEffect.FoodProduction ||
                    card.colonyEffect == ColonyEffect.MushFoodProduction ||
                    card.colonyEffect == ColonyEffect.DoubleProduction ||
                    card.colonyEffect == ColonyEffect.FoodStorageCapacity)
                    score *= foodMult;

                if (card.colonyEffect == ColonyEffect.AllHeroCombatBuff ||
                    card.colonyEffect == ColonyEffect.FirstCombatBuff ||
                    card.colonyEffect == ColonyEffect.EquippedCombatBuff)
                    score *= combatMult;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = card;
                }
            }

            return best;
        }

        /// <summary>
        /// Returns (foodCost, materialsCost) for a colony card tier using default balance values.
        /// </summary>
        public static (int food, int materials) GetColonyCardCostStatic(ColonyTier tier)
        {
            return tier switch
            {
                ColonyTier.FoodStorage => (1, 1),
                ColonyTier.StructureDefense => (1, 2),
                ColonyTier.Advanced => (1, 2),
                _ => (0, 0)
            };
        }

        private float ScoreColonyCardForPlay(ColonyCardDefinitionSO card, bool needFood)
        {
            float baseScore = card.colonyEffect switch
            {
                ColonyEffect.FoodProduction => weights.genes[DecisionWeights.COL_FOOD_PRIORITY] * card.effectValue * (needFood ? 3f : 1f),
                ColonyEffect.MushFoodProduction => weights.genes[DecisionWeights.COL_FOOD_PRIORITY] * card.effectValue * (needFood ? 2.5f : 0.8f),
                ColonyEffect.DoubleProduction => weights.genes[DecisionWeights.COL_FOOD_PRIORITY] * 5f * (needFood ? 2f : 1f),
                ColonyEffect.FoodStorageCapacity => weights.genes[DecisionWeights.COL_STORAGE_PRIORITY] * card.effectValue,
                ColonyEffect.ColonyDefenseWall => weights.genes[DecisionWeights.COL_DEFENSE_PRIORITY] * card.effectValue,
                ColonyEffect.ColonyDefenseBonus => weights.genes[DecisionWeights.COL_DEFENSE_PRIORITY] * card.effectValue,
                ColonyEffect.FogReveal => weights.genes[DecisionWeights.COL_FOG_PRIORITY] * card.effectValue,
                ColonyEffect.FogRevealHeroes => weights.genes[DecisionWeights.COL_FOG_PRIORITY] * card.effectValue,
                ColonyEffect.AllHeroCombatBuff => weights.genes[DecisionWeights.COL_COMBAT_PRIORITY] * card.effectValue * 3f,
                ColonyEffect.FirstCombatBuff => weights.genes[DecisionWeights.COL_COMBAT_PRIORITY] * card.effectValue,
                ColonyEffect.EquippedCombatBuff => weights.genes[DecisionWeights.COL_COMBAT_PRIORITY] * card.effectValue,
                ColonyEffect.HealInjured => weights.genes[DecisionWeights.COL_HEAL_PRIORITY] * card.effectValue,
                ColonyEffect.ImmediateHealReturn => weights.genes[DecisionWeights.COL_HEAL_PRIORITY] * 4f,
                ColonyEffect.AllHeroMoveBuff => weights.genes[DecisionWeights.COL_MOVE_PRIORITY] * card.effectValue,
                ColonyEffect.DoubleColonyCard => weights.genes[DecisionWeights.COL_DOUBLE_CARD_PRIORITY] * 3f,
                ColonyEffect.ForwardDeploy => weights.genes[DecisionWeights.COL_FORWARD_DEPLOY_PRIORITY] * 2f,
                _ => 0.1f
            };

            return baseScore;
        }

        // ── Deploy Phase ────────────────────────────────────────────────

        public int GetMaxDeploy(DecisionWeights w, int foodProduction, int alreadyDeployed, int turn)
        {
            // Hard cap: 2 hero deploys per turn (balance rule)
            return 2;
        }

        public List<CardDefinitionSO> SortHeroesForDeploy(List<CardDefinitionSO> available, DecisionWeights w)
        {
            return available.OrderByDescending(h =>
                h.combat * w.genes[DecisionWeights.HERO_COMBAT_W] +
                h.move * w.genes[DecisionWeights.HERO_MOVE_W] +
                h.hp * w.genes[DecisionWeights.HERO_HP_W] +
                h.carry * w.genes[DecisionWeights.HERO_CARRY_W] +
                h.initiative * w.genes[DecisionWeights.HERO_INIT_W]
            ).ToList();
        }

        public List<int> GetDeployTargets(MapGraph map, List<HeroToken> deployed,
            List<EnemyToken> enemies, int turn, DecisionWeights w)
        {
            var allNodes = map.GetAllNodes();
            int colonyNodeId = map.ColonyNodeId;

            var scored = new List<(int nodeId, float score)>();

            foreach (var node in allNodes)
            {
                if (node.nodeId == colonyNodeId) continue;

                float score = 0f;

                // Enemy presence
                int enemyCount = node.enemyTokenIds.Count(id => enemies.Any(e => e.tokenId == id && !e.isDefeated));
                score += enemyCount * w.genes[DecisionWeights.TARGET_ENEMY_W];

                // Distance from colony
                int dist = map.GetDistance(colonyNodeId, node.nodeId);
                score -= dist * w.genes[DecisionWeights.TARGET_DISTANCE_W];

                // Unvisited
                if (!node.visited)
                    score += w.genes[DecisionWeights.TARGET_UNVISITED_W] * 2f;

                // Resources
                score += node.TotalResources() * w.genes[DecisionWeights.TARGET_RESOURCE_W];

                // Piper proximity (late game)
                if (node.zone == NodeType.PiedPiper && turn >= w.PiperRushTurn)
                    score += w.genes[DecisionWeights.TARGET_PIPER_W] * 10f;

                scored.Add((node.nodeId, score));
            }

            return scored.OrderByDescending(s => s.score).Select(s => s.nodeId).ToList();
        }

        public void EquipHero(CardDefinitionSO heroDef, HeroToken token,
            List<CardDefinitionSO> availableEquip, DecisionWeights w)
        {
            bool isGatherer = heroDef.heroRole == HeroRole.Gather || heroDef.carry >= 3 ||
                              heroDef.specialAbility == SpecialAbility.EfficientGather ||
                              heroDef.specialAbility == SpecialAbility.BulkHaul;

            CardDefinitionSO bestOffensive = null, bestDefensive = null, bestUtility = null;

            // Score each equipment piece by role and slot
            foreach (var eq in availableEquip)
            {
                float eqScore;
                if (isGatherer)
                {
                    eqScore = eq.equipmentSlot switch
                    {
                        EquipmentSlot.Utility => eq.effectValue1 * w.genes[DecisionWeights.GATHERER_UTILITY_W],
                        EquipmentSlot.Offensive => eq.effectValue1 * w.genes[DecisionWeights.EQUIP_GATHERER_OFF_W],
                        EquipmentSlot.Defensive => eq.effectValue1 * w.genes[DecisionWeights.EQUIP_DEFENSIVE_W],
                        _ => 0f
                    };
                }
                else
                {
                    eqScore = eq.equipmentSlot switch
                    {
                        EquipmentSlot.Offensive => eq.effectValue1 * w.genes[DecisionWeights.FIGHTER_OFFENSIVE_W],
                        EquipmentSlot.Defensive => eq.effectValue1 * w.genes[DecisionWeights.EQUIP_FIGHTER_DEF_W],
                        EquipmentSlot.Utility => eq.effectValue1 * w.genes[DecisionWeights.EQUIP_UTILITY_W],
                        _ => 0f
                    };
                }

                if (eq.equipmentSlot == EquipmentSlot.Offensive && (bestOffensive == null || eqScore > bestOffensive.effectValue1))
                    bestOffensive = eq;
                else if (eq.equipmentSlot == EquipmentSlot.Defensive && (bestDefensive == null || eqScore > bestDefensive.effectValue1))
                    bestDefensive = eq;
                else if (eq.equipmentSlot == EquipmentSlot.Utility && (bestUtility == null || eqScore > bestUtility.effectValue1))
                    bestUtility = eq;
            }

            if (bestOffensive != null) { token.offensiveEquipment = bestOffensive; availableEquip.Remove(bestOffensive); }
            if (bestDefensive != null) { token.defensiveEquipment = bestDefensive; availableEquip.Remove(bestDefensive); }
            if (bestUtility != null) { token.utilityEquipment = bestUtility; availableEquip.Remove(bestUtility); }
        }

        // ── Tactical Card Selection ───────────────────────────────────────

        /// <summary>
        /// Chooses the best tactical card to play during combat.
        /// Returns null if no card should be played.
        /// </summary>
        public CardDefinitionSO ChooseTacticalCard(
            List<CardDefinitionSO> available,
            List<HeroToken> heroes,
            List<EnemyToken> enemies,
            bool isBossFight,
            int turn)
        {
            if (available.Count == 0) return null;

            // Save cards for boss fights if gene says so
            if (!isBossFight && weights.genes[DecisionWeights.TACTICAL_SAVE_FOR_BOSS] > 0.6f
                && available.Count <= 2)
                return null;

            float threshold = weights.genes[DecisionWeights.TACTICAL_PLAY_THRESHOLD];
            CardDefinitionSO best = null;
            float bestScore = threshold;

            // Situational modifiers
            float avgHpFrac = heroes.Where(h => h.IsAlive).DefaultIfEmpty()
                .Average(h => h != null ? (float)h.currentHP / Mathf.Max(1, h.EffectiveHP) : 1f);
            int aliveHeroes = heroes.Count(h => h.IsAlive);
            int aliveEnemies = enemies.Count(e => e.IsAlive);
            int totalEnemyStr = enemies.Where(e => e.IsAlive).Sum(e => e.strength);
            int totalHeroStr = heroes.Where(h => h.IsAlive).Sum(h => h.EffectiveCombat);
            bool outmatched = totalEnemyStr > totalHeroStr;

            foreach (var card in available)
            {
                if (card.cardType != CardType.Tactical) continue;

                float score = 0f;

                // Category-based scoring
                if (card.cardId >= 91 && card.cardId <= 100)
                {
                    // Combat tactics
                    score = weights.genes[DecisionWeights.TACTICAL_COMBAT_PREF];

                    // Boost combat cards when outmatched
                    if (outmatched) score *= 1.5f;

                    // Retreat cards: only when badly outmatched
                    if (card.cardId == 96 || card.cardId == 98) // Tactical Retreat, Hidden Tunnel
                        score = outmatched && avgHpFrac < 0.4f ? score * 2f : score * 0.1f;
                }
                else if (card.cardId >= 101 && card.cardId <= 110)
                {
                    // Support tactics
                    score = weights.genes[DecisionWeights.TACTICAL_SUPPORT_PREF];

                    // Healing cards boosted when heroes are damaged
                    if ((card.cardId == 106 || card.cardId == 108 || card.cardId == 109 || card.cardId == 110)
                        && avgHpFrac < 0.6f)
                        score *= 2f;
                }
                else if (card.cardId >= 111 && card.cardId <= 120)
                {
                    // Power tactics
                    score = weights.genes[DecisionWeights.TACTICAL_POWER_PREF];

                    // Boss fight bonus for power cards
                    if (isBossFight) score *= 2f;
                }

                // Scale by effect value for cards that have one
                if (card.effectValue1 > 0)
                    score *= Mathf.Max(1f, card.effectValue1 * 0.5f);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = card;
                }
            }

            return best;
        }

        // ── Hero Movement ───────────────────────────────────────────────

        public int ChooseTarget(HeroToken hero, MapGraph map, ColonyGraph colony,
            List<EnemyToken> enemies, int turn, DecisionWeights w,
            List<HeroToken> deployed = null)
        {
            int colonyNodeId = map.ColonyNodeId;
            deployed ??= new List<HeroToken>();

            // Return to colony when carrying enough resources
            if (hero.TotalCarried > 0)
            {
                float threshold = hero.EffectiveCarry * w.ReturnCarryThreshold;
                if (hero.TotalCarried >= threshold || hero.currentNodeId == colonyNodeId)
                    return colonyNodeId;
            }

            // Return to colony when low HP (threshold controlled by gene)
            float hpThreshold = w.HpRetreatThreshold;
            if (hero.currentHP < hero.EffectiveHP * hpThreshold && w.genes[DecisionWeights.MOVE_COLONY_HEAL_W] > 0.5f)
                return colonyNodeId;

            bool isGatherer = hero.cardDef != null && (hero.cardDef.heroRole == HeroRole.Gather ||
                              hero.cardDef.carry >= 3);

            // Late game: rush Pied Piper if enough heroes deployed
            if (turn >= w.PiperRushTurn && !isGatherer)
            {
                var piperNode = map.GetAllNodes().FirstOrDefault(n => n.zone == NodeType.PiedPiper);
                if (piperNode != null)
                    return piperNode.nodeId;
            }

            var allNodes = map.GetAllNodes();
            float bestScore = float.MinValue;
            int bestTarget = hero.currentNodeId;

            // Timing phase boosts
            float exploreBoost = turn <= 4 ? w.genes[DecisionWeights.EARLY_EXPLORE_BOOST] : 0f;
            float fightBoost = turn >= 5 && turn <= 9 ? w.genes[DecisionWeights.MID_FIGHT_BOOST] : 0f;
            float piperBoost = turn >= 10 ? w.genes[DecisionWeights.LATE_PIPER_BOOST] : 0f;

            // Count allied heroes near this hero for grouping decisions
            int alliedNearby = deployed.Count(h => h.tokenId != hero.tokenId && !h.isInjured &&
                map.GetDistance(hero.currentNodeId, h.currentNodeId) <= 2);

            foreach (var node in allNodes)
            {
                if (node.nodeId == hero.currentNodeId) continue;
                if (node.nodeId == colonyNodeId) continue;

                float score = 0f;
                int dist = map.GetDistance(hero.currentNodeId, node.nodeId);
                if (dist < 0) dist = 20;

                // Resource value
                int resources = node.TotalResources();
                int foodOnNode = node.resources.GetValueOrDefault(ResourceType.Food, 0);
                score += resources * w.genes[DecisionWeights.MOVE_GATHER_W];
                score += foodOnNode * w.genes[DecisionWeights.MOVE_FOOD_NODE_W];

                // Resource deposit urgency — boost colony return when carrying a lot
                if (hero.TotalCarried > 0)
                {
                    float carryRatio = (float)hero.TotalCarried / Mathf.Max(1, hero.EffectiveCarry);
                    score += carryRatio * w.genes[DecisionWeights.RESOURCE_DEPOSIT_URGENCY];
                }

                // Enemy presence with zone-specific weights
                int enemyCount = node.enemyTokenIds.Count(id => enemies.Any(e => e.tokenId == id && !e.isDefeated));
                int totalEnemyStrength = node.enemyTokenIds
                    .Select(id => enemies.FirstOrDefault(e => e.tokenId == id && !e.isDefeated))
                    .Where(e => e != null).Sum(e => e.strength);

                if (isGatherer)
                {
                    score -= enemyCount * w.genes[DecisionWeights.MOVE_AVOID_ENEMY_W]; // Gatherers avoid
                }
                else
                {
                    float zoneFightBonus = node.zone switch
                    {
                        NodeType.Wilderness => w.genes[DecisionWeights.ZONE_WILD_FIGHT_W],
                        NodeType.Farmland => w.genes[DecisionWeights.ZONE_FARM_FIGHT_W],
                        NodeType.Town => w.genes[DecisionWeights.ZONE_TOWN_FIGHT_W],
                        _ => 0f
                    };

                    score += enemyCount * (w.genes[DecisionWeights.MOVE_FIGHT_W] + fightBoost + zoneFightBonus);

                    // Strength ratio check — avoid fights where we're outmatched
                    if (totalEnemyStrength > 0 && enemyCount > 0)
                    {
                        float heroStrength = hero.EffectiveCombat + alliedNearby * 3f; // estimate group combat
                        float ratio = heroStrength / totalEnemyStrength;
                        if (ratio < w.FightStrengthRatio)
                            score -= (w.FightStrengthRatio - ratio) * 3f; // Penalize fights we'd likely lose
                    }
                }

                // Unvisited (boosted early game)
                if (!node.visited && node.fogState != FogState.Visible)
                    score += w.genes[DecisionWeights.MOVE_EXPLORE_W] + exploreBoost;

                // Boss nodes — only approach after BossApproachTurn and with enough allies
                bool isBoss = node.enemyTokenIds.Any(id => enemies.Any(e => e.tokenId == id && !e.isDefeated && e.strength >= 8));
                if (isBoss && !isGatherer)
                {
                    if (turn >= w.BossApproachTurn && alliedNearby + 1 >= w.BossMinGroup)
                        score += w.genes[DecisionWeights.MOVE_BOSS_W] * 2f;
                    else if (turn >= w.BossApproachTurn)
                        score += w.genes[DecisionWeights.MOVE_BOSS_W] * 0.5f; // Weak pull to start grouping up
                    else
                        score -= 2f; // Avoid bosses before ready
                }

                // Regroup — pull toward nodes where other heroes are heading
                int heroesAtNode = node.heroTokenIds.Count(id =>
                    deployed.Any(h => h.tokenId == id && !h.isInjured));
                if (heroesAtNode > 0 && !isGatherer)
                    score += heroesAtNode * w.genes[DecisionWeights.HERO_REGROUP_W];

                // Piper node late game (with extra late boost)
                if (node.zone == NodeType.PiedPiper && turn >= w.PiperRushTurn - 2)
                    score += (w.genes[DecisionWeights.MOVE_PIPER_LATE_W] + piperBoost) * (turn - w.PiperRushTurn + 3);

                // Distance penalty (gene-controlled)
                score -= dist * w.MoveDistancePenalty;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = node.nodeId;
                }
            }

            return bestTarget;
        }
    }
}
