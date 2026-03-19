using System.Collections.Generic;
using Scurry.Core;

namespace Scurry.Combat
{
    /// <summary>
    /// Tracks per-combat mutable state: temp buffs, damage blocks, tactical card effects, etc.
    /// Created fresh for each combat encounter. Shared across CombatResolver, TacticalEffectProcessor,
    /// EquipmentEffectProcessor, and SpecialAbilityProcessor.
    /// </summary>
    public class CombatContext
    {
        public int nodeId;
        public int roundNumber;

        /// <summary>Per-hero temporary combat buffs that last 1 round (cleared each round).</summary>
        public Dictionary<int, int> tempCombatBuffs = new Dictionary<int, int>();

        /// <summary>Per-hero permanent combat buffs for the rest of this combat.</summary>
        public Dictionary<int, int> permanentCombatBuffs = new Dictionary<int, int>();

        /// <summary>Per-hero damage blocked per round.</summary>
        public Dictionary<int, int> damageBlockPerRound = new Dictionary<int, int>();

        /// <summary>Heroes that attack twice this round.</summary>
        public HashSet<int> doubleAttackHeroes = new HashSet<int>();

        /// <summary>Heroes that deal double damage this round.</summary>
        public HashSet<int> doubleDamageHeroes = new HashSet<int>();

        /// <summary>Heroes that will be injured after combat ends (e.g., Final Gambit).</summary>
        public HashSet<int> injureAfterCombat = new HashSet<int>();

        /// <summary>True if heroes entered a fogged node — enemies get one free attack round.</summary>
        public bool isAmbush;

        /// <summary>True if Tactical Retreat was played (combat ends, heroes relocate).</summary>
        public bool retreatTriggered;

        /// <summary>True if Emergency Rations was played (no food cost this turn).</summary>
        public bool emergencyRations;

        /// <summary>True if Harvest Bounty was played (double gathering this turn).</summary>
        public bool doubleGathering;

        /// <summary>Bonus resources to gather on this node.</summary>
        public int gatherBonus;

        /// <summary>Colony combat defense bonus (from Burrow Defense tactical).</summary>
        public int colonyDefenseBonus;

        /// <summary>Enemies that are stunned this round (skip attacking).</summary>
        public HashSet<int> stunnedEnemies = new HashSet<int>();

        /// <summary>Total food production bonus this turn (from Victory Feast, etc.).</summary>
        public int foodProductionBonus;

        /// <summary>Tracks which heroes have already used FirstStrike this combat.</summary>
        public HashSet<int> firstStrikeUsed = new HashSet<int>();

        /// <summary>Tracks which heroes have already used RangedStrike this round.</summary>
        public HashSet<int> rangedStrikeUsed = new HashSet<int>();

        /// <summary>
        /// Clears per-round state. Called at the start of each combat round.
        /// Permanent buffs are preserved.
        /// </summary>
        public void AdvanceRound()
        {
            roundNumber++;
            tempCombatBuffs.Clear();
            damageBlockPerRound.Clear();
            doubleAttackHeroes.Clear();
            doubleDamageHeroes.Clear();
            stunnedEnemies.Clear();
            rangedStrikeUsed.Clear();
            if (!SimulationFlags.SuppressLogging) UnityEngine.Debug.Log($"[CombatContext] AdvanceRound: nodeId={nodeId}, now round={roundNumber} (cleared temp buffs, blocks, double-attacks, stuns)");
        }

        /// <summary>
        /// Gets the total combat buff for a hero (temp + permanent).
        /// </summary>
        public int GetTotalCombatBuff(int heroTokenId)
        {
            int temp = tempCombatBuffs.TryGetValue(heroTokenId, out int t) ? t : 0;
            int perm = permanentCombatBuffs.TryGetValue(heroTokenId, out int p) ? p : 0;
            return temp + perm;
        }

        /// <summary>
        /// Gets damage block amount for a hero this round.
        /// </summary>
        public int GetDamageBlock(int heroTokenId)
        {
            return damageBlockPerRound.TryGetValue(heroTokenId, out int block) ? block : 0;
        }

        public void AddTempCombatBuff(int heroTokenId, int amount)
        {
            if (!tempCombatBuffs.ContainsKey(heroTokenId))
                tempCombatBuffs[heroTokenId] = 0;
            tempCombatBuffs[heroTokenId] += amount;
            if (!SimulationFlags.SuppressLogging) UnityEngine.Debug.Log($"[CombatContext] AddTempCombatBuff: heroTokenId={heroTokenId}, amount={amount}, total={tempCombatBuffs[heroTokenId]}");
        }

        public void AddPermanentCombatBuff(int heroTokenId, int amount)
        {
            if (!permanentCombatBuffs.ContainsKey(heroTokenId))
                permanentCombatBuffs[heroTokenId] = 0;
            permanentCombatBuffs[heroTokenId] += amount;
            if (!SimulationFlags.SuppressLogging) UnityEngine.Debug.Log($"[CombatContext] AddPermanentCombatBuff: heroTokenId={heroTokenId}, amount={amount}, total={permanentCombatBuffs[heroTokenId]}");
        }

        public void AddDamageBlock(int heroTokenId, int amount)
        {
            if (!damageBlockPerRound.ContainsKey(heroTokenId))
                damageBlockPerRound[heroTokenId] = 0;
            damageBlockPerRound[heroTokenId] += amount;
            if (!SimulationFlags.SuppressLogging) UnityEngine.Debug.Log($"[CombatContext] AddDamageBlock: heroTokenId={heroTokenId}, amount={amount}, total={damageBlockPerRound[heroTokenId]}");
        }
    }
}
