using UnityEngine;

namespace Scurry.Core
{
    public static class ScoreCalculator
    {
        /// <summary>
        /// Calculates the final score for a completed run.
        /// Factors: turns taken, deck size multiplier, enemies defeated,
        /// resources gathered, colony cards played, heroes never injured.
        /// </summary>
        public static ScoreBreakdown CalculateScore(ScoreInput input)
        {
            Debug.Log($"[ScoreCalculator] CalculateScore: calculating score " +
                      $"(turnsUsed={input.turnsUsed}, deckSize={input.deckSize}, " +
                      $"enemiesDefeated={input.enemiesDefeated}, zoneBossesDefeated={input.zoneBossesDefeated}, " +
                      $"piedPiperDefeated={input.piedPiperDefeated}, totalResourcesGathered={input.totalResourcesGathered}, " +
                      $"colonyCardsPlayed={input.colonyCardsPlayed}, heroesNeverInjured={input.heroesNeverInjured})");

            var breakdown = new ScoreBreakdown();

            // ── Turn score: fewer turns = higher score. Baseline 30 turns. ──
            int turnsUsed = Mathf.Clamp(input.turnsUsed, 1, 50);
            breakdown.turnScore = Mathf.Max(0, (31 - turnsUsed)) * 100;
            Debug.Log($"[ScoreCalculator] CalculateScore: turn score " +
                      $"(turnsUsed={turnsUsed}, turnScore={breakdown.turnScore})");

            // ── Deck size multiplier ─────────────────────────────────────
            // 30-card=1.0x, 20-card=1.5x, 15-card=2.0x, 10-card=3.0x
            // Linear interpolation between these breakpoints
            breakdown.deckMultiplier = CalculateDeckMultiplier(input.deckSize);
            Debug.Log($"[ScoreCalculator] CalculateScore: deck multiplier " +
                      $"(deckSize={input.deckSize}, multiplier={breakdown.deckMultiplier:F2})");

            // ── Enemy score: +10 per regular, +50 per zone boss, +100 for Pied Piper ──
            int regularEnemyScore = input.enemiesDefeated * 10;
            int bossScore = input.zoneBossesDefeated * 50;
            int piedPiperScore = input.piedPiperDefeated ? 100 : 0;
            breakdown.enemyScore = regularEnemyScore + bossScore + piedPiperScore;
            Debug.Log($"[ScoreCalculator] CalculateScore: enemy score " +
                      $"(regular={regularEnemyScore}, bosses={bossScore}, piedPiper={piedPiperScore}, " +
                      $"total={breakdown.enemyScore})");

            // ── Resource score: +2 per resource gathered ─────────────────
            breakdown.resourceScore = input.totalResourcesGathered * 2;
            Debug.Log($"[ScoreCalculator] CalculateScore: resource score " +
                      $"(totalGathered={input.totalResourcesGathered}, score={breakdown.resourceScore})");

            // ── Colony score: +20 per colony card played ─────────────────
            breakdown.colonyScore = input.colonyCardsPlayed * 20;
            Debug.Log($"[ScoreCalculator] CalculateScore: colony score " +
                      $"(cardsPlayed={input.colonyCardsPlayed}, score={breakdown.colonyScore})");

            // ── Hero bonus: +50 per hero never injured ───────────────────
            breakdown.heroBonus = input.heroesNeverInjured * 50;
            Debug.Log($"[ScoreCalculator] CalculateScore: hero bonus " +
                      $"(heroesNeverInjured={input.heroesNeverInjured}, bonus={breakdown.heroBonus})");

            // ── Final score ──────────────────────────────────────────────
            int preMultiplier = breakdown.turnScore + breakdown.enemyScore +
                                breakdown.resourceScore + breakdown.colonyScore + breakdown.heroBonus;
            breakdown.finalScore = Mathf.RoundToInt(preMultiplier * breakdown.deckMultiplier);

            Debug.Log($"[ScoreCalculator] CalculateScore: final calculation " +
                      $"(preMultiplier={preMultiplier}, deckMultiplier={breakdown.deckMultiplier:F2}, " +
                      $"finalScore={breakdown.finalScore})");

            return breakdown;
        }

        /// <summary>
        /// Calculates deck size multiplier using linear interpolation between breakpoints:
        /// 30-card=1.0x, 20-card=1.5x, 15-card=2.0x, 10-card=3.0x
        /// Clamped: >=30 returns 1.0, <=10 returns 3.0
        /// </summary>
        private static float CalculateDeckMultiplier(int deckSize)
        {
            Debug.Log($"[ScoreCalculator] CalculateDeckMultiplier: (deckSize={deckSize})");

            if (deckSize >= 30)
            {
                Debug.Log($"[ScoreCalculator] CalculateDeckMultiplier: deckSize>=30, returning 1.0");
                return 1.0f;
            }
            if (deckSize <= 10)
            {
                Debug.Log($"[ScoreCalculator] CalculateDeckMultiplier: deckSize<=10, returning 3.0");
                return 3.0f;
            }

            // Piecewise linear interpolation
            float multiplier;

            if (deckSize >= 20)
            {
                // 30->1.0, 20->1.5 — range of 10 cards, 0.5 multiplier range
                float t = (30f - deckSize) / 10f; // 0.0 at 30, 1.0 at 20
                multiplier = 1.0f + t * 0.5f;
                Debug.Log($"[ScoreCalculator] CalculateDeckMultiplier: interpolating 30-20 range " +
                          $"(t={t:F2}, multiplier={multiplier:F2})");
            }
            else if (deckSize >= 15)
            {
                // 20->1.5, 15->2.0 — range of 5 cards, 0.5 multiplier range
                float t = (20f - deckSize) / 5f; // 0.0 at 20, 1.0 at 15
                multiplier = 1.5f + t * 0.5f;
                Debug.Log($"[ScoreCalculator] CalculateDeckMultiplier: interpolating 20-15 range " +
                          $"(t={t:F2}, multiplier={multiplier:F2})");
            }
            else
            {
                // 15->2.0, 10->3.0 — range of 5 cards, 1.0 multiplier range
                float t = (15f - deckSize) / 5f; // 0.0 at 15, 1.0 at 10
                multiplier = 2.0f + t * 1.0f;
                Debug.Log($"[ScoreCalculator] CalculateDeckMultiplier: interpolating 15-10 range " +
                          $"(t={t:F2}, multiplier={multiplier:F2})");
            }

            return multiplier;
        }
    }

    public struct ScoreInput
    {
        public int turnsUsed;
        public int deckSize;
        public int enemiesDefeated;
        public int zoneBossesDefeated;
        public bool piedPiperDefeated;
        public int totalResourcesGathered;
        public int colonyCardsPlayed;
        public int heroesNeverInjured;
    }

    public struct ScoreBreakdown
    {
        public int turnScore;
        public float deckMultiplier;
        public int enemyScore;
        public int resourceScore;
        public int colonyScore;
        public int heroBonus;
        public int finalScore;
    }
}
