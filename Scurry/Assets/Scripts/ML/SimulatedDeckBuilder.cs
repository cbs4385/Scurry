using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scurry.Data;

namespace Scurry.ML
{
    /// <summary>
    /// Builds a deck from DecisionWeights, selecting cards from the full pool
    /// based on evolved preferences. Returns separate lists for heroes, colony,
    /// equipment, and tactical cards.
    /// </summary>
    public class SimulatedDeckBuilder
    {
        private readonly IReadOnlyList<CardDefinitionSO> allCards;
        private readonly IReadOnlyList<ColonyCardDefinitionSO> allColonyCards;

        public SimulatedDeckBuilder(
            IReadOnlyList<CardDefinitionSO> cards,
            IReadOnlyList<ColonyCardDefinitionSO> colonyCards)
        {
            allCards = cards;
            allColonyCards = colonyCards;
        }

        /// <summary>
        /// Builds a deck using the given weights. Returns (heroes, colony, equipment, tactical).
        /// Respects copy limits: 1-cost=3 copies, 2-cost=2 copies, 3+=1 copy.
        /// </summary>
        public (List<CardDefinitionSO> heroes, List<ColonyCardDefinitionSO> colony,
                List<CardDefinitionSO> equipment, List<CardDefinitionSO> tactical)
            BuildDeck(DecisionWeights w)
        {
            int targetDeckSize = w.PreferredDeckSize;
            int targetHeroes = Mathf.RoundToInt(w.HeroCount);
            int targetColony = Mathf.RoundToInt(w.ColonyCount);

            var heroes = new List<CardDefinitionSO>();
            var colony = new List<ColonyCardDefinitionSO>();
            var equipment = new List<CardDefinitionSO>();
            var tactical = new List<CardDefinitionSO>();

            var cardCopyCounts = new Dictionary<int, int>(); // cardId -> copies added

            // ── Heroes ──────────────────────────────────────────────────
            var heroPool = allCards.Where(c => c.cardType == CardType.Hero).ToList();
            var rankedHeroes = heroPool.OrderByDescending(h =>
                h.combat * w.genes[DecisionWeights.HERO_COMBAT_W] +
                h.move * w.genes[DecisionWeights.HERO_MOVE_W] +
                h.hp * w.genes[DecisionWeights.HERO_HP_W] +
                h.carry * w.genes[DecisionWeights.HERO_CARRY_W] +
                h.initiative * w.genes[DecisionWeights.HERO_INIT_W]
            ).ToList();

            foreach (var hero in rankedHeroes)
            {
                if (heroes.Count >= targetHeroes) break;
                int maxCopies = GetMaxCopies(hero.deckCost);
                int current = cardCopyCounts.GetValueOrDefault(hero.cardId, 0);
                while (current < maxCopies && heroes.Count < targetHeroes)
                {
                    heroes.Add(hero);
                    current++;
                    cardCopyCounts[hero.cardId] = current;
                }
            }

            // Ensure at least 4 heroes
            while (heroes.Count < 4 && heroPool.Count > 0)
            {
                var next = heroPool.FirstOrDefault(h => cardCopyCounts.GetValueOrDefault(h.cardId, 0) < GetMaxCopies(h.deckCost));
                if (next == null) break;
                heroes.Add(next);
                cardCopyCounts[next.cardId] = cardCopyCounts.GetValueOrDefault(next.cardId, 0) + 1;
            }

            // ── Colony Cards ────────────────────────────────────────────
            // Always include starters
            var starters = allColonyCards.Where(c => c.isStarter).ToList();
            foreach (var s in starters)
                colony.Add(s);

            var colonyPool = allColonyCards.Where(c => !c.isStarter).ToList();
            var rankedColony = colonyPool.OrderByDescending(c => ScoreColonyCard(c, w)).ToList();

            foreach (var card in rankedColony)
            {
                if (colony.Count >= targetColony + starters.Count) break;
                int maxCopies = GetMaxCopies(card.deckCost);
                int current = cardCopyCounts.GetValueOrDefault(card.cardId, 0);
                if (current < maxCopies)
                {
                    colony.Add(card);
                    cardCopyCounts[card.cardId] = current + 1;
                }
            }

            // ── Equipment ───────────────────────────────────────────────
            int remaining = targetDeckSize - heroes.Count - colony.Count;
            int equipTarget = Mathf.Max(0, Mathf.RoundToInt(remaining * (1f - w.genes[DecisionWeights.TACTICAL_W])));

            var equipPool = allCards.Where(c => c.cardType == CardType.Equipment).ToList();
            var rankedEquip = equipPool.OrderByDescending(e =>
            {
                float slotWeight = e.equipmentSlot switch
                {
                    EquipmentSlot.Offensive => w.genes[DecisionWeights.EQUIP_OFFENSIVE_W],
                    EquipmentSlot.Defensive => w.genes[DecisionWeights.EQUIP_DEFENSIVE_W],
                    EquipmentSlot.Utility => w.genes[DecisionWeights.EQUIP_UTILITY_W],
                    _ => 0.5f
                };
                return slotWeight * (e.effectValue1 + 1);
            }).ToList();

            foreach (var equip in rankedEquip)
            {
                if (equipment.Count >= equipTarget) break;
                int maxCopies = GetMaxCopies(equip.deckCost);
                int current = cardCopyCounts.GetValueOrDefault(equip.cardId, 0);
                if (current < maxCopies)
                {
                    equipment.Add(equip);
                    cardCopyCounts[equip.cardId] = current + 1;
                }
            }

            // ── Tactical ────────────────────────────────────────────────
            int tactTarget = targetDeckSize - heroes.Count - colony.Count - equipment.Count;
            var tactPool = allCards.Where(c => c.cardType == CardType.Tactical).ToList();

            foreach (var tact in tactPool)
            {
                if (tactical.Count >= tactTarget) break;
                int maxCopies = GetMaxCopies(tact.deckCost);
                int current = cardCopyCounts.GetValueOrDefault(tact.cardId, 0);
                if (current < maxCopies)
                {
                    tactical.Add(tact);
                    cardCopyCounts[tact.cardId] = current + 1;
                }
            }

            if (!Scurry.Core.SimulationFlags.SuppressLogging) Debug.Log($"[SimulatedDeckBuilder] BuildDeck: heroes={heroes.Count}, colony={colony.Count}, " +
                      $"equipment={equipment.Count}, tactical={tactical.Count}, " +
                      $"total={heroes.Count + colony.Count + equipment.Count + tactical.Count}");

            return (heroes, colony, equipment, tactical);
        }

        private float ScoreColonyCard(ColonyCardDefinitionSO card, DecisionWeights w)
        {
            return card.colonyEffect switch
            {
                ColonyEffect.FoodProduction => w.genes[DecisionWeights.COLONY_FOOD_W] * card.effectValue,
                ColonyEffect.MushFoodProduction => w.genes[DecisionWeights.COLONY_MUSH_W] * card.effectValue,
                ColonyEffect.DoubleProduction => w.genes[DecisionWeights.COLONY_DOUBLE_W] * 5f,
                ColonyEffect.ColonyDefenseWall => w.genes[DecisionWeights.COLONY_DEFENSE_W] * card.effectValue,
                ColonyEffect.ColonyDefenseBonus => w.genes[DecisionWeights.COLONY_DEFENSE_W] * card.effectValue,
                ColonyEffect.FogReveal => w.genes[DecisionWeights.COLONY_FOG_W] * card.effectValue,
                ColonyEffect.FogRevealHeroes => w.genes[DecisionWeights.COLONY_FOG_W] * card.effectValue,
                ColonyEffect.AllHeroCombatBuff => w.genes[DecisionWeights.COLONY_COMBAT_W] * card.effectValue * 3f,
                ColonyEffect.FirstCombatBuff => w.genes[DecisionWeights.COLONY_COMBAT_W] * card.effectValue,
                ColonyEffect.HealInjured => w.genes[DecisionWeights.COLONY_HEAL_W] * card.effectValue,
                ColonyEffect.AllHeroMoveBuff => w.genes[DecisionWeights.COLONY_MOVE_W] * card.effectValue,
                _ => 0.5f
            };
        }

        private static int GetMaxCopies(int deckCost)
        {
            return deckCost switch
            {
                0 => 1, // Starters
                1 => 3,
                2 => 2,
                _ => 1
            };
        }
    }
}
