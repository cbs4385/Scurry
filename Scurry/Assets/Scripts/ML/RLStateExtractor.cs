using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scurry.Data;
using Scurry.Map;
using Scurry.Colony;

namespace Scurry.ML
{
    /// <summary>
    /// Extracts a normalized feature vector from the current game state.
    /// Used by the RL agent to observe the environment each turn.
    /// </summary>
    public static class RLStateExtractor
    {
        public const int FEATURE_COUNT = 38;

        /// <summary>
        /// Extracts a normalized feature vector from the current game state.
        /// All features are scaled to approximately [0, 1] or [-1, 1].
        /// </summary>
        public static float[] Extract(
            int turn, int maxTurns,
            int food, int materials, int currency,
            int foodProduction,
            List<HeroToken> deployedHeroes,
            List<HeroToken> allHeroes,
            List<CardDefinitionSO> availableHeroes,
            List<EnemyToken> enemyTokens,
            MapGraph mapGraph,
            ColonyGraph colony,
            HashSet<NodeType> zonesReached,
            HashSet<int> nodesExplored,
            int zoneBossesDefeated,
            int tacticalCardsAvailable = 0,
            bool piperCountdownActive = false,
            int piperCountdownRemaining = -1)
        {
            var features = new float[FEATURE_COUNT];
            int totalHeroCap = 13; // rough max heroes

            // ── Turn and timing (0-1) ──────────────────────────────────
            features[0] = Mathf.Clamp01((float)turn / 50f);          // turn progress (normalized to ~50 turns)
            features[1] = piperCountdownActive
                ? Mathf.Clamp01((float)piperCountdownRemaining / 15f) // countdown urgency
                : 1f;                                                  // no urgency yet

            // ── Resources (2-5) ────────────────────────────────────────
            features[2] = Mathf.Clamp01(food / 50f);
            features[3] = Mathf.Clamp01(materials / 20f);
            features[4] = Mathf.Clamp01(currency / 20f);
            features[5] = Mathf.Clamp01(foodProduction / 15f);

            // ── Hero counts (6-8) ──────────────────────────────────────
            int deployed = deployedHeroes.Count(h => !h.isInjured);
            int available = availableHeroes?.Count ?? 0;
            int injured = allHeroes.Count(h => h.isInjured);

            features[6] = Mathf.Clamp01((float)deployed / totalHeroCap);
            features[7] = Mathf.Clamp01((float)available / totalHeroCap);
            features[8] = Mathf.Clamp01((float)injured / totalHeroCap);

            // ── Hero condition (9-10) ──────────────────────────────────
            float avgHpFrac = 0f;
            float totalCombat = 0f;
            if (deployed > 0)
            {
                avgHpFrac = deployedHeroes.Where(h => !h.isInjured)
                    .Average(h => (float)h.currentHP / Mathf.Max(1, h.EffectiveHP));
                totalCombat = deployedHeroes.Where(h => !h.isInjured)
                    .Sum(h => h.EffectiveCombat);
            }
            features[9] = avgHpFrac;
            features[10] = Mathf.Clamp01(totalCombat / 50f);

            // ── Map progress (11-14) ───────────────────────────────────
            features[11] = Mathf.Clamp01(zonesReached.Count / 5f);
            features[12] = Mathf.Clamp01(nodesExplored.Count / 51f);
            features[13] = Mathf.Clamp01(zoneBossesDefeated / 3f);
            features[14] = mapGraph.IsPiperAccessible ? 1f : 0f;

            // ── Enemy state (15-18) ────────────────────────────────────
            var aliveEnemies = enemyTokens.Where(e => !e.isDefeated).ToList();
            features[15] = Mathf.Clamp01(aliveEnemies.Count / 20f);
            features[16] = Mathf.Clamp01(aliveEnemies.Count(e => e.homeZone == NodeType.Wilderness) / 10f);
            features[17] = Mathf.Clamp01(aliveEnemies.Count(e => e.homeZone == NodeType.Farmland) / 10f);
            features[18] = Mathf.Clamp01(aliveEnemies.Count(e => e.homeZone == NodeType.Town) / 10f);

            // ── Hero distribution (19-22) ──────────────────────────────
            var activeHeroes = deployedHeroes.Where(h => !h.isInjured).ToList();
            int atColony = 0, inWild = 0, inFarm = 0, inTown = 0;
            foreach (var hero in activeHeroes)
            {
                var node = mapGraph.GetNode(hero.currentNodeId);
                if (node == null) continue;
                switch (node.zone)
                {
                    case NodeType.Colony: atColony++; break;
                    case NodeType.Wilderness: inWild++; break;
                    case NodeType.Farmland: inFarm++; break;
                    case NodeType.Town: inTown++; break;
                }
            }
            features[19] = Mathf.Clamp01((float)atColony / totalHeroCap);
            features[20] = Mathf.Clamp01((float)inWild / totalHeroCap);
            features[21] = Mathf.Clamp01((float)inFarm / totalHeroCap);
            features[22] = Mathf.Clamp01((float)inTown / totalHeroCap);

            // ── Resources on map (23-25) ───────────────────────────────
            float foodOnMap = 0, matOnMap = 0, curOnMap = 0;
            foreach (var node in mapGraph.GetAllNodes())
            {
                if (node.resources.TryGetValue(ResourceType.Food, out int f)) foodOnMap += f;
                if (node.resources.TryGetValue(ResourceType.Materials, out int m)) matOnMap += m;
                if (node.resources.TryGetValue(ResourceType.Currency, out int c)) curOnMap += c;
            }
            features[23] = Mathf.Clamp01(foodOnMap / 30f);
            features[24] = Mathf.Clamp01(matOnMap / 20f);
            features[25] = Mathf.Clamp01(curOnMap / 20f);

            // ── Combat readiness (26-29) ───────────────────────────────
            int carrying = activeHeroes.Count(h => h.TotalCarried > 0);
            float avgCarry = activeHeroes.Count > 0
                ? activeHeroes.Average(h => (float)h.TotalCarried / Mathf.Max(1, h.EffectiveCarry))
                : 0f;
            features[26] = Mathf.Clamp01((float)carrying / totalHeroCap);
            features[27] = avgCarry;

            // Distance to nearest boss (from any deployed hero)
            float nearestBoss = 15f;
            float nearestPiper = 15f;
            foreach (var hero in activeHeroes)
            {
                foreach (var enemy in aliveEnemies)
                {
                    if (enemy.strength < 8) continue;
                    int dist = mapGraph.GetDistance(hero.currentNodeId, enemy.currentNodeId);
                    if (dist >= 0)
                    {
                        if (enemy.homeZone == NodeType.PiedPiper)
                            nearestPiper = Mathf.Min(nearestPiper, dist);
                        else
                            nearestBoss = Mathf.Min(nearestBoss, dist);
                    }
                }
            }
            features[28] = Mathf.Clamp01(nearestBoss / 10f);
            features[29] = Mathf.Clamp01(nearestPiper / 15f);

            // ── Colony state (30-34) ───────────────────────────────────
            var effects = colony.GetActiveEffects();
            features[30] = Mathf.Clamp01(colony.GetPlacedCards().Count / 10f);
            features[31] = effects.ContainsKey(ColonyEffect.FoodProduction) ? 1f : 0f;
            features[32] = (effects.ContainsKey(ColonyEffect.AllHeroCombatBuff) ||
                            effects.ContainsKey(ColonyEffect.FirstCombatBuff)) ? 1f : 0f;
            features[33] = (effects.ContainsKey(ColonyEffect.FogReveal) ||
                            effects.ContainsKey(ColonyEffect.FogRevealHeroes)) ? 1f : 0f;
            features[34] = (effects.ContainsKey(ColonyEffect.HealInjured) ||
                            effects.ContainsKey(ColonyEffect.ImmediateHealReturn)) ? 1f : 0f;

            // ── Tactical cards (35) ─────────────────────────────────
            features[35] = Mathf.Clamp01(tacticalCardsAvailable / 10f);

            // ── Pied Piper countdown (36-37) ─────────────────────────
            features[36] = piperCountdownActive ? 1f : 0f;
            features[37] = piperCountdownActive
                ? Mathf.Clamp01((float)piperCountdownRemaining / 15f)
                : 0f;

            return features;
        }
    }
}
