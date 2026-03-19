using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Scurry.ML
{
    /// <summary>
    /// Genetic algorithm optimizer that evolves DecisionWeights to maximize game fitness.
    /// Uses tournament selection, uniform crossover, and Gaussian mutation.
    /// </summary>
    public class EvolutionaryOptimizer
    {
        public struct Individual
        {
            public DecisionWeights weights;
            public float fitness;
            public float bestFitness;  // best across evaluation games
            public float worstFitness;
            public int victories;
        }

        private readonly int populationSize;
        private readonly int tournamentSize;
        private readonly int eliteCount;
        private readonly float mutationRate;
        private readonly float mutationSigma;
        private readonly System.Random rng;

        private List<Individual> population;
        private Individual allTimeBest;
        private int generation;

        public Individual BestIndividual => population.OrderByDescending(i => i.fitness).First();
        public Individual AllTimeBest => allTimeBest;
        public int Generation => generation;
        public IReadOnlyList<Individual> Population => population;

        public EvolutionaryOptimizer(int popSize, int seed = 42,
            int tournamentSize = 5, float eliteRatio = 0.05f,
            float mutationRate = 0.15f, float mutationSigma = 0.3f)
        {
            populationSize = popSize;
            this.tournamentSize = tournamentSize;
            eliteCount = Mathf.Max(1, Mathf.RoundToInt(popSize * eliteRatio));
            this.mutationRate = mutationRate;
            this.mutationSigma = mutationSigma;
            rng = new System.Random(seed);

            // Initialize population
            population = new List<Individual>(popSize);
            for (int i = 0; i < popSize; i++)
            {
                population.Add(new Individual
                {
                    weights = DecisionWeights.Random(rng),
                    fitness = 0f,
                    bestFitness = 0f,
                    worstFitness = float.MaxValue,
                    victories = 0
                });
            }

            allTimeBest = population[0];
            generation = 0;

            Debug.Log($"[EvolutionaryOptimizer] Initialized: popSize={popSize}, tournament={tournamentSize}, " +
                      $"elite={eliteCount}, mutRate={mutationRate}, mutSigma={mutationSigma}");
        }

        /// <summary>
        /// Evaluates the entire population by running N games per individual.
        /// </summary>
        public void EvaluateGeneration(GameSimulator simulator, int gamesPerIndividual, int baseSeed)
        {
            if (!Scurry.Core.SimulationFlags.SuppressLogging) Debug.Log($"[EvolutionaryOptimizer] EvaluateGeneration: gen={generation}, " +
                      $"pop={populationSize}, gamesPerInd={gamesPerIndividual}");

            for (int i = 0; i < population.Count; i++)
            {
                var ind = population[i];
                float totalFitness = 0f;
                float best = float.MinValue;
                float worst = float.MaxValue;
                int victories = 0;

                for (int g = 0; g < gamesPerIndividual; g++)
                {
                    int gameSeed = baseSeed + generation * 10000 + i * 100 + g;
                    var result = simulator.Simulate(ind.weights, gameSeed);

                    totalFitness += result.fitness;
                    if (result.fitness > best) best = result.fitness;
                    if (result.fitness < worst) worst = result.fitness;
                    if (result.victory) victories++;
                }

                ind.fitness = totalFitness / gamesPerIndividual;
                ind.bestFitness = best;
                ind.worstFitness = worst;
                ind.victories = victories;
                population[i] = ind;

                if (ind.fitness > allTimeBest.fitness)
                {
                    allTimeBest = new Individual
                    {
                        weights = ind.weights.Clone(),
                        fitness = ind.fitness,
                        bestFitness = ind.bestFitness,
                        worstFitness = ind.worstFitness,
                        victories = ind.victories
                    };
                }
            }

            var currentBest = BestIndividual;
            float avgFitness = population.Average(p => p.fitness);
            int totalVictories = population.Sum(p => p.victories);

            if (!Scurry.Core.SimulationFlags.SuppressLogging) Debug.Log($"[EvolutionaryOptimizer] Gen {generation}: " +
                      $"bestFit={currentBest.fitness:F0}, avgFit={avgFitness:F0}, " +
                      $"victories={totalVictories}/{populationSize * gamesPerIndividual}, " +
                      $"allTimeBest={allTimeBest.fitness:F0}");
        }

        /// <summary>
        /// Creates the next generation using selection, crossover, and mutation.
        /// Preserves elite individuals unchanged.
        /// </summary>
        public void AdvanceGeneration()
        {
            generation++;

            // Sort by fitness descending
            var sorted = population.OrderByDescending(i => i.fitness).ToList();

            var newPop = new List<Individual>(populationSize);

            // Elitism: carry top individuals unchanged
            for (int i = 0; i < eliteCount; i++)
            {
                newPop.Add(new Individual
                {
                    weights = sorted[i].weights.Clone(),
                    fitness = 0f,
                    bestFitness = 0f,
                    worstFitness = float.MaxValue,
                    victories = 0
                });
            }

            // Fill rest with offspring
            while (newPop.Count < populationSize)
            {
                var parentA = TournamentSelect(sorted);
                var parentB = TournamentSelect(sorted);

                var childWeights = DecisionWeights.Crossover(parentA.weights, parentB.weights, rng);
                childWeights.Mutate(mutationSigma, mutationRate, rng);

                newPop.Add(new Individual
                {
                    weights = childWeights,
                    fitness = 0f,
                    bestFitness = 0f,
                    worstFitness = float.MaxValue,
                    victories = 0
                });
            }

            population = newPop;
        }

        private Individual TournamentSelect(List<Individual> sorted)
        {
            Individual best = sorted[rng.Next(sorted.Count)];
            for (int i = 1; i < tournamentSize; i++)
            {
                var candidate = sorted[rng.Next(sorted.Count)];
                if (candidate.fitness > best.fitness)
                    best = candidate;
            }
            return best;
        }

        /// <summary>
        /// Seeds the population from previously trained weights.
        /// The seed individual is placed as-is, and the rest of the population
        /// is initialized as mutated variants to maintain diversity.
        /// </summary>
        public void SeedFromWeights(DecisionWeights seedWeights)
        {
            Debug.Log($"[EvolutionaryOptimizer] SeedFromWeights: seeding population from previous best");

            // Slot 0: exact copy of the seed
            population[0] = new Individual
            {
                weights = seedWeights.Clone(),
                fitness = 0f,
                bestFitness = 0f,
                worstFitness = float.MaxValue,
                victories = 0
            };

            // Remaining slots: mutated variants of the seed (varying mutation intensity)
            for (int i = 1; i < population.Count; i++)
            {
                var variant = seedWeights.Clone();
                // Progressively wider mutations: first half gets small perturbations, second half gets larger
                float intensityScale = 1f + (float)i / population.Count * 3f;
                variant.Mutate(mutationSigma * intensityScale, mutationRate + 0.2f, rng);
                population[i] = new Individual
                {
                    weights = variant,
                    fitness = 0f,
                    bestFitness = 0f,
                    worstFitness = float.MaxValue,
                    victories = 0
                };
            }

            Debug.Log($"[EvolutionaryOptimizer] SeedFromWeights: seeded 1 exact + {population.Count - 1} mutated variants");
        }

        /// <summary>
        /// Returns a summary string of the best individual's key weights for logging.
        /// </summary>
        public string GetBestWeightsSummary()
        {
            var w = BestIndividual.weights;
            return $"heroes={w.HeroCount:F0}, colony={w.ColonyCount:F0}, deckSize={w.PreferredDeckSize}, " +
                   $"maxDeploy={w.MaxDeployPerTurn}, foodBuffer={w.FoodBufferRatio:F2}, " +
                   $"deployStart={w.DeployStartTurn}, groupSize={w.HeroGroupSize}, " +
                   $"piperRush={w.PiperRushTurn}, foodThreshold={w.FoodThreshold:F1}";
        }
    }
}
