using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Core;
using Scurry.Gathering;

namespace Scurry.Encounter
{
    public class BossManager : MonoBehaviour
    {
        private BossDefinitionSO bossDef;
        private int bossHP;
        private int bossMaxHP;
        private int bossAttack;
        private int currentPhaseIndex;

        private List<HeroAgent> heroes = new List<HeroAgent>();
        private bool fightActive;
        private int roundNumber;

        // Summoned adds
        private List<SummonedAdd> summonedAdds = new List<SummonedAdd>();

        private class SummonedAdd
        {
            public string name;
            public int attack;
            public int hp;
            public bool alive => hp > 0;
        }

        public int BossHP => bossHP;
        public int BossMaxHP => bossMaxHP;
        public bool IsFightActive => fightActive;

        public void StartBossFight(BossDefinitionSO boss, List<HeroAgent> deployedHeroes)
        {
            bossDef = boss;
            bossHP = boss.maxHP;
            bossMaxHP = boss.maxHP;
            bossAttack = boss.baseAttack;
            currentPhaseIndex = -1;
            heroes = new List<HeroAgent>(deployedHeroes);
            summonedAdds.Clear();
            fightActive = true;
            roundNumber = 0;

            Debug.Log($"[BossManager] StartBossFight: boss='{boss.bossName}', HP={bossHP}/{bossMaxHP}, attack={bossAttack}, heroes={heroes.Count}, phases={boss.phases?.Length ?? 0}");

            EventBus.OnBossHPChanged?.Invoke(bossHP, bossMaxHP);
            CheckPhaseTransition();

            StartCoroutine(RunBossFight());
        }

        private IEnumerator RunBossFight()
        {
            float speedMult = GameSettings.Instance != null ? GameSettings.Instance.BattleWaitMultiplier : 1f;
            yield return new WaitForSeconds(0.5f * speedMult);

            while (fightActive)
            {
                roundNumber++;
                Debug.Log($"[BossManager] RunBossFight: === ROUND {roundNumber} === bossHP={bossHP}, aliveHeroes={CountAliveHeroes()}");

                // Heroes attack phase
                foreach (var hero in heroes)
                {
                    if (hero == null || hero.IsWounded) continue;

                    int damage = hero.CurrentCombat;
                    bossHP -= damage;
                    Debug.Log($"[BossManager] RunBossFight: hero '{hero.CardData.cardName}' deals {damage} damage to boss (bossHP={bossHP})");
                    EventBus.OnBossHPChanged?.Invoke(bossHP, bossMaxHP);

                    if (bossHP <= 0)
                    {
                        Debug.Log($"[BossManager] RunBossFight: boss '{bossDef.bossName}' defeated!");
                        OnBossDefeated();
                        yield break;
                    }

                    yield return new WaitForSeconds(0.3f * speedMult);
                }

                // Check phase transition after hero attacks
                CheckPhaseTransition();

                // Boss attack phase — targets highest-combat hero
                HeroAgent target = GetHighestCombatHero();
                if (target != null)
                {
                    Debug.Log($"[BossManager] RunBossFight: boss attacks '{target.CardData.cardName}' for {bossAttack} damage");
                    bool heroSurvives = target.CurrentCombat >= bossAttack;
                    if (!heroSurvives)
                    {
                        target.ApplyWound();
                        Debug.Log($"[BossManager] RunBossFight: hero '{target.CardData.cardName}' wounded by boss attack");
                    }
                    else
                    {
                        Debug.Log($"[BossManager] RunBossFight: hero '{target.CardData.cardName}' withstood boss attack (combat={target.CurrentCombat} >= bossAttack={bossAttack})");
                    }
                }

                // Execute current phase ability
                ExecutePhaseAbility();

                // Summoned adds attack
                for (int i = summonedAdds.Count - 1; i >= 0; i--)
                {
                    var add = summonedAdds[i];
                    if (!add.alive) { summonedAdds.RemoveAt(i); continue; }

                    HeroAgent addTarget = GetLowestCombatHero();
                    if (addTarget != null)
                    {
                        bool survives = addTarget.CurrentCombat >= add.attack;
                        if (!survives)
                        {
                            addTarget.ApplyWound();
                            Debug.Log($"[BossManager] RunBossFight: summoned add '{add.name}' wounded '{addTarget.CardData.cardName}' (attack={add.attack})");
                        }
                        else
                        {
                            // Hero fights back and kills the add
                            add.hp = 0;
                            Debug.Log($"[BossManager] RunBossFight: hero '{addTarget.CardData.cardName}' killed add '{add.name}'");
                        }
                    }
                }

                yield return new WaitForSeconds(0.5f * speedMult);

                // Check if all heroes defeated
                if (CountAliveHeroes() == 0)
                {
                    Debug.Log("[BossManager] RunBossFight: all heroes defeated!");
                    OnAllHeroesDefeated();
                    yield break;
                }
            }
        }

        private void CheckPhaseTransition()
        {
            if (bossDef.phases == null || bossDef.phases.Length == 0) return;

            for (int i = bossDef.phases.Length - 1; i >= 0; i--)
            {
                if (bossHP <= bossDef.phases[i].hpThreshold && i > currentPhaseIndex)
                {
                    currentPhaseIndex = i;
                    string phaseName = bossDef.phases[i].ability.ToString();
                    Debug.Log($"[BossManager] CheckPhaseTransition: entering phase {i + 1} — ability={phaseName}, hpThreshold={bossDef.phases[i].hpThreshold}");
                    EventBus.OnBossPhaseChanged?.Invoke(phaseName);
                    break;
                }
            }
        }

        private void ExecutePhaseAbility()
        {
            if (bossDef.phases == null || currentPhaseIndex < 0 || currentPhaseIndex >= bossDef.phases.Length) return;

            var phase = bossDef.phases[currentPhaseIndex];
            Debug.Log($"[BossManager] ExecutePhaseAbility: phase={currentPhaseIndex + 1}, ability={phase.ability}, value={phase.abilityValue}");

            switch (phase.ability)
            {
                case BossAbility.Swoop:
                    // Stun highest-carry hero for 1 round
                    HeroAgent carryTarget = GetHighestCarryHero();
                    if (carryTarget != null)
                    {
                        Debug.Log($"[BossManager] ExecutePhaseAbility: Swoop — stunning '{carryTarget.CardData.cardName}' (highest carry={carryTarget.CarryCapacity})");
                        carryTarget.ApplyWound(); // Simulates stun as wound for 1 round
                    }
                    break;

                case BossAbility.Talons:
                    // AoE damage to all heroes
                    int aoeDamage = phase.abilityValue;
                    Debug.Log($"[BossManager] ExecutePhaseAbility: Talons — {aoeDamage} AoE damage to all heroes");
                    foreach (var hero in heroes)
                    {
                        if (hero == null || hero.IsWounded) continue;
                        if (hero.CurrentCombat < aoeDamage)
                        {
                            hero.ApplyWound();
                            Debug.Log($"[BossManager] ExecutePhaseAbility: Talons wounded '{hero.CardData.cardName}' (combat={hero.CurrentCombat} < aoe={aoeDamage})");
                        }
                    }
                    break;

                case BossAbility.AoEDamage:
                    int damage = phase.abilityValue;
                    Debug.Log($"[BossManager] ExecutePhaseAbility: AoEDamage — {damage} to all heroes");
                    foreach (var hero in heroes)
                    {
                        if (hero == null || hero.IsWounded) continue;
                        if (hero.CurrentCombat < damage)
                        {
                            hero.ApplyWound();
                            Debug.Log($"[BossManager] ExecutePhaseAbility: AoEDamage wounded '{hero.CardData.cardName}'");
                        }
                    }
                    break;

                case BossAbility.Summon:
                    int addCount = Mathf.Max(1, phase.abilityValue);
                    Debug.Log($"[BossManager] ExecutePhaseAbility: Summon — spawning {addCount} adds");
                    for (int s = 0; s < addCount; s++)
                    {
                        var add = new SummonedAdd
                        {
                            name = $"{bossDef.bossName}'s Minion #{summonedAdds.Count + 1}",
                            attack = Mathf.Max(1, bossAttack / 2),
                            hp = 1
                        };
                        summonedAdds.Add(add);
                        Debug.Log($"[BossManager] ExecutePhaseAbility: spawned '{add.name}' (attack={add.attack}, hp={add.hp})");
                    }
                    break;

                case BossAbility.Stun:
                    HeroAgent stunTarget = GetHighestCombatHero();
                    if (stunTarget != null)
                    {
                        stunTarget.ApplyWound();
                        Debug.Log($"[BossManager] ExecutePhaseAbility: Stun — wounded '{stunTarget.CardData.cardName}'");
                    }
                    break;
            }
        }

        private HeroAgent GetHighestCombatHero()
        {
            HeroAgent best = null;
            int bestCombat = -1;
            foreach (var hero in heroes)
            {
                if (hero == null || hero.IsWounded) continue;
                if (hero.CurrentCombat > bestCombat)
                {
                    bestCombat = hero.CurrentCombat;
                    best = hero;
                }
            }
            return best;
        }

        private HeroAgent GetHighestCarryHero()
        {
            HeroAgent best = null;
            int bestCarry = -1;
            foreach (var hero in heroes)
            {
                if (hero == null || hero.IsWounded) continue;
                if (hero.CarryCapacity > bestCarry)
                {
                    bestCarry = hero.CarryCapacity;
                    best = hero;
                }
            }
            return best;
        }

        private HeroAgent GetLowestCombatHero()
        {
            HeroAgent best = null;
            int bestCombat = int.MaxValue;
            foreach (var hero in heroes)
            {
                if (hero == null || hero.IsWounded) continue;
                if (hero.CurrentCombat < bestCombat)
                {
                    bestCombat = hero.CurrentCombat;
                    best = hero;
                }
            }
            return best;
        }

        private int CountAliveHeroes()
        {
            int count = 0;
            foreach (var hero in heroes)
            {
                if (hero != null && !hero.IsWounded) count++;
            }
            return count;
        }

        private void OnBossDefeated()
        {
            fightActive = false;
            Debug.Log($"[BossManager] OnBossDefeated: '{bossDef.bossName}' defeated in {roundNumber} rounds");

            // Check for perfect boss kill (no heroes wounded)
            bool perfectKill = true;
            foreach (var hero in heroes)
            {
                if (hero == null || hero.IsWounded)
                {
                    perfectKill = false;
                    break;
                }
            }
            if (perfectKill)
            {
                Debug.Log("[BossManager] OnBossDefeated: PERFECT BOSS KILL — no heroes wounded");
                var achievementMgr = Core.AchievementManager.Instance;
                if (achievementMgr != null)
                    achievementMgr.OnPerfectBossKill();
            }

            // Track boss defeat for achievements
            var achMgr = Core.AchievementManager.Instance;
            if (achMgr != null)
                achMgr.OnBossDefeatedByName(bossDef.bossName);

            EventBus.OnBossDefeated?.Invoke();
        }

        private void OnAllHeroesDefeated()
        {
            fightActive = false;
            int remainingBossHP = Mathf.Max(0, bossHP);
            Debug.Log($"[BossManager] OnAllHeroesDefeated: all heroes fell against '{bossDef.bossName}', remaining boss HP={remainingBossHP}");

            // Build result — boss fight failure
            var result = new EncounterResult
            {
                success = false,
                recalled = false,
                resourcesGathered = new Dictionary<ResourceType, int>(),
                woundedHeroes = new List<CardDefinitionSO>(),
                exhaustedHeroes = new List<CardDefinitionSO>()
            };

            // All heroes are wounded/exhausted
            foreach (var hero in heroes)
            {
                if (hero != null)
                    result.exhaustedHeroes.Add(hero.CardData);
            }

            // Apply boss failure colony damage
            var bc = Data.BalanceConfigSO.Instance;
            int failDmg = bc != null ? bc.bossFailureDamage : 10;
            Debug.Log($"[BossManager] OnAllHeroesDefeated: applying {failDmg} colony damage for boss failure");
            EventBus.OnColonyHPChanged?.Invoke(-failDmg, 0);

            EventBus.OnEncounterComplete?.Invoke(result);
        }

        public List<CardDefinitionSO> GetRewardCards()
        {
            if (bossDef == null || bossDef.rewardCards == null) return new List<CardDefinitionSO>();
            return new List<CardDefinitionSO>(bossDef.rewardCards);
        }

        public RelicDefinitionSO GetRewardRelic()
        {
            return bossDef?.rewardRelic;
        }
    }
}
