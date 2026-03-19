using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scurry.Data;
using Scurry.Map;
using Scurry.Core;

namespace Scurry.Combat
{
    /// <summary>
    /// Processes equipment special effects during combat and stat computation.
    /// Equipment is attached to heroes at deployment. Each hero has up to 3 slots
    /// (Offensive, Defensive, Utility). Legendary items occupy one of these slots.
    ///
    /// Card ID ranges:
    ///   51-60: Weapons (Offensive)
    ///   61-70: Armor (Defensive)
    ///   71-80: Utility
    ///   81-90: Legendary
    /// </summary>
    public static class EquipmentEffectProcessor
    {
        // ===================================================================
        // Stat Bonuses (computed from all equipped items)
        // ===================================================================

        /// <summary>
        /// Returns the total Combat bonus from all equipped items on a hero.
        /// </summary>
        public static int GetCombatBonus(HeroToken hero)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetCombatBonus: ENTER (hero={hero.cardDef.cardName}, equippedCount={hero.equippedItems.Count})");
            int total = 0;

            foreach (var item in hero.equippedItems)
            {
                int bonus = GetCombatBonusForItem(item, hero);
                total += bonus;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetCombatBonus: item={item.cardName} (id={item.cardId}), combatBonus={bonus}");
            }

            // DualWield ability: +1 Combat per equipped offensive item
            if (hero.cardDef.specialAbility == SpecialAbility.DualWield)
            {
                int offensiveCount = hero.equippedItems.Count(i => i.equipmentSlot == EquipmentSlot.Offensive);
                if (offensiveCount > 0)
                {
                    total += offensiveCount;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetCombatBonus: DualWield ability — +{offensiveCount} Combat (from {offensiveCount} offensive items)");
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetCombatBonus: EXIT (hero={hero.cardDef.cardName}, totalCombatBonus={total})");
            return total;
        }

        /// <summary>
        /// Returns the Combat bonus from a single equipment card.
        /// </summary>
        public static int GetCombatBonusForItem(CardDefinitionSO item, HeroToken hero = null)
        {
            switch (item.cardId)
            {
                // --- Weapons (Offensive) ---
                case 51: return item.effectValue1; // Needle Sword: +1 Combat
                case 52: return item.effectValue1; // Thorn Spear: +1 Combat (also has range)
                case 53: return item.effectValue1; // Pin Dagger: +1 Combat (+1 Init is separate)
                case 54: // Splinter Staff: +1 Combat to ALL friendly tokens on same node
                    // The aura effect is applied via the round processor; base combat for wielder is effectValue1
                    return item.effectValue1;
                case 55: return item.effectValue1; // Needle Lance: +2 Combat
                case 56: return item.effectValue1; // Pebble Sling: +1 Combat (also ranged)
                case 57: return item.effectValue1; // Thorn Whip: +2 Combat
                case 58: return item.effectValue1; // Bone Knife: +2 Combat (+1 Init is separate)
                case 59: return item.effectValue1; // Razor Leaf Blade: +3 Combat
                case 60: return item.effectValue1; // Acorn Hammer: +3 Combat (also stuns)

                // --- Armor (Defensive) — generally no combat bonus ---
                case 61: return 0; // Thimble Helmet: +1 HP only
                case 62: return 0; // Button Shield: +1 HP, blocks 1 dmg
                case 63: return 0; // Bark Armor: +2 HP only
                case 64: return 0; // Moss Cloak: cannot be targeted first
                case 65: return 0; // Feather Cape: +1 HP, +1 Move
                case 66: return item.effectValue2; // Leaf Armor: +2 HP, +1 Combat
                case 67: return 0; // Beetle Shell Shield: +2 HP, blocks 1 dmg
                case 68: return 0; // Acorn Helm: +3 HP
                case 69: return 0; // Grapple Hook: +1 HP, traversal
                case 70: return 0; // Thread Rope: +1 HP, pull

                // --- Utility — generally no combat bonus ---
                case 71: return 0; // Bead Lantern: fog reveal
                case 72: return 0; // Button Compass: +1 Move
                case 73: return 0; // Cloth Satchel: +2 Carry
                case 74: return 0; // Matchstick Ladder: terrain ignore
                case 75: return 0; // Berry Basket: +3 Carry
                case 76: return 0; // Map Scroll: path reveal
                case 77: return 0; // Healing Herb Kit: heal per turn
                case 78: // Snail Shell Horn: +1 Combat to ADJACENT heroes (aura, not self)
                    return 0; // Aura applied in round processing
                case 79: return 0; // Flint Firestarter: pre-combat damage
                case 80: return 0; // Climbing Claws: +2 Move, traversal

                // --- Legendary ---
                case 81: return item.effectValue1; // Storm Needle: +4 Combat (splash separate)
                case 82: return item.effectValue1; // Ember Torch: +3 Combat (fog reveal separate)
                case 83: return 0; // Frost Thread Cloak: +3 HP, enemies -1 Combat (debuff applied elsewhere)
                case 84: return 0; // Golden Acorn Charm: +2 HP, food production
                case 85: // War Banner Pin: +2 Combat to ALL friendlies on node (aura)
                    return item.effectValue1; // Wielder also gets the bonus
                case 86: return item.effectValue1; // Thorn Crown: +3 Combat (self-dmg separate)
                case 87: return 0; // Root Armor: +5 HP, -1 Move
                case 88: return item.effectValue1; // Moonlight Dagger: +3 Combat (+2 Init separate)
                case 89: return 0; // Lantern of the Deep: +2 HP, zone reveal
                case 90: return item.effectValue1; // Banner of Rat Pack: +2 Combat to ALL on map (aura)

                default:
                    if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[EquipmentEffectProcessor] GetCombatBonusForItem: unknown cardId={item.cardId}, name={item.cardName}");
                    return item.effectValue1;
            }
        }

        /// <summary>
        /// Returns the total HP bonus from all equipped items on a hero.
        /// </summary>
        public static int GetHPBonus(HeroToken hero)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetHPBonus: ENTER (hero={hero.cardDef.cardName})");
            int total = 0;

            foreach (var item in hero.equippedItems)
            {
                int bonus = GetHPBonusForItem(item);
                total += bonus;
                if (bonus != 0)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetHPBonus: item={item.cardName}, hpBonus={bonus}");
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetHPBonus: EXIT (hero={hero.cardDef.cardName}, totalHPBonus={total})");
            return total;
        }

        private static int GetHPBonusForItem(CardDefinitionSO item)
        {
            switch (item.cardId)
            {
                case 61: return item.effectValue1; // Thimble Helmet: +1 HP
                case 62: return item.effectValue1; // Button Shield: +1 HP
                case 63: return item.effectValue1; // Bark Armor: +2 HP
                case 64: return 0;                 // Moss Cloak: no HP
                case 65: return item.effectValue1; // Feather Cape: +1 HP
                case 66: return item.effectValue1; // Leaf Armor: +2 HP
                case 67: return item.effectValue1; // Beetle Shell Shield: +2 HP
                case 68: return item.effectValue1; // Acorn Helm: +3 HP
                case 69: return item.effectValue1; // Grapple Hook: +1 HP
                case 70: return item.effectValue1; // Thread Rope: +1 HP
                case 83: return item.effectValue1; // Frost Thread Cloak: +3 HP
                case 84: return item.effectValue1; // Golden Acorn Charm: +2 HP
                case 87: return item.effectValue1; // Root Armor: +5 HP
                case 89: return item.effectValue1; // Lantern of the Deep: +2 HP
                default: return 0;
            }
        }

        /// <summary>
        /// Returns the total Move bonus from all equipped items on a hero.
        /// </summary>
        public static int GetMoveBonus(HeroToken hero)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetMoveBonus: ENTER (hero={hero.cardDef.cardName})");
            int total = 0;

            foreach (var item in hero.equippedItems)
            {
                int bonus = GetMoveBonusForItem(item);
                total += bonus;
                if (bonus != 0)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetMoveBonus: item={item.cardName}, moveBonus={bonus}");
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetMoveBonus: EXIT (hero={hero.cardDef.cardName}, totalMoveBonus={total})");
            return total;
        }

        private static int GetMoveBonusForItem(CardDefinitionSO item)
        {
            switch (item.cardId)
            {
                case 65: return item.effectValue2; // Feather Cape: +1 Move
                case 72: return item.effectValue1; // Button Compass: +1 Move
                case 80: return item.effectValue1; // Climbing Claws: +2 Move
                case 87: return item.effectValue2; // Root Armor: -1 Move
                default: return 0;
            }
        }

        /// <summary>
        /// Returns the total Carry bonus from all equipped items on a hero.
        /// Tinker ability: utility items grant double effect.
        /// </summary>
        public static int GetCarryBonus(HeroToken hero)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetCarryBonus: ENTER (hero={hero.cardDef.cardName})");
            int total = 0;
            bool hasTinker = hero.cardDef.specialAbility == SpecialAbility.Tinker;

            foreach (var item in hero.equippedItems)
            {
                int bonus = GetCarryBonusForItem(item);
                if (bonus > 0 && hasTinker && item.equipmentSlot == EquipmentSlot.Utility)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetCarryBonus: Tinker doubles utility carry bonus: {bonus} -> {bonus * 2} (item={item.cardName})");
                    bonus *= 2;
                }
                total += bonus;
                if (bonus != 0)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetCarryBonus: item={item.cardName}, carryBonus={bonus}");
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetCarryBonus: EXIT (hero={hero.cardDef.cardName}, totalCarryBonus={total})");
            return total;
        }

        private static int GetCarryBonusForItem(CardDefinitionSO item)
        {
            switch (item.cardId)
            {
                case 73: return item.effectValue1; // Cloth Satchel: +2 Carry
                case 75: return item.effectValue1; // Berry Basket: +3 Carry
                default: return 0;
            }
        }

        /// <summary>
        /// Returns the total Initiative bonus from all equipped items on a hero.
        /// </summary>
        public static int GetInitiativeBonus(HeroToken hero)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetInitiativeBonus: ENTER (hero={hero.cardDef.cardName})");
            int total = 0;

            foreach (var item in hero.equippedItems)
            {
                int bonus = GetInitiativeBonusForItem(item);
                total += bonus;
                if (bonus != 0)
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetInitiativeBonus: item={item.cardName}, initBonus={bonus}");
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetInitiativeBonus: EXIT (hero={hero.cardDef.cardName}, totalInitBonus={total})");
            return total;
        }

        private static int GetInitiativeBonusForItem(CardDefinitionSO item)
        {
            switch (item.cardId)
            {
                case 53: return item.effectValue2; // Pin Dagger: +1 Initiative
                case 58: return item.effectValue2; // Bone Knife: +1 Initiative
                case 88: return item.effectValue2; // Moonlight Dagger: +2 Initiative
                default: return 0;
            }
        }

        // ===================================================================
        // Pre-Combat Effects
        // ===================================================================

        /// <summary>
        /// Processes pre-combat equipment effects for a hero (e.g., Flint Firestarter damage).
        /// Called once at the start of combat, before any rounds.
        /// </summary>
        public static void ProcessPreCombatEffects(HeroToken hero, List<EnemyToken> enemies)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPreCombatEffects: ENTER (hero={hero.cardDef.cardName}, enemies={enemies.Count})");

            foreach (var item in hero.equippedItems)
            {
                switch (item.cardId)
                {
                    case 79: // Flint Firestarter: enemies on this node take 1 damage at start of combat
                        int dmg = item.effectValue1; // 1
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPreCombatEffects: Flint Firestarter — dealing {dmg} damage to all {enemies.Count(e => e.IsAlive)} enemies");
                        foreach (var enemy in enemies.Where(e => e.IsAlive))
                        {
                            enemy.TakeDamage(dmg);
                            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPreCombatEffects: Flint Firestarter hit enemy={enemy.definition.enemyName}, hp={enemy.currentHP}");
                        }
                        break;

                    case 83: // Frost Thread Cloak: enemies on node lose 1 Combat (reduce strength)
                        int debuff = item.effectValue2; // 1
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPreCombatEffects: Frost Thread Cloak — enemies lose {debuff} strength");
                        foreach (var enemy in enemies.Where(e => e.IsAlive))
                        {
                            int prev = enemy.strength;
                            enemy.strength = Mathf.Max(0, enemy.strength - debuff);
                            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPreCombatEffects: Frost Thread Cloak — enemy={enemy.definition.enemyName} strength {prev}->{enemy.strength}");
                        }
                        break;
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPreCombatEffects: EXIT (hero={hero.cardDef.cardName})");
        }

        // ===================================================================
        // Per-Round Effects
        // ===================================================================

        /// <summary>
        /// Processes per-round equipment effects for a hero (damage block, Thorn Crown self-damage, etc.).
        /// Called at the start of each combat round.
        /// </summary>
        public static void ProcessPerRoundEffects(HeroToken hero, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPerRoundEffects: ENTER (hero={hero.cardDef.cardName}, round={ctx.roundNumber})");

            foreach (var item in hero.equippedItems)
            {
                switch (item.cardId)
                {
                    case 62: // Button Shield: blocks 1 damage per round
                        ctx.AddDamageBlock(hero.tokenId, item.effectValue2); // effectValue2 = 1
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPerRoundEffects: Button Shield — hero={hero.cardDef.cardName} blocks {item.effectValue2} damage this round");
                        break;

                    case 67: // Beetle Shell Shield: blocks 1 damage per round
                        ctx.AddDamageBlock(hero.tokenId, item.effectValue2); // effectValue2 = 1
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPerRoundEffects: Beetle Shell Shield — hero={hero.cardDef.cardName} blocks {item.effectValue2} damage this round");
                        break;

                    case 60: // Acorn Hammer: stuns target for 1 round
                        // Stun is applied to the lowest-strength enemy that hasn't been stunned yet
                        var unstunnedEnemies = ctx.stunnedEnemies;
                        // We don't have enemy list here; stun is handled by CombatResolver directly
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPerRoundEffects: Acorn Hammer — stun effect (handled by CombatResolver)");
                        break;

                    case 86: // Thorn Crown: +3 Combat but takes 1 self-damage per round
                        int selfDmg = item.effectValue2; // 1
                        hero.TakeDamage(selfDmg);
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPerRoundEffects: Thorn Crown — hero={hero.cardDef.cardName} takes {selfDmg} self-damage, hp={hero.currentHP}");
                        break;

                    case 77: // Healing Herb Kit: heal 1 HP per turn (applied once per combat, first round)
                        if (ctx.roundNumber == 1)
                        {
                            int heal = item.effectValue1; // 1
                            hero.Heal(heal);
                            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPerRoundEffects: Healing Herb Kit — healed hero={hero.cardDef.cardName} for {heal}, hp={hero.currentHP}");
                        }
                        break;
                }
            }

            // Splinter Staff aura: +1 Combat to all friendly tokens on same node
            // (Applied as temp buff to other heroes in round processing — handled by CombatResolver)
            // We check here and add the aura buff
            foreach (var item in hero.equippedItems)
            {
                if (item.cardId == 54) // Splinter Staff
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPerRoundEffects: Splinter Staff aura flagged for hero={hero.cardDef.cardName}");
                    // Aura: the WIELDER's bonus is already in GetCombatBonus; the aura for allies
                    // must be applied by CombatResolver since we don't have the full hero list here
                }

                if (item.cardId == 85) // War Banner Pin: +2 Combat to ALL on node (aura)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPerRoundEffects: War Banner Pin aura flagged for hero={hero.cardDef.cardName}");
                }

                if (item.cardId == 90) // Banner of the Rat Pack: +2 Combat to ALL on map (aura)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPerRoundEffects: Banner of Rat Pack aura flagged for hero={hero.cardDef.cardName}");
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] ProcessPerRoundEffects: EXIT (hero={hero.cardDef.cardName})");
        }

        // ===================================================================
        // Special Ability Queries
        // ===================================================================

        /// <summary>
        /// Returns true if the hero has a ranged strike capability via equipment
        /// (Thorn Spear or Pebble Sling).
        /// Note: The hero's innate RangedStrike ability is checked separately via cardDef.specialAbility.
        /// </summary>
        public static bool HasRangedStrike(HeroToken hero)
        {
            bool result = hero.equippedItems.Any(i =>
                i.cardId == 52 || // Thorn Spear: +1 range (can strike before melee)
                i.cardId == 56    // Pebble Sling: ranged (strikes before melee round)
            );
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] HasRangedStrike: hero={hero.cardDef.cardName}, result={result}");
            return result;
        }

        /// <summary>
        /// Returns true if the hero has splash damage via equipment (Storm Needle).
        /// Note: Cleave ability is checked separately via specialAbility.
        /// </summary>
        public static bool HasSplashDamage(HeroToken hero)
        {
            bool result = hero.equippedItems.Any(i => i.cardId == 81); // Storm Needle
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] HasSplashDamage: hero={hero.cardDef.cardName}, result={result}");
            return result;
        }

        /// <summary>
        /// Returns the splash damage amount from equipment.
        /// </summary>
        public static int GetSplashDamage(HeroToken hero)
        {
            int total = 0;
            foreach (var item in hero.equippedItems)
            {
                if (item.cardId == 81) // Storm Needle: 1 splash damage
                {
                    total += item.effectValue2; // effectValue2 = 1
                }
            }
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetSplashDamage: hero={hero.cardDef.cardName}, splashDamage={total}");
            return total;
        }

        /// <summary>
        /// Returns true if the hero has Taunt (either via ability or equipment).
        /// Enemies must target this hero first.
        /// </summary>
        public static bool IsTauntActive(HeroToken hero)
        {
            bool result = hero.cardDef.specialAbility == SpecialAbility.Taunt;
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] IsTauntActive: hero={hero.cardDef.cardName}, result={result}");
            return result;
        }

        /// <summary>
        /// Returns true if the hero CANNOT be targeted first (Moss Cloak effect).
        /// </summary>
        public static bool CannotBeTargetedFirst(HeroToken hero)
        {
            bool result = hero.equippedItems.Any(i => i.cardId == 64); // Moss Cloak
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] CannotBeTargetedFirst: hero={hero.cardDef.cardName}, result={result}");
            return result;
        }

        /// <summary>
        /// Returns true if the hero has a Splinter Staff (grants +1 Combat aura to allies on same node).
        /// </summary>
        public static bool HasSplinterStaffAura(HeroToken hero)
        {
            return hero.equippedItems.Any(i => i.cardId == 54);
        }

        /// <summary>
        /// Returns true if the hero has War Banner Pin (grants +2 Combat aura to all on node).
        /// </summary>
        public static bool HasWarBannerAura(HeroToken hero)
        {
            return hero.equippedItems.Any(i => i.cardId == 85);
        }

        /// <summary>
        /// Returns true if the hero has Banner of the Rat Pack (grants +2 Combat aura to all on map).
        /// </summary>
        public static bool HasRatPackBannerAura(HeroToken hero)
        {
            return hero.equippedItems.Any(i => i.cardId == 90);
        }

        /// <summary>
        /// Returns true if the hero has Snail Shell Horn (grants +1 Combat to adjacent heroes).
        /// </summary>
        public static bool HasSnailShellHornAura(HeroToken hero)
        {
            return hero.equippedItems.Any(i => i.cardId == 78);
        }

        /// <summary>
        /// Returns the fog reveal bonus from equipment.
        /// </summary>
        public static int GetFogRevealBonus(HeroToken hero)
        {
            int total = 0;
            foreach (var item in hero.equippedItems)
            {
                switch (item.cardId)
                {
                    case 71: total += item.effectValue1; break; // Bead Lantern: +1 fog reveal
                    case 82: total += item.effectValue2; break; // Ember Torch: +2 fog reveal
                }
            }
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EquipmentEffectProcessor] GetFogRevealBonus: hero={hero.cardDef.cardName}, fogBonus={total}");
            return total;
        }
    }
}
