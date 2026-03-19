using System.Collections.Generic;

namespace Scurry.Combat
{
    /// <summary>
    /// Result of a single combat round from the stepped CombatResolver API.
    /// </summary>
    [System.Serializable]
    public struct RoundResult
    {
        /// <summary>True if both sides still have alive tokens and combat should continue.</summary>
        public bool combatContinues;

        /// <summary>Total hero strength this round (after buffs, equipment, group bonus).</summary>
        public int heroStrength;

        /// <summary>Total enemy strength this round (after stuns, difficulty scaling).</summary>
        public int enemyStrength;

        /// <summary>Damage dealt to enemies this round (positive = heroes won the round).</summary>
        public int damageToEnemies;

        /// <summary>Damage dealt to heroes this round (positive = enemies won the round).</summary>
        public int damageToHeroes;

        /// <summary>Round number (1-based).</summary>
        public int roundNumber;

        /// <summary>Damage events from this round for UI display.</summary>
        public List<DamageEvent> damageEvents;

        /// <summary>True if retreat was triggered this round.</summary>
        public bool retreatTriggered;

        /// <summary>
        /// A single damage event for UI animation.
        /// </summary>
        [System.Serializable]
        public struct DamageEvent
        {
            public int targetId;
            public int damage;
            public bool isHero;
            public bool isDefeated;
            public string abilityName; // e.g. "FIRST STRIKE", "CLEAVE" — null for normal damage

            public override string ToString()
            {
                return $"DamageEvent(targetId={targetId}, damage={damage}, isHero={isHero}, " +
                       $"isDefeated={isDefeated}, ability={abilityName ?? "none"})";
            }
        }
    }
}
