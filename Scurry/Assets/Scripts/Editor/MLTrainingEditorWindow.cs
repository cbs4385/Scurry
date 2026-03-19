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
/// Editor window that runs ML training without entering play mode.
/// All simulation runs on a background thread — Unity Editor stays fully responsive.
/// Access via menu: Scurry > ML Training
/// </summary>
public class MLTrainingEditorWindow : EditorWindow
{
    // Training parameters
    private int populationSize = 150;
    private int generations = 200;
    private int gamesPerEvaluation = 10;
    private int randomSeed = 42;
    private float mutationRate = 0.15f;
    private float mutationSigma = 0.3f;
    private float eliteRatio = 0.05f;
    private int tournamentSize = 5;

    // Seeding
    private bool seedFromPrevious = true;
    private string previousWeightsPath = "";
    private bool seedGroupOf4 = true;

    // State
    private bool isTraining;
    private CancellationTokenSource cts;
    private string statusMessage = "Ready";
    private int currentGen;
    private float bestFitness;
    private float avgFitness;
    private int totalVictories;
    private float elapsedSeconds;
    private string allTimeBestSummary = "";

    [MenuItem("Scurry/ML Training")]
    public static void ShowWindow()
    {
        GetWindow<MLTrainingEditorWindow>("ML Training");
    }

    [MenuItem("Scurry/ML Training (Auto-Start)")]
    public static void AutoStart()
    {
        var window = GetWindow<MLTrainingEditorWindow>("ML Training");
        if (window.isTraining)
        {
            Debug.Log("[MLTrainingEditor] AutoStart: force-resetting stale training state");
            window.cts?.Cancel();
            window.isTraining = false;
            window.statusMessage = "Ready (reset)";
        }
        window.StartTraining();
    }

    private void OnGUI()
    {
        GUILayout.Label("ML Training (Editor Mode)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Parameters
        GUI.enabled = !isTraining;
        populationSize = EditorGUILayout.IntField("Population Size", populationSize);
        generations = EditorGUILayout.IntField("Generations", generations);
        gamesPerEvaluation = EditorGUILayout.IntField("Games Per Eval", gamesPerEvaluation);
        randomSeed = EditorGUILayout.IntField("Random Seed", randomSeed);
        mutationRate = EditorGUILayout.FloatField("Mutation Rate", mutationRate);
        mutationSigma = EditorGUILayout.FloatField("Mutation Sigma", mutationSigma);
        eliteRatio = EditorGUILayout.FloatField("Elite Ratio", eliteRatio);
        tournamentSize = EditorGUILayout.IntField("Tournament Size", tournamentSize);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Seeding", EditorStyles.boldLabel);
        seedFromPrevious = EditorGUILayout.Toggle("Seed from Previous Best", seedFromPrevious);
        if (seedFromPrevious)
        {
            if (string.IsNullOrEmpty(previousWeightsPath))
                previousWeightsPath = Application.dataPath + "/../TestReports/ml_best_weights.json";

            EditorGUILayout.BeginHorizontal();
            previousWeightsPath = EditorGUILayout.TextField("Weights File", previousWeightsPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFilePanel("Select Weights JSON", Application.dataPath + "/../TestReports", "json");
                if (!string.IsNullOrEmpty(path))
                    previousWeightsPath = path;
            }
            EditorGUILayout.EndHorizontal();

            bool fileExists = File.Exists(previousWeightsPath);
            string seedMsg = fileExists
                ? "Population will be seeded from previous best weights."
                : "Weights file not found. Training will start from scratch.";
            MessageType seedMsgType = fileExists ? MessageType.Info : MessageType.Warning;
            EditorGUILayout.HelpBox(seedMsg, seedMsgType);
        }

        seedGroupOf4 = EditorGUILayout.Toggle("Seed Group-of-4 Strategy", seedGroupOf4);
        if (seedGroupOf4)
            EditorGUILayout.HelpBox("Injects the group-of-4 combat strategy as a seed. " +
                "Overrides file seed if both enabled.", MessageType.Info);

        GUI.enabled = true;

        EditorGUILayout.Space();

        // Controls
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

        // Status
        EditorGUILayout.LabelField("Status", statusMessage);
        EditorGUILayout.LabelField("Generation", $"{currentGen} / {generations}");

        // Progress bar (always show to avoid layout mismatch)
        float progress = isTraining ? (float)currentGen / generations : 0f;
        string progressLabel = isTraining ? $"{currentGen}/{generations} ({progress * 100:F1}%)" : "Idle";
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), progress, progressLabel);

        EditorGUILayout.LabelField("Best Fitness", $"{bestFitness:F0}");
        EditorGUILayout.LabelField("Avg Fitness", $"{avgFitness:F0}");
        EditorGUILayout.LabelField("Victories", $"{totalVictories}");
        EditorGUILayout.LabelField("Elapsed", $"{elapsedSeconds:F1}s");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Best Strategy", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            string.IsNullOrEmpty(allTimeBestSummary) ? "No data yet." : allTimeBestSummary,
            MessageType.Info);

        // Auto-repaint while training
        if (isTraining)
            Repaint();
    }

    private void StartTraining()
    {
        // Load databases on main thread
        var cardDb = CardDatabase.Instance;
        var enemyDb = EnemyDatabase.Instance;
        var mapConfig = Resources.Load<MapConfigSO>("MapConfig");

        if (cardDb == null || enemyDb == null)
        {
            statusMessage = "ERROR: Failed to load databases";
            Debug.LogError("[MLTrainingEditor] Failed to load CardDatabase or EnemyDatabase");
            return;
        }

        if (mapConfig == null)
        {
            mapConfig = ScriptableObject.CreateInstance<MapConfigSO>();
            Debug.LogWarning("[MLTrainingEditor] MapConfig not found, using default");
        }

        Debug.Log($"[MLTrainingEditor] Starting training: pop={populationSize}, gen={generations}, " +
                  $"games={gamesPerEvaluation}, seed={randomSeed}");
        Debug.Log($"[MLTrainingEditor] Databases: cards={cardDb.AllCards.Count}, " +
                  $"colony={cardDb.AllColonyCards.Count}, enemies={enemyDb.AllEnemies.Count}");

        // Create simulator on main thread (caches BalanceConfigSO)
        var simulator = new GameSimulator(
            cardDb.AllCards, cardDb.AllColonyCards, enemyDb.AllEnemies, mapConfig);

        var optimizer = new EvolutionaryOptimizer(
            populationSize, randomSeed, tournamentSize, eliteRatio, mutationRate, mutationSigma);

        // Seed from group-of-4 strategy (takes priority over file seed)
        if (seedGroupOf4)
        {
            var groupSeed = DecisionWeights.CreateGroupOf4Seed();

            // If also seeding from file, merge: use file weights but override group-critical genes
            if (seedFromPrevious && !string.IsNullOrEmpty(previousWeightsPath) && File.Exists(previousWeightsPath))
            {
                try
                {
                    string json = File.ReadAllText(previousWeightsPath);
                    var fileWeights = DecisionWeights.FromJson(json);
                    if (fileWeights != null && fileWeights.genes != null)
                    {
                        // Copy file weights, then override group-critical genes from the group-of-4 seed
                        var merged = new DecisionWeights(new float[DecisionWeights.GENE_COUNT]);
                        System.Array.Copy(fileWeights.genes, merged.genes,
                            System.Math.Min(fileWeights.genes.Length, DecisionWeights.GENE_COUNT));
                        merged.genes[DecisionWeights.HERO_GROUP_SIZE] = groupSeed.genes[DecisionWeights.HERO_GROUP_SIZE];
                        merged.genes[DecisionWeights.HERO_REGROUP_W] = groupSeed.genes[DecisionWeights.HERO_REGROUP_W];
                        merged.genes[DecisionWeights.MOVE_BOSS_W] = groupSeed.genes[DecisionWeights.MOVE_BOSS_W];
                        merged.genes[DecisionWeights.BOSS_MIN_GROUP] = groupSeed.genes[DecisionWeights.BOSS_MIN_GROUP];
                        merged.genes[DecisionWeights.BOSS_APPROACH_TURN] = groupSeed.genes[DecisionWeights.BOSS_APPROACH_TURN];
                        merged.genes[DecisionWeights.MOVE_FIGHT_W] = groupSeed.genes[DecisionWeights.MOVE_FIGHT_W];
                        merged.genes[DecisionWeights.MID_FIGHT_BOOST] = groupSeed.genes[DecisionWeights.MID_FIGHT_BOOST];
                        merged.genes[DecisionWeights.LATE_PIPER_BOOST] = groupSeed.genes[DecisionWeights.LATE_PIPER_BOOST];
                        optimizer.SeedFromWeights(merged);
                        Debug.Log($"[MLTrainingEditor] Seeded from file + group-of-4 overrides");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[MLTrainingEditor] File merge failed, using pure group-of-4 seed: {ex.Message}");
                    optimizer.SeedFromWeights(groupSeed);
                }
            }
            else
            {
                optimizer.SeedFromWeights(groupSeed);
                Debug.Log("[MLTrainingEditor] Seeded population with group-of-4 strategy");
            }
        }
        else if (seedFromPrevious && !string.IsNullOrEmpty(previousWeightsPath) && File.Exists(previousWeightsPath))
        {
            // Original file-only seeding path
            try
            {
                string json = File.ReadAllText(previousWeightsPath);
                var seedWeights = DecisionWeights.FromJson(json);
                if (seedWeights != null && seedWeights.genes != null && seedWeights.genes.Length <= DecisionWeights.GENE_COUNT)
                {
                    optimizer.SeedFromWeights(seedWeights);
                    Debug.Log($"[MLTrainingEditor] Seeded population from: {previousWeightsPath}");
                }
                else
                {
                    Debug.LogWarning("[MLTrainingEditor] Weights file invalid, starting from scratch");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MLTrainingEditor] Failed to load seed weights: {ex.Message}");
            }
        }

        // Set up output
        string outputDir = Application.dataPath + "/../TestReports";
        string csvPath = outputDir + "/ml_training.csv";
        string weightsPath = outputDir + "/ml_best_weights.json";
        string summaryPath = outputDir + "/ml_best_summary.txt";

        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        File.WriteAllText(csvPath,
            "Generation,BestFitness,AvgFitness,WorstFitness,Victories,AllTimeBest," +
            "BestHeroes,BestColony,BestDeckSize,BestMaxDeploy,BestFoodBuffer," +
            "BestDeployStart,BestGroupSize,BestPiperRush,BestFoodThreshold,ElapsedSec\n");

        isTraining = true;
        statusMessage = "Training...";
        currentGen = 0;
        cts = new CancellationTokenSource();
        var token = cts.Token;

        // Capture local refs for the closure
        int localGenerations = generations;
        int localGamesPerEval = gamesPerEvaluation;
        int localRandomSeed = randomSeed;

        // Run entire training on background thread
        Task.Run(() =>
        {
            SimulationFlags.SuppressLogging = true;
            var sw = Stopwatch.StartNew();

            try
            {
                for (int gen = 0; gen < localGenerations; gen++)
                {
                    if (token.IsCancellationRequested) break;

                    var genSw = Stopwatch.StartNew();

                    optimizer.EvaluateGeneration(simulator, localGamesPerEval, localRandomSeed);

                    genSw.Stop();

                    // Compute stats
                    var best = optimizer.BestIndividual;
                    var pop = optimizer.Population;
                    float avg = pop.Average(p => p.fitness);
                    float worst = pop.Min(p => p.fitness);
                    int victories = pop.Sum(p => p.victories);

                    // Update UI state (read by OnGUI on main thread)
                    currentGen = gen + 1;
                    bestFitness = optimizer.AllTimeBest.fitness;
                    avgFitness = avg;
                    totalVictories = victories;
                    elapsedSeconds = (float)sw.Elapsed.TotalSeconds;

                    var w = optimizer.AllTimeBest.weights;
                    allTimeBestSummary = $"Heroes={w.HeroCount:F0}, Colony={w.ColonyCount:F0}, " +
                        $"Deck={w.PreferredDeckSize}, Deploy={w.MaxDeployPerTurn}/turn, " +
                        $"FoodBuffer={w.FoodBufferRatio:F2}, DeployStart=T{w.DeployStartTurn}, " +
                        $"GroupSize={w.HeroGroupSize}, PiperRush=T{w.PiperRushTurn}, " +
                        $"FoodThreshold={w.FoodThreshold:F1}\n" +
                        $"BossMinGrp={w.BossMinGroup}, BossApproach=T{w.BossApproachTurn}, " +
                        $"HpRetreat={w.HpRetreatThreshold:F2}, StrengthRatio={w.FightStrengthRatio:F2}, " +
                        $"2ndWave=T{w.SecondWaveTurn}\n" +
                        $"Fitness={optimizer.AllTimeBest.fitness:F0}, " +
                        $"Victories={optimizer.AllTimeBest.victories}";

                    // Write CSV
                    string csvLine = $"{gen},{best.fitness:F0},{avg:F0},{worst:F0}," +
                        $"{victories},{optimizer.AllTimeBest.fitness:F0}," +
                        $"{w.HeroCount:F0},{w.ColonyCount:F0},{w.PreferredDeckSize}," +
                        $"{w.MaxDeployPerTurn},{w.FoodBufferRatio:F2}," +
                        $"{w.DeployStartTurn},{w.HeroGroupSize},{w.PiperRushTurn}," +
                        $"{w.FoodThreshold:F1},{sw.Elapsed.TotalSeconds:F1}\n";
                    File.AppendAllText(csvPath, csvLine);

                    // Save weights periodically
                    if (gen % 10 == 0 || gen == localGenerations - 1)
                    {
                        var allTimeBest = optimizer.AllTimeBest;
                        File.WriteAllText(weightsPath, allTimeBest.weights.ToJson());

                        var sb = new StringBuilder();
                        sb.AppendLine($"Generation: {gen + 1}");
                        sb.AppendLine($"Fitness: {allTimeBest.fitness:F0}");
                        sb.AppendLine($"Victories: {allTimeBest.victories}");
                        sb.AppendLine($"Weights: heroes={w.HeroCount:F0}, colony={w.ColonyCount:F0}, " +
                            $"deckSize={w.PreferredDeckSize}, maxDeploy={w.MaxDeployPerTurn}, " +
                            $"foodBuffer={w.FoodBufferRatio:F2}, deployStart={w.DeployStartTurn}, " +
                            $"groupSize={w.HeroGroupSize}, piperRush={w.PiperRushTurn}, " +
                            $"foodThreshold={w.FoodThreshold:F1}");
                        sb.AppendLine();
                        sb.AppendLine("All genes:");
                        for (int i = 0; i < DecisionWeights.GENE_COUNT; i++)
                            sb.AppendLine($"  [{i}] = {allTimeBest.weights.genes[i]:F4}");
                        File.WriteAllText(summaryPath, sb.ToString());
                    }

                    optimizer.AdvanceGeneration();
                }
            }
            catch (System.Exception ex)
            {
                statusMessage = $"ERROR: {ex.Message}";
                Debug.LogError($"[MLTrainingEditor] Training failed: {ex}");
            }
            finally
            {
                SimulationFlags.SuppressLogging = false;
                sw.Stop();
                elapsedSeconds = (float)sw.Elapsed.TotalSeconds;
                isTraining = false;
                statusMessage = token.IsCancellationRequested ? "Cancelled" : "Complete";
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
