using System;
using UnityEngine;

namespace Scurry.ML
{
    /// <summary>
    /// Encodes all game decisions as a flat vector of floats (the "genome").
    /// Each gene controls a specific aspect of deck building or in-game strategy.
    /// </summary>
    [Serializable]
    public class DecisionWeights
    {
        public const int GENE_COUNT = 80;

        public float[] genes;

        // ── Gene Index Constants ────────────────────────────────────────

        // Deck Construction (indices 0-19)
        public const int HERO_COUNT = 0;          // 4-16: target hero count
        public const int COLONY_COUNT = 1;         // 4-14: target colony card count
        public const int HERO_COMBAT_W = 2;        // weight for combat when ranking heroes
        public const int HERO_MOVE_W = 3;          // weight for move
        public const int HERO_HP_W = 4;            // weight for hp
        public const int HERO_CARRY_W = 5;         // weight for carry
        public const int HERO_INIT_W = 6;          // weight for initiative
        public const int COLONY_FOOD_W = 7;        // priority for FoodProduction
        public const int COLONY_MUSH_W = 8;        // priority for MushFoodProduction
        public const int COLONY_DOUBLE_W = 9;      // priority for DoubleProduction
        public const int COLONY_DEFENSE_W = 10;    // priority for defense cards
        public const int COLONY_FOG_W = 11;        // priority for fog reveal
        public const int COLONY_COMBAT_W = 12;     // priority for combat buff cards
        public const int COLONY_HEAL_W = 13;       // priority for healing
        public const int COLONY_MOVE_W = 14;       // priority for movement buffs
        public const int EQUIP_OFFENSIVE_W = 15;   // preference for offensive equipment
        public const int EQUIP_DEFENSIVE_W = 16;   // preference for defensive equipment
        public const int EQUIP_UTILITY_W = 17;     // preference for utility equipment
        public const int TACTICAL_W = 18;          // how much to fill with tactical
        public const int PREFERRED_DECK_SIZE = 19; // 10-30 target deck size

        // Colony Phase (indices 20-29)
        public const int COL_FOOD_THRESHOLD = 20;  // food production level before switching to non-food
        public const int COL_FOOD_PRIORITY = 21;   // in-game food card priority
        public const int COL_DEFENSE_PRIORITY = 22; // in-game defense priority
        public const int COL_COMBAT_PRIORITY = 23; // in-game combat buff priority
        public const int COL_FOG_PRIORITY = 24;    // in-game fog priority
        public const int COL_HEAL_PRIORITY = 25;   // in-game heal priority
        public const int COL_MOVE_PRIORITY = 26;   // in-game move priority
        public const int COL_STORAGE_PRIORITY = 27; // food storage priority
        public const int COL_DOUBLE_CARD_PRIORITY = 28; // double colony card priority
        public const int COL_FORWARD_DEPLOY_PRIORITY = 29; // forward deploy priority

        // Deploy Phase (indices 30-44)
        public const int MAX_DEPLOY_PER_TURN = 30; // 1-8: cap new deployments per turn
        public const int FOOD_BUFFER_RATIO = 31;   // 0-1: fraction of food prod to keep as buffer
        public const int DEPLOY_START_TURN = 32;   // 0-5: turn to begin deploying
        public const int HERO_GROUP_SIZE = 33;     // 1-4: heroes per target
        public const int TARGET_ENEMY_W = 34;      // weight for enemy presence on target node
        public const int TARGET_DISTANCE_W = 35;   // weight for distance from colony (negative = closer)
        public const int TARGET_UNVISITED_W = 36;  // weight for unvisited nodes
        public const int TARGET_RESOURCE_W = 37;   // weight for resource value
        public const int TARGET_PIPER_W = 38;      // weight for piper proximity
        public const int GATHERER_UTILITY_W = 39;  // equipment priority for gatherers
        public const int FIGHTER_OFFENSIVE_W = 40;  // equipment priority for fighters
        public const int RETURN_CARRY_THRESHOLD = 41; // 0-1: fraction of carry triggering return
        public const int DEPLOY_GATHERER_RATIO = 42; // 0-1: ratio of gatherers to deploy
        public const int PIPER_RUSH_TURN = 43;     // turn after which all heroes target piper
        public const int MIN_HEROES_FOR_PIPER = 44; // min heroes before targeting piper

        // Hero Movement (indices 45-54)
        public const int MOVE_GATHER_W = 45;       // preference for resource nodes
        public const int MOVE_FIGHT_W = 46;        // preference for enemy nodes
        public const int MOVE_EXPLORE_W = 47;      // preference for unvisited nodes
        public const int MOVE_RETURN_W = 48;       // preference to return to colony
        public const int MOVE_AVOID_ENEMY_W = 49;  // gatherer tendency to avoid enemies
        public const int MOVE_FOOD_NODE_W = 50;    // extra weight for food-containing nodes
        public const int MOVE_BOSS_W = 51;         // preference for boss node targeting
        public const int MOVE_CLUSTER_W = 52;      // tendency for heroes to stick together
        public const int MOVE_PIPER_LATE_W = 53;   // late-game piper targeting weight
        public const int MOVE_COLONY_HEAL_W = 54;  // preference to return when damaged

        // Combat Tactics (indices 55-64)
        public const int HP_RETREAT_THRESHOLD = 55;    // 0-1: fraction of max HP to trigger colony return
        public const int BOSS_MIN_GROUP = 56;          // 2-8: min heroes before engaging a boss
        public const int FIGHT_STRENGTH_RATIO = 57;    // 0-3: min hero/enemy strength ratio to engage
        public const int ZONE_WILD_FIGHT_W = 58;       // extra fight preference in Wilderness
        public const int ZONE_FARM_FIGHT_W = 59;       // extra fight preference in Farmland
        public const int ZONE_TOWN_FIGHT_W = 60;       // extra fight preference in Town
        public const int MOVE_DISTANCE_PENALTY = 61;   // 0-2: per-node distance cost in targeting
        public const int HERO_REGROUP_W = 62;          // preference to move toward other heroes
        public const int EQUIP_FIGHTER_DEF_W = 63;     // fighter's defensive equipment priority
        public const int EQUIP_GATHERER_OFF_W = 64;    // gatherer's offensive equipment priority

        // Timing / Adaptation (indices 65-74)
        public const int EARLY_EXPLORE_BOOST = 65;     // extra explore weight in turns 1-4
        public const int MID_FIGHT_BOOST = 66;         // extra fight weight in turns 5-9
        public const int LATE_PIPER_BOOST = 67;        // extra piper weight in turns 10+
        public const int COLONY_EARLY_FOOD_MULT = 68;  // food card priority multiplier turns 1-3
        public const int COLONY_MID_COMBAT_MULT = 69;  // combat buff priority multiplier turns 4-8
        public const int DEPLOY_RAMP_RATE = 70;        // 0-1: increase deploy cap per turn
        public const int INJURED_REDEPLOY_DELAY = 71;  // 0-3: extra turns before redeploying recovered heroes
        public const int RESOURCE_DEPOSIT_URGENCY = 72; // 0-2: urgency multiplier when carrying resources
        public const int BOSS_APPROACH_TURN = 73;      // 3-12: turn to start targeting zone bosses
        public const int SECOND_WAVE_TURN = 74;        // 3-10: turn to deploy a second burst of heroes

        // Tactical Card Usage (indices 75-79)
        public const int TACTICAL_PLAY_THRESHOLD = 75;      // 0-1: min score threshold to play a tactical card
        public const int TACTICAL_COMBAT_PREF = 76;          // 0-2: preference for combat tactical cards (91-100)
        public const int TACTICAL_SUPPORT_PREF = 77;         // 0-2: preference for support tactical cards (101-110)
        public const int TACTICAL_POWER_PREF = 78;           // 0-2: preference for power tactical cards (111-120)
        public const int TACTICAL_SAVE_FOR_BOSS = 79;        // 0-1: tendency to save tactical cards for boss fights

        // ── Named Accessors ─────────────────────────────────────────────

        public float HeroCount => Mathf.Clamp(genes[HERO_COUNT], 4f, 16f);
        public float ColonyCount => Mathf.Clamp(genes[COLONY_COUNT], 4f, 14f);
        public int PreferredDeckSize => Mathf.Clamp(Mathf.RoundToInt(genes[PREFERRED_DECK_SIZE]), 10, 30);
        public int MaxDeployPerTurn => Mathf.Clamp(Mathf.RoundToInt(genes[MAX_DEPLOY_PER_TURN]), 1, 8);
        public float FoodBufferRatio => Mathf.Clamp01(genes[FOOD_BUFFER_RATIO]);
        public int DeployStartTurn => Mathf.Clamp(Mathf.RoundToInt(genes[DEPLOY_START_TURN]), 0, 5);
        public int HeroGroupSize => Mathf.Clamp(Mathf.RoundToInt(genes[HERO_GROUP_SIZE]), 1, 4);
        public float ReturnCarryThreshold => Mathf.Clamp01(genes[RETURN_CARRY_THRESHOLD]);
        public int PiperRushTurn => Mathf.Clamp(Mathf.RoundToInt(genes[PIPER_RUSH_TURN]), 5, 15);
        public int MinHeroesForPiper => Mathf.Clamp(Mathf.RoundToInt(genes[MIN_HEROES_FOR_PIPER]), 3, 12);
        public float FoodThreshold => Mathf.Clamp(genes[COL_FOOD_THRESHOLD], 2f, 15f);
        public float HpRetreatThreshold => Mathf.Clamp01(genes[HP_RETREAT_THRESHOLD]);
        public int BossMinGroup => Mathf.Clamp(Mathf.RoundToInt(genes[BOSS_MIN_GROUP]), 2, 8);
        public float FightStrengthRatio => Mathf.Clamp(genes[FIGHT_STRENGTH_RATIO], 0f, 3f);
        public float MoveDistancePenalty => Mathf.Clamp(genes[MOVE_DISTANCE_PENALTY], 0f, 2f);
        public int BossApproachTurn => Mathf.Clamp(Mathf.RoundToInt(genes[BOSS_APPROACH_TURN]), 3, 12);
        public int SecondWaveTurn => Mathf.Clamp(Mathf.RoundToInt(genes[SECOND_WAVE_TURN]), 3, 10);

        // ── Construction ────────────────────────────────────────────────

        public DecisionWeights()
        {
            genes = new float[GENE_COUNT];
        }

        public DecisionWeights(float[] source)
        {
            genes = new float[GENE_COUNT];
            Array.Copy(source, genes, Mathf.Min(source.Length, GENE_COUNT));
        }

        /// <summary>
        /// Creates a random individual with sensible ranges.
        /// </summary>
        public static DecisionWeights Random(System.Random rng)
        {
            var w = new DecisionWeights();
            // Deck construction
            w.genes[HERO_COUNT] = rng.Next(4, 17);
            w.genes[COLONY_COUNT] = rng.Next(4, 15);
            w.genes[HERO_COMBAT_W] = (float)(rng.NextDouble() * 2.0);
            w.genes[HERO_MOVE_W] = (float)(rng.NextDouble() * 2.0);
            w.genes[HERO_HP_W] = (float)(rng.NextDouble() * 2.0);
            w.genes[HERO_CARRY_W] = (float)(rng.NextDouble() * 2.0);
            w.genes[HERO_INIT_W] = (float)(rng.NextDouble() * 2.0);
            for (int i = COLONY_FOOD_W; i <= COLONY_MOVE_W; i++)
                w.genes[i] = (float)(rng.NextDouble() * 2.0);
            w.genes[EQUIP_OFFENSIVE_W] = (float)(rng.NextDouble() * 2.0);
            w.genes[EQUIP_DEFENSIVE_W] = (float)(rng.NextDouble() * 2.0);
            w.genes[EQUIP_UTILITY_W] = (float)(rng.NextDouble() * 2.0);
            w.genes[TACTICAL_W] = (float)(rng.NextDouble());
            w.genes[PREFERRED_DECK_SIZE] = rng.Next(15, 31);

            // Colony phase
            w.genes[COL_FOOD_THRESHOLD] = 3f + (float)(rng.NextDouble() * 10.0);
            for (int i = COL_FOOD_PRIORITY; i <= COL_FORWARD_DEPLOY_PRIORITY; i++)
                w.genes[i] = (float)(rng.NextDouble() * 2.0);

            // Deploy phase
            w.genes[MAX_DEPLOY_PER_TURN] = rng.Next(1, 9);
            w.genes[FOOD_BUFFER_RATIO] = (float)(rng.NextDouble());
            w.genes[DEPLOY_START_TURN] = rng.Next(0, 4);
            w.genes[HERO_GROUP_SIZE] = rng.Next(1, 5);
            for (int i = TARGET_ENEMY_W; i <= TARGET_PIPER_W; i++)
                w.genes[i] = (float)(rng.NextDouble() * 2.0);
            w.genes[GATHERER_UTILITY_W] = (float)(rng.NextDouble() * 2.0);
            w.genes[FIGHTER_OFFENSIVE_W] = (float)(rng.NextDouble() * 2.0);
            w.genes[RETURN_CARRY_THRESHOLD] = (float)(rng.NextDouble());
            w.genes[DEPLOY_GATHERER_RATIO] = (float)(rng.NextDouble());
            w.genes[PIPER_RUSH_TURN] = rng.Next(5, 16);
            w.genes[MIN_HEROES_FOR_PIPER] = rng.Next(3, 10);

            // Hero movement
            for (int i = MOVE_GATHER_W; i <= MOVE_COLONY_HEAL_W; i++)
                w.genes[i] = (float)(rng.NextDouble() * 2.0);

            // Combat tactics
            w.genes[HP_RETREAT_THRESHOLD] = 0.1f + (float)(rng.NextDouble() * 0.5);
            w.genes[BOSS_MIN_GROUP] = rng.Next(2, 7);
            w.genes[FIGHT_STRENGTH_RATIO] = 0.3f + (float)(rng.NextDouble() * 1.5);
            w.genes[ZONE_WILD_FIGHT_W] = (float)(rng.NextDouble() * 2.0);
            w.genes[ZONE_FARM_FIGHT_W] = (float)(rng.NextDouble() * 2.0);
            w.genes[ZONE_TOWN_FIGHT_W] = (float)(rng.NextDouble() * 2.0);
            w.genes[MOVE_DISTANCE_PENALTY] = 0.2f + (float)(rng.NextDouble() * 1.0);
            w.genes[HERO_REGROUP_W] = (float)(rng.NextDouble() * 2.0);
            w.genes[EQUIP_FIGHTER_DEF_W] = (float)(rng.NextDouble() * 2.0);
            w.genes[EQUIP_GATHERER_OFF_W] = (float)(rng.NextDouble() * 2.0);

            // Timing / adaptation
            w.genes[EARLY_EXPLORE_BOOST] = (float)(rng.NextDouble() * 3.0);
            w.genes[MID_FIGHT_BOOST] = (float)(rng.NextDouble() * 3.0);
            w.genes[LATE_PIPER_BOOST] = (float)(rng.NextDouble() * 3.0);
            w.genes[COLONY_EARLY_FOOD_MULT] = 1f + (float)(rng.NextDouble() * 2.0);
            w.genes[COLONY_MID_COMBAT_MULT] = 1f + (float)(rng.NextDouble() * 2.0);
            w.genes[DEPLOY_RAMP_RATE] = (float)(rng.NextDouble());
            w.genes[INJURED_REDEPLOY_DELAY] = rng.Next(0, 3);
            w.genes[RESOURCE_DEPOSIT_URGENCY] = (float)(rng.NextDouble() * 2.0);
            w.genes[BOSS_APPROACH_TURN] = rng.Next(3, 10);
            w.genes[SECOND_WAVE_TURN] = rng.Next(3, 8);

            // Tactical card usage
            w.genes[TACTICAL_PLAY_THRESHOLD] = (float)(rng.NextDouble() * 0.5);
            w.genes[TACTICAL_COMBAT_PREF] = (float)(rng.NextDouble() * 2.0);
            w.genes[TACTICAL_SUPPORT_PREF] = (float)(rng.NextDouble() * 2.0);
            w.genes[TACTICAL_POWER_PREF] = (float)(rng.NextDouble() * 2.0);
            w.genes[TACTICAL_SAVE_FOR_BOSS] = (float)(rng.NextDouble());

            return w;
        }

        /// <summary>
        /// Uniform crossover: each gene has 50% chance of coming from either parent.
        /// </summary>
        public static DecisionWeights Crossover(DecisionWeights a, DecisionWeights b, System.Random rng)
        {
            var child = new DecisionWeights();
            for (int i = 0; i < GENE_COUNT; i++)
            {
                child.genes[i] = rng.NextDouble() < 0.5 ? a.genes[i] : b.genes[i];
            }
            return child;
        }

        /// <summary>
        /// Gaussian mutation: each gene has a chance of being perturbed.
        /// </summary>
        public void Mutate(float sigma, float mutationRate, System.Random rng)
        {
            for (int i = 0; i < GENE_COUNT; i++)
            {
                if (rng.NextDouble() < mutationRate)
                {
                    // Box-Muller transform for Gaussian noise
                    double u1 = 1.0 - rng.NextDouble();
                    double u2 = rng.NextDouble();
                    double normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                    genes[i] += (float)(normal * sigma);
                }
            }
        }

        /// <summary>
        /// Deep copy.
        /// </summary>
        public DecisionWeights Clone()
        {
            return new DecisionWeights((float[])genes.Clone());
        }

        /// <summary>
        /// Creates a seed based on the best-known group-of-4 strategy.
        /// Encodes the key tactical insights discovered through prior GA runs:
        /// - Group size 4 for combat coordination bonus (+3/hero)
        /// - 13 heroes, aggressive deployment
        /// - Early boss approach, mid-game Piper rush
        /// - Cautious retreat, aggressive engagement
        /// </summary>
        public static DecisionWeights CreateGroupOf4Seed()
        {
            var w = new DecisionWeights();

            // Deck construction — 13 heroes, 4 colony, 28-card deck
            w.genes[HERO_COUNT] = 13f;
            w.genes[COLONY_COUNT] = 4f;
            w.genes[HERO_COMBAT_W] = 1.8f;   // prioritize combat heroes
            w.genes[HERO_MOVE_W] = 1.2f;
            w.genes[HERO_HP_W] = 1.5f;
            w.genes[HERO_CARRY_W] = 0.5f;
            w.genes[HERO_INIT_W] = 0.8f;
            w.genes[COLONY_FOOD_W] = 1.5f;
            w.genes[COLONY_MUSH_W] = 1.2f;
            w.genes[COLONY_DOUBLE_W] = 0.8f;
            w.genes[COLONY_DEFENSE_W] = 0.5f;
            w.genes[COLONY_FOG_W] = 0.6f;
            w.genes[COLONY_COMBAT_W] = 1.8f;  // combat buffs valuable
            w.genes[COLONY_HEAL_W] = 1.0f;
            w.genes[COLONY_MOVE_W] = 1.0f;
            w.genes[EQUIP_OFFENSIVE_W] = 1.8f;
            w.genes[EQUIP_DEFENSIVE_W] = 1.2f;
            w.genes[EQUIP_UTILITY_W] = 0.8f;
            w.genes[TACTICAL_W] = 0.3f;
            w.genes[PREFERRED_DECK_SIZE] = 26f;

            // Colony phase
            w.genes[COL_FOOD_THRESHOLD] = 8.5f;
            w.genes[COL_FOOD_PRIORITY] = 1.5f;
            w.genes[COL_DEFENSE_PRIORITY] = 0.5f;
            w.genes[COL_COMBAT_PRIORITY] = 1.8f;
            w.genes[COL_FOG_PRIORITY] = 0.6f;
            w.genes[COL_HEAL_PRIORITY] = 1.0f;
            w.genes[COL_MOVE_PRIORITY] = 0.8f;
            w.genes[COL_STORAGE_PRIORITY] = 0.5f;
            w.genes[COL_DOUBLE_CARD_PRIORITY] = 0.8f;
            w.genes[COL_FORWARD_DEPLOY_PRIORITY] = 1.2f;

            // Deploy phase — aggressive, grouped
            w.genes[MAX_DEPLOY_PER_TURN] = 5f;
            w.genes[FOOD_BUFFER_RATIO] = 0f;
            w.genes[DEPLOY_START_TURN] = 0f;
            w.genes[HERO_GROUP_SIZE] = 4f;     // KEY: group of 4
            w.genes[TARGET_ENEMY_W] = 1.5f;
            w.genes[TARGET_DISTANCE_W] = 0.8f;
            w.genes[TARGET_UNVISITED_W] = 1.2f;
            w.genes[TARGET_RESOURCE_W] = 0.5f;
            w.genes[TARGET_PIPER_W] = 2.0f;
            w.genes[GATHERER_UTILITY_W] = 1.0f;
            w.genes[FIGHTER_OFFENSIVE_W] = 1.8f;
            w.genes[RETURN_CARRY_THRESHOLD] = 0.5f;
            w.genes[DEPLOY_GATHERER_RATIO] = 0.2f;
            w.genes[PIPER_RUSH_TURN] = 9f;
            w.genes[MIN_HEROES_FOR_PIPER] = 4f;

            // Hero movement — fight-oriented
            w.genes[MOVE_GATHER_W] = 0.8f;
            w.genes[MOVE_FIGHT_W] = 1.8f;     // seek combat
            w.genes[MOVE_EXPLORE_W] = 1.0f;
            w.genes[MOVE_RETURN_W] = 0.5f;
            w.genes[MOVE_AVOID_ENEMY_W] = 0.3f;
            w.genes[MOVE_FOOD_NODE_W] = 0.5f;
            w.genes[MOVE_BOSS_W] = 2.5f;      // strongly target bosses
            w.genes[MOVE_CLUSTER_W] = 1.5f;
            w.genes[MOVE_PIPER_LATE_W] = 2.0f;
            w.genes[MOVE_COLONY_HEAL_W] = 0.8f;

            // Combat tactics
            w.genes[HP_RETREAT_THRESHOLD] = 0.5f;  // retreat at 50% HP
            w.genes[BOSS_MIN_GROUP] = 3f;          // need 3+ for boss
            w.genes[FIGHT_STRENGTH_RATIO] = 0.5f;  // moderately aggressive
            w.genes[ZONE_WILD_FIGHT_W] = 1.5f;
            w.genes[ZONE_FARM_FIGHT_W] = 1.8f;
            w.genes[ZONE_TOWN_FIGHT_W] = 1.5f;
            w.genes[MOVE_DISTANCE_PENALTY] = 0.5f;
            w.genes[HERO_REGROUP_W] = 2.0f;       // strong regrouping
            w.genes[EQUIP_FIGHTER_DEF_W] = 1.0f;
            w.genes[EQUIP_GATHERER_OFF_W] = 0.5f;

            // Timing / adaptation
            w.genes[EARLY_EXPLORE_BOOST] = 1.5f;
            w.genes[MID_FIGHT_BOOST] = 2.0f;
            w.genes[LATE_PIPER_BOOST] = 2.5f;
            w.genes[COLONY_EARLY_FOOD_MULT] = 2.0f;
            w.genes[COLONY_MID_COMBAT_MULT] = 2.5f;
            w.genes[DEPLOY_RAMP_RATE] = 0.5f;
            w.genes[INJURED_REDEPLOY_DELAY] = 0f;
            w.genes[RESOURCE_DEPOSIT_URGENCY] = 0.8f;
            w.genes[BOSS_APPROACH_TURN] = 4f;      // target bosses early
            w.genes[SECOND_WAVE_TURN] = 5f;

            // Tactical card usage — aggressive play
            w.genes[TACTICAL_PLAY_THRESHOLD] = 0.1f;      // low threshold = play often
            w.genes[TACTICAL_COMBAT_PREF] = 1.5f;         // combat tactics favored
            w.genes[TACTICAL_SUPPORT_PREF] = 0.8f;
            w.genes[TACTICAL_POWER_PREF] = 1.8f;          // power tactics for boss fights
            w.genes[TACTICAL_SAVE_FOR_BOSS] = 0.7f;       // save best cards for bosses

            return w;
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static DecisionWeights FromJson(string json)
        {
            return JsonUtility.FromJson<DecisionWeights>(json);
        }
    }
}
