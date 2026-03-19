using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scurry.Data;
using Scurry.Core;
using Scurry.Map;
using Scurry.Interfaces;


namespace Scurry.Combat
{
    /// <summary>
    /// v2.0 Combat Resolver. Implements pooled-strength auto-resolving combat per the GDD:
    /// Each round: sum hero combat vs sum enemy strength; weaker side takes damage equal to
    /// the difference, applied to lowest-Combat units first with overflow. Combat repeats
    /// until one side is eliminated.
    /// </summary>
    public class CombatResolver : ICombatResolver
    {
        private const int MaxRounds = 100; // safety cap to prevent infinite loops

        /// <summary>
        /// Resolves a full combat encounter at a single map node.
        /// Synchronous — preserves ML simulator compatibility.
        /// </summary>
        public CombatResult ResolveCombat(List<HeroToken> heroes, List<EnemyToken> enemies, int nodeId, bool isAmbush = false)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ResolveCombat: ENTER (nodeId={nodeId}, heroes={heroes.Count}, enemies={enemies.Count}, isAmbush={isAmbush})");
            if (!SimulationFlags.SuppressLogging) for (int i = 0; i < heroes.Count; i++)
                Debug.Log($"[CombatResolver] ResolveCombat: hero[{i}] = {heroes[i]}");
            if (!SimulationFlags.SuppressLogging) for (int i = 0; i < enemies.Count; i++)
                Debug.Log($"[CombatResolver] ResolveCombat: enemy[{i}] = {enemies[i]}");

            var (ctx, result) = BeginCombat(heroes, enemies, nodeId, isAmbush);

            if (HasBothSides(heroes, enemies))
            {
                bool combatContinues = true;
                while (combatContinues && result.roundsFought < MaxRounds)
                {
                    if (ctx.retreatTriggered)
                    {
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ResolveCombat: retreat triggered, ending combat (nodeId={nodeId})");
                        break;
                    }

                    var roundResult = ExecuteOneRound(heroes, enemies, ctx, result);
                    combatContinues = roundResult.combatContinues;
                }

                if (result.roundsFought >= MaxRounds)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[CombatResolver] ResolveCombat: hit max rounds cap ({MaxRounds}), forcing end (nodeId={nodeId})");
                }
            }

            EndCombat(heroes, enemies, result, ctx);

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ResolveCombat: EXIT — {result}");
            return result;
        }

        /// <summary>
        /// Stepped API: Runs pre-combat phase. Returns context and result for subsequent rounds.
        /// </summary>
        public (CombatContext ctx, CombatResult result) BeginCombat(List<HeroToken> heroes, List<EnemyToken> enemies, int nodeId, bool isAmbush = false)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] BeginCombat: ENTER (nodeId={nodeId}, heroes={heroes.Count}, enemies={enemies.Count}, isAmbush={isAmbush})");

            var result = CombatResult.Create(nodeId);
            var ctx = new CombatContext { nodeId = nodeId, roundNumber = 0, isAmbush = isAmbush };

            // Initialize damage tracking
            foreach (var h in heroes)
                result.damageDealtByHero[h.tokenId] = 0;
            foreach (var e in enemies)
                result.damageDealtByEnemy[e.tokenId] = 0;

            // Fire combat started event
            EventBus.OnCombatStarted?.Invoke(nodeId);

            // --- Pre-combat phase ---
            ProcessPreCombat(heroes, enemies, ctx, result);

            // Remove dead after pre-combat
            RemoveDeadHeroes(heroes, result, nodeId);
            RemoveDeadEnemies(enemies, result);

            if (!HasBothSides(heroes, enemies))
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] BeginCombat: combat ended during pre-combat (nodeId={nodeId})");
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] BeginCombat: EXIT (nodeId={nodeId}, bothSidesAlive={HasBothSides(heroes, enemies)})");
            return (ctx, result);
        }

        /// <summary>
        /// Stepped API: Executes one combat round. Returns RoundResult with round state.
        /// </summary>
        public RoundResult ExecuteOneRound(List<HeroToken> heroes, List<EnemyToken> enemies, CombatContext ctx, CombatResult result)
        {
            ctx.AdvanceRound();
            result.roundsFought++;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ExecuteOneRound: === ROUND {result.roundsFought} START === (nodeId={ctx.nodeId})");

            bool continues = ProcessRound(heroes, enemies, ctx, result);

            // Calculate strengths for UI display
            int heroStr = 0;
            foreach (var h in heroes)
            {
                if (h.IsAlive) heroStr += CalculateEffectiveHeroCombat(h, heroes, ctx);
            }
            int enemyStr = CalculateTotalEnemyStrength(enemies, ctx);

            var roundResult = new RoundResult
            {
                combatContinues = continues && !ctx.retreatTriggered,
                heroStrength = heroStr,
                enemyStrength = enemyStr,
                damageToEnemies = 0,
                damageToHeroes = 0,
                roundNumber = result.roundsFought,
                damageEvents = new List<RoundResult.DamageEvent>(),
                retreatTriggered = ctx.retreatTriggered
            };

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ExecuteOneRound: EXIT (round={result.roundsFought}, continues={roundResult.combatContinues}, retreat={roundResult.retreatTriggered})");
            return roundResult;
        }

        /// <summary>
        /// Stepped API: Finalizes combat — populates survivors, determines winner, fires events.
        /// </summary>
        public void EndCombat(List<HeroToken> heroes, List<EnemyToken> enemies, CombatResult result, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] EndCombat: ENTER (nodeId={result.nodeId})");
            FinalizeCombat(heroes, enemies, result, ctx);
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] EndCombat: EXIT — {result}");
        }

        /// <summary>
        /// Handles FirstStrike, RangedStrike, and equipment pre-combat effects.
        /// </summary>
        private void ProcessPreCombat(List<HeroToken> heroes, List<EnemyToken> enemies, CombatContext ctx, CombatResult result)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessPreCombat: ENTER (nodeId={ctx.nodeId}, heroes={heroes.Count}, enemies={enemies.Count}, isAmbush={ctx.isAmbush})");

            // Ambush: enemies get one free attack round before heroes can react
            if (ctx.isAmbush)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessPreCombat: AMBUSH — enemies get a free attack round");
                int ambushStrength = CalculateTotalEnemyStrength(enemies, ctx);
                if (ambushStrength > 0)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessPreCombat: ambush damage={ambushStrength} to heroes");
                    ApplyDamageToHeroes(ambushStrength, heroes, ctx, result);

                    // Track damage by enemies
                    var aliveEnemies = enemies.Where(e => e.IsAlive).ToList();
                    foreach (var e in aliveEnemies)
                    {
                        int share = ambushStrength > 0 ? (ambushStrength * e.strength / ambushStrength) : 0;
                        if (result.damageDealtByEnemy.ContainsKey(e.tokenId))
                            result.damageDealtByEnemy[e.tokenId] += share;
                    }

                    RemoveDeadHeroes(heroes, result, ctx.nodeId);

                    if (!HasBothSides(heroes, enemies))
                    {
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessPreCombat: combat ended during ambush round");
                        return;
                    }
                }
            }

            // Equipment pre-combat effects (e.g., Flint Firestarter)
            foreach (var hero in heroes)
            {
                EquipmentEffectProcessor.ProcessPreCombatEffects(hero, enemies);
            }

            // Special ability pre-combat
            foreach (var hero in heroes)
            {
                SpecialAbilityProcessor.ProcessPreCombatAbility(hero, heroes, enemies, ctx);
            }

            // FirstStrike: heroes with this ability attack alone in a pre-round
            var firstStrikers = heroes.Where(h => h.IsAlive && h.cardDef.specialAbility == SpecialAbility.FirstStrike).ToList();
            if (firstStrikers.Count > 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessPreCombat: processing FirstStrike for {firstStrikers.Count} hero(es)");
                int fsStrength = 0;
                foreach (var fs in firstStrikers)
                {
                    int effectiveCombat = CalculateEffectiveHeroCombat(fs, heroes, ctx);
                    fsStrength += effectiveCombat;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessPreCombat: FirstStrike hero={fs.cardDef.cardName}, effectiveCombat={effectiveCombat}");
                    ctx.firstStrikeUsed.Add(fs.tokenId);
                }

                int enemyStrength = CalculateTotalEnemyStrength(enemies, ctx);
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessPreCombat: FirstStrike round — heroStr={fsStrength}, enemyStr={enemyStrength}");

                if (fsStrength > enemyStrength)
                {
                    int damage = fsStrength - enemyStrength;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessPreCombat: FirstStrike — heroes win, dealing {damage} damage to enemies");
                    ApplyDamageToEnemies(damage, enemies, result);
                    // Track damage by first strikers (split evenly for tracking)
                    int perHero = damage / firstStrikers.Count;
                    int remainder = damage % firstStrikers.Count;
                    for (int i = 0; i < firstStrikers.Count; i++)
                    {
                        int assigned = perHero + (i < remainder ? 1 : 0);
                        result.damageDealtByHero[firstStrikers[i].tokenId] += assigned;
                    }
                }
                else if (enemyStrength > fsStrength)
                {
                    int damage = enemyStrength - fsStrength;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessPreCombat: FirstStrike — enemies win, dealing {damage} damage to first-strikers");
                    ApplyDamageToHeroes(damage, firstStrikers, ctx, result);
                    foreach (var e in enemies.Where(e => e.IsAlive))
                    {
                        int share = damage / enemies.Count(en => en.IsAlive);
                        result.damageDealtByEnemy[e.tokenId] += share;
                    }
                }
                else
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessPreCombat: FirstStrike — tied at {fsStrength}, no damage");
                }

                RemoveDeadEnemies(enemies, result);
                RemoveDeadHeroes(heroes, result, ctx.nodeId);
            }

            // RangedStrike: heroes with this ability or ranged weapons deal damage before melee
            var rangedHeroes = heroes.Where(h => h.IsAlive && (h.cardDef.specialAbility == SpecialAbility.RangedStrike || EquipmentEffectProcessor.HasRangedStrike(h))).ToList();
            if (rangedHeroes.Count > 0 && HasBothSides(heroes, enemies))
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessPreCombat: processing RangedStrike for {rangedHeroes.Count} hero(es)");
                int rangedDamage = 0;
                foreach (var rh in rangedHeroes)
                {
                    int effectiveCombat = CalculateEffectiveHeroCombat(rh, heroes, ctx);
                    rangedDamage += effectiveCombat;
                    ctx.rangedStrikeUsed.Add(rh.tokenId);
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessPreCombat: RangedStrike hero={rh.cardDef.cardName}, effectiveCombat={effectiveCombat}");
                }

                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessPreCombat: RangedStrike dealing {rangedDamage} to enemies (no enemy retaliation)");
                ApplyDamageToEnemies(rangedDamage, enemies, result);
                foreach (var rh in rangedHeroes)
                {
                    int share = rangedDamage / rangedHeroes.Count;
                    result.damageDealtByHero[rh.tokenId] += share;
                }

                RemoveDeadEnemies(enemies, result);
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessPreCombat: EXIT (aliveHeroes={heroes.Count(h => h.IsAlive)}, aliveEnemies={enemies.Count(e => e.IsAlive)})");
        }

        /// <summary>
        /// Processes a single combat round. Returns true if combat should continue.
        /// </summary>
        private bool ProcessRound(List<HeroToken> heroes, List<EnemyToken> enemies, CombatContext ctx, CombatResult result)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessRound: ENTER (round={ctx.roundNumber}, nodeId={ctx.nodeId})");

            // Per-round ability and equipment effects
            foreach (var hero in heroes.Where(h => h.IsAlive))
            {
                SpecialAbilityProcessor.ProcessPerRoundAbility(hero, heroes, enemies, ctx);
                EquipmentEffectProcessor.ProcessPerRoundEffects(hero, ctx);
            }

            // Calculate effective combat for all heroes
            int heroStrength = 0;
            var aliveHeroes = heroes.Where(h => h.IsAlive).ToList();
            var aliveEnemies = enemies.Where(e => e.IsAlive).ToList();

            foreach (var hero in aliveHeroes)
            {
                int effective = CalculateEffectiveHeroCombat(hero, heroes, ctx);
                hero.EffectiveCombat = effective;

                // Double attack: hero's combat counted twice
                if (ctx.doubleAttackHeroes.Contains(hero.tokenId))
                {
                    heroStrength += effective * 2;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessRound: hero={hero.cardDef.cardName}, effectiveCombat={effective} x2 (double attack)");
                }
                else
                {
                    heroStrength += effective;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessRound: hero={hero.cardDef.cardName}, effectiveCombat={effective}");
                }
            }

            int enemyStrength = CalculateTotalEnemyStrength(aliveEnemies, ctx);

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessRound: round={ctx.roundNumber}, heroStrength={heroStrength}, enemyStrength={enemyStrength}");

            // Fire round event
            EventBus.OnCombatRound?.Invoke(ctx.nodeId, ctx.roundNumber, heroStrength, enemyStrength);

            if (heroStrength == enemyStrength)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessRound: TIE at {heroStrength} — no damage this round");
                // Check for Cleave splash damage even on ties
                AppleCleaveAndSplash(heroes, enemies, result);
                // Thorn Crown self-damage is handled in ProcessPerRoundEffects
                return HasBothSides(heroes, enemies);
            }

            if (heroStrength > enemyStrength)
            {
                int damage = heroStrength - enemyStrength;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessRound: HEROES WIN round — dealing {damage} damage to enemies");

                // Double damage heroes modify the total
                // (already counted in heroStrength via double-attack)

                ApplyDamageToEnemies(damage, aliveEnemies, result);

                // Track damage by heroes (proportional)
                foreach (var h in aliveHeroes)
                {
                    int heroContrib = ctx.doubleAttackHeroes.Contains(h.tokenId) ? h.EffectiveCombat * 2 : h.EffectiveCombat;
                    int share = heroStrength > 0 ? (damage * heroContrib / heroStrength) : 0;
                    result.damageDealtByHero[h.tokenId] += share;
                }
            }
            else
            {
                int damage = enemyStrength - heroStrength;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessRound: ENEMIES WIN round — dealing {damage} damage to heroes");

                ApplyDamageToHeroes(damage, aliveHeroes, ctx, result);

                // Track damage by enemies (proportional)
                foreach (var e in aliveEnemies)
                {
                    int share = enemyStrength > 0 ? (damage * e.strength / enemyStrength) : 0;
                    result.damageDealtByEnemy[e.tokenId] += share;
                }
            }

            // Cleave and splash damage (happens regardless of who won)
            AppleCleaveAndSplash(heroes, enemies, result);

            // Riposte: heroes with Riposte deal 1 damage to attackers when hit
            // (handled inside ApplyDamageToHeroes)

            // Remove dead
            RemoveDeadEnemies(enemies, result);
            RemoveDeadHeroes(heroes, result, ctx.nodeId);

            bool continues = HasBothSides(heroes, enemies);
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ProcessRound: EXIT (round={ctx.roundNumber}, continues={continues}, aliveHeroes={heroes.Count(h => h.IsAlive)}, aliveEnemies={enemies.Count(e => e.IsAlive)})");
            return continues;
        }

        /// <summary>
        /// Apply Cleave (hero ability) and splash damage (equipment like Storm Needle).
        /// </summary>
        private void AppleCleaveAndSplash(List<HeroToken> heroes, List<EnemyToken> enemies, CombatResult result)
        {
            foreach (var hero in heroes.Where(h => h.IsAlive))
            {
                // Cleave ability: 1 damage to all enemies
                if (hero.cardDef.specialAbility == SpecialAbility.Cleave)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] AppleCleaveAndSplash: hero={hero.cardDef.cardName} Cleave — 1 damage to each enemy");
                    foreach (var enemy in enemies.Where(e => e.IsAlive))
                    {
                        enemy.TakeDamage(1);
                        result.damageDealtByHero[hero.tokenId] += 1;
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] AppleCleaveAndSplash: Cleave hit enemy={enemy.definition.enemyName}, hp={enemy.currentHP}");
                    }
                }

                // Equipment splash damage (Storm Needle etc.)
                if (EquipmentEffectProcessor.HasSplashDamage(hero))
                {
                    int splashDmg = EquipmentEffectProcessor.GetSplashDamage(hero);
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] AppleCleaveAndSplash: hero={hero.cardDef.cardName} equipment splash={splashDmg}");
                    foreach (var enemy in enemies.Where(e => e.IsAlive))
                    {
                        enemy.TakeDamage(splashDmg);
                        result.damageDealtByHero[hero.tokenId] += splashDmg;
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] AppleCleaveAndSplash: splash hit enemy={enemy.definition.enemyName}, hp={enemy.currentHP}");
                    }
                }
            }
        }

        /// <summary>
        /// Calculates a hero's effective combat value including base, equipment, buffs, and abilities.
        /// </summary>
        private int CalculateEffectiveHeroCombat(HeroToken hero, List<HeroToken> allHeroes, CombatContext ctx)
        {
            int combat = hero.baseCombat;

            // Equipment combat bonus
            int equipBonus = EquipmentEffectProcessor.GetCombatBonus(hero);
            combat += equipBonus;

            // Context buffs (temp + permanent)
            int ctxBuff = ctx.GetTotalCombatBuff(hero.tokenId);
            combat += ctxBuff;

            // Group coordination bonus: multiple heroes fighting together get a per-hero bonus
            int aliveCount = allHeroes.Count(h => h.IsAlive);
            int groupBonus = 0;
            if (aliveCount >= 4) groupBonus = 3;
            else if (aliveCount >= 3) groupBonus = 2;
            else if (aliveCount >= 2) groupBonus = 1;
            combat += groupBonus;

            // Double damage modifier (applied at round level, not here)
            // handled in ProcessRound

            if (combat < 0) combat = 0;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] CalculateEffectiveHeroCombat: hero={hero.cardDef.cardName}, base={hero.baseCombat}, equipBonus={equipBonus}, ctxBuff={ctxBuff}, groupBonus={groupBonus}, effective={combat}");
            return combat;
        }

        /// <summary>
        /// Calculates total enemy strength for a set of enemies.
        /// Applies difficulty strength multiplier from BalanceConfigSO.
        /// </summary>
        private int CalculateTotalEnemyStrength(List<EnemyToken> enemies, CombatContext ctx)
        {
            int total = 0;
            foreach (var e in enemies)
            {
                if (!e.IsAlive) continue;
                if (ctx.stunnedEnemies.Contains(e.tokenId))
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] CalculateTotalEnemyStrength: enemy={e.definition.enemyName} STUNNED, contributing 0");
                    continue;
                }
                total += e.strength;
            }

            // Apply difficulty multiplier
            float multiplier = BalanceConfigSO.Instance != null ? BalanceConfigSO.Instance.GetEnemyStrengthMultiplier() : 1f;
            if (multiplier != 1f)
            {
                int adjusted = Mathf.RoundToInt(total * multiplier);
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] CalculateTotalEnemyStrength: applying difficulty multiplier={multiplier:F2} (raw={total}, adjusted={adjusted})");
                total = adjusted;
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] CalculateTotalEnemyStrength: total={total}");
            return total;
        }

        private int CalculateTotalEnemyStrength(IEnumerable<EnemyToken> enemies, CombatContext ctx)
        {
            return CalculateTotalEnemyStrength(enemies.ToList(), ctx);
        }

        /// <summary>
        /// Applies damage to enemies sorted by strength ascending (lowest first), with overflow.
        /// </summary>
        private void ApplyDamageToEnemies(int totalDamage, List<EnemyToken> enemies, CombatResult result)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ApplyDamageToEnemies: ENTER (totalDamage={totalDamage}, enemies={enemies.Count(e => e.IsAlive)})");

            var sorted = GetSortedEnemiesByStrength(enemies);
            int remaining = totalDamage;

            foreach (var enemy in sorted)
            {
                if (remaining <= 0) break;
                if (!enemy.IsAlive) continue;

                int dmgToApply = Mathf.Min(remaining, enemy.currentHP);
                enemy.TakeDamage(dmgToApply);
                remaining -= dmgToApply;

                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ApplyDamageToEnemies: enemy={enemy.definition.enemyName}, applied={dmgToApply}, hpNow={enemy.currentHP}, overflow={remaining}");
            }

            if (remaining > 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ApplyDamageToEnemies: {remaining} damage overkill (all enemies dead)");
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ApplyDamageToEnemies: EXIT");
        }

        /// <summary>
        /// Applies damage to heroes sorted by combat ascending (lowest first), with overflow.
        /// Respects Taunt (forced first target), Intercept (redirects damage), Moss Cloak (cannot be targeted first),
        /// and damage block from equipment/tactical cards.
        /// </summary>
        private void ApplyDamageToHeroes(int totalDamage, List<HeroToken> heroes, CombatContext ctx, CombatResult result)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ApplyDamageToHeroes: ENTER (totalDamage={totalDamage}, heroes={heroes.Count(h => h.IsAlive)})");

            var sorted = GetSortedHeroesByCombat(heroes, ctx);
            int remaining = totalDamage;

            foreach (var hero in sorted)
            {
                if (remaining <= 0) break;
                if (!hero.IsAlive) continue;

                // Check for Intercept: a hero with Intercept takes damage instead of this hero
                var interceptor = FindInterceptor(hero, heroes);
                HeroToken target = interceptor ?? hero;

                if (interceptor != null)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ApplyDamageToHeroes: Intercept — {interceptor.cardDef.cardName} takes damage instead of {hero.cardDef.cardName}");
                }

                // Apply damage block
                int block = ctx.GetDamageBlock(target.tokenId);
                int afterBlock = Mathf.Max(0, remaining - block);
                if (block > 0)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ApplyDamageToHeroes: hero={target.cardDef.cardName} blocks {Mathf.Min(block, remaining)} damage (block={block})");
                    remaining = afterBlock;
                    if (remaining <= 0) break;
                }

                int dmgToApply = Mathf.Min(remaining, target.currentHP);
                target.TakeDamage(dmgToApply);
                remaining -= dmgToApply;

                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ApplyDamageToHeroes: hero={target.cardDef.cardName}, applied={dmgToApply}, hpNow={target.currentHP}, overflow={remaining}");

                // Riposte: deal 1 damage back to lowest-HP enemy when hit
                if (target.cardDef.specialAbility == SpecialAbility.Riposte && dmgToApply > 0)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ApplyDamageToHeroes: Riposte triggered by {target.cardDef.cardName}");
                    // Riposte damage is tracked but applied elsewhere (enemies list not available here)
                    // We'll handle it via a flag; the round processor checks for it
                }
            }

            if (remaining > 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ApplyDamageToHeroes: {remaining} damage overkill (all heroes dead)");
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] ApplyDamageToHeroes: EXIT");
        }

        /// <summary>
        /// Returns heroes sorted ascending by EffectiveCombat, then by Initiative (lower first).
        /// Respects Taunt (forced first) and Moss Cloak (cannot be first).
        /// </summary>
        public List<HeroToken> GetSortedHeroesByCombat(List<HeroToken> heroes, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] GetSortedHeroesByCombat: ENTER (count={heroes.Count(h => h.IsAlive)})");

            var alive = heroes.Where(h => h.IsAlive).ToList();

            // Separate Taunt heroes (must be targeted first)
            var tauntHeroes = alive.Where(h => h.cardDef.specialAbility == SpecialAbility.Taunt).ToList();
            var nonTauntHeroes = alive.Where(h => h.cardDef.specialAbility != SpecialAbility.Taunt).ToList();

            // Among non-taunt: filter out Moss Cloak heroes (cannot be targeted first, put at end)
            var mossCloak = nonTauntHeroes.Where(h => EquipmentEffectProcessor.CannotBeTargetedFirst(h)).ToList();
            var normal = nonTauntHeroes.Where(h => !EquipmentEffectProcessor.CannotBeTargetedFirst(h)).ToList();

            // Sort each group by combat asc, then initiative asc
            tauntHeroes.Sort(CompareHeroesByTargetPriority);
            normal.Sort(CompareHeroesByTargetPriority);
            mossCloak.Sort(CompareHeroesByTargetPriority);

            // Final order: Taunt first, then normal lowest-combat, then Moss Cloak
            var result = new List<HeroToken>();
            result.AddRange(tauntHeroes);
            result.AddRange(normal);
            result.AddRange(mossCloak);

            if (!SimulationFlags.SuppressLogging) for (int i = 0; i < result.Count; i++)
                Debug.Log($"[CombatResolver] GetSortedHeroesByCombat: [{i}] {result[i].cardDef.cardName} (combat={result[i].EffectiveCombat}, init={result[i].baseInitiative}, taunt={result[i].cardDef.specialAbility == SpecialAbility.Taunt}, mossCloak={EquipmentEffectProcessor.CannotBeTargetedFirst(result[i])})");

            return result;
        }

        /// <summary>
        /// Returns enemies sorted ascending by strength (lowest first).
        /// </summary>
        public List<EnemyToken> GetSortedEnemiesByStrength(List<EnemyToken> enemies)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] GetSortedEnemiesByStrength: ENTER (count={enemies.Count(e => e.IsAlive)})");

            var sorted = enemies.Where(e => e.IsAlive)
                .OrderBy(e => e.strength)
                .ThenBy(e => e.tokenId)
                .ToList();

            if (!SimulationFlags.SuppressLogging) for (int i = 0; i < sorted.Count; i++)
                Debug.Log($"[CombatResolver] GetSortedEnemiesByStrength: [{i}] {sorted[i].definition.enemyName} (str={sorted[i].strength}, hp={sorted[i].currentHP})");

            return sorted;
        }

        /// <summary>
        /// Compare heroes for damage targeting order: lower combat first, then lower initiative.
        /// </summary>
        private int CompareHeroesByTargetPriority(HeroToken a, HeroToken b)
        {
            int combatComp = a.EffectiveCombat.CompareTo(b.EffectiveCombat);
            if (combatComp != 0) return combatComp;
            return a.baseInitiative.CompareTo(b.baseInitiative);
        }

        /// <summary>
        /// Finds a hero with Intercept ability who can redirect damage from the target.
        /// Returns null if no interceptor available.
        /// </summary>
        private HeroToken FindInterceptor(HeroToken target, List<HeroToken> heroes)
        {
            // Don't intercept for self
            foreach (var h in heroes)
            {
                if (h == target || !h.IsAlive) continue;
                if (h.cardDef.specialAbility == SpecialAbility.Intercept)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] FindInterceptor: {h.cardDef.cardName} can intercept for {target.cardDef.cardName}");
                    return h;
                }
            }
            return null;
        }

        /// <summary>
        /// Removes dead heroes from the alive pool and records them in the result.
        /// Injured heroes: equipment returned, resources dropped on node.
        /// </summary>
        private void RemoveDeadHeroes(List<HeroToken> heroes, CombatResult result, int nodeId)
        {
            for (int i = heroes.Count - 1; i >= 0; i--)
            {
                var hero = heroes[i];
                if (!hero.IsAlive && !result.injuredHeroTokenIds.Contains(hero.tokenId))
                {
                    hero.isInjured = true;
                    hero.isDeployed = false;
                    result.injuredHeroTokenIds.Add(hero.tokenId);

                    // Drop carried resources on node
                    int totalCarried = hero.TotalCarried;
                    if (totalCarried > 0)
                    {
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] RemoveDeadHeroes: hero={hero.cardDef.cardName} dropping {totalCarried} resources at nodeId={nodeId}");
                        foreach (var kvp in hero.carriedResourcesByType)
                        {
                            if (kvp.Value > 0)
                            {
                                EventBus.OnResourceDropped?.Invoke(nodeId, kvp.Key, kvp.Value);
                                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] RemoveDeadHeroes: dropped {kvp.Value} {kvp.Key} at nodeId={nodeId}");
                            }
                        }
                        hero.carriedResourcesByType.Clear();
                    }

                    // Return equipment to deck
                    if (hero.equippedItems.Count > 0)
                    {
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] RemoveDeadHeroes: hero={hero.cardDef.cardName} returning {hero.equippedItems.Count} equipment to deck");
                        hero.equippedItems.Clear();
                    }

                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] RemoveDeadHeroes: hero={hero.cardDef.cardName} (tokenId={hero.tokenId}) marked as injured");
                }
            }
        }

        /// <summary>
        /// Removes dead enemies from the alive pool and records them in the result.
        /// </summary>
        private void RemoveDeadEnemies(List<EnemyToken> enemies, CombatResult result)
        {
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive && !result.defeatedEnemyTokenIds.Contains(enemy.tokenId))
                {
                    enemy.isDefeated = true;
                    result.defeatedEnemyTokenIds.Add(enemy.tokenId);
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] RemoveDeadEnemies: enemy={enemy.definition.enemyName} (tokenId={enemy.tokenId}) defeated");
                }
            }
        }

        /// <summary>
        /// Returns true if both sides still have alive tokens.
        /// </summary>
        private bool HasBothSides(List<HeroToken> heroes, List<EnemyToken> enemies)
        {
            bool heroesAlive = heroes.Any(h => h.IsAlive);
            bool enemiesAlive = enemies.Any(e => e.IsAlive);
            return heroesAlive && enemiesAlive;
        }

        /// <summary>
        /// Finalizes combat: populates surviving lists, determines winner, fires events, handles post-combat.
        /// </summary>
        private void FinalizeCombat(List<HeroToken> heroes, List<EnemyToken> enemies, CombatResult result, CombatContext ctx)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] FinalizeCombat: ENTER (nodeId={result.nodeId})");

            // Populate surviving lists
            foreach (var h in heroes)
            {
                if (h.IsAlive)
                    result.survivingHeroTokenIds.Add(h.tokenId);
            }
            foreach (var e in enemies)
            {
                if (e.IsAlive)
                    result.survivingEnemyTokenIds.Add(e.tokenId);
            }

            result.heroesWon = result.survivingHeroTokenIds.Count > 0 && result.survivingEnemyTokenIds.Count == 0;

            // If retreat was triggered, heroes survive but didn't "win"
            if (ctx.retreatTriggered)
            {
                result.heroesWon = false;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] FinalizeCombat: retreat was triggered, heroesWon=false");
            }

            // Handle injureAfterCombat (e.g., Final Gambit)
            foreach (int tokenId in ctx.injureAfterCombat)
            {
                var hero = heroes.FirstOrDefault(h => h.tokenId == tokenId);
                if (hero != null && hero.IsAlive)
                {
                    hero.currentHP = 0;
                    hero.isInjured = true;
                    hero.isDeployed = false;
                    result.survivingHeroTokenIds.Remove(tokenId);
                    result.injuredHeroTokenIds.Add(tokenId);
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] FinalizeCombat: hero={hero.cardDef.cardName} injured after combat (Final Gambit etc.)");
                }
            }

            // Post-combat abilities
            foreach (var hero in heroes.Where(h => h.IsAlive))
            {
                SpecialAbilityProcessor.ProcessPostCombatAbility(hero, result);
            }

            // Fire combat ended event (skip during headless simulation)
            if (!SimulationFlags.SuppressLogging)
                EventBus.OnCombatEnded?.Invoke(result.nodeId, result.heroesWon);

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[CombatResolver] FinalizeCombat: EXIT — {result}");
        }
    }
}
