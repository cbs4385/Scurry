using Scurry.Data;

namespace Scurry.Interfaces
{
    /// <summary>
    /// v2.0 turn manager interface. Controls the game flow
    /// with 7 phases per turn (Colony, Deploy, HeroMove, EnemyMove, Combat, Gather, Cleanup).
    /// No fixed turn limit — once a hero enters the Town zone, a 15-turn Pied Piper
    /// countdown begins. When the countdown expires, the Pied Piper marches on the colony.
    /// </summary>
    public interface ITurnManager
    {
        /// <summary>The current turn number (1-based, no fixed limit).</summary>
        int CurrentTurn { get; }

        /// <summary>The currently active phase within the turn.</summary>
        GamePhase CurrentPhase { get; }

        /// <summary>True if the Pied Piper countdown has been triggered (Town zone entered).</summary>
        bool PiperCountdownActive { get; }

        /// <summary>Turns remaining before the Pied Piper marches on the colony. -1 if not started.</summary>
        int PiperCountdownRemaining { get; }

        /// <summary>
        /// Begins a new run, resetting the turn counter to 1 and entering the Colony phase.
        /// </summary>
        void StartRun();

        /// <summary>
        /// Advances to the next phase in the turn sequence.
        /// After Cleanup, increments the turn counter and returns to Colony.
        /// </summary>
        void AdvancePhase();

        /// <summary>
        /// Queues a hero deployment action during the Deploy phase.
        /// Heroes spawn at the colony node and move toward the target node.
        /// </summary>
        void PlayerDeployHero(CardDefinitionSO heroDef, int targetNodeId,
                              CardDefinitionSO offensive, CardDefinitionSO defensive,
                              CardDefinitionSO utility);

        /// <summary>
        /// Queues a retarget action for an already-deployed hero during the Deploy phase.
        /// </summary>
        void PlayerRetargetHero(int heroTokenId, int newTargetNodeId);

        /// <summary>The colony node ID on the map (default deploy location).</summary>
        int ColonyNodeId { get; }

        /// <summary>Number of heroes deployed so far this turn.</summary>
        int HeroesDeployedThisTurn { get; }

        /// <summary>Maximum heroes that can be deployed per turn (from balance config).</summary>
        int MaxHeroDeploysPerTurn { get; }

        /// <summary>True if the deploy cap has been reached for this turn.</summary>
        bool DeployCapReached { get; }

        /// <summary>Signals the end of the current player-input phase.</summary>
        void PlayerEndPhase();
    }
}
