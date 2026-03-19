using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Map;
using Scurry.UI;

namespace Scurry.Core
{
    /// <summary>
    /// Tracks tutorial state and triggers contextual prompts during the player's first run.
    /// Listens to EventBus events and shows TutorialUI highlights at key moments.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        private const string PREFS_KEY_TUTORIAL = "TutorialState";

        private static TutorialManager instance;
        public static TutorialManager Instance => instance;

        // Tutorial step tracking
        private HashSet<TutorialStep> completedSteps = new HashSet<TutorialStep>();
        private TutorialStep? activeStep;
        private TutorialUI tutorialUI;

        public bool TutorialActive { get; private set; }

        public enum TutorialStep
        {
            Welcome,
            DeployHeroes,
            MoveHeroes,
            PlayTacticalCards,
            BuildColony,
            GatherResources,
            GroupForBosses
        }

        private static readonly Dictionary<TutorialStep, string> StepMessages = new Dictionary<TutorialStep, string>
        {
            { TutorialStep.Welcome, "Welcome to Scurry! Your colony of rats needs brave heroes to explore the map, gather resources, and defeat the Pied Piper." },
            { TutorialStep.DeployHeroes, "Deploy your heroes to the map. Each hero has Combat, Move, HP, and Carry stats. Equip them with weapons and armor for an edge in combat." },
            { TutorialStep.MoveHeroes, "Heroes move toward their target node each turn. Set targets wisely — explore fog-covered nodes to find resources and enemies." },
            { TutorialStep.PlayTacticalCards, "Tactical cards are single-use and powerful! Play them before or between combat rounds for buffs, healing, or special effects." },
            { TutorialStep.BuildColony, "Build your colony by placing cards on the colony graph. Colony buildings produce food, defend your base, and buff your heroes." },
            { TutorialStep.GatherResources, "Heroes at resource nodes automatically gather. They must carry resources back to the colony to deposit them!" },
            { TutorialStep.GroupForBosses, "Zone bosses are tough! Group multiple heroes on the same node for combat bonuses. Use tactical cards to turn the tide." }
        };

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            Debug.Log("[TutorialManager] Awake: initializing");
            LoadState();
        }

        private void OnEnable()
        {
            Debug.Log("[TutorialManager] OnEnable: subscribing to events");
            EventBus.OnRunStarted += OnRunStarted;
            EventBus.OnPhaseChanged += OnPhaseChanged;
            EventBus.OnCombatStarted += OnCombatStarted;
            EventBus.OnResourceGathered += OnResourceGathered;
            EventBus.OnCombatEnded += OnCombatEnded;
        }

        private void OnDisable()
        {
            Debug.Log("[TutorialManager] OnDisable: unsubscribing from events");
            EventBus.OnRunStarted -= OnRunStarted;
            EventBus.OnPhaseChanged -= OnPhaseChanged;
            EventBus.OnCombatStarted -= OnCombatStarted;
            EventBus.OnResourceGathered -= OnResourceGathered;
            EventBus.OnCombatEnded -= OnCombatEnded;
        }

        /// <summary>
        /// Initializes the tutorial for a new run if the player hasn't completed it.
        /// </summary>
        private void OnRunStarted()
        {
            var settings = GameSettings.Instance;
            if (settings != null && settings.TutorialCompleted)
            {
                TutorialActive = false;
                Debug.Log("[TutorialManager] OnRunStarted: tutorial already completed — inactive");
                return;
            }

            TutorialActive = true;
            Debug.Log("[TutorialManager] OnRunStarted: tutorial active for this run");

            // Show welcome message
            TriggerStep(TutorialStep.Welcome);
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (!TutorialActive) return;

            Debug.Log($"[TutorialManager] OnPhaseChanged: phase={phase}");

            switch (phase)
            {
                case GamePhase.Deploy:
                    if (!completedSteps.Contains(TutorialStep.BuildColony))
                        TriggerStep(TutorialStep.BuildColony);
                    else if (!completedSteps.Contains(TutorialStep.DeployHeroes))
                        TriggerStep(TutorialStep.DeployHeroes);
                    break;
                case GamePhase.HeroMove:
                    if (!completedSteps.Contains(TutorialStep.MoveHeroes))
                        TriggerStep(TutorialStep.MoveHeroes);
                    break;
                case GamePhase.Gather:
                    if (!completedSteps.Contains(TutorialStep.GatherResources))
                        TriggerStep(TutorialStep.GatherResources);
                    break;
            }
        }

        private void OnCombatStarted(int nodeId)
        {
            if (!TutorialActive) return;
            if (!completedSteps.Contains(TutorialStep.PlayTacticalCards))
                TriggerStep(TutorialStep.PlayTacticalCards);
        }

        private void OnResourceGathered(HeroToken hero, ResourceType type, int amount)
        {
            if (!TutorialActive) return;
            CompleteStep(TutorialStep.GatherResources);
        }

        private void OnCombatEnded(int nodeId, bool heroesWon)
        {
            if (!TutorialActive) return;
            CompleteStep(TutorialStep.PlayTacticalCards);

            // Check if all steps completed
            if (completedSteps.Count >= System.Enum.GetValues(typeof(TutorialStep)).Length)
            {
                CompleteTutorial();
            }
        }

        /// <summary>
        /// Triggers a tutorial step — shows the prompt if not already completed.
        /// </summary>
        private void TriggerStep(TutorialStep step)
        {
            if (completedSteps.Contains(step)) return;

            Debug.Log($"[TutorialManager] TriggerStep: showing step={step}");
            activeStep = step;

            if (StepMessages.TryGetValue(step, out string message))
            {
                // Find or create TutorialUI
                if (tutorialUI == null)
                {
                    tutorialUI = Object.FindAnyObjectByType<TutorialUI>();
                    if (tutorialUI == null)
                    {
                        var go = new GameObject("TutorialUI", typeof(TutorialUI));
                        tutorialUI = go.GetComponent<TutorialUI>();
                        Debug.Log("[TutorialManager] TriggerStep: created TutorialUI dynamically");
                    }
                }

                tutorialUI.ShowTip(step.ToString(), message, () => CompleteStep(step));
            }
        }

        /// <summary>
        /// Marks a step as completed and dismisses the tutorial prompt.
        /// </summary>
        public void CompleteStep(TutorialStep step)
        {
            if (completedSteps.Contains(step)) return;

            completedSteps.Add(step);
            Debug.Log($"[TutorialManager] CompleteStep: step={step}, completed={completedSteps.Count}/{System.Enum.GetValues(typeof(TutorialStep)).Length}");

            if (activeStep == step)
            {
                activeStep = null;
                if (tutorialUI != null) tutorialUI.HideTip();
            }

            SaveState();
        }

        /// <summary>
        /// Marks the entire tutorial as completed.
        /// </summary>
        private void CompleteTutorial()
        {
            Debug.Log("[TutorialManager] CompleteTutorial: all steps completed!");
            TutorialActive = false;

            var settings = GameSettings.Instance;
            if (settings != null)
            {
                settings.SetTutorialCompleted(true);
            }

            if (tutorialUI != null) tutorialUI.HideTip();
            EventBus.OnNotification?.Invoke("Tutorial Complete!", new Color(0.4f, 1f, 0.4f));
        }

        private void SaveState()
        {
            var data = new TutorialSaveData();
            foreach (var step in completedSteps)
            {
                data.completedStepIds.Add((int)step);
            }
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PREFS_KEY_TUTORIAL, json);
            PlayerPrefs.Save();
            Debug.Log($"[TutorialManager] SaveState: saved {data.completedStepIds.Count} completed steps");
        }

        private void LoadState()
        {
            completedSteps.Clear();
            if (PlayerPrefs.HasKey(PREFS_KEY_TUTORIAL))
            {
                var data = JsonUtility.FromJson<TutorialSaveData>(PlayerPrefs.GetString(PREFS_KEY_TUTORIAL));
                if (data?.completedStepIds != null)
                {
                    foreach (int id in data.completedStepIds)
                    {
                        completedSteps.Add((TutorialStep)id);
                    }
                }
                Debug.Log($"[TutorialManager] LoadState: loaded {completedSteps.Count} completed steps");
            }
        }

        /// <summary>
        /// Resets tutorial progress (for testing or replay).
        /// </summary>
        public void ResetTutorial()
        {
            Debug.Log("[TutorialManager] ResetTutorial: clearing all tutorial progress");
            completedSteps.Clear();
            PlayerPrefs.DeleteKey(PREFS_KEY_TUTORIAL);
            var settings = GameSettings.Instance;
            if (settings != null) settings.SetTutorialCompleted(false);
        }

        [System.Serializable]
        private class TutorialSaveData
        {
            public List<int> completedStepIds = new List<int>();
        }
    }
}
