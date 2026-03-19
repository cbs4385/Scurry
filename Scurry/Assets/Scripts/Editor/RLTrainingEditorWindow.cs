using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Scurry.Core;
using Scurry.Data;
using Scurry.ML;
using Debug = UnityEngine.Debug;

/// <summary>
/// Editor window for PPO-based RL training. Runs on a background thread.
/// Access via menu: Scurry > RL Training
/// </summary>
public class RLTrainingEditorWindow : EditorWindow
{
    // Training parameters
    private int totalEpisodes = 100000;
    private int batchSize = 256;
    private float learningRate = 0.0003f;
    private float gamma = 0.99f;
    private float lambda = 0.95f;
    private float clipEpsilon = 0.2f;
    private float valueLossCoef = 0.5f;
    private float entropyCoef = 0.01f;
    private int ppoEpochs = 4;
    private int randomSeed = 42;
    private int evalInterval = 1000;
    private int evalGames = 20;

    // Network architecture
    private int hidden1 = 64;
    private int hidden2 = 32;

    // Base weights (from GA best)
    private bool useGAWeights = true;
    private string gaWeightsPath = "";

    // State
    private bool isTraining;
    private CancellationTokenSource cts;
    private string statusMessage = "Ready";
    private int currentEpisode;
    private float bestEvalFitness;
    private float avgEvalFitness;
    private int evalVictories;
    private float elapsedSeconds;
    private string bestSummary = "";

    [MenuItem("Scurry/RL Training")]
    public static void ShowWindow()
    {
        GetWindow<RLTrainingEditorWindow>("RL Training");
    }

    [MenuItem("Scurry/RL Training (Auto-Start)")]
    public static void AutoStart()
    {
        var window = GetWindow<RLTrainingEditorWindow>("RL Training");
        // Force-reset stale training state (e.g. after domain reload killed the thread)
        if (window.isTraining)
        {
            Debug.Log("[RLTrainingEditor] AutoStart: force-resetting stale training state");
            window.cts?.Cancel();
            window.isTraining = false;
            window.statusMessage = "Ready (reset)";
        }
        window.StartTraining();
    }

    private void OnGUI()
    {
        GUILayout.Label("RL Training (PPO)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GUI.enabled = !isTraining;

        // Training params
        totalEpisodes = EditorGUILayout.IntField("Total Episodes", totalEpisodes);
        batchSize = EditorGUILayout.IntField("Batch Size", batchSize);
        learningRate = EditorGUILayout.FloatField("Learning Rate", learningRate);
        gamma = EditorGUILayout.FloatField("Discount (gamma)", gamma);
        lambda = EditorGUILayout.FloatField("GAE Lambda", lambda);
        clipEpsilon = EditorGUILayout.FloatField("PPO Clip Epsilon", clipEpsilon);
        valueLossCoef = EditorGUILayout.FloatField("Value Loss Coef", valueLossCoef);
        entropyCoef = EditorGUILayout.FloatField("Entropy Coef", entropyCoef);
        ppoEpochs = EditorGUILayout.IntField("PPO Epochs", ppoEpochs);
        randomSeed = EditorGUILayout.IntField("Random Seed", randomSeed);
        evalInterval = EditorGUILayout.IntField("Eval Interval", evalInterval);
        evalGames = EditorGUILayout.IntField("Eval Games", evalGames);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Network", EditorStyles.boldLabel);
        hidden1 = EditorGUILayout.IntField("Hidden Layer 1", hidden1);
        hidden2 = EditorGUILayout.IntField("Hidden Layer 2", hidden2);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Base Weights (GA)", EditorStyles.boldLabel);
        useGAWeights = EditorGUILayout.Toggle("Use GA Best Weights", useGAWeights);
        if (useGAWeights)
        {
            if (string.IsNullOrEmpty(gaWeightsPath))
                gaWeightsPath = Application.dataPath + "/../TestReports/ml_best_weights.json";

            EditorGUILayout.BeginHorizontal();
            gaWeightsPath = EditorGUILayout.TextField("Weights File", gaWeightsPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFilePanel("Select GA Weights", Application.dataPath + "/../TestReports", "json");
                if (!string.IsNullOrEmpty(path)) gaWeightsPath = path;
            }
            EditorGUILayout.EndHorizontal();

            bool exists = File.Exists(gaWeightsPath);
            EditorGUILayout.HelpBox(
                exists ? "Base strategy loaded from GA best." : "File not found. Will use default weights.",
                exists ? MessageType.Info : MessageType.Warning);
        }

        GUI.enabled = true;
        EditorGUILayout.Space();

        if (!isTraining)
        {
            if (GUILayout.Button("Start Training", GUILayout.Height(30)))
                StartTraining();
        }
        else
        {
            if (GUILayout.Button("Stop Training", GUILayout.Height(30)))
                StopTraining();
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Status", statusMessage);
        EditorGUILayout.LabelField("Episode", $"{currentEpisode} / {totalEpisodes}");

        float progress = isTraining ? (float)currentEpisode / totalEpisodes : 0f;
        string progressLabel = isTraining ? $"{currentEpisode}/{totalEpisodes} ({progress * 100:F1}%)" : "Idle";
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), progress, progressLabel);

        EditorGUILayout.LabelField("Best Eval Fitness", $"{bestEvalFitness:F0}");
        EditorGUILayout.LabelField("Avg Eval Fitness", $"{avgEvalFitness:F0}");
        EditorGUILayout.LabelField("Eval Victories", $"{evalVictories}");
        EditorGUILayout.LabelField("Elapsed", $"{elapsedSeconds:F1}s");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Best Strategy", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            string.IsNullOrEmpty(bestSummary) ? "No data yet." : bestSummary,
            MessageType.Info);

        if (isTraining) Repaint();
    }

    private void StartTraining()
    {
        var cardDb = CardDatabase.Instance;
        var enemyDb = EnemyDatabase.Instance;
        var mapConfig = Resources.Load<MapConfigSO>("MapConfig");

        if (cardDb == null || enemyDb == null)
        {
            statusMessage = "ERROR: Failed to load databases";
            Debug.LogError("[RLTrainingEditor] Failed to load CardDatabase or EnemyDatabase");
            return;
        }

        if (mapConfig == null)
        {
            mapConfig = ScriptableObject.CreateInstance<MapConfigSO>();
            Debug.LogWarning("[RLTrainingEditor] MapConfig not found, using default");
        }

        DecisionWeights baseWeights = null;
        if (useGAWeights && !string.IsNullOrEmpty(gaWeightsPath) && File.Exists(gaWeightsPath))
        {
            try
            {
                string json = File.ReadAllText(gaWeightsPath);
                baseWeights = DecisionWeights.FromJson(json);
                Debug.Log($"[RLTrainingEditor] Loaded GA base weights from: {gaWeightsPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RLTrainingEditor] Failed to load GA weights: {ex.Message}");
            }
        }

        if (baseWeights == null || baseWeights.genes == null)
        {
            baseWeights = DecisionWeights.CreateGroupOf4Seed();
            Debug.Log("[RLTrainingEditor] Using group-of-4 seed as base weights (no GA file found)");
        }

        if (baseWeights.genes.Length < DecisionWeights.GENE_COUNT)
        {
            var expanded = new float[DecisionWeights.GENE_COUNT];
            System.Array.Copy(baseWeights.genes, expanded, baseWeights.genes.Length);
            baseWeights = new DecisionWeights(expanded);
        }

        Debug.Log($"[RLTrainingEditor] Starting PPO training: episodes={totalEpisodes}, batch={batchSize}, " +
                  $"lr={learningRate}, gamma={gamma}, lambda={lambda}, clip={clipEpsilon}, " +
                  $"epochs={ppoEpochs}, entropy={entropyCoef}");

        var simulator = new RLGameSimulator(
            cardDb.AllCards, cardDb.AllColonyCards, enemyDb.AllEnemies, mapConfig);

        var network = new NeuralNetwork(RLStateExtractor.FEATURE_COUNT, hidden1, hidden2, RLAgent.ACTION_DIM);
        var agent = new RLAgent(network);

        string outputDir = Application.dataPath + "/../TestReports";
        string csvPath = outputDir + "/rl_training.csv";
        string networkPath = outputDir + "/rl_best_network.json";
        string summaryPath = outputDir + "/rl_best_summary.txt";

        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        File.WriteAllText(csvPath,
            "Episode,EvalBestFitness,EvalAvgFitness,EvalVictories,Entropy,AvgEpReward,ElapsedSec\n");

        isTraining = true;
        statusMessage = "Training...";
        currentEpisode = 0;
        bestEvalFitness = 0;
        cts = new CancellationTokenSource();
        var token = cts.Token;

        // Capture locals for closure
        int localEpisodes = totalEpisodes;
        int localBatch = batchSize;
        float localLR = learningRate;
        float localGamma = gamma;
        float localLambda = lambda;
        float localClip = clipEpsilon;
        float localVLCoef = valueLossCoef;
        float localEntCoef = entropyCoef;
        int localEpochs = ppoEpochs;
        int localSeed = randomSeed;
        int localEvalInterval = evalInterval;
        int localEvalGames = evalGames;
        DecisionWeights localBaseWeights = baseWeights;

        Task.Run(() =>
        {
            SimulationFlags.SuppressLogging = true;
            var sw = Stopwatch.StartNew();
            var rng = new System.Random(localSeed);

            float bestNetworkAvgFitness = 0f;
            NeuralNetwork bestNetwork = agent.Network.Clone();
            float runningEpReward = 0f;
            int epRewardCount = 0;

            try
            {
                // Collect episodes in batches, update after each batch
                var batchEpisodes = new System.Collections.Generic.List<System.Collections.Generic.List<RLAgent.Transition>>();
                float batchRewardSum = 0f;

                for (int ep = 0; ep < localEpisodes; ep++)
                {
                    if (token.IsCancellationRequested) break;

                    // ── Run one episode ─────────────────────────────
                    int gameSeed = rng.Next();
                    var result = simulator.Simulate(agent, localBaseWeights, gameSeed, rng);
                    var transitions = agent.EndEpisode();

                    float epReward = transitions.Sum(t => t.reward);
                    batchEpisodes.Add(transitions);
                    batchRewardSum += epReward;

                    epRewardCount++;
                    runningEpReward += (epReward - runningEpReward) / epRewardCount;

                    // ── PPO update when batch is full ──────────────
                    if (batchEpisodes.Count >= localBatch)
                    {
                        agent.UpdatePPO(batchEpisodes, localLR, localGamma, localLambda,
                            localClip, localVLCoef, localEntCoef, localEpochs);

                        batchEpisodes.Clear();
                        batchRewardSum = 0f;
                    }

                    currentEpisode = ep + 1;
                    elapsedSeconds = (float)sw.Elapsed.TotalSeconds;

                    // ── Evaluation ──────────────────────────────────
                    if ((ep + 1) % localEvalInterval == 0)
                    {
                        float totalFitness = 0f;
                        float maxFit = 0f;
                        int victories = 0;

                        var evalAgent = new RLAgent(agent.Network.Clone());
                        for (int g = 0; g < localEvalGames; g++)
                        {
                            int evalSeed = rng.Next();
                            // Deterministic evaluation (no exploration noise)
                            var evalResult = simulator.Simulate(evalAgent, localBaseWeights, evalSeed, rng);
                            evalAgent.EndEpisode();
                            totalFitness += evalResult.fitness;
                            maxFit = Mathf.Max(maxFit, evalResult.fitness);
                            if (evalResult.victory) victories++;
                        }

                        float avgFit = totalFitness / localEvalGames;
                        avgEvalFitness = avgFit;
                        bestEvalFitness = maxFit;
                        evalVictories = victories;

                        float entropy = agent.Network.Entropy();

                        bestSummary = $"Episode {ep + 1}\n" +
                            $"Eval: best={maxFit:F0}, avg={avgFit:F0}, victories={victories}/{localEvalGames}\n" +
                            $"Entropy={entropy:F2}, AvgEpReward={runningEpReward:F0}";

                        if (avgFit > bestNetworkAvgFitness)
                        {
                            bestNetworkAvgFitness = avgFit;
                            bestNetwork = agent.Network.Clone();
                            File.WriteAllText(networkPath, bestNetwork.ToJson());

                            var sb = new StringBuilder();
                            sb.AppendLine($"Episode: {ep + 1}");
                            sb.AppendLine($"Eval Best Fitness: {maxFit:F0}");
                            sb.AppendLine($"Eval Avg Fitness: {avgFit:F0}");
                            sb.AppendLine($"Eval Victories: {victories}/{localEvalGames}");
                            sb.AppendLine($"Entropy: {entropy:F2}");
                            sb.AppendLine($"Avg Episode Reward: {runningEpReward:F0}");
                            sb.AppendLine($"Network: {RLStateExtractor.FEATURE_COUNT}→{hidden1}→{hidden2}→{RLAgent.ACTION_DIM}");
                            sb.AppendLine($"PPO: batch={localBatch}, lr={localLR}, clip={localClip}, " +
                                $"epochs={localEpochs}, gamma={localGamma}, lambda={localLambda}");
                            File.WriteAllText(summaryPath, sb.ToString());
                        }

                        string csvLine = $"{ep + 1},{maxFit:F0},{avgFit:F0},{victories}," +
                            $"{entropy:F2},{runningEpReward:F1},{sw.Elapsed.TotalSeconds:F1}\n";
                        File.AppendAllText(csvPath, csvLine);

                        Debug.Log($"[RLTrainingEditor] Episode {ep + 1}: eval_avg={avgFit:F0}, " +
                            $"eval_best={maxFit:F0}, victories={victories}, entropy={entropy:F2}, " +
                            $"avgReward={runningEpReward:F0}");
                    }
                }

                // Final PPO update on remaining episodes
                if (batchEpisodes.Count > 0)
                {
                    agent.UpdatePPO(batchEpisodes, localLR, localGamma, localLambda,
                        localClip, localVLCoef, localEntCoef, localEpochs);
                }
            }
            catch (System.Exception ex)
            {
                statusMessage = $"ERROR: {ex.Message}";
                Debug.LogError($"[RLTrainingEditor] Training failed: {ex}");
            }
            finally
            {
                SimulationFlags.SuppressLogging = false;
                sw.Stop();
                elapsedSeconds = (float)sw.Elapsed.TotalSeconds;
                isTraining = false;
                statusMessage = token.IsCancellationRequested ? "Cancelled" : "Complete";

                File.WriteAllText(networkPath, bestNetwork.ToJson());
                Debug.Log($"[RLTrainingEditor] Training complete: {currentEpisode} episodes, " +
                    $"best_eval_avg={bestNetworkAvgFitness:F0}, elapsed={elapsedSeconds:F1}s");
            }
        }, token);
    }

    private void StopTraining()
    {
        cts?.Cancel();
        statusMessage = "Cancelling...";
    }

    private void OnDestroy()
    {
        cts?.Cancel();
    }
}
