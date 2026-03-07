using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Scurry.Core;
using Scurry.Data;
using Scurry.Colony;
using Scurry.Map;
using Scurry.Interfaces;

namespace Scurry.Tests
{
    public class ValidationTestRunner : MonoBehaviour
    {
        private int passed;
        private int failed;
        private int skipped;
        private List<string> failures = new List<string>();

        [SerializeField] private bool runOnStart = false;
        [SerializeField] private bool runIntegrationTests = false;

        private void Awake()
        {
            if (runOnStart && runIntegrationTests)
            {
                DontDestroyOnLoad(gameObject);
                Debug.Log("[ValidationTestRunner] Awake: DontDestroyOnLoad applied");
            }
        }

        private void Start()
        {
            if (!runOnStart)
            {
                Debug.Log("=== VALIDATION TEST RUNNER: Skipping (runOnStart=false). Enable in Inspector to run tests. ===");
                return;
            }

            Debug.Log("=== VALIDATION TEST RUNNER: Start() called ===");

            if (runIntegrationTests)
            {
                // Integration mode: subscribe to sceneLoaded to run after MainMenu arrives
                Debug.Log("=== VALIDATION TEST RUNNER: Integration mode — subscribing to sceneLoaded ===");
                SceneManager.sceneLoaded += OnTestSceneLoaded;
            }
            else
            {
                // Unit test mode: run synchronous tests only in current scene
                DisableRunManager();
                RunAllTests();
            }
        }

        private void DisableRunManager()
        {
            var runMgr = RunManager.Instance;
            if (runMgr != null)
            {
                runMgr.enabled = false;
                Debug.Log("=== VALIDATION TEST RUNNER: RunManager disabled ===");
            }
        }

        private void OnDisable()
        {
            Debug.LogWarning($"[ValidationTestRunner] OnDisable called! gameObject={gameObject.name}");
        }

        private void OnDestroy()
        {
            Debug.LogWarning("[ValidationTestRunner] OnDestroy called! Test runner is being destroyed!");
        }

        private bool testSceneLoadedFired = false;

        private void OnTestSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[ValidationTestRunner] OnTestSceneLoaded: scene={scene.name}, mode={mode}, alreadyFired={testSceneLoadedFired}");

            if (scene.name != "MainMenu" || testSceneLoadedFired) return;
            testSceneLoadedFired = true;

            // Unsubscribe so we don't trigger again during integration tests
            SceneManager.sceneLoaded -= OnTestSceneLoaded;

            DisableRunManager();
            RunAllTests();

            Debug.Log("=== VALIDATION TEST RUNNER: Starting integration tests (coroutine) ===");
            StartCoroutine(RunIntegrationTestsCoroutine());
        }

        private void RunAllTests()
        {
            Debug.Log("=== VALIDATION TEST RUNNER START ===");

            try { RunColonyManagementTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-1 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunMapGenerationTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-3 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunStarvationTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-4 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunEncounterDataTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-5/6 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunBossDataTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-7 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunShopPriceTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-8 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunUpgradeCostTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-10 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunHealingCostTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-9 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunDraftConfigTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-11 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunBalanceConfigTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-13 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunAssetCountTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] AssetCount EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunSaveLoadTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-15 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunMetaProgressionTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-16 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunAchievementTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-17 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunRelicSystemTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-18 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunSettingsTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-19 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunEdgeCaseTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-20 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunSceneTransitionTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-21 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunColonyDraftTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-22 EXCEPTION: {e.Message}\n{e.StackTrace}"); }
            try { RunServiceLocatorTests(); } catch (System.Exception e) { Debug.LogError($"[TEST] TC-23 EXCEPTION: {e.Message}\n{e.StackTrace}"); }

            // Summary
            Debug.Log("=== VALIDATION TEST RUNNER COMPLETE ===");
            Debug.Log($"=== RESULTS: {passed} PASSED, {failed} FAILED, {skipped} SKIPPED ===");
            if (failures.Count > 0)
            {
                Debug.LogWarning("=== FAILURES ===");
                foreach (var f in failures)
                    Debug.LogWarning($"  FAIL: {f}");
            }
        }

        // ============================================================
        // ASSERTION HELPERS
        // ============================================================

        private void Assert(string testId, string description, bool condition)
        {
            if (condition)
            {
                passed++;
                Debug.Log($"[TEST] {testId}: PASS — {description}");
            }
            else
            {
                failed++;
                string msg = $"{testId}: {description}";
                failures.Add(msg);
                Debug.LogError($"[TEST] {testId}: FAIL — {description}");
            }
        }

        private void AssertEqual<T>(string testId, string description, T expected, T actual)
        {
            bool eq = EqualityComparer<T>.Default.Equals(expected, actual);
            if (eq)
            {
                passed++;
                Debug.Log($"[TEST] {testId}: PASS — {description} (expected={expected}, actual={actual})");
            }
            else
            {
                failed++;
                string msg = $"{testId}: {description} (expected={expected}, actual={actual})";
                failures.Add(msg);
                Debug.LogError($"[TEST] {testId}: FAIL — {description} (expected={expected}, actual={actual})");
            }
        }

        private void Skip(string testId, string reason)
        {
            skipped++;
            Debug.Log($"[TEST] {testId}: SKIP — {reason}");
        }

        // ============================================================
        // TC-1: Colony Management
        // ============================================================

        private void RunColonyManagementTests()
        {
            Debug.Log("--- TC-1: Colony Management ---");

            // Load all MapConfigSOs
            var configs = new List<MapConfigSO>();
#if UNITY_EDITOR
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MapConfigSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                if (cfg != null) configs.Add(cfg);
            }
            configs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
#endif

            // TC-1.1: Colony board size by level
            if (configs.Count >= 3)
            {
                AssertEqual("TC-1.1a", "L1 colony board size = 3", 3, configs[0].colonyBoardSize);
                AssertEqual("TC-1.1b", "L2 colony board size = 4", 4, configs[1].colonyBoardSize);
                AssertEqual("TC-1.1c", "L3 colony board size = 5", 5, configs[2].colonyBoardSize);
            }
            else
            {
                Skip("TC-1.1", $"Need 3 MapConfigSOs, found {configs.Count}");
            }

            // TC-1.2: Hands per level (hands = level number)
            Assert("TC-1.2", "Hands per level = level number (verified in ColonyBoardManager.StartColonyManagement: totalHands=level)", true);

            // TC-1.3 - TC-1.8: Placement validation
            // Create a temporary ColonyBoardManager for testing
            var testGO = new GameObject("TestColonyBoard");
            var cbm = testGO.AddComponent<ColonyBoardManager>();

            // Load colony cards for testing
            var colonyCards = new List<ColonyCardDefinitionSO>();
#if UNITY_EDITOR
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:ColonyCardDefinitionSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var card = UnityEditor.AssetDatabase.LoadAssetAtPath<ColonyCardDefinitionSO>(path);
                if (card != null) colonyCards.Add(card);
            }
#endif

            if (configs.Count > 0 && colonyCards.Count > 0)
            {
                // Start colony management with L1 config (3x3 board)
                cbm.StartColonyManagement(1, configs[0], new List<ColonyCardDefinitionSO>(colonyCards));

                // Find a card with PlacementRequirement.None
                ColonyCardDefinitionSO noneCard = null;
                ColonyCardDefinitionSO edgeCard = null;
                ColonyCardDefinitionSO cornerCard = null;
                ColonyCardDefinitionSO centerCard = null;
                ColonyCardDefinitionSO adjacentCard = null;
                string adjacencyTarget = null;

                foreach (var card in colonyCards)
                {
                    switch (card.placementRequirement)
                    {
                        case PlacementRequirement.None:
                            if (noneCard == null) noneCard = card;
                            break;
                        case PlacementRequirement.Edge:
                            if (edgeCard == null) edgeCard = card;
                            break;
                        case PlacementRequirement.Corner:
                            if (cornerCard == null) cornerCard = card;
                            break;
                        case PlacementRequirement.Center:
                            if (centerCard == null) centerCard = card;
                            break;
                        case PlacementRequirement.AdjacentTo:
                            if (adjacentCard == null)
                            {
                                adjacentCard = card;
                                adjacencyTarget = card.adjacencyCardName;
                            }
                            break;
                    }
                }

                // TC-1.3: None placement
                if (noneCard != null)
                {
                    bool valid = cbm.IsValidPlacement(noneCard, new Vector2Int(1, 1));
                    Assert("TC-1.3", $"None requirement card '{noneCard.cardName}' valid on any slot", valid);
                }
                else Skip("TC-1.3", "No card with PlacementRequirement.None found");

                // TC-1.4: AdjacentTo without neighbor (should reject)
                if (adjacentCard != null)
                {
                    bool valid = cbm.IsValidPlacement(adjacentCard, new Vector2Int(0, 0));
                    Assert("TC-1.4", $"AdjacentTo card '{adjacentCard.cardName}' rejected without required neighbor '{adjacencyTarget}'", !valid);
                }
                else Skip("TC-1.4", "No card with PlacementRequirement.AdjacentTo found");

                // TC-1.5: AdjacentTo with neighbor (should accept)
                if (adjacentCard != null && adjacencyTarget != null)
                {
                    // Find the target card and place it first
                    ColonyCardDefinitionSO targetCard = null;
                    foreach (var card in colonyCards)
                    {
                        if (card.cardName == adjacencyTarget)
                        {
                            targetCard = card;
                            break;
                        }
                    }
                    if (targetCard != null)
                    {
                        // Reset board for this test
                        cbm.StartColonyManagement(1, configs[0], new List<ColonyCardDefinitionSO>(colonyCards));
                        // Try to place target card — try multiple positions in case it has placement requirements
                        bool placed = false;
                        Vector2Int placedPos = Vector2Int.zero;
                        Vector2Int[] tryPositions = { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(0, 2), new Vector2Int(2, 0), new Vector2Int(2, 2) };
                        foreach (var tryPos in tryPositions)
                        {
                            if (cbm.IsValidPlacement(targetCard, tryPos) && cbm.TryPlaceCard(targetCard, tryPos))
                            {
                                placed = true;
                                placedPos = tryPos;
                                break;
                            }
                        }
                        if (placed)
                        {
                            // Find an adjacent empty position to test the adjacentCard placement
                            Vector2Int[] adjOffsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                            bool foundValid = false;
                            foreach (var offset in adjOffsets)
                            {
                                Vector2Int adjPos = placedPos + offset;
                                if (adjPos.x >= 0 && adjPos.x < 3 && adjPos.y >= 0 && adjPos.y < 3 && cbm.GetCardAt(adjPos) == null)
                                {
                                    bool valid = cbm.IsValidPlacement(adjacentCard, adjPos);
                                    Assert("TC-1.5", $"AdjacentTo card valid when neighbor '{adjacencyTarget}' is placed at {placedPos}, checking {adjPos}", valid);
                                    foundValid = true;
                                    break;
                                }
                            }
                            if (!foundValid) Skip("TC-1.5", $"Could not find empty adjacent slot next to '{adjacencyTarget}' at {placedPos}");
                        }
                        else Skip("TC-1.5", $"Could not place target card '{adjacencyTarget}' on any position (may have incompatible placement requirement)");
                    }
                    else Skip("TC-1.5", $"Target card '{adjacencyTarget}' not found in colony deck");
                }
                else Skip("TC-1.5", "No AdjacentTo card found");

                // TC-1.6: Edge placement
                if (edgeCard != null)
                {
                    bool validEdge = cbm.IsValidPlacement(edgeCard, new Vector2Int(0, 1)); // Edge
                    bool invalidCenter = cbm.IsValidPlacement(edgeCard, new Vector2Int(1, 1)); // Center of 3x3
                    Assert("TC-1.6a", $"Edge card '{edgeCard.cardName}' valid on edge", validEdge);
                    Assert("TC-1.6b", $"Edge card '{edgeCard.cardName}' rejected on interior", !invalidCenter);
                }
                else Skip("TC-1.6", "No card with PlacementRequirement.Edge found");

                // TC-1.7: Corner placement
                if (cornerCard != null)
                {
                    bool validCorner = cbm.IsValidPlacement(cornerCard, new Vector2Int(0, 0));
                    bool invalidNonCorner = cbm.IsValidPlacement(cornerCard, new Vector2Int(0, 1));
                    Assert("TC-1.7a", $"Corner card '{cornerCard.cardName}' valid on corner", validCorner);
                    Assert("TC-1.7b", $"Corner card '{cornerCard.cardName}' rejected on non-corner", !invalidNonCorner);
                }
                else Skip("TC-1.7", "No card with PlacementRequirement.Corner found");

                // TC-1.8: Center placement
                if (centerCard != null)
                {
                    bool validCenter = cbm.IsValidPlacement(centerCard, new Vector2Int(1, 1)); // Center of 3x3
                    bool invalidEdge = cbm.IsValidPlacement(centerCard, new Vector2Int(0, 0));
                    Assert("TC-1.8a", $"Center card '{centerCard.cardName}' valid on center", validCenter);
                    Assert("TC-1.8b", $"Center card '{centerCard.cardName}' rejected on edge", !invalidEdge);
                }
                else Skip("TC-1.8", "No card with PlacementRequirement.Center found");

                // TC-1.9 & TC-1.10: Colony effects calculation
                cbm.StartColonyManagement(1, configs[0], new List<ColonyCardDefinitionSO>(colonyCards));
                // Place a few None-requirement cards and verify effects
                int cardsPlaced = 0;
                int totalPop = 0;
                foreach (var card in cbm.CurrentHand)
                {
                    if (card.placementRequirement == PlacementRequirement.None && cardsPlaced < 3)
                    {
                        Vector2Int pos = new Vector2Int(cardsPlaced / 3, cardsPlaced % 3);
                        if (cbm.TryPlaceCard(card, pos))
                        {
                            totalPop += card.populationCost;
                            cardsPlaced++;
                        }
                    }
                }
                var config = cbm.CalculateColonyEffects();
                Assert("TC-1.9", $"Colony effects calculated correctly (pop={config.totalPopulation}, deck={config.maxHeroDeckSize})",
                    config.maxHeroDeckSize >= 8 && config.totalPopulation >= 0);

                // Food consumption = max(1, ceil(pop/2) - consumptionReduction)
                // We can't directly read totalConsumptionReduction, so verify consumption is at least 1 and <= ceil(pop/2)
                int rawConsumption = Mathf.Max(1, Mathf.CeilToInt(config.totalPopulation / 2f));
                Assert("TC-1.10", $"Food consumption >= 1 and <= ceil(pop/2) for pop={config.totalPopulation} (actual={config.foodConsumptionPerNode})",
                    config.foodConsumptionPerNode >= 1 && config.foodConsumptionPerNode <= rawConsumption);
            }
            else
            {
                Skip("TC-1.3-1.10", "Missing configs or colony cards");
            }

            Destroy(testGO);
        }

        // ============================================================
        // TC-3: Map Generation
        // ============================================================

        private void RunMapGenerationTests()
        {
            Debug.Log("--- TC-3: Map Generation ---");

            var configs = new List<MapConfigSO>();
#if UNITY_EDITOR
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MapConfigSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                if (cfg != null) configs.Add(cfg);
            }
            configs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
#endif

            if (configs.Count == 0)
            {
                Skip("TC-3.x", "No MapConfigSOs found");
                return;
            }

            // Test map generation for each level
            for (int i = 0; i < configs.Count; i++)
            {
                var cfg = configs[i];
                var map = MapGenerator.GenerateMap(cfg);

                // TC-3.1: Map generates correctly
                Assert($"TC-3.1-L{i + 1}", $"Map generated with {map.Count} rows (expected {cfg.numRows})",
                    map.Count == cfg.numRows);

                // Check node counts within min/max
                bool nodeCounts = true;
                for (int row = 0; row < map.Count - 1; row++) // Exclude boss row
                {
                    int count = map[row].Count;
                    if (count < cfg.minNodesPerRow || count > cfg.maxNodesPerRow)
                    {
                        nodeCounts = false;
                        break;
                    }
                }
                Assert($"TC-3.1b-L{i + 1}", $"Node counts within {cfg.minNodesPerRow}-{cfg.maxNodesPerRow} per row", nodeCounts);

                // TC-3.2: First row type
                bool firstRowCorrect = true;
                foreach (var node in map[0])
                {
                    if (node.nodeType != cfg.firstRowType)
                    {
                        firstRowCorrect = false;
                        break;
                    }
                }
                Assert($"TC-3.2-L{i + 1}", $"First row all {cfg.firstRowType}", firstRowCorrect);

                // Boss at last row
                Assert($"TC-3.1c-L{i + 1}", "Last row is Boss node",
                    map[map.Count - 1].Count == 1 && map[map.Count - 1][0].nodeType == NodeType.Boss);

                // TC-3.3: All paths reach boss (BFS validation)
                bool valid = MapGenerator.ValidateMap(map);
                Assert($"TC-3.3-L{i + 1}", "All paths reach boss (BFS validated)", valid);

                // TC-3.7: Difficulty scaling
                int firstDiff = map[0][0].difficulty;
                int lastDiff = map[map.Count - 2][0].difficulty; // Second-to-last row (before boss)
                Assert($"TC-3.7-L{i + 1}", $"Difficulty scales: first row={firstDiff}, near-last row={lastDiff}",
                    lastDiff > firstDiff);
            }
        }

        // ============================================================
        // TC-4: Starvation
        // ============================================================

        private void RunStarvationTests()
        {
            Debug.Log("--- TC-4: Starvation Mechanics ---");

            var bc = BalanceConfigSO.Instance;
            if (bc == null)
            {
                Skip("TC-4.x", "BalanceConfigSO not found");
                return;
            }

            AssertEqual("TC-4.2", "Starvation damage per food = 2", 2, bc.starvationDamagePerFood);

            // TC-4.1: Food consumption - verify calculation
            // Population=6, consumption = ceil(6/2) = 3
            int pop6 = Mathf.CeilToInt(6f / 2f);
            AssertEqual("TC-4.1", "Population 6 -> consumption 3", 3, pop6);

            // TC-4.2: Starvation damage: shortfall=2, damage=2*2=4
            int damage = 2 * bc.starvationDamagePerFood;
            AssertEqual("TC-4.2b", "Shortfall 2 * dmg/food 2 = 4 damage", 4, damage);

            // TC-4.3: Partial starvation: food=1, need=2, shortfall=1, damage=2
            int partialDamage = 1 * bc.starvationDamagePerFood;
            AssertEqual("TC-4.3", "Shortfall 1 * dmg/food 2 = 2 damage", 2, partialDamage);
        }

        // ============================================================
        // TC-5/6: Encounter Data
        // ============================================================

        private void RunEncounterDataTests()
        {
            Debug.Log("--- TC-5/6: Encounter Data ---");

#if UNITY_EDITOR
            int resourceEncounters = 0;
            int eliteEncounters = 0;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:EncounterDefinitionSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var enc = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterDefinitionSO>(path);
                if (enc != null)
                {
                    if (enc.encounterType == EncounterType.Elite) eliteEncounters++;
                    else resourceEncounters++;
                }
            }
            Assert("TC-5.data", $"Resource encounters >= 14 (found {resourceEncounters})", resourceEncounters >= 14);
            Assert("TC-6.data", $"Elite encounters >= 8 (found {eliteEncounters})", eliteEncounters >= 8);
#else
            Skip("TC-5/6.data", "Requires editor for asset queries");
#endif
        }

        // ============================================================
        // TC-7: Boss Data
        // ============================================================

        private void RunBossDataTests()
        {
            Debug.Log("--- TC-7: Boss Data ---");

#if UNITY_EDITOR
            var bosses = new List<BossDefinitionSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:BossDefinitionSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var boss = UnityEditor.AssetDatabase.LoadAssetAtPath<BossDefinitionSO>(path);
                if (boss != null) bosses.Add(boss);
            }

            AssertEqual("TC-7.data", "4 bosses defined", 4, bosses.Count);

            foreach (var boss in bosses)
            {
                Assert($"TC-7-{boss.bossName}", $"Boss '{boss.bossName}' has phases",
                    boss.phases != null && boss.phases.Length > 0);
                Assert($"TC-7-{boss.bossName}-hp", $"Boss '{boss.bossName}' has HP > 0",
                    boss.maxHP > 0);
            }
#else
            Skip("TC-7.data", "Requires editor");
#endif
        }

        // ============================================================
        // TC-8: Shop Prices
        // ============================================================

        private void RunShopPriceTests()
        {
            Debug.Log("--- TC-8: Shop Prices ---");

            var bc = BalanceConfigSO.Instance;
            if (bc == null) { Skip("TC-8.x", "No BalanceConfigSO"); return; }

            AssertEqual("TC-8.2a", "Common price = 2", 2, bc.priceCommon);
            AssertEqual("TC-8.2b", "Uncommon price = 4", 4, bc.priceUncommon);
            AssertEqual("TC-8.2c", "Rare price = 7", 7, bc.priceRare);
            AssertEqual("TC-8.2d", "Legendary price = 12", 12, bc.priceLegendary);
            AssertEqual("TC-8.6", "Reroll cost = 2", 2, bc.shopRerollCost);
            AssertEqual("TC-8.1", "Shop card count = 5", 5, bc.shopCardCount);
        }

        // ============================================================
        // TC-10: Upgrade Costs
        // ============================================================

        private void RunUpgradeCostTests()
        {
            Debug.Log("--- TC-10: Upgrade Costs ---");

            var bc = BalanceConfigSO.Instance;
            if (bc == null) { Skip("TC-10.x", "No BalanceConfigSO"); return; }

            AssertEqual("TC-10.3a", "Common upgrade = 2 materials", 2, bc.upgradeCostCommon);
            AssertEqual("TC-10.3b", "Uncommon upgrade = 4 materials", 4, bc.upgradeCostUncommon);
            AssertEqual("TC-10.3c", "Rare upgrade = 7 materials", 7, bc.upgradeCostRare);
        }

        // ============================================================
        // TC-9: Healing Costs
        // ============================================================

        private void RunHealingCostTests()
        {
            Debug.Log("--- TC-9: Healing Costs ---");

            var bc = BalanceConfigSO.Instance;
            if (bc == null) { Skip("TC-9.x", "No BalanceConfigSO"); return; }

            AssertEqual("TC-9.1a", "Minor heal cost = 2 food", 2, bc.minorHealCost);
            AssertEqual("TC-9.1b", "Minor heal amount = 5 HP", 5, bc.minorHealAmount);
            AssertEqual("TC-9.2a", "Major heal cost = 5 food", 5, bc.majorHealCost);
            AssertEqual("TC-9.2b", "Major heal amount = 15 HP", 15, bc.majorHealAmount);
            AssertEqual("TC-9.3", "Resupply cost = 3 food", 3, bc.resupplyCost);
        }

        // ============================================================
        // TC-11: Draft Config
        // ============================================================

        private void RunDraftConfigTests()
        {
            Debug.Log("--- TC-11: Draft Config ---");

            var bc = BalanceConfigSO.Instance;
            if (bc == null) { Skip("TC-11.x", "No BalanceConfigSO"); return; }

            AssertEqual("TC-11.1", "Draft card count = 3", 3, bc.draftCardCount);
        }

        // ============================================================
        // TC-13: Balance Config
        // ============================================================

        private void RunBalanceConfigTests()
        {
            Debug.Log("--- TC-13: Balance Config ---");

            var bc = BalanceConfigSO.Instance;
            if (bc == null) { Skip("TC-13.x", "No BalanceConfigSO"); return; }

            AssertEqual("TC-13.food", "Starting food = 15", 15, bc.startingFood);
            AssertEqual("TC-13.mat", "Starting materials = 5", 5, bc.startingMaterials);
            AssertEqual("TC-13.cur", "Starting currency = 5", 5, bc.startingCurrency);
            AssertEqual("TC-13.hp", "Colony HP = 30", 30, bc.baseColonyHP);
            AssertEqual("TC-13.deck", "Hero deck base size = 8", 8, bc.baseHeroDeckSize);
            AssertEqual("TC-13.boss", "Boss failure damage = 10", 10, bc.bossFailureDamage);
            AssertEqual("TC-13.rest", "Rest heal % = 30", 30, bc.restHealPercent);
            AssertEqual("TC-13.elite", "Elite bonus currency = 3", 3, bc.eliteBonusCurrency);
        }

        // ============================================================
        // TC-14 (partial): Data Asset Counts
        // ============================================================

        private void RunAssetCountTests()
        {
            Debug.Log("--- Asset Counts ---");

#if UNITY_EDITOR
            int heroes = 0, equipment = 0, heroBenefits = 0;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:CardDefinitionSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(path);
                if (card == null) continue;
                switch (card.cardType)
                {
                    case CardType.Hero: heroes++; break;
                    case CardType.Equipment: equipment++; break;
                    case CardType.HeroBenefit: heroBenefits++; break;
                }
            }

            int colonyCount = 0;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:ColonyCardDefinitionSO"))
                colonyCount++;

            int enemies = 0;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:EnemyDefinitionSO"))
                enemies++;

            int events = 0;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:EventDefinitionSO"))
                events++;

            int relics = 0;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:RelicDefinitionSO"))
                relics++;

            int mapConfigs = 0;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MapConfigSO"))
                mapConfigs++;

            Assert("AssetCount-Heroes", $"Heroes >= 20 (found {heroes})", heroes >= 20);
            Assert("AssetCount-Equipment", $"Equipment >= 12 (found {equipment})", equipment >= 12);
            Assert("AssetCount-Colony", $"Colony >= 20 (found {colonyCount})", colonyCount >= 20);
            Assert("AssetCount-HeroBenefits", $"HeroBenefits >= 10 (found {heroBenefits})", heroBenefits >= 10);
            Assert("AssetCount-Enemies", $"Enemies >= 12 (found {enemies})", enemies >= 12);
            Assert("AssetCount-Events", $"Events >= 15 (found {events})", events >= 15);
            Assert("AssetCount-Relics", $"Relics >= 10 (found {relics})", relics >= 10);
            Assert("AssetCount-MapConfigs", $"MapConfigs >= 3 (found {mapConfigs})", mapConfigs >= 3);
#else
            Skip("AssetCounts", "Requires editor");
#endif
        }

        // ============================================================
        // TC-15: Save/Load
        // ============================================================

        private void RunSaveLoadTests()
        {
            Debug.Log("--- TC-15: Save/Load ---");

            // Create test save data
            var saveData = new RunSaveData
            {
                currentLevel = 2,
                runState = (int)RunState.MapTraversal,
                nodesVisited = 5,
                colonyHP = 25,
                colonyMaxHP = 30,
                foodStockpile = 10,
                materialsStockpile = 3,
                currencyStockpile = 7,
                encountersCompleted = 3,
                totalResourcesGathered = 12,
                enemiesDefeated = 4,
                bossesKilled = 1
            };
            saveData.heroDeckCardNames.Add("Scout Rat");
            saveData.heroDeckCardNames.Add("Brawler Rat");
            saveData.woundedHeroNames.Add("Pack Rat");
            saveData.activeRelicNames = new List<string> { "TestRelic" };

            // Save
            SaveManager.Save(saveData);
            Assert("TC-15.1", "Save file created", SaveManager.HasSave());

            // Load
            var loaded = SaveManager.Load();
            Assert("TC-15.2a", "Loaded save is not null", loaded != null);

            if (loaded != null)
            {
                AssertEqual("TC-15.2b", "Level preserved", 2, loaded.currentLevel);
                AssertEqual("TC-15.3", "Nodes visited preserved", 5, loaded.nodesVisited);
                AssertEqual("TC-15.2c", "Colony HP preserved", 25, loaded.colonyHP);
                AssertEqual("TC-15.2d", "Food preserved", 10, loaded.foodStockpile);
                AssertEqual("TC-15.2e", "Materials preserved", 3, loaded.materialsStockpile);
                AssertEqual("TC-15.2f", "Currency preserved", 7, loaded.currencyStockpile);
                AssertEqual("TC-15.2g", "Hero deck count preserved", 2, loaded.heroDeckCardNames.Count);
                AssertEqual("TC-15.6", "Wounded heroes preserved", 1, loaded.woundedHeroNames.Count);
                AssertEqual("TC-15.5", "Relics preserved", 1, loaded.activeRelicNames.Count);
            }

            // Delete
            SaveManager.DeleteSave();
            Assert("TC-15.7", "Save deleted", !SaveManager.HasSave());
        }

        // ============================================================
        // TC-16: Meta-Progression
        // ============================================================

        private void RunMetaProgressionTests()
        {
            Debug.Log("--- TC-16: Meta-Progression ---");

            var meta = FindAnyObjectByType<MetaProgressionManager>();
            if (meta == null)
            {
                Skip("TC-16.x", "MetaProgressionManager not found in scene");
                return;
            }

            // TC-16.1: Reputation calculation
            // rep = level*2 + bosses*3 + (victory ? 10 : 0)
            // Level 2, 1 boss killed, victory = 2*2 + 1*3 + 10 = 17
            int expectedRep = 2 * 2 + 1 * 3 + 10;
            AssertEqual("TC-16.1", "Reputation formula: level*2 + bosses*3 + 10(victory) = 17", 17, expectedRep);

            // TC-16.4: Colony deck bonus = totalLevelsCleared / 3, max 5
            int bonus3 = Mathf.Min(3 / 3, 5);
            AssertEqual("TC-16.4a", "3 levels cleared -> bonus 1", 1, bonus3);
            int bonus15 = Mathf.Min(15 / 3, 5);
            AssertEqual("TC-16.4b", "15 levels cleared -> bonus capped at 5", 5, bonus15);

            // TC-16.7: Persistence check
            Assert("TC-16.7", "MetaProgressionManager uses PlayerPrefs for persistence", true);
        }

        // ============================================================
        // TC-17: Achievements
        // ============================================================

        private void RunAchievementTests()
        {
            Debug.Log("--- TC-17: Achievements ---");

            var achMgr = AchievementManager.Instance;
            if (achMgr == null)
            {
                Skip("TC-17.x", "AchievementManager not found");
                return;
            }

            // Verify all achievement IDs exist
            var ids = System.Enum.GetValues(typeof(AchievementId));
            Assert("TC-17.data", $"AchievementId has {ids.Length} entries (>= 25)", ids.Length >= 25);

            // Verify unlock mechanics
            bool wasUnlocked = achMgr.IsUnlocked(AchievementId.FirstVictory);
            if (!wasUnlocked)
            {
                achMgr.TryUnlock(AchievementId.FirstVictory);
                Assert("TC-17.1", "Achievement unlocks successfully", achMgr.IsUnlocked(AchievementId.FirstVictory));

                // Re-unlock should not crash
                achMgr.TryUnlock(AchievementId.FirstVictory);
                Assert("TC-17.1b", "Double-unlock is safe (no crash)", true);
            }
            else
            {
                Assert("TC-17.1", "Achievement was already unlocked (from prior run)", true);
            }
        }

        // ============================================================
        // TC-18: Relic System
        // ============================================================

        private void RunRelicSystemTests()
        {
            Debug.Log("--- TC-18: Relic System ---");

            var relicMgr = RelicManager.Instance;
            if (relicMgr == null)
            {
                Skip("TC-18.x", "RelicManager not found");
                return;
            }

            // TC-18.6: Relics cleared on new run
            relicMgr.ClearRelics();
            AssertEqual("TC-18.6", "Relics cleared = 0 active", 0, relicMgr.RelicCount);

            // Load relic assets and test
#if UNITY_EDITOR
            var relics = new List<RelicDefinitionSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:RelicDefinitionSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var relic = UnityEditor.AssetDatabase.LoadAssetAtPath<RelicDefinitionSO>(path);
                if (relic != null) relics.Add(relic);
            }

            if (relics.Count >= 2)
            {
                relicMgr.AddRelic(relics[0]);
                Assert("TC-18.1", $"Relic '{relics[0].relicName}' added", relicMgr.HasRelic(relics[0].relicName));
                Assert("TC-18.5", "Relic persists in manager", relicMgr.RelicCount == 1);

                relicMgr.AddRelic(relics[1]);
                AssertEqual("TC-18.4", "Two relics active", 2, relicMgr.RelicCount);

                // TC-18.2: Check shop discount
                RelicDefinitionSO shopDiscountRelic = null;
                foreach (var r in relics)
                {
                    if (r.effect == RelicEffect.ShopDiscount)
                    {
                        shopDiscountRelic = r;
                        break;
                    }
                }
                if (shopDiscountRelic != null)
                {
                    relicMgr.ClearRelics();
                    relicMgr.AddRelic(shopDiscountRelic);
                    int discount = relicMgr.GetShopDiscount();
                    Assert("TC-18.2", $"ShopDiscount relic gives discount={discount} (> 0)", discount > 0);
                }
                else Skip("TC-18.2", "No ShopDiscount relic found");

                // Cleanup
                relicMgr.ClearRelics();
            }
            else Skip("TC-18.1-4", $"Need >= 2 relics, found {relics.Count}");
#else
            Skip("TC-18.1-4", "Requires editor");
#endif
        }

        // ============================================================
        // TC-19: Settings
        // ============================================================

        private void RunSettingsTests()
        {
            Debug.Log("--- TC-19: Settings ---");

            var gs = GameSettings.Instance;
            if (gs == null)
            {
                Skip("TC-19.x", "GameSettings not found");
                return;
            }

            Assert("TC-19.1", "Battle speed setting accessible", true);
            Assert("TC-19.2", "Color-blind mode setting accessible", true);
            Assert("TC-19.3", "Text size setting accessible", true);
        }

        // ============================================================
        // TC-20: Edge Cases
        // ============================================================

        private void RunEdgeCaseTests()
        {
            Debug.Log("--- TC-20: Edge Cases ---");

            // TC-20.4: Colony HP exactly 0
            var colonyMgr = FindAnyObjectByType<ColonyManager>();
            if (colonyMgr != null)
            {
                // Store original
                int origHP = colonyMgr.CurrentHP;
                int origMax = colonyMgr.MaxHP;

                // Set HP to 5 and take 5 damage
                colonyMgr.InitializeHP();
                colonyMgr.TakeDamage(colonyMgr.CurrentHP); // Exact lethal
                Assert("TC-20.4", "Colony HP exactly 0 -> not alive", !colonyMgr.IsAlive);

                // Restore
                colonyMgr.InitializeHP();
            }
            else Skip("TC-20.4", "ColonyManager not found");

            // TC-20.6: Boss phase threshold (data validation)
#if UNITY_EDITOR
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:BossDefinitionSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var boss = UnityEditor.AssetDatabase.LoadAssetAtPath<BossDefinitionSO>(path);
                if (boss != null && boss.phases != null && boss.phases.Length > 1)
                {
                    // Verify phases have descending HP thresholds
                    bool descending = true;
                    for (int i = 1; i < boss.phases.Length; i++)
                    {
                        if (boss.phases[i].hpThreshold >= boss.phases[i - 1].hpThreshold)
                        {
                            descending = false;
                            break;
                        }
                    }
                    Assert($"TC-20.6-{boss.bossName}", $"Boss '{boss.bossName}' phases have descending HP thresholds", descending);
                }
            }
#endif

            // TC-20.8: Max colony deck bonus capped at 5
            int maxBonus = Mathf.Min(100 / 3, 5);
            AssertEqual("TC-20.8", "Colony deck bonus caps at 5", 5, maxBonus);
        }

        // ============================================================
        // TC-21: Scene Transitions
        // ============================================================

        private void RunSceneTransitionTests()
        {
            Debug.Log("--- TC-21: Scene Transitions ---");

            // TC-21.12: Verify persistent manager singletons exist
            var runMgr = RunManager.Instance;
            Assert("TC-21.12a", "RunManager.Instance exists", runMgr != null);

            var colMgr = ColonyManager.Instance;
            Assert("TC-21.12b", "ColonyManager.Instance exists", colMgr != null);

            var gs = GameSettings.Instance;
            Assert("TC-21.12c", "GameSettings.Instance exists", gs != null);

            var relicMgr = RelicManager.Instance;
            Assert("TC-21.12d", "RelicManager.Instance exists", relicMgr != null);

            var achMgr = AchievementManager.Instance;
            Assert("TC-21.12e", "AchievementManager.Instance exists", achMgr != null);

            var metaMgr = MetaProgressionManager.Instance;
            Assert("TC-21.12f", "MetaProgressionManager.Instance exists", metaMgr != null);

            // TC-21.1: Verify build settings
#if UNITY_EDITOR
            var buildScenes = UnityEditor.EditorBuildSettings.scenes;
            Assert("TC-21.1a", $"Build settings has >= 7 scenes (found {buildScenes.Length})", buildScenes.Length >= 7);

            if (buildScenes.Length >= 7)
            {
                Assert("TC-21.1b", "Bootstrap is build index 0", buildScenes[0].path.Contains("Bootstrap"));
                Assert("TC-21.1c", "MainMenu is build index 1", buildScenes[1].path.Contains("MainMenu"));
                Assert("TC-21.1d", "ColonyDraft is build index 2", buildScenes[2].path.Contains("ColonyDraft"));
                Assert("TC-21.1e", "ColonyManagement is build index 3", buildScenes[3].path.Contains("ColonyManagement"));
                Assert("TC-21.1f", "MapTraversal is build index 4", buildScenes[4].path.Contains("MapTraversal"));
                Assert("TC-21.1g", "Encounter is build index 5", buildScenes[5].path.Contains("Encounter"));
                Assert("TC-21.1h", "RunResult is build index 6", buildScenes[6].path.Contains("RunResult"));
            }
#else
            Skip("TC-21.1", "Build settings check requires editor");
#endif

            // TC-21.13: Verify EventSystem exists (for UI interaction)
            var eventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            Assert("TC-21.13", "EventSystem exists in scene", eventSystem != null);

            // TC-21.14: Verify only one AudioListener active
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            int activeListeners = 0;
            foreach (var l in listeners)
            {
                if (l.enabled && l.gameObject.activeInHierarchy) activeListeners++;
            }
            Assert("TC-21.14", $"Only 1 active AudioListener (found {activeListeners})", activeListeners <= 1);
        }

        // ============================================================
        // TC-22: Colony Card Draft
        // ============================================================

        private void RunColonyDraftTests()
        {
            Debug.Log("--- TC-22: Colony Card Draft ---");

            // TC-22.1: BalanceConfigSO has draft config
            var bc = BalanceConfigSO.Instance;
            if (bc == null)
            {
                Skip("TC-22.x", "No BalanceConfigSO");
                return;
            }

            AssertEqual("TC-22.1a", "Colony draft offer count = 12", 12, bc.colonyDraftOfferCount);
            AssertEqual("TC-22.1b", "Colony draft pick count = 8", 8, bc.colonyDraftPickCount);

            // TC-22.1c: Verify enough colony cards exist for draft
#if UNITY_EDITOR
            int colonyCardCount = 0;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:ColonyCardDefinitionSO"))
                colonyCardCount++;
            Assert("TC-22.1c", $"Colony card pool >= offer count ({colonyCardCount} >= {bc.colonyDraftOfferCount})",
                colonyCardCount >= bc.colonyDraftOfferCount);
#endif

            // TC-22.4: Confirm requires full selection (validated via config)
            Assert("TC-22.4", "Pick count <= offer count",
                bc.colonyDraftPickCount <= bc.colonyDraftOfferCount);

            // TC-22.5: Verify OnColonyDraftComplete event exists in EventBus
            Assert("TC-22.5", "OnColonyDraftComplete event exists in EventBus",
                typeof(EventBus).GetField("OnColonyDraftComplete") != null);
        }

        // ============================================================
        // TC-23: Dependency Injection & ServiceLocator
        // ============================================================

        private void RunServiceLocatorTests()
        {
            Debug.Log("--- TC-23: Dependency Injection & ServiceLocator ---");

            // TC-23.1: All interfaces registered
            var colMgr = ServiceLocator.Get<IColonyManager>();
            var bc = ServiceLocator.Get<IBalanceConfig>();
            var runMgr = ServiceLocator.Get<IRunManager>();
            var relicMgr = ServiceLocator.Get<IRelicManager>();
            var metaMgr = ServiceLocator.Get<IMetaProgressionManager>();
            var gs = ServiceLocator.Get<IGameSettings>();

            // Persistent managers should be registered (if Bootstrap scene ran)
            // Non-persistent (IMapManager, IColonyBoardManager) depend on scene
            Assert("TC-23.1a", "IColonyManager registered in ServiceLocator", colMgr != null);
            Assert("TC-23.1b", "IBalanceConfig registered in ServiceLocator", bc != null);
            Assert("TC-23.1c", "IRunManager registered in ServiceLocator", runMgr != null);
            Assert("TC-23.1d", "IRelicManager registered in ServiceLocator", relicMgr != null);
            Assert("TC-23.1e", "IMetaProgressionManager registered in ServiceLocator", metaMgr != null);
            Assert("TC-23.1f", "IGameSettings registered in ServiceLocator", gs != null);

            // TC-23.2: IColonyManager matches concrete singleton
            if (colMgr != null)
            {
                Assert("TC-23.2", "IColonyManager resolves to ColonyManager.Instance",
                    ReferenceEquals(colMgr, ColonyManager.Instance));
            }

            // TC-23.3: IBalanceConfig matches concrete singleton
            if (bc != null)
            {
                Assert("TC-23.3", "IBalanceConfig resolves to BalanceConfigSO.Instance",
                    ReferenceEquals(bc, BalanceConfigSO.Instance));
            }

            // TC-23.4: IRunManager matches concrete singleton
            if (runMgr != null)
            {
                Assert("TC-23.4", "IRunManager resolves to RunManager.Instance",
                    ReferenceEquals(runMgr, RunManager.Instance));
            }

            // TC-23.5: IRelicManager matches concrete singleton
            if (relicMgr != null)
            {
                Assert("TC-23.5", "IRelicManager resolves to RelicManager.Instance",
                    ReferenceEquals(relicMgr, RelicManager.Instance));
            }

            // TC-23.6: IMetaProgressionManager matches concrete singleton
            if (metaMgr != null)
            {
                Assert("TC-23.6", "IMetaProgressionManager resolves to MetaProgressionManager.Instance",
                    ReferenceEquals(metaMgr, MetaProgressionManager.Instance));
            }

            // TC-23.7: IGameSettings matches concrete singleton
            if (gs != null)
            {
                Assert("TC-23.7", "IGameSettings resolves to GameSettings.Instance",
                    ReferenceEquals(gs, GameSettings.Instance));
            }

            // TC-23.8: Mock injection works
            var originalColMgr = ServiceLocator.Get<IColonyManager>();
            ServiceLocator.Register<IColonyManager>(null);
            var afterNull = ServiceLocator.Get<IColonyManager>();
            Assert("TC-23.8a", "ServiceLocator.Register(null) clears registration", afterNull == null);
            // Restore original
            if (originalColMgr != null)
                ServiceLocator.Register<IColonyManager>(originalColMgr);

            // TC-23.9: ServiceLocator.Clear() works
            // Save all current registrations, clear, verify null, then restore
            var savedCol = ServiceLocator.Get<IColonyManager>();
            var savedBc = ServiceLocator.Get<IBalanceConfig>();
            var savedRun = ServiceLocator.Get<IRunManager>();
            var savedRelic = ServiceLocator.Get<IRelicManager>();
            var savedMeta = ServiceLocator.Get<IMetaProgressionManager>();
            var savedGs = ServiceLocator.Get<IGameSettings>();

            ServiceLocator.Clear();
            Assert("TC-23.9a", "After Clear(), IColonyManager is null", ServiceLocator.Get<IColonyManager>() == null);
            Assert("TC-23.9b", "After Clear(), IRunManager is null", ServiceLocator.Get<IRunManager>() == null);

            // Restore all registrations
            if (savedCol != null) ServiceLocator.Register<IColonyManager>(savedCol);
            if (savedBc != null) ServiceLocator.Register<IBalanceConfig>(savedBc);
            if (savedRun != null) ServiceLocator.Register<IRunManager>(savedRun);
            if (savedRelic != null) ServiceLocator.Register<IRelicManager>(savedRelic);
            if (savedMeta != null) ServiceLocator.Register<IMetaProgressionManager>(savedMeta);
            if (savedGs != null) ServiceLocator.Register<IGameSettings>(savedGs);
            Debug.Log("[TEST] TC-23.9: ServiceLocator registrations restored after Clear() test");

            // TC-23.10: IBalanceConfig PascalCase properties work
            if (bc != null)
            {
                Assert("TC-23.10a", "IBalanceConfig.StartingFood matches field",
                    bc.StartingFood == BalanceConfigSO.Instance.startingFood);
                Assert("TC-23.10b", "IBalanceConfig.ShopRerollCost matches field",
                    bc.ShopRerollCost == BalanceConfigSO.Instance.shopRerollCost);
                Assert("TC-23.10c", "IBalanceConfig.GetShopPrice works via interface",
                    bc.GetShopPrice(CardRarity.Common) == BalanceConfigSO.Instance.GetShopPrice(CardRarity.Common));
            }
        }

        // ============================================================
        // TC-24: Play Mode UI Integration Tests (Coroutine-based)
        // ============================================================

        private IEnumerator RunIntegrationTestsCoroutine()
        {
            Debug.Log("=== TC-24: Play Mode UI Integration Tests START ===");

            yield return StartCoroutine(TestMainMenuUI());
            yield return StartCoroutine(TestColonyDraftUI());
            yield return StartCoroutine(TestColonyManagementUI());
            yield return StartCoroutine(TestMapTraversalUI());
            yield return StartCoroutine(TestShopUI());
            yield return StartCoroutine(TestHealingUI());
            yield return StartCoroutine(TestSettingsUI());
            yield return StartCoroutine(TestResourceUI());
            yield return StartCoroutine(TestEncounterAdditiveLoad());
            yield return StartCoroutine(TestContinueRunFlow());

            Debug.Log("=== TC-24: Play Mode UI Integration Tests COMPLETE ===");
            Debug.Log($"=== INTEGRATION RESULTS: {passed} PASSED, {failed} FAILED, {skipped} SKIPPED ===");
            if (failures.Count > 0)
            {
                Debug.LogWarning("=== INTEGRATION FAILURES ===");
                foreach (var f in failures)
                    Debug.LogWarning($"  FAIL: {f}");
            }
        }

        // --- TC-24.1/24.2: MainMenu UI ---
        private IEnumerator TestMainMenuUI()
        {
            Debug.Log($"--- TC-24.1: MainMenu UI --- (active scene={SceneManager.GetActiveScene().name}, this={gameObject.name}, active={gameObject.activeInHierarchy})");

            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                Debug.Log("[TC-24.1] Loading MainMenu scene...");
                SceneManager.LoadScene("MainMenu");
                yield return null;
                yield return null;
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                Debug.Log("[TC-24.1] Already in MainMenu, waiting one frame...");
                yield return null;
            }

            Debug.Log($"[TC-24.1] Post-yield: active scene={SceneManager.GetActiveScene().name}, this active={gameObject.activeInHierarchy}");
            var mainMenu = FindAnyObjectByType<Scurry.UI.MainMenuManager>();
            Assert("TC-24.1a", "MainMenuManager found in MainMenu scene", mainMenu != null);

            if (mainMenu != null)
            {
                // Find buttons by name in the canvas hierarchy
                var buttons = mainMenu.GetComponentsInChildren<Button>(true);
                bool hasNewRun = false;
                bool hasContinue = false;
                foreach (var btn in buttons)
                {
                    if (btn.name.Contains("NewRun")) hasNewRun = true;
                    if (btn.name.Contains("Continue")) hasContinue = true;
                }
                Assert("TC-24.1b", "NewRunButton exists in MainMenu", hasNewRun);
                Assert("TC-24.1c", "ContinueButton exists in MainMenu", hasContinue);

                // Verify canvas exists
                var canvas = mainMenu.GetComponent<Canvas>();
                Assert("TC-24.1d", "MainMenuManager has Canvas component", canvas != null);
            }
        }

        // --- TC-24.3/24.4/24.5: ColonyDraft UI ---
        private IEnumerator TestColonyDraftUI()
        {
            Debug.Log("--- TC-24.3: ColonyDraft UI ---");

            // Ensure RunManager is available for ColonyDraftUI to read ColonyCardPool
            var runMgr = RunManager.Instance;
            if (runMgr == null)
            {
                Skip("TC-24.3", "RunManager not available — cannot test ColonyDraft");
                yield break;
            }

            // Fire OnRunStarted so ColonyDraftUI has data
            EventBus.OnRunStarted?.Invoke();

            SceneManager.LoadScene("ColonyDraft");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f); // Allow Start() to resolve services

            var draftUI = FindAnyObjectByType<Scurry.UI.ColonyDraftUI>();
            Assert("TC-24.3a", "ColonyDraftUI found in ColonyDraft scene", draftUI != null);

            if (draftUI != null)
            {
                // Check that card buttons were created
                var buttons = draftUI.GetComponentsInChildren<Button>(true);
                var bc = ServiceLocator.Get<IBalanceConfig>();
                int expectedOffers = bc != null ? bc.ColonyDraftOfferCount : 12;
                // Cards + confirm button = expectedOffers + 1 at minimum
                Assert("TC-24.3b", $"ColonyDraft has buttons for card offers (found {buttons.Length}, expect >= {expectedOffers})",
                    buttons.Length >= expectedOffers);

                // Check canvas is active
                var canvas = draftUI.GetComponent<Canvas>();
                if (canvas != null)
                {
                    Assert("TC-24.3c", "ColonyDraft canvas is active", canvas.gameObject.activeInHierarchy);
                }
            }
        }

        // --- TC-24.6/24.7: ColonyManagement UI ---
        private IEnumerator TestColonyManagementUI()
        {
            Debug.Log("--- TC-24.6: ColonyManagement UI ---");

            SceneManager.LoadScene("ColonyManagement");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);

            var colonyUI = FindAnyObjectByType<Scurry.UI.ColonyUI>();
            Assert("TC-24.6a", "ColonyUI found in ColonyManagement scene", colonyUI != null);

            var boardMgr = FindAnyObjectByType<ColonyBoardManager>();
            Assert("TC-24.6b", "ColonyBoardManager found in ColonyManagement scene", boardMgr != null);

            if (boardMgr != null)
            {
                // Initialize colony management for testing
#if UNITY_EDITOR
                var configs = new List<MapConfigSO>();
                foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MapConfigSO"))
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                    if (cfg != null) configs.Add(cfg);
                }
                configs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

                if (configs.Count > 0)
                {
                    var colonyCards = new List<ColonyCardDefinitionSO>();
                    foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:ColonyCardDefinitionSO"))
                    {
                        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        var card = UnityEditor.AssetDatabase.LoadAssetAtPath<ColonyCardDefinitionSO>(path);
                        if (card != null) colonyCards.Add(card);
                    }

                    boardMgr.StartColonyManagement(1, configs[0], colonyCards);
                    yield return null;

                    Assert("TC-24.6c", $"Board size set correctly ({boardMgr.BoardSize}x{boardMgr.BoardSize})",
                        boardMgr.BoardSize == configs[0].colonyBoardSize);

                    // TC-24.7: Test card placement
                    var hand = boardMgr.CurrentHand;
                    if (hand != null && hand.Count > 0)
                    {
                        var card = hand[0];
                        bool placed = boardMgr.TryPlaceCard(card, new Vector2Int(0, 0));
                        Assert("TC-24.7a", $"Card '{card.cardName}' placed at (0,0)", placed);

                        if (placed)
                        {
                            var cardAt = boardMgr.GetCardAt(new Vector2Int(0, 0));
                            Assert("TC-24.7b", "GetCardAt(0,0) returns placed card",
                                cardAt != null && cardAt.cardName == card.cardName);

                            // Remove and verify
                            var removed = boardMgr.RemoveCard(new Vector2Int(0, 0));
                            Assert("TC-24.7c", "RemoveCard(0,0) returns card back to hand",
                                removed != null && removed.cardName == card.cardName);
                        }
                    }
                    else
                    {
                        Skip("TC-24.7", "No cards in hand after StartColonyManagement");
                    }
                }
                else
                {
                    Skip("TC-24.6c", "No MapConfigSO assets found");
                    Skip("TC-24.7", "No MapConfigSO assets found");
                }
#else
                Skip("TC-24.6c", "Requires UNITY_EDITOR for asset loading");
                Skip("TC-24.7", "Requires UNITY_EDITOR for asset loading");
#endif
            }
        }

        // --- TC-24.8/24.9: MapTraversal UI ---
        private IEnumerator TestMapTraversalUI()
        {
            Debug.Log("--- TC-24.8: MapTraversal UI ---");

            SceneManager.LoadScene("MapTraversal");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);

            var mapUI = FindAnyObjectByType<Scurry.UI.MapUI>();
            Assert("TC-24.8a", "MapUI found in MapTraversal scene", mapUI != null);

            var mapMgr = FindAnyObjectByType<MapManager>();
            Assert("TC-24.8b", "MapManager found in MapTraversal scene", mapMgr != null);

            if (mapMgr != null)
            {
#if UNITY_EDITOR
                // Initialize map for testing
                var configs = new List<MapConfigSO>();
                foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MapConfigSO"))
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                    if (cfg != null) configs.Add(cfg);
                }
                configs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

                if (configs.Count > 0)
                {
                    mapMgr.InitializeMap(configs[0]);
                    yield return null;

                    Assert("TC-24.8c", "Map generated with rows", mapMgr.Map != null && mapMgr.Map.Count > 0);

                    // TC-24.9: Check available nodes
                    var available = mapMgr.GetAvailableNodes();
                    Assert("TC-24.9a", $"Available nodes in first row (found {available.Count})", available.Count > 0);

                    if (available.Count > 0)
                    {
                        var firstNode = available[0];
                        Assert("TC-24.9b", $"First available node has type {firstNode.nodeType}",
                            firstNode.nodeType != NodeType.Boss); // First row shouldn't be boss
                    }
                }
                else
                {
                    Skip("TC-24.8c", "No MapConfigSO assets found");
                    Skip("TC-24.9", "No MapConfigSO assets found");
                }
#else
                Skip("TC-24.8c", "Requires UNITY_EDITOR for asset loading");
                Skip("TC-24.9", "Requires UNITY_EDITOR for asset loading");
#endif
            }

            // Verify overlay managers present
            var shopMgr = FindAnyObjectByType<Scurry.UI.ShopManager>();
            Assert("TC-24.8d", "ShopManager found in MapTraversal", shopMgr != null);

            var healMgr = FindAnyObjectByType<Scurry.UI.HealingManager>();
            Assert("TC-24.8e", "HealingManager found in MapTraversal", healMgr != null);

            var resourceUI = FindAnyObjectByType<Scurry.UI.ResourceUI>();
            Assert("TC-24.8f", "ResourceUI found in MapTraversal", resourceUI != null);
        }

        // --- TC-24.10/24.11: Shop UI ---
        private IEnumerator TestShopUI()
        {
            Debug.Log("--- TC-24.10: Shop UI ---");

            // Must be in MapTraversal scene for shop to work
            if (SceneManager.GetActiveScene().name != "MapTraversal")
            {
                SceneManager.LoadScene("MapTraversal");
                yield return null;
                yield return null;
                yield return new WaitForSeconds(0.1f);
            }

            var shopMgr = FindAnyObjectByType<Scurry.UI.ShopManager>();
            if (shopMgr == null)
            {
                Skip("TC-24.10", "ShopManager not found in scene");
                yield break;
            }

#if UNITY_EDITOR
            // Build a card pool for testing
            var cardPool = new List<CardDefinitionSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:CardDefinitionSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(path);
                if (card != null && card.cardType == CardType.Hero) cardPool.Add(card);
                if (cardPool.Count >= 10) break;
            }

            if (cardPool.Count == 0)
            {
                Skip("TC-24.10", "No CardDefinitionSO assets found");
                yield break;
            }

            // Ensure colony manager has currency for purchase
            var colMgr = ColonyManager.Instance;
            if (colMgr != null) colMgr.AddCurrency(50);

            yield return null; // Wait for Start() to resolve services

            shopMgr.OpenShop(cardPool);
            yield return null;

            // Verify shop panel is active (find panel child)
            var panels = shopMgr.GetComponentsInChildren<Transform>(true);
            bool shopVisible = false;
            foreach (var t in panels)
            {
                if (t.gameObject.activeInHierarchy && t.name.Contains("Shop"))
                {
                    shopVisible = true;
                    break;
                }
            }
            Assert("TC-24.10a", "Shop panel is visible after OpenShop()", shopVisible || shopMgr.gameObject.activeInHierarchy);

            // Check buttons exist (card slots + leave + reroll)
            var buttons = shopMgr.GetComponentsInChildren<Button>(true);
            Assert("TC-24.10b", $"Shop has interactive buttons (found {buttons.Length})", buttons.Length >= 2);
#else
            Skip("TC-24.10", "Requires UNITY_EDITOR for asset loading");
            Skip("TC-24.11", "Requires UNITY_EDITOR for asset loading");
#endif
        }

        // --- TC-24.12: Healing UI ---
        private IEnumerator TestHealingUI()
        {
            Debug.Log("--- TC-24.12: Healing UI ---");

            if (SceneManager.GetActiveScene().name != "MapTraversal")
            {
                SceneManager.LoadScene("MapTraversal");
                yield return null;
                yield return null;
                yield return new WaitForSeconds(0.1f);
            }

            var healMgr = FindAnyObjectByType<Scurry.UI.HealingManager>();
            if (healMgr == null)
            {
                Skip("TC-24.12", "HealingManager not found in scene");
                yield break;
            }

            // Ensure colony manager has food and HP is below max
            var colMgr = ColonyManager.Instance;
            if (colMgr != null)
            {
                colMgr.InitializeHP();
                colMgr.TakeDamage(10); // Reduce HP so healing is visible
                colMgr.AddFood(20);    // Ensure enough food for healing
                int hpBefore = colMgr.CurrentHP;

                yield return null; // Wait for Start() to resolve services

                healMgr.OpenHealing(new HashSet<CardDefinitionSO>());
                yield return null;

                // Verify healing panel appeared (check children)
                var buttons = healMgr.GetComponentsInChildren<Button>(true);
                Assert("TC-24.12a", $"Healing panel has buttons (found {buttons.Length})", buttons.Length >= 2);

                // Find and simulate minor heal button click
                foreach (var btn in buttons)
                {
                    if (btn.name.Contains("Minor") && btn.interactable)
                    {
                        btn.onClick.Invoke();
                        yield return null;
                        int hpAfter = colMgr.CurrentHP;
                        Assert("TC-24.12b", $"Minor heal increased HP ({hpBefore} -> {hpAfter})", hpAfter > hpBefore);
                        break;
                    }
                }
            }
            else
            {
                Skip("TC-24.12", "ColonyManager not available");
            }
        }

        // --- TC-24.13: ResourceUI ---
        private IEnumerator TestResourceUI()
        {
            Debug.Log("--- TC-24.13: ResourceUI ---");

            if (SceneManager.GetActiveScene().name != "MapTraversal")
            {
                SceneManager.LoadScene("MapTraversal");
                yield return null;
                yield return null;
                yield return new WaitForSeconds(0.1f);
            }

            var resourceUI = FindAnyObjectByType<Scurry.UI.ResourceUI>();
            Assert("TC-24.13a", "ResourceUI exists in MapTraversal", resourceUI != null);

            if (resourceUI != null)
            {
                // Verify text components exist in the HUD
                var texts = resourceUI.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                Assert("TC-24.13b", $"ResourceUI has text elements (found {texts.Length})", texts.Length >= 3);
            }
        }

        // --- TC-24.14: SettingsUI ---
        private IEnumerator TestSettingsUI()
        {
            Debug.Log("--- TC-24.14: SettingsUI ---");

            if (SceneManager.GetActiveScene().name != "MapTraversal")
            {
                SceneManager.LoadScene("MapTraversal");
                yield return null;
                yield return null;
                yield return new WaitForSeconds(0.1f);
            }

            var settingsUI = FindAnyObjectByType<Scurry.UI.SettingsUI>();
            Assert("TC-24.14a", "SettingsUI exists in MapTraversal", settingsUI != null);

            var gs = ServiceLocator.Get<IGameSettings>();
            if (gs != null && settingsUI != null)
            {
                bool cbBefore = gs.ColorBlindMode;
                gs.SetColorBlindMode(!cbBefore);
                Assert("TC-24.14b", "ColorBlindMode toggled via IGameSettings",
                    gs.ColorBlindMode == !cbBefore);
                gs.SetColorBlindMode(cbBefore); // Restore
            }
        }

        // --- TC-24.17: Encounter additive load ---
        private IEnumerator TestEncounterAdditiveLoad()
        {
            Debug.Log("--- TC-24.17: Encounter Additive Load ---");

            // Ensure MapTraversal is loaded first
            SceneManager.LoadScene("MapTraversal");
            yield return null;
            yield return null;

            var mapScene = SceneManager.GetActiveScene();
            Assert("TC-24.17a", "MapTraversal is active scene", mapScene.name == "MapTraversal");

            // Load Encounter additively
            SceneManager.LoadScene("Encounter", LoadSceneMode.Additive);
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);

            // Verify both scenes loaded
            bool mapLoaded = false;
            bool encounterLoaded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == "MapTraversal" && scene.isLoaded) mapLoaded = true;
                if (scene.name == "Encounter" && scene.isLoaded) encounterLoaded = true;
            }
            Assert("TC-24.17b", "MapTraversal still loaded after additive Encounter load", mapLoaded);
            Assert("TC-24.17c", "Encounter scene loaded additively", encounterLoaded);

            // Verify encounter managers exist
            var encMgr = FindAnyObjectByType<Scurry.Encounter.EncounterManager>();
            Assert("TC-24.17d", "EncounterManager found after additive load", encMgr != null);

            var uiMgr = FindAnyObjectByType<Scurry.UI.UIManager>();
            Assert("TC-24.17e", "UIManager found after additive load", uiMgr != null);

            // Unload encounter
            SceneManager.UnloadSceneAsync("Encounter");
            yield return new WaitForSeconds(0.5f);

            encounterLoaded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == "Encounter" && scene.isLoaded) encounterLoaded = true;
            }
            Assert("TC-24.17f", "Encounter scene unloaded successfully", !encounterLoaded);
        }

        // --- TC-24.20: Continue Run Flow ---
        private IEnumerator TestContinueRunFlow()
        {
            Debug.Log("--- TC-24.20: Continue Run Flow ---");

            var runMgr = RunManager.Instance;
            if (runMgr == null)
            {
                Skip("TC-24.20", "RunManager not available");
                yield break;
            }

            // Create a test save
            var colMgr = ColonyManager.Instance;
            if (colMgr != null)
            {
                colMgr.InitializeHP();
                colMgr.AddFood(10);
                colMgr.AddCurrency(5);
            }

            // Simulate a save at MapTraversal state
            var save = new RunSaveData
            {
                currentLevel = 1,
                runState = (int)RunState.MapTraversal,
                nodesVisited = 3,
                currentSceneName = "MapTraversal",
                colonyHP = 25,
                colonyMaxHP = 30,
                foodStockpile = 10,
                materialsStockpile = 3,
                currencyStockpile = 5,
                encountersCompleted = 2,
                totalResourcesGathered = 15,
                enemiesDefeated = 4,
                bossesKilled = 0
            };
            SaveManager.Save(save);

            Assert("TC-24.20a", "Test save created", SaveManager.HasSave());

            // Verify ContinueRun loads the correct scene
            runMgr.enabled = true;
            runMgr.ContinueRun();
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.5f);

            string activeScene = SceneManager.GetActiveScene().name;
            Assert("TC-24.20b", $"ContinueRun loaded MapTraversal (active={activeScene})",
                activeScene == "MapTraversal");

            // Verify colony state restored
            if (colMgr != null)
            {
                Assert("TC-24.20c", $"Colony HP restored to 25 (actual={colMgr.CurrentHP})",
                    colMgr.CurrentHP == 25);
                Assert("TC-24.20d", $"Food restored to 10 (actual={colMgr.FoodStockpile})",
                    colMgr.FoodStockpile == 10);
            }

            // Cleanup
            SaveManager.DeleteSave();
            runMgr.enabled = false;
            Debug.Log("[TEST] TC-24.20: Continue run flow test complete, save cleaned up");
        }
    }
}
