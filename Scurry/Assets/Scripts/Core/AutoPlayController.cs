using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scurry.Data;
using Scurry.Cards;
using Scurry.Map;
using Scurry.Colony;
using Scurry.Interfaces;
using Scurry.Logistics;
using Scurry.UI;

namespace Scurry.Core
{
    /// <summary>
    /// Automated playthrough controller. Drives the game from MainMenu through
    /// DeckConstruction, GameMap gameplay, and to RunResult without manual input.
    /// Supports multiple strategies for balance testing.
    /// </summary>
    public class AutoPlayController : MonoBehaviour
    {
        private enum AutoPlayState
        {
            WaitingForMainMenu,
            InMainMenu,
            InDeckConstruction,
            InGameMap,
            WaitingForTurnManager,
            PlayingTurns,
            InRunResult,
            Done
        }

        // ── Strategy Definitions ──────────────────────────────────────────
        private const int STRATEGY_COUNT = 6;

        private static readonly string[] StrategyNames =
        {
            "ColonyFirst",      // 0: Heavy colony investment, food-limited deploy
            "ZergRush",         // 1: Max heroes, deploy all immediately
            "SpeedScouts",      // 2: High-move heroes, explore/gather, avoid combat
            "FortressColony",   // 3: Max colony, tanky heroes, late deploy
            "PiperBeeline",     // 4: Rush to Pied Piper, combat + speed heroes
            "BalancedAdaptive"  // 5: Mixed deck, adaptive targeting by turn
        };

        // ── Batch Mode ──────────────────────────────────────────────────
        [Header("Batch Mode")]
        [SerializeField] private bool batchMode = true;
        [SerializeField] private int maxBatchRuns = 500;

        [Header("Strategy")]
        [SerializeField] private int strategyIndex = -1; // -1 = cycle all strategies

        private int batchRunCount;
        private int currentStrategyIndex;
        private List<BatchRunResult> batchResults = new List<BatchRunResult>();

        public static bool IsBatchMode { get; private set; }
        private ILogHandler _originalLogHandler;

        private class BatchLogHandler : ILogHandler
        {
            private ILogHandler _original;
            public BatchLogHandler(ILogHandler original) { _original = original; }
            public ILogHandler Original => _original;
            public void LogException(System.Exception exception, Object context)
            {
                _original.LogException(exception, context);
            }
            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                if (logType == LogType.Error || logType == LogType.Exception)
                    _original.LogFormat(logType, context, format, args);
            }
        }

        private struct BatchRunResult
        {
            public int runNumber;
            public int strategy;
            public string strategyName;
            public bool victory;
            public int turnsUsed;
            public int enemiesDefeated;
            public int zoneBossesDefeated;
            public bool piedPiperDefeated;
            public int totalResourcesGathered;
            public int colonyCardsPlayed;
            public int heroesNeverInjured;
            public int totalHeroes;
            public int deckSize;
            public int colonyDeckSize;
            public int foodStockpile;
            public int materialsStockpile;
            public int currencyStockpile;
            public int score;
            public int randomSeed;
        }

        private AutoPlayState state = AutoPlayState.WaitingForMainMenu;
        private bool hasStartedNewRun;
        private float sceneWaitTimer;
        private TurnManager turnManager;
        private DeckManager deckManager;
        private int lastLoggedTurn;
        private int lastLoggedPhase;
        private float phaseEndCooldown;
        private (string label, float delay) pendingScreenshot;

        // ── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            Debug.Log($"[AutoPlayController] Awake: auto-play controller initialized (batchMode={batchMode}, maxRuns={maxBatchRuns}, strategy={strategyIndex})");
            IsBatchMode = batchMode;

            if (batchMode)
            {
                _originalLogHandler = Debug.unityLogger.logHandler;
                Debug.unityLogger.logHandler = new BatchLogHandler(_originalLogHandler);
                Time.timeScale = 10f;
                Application.targetFrameRate = -1;
                QualitySettings.vSyncCount = 0;
            }

            Application.runInBackground = true;
            currentStrategyIndex = strategyIndex >= 0 ? strategyIndex : 0;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("[AutoPlayController] OnEnable: subscribed to sceneLoaded");
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Debug.Log("[AutoPlayController] OnDisable: unsubscribed from sceneLoaded");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[AutoPlayController] OnSceneLoaded: scene='{scene.name}', mode={mode}, currentState={state}");

            float menuDelay = batchMode ? 0.1f : 3.0f;
            float deckDelay = batchMode ? 0.1f : 3.0f;
            float mapDelay = batchMode ? 0.5f : 3.0f;
            float resultDelay = batchMode ? 0.1f : 10.0f;

            switch (scene.name)
            {
                case "MainMenu":
                    state = AutoPlayState.InMainMenu;
                    hasStartedNewRun = false;
                    sceneWaitTimer = menuDelay;
                    Debug.Log($"[AutoPlayController] OnSceneLoaded: in MainMenu, batch={batchMode}, run={batchRunCount}/{maxBatchRuns}, strategy={StrategyNames[currentStrategyIndex]}");
                    break;
                case "DeckConstruction":
                    state = AutoPlayState.InDeckConstruction;
                    sceneWaitTimer = deckDelay;
                    break;
                case "GameMap":
                    if (mode == LoadSceneMode.Single)
                    {
                        state = AutoPlayState.InGameMap;
                        sceneWaitTimer = mapDelay;
                        turnManager = null;
                        waitForGameManagerAttempts = 0;
                    }
                    break;
                case "RunResult":
                    state = AutoPlayState.InRunResult;
                    sceneWaitTimer = resultDelay;
                    break;
            }
        }

        private float diagTimer;
        private void WriteDiag(string msg)
        {
            if (!batchMode) return;
            string path = Application.dataPath + "/../TestReports/batch_progress.txt";
            System.IO.File.AppendAllText(path, $"[{Time.realtimeSinceStartup:F1}s] {msg}\n");
        }

        private void Update()
        {
            if (state == AutoPlayState.Done) return;

            diagTimer -= Time.unscaledDeltaTime;
            if (diagTimer <= 0 && batchMode)
            {
                diagTimer = 30f; // Only write every 30s to avoid file bloat
                WriteDiag($"state={state}, run={batchRunCount}/{maxBatchRuns}, strategy={currentStrategyIndex}, sceneWait={sceneWaitTimer:F2}");
            }

            if (!batchMode && pendingScreenshot.label != null)
            {
                pendingScreenshot.delay -= Time.unscaledDeltaTime;
                if (pendingScreenshot.delay <= 0)
                {
                    CaptureGameView(pendingScreenshot.label);
                    pendingScreenshot = (null, 0);
                }
            }

            if (sceneWaitTimer > 0)
            {
                sceneWaitTimer -= Time.unscaledDeltaTime;
                return;
            }

            switch (state)
            {
                case AutoPlayState.InMainMenu:      HandleMainMenu(); break;
                case AutoPlayState.InDeckConstruction: HandleDeckConstruction(); break;
                case AutoPlayState.InGameMap:        HandleGameMapInit(); break;
                case AutoPlayState.WaitingForTurnManager: HandleWaitForTurnManager(); break;
                case AutoPlayState.PlayingTurns:     HandlePlayingTurns(); break;
                case AutoPlayState.InRunResult:      HandleRunResult(); break;
            }
        }

        private int screenshotIndex;

        private void CaptureGameView(string label)
        {
            string dir = Application.dataPath + "/Screenshots";
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            string filename = $"Assets/Screenshots/autoplay_{screenshotIndex:D2}_{label}.png";
            ScreenCapture.CaptureScreenshot(filename);
            Debug.Log($"[AutoPlayController] CaptureGameView: saved screenshot '{filename}'");
            screenshotIndex++;
        }

        // ── MainMenu ──────────────────────────────────────────────────────

        private void HandleMainMenu()
        {
            if (hasStartedNewRun) return;

            var runManager = RunManager.Instance;
            if (runManager == null)
            {
                sceneWaitTimer = 0.5f;
                return;
            }

            if (!batchMode)
            {
                VerifyUIElements("MainMenu");
                CaptureGameView("MainMenu");
            }

            hasStartedNewRun = true;
            runManager.StartNewRun();
            Debug.Log($"[AutoPlayController] HandleMainMenu: started new run with strategy={StrategyNames[currentStrategyIndex]}");
        }

        // ══════════════════════════════════════════════════════════════════
        // ██ DECK CONSTRUCTION — Strategy-specific deck building
        // ══════════════════════════════════════════════════════════════════

        private void HandleDeckConstruction()
        {
            Debug.Log($"[AutoPlayController] HandleDeckConstruction: building deck for strategy={StrategyNames[currentStrategyIndex]}");

            var dcm = Object.FindAnyObjectByType<DeckConstructionManager>();
            if (dcm == null) { sceneWaitTimer = 0.5f; return; }

            if (dcm.CardPool.Count == 0)
                dcm.Initialize();

            switch (currentStrategyIndex)
            {
                case 0: BuildDeck_ColonyFirst(dcm); break;
                case 1: BuildDeck_ZergRush(dcm); break;
                case 2: BuildDeck_SpeedScouts(dcm); break;
                case 3: BuildDeck_FortressColony(dcm); break;
                case 4: BuildDeck_PiperBeeline(dcm); break;
                case 5: BuildDeck_BalancedAdaptive(dcm); break;
            }

            Debug.Log($"[AutoPlayController] HandleDeckConstruction: deck built — total={dcm.DeckSize}, " +
                      $"main={dcm.CurrentDeck.Count}, colony={dcm.CurrentColonyDeck.Count}, valid={dcm.IsDeckValid}");

            if (!batchMode)
            {
                var dcUI = Object.FindAnyObjectByType<DeckConstructionUI>();
                if (dcUI != null)
                {
                    dcUI.RefreshCardPool();
                    dcUI.RefreshCurrentDeck();
                }
                VerifyUIElements("DeckConstruction");
            }

            WriteDiag($"HandleDeckConstruction: deckSize={dcm.DeckSize}, valid={dcm.IsDeckValid}, main={dcm.CurrentDeck.Count}, colony={dcm.CurrentColonyDeck.Count}");

            if (dcm.IsDeckValid)
            {
                state = AutoPlayState.Done;
                StartCoroutine(ConfirmDeckAfterDelay(dcm, batchMode ? 0.1f : 2.0f));
            }
            else
            {
                Debug.LogError($"[AutoPlayController] HandleDeckConstruction: deck is INVALID (size={dcm.DeckSize})");
                WriteDiag($"DECK INVALID: size={dcm.DeckSize}");
                state = AutoPlayState.Done;
            }
        }

        // ── Strategy 0: Colony First (baseline) ──
        private void BuildDeck_ColonyFirst(DeckConstructionManager dcm)
        {
            // 10 colony cards (food priority), 8 heroes (combat priority), defensive equipment, tactical fill
            AddColonyCards(dcm, 10, ColonyPriority_FoodFirst);
            AddHeroes(dcm, 8, h => h.combat);
            AddEquipment(dcm, 28, EquipmentSlot.Defensive);
            AddTactical(dcm, 30);
        }

        // ── Strategy 1: Zerg Rush ──
        private void BuildDeck_ZergRush(DeckConstructionManager dcm)
        {
            // 4 colony (food only), max heroes (sorted by combat), offensive equipment
            AddColonyCards(dcm, 4, ColonyPriority_FoodOnly);
            AddHeroes(dcm, 16, h => h.combat);
            AddEquipment(dcm, 28, EquipmentSlot.Offensive);
            AddTactical(dcm, 30);
        }

        // ── Strategy 2: Speed Scouts ──
        private void BuildDeck_SpeedScouts(DeckConstructionManager dcm)
        {
            // 6 colony (fog reveal priority), 8 heroes (move priority), utility equipment
            AddColonyCards(dcm, 6, ColonyPriority_FogFirst);
            AddHeroes(dcm, 8, h => h.move * 10 + h.carry);
            AddEquipment(dcm, 28, EquipmentSlot.Utility);
            AddTactical(dcm, 30);
        }

        // ── Strategy 3: Fortress Colony ──
        private void BuildDeck_FortressColony(DeckConstructionManager dcm)
        {
            // 14 colony (food + defense), 6 heroes (HP priority), defensive equipment
            AddColonyCards(dcm, 14, ColonyPriority_DefenseFirst);
            AddHeroes(dcm, 6, h => h.hp * 10 + h.combat);
            AddEquipment(dcm, 28, EquipmentSlot.Defensive);
            AddTactical(dcm, 30);
        }

        // ── Strategy 4: Piper Beeline ──
        private void BuildDeck_PiperBeeline(DeckConstructionManager dcm)
        {
            // 6 colony (move buffs + food), 10 heroes (combat+move balanced), offensive equipment
            AddColonyCards(dcm, 6, ColonyPriority_MoveFirst);
            AddHeroes(dcm, 10, h => h.combat + h.move);
            AddEquipment(dcm, 28, EquipmentSlot.Offensive);
            AddTactical(dcm, 30);
        }

        // ── Strategy 5: Balanced Adaptive ──
        private void BuildDeck_BalancedAdaptive(DeckConstructionManager dcm)
        {
            // 8 colony, 8 heroes (initiative priority), balanced equipment
            AddColonyCards(dcm, 8, ColonyPriority_FoodFirst);
            AddHeroes(dcm, 8, h => h.initiative);
            AddEquipment(dcm, 26, EquipmentSlot.Offensive); // leave room for tactical
            AddTactical(dcm, 30);
        }

        // ── Shared Deck Building Helpers ──

        private void AddColonyCards(DeckConstructionManager dcm, int target, System.Func<ColonyCardDefinitionSO, int> priorityFunc)
        {
            var sorted = dcm.ColonyCardPool
                .OrderByDescending(c => priorityFunc(c))
                .ToList();
            foreach (var card in sorted)
            {
                if (dcm.DeckSize >= target + 2) break; // +2 for starters
                if (dcm.AddColonyCard(card))
                {
                    Debug.Log($"[AutoPlayController] AddColonyCards: added '{card.cardName}' (effect={card.colonyEffect})");
                }
            }
        }

        private void AddHeroes(DeckConstructionManager dcm, int count, System.Func<CardDefinitionSO, int> sortKey)
        {
            var heroes = dcm.GetCardsByType(CardType.Hero)
                .OrderByDescending(h => sortKey(h))
                .ToList();
            int heroTarget = dcm.DeckSize + count;
            foreach (var hero in heroes)
            {
                if (dcm.DeckSize >= heroTarget) break;
                if (dcm.AddCard(hero))
                {
                    Debug.Log($"[AutoPlayController] AddHeroes: added '{hero.cardName}' (combat={hero.combat}, move={hero.move}, hp={hero.hp})");
                }
            }
        }

        private void AddEquipment(DeckConstructionManager dcm, int maxDeckSize, EquipmentSlot preferredSlot)
        {
            var equipment = dcm.GetCardsByType(CardType.Equipment)
                .OrderByDescending(e => e.equipmentSlot == preferredSlot ? 2 : 1)
                .ToList();
            foreach (var equip in equipment)
            {
                if (dcm.DeckSize >= maxDeckSize) break;
                dcm.AddCard(equip);
            }
        }

        private void AddTactical(DeckConstructionManager dcm, int maxDeckSize)
        {
            var tactical = dcm.GetCardsByType(CardType.Tactical);
            foreach (var tac in tactical)
            {
                if (dcm.DeckSize >= maxDeckSize) break;
                dcm.AddCard(tac);
            }
        }

        // ── Colony Card Priority Functions ──

        private int ColonyPriority_FoodFirst(ColonyCardDefinitionSO card)
        {
            switch (card.colonyEffect)
            {
                case ColonyEffect.FoodProduction:     return 100;
                case ColonyEffect.MushFoodProduction: return 95;
                case ColonyEffect.DoubleProduction:   return 90;
                case ColonyEffect.FogReveal:          return 80;
                case ColonyEffect.FogRevealHeroes:    return 75;
                case ColonyEffect.AllHeroCombatBuff:  return 70;
                case ColonyEffect.FirstCombatBuff:    return 65;
                case ColonyEffect.EquippedCombatBuff: return 60;
                case ColonyEffect.EquippedHPBuff:     return 55;
                case ColonyEffect.HealInjured:        return 50;
                case ColonyEffect.AllHeroMoveBuff:    return 45;
                case ColonyEffect.ForwardDeploy:      return 40;
                case ColonyEffect.DeployMoveBuff:     return 35;
                case ColonyEffect.ColonyDefenseWall:  return 30;
                case ColonyEffect.ColonyDefenseBonus: return 25;
                default: return 10;
            }
        }

        private int ColonyPriority_FoodOnly(ColonyCardDefinitionSO card)
        {
            switch (card.colonyEffect)
            {
                case ColonyEffect.FoodProduction:     return 100;
                case ColonyEffect.MushFoodProduction: return 95;
                case ColonyEffect.DoubleProduction:   return 90;
                default: return 1; // Only take food cards
            }
        }

        private int ColonyPriority_FogFirst(ColonyCardDefinitionSO card)
        {
            switch (card.colonyEffect)
            {
                case ColonyEffect.FogReveal:          return 100;
                case ColonyEffect.FogRevealHeroes:    return 95;
                case ColonyEffect.AllHeroMoveBuff:    return 90;
                case ColonyEffect.FoodProduction:     return 80;
                case ColonyEffect.MushFoodProduction: return 75;
                case ColonyEffect.DoubleProduction:   return 70;
                default: return 10;
            }
        }

        private int ColonyPriority_DefenseFirst(ColonyCardDefinitionSO card)
        {
            switch (card.colonyEffect)
            {
                case ColonyEffect.FoodProduction:     return 100;
                case ColonyEffect.MushFoodProduction: return 95;
                case ColonyEffect.DoubleProduction:   return 90;
                case ColonyEffect.ColonyDefenseWall:  return 85;
                case ColonyEffect.ColonyDefenseBonus: return 80;
                case ColonyEffect.EquippedHPBuff:     return 75;
                case ColonyEffect.HealInjured:        return 70;
                case ColonyEffect.AllHeroCombatBuff:  return 65;
                default: return 10;
            }
        }

        private int ColonyPriority_MoveFirst(ColonyCardDefinitionSO card)
        {
            switch (card.colonyEffect)
            {
                case ColonyEffect.AllHeroMoveBuff:    return 100;
                case ColonyEffect.ForwardDeploy:      return 95;
                case ColonyEffect.DeployMoveBuff:     return 90;
                case ColonyEffect.FoodProduction:     return 80;
                case ColonyEffect.MushFoodProduction: return 75;
                case ColonyEffect.AllHeroCombatBuff:  return 70;
                case ColonyEffect.FirstCombatBuff:    return 65;
                default: return 10;
            }
        }

        private IEnumerator ConfirmDeckAfterDelay(DeckConstructionManager dcm, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!batchMode) CaptureGameView("DeckConstruction");
            dcm.ConfirmDeck();
            state = AutoPlayState.InGameMap;
            sceneWaitTimer = batchMode ? 0.1f : 2.0f;
        }

        // ── GameMap Init ──────────────────────────────────────────────────

        private bool gameManagerReady;
        private int waitForGameManagerAttempts;

        private void HandleGameMapInit()
        {
            var gameManager = Object.FindAnyObjectByType<GameManager>();
            if (gameManager == null) { sceneWaitTimer = 0.5f; return; }
            if (!gameManager.IsInitialized)
            {
                waitForGameManagerAttempts++;
                if (waitForGameManagerAttempts > 20)
                {
                    Debug.LogError("[AutoPlayController] HandleGameMapInit: GameManager never initialized");
                    state = AutoPlayState.Done;
                    return;
                }
                sceneWaitTimer = 0.5f;
                return;
            }

            turnManager = Object.FindAnyObjectByType<TurnManager>();
            deckManager = Object.FindAnyObjectByType<DeckManager>();

            if (turnManager == null || deckManager == null)
            {
                Debug.LogError("[AutoPlayController] HandleGameMapInit: missing TurnManager or DeckManager");
                state = AutoPlayState.Done;
                return;
            }

            if (!batchMode)
            {
                VerifyUIElements("GameMap");
                CaptureGameView("GameMap");
            }

            state = AutoPlayState.PlayingTurns;
            phaseEndCooldown = batchMode ? 0.1f : 1.0f;
            Debug.Log($"[AutoPlayController] HandleGameMapInit: ready, strategy={StrategyNames[currentStrategyIndex]}");
        }

        private void HandleWaitForTurnManager()
        {
            if (turnManager != null && turnManager.RunActive)
            {
                state = AutoPlayState.PlayingTurns;
                phaseEndCooldown = 0;
            }
            else
            {
                sceneWaitTimer = 0.5f;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // ██ PLAYING TURNS — Strategy-specific phase handling
        // ══════════════════════════════════════════════════════════════════

        private void HandlePlayingTurns()
        {
            if (turnManager == null || !turnManager.RunActive)
            {
                state = AutoPlayState.WaitingForMainMenu;
                sceneWaitTimer = 2.0f;
                return;
            }

            int turn = turnManager.CurrentTurn;
            int phase = (int)turnManager.CurrentPhase;
            if (turn != lastLoggedTurn || phase != lastLoggedPhase)
            {
                Debug.Log($"[AutoPlayController] HandlePlayingTurns: turn={turn}, phase={turnManager.CurrentPhase}, " +
                          $"strategy={StrategyNames[currentStrategyIndex]}, deployed={turnManager.DeployedHeroes.Count}");

                if (!batchMode)
                {
                    if (turn == 1 && turnManager.CurrentPhase == GamePhase.Deploy)
                        pendingScreenshot = ("Turn1_Deploy", 0.5f);
                    else if (turn == 1 && turnManager.CurrentPhase == GamePhase.Gather)
                        CaptureGameView("Turn1_MapView");
                }

                lastLoggedTurn = turn;
                lastLoggedPhase = phase;
                phaseEndCooldown = 0;
            }

            if (!batchMode) UpdateHUD();

            if (!turnManager.WaitingForInput) return;

            if (phaseEndCooldown > 0)
            {
                phaseEndCooldown -= Time.unscaledDeltaTime;
                return;
            }

            switch (turnManager.CurrentPhase)
            {
                case GamePhase.ColonyDeploy: AutoPlayColonyDeployPhase(); break;
                case GamePhase.Deploy:  AutoPlayDeployPhase(); break;
                case GamePhase.Combat:  AutoPlayCombatPhase(); break;
                default:
                    turnManager.PlayerEndPhase();
                    phaseEndCooldown = batchMode ? 0.05f : 0.3f;
                    break;
            }
        }

        private int ColonyPriority_Adaptive(ColonyCardDefinitionSO card)
        {
            // Adaptive: prioritize food until food > deployed+1, then combat buffs
            var colonyGraph = ServiceLocator.Get<IColonyGraph>();
            int food = colonyGraph != null ? colonyGraph.CalculateFoodProduction() : 2;
            int deployed = turnManager != null ? turnManager.DeployedHeroes.Count : 0;

            if (food <= deployed + 1)
            {
                // Need more food
                return ColonyPriority_FoodFirst(card);
            }
            else
            {
                // Food is sufficient — prioritize combat buffs and healing
                switch (card.colonyEffect)
                {
                    case ColonyEffect.AllHeroCombatBuff:  return 100;
                    case ColonyEffect.FirstCombatBuff:    return 95;
                    case ColonyEffect.EquippedCombatBuff: return 90;
                    case ColonyEffect.HealInjured:        return 85;
                    case ColonyEffect.AllHeroMoveBuff:    return 80;
                    case ColonyEffect.FogReveal:          return 75;
                    case ColonyEffect.FogRevealHeroes:    return 70;
                    default: return 10;
                }
            }
        }

        // ── Deploy Phase ──────────────────────────────────────────────────

        private void AutoPlayColonyDeployPhase()
        {
            Debug.Log($"[AutoPlayController] AutoPlayColonyDeployPhase: strategy={StrategyNames[currentStrategyIndex]}");

            if (deckManager == null)
                deckManager = Object.FindAnyObjectByType<DeckManager>();

            var mapGraph = ServiceLocator.Get<MapGraph>();
            var colonyGraph = ServiceLocator.Get<IColonyGraph>();

            if (deckManager != null && deckManager.AvailableColonyCards.Count > 0)
            {
                if (colonyGraph != null && colonyGraph.CanPlayCard())
                {
                    System.Func<ColonyCardDefinitionSO, int> priorityFunc = currentStrategyIndex switch
                    {
                        1 => ColonyPriority_FoodOnly,
                        2 => ColonyPriority_FogFirst,
                        3 => ColonyPriority_DefenseFirst,
                        4 => ColonyPriority_MoveFirst,
                        5 => ColonyPriority_Adaptive,
                        _ => ColonyPriority_FoodFirst
                    };

                    ColonyCardDefinitionSO bestCard = null;
                    int bestPriority = -1;
                    foreach (var card in deckManager.AvailableColonyCards)
                    {
                        int priority = priorityFunc(card);
                        if (priority > bestPriority)
                        {
                            bestPriority = priority;
                            bestCard = card;
                        }
                    }

                    if (bestCard != null && mapGraph != null)
                    {
                        int targetNodeId = -1;
                        var emptyNodes = mapGraph.GetEmptyColonyNodes();
                        foreach (var emptyNode in emptyNodes)
                        {
                            foreach (var nid in emptyNode.neighborIds)
                            {
                                var neighbor = mapGraph.GetNode(nid);
                                if (neighbor != null && neighbor.IsColonyNode && neighbor.HasColonyCard)
                                {
                                    targetNodeId = emptyNode.nodeId;
                                    break;
                                }
                            }
                            if (targetNodeId >= 0) break;
                        }

                        if (targetNodeId >= 0)
                        {
                            Debug.Log($"[AutoPlayController] AutoPlayColonyDeployPhase: playing colony card '{bestCard.cardName}' " +
                                      $"(effect={bestCard.colonyEffect}, targetNode={targetNodeId}, priority={bestPriority})");
                            turnManager.PlayerPlayColonyCard(bestCard, targetNodeId);
                        }
                        else
                        {
                            Debug.LogWarning("[AutoPlayController] AutoPlayColonyDeployPhase: no valid colony node — skipping");
                        }
                    }
                }
            }

            turnManager.PlayerEndPhase();
            Debug.Log("[AutoPlayController] AutoPlayColonyDeployPhase: ended colony deploy phase");
        }

        private void AutoPlayDeployPhase()
        {
            Debug.Log($"[AutoPlayController] AutoPlayDeployPhase: strategy={StrategyNames[currentStrategyIndex]}");

            if (deckManager == null)
                deckManager = Object.FindAnyObjectByType<DeckManager>();

            var mapGraph = ServiceLocator.Get<MapGraph>();

            // ── Step 1: Retarget existing heroes ──
            if (mapGraph != null)
            {
                int retargeted = 0;
                foreach (var hero in turnManager.DeployedHeroes)
                {
                    if (hero.isInjured) continue;
                    if (hero.currentNodeId != hero.targetNodeId) continue;

                    int newTarget = ChooseNewTarget(hero, mapGraph);
                    if (newTarget >= 0 && newTarget != hero.targetNodeId)
                    {
                        turnManager.PlayerRetargetHero(hero.tokenId, newTarget);
                        retargeted++;
                    }
                }
                Debug.Log($"[AutoPlayController] AutoPlayDeployPhase: retargeted {retargeted} heroes");
            }

            // ── Step 2: Deploy new heroes (strategy-specific limits) ──
            if (deckManager != null)
            {
                var colonyGraph = ServiceLocator.Get<IColonyGraph>();
                int foodProduction = colonyGraph != null ? colonyGraph.CalculateFoodProduction() : 2;
                int alreadyDeployed = turnManager.DeployedHeroes.Count;
                int maxDeploy = GetMaxDeploy(foodProduction, alreadyDeployed);

                // Strategy 3 (Fortress): skip deployment turns 1-2
                if (currentStrategyIndex == 3 && turnManager.CurrentTurn < 3)
                {
                    maxDeploy = 0;
                    Debug.Log("[AutoPlayController] AutoPlayDeployPhase: FortressColony — skipping early deployment");
                }

                var availableHeroes = deckManager.AvailableHeroes;

                // Sort heroes based on strategy preference
                var sortedHeroes = SortHeroesForDeploy(availableHeroes);

                var targets = GetExplorationTargets(mapGraph);

                Debug.Log($"[AutoPlayController] AutoPlayDeployPhase: {targets.Count} targets, " +
                          $"{sortedHeroes.Count} heroes, food={foodProduction}, deployed={alreadyDeployed}, max={maxDeploy}");

                int deployCount = 0;

                // Group heroes onto fewer targets so they fight together.
                // Strategies 1,4 (mass deploy) spread across more targets; others concentrate.
                int groupSize = currentStrategyIndex switch
                {
                    1 => 3,  // ZergRush: groups of 3
                    4 => 3,  // PiperBeeline: groups of 3
                    _ => 2   // Default: pairs
                };
                int maxTargets = Mathf.Max(1, maxDeploy / groupSize);
                if (maxTargets > targets.Count && targets.Count > 0)
                    maxTargets = targets.Count;

                int targetIndex = 0;
                int heroesOnCurrentTarget = 0;
                foreach (var heroDef in sortedHeroes)
                {
                    if (deployCount >= maxDeploy) break;

                    int targetNodeId = targets.Count > 0
                        ? targets[targetIndex % targets.Count].nodeId
                        : 1;

                    // Equip based on strategy and hero role
                    CardDefinitionSO offensive = null, defensive = null, utility = null;
                    EquipHero(heroDef, ref offensive, ref defensive, ref utility);

                    turnManager.PlayerDeployHero(heroDef, targetNodeId, offensive, defensive, utility);
                    deployCount++;
                    heroesOnCurrentTarget++;

                    // Move to next target after filling current group
                    if (heroesOnCurrentTarget >= groupSize)
                    {
                        targetIndex++;
                        heroesOnCurrentTarget = 0;
                    }
                }
            }

            turnManager.PlayerEndPhase();
            phaseEndCooldown = batchMode ? 0.05f : 0.5f;
        }

        private int GetMaxDeploy(int foodProduction, int alreadyDeployed)
        {
            // Hard cap from balance config (default: 2 hero deploys per turn)
            var bc = BalanceConfigSO.Instance;
            return bc != null ? bc.GetMaxHeroDeploysPerTurn() : 2;
        }

        private List<CardDefinitionSO> SortHeroesForDeploy(IReadOnlyList<CardDefinitionSO> heroes)
        {
            switch (currentStrategyIndex)
            {
                case 2: // Speed Scouts — move priority, then carry
                    return heroes.OrderByDescending(h => h.move * 10 + h.carry).ToList();

                case 3: // Fortress Colony — HP priority, then combat
                    return heroes.OrderByDescending(h => h.hp * 10 + h.combat).ToList();

                case 4: // Piper Beeline — combat+move balanced
                    return heroes.OrderByDescending(h => h.combat + h.move).ToList();

                case 5: // Balanced Adaptive — initiative first
                    return heroes.OrderByDescending(h => h.initiative).ToList();

                default: // Colony First, Zerg Rush — combat priority
                    return heroes.OrderByDescending(h => h.combat).ToList();
            }
        }

        private void EquipHero(CardDefinitionSO heroDef, ref CardDefinitionSO offensive, ref CardDefinitionSO defensive, ref CardDefinitionSO utility)
        {
            if (deckManager == null) return;

            var availEquip = deckManager.AvailableEquipment.ToList();
            bool isGatherer = heroDef != null && (heroDef.heroRole == HeroRole.Gather ||
                              heroDef.carry >= 3 || heroDef.specialAbility == SpecialAbility.EfficientGather ||
                              heroDef.specialAbility == SpecialAbility.BulkHaul);

            Debug.Log($"[AutoPlayController] EquipHero: hero='{heroDef?.cardName ?? "unknown"}' " +
                      $"(role={heroDef?.heroRole}, carry={heroDef?.carry ?? 0}, isGatherer={isGatherer}, " +
                      $"availableEquip={availEquip.Count})");

            if (isGatherer)
            {
                // Gatherers: prioritize carry-boosting utility, then defensive, then offensive
                foreach (var eq in availEquip.OrderByDescending(e => e.equipmentSlot == EquipmentSlot.Utility ? e.effectValue1 : 0))
                {
                    if (eq.equipmentSlot == EquipmentSlot.Utility && utility == null)
                        utility = eq;
                    else if (eq.equipmentSlot == EquipmentSlot.Defensive && defensive == null)
                        defensive = eq;
                    else if (eq.equipmentSlot == EquipmentSlot.Offensive && offensive == null)
                        offensive = eq;
                }
            }
            else
            {
                // Fighters: prioritize offensive (highest combat bonus), then defensive, then utility
                foreach (var eq in availEquip.OrderByDescending(e => e.equipmentSlot == EquipmentSlot.Offensive ? e.effectValue1 : 0))
                {
                    if (eq.equipmentSlot == EquipmentSlot.Offensive && offensive == null)
                        offensive = eq;
                    else if (eq.equipmentSlot == EquipmentSlot.Defensive && defensive == null)
                        defensive = eq;
                    else if (eq.equipmentSlot == EquipmentSlot.Utility && utility == null)
                        utility = eq;
                }
            }

            Debug.Log($"[AutoPlayController] EquipHero: equipped " +
                      $"(offensive='{offensive?.cardName ?? "none"}', defensive='{defensive?.cardName ?? "none"}', " +
                      $"utility='{utility?.cardName ?? "none"}')");
        }

        // ── Combat Phase ──────────────────────────────────────────────────

        private void AutoPlayCombatPhase()
        {
            // Strategy 5 (Balanced Adaptive) attempts to use tactical cards
            if (currentStrategyIndex == 5 && deckManager != null)
            {
                var tacticals = deckManager.AvailableTactical;
                if (tacticals != null && tacticals.Count > 0)
                {
                    Debug.Log($"[AutoPlayController] AutoPlayCombatPhase: strategy 5 has {tacticals.Count} tactical cards available");
                    // Tactical card usage would go here when the API supports it
                }
            }

            turnManager.PlayerEndPhase();
            phaseEndCooldown = batchMode ? 0.05f : 0.5f;
        }

        // ══════════════════════════════════════════════════════════════════
        // ██ HERO TARGETING — Strategy-specific target selection
        // ══════════════════════════════════════════════════════════════════

        private int ChooseNewTarget(HeroToken hero, MapGraph mapGraph)
        {
            int colonyNodeId = mapGraph.ColonyNodeId;

            // All strategies: return to colony when carrying resources
            if (hero.TotalCarried > 0)
            {
                Debug.Log($"[AutoPlayController] ChooseNewTarget: hero carrying {hero.TotalCarried} — returning to colony " +
                          $"(tokenId={hero.tokenId}, name={hero.cardDef?.cardName ?? "unknown"})");
                return colonyNodeId;
            }

            // Gatherer heroes: prioritize resource-rich nodes before combat
            bool isGatherer = hero.cardDef != null && (hero.cardDef.heroRole == HeroRole.Gather ||
                              hero.cardDef.carry >= 3 || hero.cardDef.specialAbility == SpecialAbility.EfficientGather ||
                              hero.cardDef.specialAbility == SpecialAbility.BulkHaul);
            if (isGatherer && hero.EffectiveCarry > hero.TotalCarried)
            {
                int gatherTarget = ChooseTarget_Gather(hero, mapGraph);
                if (gatherTarget >= 0)
                {
                    Debug.Log($"[AutoPlayController] ChooseNewTarget: gatherer routing to resources " +
                              $"(tokenId={hero.tokenId}, name={hero.cardDef?.cardName ?? "unknown"}, " +
                              $"targetNode={gatherTarget}, carry={hero.TotalCarried}/{hero.EffectiveCarry})");
                    return gatherTarget;
                }
            }

            switch (currentStrategyIndex)
            {
                case 2:  return ChooseTarget_SpeedScouts(hero, mapGraph);
                case 3:  return ChooseTarget_FortressColony(hero, mapGraph);
                case 4:  return ChooseTarget_PiperBeeline(hero, mapGraph);
                case 5:  return ChooseTarget_Adaptive(hero, mapGraph);
                default: return ChooseTarget_Default(hero, mapGraph); // 0, 1
            }
        }

        /// <summary>
        /// Targets closest visible node with resources (food-heavy preferred). Returns -1 if none found.
        /// </summary>
        private int ChooseTarget_Gather(HeroToken hero, MapGraph mapGraph)
        {
            var allNodes = mapGraph.GetAllNodes();

            // Find visible nodes with resources, preferring food-rich nodes
            var resourceNodes = allNodes
                .Where(n => n.fogState == FogState.Visible && n.TotalResources() > 0 &&
                       n.nodeId != hero.currentNodeId && n.enemyTokenIds.Count == 0)
                .ToList();

            if (resourceNodes.Count == 0)
            {
                Debug.Log($"[AutoPlayController] ChooseTarget_Gather: no safe resource nodes found " +
                          $"(tokenId={hero.tokenId})");
                return -1;
            }

            // Score nodes: food value * 3 + other resources, penalize by distance
            int bestNode = -1;
            float bestScore = -1f;
            foreach (var node in resourceNodes)
            {
                int foodOnNode = node.resources.ContainsKey(ResourceType.Food) ? node.resources[ResourceType.Food] : 0;
                int totalRes = node.TotalResources();
                int dist = mapGraph.GetDistance(hero.currentNodeId, node.nodeId);
                if (dist < 0) dist = 10; // unreachable fallback
                float score = (foodOnNode * 3f + totalRes) / Mathf.Max(1, dist);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestNode = node.nodeId;
                }
            }

            Debug.Log($"[AutoPlayController] ChooseTarget_Gather: best resource node " +
                      $"(tokenId={hero.tokenId}, targetNode={bestNode}, score={bestScore:F1})");
            return bestNode;
        }

        // ── Default targeting (strategies 0, 1): enemies > unvisited > furthest ──
        private int ChooseTarget_Default(HeroToken hero, MapGraph mapGraph)
        {
            var allNodes = mapGraph.GetAllNodes();
            int colonyNodeId = mapGraph.ColonyNodeId;

            // Closest known enemy
            var enemyNodes = allNodes
                .Where(n => n.fogState == FogState.Visible && n.enemyTokenIds.Count > 0 && n.nodeId != hero.currentNodeId)
                .ToList();
            if (enemyNodes.Count > 0)
            {
                int bestNode = -1, bestDist = int.MaxValue;
                foreach (var node in enemyNodes)
                {
                    int dist = mapGraph.GetDistance(hero.currentNodeId, node.nodeId);
                    if (dist >= 0 && dist < bestDist) { bestDist = dist; bestNode = node.nodeId; }
                }
                if (bestNode >= 0) return bestNode;
            }

            // Furthest unvisited
            var unvisited = allNodes
                .Where(n => !n.visited && n.nodeId != colonyNodeId && n.nodeId != hero.currentNodeId)
                .ToList();
            if (unvisited.Count > 0)
            {
                int bestNode = -1, bestDist = -1;
                foreach (var node in unvisited)
                {
                    int dist = mapGraph.GetDistance(colonyNodeId, node.nodeId);
                    if (dist > bestDist) { bestDist = dist; bestNode = node.nodeId; }
                }
                if (bestNode >= 0) return bestNode;
            }

            // Any non-colony, furthest from colony
            var any = allNodes
                .Where(n => n.nodeId != colonyNodeId && n.nodeId != hero.currentNodeId)
                .OrderByDescending(n => mapGraph.GetDistance(colonyNodeId, n.nodeId))
                .FirstOrDefault();
            return any?.nodeId ?? -1;
        }

        // ── Speed Scouts: avoid enemies, explore unvisited ──
        private int ChooseTarget_SpeedScouts(HeroToken hero, MapGraph mapGraph)
        {
            var allNodes = mapGraph.GetAllNodes();
            int colonyNodeId = mapGraph.ColonyNodeId;

            // Furthest unvisited node (avoid enemy nodes)
            var unvisited = allNodes
                .Where(n => !n.visited && n.nodeId != colonyNodeId && n.nodeId != hero.currentNodeId)
                .Where(n => !(n.fogState == FogState.Visible && n.enemyTokenIds.Count > 0)) // Avoid known enemies
                .ToList();
            if (unvisited.Count > 0)
            {
                int bestNode = -1, bestDist = -1;
                foreach (var node in unvisited)
                {
                    int dist = mapGraph.GetDistance(colonyNodeId, node.nodeId);
                    if (dist > bestDist) { bestDist = dist; bestNode = node.nodeId; }
                }
                if (bestNode >= 0) return bestNode;
            }

            // If all unvisited have enemies, target the closest unvisited anyway
            var anyUnvisited = allNodes
                .Where(n => !n.visited && n.nodeId != colonyNodeId && n.nodeId != hero.currentNodeId)
                .OrderBy(n => mapGraph.GetDistance(hero.currentNodeId, n.nodeId))
                .FirstOrDefault();
            if (anyUnvisited != null) return anyUnvisited.nodeId;

            // All visited — return to colony
            return colonyNodeId;
        }

        // ── Fortress Colony: stay close, never push deep ──
        private int ChooseTarget_FortressColony(HeroToken hero, MapGraph mapGraph)
        {
            var allNodes = mapGraph.GetAllNodes();
            int colonyNodeId = mapGraph.ColonyNodeId;

            // Closest unvisited node (never push deep)
            var unvisited = allNodes
                .Where(n => !n.visited && n.nodeId != colonyNodeId && n.nodeId != hero.currentNodeId)
                .ToList();
            if (unvisited.Count > 0)
            {
                int bestNode = -1, bestDist = int.MaxValue;
                foreach (var node in unvisited)
                {
                    int dist = mapGraph.GetDistance(hero.currentNodeId, node.nodeId);
                    if (dist >= 0 && dist < bestDist) { bestDist = dist; bestNode = node.nodeId; }
                }
                if (bestNode >= 0) return bestNode;
            }

            // All visited — closest known enemy
            var enemyNodes = allNodes
                .Where(n => n.fogState == FogState.Visible && n.enemyTokenIds.Count > 0 && n.nodeId != hero.currentNodeId)
                .OrderBy(n => mapGraph.GetDistance(hero.currentNodeId, n.nodeId))
                .FirstOrDefault();
            if (enemyNodes != null) return enemyNodes.nodeId;

            return colonyNodeId; // Fall back to colony
        }

        // ── Piper Beeline: always target Pied Piper ──
        private int ChooseTarget_PiperBeeline(HeroToken hero, MapGraph mapGraph)
        {
            int piperNode = mapGraph.PiedPiperNodeId;
            if (piperNode >= 0 && piperNode != hero.currentNodeId)
                return piperNode;

            // If at piper node or piper not set, fall back to default
            return ChooseTarget_Default(hero, mapGraph);
        }

        // ── Balanced Adaptive: changes priorities by turn ──
        private int ChooseTarget_Adaptive(HeroToken hero, MapGraph mapGraph)
        {
            int turn = turnManager != null ? turnManager.CurrentTurn : 1;

            if (turn <= 5)
            {
                // Early game: explore — target closest unvisited
                var allNodes = mapGraph.GetAllNodes();
                int colonyNodeId = mapGraph.ColonyNodeId;
                var unvisited = allNodes
                    .Where(n => !n.visited && n.nodeId != colonyNodeId && n.nodeId != hero.currentNodeId)
                    .OrderBy(n => mapGraph.GetDistance(hero.currentNodeId, n.nodeId))
                    .FirstOrDefault();
                if (unvisited != null) return unvisited.nodeId;
                return ChooseTarget_Default(hero, mapGraph);
            }
            else if (turn <= 10)
            {
                // Mid game: fight enemies
                return ChooseTarget_Default(hero, mapGraph);
            }
            else
            {
                // Late game: rush Pied Piper
                return ChooseTarget_PiperBeeline(hero, mapGraph);
            }
        }

        // ── Exploration Targets for deployment ──

        private List<MapNode> GetExplorationTargets(MapGraph mapGraph)
        {
            if (mapGraph == null)
                return new List<MapNode>();

            var allNodes = mapGraph.GetAllNodes();
            int colonyNodeId = mapGraph.ColonyNodeId;

            // Strategy 4: all heroes target Pied Piper path
            if (currentStrategyIndex == 4)
            {
                int piperNode = mapGraph.PiedPiperNodeId;
                if (piperNode >= 0)
                {
                    var piperPath = mapGraph.ShortestPath(colonyNodeId, piperNode);
                    if (piperPath.Count > 1)
                    {
                        // Target nodes along the path to Pied Piper
                        var pathNodes = piperPath.Skip(1)
                            .Select(id => mapGraph.GetNode(id))
                            .Where(n => n != null)
                            .ToList();
                        if (pathNodes.Count > 0)
                        {
                            Debug.Log($"[AutoPlayController] GetExplorationTargets: PiperBeeline — {pathNodes.Count} path nodes");
                            return pathNodes;
                        }
                    }
                }
            }

            // Known enemy nodes (visible only — fog hides tokens)
            var enemyNodes = allNodes
                .Where(n => n.fogState == FogState.Visible && n.enemyTokenIds.Count > 0)
                .OrderBy(n => mapGraph.GetDistance(colonyNodeId, n.nodeId))
                .ToList();

            // All nodes are visible on the map (fog only hides tokens)
            var unvisited = allNodes
                .Where(n => !n.visited && n.nodeId != colonyNodeId)
                .OrderByDescending(n => mapGraph.GetDistance(colonyNodeId, n.nodeId))
                .ToList();

            var anyNode = allNodes
                .Where(n => n.nodeId != colonyNodeId)
                .OrderByDescending(n => mapGraph.GetDistance(colonyNodeId, n.nodeId))
                .ToList();

            var combined = new List<MapNode>();
            combined.AddRange(enemyNodes);
            foreach (var n in unvisited)
            {
                if (!combined.Any(c => c.nodeId == n.nodeId))
                    combined.Add(n);
            }
            foreach (var n in anyNode)
            {
                if (!combined.Any(c => c.nodeId == n.nodeId))
                    combined.Add(n);
            }

            if (combined.Count == 0)
                combined = allNodes.Where(n => n.nodeId != colonyNodeId).ToList();

            Debug.Log($"[AutoPlayController] GetExplorationTargets: {combined.Count} targets " +
                      $"(enemy={enemyNodes.Count}, unvisited={unvisited.Count})");

            return combined;
        }

        // ══════════════════════════════════════════════════════════════════
        // ██ RUN RESULT — Record stats and cycle strategies
        // ══════════════════════════════════════════════════════════════════

        private void HandleRunResult()
        {
            var rm = RunManager.Instance;
            if (rm == null)
            {
                Debug.LogError("[AutoPlayController] HandleRunResult: RunManager is null");
                state = AutoPlayState.Done;
                return;
            }

            batchRunCount++;

            // Determine strategy for THIS run
            if (strategyIndex >= 0)
                currentStrategyIndex = strategyIndex; // Fixed strategy
            else
                currentStrategyIndex = (batchRunCount - 1) % STRATEGY_COUNT; // Cycle through all

            bool victory = rm.CurrentRunState == RunState.RunComplete;
            int turnsUsed = rm.CurrentTurn;
            int totalHeroes = rm.ConstructedDeck.Count(c => c.cardType == CardType.Hero);

            var result = new BatchRunResult
            {
                runNumber = batchRunCount,
                strategy = currentStrategyIndex,
                strategyName = StrategyNames[currentStrategyIndex],
                victory = victory,
                turnsUsed = turnsUsed,
                enemiesDefeated = rm.TotalEnemiesDefeated,
                zoneBossesDefeated = rm.ZoneBossesDefeated,
                piedPiperDefeated = rm.PiedPiperDefeated,
                totalResourcesGathered = rm.TotalResourcesGathered,
                colonyCardsPlayed = rm.ColonyCardsPlayed,
                heroesNeverInjured = totalHeroes,
                totalHeroes = totalHeroes,
                deckSize = rm.ConstructedDeck.Count,
                colonyDeckSize = rm.ConstructedColonyDeck.Count,
                foodStockpile = rm.FoodStockpile,
                materialsStockpile = rm.MaterialsStockpile,
                currencyStockpile = rm.CurrencyStockpile,
                score = 0,
                randomSeed = rm.RandomSeed
            };

            result.score = (victory ? 1000 : 0)
                + Mathf.Max(0, (15 - turnsUsed) * 50)
                + Mathf.Max(0, (30 - (result.deckSize + result.colonyDeckSize)) * 20)
                + result.totalResourcesGathered * 5
                + result.enemiesDefeated * 10
                + result.zoneBossesDefeated * 100
                + (result.piedPiperDefeated ? 500 : 0)
                + result.colonyCardsPlayed * 15;

            batchResults.Add(result);

            // Advance strategy for NEXT run
            if (strategyIndex < 0)
                currentStrategyIndex = batchRunCount % STRATEGY_COUNT;

            // Log milestone runs
            string runMsg = $"[AutoPlayController] HandleRunResult: run #{batchRunCount}/{maxBatchRuns} [{result.strategyName}] — " +
                      $"victory={victory}, turns={turnsUsed}, enemies={result.enemiesDefeated}, " +
                      $"bosses={result.zoneBossesDefeated}, resources={result.totalResourcesGathered}, score={result.score}";
            if (batchMode && _originalLogHandler != null && (batchRunCount % 50 == 0 || batchRunCount <= 6 || batchRunCount == maxBatchRuns))
            {
                _originalLogHandler.LogFormat(LogType.Log, null, "{0}", runMsg);
            }
            else if (!batchMode)
            {
                Debug.Log(runMsg);
            }

            if (batchMode && batchRunCount < maxBatchRuns)
            {
                state = AutoPlayState.WaitingForMainMenu;
                rm.ReturnToMainMenu();
                return;
            }

            if (batchMode)
                WriteBatchResults();
            else
            {
                VerifyUIElements("RunResult");
                CaptureGameView("RunResult");
            }

            state = AutoPlayState.Done;

            if (_originalLogHandler != null)
            {
                Debug.unityLogger.logHandler = _originalLogHandler;
                _originalLogHandler = null;
            }
            Debug.Log("[AutoPlayController] HandleRunResult: auto-play complete");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // ══════════════════════════════════════════════════════════════════
        // ██ BATCH RESULTS OUTPUT — Per-strategy breakdown
        // ══════════════════════════════════════════════════════════════════

        private void WriteBatchResults()
        {
            string dir = System.IO.Path.Combine(Application.dataPath, "..", "TestReports");
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            string filePath = System.IO.Path.Combine(dir, "batch_results.csv");

            var sb = new StringBuilder();
            sb.AppendLine("Run,Strategy,StrategyName,Victory,Turns,EnemiesDefeated,ZoneBosses,PiperDefeated," +
                          "ResourcesGathered,ColonyCardsPlayed,HeroesNeverInjured,TotalHeroes," +
                          "DeckSize,ColonyDeckSize,Food,Materials,Currency,Score,Seed");

            foreach (var r in batchResults)
            {
                sb.AppendLine($"{r.runNumber},{r.strategy},{r.strategyName},{r.victory},{r.turnsUsed}," +
                              $"{r.enemiesDefeated},{r.zoneBossesDefeated},{r.piedPiperDefeated}," +
                              $"{r.totalResourcesGathered},{r.colonyCardsPlayed},{r.heroesNeverInjured}," +
                              $"{r.totalHeroes},{r.deckSize},{r.colonyDeckSize},{r.foodStockpile}," +
                              $"{r.materialsStockpile},{r.currencyStockpile},{r.score},{r.randomSeed}");
            }

            System.IO.File.WriteAllText(filePath, sb.ToString());

            // Restore logging for summary
            if (_originalLogHandler != null)
            {
                Debug.unityLogger.logHandler = _originalLogHandler;
                _originalLogHandler = null;
            }

            // Per-strategy summary
            Debug.Log($"[AutoPlayController] ═══════════════ BATCH RESULTS SUMMARY ═══════════════");
            Debug.Log($"[AutoPlayController] Total runs: {batchResults.Count}");

            for (int s = 0; s < STRATEGY_COUNT; s++)
            {
                var stratResults = batchResults.Where(r => r.strategy == s).ToList();
                if (stratResults.Count == 0) continue;

                int wins = stratResults.Count(r => r.victory);
                float avgTurns = (float)stratResults.Sum(r => r.turnsUsed) / stratResults.Count;
                float avgEnemies = (float)stratResults.Sum(r => r.enemiesDefeated) / stratResults.Count;
                float avgResources = (float)stratResults.Sum(r => r.totalResourcesGathered) / stratResults.Count;
                float avgScore = (float)stratResults.Sum(r => r.score) / stratResults.Count;
                int bosses = stratResults.Sum(r => r.zoneBossesDefeated);
                int pipers = stratResults.Count(r => r.piedPiperDefeated);

                Debug.Log($"[AutoPlayController]  [{s}] {StrategyNames[s]}: " +
                          $"runs={stratResults.Count}, wins={wins} ({100f * wins / stratResults.Count:F1}%), " +
                          $"avgTurns={avgTurns:F1}, avgEnemies={avgEnemies:F1}, " +
                          $"avgResources={avgResources:F1}, bosses={bosses}, pipers={pipers}, " +
                          $"avgScore={avgScore:F0}");
            }

            Debug.Log($"[AutoPlayController] Results written to: {filePath}");
            Debug.Log($"[AutoPlayController] ═══════════════════════════════════════════════════");

            // Also write per-strategy summary to a text file
            WriteStrategySummary(dir);
        }

        private void WriteStrategySummary(string dir)
        {
            var sb = new StringBuilder();
            sb.AppendLine("MULTI-STRATEGY BATCH SIMULATION REPORT");
            sb.AppendLine($"Date: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total runs: {batchResults.Count}");
            sb.AppendLine(new string('=', 80));
            sb.AppendLine();

            for (int s = 0; s < STRATEGY_COUNT; s++)
            {
                var results = batchResults.Where(r => r.strategy == s).ToList();
                if (results.Count == 0) continue;

                int wins = results.Count(r => r.victory);
                var turns = results.Select(r => r.turnsUsed).ToList();
                var enemies = results.Select(r => r.enemiesDefeated).ToList();
                var resources = results.Select(r => r.totalResourcesGathered).ToList();
                var scores = results.Select(r => r.score).ToList();

                sb.AppendLine($"Strategy {s}: {StrategyNames[s]}");
                sb.AppendLine(new string('-', 50));
                sb.AppendLine($"  Runs:          {results.Count}");
                sb.AppendLine($"  Win rate:      {100f * wins / results.Count:F1}% ({wins}/{results.Count})");
                sb.AppendLine($"  Turns:         avg={turns.Average():F1}, min={turns.Min()}, max={turns.Max()}, median={turns.OrderBy(t => t).ElementAt(turns.Count / 2)}");
                sb.AppendLine($"  Enemies:       avg={enemies.Average():F1}, min={enemies.Min()}, max={enemies.Max()}, total={enemies.Sum()}");
                sb.AppendLine($"  Zone bosses:   {results.Sum(r => r.zoneBossesDefeated)} ({results.Count(r => r.zoneBossesDefeated > 0)} games)");
                sb.AppendLine($"  Pied Piper:    {results.Count(r => r.piedPiperDefeated)} defeats");
                sb.AppendLine($"  Resources:     avg={resources.Average():F1}");
                sb.AppendLine($"  Colony cards:  avg={results.Average(r => r.colonyCardsPlayed):F1}");
                sb.AppendLine($"  Heroes:        avg={results.Average(r => r.totalHeroes):F1}");
                sb.AppendLine($"  Deck size:     avg={results.Average(r => r.deckSize):F1}+{results.Average(r => r.colonyDeckSize):F1} colony");
                sb.AppendLine($"  Score:         avg={scores.Average():F0}, min={scores.Min()}, max={scores.Max()}");
                sb.AppendLine();
            }

            // Comparison table
            sb.AppendLine("STRATEGY COMPARISON TABLE");
            sb.AppendLine(new string('=', 80));
            sb.AppendLine($"{"Strategy",-20} {"Runs",5} {"Win%",6} {"AvgTrn",7} {"AvgEnm",7} {"Bosses",7} {"AvgRes",7} {"AvgScr",7}");
            sb.AppendLine(new string('-', 80));

            for (int s = 0; s < STRATEGY_COUNT; s++)
            {
                var results = batchResults.Where(r => r.strategy == s).ToList();
                if (results.Count == 0) continue;

                int wins = results.Count(r => r.victory);
                sb.AppendLine($"{StrategyNames[s],-20} {results.Count,5} {100f * wins / results.Count,5:F1}% " +
                              $"{results.Average(r => r.turnsUsed),7:F1} " +
                              $"{results.Average(r => r.enemiesDefeated),7:F1} " +
                              $"{results.Sum(r => r.zoneBossesDefeated),7} " +
                              $"{results.Average(r => r.totalResourcesGathered),7:F1} " +
                              $"{results.Average(r => r.score),7:F0}");
            }

            string summaryPath = System.IO.Path.Combine(dir, "strategy_comparison.txt");
            System.IO.File.WriteAllText(summaryPath, sb.ToString());
        }

        // ── UI Helpers ────────────────────────────────────────────────────

        private void UpdateHUD()
        {
            var hudManager = Object.FindAnyObjectByType<HUDManager>();
            if (hudManager == null) return;

            var resourceManager = Object.FindAnyObjectByType<ResourceManager>();
            if (turnManager != null)
            {
                hudManager.UpdateTurnDisplay(turnManager.CurrentTurn, turnManager.CurrentPhase);
                hudManager.UpdateHeroCount(turnManager.DeployedHeroes.Count, turnManager.AllHeroes.Count);
            }
            if (resourceManager != null)
            {
                hudManager.UpdateResources(
                    resourceManager.FoodStockpile,
                    resourceManager.MaterialsStockpile,
                    resourceManager.CurrencyStockpile
                );
            }
        }

        private void VerifyUIElements(string sceneName)
        {
            Debug.Log($"[AutoPlayController] VerifyUIElements: checking UI in scene '{sceneName}'");
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Debug.Log($"[AutoPlayController] VerifyUIElements: found {canvases.Length} Canvas objects");
            foreach (var canvas in canvases)
            {
                Debug.Log($"[AutoPlayController] VerifyUIElements: Canvas '{canvas.gameObject.name}' " +
                          $"(renderMode={canvas.renderMode}, sortingOrder={canvas.sortingOrder}, " +
                          $"enabled={canvas.enabled}, activeInHierarchy={canvas.gameObject.activeInHierarchy})");
                int childCount = canvas.transform.childCount;
                Debug.Log($"[AutoPlayController] VerifyUIElements: Canvas '{canvas.gameObject.name}' has {childCount} child elements");
            }

            var tmpTexts = Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None);
            int visibleTextCount = tmpTexts.Count(t => t.gameObject.activeInHierarchy && !string.IsNullOrEmpty(t.text));
            Debug.Log($"[AutoPlayController] VerifyUIElements: total visible TextMeshProUGUI elements={visibleTextCount}");

            var buttons = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
            int activeButtonCount = buttons.Count(b => b.gameObject.activeInHierarchy);
            Debug.Log($"[AutoPlayController] VerifyUIElements: total active buttons={activeButtonCount}");
        }

        private void OnDestroy()
        {
            if (_originalLogHandler != null)
            {
                Debug.unityLogger.logHandler = _originalLogHandler;
                _originalLogHandler = null;
            }
            Debug.unityLogger.logEnabled = true;
            Time.timeScale = 1f;
            QualitySettings.vSyncCount = 1;
            IsBatchMode = false;
            Debug.Log("[AutoPlayController] OnDestroy: cleaning up, logging restored");
        }
    }
}
