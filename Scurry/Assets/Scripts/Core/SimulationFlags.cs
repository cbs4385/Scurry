namespace Scurry.Core
{
    /// <summary>
    /// Static flags used to control behavior during headless simulation.
    /// Lives in Core assembly so all game systems can reference it without circular dependencies.
    /// </summary>
    public static class SimulationFlags
    {
        /// <summary>
        /// When true, game systems should skip Debug.Log calls to avoid
        /// string formatting overhead during ML training.
        /// </summary>
        public static bool SuppressLogging { get; set; }

        /// <summary>
        /// When true, TurnManager skips scene transitions (LoadSceneAsync) so all phases
        /// execute in the current scene. Used by integration tests to avoid destroying
        /// test-created objects during scene loads.
        /// </summary>
        public static bool SkipSceneTransitions { get; set; }
    }
}
