using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Scurry.ML
{
    /// <summary>
    /// PPO (Proximal Policy Optimization) agent with GAE (Generalized Advantage Estimation).
    /// Actor-critic architecture: shared backbone with separate policy and value heads.
    /// </summary>
    public class RLAgent
    {
        public const int ACTION_DIM = 16;

        public NeuralNetwork Network { get; private set; }

        // Experience buffer for current episode
        private readonly List<Transition> episodeBuffer = new List<Transition>();

        public struct Transition
        {
            public float[] state;
            public float[] action;
            public float[] mean;
            public float logProb;    // log π_old(a|s)
            public float value;      // V(s) from critic
            public float reward;
        }

        public RLAgent(NeuralNetwork network)
        {
            Network = network;
        }

        public RLAgent() : this(new NeuralNetwork(RLStateExtractor.FEATURE_COUNT, 64, 32, ACTION_DIM))
        {
        }

        /// <summary>
        /// Selects an action given state features. Stores transition for training.
        /// </summary>
        public float[] Act(float[] stateFeatures, System.Random rng, bool deterministic = false)
        {
            if (deterministic)
            {
                var (mean, _) = Network.ForwardActorCritic(stateFeatures);
                return mean;
            }

            var (action, mean2, logProb, value) = Network.SampleActionPPO(stateFeatures, rng);

            episodeBuffer.Add(new Transition
            {
                state = (float[])stateFeatures.Clone(),
                action = (float[])action.Clone(),
                mean = (float[])mean2.Clone(),
                logProb = logProb,
                value = value,
                reward = 0f
            });

            return action;
        }

        /// <summary>
        /// Sets reward for the most recent transition.
        /// </summary>
        public void SetReward(float reward)
        {
            if (episodeBuffer.Count == 0) return;
            var t = episodeBuffer[episodeBuffer.Count - 1];
            t.reward = reward;
            episodeBuffer[episodeBuffer.Count - 1] = t;
        }

        /// <summary>
        /// Returns episode buffer and clears it.
        /// </summary>
        public List<Transition> EndEpisode()
        {
            var transitions = new List<Transition>(episodeBuffer);
            episodeBuffer.Clear();
            return transitions;
        }

        /// <summary>
        /// Computes GAE (Generalized Advantage Estimation) for an episode.
        /// Returns (advantages, value_targets) arrays.
        /// </summary>
        public static (float[] advantages, float[] valueTargets) ComputeGAE(
            List<Transition> episode, float gamma, float lambda)
        {
            int T = episode.Count;
            float[] advantages = new float[T];
            float[] valueTargets = new float[T];

            float gae = 0f;
            float nextValue = 0f; // terminal state value = 0

            for (int t = T - 1; t >= 0; t--)
            {
                float delta = episode[t].reward + gamma * nextValue - episode[t].value;
                gae = delta + gamma * lambda * gae;
                advantages[t] = gae;
                valueTargets[t] = gae + episode[t].value; // advantage + V(s) = return
                nextValue = episode[t].value;
            }

            return (advantages, valueTargets);
        }

        /// <summary>
        /// Normalizes advantages to zero mean, unit variance (per batch).
        /// </summary>
        public static void NormalizeAdvantages(float[] advantages)
        {
            if (advantages.Length <= 1) return;

            float mean = 0f;
            for (int i = 0; i < advantages.Length; i++)
                mean += advantages[i];
            mean /= advantages.Length;

            float variance = 0f;
            for (int i = 0; i < advantages.Length; i++)
            {
                float diff = advantages[i] - mean;
                variance += diff * diff;
            }
            variance /= advantages.Length;
            float std = (float)Math.Sqrt(variance + 1e-8);

            for (int i = 0; i < advantages.Length; i++)
                advantages[i] = (advantages[i] - mean) / std;
        }

        /// <summary>
        /// Runs PPO update on a batch of episodes.
        /// Multiple epochs over the same data with clipped objective.
        /// </summary>
        public void UpdatePPO(List<List<Transition>> episodeBatch,
            float learningRate, float gamma = 0.99f, float lambda = 0.95f,
            float clipEpsilon = 0.2f, float valueLossCoef = 0.5f, float entropyCoef = 0.01f,
            int epochs = 4)
        {
            // ── Flatten all transitions and compute GAE ────────────────
            var allStates = new List<float[]>();
            var allActions = new List<float[]>();
            var allOldLogProbs = new List<float>();
            var allAdvantages = new List<float>();
            var allValueTargets = new List<float>();

            foreach (var episode in episodeBatch)
            {
                if (episode.Count == 0) continue;

                var (advantages, valueTargets) = ComputeGAE(episode, gamma, lambda);

                for (int t = 0; t < episode.Count; t++)
                {
                    allStates.Add(episode[t].state);
                    allActions.Add(episode[t].action);
                    allOldLogProbs.Add(episode[t].logProb);
                    allAdvantages.Add(advantages[t]);
                    allValueTargets.Add(valueTargets[t]);
                }
            }

            if (allStates.Count == 0) return;

            // Normalize advantages across the entire batch
            float[] advArray = allAdvantages.ToArray();
            NormalizeAdvantages(advArray);

            int totalSamples = allStates.Count;

            // ── Multiple epochs of gradient updates ────────────────────
            for (int epoch = 0; epoch < epochs; epoch++)
            {
                Network.ZeroGradients();

                for (int i = 0; i < totalSamples; i++)
                {
                    Network.AccumulatePPOGradient(
                        allStates[i],
                        allActions[i],
                        allOldLogProbs[i],
                        advArray[i],
                        allValueTargets[i],
                        clipEpsilon,
                        valueLossCoef,
                        entropyCoef);
                }

                Network.ApplyGradients(learningRate, totalSamples);
            }
        }

        /// <summary>
        /// Legacy REINFORCE update (kept for compatibility).
        /// </summary>
        public void UpdatePolicy(List<List<Transition>> episodeBatch, float learningRate, float gamma = 0.99f)
        {
            UpdatePPO(episodeBatch, learningRate, gamma, 0.95f, 1000f, 0f, 0f, 1);
        }

        public float Baseline => 0f; // PPO uses value function instead of running baseline

        /// <summary>
        /// Maps raw network outputs to DecisionWeights overrides.
        /// </summary>
        public static DecisionWeights MapActionToWeights(float[] action, DecisionWeights baseWeights)
        {
            var w = baseWeights.Clone();

            w.genes[DecisionWeights.MOVE_FIGHT_W] = action[0] * 2f;
            w.genes[DecisionWeights.MOVE_EXPLORE_W] = action[1] * 2f;
            w.genes[DecisionWeights.MOVE_GATHER_W] = action[2] * 2f;
            w.genes[DecisionWeights.MOVE_BOSS_W] = action[3] * 3f;
            w.genes[DecisionWeights.MOVE_PIPER_LATE_W] = action[4] * 3f;
            w.genes[DecisionWeights.MAX_DEPLOY_PER_TURN] = Mathf.Clamp(action[5] * 4f + 4f, 1f, 8f);
            w.genes[DecisionWeights.HERO_GROUP_SIZE] = Mathf.Clamp(action[6] * 2f + 2f, 1f, 4f);
            w.genes[DecisionWeights.HP_RETREAT_THRESHOLD] = Sigmoid(action[7]);
            w.genes[DecisionWeights.HERO_REGROUP_W] = action[8] * 2f;
            w.genes[DecisionWeights.COL_FOOD_PRIORITY] = action[9] * 3f;
            w.genes[DecisionWeights.COL_COMBAT_PRIORITY] = action[10] * 3f;
            w.genes[DecisionWeights.COL_DEFENSE_PRIORITY] = action[11] * 3f;
            w.genes[DecisionWeights.MOVE_DISTANCE_PENALTY] = Mathf.Clamp(action[12], 0f, 2f);
            w.genes[DecisionWeights.RETURN_CARRY_THRESHOLD] = Sigmoid(action[13]);
            w.genes[DecisionWeights.FIGHT_STRENGTH_RATIO] = Mathf.Clamp(action[14] + 0.5f, 0f, 3f);
            w.genes[DecisionWeights.TACTICAL_COMBAT_PREF] = Mathf.Clamp(action[15] * 2f, 0f, 3f);

            return w;
        }

        private static float Sigmoid(float x)
        {
            return 1f / (1f + (float)Math.Exp(-x));
        }
    }
}
