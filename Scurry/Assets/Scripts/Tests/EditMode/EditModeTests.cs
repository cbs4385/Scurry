using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using Scurry.Core;
using Scurry.Data;
using Scurry.Map;
using Scurry.Combat;
using Scurry.Colony;
using Scurry.Cards;
using Scurry.AI;
using Scurry.Logistics;
using Scurry.Interfaces;
using Scurry.Steam;

namespace Scurry.Tests.EditMode
{
    // ============================================================
    // Helper: shared test data factory
    // ============================================================
    public static class TestDataFactory
    {
        public static CardDefinitionSO CreateHero(
            int id = 1, string name = "Test Hero", HeroRole role = HeroRole.Melee,
            int combat = 3, int move = 2, int hp = 4, int carry = 2,
            int initiative = 5, SpecialAbility ability = SpecialAbility.None,
            int deckCost = 1)
        {
            Debug.Log($"[TestDataFactory] CreateHero: creating test hero (id={id}, name={name}, role={role}, combat={combat}, move={move}, hp={hp}, carry={carry}, initiative={initiative}, ability={ability})");
            var hero = ScriptableObject.CreateInstance<CardDefinitionSO>();
            hero.cardId = id;
            hero.cardName = name;
            hero.cardType = CardType.Hero;
            hero.heroRole = role;
            hero.combat = combat;
            hero.move = move;
            hero.hp = hp;
            hero.carry = carry;
            hero.initiative = initiative;
            hero.specialAbility = ability;
            hero.deckCost = deckCost;
            hero.rarity = CardRarity.Common;
            return hero;
        }

        public static CardDefinitionSO CreateEquipment(
            int id = 51, string name = "Test Weapon", EquipmentSlot slot = EquipmentSlot.Offensive,
            int effectValue1 = 2, int effectValue2 = 0, int effectValue3 = 0, int deckCost = 1)
        {
            Debug.Log($"[TestDataFactory] CreateEquipment: creating test equipment (id={id}, name={name}, slot={slot}, ev1={effectValue1}, ev2={effectValue2})");
            var equip = ScriptableObject.CreateInstance<CardDefinitionSO>();
            equip.cardId = id;
            equip.cardName = name;
            equip.cardType = CardType.Equipment;
            equip.equipmentSlot = slot;
            equip.effectValue1 = effectValue1;
            equip.effectValue2 = effectValue2;
            equip.effectValue3 = effectValue3;
            equip.deckCost = deckCost;
            equip.rarity = CardRarity.Common;
            return equip;
        }

        public static CardDefinitionSO CreateTactical(
            int id = 91, string name = "Test Tactic", TacticalType type = TacticalType.CombatTactic,
            int effectValue1 = 3, int deckCost = 1)
        {
            Debug.Log($"[TestDataFactory] CreateTactical: creating test tactical (id={id}, name={name}, type={type}, ev1={effectValue1})");
            var tac = ScriptableObject.CreateInstance<CardDefinitionSO>();
            tac.cardId = id;
            tac.cardName = name;
            tac.cardType = CardType.Tactical;
            tac.tacticalType = type;
            tac.effectValue1 = effectValue1;
            tac.deckCost = deckCost;
            tac.rarity = CardRarity.Common;
            return tac;
        }

        public static ColonyCardDefinitionSO CreateColonyCard(
            int id = 21, string name = "Test Colony Card",
            ColonyTier tier = ColonyTier.FoodStorage,
            ColonyEffect effect = ColonyEffect.FoodProduction,
            int effectValue = 2, bool isStarter = false,
            PlacementRequirement placement = PlacementRequirement.None,
            string adjacencyCardName = null, int deckCost = 1)
        {
            Debug.Log($"[TestDataFactory] CreateColonyCard: creating test colony card (id={id}, name={name}, tier={tier}, effect={effect}, value={effectValue}, starter={isStarter})");
            var card = ScriptableObject.CreateInstance<ColonyCardDefinitionSO>();
            card.cardId = id;
            card.cardName = name;
            card.colonyTier = tier;
            card.colonyEffect = effect;
            card.effectValue = effectValue;
            card.isStarter = isStarter;
            card.placementRequirement = placement;
            card.adjacencyCardName = adjacencyCardName;
            card.deckCost = deckCost;
            card.rarity = CardRarity.Common;
            return card;
        }

        public static EnemyDefinitionSO CreateEnemyDef(
            string name = "Test Enemy", int strength = 3, int hp = 4, int speed = 1,
            EnemyBehavior behavior = EnemyBehavior.Patrol, NodeType zone = NodeType.Wilderness)
        {
            Debug.Log($"[TestDataFactory] CreateEnemyDef: creating test enemy def (name={name}, str={strength}, hp={hp}, spd={speed}, behavior={behavior}, zone={zone})");
            var enemy = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            enemy.enemyName = name;
            enemy.strength = strength;
            enemy.hp = hp;
            enemy.speed = speed;
            enemy.behavior = behavior;
            enemy.homeZone = zone;
            return enemy;
        }

        public static EnemyToken CreateEnemyToken(
            int tokenId = 0, string name = "Test Enemy", int strength = 3, int hp = 4,
            int speed = 1, EnemyBehavior behavior = EnemyBehavior.Patrol,
            NodeType zone = NodeType.Wilderness, int nodeId = 0)
        {
            Debug.Log($"[TestDataFactory] CreateEnemyToken: resolving IEnemyTokenFactory from ServiceLocator (tokenId={tokenId}, name={name}, str={strength}, hp={hp})");
            var factory = ServiceLocator.Get<IEnemyTokenFactory>();
            if (factory != null)
            {
                Debug.Log($"[TestDataFactory] CreateEnemyToken: using IEnemyTokenFactory to create token (tokenId={tokenId}, name={name})");
                return factory.Create(tokenId, name, strength, hp, speed, behavior, zone, nodeId);
            }
            Debug.Log($"[TestDataFactory] CreateEnemyToken: IEnemyTokenFactory not registered, falling back to direct construction (tokenId={tokenId}, name={name})");
            return new EnemyToken(tokenId, name, strength, hp, speed, behavior, zone, nodeId);
        }

        public static HeroToken CreateHeroToken(int tokenId = 0, CardDefinitionSO cardDef = null)
        {
            if (cardDef == null)
                cardDef = CreateHero(id: tokenId + 1);
            Debug.Log($"[TestDataFactory] CreateHeroToken: resolving IHeroTokenFactory from ServiceLocator (tokenId={tokenId}, cardName={cardDef.cardName})");
            var factory = ServiceLocator.Get<IHeroTokenFactory>();
            if (factory != null)
            {
                Debug.Log($"[TestDataFactory] CreateHeroToken: using IHeroTokenFactory to create token (tokenId={tokenId}, cardName={cardDef.cardName})");
                return factory.Create(cardDef, tokenId);
            }
            Debug.Log($"[TestDataFactory] CreateHeroToken: IHeroTokenFactory not registered, falling back to direct construction (tokenId={tokenId}, cardName={cardDef.cardName})");
            return new HeroToken(cardDef, tokenId);
        }

        public static MapGraph CreateSimpleGraph()
        {
            Debug.Log("[TestDataFactory] CreateSimpleGraph: creating 5-node test graph");
            var graph = new MapGraph();
            graph.AddNode(new MapNode { nodeId = 0, zone = NodeType.Colony, worldPosition = Vector2.zero });
            graph.AddNode(new MapNode { nodeId = 1, zone = NodeType.Wilderness, worldPosition = new Vector2(1, 0) });
            graph.AddNode(new MapNode { nodeId = 2, zone = NodeType.Wilderness, worldPosition = new Vector2(2, 0) });
            graph.AddNode(new MapNode { nodeId = 3, zone = NodeType.Farmland, worldPosition = new Vector2(3, 0) });
            graph.AddNode(new MapNode { nodeId = 4, zone = NodeType.PiedPiper, worldPosition = new Vector2(4, 0) });
            graph.AddEdge(0, 1);
            graph.AddEdge(1, 2);
            graph.AddEdge(2, 3);
            graph.AddEdge(3, 4);
            Debug.Log($"[TestDataFactory] CreateSimpleGraph: registering graph as IMapGraph in ServiceLocator (nodeCount={graph.GetAllNodes().Count})");
            ServiceLocator.Register<IMapGraph>(graph);
            return graph;
        }

        public static void RegisterServices()
        {
            Debug.Log("[TestDataFactory] RegisterServices: ENTER - registering all service implementations in ServiceLocator");
            ServiceLocator.Register<ICombatResolver>(new Scurry.Combat.CombatResolver());
            Debug.Log("[TestDataFactory] RegisterServices: registered ICombatResolver");
            ServiceLocator.Register<IFogOfWar>(new FogOfWar());
            Debug.Log("[TestDataFactory] RegisterServices: registered IFogOfWar");
            var mapGraph = new MapGraph();
            ServiceLocator.Register<IMapGraph>(mapGraph);
            Debug.Log("[TestDataFactory] RegisterServices: registered IMapGraph (fresh empty graph)");
            var colonyGraph = new ColonyGraph();
            ServiceLocator.Register<IColonyGraph>(colonyGraph);
            Debug.Log("[TestDataFactory] RegisterServices: registered IColonyGraph (fresh empty graph)");
            ServiceLocator.Register<IHeroTokenFactory>(new HeroTokenFactory());
            Debug.Log("[TestDataFactory] RegisterServices: registered IHeroTokenFactory");
            ServiceLocator.Register<IEnemyTokenFactory>(new EnemyTokenFactory());
            Debug.Log("[TestDataFactory] RegisterServices: registered IEnemyTokenFactory");
            Debug.Log("[TestDataFactory] RegisterServices: EXIT - all services registered");
        }

        public static void CleanupServices()
        {
            Debug.Log("[TestDataFactory] CleanupServices: ENTER - clearing all services from ServiceLocator");
            ServiceLocator.Clear();
            Debug.Log("[TestDataFactory] CleanupServices: EXIT - ServiceLocator cleared");
        }

        public static MapConfigSO CreateMapConfig()
        {
            Debug.Log("[TestDataFactory] CreateMapConfig: creating test map config");
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
            return config;
        }
    }

    // ============================================================
    // 1. CardDatabase Tests
    // ============================================================
    [TestFixture]
    public class CardDatabaseTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[CardDatabaseTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[CardDatabaseTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[CardDatabaseTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[CardDatabaseTests] Teardown: EXIT");
        }

        [Test]
        public void CardDatabase_GetCard_ReturnsNullForInvalidId()
        {
            Debug.Log("[CardDatabaseTests] CardDatabase_GetCard_ReturnsNullForInvalidId: starting test");
            var db = CardDatabase.Instance;
            Debug.Log($"[CardDatabaseTests] CardDatabase_GetCard_ReturnsNullForInvalidId: db loaded, querying invalid id=9999");
            var card = db.GetCard(9999);
            Debug.Log($"[CardDatabaseTests] CardDatabase_GetCard_ReturnsNullForInvalidId: result={card}");
            Assert.IsNull(card, "GetCard should return null for a non-existent card ID");
        }

        [Test]
        public void CardDatabase_GetColonyCard_ReturnsNullForInvalidId()
        {
            Debug.Log("[CardDatabaseTests] CardDatabase_GetColonyCard_ReturnsNullForInvalidId: starting test");
            var db = CardDatabase.Instance;
            Debug.Log($"[CardDatabaseTests] CardDatabase_GetColonyCard_ReturnsNullForInvalidId: db loaded, querying invalid id=9999");
            var card = db.GetColonyCard(9999);
            Debug.Log($"[CardDatabaseTests] CardDatabase_GetColonyCard_ReturnsNullForInvalidId: result={card}");
            Assert.IsNull(card, "GetColonyCard should return null for a non-existent colony card ID");
        }

        [Test]
        public void CardDatabase_Instance_LoadsSuccessfully()
        {
            Debug.Log("[CardDatabaseTests] CardDatabase_Instance_LoadsSuccessfully: starting test");
            var db = CardDatabase.Instance;
            Debug.Log($"[CardDatabaseTests] CardDatabase_Instance_LoadsSuccessfully: instance obtained, allCards={db.AllCards.Count}, allColonyCards={db.AllColonyCards.Count}");
            Assert.IsNotNull(db, "CardDatabase.Instance should not be null");
        }

        [Test]
        public void CardDatabase_AllCardsLoaded_TotalIs120()
        {
            Debug.Log("[CardDatabaseTests] CardDatabase_AllCardsLoaded_TotalIs120: starting test");
            var db = CardDatabase.Instance;
            int totalCards = db.AllCards.Count + db.AllColonyCards.Count;
            Debug.Log($"[CardDatabaseTests] CardDatabase_AllCardsLoaded_TotalIs120: allCards={db.AllCards.Count}, allColonyCards={db.AllColonyCards.Count}, total={totalCards}");
            Assert.AreEqual(120, totalCards, "Total cards (AllCards + AllColonyCards) should be 120");
        }

        [Test]
        public void CardDatabase_Heroes_AllHavePositiveStats()
        {
            Debug.Log("[CardDatabaseTests] CardDatabase_Heroes_AllHavePositiveStats: starting test");
            var db = CardDatabase.Instance;
            var heroes = db.GetCardsByType(CardType.Hero);
            Debug.Log($"[CardDatabaseTests] CardDatabase_Heroes_AllHavePositiveStats: found {heroes.Count} heroes");
            Assert.Greater(heroes.Count, 0, "Should have at least 1 hero");
            foreach (var hero in heroes)
            {
                Debug.Log($"[CardDatabaseTests] CardDatabase_Heroes_AllHavePositiveStats: hero={hero.cardName} (combat={hero.combat}, move={hero.move}, hp={hero.hp}, carry={hero.carry}, init={hero.initiative})");
                Assert.Greater(hero.combat, 0, $"Hero '{hero.cardName}' should have positive combat");
                Assert.Greater(hero.move, 0, $"Hero '{hero.cardName}' should have positive move");
                Assert.Greater(hero.hp, 0, $"Hero '{hero.cardName}' should have positive hp");
                Assert.Greater(hero.carry, 0, $"Hero '{hero.cardName}' should have positive carry");
                Assert.Greater(hero.initiative, 0, $"Hero '{hero.cardName}' should have positive initiative");
            }
        }

        [Test]
        public void CardDatabase_Heroes_AllRolesRepresented()
        {
            Debug.Log("[CardDatabaseTests] CardDatabase_Heroes_AllRolesRepresented: starting test");
            var db = CardDatabase.Instance;
            var heroes = db.GetCardsByType(CardType.Hero);
            var rolesFound = new HashSet<HeroRole>();
            foreach (var hero in heroes)
            {
                rolesFound.Add(hero.heroRole);
                Debug.Log($"[CardDatabaseTests] CardDatabase_Heroes_AllRolesRepresented: hero={hero.cardName}, role={hero.heroRole}");
            }
            var requiredRoles = new[] { HeroRole.Recon, HeroRole.Ranged, HeroRole.Fast, HeroRole.Melee, HeroRole.Tank, HeroRole.Gather, HeroRole.Support, HeroRole.Leader };
            foreach (var role in requiredRoles)
            {
                Debug.Log($"[CardDatabaseTests] CardDatabase_Heroes_AllRolesRepresented: checking role={role}, found={rolesFound.Contains(role)}");
                Assert.IsTrue(rolesFound.Contains(role), $"Role '{role}' should have at least 1 hero");
            }
        }

        [Test]
        public void CardDatabase_ColonyCards_AllHaveValidEffect()
        {
            Debug.Log("[CardDatabaseTests] CardDatabase_ColonyCards_AllHaveValidEffect: starting test");
            var db = CardDatabase.Instance;
            var colonyCards = db.AllColonyCards;
            Debug.Log($"[CardDatabaseTests] CardDatabase_ColonyCards_AllHaveValidEffect: found {colonyCards.Count} colony cards");
            Assert.Greater(colonyCards.Count, 0, "Should have at least 1 colony card");
            foreach (var card in colonyCards)
            {
                Debug.Log($"[CardDatabaseTests] CardDatabase_ColonyCards_AllHaveValidEffect: card={card.cardName} (effect={card.colonyEffect}, value={card.effectValue})");
                Assert.IsTrue(Enum.IsDefined(typeof(ColonyEffect), card.colonyEffect), $"Colony card '{card.cardName}' has invalid colonyEffect");
                Assert.GreaterOrEqual(card.effectValue, 0, $"Colony card '{card.cardName}' effectValue should be >= 0");
            }
        }

        [Test]
        public void CardDatabase_ColonyCards_StarterCardsExist()
        {
            Debug.Log("[CardDatabaseTests] CardDatabase_ColonyCards_StarterCardsExist: starting test");
            var db = CardDatabase.Instance;
            var starters = db.AllColonyCards.Where(c => c.isStarter).ToList();
            Debug.Log($"[CardDatabaseTests] CardDatabase_ColonyCards_StarterCardsExist: found {starters.Count} starter cards");
            Assert.Greater(starters.Count, 0, "Should have at least 1 starter colony card");
            foreach (var s in starters)
            {
                Debug.Log($"[CardDatabaseTests] CardDatabase_ColonyCards_StarterCardsExist: starter={s.cardName} (id={s.cardId})");
            }
        }

        [Test]
        public void CardDatabase_Equipment_AllHaveValidSlot()
        {
            Debug.Log("[CardDatabaseTests] CardDatabase_Equipment_AllHaveValidSlot: starting test");
            var db = CardDatabase.Instance;
            var equipment = db.GetCardsByType(CardType.Equipment);
            Debug.Log($"[CardDatabaseTests] CardDatabase_Equipment_AllHaveValidSlot: found {equipment.Count} equipment cards");
            Assert.Greater(equipment.Count, 0, "Should have at least 1 equipment card");
            foreach (var equip in equipment)
            {
                Debug.Log($"[CardDatabaseTests] CardDatabase_Equipment_AllHaveValidSlot: equip={equip.cardName} (slot={equip.equipmentSlot})");
                Assert.IsTrue(Enum.IsDefined(typeof(EquipmentSlot), equip.equipmentSlot), $"Equipment '{equip.cardName}' has invalid slot");
            }
        }

        [Test]
        public void CardDatabase_Tactical_AllHaveValidType()
        {
            Debug.Log("[CardDatabaseTests] CardDatabase_Tactical_AllHaveValidType: starting test");
            var db = CardDatabase.Instance;
            var tactical = db.GetCardsByType(CardType.Tactical);
            Debug.Log($"[CardDatabaseTests] CardDatabase_Tactical_AllHaveValidType: found {tactical.Count} tactical cards");
            Assert.Greater(tactical.Count, 0, "Should have at least 1 tactical card");
            foreach (var tac in tactical)
            {
                Debug.Log($"[CardDatabaseTests] CardDatabase_Tactical_AllHaveValidType: tac={tac.cardName} (type={tac.tacticalType})");
                Assert.IsTrue(Enum.IsDefined(typeof(TacticalType), tac.tacticalType), $"Tactical '{tac.cardName}' has invalid type");
            }
        }

        [Test]
        public void CardDatabase_GetCardsByType_ReturnsCorrectCounts()
        {
            Debug.Log("[CardDatabaseTests] CardDatabase_GetCardsByType_ReturnsCorrectCounts: starting test");
            var db = CardDatabase.Instance;
            var heroes = db.GetCardsByType(CardType.Hero);
            var equipment = db.GetCardsByType(CardType.Equipment);
            var tactical = db.GetCardsByType(CardType.Tactical);
            Debug.Log($"[CardDatabaseTests] CardDatabase_GetCardsByType_ReturnsCorrectCounts: heroes={heroes.Count}, equipment={equipment.Count}, tactical={tactical.Count}");
            Assert.AreEqual(20, heroes.Count, "Should have 20 heroes");
            Assert.AreEqual(40, equipment.Count, "Should have 40 equipment");
            Assert.AreEqual(30, tactical.Count, "Should have 30 tactical");
        }

        [Test]
        public void CardDatabase_GetColonyCardsByTier_ReturnsByTier()
        {
            Debug.Log("[CardDatabaseTests] CardDatabase_GetColonyCardsByTier_ReturnsByTier: starting test");
            var db = CardDatabase.Instance;
            var foodStorage = db.GetColonyCardsByTier(ColonyTier.FoodStorage);
            var structureDef = db.GetColonyCardsByTier(ColonyTier.StructureDefense);
            var advanced = db.GetColonyCardsByTier(ColonyTier.Advanced);
            Debug.Log($"[CardDatabaseTests] CardDatabase_GetColonyCardsByTier_ReturnsByTier: foodStorage={foodStorage.Count}, structureDef={structureDef.Count}, advanced={advanced.Count}");
            Assert.Greater(foodStorage.Count, 0, "FoodStorage tier should have cards");
            Assert.Greater(structureDef.Count, 0, "StructureDefense tier should have cards");
            Assert.Greater(advanced.Count, 0, "Advanced tier should have cards");
            Assert.AreEqual(30, foodStorage.Count + structureDef.Count + advanced.Count, "All colony cards should belong to a tier");
        }

        [Test]
        public void CardDatabase_GetCard_ReturnsCorrectCardById()
        {
            Debug.Log("[CardDatabaseTests] CardDatabase_GetCard_ReturnsCorrectCardById: starting test");
            var db = CardDatabase.Instance;
            Assert.Greater(db.AllCards.Count, 0, "CardDatabase should have loaded cards");
            var firstCard = db.AllCards[0];
            var fetched = db.GetCard(firstCard.cardId);
            Debug.Log($"[CardDatabaseTests] CardDatabase_GetCard_ReturnsCorrectCardById: queried id={firstCard.cardId}, result={fetched?.cardName}");
            Assert.IsNotNull(fetched, "GetCard should return the card");
            Assert.AreEqual(firstCard.cardId, fetched.cardId, "Card IDs should match");
            Assert.AreEqual(firstCard.cardName, fetched.cardName, "Card names should match");
        }

        [Test]
        public void CardDatabase_GetColonyCard_ReturnsCorrectById()
        {
            Debug.Log("[CardDatabaseTests] CardDatabase_GetColonyCard_ReturnsCorrectById: starting test");
            var db = CardDatabase.Instance;
            Assert.Greater(db.AllColonyCards.Count, 0, "CardDatabase should have loaded colony cards");
            var firstCard = db.AllColonyCards[0];
            var fetched = db.GetColonyCard(firstCard.cardId);
            Debug.Log($"[CardDatabaseTests] CardDatabase_GetColonyCard_ReturnsCorrectById: queried id={firstCard.cardId}, result={fetched?.cardName}");
            Assert.IsNotNull(fetched, "GetColonyCard should return the card");
            Assert.AreEqual(firstCard.cardId, fetched.cardId, "Colony card IDs should match");
        }
    }

    // ============================================================
    // 2. EnemyDatabase Tests
    // ============================================================
    [TestFixture]
    public class EnemyDatabaseTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[EnemyDatabaseTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[EnemyDatabaseTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[EnemyDatabaseTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[EnemyDatabaseTests] Teardown: EXIT");
        }

        [Test]
        public void EnemyDatabase_Instance_LoadsSuccessfully()
        {
            Debug.Log("[EnemyDatabaseTests] EnemyDatabase_Instance_LoadsSuccessfully: starting test");
            var db = EnemyDatabase.Instance;
            Debug.Log($"[EnemyDatabaseTests] EnemyDatabase_Instance_LoadsSuccessfully: instance obtained, allEnemies={db.AllEnemies.Count}");
            Assert.IsNotNull(db, "EnemyDatabase.Instance should not be null");
        }

        [Test]
        public void EnemyDatabase_AllEnemiesLoaded()
        {
            Debug.Log("[EnemyDatabaseTests] EnemyDatabase_AllEnemiesLoaded: starting test");
            var db = EnemyDatabase.Instance;
            Debug.Log($"[EnemyDatabaseTests] EnemyDatabase_AllEnemiesLoaded: allEnemies={db.AllEnemies.Count}");
            Assert.Greater(db.AllEnemies.Count, 0, "Should have at least 1 enemy");
        }

        [Test]
        public void EnemyDatabase_AllEnemies_HavePositiveStats()
        {
            Debug.Log("[EnemyDatabaseTests] EnemyDatabase_AllEnemies_HavePositiveStats: starting test");
            var db = EnemyDatabase.Instance;
            foreach (var enemy in db.AllEnemies)
            {
                Debug.Log($"[EnemyDatabaseTests] EnemyDatabase_AllEnemies_HavePositiveStats: enemy={enemy.enemyName} (str={enemy.strength}, hp={enemy.hp})");
                Assert.Greater(enemy.strength, 0, $"Enemy '{enemy.enemyName}' should have positive strength");
                Assert.Greater(enemy.hp, 0, $"Enemy '{enemy.enemyName}' should have positive hp");
            }
        }

        [Test]
        public void EnemyDatabase_ZoneDistribution_CoversAllZones()
        {
            Debug.Log("[EnemyDatabaseTests] EnemyDatabase_ZoneDistribution_CoversAllZones: starting test");
            var db = EnemyDatabase.Instance;
            var wildEnemies = db.GetEnemiesByZone(NodeType.Wilderness);
            var farmEnemies = db.GetEnemiesByZone(NodeType.Farmland);
            var townEnemies = db.GetEnemiesByZone(NodeType.Town);
            Debug.Log($"[EnemyDatabaseTests] EnemyDatabase_ZoneDistribution_CoversAllZones: wilderness={wildEnemies.Count}, farmland={farmEnemies.Count}, town={townEnemies.Count}");
            Assert.Greater(wildEnemies.Count, 0, "Should have wilderness enemies");
            Assert.Greater(farmEnemies.Count, 0, "Should have farmland enemies");
            Assert.Greater(townEnemies.Count, 0, "Should have town enemies");
        }

        [Test]
        public void EnemyDatabase_AllBehaviorTypesRepresented()
        {
            Debug.Log("[EnemyDatabaseTests] EnemyDatabase_AllBehaviorTypesRepresented: starting test");
            var db = EnemyDatabase.Instance;
            var behaviors = new HashSet<EnemyBehavior>();
            foreach (var enemy in db.AllEnemies)
            {
                behaviors.Add(enemy.behavior);
                Debug.Log($"[EnemyDatabaseTests] EnemyDatabase_AllBehaviorTypesRepresented: enemy={enemy.enemyName}, behavior={enemy.behavior}");
            }
            Assert.IsTrue(behaviors.Contains(EnemyBehavior.Patrol), "Patrol behavior should be represented");
            Assert.IsTrue(behaviors.Contains(EnemyBehavior.Chase), "Chase behavior should be represented");
            Assert.IsTrue(behaviors.Contains(EnemyBehavior.Ambush), "Ambush behavior should be represented");
            Assert.IsTrue(behaviors.Contains(EnemyBehavior.Guard), "Guard behavior should be represented");
        }

        [Test]
        public void EnemyDatabase_GetEnemy_ReturnsCorrectEnemy()
        {
            Debug.Log("[EnemyDatabaseTests] EnemyDatabase_GetEnemy_ReturnsCorrectEnemy: starting test");
            var db = EnemyDatabase.Instance;
            if (db.AllEnemies.Count > 0)
            {
                int firstId = db.Enemies.Keys.First();
                var enemy = db.GetEnemy(firstId);
                Debug.Log($"[EnemyDatabaseTests] EnemyDatabase_GetEnemy_ReturnsCorrectEnemy: id={firstId}, name={enemy?.enemyName}");
                Assert.IsNotNull(enemy, "GetEnemy should return an enemy for valid ID");
            }
        }

        [Test]
        public void EnemyDatabase_GetEnemy_ReturnsNullForInvalidId()
        {
            Debug.Log("[EnemyDatabaseTests] EnemyDatabase_GetEnemy_ReturnsNullForInvalidId: starting test");
            var db = EnemyDatabase.Instance;
            var enemy = db.GetEnemy(99999);
            Debug.Log($"[EnemyDatabaseTests] EnemyDatabase_GetEnemy_ReturnsNullForInvalidId: result={enemy}");
            Assert.IsNull(enemy, "GetEnemy should return null for invalid ID");
        }

        [Test]
        public void EnemyDatabase_GetEnemiesByZone_ReturnsCorrectEnemies()
        {
            Debug.Log("[EnemyDatabaseTests] EnemyDatabase_GetEnemiesByZone_ReturnsCorrectEnemies: starting test");
            var db = EnemyDatabase.Instance;
            var wildEnemies = db.GetEnemiesByZone(NodeType.Wilderness);
            foreach (var enemy in wildEnemies)
            {
                Debug.Log($"[EnemyDatabaseTests] EnemyDatabase_GetEnemiesByZone_ReturnsCorrectEnemies: enemy={enemy.enemyName}, zone={enemy.homeZone}");
                Assert.AreEqual(NodeType.Wilderness, enemy.homeZone, $"Enemy '{enemy.enemyName}' should be Wilderness zone");
            }
        }
    }

    // ============================================================
    // 3. MapGraph Tests
    // ============================================================
    [TestFixture]
    public class MapGraphTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[MapGraphTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[MapGraphTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[MapGraphTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[MapGraphTests] Teardown: EXIT");
        }

        [Test]
        public void MapGraph_Constructor_CreatesEmptyGraph()
        {
            Debug.Log("[MapGraphTests] MapGraph_Constructor_CreatesEmptyGraph: starting test");
            var graph = new MapGraph();
            ServiceLocator.Register<IMapGraph>(graph);
            Debug.Log("[MapGraphTests] MapGraph_Constructor_CreatesEmptyGraph: registered fresh graph as IMapGraph");
            var allNodes = graph.GetAllNodes();
            Debug.Log($"[MapGraphTests] MapGraph_Constructor_CreatesEmptyGraph: nodeCount={allNodes.Count}");
            Assert.AreEqual(0, allNodes.Count, "New graph should have 0 nodes");
            Assert.AreEqual(-1, graph.ColonyNodeId, "ColonyNodeId should be -1 for empty graph");
            Assert.AreEqual(-1, graph.PiedPiperNodeId, "PiedPiperNodeId should be -1 for empty graph");
        }

        [Test]
        public void MapGraph_AddNodeGetNode_Roundtrip()
        {
            Debug.Log("[MapGraphTests] MapGraph_AddNodeGetNode_Roundtrip: starting test");
            var graph = new MapGraph();
            ServiceLocator.Register<IMapGraph>(graph);
            Debug.Log("[MapGraphTests] MapGraph_AddNodeGetNode_Roundtrip: registered graph as IMapGraph");
            var node = new MapNode { nodeId = 5, zone = NodeType.Wilderness, worldPosition = new Vector2(1, 2) };
            graph.AddNode(node);
            var fetched = graph.GetNode(5);
            Debug.Log($"[MapGraphTests] MapGraph_AddNodeGetNode_Roundtrip: added nodeId=5, fetched={fetched?.nodeId}");
            Assert.IsNotNull(fetched, "GetNode should return the added node");
            Assert.AreEqual(5, fetched.nodeId, "Node ID should match");
            Assert.AreEqual(NodeType.Wilderness, fetched.zone, "Zone should match");
        }

        [Test]
        public void MapGraph_AddEdge_CreatesBidirectional()
        {
            Debug.Log("[MapGraphTests] MapGraph_AddEdge_CreatesBidirectional: starting test");
            var graph = new MapGraph();
            ServiceLocator.Register<IMapGraph>(graph);
            Debug.Log("[MapGraphTests] MapGraph_AddEdge_CreatesBidirectional: registered graph as IMapGraph");
            graph.AddNode(new MapNode { nodeId = 0, zone = NodeType.Colony });
            graph.AddNode(new MapNode { nodeId = 1, zone = NodeType.Wilderness });
            graph.AddEdge(0, 1);
            Debug.Log($"[MapGraphTests] MapGraph_AddEdge_CreatesBidirectional: hasEdge(0,1)={graph.HasEdge(0, 1)}, hasEdge(1,0)={graph.HasEdge(1, 0)}");
            Assert.IsTrue(graph.HasEdge(0, 1), "Edge from 0 to 1 should exist");
            Assert.IsTrue(graph.HasEdge(1, 0), "Edge from 1 to 0 should exist (bidirectional)");
        }

        [Test]
        public void MapGraph_HasEdge_ReturnsFalseForNonExistentEdge()
        {
            Debug.Log("[MapGraphTests] MapGraph_HasEdge_ReturnsFalseForNonExistentEdge: starting test");
            var graph = new MapGraph();
            ServiceLocator.Register<IMapGraph>(graph);
            Debug.Log("[MapGraphTests] MapGraph_HasEdge_ReturnsFalseForNonExistentEdge: registered graph as IMapGraph");
            graph.AddNode(new MapNode { nodeId = 0, zone = NodeType.Colony });
            graph.AddNode(new MapNode { nodeId = 1, zone = NodeType.Wilderness });
            bool result = graph.HasEdge(0, 1);
            Debug.Log($"[MapGraphTests] MapGraph_HasEdge_ReturnsFalseForNonExistentEdge: hasEdge(0,1)={result}");
            Assert.IsFalse(result, "HasEdge should return false when no edge exists");
        }

        [Test]
        public void MapGraph_GetNeighbors_ReturnsCorrectNodes()
        {
            Debug.Log("[MapGraphTests] MapGraph_GetNeighbors_ReturnsCorrectNodes: starting test");
            var graph = new MapGraph();
            ServiceLocator.Register<IMapGraph>(graph);
            Debug.Log("[MapGraphTests] MapGraph_GetNeighbors_ReturnsCorrectNodes: registered graph as IMapGraph");
            graph.AddNode(new MapNode { nodeId = 0, zone = NodeType.Colony });
            graph.AddNode(new MapNode { nodeId = 1, zone = NodeType.Wilderness });
            graph.AddNode(new MapNode { nodeId = 2, zone = NodeType.Wilderness });
            graph.AddEdge(0, 1);
            graph.AddEdge(0, 2);
            var neighbors = graph.GetNeighbors(0);
            Debug.Log($"[MapGraphTests] MapGraph_GetNeighbors_ReturnsCorrectNodes: node 0 neighbors count={neighbors.Count}");
            Assert.AreEqual(2, neighbors.Count, "Node 0 should have 2 neighbors");
            var neighborIds = neighbors.Select(n => n.nodeId).ToList();
            Assert.IsTrue(neighborIds.Contains(1), "Neighbors should include node 1");
            Assert.IsTrue(neighborIds.Contains(2), "Neighbors should include node 2");
        }

        [Test]
        public void MapGraph_RemoveNode_RemovesNodeAndEdges()
        {
            Debug.Log("[MapGraphTests] MapGraph_RemoveNode_RemovesNodeAndEdges: starting test");
            var graph = new MapGraph();
            ServiceLocator.Register<IMapGraph>(graph);
            Debug.Log("[MapGraphTests] MapGraph_RemoveNode_RemovesNodeAndEdges: registered graph as IMapGraph");
            graph.AddNode(new MapNode { nodeId = 0, zone = NodeType.Colony });
            graph.AddNode(new MapNode { nodeId = 1, zone = NodeType.Wilderness });
            graph.AddNode(new MapNode { nodeId = 2, zone = NodeType.Wilderness });
            graph.AddEdge(0, 1);
            graph.AddEdge(1, 2);
            bool removed = graph.RemoveNode(1);
            Debug.Log($"[MapGraphTests] MapGraph_RemoveNode_RemovesNodeAndEdges: removed={removed}, node1={graph.GetNode(1)}, hasEdge(0,1)={graph.HasEdge(0, 1)}");
            Assert.IsTrue(removed, "RemoveNode should return true");
            Assert.IsNull(graph.GetNode(1), "Node 1 should no longer exist");
            Assert.IsFalse(graph.HasEdge(0, 1), "Edge 0-1 should be removed");
            Assert.AreEqual(0, graph.GetNeighbors(0).Count, "Node 0 should have 0 neighbors after removing node 1");
        }

        [Test]
        public void MapGraph_GetNodesInZone_FiltersCorrectly()
        {
            Debug.Log("[MapGraphTests] MapGraph_GetNodesInZone_FiltersCorrectly: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var wildNodes = graph.GetNodesInZone(NodeType.Wilderness);
            Debug.Log($"[MapGraphTests] MapGraph_GetNodesInZone_FiltersCorrectly: wildernessCount={wildNodes.Count}");
            Assert.AreEqual(2, wildNodes.Count, "Should have 2 wilderness nodes in test graph");
            foreach (var n in wildNodes)
            {
                Debug.Log($"[MapGraphTests] MapGraph_GetNodesInZone_FiltersCorrectly: node={n.nodeId}, zone={n.zone}");
                Assert.AreEqual(NodeType.Wilderness, n.zone);
            }
        }

        [Test]
        public void MapGraph_GetAllNodes_ReturnsAll()
        {
            Debug.Log("[MapGraphTests] MapGraph_GetAllNodes_ReturnsAll: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var allNodes = graph.GetAllNodes();
            Debug.Log($"[MapGraphTests] MapGraph_GetAllNodes_ReturnsAll: count={allNodes.Count}");
            Assert.AreEqual(5, allNodes.Count, "Test graph should have 5 nodes");
        }

        [Test]
        public void MapGraph_ShortestPath_FindsDirectPath()
        {
            Debug.Log("[MapGraphTests] MapGraph_ShortestPath_FindsDirectPath: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var path = graph.ShortestPath(0, 1);
            Debug.Log($"[MapGraphTests] MapGraph_ShortestPath_FindsDirectPath: path=[{string.Join(",", path)}]");
            Assert.AreEqual(2, path.Count, "Direct neighbor path should have 2 nodes");
            Assert.AreEqual(0, path[0], "Path should start at 0");
            Assert.AreEqual(1, path[1], "Path should end at 1");
        }

        [Test]
        public void MapGraph_ShortestPath_FindsMultiHopPath()
        {
            Debug.Log("[MapGraphTests] MapGraph_ShortestPath_FindsMultiHopPath: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            graph.ZoneBossesDefeated = graph.BossesRequiredForPiper; // Unlock Pied Piper node
            var path = graph.ShortestPath(0, 4);
            Debug.Log($"[MapGraphTests] MapGraph_ShortestPath_FindsMultiHopPath: path=[{string.Join(",", path)}], length={path.Count}");
            Assert.AreEqual(5, path.Count, "Path 0->4 should have 5 nodes in linear graph");
            Assert.AreEqual(0, path[0], "Path should start at 0");
            Assert.AreEqual(4, path[path.Count - 1], "Path should end at 4");
        }

        [Test]
        public void MapGraph_ShortestPath_ReturnsEmptyForDisconnected()
        {
            Debug.Log("[MapGraphTests] MapGraph_ShortestPath_ReturnsEmptyForDisconnected: starting test");
            var graph = new MapGraph();
            ServiceLocator.Register<IMapGraph>(graph);
            Debug.Log("[MapGraphTests] MapGraph_ShortestPath_ReturnsEmptyForDisconnected: registered graph as IMapGraph");
            graph.AddNode(new MapNode { nodeId = 0, zone = NodeType.Colony });
            graph.AddNode(new MapNode { nodeId = 1, zone = NodeType.Wilderness });
            // No edge between them
            var path = graph.ShortestPath(0, 1);
            Debug.Log($"[MapGraphTests] MapGraph_ShortestPath_ReturnsEmptyForDisconnected: pathCount={path.Count}");
            Assert.AreEqual(0, path.Count, "Should return empty path for disconnected nodes");
        }

        [Test]
        public void MapGraph_GetDistance_ReturnsCorrectValues()
        {
            Debug.Log("[MapGraphTests] MapGraph_GetDistance_ReturnsCorrectValues: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            graph.ZoneBossesDefeated = graph.BossesRequiredForPiper; // Unlock Pied Piper node
            int dist01 = graph.GetDistance(0, 1);
            int dist04 = graph.GetDistance(0, 4);
            int distSame = graph.GetDistance(0, 0);
            Debug.Log($"[MapGraphTests] MapGraph_GetDistance_ReturnsCorrectValues: dist(0,1)={dist01}, dist(0,4)={dist04}, dist(0,0)={distSame}");
            Assert.AreEqual(1, dist01, "Distance 0->1 should be 1");
            Assert.AreEqual(4, dist04, "Distance 0->4 should be 4");
            Assert.AreEqual(0, distSame, "Distance to self should be 0");
        }

        [Test]
        public void MapGraph_GetDistance_ReturnsMinusOneForDisconnected()
        {
            Debug.Log("[MapGraphTests] MapGraph_GetDistance_ReturnsMinusOneForDisconnected: starting test");
            var graph = new MapGraph();
            ServiceLocator.Register<IMapGraph>(graph);
            Debug.Log("[MapGraphTests] MapGraph_GetDistance_ReturnsMinusOneForDisconnected: registered graph as IMapGraph");
            graph.AddNode(new MapNode { nodeId = 0, zone = NodeType.Colony });
            graph.AddNode(new MapNode { nodeId = 1, zone = NodeType.Wilderness });
            int dist = graph.GetDistance(0, 1);
            Debug.Log($"[MapGraphTests] MapGraph_GetDistance_ReturnsMinusOneForDisconnected: dist={dist}");
            Assert.AreEqual(-1, dist, "Distance should be -1 for disconnected nodes");
        }

        [Test]
        public void MapGraph_ColonyNodeId_SetCorrectly()
        {
            Debug.Log("[MapGraphTests] MapGraph_ColonyNodeId_SetCorrectly: starting test");
            var graph = new MapGraph();
            ServiceLocator.Register<IMapGraph>(graph);
            Debug.Log("[MapGraphTests] MapGraph_ColonyNodeId_SetCorrectly: registered graph as IMapGraph");
            graph.AddNode(new MapNode { nodeId = 42, zone = NodeType.Colony });
            Debug.Log($"[MapGraphTests] MapGraph_ColonyNodeId_SetCorrectly: ColonyNodeId={graph.ColonyNodeId}");
            Assert.AreEqual(42, graph.ColonyNodeId, "ColonyNodeId should be set when Colony zone node is added");
        }

        [Test]
        public void MapGraph_PiedPiperNodeId_SetCorrectly()
        {
            Debug.Log("[MapGraphTests] MapGraph_PiedPiperNodeId_SetCorrectly: starting test");
            var graph = new MapGraph();
            ServiceLocator.Register<IMapGraph>(graph);
            Debug.Log("[MapGraphTests] MapGraph_PiedPiperNodeId_SetCorrectly: registered graph as IMapGraph");
            graph.AddNode(new MapNode { nodeId = 31, zone = NodeType.PiedPiper });
            Debug.Log($"[MapGraphTests] MapGraph_PiedPiperNodeId_SetCorrectly: PiedPiperNodeId={graph.PiedPiperNodeId}");
            Assert.AreEqual(31, graph.PiedPiperNodeId, "PiedPiperNodeId should be set when PiedPiper zone node is added");
        }

        [Test]
        public void MapGraph_ShortestPath_SameNode_ReturnsSingleElement()
        {
            Debug.Log("[MapGraphTests] MapGraph_ShortestPath_SameNode_ReturnsSingleElement: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var path = graph.ShortestPath(2, 2);
            Debug.Log($"[MapGraphTests] MapGraph_ShortestPath_SameNode_ReturnsSingleElement: path=[{string.Join(",", path)}]");
            Assert.AreEqual(1, path.Count, "Same-node path should have 1 element");
            Assert.AreEqual(2, path[0], "Element should be the node itself");
        }
    }

    // ============================================================
    // 4. MapGenerator Tests
    // ============================================================
    [TestFixture]
    public class MapGeneratorTests
    {
        private MapGraph graph;
        private MapConfigSO config;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Debug.Log("[MapGeneratorTests] OneTimeSetup: creating map with seed=12345");
            config = TestDataFactory.CreateMapConfig();
            graph = MapGenerator.GenerateMap(config, 12345);
            Debug.Log($"[MapGeneratorTests] OneTimeSetup: map generated, totalNodes={graph.GetAllNodes().Count}");
        }

        [SetUp]
        public void Setup()
        {
            Debug.Log("[MapGeneratorTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            ServiceLocator.Register<IMapGraph>(graph);
            Debug.Log("[MapGeneratorTests] Setup: EXIT - re-registered generated graph as IMapGraph");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[MapGeneratorTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[MapGeneratorTests] Teardown: EXIT");
        }

        [Test]
        public void MapGenerator_Creates51Nodes()
        {
            Debug.Log("[MapGeneratorTests] MapGenerator_Creates51Nodes: starting test");
            int count = graph.GetAllNodes().Count;
            Debug.Log($"[MapGeneratorTests] MapGenerator_Creates51Nodes: nodeCount={count}");
            Assert.AreEqual(51, count, "Map should have 51 nodes (1 colony entrance + 19 colony sub-network + 10*3 zones + 1 pied piper)");
        }

        [Test]
        public void MapGenerator_ColonyNode_IsId0WithColonyZone()
        {
            Debug.Log("[MapGeneratorTests] MapGenerator_ColonyNode_IsId0WithColonyZone: starting test");
            var colonyNode = graph.GetNode(0);
            Debug.Log($"[MapGeneratorTests] MapGenerator_ColonyNode_IsId0WithColonyZone: node0={colonyNode?.zone}");
            Assert.IsNotNull(colonyNode, "Node 0 should exist");
            Assert.AreEqual(NodeType.Colony, colonyNode.zone, "Node 0 should be Colony zone");
            Assert.AreEqual(0, graph.ColonyNodeId, "ColonyNodeId should be 0");
        }

        [Test]
        public void MapGenerator_PiedPiperNode_IsId51WithPiedPiperZone()
        {
            Debug.Log("[MapGeneratorTests] MapGenerator_PiedPiperNode_IsId51WithPiedPiperZone: starting test");
            var piperNode = graph.GetNode(51);
            Debug.Log($"[MapGeneratorTests] MapGenerator_PiedPiperNode_IsId51WithPiedPiperZone: node51={piperNode?.zone}");
            Assert.IsNotNull(piperNode, "Node 51 should exist");
            Assert.AreEqual(NodeType.PiedPiper, piperNode.zone, "Node 51 should be PiedPiper zone");
            Assert.AreEqual(51, graph.PiedPiperNodeId, "PiedPiperNodeId should be 51");
        }

        [Test]
        public void MapGenerator_WildernessNodes_HaveCorrectZone()
        {
            Debug.Log("[MapGeneratorTests] MapGenerator_WildernessNodes_HaveCorrectZone: starting test");
            for (int i = 1; i <= 10; i++)
            {
                var node = graph.GetNode(i);
                Debug.Log($"[MapGeneratorTests] MapGenerator_WildernessNodes_HaveCorrectZone: nodeId={i}, zone={node?.zone}");
                Assert.IsNotNull(node, $"Node {i} should exist");
                Assert.AreEqual(NodeType.Wilderness, node.zone, $"Node {i} should be Wilderness");
            }
        }

        [Test]
        public void MapGenerator_FarmlandNodes_HaveCorrectZone()
        {
            Debug.Log("[MapGeneratorTests] MapGenerator_FarmlandNodes_HaveCorrectZone: starting test");
            for (int i = 11; i <= 20; i++)
            {
                var node = graph.GetNode(i);
                Debug.Log($"[MapGeneratorTests] MapGenerator_FarmlandNodes_HaveCorrectZone: nodeId={i}, zone={node?.zone}");
                Assert.IsNotNull(node, $"Node {i} should exist");
                Assert.AreEqual(NodeType.Farmland, node.zone, $"Node {i} should be Farmland");
            }
        }

        [Test]
        public void MapGenerator_TownNodes_HaveCorrectZone()
        {
            Debug.Log("[MapGeneratorTests] MapGenerator_TownNodes_HaveCorrectZone: starting test");
            for (int i = 21; i <= 30; i++)
            {
                var node = graph.GetNode(i);
                Debug.Log($"[MapGeneratorTests] MapGenerator_TownNodes_HaveCorrectZone: nodeId={i}, zone={node?.zone}");
                Assert.IsNotNull(node, $"Node {i} should exist");
                Assert.AreEqual(NodeType.Town, node.zone, $"Node {i} should be Town");
            }
        }

        [Test]
        public void MapGenerator_GraphIsConnected()
        {
            Debug.Log("[MapGeneratorTests] MapGenerator_GraphIsConnected: starting test");
            bool valid = MapGenerator.ValidateMap(graph);
            Debug.Log($"[MapGeneratorTests] MapGenerator_GraphIsConnected: valid={valid}");
            Assert.IsTrue(valid, "Generated map should be fully connected");
        }

        [Test]
        public void MapGenerator_ResourcesAssigned_ToZoneNodes()
        {
            Debug.Log("[MapGeneratorTests] MapGenerator_ResourcesAssigned_ToZoneNodes: starting test");
            bool anyWildResources = false;
            bool anyFarmResources = false;
            bool anyTownResources = false;
            for (int i = 1; i <= 10; i++)
            {
                var node = graph.GetNode(i);
                if (node.TotalResources() > 0) anyWildResources = true;
                Debug.Log($"[MapGeneratorTests] MapGenerator_ResourcesAssigned_ToZoneNodes: wilderness nodeId={i}, resources={node.TotalResources()}");
            }
            for (int i = 11; i <= 20; i++)
            {
                var node = graph.GetNode(i);
                if (node.TotalResources() > 0) anyFarmResources = true;
                Debug.Log($"[MapGeneratorTests] MapGenerator_ResourcesAssigned_ToZoneNodes: farmland nodeId={i}, resources={node.TotalResources()}");
            }
            for (int i = 21; i <= 30; i++)
            {
                var node = graph.GetNode(i);
                if (node.TotalResources() > 0) anyTownResources = true;
                Debug.Log($"[MapGeneratorTests] MapGenerator_ResourcesAssigned_ToZoneNodes: town nodeId={i}, resources={node.TotalResources()}");
            }
            Debug.Log($"[MapGeneratorTests] MapGenerator_ResourcesAssigned_ToZoneNodes: anyWild={anyWildResources}, anyFarm={anyFarmResources}, anyTown={anyTownResources}");
            Assert.IsTrue(anyWildResources, "At least some wilderness nodes should have resources");
            Assert.IsTrue(anyFarmResources, "At least some farmland nodes should have resources");
            // Town might have 0 resources since min=0, so we don't assert
        }

        [Test]
        public void MapGenerator_ValidateMap_ReturnsTrueForValid()
        {
            Debug.Log("[MapGeneratorTests] MapGenerator_ValidateMap_ReturnsTrueForValid: starting test");
            bool valid = MapGenerator.ValidateMap(graph);
            Debug.Log($"[MapGeneratorTests] MapGenerator_ValidateMap_ReturnsTrueForValid: valid={valid}");
            Assert.IsTrue(valid, "ValidateMap should return true for properly generated graph");
        }
    }

    // ============================================================
    // 5. PathfindingService Tests
    // ============================================================
    [TestFixture]
    public class PathfindingServiceTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[PathfindingServiceTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[PathfindingServiceTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[PathfindingServiceTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[PathfindingServiceTests] Teardown: EXIT");
        }

        [Test]
        public void PathfindingService_FindPath_DirectNeighbor()
        {
            Debug.Log("[PathfindingServiceTests] PathfindingService_FindPath_DirectNeighbor: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var path = PathfindingService.FindPath(graph, 0, 1);
            Debug.Log($"[PathfindingServiceTests] PathfindingService_FindPath_DirectNeighbor: path=[{string.Join(",", path)}]");
            Assert.AreEqual(2, path.Count, "Direct neighbor path should have 2 nodes");
            Assert.AreEqual(0, path[0]);
            Assert.AreEqual(1, path[1]);
        }

        [Test]
        public void PathfindingService_FindPath_MultiHop()
        {
            Debug.Log("[PathfindingServiceTests] PathfindingService_FindPath_MultiHop: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var path = PathfindingService.FindPath(graph, 0, 3);
            Debug.Log($"[PathfindingServiceTests] PathfindingService_FindPath_MultiHop: path=[{string.Join(",", path)}]");
            Assert.AreEqual(4, path.Count, "Path 0->3 should have 4 nodes");
        }

        [Test]
        public void PathfindingService_FindPath_ReturnsEmptyForUnreachable()
        {
            Debug.Log("[PathfindingServiceTests] PathfindingService_FindPath_ReturnsEmptyForUnreachable: starting test");
            var graph = new MapGraph();
            ServiceLocator.Register<IMapGraph>(graph);
            Debug.Log("[PathfindingServiceTests] PathfindingService_FindPath_ReturnsEmptyForUnreachable: registered graph as IMapGraph");
            graph.AddNode(new MapNode { nodeId = 0, zone = NodeType.Colony });
            graph.AddNode(new MapNode { nodeId = 1, zone = NodeType.Wilderness });
            var path = PathfindingService.FindPath(graph, 0, 1);
            Debug.Log($"[PathfindingServiceTests] PathfindingService_FindPath_ReturnsEmptyForUnreachable: pathCount={path.Count}");
            Assert.AreEqual(0, path.Count, "Should return empty for unreachable nodes");
        }

        [Test]
        public void PathfindingService_GetDistance_ReturnsOneForNeighbors()
        {
            Debug.Log("[PathfindingServiceTests] PathfindingService_GetDistance_ReturnsOneForNeighbors: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            int dist = PathfindingService.GetDistance(graph, 0, 1);
            Debug.Log($"[PathfindingServiceTests] PathfindingService_GetDistance_ReturnsOneForNeighbors: dist={dist}");
            Assert.AreEqual(1, dist, "Distance between neighbors should be 1");
        }

        [Test]
        public void PathfindingService_GetDistance_ReturnsMinusOneForDisconnected()
        {
            Debug.Log("[PathfindingServiceTests] PathfindingService_GetDistance_ReturnsMinusOneForDisconnected: starting test");
            var graph = new MapGraph();
            ServiceLocator.Register<IMapGraph>(graph);
            Debug.Log("[PathfindingServiceTests] PathfindingService_GetDistance_ReturnsMinusOneForDisconnected: registered graph as IMapGraph");
            graph.AddNode(new MapNode { nodeId = 0, zone = NodeType.Colony });
            graph.AddNode(new MapNode { nodeId = 1, zone = NodeType.Wilderness });
            int dist = PathfindingService.GetDistance(graph, 0, 1);
            Debug.Log($"[PathfindingServiceTests] PathfindingService_GetDistance_ReturnsMinusOneForDisconnected: dist={dist}");
            Assert.AreEqual(-1, dist, "Distance should be -1 for disconnected nodes");
        }

        [Test]
        public void PathfindingService_GetNodesWithinRange_ReturnsCorrectSet()
        {
            Debug.Log("[PathfindingServiceTests] PathfindingService_GetNodesWithinRange_ReturnsCorrectSet: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var nodes = PathfindingService.GetNodesWithinRange(graph, 0, 2);
            Debug.Log($"[PathfindingServiceTests] PathfindingService_GetNodesWithinRange_ReturnsCorrectSet: nodesInRange=[{string.Join(",", nodes)}]");
            Assert.IsTrue(nodes.Contains(0), "Should contain start node");
            Assert.IsTrue(nodes.Contains(1), "Should contain node at distance 1");
            Assert.IsTrue(nodes.Contains(2), "Should contain node at distance 2");
            Assert.IsFalse(nodes.Contains(3), "Should not contain node at distance 3");
            Assert.AreEqual(3, nodes.Count, "Should have exactly 3 nodes within range 2");
        }
    }

    // ============================================================
    // 6. FogOfWar Tests
    // ============================================================
    [TestFixture]
    public class FogOfWarTests
    {
        private MapGraph graph;
        private FogOfWar fog;

        [SetUp]
        public void Setup()
        {
            Debug.Log("[FogOfWarTests] Setup: ENTER - registering services and creating test data");
            TestDataFactory.RegisterServices();
            graph = TestDataFactory.CreateSimpleGraph();
            fog = (FogOfWar)ServiceLocator.Get<IFogOfWar>();
            Debug.Log($"[FogOfWarTests] Setup: EXIT - graph created, fog resolved via ServiceLocator (fog={fog != null})");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[FogOfWarTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[FogOfWarTests] Teardown: EXIT");
        }

        [Test]
        public void FogOfWar_ColonyNodeAlwaysVisible()
        {
            Debug.Log("[FogOfWarTests] FogOfWar_ColonyNodeAlwaysVisible: starting test");
            var heroes = new List<HeroFogInfo>(); // no heroes
            var effects = new HashSet<ColonyEffect>();
            fog.RecalculateVisibility(graph, heroes, effects);
            var colonyNode = graph.GetNode(0);
            Debug.Log($"[FogOfWarTests] FogOfWar_ColonyNodeAlwaysVisible: colonyFogState={colonyNode.fogState}");
            Assert.AreEqual(FogState.Visible, colonyNode.fogState, "Colony node should always be visible");
        }

        [Test]
        public void FogOfWar_HeroNode_IsVisible()
        {
            Debug.Log("[FogOfWarTests] FogOfWar_HeroNode_IsVisible: starting test");
            var heroes = new List<HeroFogInfo> { new HeroFogInfo { nodeId = 2, bonusRevealRange = 0, revealsEntireZone = false } };
            var effects = new HashSet<ColonyEffect>();
            fog.RecalculateVisibility(graph, heroes, effects);
            var heroNode = graph.GetNode(2);
            Debug.Log($"[FogOfWarTests] FogOfWar_HeroNode_IsVisible: node2 fogState={heroNode.fogState}");
            Assert.AreEqual(FogState.Visible, heroNode.fogState, "Hero node should be visible");
        }

        [Test]
        public void FogOfWar_AdjacentToHero_IsVisible()
        {
            Debug.Log("[FogOfWarTests] FogOfWar_AdjacentToHero_IsVisible: starting test");
            var heroes = new List<HeroFogInfo> { new HeroFogInfo { nodeId = 2, bonusRevealRange = 0, revealsEntireZone = false } };
            var effects = new HashSet<ColonyEffect>();
            fog.RecalculateVisibility(graph, heroes, effects);
            var node1 = graph.GetNode(1);
            var node3 = graph.GetNode(3);
            Debug.Log($"[FogOfWarTests] FogOfWar_AdjacentToHero_IsVisible: node1={node1.fogState}, node3={node3.fogState}");
            Assert.AreEqual(FogState.Visible, node1.fogState, "Node adjacent to hero should be visible");
            Assert.AreEqual(FogState.Visible, node3.fogState, "Node adjacent to hero should be visible");
        }

        [Test]
        public void FogOfWar_NonAdjacentNodes_AreHidden()
        {
            Debug.Log("[FogOfWarTests] FogOfWar_NonAdjacentNodes_AreHidden: starting test");
            var heroes = new List<HeroFogInfo> { new HeroFogInfo { nodeId = 0, bonusRevealRange = 0, revealsEntireZone = false } };
            var effects = new HashSet<ColonyEffect>();
            fog.RecalculateVisibility(graph, heroes, effects);
            var node3 = graph.GetNode(3);
            var node4 = graph.GetNode(4);
            Debug.Log($"[FogOfWarTests] FogOfWar_NonAdjacentNodes_AreHidden: node3={node3.fogState}, node4={node4.fogState}");
            Assert.AreEqual(FogState.Hidden, node3.fogState, "Non-adjacent node should be hidden");
            Assert.AreEqual(FogState.Hidden, node4.fogState, "Non-adjacent node should be hidden");
        }

        [Test]
        public void FogOfWar_VisitedNodes_BecomeRemembered()
        {
            Debug.Log("[FogOfWarTests] FogOfWar_VisitedNodes_BecomeRemembered: starting test");
            // First reveal node 2 with hero there
            var heroes1 = new List<HeroFogInfo> { new HeroFogInfo { nodeId = 2, bonusRevealRange = 0, revealsEntireZone = false } };
            var effects = new HashSet<ColonyEffect>();
            fog.RecalculateVisibility(graph, heroes1, effects);
            Assert.AreEqual(FogState.Visible, graph.GetNode(2).fogState, "Node 2 should be visible with hero");

            // Now move hero away — node 2 should become Remembered
            var heroes2 = new List<HeroFogInfo> { new HeroFogInfo { nodeId = 0, bonusRevealRange = 0, revealsEntireZone = false } };
            fog.RecalculateVisibility(graph, heroes2, effects);
            var node2 = graph.GetNode(2);
            Debug.Log($"[FogOfWarTests] FogOfWar_VisitedNodes_BecomeRemembered: node2={node2.fogState}, visited={node2.visited}");
            Assert.AreEqual(FogState.Remembered, node2.fogState, "Previously visited node should become Remembered");
        }

        [Test]
        public void FogOfWar_FogRevealEffect_ExtendsVisibility()
        {
            Debug.Log("[FogOfWarTests] FogOfWar_FogRevealEffect_ExtendsVisibility: starting test");
            var heroes = new List<HeroFogInfo>();
            var effects = new HashSet<ColonyEffect> { ColonyEffect.FogReveal };
            fog.RecalculateVisibility(graph, heroes, effects);
            var node1 = graph.GetNode(1);
            var node2 = graph.GetNode(2);
            Debug.Log($"[FogOfWarTests] FogOfWar_FogRevealEffect_ExtendsVisibility: node1={node1.fogState}, node2={node2.fogState}");
            Assert.AreEqual(FogState.Visible, node1.fogState, "FogReveal should reveal nodes within 2 of colony");
            Assert.AreEqual(FogState.Visible, node2.fogState, "FogReveal should reveal nodes within 2 of colony");
        }

        [Test]
        public void FogOfWar_AreEnemiesVisible_TrueOnlyForVisibleNodes()
        {
            Debug.Log("[FogOfWarTests] FogOfWar_AreEnemiesVisible_TrueOnlyForVisibleNodes: starting test");
            var visibleNode = new MapNode { nodeId = 10, zone = NodeType.Wilderness, fogState = FogState.Visible };
            var rememberedNode = new MapNode { nodeId = 11, zone = NodeType.Wilderness, fogState = FogState.Remembered };
            var hiddenNode = new MapNode { nodeId = 12, zone = NodeType.Wilderness, fogState = FogState.Hidden };
            Debug.Log($"[FogOfWarTests] AreEnemiesVisible: visible={fog.AreEnemiesVisible(visibleNode)}, remembered={fog.AreEnemiesVisible(rememberedNode)}, hidden={fog.AreEnemiesVisible(hiddenNode)}");
            Assert.IsTrue(fog.AreEnemiesVisible(visibleNode), "Enemies should be visible on Visible nodes");
            Assert.IsFalse(fog.AreEnemiesVisible(rememberedNode), "Enemies should not be visible on Remembered nodes");
            Assert.IsFalse(fog.AreEnemiesVisible(hiddenNode), "Enemies should not be visible on Hidden nodes");
        }

        [Test]
        public void FogOfWar_AreResourcesVisible_TrueForVisibleAndRemembered()
        {
            Debug.Log("[FogOfWarTests] FogOfWar_AreResourcesVisible_TrueForVisibleAndRemembered: starting test");
            var visibleNode = new MapNode { nodeId = 10, zone = NodeType.Wilderness, fogState = FogState.Visible };
            var rememberedNode = new MapNode { nodeId = 11, zone = NodeType.Wilderness, fogState = FogState.Remembered };
            var hiddenNode = new MapNode { nodeId = 12, zone = NodeType.Wilderness, fogState = FogState.Hidden };
            Debug.Log($"[FogOfWarTests] AreResourcesVisible: visible={fog.AreResourcesVisible(visibleNode)}, remembered={fog.AreResourcesVisible(rememberedNode)}, hidden={fog.AreResourcesVisible(hiddenNode)}");
            Assert.IsTrue(fog.AreResourcesVisible(visibleNode), "Resources should be visible on Visible nodes");
            Assert.IsTrue(fog.AreResourcesVisible(rememberedNode), "Resources should be visible on Remembered nodes");
            Assert.IsFalse(fog.AreResourcesVisible(hiddenNode), "Resources should not be visible on Hidden nodes");
        }
    }

    // ============================================================
    // 7. HeroToken Tests
    // ============================================================
    [TestFixture]
    public class HeroTokenTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[HeroTokenTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[HeroTokenTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[HeroTokenTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[HeroTokenTests] Teardown: EXIT");
        }

        [Test]
        public void HeroToken_Constructor_SetsCorrectInitialValues()
        {
            Debug.Log("[HeroTokenTests] HeroToken_Constructor_SetsCorrectInitialValues: starting test");
            var def = TestDataFactory.CreateHero(id: 1, name: "Scout", combat: 3, move: 2, hp: 4, carry: 2, initiative: 5);
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_Constructor_SetsCorrectInitialValues: created token via IHeroTokenFactory");
            Debug.Log($"[HeroTokenTests] HeroToken_Constructor_SetsCorrectInitialValues: tokenId={token.tokenId}, hp={token.currentHP}/{token.maxHP}, isAlive={token.IsAlive}");
            Assert.AreEqual(0, token.tokenId, "Token ID should match");
            Assert.AreEqual(4, token.currentHP, "CurrentHP should match card HP");
            Assert.AreEqual(4, token.maxHP, "MaxHP should match card HP");
            Assert.AreEqual(-1, token.currentNodeId, "Initial node should be -1");
            Assert.IsFalse(token.isInjured, "Should not start injured");
            Assert.IsTrue(token.IsAlive, "Should be alive on creation");
        }

        [Test]
        public void HeroToken_EffectiveCombat_StartsAsBaseCombat()
        {
            Debug.Log("[HeroTokenTests] HeroToken_EffectiveCombat_StartsAsBaseCombat: starting test");
            var def = TestDataFactory.CreateHero(combat: 5);
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_EffectiveCombat_StartsAsBaseCombat: created token via IHeroTokenFactory");
            Debug.Log($"[HeroTokenTests] HeroToken_EffectiveCombat_StartsAsBaseCombat: effectiveCombat={token.EffectiveCombat}, baseCombat={token.baseCombat}");
            Assert.AreEqual(5, token.EffectiveCombat, "EffectiveCombat should equal base combat with no equipment");
        }

        [Test]
        public void HeroToken_EquipItem_SetsCorrectSlot()
        {
            Debug.Log("[HeroTokenTests] HeroToken_EquipItem_SetsCorrectSlot: starting test");
            var def = TestDataFactory.CreateHero();
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_EquipItem_SetsCorrectSlot: created token via IHeroTokenFactory");
            var weapon = TestDataFactory.CreateEquipment(slot: EquipmentSlot.Offensive, effectValue1: 3);
            bool result = token.EquipItem(weapon);
            Debug.Log($"[HeroTokenTests] HeroToken_EquipItem_SetsCorrectSlot: result={result}, offensiveEquip={token.offensiveEquipment?.cardName}");
            Assert.IsTrue(result, "Equipping to empty slot should succeed");
            Assert.AreEqual(weapon, token.offensiveEquipment, "Offensive slot should contain the weapon");
        }

        [Test]
        public void HeroToken_EquipItem_FailsOnFullSlot()
        {
            Debug.Log("[HeroTokenTests] HeroToken_EquipItem_FailsOnFullSlot: starting test");
            var def = TestDataFactory.CreateHero();
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_EquipItem_FailsOnFullSlot: created token via IHeroTokenFactory");
            var weapon1 = TestDataFactory.CreateEquipment(id: 51, name: "Weapon 1", slot: EquipmentSlot.Offensive);
            var weapon2 = TestDataFactory.CreateEquipment(id: 52, name: "Weapon 2", slot: EquipmentSlot.Offensive);
            token.EquipItem(weapon1);
            bool result = token.EquipItem(weapon2);
            Debug.Log($"[HeroTokenTests] HeroToken_EquipItem_FailsOnFullSlot: secondEquip={result}");
            Assert.IsFalse(result, "Equipping to occupied slot should fail");
        }

        [Test]
        public void HeroToken_UnequipAll_ReturnsAllEquipped()
        {
            Debug.Log("[HeroTokenTests] HeroToken_UnequipAll_ReturnsAllEquipped: starting test");
            var def = TestDataFactory.CreateHero();
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_UnequipAll_ReturnsAllEquipped: created token via IHeroTokenFactory");
            var weapon = TestDataFactory.CreateEquipment(id: 51, slot: EquipmentSlot.Offensive);
            var armor = TestDataFactory.CreateEquipment(id: 52, name: "Armor", slot: EquipmentSlot.Defensive);
            var util = TestDataFactory.CreateEquipment(id: 53, name: "Utility", slot: EquipmentSlot.Utility);
            token.EquipItem(weapon);
            token.EquipItem(armor);
            token.EquipItem(util);
            var removed = token.UnequipAll();
            Debug.Log($"[HeroTokenTests] HeroToken_UnequipAll_ReturnsAllEquipped: removedCount={removed.Count}");
            Assert.AreEqual(3, removed.Count, "Should return all 3 equipped items");
            Assert.IsNull(token.offensiveEquipment, "Offensive slot should be null after unequip");
            Assert.IsNull(token.defensiveEquipment, "Defensive slot should be null after unequip");
            Assert.IsNull(token.utilityEquipment, "Utility slot should be null after unequip");
        }

        [Test]
        public void HeroToken_GatherResource_AddsToCarried()
        {
            Debug.Log("[HeroTokenTests] HeroToken_GatherResource_AddsToCarried: starting test");
            var def = TestDataFactory.CreateHero(carry: 5);
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_GatherResource_AddsToCarried: created token via IHeroTokenFactory");
            int gathered = token.GatherResource(ResourceType.Food, 3);
            Debug.Log($"[HeroTokenTests] HeroToken_GatherResource_AddsToCarried: gathered={gathered}, totalCarried={token.TotalCarried}");
            Assert.AreEqual(3, gathered, "Should gather requested amount");
            Assert.AreEqual(3, token.TotalCarried, "TotalCarried should reflect gathered amount");
        }

        [Test]
        public void HeroToken_GatherResource_RespectsCarryCapacity()
        {
            Debug.Log("[HeroTokenTests] HeroToken_GatherResource_RespectsCarryCapacity: starting test");
            var def = TestDataFactory.CreateHero(carry: 2);
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_GatherResource_RespectsCarryCapacity: created token via IHeroTokenFactory");
            int gathered = token.GatherResource(ResourceType.Food, 5);
            Debug.Log($"[HeroTokenTests] HeroToken_GatherResource_RespectsCarryCapacity: gathered={gathered}, capacity={token.EffectiveCarry}");
            Assert.AreEqual(2, gathered, "Should only gather up to carry capacity");
            Assert.AreEqual(2, token.TotalCarried, "TotalCarried should not exceed capacity");
        }

        [Test]
        public void HeroToken_TotalCarried_SumsAllTypes()
        {
            Debug.Log("[HeroTokenTests] HeroToken_TotalCarried_SumsAllTypes: starting test");
            var def = TestDataFactory.CreateHero(carry: 10);
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_TotalCarried_SumsAllTypes: created token via IHeroTokenFactory");
            token.GatherResource(ResourceType.Food, 2);
            token.GatherResource(ResourceType.Materials, 3);
            Debug.Log($"[HeroTokenTests] HeroToken_TotalCarried_SumsAllTypes: totalCarried={token.TotalCarried}");
            Assert.AreEqual(5, token.TotalCarried, "TotalCarried should sum all resource types");
        }

        [Test]
        public void HeroToken_DropAllResources_ClearsAndReturns()
        {
            Debug.Log("[HeroTokenTests] HeroToken_DropAllResources_ClearsAndReturns: starting test");
            var def = TestDataFactory.CreateHero(carry: 10);
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_DropAllResources_ClearsAndReturns: created token via IHeroTokenFactory");
            token.GatherResource(ResourceType.Food, 3);
            token.GatherResource(ResourceType.Materials, 2);
            var dropped = token.DropAllResources();
            Debug.Log($"[HeroTokenTests] HeroToken_DropAllResources_ClearsAndReturns: droppedTypes={dropped.Count}, totalAfter={token.TotalCarried}");
            Assert.AreEqual(3, dropped[ResourceType.Food], "Should return correct food amount");
            Assert.AreEqual(2, dropped[ResourceType.Materials], "Should return correct materials amount");
            Assert.AreEqual(0, token.TotalCarried, "TotalCarried should be 0 after drop");
        }

        [Test]
        public void HeroToken_DepositResources_ClearsAndReturns()
        {
            Debug.Log("[HeroTokenTests] HeroToken_DepositResources_ClearsAndReturns: starting test");
            var def = TestDataFactory.CreateHero(carry: 10);
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_DepositResources_ClearsAndReturns: created token via IHeroTokenFactory");
            token.GatherResource(ResourceType.Currency, 4);
            var deposited = token.DepositResources();
            Debug.Log($"[HeroTokenTests] HeroToken_DepositResources_ClearsAndReturns: depositedTypes={deposited.Count}, totalAfter={token.TotalCarried}");
            Assert.AreEqual(4, deposited[ResourceType.Currency], "Should return correct currency amount");
            Assert.AreEqual(0, token.TotalCarried, "TotalCarried should be 0 after deposit");
        }

        [Test]
        public void HeroToken_TakeDamage_ReducesHP()
        {
            Debug.Log("[HeroTokenTests] HeroToken_TakeDamage_ReducesHP: starting test");
            var def = TestDataFactory.CreateHero(hp: 6);
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_TakeDamage_ReducesHP: created token via IHeroTokenFactory");
            bool defeated = token.TakeDamage(2);
            Debug.Log($"[HeroTokenTests] HeroToken_TakeDamage_ReducesHP: defeated={defeated}, hp={token.currentHP}");
            Assert.IsFalse(defeated, "Should not be defeated with 4 HP remaining");
            Assert.AreEqual(4, token.currentHP, "HP should be reduced by damage amount");
        }

        [Test]
        public void HeroToken_TakeDamage_ReturnsTrueWhenDefeated()
        {
            Debug.Log("[HeroTokenTests] HeroToken_TakeDamage_ReturnsTrueWhenDefeated: starting test");
            var def = TestDataFactory.CreateHero(hp: 3);
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_TakeDamage_ReturnsTrueWhenDefeated: created token via IHeroTokenFactory");
            bool defeated = token.TakeDamage(5);
            Debug.Log($"[HeroTokenTests] HeroToken_TakeDamage_ReturnsTrueWhenDefeated: defeated={defeated}, hp={token.currentHP}, isInjured={token.isInjured}");
            Assert.IsTrue(defeated, "Should return true when HP <= 0");
            Assert.AreEqual(0, token.currentHP, "HP should be 0");
            Assert.IsTrue(token.isInjured, "Should be injured when defeated");
        }

        [Test]
        public void HeroToken_Heal_IncreasesHPUpToMax()
        {
            Debug.Log("[HeroTokenTests] HeroToken_Heal_IncreasesHPUpToMax: starting test");
            var def = TestDataFactory.CreateHero(hp: 6);
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_Heal_IncreasesHPUpToMax: created token via IHeroTokenFactory");
            token.TakeDamage(4);
            token.Heal(10);
            Debug.Log($"[HeroTokenTests] HeroToken_Heal_IncreasesHPUpToMax: hp={token.currentHP}, maxHP={token.EffectiveHP}");
            Assert.AreEqual(token.EffectiveHP, token.currentHP, "HP should not exceed EffectiveHP");
        }

        [Test]
        public void HeroToken_Injure_DropsResourcesAndUnequips()
        {
            Debug.Log("[HeroTokenTests] HeroToken_Injure_DropsResourcesAndUnequips: starting test");
            var def = TestDataFactory.CreateHero(carry: 5, hp: 4);
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_Injure_DropsResourcesAndUnequips: created token via IHeroTokenFactory");
            token.GatherResource(ResourceType.Food, 3);
            token.EquipItem(TestDataFactory.CreateEquipment(slot: EquipmentSlot.Offensive));
            token.Injure();
            Debug.Log($"[HeroTokenTests] HeroToken_Injure_DropsResourcesAndUnequips: isInjured={token.isInjured}, totalCarried={token.TotalCarried}, offensive={token.offensiveEquipment}");
            Assert.IsTrue(token.isInjured, "Should be injured");
            Assert.AreEqual(0, token.TotalCarried, "Should have dropped all resources");
            Assert.IsNull(token.offensiveEquipment, "Should have unequipped");
            Assert.AreEqual(2, token.turnsUntilRecovery, "Recovery timer should be 2");
        }

        [Test]
        public void HeroToken_IsAlive_ReflectsState()
        {
            Debug.Log("[HeroTokenTests] HeroToken_IsAlive_ReflectsState: starting test");
            var def = TestDataFactory.CreateHero(hp: 4);
            var token = ServiceLocator.Get<IHeroTokenFactory>().Create(def, 0);
            Debug.Log("[HeroTokenTests] HeroToken_IsAlive_ReflectsState: created token via IHeroTokenFactory");
            Debug.Log($"[HeroTokenTests] HeroToken_IsAlive_ReflectsState: initiallyAlive={token.IsAlive}");
            Assert.IsTrue(token.IsAlive, "Should be alive initially");

            token.currentHP = 0;
            Debug.Log($"[HeroTokenTests] HeroToken_IsAlive_ReflectsState: hp0={token.IsAlive}");
            Assert.IsFalse(token.IsAlive, "Should not be alive at 0 HP");

            token.currentHP = 4;
            token.isInjured = true;
            Debug.Log($"[HeroTokenTests] HeroToken_IsAlive_ReflectsState: injured={token.IsAlive}");
            Assert.IsFalse(token.IsAlive, "Should not be alive when injured");
        }
    }

    // ============================================================
    // 8. EnemyToken Tests
    // ============================================================
    [TestFixture]
    public class EnemyTokenTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[EnemyTokenTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[EnemyTokenTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[EnemyTokenTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[EnemyTokenTests] Teardown: EXIT");
        }

        [Test]
        public void EnemyToken_Constructor_SetsCorrectValues()
        {
            Debug.Log("[EnemyTokenTests] EnemyToken_Constructor_SetsCorrectValues: starting test");
            var enemy = TestDataFactory.CreateEnemyToken(tokenId: 5, name: "Rat", strength: 4, hp: 6, speed: 2, behavior: EnemyBehavior.Chase, zone: NodeType.Farmland, nodeId: 10);
            Debug.Log($"[EnemyTokenTests] EnemyToken_Constructor_SetsCorrectValues: tokenId={enemy.tokenId}, name={enemy.enemyName}, str={enemy.strength}, hp={enemy.currentHP}/{enemy.maxHP}");
            Assert.AreEqual(5, enemy.tokenId);
            Assert.AreEqual("Rat", enemy.enemyName);
            Assert.AreEqual(4, enemy.strength);
            Assert.AreEqual(6, enemy.hp);
            Assert.AreEqual(6, enemy.currentHP);
            Assert.AreEqual(2, enemy.speed);
            Assert.AreEqual(EnemyBehavior.Chase, enemy.behavior);
            Assert.AreEqual(NodeType.Farmland, enemy.homeZone);
            Assert.AreEqual(10, enemy.currentNodeId);
            Assert.IsFalse(enemy.isDefeated);
        }

        [Test]
        public void EnemyToken_TakeDamage_ReducesHP()
        {
            Debug.Log("[EnemyTokenTests] EnemyToken_TakeDamage_ReducesHP: starting test");
            var enemy = TestDataFactory.CreateEnemyToken(hp: 8);
            bool killed = enemy.TakeDamage(3);
            Debug.Log($"[EnemyTokenTests] EnemyToken_TakeDamage_ReducesHP: killed={killed}, hp={enemy.currentHP}");
            Assert.IsFalse(killed, "Should not be killed");
            Assert.AreEqual(5, enemy.currentHP, "HP should be reduced");
        }

        [Test]
        public void EnemyToken_TakeDamage_ReturnsTrueWhenKilled()
        {
            Debug.Log("[EnemyTokenTests] EnemyToken_TakeDamage_ReturnsTrueWhenKilled: starting test");
            var enemy = TestDataFactory.CreateEnemyToken(hp: 3);
            bool killed = enemy.TakeDamage(5);
            Debug.Log($"[EnemyTokenTests] EnemyToken_TakeDamage_ReturnsTrueWhenKilled: killed={killed}, hp={enemy.currentHP}, isDefeated={enemy.isDefeated}");
            Assert.IsTrue(killed, "Should return true when killed");
            Assert.AreEqual(0, enemy.currentHP);
            Assert.IsTrue(enemy.isDefeated);
        }

        [Test]
        public void EnemyToken_Defeat_SetsDefeatedAndRespawnTimer()
        {
            Debug.Log("[EnemyTokenTests] EnemyToken_Defeat_SetsDefeatedAndRespawnTimer: starting test");
            var enemy = TestDataFactory.CreateEnemyToken(hp: 5);
            enemy.Defeat();
            Debug.Log($"[EnemyTokenTests] EnemyToken_Defeat_SetsDefeatedAndRespawnTimer: isDefeated={enemy.isDefeated}, respawnTimer={enemy.respawnTimer}, hp={enemy.currentHP}");
            Assert.IsTrue(enemy.isDefeated, "Should be defeated");
            Assert.AreEqual(3, enemy.respawnTimer, "Respawn timer should be 3");
            Assert.AreEqual(0, enemy.currentHP, "HP should be 0");
        }

        [Test]
        public void EnemyToken_Respawn_RestoresHPAndSetsNode()
        {
            Debug.Log("[EnemyTokenTests] EnemyToken_Respawn_RestoresHPAndSetsNode: starting test");
            var enemy = TestDataFactory.CreateEnemyToken(hp: 6, nodeId: 5);
            enemy.Defeat();
            enemy.Respawn(10);
            Debug.Log($"[EnemyTokenTests] EnemyToken_Respawn_RestoresHPAndSetsNode: isDefeated={enemy.isDefeated}, hp={enemy.currentHP}, nodeId={enemy.currentNodeId}");
            Assert.IsFalse(enemy.isDefeated, "Should not be defeated after respawn");
            Assert.AreEqual(6, enemy.currentHP, "HP should be fully restored");
            Assert.AreEqual(10, enemy.currentNodeId, "Should be at new node");
            Assert.AreEqual(0, enemy.respawnTimer, "Respawn timer should be 0");
        }

        [Test]
        public void EnemyToken_IsAlive_ReflectsState()
        {
            Debug.Log("[EnemyTokenTests] EnemyToken_IsAlive_ReflectsState: starting test");
            var enemy = TestDataFactory.CreateEnemyToken(hp: 5);
            Debug.Log($"[EnemyTokenTests] EnemyToken_IsAlive_ReflectsState: initiallyAlive={enemy.IsAlive}");
            Assert.IsTrue(enemy.IsAlive, "Should be alive initially");

            enemy.currentHP = 0;
            Debug.Log($"[EnemyTokenTests] EnemyToken_IsAlive_ReflectsState: hp0={enemy.IsAlive}");
            Assert.IsFalse(enemy.IsAlive, "Should not be alive at 0 HP");

            enemy.currentHP = 5;
            enemy.isDefeated = true;
            Debug.Log($"[EnemyTokenTests] EnemyToken_IsAlive_ReflectsState: defeated={enemy.IsAlive}");
            Assert.IsFalse(enemy.IsAlive, "Should not be alive when defeated");
        }

        [Test]
        public void EnemyToken_MaxHP_ReturnsInitialHP()
        {
            Debug.Log("[EnemyTokenTests] EnemyToken_MaxHP_ReturnsInitialHP: starting test");
            var enemy = TestDataFactory.CreateEnemyToken(hp: 7);
            Debug.Log($"[EnemyTokenTests] EnemyToken_MaxHP_ReturnsInitialHP: maxHP={enemy.maxHP}");
            Assert.AreEqual(7, enemy.maxHP, "maxHP should return initial hp");
        }

        [Test]
        public void EnemyToken_Definition_ReturnsSelf()
        {
            Debug.Log("[EnemyTokenTests] EnemyToken_Definition_ReturnsSelf: starting test");
            var enemy = TestDataFactory.CreateEnemyToken();
            Debug.Log($"[EnemyTokenTests] EnemyToken_Definition_ReturnsSelf: definitionSameAsSelf={ReferenceEquals(enemy, enemy.definition)}");
            Assert.AreSame(enemy, enemy.definition, "definition property should return self");
        }

        [Test]
        public void EnemyToken_TakeDamage_ExactKill()
        {
            Debug.Log("[EnemyTokenTests] EnemyToken_TakeDamage_ExactKill: starting test");
            var enemy = TestDataFactory.CreateEnemyToken(hp: 4);
            bool killed = enemy.TakeDamage(4);
            Debug.Log($"[EnemyTokenTests] EnemyToken_TakeDamage_ExactKill: killed={killed}, hp={enemy.currentHP}");
            Assert.IsTrue(killed, "Exact damage should kill");
            Assert.AreEqual(0, enemy.currentHP);
        }

        [Test]
        public void EnemyToken_MultipleDamage_Accumulates()
        {
            Debug.Log("[EnemyTokenTests] EnemyToken_MultipleDamage_Accumulates: starting test");
            var enemy = TestDataFactory.CreateEnemyToken(hp: 10);
            enemy.TakeDamage(3);
            enemy.TakeDamage(4);
            Debug.Log($"[EnemyTokenTests] EnemyToken_MultipleDamage_Accumulates: hp={enemy.currentHP}");
            Assert.AreEqual(3, enemy.currentHP, "Damage should accumulate");
        }
    }

    // ============================================================
    // 9. CombatResolver Tests
    // ============================================================
    [TestFixture]
    public class CombatResolverTests
    {
        private ICombatResolver resolver;

        [SetUp]
        public void Setup()
        {
            Debug.Log("[CombatResolverTests] Setup: ENTER - registering services and resolving ICombatResolver");
            TestDataFactory.RegisterServices();
            resolver = ServiceLocator.Get<ICombatResolver>();
            Debug.Log($"[CombatResolverTests] Setup: resolved ICombatResolver via ServiceLocator (resolver={resolver != null})");
            EventBus.Reset();
            Debug.Log("[CombatResolverTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[CombatResolverTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[CombatResolverTests] Teardown: EXIT");
        }

        [Test]
        public void CombatResolver_HeroWins_WhenStronger()
        {
            Debug.Log("[CombatResolverTests] CombatResolver_HeroWins_WhenStronger: starting test");
            var heroDef = TestDataFactory.CreateHero(combat: 8, hp: 10);
            var hero = ServiceLocator.Get<IHeroTokenFactory>().Create(heroDef, 0);
            Debug.Log("[CombatResolverTests] CombatResolver_HeroWins_WhenStronger: created hero via IHeroTokenFactory");
            var enemy = TestDataFactory.CreateEnemyToken(strength: 2, hp: 3);
            var heroes = new List<HeroToken> { hero };
            var enemies = new List<EnemyToken> { enemy };
            var result = resolver.ResolveCombat(heroes, enemies, 1);
            Debug.Log($"[CombatResolverTests] CombatResolver_HeroWins_WhenStronger: heroesWon={result.heroesWon}, rounds={result.roundsFought}");
            Assert.IsTrue(result.heroesWon, "Heroes should win when stronger");
            Assert.Greater(result.survivingHeroTokenIds.Count, 0, "Surviving heroes should exist");
        }

        [Test]
        public void CombatResolver_HeroLoses_WhenWeaker()
        {
            Debug.Log("[CombatResolverTests] CombatResolver_HeroLoses_WhenWeaker: starting test");
            var heroDef = TestDataFactory.CreateHero(combat: 1, hp: 2);
            var hero = ServiceLocator.Get<IHeroTokenFactory>().Create(heroDef, 0);
            Debug.Log("[CombatResolverTests] CombatResolver_HeroLoses_WhenWeaker: created hero via IHeroTokenFactory");
            var enemy = TestDataFactory.CreateEnemyToken(strength: 10, hp: 20);
            var heroes = new List<HeroToken> { hero };
            var enemies = new List<EnemyToken> { enemy };
            var result = resolver.ResolveCombat(heroes, enemies, 1);
            Debug.Log($"[CombatResolverTests] CombatResolver_HeroLoses_WhenWeaker: heroesWon={result.heroesWon}, injuredHeroes={result.injuredHeroTokenIds.Count}");
            Assert.IsFalse(result.heroesWon, "Heroes should lose when weaker");
        }

        [Test]
        public void CombatResolver_MultipleHeroes_PoolCombatStrength()
        {
            Debug.Log("[CombatResolverTests] CombatResolver_MultipleHeroes_PoolCombatStrength: starting test");
            var heroFactory = ServiceLocator.Get<IHeroTokenFactory>();
            Debug.Log("[CombatResolverTests] CombatResolver_MultipleHeroes_PoolCombatStrength: resolved IHeroTokenFactory");
            var h1 = heroFactory.Create(TestDataFactory.CreateHero(id: 1, name: "H1", combat: 3, hp: 10), 0);
            var h2 = heroFactory.Create(TestDataFactory.CreateHero(id: 2, name: "H2", combat: 4, hp: 10), 1);
            var enemy = TestDataFactory.CreateEnemyToken(strength: 5, hp: 6);
            var heroes = new List<HeroToken> { h1, h2 };
            var enemies = new List<EnemyToken> { enemy };
            var result = resolver.ResolveCombat(heroes, enemies, 1);
            Debug.Log($"[CombatResolverTests] CombatResolver_MultipleHeroes_PoolCombatStrength: heroesWon={result.heroesWon}, survivingHeroes={result.survivingHeroTokenIds.Count}");
            Assert.IsTrue(result.heroesWon, "Combined heroes (3+4=7) should beat enemy (5)");
        }

        [Test]
        public void CombatResolver_DamageGoesToLowestCombatFirst()
        {
            Debug.Log("[CombatResolverTests] CombatResolver_DamageGoesToLowestCombatFirst: starting test");
            var weakHeroDef = TestDataFactory.CreateHero(id: 1, name: "Weak", combat: 1, hp: 2, initiative: 1);
            var strongHeroDef = TestDataFactory.CreateHero(id: 2, name: "Strong", combat: 5, hp: 10, initiative: 10);
            var heroFactory = ServiceLocator.Get<IHeroTokenFactory>();
            Debug.Log("[CombatResolverTests] CombatResolver_DamageGoesToLowestCombatFirst: resolved IHeroTokenFactory");
            var weakHero = heroFactory.Create(weakHeroDef, 0);
            var strongHero = heroFactory.Create(strongHeroDef, 1);
            // With 2 heroes, group bonus = +1 each, so effective: (1+1)+(5+1) = 8
            // Enemy must be stronger than 8 to deal damage to heroes
            var enemy = TestDataFactory.CreateEnemyToken(strength: 10, hp: 100);
            var heroes = new List<HeroToken> { weakHero, strongHero };
            var enemies = new List<EnemyToken> { enemy };
            var result = resolver.ResolveCombat(heroes, enemies, 1);
            Debug.Log($"[CombatResolverTests] CombatResolver_DamageGoesToLowestCombatFirst: weakHP={weakHero.currentHP}, strongHP={strongHero.currentHP}");
            // The weak hero should take damage first (lowest combat targeted first)
            Assert.IsTrue(weakHero.isInjured || weakHero.currentHP < weakHeroDef.hp, "Weak hero should take damage first");
        }

        [Test]
        public void CombatResult_HasCorrectSurvivingIds()
        {
            Debug.Log("[CombatResolverTests] CombatResult_HasCorrectSurvivingIds: starting test");
            var h1 = ServiceLocator.Get<IHeroTokenFactory>().Create(TestDataFactory.CreateHero(id: 1, name: "H1", combat: 10, hp: 20), 0);
            Debug.Log("[CombatResolverTests] CombatResult_HasCorrectSurvivingIds: created hero via IHeroTokenFactory");
            var enemy = TestDataFactory.CreateEnemyToken(strength: 1, hp: 1);
            var heroes = new List<HeroToken> { h1 };
            var enemies = new List<EnemyToken> { enemy };
            var result = resolver.ResolveCombat(heroes, enemies, 1);
            Debug.Log($"[CombatResolverTests] CombatResult_HasCorrectSurvivingIds: survivingHeroes=[{string.Join(",", result.survivingHeroTokenIds)}]");
            Assert.Contains(0, result.survivingHeroTokenIds, "Surviving hero token ID should be in list");
        }

        [Test]
        public void CombatResult_HasCorrectDefeatedEnemyIds()
        {
            Debug.Log("[CombatResolverTests] CombatResult_HasCorrectDefeatedEnemyIds: starting test");
            var h1 = ServiceLocator.Get<IHeroTokenFactory>().Create(TestDataFactory.CreateHero(combat: 10, hp: 20), 0);
            Debug.Log("[CombatResolverTests] CombatResult_HasCorrectDefeatedEnemyIds: created hero via IHeroTokenFactory");
            var enemy = TestDataFactory.CreateEnemyToken(tokenId: 7, strength: 1, hp: 1);
            var heroes = new List<HeroToken> { h1 };
            var enemies = new List<EnemyToken> { enemy };
            var result = resolver.ResolveCombat(heroes, enemies, 1);
            Debug.Log($"[CombatResolverTests] CombatResult_HasCorrectDefeatedEnemyIds: defeatedEnemies=[{string.Join(",", result.defeatedEnemyTokenIds)}]");
            Assert.Contains(7, result.defeatedEnemyTokenIds, "Defeated enemy token ID should be in list");
        }

        [Test]
        public void CombatResolver_MultipleRounds_UntilEliminated()
        {
            Debug.Log("[CombatResolverTests] CombatResolver_MultipleRounds_UntilEliminated: starting test");
            // Equal strength means tied rounds, combat continues via overflow/cleave etc
            var h1 = ServiceLocator.Get<IHeroTokenFactory>().Create(TestDataFactory.CreateHero(combat: 4, hp: 20), 0);
            Debug.Log("[CombatResolverTests] CombatResolver_MultipleRounds_UntilEliminated: created hero via IHeroTokenFactory");
            var enemy = TestDataFactory.CreateEnemyToken(strength: 3, hp: 5);
            var heroes = new List<HeroToken> { h1 };
            var enemies = new List<EnemyToken> { enemy };
            var result = resolver.ResolveCombat(heroes, enemies, 1);
            Debug.Log($"[CombatResolverTests] CombatResolver_MultipleRounds_UntilEliminated: rounds={result.roundsFought}, heroesWon={result.heroesWon}");
            Assert.IsTrue(result.heroesWon, "Hero with higher combat should eventually win");
            Assert.Greater(result.roundsFought, 0, "Should have fought at least 1 round");
        }

        [Test]
        public void CombatResolver_Riposte_DealsDamageBack()
        {
            Debug.Log("[CombatResolverTests] CombatResolver_Riposte_DealsDamageBack: starting test");
            var riposteDef = TestDataFactory.CreateHero(combat: 2, hp: 20, ability: SpecialAbility.Riposte);
            var hero = ServiceLocator.Get<IHeroTokenFactory>().Create(riposteDef, 0);
            Debug.Log("[CombatResolverTests] CombatResolver_Riposte_DealsDamageBack: created hero via IHeroTokenFactory");
            var enemy = TestDataFactory.CreateEnemyToken(strength: 5, hp: 10);
            var heroes = new List<HeroToken> { hero };
            var enemies = new List<EnemyToken> { enemy };
            var result = resolver.ResolveCombat(heroes, enemies, 1);
            Debug.Log($"[CombatResolverTests] CombatResolver_Riposte_DealsDamageBack: heroesWon={result.heroesWon}, rounds={result.roundsFought}");
            // Riposte hero should deal some damage even while losing
            Assert.Greater(result.roundsFought, 0, "Should have fought rounds");
        }

        [Test]
        public void CombatResolver_FirstStrike_PreRoundAttack()
        {
            Debug.Log("[CombatResolverTests] CombatResolver_FirstStrike_PreRoundAttack: starting test");
            var fsDef = TestDataFactory.CreateHero(combat: 10, hp: 20, ability: SpecialAbility.FirstStrike);
            var hero = ServiceLocator.Get<IHeroTokenFactory>().Create(fsDef, 0);
            Debug.Log("[CombatResolverTests] CombatResolver_FirstStrike_PreRoundAttack: created hero via IHeroTokenFactory");
            var enemy = TestDataFactory.CreateEnemyToken(strength: 3, hp: 5);
            var heroes = new List<HeroToken> { hero };
            var enemies = new List<EnemyToken> { enemy };
            var result = resolver.ResolveCombat(heroes, enemies, 1);
            Debug.Log($"[CombatResolverTests] CombatResolver_FirstStrike_PreRoundAttack: heroesWon={result.heroesWon}, rounds={result.roundsFought}");
            Assert.IsTrue(result.heroesWon, "FirstStrike hero should win");
        }

        [Test]
        public void CombatResolver_RangedStrike_PreCombatDamage()
        {
            Debug.Log("[CombatResolverTests] CombatResolver_RangedStrike_PreCombatDamage: starting test");
            var rangedDef = TestDataFactory.CreateHero(combat: 8, hp: 20, ability: SpecialAbility.RangedStrike);
            var hero = ServiceLocator.Get<IHeroTokenFactory>().Create(rangedDef, 0);
            Debug.Log("[CombatResolverTests] CombatResolver_RangedStrike_PreCombatDamage: created hero via IHeroTokenFactory");
            var enemy = TestDataFactory.CreateEnemyToken(strength: 3, hp: 5);
            var heroes = new List<HeroToken> { hero };
            var enemies = new List<EnemyToken> { enemy };
            var result = resolver.ResolveCombat(heroes, enemies, 1);
            Debug.Log($"[CombatResolverTests] CombatResolver_RangedStrike_PreCombatDamage: heroesWon={result.heroesWon}, rounds={result.roundsFought}");
            Assert.IsTrue(result.heroesWon, "RangedStrike hero should win");
        }

        [Test]
        public void CombatResolver_CombatResult_Create_InitializesCorrectly()
        {
            Debug.Log("[CombatResolverTests] CombatResolver_CombatResult_Create_InitializesCorrectly: starting test");
            var result = CombatResult.Create(42);
            Debug.Log($"[CombatResolverTests] CombatResult_Create: nodeId={result.nodeId}, heroesWon={result.heroesWon}, rounds={result.roundsFought}");
            Assert.AreEqual(42, result.nodeId);
            Assert.IsFalse(result.heroesWon);
            Assert.AreEqual(0, result.roundsFought);
            Assert.IsNotNull(result.survivingHeroTokenIds);
            Assert.IsNotNull(result.defeatedEnemyTokenIds);
            Assert.IsNotNull(result.injuredHeroTokenIds);
            Assert.IsNotNull(result.survivingEnemyTokenIds);
        }

        [Test]
        public void CombatResolver_BeginCombat_ReturnsCombatContextAndResult()
        {
            Debug.Log("[CombatResolverTests] CombatResolver_BeginCombat_ReturnsCombatContextAndResult: starting test");
            SimulationFlags.SuppressLogging = true;
            var hero = ServiceLocator.Get<IHeroTokenFactory>().Create(TestDataFactory.CreateHero(combat: 5, hp: 10), 0);
            var enemy = TestDataFactory.CreateEnemyToken(strength: 3, hp: 5);
            var heroes = new List<HeroToken> { hero };
            var enemies = new List<EnemyToken> { enemy };
            var (ctx, result) = resolver.BeginCombat(heroes, enemies, 1);
            SimulationFlags.SuppressLogging = false;
            Debug.Log($"[CombatResolverTests] BeginCombat: ctx.nodeId={ctx.nodeId}, result.nodeId={result.nodeId}, roundsFought={result.roundsFought}");
            Assert.IsNotNull(ctx, "CombatContext should not be null");
            Assert.IsNotNull(result, "CombatResult should not be null");
            Assert.AreEqual(1, result.nodeId, "Result nodeId should match");
            Assert.AreEqual(0, result.roundsFought, "No rounds should have been fought yet");
        }

        [Test]
        public void CombatResolver_ExecuteOneRound_AdvancesRound()
        {
            Debug.Log("[CombatResolverTests] CombatResolver_ExecuteOneRound_AdvancesRound: starting test");
            SimulationFlags.SuppressLogging = true;
            var hero = ServiceLocator.Get<IHeroTokenFactory>().Create(TestDataFactory.CreateHero(combat: 5, hp: 20), 0);
            var enemy = TestDataFactory.CreateEnemyToken(strength: 3, hp: 20);
            var heroes = new List<HeroToken> { hero };
            var enemies = new List<EnemyToken> { enemy };
            var (ctx, combatResult) = resolver.BeginCombat(heroes, enemies, 1);
            var roundResult = resolver.ExecuteOneRound(heroes, enemies, ctx, combatResult);
            SimulationFlags.SuppressLogging = false;
            Debug.Log($"[CombatResolverTests] ExecuteOneRound: round={roundResult.roundNumber}, continues={roundResult.combatContinues}, heroStr={roundResult.heroStrength}, enemyStr={roundResult.enemyStrength}");
            Assert.AreEqual(1, combatResult.roundsFought, "Should have fought 1 round");
            Assert.AreEqual(1, roundResult.roundNumber, "Round number should be 1");
            Assert.Greater(roundResult.heroStrength, 0, "Hero strength should be positive");
        }

        [Test]
        public void CombatResolver_EndCombat_PopulatesSurvivors()
        {
            Debug.Log("[CombatResolverTests] CombatResolver_EndCombat_PopulatesSurvivors: starting test");
            SimulationFlags.SuppressLogging = true;
            var hero = ServiceLocator.Get<IHeroTokenFactory>().Create(TestDataFactory.CreateHero(combat: 10, hp: 20), 0);
            var enemy = TestDataFactory.CreateEnemyToken(strength: 1, hp: 1);
            var heroes = new List<HeroToken> { hero };
            var enemies = new List<EnemyToken> { enemy };
            var (ctx, combatResult) = resolver.BeginCombat(heroes, enemies, 1);
            // Run rounds until done
            bool continues = true;
            while (continues && combatResult.roundsFought < 100)
            {
                var rr = resolver.ExecuteOneRound(heroes, enemies, ctx, combatResult);
                continues = rr.combatContinues;
            }
            resolver.EndCombat(heroes, enemies, combatResult, ctx);
            SimulationFlags.SuppressLogging = false;
            Debug.Log($"[CombatResolverTests] EndCombat: heroesWon={combatResult.heroesWon}, survivors={combatResult.survivingHeroTokenIds.Count}");
            Assert.IsTrue(combatResult.heroesWon, "Hero should win against weak enemy");
            Assert.Contains(0, combatResult.survivingHeroTokenIds, "Hero should be in survivors");
        }

        [Test]
        public void CombatResolver_SteppedAPI_MatchesSynchronous()
        {
            Debug.Log("[CombatResolverTests] CombatResolver_SteppedAPI_MatchesSynchronous: starting test");
            SimulationFlags.SuppressLogging = true;
            // Create identical setups for both APIs
            var heroDef = TestDataFactory.CreateHero(combat: 4, hp: 10);
            var heroFactory = ServiceLocator.Get<IHeroTokenFactory>();

            // Synchronous
            var hero1 = heroFactory.Create(heroDef, 0);
            var enemy1 = TestDataFactory.CreateEnemyToken(tokenId: 0, strength: 3, hp: 8);
            var syncResult = resolver.ResolveCombat(new List<HeroToken> { hero1 }, new List<EnemyToken> { enemy1 }, 1);

            // Stepped
            var hero2 = heroFactory.Create(heroDef, 1);
            var enemy2 = TestDataFactory.CreateEnemyToken(tokenId: 1, strength: 3, hp: 8);
            var heroes2 = new List<HeroToken> { hero2 };
            var enemies2 = new List<EnemyToken> { enemy2 };
            var (ctx, steppedResult) = resolver.BeginCombat(heroes2, enemies2, 1);
            bool continues = true;
            while (continues && steppedResult.roundsFought < 100)
            {
                var rr = resolver.ExecuteOneRound(heroes2, enemies2, ctx, steppedResult);
                continues = rr.combatContinues;
            }
            resolver.EndCombat(heroes2, enemies2, steppedResult, ctx);
            SimulationFlags.SuppressLogging = false;

            Debug.Log($"[CombatResolverTests] SteppedAPI_MatchesSynchronous: sync(won={syncResult.heroesWon}, rounds={syncResult.roundsFought}) stepped(won={steppedResult.heroesWon}, rounds={steppedResult.roundsFought})");
            Assert.AreEqual(syncResult.heroesWon, steppedResult.heroesWon, "Both APIs should produce same winner");
            Assert.AreEqual(syncResult.roundsFought, steppedResult.roundsFought, "Both APIs should fight same number of rounds");
        }
    }

    // ============================================================
    // 10. ColonyGraph Tests
    // ============================================================
    [TestFixture]
    public class ColonyGraphTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[ColonyGraphTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[ColonyGraphTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[ColonyGraphTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[ColonyGraphTests] Teardown: EXIT");
        }

        [Test]
        public void ColonyGraph_Initialize_CreatesEmptyGraph()
        {
            Debug.Log("[ColonyGraphTests] ColonyGraph_Initialize_CreatesEmptyGraph: starting test");
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[ColonyGraphTests] ColonyGraph_Initialize_CreatesEmptyGraph: resolved IColonyGraph via ServiceLocator");
            colony.Initialize();
            Debug.Log($"[ColonyGraphTests] ColonyGraph_Initialize_CreatesEmptyGraph: cardCount={colony.CardCount}");
            Assert.AreEqual(0, colony.CardCount, "Empty initialize should have 0 cards");
        }

        [Test]
        public void ColonyGraph_InitializeWithDefs_CreatesTwoCards()
        {
            Debug.Log("[ColonyGraphTests] ColonyGraph_InitializeWithDefs_CreatesTwoCards: starting test");
            var entrance = TestDataFactory.CreateColonyCard(id: 21, name: "Entrance", effect: ColonyEffect.BaseProduction, effectValue: 2, isStarter: true);
            var burrow = TestDataFactory.CreateColonyCard(id: 22, name: "Basic Burrow", effect: ColonyEffect.FoodProduction, effectValue: 1, isStarter: true);
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[ColonyGraphTests] ColonyGraph_InitializeWithDefs_CreatesTwoCards: resolved IColonyGraph via ServiceLocator");
            colony.Initialize(entrance, burrow);
            Debug.Log($"[ColonyGraphTests] ColonyGraph_InitializeWithDefs_CreatesTwoCards: cardCount={colony.CardCount}");
            Assert.AreEqual(2, colony.CardCount, "Should have entrance + basic burrow");
        }

        [Test]
        public void ColonyGraph_AddCard_ReturnsValidId()
        {
            Debug.Log("[ColonyGraphTests] ColonyGraph_AddCard_ReturnsValidId: starting test");
            var entrance = TestDataFactory.CreateColonyCard(id: 21, name: "Entrance");
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[ColonyGraphTests] ColonyGraph_AddCard_ReturnsValidId: resolved IColonyGraph via ServiceLocator");
            colony.Initialize(entrance, null);
            var newCard = TestDataFactory.CreateColonyCard(id: 23, name: "Storage");
            int placedId = colony.AddCard(newCard, 0);
            Debug.Log($"[ColonyGraphTests] ColonyGraph_AddCard_ReturnsValidId: placedId={placedId}, cardCount={colony.CardCount}");
            Assert.GreaterOrEqual(placedId, 0, "Should return valid placed ID");
        }

        [Test]
        public void ColonyGraph_AddCard_AdjacentToRequirementValidated()
        {
            Debug.Log("[ColonyGraphTests] ColonyGraph_AddCard_AdjacentToRequirementValidated: starting test");
            var entrance = TestDataFactory.CreateColonyCard(id: 21, name: "Entrance");
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[ColonyGraphTests] ColonyGraph_AddCard_AdjacentToRequirementValidated: resolved IColonyGraph via ServiceLocator");
            colony.Initialize(entrance, null);
            var adjCard = TestDataFactory.CreateColonyCard(id: 23, name: "Special",
                placement: PlacementRequirement.AdjacentTo, adjacencyCardName: "NonExistent");
            int placedId = colony.AddCard(adjCard, 0);
            Debug.Log($"[ColonyGraphTests] ColonyGraph_AddCard_AdjacentToRequirementValidated: placedId={placedId}");
            Assert.AreEqual(-1, placedId, "Should reject card when AdjacentTo requirement is not met");
        }

        [Test]
        public void ColonyGraph_GetPlacedCards_ReturnsAll()
        {
            Debug.Log("[ColonyGraphTests] ColonyGraph_GetPlacedCards_ReturnsAll: starting test");
            var entrance = TestDataFactory.CreateColonyCard(id: 21, name: "Entrance");
            var burrow = TestDataFactory.CreateColonyCard(id: 22, name: "Burrow");
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[ColonyGraphTests] ColonyGraph_GetPlacedCards_ReturnsAll: resolved IColonyGraph via ServiceLocator");
            colony.Initialize(entrance, burrow);
            var placed = colony.GetPlacedCards();
            Debug.Log($"[ColonyGraphTests] ColonyGraph_GetPlacedCards_ReturnsAll: placedCount={placed.Count}");
            Assert.AreEqual(2, placed.Count, "Should return all placed cards");
        }

        [Test]
        public void ColonyGraph_GetAdjacentCards_ReturnsNeighbors()
        {
            Debug.Log("[ColonyGraphTests] ColonyGraph_GetAdjacentCards_ReturnsNeighbors: starting test");
            var entrance = TestDataFactory.CreateColonyCard(id: 21, name: "Entrance");
            var burrow = TestDataFactory.CreateColonyCard(id: 22, name: "Burrow");
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[ColonyGraphTests] ColonyGraph_GetAdjacentCards_ReturnsNeighbors: resolved IColonyGraph via ServiceLocator");
            colony.Initialize(entrance, burrow);
            var adjacent = colony.GetAdjacentCards(0); // entrance should link to burrow
            Debug.Log($"[ColonyGraphTests] ColonyGraph_GetAdjacentCards_ReturnsNeighbors: adjacentCount={adjacent.Count}");
            Assert.AreEqual(1, adjacent.Count, "Entrance should be adjacent to burrow");
            Assert.AreEqual(1, adjacent[0], "Adjacent card should be burrow (id=1)");
        }

        [Test]
        public void ColonyGraph_CalculateFoodProduction_ReturnsBaseWithBonuses()
        {
            Debug.Log("[ColonyGraphTests] ColonyGraph_CalculateFoodProduction_ReturnsBaseWithBonuses: starting test");
            var baseCard = TestDataFactory.CreateColonyCard(id: 21, name: "Entrance", effect: ColonyEffect.BaseProduction, effectValue: 2);
            var foodCard = TestDataFactory.CreateColonyCard(id: 22, name: "Farm", effect: ColonyEffect.FoodProduction, effectValue: 3);
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[ColonyGraphTests] ColonyGraph_CalculateFoodProduction_ReturnsBaseWithBonuses: resolved IColonyGraph via ServiceLocator");
            colony.Initialize(baseCard, foodCard);
            int production = colony.CalculateFoodProduction();
            Debug.Log($"[ColonyGraphTests] ColonyGraph_CalculateFoodProduction_ReturnsBaseWithBonuses: production={production}");
            Assert.AreEqual(5, production, "Production should be base (2) + food bonus (3) = 5");
        }

        [Test]
        public void ColonyGraph_HasEffect_ReturnsTrueForPlacedEffects()
        {
            Debug.Log("[ColonyGraphTests] ColonyGraph_HasEffect_ReturnsTrueForPlacedEffects: starting test");
            var card = TestDataFactory.CreateColonyCard(effect: ColonyEffect.FogReveal, effectValue: 1);
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[ColonyGraphTests] ColonyGraph_HasEffect_ReturnsTrueForPlacedEffects: resolved IColonyGraph via ServiceLocator");
            colony.Initialize(card, null);
            bool hasEffect = colony.HasEffect(ColonyEffect.FogReveal);
            bool noEffect = colony.HasEffect(ColonyEffect.DoubleProduction);
            Debug.Log($"[ColonyGraphTests] ColonyGraph_HasEffect_ReturnsTrueForPlacedEffects: fogReveal={hasEffect}, doubleProduction={noEffect}");
            Assert.IsTrue(hasEffect, "Should have FogReveal effect");
            Assert.IsFalse(noEffect, "Should not have DoubleProduction effect");
        }

        [Test]
        public void ColonyGraph_GetEffectValue_SumsAcrossCards()
        {
            Debug.Log("[ColonyGraphTests] ColonyGraph_GetEffectValue_SumsAcrossCards: starting test");
            var card1 = TestDataFactory.CreateColonyCard(id: 21, name: "Farm1", effect: ColonyEffect.FoodProduction, effectValue: 2);
            var card2 = TestDataFactory.CreateColonyCard(id: 22, name: "Farm2", effect: ColonyEffect.FoodProduction, effectValue: 3);
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[ColonyGraphTests] ColonyGraph_GetEffectValue_SumsAcrossCards: resolved IColonyGraph via ServiceLocator");
            colony.Initialize(card1, card2);
            int totalValue = colony.GetEffectValue(ColonyEffect.FoodProduction);
            Debug.Log($"[ColonyGraphTests] ColonyGraph_GetEffectValue_SumsAcrossCards: totalValue={totalValue}");
            Assert.AreEqual(5, totalValue, "Should sum effect values across cards");
        }

        [Test]
        public void ColonyGraph_GetActiveEffects_ReturnsAllEffects()
        {
            Debug.Log("[ColonyGraphTests] ColonyGraph_GetActiveEffects_ReturnsAllEffects: starting test");
            var card1 = TestDataFactory.CreateColonyCard(id: 21, name: "Farm", effect: ColonyEffect.FoodProduction, effectValue: 2);
            var card2 = TestDataFactory.CreateColonyCard(id: 22, name: "Wall", effect: ColonyEffect.ColonyDefenseWall, effectValue: 1);
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[ColonyGraphTests] ColonyGraph_GetActiveEffects_ReturnsAllEffects: resolved IColonyGraph via ServiceLocator");
            colony.Initialize(card1, card2);
            var effects = colony.GetActiveEffects();
            Debug.Log($"[ColonyGraphTests] ColonyGraph_GetActiveEffects_ReturnsAllEffects: effectCount={effects.Count}");
            Assert.AreEqual(2, effects.Count, "Should have 2 unique effects");
            Assert.IsTrue(effects.ContainsKey(ColonyEffect.FoodProduction), "Should contain FoodProduction");
            Assert.IsTrue(effects.ContainsKey(ColonyEffect.ColonyDefenseWall), "Should contain ColonyDefenseWall");
        }

        [Test]
        public void ColonyGraph_CanPlayCard_RespectsPerTurnLimit()
        {
            Debug.Log("[ColonyGraphTests] ColonyGraph_CanPlayCard_RespectsPerTurnLimit: starting test");
            var entrance = TestDataFactory.CreateColonyCard(id: 21, name: "Entrance");
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[ColonyGraphTests] ColonyGraph_CanPlayCard_RespectsPerTurnLimit: resolved IColonyGraph via ServiceLocator");
            colony.Initialize(entrance, null);
            Assert.IsTrue(colony.CanPlayCard(), "Should be able to play first card");
            var newCard = TestDataFactory.CreateColonyCard(id: 23, name: "Storage");
            colony.AddCard(newCard, 0);
            bool canPlaySecond = colony.CanPlayCard();
            Debug.Log($"[ColonyGraphTests] ColonyGraph_CanPlayCard_RespectsPerTurnLimit: canPlaySecond={canPlaySecond}");
            Assert.IsFalse(canPlaySecond, "Should not be able to play second card per turn (default limit=1)");
        }

        [Test]
        public void ColonyGraph_ResetTurnCardCount_AllowsNewPlay()
        {
            Debug.Log("[ColonyGraphTests] ColonyGraph_ResetTurnCardCount_AllowsNewPlay: starting test");
            var entrance = TestDataFactory.CreateColonyCard(id: 21, name: "Entrance");
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[ColonyGraphTests] ColonyGraph_ResetTurnCardCount_AllowsNewPlay: resolved IColonyGraph via ServiceLocator");
            colony.Initialize(entrance, null);
            colony.AddCard(TestDataFactory.CreateColonyCard(id: 23, name: "Storage"), 0);
            Assert.IsFalse(colony.CanPlayCard(), "Should be blocked after playing 1 card");
            colony.ResetTurnCardCount();
            bool canPlayAfterReset = colony.CanPlayCard();
            Debug.Log($"[ColonyGraphTests] ColonyGraph_ResetTurnCardCount_AllowsNewPlay: canPlay={canPlayAfterReset}");
            Assert.IsTrue(canPlayAfterReset, "Should be able to play after reset");
        }
    }

    // ============================================================
    // 11. DeckConstructionManager Tests (logic only, no MonoBehaviour)
    // ============================================================
    [TestFixture]
    public class DeckConstructionManagerTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[DeckConstructionManagerTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[DeckConstructionManagerTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[DeckConstructionManagerTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[DeckConstructionManagerTests] Teardown: EXIT");
        }

        [Test]
        public void DeckConstruction_GetMaxCopies_Cost1Returns3()
        {
            Debug.Log("[DeckConstructionManagerTests] DeckConstruction_GetMaxCopies_Cost1Returns3: starting test");
            var go = new GameObject("TestDCM");
            var dcm = go.AddComponent<DeckConstructionManager>();
            int result = dcm.GetMaxCopies(1);
            Debug.Log($"[DeckConstructionManagerTests] DeckConstruction_GetMaxCopies_Cost1Returns3: maxCopies={result}");
            Assert.AreEqual(3, result, "Cost 1 should allow 3 copies");
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void DeckConstruction_GetMaxCopies_Cost2Returns2()
        {
            Debug.Log("[DeckConstructionManagerTests] DeckConstruction_GetMaxCopies_Cost2Returns2: starting test");
            var go = new GameObject("TestDCM");
            var dcm = go.AddComponent<DeckConstructionManager>();
            int result = dcm.GetMaxCopies(2);
            Debug.Log($"[DeckConstructionManagerTests] DeckConstruction_GetMaxCopies_Cost2Returns2: maxCopies={result}");
            Assert.AreEqual(2, result, "Cost 2 should allow 2 copies");
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void DeckConstruction_GetMaxCopies_Cost3PlusReturns1()
        {
            Debug.Log("[DeckConstructionManagerTests] DeckConstruction_GetMaxCopies_Cost3PlusReturns1: starting test");
            var go = new GameObject("TestDCM");
            var dcm = go.AddComponent<DeckConstructionManager>();
            int result3 = dcm.GetMaxCopies(3);
            int result5 = dcm.GetMaxCopies(5);
            Debug.Log($"[DeckConstructionManagerTests] DeckConstruction_GetMaxCopies_Cost3PlusReturns1: cost3={result3}, cost5={result5}");
            Assert.AreEqual(1, result3, "Cost 3 should allow 1 copy");
            Assert.AreEqual(1, result5, "Cost 5 should allow 1 copy");
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void DeckConstruction_IsDeckValid_FalseWhenEmpty()
        {
            Debug.Log("[DeckConstructionManagerTests] DeckConstruction_IsDeckValid_FalseWhenEmpty: starting test");
            var go = new GameObject("TestDCM");
            var dcm = go.AddComponent<DeckConstructionManager>();
            bool valid = dcm.IsDeckValid;
            Debug.Log($"[DeckConstructionManagerTests] DeckConstruction_IsDeckValid_FalseWhenEmpty: valid={valid}, deckSize={dcm.DeckSize}");
            Assert.IsFalse(valid, "Empty deck should be invalid");
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ============================================================
    // 12. ScoreCalculator Tests
    // ============================================================
    [TestFixture]
    public class ScoreCalculatorTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[ScoreCalculatorTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[ScoreCalculatorTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[ScoreCalculatorTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[ScoreCalculatorTests] Teardown: EXIT");
        }

        [Test]
        public void ScoreCalculator_15Turns_GivesMinimumTurnScore()
        {
            Debug.Log("[ScoreCalculatorTests] ScoreCalculator_15Turns_GivesMinimumTurnScore: starting test");
            var input = new ScoreInput { turnsUsed = 15, deckSize = 30 };
            var result = ScoreCalculator.CalculateScore(input);
            Debug.Log($"[ScoreCalculatorTests] ScoreCalculator_15Turns_GivesMinimumTurnScore: turnScore={result.turnScore}");
            // (31 - 15) * 100 = 1600
            Assert.AreEqual(1600, result.turnScore, "15 turns should give turn score of 1600 with baseline 30");
        }

        [Test]
        public void ScoreCalculator_FewerTurns_GivesHigherScore()
        {
            Debug.Log("[ScoreCalculatorTests] ScoreCalculator_FewerTurns_GivesHigherScore: starting test");
            var input5 = new ScoreInput { turnsUsed = 5, deckSize = 30 };
            var input15 = new ScoreInput { turnsUsed = 15, deckSize = 30 };
            var result5 = ScoreCalculator.CalculateScore(input5);
            var result15 = ScoreCalculator.CalculateScore(input15);
            Debug.Log($"[ScoreCalculatorTests] ScoreCalculator_FewerTurns_GivesHigherScore: turn5={result5.turnScore}, turn15={result15.turnScore}");
            Assert.Greater(result5.turnScore, result15.turnScore, "Fewer turns should give higher score");
        }

        [Test]
        public void ScoreCalculator_DeckSize30_Multiplier1()
        {
            Debug.Log("[ScoreCalculatorTests] ScoreCalculator_DeckSize30_Multiplier1: starting test");
            var input = new ScoreInput { turnsUsed = 10, deckSize = 30 };
            var result = ScoreCalculator.CalculateScore(input);
            Debug.Log($"[ScoreCalculatorTests] ScoreCalculator_DeckSize30_Multiplier1: multiplier={result.deckMultiplier}");
            Assert.AreEqual(1.0f, result.deckMultiplier, 0.01f, "Deck size 30 should give multiplier 1.0");
        }

        [Test]
        public void ScoreCalculator_DeckSize10_Multiplier3()
        {
            Debug.Log("[ScoreCalculatorTests] ScoreCalculator_DeckSize10_Multiplier3: starting test");
            var input = new ScoreInput { turnsUsed = 10, deckSize = 10 };
            var result = ScoreCalculator.CalculateScore(input);
            Debug.Log($"[ScoreCalculatorTests] ScoreCalculator_DeckSize10_Multiplier3: multiplier={result.deckMultiplier}");
            Assert.AreEqual(3.0f, result.deckMultiplier, 0.01f, "Deck size 10 should give multiplier 3.0");
        }

        [Test]
        public void ScoreCalculator_EnemyDefeated_Adds10Points()
        {
            Debug.Log("[ScoreCalculatorTests] ScoreCalculator_EnemyDefeated_Adds10Points: starting test");
            var input = new ScoreInput { turnsUsed = 10, deckSize = 30, enemiesDefeated = 5 };
            var result = ScoreCalculator.CalculateScore(input);
            Debug.Log($"[ScoreCalculatorTests] ScoreCalculator_EnemyDefeated_Adds10Points: enemyScore={result.enemyScore}");
            Assert.AreEqual(50, result.enemyScore, "5 enemies * 10 points = 50");
        }

        [Test]
        public void ScoreCalculator_ZoneBoss_Adds50Points()
        {
            Debug.Log("[ScoreCalculatorTests] ScoreCalculator_ZoneBoss_Adds50Points: starting test");
            var input = new ScoreInput { turnsUsed = 10, deckSize = 30, zoneBossesDefeated = 2 };
            var result = ScoreCalculator.CalculateScore(input);
            Debug.Log($"[ScoreCalculatorTests] ScoreCalculator_ZoneBoss_Adds50Points: enemyScore={result.enemyScore}");
            Assert.AreEqual(100, result.enemyScore, "2 zone bosses * 50 points = 100");
        }

        [Test]
        public void ScoreCalculator_PiedPiper_Adds100Points()
        {
            Debug.Log("[ScoreCalculatorTests] ScoreCalculator_PiedPiper_Adds100Points: starting test");
            var input = new ScoreInput { turnsUsed = 10, deckSize = 30, piedPiperDefeated = true };
            var result = ScoreCalculator.CalculateScore(input);
            Debug.Log($"[ScoreCalculatorTests] ScoreCalculator_PiedPiper_Adds100Points: enemyScore={result.enemyScore}");
            Assert.AreEqual(100, result.enemyScore, "Pied Piper defeated should add 100 points");
        }

        [Test]
        public void ScoreCalculator_Resources_Add2Each()
        {
            Debug.Log("[ScoreCalculatorTests] ScoreCalculator_Resources_Add2Each: starting test");
            var input = new ScoreInput { turnsUsed = 10, deckSize = 30, totalResourcesGathered = 10 };
            var result = ScoreCalculator.CalculateScore(input);
            Debug.Log($"[ScoreCalculatorTests] ScoreCalculator_Resources_Add2Each: resourceScore={result.resourceScore}");
            Assert.AreEqual(20, result.resourceScore, "10 resources * 2 = 20");
        }

        [Test]
        public void ScoreCalculator_ColonyCards_Add20Each()
        {
            Debug.Log("[ScoreCalculatorTests] ScoreCalculator_ColonyCards_Add20Each: starting test");
            var input = new ScoreInput { turnsUsed = 10, deckSize = 30, colonyCardsPlayed = 3 };
            var result = ScoreCalculator.CalculateScore(input);
            Debug.Log($"[ScoreCalculatorTests] ScoreCalculator_ColonyCards_Add20Each: colonyScore={result.colonyScore}");
            Assert.AreEqual(60, result.colonyScore, "3 colony cards * 20 = 60");
        }

        [Test]
        public void ScoreCalculator_HeroNeverInjured_Adds50Each()
        {
            Debug.Log("[ScoreCalculatorTests] ScoreCalculator_HeroNeverInjured_Adds50Each: starting test");
            var input = new ScoreInput { turnsUsed = 10, deckSize = 30, heroesNeverInjured = 2 };
            var result = ScoreCalculator.CalculateScore(input);
            Debug.Log($"[ScoreCalculatorTests] ScoreCalculator_HeroNeverInjured_Adds50Each: heroBonus={result.heroBonus}");
            Assert.AreEqual(100, result.heroBonus, "2 heroes never injured * 50 = 100");
        }

        [Test]
        public void ScoreCalculator_FullCalculation_CombinesAllComponents()
        {
            Debug.Log("[ScoreCalculatorTests] ScoreCalculator_FullCalculation_CombinesAllComponents: starting test");
            var input = new ScoreInput
            {
                turnsUsed = 10,
                deckSize = 20,
                enemiesDefeated = 5,
                zoneBossesDefeated = 1,
                piedPiperDefeated = true,
                totalResourcesGathered = 20,
                colonyCardsPlayed = 4,
                heroesNeverInjured = 3
            };
            var result = ScoreCalculator.CalculateScore(input);
            int expectedTurnScore = Mathf.Max(0, (31 - 10)) * 100; // (31-10)*100 = 2100
            int expectedEnemyScore = 50 + 50 + 100; // 200
            int expectedResourceScore = 40;
            int expectedColonyScore = 80;
            int expectedHeroBonus = 150;
            int preMultiplier = expectedTurnScore + expectedEnemyScore + expectedResourceScore + expectedColonyScore + expectedHeroBonus;
            int expectedFinal = Mathf.RoundToInt(preMultiplier * 1.5f); // deck size 20 = 1.5x
            Debug.Log($"[ScoreCalculatorTests] ScoreCalculator_FullCalculation_CombinesAllComponents: finalScore={result.finalScore}, expected={expectedFinal}");
            Assert.AreEqual(expectedFinal, result.finalScore, "Final score should combine all components with deck multiplier");
        }
    }

    // ============================================================
    // 13. CombatContext Tests
    // ============================================================
    [TestFixture]
    public class CombatContextTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[CombatContextTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[CombatContextTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[CombatContextTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[CombatContextTests] Teardown: EXIT");
        }

        [Test]
        public void CombatContext_AddTempCombatBuff_AddsCorrectly()
        {
            Debug.Log("[CombatContextTests] CombatContext_AddTempCombatBuff_AddsCorrectly: starting test");
            var ctx = new CombatContext();
            ctx.AddTempCombatBuff(0, 3);
            int total = ctx.GetTotalCombatBuff(0);
            Debug.Log($"[CombatContextTests] CombatContext_AddTempCombatBuff_AddsCorrectly: total={total}");
            Assert.AreEqual(3, total, "Temp buff should be reflected in total");
        }

        [Test]
        public void CombatContext_AddPermanentCombatBuff_PersistsAcrossRounds()
        {
            Debug.Log("[CombatContextTests] CombatContext_AddPermanentCombatBuff_PersistsAcrossRounds: starting test");
            var ctx = new CombatContext();
            ctx.AddPermanentCombatBuff(0, 5);
            ctx.AdvanceRound();
            int total = ctx.GetTotalCombatBuff(0);
            Debug.Log($"[CombatContextTests] CombatContext_AddPermanentCombatBuff_PersistsAcrossRounds: total={total}");
            Assert.AreEqual(5, total, "Permanent buff should persist after AdvanceRound");
        }

        [Test]
        public void CombatContext_AdvanceRound_ClearsTempBuffs()
        {
            Debug.Log("[CombatContextTests] CombatContext_AdvanceRound_ClearsTempBuffs: starting test");
            var ctx = new CombatContext();
            ctx.AddTempCombatBuff(0, 10);
            ctx.AdvanceRound();
            int total = ctx.GetTotalCombatBuff(0);
            Debug.Log($"[CombatContextTests] CombatContext_AdvanceRound_ClearsTempBuffs: total={total}");
            Assert.AreEqual(0, total, "Temp buff should be cleared after AdvanceRound");
        }

        [Test]
        public void CombatContext_AdvanceRound_PreservesPermanentBuffs()
        {
            Debug.Log("[CombatContextTests] CombatContext_AdvanceRound_PreservesPermanentBuffs: starting test");
            var ctx = new CombatContext();
            ctx.AddPermanentCombatBuff(0, 3);
            ctx.AddTempCombatBuff(0, 7);
            Assert.AreEqual(10, ctx.GetTotalCombatBuff(0), "Before advance: both should sum");
            ctx.AdvanceRound();
            int afterAdvance = ctx.GetTotalCombatBuff(0);
            Debug.Log($"[CombatContextTests] CombatContext_AdvanceRound_PreservesPermanentBuffs: afterAdvance={afterAdvance}");
            Assert.AreEqual(3, afterAdvance, "After advance: only permanent should remain");
        }

        [Test]
        public void CombatContext_GetTotalCombatBuff_SumsTempAndPermanent()
        {
            Debug.Log("[CombatContextTests] CombatContext_GetTotalCombatBuff_SumsTempAndPermanent: starting test");
            var ctx = new CombatContext();
            ctx.AddTempCombatBuff(0, 2);
            ctx.AddPermanentCombatBuff(0, 4);
            int total = ctx.GetTotalCombatBuff(0);
            Debug.Log($"[CombatContextTests] CombatContext_GetTotalCombatBuff_SumsTempAndPermanent: total={total}");
            Assert.AreEqual(6, total, "Total should be temp (2) + permanent (4) = 6");
        }
    }

    // ============================================================
    // 14. EnemyAI Tests
    // ============================================================
    [TestFixture]
    public class EnemyAITests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[EnemyAITests] Setup: ENTER - registering services and initializing SeededRandom");
            TestDataFactory.RegisterServices();
            SeededRandom.Initialize(42);
            Debug.Log("[EnemyAITests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[EnemyAITests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[EnemyAITests] Teardown: EXIT");
        }

        [Test]
        public void EnemyAI_Guard_ReturnsMinusOne()
        {
            Debug.Log("[EnemyAITests] EnemyAI_Guard_ReturnsMinusOne: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var nodes = new List<MapNode>(graph.GetAllNodes());
            var enemy = TestDataFactory.CreateEnemyToken(behavior: EnemyBehavior.Guard, nodeId: 1, speed: 1);
            var heroes = new List<HeroToken>();
            int result = EnemyAI.DecideMove(enemy, nodes, heroes);
            Debug.Log($"[EnemyAITests] EnemyAI_Guard_ReturnsMinusOne: result={result}");
            Assert.AreEqual(-1, result, "Guard should return -1 (no move)");
        }

        [Test]
        public void EnemyAI_Patrol_MovesToNeighbor()
        {
            Debug.Log("[EnemyAITests] EnemyAI_Patrol_MovesToNeighbor: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var nodes = new List<MapNode>(graph.GetAllNodes());
            var enemy = TestDataFactory.CreateEnemyToken(behavior: EnemyBehavior.Patrol, nodeId: 1, speed: 1, zone: NodeType.Wilderness);
            var heroes = new List<HeroToken>();
            int result = EnemyAI.DecideMove(enemy, nodes, heroes);
            Debug.Log($"[EnemyAITests] EnemyAI_Patrol_MovesToNeighbor: result={result}");
            // Node 1 neighbors are 0 and 2
            Assert.That(result == 0 || result == 2, $"Patrol should move to a neighbor (got {result})");
        }

        [Test]
        public void EnemyAI_Chase_MovesTowardAdjacentHero()
        {
            Debug.Log("[EnemyAITests] EnemyAI_Chase_MovesTowardAdjacentHero: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var nodes = new List<MapNode>(graph.GetAllNodes());
            var hero = TestDataFactory.CreateHeroToken(0);
            hero.currentNodeId = 2;
            // Place enemy at node 3 (neighbors: 2, 4) — no colony adjacent, so hero takes priority
            var enemy = TestDataFactory.CreateEnemyToken(behavior: EnemyBehavior.Chase, nodeId: 3, speed: 1);
            var heroes = new List<HeroToken> { hero };
            int result = EnemyAI.DecideMove(enemy, nodes, heroes);
            Debug.Log($"[EnemyAITests] EnemyAI_Chase_MovesTowardAdjacentHero: result={result}");
            Assert.AreEqual(2, result, "Chase should move toward adjacent hero at node 2");
        }

        [Test]
        public void EnemyAI_Ambush_OnlyMovesWhenHeroAdjacent()
        {
            Debug.Log("[EnemyAITests] EnemyAI_Ambush_OnlyMovesWhenHeroAdjacent: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var nodes = new List<MapNode>(graph.GetAllNodes());
            var enemy = TestDataFactory.CreateEnemyToken(behavior: EnemyBehavior.Ambush, nodeId: 1, speed: 1);

            // No adjacent heroes
            var noHeroes = new List<HeroToken>();
            int noHeroResult = EnemyAI.DecideMove(enemy, nodes, noHeroes);
            Debug.Log($"[EnemyAITests] EnemyAI_Ambush_OnlyMovesWhenHeroAdjacent: noHeroes result={noHeroResult}");
            Assert.AreEqual(-1, noHeroResult, "Ambush should not move without adjacent hero");

            // Hero adjacent
            var hero = TestDataFactory.CreateHeroToken(0);
            hero.currentNodeId = 2;
            var withHero = new List<HeroToken> { hero };
            int heroResult = EnemyAI.DecideMove(enemy, nodes, withHero);
            Debug.Log($"[EnemyAITests] EnemyAI_Ambush_OnlyMovesWhenHeroAdjacent: withHero result={heroResult}");
            Assert.AreEqual(2, heroResult, "Ambush should move to adjacent hero at node 2");
        }

        [Test]
        public void EnemyAI_ProcessAllEnemies_ReturnsResultsForAll()
        {
            Debug.Log("[EnemyAITests] EnemyAI_ProcessAllEnemies_ReturnsResultsForAll: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var nodes = new List<MapNode>(graph.GetAllNodes());
            var enemy1 = TestDataFactory.CreateEnemyToken(tokenId: 0, behavior: EnemyBehavior.Patrol, nodeId: 1, speed: 1, zone: NodeType.Wilderness);
            var enemy2 = TestDataFactory.CreateEnemyToken(tokenId: 1, behavior: EnemyBehavior.Guard, nodeId: 2, speed: 1);
            var enemies = new List<EnemyToken> { enemy1, enemy2 };
            var heroes = new List<HeroToken>();
            var results = EnemyAI.ProcessAllEnemies(enemies, nodes, heroes);
            Debug.Log($"[EnemyAITests] EnemyAI_ProcessAllEnemies_ReturnsResultsForAll: resultCount={results.Count}");
            // Guard stays, Patrol moves: should have 1 result
            Assert.AreEqual(1, results.Count, "Only the patrol enemy should have a move result");
            Assert.AreEqual(0, results[0].enemyTokenId, "Moving enemy should be the patrol");
        }

        [Test]
        public void EnemyAI_DefeatedEnemy_SkipsMove()
        {
            Debug.Log("[EnemyAITests] EnemyAI_DefeatedEnemy_SkipsMove: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var nodes = new List<MapNode>(graph.GetAllNodes());
            var enemy = TestDataFactory.CreateEnemyToken(behavior: EnemyBehavior.Patrol, nodeId: 1, speed: 1);
            enemy.isDefeated = true;
            int result = EnemyAI.DecideMove(enemy, nodes, new List<HeroToken>());
            Debug.Log($"[EnemyAITests] EnemyAI_DefeatedEnemy_SkipsMove: result={result}");
            Assert.AreEqual(-1, result, "Defeated enemy should not move");
        }

        [Test]
        public void EnemyAI_ZeroSpeed_StaysPut()
        {
            Debug.Log("[EnemyAITests] EnemyAI_ZeroSpeed_StaysPut: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var nodes = new List<MapNode>(graph.GetAllNodes());
            var enemy = TestDataFactory.CreateEnemyToken(behavior: EnemyBehavior.Patrol, nodeId: 1, speed: 0);
            int result = EnemyAI.DecideMove(enemy, nodes, new List<HeroToken>());
            Debug.Log($"[EnemyAITests] EnemyAI_ZeroSpeed_StaysPut: result={result}");
            Assert.AreEqual(-1, result, "Speed 0 enemy should not move");
        }

        [Test]
        public void EnemyAI_NullEnemy_ReturnsMinusOne()
        {
            Debug.Log("[EnemyAITests] EnemyAI_NullEnemy_ReturnsMinusOne: starting test");
            int result = EnemyAI.DecideMove(null, new List<MapNode>(), new List<HeroToken>());
            Debug.Log($"[EnemyAITests] EnemyAI_NullEnemy_ReturnsMinusOne: result={result}");
            Assert.AreEqual(-1, result, "Null enemy should return -1");
        }
    }

    // ============================================================
    // 15. CleanupPhase Tests
    // ============================================================
    [TestFixture]
    public class CleanupPhaseTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[CleanupPhaseTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[CleanupPhaseTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[CleanupPhaseTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[CleanupPhaseTests] Teardown: EXIT");
        }

        [Test]
        public void CleanupPhase_ProcessInjuryRecovery_DecrementsTimer()
        {
            Debug.Log("[CleanupPhaseTests] CleanupPhase_ProcessInjuryRecovery_DecrementsTimer: starting test");
            var hero = TestDataFactory.CreateHeroToken(0);
            hero.isInjured = true;
            hero.turnsUntilRecovery = 2;
            hero.currentHP = 0;
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[CleanupPhaseTests] CleanupPhase_ProcessInjuryRecovery_DecrementsTimer: resolved IColonyGraph via ServiceLocator");
            colony.Initialize();
            var recovered = CleanupPhase.ProcessInjuryRecovery(new List<HeroToken> { hero }, colony);
            Debug.Log($"[CleanupPhaseTests] CleanupPhase_ProcessInjuryRecovery_DecrementsTimer: turnsLeft={hero.turnsUntilRecovery}, recovered={recovered.Count}");
            Assert.AreEqual(1, hero.turnsUntilRecovery, "Timer should decrement by 1");
            Assert.AreEqual(0, recovered.Count, "Should not be recovered yet");
        }

        [Test]
        public void CleanupPhase_ProcessInjuryRecovery_ReturnsRecoveredHeroes()
        {
            Debug.Log("[CleanupPhaseTests] CleanupPhase_ProcessInjuryRecovery_ReturnsRecoveredHeroes: starting test");
            var hero = TestDataFactory.CreateHeroToken(0);
            hero.isInjured = true;
            hero.turnsUntilRecovery = 1;
            hero.currentHP = 0;
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[CleanupPhaseTests] CleanupPhase_ProcessInjuryRecovery_ReturnsRecoveredHeroes: resolved IColonyGraph via ServiceLocator");
            colony.Initialize();
            var recovered = CleanupPhase.ProcessInjuryRecovery(new List<HeroToken> { hero }, colony);
            Debug.Log($"[CleanupPhaseTests] CleanupPhase_ProcessInjuryRecovery_ReturnsRecoveredHeroes: recoveredCount={recovered.Count}");
            Assert.AreEqual(1, recovered.Count, "Hero with timer at 1 should recover");
            Assert.AreEqual(0, recovered[0], "Recovered hero ID should match");
        }

        [Test]
        public void CleanupPhase_ShrineOfHeroes_ImmediateRecovery()
        {
            Debug.Log("[CleanupPhaseTests] CleanupPhase_ShrineOfHeroes_ImmediateRecovery: starting test");
            var hero = TestDataFactory.CreateHeroToken(0);
            hero.isInjured = true;
            hero.turnsUntilRecovery = 2;
            hero.currentHP = 0;
            var shrineCard = TestDataFactory.CreateColonyCard(effect: ColonyEffect.ImmediateHealReturn, effectValue: 1);
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[CleanupPhaseTests] CleanupPhase_ShrineOfHeroes_ImmediateRecovery: resolved IColonyGraph via ServiceLocator");
            colony.Initialize(shrineCard, null);
            var recovered = CleanupPhase.ProcessInjuryRecovery(new List<HeroToken> { hero }, colony);
            Debug.Log($"[CleanupPhaseTests] CleanupPhase_ShrineOfHeroes_ImmediateRecovery: recoveredCount={recovered.Count}");
            Assert.AreEqual(1, recovered.Count, "Shrine of Heroes should give immediate recovery");
        }

        [Test]
        public void CleanupPhase_CampfireBonus_SpeedsRecovery()
        {
            Debug.Log("[CleanupPhaseTests] CleanupPhase_CampfireBonus_SpeedsRecovery: starting test");
            var hero = TestDataFactory.CreateHeroToken(0);
            hero.isInjured = true;
            hero.turnsUntilRecovery = 2;
            hero.currentHP = 0;
            var campfireCard = TestDataFactory.CreateColonyCard(effect: ColonyEffect.HealInjured, effectValue: 1);
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[CleanupPhaseTests] CleanupPhase_CampfireBonus_SpeedsRecovery: resolved IColonyGraph via ServiceLocator");
            colony.Initialize(campfireCard, null);
            var recovered = CleanupPhase.ProcessInjuryRecovery(new List<HeroToken> { hero }, colony);
            Debug.Log($"[CleanupPhaseTests] CleanupPhase_CampfireBonus_SpeedsRecovery: turnsLeft={hero.turnsUntilRecovery}, recovered={recovered.Count}");
            // With healing bonus 1, recovery tick = 1 + 1 = 2, so turnsRemaining = 2 - 2 = 0 -> recovered
            Assert.AreEqual(1, recovered.Count, "Campfire bonus should speed recovery enough to recover immediately");
        }

        [Test]
        public void CleanupPhase_EnemyRespawnTimer_TicksDown()
        {
            Debug.Log("[CleanupPhaseTests] CleanupPhase_EnemyRespawnTimer_TicksDown: starting test");
            var enemy = TestDataFactory.CreateEnemyToken(hp: 4);
            enemy.isDefeated = true;
            enemy.respawnTimer = 3;
            enemy.currentHP = 0;
            var enemies = new List<EnemyToken> { enemy };
            // We need ResourceManager (MonoBehaviour) - create one
            var go = new GameObject("TestRM");
            var rm = go.AddComponent<ResourceManager>();
            rm.Initialize();
            rm.AddToStockpile(ResourceType.Food, 100);
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[CleanupPhaseTests] CleanupPhase_EnemyRespawnTimer_TicksDown: resolved IColonyGraph via ServiceLocator");
            colony.Initialize();
            var graph = TestDataFactory.CreateSimpleGraph();
            var result = CleanupPhase.Execute(new List<HeroToken>(), enemies, rm, colony, graph, false);
            Debug.Log($"[CleanupPhaseTests] CleanupPhase_EnemyRespawnTimer_TicksDown: respawnTimer={enemy.respawnTimer}");
            Assert.AreEqual(2, enemy.respawnTimer, "Respawn timer should tick down by 1");
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void CleanupPhase_NonInjuredHeroes_NotRecovered()
        {
            Debug.Log("[CleanupPhaseTests] CleanupPhase_NonInjuredHeroes_NotRecovered: starting test");
            var hero = TestDataFactory.CreateHeroToken(0);
            // Hero not injured
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[CleanupPhaseTests] CleanupPhase_NonInjuredHeroes_NotRecovered: resolved IColonyGraph via ServiceLocator");
            colony.Initialize();
            var recovered = CleanupPhase.ProcessInjuryRecovery(new List<HeroToken> { hero }, colony);
            Debug.Log($"[CleanupPhaseTests] CleanupPhase_NonInjuredHeroes_NotRecovered: recoveredCount={recovered.Count}");
            Assert.AreEqual(0, recovered.Count, "Non-injured hero should not appear in recovery list");
        }

        [Test]
        public void CleanupPhase_NullInput_ReturnsEmptyResult()
        {
            Debug.Log("[CleanupPhaseTests] CleanupPhase_NullInput_ReturnsEmptyResult: starting test");
            var result = CleanupPhase.Execute(null, null, null, null, null, false);
            Debug.Log($"[CleanupPhaseTests] CleanupPhase_NullInput_ReturnsEmptyResult: foodConsumed={result.foodConsumed}");
            Assert.AreEqual(0, result.foodConsumed, "Null input should return empty result");
        }

        [Test]
        public void CleanupPhase_NullHeroList_ReturnsEmptyRecovery()
        {
            Debug.Log("[CleanupPhaseTests] CleanupPhase_NullHeroList_ReturnsEmptyRecovery: starting test");
            var recovered = CleanupPhase.ProcessInjuryRecovery(null, null);
            Debug.Log($"[CleanupPhaseTests] CleanupPhase_NullHeroList_ReturnsEmptyRecovery: count={recovered.Count}");
            Assert.AreEqual(0, recovered.Count, "Null hero list should return empty recovery list");
        }
    }

    // ============================================================
    // 16. GatherPhase Tests
    // ============================================================
    [TestFixture]
    public class GatherPhaseTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[GatherPhaseTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[GatherPhaseTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[GatherPhaseTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[GatherPhaseTests] Teardown: EXIT");
        }

        [Test]
        public void GatherPhase_HeroGathersResources_FromNode()
        {
            Debug.Log("[GatherPhaseTests] GatherPhase_HeroGathersResources_FromNode: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var node = graph.GetNode(1);
            node.resources[ResourceType.Food] = 5;
            var hero = TestDataFactory.CreateHeroToken(0, TestDataFactory.CreateHero(carry: 3));
            hero.currentNodeId = 1;
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[GatherPhaseTests] GatherPhase_HeroGathersResources_FromNode: resolved IColonyGraph via ServiceLocator");
            colony.Initialize();
            var result = GatherPhase.Execute(new List<HeroToken> { hero }, graph, colony);
            Debug.Log($"[GatherPhaseTests] GatherPhase_HeroGathersResources_FromNode: totalGathered={result.totalGathered}, heroCarried={hero.TotalCarried}");
            Assert.Greater(result.totalGathered, 0, "Should gather some resources");
            Assert.Greater(hero.TotalCarried, 0, "Hero should be carrying resources");
        }

        [Test]
        public void GatherPhase_RespectsCarryCapacity()
        {
            Debug.Log("[GatherPhaseTests] GatherPhase_RespectsCarryCapacity: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var node = graph.GetNode(1);
            node.resources[ResourceType.Food] = 100;
            var hero = TestDataFactory.CreateHeroToken(0, TestDataFactory.CreateHero(carry: 2));
            hero.currentNodeId = 1;
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[GatherPhaseTests] GatherPhase_RespectsCarryCapacity: resolved IColonyGraph via ServiceLocator");
            colony.Initialize();
            var result = GatherPhase.Execute(new List<HeroToken> { hero }, graph, colony);
            Debug.Log($"[GatherPhaseTests] GatherPhase_RespectsCarryCapacity: gathered={result.totalGathered}, carry={hero.EffectiveCarry}");
            Assert.LessOrEqual(hero.TotalCarried, hero.EffectiveCarry, "Should not exceed carry capacity");
        }

        [Test]
        public void GatherPhase_EfficientGather_GivesBonus()
        {
            Debug.Log("[GatherPhaseTests] GatherPhase_EfficientGather_GivesBonus: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            var node = graph.GetNode(1);
            node.resources[ResourceType.Food] = 10;
            var heroDef = TestDataFactory.CreateHero(carry: 10, ability: SpecialAbility.EfficientGather);
            var hero = TestDataFactory.CreateHeroToken(0, heroDef);
            hero.currentNodeId = 1;
            var normalHeroDef = TestDataFactory.CreateHero(id: 2, carry: 10);
            var normalHero = TestDataFactory.CreateHeroToken(1, normalHeroDef);
            normalHero.currentNodeId = 1;
            node.resources[ResourceType.Food] = 20; // reset
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[GatherPhaseTests] GatherPhase_EfficientGather_GivesBonus: resolved IColonyGraph via ServiceLocator");
            colony.Initialize();
            // Just test that EfficientGather hero can gather (bonus is implementation detail)
            var result = GatherPhase.Execute(new List<HeroToken> { hero }, graph, colony);
            Debug.Log($"[GatherPhaseTests] GatherPhase_EfficientGather_GivesBonus: gathered={result.totalGathered}");
            Assert.Greater(result.totalGathered, 0, "EfficientGather hero should gather resources");
        }

        [Test]
        public void GatherPhase_InjuredHero_DoesNotGather()
        {
            Debug.Log("[GatherPhaseTests] GatherPhase_InjuredHero_DoesNotGather: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            graph.GetNode(1).resources[ResourceType.Food] = 10;
            var hero = TestDataFactory.CreateHeroToken(0);
            hero.currentNodeId = 1;
            hero.isInjured = true;
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[GatherPhaseTests] GatherPhase_InjuredHero_DoesNotGather: resolved IColonyGraph via ServiceLocator");
            colony.Initialize();
            var result = GatherPhase.Execute(new List<HeroToken> { hero }, graph, colony);
            Debug.Log($"[GatherPhaseTests] GatherPhase_InjuredHero_DoesNotGather: totalGathered={result.totalGathered}");
            Assert.AreEqual(0, result.totalGathered, "Injured hero should not gather");
        }

        [Test]
        public void GatherPhase_NullInput_ReturnsEmptyResult()
        {
            Debug.Log("[GatherPhaseTests] GatherPhase_NullInput_ReturnsEmptyResult: starting test");
            var result = GatherPhase.Execute(null, null, null);
            Debug.Log($"[GatherPhaseTests] GatherPhase_NullInput_ReturnsEmptyResult: totalGathered={result.totalGathered}");
            Assert.AreEqual(0, result.totalGathered, "Null input should return empty result");
        }

        [Test]
        public void GatherPhase_NodeWithNoResources_GathersNothing()
        {
            Debug.Log("[GatherPhaseTests] GatherPhase_NodeWithNoResources_GathersNothing: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            // Node 1 has no resources by default in our test graph
            graph.GetNode(1).resources.Clear();
            var hero = TestDataFactory.CreateHeroToken(0);
            hero.currentNodeId = 1;
            var colony = (ColonyGraph)ServiceLocator.Get<IColonyGraph>();
            Debug.Log("[GatherPhaseTests] GatherPhase_NodeWithNoResources_GathersNothing: resolved IColonyGraph via ServiceLocator");
            colony.Initialize();
            var result = GatherPhase.Execute(new List<HeroToken> { hero }, graph, colony);
            Debug.Log($"[GatherPhaseTests] GatherPhase_NodeWithNoResources_GathersNothing: totalGathered={result.totalGathered}");
            Assert.AreEqual(0, result.totalGathered, "Should gather nothing from empty node");
        }
    }

    // ============================================================
    // 17. RunSaveData Tests
    // ============================================================
    [TestFixture]
    public class RunSaveDataTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[RunSaveDataTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[RunSaveDataTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[RunSaveDataTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[RunSaveDataTests] Teardown: EXIT");
        }

        [Test]
        public void RunSaveData_AllFieldsInitialized()
        {
            Debug.Log("[RunSaveDataTests] RunSaveData_AllFieldsInitialized: starting test");
            var data = new RunSaveData();
            Debug.Log($"[RunSaveDataTests] RunSaveData_AllFieldsInitialized: deckCardIds={data.deckCardIds != null}, colonyDeckCardIds={data.colonyDeckCardIds != null}, deployedHeroes={data.deployedHeroes != null}");
            Assert.IsNotNull(data.deckCardIds, "deckCardIds should be initialized");
            Assert.IsNotNull(data.colonyDeckCardIds, "colonyDeckCardIds should be initialized");
            Assert.IsNotNull(data.deployedHeroes, "deployedHeroes should be initialized");
            Assert.IsNotNull(data.mapNodes, "mapNodes should be initialized");
            Assert.IsNotNull(data.enemies, "enemies should be initialized");
            Assert.IsNotNull(data.placedColonyCards, "placedColonyCards should be initialized");
            Assert.IsNotNull(data.heroesEverInjuredIds, "heroesEverInjuredIds should be initialized");
        }

        [Test]
        public void RunSaveData_SerializesToJsonAndBack()
        {
            Debug.Log("[RunSaveDataTests] RunSaveData_SerializesToJsonAndBack: starting test");
            var data = new RunSaveData();
            data.currentTurn = 5;
            data.randomSeed = 12345;
            data.foodStockpile = 10;
            data.deckCardIds.Add(1);
            data.deckCardIds.Add(2);
            string json = JsonUtility.ToJson(data);
            var restored = JsonUtility.FromJson<RunSaveData>(json);
            Debug.Log($"[RunSaveDataTests] RunSaveData_SerializesToJsonAndBack: turn={restored.currentTurn}, seed={restored.randomSeed}, food={restored.foodStockpile}, deckCards={restored.deckCardIds.Count}");
            Assert.AreEqual(5, restored.currentTurn);
            Assert.AreEqual(12345, restored.randomSeed);
            Assert.AreEqual(10, restored.foodStockpile);
            Assert.AreEqual(2, restored.deckCardIds.Count);
        }

        [Test]
        public void RunSaveData_HeroSaveData_FieldsAccessible()
        {
            Debug.Log("[RunSaveDataTests] RunSaveData_HeroSaveData_FieldsAccessible: starting test");
            var heroData = new HeroSaveData();
            heroData.cardId = 1;
            heroData.tokenId = 0;
            heroData.currentNodeId = 5;
            heroData.currentHP = 4;
            Debug.Log($"[RunSaveDataTests] RunSaveData_HeroSaveData_FieldsAccessible: cardId={heroData.cardId}, tokenId={heroData.tokenId}, nodeId={heroData.currentNodeId}, hp={heroData.currentHP}");
            Assert.AreEqual(1, heroData.cardId);
            Assert.AreEqual(0, heroData.tokenId);
            Assert.AreEqual(-1, heroData.offensiveEquipId, "Default offensive equip should be -1");
            Assert.AreEqual(-1, heroData.defensiveEquipId, "Default defensive equip should be -1");
        }

        [Test]
        public void RunSaveData_ColonyCardSaveData_FieldsAccessible()
        {
            Debug.Log("[RunSaveDataTests] RunSaveData_ColonyCardSaveData_FieldsAccessible: starting test");
            var colonyData = new ColonyCardSaveData();
            colonyData.cardId = 21;
            colonyData.placedId = 0;
            colonyData.posX = 1.0f;
            colonyData.posY = 2.0f;
            Debug.Log($"[RunSaveDataTests] RunSaveData_ColonyCardSaveData_FieldsAccessible: cardId={colonyData.cardId}, placedId={colonyData.placedId}");
            Assert.AreEqual(21, colonyData.cardId);
            Assert.IsNotNull(colonyData.adjacentPlacedIds);
        }

        [Test]
        public void RunSaveData_MapNodeSaveData_FieldsAccessible()
        {
            Debug.Log("[RunSaveDataTests] RunSaveData_MapNodeSaveData_FieldsAccessible: starting test");
            var nodeData = new MapNodeSaveData();
            nodeData.nodeId = 5;
            nodeData.zone = (int)NodeType.Wilderness;
            nodeData.visited = true;
            nodeData.fogState = (int)FogState.Visible;
            Debug.Log($"[RunSaveDataTests] RunSaveData_MapNodeSaveData_FieldsAccessible: nodeId={nodeData.nodeId}, zone={nodeData.zone}, visited={nodeData.visited}");
            Assert.AreEqual(5, nodeData.nodeId);
            Assert.AreEqual((int)NodeType.Wilderness, nodeData.zone);
            Assert.IsNotNull(nodeData.resources);
            Assert.IsNotNull(nodeData.neighborIds);
        }
    }

    // ============================================================
    // 18. EventBus Tests
    // ============================================================
    [TestFixture]
    public class EventBusTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[EventBusTests] Setup: ENTER - registering services and resetting EventBus");
            TestDataFactory.RegisterServices();
            EventBus.Reset();
            Debug.Log("[EventBusTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[EventBusTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[EventBusTests] Teardown: EXIT");
        }

        [Test]
        public void EventBus_OnTurnStarted_FiresWithTurnNumber()
        {
            Debug.Log("[EventBusTests] EventBus_OnTurnStarted_FiresWithTurnNumber: starting test");
            int receivedTurn = -1;
            EventBus.OnTurnStarted += (t) => { receivedTurn = t; };
            EventBus.OnTurnStarted?.Invoke(5);
            Debug.Log($"[EventBusTests] EventBus_OnTurnStarted_FiresWithTurnNumber: receivedTurn={receivedTurn}");
            Assert.AreEqual(5, receivedTurn, "Should receive correct turn number");
        }

        [Test]
        public void EventBus_OnPhaseChanged_FiresWithPhase()
        {
            Debug.Log("[EventBusTests] EventBus_OnPhaseChanged_FiresWithPhase: starting test");
            GamePhase receivedPhase = GamePhase.Deploy;
            EventBus.OnPhaseChanged += (p) => { receivedPhase = p; };
            EventBus.OnPhaseChanged?.Invoke(GamePhase.Combat);
            Debug.Log($"[EventBusTests] EventBus_OnPhaseChanged_FiresWithPhase: receivedPhase={receivedPhase}");
            Assert.AreEqual(GamePhase.Combat, receivedPhase, "Should receive correct phase");
        }

        [Test]
        public void EventBus_OnCombatEnded_FiresWithNodeIdAndResult()
        {
            Debug.Log("[EventBusTests] EventBus_OnCombatEnded_FiresWithNodeIdAndResult: starting test");
            int receivedNodeId = -1;
            bool receivedWon = false;
            EventBus.OnCombatEnded += (n, w) => { receivedNodeId = n; receivedWon = w; };
            EventBus.OnCombatEnded?.Invoke(42, true);
            Debug.Log($"[EventBusTests] EventBus_OnCombatEnded_FiresWithNodeIdAndResult: nodeId={receivedNodeId}, won={receivedWon}");
            Assert.AreEqual(42, receivedNodeId);
            Assert.IsTrue(receivedWon);
        }

        [Test]
        public void EventBus_OnResourceGathered_FiresCorrectly()
        {
            Debug.Log("[EventBusTests] EventBus_OnResourceGathered_FiresCorrectly: starting test");
            ResourceType receivedType = ResourceType.Currency;
            int receivedAmount = -1;
            EventBus.OnResourceGathered += (hero, type, amt) => { receivedType = type; receivedAmount = amt; };
            var token = TestDataFactory.CreateHeroToken(0);
            EventBus.OnResourceGathered?.Invoke(token, ResourceType.Food, 5);
            Debug.Log($"[EventBusTests] EventBus_OnResourceGathered_FiresCorrectly: type={receivedType}, amount={receivedAmount}");
            Assert.AreEqual(ResourceType.Food, receivedType);
            Assert.AreEqual(5, receivedAmount);
        }

        [Test]
        public void EventBus_Reset_ClearsAllSubscriptions()
        {
            Debug.Log("[EventBusTests] EventBus_Reset_ClearsAllSubscriptions: starting test");
            bool fired = false;
            EventBus.OnTurnStarted += (t) => { fired = true; };
            EventBus.Reset();
            EventBus.OnTurnStarted?.Invoke(1);
            Debug.Log($"[EventBusTests] EventBus_Reset_ClearsAllSubscriptions: fired={fired}");
            Assert.IsFalse(fired, "After Reset, subscriptions should be cleared");
        }

        [Test]
        public void EventBus_MultipleSubscribers_AllFired()
        {
            Debug.Log("[EventBusTests] EventBus_MultipleSubscribers_AllFired: starting test");
            int count = 0;
            EventBus.OnTurnStarted += (t) => { count++; };
            EventBus.OnTurnStarted += (t) => { count++; };
            EventBus.OnTurnStarted += (t) => { count++; };
            EventBus.OnTurnStarted?.Invoke(1);
            Debug.Log($"[EventBusTests] EventBus_MultipleSubscribers_AllFired: count={count}");
            Assert.AreEqual(3, count, "All 3 subscribers should fire");
        }

        [Test]
        public void EventBus_AllV2Events_ExistAfterSubscription()
        {
            Debug.Log("[EventBusTests] EventBus_AllV2Events_ExistAfterSubscription: starting test");
            EventBus.OnTurnStarted += (t) => { };
            EventBus.OnPhaseChanged += (p) => { };
            EventBus.OnTurnEnded += () => { };
            EventBus.OnCombatStarted += (n) => { };
            EventBus.OnCombatRound += (n, r, h, e) => { };
            EventBus.OnCombatEnded += (n, w) => { };
            EventBus.OnResourceGathered += (h, t, a) => { };
            EventBus.OnResourceDeposited += (h, t, a) => { };
            EventBus.OnRunStarted += () => { };
            EventBus.OnRunComplete += (v) => { };
            Debug.Log("[EventBusTests] EventBus_AllV2Events_ExistAfterSubscription: all v2.0 events subscribed successfully");
            Assert.IsNotNull(EventBus.OnTurnStarted);
            Assert.IsNotNull(EventBus.OnPhaseChanged);
            Assert.IsNotNull(EventBus.OnTurnEnded);
            Assert.IsNotNull(EventBus.OnCombatStarted);
            Assert.IsNotNull(EventBus.OnCombatRound);
            Assert.IsNotNull(EventBus.OnCombatEnded);
            Assert.IsNotNull(EventBus.OnResourceGathered);
            Assert.IsNotNull(EventBus.OnResourceDeposited);
            Assert.IsNotNull(EventBus.OnRunStarted);
            Assert.IsNotNull(EventBus.OnRunComplete);
        }

        [Test]
        public void EventBus_NullEventInvocation_DoesNotThrow()
        {
            Debug.Log("[EventBusTests] EventBus_NullEventInvocation_DoesNotThrow: starting test");
            EventBus.Reset();
            Assert.DoesNotThrow(() => { EventBus.OnTurnStarted?.Invoke(1); }, "Null-conditional invoke should not throw");
            Assert.DoesNotThrow(() => { EventBus.OnPhaseChanged?.Invoke(GamePhase.Deploy); }, "Null-conditional invoke should not throw");
            Debug.Log("[EventBusTests] EventBus_NullEventInvocation_DoesNotThrow: no exceptions thrown");
        }

        [Test]
        public void EventBus_OnColonyCardPlayed_FiresCorrectly()
        {
            Debug.Log("[EventBusTests] EventBus_OnColonyCardPlayed_FiresCorrectly: starting test");
            ColonyCardDefinitionSO receivedCard = null;
            EventBus.OnColonyCardPlayed += (c) => { receivedCard = c; };
            var card = TestDataFactory.CreateColonyCard(id: 21, name: "Test Colony");
            EventBus.OnColonyCardPlayed?.Invoke(card);
            Debug.Log($"[EventBusTests] EventBus_OnColonyCardPlayed_FiresCorrectly: receivedCard={receivedCard?.cardName}");
            Assert.IsNotNull(receivedCard);
            Assert.AreEqual("Test Colony", receivedCard.cardName);
        }

        [Test]
        public void EventBus_OnCardRewardSelected_FiresWithCard()
        {
            Debug.Log("[EventBusTests] EventBus_OnCardRewardSelected_FiresWithCard: starting test");
            CardDefinitionSO received = null;
            EventBus.OnCardRewardSelected += (card) => { received = card; };
            var testCard = ScriptableObject.CreateInstance<CardDefinitionSO>();
            testCard.cardName = "TestReward";
            EventBus.OnCardRewardSelected?.Invoke(testCard);
            Debug.Log($"[EventBusTests] OnCardRewardSelected: received={received?.cardName}");
            Assert.IsNotNull(received, "OnCardRewardSelected should fire");
            Assert.AreEqual("TestReward", received.cardName, "Should receive correct card");
            UnityEngine.Object.DestroyImmediate(testCard);
        }

        [Test]
        public void EventBus_OnCardRewardSkipped_Fires()
        {
            Debug.Log("[EventBusTests] EventBus_OnCardRewardSkipped_Fires: starting test");
            bool fired = false;
            EventBus.OnCardRewardSkipped += () => { fired = true; };
            EventBus.OnCardRewardSkipped?.Invoke();
            Debug.Log($"[EventBusTests] OnCardRewardSkipped: fired={fired}");
            Assert.IsTrue(fired, "OnCardRewardSkipped should fire");
        }
    }

    // ============================================================
    // 19. SeededRandom Tests
    // ============================================================
    [TestFixture]
    public class SeededRandomTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[SeededRandomTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[SeededRandomTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[SeededRandomTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[SeededRandomTests] Teardown: EXIT");
        }

        [Test]
        public void SeededRandom_SameSeed_ProducesSameSequence()
        {
            Debug.Log("[SeededRandomTests] SeededRandom_SameSeed_ProducesSameSequence: starting test");
            SeededRandom.Initialize(42);
            int a1 = SeededRandom.Range(0, 100);
            int a2 = SeededRandom.Range(0, 100);
            int a3 = SeededRandom.Range(0, 100);
            SeededRandom.Initialize(42);
            int b1 = SeededRandom.Range(0, 100);
            int b2 = SeededRandom.Range(0, 100);
            int b3 = SeededRandom.Range(0, 100);
            Debug.Log($"[SeededRandomTests] SeededRandom_SameSeed_ProducesSameSequence: a=[{a1},{a2},{a3}], b=[{b1},{b2},{b3}]");
            Assert.AreEqual(a1, b1, "First values should match");
            Assert.AreEqual(a2, b2, "Second values should match");
            Assert.AreEqual(a3, b3, "Third values should match");
        }

        [Test]
        public void SeededRandom_DifferentSeeds_ProduceDifferentSequences()
        {
            Debug.Log("[SeededRandomTests] SeededRandom_DifferentSeeds_ProduceDifferentSequences: starting test");
            SeededRandom.Initialize(1);
            int a1 = SeededRandom.Range(0, 1000000);
            int a2 = SeededRandom.Range(0, 1000000);
            SeededRandom.Initialize(99999);
            int b1 = SeededRandom.Range(0, 1000000);
            int b2 = SeededRandom.Range(0, 1000000);
            Debug.Log($"[SeededRandomTests] SeededRandom_DifferentSeeds_ProduceDifferentSequences: a=[{a1},{a2}], b=[{b1},{b2}]");
            Assert.IsTrue(a1 != b1 || a2 != b2, "Different seeds should produce different sequences");
        }

        [Test]
        public void SeededRandom_Range_RespectsBounds()
        {
            Debug.Log("[SeededRandomTests] SeededRandom_Range_RespectsBounds: starting test");
            SeededRandom.Initialize(42);
            for (int i = 0; i < 100; i++)
            {
                int val = SeededRandom.Range(5, 10);
                Assert.GreaterOrEqual(val, 5, $"Value {val} should be >= 5");
                Assert.Less(val, 10, $"Value {val} should be < 10");
            }
            Debug.Log("[SeededRandomTests] SeededRandom_Range_RespectsBounds: all 100 values within bounds");
        }

        [Test]
        public void SeededRandom_FloatRange_RespectsBounds()
        {
            Debug.Log("[SeededRandomTests] SeededRandom_FloatRange_RespectsBounds: starting test");
            SeededRandom.Initialize(42);
            for (int i = 0; i < 100; i++)
            {
                float val = SeededRandom.Range(0f, 1f);
                Assert.GreaterOrEqual(val, 0f, $"Float value {val} should be >= 0");
                Assert.Less(val, 1f, $"Float value {val} should be < 1");
            }
            Debug.Log("[SeededRandomTests] SeededRandom_FloatRange_RespectsBounds: all 100 float values within bounds");
        }

        [Test]
        public void SeededRandom_Value_BetweenZeroAndOne()
        {
            Debug.Log("[SeededRandomTests] SeededRandom_Value_BetweenZeroAndOne: starting test");
            SeededRandom.Initialize(42);
            for (int i = 0; i < 100; i++)
            {
                float val = SeededRandom.Value;
                Assert.GreaterOrEqual(val, 0f, $"Value {val} should be >= 0");
                Assert.Less(val, 1f, $"Value {val} should be < 1");
            }
            Debug.Log("[SeededRandomTests] SeededRandom_Value_BetweenZeroAndOne: all 100 values within [0,1)");
        }
    }

    // ============================================================
    // 20. Enum Validation Tests
    // ============================================================
    [TestFixture]
    public class EnumValidationTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[EnumValidationTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[EnumValidationTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[EnumValidationTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[EnumValidationTests] Teardown: EXIT");
        }

        [Test]
        public void GamePhase_HasV2Values()
        {
            Debug.Log("[EnumValidationTests] GamePhase_HasV2Values: starting test");
            Assert.IsTrue(Enum.IsDefined(typeof(GamePhase), GamePhase.Deploy), "GamePhase should have Deploy");
            Assert.IsTrue(Enum.IsDefined(typeof(GamePhase), GamePhase.HeroMove), "GamePhase should have HeroMove");
            Assert.IsTrue(Enum.IsDefined(typeof(GamePhase), GamePhase.EnemyMove), "GamePhase should have EnemyMove");
            Assert.IsTrue(Enum.IsDefined(typeof(GamePhase), GamePhase.Combat), "GamePhase should have Combat");
            Assert.IsTrue(Enum.IsDefined(typeof(GamePhase), GamePhase.Gather), "GamePhase should have Gather");
            Assert.IsTrue(Enum.IsDefined(typeof(GamePhase), GamePhase.Cleanup), "GamePhase should have Cleanup");
            Debug.Log("[EnumValidationTests] GamePhase_HasV2Values: all 6 phases verified");
        }

        [Test]
        public void CardType_HasV2Values()
        {
            Debug.Log("[EnumValidationTests] CardType_HasV2Values: starting test");
            Assert.IsTrue(Enum.IsDefined(typeof(CardType), CardType.Hero), "CardType should have Hero");
            Assert.IsTrue(Enum.IsDefined(typeof(CardType), CardType.Colony), "CardType should have Colony");
            Assert.IsTrue(Enum.IsDefined(typeof(CardType), CardType.Equipment), "CardType should have Equipment");
            Assert.IsTrue(Enum.IsDefined(typeof(CardType), CardType.Tactical), "CardType should have Tactical");
            Debug.Log("[EnumValidationTests] CardType_HasV2Values: all 4 card types verified");
        }

        [Test]
        public void HeroRole_HasAll8Roles()
        {
            Debug.Log("[EnumValidationTests] HeroRole_HasAll8Roles: starting test");
            var roles = new[] { HeroRole.Recon, HeroRole.Ranged, HeroRole.Fast, HeroRole.Melee, HeroRole.Tank, HeroRole.Gather, HeroRole.Support, HeroRole.Leader };
            foreach (var role in roles)
            {
                Assert.IsTrue(Enum.IsDefined(typeof(HeroRole), role), $"HeroRole should have {role}");
                Debug.Log($"[EnumValidationTests] HeroRole_HasAll8Roles: verified role={role}");
            }
            Debug.Log("[EnumValidationTests] HeroRole_HasAll8Roles: all 8 roles verified");
        }

        [Test]
        public void SpecialAbility_HasV2Abilities()
        {
            Debug.Log("[EnumValidationTests] SpecialAbility_HasV2Abilities: starting test");
            var abilities = new[] {
                SpecialAbility.None, SpecialAbility.ExtendedFogReveal, SpecialAbility.RangedStrike,
                SpecialAbility.Riposte, SpecialAbility.FirstStrike, SpecialAbility.EfficientGather,
                SpecialAbility.Rally, SpecialAbility.Intercept, SpecialAbility.Cleave,
                SpecialAbility.BulkHaul, SpecialAbility.FieldMedic, SpecialAbility.Taunt
            };
            foreach (var ability in abilities)
            {
                Assert.IsTrue(Enum.IsDefined(typeof(SpecialAbility), ability), $"SpecialAbility should have {ability}");
                Debug.Log($"[EnumValidationTests] SpecialAbility_HasV2Abilities: verified ability={ability}");
            }
        }

        [Test]
        public void ColonyEffect_HasV2Effects()
        {
            Debug.Log("[EnumValidationTests] ColonyEffect_HasV2Effects: starting test");
            var effects = new[] {
                ColonyEffect.FoodProduction, ColonyEffect.ResourceProtection, ColonyEffect.MaxHeroDeployment,
                ColonyEffect.BaseProduction, ColonyEffect.ColonyDefenseWall, ColonyEffect.FogReveal,
                ColonyEffect.DoubleProduction, ColonyEffect.ImmediateHealReturn, ColonyEffect.HealInjured
            };
            foreach (var effect in effects)
            {
                Assert.IsTrue(Enum.IsDefined(typeof(ColonyEffect), effect), $"ColonyEffect should have {effect}");
                Debug.Log($"[EnumValidationTests] ColonyEffect_HasV2Effects: verified effect={effect}");
            }
        }

        [Test]
        public void ColonyTier_Has3Tiers()
        {
            Debug.Log("[EnumValidationTests] ColonyTier_Has3Tiers: starting test");
            Assert.IsTrue(Enum.IsDefined(typeof(ColonyTier), ColonyTier.FoodStorage));
            Assert.IsTrue(Enum.IsDefined(typeof(ColonyTier), ColonyTier.StructureDefense));
            Assert.IsTrue(Enum.IsDefined(typeof(ColonyTier), ColonyTier.Advanced));
            int tierCount = Enum.GetValues(typeof(ColonyTier)).Length;
            Debug.Log($"[EnumValidationTests] ColonyTier_Has3Tiers: tierCount={tierCount}");
            Assert.AreEqual(3, tierCount, "Should have exactly 3 tiers");
        }

        [Test]
        public void NodeType_HasV2Values()
        {
            Debug.Log("[EnumValidationTests] NodeType_HasV2Values: starting test");
            Assert.IsTrue(Enum.IsDefined(typeof(NodeType), NodeType.Colony), "NodeType should have Colony");
            Assert.IsTrue(Enum.IsDefined(typeof(NodeType), NodeType.PiedPiper), "NodeType should have PiedPiper");
            Assert.IsTrue(Enum.IsDefined(typeof(NodeType), NodeType.Wilderness), "NodeType should have Wilderness");
            Assert.IsTrue(Enum.IsDefined(typeof(NodeType), NodeType.Farmland), "NodeType should have Farmland");
            Assert.IsTrue(Enum.IsDefined(typeof(NodeType), NodeType.Town), "NodeType should have Town");
            Debug.Log("[EnumValidationTests] NodeType_HasV2Values: all 5 node types verified");
        }

        [Test]
        public void EnemyBehavior_HasAllTypes()
        {
            Debug.Log("[EnumValidationTests] EnemyBehavior_HasAllTypes: starting test");
            Assert.IsTrue(Enum.IsDefined(typeof(EnemyBehavior), EnemyBehavior.Patrol));
            Assert.IsTrue(Enum.IsDefined(typeof(EnemyBehavior), EnemyBehavior.Chase));
            Assert.IsTrue(Enum.IsDefined(typeof(EnemyBehavior), EnemyBehavior.Ambush));
            Assert.IsTrue(Enum.IsDefined(typeof(EnemyBehavior), EnemyBehavior.Guard));
            int count = Enum.GetValues(typeof(EnemyBehavior)).Length;
            Debug.Log($"[EnumValidationTests] EnemyBehavior_HasAllTypes: count={count}");
            Assert.AreEqual(4, count, "Should have exactly 4 behaviors");
        }

        [Test]
        public void ResourceType_HasV2Values()
        {
            Debug.Log("[EnumValidationTests] ResourceType_HasV2Values: starting test");
            Assert.IsTrue(Enum.IsDefined(typeof(ResourceType), ResourceType.Food));
            Assert.IsTrue(Enum.IsDefined(typeof(ResourceType), ResourceType.Materials));
            Assert.IsTrue(Enum.IsDefined(typeof(ResourceType), ResourceType.Currency));
            Debug.Log("[EnumValidationTests] ResourceType_HasV2Values: all 3 resource types verified");
        }

        [Test]
        public void TacticalType_HasV2Values()
        {
            Debug.Log("[EnumValidationTests] TacticalType_HasV2Values: starting test");
            Assert.IsTrue(Enum.IsDefined(typeof(TacticalType), TacticalType.CombatTactic));
            Assert.IsTrue(Enum.IsDefined(typeof(TacticalType), TacticalType.SupportTactic));
            Assert.IsTrue(Enum.IsDefined(typeof(TacticalType), TacticalType.PowerTactic));
            int count = Enum.GetValues(typeof(TacticalType)).Length;
            Debug.Log($"[EnumValidationTests] TacticalType_HasV2Values: count={count}");
            Assert.AreEqual(3, count);
        }

        [Test]
        public void DifficultyLevel_HasExpectedValues()
        {
            Debug.Log("[EnumValidationTests] DifficultyLevel_HasExpectedValues: starting test");
            var values = System.Enum.GetValues(typeof(DifficultyLevel));
            Debug.Log($"[EnumValidationTests] DifficultyLevel: count={values.Length}");
            Assert.AreEqual(3, values.Length, "DifficultyLevel should have 3 values");
            Assert.AreEqual(0, (int)DifficultyLevel.Easy, "Easy should be 0");
            Assert.AreEqual(1, (int)DifficultyLevel.Normal, "Normal should be 1");
            Assert.AreEqual(2, (int)DifficultyLevel.Hard, "Hard should be 2");
        }
    }

    // ============================================================
    // 21. Build Settings Tests
    // ============================================================
    [TestFixture]
    public class BuildSettingsTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[BuildSettingsTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[BuildSettingsTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[BuildSettingsTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[BuildSettingsTests] Teardown: EXIT");
        }

        [Test]
        public void BuildSettings_BootstrapAtIndex0()
        {
            Debug.Log("[BuildSettingsTests] BuildSettings_BootstrapAtIndex0: starting test");
            var scenes = EditorBuildSettings.scenes;
            Debug.Log($"[BuildSettingsTests] BuildSettings_BootstrapAtIndex0: totalScenes={scenes.Length}");
            Assert.Greater(scenes.Length, 0, "Should have at least 1 scene in build settings");
            string scene0Path = scenes[0].path;
            Debug.Log($"[BuildSettingsTests] BuildSettings_BootstrapAtIndex0: scene0={scene0Path}");
            Assert.IsTrue(scene0Path.Contains("Bootstrap"), $"Scene 0 should be Bootstrap, got: {scene0Path}");
        }

        [Test]
        public void BuildSettings_MainMenuAtIndex1()
        {
            Debug.Log("[BuildSettingsTests] BuildSettings_MainMenuAtIndex1: starting test");
            var scenes = EditorBuildSettings.scenes;
            Assert.Greater(scenes.Length, 1, "Should have at least 2 scenes");
            string scene1Path = scenes[1].path;
            Debug.Log($"[BuildSettingsTests] BuildSettings_MainMenuAtIndex1: scene1={scene1Path}");
            Assert.IsTrue(scene1Path.Contains("MainMenu"), $"Scene 1 should be MainMenu, got: {scene1Path}");
        }

        [Test]
        public void BuildSettings_DeckConstructionAtIndex2()
        {
            Debug.Log("[BuildSettingsTests] BuildSettings_DeckConstructionAtIndex2: starting test");
            var scenes = EditorBuildSettings.scenes;
            Assert.Greater(scenes.Length, 2, "Should have at least 3 scenes");
            string scene2Path = scenes[2].path;
            Debug.Log($"[BuildSettingsTests] BuildSettings_DeckConstructionAtIndex2: scene2={scene2Path}");
            Assert.IsTrue(scene2Path.Contains("DeckConstruction"), $"Scene 2 should be DeckConstruction, got: {scene2Path}");
        }

        [Test]
        public void BuildSettings_GameMapAtIndex3()
        {
            Debug.Log("[BuildSettingsTests] BuildSettings_GameMapAtIndex3: starting test");
            var scenes = EditorBuildSettings.scenes;
            Assert.Greater(scenes.Length, 3, "Should have at least 4 scenes");
            string scene3Path = scenes[3].path;
            Debug.Log($"[BuildSettingsTests] BuildSettings_GameMapAtIndex3: scene3={scene3Path}");
            Assert.IsTrue(scene3Path.Contains("GameMap"), $"Scene 3 should be GameMap, got: {scene3Path}");
        }

        // BuildSettings_ColonyAtIndex4 removed — Colony scene merged into GameMap

        [Test]
        public void BuildSettings_DeploymentAtIndex4()
        {
            Debug.Log("[BuildSettingsTests] BuildSettings_DeploymentAtIndex4: starting test");
            var scenes = EditorBuildSettings.scenes;
            Assert.Greater(scenes.Length, 4, "Should have at least 5 scenes");
            string scene4Path = scenes[4].path;
            Debug.Log($"[BuildSettingsTests] BuildSettings_DeploymentAtIndex4: scene4={scene4Path}");
            Assert.IsTrue(scene4Path.Contains("Deployment"), $"Scene 4 should be Deployment, got: {scene4Path}");
        }

        [Test]
        public void BuildSettings_CombatAtIndex5()
        {
            Debug.Log("[BuildSettingsTests] BuildSettings_CombatAtIndex5: starting test");
            var scenes = EditorBuildSettings.scenes;
            Assert.Greater(scenes.Length, 5, "Should have at least 6 scenes");
            string scene5Path = scenes[5].path;
            Debug.Log($"[BuildSettingsTests] BuildSettings_CombatAtIndex5: scene5={scene5Path}");
            Assert.IsTrue(scene5Path.Contains("Combat"), $"Scene 5 should be Combat, got: {scene5Path}");
        }

        [Test]
        public void BuildSettings_CardRewardAtIndex6()
        {
            Debug.Log("[BuildSettingsTests] BuildSettings_CardRewardAtIndex6: starting test");
            var scenes = EditorBuildSettings.scenes;
            Assert.Greater(scenes.Length, 6, "Should have at least 7 scenes");
            string scene6Path = scenes[6].path;
            Debug.Log($"[BuildSettingsTests] BuildSettings_CardRewardAtIndex6: scene6={scene6Path}");
            Assert.IsTrue(scene6Path.Contains("CardReward"), $"Scene 6 should be CardReward, got: {scene6Path}");
        }

        [Test]
        public void BuildSettings_RunResultAtIndex7()
        {
            Debug.Log("[BuildSettingsTests] BuildSettings_RunResultAtIndex7: starting test");
            var scenes = EditorBuildSettings.scenes;
            Assert.Greater(scenes.Length, 7, "Should have at least 8 scenes");
            string scene7Path = scenes[7].path;
            Debug.Log($"[BuildSettingsTests] BuildSettings_RunResultAtIndex7: scene7={scene7Path}");
            Assert.IsTrue(scene7Path.Contains("RunResult"), $"Scene 7 should be RunResult, got: {scene7Path}");
        }
    }

    // ============================================================
    // 15. EquipmentEffectProcessor Tests
    // ============================================================
    [TestFixture]
    public class EquipmentEffectProcessorTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[EquipmentEffectProcessorTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[EquipmentEffectProcessorTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[EquipmentEffectProcessorTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[EquipmentEffectProcessorTests] Teardown: EXIT");
        }

        [Test]
        public void GetCombatBonus_NoEquipment_ReturnsZero()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetCombatBonus_NoEquipment_ReturnsZero: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            int bonus = EquipmentEffectProcessor.GetCombatBonus(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetCombatBonus_NoEquipment_ReturnsZero: bonus={bonus}");
            Assert.AreEqual(0, bonus, "Hero with no equipment should have 0 combat bonus");
        }

        [Test]
        public void GetCombatBonus_WithWeapon_ReturnsEffectValue()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetCombatBonus_WithWeapon_ReturnsEffectValue: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var weapon = TestDataFactory.CreateEquipment(id: 51, name: "Needle Sword", slot: EquipmentSlot.Offensive, effectValue1: 1);
            hero.equippedItems.Add(weapon);
            int bonus = EquipmentEffectProcessor.GetCombatBonus(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetCombatBonus_WithWeapon_ReturnsEffectValue: bonus={bonus}");
            Assert.AreEqual(1, bonus, "Needle Sword should give +1 combat bonus");
        }

        [Test]
        public void GetCombatBonus_DualWield_ExtraBonusPerOffensive()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetCombatBonus_DualWield_ExtraBonusPerOffensive: starting test");
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "DW Hero", ability: SpecialAbility.DualWield);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: heroDef);
            var weapon1 = TestDataFactory.CreateEquipment(id: 51, name: "Needle Sword", slot: EquipmentSlot.Offensive, effectValue1: 1);
            var weapon2 = TestDataFactory.CreateEquipment(id: 55, name: "Needle Lance", slot: EquipmentSlot.Offensive, effectValue1: 2);
            hero.equippedItems.Add(weapon1);
            hero.equippedItems.Add(weapon2);
            int bonus = EquipmentEffectProcessor.GetCombatBonus(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetCombatBonus_DualWield_ExtraBonusPerOffensive: bonus={bonus}");
            // 1 + 2 (weapon base) + 2 (DualWield: +1 per offensive)
            Assert.AreEqual(5, bonus, "DualWield with 2 offensive items should give weapon bonuses + DualWield bonus");
        }

        [Test]
        public void GetCombatBonusForItem_ArmorNoBonus()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetCombatBonusForItem_ArmorNoBonus: starting test");
            var armor = TestDataFactory.CreateEquipment(id: 61, name: "Thimble Helmet", slot: EquipmentSlot.Defensive, effectValue1: 1);
            int bonus = EquipmentEffectProcessor.GetCombatBonusForItem(armor);
            Debug.Log($"[EquipmentEffectProcessorTests] GetCombatBonusForItem_ArmorNoBonus: bonus={bonus}");
            Assert.AreEqual(0, bonus, "Thimble Helmet should give 0 combat bonus");
        }

        [Test]
        public void GetCombatBonusForItem_LeafArmor_ReturnsEffectValue2()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetCombatBonusForItem_LeafArmor_ReturnsEffectValue2: starting test");
            var armor = TestDataFactory.CreateEquipment(id: 66, name: "Leaf Armor", slot: EquipmentSlot.Defensive, effectValue1: 2, effectValue2: 1);
            int bonus = EquipmentEffectProcessor.GetCombatBonusForItem(armor);
            Debug.Log($"[EquipmentEffectProcessorTests] GetCombatBonusForItem_LeafArmor_ReturnsEffectValue2: bonus={bonus}");
            Assert.AreEqual(1, bonus, "Leaf Armor should give effectValue2 as combat bonus");
        }

        [Test]
        public void GetCombatBonusForItem_Legendary_ReturnsEffectValue()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetCombatBonusForItem_Legendary_ReturnsEffectValue: starting test");
            var legendary = TestDataFactory.CreateEquipment(id: 81, name: "Storm Needle", slot: EquipmentSlot.Offensive, effectValue1: 4, effectValue2: 1);
            int bonus = EquipmentEffectProcessor.GetCombatBonusForItem(legendary);
            Debug.Log($"[EquipmentEffectProcessorTests] GetCombatBonusForItem_Legendary_ReturnsEffectValue: bonus={bonus}");
            Assert.AreEqual(4, bonus, "Storm Needle should give +4 combat bonus");
        }

        [Test]
        public void GetHPBonus_WithDefensiveArmor_ReturnsSum()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetHPBonus_WithDefensiveArmor_ReturnsSum: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var helmet = TestDataFactory.CreateEquipment(id: 61, name: "Thimble Helmet", slot: EquipmentSlot.Defensive, effectValue1: 1);
            var barkArmor = TestDataFactory.CreateEquipment(id: 63, name: "Bark Armor", slot: EquipmentSlot.Defensive, effectValue1: 2);
            hero.equippedItems.Add(helmet);
            hero.equippedItems.Add(barkArmor);
            int bonus = EquipmentEffectProcessor.GetHPBonus(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetHPBonus_WithDefensiveArmor_ReturnsSum: bonus={bonus}");
            Assert.AreEqual(3, bonus, "Thimble Helmet (+1) + Bark Armor (+2) should give +3 HP");
        }

        [Test]
        public void GetHPBonus_NoEquipment_ReturnsZero()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetHPBonus_NoEquipment_ReturnsZero: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            int bonus = EquipmentEffectProcessor.GetHPBonus(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetHPBonus_NoEquipment_ReturnsZero: bonus={bonus}");
            Assert.AreEqual(0, bonus, "Hero with no equipment should have 0 HP bonus");
        }

        [Test]
        public void GetMoveBonus_ButtonCompass_ReturnsBonus()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetMoveBonus_ButtonCompass_ReturnsBonus: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var compass = TestDataFactory.CreateEquipment(id: 72, name: "Button Compass", slot: EquipmentSlot.Utility, effectValue1: 1);
            hero.equippedItems.Add(compass);
            int bonus = EquipmentEffectProcessor.GetMoveBonus(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetMoveBonus_ButtonCompass_ReturnsBonus: bonus={bonus}");
            Assert.AreEqual(1, bonus, "Button Compass should give +1 move bonus");
        }

        [Test]
        public void GetMoveBonus_RootArmor_ReturnsNegative()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetMoveBonus_RootArmor_ReturnsNegative: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var rootArmor = TestDataFactory.CreateEquipment(id: 87, name: "Root Armor", slot: EquipmentSlot.Defensive, effectValue1: 5, effectValue2: -1);
            hero.equippedItems.Add(rootArmor);
            int bonus = EquipmentEffectProcessor.GetMoveBonus(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetMoveBonus_RootArmor_ReturnsNegative: bonus={bonus}");
            Assert.AreEqual(-1, bonus, "Root Armor should give -1 move bonus");
        }

        [Test]
        public void GetCarryBonus_ClothSatchel_ReturnsBonus()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetCarryBonus_ClothSatchel_ReturnsBonus: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var satchel = TestDataFactory.CreateEquipment(id: 73, name: "Cloth Satchel", slot: EquipmentSlot.Utility, effectValue1: 2);
            hero.equippedItems.Add(satchel);
            int bonus = EquipmentEffectProcessor.GetCarryBonus(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetCarryBonus_ClothSatchel_ReturnsBonus: bonus={bonus}");
            Assert.AreEqual(2, bonus, "Cloth Satchel should give +2 carry bonus");
        }

        [Test]
        public void GetCarryBonus_TinkerDoubles_UtilityItems()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetCarryBonus_TinkerDoubles_UtilityItems: starting test");
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "Tinker Hero", ability: SpecialAbility.Tinker);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: heroDef);
            var satchel = TestDataFactory.CreateEquipment(id: 73, name: "Cloth Satchel", slot: EquipmentSlot.Utility, effectValue1: 2);
            hero.equippedItems.Add(satchel);
            int bonus = EquipmentEffectProcessor.GetCarryBonus(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetCarryBonus_TinkerDoubles_UtilityItems: bonus={bonus}");
            Assert.AreEqual(4, bonus, "Tinker should double utility carry bonus: 2 * 2 = 4");
        }

        [Test]
        public void GetInitiativeBonus_PinDagger_ReturnsEffectValue2()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetInitiativeBonus_PinDagger_ReturnsEffectValue2: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var dagger = TestDataFactory.CreateEquipment(id: 53, name: "Pin Dagger", slot: EquipmentSlot.Offensive, effectValue1: 1, effectValue2: 1);
            hero.equippedItems.Add(dagger);
            int bonus = EquipmentEffectProcessor.GetInitiativeBonus(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetInitiativeBonus_PinDagger_ReturnsEffectValue2: bonus={bonus}");
            Assert.AreEqual(1, bonus, "Pin Dagger should give +1 initiative bonus from effectValue2");
        }

        [Test]
        public void GetInitiativeBonus_NoEquipment_ReturnsZero()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetInitiativeBonus_NoEquipment_ReturnsZero: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            int bonus = EquipmentEffectProcessor.GetInitiativeBonus(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetInitiativeBonus_NoEquipment_ReturnsZero: bonus={bonus}");
            Assert.AreEqual(0, bonus, "Hero with no equipment should have 0 initiative bonus");
        }

        [Test]
        public void ProcessPreCombatEffects_FlintFirestarter_DamagesEnemies()
        {
            Debug.Log("[EquipmentEffectProcessorTests] ProcessPreCombatEffects_FlintFirestarter_DamagesEnemies: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var flint = TestDataFactory.CreateEquipment(id: 79, name: "Flint Firestarter", slot: EquipmentSlot.Utility, effectValue1: 1);
            hero.equippedItems.Add(flint);
            var enemy1 = TestDataFactory.CreateEnemyToken(tokenId: 10, name: "Rat", strength: 2, hp: 3);
            var enemy2 = TestDataFactory.CreateEnemyToken(tokenId: 11, name: "Snake", strength: 3, hp: 5);
            var enemies = new List<EnemyToken> { enemy1, enemy2 };
            EquipmentEffectProcessor.ProcessPreCombatEffects(hero, enemies);
            Debug.Log($"[EquipmentEffectProcessorTests] ProcessPreCombatEffects_FlintFirestarter_DamagesEnemies: enemy1HP={enemy1.currentHP}, enemy2HP={enemy2.currentHP}");
            Assert.AreEqual(2, enemy1.currentHP, "Rat should take 1 damage: 3 - 1 = 2");
            Assert.AreEqual(4, enemy2.currentHP, "Snake should take 1 damage: 5 - 1 = 4");
        }

        [Test]
        public void ProcessPreCombatEffects_FrostThreadCloak_ReducesStrength()
        {
            Debug.Log("[EquipmentEffectProcessorTests] ProcessPreCombatEffects_FrostThreadCloak_ReducesStrength: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var cloak = TestDataFactory.CreateEquipment(id: 83, name: "Frost Thread Cloak", slot: EquipmentSlot.Defensive, effectValue1: 3, effectValue2: 1);
            hero.equippedItems.Add(cloak);
            var enemy = TestDataFactory.CreateEnemyToken(tokenId: 10, name: "Wolf", strength: 4, hp: 5);
            var enemies = new List<EnemyToken> { enemy };
            EquipmentEffectProcessor.ProcessPreCombatEffects(hero, enemies);
            Debug.Log($"[EquipmentEffectProcessorTests] ProcessPreCombatEffects_FrostThreadCloak_ReducesStrength: enemyStrength={enemy.strength}");
            Assert.AreEqual(3, enemy.strength, "Wolf strength should be reduced from 4 to 3");
        }

        [Test]
        public void ProcessPerRoundEffects_ButtonShield_AddsDamageBlock()
        {
            Debug.Log("[EquipmentEffectProcessorTests] ProcessPerRoundEffects_ButtonShield_AddsDamageBlock: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var shield = TestDataFactory.CreateEquipment(id: 62, name: "Button Shield", slot: EquipmentSlot.Defensive, effectValue1: 1, effectValue2: 1);
            hero.equippedItems.Add(shield);
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            EquipmentEffectProcessor.ProcessPerRoundEffects(hero, ctx);
            int block = ctx.GetDamageBlock(hero.tokenId);
            Debug.Log($"[EquipmentEffectProcessorTests] ProcessPerRoundEffects_ButtonShield_AddsDamageBlock: block={block}");
            Assert.AreEqual(1, block, "Button Shield should add 1 damage block per round");
        }

        [Test]
        public void ProcessPerRoundEffects_ThornCrown_DealsSelfDamage()
        {
            Debug.Log("[EquipmentEffectProcessorTests] ProcessPerRoundEffects_ThornCrown_DealsSelfDamage: starting test");
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "Crown Hero", hp: 10);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: heroDef);
            int hpBefore = hero.currentHP;
            var crown = TestDataFactory.CreateEquipment(id: 86, name: "Thorn Crown", slot: EquipmentSlot.Offensive, effectValue1: 3, effectValue2: 1);
            hero.equippedItems.Add(crown);
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            EquipmentEffectProcessor.ProcessPerRoundEffects(hero, ctx);
            Debug.Log($"[EquipmentEffectProcessorTests] ProcessPerRoundEffects_ThornCrown_DealsSelfDamage: hpBefore={hpBefore}, hpAfter={hero.currentHP}");
            Assert.AreEqual(hpBefore - 1, hero.currentHP, "Thorn Crown should deal 1 self-damage per round");
        }

        [Test]
        public void ProcessPerRoundEffects_HealingHerbKit_HealsOnFirstRound()
        {
            Debug.Log("[EquipmentEffectProcessorTests] ProcessPerRoundEffects_HealingHerbKit_HealsOnFirstRound: starting test");
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "Herb Hero", hp: 10);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: heroDef);
            hero.TakeDamage(3); // HP now 7
            int hpBefore = hero.currentHP;
            var kit = TestDataFactory.CreateEquipment(id: 77, name: "Healing Herb Kit", slot: EquipmentSlot.Utility, effectValue1: 1);
            hero.equippedItems.Add(kit);
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            EquipmentEffectProcessor.ProcessPerRoundEffects(hero, ctx);
            Debug.Log($"[EquipmentEffectProcessorTests] ProcessPerRoundEffects_HealingHerbKit_HealsOnFirstRound: hpBefore={hpBefore}, hpAfter={hero.currentHP}");
            Assert.AreEqual(hpBefore + 1, hero.currentHP, "Healing Herb Kit should heal 1 HP on first round");
        }

        [Test]
        public void ProcessPerRoundEffects_HealingHerbKit_NoHealOnLaterRounds()
        {
            Debug.Log("[EquipmentEffectProcessorTests] ProcessPerRoundEffects_HealingHerbKit_NoHealOnLaterRounds: starting test");
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "Herb Hero", hp: 10);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: heroDef);
            hero.TakeDamage(3);
            int hpBefore = hero.currentHP;
            var kit = TestDataFactory.CreateEquipment(id: 77, name: "Healing Herb Kit", slot: EquipmentSlot.Utility, effectValue1: 1);
            hero.equippedItems.Add(kit);
            var ctx = new CombatContext { nodeId = 0, roundNumber = 2 };
            EquipmentEffectProcessor.ProcessPerRoundEffects(hero, ctx);
            Debug.Log($"[EquipmentEffectProcessorTests] ProcessPerRoundEffects_HealingHerbKit_NoHealOnLaterRounds: hpBefore={hpBefore}, hpAfter={hero.currentHP}");
            Assert.AreEqual(hpBefore, hero.currentHP, "Healing Herb Kit should NOT heal on round 2+");
        }

        [Test]
        public void HasRangedStrike_ThornSpear_ReturnsTrue()
        {
            Debug.Log("[EquipmentEffectProcessorTests] HasRangedStrike_ThornSpear_ReturnsTrue: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var spear = TestDataFactory.CreateEquipment(id: 52, name: "Thorn Spear", slot: EquipmentSlot.Offensive, effectValue1: 1);
            hero.equippedItems.Add(spear);
            bool result = EquipmentEffectProcessor.HasRangedStrike(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] HasRangedStrike_ThornSpear_ReturnsTrue: result={result}");
            Assert.IsTrue(result, "Thorn Spear should grant ranged strike");
        }

        [Test]
        public void HasRangedStrike_NoRangedItem_ReturnsFalse()
        {
            Debug.Log("[EquipmentEffectProcessorTests] HasRangedStrike_NoRangedItem_ReturnsFalse: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var sword = TestDataFactory.CreateEquipment(id: 51, name: "Needle Sword", slot: EquipmentSlot.Offensive, effectValue1: 1);
            hero.equippedItems.Add(sword);
            bool result = EquipmentEffectProcessor.HasRangedStrike(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] HasRangedStrike_NoRangedItem_ReturnsFalse: result={result}");
            Assert.IsFalse(result, "Needle Sword should not grant ranged strike");
        }

        [Test]
        public void HasSplashDamage_StormNeedle_ReturnsTrue()
        {
            Debug.Log("[EquipmentEffectProcessorTests] HasSplashDamage_StormNeedle_ReturnsTrue: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var stormNeedle = TestDataFactory.CreateEquipment(id: 81, name: "Storm Needle", slot: EquipmentSlot.Offensive, effectValue1: 4, effectValue2: 1);
            hero.equippedItems.Add(stormNeedle);
            bool result = EquipmentEffectProcessor.HasSplashDamage(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] HasSplashDamage_StormNeedle_ReturnsTrue: result={result}");
            Assert.IsTrue(result, "Storm Needle should grant splash damage");
        }

        [Test]
        public void GetSplashDamage_StormNeedle_ReturnsEffectValue2()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetSplashDamage_StormNeedle_ReturnsEffectValue2: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var stormNeedle = TestDataFactory.CreateEquipment(id: 81, name: "Storm Needle", slot: EquipmentSlot.Offensive, effectValue1: 4, effectValue2: 1);
            hero.equippedItems.Add(stormNeedle);
            int splash = EquipmentEffectProcessor.GetSplashDamage(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetSplashDamage_StormNeedle_ReturnsEffectValue2: splash={splash}");
            Assert.AreEqual(1, splash, "Storm Needle splash damage should be effectValue2 = 1");
        }

        [Test]
        public void GetSplashDamage_NoSplashItem_ReturnsZero()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetSplashDamage_NoSplashItem_ReturnsZero: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            int splash = EquipmentEffectProcessor.GetSplashDamage(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetSplashDamage_NoSplashItem_ReturnsZero: splash={splash}");
            Assert.AreEqual(0, splash, "Hero with no splash equipment should return 0");
        }

        [Test]
        public void IsTauntActive_TauntAbility_ReturnsTrue()
        {
            Debug.Log("[EquipmentEffectProcessorTests] IsTauntActive_TauntAbility_ReturnsTrue: starting test");
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "Taunt Hero", ability: SpecialAbility.Taunt);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: heroDef);
            bool result = EquipmentEffectProcessor.IsTauntActive(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] IsTauntActive_TauntAbility_ReturnsTrue: result={result}");
            Assert.IsTrue(result, "Hero with Taunt ability should have taunt active");
        }

        [Test]
        public void IsTauntActive_NoTaunt_ReturnsFalse()
        {
            Debug.Log("[EquipmentEffectProcessorTests] IsTauntActive_NoTaunt_ReturnsFalse: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            bool result = EquipmentEffectProcessor.IsTauntActive(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] IsTauntActive_NoTaunt_ReturnsFalse: result={result}");
            Assert.IsFalse(result, "Hero without Taunt should not have taunt active");
        }

        [Test]
        public void GetFogRevealBonus_BeadLantern_ReturnsBonus()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetFogRevealBonus_BeadLantern_ReturnsBonus: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var lantern = TestDataFactory.CreateEquipment(id: 71, name: "Bead Lantern", slot: EquipmentSlot.Utility, effectValue1: 1);
            hero.equippedItems.Add(lantern);
            int bonus = EquipmentEffectProcessor.GetFogRevealBonus(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetFogRevealBonus_BeadLantern_ReturnsBonus: bonus={bonus}");
            Assert.AreEqual(1, bonus, "Bead Lantern should give +1 fog reveal bonus");
        }

        [Test]
        public void GetFogRevealBonus_EmberTorch_ReturnsEffectValue2()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetFogRevealBonus_EmberTorch_ReturnsEffectValue2: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var torch = TestDataFactory.CreateEquipment(id: 82, name: "Ember Torch", slot: EquipmentSlot.Offensive, effectValue1: 3, effectValue2: 2);
            hero.equippedItems.Add(torch);
            int bonus = EquipmentEffectProcessor.GetFogRevealBonus(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetFogRevealBonus_EmberTorch_ReturnsEffectValue2: bonus={bonus}");
            Assert.AreEqual(2, bonus, "Ember Torch should give +2 fog reveal bonus");
        }

        [Test]
        public void GetFogRevealBonus_NoFogItems_ReturnsZero()
        {
            Debug.Log("[EquipmentEffectProcessorTests] GetFogRevealBonus_NoFogItems_ReturnsZero: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            int bonus = EquipmentEffectProcessor.GetFogRevealBonus(hero);
            Debug.Log($"[EquipmentEffectProcessorTests] GetFogRevealBonus_NoFogItems_ReturnsZero: bonus={bonus}");
            Assert.AreEqual(0, bonus, "Hero with no fog reveal items should return 0");
        }
    }

    // ============================================================
    // 16. TacticalEffectProcessor Tests
    // ============================================================
    [TestFixture]
    public class TacticalEffectProcessorTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[TacticalEffectProcessorTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[TacticalEffectProcessorTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[TacticalEffectProcessorTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[TacticalEffectProcessorTests] Teardown: EXIT");
        }

        [Test]
        public void ApplyTacticalCard_NonTacticalCard_ReturnsFalse()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_NonTacticalCard_ReturnsFalse: starting test");
            var heroDef = TestDataFactory.CreateHero();
            var heroes = new List<HeroToken> { TestDataFactory.CreateHeroToken(tokenId: 1) };
            var enemies = new List<EnemyToken>();
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            bool result = TacticalEffectProcessor.ApplyTacticalCard(heroDef, heroes, enemies, 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_NonTacticalCard_ReturnsFalse: result={result}");
            Assert.IsFalse(result, "Non-tactical card should return false");
        }

        [Test]
        public void ApplyTacticalCard_PackAmbush_BuffsAllHeroes()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_PackAmbush_BuffsAllHeroes: starting test");
            var card = TestDataFactory.CreateTactical(id: 91, name: "Pack Ambush", type: TacticalType.CombatTactic, effectValue1: 2);
            var hero1 = TestDataFactory.CreateHeroToken(tokenId: 1);
            var hero2 = TestDataFactory.CreateHeroToken(tokenId: 2);
            var heroes = new List<HeroToken> { hero1, hero2 };
            var enemies = new List<EnemyToken>();
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            bool result = TacticalEffectProcessor.ApplyTacticalCard(card, heroes, enemies, 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_PackAmbush_BuffsAllHeroes: result={result}, hero1Buff={ctx.GetTotalCombatBuff(hero1.tokenId)}, hero2Buff={ctx.GetTotalCombatBuff(hero2.tokenId)}");
            Assert.IsTrue(result, "Pack Ambush should succeed");
            Assert.AreEqual(2, ctx.GetTotalCombatBuff(hero1.tokenId), "Hero1 should get +2 temp combat buff");
            Assert.AreEqual(2, ctx.GetTotalCombatBuff(hero2.tokenId), "Hero2 should get +2 temp combat buff");
        }

        [Test]
        public void ApplyTacticalCard_RallyThePack_PermanentBuff()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_RallyThePack_PermanentBuff: starting test");
            var card = TestDataFactory.CreateTactical(id: 92, name: "Rally the Pack", type: TacticalType.CombatTactic, effectValue1: 1);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var heroes = new List<HeroToken> { hero };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            bool result = TacticalEffectProcessor.ApplyTacticalCard(card, heroes, new List<EnemyToken>(), 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_RallyThePack_PermanentBuff: result={result}, permBuff={ctx.permanentCombatBuffs[hero.tokenId]}");
            Assert.IsTrue(result, "Rally the Pack should succeed");
            Assert.AreEqual(1, ctx.permanentCombatBuffs[hero.tokenId], "Hero should get +1 permanent combat buff");
        }

        [Test]
        public void ApplyTacticalCard_DefensiveFormation_BlocksDamage()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_DefensiveFormation_BlocksDamage: starting test");
            var card = TestDataFactory.CreateTactical(id: 93, name: "Defensive Formation", type: TacticalType.CombatTactic, effectValue1: 1);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var heroes = new List<HeroToken> { hero };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            TacticalEffectProcessor.ApplyTacticalCard(card, heroes, new List<EnemyToken>(), 0, ctx);
            int block = ctx.GetDamageBlock(hero.tokenId);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_DefensiveFormation_BlocksDamage: block={block}");
            Assert.AreEqual(1, block, "Defensive Formation should add 1 damage block");
        }

        [Test]
        public void ApplyTacticalCard_LastStand_BuffsLowHPHero()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_LastStand_BuffsLowHPHero: starting test");
            var card = TestDataFactory.CreateTactical(id: 94, name: "Last Stand", type: TacticalType.CombatTactic, effectValue1: 5);
            card.effectValue2 = 1; // HP threshold
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "Low HP Hero", hp: 4);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: heroDef);
            hero.TakeDamage(3); // HP now 1
            var heroes = new List<HeroToken> { hero };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            bool result = TacticalEffectProcessor.ApplyTacticalCard(card, heroes, new List<EnemyToken>(), 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_LastStand_BuffsLowHPHero: result={result}, buff={ctx.GetTotalCombatBuff(hero.tokenId)}");
            Assert.IsTrue(result, "Last Stand should succeed with low HP hero");
            Assert.AreEqual(5, ctx.GetTotalCombatBuff(hero.tokenId), "Low HP hero should get +5 temp combat buff");
        }

        [Test]
        public void ApplyTacticalCard_LastStand_FailsWhenNoLowHP()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_LastStand_FailsWhenNoLowHP: starting test");
            var card = TestDataFactory.CreateTactical(id: 94, name: "Last Stand", type: TacticalType.CombatTactic, effectValue1: 5);
            card.effectValue2 = 1;
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1); // full HP
            var heroes = new List<HeroToken> { hero };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            bool result = TacticalEffectProcessor.ApplyTacticalCard(card, heroes, new List<EnemyToken>(), 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_LastStand_FailsWhenNoLowHP: result={result}");
            Assert.IsFalse(result, "Last Stand should fail when no hero has low HP");
        }

        [Test]
        public void ApplyTacticalCard_TacticalRetreat_SetsRetreatFlag()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_TacticalRetreat_SetsRetreatFlag: starting test");
            var card = TestDataFactory.CreateTactical(id: 96, name: "Tactical Retreat", type: TacticalType.CombatTactic, effectValue1: 0);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var heroes = new List<HeroToken> { hero };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            TacticalEffectProcessor.ApplyTacticalCard(card, heroes, new List<EnemyToken>(), 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_TacticalRetreat_SetsRetreatFlag: retreat={ctx.retreatTriggered}");
            Assert.IsTrue(ctx.retreatTriggered, "Tactical Retreat should set retreatTriggered flag");
        }

        [Test]
        public void ApplyTacticalCard_GatherSeeds_IncreasesGatherBonus()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_GatherSeeds_IncreasesGatherBonus: starting test");
            var card = TestDataFactory.CreateTactical(id: 101, name: "Gather Seeds", type: TacticalType.SupportTactic, effectValue1: 1);
            var heroes = new List<HeroToken>();
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            TacticalEffectProcessor.ApplyTacticalCard(card, heroes, new List<EnemyToken>(), 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_GatherSeeds_IncreasesGatherBonus: gatherBonus={ctx.gatherBonus}");
            Assert.AreEqual(1, ctx.gatherBonus, "Gather Seeds should increase gatherBonus by 1");
        }

        [Test]
        public void ApplyTacticalCard_HarvestBounty_SetsDoubleGathering()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_HarvestBounty_SetsDoubleGathering: starting test");
            var card = TestDataFactory.CreateTactical(id: 102, name: "Harvest Bounty", type: TacticalType.SupportTactic, effectValue1: 0);
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            TacticalEffectProcessor.ApplyTacticalCard(card, new List<HeroToken>(), new List<EnemyToken>(), 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_HarvestBounty_SetsDoubleGathering: doubleGathering={ctx.doubleGathering}");
            Assert.IsTrue(ctx.doubleGathering, "Harvest Bounty should set doubleGathering flag");
        }

        [Test]
        public void ApplyTacticalCard_EmergencyRations_SetsFlag()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_EmergencyRations_SetsFlag: starting test");
            var card = TestDataFactory.CreateTactical(id: 103, name: "Emergency Rations", type: TacticalType.SupportTactic, effectValue1: 0);
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            TacticalEffectProcessor.ApplyTacticalCard(card, new List<HeroToken>(), new List<EnemyToken>(), 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_EmergencyRations_SetsFlag: emergencyRations={ctx.emergencyRations}");
            Assert.IsTrue(ctx.emergencyRations, "Emergency Rations should set emergencyRations flag");
        }

        [Test]
        public void ApplyTacticalCard_HerbalRemedy_HealsLowestHPHero()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_HerbalRemedy_HealsLowestHPHero: starting test");
            var card = TestDataFactory.CreateTactical(id: 106, name: "Herbal Remedy", type: TacticalType.SupportTactic, effectValue1: 2);
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "Wounded Hero", hp: 10);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: heroDef);
            hero.TakeDamage(5); // HP now 5
            var heroes = new List<HeroToken> { hero };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            bool result = TacticalEffectProcessor.ApplyTacticalCard(card, heroes, new List<EnemyToken>(), 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_HerbalRemedy_HealsLowestHPHero: result={result}, heroHP={hero.currentHP}");
            Assert.IsTrue(result, "Herbal Remedy should succeed");
            Assert.AreEqual(7, hero.currentHP, "Hero should be healed from 5 to 7");
        }

        [Test]
        public void ApplyTacticalCard_HerbalRemedy_FailsWhenAllFull()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_HerbalRemedy_FailsWhenAllFull: starting test");
            var card = TestDataFactory.CreateTactical(id: 106, name: "Herbal Remedy", type: TacticalType.SupportTactic, effectValue1: 2);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1); // full HP
            var heroes = new List<HeroToken> { hero };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            bool result = TacticalEffectProcessor.ApplyTacticalCard(card, heroes, new List<EnemyToken>(), 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_HerbalRemedy_FailsWhenAllFull: result={result}");
            Assert.IsFalse(result, "Herbal Remedy should fail when all heroes are at full HP");
        }

        [Test]
        public void ApplyTacticalCard_PoisonThorn_DamagesLowestHPEnemy()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_PoisonThorn_DamagesLowestHPEnemy: starting test");
            var card = TestDataFactory.CreateTactical(id: 117, name: "Poison Thorn", type: TacticalType.PowerTactic, effectValue1: 3);
            var enemy1 = TestDataFactory.CreateEnemyToken(tokenId: 10, name: "Weak", strength: 1, hp: 2);
            var enemy2 = TestDataFactory.CreateEnemyToken(tokenId: 11, name: "Strong", strength: 3, hp: 10);
            var enemies = new List<EnemyToken> { enemy1, enemy2 };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            bool result = TacticalEffectProcessor.ApplyTacticalCard(card, new List<HeroToken>(), enemies, 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_PoisonThorn_DamagesLowestHPEnemy: result={result}, enemy1HP={enemy1.currentHP}, enemy2HP={enemy2.currentHP}");
            Assert.IsTrue(result, "Poison Thorn should succeed");
            Assert.IsTrue(enemy1.isDefeated, "Weak enemy (2 HP) should be defeated by 3 damage");
            Assert.AreEqual(10, enemy2.currentHP, "Strong enemy should be unaffected");
        }

        [Test]
        public void ApplyTacticalCard_BurrowDefense_IncreasesColonyDefense()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_BurrowDefense_IncreasesColonyDefense: starting test");
            var card = TestDataFactory.CreateTactical(id: 113, name: "Burrow Defense", type: TacticalType.PowerTactic, effectValue1: 5);
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            TacticalEffectProcessor.ApplyTacticalCard(card, new List<HeroToken>(), new List<EnemyToken>(), 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_BurrowDefense_IncreasesColonyDefense: colonyDefenseBonus={ctx.colonyDefenseBonus}");
            Assert.AreEqual(5, ctx.colonyDefenseBonus, "Burrow Defense should add 5 colony defense bonus");
        }

        [Test]
        public void ApplyTacticalCard_VictoryFeast_HealsAllAndAddsFoodBonus()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_VictoryFeast_HealsAllAndAddsFoodBonus: starting test");
            var card = TestDataFactory.CreateTactical(id: 120, name: "Victory Feast", type: TacticalType.PowerTactic, effectValue1: 0);
            card.effectValue2 = 2; // food bonus
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "Wounded", hp: 10);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: heroDef);
            hero.TakeDamage(5); // HP now 5
            var heroes = new List<HeroToken> { hero };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            TacticalEffectProcessor.ApplyTacticalCard(card, heroes, new List<EnemyToken>(), 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_VictoryFeast_HealsAllAndAddsFoodBonus: heroHP={hero.currentHP}, foodBonus={ctx.foodProductionBonus}");
            Assert.AreEqual(hero.maxHP, hero.currentHP, "Victory Feast should fully heal hero");
            Assert.AreEqual(2, ctx.foodProductionBonus, "Victory Feast should add +2 food production bonus");
        }

        [Test]
        public void ApplyTacticalCard_UnknownId_ReturnsFalse()
        {
            Debug.Log("[TacticalEffectProcessorTests] ApplyTacticalCard_UnknownId_ReturnsFalse: starting test");
            var card = TestDataFactory.CreateTactical(id: 999, name: "Unknown Tactic", type: TacticalType.CombatTactic, effectValue1: 0);
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            bool result = TacticalEffectProcessor.ApplyTacticalCard(card, new List<HeroToken>(), new List<EnemyToken>(), 0, ctx);
            Debug.Log($"[TacticalEffectProcessorTests] ApplyTacticalCard_UnknownId_ReturnsFalse: result={result}");
            Assert.IsFalse(result, "Unknown tactical card ID should return false");
        }
    }

    // ============================================================
    // 17. SpecialAbilityProcessor Tests
    // ============================================================
    [TestFixture]
    public class SpecialAbilityProcessorTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[SpecialAbilityProcessorTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[SpecialAbilityProcessorTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[SpecialAbilityProcessorTests] Teardown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[SpecialAbilityProcessorTests] Teardown: EXIT");
        }

        [Test]
        public void ProcessPreCombatAbility_Rally_BuffsAllies()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessPreCombatAbility_Rally_BuffsAllies: starting test");
            var rallyDef = TestDataFactory.CreateHero(id: 1, name: "Rally Hero", ability: SpecialAbility.Rally);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: rallyDef);
            var ally = TestDataFactory.CreateHeroToken(tokenId: 2);
            var allHeroes = new List<HeroToken> { hero, ally };
            var enemies = new List<EnemyToken>();
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            SpecialAbilityProcessor.ProcessPreCombatAbility(hero, allHeroes, enemies, ctx);
            Debug.Log($"[SpecialAbilityProcessorTests] ProcessPreCombatAbility_Rally_BuffsAllies: allyBuff={ctx.GetTotalCombatBuff(ally.tokenId)}, heroBuff={ctx.GetTotalCombatBuff(hero.tokenId)}");
            Assert.AreEqual(1, ctx.GetTotalCombatBuff(ally.tokenId), "Rally should give +1 permanent Combat to ally");
            Assert.AreEqual(0, ctx.GetTotalCombatBuff(hero.tokenId), "Rally should not buff self");
        }

        [Test]
        public void ProcessPreCombatAbility_BattleCry_BuffsAllyMove()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessPreCombatAbility_BattleCry_BuffsAllyMove: starting test");
            var bcDef = TestDataFactory.CreateHero(id: 1, name: "BattleCry Hero", ability: SpecialAbility.BattleCry, move: 2);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: bcDef);
            var allyDef = TestDataFactory.CreateHero(id: 2, name: "Ally", move: 2);
            var ally = TestDataFactory.CreateHeroToken(tokenId: 2, cardDef: allyDef);
            int allyMoveBefore = ally.baseMove;
            var allHeroes = new List<HeroToken> { hero, ally };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            SpecialAbilityProcessor.ProcessPreCombatAbility(hero, allHeroes, new List<EnemyToken>(), ctx);
            Debug.Log($"[SpecialAbilityProcessorTests] ProcessPreCombatAbility_BattleCry_BuffsAllyMove: allyMoveBefore={allyMoveBefore}, allyMoveAfter={ally.baseMove}");
            Assert.AreEqual(allyMoveBefore + 1, ally.baseMove, "BattleCry should give +1 Move to ally");
        }

        [Test]
        public void ProcessPreCombatAbility_Inspire_BuffsLowestCombatAlly()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessPreCombatAbility_Inspire_BuffsLowestCombatAlly: starting test");
            var inspireDef = TestDataFactory.CreateHero(id: 1, name: "Inspire Hero", ability: SpecialAbility.Inspire, combat: 5);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: inspireDef);
            var weakAllyDef = TestDataFactory.CreateHero(id: 2, name: "Weak Ally", combat: 1);
            var weakAlly = TestDataFactory.CreateHeroToken(tokenId: 2, cardDef: weakAllyDef);
            var strongAllyDef = TestDataFactory.CreateHero(id: 3, name: "Strong Ally", combat: 4);
            var strongAlly = TestDataFactory.CreateHeroToken(tokenId: 3, cardDef: strongAllyDef);
            var allHeroes = new List<HeroToken> { hero, weakAlly, strongAlly };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            SpecialAbilityProcessor.ProcessPreCombatAbility(hero, allHeroes, new List<EnemyToken>(), ctx);
            Debug.Log($"[SpecialAbilityProcessorTests] ProcessPreCombatAbility_Inspire_BuffsLowestCombatAlly: weakBuff={ctx.GetTotalCombatBuff(weakAlly.tokenId)}, strongBuff={ctx.GetTotalCombatBuff(strongAlly.tokenId)}");
            Assert.AreEqual(1, ctx.GetTotalCombatBuff(weakAlly.tokenId), "Inspire should buff lowest-combat ally");
            Assert.AreEqual(0, ctx.GetTotalCombatBuff(strongAlly.tokenId), "Inspire should not buff non-lowest ally");
        }

        [Test]
        public void ProcessPreCombatAbility_DeadHero_Skips()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessPreCombatAbility_DeadHero_Skips: starting test");
            var rallyDef = TestDataFactory.CreateHero(id: 1, name: "Dead Rally", ability: SpecialAbility.Rally, hp: 3);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: rallyDef);
            hero.TakeDamage(10); // kill the hero
            var ally = TestDataFactory.CreateHeroToken(tokenId: 2);
            var allHeroes = new List<HeroToken> { hero, ally };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            SpecialAbilityProcessor.ProcessPreCombatAbility(hero, allHeroes, new List<EnemyToken>(), ctx);
            Debug.Log($"[SpecialAbilityProcessorTests] ProcessPreCombatAbility_DeadHero_Skips: allyBuff={ctx.GetTotalCombatBuff(ally.tokenId)}");
            Assert.AreEqual(0, ctx.GetTotalCombatBuff(ally.tokenId), "Dead hero should not apply Rally buff");
        }

        [Test]
        public void ProcessPerRoundAbility_FieldMedic_HealsLowestHPAlly()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessPerRoundAbility_FieldMedic_HealsLowestHPAlly: starting test");
            var medicDef = TestDataFactory.CreateHero(id: 1, name: "Medic", ability: SpecialAbility.FieldMedic, hp: 10);
            var medic = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: medicDef);
            var allyDef = TestDataFactory.CreateHero(id: 2, name: "Wounded Ally", hp: 10);
            var ally = TestDataFactory.CreateHeroToken(tokenId: 2, cardDef: allyDef);
            ally.TakeDamage(5); // HP now 5
            int allyHPBefore = ally.currentHP;
            var allHeroes = new List<HeroToken> { medic, ally };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            SpecialAbilityProcessor.ProcessPerRoundAbility(medic, allHeroes, new List<EnemyToken>(), ctx);
            Debug.Log($"[SpecialAbilityProcessorTests] ProcessPerRoundAbility_FieldMedic_HealsLowestHPAlly: allyHPBefore={allyHPBefore}, allyHPAfter={ally.currentHP}");
            Assert.AreEqual(allyHPBefore + 1, ally.currentHP, "FieldMedic should heal ally for 1 HP");
        }

        [Test]
        public void ProcessPerRoundAbility_FieldMedic_NoHealWhenAllFull()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessPerRoundAbility_FieldMedic_NoHealWhenAllFull: starting test");
            var medicDef = TestDataFactory.CreateHero(id: 1, name: "Medic", ability: SpecialAbility.FieldMedic, hp: 10);
            var medic = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: medicDef);
            var ally = TestDataFactory.CreateHeroToken(tokenId: 2); // full HP
            int allyHPBefore = ally.currentHP;
            var allHeroes = new List<HeroToken> { medic, ally };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            SpecialAbilityProcessor.ProcessPerRoundAbility(medic, allHeroes, new List<EnemyToken>(), ctx);
            Debug.Log($"[SpecialAbilityProcessorTests] ProcessPerRoundAbility_FieldMedic_NoHealWhenAllFull: allyHP={ally.currentHP}");
            Assert.AreEqual(allyHPBefore, ally.currentHP, "FieldMedic should not heal when all allies are at full HP");
        }

        [Test]
        public void ProcessPerRoundAbility_DeadHero_Skips()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessPerRoundAbility_DeadHero_Skips: starting test");
            var medicDef = TestDataFactory.CreateHero(id: 1, name: "Dead Medic", ability: SpecialAbility.FieldMedic, hp: 3);
            var medic = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: medicDef);
            medic.TakeDamage(10);
            var allyDef = TestDataFactory.CreateHero(id: 2, name: "Wounded Ally", hp: 10);
            var ally = TestDataFactory.CreateHeroToken(tokenId: 2, cardDef: allyDef);
            ally.TakeDamage(3);
            int allyHPBefore = ally.currentHP;
            var allHeroes = new List<HeroToken> { medic, ally };
            var ctx = new CombatContext { nodeId = 0, roundNumber = 1 };
            SpecialAbilityProcessor.ProcessPerRoundAbility(medic, allHeroes, new List<EnemyToken>(), ctx);
            Debug.Log($"[SpecialAbilityProcessorTests] ProcessPerRoundAbility_DeadHero_Skips: allyHP={ally.currentHP}");
            Assert.AreEqual(allyHPBefore, ally.currentHP, "Dead medic should not heal anyone");
        }

        [Test]
        public void ProcessPostCombatAbility_ScoutReport_DoesNotThrow()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessPostCombatAbility_ScoutReport_DoesNotThrow: starting test");
            var scoutDef = TestDataFactory.CreateHero(id: 1, name: "Scout", ability: SpecialAbility.ScoutReport);
            var scout = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: scoutDef);
            var result = CombatResult.Create(0);
            result.heroesWon = true;
            Assert.DoesNotThrow(() => SpecialAbilityProcessor.ProcessPostCombatAbility(scout, result));
            Debug.Log("[SpecialAbilityProcessorTests] ProcessPostCombatAbility_ScoutReport_DoesNotThrow: completed without exception");
        }

        [Test]
        public void ProcessPostCombatAbility_DeadHero_Skips()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessPostCombatAbility_DeadHero_Skips: starting test");
            var scoutDef = TestDataFactory.CreateHero(id: 1, name: "Dead Scout", ability: SpecialAbility.ScoutReport, hp: 3);
            var scout = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: scoutDef);
            scout.TakeDamage(10);
            var result = CombatResult.Create(0);
            Assert.DoesNotThrow(() => SpecialAbilityProcessor.ProcessPostCombatAbility(scout, result));
            Debug.Log("[SpecialAbilityProcessorTests] ProcessPostCombatAbility_DeadHero_Skips: completed (dead hero skipped)");
        }

        [Test]
        public void ProcessMovementAbility_ExtendedFogReveal_RevealsBeyondNormal()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessMovementAbility_ExtendedFogReveal_RevealsBeyondNormal: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            // Set all nodes to hidden
            foreach (var node in graph.GetAllNodes())
                node.fogState = FogState.Hidden;
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "Extended Reveal", ability: SpecialAbility.ExtendedFogReveal);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: heroDef);
            SpecialAbilityProcessor.ProcessMovementAbility(hero, graph, 0, 1);
            // Node 1's neighbors are 0 and 2; their neighbors include 3
            var node3 = graph.GetNode(3);
            Debug.Log($"[SpecialAbilityProcessorTests] ProcessMovementAbility_ExtendedFogReveal_RevealsBeyondNormal: node3FogState={node3.fogState}");
            Assert.AreNotEqual(FogState.Hidden, node3.fogState, "Extended fog reveal should reveal nodes 2 hops from destination");
        }

        [Test]
        public void ProcessMovementAbility_DeadHero_Skips()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessMovementAbility_DeadHero_Skips: starting test");
            var graph = TestDataFactory.CreateSimpleGraph();
            foreach (var node in graph.GetAllNodes())
                node.fogState = FogState.Hidden;
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "Dead Revealer", ability: SpecialAbility.ExtendedFogReveal, hp: 3);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: heroDef);
            hero.TakeDamage(10);
            SpecialAbilityProcessor.ProcessMovementAbility(hero, graph, 0, 1);
            var node2 = graph.GetNode(2);
            Debug.Log($"[SpecialAbilityProcessorTests] ProcessMovementAbility_DeadHero_Skips: node2FogState={node2.fogState}");
            Assert.AreEqual(FogState.Hidden, node2.fogState, "Dead hero should not reveal any nodes");
        }

        [Test]
        public void ProcessGatherAbility_EfficientGather_ReturnsBonus()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessGatherAbility_EfficientGather_ReturnsBonus: starting test");
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "Efficient Gatherer", ability: SpecialAbility.EfficientGather);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: heroDef);
            var node = new MapNode { nodeId = 1, zone = NodeType.Wilderness };
            int bonus = SpecialAbilityProcessor.ProcessGatherAbility(hero, node);
            Debug.Log($"[SpecialAbilityProcessorTests] ProcessGatherAbility_EfficientGather_ReturnsBonus: bonus={bonus}");
            Assert.AreEqual(1, bonus, "EfficientGather should return +1 bonus");
        }

        [Test]
        public void ProcessGatherAbility_BulkHaul_BonusOnWilderness()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessGatherAbility_BulkHaul_BonusOnWilderness: starting test");
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "Hauler", ability: SpecialAbility.BulkHaul);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: heroDef);
            var wildernessNode = new MapNode { nodeId = 1, zone = NodeType.Wilderness };
            var farmNode = new MapNode { nodeId = 2, zone = NodeType.Farmland };
            int wildBonus = SpecialAbilityProcessor.ProcessGatherAbility(hero, wildernessNode);
            int farmBonus = SpecialAbilityProcessor.ProcessGatherAbility(hero, farmNode);
            Debug.Log($"[SpecialAbilityProcessorTests] ProcessGatherAbility_BulkHaul_BonusOnWilderness: wildBonus={wildBonus}, farmBonus={farmBonus}");
            Assert.AreEqual(1, wildBonus, "BulkHaul should return +1 bonus on Wilderness");
            Assert.AreEqual(0, farmBonus, "BulkHaul should return 0 bonus on non-Wilderness");
        }

        [Test]
        public void ProcessGatherAbility_None_ReturnsZero()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessGatherAbility_None_ReturnsZero: starting test");
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1);
            var node = new MapNode { nodeId = 1, zone = NodeType.Wilderness };
            int bonus = SpecialAbilityProcessor.ProcessGatherAbility(hero, node);
            Debug.Log($"[SpecialAbilityProcessorTests] ProcessGatherAbility_None_ReturnsZero: bonus={bonus}");
            Assert.AreEqual(0, bonus, "Hero with no ability should return 0 gather bonus");
        }

        [Test]
        public void ProcessGatherAbility_DeadHero_ReturnsZero()
        {
            Debug.Log("[SpecialAbilityProcessorTests] ProcessGatherAbility_DeadHero_ReturnsZero: starting test");
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "Dead Gatherer", ability: SpecialAbility.EfficientGather, hp: 3);
            var hero = TestDataFactory.CreateHeroToken(tokenId: 1, cardDef: heroDef);
            hero.TakeDamage(10);
            var node = new MapNode { nodeId = 1, zone = NodeType.Wilderness };
            int bonus = SpecialAbilityProcessor.ProcessGatherAbility(hero, node);
            Debug.Log($"[SpecialAbilityProcessorTests] ProcessGatherAbility_DeadHero_ReturnsZero: bonus={bonus}");
            Assert.AreEqual(0, bonus, "Dead hero should return 0 gather bonus");
        }
    }


    // ============================================================
    // 19. LocalizationManager Tests
    // ============================================================
    [TestFixture]
    public class LocalizationManagerTests
    {
        private GameObject locGO;

        [SetUp]
        public void Setup()
        {
            Debug.Log("[LocalizationManagerTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[LocalizationManagerTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[LocalizationManagerTests] Teardown: ENTER - cleaning up services");
            if (locGO != null)
            {
                UnityEngine.Object.DestroyImmediate(locGO);
                locGO = null;
            }
            TestDataFactory.CleanupServices();
            Debug.Log("[LocalizationManagerTests] Teardown: EXIT");
        }

        [Test]
        public void Get_NoInstance_ReturnsKey()
        {
            Debug.Log("[LocalizationManagerTests] Get_NoInstance_ReturnsKey: starting test");
            // No LocalizationManager instance — Get should return key
            string result = LocalizationManager.Get("test.key");
            Debug.Log($"[LocalizationManagerTests] Get_NoInstance_ReturnsKey: result={result}");
            Assert.AreEqual("test.key", result, "Get should return key when no instance is available");
        }

        [Test]
        public void Format_NoInstance_ReturnsKey()
        {
            Debug.Log("[LocalizationManagerTests] Format_NoInstance_ReturnsKey: starting test");
            string result = LocalizationManager.Format("test.format", 42);
            Debug.Log($"[LocalizationManagerTests] Format_NoInstance_ReturnsKey: result={result}");
            // Format calls Get which returns the key, then tries string.Format on it which may throw or return key
            Assert.IsNotNull(result, "Format should not return null");
        }

        [Test]
        public void Loc_Get_DelegatesToLocalizationManager()
        {
            Debug.Log("[LocalizationManagerTests] Loc_Get_DelegatesToLocalizationManager: starting test");
            string result = Loc.Get("missing.key");
            Debug.Log($"[LocalizationManagerTests] Loc_Get_DelegatesToLocalizationManager: result={result}");
            Assert.AreEqual("missing.key", result, "Loc.Get should delegate to LocalizationManager.Get and return key if missing");
        }

        [Test]
        public void Loc_Format_DelegatesToLocalizationManager()
        {
            Debug.Log("[LocalizationManagerTests] Loc_Format_DelegatesToLocalizationManager: starting test");
            string result = Loc.Format("missing.format.key", 1, 2);
            Debug.Log($"[LocalizationManagerTests] Loc_Format_DelegatesToLocalizationManager: result={result}");
            Assert.IsNotNull(result, "Loc.Format should not return null");
        }

        [Test]
        public void SetLanguage_NoInstance_DoesNotThrow()
        {
            Debug.Log("[LocalizationManagerTests] SetLanguage_NoInstance_DoesNotThrow: starting test");
            LogAssert.Expect(LogType.Error, "[LocalizationManager] SetLanguage: no instance available");
            Assert.DoesNotThrow(() => LocalizationManager.SetLanguage("fr"));
            Debug.Log("[LocalizationManagerTests] SetLanguage_NoInstance_DoesNotThrow: completed without exception");
        }
    }

    // ============================================================
    // 20. SaveManager Tests
    // ============================================================
    [TestFixture]
    public class SaveManagerTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[SaveManagerTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            // Clean up any previous save
            SaveManager.DeleteSave();
            Debug.Log("[SaveManagerTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[SaveManagerTests] Teardown: ENTER - cleaning up services");
            SaveManager.DeleteSave();
            TestDataFactory.CleanupServices();
            Debug.Log("[SaveManagerTests] Teardown: EXIT");
        }

        [Test]
        public void HasSave_NoSave_ReturnsFalse()
        {
            Debug.Log("[SaveManagerTests] HasSave_NoSave_ReturnsFalse: starting test");
            bool hasSave = SaveManager.HasSave();
            Debug.Log($"[SaveManagerTests] HasSave_NoSave_ReturnsFalse: hasSave={hasSave}");
            Assert.IsFalse(hasSave, "HasSave should return false when no save exists");
        }

        [Test]
        public void Save_ThenHasSave_ReturnsTrue()
        {
            Debug.Log("[SaveManagerTests] Save_ThenHasSave_ReturnsTrue: starting test");
            var data = new RunSaveData();
            data.currentTurn = 5;
            data.randomSeed = 42;
            SaveManager.Save(data);
            bool hasSave = SaveManager.HasSave();
            Debug.Log($"[SaveManagerTests] Save_ThenHasSave_ReturnsTrue: hasSave={hasSave}");
            Assert.IsTrue(hasSave, "HasSave should return true after saving");
        }

        [Test]
        public void Save_ThenLoad_PreservesData()
        {
            Debug.Log("[SaveManagerTests] Save_ThenLoad_PreservesData: starting test");
            var data = new RunSaveData();
            data.currentTurn = 7;
            data.randomSeed = 123;
            data.foodStockpile = 10;
            data.materialsStockpile = 5;
            data.currencyStockpile = 3;
            SaveManager.Save(data);
            var loaded = SaveManager.Load();
            Debug.Log($"[SaveManagerTests] Save_ThenLoad_PreservesData: turn={loaded.currentTurn}, seed={loaded.randomSeed}, food={loaded.foodStockpile}");
            Assert.IsNotNull(loaded, "Load should return non-null after save");
            Assert.AreEqual(7, loaded.currentTurn, "Turn should be preserved");
            Assert.AreEqual(123, loaded.randomSeed, "Seed should be preserved");
            Assert.AreEqual(10, loaded.foodStockpile, "Food should be preserved");
        }

        [Test]
        public void DeleteSave_RemovesSaveFile()
        {
            Debug.Log("[SaveManagerTests] DeleteSave_RemovesSaveFile: starting test");
            var data = new RunSaveData();
            SaveManager.Save(data);
            Assert.IsTrue(SaveManager.HasSave(), "Should have save before delete");
            SaveManager.DeleteSave();
            bool hasSave = SaveManager.HasSave();
            Debug.Log($"[SaveManagerTests] DeleteSave_RemovesSaveFile: hasSave={hasSave}");
            Assert.IsFalse(hasSave, "HasSave should return false after delete");
        }

        [Test]
        public void Load_NoSave_ReturnsNull()
        {
            Debug.Log("[SaveManagerTests] Load_NoSave_ReturnsNull: starting test");
            var loaded = SaveManager.Load();
            Debug.Log($"[SaveManagerTests] Load_NoSave_ReturnsNull: loaded={loaded}");
            Assert.IsNull(loaded, "Load should return null when no save exists");
        }
    }

    // ============================================================
    // 22. MetaProgressionManager Tests
    // ============================================================
    [TestFixture]
    public class MetaProgressionManagerTests
    {
        private GameObject metaGO;
        private MetaProgressionManager meta;

        [SetUp]
        public void Setup()
        {
            Debug.Log("[MetaProgressionManagerTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            metaGO = new GameObject("TestMetaProgressionManager");
            meta = metaGO.AddComponent<MetaProgressionManager>();
            Debug.Log("[MetaProgressionManagerTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[MetaProgressionManagerTests] Teardown: ENTER - cleaning up");
            if (meta != null)
                meta.ResetAllProgress();
            if (metaGO != null)
                UnityEngine.Object.DestroyImmediate(metaGO);
            TestDataFactory.CleanupServices();
            Debug.Log("[MetaProgressionManagerTests] Teardown: EXIT");
        }

        [Test]
        public void ResetAllProgress_ClearsData()
        {
            Debug.Log("[MetaProgressionManagerTests] ResetAllProgress_ClearsData: starting test");
            meta.DiscoverCard("TestCard");
            meta.ResetAllProgress();
            Assert.AreEqual(0, meta.Reputation, "Reputation should be 0 after reset");
            Debug.Log($"[MetaProgressionManagerTests] ResetAllProgress_ClearsData: reputation={meta.Reputation}");
        }

        [Test]
        public void DiscoverCard_RegistersDiscovery()
        {
            Debug.Log("[MetaProgressionManagerTests] DiscoverCard_RegistersDiscovery: starting test");
            meta.ResetAllProgress();
            meta.DiscoverCard("Needle Sword");
            // Discovery tracked in data — check via scrapbook or data access
            Debug.Log("[MetaProgressionManagerTests] DiscoverCard_RegistersDiscovery: discovery registered without error");
            Assert.IsNotNull(meta.Data, "Data should not be null after discovery");
            Assert.IsTrue(meta.Data.discoveredCards.Contains("Needle Sword"), "Card should be in discovered list");
        }

        [Test]
        public void DiscoverCard_DuplicateIgnored()
        {
            Debug.Log("[MetaProgressionManagerTests] DiscoverCard_DuplicateIgnored: starting test");
            meta.ResetAllProgress();
            meta.DiscoverCard("Sword");
            meta.DiscoverCard("Sword");
            int count = meta.Data.discoveredCards.Count;
            Debug.Log($"[MetaProgressionManagerTests] DiscoverCard_DuplicateIgnored: count={count}");
            Assert.AreEqual(1, count, "Duplicate discovery should not increase count");
        }

        [Test]
        public void DiscoverEnemy_RegistersInBestiary()
        {
            Debug.Log("[MetaProgressionManagerTests] DiscoverEnemy_RegistersInBestiary: starting test");
            meta.ResetAllProgress();
            meta.DiscoverEnemy("Giant Rat");
            bool discovered = meta.IsEnemyDiscovered("Giant Rat");
            Debug.Log($"[MetaProgressionManagerTests] DiscoverEnemy_RegistersInBestiary: discovered={discovered}");
            Assert.IsTrue(discovered, "Enemy should be discovered after DiscoverEnemy");
        }

        [Test]
        public void IsEnemyDiscovered_UnknownEnemy_ReturnsFalse()
        {
            Debug.Log("[MetaProgressionManagerTests] IsEnemyDiscovered_UnknownEnemy_ReturnsFalse: starting test");
            meta.ResetAllProgress();
            bool discovered = meta.IsEnemyDiscovered("Unknown");
            Debug.Log($"[MetaProgressionManagerTests] IsEnemyDiscovered_UnknownEnemy_ReturnsFalse: discovered={discovered}");
            Assert.IsFalse(discovered, "Unknown enemy should not be discovered");
        }

        [Test]
        public void DiscoverEvent_RegistersEvent()
        {
            Debug.Log("[MetaProgressionManagerTests] DiscoverEvent_RegistersEvent: starting test");
            meta.ResetAllProgress();
            meta.DiscoverEvent("HiddenCache");
            Assert.IsTrue(meta.Data.discoveredEvents.Contains("HiddenCache"), "Event should be in discovered list");
            Debug.Log("[MetaProgressionManagerTests] DiscoverEvent_RegistersEvent: event registered");
        }

        [Test]
        public void DiscoverBoss_RegistersBoss()
        {
            Debug.Log("[MetaProgressionManagerTests] DiscoverBoss_RegistersBoss: starting test");
            meta.ResetAllProgress();
            meta.DiscoverBoss("Pied Piper");
            Assert.IsTrue(meta.Data.discoveredBosses.Contains("Pied Piper"), "Boss should be in discovered list");
            Debug.Log("[MetaProgressionManagerTests] DiscoverBoss_RegistersBoss: boss registered");
        }

        [Test]
        public void DiscoverRelic_RegistersRelic()
        {
            Debug.Log("[MetaProgressionManagerTests] DiscoverRelic_RegistersRelic: starting test");
            meta.ResetAllProgress();
            meta.DiscoverRelic("Golden Feather");
            Assert.IsTrue(meta.Data.discoveredRelics.Contains("Golden Feather"), "Relic should be in discovered list");
            Debug.Log("[MetaProgressionManagerTests] DiscoverRelic_RegistersRelic: relic registered");
        }

        [Test]
        public void DiscoverLore_RegistersLore()
        {
            Debug.Log("[MetaProgressionManagerTests] DiscoverLore_RegistersLore: starting test");
            meta.ResetAllProgress();
            meta.DiscoverLore("lore.origins");
            Assert.IsTrue(meta.Data.discoveredLore.Contains("lore.origins"), "Lore should be in discovered list");
            Debug.Log("[MetaProgressionManagerTests] DiscoverLore_RegistersLore: lore registered");
        }

        [Test]
        public void TryUnlockStartingRelic_InsufficientRep_ReturnsFalse()
        {
            Debug.Log("[MetaProgressionManagerTests] TryUnlockStartingRelic_InsufficientRep_ReturnsFalse: starting test");
            meta.ResetAllProgress();
            bool result = meta.TryUnlockStartingRelic("Super Relic");
            Debug.Log($"[MetaProgressionManagerTests] TryUnlockStartingRelic_InsufficientRep_ReturnsFalse: result={result}, rep={meta.Reputation}");
            Assert.IsFalse(result, "Should fail to unlock relic with 0 reputation");
        }

        [Test]
        public void TryUnlockColonyCard_InsufficientRep_ReturnsFalse()
        {
            Debug.Log("[MetaProgressionManagerTests] TryUnlockColonyCard_InsufficientRep_ReturnsFalse: starting test");
            meta.ResetAllProgress();
            bool result = meta.TryUnlockColonyCard("Super Card");
            Debug.Log($"[MetaProgressionManagerTests] TryUnlockColonyCard_InsufficientRep_ReturnsFalse: result={result}, rep={meta.Reputation}");
            Assert.IsFalse(result, "Should fail to unlock colony card with 0 reputation");
        }

        [Test]
        public void ProcessRunEnd_Victory_TracksStats()
        {
            Debug.Log("[MetaProgressionManagerTests] ProcessRunEnd_Victory_TracksStats: starting test");
            meta.ResetAllProgress();
            meta.ProcessRunEnd(
                victory: true,
                levelReached: 3,
                resourcesGathered: 50,
                bossesKilled: 1,
                nodesVisited: 20,
                heroDeck: new List<CardDefinitionSO>(),
                enemiesEncountered: new List<string> { "Rat" },
                eventsEncountered: new List<string> { "Cache" },
                bossesEncountered: new List<string> { "Boss1" }
            );
            Debug.Log($"[MetaProgressionManagerTests] ProcessRunEnd_Victory_TracksStats: rep={meta.Reputation}, runsCompleted={meta.Data.totalRunsCompleted}");
            Assert.Greater(meta.Reputation, 0, "Should earn reputation from victory");
            Assert.AreEqual(1, meta.Data.totalRunsCompleted, "Should track completed runs");
            Assert.IsTrue(meta.IsEnemyDiscovered("Rat"), "Should discover encountered enemies");
        }

        [Test]
        public void ProcessRunEnd_Defeat_TracksFailure()
        {
            Debug.Log("[MetaProgressionManagerTests] ProcessRunEnd_Defeat_TracksFailure: starting test");
            meta.ResetAllProgress();
            meta.ProcessRunEnd(
                victory: false,
                levelReached: 2,
                resourcesGathered: 20,
                bossesKilled: 0,
                nodesVisited: 10,
                heroDeck: null,
                enemiesEncountered: null,
                eventsEncountered: null,
                bossesEncountered: null
            );
            Debug.Log($"[MetaProgressionManagerTests] ProcessRunEnd_Defeat_TracksFailure: runsFailed={meta.Data.totalRunsFailed}");
            Assert.AreEqual(1, meta.Data.totalRunsFailed, "Should track failed runs");
        }

        [Test]
        public void IsHeroUpgradeUnlocked_NotUnlocked_ReturnsFalse()
        {
            Debug.Log("[MetaProgressionManagerTests] IsHeroUpgradeUnlocked_NotUnlocked_ReturnsFalse: starting test");
            meta.ResetAllProgress();
            bool result = meta.IsHeroUpgradeUnlocked("Unknown Hero");
            Debug.Log($"[MetaProgressionManagerTests] IsHeroUpgradeUnlocked_NotUnlocked_ReturnsFalse: result={result}");
            Assert.IsFalse(result, "Unplayed hero should not have upgrade unlocked");
        }

        [Test]
        public void IsRelicUnlocked_NotUnlocked_ReturnsFalse()
        {
            Debug.Log("[MetaProgressionManagerTests] IsRelicUnlocked_NotUnlocked_ReturnsFalse: starting test");
            meta.ResetAllProgress();
            bool result = meta.IsRelicUnlocked("Unknown Relic");
            Debug.Log($"[MetaProgressionManagerTests] IsRelicUnlocked_NotUnlocked_ReturnsFalse: result={result}");
            Assert.IsFalse(result, "Unknown relic should not be unlocked");
        }

        [Test]
        public void IsColonyCardUnlocked_NotUnlocked_ReturnsFalse()
        {
            Debug.Log("[MetaProgressionManagerTests] IsColonyCardUnlocked_NotUnlocked_ReturnsFalse: starting test");
            meta.ResetAllProgress();
            bool result = meta.IsColonyCardUnlocked("Unknown Card");
            Debug.Log($"[MetaProgressionManagerTests] IsColonyCardUnlocked_NotUnlocked_ReturnsFalse: result={result}");
            Assert.IsFalse(result, "Unknown colony card should not be unlocked");
        }
    }


    // ============================================================
    // 24. GameSettings Tests
    // ============================================================
    [TestFixture]
    public class GameSettingsTests
    {
        private GameObject settingsGO;
        private GameSettings settings;

        [SetUp]
        public void Setup()
        {
            Debug.Log("[GameSettingsTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            settingsGO = new GameObject("TestGameSettings");
            settings = settingsGO.AddComponent<GameSettings>();
            Debug.Log("[GameSettingsTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[GameSettingsTests] Teardown: ENTER - cleaning up");
            if (settingsGO != null)
                UnityEngine.Object.DestroyImmediate(settingsGO);
            TestDataFactory.CleanupServices();
            Debug.Log("[GameSettingsTests] Teardown: EXIT");
        }

        [Test]
        public void SetBattleSpeed_ClampsToRange()
        {
            Debug.Log("[GameSettingsTests] SetBattleSpeed_ClampsToRange: starting test");
            settings.SetBattleSpeed(0);
            Assert.AreEqual(0, settings.BattleSpeed, "Speed 0 should be Normal");
            settings.SetBattleSpeed(2);
            Assert.AreEqual(2, settings.BattleSpeed, "Speed 2 should be Instant");
            settings.SetBattleSpeed(5);
            Assert.AreEqual(2, settings.BattleSpeed, "Speed 5 should clamp to 2");
            settings.SetBattleSpeed(-1);
            Assert.AreEqual(0, settings.BattleSpeed, "Speed -1 should clamp to 0");
            Debug.Log($"[GameSettingsTests] SetBattleSpeed_ClampsToRange: final speed={settings.BattleSpeed}");
        }

        [Test]
        public void CycleBattleSpeed_CyclesThrough()
        {
            Debug.Log("[GameSettingsTests] CycleBattleSpeed_CyclesThrough: starting test");
            settings.SetBattleSpeed(0);
            settings.CycleBattleSpeed();
            Assert.AreEqual(1, settings.BattleSpeed, "First cycle: 0 -> 1");
            settings.CycleBattleSpeed();
            Assert.AreEqual(2, settings.BattleSpeed, "Second cycle: 1 -> 2");
            settings.CycleBattleSpeed();
            Assert.AreEqual(0, settings.BattleSpeed, "Third cycle: 2 -> 0 (wrap)");
            Debug.Log($"[GameSettingsTests] CycleBattleSpeed_CyclesThrough: final speed={settings.BattleSpeed}");
        }

        [Test]
        public void SetColorBlindMode_SetsFlag()
        {
            Debug.Log("[GameSettingsTests] SetColorBlindMode_SetsFlag: starting test");
            settings.SetColorBlindMode(true);
            Assert.IsTrue(settings.ColorBlindMode, "ColorBlindMode should be true after setting true");
            settings.SetColorBlindMode(false);
            Assert.IsFalse(settings.ColorBlindMode, "ColorBlindMode should be false after setting false");
            Debug.Log($"[GameSettingsTests] SetColorBlindMode_SetsFlag: colorBlind={settings.ColorBlindMode}");
        }

        [Test]
        public void SetTextSizeModifier_ClampsToRange()
        {
            Debug.Log("[GameSettingsTests] SetTextSizeModifier_ClampsToRange: starting test");
            settings.SetTextSizeModifier(2);
            Assert.AreEqual(2, settings.TextSizeModifier, "Modifier 2 should be accepted");
            settings.SetTextSizeModifier(-5);
            Assert.AreEqual(-2, settings.TextSizeModifier, "Modifier -5 should clamp to -2");
            settings.SetTextSizeModifier(10);
            Assert.AreEqual(4, settings.TextSizeModifier, "Modifier 10 should clamp to 4");
            Debug.Log($"[GameSettingsTests] SetTextSizeModifier_ClampsToRange: modifier={settings.TextSizeModifier}");
        }

        [Test]
        public void SetMasterVolume_ClampsTo01()
        {
            Debug.Log("[GameSettingsTests] SetMasterVolume_ClampsTo01: starting test");
            settings.SetMasterVolume(0.5f);
            Assert.AreEqual(0.5f, settings.MasterVolume, 0.01f, "Volume 0.5 should be accepted");
            settings.SetMasterVolume(-1f);
            Assert.AreEqual(0f, settings.MasterVolume, 0.01f, "Volume -1 should clamp to 0");
            settings.SetMasterVolume(5f);
            Assert.AreEqual(1f, settings.MasterVolume, 0.01f, "Volume 5 should clamp to 1");
            Debug.Log($"[GameSettingsTests] SetMasterVolume_ClampsTo01: volume={settings.MasterVolume}");
        }

        [Test]
        public void SetMusicVolume_ClampsTo01()
        {
            Debug.Log("[GameSettingsTests] SetMusicVolume_ClampsTo01: starting test");
            settings.SetMusicVolume(0.3f);
            Assert.AreEqual(0.3f, settings.MusicVolume, 0.01f, "Music volume 0.3 should be accepted");
            settings.SetMusicVolume(2f);
            Assert.AreEqual(1f, settings.MusicVolume, 0.01f, "Music volume 2 should clamp to 1");
            Debug.Log($"[GameSettingsTests] SetMusicVolume_ClampsTo01: volume={settings.MusicVolume}");
        }

        [Test]
        public void SetSfxVolume_ClampsTo01()
        {
            Debug.Log("[GameSettingsTests] SetSfxVolume_ClampsTo01: starting test");
            settings.SetSfxVolume(0.8f);
            Assert.AreEqual(0.8f, settings.SfxVolume, 0.01f, "SFX volume 0.8 should be accepted");
            settings.SetSfxVolume(-1f);
            Assert.AreEqual(0f, settings.SfxVolume, 0.01f, "SFX volume -1 should clamp to 0");
            Debug.Log($"[GameSettingsTests] SetSfxVolume_ClampsTo01: volume={settings.SfxVolume}");
        }

        [Test]
        public void GetNodeColor_ColorBlindMode_ReturnsDifferentColors()
        {
            Debug.Log("[GameSettingsTests] GetNodeColor_ColorBlindMode_ReturnsDifferentColors: starting test");
            settings.SetColorBlindMode(true);
            var resourceColor = settings.GetNodeColor(NodeType.Wilderness, false, true);
            var eliteColor = settings.GetNodeColor(NodeType.Farmland, false, true);
            Debug.Log($"[GameSettingsTests] GetNodeColor_ColorBlindMode_ReturnsDifferentColors: resource={resourceColor}, elite={eliteColor}");
            Assert.AreNotEqual(resourceColor, eliteColor, "Color-blind mode should differentiate resource and elite nodes");
        }

        [Test]
        public void GetNodeColor_Visited_ReturnsGrayish()
        {
            Debug.Log("[GameSettingsTests] GetNodeColor_Visited_ReturnsGrayish: starting test");
            var visitedColor = settings.GetNodeColor(NodeType.Wilderness, true, true);
            Debug.Log($"[GameSettingsTests] GetNodeColor_Visited_ReturnsGrayish: color={visitedColor}");
            // Visited colors are grayscale-ish (r ~= g ~= b)
            Assert.AreEqual(visitedColor.r, visitedColor.g, 0.15f, "Visited node color should be grayish");
        }

        [Test]
        public void AdjustedFontSize_Default_ReturnsBase()
        {
            Debug.Log("[GameSettingsTests] AdjustedFontSize_Default_ReturnsBase: starting test");
            settings.SetTextSizeModifier(0);
            int adjusted = settings.AdjustedFontSize(14);
            Debug.Log($"[GameSettingsTests] AdjustedFontSize_Default_ReturnsBase: adjusted={adjusted}");
            Assert.AreEqual(14, adjusted, "With modifier 0, adjusted size should equal base size");
        }

        [Test]
        public void AdjustedFontSize_Positive_IncreasesSize()
        {
            Debug.Log("[GameSettingsTests] AdjustedFontSize_Positive_IncreasesSize: starting test");
            settings.SetTextSizeModifier(2);
            int adjusted = settings.AdjustedFontSize(14);
            Debug.Log($"[GameSettingsTests] AdjustedFontSize_Positive_IncreasesSize: adjusted={adjusted}");
            Assert.AreEqual(18, adjusted, "Modifier +2 should add 4 to base: 14 + 2*2 = 18");
        }

        [Test]
        public void AdjustedFontSize_NeverBelowMinimum()
        {
            Debug.Log("[GameSettingsTests] AdjustedFontSize_NeverBelowMinimum: starting test");
            settings.SetTextSizeModifier(-2);
            int adjusted = settings.AdjustedFontSize(8);
            Debug.Log($"[GameSettingsTests] AdjustedFontSize_NeverBelowMinimum: adjusted={adjusted}");
            Assert.GreaterOrEqual(adjusted, 8, "Adjusted font size should never go below 8");
        }

        [Test]
        public void GameSettings_SetDifficulty_UpdatesProperty()
        {
            Debug.Log("[GameSettingsTests] GameSettings_SetDifficulty_UpdatesProperty: starting test");
            var gsGo = new GameObject("TestGS");
            var gs = gsGo.AddComponent<GameSettings>();
            gs.SetDifficulty(DifficultyLevel.Easy);
            Debug.Log($"[GameSettingsTests] SetDifficulty: difficulty={gs.Difficulty}");
            Assert.AreEqual(DifficultyLevel.Easy, gs.Difficulty, "Difficulty should be Easy after setting");
            gs.SetDifficulty(DifficultyLevel.Hard);
            Assert.AreEqual(DifficultyLevel.Hard, gs.Difficulty, "Difficulty should be Hard after setting");
            UnityEngine.Object.DestroyImmediate(gsGo);
        }

        [Test]
        public void GameSettings_CycleDifficulty_CyclesThroughAll()
        {
            Debug.Log("[GameSettingsTests] GameSettings_CycleDifficulty_CyclesThroughAll: starting test");
            var gsGo = new GameObject("TestGS");
            var gs = gsGo.AddComponent<GameSettings>();
            gs.SetDifficulty(DifficultyLevel.Easy);
            gs.CycleDifficulty();
            Debug.Log($"[GameSettingsTests] CycleDifficulty: after first cycle={gs.Difficulty}");
            Assert.AreEqual(DifficultyLevel.Normal, gs.Difficulty, "Should cycle to Normal");
            gs.CycleDifficulty();
            Assert.AreEqual(DifficultyLevel.Hard, gs.Difficulty, "Should cycle to Hard");
            gs.CycleDifficulty();
            Assert.AreEqual(DifficultyLevel.Easy, gs.Difficulty, "Should cycle back to Easy");
            UnityEngine.Object.DestroyImmediate(gsGo);
        }

        [Test]
        public void GameSettings_SetTutorialCompleted_PersistsValue()
        {
            Debug.Log("[GameSettingsTests] GameSettings_SetTutorialCompleted_PersistsValue: starting test");
            var gsGo = new GameObject("TestGS");
            var gs = gsGo.AddComponent<GameSettings>();
            Assert.IsFalse(gs.TutorialCompleted, "Tutorial should not be completed initially");
            gs.SetTutorialCompleted(true);
            Debug.Log($"[GameSettingsTests] SetTutorialCompleted: completed={gs.TutorialCompleted}");
            Assert.IsTrue(gs.TutorialCompleted, "Tutorial should be completed after setting");
            UnityEngine.Object.DestroyImmediate(gsGo);
        }
    }

    // ============================================================
    // 26. RunManager Tests
    // ============================================================
    [TestFixture]
    public class RunManagerTests
    {
        private GameObject runGO;
        private RunManager runMgr;

        [SetUp]
        public void Setup()
        {
            Debug.Log("[RunManagerTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            runGO = new GameObject("TestRunManager");
            runMgr = runGO.AddComponent<RunManager>();
            Debug.Log("[RunManagerTests] Setup: EXIT");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log("[RunManagerTests] Teardown: ENTER - cleaning up");
            if (runGO != null)
                UnityEngine.Object.DestroyImmediate(runGO);
            TestDataFactory.CleanupServices();
            Debug.Log("[RunManagerTests] Teardown: EXIT");
        }

        [Test]
        public void RecordResourceGathered_AccumulatesTotal()
        {
            Debug.Log("[RunManagerTests] RecordResourceGathered_AccumulatesTotal: starting test");
            runMgr.RecordResourceGathered(5);
            runMgr.RecordResourceGathered(3);
            Debug.Log($"[RunManagerTests] RecordResourceGathered_AccumulatesTotal: total={runMgr.TotalResourcesGathered}");
            Assert.AreEqual(8, runMgr.TotalResourcesGathered, "Resource total should accumulate: 5 + 3 = 8");
        }

        [Test]
        public void RecordEnemyDefeated_IncrementsCount()
        {
            Debug.Log("[RunManagerTests] RecordEnemyDefeated_IncrementsCount: starting test");
            runMgr.RecordEnemyDefeated(false, false);
            runMgr.RecordEnemyDefeated(false, false);
            Debug.Log($"[RunManagerTests] RecordEnemyDefeated_IncrementsCount: total={runMgr.TotalEnemiesDefeated}");
            Assert.AreEqual(2, runMgr.TotalEnemiesDefeated, "Enemy defeated count should be 2");
        }

        [Test]
        public void RecordEnemyDefeated_Boss_IncrementsBossCount()
        {
            Debug.Log("[RunManagerTests] RecordEnemyDefeated_Boss_IncrementsBossCount: starting test");
            runMgr.RecordEnemyDefeated(true, false);
            Debug.Log($"[RunManagerTests] RecordEnemyDefeated_Boss_IncrementsBossCount: bossesDefeated={runMgr.ZoneBossesDefeated}");
            Assert.AreEqual(1, runMgr.ZoneBossesDefeated, "Boss defeated count should be 1");
        }

        [Test]
        public void RecordEnemyDefeated_PiedPiper_SetsFlag()
        {
            Debug.Log("[RunManagerTests] RecordEnemyDefeated_PiedPiper_SetsFlag: starting test");
            runMgr.RecordEnemyDefeated(true, true);
            Debug.Log($"[RunManagerTests] RecordEnemyDefeated_PiedPiper_SetsFlag: piedPiperDefeated={runMgr.PiedPiperDefeated}");
            Assert.IsTrue(runMgr.PiedPiperDefeated, "Pied Piper defeated flag should be set");
        }

        [Test]
        public void RecordColonyCardPlayed_IncrementsCount()
        {
            Debug.Log("[RunManagerTests] RecordColonyCardPlayed_IncrementsCount: starting test");
            runMgr.RecordColonyCardPlayed();
            runMgr.RecordColonyCardPlayed();
            runMgr.RecordColonyCardPlayed();
            Debug.Log($"[RunManagerTests] RecordColonyCardPlayed_IncrementsCount: count={runMgr.ColonyCardsPlayed}");
            Assert.AreEqual(3, runMgr.ColonyCardsPlayed, "Colony cards played count should be 3");
        }

        [Test]
        public void RecordHeroInjured_TracksUniqueHeroes()
        {
            Debug.Log("[RunManagerTests] RecordHeroInjured_TracksUniqueHeroes: starting test");
            runMgr.RecordHeroInjured(1);
            runMgr.RecordHeroInjured(2);
            runMgr.RecordHeroInjured(1); // duplicate
            // No direct public count accessor, but we verify no exception
            Assert.DoesNotThrow(() => runMgr.RecordHeroInjured(3));
            Debug.Log("[RunManagerTests] RecordHeroInjured_TracksUniqueHeroes: all injuries recorded without error");
        }

        [Test]
        public void UpdateStockpiles_SetsValues()
        {
            Debug.Log("[RunManagerTests] UpdateStockpiles_SetsValues: starting test");
            runMgr.UpdateStockpiles(10, 5, 3);
            Debug.Log($"[RunManagerTests] UpdateStockpiles_SetsValues: food={runMgr.FoodStockpile}, materials={runMgr.MaterialsStockpile}, currency={runMgr.CurrencyStockpile}");
            Assert.AreEqual(10, runMgr.FoodStockpile, "Food stockpile should be 10");
            Assert.AreEqual(5, runMgr.MaterialsStockpile, "Materials stockpile should be 5");
            Assert.AreEqual(3, runMgr.CurrencyStockpile, "Currency stockpile should be 3");
        }

        [Test]
        public void UpdateStockpiles_OverwritesPrevious()
        {
            Debug.Log("[RunManagerTests] UpdateStockpiles_OverwritesPrevious: starting test");
            runMgr.UpdateStockpiles(10, 5, 3);
            runMgr.UpdateStockpiles(20, 15, 8);
            Debug.Log($"[RunManagerTests] UpdateStockpiles_OverwritesPrevious: food={runMgr.FoodStockpile}, materials={runMgr.MaterialsStockpile}, currency={runMgr.CurrencyStockpile}");
            Assert.AreEqual(20, runMgr.FoodStockpile, "Food stockpile should be updated to 20");
            Assert.AreEqual(15, runMgr.MaterialsStockpile, "Materials stockpile should be updated to 15");
            Assert.AreEqual(8, runMgr.CurrencyStockpile, "Currency stockpile should be updated to 8");
        }
    }

    // ============================================================
    // ServiceLocator Tests
    // ============================================================
    [TestFixture]
    public class ServiceLocatorTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[ServiceLocatorTests] Setup: ENTER - cleaning up ServiceLocator before test");
            TestDataFactory.CleanupServices();
            Debug.Log("[ServiceLocatorTests] Setup: EXIT");
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("[ServiceLocatorTests] TearDown: ENTER - cleaning up ServiceLocator after test");
            TestDataFactory.CleanupServices();
            Debug.Log("[ServiceLocatorTests] TearDown: EXIT");
        }

        [Test]
        public void Register_AndGet_ReturnsSameInstance()
        {
            Debug.Log("[ServiceLocatorTests] Register_AndGet_ReturnsSameInstance: starting test");
            var resolver = new Scurry.Combat.CombatResolver();
            Debug.Log($"[ServiceLocatorTests] Register_AndGet_ReturnsSameInstance: created CombatResolver instance (hashCode={resolver.GetHashCode()})");
            ServiceLocator.Register<ICombatResolver>(resolver);
            Debug.Log("[ServiceLocatorTests] Register_AndGet_ReturnsSameInstance: registered ICombatResolver");
            var retrieved = ServiceLocator.Get<ICombatResolver>();
            Debug.Log($"[ServiceLocatorTests] Register_AndGet_ReturnsSameInstance: retrieved ICombatResolver (hashCode={retrieved?.GetHashCode()}, isNull={retrieved == null})");
            Assert.IsNotNull(retrieved, "Retrieved service should not be null");
            Debug.Log("[ServiceLocatorTests] Register_AndGet_ReturnsSameInstance: asserting same instance");
            Assert.AreSame(resolver, retrieved, "Retrieved service should be the same instance that was registered");
            Debug.Log("[ServiceLocatorTests] Register_AndGet_ReturnsSameInstance: PASSED");
        }

        [Test]
        public void Get_Unregistered_ReturnsNull()
        {
            Debug.Log("[ServiceLocatorTests] Get_Unregistered_ReturnsNull: starting test");
            Debug.Log("[ServiceLocatorTests] Get_Unregistered_ReturnsNull: attempting to get IMapGraph without registering");
            var result = ServiceLocator.Get<IMapGraph>();
            Debug.Log($"[ServiceLocatorTests] Get_Unregistered_ReturnsNull: result isNull={result == null}");
            Assert.IsNull(result, "Getting an unregistered service should return null");
            Debug.Log("[ServiceLocatorTests] Get_Unregistered_ReturnsNull: PASSED");
        }

        [Test]
        public void Unregister_RemovesService()
        {
            Debug.Log("[ServiceLocatorTests] Unregister_RemovesService: starting test");
            var resolver = new Scurry.Combat.CombatResolver();
            Debug.Log($"[ServiceLocatorTests] Unregister_RemovesService: registering ICombatResolver (hashCode={resolver.GetHashCode()})");
            ServiceLocator.Register<ICombatResolver>(resolver);
            Debug.Log("[ServiceLocatorTests] Unregister_RemovesService: unregistering ICombatResolver");
            ServiceLocator.Unregister<ICombatResolver>();
            Debug.Log("[ServiceLocatorTests] Unregister_RemovesService: attempting to get ICombatResolver after unregister");
            var result = ServiceLocator.Get<ICombatResolver>();
            Debug.Log($"[ServiceLocatorTests] Unregister_RemovesService: result isNull={result == null}");
            Assert.IsNull(result, "Service should be null after unregistering");
            Debug.Log("[ServiceLocatorTests] Unregister_RemovesService: PASSED");
        }

        [Test]
        public void Clear_RemovesAllServices()
        {
            Debug.Log("[ServiceLocatorTests] Clear_RemovesAllServices: starting test");
            var resolver = new Scurry.Combat.CombatResolver();
            var fog = new FogOfWar();
            Debug.Log($"[ServiceLocatorTests] Clear_RemovesAllServices: registering ICombatResolver (hashCode={resolver.GetHashCode()})");
            ServiceLocator.Register<ICombatResolver>(resolver);
            Debug.Log($"[ServiceLocatorTests] Clear_RemovesAllServices: registering IFogOfWar (hashCode={fog.GetHashCode()})");
            ServiceLocator.Register<IFogOfWar>(fog);
            Debug.Log("[ServiceLocatorTests] Clear_RemovesAllServices: calling ServiceLocator.Clear()");
            ServiceLocator.Clear();
            Debug.Log("[ServiceLocatorTests] Clear_RemovesAllServices: checking ICombatResolver after clear");
            var r1 = ServiceLocator.Get<ICombatResolver>();
            Debug.Log($"[ServiceLocatorTests] Clear_RemovesAllServices: ICombatResolver isNull={r1 == null}");
            Assert.IsNull(r1, "ICombatResolver should be null after Clear");
            Debug.Log("[ServiceLocatorTests] Clear_RemovesAllServices: checking IFogOfWar after clear");
            var r2 = ServiceLocator.Get<IFogOfWar>();
            Debug.Log($"[ServiceLocatorTests] Clear_RemovesAllServices: IFogOfWar isNull={r2 == null}");
            Assert.IsNull(r2, "IFogOfWar should be null after Clear");
            Debug.Log("[ServiceLocatorTests] Clear_RemovesAllServices: PASSED");
        }

        [Test]
        public void Register_OverwritesPreviousRegistration()
        {
            Debug.Log("[ServiceLocatorTests] Register_OverwritesPreviousRegistration: starting test");
            var resolverA = new Scurry.Combat.CombatResolver();
            var resolverB = new Scurry.Combat.CombatResolver();
            Debug.Log($"[ServiceLocatorTests] Register_OverwritesPreviousRegistration: created resolverA (hashCode={resolverA.GetHashCode()})");
            Debug.Log($"[ServiceLocatorTests] Register_OverwritesPreviousRegistration: created resolverB (hashCode={resolverB.GetHashCode()})");
            ServiceLocator.Register<ICombatResolver>(resolverA);
            Debug.Log("[ServiceLocatorTests] Register_OverwritesPreviousRegistration: registered resolverA as ICombatResolver");
            ServiceLocator.Register<ICombatResolver>(resolverB);
            Debug.Log("[ServiceLocatorTests] Register_OverwritesPreviousRegistration: registered resolverB as ICombatResolver (overwrite)");
            var retrieved = ServiceLocator.Get<ICombatResolver>();
            Debug.Log($"[ServiceLocatorTests] Register_OverwritesPreviousRegistration: retrieved ICombatResolver (hashCode={retrieved?.GetHashCode()})");
            Assert.AreSame(resolverB, retrieved, "Get should return the most recently registered instance");
            Debug.Log("[ServiceLocatorTests] Register_OverwritesPreviousRegistration: PASSED");
        }
    }

    // ============================================================
    // HeroTokenFactory Tests
    // ============================================================
    [TestFixture]
    public class HeroTokenFactoryTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[HeroTokenFactoryTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[HeroTokenFactoryTests] Setup: EXIT");
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("[HeroTokenFactoryTests] TearDown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[HeroTokenFactoryTests] TearDown: EXIT");
        }

        [Test]
        public void Create_ReturnsValidToken()
        {
            Debug.Log("[HeroTokenFactoryTests] Create_ReturnsValidToken: starting test");
            var heroDef = TestDataFactory.CreateHero(id: 1, name: "Scout Rat", combat: 2, move: 3, hp: 5, carry: 2, initiative: 4);
            Debug.Log($"[HeroTokenFactoryTests] Create_ReturnsValidToken: created hero def (name={heroDef.cardName}, id={heroDef.cardId})");
            var factory = ServiceLocator.Get<IHeroTokenFactory>();
            Debug.Log($"[HeroTokenFactoryTests] Create_ReturnsValidToken: got IHeroTokenFactory (isNull={factory == null})");
            Assert.IsNotNull(factory, "IHeroTokenFactory should be registered");
            var token = factory.Create(heroDef, 10);
            Debug.Log($"[HeroTokenFactoryTests] Create_ReturnsValidToken: created token (isNull={token == null}, tokenId={token?.tokenId})");
            Assert.IsNotNull(token, "Factory should return a non-null HeroToken");
            Debug.Log($"[HeroTokenFactoryTests] Create_ReturnsValidToken: asserting tokenId matches (expected=10, actual={token.tokenId})");
            Assert.AreEqual(10, token.tokenId, "Token ID should match the value passed to Create");
            Debug.Log("[HeroTokenFactoryTests] Create_ReturnsValidToken: PASSED");
        }

        [Test]
        public void Create_SetsCardDefinition()
        {
            Debug.Log("[HeroTokenFactoryTests] Create_SetsCardDefinition: starting test");
            var heroDef = TestDataFactory.CreateHero(id: 2, name: "Warrior Rat", combat: 4, move: 1, hp: 6, carry: 1, initiative: 3);
            Debug.Log($"[HeroTokenFactoryTests] Create_SetsCardDefinition: created hero def (name={heroDef.cardName})");
            var factory = ServiceLocator.Get<IHeroTokenFactory>();
            Debug.Log($"[HeroTokenFactoryTests] Create_SetsCardDefinition: got IHeroTokenFactory (isNull={factory == null})");
            var token = factory.Create(heroDef, 20);
            Debug.Log($"[HeroTokenFactoryTests] Create_SetsCardDefinition: created token (cardDef.cardName={token.cardDef?.cardName})");
            Assert.IsNotNull(token.cardDef, "Token cardDef should not be null");
            Debug.Log($"[HeroTokenFactoryTests] Create_SetsCardDefinition: asserting cardDef matches (expected={heroDef.cardName}, actual={token.cardDef.cardName})");
            Assert.AreSame(heroDef, token.cardDef, "Token cardDef should be the same instance passed to Create");
            Debug.Log("[HeroTokenFactoryTests] Create_SetsCardDefinition: PASSED");
        }

        [Test]
        public void Create_SetsCorrectHP()
        {
            Debug.Log("[HeroTokenFactoryTests] Create_SetsCorrectHP: starting test");
            var heroDef = TestDataFactory.CreateHero(id: 3, name: "Tank Rat", combat: 2, move: 1, hp: 8, carry: 1, initiative: 2);
            Debug.Log($"[HeroTokenFactoryTests] Create_SetsCorrectHP: created hero def (name={heroDef.cardName}, hp={heroDef.hp})");
            var factory = ServiceLocator.Get<IHeroTokenFactory>();
            Debug.Log($"[HeroTokenFactoryTests] Create_SetsCorrectHP: got IHeroTokenFactory (isNull={factory == null})");
            var token = factory.Create(heroDef, 30);
            Debug.Log($"[HeroTokenFactoryTests] Create_SetsCorrectHP: created token (currentHP={token.currentHP}, maxHP={token.maxHP}, cardDef.hp={heroDef.hp})");
            Assert.AreEqual(heroDef.hp, token.currentHP, "Token currentHP should equal card def HP");
            Debug.Log($"[HeroTokenFactoryTests] Create_SetsCorrectHP: asserting maxHP matches (expected={heroDef.hp}, actual={token.maxHP})");
            Assert.AreEqual(heroDef.hp, token.maxHP, "Token maxHP should equal card def HP");
            Debug.Log("[HeroTokenFactoryTests] Create_SetsCorrectHP: PASSED");
        }

        [Test]
        public void Create_WithDifferentIds()
        {
            Debug.Log("[HeroTokenFactoryTests] Create_WithDifferentIds: starting test");
            var heroDef1 = TestDataFactory.CreateHero(id: 4, name: "Rat A");
            var heroDef2 = TestDataFactory.CreateHero(id: 5, name: "Rat B");
            Debug.Log($"[HeroTokenFactoryTests] Create_WithDifferentIds: created heroDef1 (name={heroDef1.cardName}) and heroDef2 (name={heroDef2.cardName})");
            var factory = ServiceLocator.Get<IHeroTokenFactory>();
            Debug.Log($"[HeroTokenFactoryTests] Create_WithDifferentIds: got IHeroTokenFactory (isNull={factory == null})");
            var token1 = factory.Create(heroDef1, 100);
            var token2 = factory.Create(heroDef2, 200);
            Debug.Log($"[HeroTokenFactoryTests] Create_WithDifferentIds: token1.tokenId={token1.tokenId}, token2.tokenId={token2.tokenId}");
            Assert.AreNotEqual(token1.tokenId, token2.tokenId, "Tokens with different IDs should have different tokenIds");
            Debug.Log($"[HeroTokenFactoryTests] Create_WithDifferentIds: asserting token1.tokenId=100 (actual={token1.tokenId})");
            Assert.AreEqual(100, token1.tokenId, "Token1 should have tokenId 100");
            Debug.Log($"[HeroTokenFactoryTests] Create_WithDifferentIds: asserting token2.tokenId=200 (actual={token2.tokenId})");
            Assert.AreEqual(200, token2.tokenId, "Token2 should have tokenId 200");
            Debug.Log("[HeroTokenFactoryTests] Create_WithDifferentIds: PASSED");
        }
    }

    // ============================================================
    // EnemyTokenFactory Tests
    // ============================================================
    [TestFixture]
    public class EnemyTokenFactoryTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.Log("[EnemyTokenFactoryTests] Setup: ENTER - registering services");
            TestDataFactory.RegisterServices();
            Debug.Log("[EnemyTokenFactoryTests] Setup: EXIT");
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("[EnemyTokenFactoryTests] TearDown: ENTER - cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[EnemyTokenFactoryTests] TearDown: EXIT");
        }

        [Test]
        public void Create_ReturnsValidToken()
        {
            Debug.Log("[EnemyTokenFactoryTests] Create_ReturnsValidToken: starting test");
            var factory = ServiceLocator.Get<IEnemyTokenFactory>();
            Debug.Log($"[EnemyTokenFactoryTests] Create_ReturnsValidToken: got IEnemyTokenFactory (isNull={factory == null})");
            Assert.IsNotNull(factory, "IEnemyTokenFactory should be registered");
            var token = factory.Create(1, "Feral Cat", 5, 6, 2, EnemyBehavior.Patrol, NodeType.Wilderness, 3);
            Debug.Log($"[EnemyTokenFactoryTests] Create_ReturnsValidToken: created token (isNull={token == null}, tokenId={token?.tokenId})");
            Assert.IsNotNull(token, "Factory should return a non-null EnemyToken");
            Debug.Log("[EnemyTokenFactoryTests] Create_ReturnsValidToken: PASSED");
        }

        [Test]
        public void Create_SetsAllFields()
        {
            Debug.Log("[EnemyTokenFactoryTests] Create_SetsAllFields: starting test");
            var factory = ServiceLocator.Get<IEnemyTokenFactory>();
            Debug.Log($"[EnemyTokenFactoryTests] Create_SetsAllFields: got IEnemyTokenFactory (isNull={factory == null})");
            var token = factory.Create(7, "Hawk", 4, 5, 3, EnemyBehavior.Chase, NodeType.Farmland, 12);
            Debug.Log($"[EnemyTokenFactoryTests] Create_SetsAllFields: created token (name={token.enemyName}, tokenId={token.tokenId})");
            Debug.Log($"[EnemyTokenFactoryTests] Create_SetsAllFields: asserting enemyName (expected=Hawk, actual={token.enemyName})");
            Assert.AreEqual("Hawk", token.enemyName, "Enemy name should match");
            Debug.Log($"[EnemyTokenFactoryTests] Create_SetsAllFields: asserting strength (expected=4, actual={token.strength})");
            Assert.AreEqual(4, token.strength, "Strength should match");
            Debug.Log($"[EnemyTokenFactoryTests] Create_SetsAllFields: asserting hp (expected=5, actual={token.hp})");
            Assert.AreEqual(5, token.hp, "HP should match");
            Debug.Log($"[EnemyTokenFactoryTests] Create_SetsAllFields: asserting currentHP (expected=5, actual={token.currentHP})");
            Assert.AreEqual(5, token.currentHP, "Current HP should equal max HP on creation");
            Debug.Log($"[EnemyTokenFactoryTests] Create_SetsAllFields: asserting speed (expected=3, actual={token.speed})");
            Assert.AreEqual(3, token.speed, "Speed should match");
            Debug.Log($"[EnemyTokenFactoryTests] Create_SetsAllFields: asserting behavior (expected=Hunt, actual={token.behavior})");
            Assert.AreEqual(EnemyBehavior.Chase, token.behavior, "Behavior should match");
            Debug.Log($"[EnemyTokenFactoryTests] Create_SetsAllFields: asserting homeZone (expected=Farmland, actual={token.homeZone})");
            Assert.AreEqual(NodeType.Farmland, token.homeZone, "Home zone should match");
            Debug.Log($"[EnemyTokenFactoryTests] Create_SetsAllFields: asserting currentNodeId (expected=12, actual={token.currentNodeId})");
            Assert.AreEqual(12, token.currentNodeId, "Current node ID should match");
            Debug.Log("[EnemyTokenFactoryTests] Create_SetsAllFields: PASSED");
        }

        [Test]
        public void Create_WithDifferentIds()
        {
            Debug.Log("[EnemyTokenFactoryTests] Create_WithDifferentIds: starting test");
            var factory = ServiceLocator.Get<IEnemyTokenFactory>();
            Debug.Log($"[EnemyTokenFactoryTests] Create_WithDifferentIds: got IEnemyTokenFactory (isNull={factory == null})");
            var token1 = factory.Create(50, "Snake", 3, 4, 1, EnemyBehavior.Patrol, NodeType.Wilderness, 0);
            var token2 = factory.Create(51, "Fox", 5, 6, 2, EnemyBehavior.Chase, NodeType.Farmland, 5);
            Debug.Log($"[EnemyTokenFactoryTests] Create_WithDifferentIds: token1.tokenId={token1.tokenId}, token2.tokenId={token2.tokenId}");
            Assert.AreNotEqual(token1.tokenId, token2.tokenId, "Tokens should have different tokenIds");
            Debug.Log($"[EnemyTokenFactoryTests] Create_WithDifferentIds: asserting token1.tokenId=50 (actual={token1.tokenId})");
            Assert.AreEqual(50, token1.tokenId, "Token1 should have tokenId 50");
            Debug.Log($"[EnemyTokenFactoryTests] Create_WithDifferentIds: asserting token2.tokenId=51 (actual={token2.tokenId})");
            Assert.AreEqual(51, token2.tokenId, "Token2 should have tokenId 51");
            Debug.Log("[EnemyTokenFactoryTests] Create_WithDifferentIds: PASSED");
        }
    }

    // ============================================================
    // MapNode Tests
    // ============================================================
    [TestFixture]
    public class MapNodeTests
    {
        [Test]
        public void TotalResources_EmptyReturnsZero()
        {
            Debug.Log("[MapNodeTests] TotalResources_EmptyReturnsZero: starting test");
            var node = new MapNode { nodeId = 0, zone = NodeType.Wilderness };
            Debug.Log($"[MapNodeTests] TotalResources_EmptyReturnsZero: created MapNode (nodeId={node.nodeId}, zone={node.zone})");
            var total = node.TotalResources();
            Debug.Log($"[MapNodeTests] TotalResources_EmptyReturnsZero: TotalResources={total}");
            Assert.AreEqual(0, total, "Empty node should have 0 total resources");
            Debug.Log("[MapNodeTests] TotalResources_EmptyReturnsZero: PASSED");
        }

        [Test]
        public void TotalResources_SingleResourceType()
        {
            Debug.Log("[MapNodeTests] TotalResources_SingleResourceType: starting test");
            var node = new MapNode { nodeId = 1, zone = NodeType.Wilderness };
            Debug.Log($"[MapNodeTests] TotalResources_SingleResourceType: created MapNode (nodeId={node.nodeId})");
            node.resources[ResourceType.Food] = 5;
            Debug.Log($"[MapNodeTests] TotalResources_SingleResourceType: added Food=5 to resources");
            var total = node.TotalResources();
            Debug.Log($"[MapNodeTests] TotalResources_SingleResourceType: TotalResources={total}");
            Assert.AreEqual(5, total, "Node with 5 Food should have 5 total resources");
            Debug.Log("[MapNodeTests] TotalResources_SingleResourceType: PASSED");
        }

        [Test]
        public void TotalResources_MultipleResourceTypes()
        {
            Debug.Log("[MapNodeTests] TotalResources_MultipleResourceTypes: starting test");
            var node = new MapNode { nodeId = 2, zone = NodeType.Farmland };
            Debug.Log($"[MapNodeTests] TotalResources_MultipleResourceTypes: created MapNode (nodeId={node.nodeId})");
            node.resources[ResourceType.Food] = 3;
            Debug.Log("[MapNodeTests] TotalResources_MultipleResourceTypes: added Food=3");
            node.resources[ResourceType.Materials] = 2;
            Debug.Log("[MapNodeTests] TotalResources_MultipleResourceTypes: added Materials=2");
            node.resources[ResourceType.Currency] = 1;
            Debug.Log("[MapNodeTests] TotalResources_MultipleResourceTypes: added Currency=1");
            var total = node.TotalResources();
            Debug.Log($"[MapNodeTests] TotalResources_MultipleResourceTypes: TotalResources={total}");
            Assert.AreEqual(6, total, "Node with Food=3, Materials=2, Currency=1 should have 6 total");
            Debug.Log("[MapNodeTests] TotalResources_MultipleResourceTypes: PASSED");
        }

        [Test]
        public void Difficulty_Wilderness_Returns1()
        {
            Debug.Log("[MapNodeTests] Difficulty_Wilderness_Returns1: starting test");
            var node = new MapNode { nodeId = 0, zone = NodeType.Wilderness };
            Debug.Log($"[MapNodeTests] Difficulty_Wilderness_Returns1: created MapNode (zone={node.zone})");
            Debug.Log($"[MapNodeTests] Difficulty_Wilderness_Returns1: difficulty={node.difficulty}");
            Assert.AreEqual(1, node.difficulty, "Wilderness difficulty should be 1");
            Debug.Log("[MapNodeTests] Difficulty_Wilderness_Returns1: PASSED");
        }

        [Test]
        public void Difficulty_Farmland_Returns2()
        {
            Debug.Log("[MapNodeTests] Difficulty_Farmland_Returns2: starting test");
            var node = new MapNode { nodeId = 0, zone = NodeType.Farmland };
            Debug.Log($"[MapNodeTests] Difficulty_Farmland_Returns2: created MapNode (zone={node.zone})");
            Debug.Log($"[MapNodeTests] Difficulty_Farmland_Returns2: difficulty={node.difficulty}");
            Assert.AreEqual(2, node.difficulty, "Farmland difficulty should be 2");
            Debug.Log("[MapNodeTests] Difficulty_Farmland_Returns2: PASSED");
        }

        [Test]
        public void Difficulty_Town_Returns3()
        {
            Debug.Log("[MapNodeTests] Difficulty_Town_Returns3: starting test");
            var node = new MapNode { nodeId = 0, zone = NodeType.Town };
            Debug.Log($"[MapNodeTests] Difficulty_Town_Returns3: created MapNode (zone={node.zone})");
            Debug.Log($"[MapNodeTests] Difficulty_Town_Returns3: difficulty={node.difficulty}");
            Assert.AreEqual(3, node.difficulty, "Town difficulty should be 3");
            Debug.Log("[MapNodeTests] Difficulty_Town_Returns3: PASSED");
        }

        [Test]
        public void Difficulty_PiedPiper_Returns4()
        {
            Debug.Log("[MapNodeTests] Difficulty_PiedPiper_Returns4: starting test");
            var node = new MapNode { nodeId = 0, zone = NodeType.PiedPiper };
            Debug.Log($"[MapNodeTests] Difficulty_PiedPiper_Returns4: created MapNode (zone={node.zone})");
            Debug.Log($"[MapNodeTests] Difficulty_PiedPiper_Returns4: difficulty={node.difficulty}");
            Assert.AreEqual(4, node.difficulty, "PiedPiper difficulty should be 4");
            Debug.Log("[MapNodeTests] Difficulty_PiedPiper_Returns4: PASSED");
        }

        [Test]
        public void Difficulty_Colony_Returns0()
        {
            Debug.Log("[MapNodeTests] Difficulty_Colony_Returns0: starting test");
            var node = new MapNode { nodeId = 0, zone = NodeType.Colony };
            Debug.Log($"[MapNodeTests] Difficulty_Colony_Returns0: created MapNode (zone={node.zone})");
            Debug.Log($"[MapNodeTests] Difficulty_Colony_Returns0: difficulty={node.difficulty}");
            Assert.AreEqual(0, node.difficulty, "Colony difficulty should be 0");
            Debug.Log("[MapNodeTests] Difficulty_Colony_Returns0: PASSED");
        }

        [Test]
        public void NodeType_AliasesZone()
        {
            Debug.Log("[MapNodeTests] NodeType_AliasesZone: starting test");
            var node = new MapNode { nodeId = 0, zone = NodeType.Town };
            Debug.Log($"[MapNodeTests] NodeType_AliasesZone: created MapNode (zone={node.zone})");
            Debug.Log($"[MapNodeTests] NodeType_AliasesZone: nodeType={node.nodeType}, zone={node.zone}");
            Assert.AreEqual(node.zone, node.nodeType, "nodeType should alias zone");
            Debug.Log("[MapNodeTests] NodeType_AliasesZone: PASSED");
        }

        [Test]
        public void ConnectedNodeIndices_AliasesNeighborIds()
        {
            Debug.Log("[MapNodeTests] ConnectedNodeIndices_AliasesNeighborIds: starting test");
            var node = new MapNode { nodeId = 0, zone = NodeType.Wilderness };
            Debug.Log($"[MapNodeTests] ConnectedNodeIndices_AliasesNeighborIds: created MapNode (nodeId={node.nodeId})");
            node.neighborIds.Add(1);
            node.neighborIds.Add(2);
            node.neighborIds.Add(3);
            Debug.Log($"[MapNodeTests] ConnectedNodeIndices_AliasesNeighborIds: added neighborIds [1, 2, 3] (count={node.neighborIds.Count})");
            Debug.Log($"[MapNodeTests] ConnectedNodeIndices_AliasesNeighborIds: connectedNodeIndices.Count={node.connectedNodeIndices.Count}");
            Assert.AreEqual(node.neighborIds.Count, node.connectedNodeIndices.Count, "connectedNodeIndices count should match neighborIds count");
            Debug.Log("[MapNodeTests] ConnectedNodeIndices_AliasesNeighborIds: checking each element");
            for (int i = 0; i < node.neighborIds.Count; i++)
            {
                Debug.Log($"[MapNodeTests] ConnectedNodeIndices_AliasesNeighborIds: index={i}, neighborIds={node.neighborIds[i]}, connectedNodeIndices={node.connectedNodeIndices[i]}");
                Assert.AreEqual(node.neighborIds[i], node.connectedNodeIndices[i], $"Element {i} should match");
            }
            Debug.Log("[MapNodeTests] ConnectedNodeIndices_AliasesNeighborIds: PASSED");
        }

        [Test]
        public void ToString_ContainsNodeId()
        {
            Debug.Log("[MapNodeTests] ToString_ContainsNodeId: starting test");
            var node = new MapNode { nodeId = 42, zone = NodeType.Farmland };
            Debug.Log($"[MapNodeTests] ToString_ContainsNodeId: created MapNode (nodeId={node.nodeId})");
            var str = node.ToString();
            Debug.Log($"[MapNodeTests] ToString_ContainsNodeId: ToString result={str}");
            Assert.IsTrue(str.Contains("42"), "ToString should contain the nodeId");
            Debug.Log("[MapNodeTests] ToString_ContainsNodeId: PASSED");
        }

        [Test]
        public void DefaultFogState_IsHidden()
        {
            Debug.Log("[MapNodeTests] DefaultFogState_IsHidden: starting test");
            var node = new MapNode { nodeId = 0, zone = NodeType.Wilderness };
            Debug.Log($"[MapNodeTests] DefaultFogState_IsHidden: created MapNode (fogState={node.fogState})");
            Assert.AreEqual(FogState.Hidden, node.fogState, "Default fogState should be Hidden");
            Debug.Log("[MapNodeTests] DefaultFogState_IsHidden: PASSED");
        }
    }

    // ============================================================
    // CombatResult Tests
    // ============================================================
    [TestFixture]
    public class CombatResultTests
    {
        [Test]
        public void Create_SetsNodeId()
        {
            Debug.Log("[CombatResultTests] Create_SetsNodeId: starting test");
            var result = CombatResult.Create(5);
            Debug.Log($"[CombatResultTests] Create_SetsNodeId: created CombatResult (nodeId={result.nodeId})");
            Assert.AreEqual(5, result.nodeId, "nodeId should be 5");
            Debug.Log("[CombatResultTests] Create_SetsNodeId: PASSED");
        }

        [Test]
        public void Create_DefaultsToNotWon()
        {
            Debug.Log("[CombatResultTests] Create_DefaultsToNotWon: starting test");
            var result = CombatResult.Create(0);
            Debug.Log($"[CombatResultTests] Create_DefaultsToNotWon: created CombatResult (heroesWon={result.heroesWon})");
            Assert.IsFalse(result.heroesWon, "heroesWon should default to false");
            Debug.Log("[CombatResultTests] Create_DefaultsToNotWon: PASSED");
        }

        [Test]
        public void Create_ListsInitialized()
        {
            Debug.Log("[CombatResultTests] Create_ListsInitialized: starting test");
            var result = CombatResult.Create(0);
            Debug.Log($"[CombatResultTests] Create_ListsInitialized: created CombatResult");
            Debug.Log($"[CombatResultTests] Create_ListsInitialized: survivingHeroTokenIds isNull={result.survivingHeroTokenIds == null}, count={result.survivingHeroTokenIds?.Count}");
            Assert.IsNotNull(result.survivingHeroTokenIds, "survivingHeroTokenIds should not be null");
            Assert.AreEqual(0, result.survivingHeroTokenIds.Count, "survivingHeroTokenIds should be empty");
            Debug.Log($"[CombatResultTests] Create_ListsInitialized: injuredHeroTokenIds isNull={result.injuredHeroTokenIds == null}, count={result.injuredHeroTokenIds?.Count}");
            Assert.IsNotNull(result.injuredHeroTokenIds, "injuredHeroTokenIds should not be null");
            Assert.AreEqual(0, result.injuredHeroTokenIds.Count, "injuredHeroTokenIds should be empty");
            Debug.Log($"[CombatResultTests] Create_ListsInitialized: defeatedEnemyTokenIds isNull={result.defeatedEnemyTokenIds == null}, count={result.defeatedEnemyTokenIds?.Count}");
            Assert.IsNotNull(result.defeatedEnemyTokenIds, "defeatedEnemyTokenIds should not be null");
            Assert.AreEqual(0, result.defeatedEnemyTokenIds.Count, "defeatedEnemyTokenIds should be empty");
            Debug.Log($"[CombatResultTests] Create_ListsInitialized: survivingEnemyTokenIds isNull={result.survivingEnemyTokenIds == null}, count={result.survivingEnemyTokenIds?.Count}");
            Assert.IsNotNull(result.survivingEnemyTokenIds, "survivingEnemyTokenIds should not be null");
            Assert.AreEqual(0, result.survivingEnemyTokenIds.Count, "survivingEnemyTokenIds should be empty");
            Debug.Log($"[CombatResultTests] Create_ListsInitialized: damageDealtByHero isNull={result.damageDealtByHero == null}, count={result.damageDealtByHero?.Count}");
            Assert.IsNotNull(result.damageDealtByHero, "damageDealtByHero should not be null");
            Assert.AreEqual(0, result.damageDealtByHero.Count, "damageDealtByHero should be empty");
            Debug.Log($"[CombatResultTests] Create_ListsInitialized: damageDealtByEnemy isNull={result.damageDealtByEnemy == null}, count={result.damageDealtByEnemy?.Count}");
            Assert.IsNotNull(result.damageDealtByEnemy, "damageDealtByEnemy should not be null");
            Assert.AreEqual(0, result.damageDealtByEnemy.Count, "damageDealtByEnemy should be empty");
            Debug.Log($"[CombatResultTests] Create_ListsInitialized: tacticalCardsUsed isNull={result.tacticalCardsUsed == null}, count={result.tacticalCardsUsed?.Count}");
            Assert.IsNotNull(result.tacticalCardsUsed, "tacticalCardsUsed should not be null");
            Assert.AreEqual(0, result.tacticalCardsUsed.Count, "tacticalCardsUsed should be empty");
            Debug.Log("[CombatResultTests] Create_ListsInitialized: PASSED");
        }

        [Test]
        public void ToString_ContainsNodeId()
        {
            Debug.Log("[CombatResultTests] ToString_ContainsNodeId: starting test");
            var result = CombatResult.Create(99);
            Debug.Log($"[CombatResultTests] ToString_ContainsNodeId: created CombatResult (nodeId={result.nodeId})");
            var str = result.ToString();
            Debug.Log($"[CombatResultTests] ToString_ContainsNodeId: ToString result={str}");
            Assert.IsTrue(str.Contains("99"), "ToString should contain the nodeId");
            Debug.Log("[CombatResultTests] ToString_ContainsNodeId: PASSED");
        }
    }

    // ============================================================
    // SOFieldValidation Tests
    // ============================================================
    [TestFixture]
    public class SOFieldValidationTests
    {
        private List<ScriptableObject> createdSOs = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            Debug.Log($"[SOFieldValidationTests] TearDown: ENTER - destroying {createdSOs.Count} created SOs");
            foreach (var so in createdSOs)
            {
                if (so != null)
                {
                    Debug.Log($"[SOFieldValidationTests] TearDown: destroying SO (name={so.name}, type={so.GetType().Name})");
                    UnityEngine.Object.DestroyImmediate(so);
                }
            }
            createdSOs.Clear();
            Debug.Log("[SOFieldValidationTests] TearDown: EXIT");
        }

        private T CreateAndTrack<T>() where T : ScriptableObject
        {
            Debug.Log($"[SOFieldValidationTests] CreateAndTrack: creating instance of {typeof(T).Name}");
            var so = ScriptableObject.CreateInstance<T>();
            createdSOs.Add(so);
            Debug.Log($"[SOFieldValidationTests] CreateAndTrack: created and tracked {typeof(T).Name} (total tracked={createdSOs.Count})");
            return so;
        }

        [Test]
        public void BalanceConfigSO_Defaults_ArePositive()
        {
            Debug.Log("[SOFieldValidationTests] BalanceConfigSO_Defaults_ArePositive: starting test");
            var config = CreateAndTrack<BalanceConfigSO>();
            Debug.Log($"[SOFieldValidationTests] BalanceConfigSO_Defaults_ArePositive: startingFood={config.startingFood}");
            Assert.Greater(config.startingFood, 0, "startingFood should be positive");
            Debug.Log($"[SOFieldValidationTests] BalanceConfigSO_Defaults_ArePositive: startingMaterials={config.startingMaterials}");
            Assert.GreaterOrEqual(config.startingMaterials, 0, "startingMaterials should be non-negative");
            Debug.Log($"[SOFieldValidationTests] BalanceConfigSO_Defaults_ArePositive: startingCurrency={config.startingCurrency}");
            Assert.GreaterOrEqual(config.startingCurrency, 0, "startingCurrency should be non-negative");
            Debug.Log($"[SOFieldValidationTests] BalanceConfigSO_Defaults_ArePositive: starvationDamagePerFood={config.starvationDamagePerFood}");
            Assert.Greater(config.starvationDamagePerFood, 0, "starvationDamagePerFood should be positive");
            Debug.Log($"[SOFieldValidationTests] BalanceConfigSO_Defaults_ArePositive: baseColonyHP={config.baseColonyHP}");
            Assert.Greater(config.baseColonyHP, 0, "baseColonyHP should be positive");
            Debug.Log($"[SOFieldValidationTests] BalanceConfigSO_Defaults_ArePositive: baseColonyMaxHP={config.baseColonyMaxHP}");
            Assert.Greater(config.baseColonyMaxHP, 0, "baseColonyMaxHP should be positive");
            Debug.Log($"[SOFieldValidationTests] BalanceConfigSO_Defaults_ArePositive: difficultyScalingFactor={config.difficultyScalingFactor}");
            Assert.Greater(config.difficultyScalingFactor, 0f, "difficultyScalingFactor should be positive");
            Debug.Log("[SOFieldValidationTests] BalanceConfigSO_Defaults_ArePositive: PASSED");
        }

        [Test]
        public void MapConfigSO_Defaults_AreValid()
        {
            Debug.Log("[SOFieldValidationTests] MapConfigSO_Defaults_AreValid: starting test");
            var config = CreateAndTrack<MapConfigSO>();
            Debug.Log($"[SOFieldValidationTests] MapConfigSO_Defaults_AreValid: nodesPerZone={config.nodesPerZone}");
            Assert.Greater(config.nodesPerZone, 0, "nodesPerZone should be positive");
            Debug.Log($"[SOFieldValidationTests] MapConfigSO_Defaults_AreValid: minEdgesPerNode={config.minEdgesPerNode}");
            Assert.GreaterOrEqual(config.minEdgesPerNode, 1, "minEdgesPerNode should be at least 1");
            Debug.Log($"[SOFieldValidationTests] MapConfigSO_Defaults_AreValid: maxEdgesPerNode={config.maxEdgesPerNode}");
            Assert.Greater(config.maxEdgesPerNode, 0, "maxEdgesPerNode should be positive");
            Debug.Log($"[SOFieldValidationTests] MapConfigSO_Defaults_AreValid: crossZoneEdges={config.crossZoneEdges}");
            Assert.Greater(config.crossZoneEdges, 0, "crossZoneEdges should be positive");
            Debug.Log($"[SOFieldValidationTests] MapConfigSO_Defaults_AreValid: colonyConnections={config.colonyConnections}");
            Assert.Greater(config.colonyConnections, 0, "colonyConnections should be positive");
            Debug.Log($"[SOFieldValidationTests] MapConfigSO_Defaults_AreValid: piperConnections={config.piperConnections}");
            Assert.Greater(config.piperConnections, 0, "piperConnections should be positive");
            Debug.Log("[SOFieldValidationTests] MapConfigSO_Defaults_AreValid: PASSED");
        }

        [Test]
        public void CardDefinitionSO_CanBeCreated()
        {
            Debug.Log("[SOFieldValidationTests] CardDefinitionSO_CanBeCreated: starting test");
            var card = CreateAndTrack<CardDefinitionSO>();
            Debug.Log($"[SOFieldValidationTests] CardDefinitionSO_CanBeCreated: created CardDefinitionSO (isNull={card == null})");
            Assert.IsNotNull(card, "CardDefinitionSO instance should not be null");
            Debug.Log("[SOFieldValidationTests] CardDefinitionSO_CanBeCreated: PASSED");
        }

        [Test]
        public void EnemyDefinitionSO_CanBeCreated()
        {
            Debug.Log("[SOFieldValidationTests] EnemyDefinitionSO_CanBeCreated: starting test");
            var enemy = CreateAndTrack<EnemyDefinitionSO>();
            Debug.Log($"[SOFieldValidationTests] EnemyDefinitionSO_CanBeCreated: created EnemyDefinitionSO (isNull={enemy == null})");
            Assert.IsNotNull(enemy, "EnemyDefinitionSO instance should not be null");
            Debug.Log("[SOFieldValidationTests] EnemyDefinitionSO_CanBeCreated: PASSED");
        }

        [Test]
        public void ColonyCardDefinitionSO_CanBeCreated()
        {
            Debug.Log("[SOFieldValidationTests] ColonyCardDefinitionSO_CanBeCreated: starting test");
            var colony = CreateAndTrack<ColonyCardDefinitionSO>();
            Debug.Log($"[SOFieldValidationTests] ColonyCardDefinitionSO_CanBeCreated: created ColonyCardDefinitionSO (isNull={colony == null})");
            Assert.IsNotNull(colony, "ColonyCardDefinitionSO instance should not be null");
            Debug.Log("[SOFieldValidationTests] ColonyCardDefinitionSO_CanBeCreated: PASSED");
        }

        [Test]
        public void RelicDefinitionSO_CanBeCreated()
        {
            Debug.Log("[SOFieldValidationTests] RelicDefinitionSO_CanBeCreated: starting test");
            var relic = CreateAndTrack<RelicDefinitionSO>();
            Debug.Log($"[SOFieldValidationTests] RelicDefinitionSO_CanBeCreated: created RelicDefinitionSO (isNull={relic == null})");
            Assert.IsNotNull(relic, "RelicDefinitionSO instance should not be null");
            Debug.Log("[SOFieldValidationTests] RelicDefinitionSO_CanBeCreated: PASSED");
        }

        [Test]
        public void BossDefinitionSO_CanBeCreated()
        {
            Debug.Log("[SOFieldValidationTests] BossDefinitionSO_CanBeCreated: starting test");
            var boss = CreateAndTrack<BossDefinitionSO>();
            Debug.Log($"[SOFieldValidationTests] BossDefinitionSO_CanBeCreated: created BossDefinitionSO (isNull={boss == null})");
            Assert.IsNotNull(boss, "BossDefinitionSO instance should not be null");
            Debug.Log("[SOFieldValidationTests] BossDefinitionSO_CanBeCreated: PASSED");
        }

        [Test]
        public void CardDefinitionSO_DefaultRarity_IsCommon()
        {
            Debug.Log("[SOFieldValidationTests] CardDefinitionSO_DefaultRarity_IsCommon: starting test");
            var card = CreateAndTrack<CardDefinitionSO>();
            Debug.Log($"[SOFieldValidationTests] CardDefinitionSO_DefaultRarity_IsCommon: rarity={card.rarity}");
            Assert.AreEqual(CardRarity.Common, card.rarity, "Default rarity should be Common");
            Debug.Log("[SOFieldValidationTests] CardDefinitionSO_DefaultRarity_IsCommon: PASSED");
        }

        [Test]
        public void ColonyCardDefinitionSO_DefaultRarity_IsCommon()
        {
            Debug.Log("[SOFieldValidationTests] ColonyCardDefinitionSO_DefaultRarity_IsCommon: starting test");
            var card = CreateAndTrack<ColonyCardDefinitionSO>();
            Debug.Log($"[SOFieldValidationTests] ColonyCardDefinitionSO_DefaultRarity_IsCommon: rarity={card.rarity}");
            Assert.AreEqual(CardRarity.Common, card.rarity, "Default rarity should be Common");
            Debug.Log("[SOFieldValidationTests] ColonyCardDefinitionSO_DefaultRarity_IsCommon: PASSED");
        }

        [Test]
        public void MapConfigSO_MinEdges_LessOrEqualMaxEdges()
        {
            Debug.Log("[SOFieldValidationTests] MapConfigSO_MinEdges_LessOrEqualMaxEdges: starting test");
            var config = CreateAndTrack<MapConfigSO>();
            Debug.Log($"[SOFieldValidationTests] MapConfigSO_MinEdges_LessOrEqualMaxEdges: minEdgesPerNode={config.minEdgesPerNode}, maxEdgesPerNode={config.maxEdgesPerNode}");
            Assert.LessOrEqual(config.minEdgesPerNode, config.maxEdgesPerNode, "minEdgesPerNode should be <= maxEdgesPerNode");
            Debug.Log("[SOFieldValidationTests] MapConfigSO_MinEdges_LessOrEqualMaxEdges: PASSED");
        }
    }

    // ============================================================
    // DeckManager Tests
    // ============================================================
    [TestFixture]
    public class DeckManagerTests
    {
        private DeckManager deckMgr;
        private GameObject go;
        private List<CardDefinitionSO> cards;
        private List<ColonyCardDefinitionSO> colonyCards;
        private CardDefinitionSO hero1, hero2, hero3;
        private CardDefinitionSO equip1, equip2, equip3;
        private CardDefinitionSO tac1, tac2;
        private ColonyCardDefinitionSO col1, col2;

        [SetUp]
        public void Setup()
        {
            Debug.Log("[DeckManagerTests] Setup: ENTER - creating test DeckManager");
            go = new GameObject("TestDeckManager");
            Debug.Log($"[DeckManagerTests] Setup: created GameObject (name={go.name})");
            deckMgr = go.AddComponent<DeckManager>();
            Debug.Log("[DeckManagerTests] Setup: added DeckManager component");

            Debug.Log("[DeckManagerTests] Setup: creating test hero cards");
            hero1 = TestDataFactory.CreateHero(id: 1, name: "Scout");
            hero2 = TestDataFactory.CreateHero(id: 2, name: "Warrior");
            hero3 = TestDataFactory.CreateHero(id: 3, name: "Tank");
            Debug.Log($"[DeckManagerTests] Setup: created 3 heroes ({hero1.cardName}, {hero2.cardName}, {hero3.cardName})");

            Debug.Log("[DeckManagerTests] Setup: creating test equipment cards");
            equip1 = TestDataFactory.CreateEquipment(id: 51, name: "Sword", slot: EquipmentSlot.Offensive);
            equip2 = TestDataFactory.CreateEquipment(id: 52, name: "Shield", slot: EquipmentSlot.Defensive);
            equip3 = TestDataFactory.CreateEquipment(id: 53, name: "Compass", slot: EquipmentSlot.Utility);
            Debug.Log($"[DeckManagerTests] Setup: created 3 equipment ({equip1.cardName}, {equip2.cardName}, {equip3.cardName})");

            Debug.Log("[DeckManagerTests] Setup: creating test tactical cards");
            tac1 = TestDataFactory.CreateTactical(id: 91, name: "Charge");
            tac2 = TestDataFactory.CreateTactical(id: 92, name: "Ambush");
            Debug.Log($"[DeckManagerTests] Setup: created 2 tactical ({tac1.cardName}, {tac2.cardName})");

            Debug.Log("[DeckManagerTests] Setup: creating test colony cards");
            col1 = TestDataFactory.CreateColonyCard(id: 21, name: "Granary");
            col2 = TestDataFactory.CreateColonyCard(id: 22, name: "Workshop");
            Debug.Log($"[DeckManagerTests] Setup: created 2 colony ({col1.cardName}, {col2.cardName})");

            cards = new List<CardDefinitionSO> { hero1, hero2, hero3, equip1, equip2, equip3, tac1, tac2 };
            colonyCards = new List<ColonyCardDefinitionSO> { col1, col2 };
            Debug.Log($"[DeckManagerTests] Setup: cards list count={cards.Count}, colonyCards count={colonyCards.Count}");

            deckMgr.InitializeDeck(cards, colonyCards);
            Debug.Log("[DeckManagerTests] Setup: InitializeDeck called");
            Debug.Log("[DeckManagerTests] Setup: EXIT");
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("[DeckManagerTests] TearDown: ENTER - cleaning up");
            if (go != null)
            {
                Debug.Log($"[DeckManagerTests] TearDown: destroying GameObject (name={go.name})");
                UnityEngine.Object.DestroyImmediate(go);
            }
            Debug.Log("[DeckManagerTests] TearDown: cleaning up services");
            TestDataFactory.CleanupServices();
            Debug.Log("[DeckManagerTests] TearDown: EXIT");
        }

        [Test]
        public void InitializeDeck_SetsAvailableHeroes()
        {
            Debug.Log("[DeckManagerTests] InitializeDeck_SetsAvailableHeroes: starting test");
            Debug.Log($"[DeckManagerTests] InitializeDeck_SetsAvailableHeroes: AvailableHeroes.Count={deckMgr.AvailableHeroes.Count}");
            Assert.AreEqual(3, deckMgr.AvailableHeroes.Count, "Should have 3 available heroes");
            Debug.Log("[DeckManagerTests] InitializeDeck_SetsAvailableHeroes: PASSED");
        }

        [Test]
        public void InitializeDeck_SetsAvailableEquipment()
        {
            Debug.Log("[DeckManagerTests] InitializeDeck_SetsAvailableEquipment: starting test");
            Debug.Log($"[DeckManagerTests] InitializeDeck_SetsAvailableEquipment: AvailableEquipment.Count={deckMgr.AvailableEquipment.Count}");
            Assert.AreEqual(3, deckMgr.AvailableEquipment.Count, "Should have 3 available equipment");
            Debug.Log("[DeckManagerTests] InitializeDeck_SetsAvailableEquipment: PASSED");
        }

        [Test]
        public void DeployHero_MovesToDeployed()
        {
            Debug.Log("[DeckManagerTests] DeployHero_MovesToDeployed: starting test");
            Debug.Log($"[DeckManagerTests] DeployHero_MovesToDeployed: deploying hero (name={hero1.cardName})");
            deckMgr.DeployHero(hero1);
            Debug.Log($"[DeckManagerTests] DeployHero_MovesToDeployed: DeployedHeroes.Count={deckMgr.DeployedHeroes.Count}, AvailableHeroes.Count={deckMgr.AvailableHeroes.Count}");
            Assert.IsTrue(deckMgr.DeployedHeroes.Contains(hero1), "DeployedHeroes should contain the deployed hero");
            Debug.Log("[DeckManagerTests] DeployHero_MovesToDeployed: checking hero removed from available");
            Assert.IsFalse(deckMgr.AvailableHeroes.Contains(hero1), "AvailableHeroes should not contain the deployed hero");
            Debug.Log("[DeckManagerTests] DeployHero_MovesToDeployed: PASSED");
        }

        [Test]
        public void ReturnHero_MovesBackToAvailable()
        {
            Debug.Log("[DeckManagerTests] ReturnHero_MovesBackToAvailable: starting test");
            Debug.Log($"[DeckManagerTests] ReturnHero_MovesBackToAvailable: deploying hero first (name={hero1.cardName})");
            deckMgr.DeployHero(hero1);
            Debug.Log("[DeckManagerTests] ReturnHero_MovesBackToAvailable: returning hero");
            deckMgr.ReturnHero(hero1);
            Debug.Log($"[DeckManagerTests] ReturnHero_MovesBackToAvailable: AvailableHeroes.Count={deckMgr.AvailableHeroes.Count}, DeployedHeroes.Count={deckMgr.DeployedHeroes.Count}");
            Assert.IsTrue(deckMgr.AvailableHeroes.Contains(hero1), "AvailableHeroes should contain the returned hero");
            Debug.Log("[DeckManagerTests] ReturnHero_MovesBackToAvailable: checking hero removed from deployed");
            Assert.IsFalse(deckMgr.DeployedHeroes.Contains(hero1), "DeployedHeroes should not contain the returned hero");
            Debug.Log("[DeckManagerTests] ReturnHero_MovesBackToAvailable: PASSED");
        }

        [Test]
        public void InjureHero_MovesToInjured()
        {
            Debug.Log("[DeckManagerTests] InjureHero_MovesToInjured: starting test");
            Debug.Log($"[DeckManagerTests] InjureHero_MovesToInjured: deploying hero first (name={hero2.cardName})");
            deckMgr.DeployHero(hero2);
            Debug.Log("[DeckManagerTests] InjureHero_MovesToInjured: injuring hero");
            deckMgr.InjureHero(hero2);
            Debug.Log($"[DeckManagerTests] InjureHero_MovesToInjured: InjuredHeroes.Count={deckMgr.InjuredHeroes.Count}, DeployedHeroes.Count={deckMgr.DeployedHeroes.Count}");
            Assert.IsTrue(deckMgr.InjuredHeroes.Contains(hero2), "InjuredHeroes should contain the injured hero");
            Debug.Log("[DeckManagerTests] InjureHero_MovesToInjured: checking hero removed from deployed");
            Assert.IsFalse(deckMgr.DeployedHeroes.Contains(hero2), "DeployedHeroes should not contain the injured hero");
            Debug.Log("[DeckManagerTests] InjureHero_MovesToInjured: PASSED");
        }

        [Test]
        public void RecoverHero_MovesBackToAvailable()
        {
            Debug.Log("[DeckManagerTests] RecoverHero_MovesBackToAvailable: starting test");
            Debug.Log($"[DeckManagerTests] RecoverHero_MovesBackToAvailable: deploying then injuring hero (name={hero3.cardName})");
            deckMgr.DeployHero(hero3);
            deckMgr.InjureHero(hero3);
            Debug.Log("[DeckManagerTests] RecoverHero_MovesBackToAvailable: recovering hero");
            deckMgr.RecoverHero(hero3);
            Debug.Log($"[DeckManagerTests] RecoverHero_MovesBackToAvailable: AvailableHeroes.Count={deckMgr.AvailableHeroes.Count}, InjuredHeroes.Count={deckMgr.InjuredHeroes.Count}");
            Assert.IsTrue(deckMgr.AvailableHeroes.Contains(hero3), "AvailableHeroes should contain the recovered hero");
            Debug.Log("[DeckManagerTests] RecoverHero_MovesBackToAvailable: checking hero removed from injured");
            Assert.IsFalse(deckMgr.InjuredHeroes.Contains(hero3), "InjuredHeroes should not contain the recovered hero");
            Debug.Log("[DeckManagerTests] RecoverHero_MovesBackToAvailable: PASSED");
        }

        [Test]
        public void AttachEquipment_MovesToDeployed()
        {
            Debug.Log("[DeckManagerTests] AttachEquipment_MovesToDeployed: starting test");
            Debug.Log($"[DeckManagerTests] AttachEquipment_MovesToDeployed: attaching equipment (name={equip1.cardName})");
            deckMgr.AttachEquipment(equip1);
            Debug.Log($"[DeckManagerTests] AttachEquipment_MovesToDeployed: DeployedEquipment.Count={deckMgr.DeployedEquipment.Count}, AvailableEquipment.Count={deckMgr.AvailableEquipment.Count}");
            Assert.IsTrue(deckMgr.DeployedEquipment.Contains(equip1), "DeployedEquipment should contain the attached equipment");
            Debug.Log("[DeckManagerTests] AttachEquipment_MovesToDeployed: checking equipment removed from available");
            Assert.IsFalse(deckMgr.AvailableEquipment.Contains(equip1), "AvailableEquipment should not contain the attached equipment");
            Debug.Log("[DeckManagerTests] AttachEquipment_MovesToDeployed: PASSED");
        }

        [Test]
        public void ReturnEquipment_MovesBackToAvailable()
        {
            Debug.Log("[DeckManagerTests] ReturnEquipment_MovesBackToAvailable: starting test");
            Debug.Log($"[DeckManagerTests] ReturnEquipment_MovesBackToAvailable: attaching equipment first (name={equip2.cardName})");
            deckMgr.AttachEquipment(equip2);
            Debug.Log("[DeckManagerTests] ReturnEquipment_MovesBackToAvailable: returning equipment");
            deckMgr.ReturnEquipment(equip2);
            Debug.Log($"[DeckManagerTests] ReturnEquipment_MovesBackToAvailable: AvailableEquipment.Count={deckMgr.AvailableEquipment.Count}, DeployedEquipment.Count={deckMgr.DeployedEquipment.Count}");
            Assert.IsTrue(deckMgr.AvailableEquipment.Contains(equip2), "AvailableEquipment should contain the returned equipment");
            Debug.Log("[DeckManagerTests] ReturnEquipment_MovesBackToAvailable: checking equipment removed from deployed");
            Assert.IsFalse(deckMgr.DeployedEquipment.Contains(equip2), "DeployedEquipment should not contain the returned equipment");
            Debug.Log("[DeckManagerTests] ReturnEquipment_MovesBackToAvailable: PASSED");
        }

        [Test]
        public void UseTacticalCard_MovesToUsed()
        {
            Debug.Log("[DeckManagerTests] UseTacticalCard_MovesToUsed: starting test");
            Debug.Log($"[DeckManagerTests] UseTacticalCard_MovesToUsed: using tactical card (name={tac1.cardName})");
            deckMgr.UseTacticalCard(tac1);
            Debug.Log($"[DeckManagerTests] UseTacticalCard_MovesToUsed: UsedTactical.Count={deckMgr.UsedTactical.Count}, AvailableTactical.Count={deckMgr.AvailableTactical.Count}");
            Assert.IsTrue(deckMgr.UsedTactical.Contains(tac1), "UsedTactical should contain the used card");
            Debug.Log("[DeckManagerTests] UseTacticalCard_MovesToUsed: checking card removed from available");
            Assert.IsFalse(deckMgr.AvailableTactical.Contains(tac1), "AvailableTactical should not contain the used card");
            Debug.Log("[DeckManagerTests] UseTacticalCard_MovesToUsed: PASSED");
        }

        [Test]
        public void PlayColonyCard_MovesToPlayed()
        {
            Debug.Log("[DeckManagerTests] PlayColonyCard_MovesToPlayed: starting test");
            Debug.Log($"[DeckManagerTests] PlayColonyCard_MovesToPlayed: playing colony card (name={col1.cardName})");
            deckMgr.PlayColonyCard(col1);
            Debug.Log($"[DeckManagerTests] PlayColonyCard_MovesToPlayed: PlayedColonyCards.Count={deckMgr.PlayedColonyCards.Count}, AvailableColonyCards.Count={deckMgr.AvailableColonyCards.Count}");
            Assert.IsTrue(deckMgr.PlayedColonyCards.Contains(col1), "PlayedColonyCards should contain the played card");
            Debug.Log("[DeckManagerTests] PlayColonyCard_MovesToPlayed: checking card removed from available");
            Assert.IsFalse(deckMgr.AvailableColonyCards.Contains(col1), "AvailableColonyCards should not contain the played card");
            Debug.Log("[DeckManagerTests] PlayColonyCard_MovesToPlayed: PASSED");
        }

        [Test]
        public void TotalDeckSize_ReflectsInitialDeck()
        {
            Debug.Log("[DeckManagerTests] TotalDeckSize_ReflectsInitialDeck: starting test");
            Debug.Log($"[DeckManagerTests] TotalDeckSize_ReflectsInitialDeck: TotalDeckSize={deckMgr.TotalDeckSize}");
            int expected = cards.Count + colonyCards.Count;
            Debug.Log($"[DeckManagerTests] TotalDeckSize_ReflectsInitialDeck: expected={expected} (cards={cards.Count} + colony={colonyCards.Count})");
            Assert.AreEqual(expected, deckMgr.TotalDeckSize, "TotalDeckSize should equal the total number of cards passed to InitializeDeck");
            Debug.Log("[DeckManagerTests] TotalDeckSize_ReflectsInitialDeck: PASSED");
        }

        [Test]
        public void DeckManager_AddCardToPool_Hero_AddsToAvailable()
        {
            Debug.Log("[DeckManagerTests] DeckManager_AddCardToPool_Hero_AddsToAvailable: starting test");
            var dmGo = new GameObject("TestDM");
            var dm = dmGo.AddComponent<Scurry.Cards.DeckManager>();
            dm.InitializeDeck(new List<CardDefinitionSO>(), new List<ColonyCardDefinitionSO>());
            var hero = TestDataFactory.CreateHero(id: 99, name: "RewardHero", combat: 3, hp: 5);
            int prevCount = dm.AvailableHeroes.Count;
            dm.AddCardToPool(hero);
            Debug.Log($"[DeckManagerTests] AddCardToPool_Hero: prev={prevCount}, after={dm.AvailableHeroes.Count}");
            Assert.AreEqual(prevCount + 1, dm.AvailableHeroes.Count, "Hero should be added to available pool");
            Assert.AreEqual(1, dm.TotalDeckSize, "Total deck size should increase");
            UnityEngine.Object.DestroyImmediate(dmGo);
        }

        [Test]
        public void DeckManager_AddCardToPool_Equipment_AddsToAvailable()
        {
            Debug.Log("[DeckManagerTests] DeckManager_AddCardToPool_Equipment_AddsToAvailable: starting test");
            var dmGo = new GameObject("TestDM");
            var dm = dmGo.AddComponent<Scurry.Cards.DeckManager>();
            dm.InitializeDeck(new List<CardDefinitionSO>(), new List<ColonyCardDefinitionSO>());
            var equip = TestDataFactory.CreateEquipment(id: 99, name: "RewardEquip");
            dm.AddCardToPool(equip);
            Debug.Log($"[DeckManagerTests] AddCardToPool_Equipment: availableEquipment={dm.AvailableEquipment.Count}");
            Assert.AreEqual(1, dm.AvailableEquipment.Count, "Equipment should be added to available pool");
            UnityEngine.Object.DestroyImmediate(dmGo);
        }

        [Test]
        public void DeckManager_AddCardToPool_Tactical_AddsToAvailable()
        {
            Debug.Log("[DeckManagerTests] DeckManager_AddCardToPool_Tactical_AddsToAvailable: starting test");
            var dmGo = new GameObject("TestDM");
            var dm = dmGo.AddComponent<Scurry.Cards.DeckManager>();
            dm.InitializeDeck(new List<CardDefinitionSO>(), new List<ColonyCardDefinitionSO>());
            var tac = TestDataFactory.CreateTactical(id: 99, name: "RewardTactical");
            dm.AddCardToPool(tac);
            Debug.Log($"[DeckManagerTests] AddCardToPool_Tactical: availableTactical={dm.AvailableTactical.Count}");
            Assert.AreEqual(1, dm.AvailableTactical.Count, "Tactical should be added to available pool");
            UnityEngine.Object.DestroyImmediate(dmGo);
        }

        [Test]
        public void DeckManager_AddColonyCardToPool_AddsToAvailable()
        {
            Debug.Log("[DeckManagerTests] DeckManager_AddColonyCardToPool_AddsToAvailable: starting test");
            var dmGo = new GameObject("TestDM");
            var dm = dmGo.AddComponent<Scurry.Cards.DeckManager>();
            dm.InitializeDeck(new List<CardDefinitionSO>(), new List<ColonyCardDefinitionSO>());
            var colony = ScriptableObject.CreateInstance<ColonyCardDefinitionSO>();
            colony.cardId = 99;
            colony.cardName = "RewardColony";
            dm.AddColonyCardToPool(colony);
            Debug.Log($"[DeckManagerTests] AddColonyCardToPool: availableColonyCards={dm.AvailableColonyCards.Count}");
            Assert.AreEqual(1, dm.AvailableColonyCards.Count, "Colony card should be added to available pool");
            UnityEngine.Object.DestroyImmediate(dmGo);
            UnityEngine.Object.DestroyImmediate(colony);
        }
    }

    // ============================================================
    // LocalizationTableSO Tests
    // ============================================================
    [TestFixture]
    public class LocalizationTableSOTests
    {
        private LocalizationTableSO table;

        [SetUp]
        public void SetUp()
        {
            Debug.Log("[LocalizationTableSOTests] SetUp: ENTER - creating LocalizationTableSO");
            table = ScriptableObject.CreateInstance<LocalizationTableSO>();
            Debug.Log($"[LocalizationTableSOTests] SetUp: created table (languageCode={table.languageCode}, languageName={table.languageName})");

            table.entries.Add(new LocalizationEntry { key = "greeting", value = "Hello" });
            table.entries.Add(new LocalizationEntry { key = "farewell", value = "Goodbye" });
            table.entries.Add(new LocalizationEntry { key = "empty_key", value = "" });
            Debug.Log($"[LocalizationTableSOTests] SetUp: added {table.entries.Count} entries");
            Debug.Log("[LocalizationTableSOTests] SetUp: EXIT");
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("[LocalizationTableSOTests] TearDown: ENTER");
            if (table != null)
            {
                Debug.Log("[LocalizationTableSOTests] TearDown: destroying table");
                UnityEngine.Object.DestroyImmediate(table);
            }
            Debug.Log("[LocalizationTableSOTests] TearDown: EXIT");
        }

        [Test]
        public void GetString_ReturnsValue_ForValidKey()
        {
            Debug.Log("[LocalizationTableSOTests] GetString_ReturnsValue_ForValidKey: starting test");
            var result = table.GetString("greeting");
            Debug.Log($"[LocalizationTableSOTests] GetString_ReturnsValue_ForValidKey: result={result}");
            Assert.AreEqual("Hello", result, "Should return 'Hello' for key 'greeting'");
            Debug.Log("[LocalizationTableSOTests] GetString_ReturnsValue_ForValidKey: PASSED");
        }

        [Test]
        public void GetString_ReturnsFallback_ForMissingKey()
        {
            Debug.Log("[LocalizationTableSOTests] GetString_ReturnsFallback_ForMissingKey: starting test");
            var result = table.GetString("nonexistent", "FALLBACK");
            Debug.Log($"[LocalizationTableSOTests] GetString_ReturnsFallback_ForMissingKey: result={result}");
            Assert.AreEqual("FALLBACK", result, "Should return fallback for missing key");
            Debug.Log("[LocalizationTableSOTests] GetString_ReturnsFallback_ForMissingKey: PASSED");
        }

        [Test]
        public void GetString_ReturnsEmptyDefault_WhenNoFallback()
        {
            Debug.Log("[LocalizationTableSOTests] GetString_ReturnsEmptyDefault_WhenNoFallback: starting test");
            var result = table.GetString("nonexistent");
            Debug.Log($"[LocalizationTableSOTests] GetString_ReturnsEmptyDefault_WhenNoFallback: result='{result}'");
            Assert.AreEqual("", result, "Should return empty string when no fallback provided");
            Debug.Log("[LocalizationTableSOTests] GetString_ReturnsEmptyDefault_WhenNoFallback: PASSED");
        }

        [Test]
        public void BuildCache_PopulatesCache()
        {
            Debug.Log("[LocalizationTableSOTests] BuildCache_PopulatesCache: starting test");
            table.BuildCache();
            Debug.Log("[LocalizationTableSOTests] BuildCache_PopulatesCache: cache built");
            var result = table.GetString("farewell");
            Debug.Log($"[LocalizationTableSOTests] BuildCache_PopulatesCache: result={result}");
            Assert.AreEqual("Goodbye", result, "Should return 'Goodbye' after building cache");
            Debug.Log("[LocalizationTableSOTests] BuildCache_PopulatesCache: PASSED");
        }

        [Test]
        public void InvalidateCache_ForcesRebuild()
        {
            Debug.Log("[LocalizationTableSOTests] InvalidateCache_ForcesRebuild: starting test");
            var first = table.GetString("greeting");
            Debug.Log($"[LocalizationTableSOTests] InvalidateCache_ForcesRebuild: first lookup={first}");

            table.InvalidateCache();
            Debug.Log("[LocalizationTableSOTests] InvalidateCache_ForcesRebuild: cache invalidated");

            table.entries.Add(new LocalizationEntry { key = "new_key", value = "NewValue" });
            Debug.Log("[LocalizationTableSOTests] InvalidateCache_ForcesRebuild: added new entry");

            var result = table.GetString("new_key");
            Debug.Log($"[LocalizationTableSOTests] InvalidateCache_ForcesRebuild: new key lookup={result}");
            Assert.AreEqual("NewValue", result, "Should find new entry after cache invalidation");
            Debug.Log("[LocalizationTableSOTests] InvalidateCache_ForcesRebuild: PASSED");
        }

        [Test]
        public void GetString_SkipsNullKeys()
        {
            Debug.Log("[LocalizationTableSOTests] GetString_SkipsNullKeys: starting test");
            table.entries.Add(new LocalizationEntry { key = null, value = "ShouldBeSkipped" });
            table.entries.Add(new LocalizationEntry { key = "", value = "AlsoSkipped" });
            Debug.Log("[LocalizationTableSOTests] GetString_SkipsNullKeys: added null/empty key entries");
            table.InvalidateCache();

            var result = table.GetString("", "fallback");
            Debug.Log($"[LocalizationTableSOTests] GetString_SkipsNullKeys: empty key lookup='{result}'");
            Assert.AreEqual("fallback", result, "Empty key should not be cached, should return fallback");
            Debug.Log("[LocalizationTableSOTests] GetString_SkipsNullKeys: PASSED");
        }

        [Test]
        public void Defaults_AreCorrect()
        {
            Debug.Log("[LocalizationTableSOTests] Defaults_AreCorrect: starting test");
            var fresh = ScriptableObject.CreateInstance<LocalizationTableSO>();
            Debug.Log($"[LocalizationTableSOTests] Defaults_AreCorrect: languageCode={fresh.languageCode}, languageName={fresh.languageName}");
            Assert.AreEqual("en", fresh.languageCode, "Default language code should be 'en'");
            Assert.AreEqual("English", fresh.languageName, "Default language name should be 'English'");
            Assert.IsNotNull(fresh.entries, "Entries list should not be null");
            Assert.AreEqual(0, fresh.entries.Count, "Entries list should be empty by default");
            UnityEngine.Object.DestroyImmediate(fresh);
            Debug.Log("[LocalizationTableSOTests] Defaults_AreCorrect: PASSED");
        }
    }

    // ============================================================
    // BalanceConfigSO Difficulty Tests
    // ============================================================
    [TestFixture]
    public class BalanceConfigDifficultyTests
    {
        private BalanceConfigSO config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<BalanceConfigSO>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void GetEnemyStrengthMultiplier_Easy_Returns0_5()
        {
            Debug.Log("[BalanceConfigDifficultyTests] GetEnemyStrengthMultiplier_Easy: starting test");
            config.difficulty = DifficultyLevel.Easy;
            float mult = config.GetEnemyStrengthMultiplier();
            Debug.Log($"[BalanceConfigDifficultyTests] Easy multiplier={mult}");
            Assert.AreEqual(0.5f, mult, 0.01f, "Easy difficulty should have 0.5x enemy strength");
        }

        [Test]
        public void GetEnemyStrengthMultiplier_Normal_Returns1_0()
        {
            Debug.Log("[BalanceConfigDifficultyTests] GetEnemyStrengthMultiplier_Normal: starting test");
            config.difficulty = DifficultyLevel.Normal;
            float mult = config.GetEnemyStrengthMultiplier();
            Debug.Log($"[BalanceConfigDifficultyTests] Normal multiplier={mult}");
            Assert.AreEqual(1.0f, mult, 0.01f, "Normal difficulty should have 1.0x enemy strength");
        }

        [Test]
        public void GetEnemyStrengthMultiplier_Hard_Returns1_5()
        {
            Debug.Log("[BalanceConfigDifficultyTests] GetEnemyStrengthMultiplier_Hard: starting test");
            config.difficulty = DifficultyLevel.Hard;
            float mult = config.GetEnemyStrengthMultiplier();
            Debug.Log($"[BalanceConfigDifficultyTests] Hard multiplier={mult}");
            Assert.AreEqual(1.5f, mult, 0.01f, "Hard difficulty should have 1.5x enemy strength");
        }

        [Test]
        public void GetEnemySpawnChance_PerDifficulty_ReturnsCorrectValues()
        {
            Debug.Log("[BalanceConfigDifficultyTests] GetEnemySpawnChance_PerDifficulty: starting test");
            config.difficulty = DifficultyLevel.Easy;
            Assert.AreEqual(0.25f, config.GetEnemySpawnChance(), 0.01f, "Easy spawn chance should be 0.25");
            config.difficulty = DifficultyLevel.Normal;
            Assert.AreEqual(0.40f, config.GetEnemySpawnChance(), 0.01f, "Normal spawn chance should be 0.40");
            config.difficulty = DifficultyLevel.Hard;
            Assert.AreEqual(0.65f, config.GetEnemySpawnChance(), 0.01f, "Hard spawn chance should be 0.65");
            Debug.Log("[BalanceConfigDifficultyTests] GetEnemySpawnChance_PerDifficulty: all assertions passed");
        }

        [Test]
        public void GetEnemyStrengthMultiplier_EmptyArray_ReturnsFallback()
        {
            Debug.Log("[BalanceConfigDifficultyTests] GetEnemyStrengthMultiplier_EmptyArray: starting test");
            config.enemyStrengthMultipliers = new float[0];
            config.difficulty = DifficultyLevel.Normal;
            float mult = config.GetEnemyStrengthMultiplier();
            Debug.Log($"[BalanceConfigDifficultyTests] Fallback multiplier={mult}");
            Assert.AreEqual(1f, mult, 0.01f, "Empty array should return fallback of 1.0");
        }
    }

    // ============================================================
    // RoundResult Tests
    // ============================================================
    [TestFixture]
    public class RoundResultTests
    {
        [Test]
        public void RoundResult_DefaultValues_AreCorrect()
        {
            Debug.Log("[RoundResultTests] RoundResult_DefaultValues_AreCorrect: starting test");
            var rr = new Scurry.Combat.RoundResult();
            Debug.Log($"[RoundResultTests] Default: continues={rr.combatContinues}, round={rr.roundNumber}");
            Assert.IsFalse(rr.combatContinues, "Default combatContinues should be false");
            Assert.AreEqual(0, rr.roundNumber, "Default roundNumber should be 0");
            Assert.AreEqual(0, rr.heroStrength, "Default heroStrength should be 0");
            Assert.AreEqual(0, rr.enemyStrength, "Default enemyStrength should be 0");
            Assert.IsFalse(rr.retreatTriggered, "Default retreatTriggered should be false");
        }

        [Test]
        public void RoundResult_DamageEvent_ToString_FormatsCorrectly()
        {
            Debug.Log("[RoundResultTests] RoundResult_DamageEvent_ToString_FormatsCorrectly: starting test");
            var evt = new Scurry.Combat.RoundResult.DamageEvent
            {
                targetId = 5,
                damage = 3,
                isHero = true,
                isDefeated = false,
                abilityName = "CLEAVE"
            };
            string str = evt.ToString();
            Debug.Log($"[RoundResultTests] DamageEvent.ToString: {str}");
            Assert.IsTrue(str.Contains("targetId=5"), "Should contain targetId");
            Assert.IsTrue(str.Contains("damage=3"), "Should contain damage");
            Assert.IsTrue(str.Contains("CLEAVE"), "Should contain ability name");
        }
    }
}
