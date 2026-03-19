using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scurry.Data;
using Scurry.Core;
using Scurry.Map;

namespace Scurry.Combat
{
    /// <summary>
    /// Processes all 30 tactical card effects during combat. Tactical cards are single-use
    /// and can be played before the first round or between rounds.
    /// </summary>
    public static class TacticalEffectProcessor
    {
        /// <summary>
        /// Applies a tactical card's effect. Returns true if the card was successfully applied.
        /// </summary>
        public static bool ApplyTacticalCard(
            CardDefinitionSO card,
            List<HeroToken> heroes,
            List<EnemyToken> enemies,
            int nodeId,
            CombatContext context)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyTacticalCard: ENTER (card={card.cardName}, cardId={card.cardId}, nodeId={nodeId}, round={context.roundNumber})");

            if (card.cardType != CardType.Tactical)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[TacticalEffectProcessor] ApplyTacticalCard: card={card.cardName} is not a Tactical card (type={card.cardType})");
                return false;
            }

            EventBus.OnTacticalCardPlayed?.Invoke(card);

            bool result = false;

            switch (card.cardId)
            {
                // --- Combat Tactics (91-100) ---
                case 91: result = ApplyPackAmbush(card, heroes, context); break;
                case 92: result = ApplyRallyThePack(card, heroes, context); break;
                case 93: result = ApplyDefensiveFormation(card, heroes, context); break;
                case 94: result = ApplyLastStand(card, heroes, context); break;
                case 95: result = ApplyRapidStrike(card, heroes, context); break;
                case 96: result = ApplyTacticalRetreat(card, heroes, context); break;
                case 97: result = ApplyReinforcements(card, heroes, nodeId, context); break;
                case 98: result = ApplyHiddenTunnel(card, heroes, nodeId, context); break;
                case 99: result = ApplyMapAdvantage(card, heroes, nodeId, context); break;
                case 100: result = ApplyScoutAhead(card, nodeId, context); break;

                // --- Support Tactics (101-110) ---
                case 101: result = ApplyGatherSeeds(card, context); break;
                case 102: result = ApplyHarvestBounty(card, context); break;
                case 103: result = ApplyEmergencyRations(card, context); break;
                case 104: result = ApplySalvageScrap(card, nodeId, context); break;
                case 105: result = ApplyTradeCaravan(card, context); break;
                case 106: result = ApplyHerbalRemedy(card, heroes, nodeId, context); break;
                case 107: result = ApplyWarmBurrow(card, context); break;
                case 108: result = ApplyRestAndRecover(card, heroes, nodeId, context); break;
                case 109: result = ApplyMedicCall(card, heroes, context); break;
                case 110: result = ApplyRenewStrength(card, heroes, nodeId, context); break;

                // --- Power Tactics (111-120) ---
                case 111: result = ApplyHeroicCharge(card, heroes, context); break;
                case 112: result = ApplyPackUnity(card, heroes, context); break;
                case 113: result = ApplyBurrowDefense(card, context); break;
                case 114: result = ApplyFinalGambit(card, heroes, context); break;
                case 115: result = ApplyHiddenKingdom(card, context); break;
                case 116: result = ApplySurpriseRaid(card, heroes, context); break;
                case 117: result = ApplyPoisonThorn(card, enemies, context); break;
                case 118: result = ApplyMoonlightStrike(card, heroes, context); break;
                case 119: result = ApplyRatSwarm(card, heroes, nodeId, context); break;
                case 120: result = ApplyVictoryFeast(card, heroes, context); break;

                default:
                    if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[TacticalEffectProcessor] ApplyTacticalCard: unknown cardId={card.cardId}, name={card.cardName}");
                    return false;
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyTacticalCard: EXIT (card={card.cardName}, success={result})");
            return result;
        }

        // ===================================================================
        // Combat Tactics (91-100)
        // ===================================================================

        /// <summary>91: Pack Ambush — +2 Combat to all heroes on this node for 1 round.</summary>
        private static bool ApplyPackAmbush(CardDefinitionSO card, List<HeroToken> heroes, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyPackAmbush: ENTER (effectValue1={card.effectValue1}, duration={card.effectValue2} round(s))");
            int buffAmount = card.effectValue1; // 2
            int count = 0;
            foreach (var hero in heroes.Where(h => h.IsAlive))
            {
                ctx.AddTempCombatBuff(hero.tokenId, buffAmount);
                count++;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyPackAmbush: hero={hero.cardDef.cardName} gets +{buffAmount} temp Combat");
            }
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyPackAmbush: EXIT (buffed {count} heroes with +{buffAmount} Combat for 1 round)");
            return count > 0;
        }

        /// <summary>92: Rally the Pack — +1 Combat to all heroes for rest of combat.</summary>
        private static bool ApplyRallyThePack(CardDefinitionSO card, List<HeroToken> heroes, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRallyThePack: ENTER (effectValue1={card.effectValue1})");
            int buffAmount = card.effectValue1; // 1
            int count = 0;
            foreach (var hero in heroes.Where(h => h.IsAlive))
            {
                ctx.AddPermanentCombatBuff(hero.tokenId, buffAmount);
                count++;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRallyThePack: hero={hero.cardDef.cardName} gets +{buffAmount} permanent Combat");
            }
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRallyThePack: EXIT (buffed {count} heroes with +{buffAmount} Combat permanently)");
            return count > 0;
        }

        /// <summary>93: Defensive Formation — all heroes block 1 damage this round.</summary>
        private static bool ApplyDefensiveFormation(CardDefinitionSO card, List<HeroToken> heroes, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyDefensiveFormation: ENTER (blockAmount={card.effectValue1})");
            int blockAmount = card.effectValue1; // 1
            int count = 0;
            foreach (var hero in heroes.Where(h => h.IsAlive))
            {
                ctx.AddDamageBlock(hero.tokenId, blockAmount);
                count++;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyDefensiveFormation: hero={hero.cardDef.cardName} blocks {blockAmount} damage this round");
            }
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyDefensiveFormation: EXIT (applied {blockAmount} block to {count} heroes)");
            return count > 0;
        }

        /// <summary>94: Last Stand — hero with 1 HP gets +5 Combat this round.</summary>
        private static bool ApplyLastStand(CardDefinitionSO card, List<HeroToken> heroes, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyLastStand: ENTER (combatBuff={card.effectValue1}, hpThreshold={card.effectValue2})");
            int buff = card.effectValue1; // 5
            int hpThreshold = card.effectValue2; // 1

            var target = heroes.Where(h => h.IsAlive && h.currentHP <= hpThreshold)
                .OrderBy(h => h.currentHP)
                .ThenBy(h => h.EffectiveCombat)
                .FirstOrDefault();

            if (target == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyLastStand: no hero with HP <= {hpThreshold}, card wasted");
                return false;
            }

            ctx.AddTempCombatBuff(target.tokenId, buff);
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyLastStand: EXIT (hero={target.cardDef.cardName}, hp={target.currentHP}, +{buff} Combat this round)");
            return true;
        }

        /// <summary>95: Rapid Strike — one hero attacks twice this round.</summary>
        private static bool ApplyRapidStrike(CardDefinitionSO card, List<HeroToken> heroes, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRapidStrike: ENTER");

            // Pick the highest-combat hero for maximum impact
            var target = heroes.Where(h => h.IsAlive)
                .OrderByDescending(h => h.EffectiveCombat)
                .FirstOrDefault();

            if (target == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRapidStrike: no alive heroes");
                return false;
            }

            ctx.doubleAttackHeroes.Add(target.tokenId);
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRapidStrike: EXIT (hero={target.cardDef.cardName} attacks twice this round, effectiveCombat={target.EffectiveCombat})");
            return true;
        }

        /// <summary>96: Tactical Retreat — remove all heroes from this node.</summary>
        private static bool ApplyTacticalRetreat(CardDefinitionSO card, List<HeroToken> heroes, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyTacticalRetreat: ENTER (heroes={heroes.Count(h => h.IsAlive)})");
            ctx.retreatTriggered = true;
            int count = heroes.Count(h => h.IsAlive);
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyTacticalRetreat: EXIT (retreat triggered, {count} heroes will relocate to nearest friendly node)");
            return true;
        }

        /// <summary>97: Reinforcements — deploy 1 hero from available pool to this node.</summary>
        private static bool ApplyReinforcements(CardDefinitionSO card, List<HeroToken> heroes, int nodeId, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyReinforcements: ENTER (nodeId={nodeId})");
            // The actual deployment is handled by the game manager; we set a flag.
            // For combat purposes, this is a signal. The caller inspects the context.
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyReinforcements: EXIT (reinforcement request flagged for nodeId={nodeId}; caller must deploy hero)");
            return true;
        }

        /// <summary>98: Hidden Tunnel — move all heroes to an adjacent node (escape combat).</summary>
        private static bool ApplyHiddenTunnel(CardDefinitionSO card, List<HeroToken> heroes, int nodeId, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyHiddenTunnel: ENTER (nodeId={nodeId}, heroes={heroes.Count(h => h.IsAlive)})");
            // Like retreat but heroes move to adjacent node specifically
            ctx.retreatTriggered = true;
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyHiddenTunnel: EXIT (retreat via tunnel triggered; caller must move heroes to adjacent node)");
            return true;
        }

        /// <summary>99: Map Advantage — reveal zone + heroes get +2 Combat.</summary>
        private static bool ApplyMapAdvantage(CardDefinitionSO card, List<HeroToken> heroes, int nodeId, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyMapAdvantage: ENTER (nodeId={nodeId}, combatBuff={card.effectValue1})");
            int buff = card.effectValue1; // 2
            int count = 0;
            foreach (var hero in heroes.Where(h => h.IsAlive))
            {
                ctx.AddPermanentCombatBuff(hero.tokenId, buff);
                count++;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyMapAdvantage: hero={hero.cardDef.cardName} gets +{buff} Combat");
            }
            // Fog reveal is handled by the caller (MapManager) via event
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyMapAdvantage: EXIT (buffed {count} heroes +{buff} Combat, zone reveal signaled)");
            return true;
        }

        /// <summary>100: Scout Ahead — reveal nodes within 2 edges, no combat effect.</summary>
        private static bool ApplyScoutAhead(CardDefinitionSO card, int nodeId, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyScoutAhead: ENTER (nodeId={nodeId}, revealRange={card.effectValue1})");
            // Fog reveal handled by caller via event
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyScoutAhead: EXIT (reveal range={card.effectValue1} signaled; no combat effect)");
            return true;
        }

        // ===================================================================
        // Support Tactics (101-110)
        // ===================================================================

        /// <summary>101: Gather Seeds — heroes gather +1 resource this turn.</summary>
        private static bool ApplyGatherSeeds(CardDefinitionSO card, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyGatherSeeds: ENTER (bonus={card.effectValue1})");
            ctx.gatherBonus += card.effectValue1; // 1
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyGatherSeeds: EXIT (gatherBonus now={ctx.gatherBonus})");
            return true;
        }

        /// <summary>102: Harvest Bounty — double gathering this turn.</summary>
        private static bool ApplyHarvestBounty(CardDefinitionSO card, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyHarvestBounty: ENTER");
            ctx.doubleGathering = true;
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyHarvestBounty: EXIT (doubleGathering=true)");
            return true;
        }

        /// <summary>103: Emergency Rations — no food cost this turn.</summary>
        private static bool ApplyEmergencyRations(CardDefinitionSO card, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyEmergencyRations: ENTER");
            ctx.emergencyRations = true;
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyEmergencyRations: EXIT (emergencyRations=true)");
            return true;
        }

        /// <summary>104: Salvage Scrap — recover previously gathered resources on this node.</summary>
        private static bool ApplySalvageScrap(CardDefinitionSO card, int nodeId, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplySalvageScrap: ENTER (nodeId={nodeId})");
            // Resource restoration handled by caller (MapManager / ResourceManager)
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplySalvageScrap: EXIT (resource recovery signaled for nodeId={nodeId})");
            return true;
        }

        /// <summary>105: Trade Caravan — convert 3 resources to 3 other type.</summary>
        private static bool ApplyTradeCaravan(CardDefinitionSO card, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyTradeCaravan: ENTER (from={card.effectValue1}, to={card.effectValue2})");
            // Resource conversion handled by caller (ResourceManager)
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyTradeCaravan: EXIT (conversion signaled: {card.effectValue1} -> {card.effectValue2})");
            return true;
        }

        /// <summary>106: Herbal Remedy — heal 2 HP to one hero on this node.</summary>
        private static bool ApplyHerbalRemedy(CardDefinitionSO card, List<HeroToken> heroes, int nodeId, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyHerbalRemedy: ENTER (healAmount={card.effectValue1}, nodeId={nodeId})");
            int healAmount = card.effectValue1; // 2

            var target = heroes.Where(h => h.IsAlive && h.currentHP < h.maxHP)
                .OrderBy(h => h.currentHP)
                .FirstOrDefault();

            if (target == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyHerbalRemedy: no hero needs healing on this node");
                return false;
            }

            target.Heal(healAmount);
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyHerbalRemedy: EXIT (healed hero={target.cardDef.cardName} for {healAmount} HP, now={target.currentHP}/{target.maxHP})");
            return true;
        }

        /// <summary>107: Warm Burrow — all injured heroes in available pool heal immediately.</summary>
        private static bool ApplyWarmBurrow(CardDefinitionSO card, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyWarmBurrow: ENTER");
            // Healing of the available pool is handled by the caller (RunManager / ColonyManager)
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyWarmBurrow: EXIT (pool heal signaled)");
            return true;
        }

        /// <summary>108: Rest and Recover — heal 1 HP to all heroes on this node.</summary>
        private static bool ApplyRestAndRecover(CardDefinitionSO card, List<HeroToken> heroes, int nodeId, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRestAndRecover: ENTER (healAmount={card.effectValue1}, nodeId={nodeId})");
            int healAmount = card.effectValue1; // 1
            int count = 0;
            foreach (var hero in heroes.Where(h => h.IsAlive))
            {
                if (hero.currentHP < hero.maxHP)
                {
                    hero.Heal(healAmount);
                    count++;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRestAndRecover: healed hero={hero.cardDef.cardName} for {healAmount}, now={hero.currentHP}/{hero.maxHP}");
                }
                else
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRestAndRecover: hero={hero.cardDef.cardName} already at full HP");
                }
            }
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRestAndRecover: EXIT (healed {count} heroes for {healAmount} HP each)");
            return true;
        }

        /// <summary>109: Medic Call — heal 3 HP to one hero anywhere on the map.</summary>
        private static bool ApplyMedicCall(CardDefinitionSO card, List<HeroToken> heroes, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyMedicCall: ENTER (healAmount={card.effectValue1})");
            int healAmount = card.effectValue1; // 3

            // Pick lowest-HP hero that needs healing (can be anywhere, but we only have local heroes here)
            // The caller should provide all heroes on the map for this card.
            var target = heroes.Where(h => h.IsAlive && h.currentHP < h.maxHP)
                .OrderBy(h => h.currentHP)
                .FirstOrDefault();

            if (target == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyMedicCall: no hero needs healing");
                return false;
            }

            target.Heal(healAmount);
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyMedicCall: EXIT (healed hero={target.cardDef.cardName} for {healAmount} HP, now={target.currentHP}/{target.maxHP})");
            return true;
        }

        /// <summary>110: Renew Strength — fully heal one hero on this node.</summary>
        private static bool ApplyRenewStrength(CardDefinitionSO card, List<HeroToken> heroes, int nodeId, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRenewStrength: ENTER (nodeId={nodeId})");

            var target = heroes.Where(h => h.IsAlive && h.currentHP < h.maxHP)
                .OrderBy(h => h.currentHP)
                .FirstOrDefault();

            if (target == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRenewStrength: no hero needs healing on this node");
                return false;
            }

            int prevHP = target.currentHP;
            target.Heal(target.maxHP); // Heal method clamps to maxHP
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRenewStrength: EXIT (hero={target.cardDef.cardName} fully healed {prevHP}->{target.currentHP})");
            return true;
        }

        // ===================================================================
        // Power Tactics (111-120)
        // ===================================================================

        /// <summary>111: Heroic Charge — all heroes +3 Combat, +1 Move this turn.</summary>
        private static bool ApplyHeroicCharge(CardDefinitionSO card, List<HeroToken> heroes, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyHeroicCharge: ENTER (combatBuff={card.effectValue1}, moveBuff={card.effectValue2})");
            int combatBuff = card.effectValue1; // 3
            int moveBuff = card.effectValue2;   // 1
            int count = 0;
            foreach (var hero in heroes.Where(h => h.IsAlive))
            {
                ctx.AddPermanentCombatBuff(hero.tokenId, combatBuff);
                // Move buff applied to the hero token directly (persists for this turn)
                hero.baseMove += moveBuff;
                count++;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyHeroicCharge: hero={hero.cardDef.cardName} +{combatBuff} Combat, +{moveBuff} Move");
            }
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyHeroicCharge: EXIT (buffed {count} heroes)");
            return count > 0;
        }

        /// <summary>112: Pack Unity — all heroes on the map +1 Combat this turn.</summary>
        private static bool ApplyPackUnity(CardDefinitionSO card, List<HeroToken> heroes, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyPackUnity: ENTER (combatBuff={card.effectValue1})");
            int combatBuff = card.effectValue1; // 1
            int count = 0;
            // Note: affects ALL heroes on map, not just this node. Caller should provide full hero list.
            foreach (var hero in heroes.Where(h => h.IsAlive))
            {
                ctx.AddPermanentCombatBuff(hero.tokenId, combatBuff);
                count++;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyPackUnity: hero={hero.cardDef.cardName} +{combatBuff} Combat");
            }
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyPackUnity: EXIT (buffed {count} heroes on map with +{combatBuff} Combat)");
            return count > 0;
        }

        /// <summary>113: Burrow Defense — colony +5 Combat defense.</summary>
        private static bool ApplyBurrowDefense(CardDefinitionSO card, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyBurrowDefense: ENTER (defenseBonus={card.effectValue1})");
            ctx.colonyDefenseBonus += card.effectValue1; // 5
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyBurrowDefense: EXIT (colonyDefenseBonus now={ctx.colonyDefenseBonus})");
            return true;
        }

        /// <summary>114: Final Gambit — one hero +10 Combat, injured after combat.</summary>
        private static bool ApplyFinalGambit(CardDefinitionSO card, List<HeroToken> heroes, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyFinalGambit: ENTER (combatBuff={card.effectValue1})");
            int buff = card.effectValue1; // 10

            // Pick highest-combat hero for maximum impact
            var target = heroes.Where(h => h.IsAlive)
                .OrderByDescending(h => h.EffectiveCombat)
                .FirstOrDefault();

            if (target == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyFinalGambit: no alive heroes");
                return false;
            }

            ctx.AddTempCombatBuff(target.tokenId, buff);
            ctx.injureAfterCombat.Add(target.tokenId);
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyFinalGambit: EXIT (hero={target.cardDef.cardName} gets +{buff} Combat this round, will be injured after combat)");
            return true;
        }

        /// <summary>115: Hidden Kingdom — reveal entire map.</summary>
        private static bool ApplyHiddenKingdom(CardDefinitionSO card, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyHiddenKingdom: ENTER");
            // Full map reveal handled by caller (MapManager)
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyHiddenKingdom: EXIT (full map reveal signaled)");
            return true;
        }

        /// <summary>116: Surprise Raid — one hero moves to any node and attacks.</summary>
        private static bool ApplySurpriseRaid(CardDefinitionSO card, List<HeroToken> heroes, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplySurpriseRaid: ENTER");
            // The actual movement and attack is handled by the caller (TurnManager / MapManager)
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplySurpriseRaid: EXIT (surprise raid signaled; caller must handle hero teleport + attack)");
            return true;
        }

        /// <summary>117: Poison Thorn — deal 3 damage to one enemy before combat.</summary>
        private static bool ApplyPoisonThorn(CardDefinitionSO card, List<EnemyToken> enemies, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyPoisonThorn: ENTER (damage={card.effectValue1})");
            int damage = card.effectValue1; // 3

            // Target the enemy with the lowest HP for maximum chance of elimination
            var target = enemies.Where(e => e.IsAlive)
                .OrderBy(e => e.currentHP)
                .FirstOrDefault();

            if (target == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyPoisonThorn: no alive enemies");
                return false;
            }

            target.TakeDamage(damage);
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyPoisonThorn: EXIT (enemy={target.definition.enemyName} took {damage} damage, hp={target.currentHP}, defeated={target.isDefeated})");
            return true;
        }

        /// <summary>118: Moonlight Strike — one hero deals double damage this round.</summary>
        private static bool ApplyMoonlightStrike(CardDefinitionSO card, List<HeroToken> heroes, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyMoonlightStrike: ENTER");

            var target = heroes.Where(h => h.IsAlive)
                .OrderByDescending(h => h.EffectiveCombat)
                .FirstOrDefault();

            if (target == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyMoonlightStrike: no alive heroes");
                return false;
            }

            ctx.doubleDamageHeroes.Add(target.tokenId);
            // Double damage implemented as double-attack (same mechanical effect on pooled strength)
            ctx.doubleAttackHeroes.Add(target.tokenId);
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyMoonlightStrike: EXIT (hero={target.cardDef.cardName} deals double damage this round)");
            return true;
        }

        /// <summary>119: Rat Swarm — deploy ALL available heroes to this node.</summary>
        private static bool ApplyRatSwarm(CardDefinitionSO card, List<HeroToken> heroes, int nodeId, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRatSwarm: ENTER (nodeId={nodeId})");
            // The actual deployment of available heroes is handled by the caller (TurnManager)
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyRatSwarm: EXIT (mass deploy signaled for nodeId={nodeId}; caller must deploy all available heroes)");
            return true;
        }

        /// <summary>120: Victory Feast — heal all heroes full, +2 food production this turn.</summary>
        private static bool ApplyVictoryFeast(CardDefinitionSO card, List<HeroToken> heroes, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyVictoryFeast: ENTER (foodBonus={card.effectValue2})");
            int count = 0;
            foreach (var hero in heroes.Where(h => h.IsAlive))
            {
                if (hero.currentHP < hero.maxHP)
                {
                    int prevHP = hero.currentHP;
                    hero.Heal(hero.maxHP);
                    count++;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyVictoryFeast: healed hero={hero.cardDef.cardName} {prevHP}->{hero.currentHP}");
                }
            }
            ctx.foodProductionBonus += card.effectValue2; // 2
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[TacticalEffectProcessor] ApplyVictoryFeast: EXIT (healed {count} heroes to full, foodProductionBonus now={ctx.foodProductionBonus})");
            return true;
        }
    }
}
