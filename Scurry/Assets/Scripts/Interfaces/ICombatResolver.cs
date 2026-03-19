using System.Collections.Generic;
using Scurry.Map;
using Scurry.Combat;

namespace Scurry.Interfaces
{
    /// <summary>
    /// v2.0 combat resolver interface. Resolves combat at a node between
    /// hero tokens and enemy tokens using pooled strength, with damage
    /// applied to lowest-combat units first.
    /// </summary>
    public interface ICombatResolver
    {
        /// <summary>
        /// Resolves combat at a map node. Runs rounds until one side is eliminated.
        /// Synchronous — used by ML simulator and auto-play.
        /// </summary>
        CombatResult ResolveCombat(List<HeroToken> heroes, List<EnemyToken> enemies, int nodeId, bool isAmbush = false);

        /// <summary>
        /// Stepped API: Runs pre-combat (ambush, FirstStrike, RangedStrike), returns initial context and result.
        /// Call ExecuteOneRound() in a loop, then EndCombat() to finalize.
        /// </summary>
        (CombatContext ctx, CombatResult result) BeginCombat(List<HeroToken> heroes, List<EnemyToken> enemies, int nodeId, bool isAmbush = false);

        /// <summary>
        /// Stepped API: Executes one combat round. Returns RoundResult with round outcome.
        /// </summary>
        RoundResult ExecuteOneRound(List<HeroToken> heroes, List<EnemyToken> enemies, CombatContext ctx, CombatResult result);

        /// <summary>
        /// Stepped API: Finalizes combat — populates survivors, determines winner, fires events.
        /// </summary>
        void EndCombat(List<HeroToken> heroes, List<EnemyToken> enemies, CombatResult result, CombatContext ctx);
    }
}
