using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Colony;

namespace Scurry.Core
{
    public class RunManager : MonoBehaviour
    {
        [Header("Zone Config")]
        [SerializeField] private ZoneSO currentZone;

        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ColonyManager colonyManager;

        [Header("Step Choice")]
        [SerializeField] private int stepChoiceCount = 3;

        // Run state
        private RunState runState;
        private int currentStageIndex;
        private int currentStepIndex;
        private int totalStepsForStage;
        private bool bossTriggered;

        // Deck (persists across steps within a stage)
        private List<CardDefinitionSO> currentDeck;
        private List<CardDefinitionSO> fullDeck; // original deck for stage restoration

        private void Awake()
        {
            Debug.Log($"[RunManager] Awake: zone={currentZone?.zoneName ?? "NULL"}, gameManager={gameManager?.name ?? "NULL"}, colonyManager={colonyManager?.name ?? "NULL"}");
        }

        private void OnEnable()
        {
            Debug.Log("[RunManager] OnEnable: subscribing to events");
            EventBus.OnDeckBuildComplete += OnDeckBuildComplete;
            EventBus.OnCardPlacementGameComplete += OnCardPlacementGameCompleteHandler;
            EventBus.OnStepChosen += OnStepChosen;
        }

        private void OnDisable()
        {
            Debug.Log("[RunManager] OnDisable: unsubscribing from events");
            EventBus.OnDeckBuildComplete -= OnDeckBuildComplete;
            EventBus.OnCardPlacementGameComplete -= OnCardPlacementGameCompleteHandler;
            EventBus.OnStepChosen -= OnStepChosen;
        }

        private void Start()
        {
            if (gameManager != null && gameManager.StandaloneMode)
            {
                Debug.Log("[RunManager] Start: GameManager is in standalone mode — RunManager inactive");
                enabled = false;
                return;
            }

            Debug.Log("[RunManager] Start: external mode — starting run");
            StartRun();
        }

        private void StartRun()
        {
            if (currentZone == null)
            {
                Debug.LogError("[RunManager] StartRun: no zone assigned!");
                return;
            }

            Debug.Log($"[RunManager] StartRun: zone='{currentZone.zoneName}', stages={currentZone.stagesPerZone}");
            runState = RunState.Draft;
            currentStageIndex = 0;
            currentStepIndex = 0;
            currentDeck = null;

            // Initialize colony HP
            colonyManager.InitializeHP();

            // DeckBuild phase — GameManager shows DeckBuildingManager UI via phase change
            // RunManager listens for OnDeckBuildComplete to receive the deck
            Debug.Log("[RunManager] StartRun: entering Draft state, triggering DeckBuild phase");
            EventBus.OnPhaseChanged?.Invoke(GamePhase.DeckBuild);
        }

        private void OnDeckBuildComplete(List<CardDefinitionSO> selectedCards)
        {
            Debug.Log($"[RunManager] OnDeckBuildComplete: received {selectedCards.Count} cards");
            fullDeck = new List<CardDefinitionSO>(selectedCards);
            currentDeck = new List<CardDefinitionSO>(selectedCards);
            runState = RunState.InStage;
            BeginZone();
        }

        private void BeginZone()
        {
            Debug.Log($"[RunManager] BeginZone: zone='{currentZone.zoneName}', starting stage 1 of {currentZone.stagesPerZone}");
            currentStageIndex = 0;
            BeginStage();
        }

        private void BeginStage()
        {
            // Restore full deck at stage start (exhausted resources return)
            currentDeck = new List<CardDefinitionSO>(fullDeck);
            totalStepsForStage = Random.Range(currentZone.minStepsPerStage, currentZone.maxStepsPerStage + 1);
            currentStepIndex = 0;
            bossTriggered = false;

            // All wounded heroes heal between stages
            gameManager.ClearWoundedHeroes();

            Debug.Log($"[RunManager] BeginStage: stage={currentStageIndex + 1}/{currentZone.stagesPerZone}, deck restored to {currentDeck.Count} cards, totalSteps={totalStepsForStage}, wounds cleared");

            string stageMsg = Loc.Format("run.stage.begin", currentStageIndex + 1, currentZone.stagesPerZone);
            EventBus.OnGatheringNotification?.Invoke(stageMsg, new Color(1f, 0.9f, 0.3f));
            EventBus.OnStageProgress?.Invoke(0, totalStepsForStage);

            // First step is always CardPlacement
            ExecuteStep(StepType.CardPlacement);
        }

        private void ExecuteStep(StepType step)
        {
            currentStepIndex++;
            Debug.Log($"[RunManager] ExecuteStep: step={currentStepIndex}/{totalStepsForStage}, type={step}");

            EventBus.OnStepStarted?.Invoke(step);
            EventBus.OnStageProgress?.Invoke(currentStepIndex, totalStepsForStage);

            string stepName = Loc.Get("run.step." + step.ToString().ToLower());
            string stepMsg = Loc.Format("run.step.begin", currentStepIndex, stepName);
            EventBus.OnGatheringNotification?.Invoke(stepMsg, new Color(0.8f, 0.8f, 1f));

            switch (step)
            {
                case StepType.CardPlacement:
                    Debug.Log($"[RunManager] ExecuteStep: CardPlacement — delegating to GameManager with {currentDeck.Count} cards");
                    gameManager.StartCardPlacementGame(currentDeck);
                    break;

                case StepType.Shop:
                    Debug.Log("[RunManager] ExecuteStep: Shop — stub, auto-advancing");
                    OnStepComplete();
                    break;

                case StepType.Healing:
                    Debug.Log("[RunManager] ExecuteStep: Healing — stub, auto-advancing");
                    OnStepComplete();
                    break;

                case StepType.CardAddRemove:
                    Debug.Log("[RunManager] ExecuteStep: CardAddRemove — stub, auto-advancing");
                    OnStepComplete();
                    break;

                case StepType.BossFight:
                    Debug.Log("[RunManager] ExecuteStep: BossFight — stub, auto-advancing");
                    OnStepComplete();
                    break;

                default:
                    Debug.LogWarning($"[RunManager] ExecuteStep: unknown step type {step} — auto-advancing");
                    OnStepComplete();
                    break;
            }
        }

        private void OnCardPlacementGameCompleteHandler(bool heroesLost)
        {
            Debug.Log($"[RunManager] OnCardPlacementGameComplete: step {currentStepIndex} done, heroesLost={heroesLost}");

            // Remove exhausted resources from deck for this stage
            var exhausted = gameManager.ExhaustedResources;
            if (exhausted.Count > 0)
            {
                foreach (var card in exhausted)
                {
                    currentDeck.Remove(card);
                    Debug.Log($"[RunManager] OnCardPlacementGameComplete: removed exhausted resource '{card.cardName}' from deck (deck={currentDeck.Count})");
                }
            }

            // Check if colony died during the card placement game
            if (!colonyManager.IsAlive)
            {
                Debug.Log("[RunManager] OnCardPlacementGameComplete: colony is dead — run failed");
                RunFailed("run.complete.defeat");
                return;
            }

            // Check if all heroes were lost (no heroes on board or in deck)
            if (heroesLost)
            {
                Debug.Log("[RunManager] OnCardPlacementGameComplete: all heroes lost — run failed");
                RunFailed("run.complete.defeat");
                return;
            }

            OnStepComplete();
        }

        private void RunFailed(string messageKey)
        {
            runState = RunState.GameOver;
            EventBus.OnRunFailed?.Invoke();
            string defeatMsg = Loc.Get(messageKey);
            EventBus.OnGatheringNotification?.Invoke(defeatMsg, new Color(1f, 0.2f, 0.2f));
        }

        private void OnStepComplete()
        {
            Debug.Log($"[RunManager] OnStepComplete: step {currentStepIndex}/{totalStepsForStage} done");

            // Check if this was the last step in the stage
            if (currentStepIndex >= totalStepsForStage)
            {
                // Boss fight at end of last stage only (once)
                if (currentStageIndex == currentZone.stagesPerZone - 1 && !bossTriggered)
                {
                    Debug.Log("[RunManager] OnStepComplete: last stage — triggering BossFight");
                    bossTriggered = true;
                    ExecuteStep(StepType.BossFight);
                    return;
                }

                Debug.Log($"[RunManager] OnStepComplete: all steps complete for stage {currentStageIndex + 1}");
                string completeMsg = Loc.Format("run.stage.complete", currentStageIndex + 1);
                EventBus.OnGatheringNotification?.Invoke(completeMsg, new Color(0.3f, 1f, 0.5f));
                AdvanceStage();
                return;
            }

            // Present step choices to the player
            PresentStepChoices();
        }

        private void PresentStepChoices()
        {
            runState = RunState.StepTransition;
            var options = GenerateStepOptions();

            string optionList = "";
            for (int i = 0; i < options.Length; i++)
            {
                if (i > 0) optionList += ", ";
                optionList += options[i].ToString();
            }
            Debug.Log($"[RunManager] PresentStepChoices: presenting {options.Length} options: [{optionList}]");

            EventBus.OnStepChoicePresented?.Invoke(options);
        }

        private void OnStepChosen(StepType choice)
        {
            if (runState != RunState.StepTransition)
            {
                Debug.LogWarning($"[RunManager] OnStepChosen: ignoring — not in StepTransition state (current={runState})");
                return;
            }

            Debug.Log($"[RunManager] OnStepChosen: player chose {choice}");
            runState = RunState.InStage;
            ExecuteStep(choice);
        }

        private StepType[] GenerateStepOptions()
        {
            if (currentZone.stepPool == null || currentZone.stepPool.Length == 0)
            {
                Debug.LogWarning("[RunManager] GenerateStepOptions: no step pool — defaulting to CardPlacement only");
                return new StepType[] { StepType.CardPlacement };
            }

            int count = Mathf.Min(stepChoiceCount, currentZone.stepPool.Length);
            var options = new List<StepType>();
            var availableIndices = new List<int>();
            for (int i = 0; i < currentZone.stepPool.Length; i++)
                availableIndices.Add(i);

            // Weighted random selection without replacement
            for (int picked = 0; picked < count && availableIndices.Count > 0; picked++)
            {
                float totalWeight = 0f;
                foreach (int idx in availableIndices)
                    totalWeight += currentZone.stepWeights[idx];

                float roll = Random.Range(0f, totalWeight);
                float cumulative = 0f;
                int selectedIdx = availableIndices[availableIndices.Count - 1];

                for (int i = 0; i < availableIndices.Count; i++)
                {
                    cumulative += currentZone.stepWeights[availableIndices[i]];
                    if (roll <= cumulative)
                    {
                        selectedIdx = availableIndices[i];
                        break;
                    }
                }

                options.Add(currentZone.stepPool[selectedIdx]);
                availableIndices.Remove(selectedIdx);
            }

            Debug.Log($"[RunManager] GenerateStepOptions: generated {options.Count} options from pool of {currentZone.stepPool.Length}");
            return options.ToArray();
        }

        private void AdvanceStage()
        {
            currentStageIndex++;
            Debug.Log($"[RunManager] AdvanceStage: advancing to stage {currentStageIndex + 1}/{currentZone.stagesPerZone}");

            if (currentStageIndex >= currentZone.stagesPerZone)
            {
                Debug.Log("[RunManager] AdvanceStage: all stages complete — run victory!");
                runState = RunState.RunComplete;
                EventBus.OnRunComplete?.Invoke();
                string victoryMsg = Loc.Get("run.complete.victory");
                EventBus.OnGatheringNotification?.Invoke(victoryMsg, new Color(1f, 0.9f, 0.3f));
                return;
            }

            BeginStage();
        }

        // Public accessors
        public RunState CurrentRunState => runState;
        public int CurrentStageIndex => currentStageIndex;
        public int CurrentStepIndex => currentStepIndex;
        public ZoneSO CurrentZone => currentZone;
    }
}
