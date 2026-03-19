using System;
using UnityEngine;

namespace Scurry.ML
{
    /// <summary>
    /// Actor-Critic feedforward neural network in pure C#.
    /// Shared hidden layers with separate policy (actor) and value (critic) heads.
    /// Supports PPO-style gradient accumulation.
    /// </summary>
    [Serializable]
    public class NeuralNetwork
    {
        public int inputSize;
        public int hidden1Size;
        public int hidden2Size;
        public int outputSize;

        // Shared backbone weights
        public float[] w1, b1; // input → hidden1
        public float[] w2, b2; // hidden1 → hidden2

        // Policy head (actor)
        public float[] w3, b3; // hidden2 → policy output
        public float[] logStd; // learned exploration (per output dim)

        // Value head (critic)
        public float[] wV, bV; // hidden2 → 1 scalar value

        // Cached activations for backprop
        [NonSerialized] private float[] z1, a1, z2, a2;
        [NonSerialized] private float cachedValue;

        // Gradients (all parameters)
        [NonSerialized] private float[] dw1, db1, dw2, db2, dw3, db3, dLogStd, dwV, dbV;

        public NeuralNetwork() { }

        public NeuralNetwork(int input, int h1, int h2, int output)
        {
            inputSize = input;
            hidden1Size = h1;
            hidden2Size = h2;
            outputSize = output;

            w1 = new float[h1 * input];
            b1 = new float[h1];
            w2 = new float[h2 * h1];
            b2 = new float[h2];
            w3 = new float[output * h2];
            b3 = new float[output];
            logStd = new float[output];
            wV = new float[h2]; // hidden2 → 1
            bV = new float[1];

            InitializeXavier();
            AllocateBuffers();
        }

        private void InitializeXavier()
        {
            var rng = new System.Random(42);
            XavierInit(w1, inputSize, hidden1Size, rng);
            XavierInit(w2, hidden1Size, hidden2Size, rng);
            XavierInit(w3, hidden2Size, outputSize, rng);
            XavierInit(wV, hidden2Size, 1, rng);

            for (int i = 0; i < outputSize; i++)
                logStd[i] = -0.5f; // std ≈ 0.6
        }

        private void AllocateBuffers()
        {
            z1 = new float[hidden1Size];
            a1 = new float[hidden1Size];
            z2 = new float[hidden2Size];
            a2 = new float[hidden2Size];

            dw1 = new float[w1.Length];
            db1 = new float[hidden1Size];
            dw2 = new float[w2.Length];
            db2 = new float[hidden2Size];
            dw3 = new float[w3.Length];
            db3 = new float[outputSize];
            dLogStd = new float[outputSize];
            dwV = new float[wV.Length];
            dbV = new float[1];
        }

        /// <summary>
        /// Forward pass through shared backbone. Must be called before PolicyHead/ValueHead.
        /// </summary>
        private void ForwardBackbone(float[] input)
        {
            if (z1 == null) AllocateBuffers();

            for (int j = 0; j < hidden1Size; j++)
            {
                float sum = b1[j];
                int offset = j * inputSize;
                for (int i = 0; i < inputSize; i++)
                    sum += w1[offset + i] * input[i];
                z1[j] = sum;
                a1[j] = (float)Math.Tanh(sum);
            }

            for (int j = 0; j < hidden2Size; j++)
            {
                float sum = b2[j];
                int offset = j * hidden1Size;
                for (int i = 0; i < hidden1Size; i++)
                    sum += w2[offset + i] * a1[i];
                z2[j] = sum;
                a2[j] = (float)Math.Tanh(sum);
            }
        }

        /// <summary>
        /// Forward pass: returns policy mean (deterministic action).
        /// </summary>
        public float[] Forward(float[] input)
        {
            ForwardBackbone(input);

            var output = new float[outputSize];
            for (int j = 0; j < outputSize; j++)
            {
                float sum = b3[j];
                int offset = j * hidden2Size;
                for (int i = 0; i < hidden2Size; i++)
                    sum += w3[offset + i] * a2[i];
                output[j] = sum;
            }
            return output;
        }

        /// <summary>
        /// Forward pass returning both policy mean and state value.
        /// </summary>
        public (float[] policyMean, float value) ForwardActorCritic(float[] input)
        {
            ForwardBackbone(input);

            // Policy head
            var policyMean = new float[outputSize];
            for (int j = 0; j < outputSize; j++)
            {
                float sum = b3[j];
                int offset = j * hidden2Size;
                for (int i = 0; i < hidden2Size; i++)
                    sum += w3[offset + i] * a2[i];
                policyMean[j] = sum;
            }

            // Value head
            float value = bV[0];
            for (int i = 0; i < hidden2Size; i++)
                value += wV[i] * a2[i];
            cachedValue = value;

            return (policyMean, value);
        }

        /// <summary>
        /// Samples action from Gaussian policy and returns all data needed for PPO.
        /// </summary>
        public (float[] action, float[] mean, float logProb, float value) SampleActionPPO(float[] input, System.Random rng)
        {
            var (mean, value) = ForwardActorCritic(input);
            var action = new float[outputSize];

            for (int i = 0; i < outputSize; i++)
            {
                float std = (float)Math.Exp(logStd[i]);
                double u1 = 1.0 - rng.NextDouble();
                double u2 = rng.NextDouble();
                float noise = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
                action[i] = mean[i] + noise * std;
            }

            float logProb = LogProbability(action, mean);
            return (action, mean, logProb, value);
        }

        /// <summary>
        /// Samples an action from the Gaussian policy (legacy compatibility).
        /// </summary>
        public (float[] action, float[] mean) SampleAction(float[] input, System.Random rng)
        {
            var (action, mean, _, _) = SampleActionPPO(input, rng);
            return (action, mean);
        }

        public float LogProbability(float[] action, float[] mean)
        {
            float logProb = 0f;
            for (int i = 0; i < outputSize; i++)
            {
                float std = (float)Math.Exp(logStd[i]);
                float diff = action[i] - mean[i];
                logProb += -0.5f * (diff * diff) / (std * std) - logStd[i] - 0.9189385f;
            }
            return logProb;
        }

        /// <summary>
        /// Computes Gaussian entropy: 0.5 * sum(log(2πe * σ²)) = 0.5 * sum(1 + log(2π) + 2*logStd)
        /// </summary>
        public float Entropy()
        {
            float ent = 0f;
            for (int i = 0; i < outputSize; i++)
                ent += 1f + 1.8378770f + 2f * logStd[i]; // 1 + log(2π) + 2*logStd
            return 0.5f * ent;
        }

        /// <summary>
        /// Accumulates PPO gradient for one transition.
        /// Combines clipped policy gradient + value loss + entropy bonus.
        /// </summary>
        public void AccumulatePPOGradient(float[] input, float[] action, float oldLogProb,
            float advantage, float valueTarget, float clipEpsilon, float valueLossCoef, float entropyCoef)
        {
            if (dw1 == null) AllocateBuffers();

            var (mean, value) = ForwardActorCritic(input);
            float newLogProb = LogProbability(action, mean);

            // ── PPO clipped policy gradient ────────────────────────────
            float ratio = (float)Math.Exp(newLogProb - oldLogProb);
            float clippedRatio = Mathf.Clamp(ratio, 1f - clipEpsilon, 1f + clipEpsilon);
            float surr1 = ratio * advantage;
            float surr2 = clippedRatio * advantage;

            // Use the gradient only if unclipped gives the min (PPO objective)
            float policyScale;
            if (surr1 <= surr2)
                policyScale = advantage; // unclipped — use full gradient
            else
                policyScale = 0f; // clipped — no gradient

            // ∂logπ/∂mean_j
            float[] dPolicyOutput = new float[outputSize];
            for (int j = 0; j < outputSize; j++)
            {
                float std = (float)Math.Exp(logStd[j]);
                float std2 = std * std;
                float diff = action[j] - mean[j];

                // Policy gradient
                dPolicyOutput[j] = policyScale * ratio * diff / std2;

                // logStd gradient: policy + entropy
                float policyLogStdGrad = policyScale * ratio * (diff * diff / std2 - 1f);
                float entropyLogStdGrad = entropyCoef * 1f; // ∂entropy/∂logStd = 1
                dLogStd[j] += policyLogStdGrad + entropyLogStdGrad;
            }

            // ── Value loss gradient: ∂(value - target)²/∂value = 2*(value - target) ──
            float valueGrad = -valueLossCoef * 2f * (value - valueTarget);

            // ── Backprop through policy head (layer 3) ─────────────────
            float[] dA2 = new float[hidden2Size];
            for (int j = 0; j < outputSize; j++)
            {
                int offset = j * hidden2Size;
                for (int i = 0; i < hidden2Size; i++)
                {
                    dw3[offset + i] += dPolicyOutput[j] * a2[i];
                    dA2[i] += dPolicyOutput[j] * w3[offset + i];
                }
                db3[j] += dPolicyOutput[j];
            }

            // ── Backprop through value head ────────────────────────────
            for (int i = 0; i < hidden2Size; i++)
            {
                dwV[i] += valueGrad * a2[i];
                dA2[i] += valueGrad * wV[i]; // value head also contributes to backbone
            }
            dbV[0] += valueGrad;

            // ── Backprop through shared backbone ───────────────────────
            // Layer 2 (tanh)
            float[] dZ2 = new float[hidden2Size];
            for (int i = 0; i < hidden2Size; i++)
                dZ2[i] = dA2[i] * (1f - a2[i] * a2[i]);

            float[] dA1 = new float[hidden1Size];
            for (int j = 0; j < hidden2Size; j++)
            {
                int offset = j * hidden1Size;
                for (int i = 0; i < hidden1Size; i++)
                {
                    dw2[offset + i] += dZ2[j] * a1[i];
                    dA1[i] += dZ2[j] * w2[offset + i];
                }
                db2[j] += dZ2[j];
            }

            // Layer 1 (tanh)
            float[] dZ1 = new float[hidden1Size];
            for (int i = 0; i < hidden1Size; i++)
                dZ1[i] = dA1[i] * (1f - a1[i] * a1[i]);

            for (int j = 0; j < hidden1Size; j++)
            {
                int offset = j * inputSize;
                for (int i = 0; i < inputSize; i++)
                    dw1[offset + i] += dZ1[j] * input[i];
                db1[j] += dZ1[j];
            }
        }

        /// <summary>
        /// Legacy REINFORCE gradient accumulation (kept for compatibility).
        /// </summary>
        public void AccumulateGradient(float[] input, float[] action, float[] mean, float advantage)
        {
            AccumulatePPOGradient(input, action, LogProbability(action, mean),
                advantage, 0f, 1000f, 0f, 0f);
        }

        public void ZeroGradients()
        {
            if (dw1 == null) AllocateBuffers();
            Array.Clear(dw1, 0, dw1.Length);
            Array.Clear(db1, 0, db1.Length);
            Array.Clear(dw2, 0, dw2.Length);
            Array.Clear(db2, 0, db2.Length);
            Array.Clear(dw3, 0, dw3.Length);
            Array.Clear(db3, 0, db3.Length);
            Array.Clear(dLogStd, 0, dLogStd.Length);
            Array.Clear(dwV, 0, dwV.Length);
            Array.Clear(dbV, 0, dbV.Length);
        }

        public void ApplyGradients(float learningRate, int batchSize)
        {
            float scale = learningRate / batchSize;
            ApplyGradArray(w1, dw1, scale);
            ApplyGradArray(b1, db1, scale);
            ApplyGradArray(w2, dw2, scale);
            ApplyGradArray(b2, db2, scale);
            ApplyGradArray(w3, dw3, scale);
            ApplyGradArray(b3, db3, scale);
            ApplyGradArray(logStd, dLogStd, scale);
            ApplyGradArray(wV, dwV, scale);
            ApplyGradArray(bV, dbV, scale);

            for (int i = 0; i < outputSize; i++)
                logStd[i] = Mathf.Clamp(logStd[i], -2f, 0.5f); // std: ~0.14 to ~1.65
        }

        private void ApplyGradArray(float[] param, float[] grad, float scale)
        {
            for (int i = 0; i < param.Length; i++)
                param[i] += grad[i] * scale;
        }

        public NeuralNetwork Clone()
        {
            var clone = new NeuralNetwork
            {
                inputSize = inputSize,
                hidden1Size = hidden1Size,
                hidden2Size = hidden2Size,
                outputSize = outputSize,
                w1 = (float[])w1.Clone(),
                b1 = (float[])b1.Clone(),
                w2 = (float[])w2.Clone(),
                b2 = (float[])b2.Clone(),
                w3 = (float[])w3.Clone(),
                b3 = (float[])b3.Clone(),
                logStd = (float[])logStd.Clone(),
                wV = (float[])wV.Clone(),
                bV = (float[])bV.Clone()
            };
            clone.AllocateBuffers();
            return clone;
        }

        public string ToJson() => JsonUtility.ToJson(this);

        public static NeuralNetwork FromJson(string json)
        {
            var net = JsonUtility.FromJson<NeuralNetwork>(json);
            if (net.wV == null || net.wV.Length == 0)
            {
                net.wV = new float[net.hidden2Size];
                net.bV = new float[1];
            }
            net.AllocateBuffers();
            return net;
        }

        private static void XavierInit(float[] weights, int fanIn, int fanOut, System.Random rng)
        {
            float limit = (float)Math.Sqrt(6.0 / (fanIn + fanOut));
            for (int i = 0; i < weights.Length; i++)
                weights[i] = (float)(rng.NextDouble() * 2.0 * limit - limit);
        }
    }
}
