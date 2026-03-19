using System.Collections;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Scurry.Data;

namespace Scurry.ML
{
    /// <summary>
    /// Log handler that discards normal log messages but preserves exceptions.
    /// Used during ML evaluation to prevent background thread logs from flooding Unity's console.
    /// </summary>
    internal class SilentLogHandler : ILogHandler
    {
        private readonly ILogHandler fallback;
        public SilentLogHandler(ILogHandler fallback) { this.fallback = fallback; }
        public void LogFormat(LogType logType, Object context, string format, params object[] args) { }
        public void LogException(System.Exception exception, Object context)
        {
            fallback?.LogException(exception, context);
        }
    }

    /// <summary>
    /// MonoBehaviour entry point for ML training. Attach to a GameObject in the Bootstrap scene.
    /// Loads card/enemy databases once, then runs evolutionary optimization using the headless simulator.
    /// Outputs progress to TestReports/ml_training.csv and best weights to TestReports/ml_best_weights.json.
    /// Evaluation runs on a background thread to keep Unity responsive.
    /// </summary>
    public class MLTrainingRunner : MonoBehaviour
    {
        [Header("Training Parameters")]
        [SerializeField] private int populationSize = 150;
        [SerializeField] private int generations = 200;
        [SerializeField] private int gamesPerEvaluation = 10;
        [SerializeField] private int randomSeed = 42;

        [Header("GA Parameters")]
        [SerializeField] private float mutationRate = 0.15f;
        [SerializeField] private float mutationSigma = 0.3f;
        [SerializeField] private float eliteRatio = 0.05f;
        [SerializeField] private int tournamentSize = 5;

        [Header("Control")]
        [SerializeField] private bool startOnAwake = false;

        private EvolutionaryOptimizer optimizer;
        private GameSimulator simulator;
        private bool isTraining;
        private float trainingStartTime;

        private string outputDir;
        private string csvPath;
        private string weightsPath;

        private void Awake()
        {
            outputDir = Application.dataPath + "/../TestReports";
            csvPath = outputDir + "/ml_training.csv";
            weightsPath = outputDir + "/ml_best_weights.json";

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
        }

        private IEnumerator Start()
        {
            if (startOnAwake)
            {
                // Wait for scene transition and database initialization
                yield return null;
                yield return null;
                Debug.Log("[MLTrainingRunner] Start: delayed start, beginning training");
                StartTraining();
            }
        }

        public void StartTraining()
        {
            if (isTraining)
            {
                Debug.LogWarning("[MLTrainingRunner] StartTraining: already training");
                return;
            }

            Debug.Log($"[MLTrainingRunner] StartTraining: initializing (pop={populationSize}, " +
                      $"gen={generations}, games={gamesPerEvaluation}, seed={randomSeed})");

            // Load databases
            var cardDb = CardDatabase.Instance;
            var enemyDb = EnemyDatabase.Instance;
            var mapConfig = Resources.Load<MapConfigSO>("MapConfig");

            if (cardDb == null || enemyDb == null)
            {
                Debug.LogError("[MLTrainingRunner] StartTraining: failed to load databases");
                return;
            }

            if (mapConfig == null)
            {
                Debug.LogWarning("[MLTrainingRunner] StartTraining: MapConfig not found, using default");
                mapConfig = ScriptableObject.CreateInstance<MapConfigSO>();
            }

            Debug.Log($"[MLTrainingRunner] StartTraining: loaded databases " +
                      $"(cards={cardDb.AllCards.Count}, colonyCards={cardDb.AllColonyCards.Count}, " +
                      $"enemies={enemyDb.AllEnemies.Count})");

            // Create simulator
            simulator = new GameSimulator(
                cardDb.AllCards, cardDb.AllColonyCards, enemyDb.AllEnemies, mapConfig);

            // Create optimizer
            optimizer = new EvolutionaryOptimizer(
                populationSize, randomSeed, tournamentSize, eliteRatio, mutationRate, mutationSigma);

            // Write CSV header
            File.WriteAllText(csvPath,
                "Generation,BestFitness,AvgFitness,WorstFitness,Victories,AllTimeBest," +
                "BestHeroes,BestColony,BestDeckSize,BestMaxDeploy,BestFoodBuffer," +
                "BestDeployStart,BestGroupSize,BestPiperRush,BestFoodThreshold,ElapsedSec\n");

            trainingStartTime = Time.realtimeSinceStartup;
            isTraining = true;

            StartCoroutine(RunTraining());
        }

        private IEnumerator RunTraining()
        {
            Debug.Log("[MLTrainingRunner] RunTraining: starting evolutionary optimization (threaded)");

            for (int gen = 0; gen < generations; gen++)
            {
                float genStart = Time.realtimeSinceStartup;

                // Suppress logging during evaluation (guards in game systems skip Debug.Log)
                Scurry.Core.SimulationFlags.SuppressLogging = true;

                // Run evaluation on background thread to keep Unity responsive
                var evalTask = Task.Run(() =>
                {
                    optimizer.EvaluateGeneration(simulator, gamesPerEvaluation, randomSeed);
                });

                // Yield until background thread completes (timeout after 5 min)
                float timeout = Time.realtimeSinceStartup + 300f;
                while (!evalTask.IsCompleted)
                {
                    if (Time.realtimeSinceStartup > timeout)
                    {
                        Scurry.Core.SimulationFlags.SuppressLogging = false;
                        Debug.LogError($"[MLTrainingRunner] Gen {gen} timed out after 300s, skipping");
                        optimizer.AdvanceGeneration();
                        goto NextGeneration;
                    }
                    yield return null;
                }

                // Restore logging
                Scurry.Core.SimulationFlags.SuppressLogging = false;

                // Re-throw any exceptions from the background thread
                if (evalTask.IsFaulted)
                {
                    Debug.LogError($"[MLTrainingRunner] Gen {gen} evaluation failed: {evalTask.Exception?.InnerException?.Message}");
                    Debug.LogException(evalTask.Exception?.InnerException);
                    isTraining = false;
                    yield break;
                }

                float genTime = Time.realtimeSinceStartup - genStart;
                float totalTime = Time.realtimeSinceStartup - trainingStartTime;

                // Log progress
                var best = optimizer.BestIndividual;
                var pop = optimizer.Population;
                float avgFit = 0f;
                float worstFit = float.MaxValue;
                int totalVictories = 0;
                foreach (var ind in pop)
                {
                    avgFit += ind.fitness;
                    if (ind.fitness < worstFit) worstFit = ind.fitness;
                    totalVictories += ind.victories;
                }
                avgFit /= pop.Count;

                var w = best.weights;
                string csvLine = $"{gen},{best.fitness:F0},{avgFit:F0},{worstFit:F0}," +
                    $"{totalVictories},{optimizer.AllTimeBest.fitness:F0}," +
                    $"{w.HeroCount:F0},{w.ColonyCount:F0},{w.PreferredDeckSize}," +
                    $"{w.MaxDeployPerTurn},{w.FoodBufferRatio:F2}," +
                    $"{w.DeployStartTurn},{w.HeroGroupSize},{w.PiperRushTurn}," +
                    $"{w.FoodThreshold:F1},{totalTime:F1}\n";
                File.AppendAllText(csvPath, csvLine);

                // Save best weights every 10 generations
                if (gen % 10 == 0 || gen == generations - 1)
                {
                    SaveBestWeights();
                    Debug.Log($"[MLTrainingRunner] Gen {gen}/{generations}: " +
                              $"best={best.fitness:F0}, avg={avgFit:F0}, victories={totalVictories}, " +
                              $"genTime={genTime:F1}s, totalTime={totalTime:F1}s\n" +
                              $"  Weights: {optimizer.GetBestWeightsSummary()}");
                }

                // Advance to next generation (fast, can stay on main thread)
                optimizer.AdvanceGeneration();

                // Yield to keep Unity responsive between generations
                yield return null;

                NextGeneration:;
            }

            // Final save
            SaveBestWeights();

            float finalTime = Time.realtimeSinceStartup - trainingStartTime;
            var finalBest = optimizer.AllTimeBest;

            Debug.Log($"[MLTrainingRunner] Training complete: " +
                      $"{generations} generations in {finalTime:F1}s\n" +
                      $"  All-time best fitness: {finalBest.fitness:F0}\n" +
                      $"  Best victories: {finalBest.victories}\n" +
                      $"  Weights: {optimizer.GetBestWeightsSummary()}\n" +
                      $"  Results saved to: {csvPath}\n" +
                      $"  Weights saved to: {weightsPath}");

            isTraining = false;
        }

        private void SaveBestWeights()
        {
            var best = optimizer.AllTimeBest;
            string json = best.weights.ToJson();
            File.WriteAllText(weightsPath, json);

            // Also save a human-readable summary
            string summaryPath = outputDir + "/ml_best_summary.txt";
            var sb = new StringBuilder();
            sb.AppendLine($"Generation: {optimizer.Generation}");
            sb.AppendLine($"Fitness: {best.fitness:F0}");
            sb.AppendLine($"Victories: {best.victories}");
            sb.AppendLine($"Weights: {optimizer.GetBestWeightsSummary()}");
            sb.AppendLine();
            sb.AppendLine("All genes:");
            for (int i = 0; i < DecisionWeights.GENE_COUNT; i++)
            {
                sb.AppendLine($"  [{i}] = {best.weights.genes[i]:F4}");
            }
            File.WriteAllText(summaryPath, sb.ToString());
        }
    }
}
