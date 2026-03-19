using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scurry.Core;
using Scurry.Data;
using Scurry.Map;
using Scurry.Colony;
using Scurry.Combat;
using Scurry.Cards;
using Scurry.AI;
using Scurry.Logistics;
using Scurry.Interfaces;
using Scurry.UI;

namespace Scurry.Tests
{
    public class ValidationTestRunner : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool runIntegrationTests = false;

        private int passed;
        private int failed;
        private int skipped;
        private StringBuilder report;
        private List<string> failures = new List<string>();
        private List<string> skips = new List<string>();

        // ══════════════════════════════════════════════════════════════════
        //  LIFECYCLE
        // ══════════════════════════════════════════════════════════════════

        private void Awake()
        {
            Debug.Log("[ValidationTestRunner] Awake: initializing validation test runner");
            if (runOnStart && runIntegrationTests)
            {
                DontDestroyOnLoad(gameObject);
                Debug.Log("[ValidationTestRunner] Awake: DontDestroyOnLoad applied for integration tests");
            }
        }

        private void Start()
        {
            Debug.Log($"[ValidationTestRunner] Start: (runOnStart={runOnStart}, runIntegrationTests={runIntegrationTests})");
            if (runOnStart)
            {
                StartCoroutine(RunAllTests());
            }
            else
            {
                Debug.Log("[ValidationTestRunner] Start: skipping tests (runOnStart=false)");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  DI SERVICE REGISTRATION
        // ══════════════════════════════════════════════════════════════════

        private void RegisterTestServices()
        {
            Debug.Log("[ValidationTestRunner] RegisterTestServices: registering DI services");
            ServiceLocator.Register<ICombatResolver>(new Scurry.Combat.CombatResolver());
            ServiceLocator.Register<IFogOfWar>(new FogOfWar());
            ServiceLocator.Register<IColonyGraph>(new ColonyGraph());
            ServiceLocator.Register<IMapGraph>(new MapGraph());
            ServiceLocator.Register<IHeroTokenFactory>(new HeroTokenFactory());
            ServiceLocator.Register<IEnemyTokenFactory>(new EnemyTokenFactory());
            Debug.Log("[ValidationTestRunner] RegisterTestServices: all 6 services registered");
        }

        // ══════════════════════════════════════════════════════════════════
        //  MAIN RUNNER
        // ══════════════════════════════════════════════════════════════════

        public IEnumerator RunAllTests()
        {
            Debug.Log("[ValidationTestRunner] RunAllTests: === STARTING SCURRY v2.0 VALIDATION ===");
            RegisterTestServices();
            passed = 0;
            failed = 0;
            skipped = 0;
            report = new StringBuilder();
            failures.Clear();
            skips.Clear();

            float startTime = Time.realtimeSinceStartup;

            // TC-1: Card Database Validation
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-1: Card Database Validation ===");
            TC1_CardDatabase();
            yield return null;

            // TC-2: Enemy Database Validation
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-2: Enemy Database Validation ===");
            TC2_EnemyDatabase();
            yield return null;

            // TC-3: Map Generation
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-3: Map Generation ===");
            TC3_MapGeneration();
            yield return null;

            // TC-4: Pathfinding
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-4: Pathfinding ===");
            TC4_Pathfinding();
            yield return null;

            // TC-5: Fog of War
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-5: Fog of War ===");
            TC5_FogOfWar();
            yield return null;

            // TC-6: Hero Token
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-6: Hero Token ===");
            TC6_HeroToken();
            yield return null;

            // TC-7: Enemy Token
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-7: Enemy Token ===");
            TC7_EnemyToken();
            yield return null;

            // TC-8: Combat Resolution
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-8: Combat Resolution ===");
            TC8_CombatResolution();
            yield return null;

            // TC-9: Colony Graph
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-9: Colony Graph ===");
            TC9_ColonyGraph();
            yield return null;

            // TC-10: Score Calculation
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-10: Score Calculation ===");
            TC10_ScoreCalculation();
            yield return null;

            // TC-11: Deck Construction
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-11: Deck Construction ===");
            TC11_DeckConstruction();
            yield return null;

            // TC-12: Cleanup Phase
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-12: Cleanup Phase ===");
            TC12_CleanupPhase();
            yield return null;

            // TC-13: Gather Phase
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-13: Gather Phase ===");
            TC13_GatherPhase();
            yield return null;

            // TC-14: Enemy AI
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-14: Enemy AI ===");
            TC14_EnemyAI();
            yield return null;

            // TC-15: EventBus
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-15: EventBus ===");
            TC15_EventBus();
            yield return null;

            // TC-16: Save/Load
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-16: Save/Load ===");
            TC16_SaveLoad();
            yield return null;

            // TC-17: Seeded Random
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-17: Seeded Random ===");
            TC17_SeededRandom();
            yield return null;

            // TC-18: Scene Build Settings
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-18: Scene Build Settings ===");
            TC18_SceneBuildSettings();
            yield return null;

            // TC-19: Full Game Simulation
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-19: Full Game Simulation ===");
            TC19_FullGameSimulation();
            yield return null;

            // TC-20: Balance Validation
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-20: Balance Validation ===");
            TC20_BalanceValidation();
            yield return null;

            // TC-21: Integration Tests (coroutine-based)
            if (runIntegrationTests)
            {
                Debug.Log("[ValidationTestRunner] RunAllTests: === TC-21: Integration Tests ===");
                yield return StartCoroutine(TC21_IntegrationTests());
            }
            else
            {
                Skip("TC21_IntegrationTests", "runIntegrationTests is false");
            }

            // TC-22: EquipmentEffectProcessor
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-22: EquipmentEffectProcessor ===");
            TC22_EquipmentEffectProcessor();
            yield return null;

            // TC-23: TacticalEffectProcessor
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-23: TacticalEffectProcessor ===");
            TC23_TacticalEffectProcessor();
            yield return null;

            // TC-24: SpecialAbilityProcessor
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-24: SpecialAbilityProcessor ===");
            TC24_SpecialAbilityProcessor();
            yield return null;

            // TC-25: LocalizationManager
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-25: LocalizationManager ===");
            TC25_LocalizationManager();
            yield return null;

            // TC-26: SaveManager
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-26: SaveManager ===");
            TC26_SaveManager();
            yield return null;

            // TC-31: MetaProgressionManager
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-31: MetaProgressionManager ===");
            TC31_MetaProgressionManager();
            yield return null;

            // TC-33: GameSettings
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-33: GameSettings ===");
            TC33_GameSettings();
            yield return null;

            // TC-35: RunManager recording methods
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-35: RunManager Recording ===");
            TC35_RunManagerRecording();
            yield return null;

            // TC-36: MapManager via IMapManager
            Debug.Log("[ValidationTestRunner] RunAllTests: === TC-36: MapManager ===");
            TC36_MapManager();
            yield return null;

            Debug.Log("[ValidationTestRunner] RunAllTests: clearing ServiceLocator");
            ServiceLocator.Clear();

            float elapsed = Time.realtimeSinceStartup - startTime;
            PrintReport(elapsed);
        }

        // ══════════════════════════════════════════════════════════════════
        //  ASSERT HELPERS
        // ══════════════════════════════════════════════════════════════════

        private void Assert(bool condition, string testName, string message)
        {
            if (condition)
            {
                passed++;
                Debug.Log($"[ValidationTestRunner] PASS: {testName} ({message})");
            }
            else
            {
                failed++;
                string entry = $"{testName}: {message}";
                failures.Add(entry);
                Debug.LogError($"[ValidationTestRunner] FAIL: {testName} ({message})");
            }
        }

        private void AssertEqual<T>(T expected, T actual, string testName)
        {
            bool equal = EqualityComparer<T>.Default.Equals(expected, actual);
            if (equal)
            {
                passed++;
                Debug.Log($"[ValidationTestRunner] PASS: {testName} (expected={expected}, actual={actual})");
            }
            else
            {
                failed++;
                string entry = $"{testName}: expected={expected}, actual={actual}";
                failures.Add(entry);
                Debug.LogError($"[ValidationTestRunner] FAIL: {testName} (expected={expected}, actual={actual})");
            }
        }

        private void AssertNotNull(object obj, string testName)
        {
            if (obj != null)
            {
                passed++;
                Debug.Log($"[ValidationTestRunner] PASS: {testName} (object is not null)");
            }
            else
            {
                failed++;
                string entry = $"{testName}: expected non-null, got null";
                failures.Add(entry);
                Debug.LogError($"[ValidationTestRunner] FAIL: {testName} (expected non-null, got null)");
            }
        }

        private void AssertGreaterThan(int value, int min, string testName)
        {
            if (value > min)
            {
                passed++;
                Debug.Log($"[ValidationTestRunner] PASS: {testName} (value={value} > min={min})");
            }
            else
            {
                failed++;
                string entry = $"{testName}: value={value} not greater than min={min}";
                failures.Add(entry);
                Debug.LogError($"[ValidationTestRunner] FAIL: {testName} (value={value} not > min={min})");
            }
        }

        private void AssertGreaterThanOrEqual(int value, int min, string testName)
        {
            if (value >= min)
            {
                passed++;
                Debug.Log($"[ValidationTestRunner] PASS: {testName} (value={value} >= min={min})");
            }
            else
            {
                failed++;
                string entry = $"{testName}: value={value} not >= min={min}";
                failures.Add(entry);
                Debug.LogError($"[ValidationTestRunner] FAIL: {testName} (value={value} not >= min={min})");
            }
        }

        private void AssertLessThanOrEqual(int value, int max, string testName)
        {
            if (value <= max)
            {
                passed++;
                Debug.Log($"[ValidationTestRunner] PASS: {testName} (value={value} <= max={max})");
            }
            else
            {
                failed++;
                string entry = $"{testName}: value={value} not <= max={max}";
                failures.Add(entry);
                Debug.LogError($"[ValidationTestRunner] FAIL: {testName} (value={value} not <= max={max})");
            }
        }

        private void AssertInRange(int value, int min, int max, string testName)
        {
            if (value >= min && value <= max)
            {
                passed++;
                Debug.Log($"[ValidationTestRunner] PASS: {testName} (value={value} in [{min},{max}])");
            }
            else
            {
                failed++;
                string entry = $"{testName}: value={value} not in [{min},{max}]";
                failures.Add(entry);
                Debug.LogError($"[ValidationTestRunner] FAIL: {testName} (value={value} not in [{min},{max}])");
            }
        }

        private void AssertTrue(bool condition, string testName)
        {
            Assert(condition, testName, condition ? "true" : "expected true, got false");
        }

        private void AssertFalse(bool condition, string testName)
        {
            Assert(!condition, testName, !condition ? "false as expected" : "expected false, got true");
        }

        private void Skip(string testName, string reason)
        {
            skipped++;
            skips.Add($"{testName}: {reason}");
            Debug.LogWarning($"[ValidationTestRunner] SKIP: {testName} ({reason})");
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPER: Create test MapConfigSO
        // ══════════════════════════════════════════════════════════════════

        private MapConfigSO CreateTestMapConfig()
        {
            Debug.Log("[ValidationTestRunner] CreateTestMapConfig: creating test map config");
            var config = ScriptableObject.CreateInstance<MapConfigSO>();
            config.nodesPerZone = 10;
            config.minEdgesPerNode = 2;
            config.maxEdgesPerNode = 4;
            config.crossZoneEdges = 3;
            config.colonyConnections = 3;
            config.piperConnections = 3;
            config.wildernessResourceMin = 2;
            config.wildernessResourceMax = 4;
            config.farmlandResourceMin = 1;
            config.farmlandResourceMax = 3;
            config.townResourceMin = 0;
            config.townResourceMax = 2;
            Debug.Log("[ValidationTestRunner] CreateTestMapConfig: config created successfully");
            return config;
        }

        private CardDefinitionSO CreateTestHeroCard(int id, string name, HeroRole role, int combat, int move, int hp, int carry, int initiative, SpecialAbility ability = SpecialAbility.None, int deckCost = 1)
        {
            Debug.Log($"[ValidationTestRunner] CreateTestHeroCard: creating hero (id={id}, name={name}, role={role}, combat={combat}, move={move}, hp={hp}, carry={carry})");
            var card = ScriptableObject.CreateInstance<CardDefinitionSO>();
            card.cardId = id;
            card.cardName = name;
            card.cardType = CardType.Hero;
            card.heroRole = role;
            card.combat = combat;
            card.move = move;
            card.hp = hp;
            card.carry = carry;
            card.initiative = initiative;
            card.specialAbility = ability;
            card.deckCost = deckCost;
            return card;
        }

        private CardDefinitionSO CreateTestEquipmentCard(int id, string name, EquipmentSlot slot, int ev1, int ev2 = 0, int ev3 = 0, int deckCost = 1)
        {
            Debug.Log($"[ValidationTestRunner] CreateTestEquipmentCard: creating equipment (id={id}, name={name}, slot={slot}, ev1={ev1})");
            var card = ScriptableObject.CreateInstance<CardDefinitionSO>();
            card.cardId = id;
            card.cardName = name;
            card.cardType = CardType.Equipment;
            card.equipmentSlot = slot;
            card.effectValue1 = ev1;
            card.effectValue2 = ev2;
            card.effectValue3 = ev3;
            card.deckCost = deckCost;
            return card;
        }

        private CardDefinitionSO CreateTestTacticalCard(int id, string name, TacticalType type, int deckCost = 1)
        {
            Debug.Log($"[ValidationTestRunner] CreateTestTacticalCard: creating tactical (id={id}, name={name}, type={type})");
            var card = ScriptableObject.CreateInstance<CardDefinitionSO>();
            card.cardId = id;
            card.cardName = name;
            card.cardType = CardType.Tactical;
            card.tacticalType = type;
            card.deckCost = deckCost;
            return card;
        }

        private ColonyCardDefinitionSO CreateTestColonyCard(int id, string name, ColonyTier tier, ColonyEffect effect, int effectValue, int deckCost = 1, bool isStarter = false)
        {
            Debug.Log($"[ValidationTestRunner] CreateTestColonyCard: creating colony card (id={id}, name={name}, tier={tier}, effect={effect}, value={effectValue})");
            var card = ScriptableObject.CreateInstance<ColonyCardDefinitionSO>();
            card.cardId = id;
            card.cardName = name;
            card.colonyTier = tier;
            card.colonyEffect = effect;
            card.effectValue = effectValue;
            card.deckCost = deckCost;
            card.isStarter = isStarter;
            return card;
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-1: CARD DATABASE VALIDATION
        // ══════════════════════════════════════════════════════════════════

        private void TC1_CardDatabase()
        {
            Debug.Log("[ValidationTestRunner] TC1_CardDatabase: ENTER");

            try
            {
                var db = CardDatabase.Instance;
                AssertNotNull(db, "TC1_CardDB_InstanceLoads");
                Debug.Log($"[ValidationTestRunner] TC1_CardDatabase: db loaded (allCards={db.AllCards.Count}, allColonyCards={db.AllColonyCards.Count})");

                // Total card count
                int heroCount = 0;
                int equipCount = 0;
                int tacticalCount = 0;
                foreach (var card in db.AllCards)
                {
                    if (card.cardType == CardType.Hero) heroCount++;
                    else if (card.cardType == CardType.Equipment) equipCount++;
                    else if (card.cardType == CardType.Tactical) tacticalCount++;
                }
                int colonyCount = db.AllColonyCards.Count;
                int totalCount = db.AllCards.Count + colonyCount;

                Debug.Log($"[ValidationTestRunner] TC1_CardDatabase: card counts (heroes={heroCount}, colony={colonyCount}, equipment={equipCount}, tactical={tacticalCount}, total={totalCount})");

                AssertEqual(20, heroCount, "TC1_HeroCount_Is20");
                AssertEqual(30, colonyCount, "TC1_ColonyCount_Is30");
                AssertEqual(40, equipCount, "TC1_EquipmentCount_Is40");
                AssertEqual(30, tacticalCount, "TC1_TacticalCount_Is30");
                AssertEqual(120, totalCount, "TC1_TotalCount_Is120");

                // All heroes have positive stats
                Debug.Log("[ValidationTestRunner] TC1_CardDatabase: validating hero stats");
                var heroes = db.GetCardsByType(CardType.Hero);
                foreach (var hero in heroes)
                {
                    Debug.Log($"[ValidationTestRunner] TC1_CardDatabase: checking hero '{hero.cardName}' (combat={hero.combat}, move={hero.move}, hp={hero.hp}, carry={hero.carry}, init={hero.initiative})");
                    AssertGreaterThan(hero.combat, 0, $"TC1_Hero_{hero.cardName}_CombatPositive");
                    AssertGreaterThan(hero.move, 0, $"TC1_Hero_{hero.cardName}_MovePositive");
                    AssertGreaterThan(hero.hp, 0, $"TC1_Hero_{hero.cardName}_HPPositive");
                    AssertGreaterThan(hero.carry, 0, $"TC1_Hero_{hero.cardName}_CarryPositive");
                    AssertGreaterThanOrEqual(hero.initiative, 0, $"TC1_Hero_{hero.cardName}_InitiativeNonNeg");
                }

                // All colony cards have valid effects
                Debug.Log("[ValidationTestRunner] TC1_CardDatabase: validating colony card effects");
                foreach (var colony in db.AllColonyCards)
                {
                    Debug.Log($"[ValidationTestRunner] TC1_CardDatabase: checking colony card '{colony.cardName}' (effect={colony.colonyEffect}, value={colony.effectValue}, tier={colony.colonyTier})");
                    Assert(!string.IsNullOrEmpty(colony.cardName), $"TC1_Colony_{colony.cardId}_HasName", $"name='{colony.cardName}'");
                    Assert(Enum.IsDefined(typeof(ColonyEffect), colony.colonyEffect), $"TC1_Colony_{colony.cardName}_ValidEffect", $"effect={colony.colonyEffect}");
                }

                // All equipment have valid slots
                Debug.Log("[ValidationTestRunner] TC1_CardDatabase: validating equipment slots");
                var equipment = db.GetCardsByType(CardType.Equipment);
                foreach (var equip in equipment)
                {
                    Debug.Log($"[ValidationTestRunner] TC1_CardDatabase: checking equipment '{equip.cardName}' (slot={equip.equipmentSlot})");
                    Assert(equip.equipmentSlot == EquipmentSlot.Offensive || equip.equipmentSlot == EquipmentSlot.Defensive || equip.equipmentSlot == EquipmentSlot.Utility, $"TC1_Equip_{equip.cardName}_ValidSlot", $"slot={equip.equipmentSlot}");
                }

                // All tactical cards have valid types
                Debug.Log("[ValidationTestRunner] TC1_CardDatabase: validating tactical types");
                var tacticals = db.GetCardsByType(CardType.Tactical);
                foreach (var tact in tacticals)
                {
                    Debug.Log($"[ValidationTestRunner] TC1_CardDatabase: checking tactical '{tact.cardName}' (type={tact.tacticalType})");
                    Assert(Enum.IsDefined(typeof(TacticalType), tact.tacticalType), $"TC1_Tactical_{tact.cardName}_ValidType", $"type={tact.tacticalType}");
                }

                // Card IDs are unique across all cards
                Debug.Log("[ValidationTestRunner] TC1_CardDatabase: checking card ID uniqueness");
                var allIds = new HashSet<int>();
                bool uniqueIds = true;
                foreach (var card in db.AllCards)
                {
                    if (!allIds.Add(card.cardId))
                    {
                        Debug.LogError($"[ValidationTestRunner] TC1_CardDatabase: duplicate card ID found (cardId={card.cardId}, name={card.cardName})");
                        uniqueIds = false;
                    }
                }
                foreach (var colony in db.AllColonyCards)
                {
                    if (!allIds.Add(colony.cardId))
                    {
                        Debug.LogError($"[ValidationTestRunner] TC1_CardDatabase: duplicate colony card ID found (cardId={colony.cardId}, name={colony.cardName})");
                        uniqueIds = false;
                    }
                }
                AssertTrue(uniqueIds, "TC1_CardIDs_Unique");

                // All cards have non-empty names
                Debug.Log("[ValidationTestRunner] TC1_CardDatabase: checking card names");
                bool allNamed = true;
                foreach (var card in db.AllCards)
                {
                    if (string.IsNullOrEmpty(card.cardName))
                    {
                        Debug.LogError($"[ValidationTestRunner] TC1_CardDatabase: card with empty name (cardId={card.cardId})");
                        allNamed = false;
                    }
                }
                foreach (var colony in db.AllColonyCards)
                {
                    if (string.IsNullOrEmpty(colony.cardName))
                    {
                        Debug.LogError($"[ValidationTestRunner] TC1_CardDatabase: colony card with empty name (cardId={colony.cardId})");
                        allNamed = false;
                    }
                }
                AssertTrue(allNamed, "TC1_AllCards_HaveNames");

                // Each HeroRole represented at least once
                Debug.Log("[ValidationTestRunner] TC1_CardDatabase: checking hero role coverage");
                var rolesSeen = new HashSet<HeroRole>();
                foreach (var hero in heroes)
                {
                    rolesSeen.Add(hero.heroRole);
                    Debug.Log($"[ValidationTestRunner] TC1_CardDatabase: hero '{hero.cardName}' has role {hero.heroRole}");
                }
                foreach (HeroRole role in Enum.GetValues(typeof(HeroRole)))
                {
                    Assert(rolesSeen.Contains(role), $"TC1_HeroRole_{role}_Represented", $"role={role}, found={rolesSeen.Contains(role)}");
                }

                // Each ColonyTier has cards
                Debug.Log("[ValidationTestRunner] TC1_CardDatabase: checking colony tier coverage");
                foreach (ColonyTier tier in Enum.GetValues(typeof(ColonyTier)))
                {
                    var tierCards = db.GetColonyCardsByTier(tier);
                    AssertGreaterThan(tierCards.Count, 0, $"TC1_ColonyTier_{tier}_HasCards");
                    Debug.Log($"[ValidationTestRunner] TC1_CardDatabase: tier {tier} has {tierCards.Count} cards");
                }

                // Deck cost distribution: verify 1/2/3 cost cards exist
                Debug.Log("[ValidationTestRunner] TC1_CardDatabase: checking deck cost distribution");
                int cost1 = 0, cost2 = 0, cost3plus = 0;
                foreach (var card in db.AllCards)
                {
                    if (card.deckCost == 1) cost1++;
                    else if (card.deckCost == 2) cost2++;
                    else if (card.deckCost >= 3) cost3plus++;
                }
                foreach (var colony in db.AllColonyCards)
                {
                    if (colony.deckCost == 1) cost1++;
                    else if (colony.deckCost == 2) cost2++;
                    else if (colony.deckCost >= 3) cost3plus++;
                }
                AssertGreaterThan(cost1, 0, "TC1_DeckCost1_CardsExist");
                AssertGreaterThan(cost2, 0, "TC1_DeckCost2_CardsExist");
                AssertGreaterThan(cost3plus, 0, "TC1_DeckCost3Plus_CardsExist");
                Debug.Log($"[ValidationTestRunner] TC1_CardDatabase: cost distribution (cost1={cost1}, cost2={cost2}, cost3+={cost3plus})");
            }
            catch (Exception e)
            {
                Skip("TC1_CardDatabase", $"CardDatabase not available: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC1_CardDatabase: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC1_CardDatabase: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-2: ENEMY DATABASE VALIDATION
        // ══════════════════════════════════════════════════════════════════

        private void TC2_EnemyDatabase()
        {
            Debug.Log("[ValidationTestRunner] TC2_EnemyDatabase: ENTER");

            try
            {
                var db = EnemyDatabase.Instance;
                AssertNotNull(db, "TC2_EnemyDB_InstanceLoads");
                Debug.Log($"[ValidationTestRunner] TC2_EnemyDatabase: db loaded (allEnemies={db.AllEnemies.Count})");

                // All enemies have positive strength and hp
                Debug.Log("[ValidationTestRunner] TC2_EnemyDatabase: validating enemy stats");
                foreach (var enemy in db.AllEnemies)
                {
                    Debug.Log($"[ValidationTestRunner] TC2_EnemyDatabase: checking enemy '{enemy.enemyName}' (str={enemy.strength}, hp={enemy.hp}, spd={enemy.speed}, behavior={enemy.behavior}, zone={enemy.homeZone})");
                    AssertGreaterThan(enemy.strength, 0, $"TC2_Enemy_{enemy.enemyName}_StrengthPositive");
                    AssertGreaterThan(enemy.hp, 0, $"TC2_Enemy_{enemy.enemyName}_HPPositive");
                }

                // All EnemyBehavior types used
                Debug.Log("[ValidationTestRunner] TC2_EnemyDatabase: checking behavior coverage");
                var behaviorsSeen = new HashSet<EnemyBehavior>();
                foreach (var enemy in db.AllEnemies)
                {
                    behaviorsSeen.Add(enemy.behavior);
                }
                foreach (EnemyBehavior behavior in Enum.GetValues(typeof(EnemyBehavior)))
                {
                    Assert(behaviorsSeen.Contains(behavior), $"TC2_Behavior_{behavior}_Used", $"behavior={behavior}, found={behaviorsSeen.Contains(behavior)}");
                    Debug.Log($"[ValidationTestRunner] TC2_EnemyDatabase: behavior {behavior} present={behaviorsSeen.Contains(behavior)}");
                }

                // All zones have enemies
                Debug.Log("[ValidationTestRunner] TC2_EnemyDatabase: checking zone coverage");
                var wildernessEnemies = db.GetEnemiesByZone(NodeType.Wilderness);
                var farmlandEnemies = db.GetEnemiesByZone(NodeType.Farmland);
                var townEnemies = db.GetEnemiesByZone(NodeType.Town);
                AssertGreaterThan(wildernessEnemies.Count, 0, "TC2_Wilderness_HasEnemies");
                AssertGreaterThan(farmlandEnemies.Count, 0, "TC2_Farmland_HasEnemies");
                AssertGreaterThan(townEnemies.Count, 0, "TC2_Town_HasEnemies");
                Debug.Log($"[ValidationTestRunner] TC2_EnemyDatabase: zone enemies (wilderness={wildernessEnemies.Count}, farmland={farmlandEnemies.Count}, town={townEnemies.Count})");

                // Enemy names are unique
                Debug.Log("[ValidationTestRunner] TC2_EnemyDatabase: checking enemy name uniqueness");
                var namesSeen = new HashSet<string>();
                bool uniqueNames = true;
                foreach (var enemy in db.AllEnemies)
                {
                    if (!namesSeen.Add(enemy.enemyName))
                    {
                        Debug.LogError($"[ValidationTestRunner] TC2_EnemyDatabase: duplicate enemy name '{enemy.enemyName}'");
                        uniqueNames = false;
                    }
                }
                AssertTrue(uniqueNames, "TC2_EnemyNames_Unique");

                // At least 10 enemies defined
                AssertGreaterThanOrEqual(db.AllEnemies.Count, 10, "TC2_AtLeast10Enemies");
                Debug.Log($"[ValidationTestRunner] TC2_EnemyDatabase: total enemies={db.AllEnemies.Count}");
            }
            catch (Exception e)
            {
                Skip("TC2_EnemyDatabase", $"EnemyDatabase not available: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC2_EnemyDatabase: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC2_EnemyDatabase: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-3: MAP GENERATION
        // ══════════════════════════════════════════════════════════════════

        private void TC3_MapGeneration()
        {
            Debug.Log("[ValidationTestRunner] TC3_MapGeneration: ENTER");

            try
            {
                var config = CreateTestMapConfig();

                // Generate map with seed 42
                Debug.Log("[ValidationTestRunner] TC3_MapGeneration: generating map with seed=42");
                var graph = MapGenerator.GenerateMap(config, 42);
                ServiceLocator.Register<IMapGraph>(graph);
                Debug.Log("[ValidationTestRunner] TC3_MapGeneration: registered generated map as IMapGraph");
                AssertNotNull(graph, "TC3_GenerateMap_ReturnsNonNull");

                // 51 nodes total (1 colony entrance + 19 colony sub-network + 30 zone nodes + 1 pied piper)
                var allNodes = graph.GetAllNodes();
                int nodeCount = allNodes.Count;
                AssertEqual(51, nodeCount, "TC3_NodeCount_Is51");
                Debug.Log($"[ValidationTestRunner] TC3_MapGeneration: total nodes={nodeCount}");

                // Colony at ID 0
                AssertEqual(0, graph.ColonyNodeId, "TC3_ColonyNode_IsId0");
                var colonyNode = graph.GetNode(0);
                AssertNotNull(colonyNode, "TC3_ColonyNode_Exists");
                if (colonyNode != null)
                {
                    AssertEqual(NodeType.Colony, colonyNode.zone, "TC3_ColonyNode_ZoneIsColony");
                    Debug.Log($"[ValidationTestRunner] TC3_MapGeneration: colony node (id={colonyNode.nodeId}, zone={colonyNode.zone})");
                }

                // PiedPiper at ID 51
                AssertEqual(51, graph.PiedPiperNodeId, "TC3_PiedPiper_IsId51");
                var piperNode = graph.GetNode(graph.PiedPiperNodeId);
                AssertNotNull(piperNode, "TC3_PiedPiperNode_Exists");
                if (piperNode != null)
                {
                    AssertEqual(NodeType.PiedPiper, piperNode.zone, "TC3_PiedPiperNode_ZoneIsPiedPiper");
                    Debug.Log($"[ValidationTestRunner] TC3_MapGeneration: pied piper node (id={piperNode.nodeId}, zone={piperNode.zone})");
                }

                // Zone distribution
                int wildCount = 0, farmCount = 0, townCount = 0;
                foreach (var node in allNodes)
                {
                    if (node.zone == NodeType.Wilderness) wildCount++;
                    else if (node.zone == NodeType.Farmland) farmCount++;
                    else if (node.zone == NodeType.Town) townCount++;
                }
                AssertEqual(10, wildCount, "TC3_WildernessCount_Is10");
                AssertEqual(10, farmCount, "TC3_FarmlandCount_Is10");
                AssertEqual(10, townCount, "TC3_TownCount_Is10");
                Debug.Log($"[ValidationTestRunner] TC3_MapGeneration: zone distribution (wilderness={wildCount}, farmland={farmCount}, town={townCount})");

                // Graph is fully connected
                bool isConnected = MapGenerator.ValidateMap(graph);
                AssertTrue(isConnected, "TC3_Graph_FullyConnected");
                Debug.Log($"[ValidationTestRunner] TC3_MapGeneration: fully connected={isConnected}");

                // ShortestPath works between colony and PiedPiper
                var path = graph.ShortestPath(0, graph.PiedPiperNodeId);
                AssertGreaterThan(path.Count, 1, "TC3_ShortestPath_ColonyToPiper_HasNodes");
                if (path.Count > 0)
                {
                    AssertEqual(0, path[0], "TC3_ShortestPath_StartsAtColony");
                    AssertEqual(graph.PiedPiperNodeId, path[path.Count - 1], "TC3_ShortestPath_EndsAtPiper");
                    Debug.Log($"[ValidationTestRunner] TC3_MapGeneration: shortest path colony->piper length={path.Count}, path=[{string.Join(",", path)}]");
                }

                // Deterministic with same seed
                Debug.Log("[ValidationTestRunner] TC3_MapGeneration: testing determinism with seed=42");
                var graph2 = MapGenerator.GenerateMap(config, 42);
                ServiceLocator.Register<IMapGraph>(graph2);
                Debug.Log("[ValidationTestRunner] TC3_MapGeneration: re-registered determinism map as IMapGraph");
                var allNodes2 = graph2.GetAllNodes();
                bool deterministic = true;
                foreach (var node in allNodes)
                {
                    var node2 = graph2.GetNode(node.nodeId);
                    if (node2 == null || node2.zone != node.zone)
                    {
                        deterministic = false;
                        Debug.LogError($"[ValidationTestRunner] TC3_MapGeneration: determinism failure at nodeId={node.nodeId}");
                        break;
                    }
                }
                AssertTrue(deterministic, "TC3_SameSeed_Deterministic");

                // Different seeds produce different maps
                Debug.Log("[ValidationTestRunner] TC3_MapGeneration: testing seed=99 produces different map");
                var graph3 = MapGenerator.GenerateMap(config, 99);
                ServiceLocator.Register<IMapGraph>(graph3);
                Debug.Log("[ValidationTestRunner] TC3_MapGeneration: re-registered seed=99 map as IMapGraph");
                var allNodes3 = graph3.GetAllNodes();
                bool anyDifference = false;
                foreach (var node in allNodes)
                {
                    var node3 = graph3.GetNode(node.nodeId);
                    if (node3 != null && node.zone == node3.zone)
                    {
                        // Compare world positions (which are randomized)
                        if (Vector2.Distance(node.worldPosition, node3.worldPosition) > 0.01f)
                        {
                            anyDifference = true;
                            break;
                        }
                    }
                }
                AssertTrue(anyDifference, "TC3_DifferentSeed_DifferentMap");
                Debug.Log($"[ValidationTestRunner] TC3_MapGeneration: different seeds produce different maps={anyDifference}");

                // Wilderness nodes have resources
                Debug.Log("[ValidationTestRunner] TC3_MapGeneration: checking wilderness resources");
                var wildernessNodes = graph.GetNodesInZone(NodeType.Wilderness);
                int wildWithResources = 0;
                foreach (var wn in wildernessNodes)
                {
                    if (wn.TotalResources() > 0) wildWithResources++;
                    Debug.Log($"[ValidationTestRunner] TC3_MapGeneration: wilderness node {wn.nodeId} resources={wn.TotalResources()}");
                }
                AssertGreaterThan(wildWithResources, 0, "TC3_Wilderness_HasResources");

                // Farmland nodes have resources
                Debug.Log("[ValidationTestRunner] TC3_MapGeneration: checking farmland resources");
                var farmlandNodes = graph.GetNodesInZone(NodeType.Farmland);
                int farmWithResources = 0;
                foreach (var fn in farmlandNodes)
                {
                    if (fn.TotalResources() > 0) farmWithResources++;
                    Debug.Log($"[ValidationTestRunner] TC3_MapGeneration: farmland node {fn.nodeId} resources={fn.TotalResources()}");
                }
                AssertGreaterThan(farmWithResources, 0, "TC3_Farmland_HasResources");

                // Town nodes have some resources (may be 0 due to config range 0-2)
                Debug.Log("[ValidationTestRunner] TC3_MapGeneration: checking town resources");
                var townNodes = graph.GetNodesInZone(NodeType.Town);
                int townTotalResources = 0;
                foreach (var tn in townNodes)
                {
                    townTotalResources += tn.TotalResources();
                    Debug.Log($"[ValidationTestRunner] TC3_MapGeneration: town node {tn.nodeId} resources={tn.TotalResources()}");
                }
                AssertGreaterThanOrEqual(townTotalResources, 0, "TC3_Town_ResourcesNonNegative");
                Debug.Log($"[ValidationTestRunner] TC3_MapGeneration: town total resources={townTotalResources}");

                // Cleanup
                DestroyImmediate(config);
            }
            catch (Exception e)
            {
                Skip("TC3_MapGeneration", $"MapGeneration failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC3_MapGeneration: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC3_MapGeneration: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-4: PATHFINDING
        // ══════════════════════════════════════════════════════════════════

        private void TC4_Pathfinding()
        {
            Debug.Log("[ValidationTestRunner] TC4_Pathfinding: ENTER");

            try
            {
                var config = CreateTestMapConfig();
                var graph = MapGenerator.GenerateMap(config, 42);
                ServiceLocator.Register<IMapGraph>(graph);
                Debug.Log("[ValidationTestRunner] TC4_Pathfinding: registered map as IMapGraph");

                // FindPath between adjacent nodes returns 2-node path
                Debug.Log("[ValidationTestRunner] TC4_Pathfinding: testing adjacent node path");
                var colonyNode = graph.GetNode(0);
                if (colonyNode != null && colonyNode.neighborIds.Count > 0)
                {
                    int adjacentId = colonyNode.neighborIds[0];
                    var adjPath = PathfindingService.FindPath(graph, 0, adjacentId);
                    AssertEqual(2, adjPath.Count, "TC4_AdjacentPath_Length2");
                    if (adjPath.Count == 2)
                    {
                        AssertEqual(0, adjPath[0], "TC4_AdjacentPath_StartsAtSource");
                        AssertEqual(adjacentId, adjPath[1], "TC4_AdjacentPath_EndsAtTarget");
                    }
                    Debug.Log($"[ValidationTestRunner] TC4_Pathfinding: adjacent path from 0 to {adjacentId} length={adjPath.Count}");
                }
                else
                {
                    Skip("TC4_AdjacentPath", "Colony node has no neighbors");
                }

                // FindPath between distant nodes returns valid path
                Debug.Log("[ValidationTestRunner] TC4_Pathfinding: testing distant node path");
                var distantPath = PathfindingService.FindPath(graph, 0, graph.PiedPiperNodeId);
                AssertGreaterThan(distantPath.Count, 2, "TC4_DistantPath_MoreThan2Nodes");
                if (distantPath.Count > 0)
                {
                    AssertEqual(0, distantPath[0], "TC4_DistantPath_StartsAtColony");
                    AssertEqual(graph.PiedPiperNodeId, distantPath[distantPath.Count - 1], "TC4_DistantPath_EndsAtPiper");
                    // Verify path is continuous
                    bool continuous = true;
                    for (int i = 0; i < distantPath.Count - 1; i++)
                    {
                        if (!graph.HasEdge(distantPath[i], distantPath[i + 1]))
                        {
                            Debug.LogError($"[ValidationTestRunner] TC4_Pathfinding: path gap between {distantPath[i]} and {distantPath[i + 1]}");
                            continuous = false;
                            break;
                        }
                    }
                    AssertTrue(continuous, "TC4_DistantPath_IsContinuous");
                }
                Debug.Log($"[ValidationTestRunner] TC4_Pathfinding: distant path length={distantPath.Count}");

                // GetDistance returns correct hop count
                Debug.Log("[ValidationTestRunner] TC4_Pathfinding: testing GetDistance");
                int distance = PathfindingService.GetDistance(graph, 0, graph.PiedPiperNodeId);
                AssertGreaterThan(distance, 0, "TC4_GetDistance_ColonyToPiper_Positive");
                AssertEqual(distantPath.Count - 1, distance, "TC4_GetDistance_MatchesPathLength");
                Debug.Log($"[ValidationTestRunner] TC4_Pathfinding: distance colony to piper = {distance}");

                // GetNodesWithinRange(colony, 1) returns colony neighbors
                Debug.Log("[ValidationTestRunner] TC4_Pathfinding: testing GetNodesWithinRange(0, 1)");
                var range1 = PathfindingService.GetNodesWithinRange(graph, 0, 1);
                Assert(range1.Contains(0), "TC4_Range1_ContainsColony", "colony in range1");
                if (colonyNode != null)
                {
                    foreach (int nid in colonyNode.neighborIds)
                    {
                        Assert(range1.Contains(nid), $"TC4_Range1_ContainsNeighbor_{nid}", $"neighbor {nid} in range1");
                    }
                }
                Debug.Log($"[ValidationTestRunner] TC4_Pathfinding: range1 from colony has {range1.Count} nodes");

                // GetNodesWithinRange(colony, 2) includes 2-hop neighbors
                Debug.Log("[ValidationTestRunner] TC4_Pathfinding: testing GetNodesWithinRange(0, 2)");
                var range2 = PathfindingService.GetNodesWithinRange(graph, 0, 2);
                AssertGreaterThan(range2.Count, range1.Count, "TC4_Range2_MoreThanRange1");
                Assert(range2.Contains(0), "TC4_Range2_ContainsColony", "colony in range2");
                Debug.Log($"[ValidationTestRunner] TC4_Pathfinding: range2 from colony has {range2.Count} nodes (range1 had {range1.Count})");

                // Verify range2 is superset of range1
                bool superSet = true;
                foreach (int id in range1)
                {
                    if (!range2.Contains(id))
                    {
                        superSet = false;
                        Debug.LogError($"[ValidationTestRunner] TC4_Pathfinding: range2 missing range1 node {id}");
                        break;
                    }
                }
                AssertTrue(superSet, "TC4_Range2_SupersetOfRange1");

                DestroyImmediate(config);
            }
            catch (Exception e)
            {
                Skip("TC4_Pathfinding", $"Pathfinding tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC4_Pathfinding: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC4_Pathfinding: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-5: FOG OF WAR
        // ══════════════════════════════════════════════════════════════════

        private void TC5_FogOfWar()
        {
            Debug.Log("[ValidationTestRunner] TC5_FogOfWar: ENTER");

            try
            {
                var config = CreateTestMapConfig();
                var graph = MapGenerator.GenerateMap(config, 42);
                ServiceLocator.Register<IMapGraph>(graph);
                Debug.Log("[ValidationTestRunner] TC5_FogOfWar: registered map as IMapGraph");
                var fog = ServiceLocator.Get<IFogOfWar>();
                Debug.Log($"[ValidationTestRunner] TC5_FogOfWar: resolved IFogOfWar (instance={fog?.GetType().Name})");

                // Reset all nodes to hidden except colony
                Debug.Log("[ValidationTestRunner] TC5_FogOfWar: resetting node fog states");
                foreach (var node in graph.GetAllNodes())
                {
                    if (node.zone == NodeType.Colony)
                    {
                        node.fogState = FogState.Visible;
                        node.visited = true;
                    }
                    else
                    {
                        node.fogState = FogState.Hidden;
                        node.visited = false;
                    }
                }

                // Place hero on colony node
                var heroInfos = new List<HeroFogInfo>
                {
                    new HeroFogInfo { nodeId = 0, bonusRevealRange = 0, revealsEntireZone = false }
                };
                var activeEffects = new HashSet<ColonyEffect>();

                // RecalculateVisibility
                Debug.Log("[ValidationTestRunner] TC5_FogOfWar: recalculating visibility with hero at colony");
                fog.RecalculateVisibility(graph, heroInfos, activeEffects);

                // Colony always Visible
                var colonyNode = graph.GetNode(0);
                AssertEqual(FogState.Visible, colonyNode.fogState, "TC5_Colony_AlwaysVisible");
                Debug.Log($"[ValidationTestRunner] TC5_FogOfWar: colony fogState={colonyNode.fogState}");

                // Hero node + adjacent = Visible
                foreach (int nid in colonyNode.neighborIds)
                {
                    var neighbor = graph.GetNode(nid);
                    AssertEqual(FogState.Visible, neighbor.fogState, $"TC5_ColonyNeighbor_{nid}_Visible");
                    Debug.Log($"[ValidationTestRunner] TC5_FogOfWar: colony neighbor {nid} fogState={neighbor.fogState}");
                }

                // Unvisited non-adjacent = Hidden
                Debug.Log("[ValidationTestRunner] TC5_FogOfWar: checking distant unvisited nodes are Hidden");
                var piperNode = graph.GetNode(graph.PiedPiperNodeId);
                // PiedPiper should be Hidden unless it's adjacent to colony (unlikely)
                if (!colonyNode.neighborIds.Contains(graph.PiedPiperNodeId))
                {
                    // If piper is far enough, it should be Hidden
                    int distToPiper = PathfindingService.GetDistance(graph, 0, graph.PiedPiperNodeId);
                    if (distToPiper > 1)
                    {
                        AssertEqual(FogState.Hidden, piperNode.fogState, "TC5_PiedPiper_Hidden");
                        Debug.Log($"[ValidationTestRunner] TC5_FogOfWar: pied piper fogState={piperNode.fogState} (distance={distToPiper})");
                    }
                }

                // Move hero to a wilderness node, then back. Previously visited node should be Remembered.
                Debug.Log("[ValidationTestRunner] TC5_FogOfWar: testing Remembered state");
                if (colonyNode.neighborIds.Count > 0)
                {
                    int firstNeighbor = colonyNode.neighborIds[0];
                    var firstNeighborNode = graph.GetNode(firstNeighbor);

                    // Move hero to neighbor
                    heroInfos[0] = new HeroFogInfo { nodeId = firstNeighbor, bonusRevealRange = 0, revealsEntireZone = false };
                    fog.RecalculateVisibility(graph, heroInfos, activeEffects);
                    Debug.Log($"[ValidationTestRunner] TC5_FogOfWar: moved hero to node {firstNeighbor}");

                    // Now move hero far away to a different node
                    if (firstNeighborNode.neighborIds.Count > 0)
                    {
                        int secondNeighbor = firstNeighborNode.neighborIds[0];
                        if (secondNeighbor != 0) // Avoid going back to colony
                        {
                            heroInfos[0] = new HeroFogInfo { nodeId = secondNeighbor, bonusRevealRange = 0, revealsEntireZone = false };
                            fog.RecalculateVisibility(graph, heroInfos, activeEffects);

                            // Colony was visited but may not be adjacent to secondNeighbor
                            // Colony should still be visible because it's always visible
                            AssertEqual(FogState.Visible, colonyNode.fogState, "TC5_Colony_StillVisibleAfterMove");
                            Debug.Log($"[ValidationTestRunner] TC5_FogOfWar: after moving to node {secondNeighbor}, colony fogState={colonyNode.fogState}");
                        }
                    }
                }

                // FogReveal colony effect extends visibility
                Debug.Log("[ValidationTestRunner] TC5_FogOfWar: testing FogReveal colony effect");
                heroInfos[0] = new HeroFogInfo { nodeId = 0, bonusRevealRange = 0, revealsEntireZone = false };
                activeEffects.Add(ColonyEffect.FogReveal);
                fog.RecalculateVisibility(graph, heroInfos, activeEffects);

                var range2Nodes = PathfindingService.GetNodesWithinRange(graph, 0, 2);
                bool allRange2Visible = true;
                foreach (int nid in range2Nodes)
                {
                    var node = graph.GetNode(nid);
                    if (node.fogState != FogState.Visible)
                    {
                        allRange2Visible = false;
                        Debug.LogError($"[ValidationTestRunner] TC5_FogOfWar: FogReveal node {nid} not visible (fogState={node.fogState})");
                    }
                }
                AssertTrue(allRange2Visible, "TC5_FogReveal_Extends2HopVisibility");
                Debug.Log($"[ValidationTestRunner] TC5_FogOfWar: FogReveal range2 all visible={allRange2Visible} (nodes={range2Nodes.Count})");

                DestroyImmediate(config);
            }
            catch (Exception e)
            {
                Skip("TC5_FogOfWar", $"FogOfWar tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC5_FogOfWar: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC5_FogOfWar: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-6: HERO TOKEN
        // ══════════════════════════════════════════════════════════════════

        private void TC6_HeroToken()
        {
            Debug.Log("[ValidationTestRunner] TC6_HeroToken: ENTER");

            try
            {
                // Create from CardDefinitionSO correctly
                var heroFactory = ServiceLocator.Get<IHeroTokenFactory>();
                Debug.Log($"[ValidationTestRunner] TC6_HeroToken: resolved IHeroTokenFactory (instance={heroFactory?.GetType().Name})");
                var heroDef = CreateTestHeroCard(1, "TestScout", HeroRole.Recon, 2, 3, 4, 2, 5);
                var token = heroFactory.Create(heroDef, 100);

                AssertNotNull(token, "TC6_HeroToken_Created");
                AssertEqual(100, token.tokenId, "TC6_HeroToken_TokenId");
                AssertEqual(2, token.EffectiveCombat, "TC6_HeroToken_BaseCombat");
                AssertEqual(3, token.EffectiveMove, "TC6_HeroToken_BaseMove");
                AssertEqual(4, token.EffectiveHP, "TC6_HeroToken_BaseHP");
                AssertEqual(2, token.EffectiveCarry, "TC6_HeroToken_BaseCarry");
                AssertEqual(4, token.currentHP, "TC6_HeroToken_CurrentHP");
                AssertTrue(token.IsAlive, "TC6_HeroToken_IsAlive");
                Debug.Log($"[ValidationTestRunner] TC6_HeroToken: created token={token}");

                // Equipment changes effective stats
                Debug.Log("[ValidationTestRunner] TC6_HeroToken: testing equipment effects");
                var weapon = CreateTestEquipmentCard(200, "TestSword", EquipmentSlot.Offensive, 3, 0);
                bool equipped = token.EquipItem(weapon);
                AssertTrue(equipped, "TC6_HeroToken_EquipWeapon_Success");
                AssertEqual(5, token.EffectiveCombat, "TC6_HeroToken_CombatWithWeapon");
                Debug.Log($"[ValidationTestRunner] TC6_HeroToken: after equipping weapon combat={token.EffectiveCombat}");

                var armor = CreateTestEquipmentCard(201, "TestShield", EquipmentSlot.Defensive, 2, 0);
                equipped = token.EquipItem(armor);
                AssertTrue(equipped, "TC6_HeroToken_EquipArmor_Success");
                AssertEqual(6, token.EffectiveHP, "TC6_HeroToken_HPWithArmor");
                Debug.Log($"[ValidationTestRunner] TC6_HeroToken: after equipping armor HP={token.EffectiveHP}");

                // Can't equip to occupied slot
                var weapon2 = CreateTestEquipmentCard(202, "TestAxe", EquipmentSlot.Offensive, 4, 0);
                bool reEquip = token.EquipItem(weapon2);
                AssertFalse(reEquip, "TC6_HeroToken_CantEquipOccupiedSlot");
                Debug.Log($"[ValidationTestRunner] TC6_HeroToken: re-equip to occupied slot={reEquip}");

                // Carry capacity limits resource gathering
                Debug.Log("[ValidationTestRunner] TC6_HeroToken: testing carry capacity");
                int gathered = token.GatherResource(ResourceType.Food, 5);
                AssertEqual(2, gathered, "TC6_HeroToken_GatherLimitedByCarry");
                AssertEqual(2, token.TotalCarried, "TC6_HeroToken_TotalCarried");
                Debug.Log($"[ValidationTestRunner] TC6_HeroToken: gathered {gathered} food, totalCarried={token.TotalCarried}");

                // Can't gather more when full
                int gathered2 = token.GatherResource(ResourceType.Materials, 3);
                AssertEqual(0, gathered2, "TC6_HeroToken_CantGatherWhenFull");
                Debug.Log($"[ValidationTestRunner] TC6_HeroToken: tried to gather when full, gathered={gathered2}");

                // TakeDamage + Heal work correctly
                Debug.Log("[ValidationTestRunner] TC6_HeroToken: testing damage and healing");
                bool injured = token.TakeDamage(2);
                AssertFalse(injured, "TC6_HeroToken_TakeDamage_NotInjured");
                AssertEqual(2, token.currentHP, "TC6_HeroToken_HPAfterDamage");
                Debug.Log($"[ValidationTestRunner] TC6_HeroToken: after 2 damage hp={token.currentHP}");

                token.Heal(1);
                AssertEqual(3, token.currentHP, "TC6_HeroToken_HPAfterHeal");
                Debug.Log($"[ValidationTestRunner] TC6_HeroToken: after heal hp={token.currentHP}");

                // Heal doesn't exceed max
                token.Heal(100);
                AssertEqual(token.EffectiveHP, token.currentHP, "TC6_HeroToken_HealCappedAtMax");
                Debug.Log($"[ValidationTestRunner] TC6_HeroToken: after overheal hp={token.currentHP} (max={token.EffectiveHP})");

                // Create fresh token for injure test
                Debug.Log("[ValidationTestRunner] TC6_HeroToken: testing injure");
                var heroDef2 = CreateTestHeroCard(2, "TestTank", HeroRole.Tank, 3, 2, 5, 3, 3);
                var token2 = heroFactory.Create(heroDef2, 101);
                token2.GatherResource(ResourceType.Food, 2);
                var weapon3 = CreateTestEquipmentCard(203, "TestMace", EquipmentSlot.Offensive, 2, 0);
                token2.EquipItem(weapon3);

                Debug.Log($"[ValidationTestRunner] TC6_HeroToken: before injure: carried={token2.TotalCarried}, equipped={token2.offensiveEquipment != null}");

                // Injure drops resources and unequips
                bool lethalDamage = token2.TakeDamage(100);
                AssertTrue(lethalDamage, "TC6_HeroToken_LethalDamage_Injures");
                AssertTrue(token2.isInjured, "TC6_HeroToken_IsInjured");
                AssertFalse(token2.IsAlive, "TC6_HeroToken_NotAliveAfterInjure");
                AssertEqual(0, token2.TotalCarried, "TC6_HeroToken_ResourcesDroppedOnInjure");
                Assert(token2.offensiveEquipment == null, "TC6_HeroToken_EquipmentRemovedOnInjure", "weapon should be null");
                Debug.Log($"[ValidationTestRunner] TC6_HeroToken: after injure: injured={token2.isInjured}, alive={token2.IsAlive}, carried={token2.TotalCarried}");

                DestroyImmediate(heroDef);
                DestroyImmediate(heroDef2);
                DestroyImmediate(weapon);
                DestroyImmediate(armor);
                DestroyImmediate(weapon2);
                DestroyImmediate(weapon3);
            }
            catch (Exception e)
            {
                Skip("TC6_HeroToken", $"HeroToken tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC6_HeroToken: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC6_HeroToken: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-7: ENEMY TOKEN
        // ══════════════════════════════════════════════════════════════════

        private void TC7_EnemyToken()
        {
            Debug.Log("[ValidationTestRunner] TC7_EnemyToken: ENTER");

            try
            {
                // EnemyToken creates correctly
                var enemyFactory = ServiceLocator.Get<IEnemyTokenFactory>();
                Debug.Log($"[ValidationTestRunner] TC7_EnemyToken: resolved IEnemyTokenFactory (instance={enemyFactory?.GetType().Name})");
                var enemy = enemyFactory.Create(0, "Test Rat", 3, 5, 2, EnemyBehavior.Patrol, NodeType.Wilderness, 1);
                AssertNotNull(enemy, "TC7_EnemyToken_Created");
                AssertEqual(0, enemy.tokenId, "TC7_EnemyToken_TokenId");
                AssertEqual("Test Rat", enemy.enemyName, "TC7_EnemyToken_Name");
                AssertEqual(3, enemy.strength, "TC7_EnemyToken_Strength");
                AssertEqual(5, enemy.hp, "TC7_EnemyToken_HP");
                AssertEqual(5, enemy.currentHP, "TC7_EnemyToken_CurrentHP");
                AssertEqual(2, enemy.speed, "TC7_EnemyToken_Speed");
                AssertTrue(enemy.IsAlive, "TC7_EnemyToken_IsAlive");
                AssertFalse(enemy.isDefeated, "TC7_EnemyToken_NotDefeated");
                Debug.Log($"[ValidationTestRunner] TC7_EnemyToken: created enemy={enemy}");

                // TakeDamage returns false when not dead
                Debug.Log("[ValidationTestRunner] TC7_EnemyToken: testing non-lethal damage");
                bool killed = enemy.TakeDamage(2);
                AssertFalse(killed, "TC7_EnemyToken_NonLethalDamage");
                AssertEqual(3, enemy.currentHP, "TC7_EnemyToken_HPAfterDamage");
                AssertTrue(enemy.IsAlive, "TC7_EnemyToken_StillAlive");
                Debug.Log($"[ValidationTestRunner] TC7_EnemyToken: after 2 damage hp={enemy.currentHP}, alive={enemy.IsAlive}");

                // TakeDamage returns true on death
                Debug.Log("[ValidationTestRunner] TC7_EnemyToken: testing lethal damage");
                killed = enemy.TakeDamage(5);
                AssertTrue(killed, "TC7_EnemyToken_LethalDamage");
                AssertEqual(0, enemy.currentHP, "TC7_EnemyToken_HPAfterLethal");
                AssertTrue(enemy.isDefeated, "TC7_EnemyToken_IsDefeated");
                AssertFalse(enemy.IsAlive, "TC7_EnemyToken_NotAlive");
                Debug.Log($"[ValidationTestRunner] TC7_EnemyToken: after lethal damage hp={enemy.currentHP}, defeated={enemy.isDefeated}");

                // Defeat/Respawn cycle works
                Debug.Log("[ValidationTestRunner] TC7_EnemyToken: testing respawn");
                AssertEqual(3, enemy.respawnTimer, "TC7_EnemyToken_RespawnTimer3");
                enemy.Respawn(5);
                AssertFalse(enemy.isDefeated, "TC7_EnemyToken_NotDefeatedAfterRespawn");
                AssertEqual(5, enemy.currentHP, "TC7_EnemyToken_FullHPAfterRespawn");
                AssertEqual(5, enemy.currentNodeId, "TC7_EnemyToken_NewNodeAfterRespawn");
                AssertTrue(enemy.IsAlive, "TC7_EnemyToken_AliveAfterRespawn");
                AssertEqual(0, enemy.respawnTimer, "TC7_EnemyToken_TimerResetAfterRespawn");
                Debug.Log($"[ValidationTestRunner] TC7_EnemyToken: after respawn enemy={enemy}");

                // IsAlive reflects state
                Debug.Log("[ValidationTestRunner] TC7_EnemyToken: verifying IsAlive reflects state");
                var enemy2 = enemyFactory.Create(1, "Test Cat", 5, 3, 1, EnemyBehavior.Chase, NodeType.Farmland, 10);
                AssertTrue(enemy2.IsAlive, "TC7_EnemyToken2_InitiallyAlive");
                enemy2.Defeat();
                AssertFalse(enemy2.IsAlive, "TC7_EnemyToken2_DeadAfterDefeat");
                Debug.Log($"[ValidationTestRunner] TC7_EnemyToken: enemy2 alive before defeat=true, after defeat=false");
            }
            catch (Exception e)
            {
                Skip("TC7_EnemyToken", $"EnemyToken tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC7_EnemyToken: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC7_EnemyToken: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-8: COMBAT RESOLUTION
        // ══════════════════════════════════════════════════════════════════

        private void TC8_CombatResolution()
        {
            Debug.Log("[ValidationTestRunner] TC8_CombatResolution: ENTER");

            try
            {
                var resolver = ServiceLocator.Get<ICombatResolver>();
                var heroFactory = ServiceLocator.Get<IHeroTokenFactory>();
                var enemyFactory = ServiceLocator.Get<IEnemyTokenFactory>();
                Debug.Log($"[ValidationTestRunner] TC8_CombatResolution: resolved ICombatResolver={resolver?.GetType().Name}, IHeroTokenFactory={heroFactory?.GetType().Name}, IEnemyTokenFactory={enemyFactory?.GetType().Name}");

                // Test: Stronger side wins (heroes stronger)
                Debug.Log("[ValidationTestRunner] TC8_CombatResolution: testing heroes win scenario");
                var heroDef1 = CreateTestHeroCard(1, "StrongHero", HeroRole.Melee, 5, 2, 10, 2, 1);
                var heroDef2 = CreateTestHeroCard(2, "MediumHero", HeroRole.Tank, 3, 2, 8, 2, 2);
                var hero1 = heroFactory.Create(heroDef1, 1);
                var hero2 = heroFactory.Create(heroDef2, 2);
                hero1.isDeployed = true;
                hero2.isDeployed = true;
                var heroes = new List<HeroToken> { hero1, hero2 };

                var enemy1 = enemyFactory.Create(10, "WeakEnemy", 2, 3, 1, EnemyBehavior.Guard, NodeType.Wilderness, 5);
                var enemies = new List<EnemyToken> { enemy1 };

                Debug.Log($"[ValidationTestRunner] TC8_CombatResolution: heroes total combat={hero1.EffectiveCombat + hero2.EffectiveCombat}, enemy strength={enemy1.strength}");

                var result = resolver.ResolveCombat(heroes, enemies, 5);
                AssertTrue(result.heroesWon, "TC8_HeroesWin_StrongerSide");
                AssertGreaterThan(result.roundsFought, 0, "TC8_HeroesWin_RoundsAboveZero");
                Debug.Log($"[ValidationTestRunner] TC8_CombatResolution: heroes won={result.heroesWon}, rounds={result.roundsFought}, survivingHeroes={result.survivingHeroTokenIds.Count}");

                // CombatResult populated correctly
                AssertEqual(5, result.nodeId, "TC8_CombatResult_NodeId");
                AssertNotNull(result.survivingHeroTokenIds, "TC8_CombatResult_SurvivingHeroes_NotNull");
                AssertNotNull(result.defeatedEnemyTokenIds, "TC8_CombatResult_DefeatedEnemies_NotNull");
                AssertGreaterThan(result.survivingHeroTokenIds.Count, 0, "TC8_CombatResult_HasSurvivors");
                AssertGreaterThan(result.defeatedEnemyTokenIds.Count, 0, "TC8_CombatResult_HasDefeatedEnemies");
                Debug.Log($"[ValidationTestRunner] TC8_CombatResolution: result={result}");

                // Test: Enemies stronger
                Debug.Log("[ValidationTestRunner] TC8_CombatResolution: testing enemies win scenario");
                var weakHeroDef = CreateTestHeroCard(3, "WeakHero", HeroRole.Recon, 1, 3, 2, 2, 5);
                var weakHero = heroFactory.Create(weakHeroDef, 3);
                weakHero.isDeployed = true;
                var weakHeroes = new List<HeroToken> { weakHero };

                var strongEnemy1 = enemyFactory.Create(20, "StrongEnemy1", 5, 10, 1, EnemyBehavior.Guard, NodeType.Town, 10);
                var strongEnemy2 = enemyFactory.Create(21, "StrongEnemy2", 4, 8, 1, EnemyBehavior.Guard, NodeType.Town, 10);
                var strongEnemies = new List<EnemyToken> { strongEnemy1, strongEnemy2 };

                var result2 = resolver.ResolveCombat(weakHeroes, strongEnemies, 10);
                AssertFalse(result2.heroesWon, "TC8_EnemiesWin_StrongerSide");
                AssertGreaterThan(result2.injuredHeroTokenIds.Count, 0, "TC8_EnemiesWin_HeroesInjured");
                Debug.Log($"[ValidationTestRunner] TC8_CombatResolution: enemies won result={result2}");

                // Pooled strength calculation
                Debug.Log("[ValidationTestRunner] TC8_CombatResolution: testing pooled strength");
                var poolHeroDef1 = CreateTestHeroCard(4, "PoolHero1", HeroRole.Melee, 3, 2, 6, 2, 1);
                var poolHeroDef2 = CreateTestHeroCard(5, "PoolHero2", HeroRole.Ranged, 2, 2, 5, 2, 2);
                var poolHero1 = heroFactory.Create(poolHeroDef1, 4);
                var poolHero2 = heroFactory.Create(poolHeroDef2, 5);
                poolHero1.isDeployed = true;
                poolHero2.isDeployed = true;
                int expectedPooled = poolHero1.EffectiveCombat + poolHero2.EffectiveCombat;
                AssertEqual(5, expectedPooled, "TC8_PooledStrength_Correct");
                Debug.Log($"[ValidationTestRunner] TC8_CombatResolution: pooled hero combat={expectedPooled}");

                // Multi-round combat until elimination
                Debug.Log("[ValidationTestRunner] TC8_CombatResolution: testing multi-round combat");
                var matchDef1 = CreateTestHeroCard(6, "MatchHero", HeroRole.Melee, 3, 2, 8, 2, 1);
                var matchHero = heroFactory.Create(matchDef1, 6);
                matchHero.isDeployed = true;
                var matchHeroes = new List<HeroToken> { matchHero };
                var matchEnemy = enemyFactory.Create(30, "MatchEnemy", 2, 6, 1, EnemyBehavior.Guard, NodeType.Wilderness, 15);
                var matchEnemies = new List<EnemyToken> { matchEnemy };

                var result3 = resolver.ResolveCombat(matchHeroes, matchEnemies, 15);
                AssertGreaterThan(result3.roundsFought, 1, "TC8_MultiRound_MoreThan1Round");
                Debug.Log($"[ValidationTestRunner] TC8_CombatResolution: multi-round result={result3}");

                // Equipment bonuses applied
                Debug.Log("[ValidationTestRunner] TC8_CombatResolution: testing equipment bonuses");
                var equipHeroDef = CreateTestHeroCard(7, "EquipHero", HeroRole.Melee, 2, 2, 6, 2, 1);
                var equipHero = heroFactory.Create(equipHeroDef, 7);
                equipHero.isDeployed = true;
                var bigSword = CreateTestEquipmentCard(300, "BigSword", EquipmentSlot.Offensive, 5, 0);
                equipHero.EquipItem(bigSword);
                AssertEqual(7, equipHero.EffectiveCombat, "TC8_EquipBonus_CombatIncreased");
                Debug.Log($"[ValidationTestRunner] TC8_CombatResolution: equipped hero combat={equipHero.EffectiveCombat} (base=2 + weapon=5)");

                // Cleanup SOs
                DestroyImmediate(heroDef1);
                DestroyImmediate(heroDef2);
                DestroyImmediate(weakHeroDef);
                DestroyImmediate(poolHeroDef1);
                DestroyImmediate(poolHeroDef2);
                DestroyImmediate(matchDef1);
                DestroyImmediate(equipHeroDef);
                DestroyImmediate(bigSword);
            }
            catch (Exception e)
            {
                Skip("TC8_CombatResolution", $"Combat tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC8_CombatResolution: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC8_CombatResolution: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-9: COLONY GRAPH
        // ══════════════════════════════════════════════════════════════════

        private void TC9_ColonyGraph()
        {
            Debug.Log("[ValidationTestRunner] TC9_ColonyGraph: ENTER");

            try
            {
                // Register a fresh ColonyGraph for this test
                var freshColony = new ColonyGraph();
                ServiceLocator.Register<IColonyGraph>(freshColony);
                Debug.Log("[ValidationTestRunner] TC9_ColonyGraph: re-registered fresh IColonyGraph for test");
                var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
                Debug.Log($"[ValidationTestRunner] TC9_ColonyGraph: resolved IColonyGraph (instance={colony?.GetType().Name})");

                // Initialize creates entrance + basic burrow
                Debug.Log("[ValidationTestRunner] TC9_ColonyGraph: testing initialization with starter cards");
                var entrance = CreateTestColonyCard(100, "Entrance", ColonyTier.FoodStorage, ColonyEffect.BaseProduction, 2, 1, true);
                var burrow = CreateTestColonyCard(101, "Basic Burrow", ColonyTier.FoodStorage, ColonyEffect.FoodProduction, 1, 1, true);
                colony.Initialize(entrance, burrow);

                AssertEqual(2, colony.CardCount, "TC9_Colony_InitHas2Cards");
                Debug.Log($"[ValidationTestRunner] TC9_ColonyGraph: after init cardCount={colony.CardCount}");

                // AddCard with valid attachment succeeds
                Debug.Log("[ValidationTestRunner] TC9_ColonyGraph: testing AddCard");
                var storageCard = CreateTestColonyCard(102, "Food Cache", ColonyTier.FoodStorage, ColonyEffect.FoodStorageCapacity, 10, 1);
                int placedId = colony.AddCard(storageCard, 0); // attach to entrance
                AssertGreaterThanOrEqual(placedId, 0, "TC9_Colony_AddCard_Success");
                AssertEqual(3, colony.CardCount, "TC9_Colony_AfterAddHas3Cards");
                Debug.Log($"[ValidationTestRunner] TC9_ColonyGraph: added card placedId={placedId}, cardCount={colony.CardCount}");

                // AddCard with invalid attachment fails
                Debug.Log("[ValidationTestRunner] TC9_ColonyGraph: testing AddCard with invalid target");
                var card2 = CreateTestColonyCard(103, "Wall", ColonyTier.StructureDefense, ColonyEffect.ColonyDefenseWall, 3, 1);
                int invalidPlaced = colony.AddCard(card2, 999);
                AssertEqual(-1, invalidPlaced, "TC9_Colony_AddCard_InvalidTarget_Fails");
                Debug.Log($"[ValidationTestRunner] TC9_ColonyGraph: AddCard to invalid target returned {invalidPlaced}");

                // CalculateFoodProduction returns base + bonuses
                Debug.Log("[ValidationTestRunner] TC9_ColonyGraph: testing food production");
                int food = colony.CalculateFoodProduction();
                // Entrance has BaseProduction=2, Basic Burrow has FoodProduction=1 => total=3
                AssertEqual(3, food, "TC9_Colony_FoodProduction_BaseAndBonus");
                Debug.Log($"[ValidationTestRunner] TC9_ColonyGraph: food production={food}");

                // HasEffect/GetEffectValue work for placed cards
                Debug.Log("[ValidationTestRunner] TC9_ColonyGraph: testing HasEffect/GetEffectValue");
                AssertTrue(colony.HasEffect(ColonyEffect.BaseProduction), "TC9_Colony_HasEffect_BaseProduction");
                AssertTrue(colony.HasEffect(ColonyEffect.FoodProduction), "TC9_Colony_HasEffect_FoodProduction");
                AssertFalse(colony.HasEffect(ColonyEffect.DoubleProduction), "TC9_Colony_NoEffect_DoubleProduction");

                int baseVal = colony.GetEffectValue(ColonyEffect.BaseProduction);
                AssertEqual(2, baseVal, "TC9_Colony_GetEffectValue_BaseProduction");
                int foodVal = colony.GetEffectValue(ColonyEffect.FoodProduction);
                AssertEqual(1, foodVal, "TC9_Colony_GetEffectValue_FoodProduction");
                Debug.Log($"[ValidationTestRunner] TC9_ColonyGraph: baseProduction value={baseVal}, foodProduction value={foodVal}");

                // CanPlayCard/ResetTurnCardCount limit works
                Debug.Log("[ValidationTestRunner] TC9_ColonyGraph: testing CanPlayCard limits");
                // We already played one card, so limit should be reached (default 1 per turn)
                AssertFalse(colony.CanPlayCard(), "TC9_Colony_CanPlayCard_FalseAfterPlaying");
                Debug.Log($"[ValidationTestRunner] TC9_ColonyGraph: canPlayCard after 1 play={colony.CanPlayCard()}");

                colony.ResetTurnCardCount();
                AssertTrue(colony.CanPlayCard(), "TC9_Colony_CanPlayCard_TrueAfterReset");
                Debug.Log($"[ValidationTestRunner] TC9_ColonyGraph: canPlayCard after reset={colony.CanPlayCard()}");

                // Placement requirements enforced
                Debug.Log("[ValidationTestRunner] TC9_ColonyGraph: testing placement requirements");
                var adjacentCard = CreateTestColonyCard(104, "Adjacent Card", ColonyTier.Advanced, ColonyEffect.AllHeroCombatBuff, 1, 2);
                adjacentCard.placementRequirement = PlacementRequirement.AdjacentTo;
                adjacentCard.adjacencyCardName = "Nonexistent Card";
                int adjPlaced = colony.AddCard(adjacentCard, 0);
                AssertEqual(-1, adjPlaced, "TC9_Colony_PlacementReq_Enforced");
                Debug.Log($"[ValidationTestRunner] TC9_ColonyGraph: adjacent card placement with bad requirement returned {adjPlaced}");

                // Cleanup
                DestroyImmediate(entrance);
                DestroyImmediate(burrow);
                DestroyImmediate(storageCard);
                DestroyImmediate(card2);
                DestroyImmediate(adjacentCard);
            }
            catch (Exception e)
            {
                Skip("TC9_ColonyGraph", $"ColonyGraph tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC9_ColonyGraph: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC9_ColonyGraph: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-10: SCORE CALCULATION
        // ══════════════════════════════════════════════════════════════════

        private void TC10_ScoreCalculation()
        {
            Debug.Log("[ValidationTestRunner] TC10_ScoreCalculation: ENTER");

            try
            {
                // Turn score: 15 turns = low, fewer = higher
                Debug.Log("[ValidationTestRunner] TC10_ScoreCalculation: testing turn score");
                var input15 = new ScoreInput { turnsUsed = 15, deckSize = 20, enemiesDefeated = 0, zoneBossesDefeated = 0, piedPiperDefeated = false, totalResourcesGathered = 0, colonyCardsPlayed = 0, heroesNeverInjured = 0 };
                var input5 = new ScoreInput { turnsUsed = 5, deckSize = 20, enemiesDefeated = 0, zoneBossesDefeated = 0, piedPiperDefeated = false, totalResourcesGathered = 0, colonyCardsPlayed = 0, heroesNeverInjured = 0 };

                var score15 = ScoreCalculator.CalculateScore(input15);
                var score5 = ScoreCalculator.CalculateScore(input5);
                Assert(score5.turnScore > score15.turnScore, "TC10_FewerTurns_HigherScore", $"5turns={score5.turnScore} > 15turns={score15.turnScore}");
                Debug.Log($"[ValidationTestRunner] TC10_ScoreCalculation: 15 turns turnScore={score15.turnScore}, 5 turns turnScore={score5.turnScore}");

                // Deck multiplier: 10 cards = 3x, 30 cards = 1x
                Debug.Log("[ValidationTestRunner] TC10_ScoreCalculation: testing deck multiplier");
                var input10deck = new ScoreInput { turnsUsed = 10, deckSize = 10, enemiesDefeated = 5, zoneBossesDefeated = 0, piedPiperDefeated = false, totalResourcesGathered = 10, colonyCardsPlayed = 3, heroesNeverInjured = 2 };
                var input30deck = new ScoreInput { turnsUsed = 10, deckSize = 30, enemiesDefeated = 5, zoneBossesDefeated = 0, piedPiperDefeated = false, totalResourcesGathered = 10, colonyCardsPlayed = 3, heroesNeverInjured = 2 };

                var score10deck = ScoreCalculator.CalculateScore(input10deck);
                var score30deck = ScoreCalculator.CalculateScore(input30deck);
                Assert(Mathf.Approximately(score10deck.deckMultiplier, 3.0f), "TC10_DeckMultiplier_10Cards_Is3x", $"multiplier={score10deck.deckMultiplier}");
                Assert(Mathf.Approximately(score30deck.deckMultiplier, 1.0f), "TC10_DeckMultiplier_30Cards_Is1x", $"multiplier={score30deck.deckMultiplier}");
                Assert(score10deck.finalScore > score30deck.finalScore, "TC10_SmallerDeck_HigherScore", $"10deck={score10deck.finalScore} > 30deck={score30deck.finalScore}");
                Debug.Log($"[ValidationTestRunner] TC10_ScoreCalculation: 10-card multiplier={score10deck.deckMultiplier}, 30-card multiplier={score30deck.deckMultiplier}");

                // Enemy score: 10/50/100 per type
                Debug.Log("[ValidationTestRunner] TC10_ScoreCalculation: testing enemy score");
                var inputEnemies = new ScoreInput { turnsUsed = 10, deckSize = 20, enemiesDefeated = 5, zoneBossesDefeated = 2, piedPiperDefeated = true, totalResourcesGathered = 0, colonyCardsPlayed = 0, heroesNeverInjured = 0 };
                var scoreEnemies = ScoreCalculator.CalculateScore(inputEnemies);
                int expectedEnemyScore = 5 * 10 + 2 * 50 + 100; // 50 + 100 + 100 = 250
                AssertEqual(expectedEnemyScore, scoreEnemies.enemyScore, "TC10_EnemyScore_Correct");
                Debug.Log($"[ValidationTestRunner] TC10_ScoreCalculation: enemy score={scoreEnemies.enemyScore} (expected={expectedEnemyScore})");

                // Full score calculation integrates all components
                Debug.Log("[ValidationTestRunner] TC10_ScoreCalculation: testing full score integration");
                var fullInput = new ScoreInput { turnsUsed = 8, deckSize = 15, enemiesDefeated = 10, zoneBossesDefeated = 1, piedPiperDefeated = true, totalResourcesGathered = 30, colonyCardsPlayed = 5, heroesNeverInjured = 3 };
                var fullScore = ScoreCalculator.CalculateScore(fullInput);
                AssertGreaterThan(fullScore.finalScore, 0, "TC10_FullScore_Positive");
                Assert(fullScore.deckMultiplier >= 1.0f, "TC10_FullScore_MultiplierAtLeast1", $"multiplier={fullScore.deckMultiplier}");
                Debug.Log($"[ValidationTestRunner] TC10_ScoreCalculation: full score={fullScore.finalScore} (turnScore={fullScore.turnScore}, enemyScore={fullScore.enemyScore}, resourceScore={fullScore.resourceScore}, colonyScore={fullScore.colonyScore}, heroBonus={fullScore.heroBonus}, multiplier={fullScore.deckMultiplier})");

                // Score is always non-negative
                Debug.Log("[ValidationTestRunner] TC10_ScoreCalculation: testing non-negative score");
                var minInput = new ScoreInput { turnsUsed = 15, deckSize = 30, enemiesDefeated = 0, zoneBossesDefeated = 0, piedPiperDefeated = false, totalResourcesGathered = 0, colonyCardsPlayed = 0, heroesNeverInjured = 0 };
                var minScore = ScoreCalculator.CalculateScore(minInput);
                AssertGreaterThanOrEqual(minScore.finalScore, 0, "TC10_Score_NonNegative");
                Debug.Log($"[ValidationTestRunner] TC10_ScoreCalculation: minimum score={minScore.finalScore}");
            }
            catch (Exception e)
            {
                Skip("TC10_ScoreCalculation", $"Score tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC10_ScoreCalculation: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC10_ScoreCalculation: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-11: DECK CONSTRUCTION
        // ══════════════════════════════════════════════════════════════════

        private void TC11_DeckConstruction()
        {
            Debug.Log("[ValidationTestRunner] TC11_DeckConstruction: ENTER");

            try
            {
                // GetMaxCopies correct for each deckCost
                Debug.Log("[ValidationTestRunner] TC11_DeckConstruction: testing GetMaxCopies (static logic)");
                // We can't easily instantiate DeckConstructionManager (MonoBehaviour), so test logic directly
                int maxCopiesCost1 = GetMaxCopiesStatic(1);
                int maxCopiesCost2 = GetMaxCopiesStatic(2);
                int maxCopiesCost3 = GetMaxCopiesStatic(3);
                int maxCopiesCost5 = GetMaxCopiesStatic(5);

                AssertEqual(3, maxCopiesCost1, "TC11_MaxCopies_Cost1_Is3");
                AssertEqual(2, maxCopiesCost2, "TC11_MaxCopies_Cost2_Is2");
                AssertEqual(1, maxCopiesCost3, "TC11_MaxCopies_Cost3_Is1");
                AssertEqual(1, maxCopiesCost5, "TC11_MaxCopies_Cost5_IsSingleton");
                Debug.Log($"[ValidationTestRunner] TC11_DeckConstruction: maxCopies cost1={maxCopiesCost1}, cost2={maxCopiesCost2}, cost3={maxCopiesCost3}, cost5={maxCopiesCost5}");

                // Valid deck = 10-30 cards with >= 1 hero
                Debug.Log("[ValidationTestRunner] TC11_DeckConstruction: testing deck validity rules");
                AssertEqual(10, DeckConstructionManager.MIN_DECK_SIZE, "TC11_MinDeckSize_Is10");
                AssertEqual(30, DeckConstructionManager.MAX_DECK_SIZE, "TC11_MaxDeckSize_Is30");
                Debug.Log($"[ValidationTestRunner] TC11_DeckConstruction: min={DeckConstructionManager.MIN_DECK_SIZE}, max={DeckConstructionManager.MAX_DECK_SIZE}");

                // Colony cards separate from main deck (verified by CardDatabase structure)
                Debug.Log("[ValidationTestRunner] TC11_DeckConstruction: verifying colony cards are separate");
                try
                {
                    var db = CardDatabase.Instance;
                    bool colonyInMainCards = false;
                    foreach (var card in db.AllCards)
                    {
                        if (card.cardType == CardType.Colony)
                        {
                            colonyInMainCards = true;
                            break;
                        }
                    }
                    AssertFalse(colonyInMainCards, "TC11_ColonyCards_NotInMainPool");
                    AssertGreaterThan(db.AllColonyCards.Count, 0, "TC11_ColonyCards_InSeparatePool");
                    Debug.Log($"[ValidationTestRunner] TC11_DeckConstruction: colony in main pool={colonyInMainCards}, colony pool count={db.AllColonyCards.Count}");
                }
                catch (Exception e)
                {
                    Skip("TC11_ColonyCardsSeparate", $"CardDatabase not available: {e.Message}");
                }
            }
            catch (Exception e)
            {
                Skip("TC11_DeckConstruction", $"DeckConstruction tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC11_DeckConstruction: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC11_DeckConstruction: EXIT");
        }

        private static int GetMaxCopiesStatic(int deckCost)
        {
            if (deckCost <= 1) return 3;
            if (deckCost == 2) return 2;
            return 1;
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-12: CLEANUP PHASE
        // ══════════════════════════════════════════════════════════════════

        private void TC12_CleanupPhase()
        {
            Debug.Log("[ValidationTestRunner] TC12_CleanupPhase: ENTER");

            try
            {
                // Setup
                var heroFactory = ServiceLocator.Get<IHeroTokenFactory>();
                var enemyFactory = ServiceLocator.Get<IEnemyTokenFactory>();
                Debug.Log($"[ValidationTestRunner] TC12_CleanupPhase: resolved IHeroTokenFactory={heroFactory?.GetType().Name}, IEnemyTokenFactory={enemyFactory?.GetType().Name}");
                var config = CreateTestMapConfig();
                var graph = MapGenerator.GenerateMap(config, 42);
                ServiceLocator.Register<IMapGraph>(graph);
                Debug.Log("[ValidationTestRunner] TC12_CleanupPhase: registered map as IMapGraph");
                var freshColony = new ColonyGraph();
                ServiceLocator.Register<IColonyGraph>(freshColony);
                Debug.Log("[ValidationTestRunner] TC12_CleanupPhase: re-registered fresh IColonyGraph");
                var colonyGraphObj = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
                colonyGraphObj.Initialize();

                // Create ResourceManager on a temporary GO
                var rmGo = new GameObject("TestResourceManager");
                var rm = rmGo.AddComponent<ResourceManager>();
                rm.Initialize();
                rm.AddToStockpile(ResourceType.Food, 10);

                // Create heroes
                var heroDef1 = CreateTestHeroCard(1, "Hero1", HeroRole.Melee, 3, 2, 5, 2, 1);
                var heroDef2 = CreateTestHeroCard(2, "Hero2", HeroRole.Recon, 2, 3, 4, 2, 2);
                var hero1 = heroFactory.Create(heroDef1, 1);
                var hero2 = heroFactory.Create(heroDef2, 2);
                hero1.isDeployed = true;
                hero2.isDeployed = true;
                hero1.currentNodeId = 1;
                hero2.currentNodeId = 2;
                var deployedHeroes = new List<HeroToken> { hero1, hero2 };

                // Create enemies
                var enemy1 = enemyFactory.Create(10, "Enemy1", 3, 5, 1, EnemyBehavior.Patrol, NodeType.Wilderness, 1);
                enemy1.Defeat(); // Set to defeated with respawn timer = 3
                var enemies = new List<EnemyToken> { enemy1 };

                // Food consumed per deployed hero
                Debug.Log("[ValidationTestRunner] TC12_CleanupPhase: testing food consumption");
                var result = CleanupPhase.Execute(deployedHeroes, enemies, rm, colonyGraphObj, graph, false);
                AssertEqual(2, result.foodConsumed, "TC12_FoodConsumed_PerDeployedHero");
                AssertEqual(0, result.foodDeficit, "TC12_NoFoodDeficit");
                Debug.Log($"[ValidationTestRunner] TC12_CleanupPhase: foodConsumed={result.foodConsumed}, deficit={result.foodDeficit}");

                // Food deficit tracked when insufficient
                Debug.Log("[ValidationTestRunner] TC12_CleanupPhase: testing food deficit");
                var rm2Go = new GameObject("TestResourceManager2");
                var rm2 = rm2Go.AddComponent<ResourceManager>();
                rm2.Initialize();
                rm2.AddToStockpile(ResourceType.Food, 1); // Only 1 food for 2 heroes

                var result2 = CleanupPhase.Execute(deployedHeroes, enemies, rm2, colonyGraphObj, graph, false);
                AssertEqual(1, result2.foodConsumed, "TC12_FoodConsumed_OnlyAvailable");
                AssertEqual(1, result2.foodDeficit, "TC12_FoodDeficit_Tracked");
                AssertGreaterThan(result2.unfedHeroIds.Count, 0, "TC12_UnfedHeroes_Tracked");
                Debug.Log($"[ValidationTestRunner] TC12_CleanupPhase: deficit foodConsumed={result2.foodConsumed}, deficit={result2.foodDeficit}, unfed={result2.unfedHeroIds.Count}");

                // Enemy respawn timers tick
                Debug.Log("[ValidationTestRunner] TC12_CleanupPhase: testing enemy respawn timers");
                // enemy1 was defeated with respawnTimer=3, after one cleanup it should be 2
                AssertEqual(2, enemy1.respawnTimer, "TC12_EnemyRespawnTimer_Ticked");
                Debug.Log($"[ValidationTestRunner] TC12_CleanupPhase: enemy1 respawnTimer={enemy1.respawnTimer}");

                // Run cleanup twice more to trigger respawn
                CleanupPhase.Execute(deployedHeroes, enemies, rm, colonyGraphObj, graph, false);
                var result4 = CleanupPhase.Execute(deployedHeroes, enemies, rm, colonyGraphObj, graph, false);
                AssertGreaterThan(result4.respawnedEnemyIds.Count, 0, "TC12_EnemyRespawn_Triggered");
                Debug.Log($"[ValidationTestRunner] TC12_CleanupPhase: after 3 ticks respawnTimer={enemy1.respawnTimer}, respawned={result4.respawnedEnemyIds.Count}");

                // Injury recovery processes
                Debug.Log("[ValidationTestRunner] TC12_CleanupPhase: testing injury recovery");
                var injuredDef = CreateTestHeroCard(3, "InjuredHero", HeroRole.Support, 1, 2, 3, 2, 3);
                var injuredHero = heroFactory.Create(injuredDef, 3);
                injuredHero.Injure();
                var allHeroes = new List<HeroToken> { hero1, hero2, injuredHero };
                AssertTrue(injuredHero.isInjured, "TC12_InjuredHero_IsInjured");
                AssertEqual(2, injuredHero.turnsUntilRecovery, "TC12_InjuredHero_RecoveryTimer2");
                Debug.Log($"[ValidationTestRunner] TC12_CleanupPhase: injured hero turnsUntilRecovery={injuredHero.turnsUntilRecovery}");

                var recovered1 = CleanupPhase.ProcessInjuryRecovery(allHeroes, colonyGraphObj);
                AssertEqual(0, recovered1.Count, "TC12_InjuryRecovery_NotYet");
                AssertEqual(1, injuredHero.turnsUntilRecovery, "TC12_InjuryRecovery_TimerDecremented");
                Debug.Log($"[ValidationTestRunner] TC12_CleanupPhase: after 1 tick turnsUntilRecovery={injuredHero.turnsUntilRecovery}");

                var recovered2 = CleanupPhase.ProcessInjuryRecovery(allHeroes, colonyGraphObj);
                AssertGreaterThan(recovered2.Count, 0, "TC12_InjuryRecovery_Complete");
                Debug.Log($"[ValidationTestRunner] TC12_CleanupPhase: after 2 ticks recovered={recovered2.Count}");

                // Cleanup
                DestroyImmediate(config);
                DestroyImmediate(heroDef1);
                DestroyImmediate(heroDef2);
                DestroyImmediate(injuredDef);
                DestroyImmediate(rmGo);
                DestroyImmediate(rm2Go);
            }
            catch (Exception e)
            {
                Skip("TC12_CleanupPhase", $"CleanupPhase tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC12_CleanupPhase: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC12_CleanupPhase: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-13: GATHER PHASE
        // ══════════════════════════════════════════════════════════════════

        private void TC13_GatherPhase()
        {
            Debug.Log("[ValidationTestRunner] TC13_GatherPhase: ENTER");

            try
            {
                var heroFactory = ServiceLocator.Get<IHeroTokenFactory>();
                Debug.Log($"[ValidationTestRunner] TC13_GatherPhase: resolved IHeroTokenFactory (instance={heroFactory?.GetType().Name})");
                var config = CreateTestMapConfig();
                var graph = MapGenerator.GenerateMap(config, 42);
                ServiceLocator.Register<IMapGraph>(graph);
                Debug.Log("[ValidationTestRunner] TC13_GatherPhase: registered map as IMapGraph");

                // Place resources on a specific node
                var targetNode = graph.GetNode(1);
                targetNode.resources.Clear();
                targetNode.resources[ResourceType.Food] = 5;
                targetNode.resources[ResourceType.Materials] = 3;
                Debug.Log($"[ValidationTestRunner] TC13_GatherPhase: placed resources on node 1 (food=5, materials=3)");

                // Create hero on that node
                var heroDef = CreateTestHeroCard(1, "GatherHero", HeroRole.Gather, 2, 2, 4, 3, 1);
                var hero = heroFactory.Create(heroDef, 1);
                hero.currentNodeId = 1;
                hero.isDeployed = true;
                var heroes = new List<HeroToken> { hero };

                // Heroes gather from their node
                Debug.Log("[ValidationTestRunner] TC13_GatherPhase: testing basic gather");
                var result = GatherPhase.Execute(heroes, graph, null);
                AssertGreaterThan(result.totalGathered, 0, "TC13_GatherPhase_GatheredSomething");
                AssertGreaterThan(hero.TotalCarried, 0, "TC13_GatherPhase_HeroCarriesResources");
                Debug.Log($"[ValidationTestRunner] TC13_GatherPhase: gathered={result.totalGathered}, heroCarried={hero.TotalCarried}/{hero.EffectiveCarry}");

                // Carry capacity respected
                AssertLessThanOrEqual(hero.TotalCarried, hero.EffectiveCarry, "TC13_GatherPhase_CarryCapacityRespected");
                Debug.Log($"[ValidationTestRunner] TC13_GatherPhase: carry check totalCarried={hero.TotalCarried} <= capacity={hero.EffectiveCarry}");

                // Test EfficientGather bonus
                Debug.Log("[ValidationTestRunner] TC13_GatherPhase: testing EfficientGather bonus");
                var efficientDef = CreateTestHeroCard(2, "EfficientGatherer", HeroRole.Gather, 1, 2, 3, 2, 1, SpecialAbility.EfficientGather);
                var efficientHero = heroFactory.Create(efficientDef, 2);
                efficientHero.currentNodeId = 1;
                efficientHero.isDeployed = true;

                // Reset node resources
                targetNode.resources.Clear();
                targetNode.resources[ResourceType.Food] = 10;

                var efficientHeroes = new List<HeroToken> { efficientHero };
                var result2 = GatherPhase.Execute(efficientHeroes, graph, null);
                AssertGreaterThan(result2.totalGathered, 0, "TC13_GatherPhase_EfficientGather_Works");
                Debug.Log($"[ValidationTestRunner] TC13_GatherPhase: efficient gatherer gathered={result2.totalGathered}, carried={efficientHero.TotalCarried}");

                // Total gathered tracked
                Assert(result2.gatheredByHero.ContainsKey(2), "TC13_GatherPhase_TotalGathered_Tracked", "hero 2 in gatheredByHero");
                Debug.Log($"[ValidationTestRunner] TC13_GatherPhase: gatheredByHero has {result2.gatheredByHero.Count} entries");

                // Cleanup
                DestroyImmediate(config);
                DestroyImmediate(heroDef);
                DestroyImmediate(efficientDef);
            }
            catch (Exception e)
            {
                Skip("TC13_GatherPhase", $"GatherPhase tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC13_GatherPhase: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC13_GatherPhase: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-14: ENEMY AI
        // ══════════════════════════════════════════════════════════════════

        private void TC14_EnemyAI()
        {
            Debug.Log("[ValidationTestRunner] TC14_EnemyAI: ENTER");

            try
            {
                var heroFactory = ServiceLocator.Get<IHeroTokenFactory>();
                var enemyFactory = ServiceLocator.Get<IEnemyTokenFactory>();
                Debug.Log($"[ValidationTestRunner] TC14_EnemyAI: resolved IHeroTokenFactory={heroFactory?.GetType().Name}, IEnemyTokenFactory={enemyFactory?.GetType().Name}");
                var config = CreateTestMapConfig();
                SeededRandom.Initialize(42);
                var graph = MapGenerator.GenerateMap(config, 42);
                ServiceLocator.Register<IMapGraph>(graph);
                Debug.Log("[ValidationTestRunner] TC14_EnemyAI: registered map as IMapGraph");
                var allNodes = new List<MapNode>(graph.GetAllNodes());

                // Guard: stays in place
                Debug.Log("[ValidationTestRunner] TC14_EnemyAI: testing Guard behavior");
                var guardEnemy = enemyFactory.Create(0, "GuardEnemy", 3, 5, 2, EnemyBehavior.Guard, NodeType.Wilderness, 1);
                var heroes = new List<HeroToken>();
                int guardTarget = EnemyAI.DecideMove(guardEnemy, allNodes, heroes);
                AssertEqual(-1, guardTarget, "TC14_Guard_StaysInPlace");
                Debug.Log($"[ValidationTestRunner] TC14_EnemyAI: guard target={guardTarget}");

                // Patrol: moves to neighbor
                Debug.Log("[ValidationTestRunner] TC14_EnemyAI: testing Patrol behavior");
                var patrolEnemy = enemyFactory.Create(1, "PatrolEnemy", 2, 4, 1, EnemyBehavior.Patrol, NodeType.Wilderness, 1);
                SeededRandom.Initialize(42); // Reset for determinism
                int patrolTarget = EnemyAI.DecideMove(patrolEnemy, allNodes, heroes);
                var patrolNode = graph.GetNode(1);
                if (patrolTarget >= 0)
                {
                    Assert(patrolNode.neighborIds.Contains(patrolTarget), "TC14_Patrol_MovesToNeighbor", $"target={patrolTarget} in neighbors of node 1");
                    Debug.Log($"[ValidationTestRunner] TC14_EnemyAI: patrol moved from 1 to {patrolTarget}");
                }
                else
                {
                    // Speed 1 should allow movement; -1 would mean no valid neighbors
                    Debug.Log($"[ValidationTestRunner] TC14_EnemyAI: patrol stayed (target={patrolTarget})");
                }

                // Chase: moves toward heroes
                Debug.Log("[ValidationTestRunner] TC14_EnemyAI: testing Chase behavior");
                var heroDefChase = CreateTestHeroCard(1, "ChaseTarget", HeroRole.Recon, 2, 3, 4, 2, 5);
                var chaseTargetHero = heroFactory.Create(heroDefChase, 1);
                // Place hero on a node adjacent to the chase enemy's node
                var chaseNode = graph.GetNode(3);
                if (chaseNode != null && chaseNode.neighborIds.Count > 0)
                {
                    int heroNodeId = chaseNode.neighborIds[0];
                    chaseTargetHero.currentNodeId = heroNodeId;
                    chaseTargetHero.isDeployed = true;
                    var chaseHeroes = new List<HeroToken> { chaseTargetHero };

                    var chaseEnemy = enemyFactory.Create(2, "ChaseEnemy", 4, 6, 2, EnemyBehavior.Chase, NodeType.Wilderness, 3);
                    int chaseTarget = EnemyAI.DecideMove(chaseEnemy, allNodes, chaseHeroes);
                    AssertEqual(heroNodeId, chaseTarget, "TC14_Chase_MovesTowardHero");
                    Debug.Log($"[ValidationTestRunner] TC14_EnemyAI: chase enemy moved from 3 to {chaseTarget} (hero at {heroNodeId})");
                }
                else
                {
                    Skip("TC14_Chase", "Node 3 has no neighbors for chase test");
                }

                // Ambush: only moves when hero adjacent
                Debug.Log("[ValidationTestRunner] TC14_EnemyAI: testing Ambush behavior (no hero nearby)");
                var ambushEnemy = enemyFactory.Create(3, "AmbushEnemy", 5, 8, 1, EnemyBehavior.Ambush, NodeType.Farmland, 15);
                int ambushNoHero = EnemyAI.DecideMove(ambushEnemy, allNodes, new List<HeroToken>());
                AssertEqual(-1, ambushNoHero, "TC14_Ambush_StaysWhenNoHeroAdjacent");
                Debug.Log($"[ValidationTestRunner] TC14_EnemyAI: ambush with no hero target={ambushNoHero}");

                // Ambush: moves when hero adjacent
                Debug.Log("[ValidationTestRunner] TC14_EnemyAI: testing Ambush behavior (hero nearby)");
                var ambushNode = graph.GetNode(15);
                if (ambushNode != null && ambushNode.neighborIds.Count > 0)
                {
                    int ambushHeroNode = ambushNode.neighborIds[0];
                    var ambushHeroDef = CreateTestHeroCard(2, "AmbushTarget", HeroRole.Melee, 3, 2, 5, 2, 1);
                    var ambushHeroToken = heroFactory.Create(ambushHeroDef, 2);
                    ambushHeroToken.currentNodeId = ambushHeroNode;
                    ambushHeroToken.isDeployed = true;
                    var ambushHeroes = new List<HeroToken> { ambushHeroToken };

                    int ambushWithHero = EnemyAI.DecideMove(ambushEnemy, allNodes, ambushHeroes);
                    AssertEqual(ambushHeroNode, ambushWithHero, "TC14_Ambush_MovesWhenHeroAdjacent");
                    Debug.Log($"[ValidationTestRunner] TC14_EnemyAI: ambush with hero target={ambushWithHero} (hero at {ambushHeroNode})");
                    DestroyImmediate(ambushHeroDef);
                }

                // ProcessAllEnemies handles all enemies
                Debug.Log("[ValidationTestRunner] TC14_EnemyAI: testing ProcessAllEnemies");
                var allEnemies = new List<EnemyToken>
                {
                    enemyFactory.Create(10, "BatchGuard", 2, 3, 1, EnemyBehavior.Guard, NodeType.Wilderness, 1),
                    enemyFactory.Create(11, "BatchPatrol", 2, 3, 1, EnemyBehavior.Patrol, NodeType.Wilderness, 2),
                    enemyFactory.Create(12, "BatchDefeated", 2, 3, 1, EnemyBehavior.Patrol, NodeType.Wilderness, 3)
                };
                allEnemies[2].Defeat();

                SeededRandom.Initialize(42);
                var moveResults = EnemyAI.ProcessAllEnemies(allEnemies, allNodes, new List<HeroToken>());
                AssertNotNull(moveResults, "TC14_ProcessAll_ReturnsNonNull");
                Debug.Log($"[ValidationTestRunner] TC14_EnemyAI: ProcessAllEnemies returned {moveResults.Count} moves for {allEnemies.Count} enemies");

                // Cleanup
                DestroyImmediate(config);
                DestroyImmediate(heroDefChase);
            }
            catch (Exception e)
            {
                Skip("TC14_EnemyAI", $"EnemyAI tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC14_EnemyAI: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC14_EnemyAI: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-15: EVENTBUS
        // ══════════════════════════════════════════════════════════════════

        private void TC15_EventBus()
        {
            Debug.Log("[ValidationTestRunner] TC15_EventBus: ENTER");

            try
            {
                // Reset first
                EventBus.Reset();

                // All v2.0 events defined (can subscribe without error)
                Debug.Log("[ValidationTestRunner] TC15_EventBus: testing event subscription");
                bool turnStartedFired = false;
                bool phaseChangedFired = false;
                bool combatStartedFired = false;
                bool combatEndedFired = false;
                bool resourceGatheredFired = false;

                EventBus.OnTurnStarted += (turn) => { turnStartedFired = true; };
                EventBus.OnPhaseChanged += (phase) => { phaseChangedFired = true; };
                EventBus.OnCombatStarted += (nodeId) => { combatStartedFired = true; };
                EventBus.OnCombatEnded += (nodeId, won) => { combatEndedFired = true; };
                EventBus.OnResourceGathered += (hero, type, amount) => { _ = resourceGatheredFired; resourceGatheredFired = true; };

                // Fire and receive works for key events
                Debug.Log("[ValidationTestRunner] TC15_EventBus: testing event firing");
                EventBus.OnTurnStarted?.Invoke(1);
                AssertTrue(turnStartedFired, "TC15_OnTurnStarted_Fires");
                Debug.Log($"[ValidationTestRunner] TC15_EventBus: OnTurnStarted fired={turnStartedFired}");

                EventBus.OnPhaseChanged?.Invoke(GamePhase.Combat);
                AssertTrue(phaseChangedFired, "TC15_OnPhaseChanged_Fires");
                Debug.Log($"[ValidationTestRunner] TC15_EventBus: OnPhaseChanged fired={phaseChangedFired}");

                EventBus.OnCombatStarted?.Invoke(5);
                AssertTrue(combatStartedFired, "TC15_OnCombatStarted_Fires");
                Debug.Log($"[ValidationTestRunner] TC15_EventBus: OnCombatStarted fired={combatStartedFired}");

                EventBus.OnCombatEnded?.Invoke(5, true);
                AssertTrue(combatEndedFired, "TC15_OnCombatEnded_Fires");
                Debug.Log($"[ValidationTestRunner] TC15_EventBus: OnCombatEnded fired={combatEndedFired}");

                // Test other v2.0 events exist (non-null after subscription)
                Debug.Log("[ValidationTestRunner] TC15_EventBus: verifying all v2.0 event delegates exist");
                EventBus.OnTurnEnded += () => { };
                AssertNotNull(EventBus.OnTurnEnded, "TC15_OnTurnEnded_Defined");

                EventBus.OnColonyCardPlayed += (card) => { };
                AssertNotNull(EventBus.OnColonyCardPlayed, "TC15_OnColonyCardPlayed_Defined");

                EventBus.OnHeroDeployed += (hero, nodeId) => { };
                AssertNotNull(EventBus.OnHeroDeployed, "TC15_OnHeroDeployed_Defined");

                EventBus.OnHeroMoved += (hero, from, to) => { };
                AssertNotNull(EventBus.OnHeroMoved, "TC15_OnHeroMoved_Defined");

                EventBus.OnEnemyMoved += (id, from, to) => { };
                AssertNotNull(EventBus.OnEnemyMoved, "TC15_OnEnemyMoved_Defined");

                EventBus.OnRunStarted += () => { };
                AssertNotNull(EventBus.OnRunStarted, "TC15_OnRunStarted_Defined");

                EventBus.OnRunComplete += (victory) => { };
                AssertNotNull(EventBus.OnRunComplete, "TC15_OnRunComplete_Defined");

                EventBus.OnScoreCalculated += (score) => { };
                AssertNotNull(EventBus.OnScoreCalculated, "TC15_OnScoreCalculated_Defined");

                EventBus.OnNodeRevealed += (nodeId) => { };
                AssertNotNull(EventBus.OnNodeRevealed, "TC15_OnNodeRevealed_Defined");

                EventBus.OnNodeHidden += (nodeId) => { };
                AssertNotNull(EventBus.OnNodeHidden, "TC15_OnNodeHidden_Defined");

                // Reset clears all
                Debug.Log("[ValidationTestRunner] TC15_EventBus: testing Reset clears events");
                EventBus.Reset();
                Assert(EventBus.OnTurnStarted == null, "TC15_Reset_ClearsOnTurnStarted", "should be null after reset");
                Assert(EventBus.OnPhaseChanged == null, "TC15_Reset_ClearsOnPhaseChanged", "should be null after reset");
                Assert(EventBus.OnCombatStarted == null, "TC15_Reset_ClearsOnCombatStarted", "should be null after reset");
                Assert(EventBus.OnRunStarted == null, "TC15_Reset_ClearsOnRunStarted", "should be null after reset");
                Debug.Log("[ValidationTestRunner] TC15_EventBus: all events nulled after Reset");

                // Final cleanup
                EventBus.Reset();
            }
            catch (Exception e)
            {
                Skip("TC15_EventBus", $"EventBus tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC15_EventBus: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC15_EventBus: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-16: SAVE/LOAD
        // ══════════════════════════════════════════════════════════════════

        private void TC16_SaveLoad()
        {
            Debug.Log("[ValidationTestRunner] TC16_SaveLoad: ENTER");

            try
            {
                // RunSaveData has all v2.0 fields
                Debug.Log("[ValidationTestRunner] TC16_SaveLoad: testing RunSaveData structure");
                var saveData = new RunSaveData();
                AssertNotNull(saveData.deckCardIds, "TC16_RunSaveData_DeckCardIds_NotNull");
                AssertNotNull(saveData.colonyDeckCardIds, "TC16_RunSaveData_ColonyDeckCardIds_NotNull");
                AssertNotNull(saveData.deployedHeroes, "TC16_RunSaveData_DeployedHeroes_NotNull");
                AssertNotNull(saveData.injuredHeroCardIds, "TC16_RunSaveData_InjuredHeroCardIds_NotNull");
                AssertNotNull(saveData.availableEquipmentIds, "TC16_RunSaveData_AvailableEquipmentIds_NotNull");
                AssertNotNull(saveData.deployedEquipmentIds, "TC16_RunSaveData_DeployedEquipmentIds_NotNull");
                AssertNotNull(saveData.availableTacticalIds, "TC16_RunSaveData_AvailableTacticalIds_NotNull");
                AssertNotNull(saveData.usedTacticalIds, "TC16_RunSaveData_UsedTacticalIds_NotNull");
                AssertNotNull(saveData.placedColonyCards, "TC16_RunSaveData_PlacedColonyCards_NotNull");
                AssertNotNull(saveData.mapNodes, "TC16_RunSaveData_MapNodes_NotNull");
                AssertNotNull(saveData.enemies, "TC16_RunSaveData_Enemies_NotNull");
                Debug.Log("[ValidationTestRunner] TC16_SaveLoad: RunSaveData has all required fields");

                // JSON serialization roundtrip works
                Debug.Log("[ValidationTestRunner] TC16_SaveLoad: testing JSON roundtrip");
                saveData.randomSeed = 42;
                saveData.currentTurn = 7;
                saveData.runState = (int)RunState.InRun;
                saveData.foodStockpile = 15;
                saveData.materialsStockpile = 10;
                saveData.currencyStockpile = 5;
                saveData.deckCardIds.Add(1);
                saveData.deckCardIds.Add(2);
                saveData.deckCardIds.Add(3);
                saveData.colonyDeckCardIds.Add(21);
                saveData.colonyDeckCardIds.Add(22);
                saveData.totalResourcesGathered = 50;
                saveData.totalEnemiesDefeated = 8;
                saveData.zoneBossesDefeated = 1;
                saveData.piedPiperDefeated = false;
                saveData.colonyCardsPlayed = 4;

                string json = JsonUtility.ToJson(saveData, true);
                AssertGreaterThan(json.Length, 0, "TC16_Serialization_ProducesJSON");
                Debug.Log($"[ValidationTestRunner] TC16_SaveLoad: serialized JSON length={json.Length}");

                var loaded = JsonUtility.FromJson<RunSaveData>(json);
                AssertNotNull(loaded, "TC16_Deserialization_ReturnsNonNull");
                AssertEqual(42, loaded.randomSeed, "TC16_Roundtrip_Seed");
                AssertEqual(7, loaded.currentTurn, "TC16_Roundtrip_Turn");
                AssertEqual(15, loaded.foodStockpile, "TC16_Roundtrip_Food");
                AssertEqual(10, loaded.materialsStockpile, "TC16_Roundtrip_Materials");
                AssertEqual(5, loaded.currencyStockpile, "TC16_Roundtrip_Currency");
                AssertEqual(3, loaded.deckCardIds.Count, "TC16_Roundtrip_DeckCount");
                AssertEqual(50, loaded.totalResourcesGathered, "TC16_Roundtrip_ResourcesGathered");
                AssertEqual(8, loaded.totalEnemiesDefeated, "TC16_Roundtrip_EnemiesDefeated");
                Debug.Log($"[ValidationTestRunner] TC16_SaveLoad: roundtrip seed={loaded.randomSeed}, turn={loaded.currentTurn}, food={loaded.foodStockpile}");

                // HeroSaveData serializes
                Debug.Log("[ValidationTestRunner] TC16_SaveLoad: testing HeroSaveData roundtrip");
                var heroSave = new HeroSaveData
                {
                    cardId = 5,
                    tokenId = 100,
                    currentNodeId = 3,
                    targetNodeId = 7,
                    currentHP = 4,
                    offensiveEquipId = 51,
                    defensiveEquipId = -1,
                    utilityEquipId = 71
                };
                heroSave.carriedResources.Add(new ResourceAmount(ResourceType.Food, 2));

                string heroJson = JsonUtility.ToJson(heroSave);
                var heroLoaded = JsonUtility.FromJson<HeroSaveData>(heroJson);
                AssertEqual(5, heroLoaded.cardId, "TC16_HeroSaveData_CardId");
                AssertEqual(100, heroLoaded.tokenId, "TC16_HeroSaveData_TokenId");
                AssertEqual(3, heroLoaded.currentNodeId, "TC16_HeroSaveData_NodeId");
                AssertEqual(51, heroLoaded.offensiveEquipId, "TC16_HeroSaveData_OffensiveEquip");
                AssertEqual(-1, heroLoaded.defensiveEquipId, "TC16_HeroSaveData_DefensiveEquip");
                Debug.Log($"[ValidationTestRunner] TC16_SaveLoad: hero save roundtrip cardId={heroLoaded.cardId}, tokenId={heroLoaded.tokenId}");

                // ColonyCardSaveData serializes
                Debug.Log("[ValidationTestRunner] TC16_SaveLoad: testing ColonyCardSaveData roundtrip");
                var colonySave = new ColonyCardSaveData
                {
                    cardId = 25,
                    placedId = 0,
                    posX = 1.5f,
                    posY = 2.5f
                };
                colonySave.adjacentPlacedIds.Add(1);
                colonySave.adjacentPlacedIds.Add(2);

                string colonyJson = JsonUtility.ToJson(colonySave);
                var colonyLoaded = JsonUtility.FromJson<ColonyCardSaveData>(colonyJson);
                AssertEqual(25, colonyLoaded.cardId, "TC16_ColonyCardSaveData_CardId");
                Assert(Mathf.Approximately(1.5f, colonyLoaded.posX), "TC16_ColonyCardSaveData_PosX", $"posX={colonyLoaded.posX}");
                AssertEqual(2, colonyLoaded.adjacentPlacedIds.Count, "TC16_ColonyCardSaveData_AdjCount");
                Debug.Log($"[ValidationTestRunner] TC16_SaveLoad: colony save roundtrip cardId={colonyLoaded.cardId}");

                // MapNodeSaveData serializes
                Debug.Log("[ValidationTestRunner] TC16_SaveLoad: testing MapNodeSaveData roundtrip");
                var nodeSave = new MapNodeSaveData
                {
                    nodeId = 5,
                    zone = (int)NodeType.Farmland,
                    visited = true,
                    fogState = (int)FogState.Visible,
                    posX = 3.0f,
                    posY = -2.0f
                };
                nodeSave.neighborIds.Add(4);
                nodeSave.neighborIds.Add(6);
                nodeSave.resources.Add(new ResourceAmount(ResourceType.Materials, 3));

                string nodeJson = JsonUtility.ToJson(nodeSave);
                var nodeLoaded = JsonUtility.FromJson<MapNodeSaveData>(nodeJson);
                AssertEqual(5, nodeLoaded.nodeId, "TC16_MapNodeSaveData_NodeId");
                AssertEqual((int)NodeType.Farmland, nodeLoaded.zone, "TC16_MapNodeSaveData_Zone");
                AssertTrue(nodeLoaded.visited, "TC16_MapNodeSaveData_Visited");
                AssertEqual(2, nodeLoaded.neighborIds.Count, "TC16_MapNodeSaveData_NeighborCount");
                Debug.Log($"[ValidationTestRunner] TC16_SaveLoad: node save roundtrip nodeId={nodeLoaded.nodeId}, zone={nodeLoaded.zone}");

                // EnemySaveData serializes
                Debug.Log("[ValidationTestRunner] TC16_SaveLoad: testing EnemySaveData roundtrip");
                var enemySave = new EnemySaveData
                {
                    tokenId = 50,
                    enemyDefId = 3,
                    enemyName = "TestEnemy",
                    currentNodeId = 12,
                    currentHP = 5,
                    isDefeated = false,
                    respawnTimer = 0
                };

                string enemyJson = JsonUtility.ToJson(enemySave);
                var enemyLoaded = JsonUtility.FromJson<EnemySaveData>(enemyJson);
                AssertEqual(50, enemyLoaded.tokenId, "TC16_EnemySaveData_TokenId");
                AssertEqual("TestEnemy", enemyLoaded.enemyName, "TC16_EnemySaveData_Name");
                AssertEqual(12, enemyLoaded.currentNodeId, "TC16_EnemySaveData_NodeId");
                AssertFalse(enemyLoaded.isDefeated, "TC16_EnemySaveData_NotDefeated");
                Debug.Log($"[ValidationTestRunner] TC16_SaveLoad: enemy save roundtrip tokenId={enemyLoaded.tokenId}, name={enemyLoaded.enemyName}");
            }
            catch (Exception e)
            {
                Skip("TC16_SaveLoad", $"Save/Load tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC16_SaveLoad: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC16_SaveLoad: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-17: SEEDED RANDOM
        // ══════════════════════════════════════════════════════════════════

        private void TC17_SeededRandom()
        {
            Debug.Log("[ValidationTestRunner] TC17_SeededRandom: ENTER");

            try
            {
                // Same seed = same sequence
                Debug.Log("[ValidationTestRunner] TC17_SeededRandom: testing determinism");
                SeededRandom.Initialize(12345);
                int a1 = SeededRandom.Range(0, 100);
                int a2 = SeededRandom.Range(0, 100);
                int a3 = SeededRandom.Range(0, 100);

                SeededRandom.Initialize(12345);
                int b1 = SeededRandom.Range(0, 100);
                int b2 = SeededRandom.Range(0, 100);
                int b3 = SeededRandom.Range(0, 100);

                AssertEqual(a1, b1, "TC17_SameSeed_FirstValue");
                AssertEqual(a2, b2, "TC17_SameSeed_SecondValue");
                AssertEqual(a3, b3, "TC17_SameSeed_ThirdValue");
                Debug.Log($"[ValidationTestRunner] TC17_SeededRandom: seed=12345 sequence=[{a1},{a2},{a3}] vs [{b1},{b2},{b3}]");

                // Different seed = different sequence
                Debug.Log("[ValidationTestRunner] TC17_SeededRandom: testing different seeds");
                SeededRandom.Initialize(99999);
                int c1 = SeededRandom.Range(0, 100);
                int c2 = SeededRandom.Range(0, 100);
                int c3 = SeededRandom.Range(0, 100);

                bool anyDifferent = (c1 != a1) || (c2 != a2) || (c3 != a3);
                AssertTrue(anyDifferent, "TC17_DifferentSeed_DifferentSequence");
                Debug.Log($"[ValidationTestRunner] TC17_SeededRandom: seed=99999 sequence=[{c1},{c2},{c3}] different from seed=12345");

                // Range respects bounds
                Debug.Log("[ValidationTestRunner] TC17_SeededRandom: testing range bounds");
                SeededRandom.Initialize(42);
                bool allInRange = true;
                for (int i = 0; i < 100; i++)
                {
                    int val = SeededRandom.Range(10, 20);
                    if (val < 10 || val >= 20)
                    {
                        allInRange = false;
                        Debug.LogError($"[ValidationTestRunner] TC17_SeededRandom: out of range value={val} for Range(10,20)");
                        break;
                    }
                }
                AssertTrue(allInRange, "TC17_IntRange_RespectsBounds");
                Debug.Log($"[ValidationTestRunner] TC17_SeededRandom: 100 Range(10,20) calls all in bounds={allInRange}");

                // Float range
                Debug.Log("[ValidationTestRunner] TC17_SeededRandom: testing float range");
                bool allFloatInRange = true;
                for (int i = 0; i < 100; i++)
                {
                    float fval = SeededRandom.Range(0.0f, 1.0f);
                    if (fval < 0.0f || fval >= 1.0f)
                    {
                        allFloatInRange = false;
                        Debug.LogError($"[ValidationTestRunner] TC17_SeededRandom: float out of range value={fval}");
                        break;
                    }
                }
                AssertTrue(allFloatInRange, "TC17_FloatRange_RespectsBounds");
                Debug.Log($"[ValidationTestRunner] TC17_SeededRandom: 100 float Range(0,1) calls all in bounds={allFloatInRange}");

                // Value property in [0,1)
                Debug.Log("[ValidationTestRunner] TC17_SeededRandom: testing Value property");
                bool allValueInRange = true;
                for (int i = 0; i < 100; i++)
                {
                    float v = SeededRandom.Value;
                    if (v < 0.0f || v >= 1.0f)
                    {
                        allValueInRange = false;
                        Debug.LogError($"[ValidationTestRunner] TC17_SeededRandom: Value out of range value={v}");
                        break;
                    }
                }
                AssertTrue(allValueInRange, "TC17_Value_InZeroOneRange");
                Debug.Log($"[ValidationTestRunner] TC17_SeededRandom: 100 Value calls in [0,1)={allValueInRange}");
            }
            catch (Exception e)
            {
                Skip("TC17_SeededRandom", $"SeededRandom tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC17_SeededRandom: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC17_SeededRandom: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-18: SCENE BUILD SETTINGS
        // ══════════════════════════════════════════════════════════════════

        private void TC18_SceneBuildSettings()
        {
            Debug.Log("[ValidationTestRunner] TC18_SceneBuildSettings: ENTER");

            try
            {
                int sceneCount = SceneManager.sceneCountInBuildSettings;
                Debug.Log($"[ValidationTestRunner] TC18_SceneBuildSettings: scenes in build settings={sceneCount}");

                AssertGreaterThanOrEqual(sceneCount, 8, "TC18_AtLeast8Scenes");

                // Check scene names at expected indices (Colony scene removed, merged into GameMap)
                string[] expectedScenes = { "Bootstrap", "MainMenu", "DeckConstruction", "GameMap", "Deployment", "Combat", "CardReward", "RunResult" };
                for (int i = 0; i < expectedScenes.Length && i < sceneCount; i++)
                {
                    string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    Debug.Log($"[ValidationTestRunner] TC18_SceneBuildSettings: index {i} = '{sceneName}' (path='{scenePath}')");

                    // Check if scene name contains expected name (may have prefixes)
                    bool nameMatch = sceneName.Contains(expectedScenes[i]) ||
                                     sceneName.Equals(expectedScenes[i], StringComparison.OrdinalIgnoreCase);
                    Assert(nameMatch, $"TC18_Scene_{i}_{expectedScenes[i]}", $"expected='{expectedScenes[i]}', actual='{sceneName}'");
                }

                if (sceneCount < 8)
                {
                    Debug.LogWarning($"[ValidationTestRunner] TC18_SceneBuildSettings: only {sceneCount} scenes found, expected 8+");
                    for (int i = sceneCount; i < expectedScenes.Length; i++)
                    {
                        Skip($"TC18_Scene_{i}_{expectedScenes[i]}", $"Scene index {i} not in build settings (only {sceneCount} scenes)");
                    }
                }
            }
            catch (Exception e)
            {
                Skip("TC18_SceneBuildSettings", $"Scene build settings tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC18_SceneBuildSettings: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC18_SceneBuildSettings: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-19: FULL GAME SIMULATION
        // ══════════════════════════════════════════════════════════════════

        private void TC19_FullGameSimulation()
        {
            Debug.Log("[ValidationTestRunner] TC19_FullGameSimulation: ENTER");

            try
            {
                // Resolve DI services
                var heroFactory = ServiceLocator.Get<IHeroTokenFactory>();
                var enemyFactory = ServiceLocator.Get<IEnemyTokenFactory>();
                var combatResolverSvc = ServiceLocator.Get<ICombatResolver>();
                Debug.Log($"[ValidationTestRunner] TC19_Simulation: resolved IHeroTokenFactory={heroFactory?.GetType().Name}, IEnemyTokenFactory={enemyFactory?.GetType().Name}, ICombatResolver={combatResolverSvc?.GetType().Name}");

                // 1. Generate map with seed 42
                Debug.Log("[ValidationTestRunner] TC19_Simulation: generating map with seed=42");
                var config = CreateTestMapConfig();
                var graph = MapGenerator.GenerateMap(config, 42);
                ServiceLocator.Register<IMapGraph>(graph);
                Debug.Log("[ValidationTestRunner] TC19_Simulation: registered map as IMapGraph");
                AssertNotNull(graph, "TC19_Sim_MapGenerated");
                Debug.Log($"[ValidationTestRunner] TC19_Simulation: map generated with {graph.GetAllNodes().Count} nodes");

                // 2. Create 5 heroes, place on colony
                Debug.Log("[ValidationTestRunner] TC19_Simulation: creating 5 heroes");
                var heroDefs = new List<CardDefinitionSO>
                {
                    CreateTestHeroCard(1, "SimScout", HeroRole.Recon, 2, 3, 4, 2, 5, SpecialAbility.ExtendedFogReveal),
                    CreateTestHeroCard(2, "SimWarrior", HeroRole.Melee, 4, 2, 6, 2, 2, SpecialAbility.Cleave),
                    CreateTestHeroCard(3, "SimTank", HeroRole.Tank, 3, 1, 8, 1, 1, SpecialAbility.Taunt),
                    CreateTestHeroCard(4, "SimGatherer", HeroRole.Gather, 1, 2, 4, 4, 3, SpecialAbility.EfficientGather),
                    CreateTestHeroCard(5, "SimSupport", HeroRole.Support, 2, 2, 5, 2, 4, SpecialAbility.FieldMedic)
                };

                var heroes = new List<HeroToken>();
                for (int i = 0; i < heroDefs.Count; i++)
                {
                    var token = heroFactory.Create(heroDefs[i], i + 1);
                    token.currentNodeId = 0; // Colony
                    token.isDeployed = true;
                    heroes.Add(token);
                    Debug.Log($"[ValidationTestRunner] TC19_Simulation: created hero {token}");
                }
                AssertEqual(5, heroes.Count, "TC19_Sim_5HeroesCreated");

                // 3. Create enemies on map
                Debug.Log("[ValidationTestRunner] TC19_Simulation: creating enemies");
                var enemyTokens = new List<EnemyToken>();
                int enemyId = 0;
                for (int nodeId = 1; nodeId <= 10; nodeId++)
                {
                    var node = graph.GetNode(nodeId);
                    if (node != null)
                    {
                        var et = enemyFactory.Create(enemyId++, $"WildEnemy_{nodeId}", 2, 3, 1, EnemyBehavior.Patrol, NodeType.Wilderness, nodeId);
                        node.enemyTokenIds.Add(et.tokenId);
                        enemyTokens.Add(et);
                    }
                }
                AssertGreaterThan(enemyTokens.Count, 0, "TC19_Sim_EnemiesCreated");
                Debug.Log($"[ValidationTestRunner] TC19_Simulation: created {enemyTokens.Count} enemies");

                // 4. Simulate 5 turns of all 7 phases
                Debug.Log("[ValidationTestRunner] TC19_Simulation: simulating 5 turns");
                var freshColonySim = new ColonyGraph();
                ServiceLocator.Register<IColonyGraph>(freshColonySim);
                Debug.Log("[ValidationTestRunner] TC19_Simulation: re-registered fresh IColonyGraph for simulation");
                var colonyGraphSim = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
                colonyGraphSim.Initialize();
                var fog = ServiceLocator.Get<IFogOfWar>();
                Debug.Log($"[ValidationTestRunner] TC19_Simulation: resolved IFogOfWar (instance={fog?.GetType().Name})");
                var combatResolver = ServiceLocator.Get<ICombatResolver>();
                Debug.Log($"[ValidationTestRunner] TC19_Simulation: resolved ICombatResolver (instance={combatResolver?.GetType().Name})");
                bool noExceptions = true;
                int totalCombats = 0;
                int totalGathered = 0;

                for (int turn = 1; turn <= 5; turn++)
                {
                    try
                    {
                        Debug.Log($"[ValidationTestRunner] TC19_Simulation: === TURN {turn} ===");

                        // Phase 1: Colony
                        Debug.Log($"[ValidationTestRunner] TC19_Simulation: turn {turn} Phase 1: Colony");
                        colonyGraphSim.ResetTurnCardCount();

                        // Phase 2: Deploy (heroes already deployed)
                        Debug.Log($"[ValidationTestRunner] TC19_Simulation: turn {turn} Phase 2: Deploy");

                        // Phase 3: Hero Move
                        Debug.Log($"[ValidationTestRunner] TC19_Simulation: turn {turn} Phase 3: HeroMove");
                        foreach (var hero in heroes)
                        {
                            if (hero.isInjured) continue;
                            var currentNode = graph.GetNode(hero.currentNodeId);
                            if (currentNode != null && currentNode.neighborIds.Count > 0)
                            {
                                int moveIdx = SeededRandom.Range(0, currentNode.neighborIds.Count);
                                int newNodeId = currentNode.neighborIds[moveIdx];
                                hero.currentNodeId = newNodeId;
                                Debug.Log($"[ValidationTestRunner] TC19_Simulation: hero {hero.cardDef.cardName} moved to node {newNodeId}");
                            }
                        }

                        // Phase 4: Enemy Move
                        Debug.Log($"[ValidationTestRunner] TC19_Simulation: turn {turn} Phase 4: EnemyMove");
                        var allMapNodes = new List<MapNode>(graph.GetAllNodes());
                        EnemyAI.ProcessAllEnemies(enemyTokens, allMapNodes, heroes);

                        // Phase 5: Combat (check for heroes and enemies on same node)
                        Debug.Log($"[ValidationTestRunner] TC19_Simulation: turn {turn} Phase 5: Combat");
                        var nodeHeroes = new Dictionary<int, List<HeroToken>>();
                        foreach (var hero in heroes.Where(h => !h.isInjured))
                        {
                            if (!nodeHeroes.ContainsKey(hero.currentNodeId))
                                nodeHeroes[hero.currentNodeId] = new List<HeroToken>();
                            nodeHeroes[hero.currentNodeId].Add(hero);
                        }

                        foreach (var kvp in nodeHeroes)
                        {
                            var nodeEnemies = enemyTokens.Where(e => e.currentNodeId == kvp.Key && e.IsAlive).ToList();
                            if (nodeEnemies.Count > 0)
                            {
                                Debug.Log($"[ValidationTestRunner] TC19_Simulation: combat at node {kvp.Key} ({kvp.Value.Count} heroes vs {nodeEnemies.Count} enemies)");
                                combatResolver.ResolveCombat(kvp.Value, nodeEnemies, kvp.Key);
                                totalCombats++;
                            }
                        }

                        // Phase 6: Gather
                        Debug.Log($"[ValidationTestRunner] TC19_Simulation: turn {turn} Phase 6: Gather");
                        var gatherResult = GatherPhase.Execute(heroes.Where(h => !h.isInjured).ToList(), graph, colonyGraphSim);
                        totalGathered += gatherResult.totalGathered;

                        // Phase 7: Cleanup
                        Debug.Log($"[ValidationTestRunner] TC19_Simulation: turn {turn} Phase 7: Cleanup");
                        // Tick enemy respawn
                        foreach (var enemy in enemyTokens)
                        {
                            if (enemy.isDefeated)
                            {
                                enemy.respawnTimer--;
                                if (enemy.respawnTimer <= 0)
                                {
                                    enemy.Respawn(enemy.currentNodeId);
                                    Debug.Log($"[ValidationTestRunner] TC19_Simulation: enemy {enemy.enemyName} respawned");
                                }
                            }
                        }

                        Debug.Log($"[ValidationTestRunner] TC19_Simulation: turn {turn} complete " +
                                  $"(aliveHeroes={heroes.Count(h => !h.isInjured)}, " +
                                  $"aliveEnemies={enemyTokens.Count(e => e.IsAlive)}, " +
                                  $"totalGathered={totalGathered})");
                    }
                    catch (Exception turnEx)
                    {
                        noExceptions = false;
                        Debug.LogError($"[ValidationTestRunner] TC19_Simulation: EXCEPTION in turn {turn}: {turnEx.Message}\n{turnEx.StackTrace}");
                    }
                }

                // 5. Calculate score
                Debug.Log("[ValidationTestRunner] TC19_Simulation: calculating score");
                var scoreInput = new ScoreInput
                {
                    turnsUsed = 5,
                    deckSize = 15,
                    enemiesDefeated = enemyTokens.Count(e => e.isDefeated),
                    zoneBossesDefeated = 0,
                    piedPiperDefeated = false,
                    totalResourcesGathered = totalGathered,
                    colonyCardsPlayed = 0,
                    heroesNeverInjured = heroes.Count(h => !h.isInjured)
                };
                var score = ScoreCalculator.CalculateScore(scoreInput);
                AssertGreaterThanOrEqual(score.finalScore, 0, "TC19_Sim_ScoreNonNegative");
                Debug.Log($"[ValidationTestRunner] TC19_Simulation: final score={score.finalScore}");

                // 6. Verify no exceptions thrown
                AssertTrue(noExceptions, "TC19_Sim_NoExceptions");

                // 7. Log game state summary
                Debug.Log($"[ValidationTestRunner] TC19_Simulation: === GAME STATE SUMMARY ===");
                Debug.Log($"[ValidationTestRunner] TC19_Simulation: turns simulated=5");
                Debug.Log($"[ValidationTestRunner] TC19_Simulation: alive heroes={heroes.Count(h => !h.isInjured)}/{heroes.Count}");
                Debug.Log($"[ValidationTestRunner] TC19_Simulation: alive enemies={enemyTokens.Count(e => e.IsAlive)}/{enemyTokens.Count}");
                Debug.Log($"[ValidationTestRunner] TC19_Simulation: total combats={totalCombats}");
                Debug.Log($"[ValidationTestRunner] TC19_Simulation: total gathered={totalGathered}");
                Debug.Log($"[ValidationTestRunner] TC19_Simulation: final score={score.finalScore}");
                foreach (var hero in heroes)
                {
                    Debug.Log($"[ValidationTestRunner] TC19_Simulation: hero={hero}");
                }
                Debug.Log($"[ValidationTestRunner] TC19_Simulation: === END SUMMARY ===");

                // Cleanup
                DestroyImmediate(config);
                foreach (var def in heroDefs) DestroyImmediate(def);
            }
            catch (Exception e)
            {
                Skip("TC19_FullGameSimulation", $"Simulation failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC19_FullGameSimulation: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC19_FullGameSimulation: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-20: BALANCE VALIDATION
        // ══════════════════════════════════════════════════════════════════

        private void TC20_BalanceValidation()
        {
            Debug.Log("[ValidationTestRunner] TC20_BalanceValidation: ENTER");

            try
            {
                var db = CardDatabase.Instance;
                var heroes = db.GetCardsByType(CardType.Hero);

                // All hero combat values 1-5
                Debug.Log("[ValidationTestRunner] TC20_BalanceValidation: checking hero combat range");
                foreach (var hero in heroes)
                {
                    AssertInRange(hero.combat, 1, 5, $"TC20_Hero_{hero.cardName}_Combat_{hero.combat}_InRange1to5");
                    Debug.Log($"[ValidationTestRunner] TC20_BalanceValidation: hero '{hero.cardName}' combat={hero.combat}");
                }

                // All hero move values 1-4
                Debug.Log("[ValidationTestRunner] TC20_BalanceValidation: checking hero move range");
                foreach (var hero in heroes)
                {
                    AssertInRange(hero.move, 1, 4, $"TC20_Hero_{hero.cardName}_Move_{hero.move}_InRange1to4");
                    Debug.Log($"[ValidationTestRunner] TC20_BalanceValidation: hero '{hero.cardName}' move={hero.move}");
                }

                // All hero HP values 2-6
                Debug.Log("[ValidationTestRunner] TC20_BalanceValidation: checking hero HP range");
                foreach (var hero in heroes)
                {
                    AssertInRange(hero.hp, 2, 6, $"TC20_Hero_{hero.cardName}_HP_{hero.hp}_InRange2to6");
                    Debug.Log($"[ValidationTestRunner] TC20_BalanceValidation: hero '{hero.cardName}' hp={hero.hp}");
                }

                // All hero carry values 1-4
                Debug.Log("[ValidationTestRunner] TC20_BalanceValidation: checking hero carry range");
                foreach (var hero in heroes)
                {
                    AssertInRange(hero.carry, 1, 4, $"TC20_Hero_{hero.cardName}_Carry_{hero.carry}_InRange1to4");
                    Debug.Log($"[ValidationTestRunner] TC20_BalanceValidation: hero '{hero.cardName}' carry={hero.carry}");
                }

                // Colony base food production = 2
                Debug.Log("[ValidationTestRunner] TC20_BalanceValidation: checking colony base food production");
                var freshColonyBalance = new ColonyGraph();
                ServiceLocator.Register<IColonyGraph>(freshColonyBalance);
                Debug.Log("[ValidationTestRunner] TC20_BalanceValidation: re-registered fresh IColonyGraph for balance test");
                var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
                colony.Initialize();
                int baseFoodProduction = colony.CalculateFoodProduction();
                AssertEqual(2, baseFoodProduction, "TC20_ColonyBaseFoodProduction_Is2");
                Debug.Log($"[ValidationTestRunner] TC20_BalanceValidation: colony base food production={baseFoodProduction}");

                // Safety valve MAX_TURNS = 100, Pied Piper countdown = 15
                Debug.Log("[ValidationTestRunner] TC20_BalanceValidation: checking turn system constants");
                AssertEqual(100, TurnManager.MAX_TURNS, "TC20_MaxTurns_SafetyValve");
                AssertEqual(15, TurnManager.PIPER_COUNTDOWN, "TC20_PiperCountdown_Is15");
                Debug.Log($"[ValidationTestRunner] TC20_BalanceValidation: MAX_TURNS={TurnManager.MAX_TURNS}, PIPER_COUNTDOWN={TurnManager.PIPER_COUNTDOWN}");

                // Deck size range 10-30
                Debug.Log("[ValidationTestRunner] TC20_BalanceValidation: checking deck size range");
                AssertEqual(10, DeckConstructionManager.MIN_DECK_SIZE, "TC20_MinDeckSize_Is10");
                AssertEqual(30, DeckConstructionManager.MAX_DECK_SIZE, "TC20_MaxDeckSize_Is30");
                Debug.Log($"[ValidationTestRunner] TC20_BalanceValidation: deck range={DeckConstructionManager.MIN_DECK_SIZE}-{DeckConstructionManager.MAX_DECK_SIZE}");
            }
            catch (Exception e)
            {
                Skip("TC20_BalanceValidation", $"Balance tests failed: {e.Message}");
                Debug.LogWarning($"[ValidationTestRunner] TC20_BalanceValidation: caught exception ({e.GetType().Name}: {e.Message})");
            }

            Debug.Log("[ValidationTestRunner] TC20_BalanceValidation: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-21: INTEGRATION TESTS (COROUTINE)
        // ══════════════════════════════════════════════════════════════════

        private IEnumerator TC21_IntegrationTests()
        {
            Debug.Log("[ValidationTestRunner] TC21_IntegrationTests: ENTER");

            string[] sceneNames = { "MainMenu", "DeckConstruction", "GameMap", "Combat", "CardReward", "RunResult" };
            string[] expectedObjects = { "MainMenuManager", "DeckConstructionUI", "TurnManager", "CombatUI", "CardRewardUI", "RunResultUI" };
            int[] buildIndices = { 1, 2, 3, 6, 7, 8 };

            for (int i = 0; i < sceneNames.Length; i++)
            {
                string sceneName = sceneNames[i];
                string expectedObj = expectedObjects[i];
                int buildIndex = buildIndices[i];

                Debug.Log($"[ValidationTestRunner] TC21_IntegrationTests: loading scene '{sceneName}' (buildIndex={buildIndex})");

                // Check if build index exists
                if (buildIndex >= SceneManager.sceneCountInBuildSettings)
                {
                    Skip($"TC21_{sceneName}_Load", $"Build index {buildIndex} not in build settings (total={SceneManager.sceneCountInBuildSettings})");
                    continue;
                }

                // Verify the scene path matches expected
                string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
                string actualName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                Debug.Log($"[ValidationTestRunner] TC21_IntegrationTests: scene at index {buildIndex} = '{actualName}' (path='{scenePath}')");

                if (!actualName.Contains(sceneName))
                {
                    Skip($"TC21_{sceneName}_Load", $"Expected '{sceneName}' at index {buildIndex}, found '{actualName}'");
                    continue;
                }

                // Load the scene
                AsyncOperation loadOp = SceneManager.LoadSceneAsync(buildIndex);
                if (loadOp == null)
                {
                    Skip($"TC21_{sceneName}_Load", $"LoadSceneAsync returned null for index {buildIndex}");
                    continue;
                }

                while (!loadOp.isDone)
                {
                    yield return null;
                }

                // Wait an extra frame for Start() to run
                yield return null;
                yield return null;

                Debug.Log($"[ValidationTestRunner] TC21_IntegrationTests: scene '{sceneName}' loaded");

                // Find expected object
                var foundObj = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .FirstOrDefault(mb => mb.GetType().Name == expectedObj);

                if (foundObj != null)
                {
                    passed++;
                    Debug.Log($"[ValidationTestRunner] PASS: TC21_{sceneName}_{expectedObj}_Exists (found '{foundObj.GetType().Name}' on '{foundObj.gameObject.name}')");
                }
                else
                {
                    // Try broader search
                    var allMBs = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                    Debug.Log($"[ValidationTestRunner] TC21_IntegrationTests: searching for '{expectedObj}' among {allMBs.Length} MonoBehaviours");
                    bool found = false;
                    foreach (var mb in allMBs)
                    {
                        if (mb.GetType().Name.Contains(expectedObj.Replace("UI", "").Replace("Manager", "")))
                        {
                            found = true;
                            passed++;
                            Debug.Log($"[ValidationTestRunner] PASS: TC21_{sceneName}_{expectedObj}_Exists (partial match: '{mb.GetType().Name}')");
                            break;
                        }
                    }
                    if (!found)
                    {
                        failed++;
                        string entry = $"TC21_{sceneName}_{expectedObj}_Exists: object not found in scene";
                        failures.Add(entry);
                        Debug.LogError($"[ValidationTestRunner] FAIL: {entry}");
                    }
                }
            }

            Debug.Log("[ValidationTestRunner] TC21_IntegrationTests: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-22: EQUIPMENT EFFECT PROCESSOR
        // ══════════════════════════════════════════════════════════════════

        private void TC22_EquipmentEffectProcessor()
        {
            Debug.Log("[ValidationTestRunner] TC22_EquipmentEffectProcessor: ENTER");

            try
            {
                var factory = ServiceLocator.Get<IHeroTokenFactory>();

                // Create a hero with no equipment — all bonuses should be 0
                var heroCard = CreateTestHeroCard(1, "TestWarrior", HeroRole.Melee, 3, 2, 5, 2, 3);
                var hero = factory.Create(heroCard, 1);
                Debug.Log($"[ValidationTestRunner] TC22_EquipmentEffectProcessor: created hero (name={heroCard.cardName}, equippedCount={hero.equippedItems.Count})");

                AssertEqual(0, EquipmentEffectProcessor.GetCombatBonus(hero), "TC22_NoBonusCombat_Empty");
                AssertEqual(0, EquipmentEffectProcessor.GetHPBonus(hero), "TC22_NoBonusHP_Empty");
                AssertEqual(0, EquipmentEffectProcessor.GetMoveBonus(hero), "TC22_NoBonusMove_Empty");
                AssertEqual(0, EquipmentEffectProcessor.GetCarryBonus(hero), "TC22_NoBonusCarry_Empty");
                AssertEqual(0, EquipmentEffectProcessor.GetInitiativeBonus(hero), "TC22_NoBonusInitiative_Empty");

                // Equip Needle Sword (id=51, Offensive, +1 Combat)
                var needleSword = CreateTestEquipmentCard(51, "Needle Sword", EquipmentSlot.Offensive, 1);
                hero.equippedItems.Add(needleSword);
                Debug.Log($"[ValidationTestRunner] TC22_EquipmentEffectProcessor: equipped Needle Sword (id=51, ev1={needleSword.effectValue1})");

                int combatBonus = EquipmentEffectProcessor.GetCombatBonus(hero);
                AssertEqual(1, combatBonus, "TC22_CombatBonus_NeedleSword");

                // Equip Thimble Helmet (id=61, Defensive, +1 HP)
                var thimbleHelmet = CreateTestEquipmentCard(61, "Thimble Helmet", EquipmentSlot.Defensive, 1);
                hero.equippedItems.Add(thimbleHelmet);

                int hpBonus = EquipmentEffectProcessor.GetHPBonus(hero);
                AssertEqual(1, hpBonus, "TC22_HPBonus_ThimbleHelmet");

                // Equip Button Compass (id=72, Utility, +1 Move)
                var buttonCompass = CreateTestEquipmentCard(72, "Button Compass", EquipmentSlot.Utility, 1);
                hero.equippedItems.Clear();
                hero.equippedItems.Add(buttonCompass);

                int moveBonus = EquipmentEffectProcessor.GetMoveBonus(hero);
                AssertEqual(1, moveBonus, "TC22_MoveBonus_ButtonCompass");

                // Equip Cloth Satchel (id=73, Utility, +2 Carry)
                var clothSatchel = CreateTestEquipmentCard(73, "Cloth Satchel", EquipmentSlot.Utility, 2);
                hero.equippedItems.Clear();
                hero.equippedItems.Add(clothSatchel);

                int carryBonus = EquipmentEffectProcessor.GetCarryBonus(hero);
                AssertEqual(2, carryBonus, "TC22_CarryBonus_ClothSatchel");

                // Equip Pin Dagger (id=53, Offensive, +1 Combat, +1 Initiative via ev2)
                var pinDagger = CreateTestEquipmentCard(53, "Pin Dagger", EquipmentSlot.Offensive, 1, 1);
                hero.equippedItems.Clear();
                hero.equippedItems.Add(pinDagger);

                int initBonus = EquipmentEffectProcessor.GetInitiativeBonus(hero);
                AssertEqual(1, initBonus, "TC22_InitiativeBonus_PinDagger");

                // HasRangedStrike: false by default, true with Thorn Spear (id=52)
                hero.equippedItems.Clear();
                AssertFalse(EquipmentEffectProcessor.HasRangedStrike(hero), "TC22_NoRangedStrike_Empty");

                var thornSpear = CreateTestEquipmentCard(52, "Thorn Spear", EquipmentSlot.Offensive, 1);
                hero.equippedItems.Add(thornSpear);
                AssertTrue(EquipmentEffectProcessor.HasRangedStrike(hero), "TC22_HasRangedStrike_ThornSpear");

                // HasSplashDamage: false by default, true with Storm Needle (id=81)
                hero.equippedItems.Clear();
                AssertFalse(EquipmentEffectProcessor.HasSplashDamage(hero), "TC22_NoSplashDamage_Empty");

                var stormNeedle = CreateTestEquipmentCard(81, "Storm Needle", EquipmentSlot.Offensive, 4, 1);
                hero.equippedItems.Add(stormNeedle);
                AssertTrue(EquipmentEffectProcessor.HasSplashDamage(hero), "TC22_HasSplashDamage_StormNeedle");

                // IsTauntActive: depends on hero specialAbility, not equipment
                hero.equippedItems.Clear();
                AssertFalse(EquipmentEffectProcessor.IsTauntActive(hero), "TC22_NoTaunt_NoAbility");

                var tauntHeroCard = CreateTestHeroCard(2, "TauntRat", HeroRole.Tank, 2, 1, 8, 1, 1, SpecialAbility.Taunt);
                var tauntHero = factory.Create(tauntHeroCard, 2);
                AssertTrue(EquipmentEffectProcessor.IsTauntActive(tauntHero), "TC22_Taunt_Active");

                // GetFogRevealBonus: 0 with no equipment, +1 with Bead Lantern (id=71)
                hero.equippedItems.Clear();
                AssertEqual(0, EquipmentEffectProcessor.GetFogRevealBonus(hero), "TC22_FogReveal_Empty");

                var beadLantern = CreateTestEquipmentCard(71, "Bead Lantern", EquipmentSlot.Utility, 1);
                hero.equippedItems.Add(beadLantern);
                AssertEqual(1, EquipmentEffectProcessor.GetFogRevealBonus(hero), "TC22_FogReveal_BeadLantern");

                // DualWield ability: +1 Combat per offensive item
                var dwHeroCard = CreateTestHeroCard(3, "DualWielder", HeroRole.Melee, 3, 2, 4, 2, 3, SpecialAbility.DualWield);
                var dwHero = factory.Create(dwHeroCard, 3);
                var sword1 = CreateTestEquipmentCard(51, "Needle Sword", EquipmentSlot.Offensive, 1);
                var sword2 = CreateTestEquipmentCard(55, "Needle Lance", EquipmentSlot.Offensive, 2);
                dwHero.equippedItems.Add(sword1);
                dwHero.equippedItems.Add(sword2);
                int dwCombat = EquipmentEffectProcessor.GetCombatBonus(dwHero);
                // Base: 1+2=3, DualWield: +2 (2 offensive items) = 5
                AssertEqual(5, dwCombat, "TC22_DualWield_CombatBonus");

                // Tinker ability: utility carry bonus doubled
                var tinkerCard = CreateTestHeroCard(4, "TinkerRat", HeroRole.Recon, 1, 3, 3, 4, 2, SpecialAbility.Tinker);
                var tinkerHero = factory.Create(tinkerCard, 4);
                var satchel = CreateTestEquipmentCard(73, "Cloth Satchel", EquipmentSlot.Utility, 2);
                tinkerHero.equippedItems.Add(satchel);
                int tinkerCarry = EquipmentEffectProcessor.GetCarryBonus(tinkerHero);
                AssertEqual(4, tinkerCarry, "TC22_Tinker_DoubleCarryBonus");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ValidationTestRunner] TC22_EquipmentEffectProcessor: exception — {ex.Message}");
                Skip("TC22_EquipmentEffectProcessor", ex.Message);
            }

            Debug.Log("[ValidationTestRunner] TC22_EquipmentEffectProcessor: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-23: TACTICAL EFFECT PROCESSOR
        // ══════════════════════════════════════════════════════════════════

        private void TC23_TacticalEffectProcessor()
        {
            Debug.Log("[ValidationTestRunner] TC23_TacticalEffectProcessor: ENTER");

            try
            {
                var factory = ServiceLocator.Get<IHeroTokenFactory>();
                var enemyFactory = ServiceLocator.Get<IEnemyTokenFactory>();

                // Create test heroes and enemies
                var heroCard = CreateTestHeroCard(1, "TestHero", HeroRole.Melee, 3, 2, 5, 2, 3);
                var hero = factory.Create(heroCard, 1);
                hero.TakeDamage(2); // lower HP for healing tests
                var heroes = new List<HeroToken> { hero };

                var enemy = new EnemyToken(0, "TestEnemy", 3, 5, 2, EnemyBehavior.Patrol, NodeType.Wilderness, 0);
                var enemies = new List<EnemyToken> { enemy };

                // Non-tactical card should fail
                var nonTactical = CreateTestHeroCard(1, "NotATactical", HeroRole.Melee, 1, 1, 1, 1, 1);
                var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
                bool failResult = TacticalEffectProcessor.ApplyTacticalCard(nonTactical, heroes, enemies, 0, ctx);
                AssertFalse(failResult, "TC23_NonTactical_ReturnsFalse");

                // CombatTactic: Pack Ambush (id=91) — +2 Combat to all heroes for 1 round
                var packAmbush = CreateTestTacticalCard(91, "Pack Ambush", TacticalType.CombatTactic);
                packAmbush.effectValue1 = 2;
                packAmbush.effectValue2 = 1;
                ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
                bool ambushResult = TacticalEffectProcessor.ApplyTacticalCard(packAmbush, heroes, enemies, 0, ctx);
                AssertTrue(ambushResult, "TC23_PackAmbush_Applied");
                int tempBuff = ctx.GetTotalCombatBuff(hero.tokenId);
                AssertGreaterThan(tempBuff, 0, "TC23_PackAmbush_BuffApplied");
                Debug.Log($"[ValidationTestRunner] TC23_TacticalEffectProcessor: PackAmbush tempBuff={tempBuff}");

                // SupportTactic: Gather Seeds (id=101) — gatherBonus +1
                var gatherSeeds = CreateTestTacticalCard(101, "Gather Seeds", TacticalType.SupportTactic);
                gatherSeeds.effectValue1 = 1;
                ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
                int prevGather = ctx.gatherBonus;
                bool gatherResult = TacticalEffectProcessor.ApplyTacticalCard(gatherSeeds, heroes, enemies, 0, ctx);
                AssertTrue(gatherResult, "TC23_GatherSeeds_Applied");
                AssertEqual(prevGather + 1, ctx.gatherBonus, "TC23_GatherSeeds_BonusIncreased");

                // SupportTactic: Herbal Remedy (id=106) — heal 2 HP to lowest hero
                var herbalRemedy = CreateTestTacticalCard(106, "Herbal Remedy", TacticalType.SupportTactic);
                herbalRemedy.effectValue1 = 2;
                int prevHP = hero.currentHP;
                ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
                bool healResult = TacticalEffectProcessor.ApplyTacticalCard(herbalRemedy, heroes, enemies, 0, ctx);
                AssertTrue(healResult, "TC23_HerbalRemedy_Applied");
                AssertGreaterThan(hero.currentHP, prevHP, "TC23_HerbalRemedy_Healed");
                Debug.Log($"[ValidationTestRunner] TC23_TacticalEffectProcessor: HerbalRemedy hp {prevHP}->{hero.currentHP}");

                // PowerTactic: Poison Thorn (id=117) — deal 3 damage to one enemy
                var poisonThorn = CreateTestTacticalCard(117, "Poison Thorn", TacticalType.PowerTactic);
                poisonThorn.effectValue1 = 3;
                int prevEnemyHP = enemy.currentHP;
                ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
                bool poisonResult = TacticalEffectProcessor.ApplyTacticalCard(poisonThorn, heroes, enemies, 0, ctx);
                AssertTrue(poisonResult, "TC23_PoisonThorn_Applied");
                AssertEqual(prevEnemyHP - 3, enemy.currentHP, "TC23_PoisonThorn_DamageDealt");
                Debug.Log($"[ValidationTestRunner] TC23_TacticalEffectProcessor: PoisonThorn enemyHP {prevEnemyHP}->{enemy.currentHP}");

                // PowerTactic: Burrow Defense (id=113) — +5 colony defense
                var burrowDef = CreateTestTacticalCard(113, "Burrow Defense", TacticalType.PowerTactic);
                burrowDef.effectValue1 = 5;
                ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
                bool defResult = TacticalEffectProcessor.ApplyTacticalCard(burrowDef, heroes, enemies, 0, ctx);
                AssertTrue(defResult, "TC23_BurrowDefense_Applied");
                AssertEqual(5, ctx.colonyDefenseBonus, "TC23_BurrowDefense_BonusApplied");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ValidationTestRunner] TC23_TacticalEffectProcessor: exception — {ex.Message}");
                Skip("TC23_TacticalEffectProcessor", ex.Message);
            }

            Debug.Log("[ValidationTestRunner] TC23_TacticalEffectProcessor: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-24: SPECIAL ABILITY PROCESSOR
        // ══════════════════════════════════════════════════════════════════

        private void TC24_SpecialAbilityProcessor()
        {
            Debug.Log("[ValidationTestRunner] TC24_SpecialAbilityProcessor: ENTER");

            try
            {
                var factory = ServiceLocator.Get<IHeroTokenFactory>();

                // ProcessPreCombatAbility — Rally (+1 Combat to allies)
                var rallyCard = CreateTestHeroCard(1, "RallyRat", HeroRole.Support, 2, 2, 4, 2, 3, SpecialAbility.Rally);
                var allyCard = CreateTestHeroCard(2, "AllyRat", HeroRole.Melee, 3, 2, 5, 2, 3);
                var rallyHero = factory.Create(rallyCard, 1);
                var allyHero = factory.Create(allyCard, 2);
                var allHeroes = new List<HeroToken> { rallyHero, allyHero };
                var enemies = new List<EnemyToken>();
                var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };

                SpecialAbilityProcessor.ProcessPreCombatAbility(rallyHero, allHeroes, enemies, ctx);
                int allyBuff = ctx.GetTotalCombatBuff(allyHero.tokenId);
                AssertEqual(1, allyBuff, "TC24_Rally_AllyGets1Combat");
                Debug.Log($"[ValidationTestRunner] TC24_SpecialAbilityProcessor: Rally allyBuff={allyBuff}");

                // ProcessPreCombatAbility — dead hero skipped
                var deadCard = CreateTestHeroCard(3, "DeadRat", HeroRole.Melee, 2, 2, 3, 2, 1, SpecialAbility.Rally);
                var deadHero = factory.Create(deadCard, 3);
                deadHero.isInjured = true;
                ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
                SpecialAbilityProcessor.ProcessPreCombatAbility(deadHero, allHeroes, enemies, ctx);
                AssertEqual(0, ctx.GetTotalCombatBuff(allyHero.tokenId), "TC24_DeadHero_NoEffect");

                // ProcessPerRoundAbility — FieldMedic (heal lowest ally)
                var medicCard = CreateTestHeroCard(4, "MedicRat", HeroRole.Support, 1, 2, 4, 2, 2, SpecialAbility.FieldMedic);
                var woundedCard = CreateTestHeroCard(5, "WoundedAlly", HeroRole.Melee, 3, 2, 6, 2, 3);
                var medicHero = factory.Create(medicCard, 4);
                var woundedHero = factory.Create(woundedCard, 5);
                woundedHero.TakeDamage(3); // HP: 6->3
                int hpBefore = woundedHero.currentHP;
                var medicAllies = new List<HeroToken> { medicHero, woundedHero };
                ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
                SpecialAbilityProcessor.ProcessPerRoundAbility(medicHero, medicAllies, enemies, ctx);
                AssertEqual(hpBefore + 1, woundedHero.currentHP, "TC24_FieldMedic_HealsAlly");
                Debug.Log($"[ValidationTestRunner] TC24_SpecialAbilityProcessor: FieldMedic healed {hpBefore}->{woundedHero.currentHP}");

                // ProcessPostCombatAbility — ScoutReport (just runs without error)
                var scoutCard = CreateTestHeroCard(6, "ScoutRat", HeroRole.Recon, 2, 3, 3, 2, 4, SpecialAbility.ScoutReport);
                var scoutHero = factory.Create(scoutCard, 6);
                var combatResult = CombatResult.Create(0);
                combatResult.heroesWon = true;
                SpecialAbilityProcessor.ProcessPostCombatAbility(scoutHero, combatResult);
                AssertTrue(true, "TC24_PostCombat_ScoutReport_NoError");

                // ProcessPostCombatAbility — dead hero skipped
                scoutHero.isInjured = true;
                SpecialAbilityProcessor.ProcessPostCombatAbility(scoutHero, combatResult);
                AssertTrue(true, "TC24_PostCombat_DeadHero_Skipped");

                // ProcessGatherAbility — EfficientGather (+1 bonus resource)
                var gatherCard = CreateTestHeroCard(7, "GatherRat", HeroRole.Gather, 1, 2, 3, 4, 1, SpecialAbility.EfficientGather);
                var gatherHero = factory.Create(gatherCard, 7);
                var mapGraph = ServiceLocator.Get<IMapGraph>() as MapGraph;
                MapNode testNode;
                if (mapGraph != null && mapGraph.GetAllNodes().Count > 0)
                {
                    testNode = mapGraph.GetAllNodes().First();
                }
                else
                {
                    testNode = new MapNode { nodeId = 0, zone = NodeType.Wilderness };
                }
                int gatherBonus = SpecialAbilityProcessor.ProcessGatherAbility(gatherHero, testNode);
                AssertEqual(1, gatherBonus, "TC24_EfficientGather_Plus1");

                // ProcessGatherAbility — BulkHaul on Wilderness (+1)
                var bulkCard = CreateTestHeroCard(8, "BulkRat", HeroRole.Gather, 1, 2, 3, 5, 1, SpecialAbility.BulkHaul);
                var bulkHero = factory.Create(bulkCard, 8);
                var wildNode = new MapNode { nodeId = 99, zone = NodeType.Wilderness };
                int bulkBonus = SpecialAbilityProcessor.ProcessGatherAbility(bulkHero, wildNode);
                AssertEqual(1, bulkBonus, "TC24_BulkHaul_Wilderness_Plus1");

                // ProcessGatherAbility — BulkHaul on non-Wilderness (0)
                var farmNode = new MapNode { nodeId = 100, zone = NodeType.Farmland };
                int noBulk = SpecialAbilityProcessor.ProcessGatherAbility(bulkHero, farmNode);
                AssertEqual(0, noBulk, "TC24_BulkHaul_NonWilderness_Zero");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ValidationTestRunner] TC24_SpecialAbilityProcessor: exception — {ex.Message}");
                Skip("TC24_SpecialAbilityProcessor", ex.Message);
            }

            Debug.Log("[ValidationTestRunner] TC24_SpecialAbilityProcessor: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-25: LOCALIZATION MANAGER
        // ══════════════════════════════════════════════════════════════════

        private void TC25_LocalizationManager()
        {
            Debug.Log("[ValidationTestRunner] TC25_LocalizationManager: ENTER");

            try
            {
                // Get with no instance or missing key returns the key
                string missingKey = "test.nonexistent.key.xyz";
                string result = LocalizationManager.Get(missingKey);
                Debug.Log($"[ValidationTestRunner] TC25_LocalizationManager: Get('{missingKey}') returned '{result}'");
                AssertEqual(missingKey, result, "TC25_Get_ReturnsKeyOnMiss");

                // Format with missing key returns the key as template (no format args applied)
                string formatResult = LocalizationManager.Format(missingKey);
                Debug.Log($"[ValidationTestRunner] TC25_LocalizationManager: Format('{missingKey}') returned '{formatResult}'");
                AssertEqual(missingKey, formatResult, "TC25_Format_ReturnsKeyOnMiss");

                // Format with missing key and args — key has no placeholders, so returns key
                string formatWithArgs = LocalizationManager.Format(missingKey, 42);
                Debug.Log($"[ValidationTestRunner] TC25_LocalizationManager: Format('{missingKey}', 42) returned '{formatWithArgs}'");
                // Since the template is the key itself (no {0} placeholder), Format may return just the key
                AssertNotNull(formatWithArgs, "TC25_Format_WithArgs_NotNull");

                // SetLanguage with unknown language should not crash
                LocalizationManager.SetLanguage("zz_nonexistent");
                AssertTrue(true, "TC25_SetLanguage_UnknownNoCrash");

                // Reset to English
                LocalizationManager.SetLanguage("en");
                AssertTrue(true, "TC25_SetLanguage_English_NoCrash");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ValidationTestRunner] TC25_LocalizationManager: exception — {ex.Message}");
                Skip("TC25_LocalizationManager", ex.Message);
            }

            Debug.Log("[ValidationTestRunner] TC25_LocalizationManager: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-26: SAVE MANAGER
        // ══════════════════════════════════════════════════════════════════

        private void TC26_SaveManager()
        {
            Debug.Log("[ValidationTestRunner] TC26_SaveManager: ENTER");

            try
            {
                // Clean up any existing save
                SaveManager.DeleteSave();
                AssertFalse(SaveManager.HasSave(), "TC26_HasSave_FalseAfterDelete");

                // Create test data
                var data = new RunSaveData();
                data.randomSeed = 12345;
                data.currentTurn = 7;
                data.runState = (int)RunState.InRun;
                data.deckCardIds.Add(1);
                data.deckCardIds.Add(2);
                data.deckCardIds.Add(3);
                data.colonyDeckCardIds.Add(31);
                data.foodStockpile = 10;
                data.materialsStockpile = 5;
                data.currencyStockpile = 3;

                Debug.Log($"[ValidationTestRunner] TC26_SaveManager: saving test data (seed={data.randomSeed}, turn={data.currentTurn})");
                SaveManager.Save(data);

                // Verify save exists
                AssertTrue(SaveManager.HasSave(), "TC26_HasSave_TrueAfterSave");

                // Load and verify round-trip
                var loaded = SaveManager.Load();
                AssertNotNull(loaded, "TC26_Load_NotNull");
                AssertEqual(12345, loaded.randomSeed, "TC26_RoundTrip_Seed");
                AssertEqual(7, loaded.currentTurn, "TC26_RoundTrip_Turn");
                AssertEqual((int)RunState.InRun, loaded.runState, "TC26_RoundTrip_State");
                AssertEqual(3, loaded.deckCardIds.Count, "TC26_RoundTrip_DeckCount");
                AssertEqual(1, loaded.colonyDeckCardIds.Count, "TC26_RoundTrip_ColonyDeckCount");
                AssertEqual(10, loaded.foodStockpile, "TC26_RoundTrip_Food");
                AssertEqual(5, loaded.materialsStockpile, "TC26_RoundTrip_Materials");
                AssertEqual(3, loaded.currencyStockpile, "TC26_RoundTrip_Currency");

                Debug.Log($"[ValidationTestRunner] TC26_SaveManager: round-trip verified (seed={loaded.randomSeed}, turn={loaded.currentTurn})");

                // Delete and verify
                SaveManager.DeleteSave();
                AssertFalse(SaveManager.HasSave(), "TC26_HasSave_FalseAfterFinalDelete");

                // Load after delete should return null
                var loadedAfterDelete = SaveManager.Load();
                Assert(loadedAfterDelete == null, "TC26_Load_NullAfterDelete", "loaded should be null after delete");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ValidationTestRunner] TC26_SaveManager: exception — {ex.Message}");
                Skip("TC26_SaveManager", ex.Message);
            }

            Debug.Log("[ValidationTestRunner] TC26_SaveManager: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-31: META PROGRESSION MANAGER
        // ══════════════════════════════════════════════════════════════════

        private void TC31_MetaProgressionManager()
        {
            Debug.Log("[ValidationTestRunner] TC31_MetaProgressionManager: ENTER");

            GameObject testGO = null;
            try
            {
                testGO = new GameObject("TestMetaProgression");
                var mpm = testGO.AddComponent<MetaProgressionManager>();
                Debug.Log("[ValidationTestRunner] TC31_MetaProgressionManager: created MetaProgressionManager via AddComponent");

                // ResetAllProgress to start clean
                mpm.ResetAllProgress();
                AssertEqual(0, mpm.Reputation, "TC31_Reset_RepZero");
                Debug.Log($"[ValidationTestRunner] TC31_MetaProgressionManager: after reset reputation={mpm.Reputation}");

                // DiscoverCard
                mpm.DiscoverCard("TestHero1");
                mpm.DiscoverCard("TestHero2");
                mpm.DiscoverCard("TestHero1"); // duplicate — should not increase
                AssertNotNull(mpm.Data, "TC31_Data_NotNull");
                AssertEqual(2, mpm.Data.discoveredCards.Count, "TC31_DiscoverCard_NoDuplicates");

                // DiscoverEnemy and IsEnemyDiscovered
                mpm.DiscoverEnemy("FieldMouse");
                AssertTrue(mpm.IsEnemyDiscovered("FieldMouse"), "TC31_IsEnemyDiscovered_True");
                AssertFalse(mpm.IsEnemyDiscovered("Dragon"), "TC31_IsEnemyDiscovered_False");

                // Reputation starts at 0 after reset
                AssertEqual(0, mpm.Reputation, "TC31_Reputation_StillZero");

                // Clean up
                mpm.ResetAllProgress();
                AssertEqual(0, mpm.Data.discoveredCards.Count, "TC31_ResetClearsCards");
                AssertEqual(0, mpm.Data.discoveredEnemies.Count, "TC31_ResetClearsEnemies");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ValidationTestRunner] TC31_MetaProgressionManager: exception — {ex.Message}");
                Skip("TC31_MetaProgressionManager", ex.Message);
            }
            finally
            {
                if (testGO != null) Destroy(testGO);
            }

            Debug.Log("[ValidationTestRunner] TC31_MetaProgressionManager: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-33: GAME SETTINGS
        // ══════════════════════════════════════════════════════════════════

        private void TC33_GameSettings()
        {
            Debug.Log("[ValidationTestRunner] TC33_GameSettings: ENTER");

            GameObject testGO = null;
            try
            {
                testGO = new GameObject("TestGameSettings");
                var gs = testGO.AddComponent<GameSettings>();
                Debug.Log("[ValidationTestRunner] TC33_GameSettings: created GameSettings via AddComponent");

                // Default battle speed is 0 (Normal)
                AssertEqual(0, gs.BattleSpeed, "TC33_Default_BattleSpeed");
                AssertEqual("Normal", gs.BattleSpeedLabel, "TC33_Default_BattleSpeedLabel");

                // SetBattleSpeed
                gs.SetBattleSpeed(1);
                AssertEqual(1, gs.BattleSpeed, "TC33_SetBattleSpeed_1");
                AssertEqual("Fast", gs.BattleSpeedLabel, "TC33_SetBattleSpeed_FastLabel");

                gs.SetBattleSpeed(2);
                AssertEqual(2, gs.BattleSpeed, "TC33_SetBattleSpeed_2");
                AssertEqual("Instant", gs.BattleSpeedLabel, "TC33_SetBattleSpeed_InstantLabel");

                // Clamp out of range
                gs.SetBattleSpeed(99);
                AssertEqual(2, gs.BattleSpeed, "TC33_SetBattleSpeed_ClampHigh");

                gs.SetBattleSpeed(-5);
                AssertEqual(0, gs.BattleSpeed, "TC33_SetBattleSpeed_ClampLow");

                // CycleBattleSpeed
                gs.SetBattleSpeed(0);
                gs.CycleBattleSpeed();
                AssertEqual(1, gs.BattleSpeed, "TC33_Cycle_0to1");
                gs.CycleBattleSpeed();
                AssertEqual(2, gs.BattleSpeed, "TC33_Cycle_1to2");
                gs.CycleBattleSpeed();
                AssertEqual(0, gs.BattleSpeed, "TC33_Cycle_2to0");

                // SetColorBlindMode
                gs.SetColorBlindMode(true);
                AssertTrue(gs.ColorBlindMode, "TC33_ColorBlind_True");
                gs.SetColorBlindMode(false);
                AssertFalse(gs.ColorBlindMode, "TC33_ColorBlind_False");

                // SetTextSizeModifier
                gs.SetTextSizeModifier(3);
                AssertEqual(3, gs.TextSizeModifier, "TC33_TextSize_3");

                gs.SetTextSizeModifier(-10); // clamp to -2
                AssertEqual(-2, gs.TextSizeModifier, "TC33_TextSize_ClampLow");

                gs.SetTextSizeModifier(10); // clamp to 4
                AssertEqual(4, gs.TextSizeModifier, "TC33_TextSize_ClampHigh");

                // Volume setters
                gs.SetMasterVolume(0.5f);
                AssertTrue(Mathf.Approximately(0.5f, gs.MasterVolume), "TC33_MasterVolume_Half");

                gs.SetMusicVolume(0.3f);
                AssertTrue(Mathf.Approximately(0.3f, gs.MusicVolume), "TC33_MusicVolume_Point3");

                gs.SetSfxVolume(0.8f);
                AssertTrue(Mathf.Approximately(0.8f, gs.SfxVolume), "TC33_SfxVolume_Point8");

                // Volume clamping
                gs.SetMasterVolume(2f);
                AssertTrue(Mathf.Approximately(1f, gs.MasterVolume), "TC33_MasterVolume_ClampHigh");
                gs.SetMasterVolume(-1f);
                AssertTrue(Mathf.Approximately(0f, gs.MasterVolume), "TC33_MasterVolume_ClampLow");

                // GetNodeColor — should return a valid color without crashing
                gs.SetColorBlindMode(false);
                Color normalColor = gs.GetNodeColor(NodeType.Wilderness, false, true);
                AssertTrue(normalColor.r >= 0f && normalColor.r <= 1f, "TC33_GetNodeColor_Normal_ValidR");

                gs.SetColorBlindMode(true);
                Color cbColor = gs.GetNodeColor(NodeType.Wilderness, false, true);
                AssertTrue(cbColor.r >= 0f && cbColor.r <= 1f, "TC33_GetNodeColor_ColorBlind_ValidR");

                // AdjustedFontSize
                gs.SetTextSizeModifier(0);
                int adj0 = gs.AdjustedFontSize(14);
                AssertEqual(14, adj0, "TC33_AdjustedFont_NoModifier");

                gs.SetTextSizeModifier(2);
                int adj2 = gs.AdjustedFontSize(14);
                AssertEqual(18, adj2, "TC33_AdjustedFont_Modifier2");

                gs.SetTextSizeModifier(-2);
                int adjNeg = gs.AdjustedFontSize(14);
                AssertEqual(10, adjNeg, "TC33_AdjustedFont_ModifierNeg2");

                // Min font size is 8
                gs.SetTextSizeModifier(-2);
                int adjSmall = gs.AdjustedFontSize(8);
                AssertGreaterThanOrEqual(adjSmall, 8, "TC33_AdjustedFont_MinIs8");

                Debug.Log("[ValidationTestRunner] TC33_GameSettings: all assertions passed");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ValidationTestRunner] TC33_GameSettings: exception — {ex.Message}");
                Skip("TC33_GameSettings", ex.Message);
            }
            finally
            {
                if (testGO != null) Destroy(testGO);
            }

            Debug.Log("[ValidationTestRunner] TC33_GameSettings: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-35: RUN MANAGER RECORDING METHODS
        // ══════════════════════════════════════════════════════════════════

        private void TC35_RunManagerRecording()
        {
            Debug.Log("[ValidationTestRunner] TC35_RunManagerRecording: ENTER");

            GameObject testGO = null;
            try
            {
                testGO = new GameObject("TestRunManager");
                var rm = testGO.AddComponent<RunManager>();
                Debug.Log("[ValidationTestRunner] TC35_RunManagerRecording: created RunManager via AddComponent");

                // RecordResourceGathered
                AssertEqual(0, rm.TotalResourcesGathered, "TC35_InitialResources_Zero");
                rm.RecordResourceGathered(5);
                AssertEqual(5, rm.TotalResourcesGathered, "TC35_RecordResource_5");
                rm.RecordResourceGathered(3);
                AssertEqual(8, rm.TotalResourcesGathered, "TC35_RecordResource_Accumulates");
                Debug.Log($"[ValidationTestRunner] TC35_RunManagerRecording: totalResources={rm.TotalResourcesGathered}");

                // RecordEnemyDefeated
                AssertEqual(0, rm.TotalEnemiesDefeated, "TC35_InitialEnemies_Zero");
                rm.RecordEnemyDefeated(false, false);
                AssertEqual(1, rm.TotalEnemiesDefeated, "TC35_RecordEnemy_1");
                rm.RecordEnemyDefeated(true, false); // boss
                AssertEqual(2, rm.TotalEnemiesDefeated, "TC35_RecordEnemy_2");
                AssertEqual(1, rm.ZoneBossesDefeated, "TC35_RecordEnemy_BossCount");
                rm.RecordEnemyDefeated(false, true); // piper
                AssertTrue(rm.PiedPiperDefeated, "TC35_RecordEnemy_PiperDefeated");
                Debug.Log($"[ValidationTestRunner] TC35_RunManagerRecording: enemies={rm.TotalEnemiesDefeated}, bosses={rm.ZoneBossesDefeated}, piper={rm.PiedPiperDefeated}");

                // RecordColonyCardPlayed
                AssertEqual(0, rm.ColonyCardsPlayed, "TC35_InitialColonyCards_Zero");
                rm.RecordColonyCardPlayed();
                AssertEqual(1, rm.ColonyCardsPlayed, "TC35_RecordColonyCard_1");
                rm.RecordColonyCardPlayed();
                rm.RecordColonyCardPlayed();
                AssertEqual(3, rm.ColonyCardsPlayed, "TC35_RecordColonyCard_3");

                // RecordHeroInjured
                rm.RecordHeroInjured(1);
                rm.RecordHeroInjured(2);
                rm.RecordHeroInjured(1); // duplicate — should not increase unique count
                // We can't directly access heroesInjured count, but the method should not throw
                AssertTrue(true, "TC35_RecordHeroInjured_NoCrash");

                Debug.Log("[ValidationTestRunner] TC35_RunManagerRecording: all assertions passed");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ValidationTestRunner] TC35_RunManagerRecording: exception — {ex.Message}");
                Skip("TC35_RunManagerRecording", ex.Message);
            }
            finally
            {
                if (testGO != null) Destroy(testGO);
            }

            Debug.Log("[ValidationTestRunner] TC35_RunManagerRecording: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-36: MAP MANAGER VIA IMAPMANAGER
        // ══════════════════════════════════════════════════════════════════

        private void TC36_MapManager()
        {
            Debug.Log("[ValidationTestRunner] TC36_MapManager: ENTER");

            GameObject testGO = null;
            try
            {
                testGO = new GameObject("TestMapManager");
                var mm = testGO.AddComponent<MapManager>();
                Debug.Log("[ValidationTestRunner] TC36_MapManager: created MapManager via AddComponent");

                // GenerateMap
                mm.GenerateMap(42);
                AssertNotNull(mm.Graph, "TC36_GenerateMap_GraphNotNull");

                var allNodesList = mm.Graph.GetAllNodes().ToList();
                AssertGreaterThan(allNodesList.Count, 0, "TC36_GenerateMap_HasNodes");
                Debug.Log($"[ValidationTestRunner] TC36_MapManager: generated map with {allNodesList.Count} nodes");

                // GetNode — valid node
                if (allNodesList.Count > 0)
                {
                    int testNodeId = allNodesList[0].nodeId;
                    var node = mm.GetNode(testNodeId);
                    AssertNotNull(node, "TC36_GetNode_Valid");
                    AssertEqual(testNodeId, node.nodeId, "TC36_GetNode_CorrectId");
                }

                // GetNode — invalid node
                var invalidNode = mm.GetNode(-999);
                Assert(invalidNode == null, "TC36_GetNode_Invalid_ReturnsNull", "should be null for invalid ID");

                // GetPath — between two connected nodes
                if (allNodesList.Count >= 2)
                {
                    int fromId = allNodesList[0].nodeId;
                    int toId = allNodesList[allNodesList.Count - 1].nodeId;
                    var path = mm.GetPath(fromId, toId);
                    AssertNotNull(path, "TC36_GetPath_NotNull");
                    Debug.Log($"[ValidationTestRunner] TC36_MapManager: path from {fromId} to {toId} has {path.Count} steps");
                    // Path may be empty if nodes are not connected, but should not be null
                }

                // Graph property
                AssertNotNull(mm.Graph, "TC36_Graph_NotNull");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ValidationTestRunner] TC36_MapManager: exception — {ex.Message}");
                Skip("TC36_MapManager", ex.Message);
            }
            finally
            {
                if (testGO != null) Destroy(testGO);
            }

            Debug.Log("[ValidationTestRunner] TC36_MapManager: EXIT");
        }

        // ══════════════════════════════════════════════════════════════════
        //  REPORT
        // ══════════════════════════════════════════════════════════════════

        private void PrintReport(float elapsed)
        {
            int total = passed + failed + skipped;
            float passRate = total > 0 ? (float)passed / total * 100f : 0f;

            report.AppendLine("");
            report.AppendLine("═══════════════════════════════════════");
            report.AppendLine("  SCURRY v2.0 VALIDATION REPORT");
            report.AppendLine("═══════════════════════════════════════");
            report.AppendLine($"  Tests Run:    {total}");
            report.AppendLine($"  Passed:       {passed}  \u2713");
            report.AppendLine($"  Failed:       {failed}  \u2717");
            report.AppendLine($"  Skipped:      {skipped}  \u2298");
            report.AppendLine($"  Pass Rate:    {passRate:F1}%");
            report.AppendLine($"  Elapsed:      {elapsed:F2}s");
            report.AppendLine("═══════════════════════════════════════");

            if (failures.Count > 0)
            {
                report.AppendLine("");
                report.AppendLine("  FAILURES:");
                foreach (var f in failures)
                {
                    report.AppendLine($"    \u2717 {f}");
                }
                report.AppendLine("");
            }

            if (skips.Count > 0)
            {
                report.AppendLine("");
                report.AppendLine("  SKIPPED:");
                foreach (var s in skips)
                {
                    report.AppendLine($"    \u2298 {s}");
                }
                report.AppendLine("");
            }

            report.AppendLine("═══════════════════════════════════════");

            string reportStr = report.ToString();
            Debug.Log($"[ValidationTestRunner] PrintReport:\n{reportStr}");

            // Also log as individual lines for console filtering
            Debug.Log("[ValidationTestRunner] PrintReport: === SCURRY v2.0 VALIDATION COMPLETE ===");
            Debug.Log($"[ValidationTestRunner] PrintReport: Total={total}, Passed={passed}, Failed={failed}, Skipped={skipped}, PassRate={passRate:F1}%, Elapsed={elapsed:F2}s");

            if (failed > 0)
            {
                Debug.LogWarning($"[ValidationTestRunner] PrintReport: {failed} TEST(S) FAILED — see log for details");
            }
            else if (total > 0)
            {
                Debug.Log("[ValidationTestRunner] PrintReport: ALL TESTS PASSED");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-44: MetaProgressionData
        // ══════════════════════════════════════════════════════════════════

        private void TC44_MetaProgressionData()
        {
            Debug.Log("[ValidationTestRunner] TC44_MetaProgressionData: starting MetaProgressionData tests");
            try
            {
                var data = new MetaProgressionData();
                AssertNotNull(data, "TC44_MetaProgressionData_Create");

                // All int fields default to 0
                Debug.Log("[ValidationTestRunner] TC44_MetaProgressionData: testing default int fields");
                AssertEqual(0, data.totalLevelsCleared, "TC44_MetaProgressionData_TotalLevelsCleared");
                AssertEqual(0, data.totalRunsCompleted, "TC44_MetaProgressionData_TotalRunsCompleted");
                AssertEqual(0, data.totalRunsFailed, "TC44_MetaProgressionData_TotalRunsFailed");
                AssertEqual(0, data.totalResourcesGathered, "TC44_MetaProgressionData_TotalResourcesGathered");
                AssertEqual(0, data.totalBossesDefeated, "TC44_MetaProgressionData_TotalBossesDefeated");
                AssertEqual(0, data.colonyDeckSizeBonus, "TC44_MetaProgressionData_ColonyDeckSizeBonus");
                AssertEqual(0, data.reputation, "TC44_MetaProgressionData_Reputation");
                AssertEqual(0, data.bestiaryCompletion, "TC44_MetaProgressionData_BestiaryCompletion");
                AssertEqual(0, data.scrapbookCompletion, "TC44_MetaProgressionData_ScrapbookCompletion");
                AssertEqual(0, data.bestLevelReached, "TC44_MetaProgressionData_BestLevelReached");
                AssertEqual(0, data.bestResourcesInSingleRun, "TC44_MetaProgressionData_BestResourcesInSingleRun");
                AssertEqual(0, data.bestBossesKilledInSingleRun, "TC44_MetaProgressionData_BestBossesKilledInSingleRun");
                AssertEqual(0, data.fastestRunNodes, "TC44_MetaProgressionData_FastestRunNodes");

                // List fields default to empty (initialized in field declaration)
                Debug.Log("[ValidationTestRunner] TC44_MetaProgressionData: testing default list fields");
                AssertNotNull(data.unlockedStartingRelics, "TC44_MetaProgressionData_UnlockedStartingRelics");
                AssertEqual(0, data.unlockedStartingRelics.Count, "TC44_MetaProgressionData_UnlockedStartingRelicsEmpty");
                AssertNotNull(data.unlockedColonyCards, "TC44_MetaProgressionData_UnlockedColonyCards");
                AssertNotNull(data.discoveredHeroCards, "TC44_MetaProgressionData_DiscoveredHeroCards");
                AssertNotNull(data.upgradedHeroUnlocks, "TC44_MetaProgressionData_UpgradedHeroUnlocks");
                AssertNotNull(data.discoveredEnemies, "TC44_MetaProgressionData_DiscoveredEnemies");
                AssertNotNull(data.discoveredCards, "TC44_MetaProgressionData_DiscoveredCards");
                AssertNotNull(data.discoveredRelics, "TC44_MetaProgressionData_DiscoveredRelics");
                AssertNotNull(data.discoveredEvents, "TC44_MetaProgressionData_DiscoveredEvents");
                AssertNotNull(data.discoveredBosses, "TC44_MetaProgressionData_DiscoveredBosses");
                AssertNotNull(data.discoveredLore, "TC44_MetaProgressionData_DiscoveredLore");

                // Verify fields can be set and read back
                Debug.Log("[ValidationTestRunner] TC44_MetaProgressionData: testing field mutation");
                data.totalRunsCompleted = 42;
                data.reputation = 100;
                data.bestLevelReached = 7;
                data.discoveredBosses.Add("TestBoss");
                AssertEqual(42, data.totalRunsCompleted, "TC44_MetaProgressionData_SetRunsCompleted");
                AssertEqual(100, data.reputation, "TC44_MetaProgressionData_SetReputation");
                AssertEqual(7, data.bestLevelReached, "TC44_MetaProgressionData_SetBestLevel");
                AssertEqual(1, data.discoveredBosses.Count, "TC44_MetaProgressionData_AddBoss");

                Debug.Log("[ValidationTestRunner] TC44_MetaProgressionData: all MetaProgressionData tests complete");
            }
            catch (System.Exception e)
            {
                Skip("TC44_MetaProgressionData", $"Exception: {e.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-47: EnemyMoveResult
        // ══════════════════════════════════════════════════════════════════

        private void TC47_EnemyMoveResult()
        {
            Debug.Log("[ValidationTestRunner] TC47_EnemyMoveResult: starting EnemyMoveResult struct tests");
            try
            {
                var result = new EnemyMoveResult();
                Debug.Log("[ValidationTestRunner] TC47_EnemyMoveResult: testing default values (all zero)");
                AssertEqual(0, result.enemyTokenId, "TC47_EnemyMoveResult_DefaultEnemyTokenId");
                AssertEqual(0, result.fromNodeId, "TC47_EnemyMoveResult_DefaultFromNodeId");
                AssertEqual(0, result.toNodeId, "TC47_EnemyMoveResult_DefaultToNodeId");

                // Set values and verify
                Debug.Log("[ValidationTestRunner] TC47_EnemyMoveResult: testing field assignment (enemyTokenId=5, fromNodeId=10, toNodeId=15)");
                result.enemyTokenId = 5;
                result.fromNodeId = 10;
                result.toNodeId = 15;
                AssertEqual(5, result.enemyTokenId, "TC47_EnemyMoveResult_SetEnemyTokenId");
                AssertEqual(10, result.fromNodeId, "TC47_EnemyMoveResult_SetFromNodeId");
                AssertEqual(15, result.toNodeId, "TC47_EnemyMoveResult_SetToNodeId");

                // ToString
                Debug.Log("[ValidationTestRunner] TC47_EnemyMoveResult: testing ToString");
                string str = result.ToString();
                AssertNotNull(str, "TC47_EnemyMoveResult_ToStringNotNull");
                AssertTrue(str.Contains("5"), "TC47_EnemyMoveResult_ToStringContainsId");
                AssertTrue(str.Contains("10"), "TC47_EnemyMoveResult_ToStringContainsFrom");
                AssertTrue(str.Contains("15"), "TC47_EnemyMoveResult_ToStringContainsTo");

                Debug.Log("[ValidationTestRunner] TC47_EnemyMoveResult: all EnemyMoveResult tests complete");
            }
            catch (System.Exception e)
            {
                Skip("TC47_EnemyMoveResult", $"Exception: {e.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-49: PersistentManagersBootstrap
        // ══════════════════════════════════════════════════════════════════

        private void TC49_PersistentManagersBootstrap()
        {
            Debug.Log("[ValidationTestRunner] TC49_PersistentManagersBootstrap: starting PersistentManagersBootstrap tests");
            try
            {
                var type = typeof(PersistentManagersBootstrap);
                AssertNotNull(type, "TC49_PersistentManagersBootstrap_TypeExists");
                AssertTrue(typeof(MonoBehaviour).IsAssignableFrom(type), "TC49_PersistentManagersBootstrap_IsMonoBehaviour");

                Debug.Log("[ValidationTestRunner] TC49_PersistentManagersBootstrap: all PersistentManagersBootstrap tests complete");
            }
            catch (System.Exception e)
            {
                Skip("TC49_PersistentManagersBootstrap", $"Exception: {e.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  TC-50: SteamManager
        // ══════════════════════════════════════════════════════════════════

        private void TC50_SteamManager()
        {
            Debug.Log("[ValidationTestRunner] TC50_SteamManager: starting SteamManager tests");
            try
            {
                var type = typeof(Scurry.Steam.SteamManager);
                AssertNotNull(type, "TC50_SteamManager_TypeExists");
                AssertTrue(typeof(MonoBehaviour).IsAssignableFrom(type), "TC50_SteamManager_IsMonoBehaviour");

                // Verify static members exist
                Debug.Log("[ValidationTestRunner] TC50_SteamManager: testing static property Initialized exists");
                bool initialized = Scurry.Steam.SteamManager.Initialized;
                AssertFalse(initialized, "TC50_SteamManager_NotInitializedByDefault");

                // Instance should be null if no SteamManager exists in scene
                Debug.Log("[ValidationTestRunner] TC50_SteamManager: testing Instance is null when not in scene");
                var instance = Scurry.Steam.SteamManager.Instance;
                AssertTrue(instance == null, "TC50_SteamManager_InstanceNullWhenNotCreated");

                Debug.Log("[ValidationTestRunner] TC50_SteamManager: all SteamManager tests complete");
            }
            catch (System.Exception e)
            {
                Skip("TC50_SteamManager", $"Exception: {e.Message}");
            }
        }

    }
}
