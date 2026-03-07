using UnityEngine;

namespace Scurry.Core
{
    public static class SeededRandom
    {
        private static System.Random rng;
        private static int currentSeed;

        public static int CurrentSeed => currentSeed;

        static SeededRandom()
        {
            Initialize(System.Environment.TickCount);
        }

        public static void Initialize(int seed)
        {
            currentSeed = seed;
            rng = new System.Random(seed);
            Debug.Log($"[SeededRandom] Initialize: seed={seed}");
        }

        /// <summary>
        /// Returns a random int in [min, max) — same contract as UnityEngine.Random.Range(int, int).
        /// </summary>
        public static int Range(int min, int max)
        {
            if (min >= max) return min;
            return rng.Next(min, max);
        }

        /// <summary>
        /// Returns a random float in [min, max) — same contract as UnityEngine.Random.Range(float, float).
        /// </summary>
        public static float Range(float min, float max)
        {
            if (min >= max) return min;
            return min + (float)rng.NextDouble() * (max - min);
        }

        /// <summary>
        /// Returns a random float in [0, 1) — same contract as UnityEngine.Random.value.
        /// </summary>
        public static float Value => (float)rng.NextDouble();
    }
}
