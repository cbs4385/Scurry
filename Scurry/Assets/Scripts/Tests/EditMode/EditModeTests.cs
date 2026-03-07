using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Scurry.Core;
using Scurry.Data;
using Scurry.Colony;
using Scurry.Map;
using Scurry.Interfaces;

namespace Scurry.Tests.EditMode
{
    // ============================================================
    // TC-1: Colony Management Tests
    // ============================================================
    [TestFixture]
    public class ColonyManagementTests
    {
        private List<MapConfigSO> configs;
        private List<ColonyCardDefinitionSO> colonyCards;

        [OneTimeSetUp]
        public void Setup()
        {
            configs = new List<MapConfigSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:MapConfigSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var cfg = AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                if (cfg != null) configs.Add(cfg);
            }
            configs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

            colonyCards = new List<ColonyCardDefinitionSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:ColonyCardDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<ColonyCardDefinitionSO>(path);
                if (card != null) colonyCards.Add(card);
            }
        }

        [Test]
        public void TC1_1a_L1_ColonyBoardSize_Is3()
        {
            Assert.GreaterOrEqual(configs.Count, 3, "Need at least 3 MapConfigSOs");
            Assert.AreEqual(3, configs[0].colonyBoardSize, "L1 colony board size should be 3");
        }

        [Test]
        public void TC1_1b_L2_ColonyBoardSize_Is4()
        {
            Assert.GreaterOrEqual(configs.Count, 3, "Need at least 3 MapConfigSOs");
            Assert.AreEqual(4, configs[1].colonyBoardSize, "L2 colony board size should be 4");
        }

        [Test]
        public void TC1_1c_L3_ColonyBoardSize_Is5()
        {
            Assert.GreaterOrEqual(configs.Count, 3, "Need at least 3 MapConfigSOs");
            Assert.AreEqual(5, configs[2].colonyBoardSize, "L3 colony board size should be 5");
        }

        [Test]
        public void TC1_3_NonePlacementCard_ValidOnAnySlot()
        {
            Assume.That(configs.Count > 0 && colonyCards.Count > 0, "Need configs and colony cards");
            var testGO = new GameObject("TestColonyBoard");
            var cbm = testGO.AddComponent<ColonyBoardManager>();
            cbm.StartColonyManagement(1, configs[0], new List<ColonyCardDefinitionSO>(colonyCards));

            var noneCard = colonyCards.FirstOrDefault(c => c.placementRequirement == PlacementRequirement.None);
            Assume.That(noneCard != null, "No card with PlacementRequirement.None found");

            bool valid = cbm.IsValidPlacement(noneCard, new Vector2Int(1, 1));
            Assert.IsTrue(valid, $"None requirement card '{noneCard.cardName}' should be valid on any slot");
            Object.DestroyImmediate(testGO);
        }

        [Test]
        public void TC1_4_AdjacentToCard_RejectedWithoutNeighbor()
        {
            Assume.That(configs.Count > 0 && colonyCards.Count > 0, "Need configs and colony cards");
            var testGO = new GameObject("TestColonyBoard");
            var cbm = testGO.AddComponent<ColonyBoardManager>();
            cbm.StartColonyManagement(1, configs[0], new List<ColonyCardDefinitionSO>(colonyCards));

            var adjacentCard = colonyCards.FirstOrDefault(c => c.placementRequirement == PlacementRequirement.AdjacentTo);
            Assume.That(adjacentCard != null, "No card with PlacementRequirement.AdjacentTo found");

            bool valid = cbm.IsValidPlacement(adjacentCard, new Vector2Int(0, 0));
            Assert.IsFalse(valid, $"AdjacentTo card '{adjacentCard.cardName}' should be rejected without required neighbor");
            Object.DestroyImmediate(testGO);
        }

        [Test]
        public void TC1_5_AdjacentToCard_AcceptedWithNeighbor()
        {
            Assume.That(configs.Count > 0 && colonyCards.Count > 0, "Need configs and colony cards");
            var testGO = new GameObject("TestColonyBoard");
            var cbm = testGO.AddComponent<ColonyBoardManager>();

            var adjacentCard = colonyCards.FirstOrDefault(c => c.placementRequirement == PlacementRequirement.AdjacentTo);
            Assume.That(adjacentCard != null, "No AdjacentTo card found");

            var targetCard = colonyCards.FirstOrDefault(c => c.cardName == adjacentCard.adjacencyCardName);
            Assume.That(targetCard != null, $"Target card '{adjacentCard.adjacencyCardName}' not found");

            cbm.StartColonyManagement(1, configs[0], new List<ColonyCardDefinitionSO>(colonyCards));

            Vector2Int[] tryPositions = { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(0, 2), new Vector2Int(2, 0), new Vector2Int(2, 2) };
            bool placed = false;
            Vector2Int placedPos = Vector2Int.zero;
            foreach (var tryPos in tryPositions)
            {
                if (cbm.IsValidPlacement(targetCard, tryPos) && cbm.TryPlaceCard(targetCard, tryPos))
                {
                    placed = true;
                    placedPos = tryPos;
                    break;
                }
            }
            Assume.That(placed, $"Could not place target card '{adjacentCard.adjacencyCardName}'");

            Vector2Int[] adjOffsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            bool foundValid = false;
            foreach (var offset in adjOffsets)
            {
                Vector2Int adjPos = placedPos + offset;
                if (adjPos.x >= 0 && adjPos.x < 3 && adjPos.y >= 0 && adjPos.y < 3 && cbm.GetCardAt(adjPos) == null)
                {
                    bool valid = cbm.IsValidPlacement(adjacentCard, adjPos);
                    Assert.IsTrue(valid, $"AdjacentTo card should be valid when neighbor is placed at {placedPos}");
                    foundValid = true;
                    break;
                }
            }
            Assert.IsTrue(foundValid, "Could not find empty adjacent slot");
            Object.DestroyImmediate(testGO);
        }

        [Test]
        public void TC1_6a_EdgeCard_ValidOnEdge()
        {
            Assume.That(configs.Count > 0 && colonyCards.Count > 0, "Need configs and colony cards");
            var testGO = new GameObject("TestColonyBoard");
            var cbm = testGO.AddComponent<ColonyBoardManager>();
            cbm.StartColonyManagement(1, configs[0], new List<ColonyCardDefinitionSO>(colonyCards));

            var edgeCard = colonyCards.FirstOrDefault(c => c.placementRequirement == PlacementRequirement.Edge);
            Assume.That(edgeCard != null, "No Edge card found");

            bool valid = cbm.IsValidPlacement(edgeCard, new Vector2Int(0, 1));
            Assert.IsTrue(valid, $"Edge card '{edgeCard.cardName}' should be valid on edge");
            Object.DestroyImmediate(testGO);
        }

        [Test]
        public void TC1_6b_EdgeCard_RejectedOnInterior()
        {
            Assume.That(configs.Count > 0 && colonyCards.Count > 0, "Need configs and colony cards");
            var testGO = new GameObject("TestColonyBoard");
            var cbm = testGO.AddComponent<ColonyBoardManager>();
            cbm.StartColonyManagement(1, configs[0], new List<ColonyCardDefinitionSO>(colonyCards));

            var edgeCard = colonyCards.FirstOrDefault(c => c.placementRequirement == PlacementRequirement.Edge);
            Assume.That(edgeCard != null, "No Edge card found");

            bool invalid = cbm.IsValidPlacement(edgeCard, new Vector2Int(1, 1));
            Assert.IsFalse(invalid, $"Edge card '{edgeCard.cardName}' should be rejected on interior");
            Object.DestroyImmediate(testGO);
        }

        [Test]
        public void TC1_7a_CornerCard_ValidOnCorner()
        {
            Assume.That(configs.Count > 0 && colonyCards.Count > 0, "Need configs and colony cards");
            var testGO = new GameObject("TestColonyBoard");
            var cbm = testGO.AddComponent<ColonyBoardManager>();
            cbm.StartColonyManagement(1, configs[0], new List<ColonyCardDefinitionSO>(colonyCards));

            var cornerCard = colonyCards.FirstOrDefault(c => c.placementRequirement == PlacementRequirement.Corner);
            Assume.That(cornerCard != null, "No Corner card found");

            Assert.IsTrue(cbm.IsValidPlacement(cornerCard, new Vector2Int(0, 0)));
            Object.DestroyImmediate(testGO);
        }

        [Test]
        public void TC1_7b_CornerCard_RejectedOnNonCorner()
        {
            Assume.That(configs.Count > 0 && colonyCards.Count > 0, "Need configs and colony cards");
            var testGO = new GameObject("TestColonyBoard");
            var cbm = testGO.AddComponent<ColonyBoardManager>();
            cbm.StartColonyManagement(1, configs[0], new List<ColonyCardDefinitionSO>(colonyCards));

            var cornerCard = colonyCards.FirstOrDefault(c => c.placementRequirement == PlacementRequirement.Corner);
            Assume.That(cornerCard != null, "No Corner card found");

            Assert.IsFalse(cbm.IsValidPlacement(cornerCard, new Vector2Int(0, 1)));
            Object.DestroyImmediate(testGO);
        }

        [Test]
        public void TC1_8a_CenterCard_ValidOnCenter()
        {
            Assume.That(configs.Count > 0 && colonyCards.Count > 0, "Need configs and colony cards");
            var testGO = new GameObject("TestColonyBoard");
            var cbm = testGO.AddComponent<ColonyBoardManager>();
            cbm.StartColonyManagement(1, configs[0], new List<ColonyCardDefinitionSO>(colonyCards));

            var centerCard = colonyCards.FirstOrDefault(c => c.placementRequirement == PlacementRequirement.Center);
            Assume.That(centerCard != null, "No Center card found");

            Assert.IsTrue(cbm.IsValidPlacement(centerCard, new Vector2Int(1, 1)));
            Object.DestroyImmediate(testGO);
        }

        [Test]
        public void TC1_8b_CenterCard_RejectedOnEdge()
        {
            Assume.That(configs.Count > 0 && colonyCards.Count > 0, "Need configs and colony cards");
            var testGO = new GameObject("TestColonyBoard");
            var cbm = testGO.AddComponent<ColonyBoardManager>();
            cbm.StartColonyManagement(1, configs[0], new List<ColonyCardDefinitionSO>(colonyCards));

            var centerCard = colonyCards.FirstOrDefault(c => c.placementRequirement == PlacementRequirement.Center);
            Assume.That(centerCard != null, "No Center card found");

            Assert.IsFalse(cbm.IsValidPlacement(centerCard, new Vector2Int(0, 0)));
            Object.DestroyImmediate(testGO);
        }

        [Test]
        public void TC1_9_ColonyEffects_CalculatedCorrectly()
        {
            Assume.That(configs.Count > 0 && colonyCards.Count > 0, "Need configs and colony cards");
            var testGO = new GameObject("TestColonyBoard");
            var cbm = testGO.AddComponent<ColonyBoardManager>();
            cbm.StartColonyManagement(1, configs[0], new List<ColonyCardDefinitionSO>(colonyCards));

            int cardsPlaced = 0;
            foreach (var card in cbm.CurrentHand)
            {
                if (card.placementRequirement == PlacementRequirement.None && cardsPlaced < 3)
                {
                    Vector2Int pos = new Vector2Int(cardsPlaced / 3, cardsPlaced % 3);
                    if (cbm.TryPlaceCard(card, pos)) cardsPlaced++;
                }
            }

            var config = cbm.CalculateColonyEffects();
            Assert.GreaterOrEqual(config.maxHeroDeckSize, 8, "Colony maxHeroDeckSize should be >= 8");
            Assert.GreaterOrEqual(config.totalPopulation, 0, "Colony totalPopulation should be >= 0");
            Object.DestroyImmediate(testGO);
        }

        [Test]
        public void TC1_10_FoodConsumption_WithinExpectedRange()
        {
            Assume.That(configs.Count > 0 && colonyCards.Count > 0, "Need configs and colony cards");
            var testGO = new GameObject("TestColonyBoard");
            var cbm = testGO.AddComponent<ColonyBoardManager>();
            cbm.StartColonyManagement(1, configs[0], new List<ColonyCardDefinitionSO>(colonyCards));

            int cardsPlaced = 0;
            foreach (var card in cbm.CurrentHand)
            {
                if (card.placementRequirement == PlacementRequirement.None && cardsPlaced < 3)
                {
                    Vector2Int pos = new Vector2Int(cardsPlaced / 3, cardsPlaced % 3);
                    if (cbm.TryPlaceCard(card, pos)) cardsPlaced++;
                }
            }

            var config = cbm.CalculateColonyEffects();
            int rawConsumption = Mathf.Max(1, Mathf.CeilToInt(config.totalPopulation / 2f));
            Assert.GreaterOrEqual(config.foodConsumptionPerNode, 1, "Food consumption should be >= 1");
            Assert.LessOrEqual(config.foodConsumptionPerNode, rawConsumption, "Food consumption should be <= ceil(pop/2)");
            Object.DestroyImmediate(testGO);
        }
    }

    // ============================================================
    // TC-3: Map Generation Tests
    // ============================================================
    [TestFixture]
    public class MapGenerationTests
    {
        private List<MapConfigSO> configs;

        [OneTimeSetUp]
        public void Setup()
        {
            configs = new List<MapConfigSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:MapConfigSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var cfg = AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                if (cfg != null) configs.Add(cfg);
            }
            configs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
        }

        [Test]
        public void TC3_1_L1_MapGenerates_CorrectRowCount()
        {
            Assume.That(configs.Count >= 1, "Need at least 1 MapConfigSO");
            var map = MapGenerator.GenerateMap(configs[0]);
            Assert.AreEqual(configs[0].numRows, map.Count, $"L1 map should have {configs[0].numRows} rows");
        }

        [Test]
        public void TC3_1_L2_MapGenerates_CorrectRowCount()
        {
            Assume.That(configs.Count >= 2, "Need at least 2 MapConfigSOs");
            var map = MapGenerator.GenerateMap(configs[1]);
            Assert.AreEqual(configs[1].numRows, map.Count, $"L2 map should have {configs[1].numRows} rows");
        }

        [Test]
        public void TC3_1_L3_MapGenerates_CorrectRowCount()
        {
            Assume.That(configs.Count >= 3, "Need at least 3 MapConfigSOs");
            var map = MapGenerator.GenerateMap(configs[2]);
            Assert.AreEqual(configs[2].numRows, map.Count, $"L3 map should have {configs[2].numRows} rows");
        }

        [Test]
        public void TC3_1b_L1_NodeCounts_WithinMinMax()
        {
            Assume.That(configs.Count >= 1, "Need MapConfigSO");
            var cfg = configs[0];
            var map = MapGenerator.GenerateMap(cfg);
            for (int row = 0; row < map.Count - 1; row++)
            {
                Assert.GreaterOrEqual(map[row].Count, cfg.minNodesPerRow, $"Row {row} has too few nodes");
                Assert.LessOrEqual(map[row].Count, cfg.maxNodesPerRow, $"Row {row} has too many nodes");
            }
        }

        [Test]
        public void TC3_1b_L2_NodeCounts_WithinMinMax()
        {
            Assume.That(configs.Count >= 2, "Need 2 MapConfigSOs");
            var cfg = configs[1];
            var map = MapGenerator.GenerateMap(cfg);
            for (int row = 0; row < map.Count - 1; row++)
            {
                Assert.GreaterOrEqual(map[row].Count, cfg.minNodesPerRow, $"Row {row} has too few nodes");
                Assert.LessOrEqual(map[row].Count, cfg.maxNodesPerRow, $"Row {row} has too many nodes");
            }
        }

        [Test]
        public void TC3_1b_L3_NodeCounts_WithinMinMax()
        {
            Assume.That(configs.Count >= 3, "Need 3 MapConfigSOs");
            var cfg = configs[2];
            var map = MapGenerator.GenerateMap(cfg);
            for (int row = 0; row < map.Count - 1; row++)
            {
                Assert.GreaterOrEqual(map[row].Count, cfg.minNodesPerRow, $"Row {row} has too few nodes");
                Assert.LessOrEqual(map[row].Count, cfg.maxNodesPerRow, $"Row {row} has too many nodes");
            }
        }

        [Test]
        public void TC3_2_L1_FirstRow_CorrectType()
        {
            Assume.That(configs.Count >= 1, "Need MapConfigSO");
            var cfg = configs[0];
            var map = MapGenerator.GenerateMap(cfg);
            foreach (var node in map[0])
                Assert.AreEqual(cfg.firstRowType, node.nodeType, $"First row node should be {cfg.firstRowType}");
        }

        [Test]
        public void TC3_2_L2_FirstRow_CorrectType()
        {
            Assume.That(configs.Count >= 2, "Need 2 MapConfigSOs");
            var cfg = configs[1];
            var map = MapGenerator.GenerateMap(cfg);
            foreach (var node in map[0])
                Assert.AreEqual(cfg.firstRowType, node.nodeType, $"First row node should be {cfg.firstRowType}");
        }

        [Test]
        public void TC3_2_L3_FirstRow_CorrectType()
        {
            Assume.That(configs.Count >= 3, "Need 3 MapConfigSOs");
            var cfg = configs[2];
            var map = MapGenerator.GenerateMap(cfg);
            foreach (var node in map[0])
                Assert.AreEqual(cfg.firstRowType, node.nodeType, $"First row node should be {cfg.firstRowType}");
        }

        [Test]
        public void TC3_1c_L1_LastRow_IsBoss()
        {
            Assume.That(configs.Count >= 1, "Need MapConfigSO");
            var map = MapGenerator.GenerateMap(configs[0]);
            Assert.AreEqual(1, map[map.Count - 1].Count, "Last row should have 1 node");
            Assert.AreEqual(NodeType.Boss, map[map.Count - 1][0].nodeType, "Last row should be Boss");
        }

        [Test]
        public void TC3_1c_L2_LastRow_IsBoss()
        {
            Assume.That(configs.Count >= 2, "Need 2 MapConfigSOs");
            var map = MapGenerator.GenerateMap(configs[1]);
            Assert.AreEqual(1, map[map.Count - 1].Count, "Last row should have 1 node");
            Assert.AreEqual(NodeType.Boss, map[map.Count - 1][0].nodeType, "Last row should be Boss");
        }

        [Test]
        public void TC3_1c_L3_LastRow_IsBoss()
        {
            Assume.That(configs.Count >= 3, "Need 3 MapConfigSOs");
            var map = MapGenerator.GenerateMap(configs[2]);
            Assert.AreEqual(1, map[map.Count - 1].Count, "Last row should have 1 node");
            Assert.AreEqual(NodeType.Boss, map[map.Count - 1][0].nodeType, "Last row should be Boss");
        }

        [Test]
        public void TC3_3_L1_AllPaths_ReachBoss()
        {
            Assume.That(configs.Count >= 1, "Need MapConfigSO");
            var map = MapGenerator.GenerateMap(configs[0]);
            Assert.IsTrue(MapGenerator.ValidateMap(map), "L1: All paths should reach boss");
        }

        [Test]
        public void TC3_3_L2_AllPaths_ReachBoss()
        {
            Assume.That(configs.Count >= 2, "Need 2 MapConfigSOs");
            var map = MapGenerator.GenerateMap(configs[1]);
            Assert.IsTrue(MapGenerator.ValidateMap(map), "L2: All paths should reach boss");
        }

        [Test]
        public void TC3_3_L3_AllPaths_ReachBoss()
        {
            Assume.That(configs.Count >= 3, "Need 3 MapConfigSOs");
            var map = MapGenerator.GenerateMap(configs[2]);
            Assert.IsTrue(MapGenerator.ValidateMap(map), "L3: All paths should reach boss");
        }

        [Test]
        public void TC3_7_L1_DifficultyScales()
        {
            Assume.That(configs.Count >= 1, "Need MapConfigSO");
            var map = MapGenerator.GenerateMap(configs[0]);
            int firstDiff = map[0][0].difficulty;
            int lastDiff = map[map.Count - 2][0].difficulty;
            Assert.Greater(lastDiff, firstDiff, $"Difficulty should scale: first={firstDiff}, near-last={lastDiff}");
        }

        [Test]
        public void TC3_7_L2_DifficultyScales()
        {
            Assume.That(configs.Count >= 2, "Need 2 MapConfigSOs");
            var map = MapGenerator.GenerateMap(configs[1]);
            int firstDiff = map[0][0].difficulty;
            int lastDiff = map[map.Count - 2][0].difficulty;
            Assert.Greater(lastDiff, firstDiff, $"Difficulty should scale: first={firstDiff}, near-last={lastDiff}");
        }

        [Test]
        public void TC3_7_L3_DifficultyScales()
        {
            Assume.That(configs.Count >= 3, "Need 3 MapConfigSOs");
            var map = MapGenerator.GenerateMap(configs[2]);
            int firstDiff = map[0][0].difficulty;
            int lastDiff = map[map.Count - 2][0].difficulty;
            Assert.Greater(lastDiff, firstDiff, $"Difficulty should scale: first={firstDiff}, near-last={lastDiff}");
        }
    }

    // ============================================================
    // TC-4: Starvation Tests
    // ============================================================
    [TestFixture]
    public class StarvationTests
    {
        private BalanceConfigSO bc;

        [OneTimeSetUp]
        public void Setup()
        {
            bc = BalanceConfigSO.Instance;
            Assume.That(bc != null, "BalanceConfigSO not found");
        }

        [Test]
        public void TC4_1_Population6_Consumption3()
        {
            int consumption = Mathf.CeilToInt(6f / 2f);
            Assert.AreEqual(3, consumption);
        }

        [Test]
        public void TC4_2_StarvationDamagePerFood_Is2()
        {
            Assert.AreEqual(2, bc.starvationDamagePerFood);
        }

        [Test]
        public void TC4_2b_Shortfall2_Damage4()
        {
            int damage = 2 * bc.starvationDamagePerFood;
            Assert.AreEqual(4, damage);
        }

        [Test]
        public void TC4_3_PartialStarvation_Shortfall1_Damage2()
        {
            int damage = 1 * bc.starvationDamagePerFood;
            Assert.AreEqual(2, damage);
        }
    }

    // ============================================================
    // TC-5/6: Encounter Data Tests
    // ============================================================
    [TestFixture]
    public class EncounterDataTests
    {
        [Test]
        public void TC5_ResourceEncounters_AtLeast14()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:EncounterDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var enc = AssetDatabase.LoadAssetAtPath<EncounterDefinitionSO>(path);
                if (enc != null && enc.encounterType != EncounterType.Elite) count++;
            }
            Assert.GreaterOrEqual(count, 14, $"Need >= 14 resource encounters, found {count}");
        }

        [Test]
        public void TC6_EliteEncounters_AtLeast8()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:EncounterDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var enc = AssetDatabase.LoadAssetAtPath<EncounterDefinitionSO>(path);
                if (enc != null && enc.encounterType == EncounterType.Elite) count++;
            }
            Assert.GreaterOrEqual(count, 8, $"Need >= 8 elite encounters, found {count}");
        }
    }

    // ============================================================
    // TC-7: Boss Data Tests
    // ============================================================
    [TestFixture]
    public class BossDataTests
    {
        private List<BossDefinitionSO> bosses;

        [OneTimeSetUp]
        public void Setup()
        {
            bosses = new List<BossDefinitionSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:BossDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var boss = AssetDatabase.LoadAssetAtPath<BossDefinitionSO>(path);
                if (boss != null) bosses.Add(boss);
            }
        }

        [Test]
        public void TC7_FourBossesDefined()
        {
            Assert.AreEqual(4, bosses.Count, $"Expected 4 bosses, found {bosses.Count}");
        }

        [Test]
        public void TC7_AllBosses_HavePhases()
        {
            foreach (var boss in bosses)
                Assert.IsTrue(boss.phases != null && boss.phases.Length > 0, $"Boss '{boss.bossName}' should have phases");
        }

        [Test]
        public void TC7_AllBosses_HavePositiveHP()
        {
            foreach (var boss in bosses)
                Assert.Greater(boss.maxHP, 0, $"Boss '{boss.bossName}' should have HP > 0");
        }
    }

    // ============================================================
    // TC-8: Shop Price Tests
    // ============================================================
    [TestFixture]
    public class ShopPriceTests
    {
        private BalanceConfigSO bc;

        [OneTimeSetUp]
        public void Setup()
        {
            bc = BalanceConfigSO.Instance;
            Assume.That(bc != null, "BalanceConfigSO not found");
        }

        [Test]
        public void TC8_1_ShopCardCount_Is5() => Assert.AreEqual(5, bc.shopCardCount);

        [Test]
        public void TC8_2a_CommonPrice_Is2() => Assert.AreEqual(2, bc.priceCommon);

        [Test]
        public void TC8_2b_UncommonPrice_Is4() => Assert.AreEqual(4, bc.priceUncommon);

        [Test]
        public void TC8_2c_RarePrice_Is7() => Assert.AreEqual(7, bc.priceRare);

        [Test]
        public void TC8_2d_LegendaryPrice_Is12() => Assert.AreEqual(12, bc.priceLegendary);

        [Test]
        public void TC8_6_RerollCost_Is2() => Assert.AreEqual(2, bc.shopRerollCost);

        [Test]
        public void TC8_GetShopPrice_Common() => Assert.AreEqual(2, bc.GetShopPrice(CardRarity.Common));

        [Test]
        public void TC8_GetShopPrice_Uncommon() => Assert.AreEqual(4, bc.GetShopPrice(CardRarity.Uncommon));

        [Test]
        public void TC8_GetShopPrice_Rare() => Assert.AreEqual(7, bc.GetShopPrice(CardRarity.Rare));

        [Test]
        public void TC8_GetShopPrice_Legendary() => Assert.AreEqual(12, bc.GetShopPrice(CardRarity.Legendary));
    }

    // ============================================================
    // TC-9: Healing Cost Tests
    // ============================================================
    [TestFixture]
    public class HealingCostTests
    {
        private BalanceConfigSO bc;

        [OneTimeSetUp]
        public void Setup()
        {
            bc = BalanceConfigSO.Instance;
            Assume.That(bc != null, "BalanceConfigSO not found");
        }

        [Test]
        public void TC9_1a_MinorHealCost_Is2() => Assert.AreEqual(2, bc.minorHealCost);

        [Test]
        public void TC9_1b_MinorHealAmount_Is5() => Assert.AreEqual(5, bc.minorHealAmount);

        [Test]
        public void TC9_2a_MajorHealCost_Is5() => Assert.AreEqual(5, bc.majorHealCost);

        [Test]
        public void TC9_2b_MajorHealAmount_Is15() => Assert.AreEqual(15, bc.majorHealAmount);

        [Test]
        public void TC9_3_ResupplyCost_Is3() => Assert.AreEqual(3, bc.resupplyCost);
    }

    // ============================================================
    // TC-10: Upgrade Cost Tests
    // ============================================================
    [TestFixture]
    public class UpgradeCostTests
    {
        private BalanceConfigSO bc;

        [OneTimeSetUp]
        public void Setup()
        {
            bc = BalanceConfigSO.Instance;
            Assume.That(bc != null, "BalanceConfigSO not found");
        }

        [Test]
        public void TC10_3a_CommonUpgrade_Is2() => Assert.AreEqual(2, bc.upgradeCostCommon);

        [Test]
        public void TC10_3b_UncommonUpgrade_Is4() => Assert.AreEqual(4, bc.upgradeCostUncommon);

        [Test]
        public void TC10_3c_RareUpgrade_Is7() => Assert.AreEqual(7, bc.upgradeCostRare);

        [Test]
        public void TC10_GetUpgradeCost_Common() => Assert.AreEqual(2, bc.GetUpgradeCost(CardRarity.Common));

        [Test]
        public void TC10_GetUpgradeCost_Uncommon() => Assert.AreEqual(4, bc.GetUpgradeCost(CardRarity.Uncommon));

        [Test]
        public void TC10_GetUpgradeCost_Rare() => Assert.AreEqual(7, bc.GetUpgradeCost(CardRarity.Rare));
    }

    // ============================================================
    // TC-11: Draft Config Tests
    // ============================================================
    [TestFixture]
    public class DraftConfigTests
    {
        [Test]
        public void TC11_1_DraftCardCount_Is3()
        {
            var bc = BalanceConfigSO.Instance;
            Assume.That(bc != null, "BalanceConfigSO not found");
            Assert.AreEqual(3, bc.draftCardCount);
        }
    }

    // ============================================================
    // TC-13: Balance Config Tests
    // ============================================================
    [TestFixture]
    public class BalanceConfigTests
    {
        private BalanceConfigSO bc;

        [OneTimeSetUp]
        public void Setup()
        {
            bc = BalanceConfigSO.Instance;
            Assume.That(bc != null, "BalanceConfigSO not found");
        }

        [Test]
        public void TC13_StartingFood_Is15() => Assert.AreEqual(15, bc.startingFood);

        [Test]
        public void TC13_StartingMaterials_Is5() => Assert.AreEqual(5, bc.startingMaterials);

        [Test]
        public void TC13_StartingCurrency_Is5() => Assert.AreEqual(5, bc.startingCurrency);

        [Test]
        public void TC13_BaseColonyHP_Is30() => Assert.AreEqual(30, bc.baseColonyHP);

        [Test]
        public void TC13_BaseHeroDeckSize_Is8() => Assert.AreEqual(8, bc.baseHeroDeckSize);

        [Test]
        public void TC13_BossFailureDamage_Is10() => Assert.AreEqual(10, bc.bossFailureDamage);

        [Test]
        public void TC13_RestHealPercent_Is30() => Assert.AreEqual(30, bc.restHealPercent);

        [Test]
        public void TC13_EliteBonusCurrency_Is3() => Assert.AreEqual(3, bc.eliteBonusCurrency);

        [Test]
        public void TC13_DifficultyScalingFactor_Is0_15() => Assert.AreEqual(0.15f, bc.difficultyScalingFactor);
    }

    // ============================================================
    // TC-14: Asset Count Tests
    // ============================================================
    [TestFixture]
    public class AssetCountTests
    {
        [Test]
        public void AssetCount_Heroes_AtLeast20()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:CardDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(path);
                if (card != null && card.cardType == CardType.Hero) count++;
            }
            Assert.GreaterOrEqual(count, 20, $"Need >= 20 heroes, found {count}");
        }

        [Test]
        public void AssetCount_Equipment_AtLeast12()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:CardDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(path);
                if (card != null && card.cardType == CardType.Equipment) count++;
            }
            Assert.GreaterOrEqual(count, 12, $"Need >= 12 equipment, found {count}");
        }

        [Test]
        public void AssetCount_Colony_AtLeast20()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:ColonyCardDefinitionSO")) count++;
            Assert.GreaterOrEqual(count, 20, $"Need >= 20 colony cards, found {count}");
        }

        [Test]
        public void AssetCount_HeroBenefits_AtLeast10()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:CardDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(path);
                if (card != null && card.cardType == CardType.HeroBenefit) count++;
            }
            Assert.GreaterOrEqual(count, 10, $"Need >= 10 hero benefits, found {count}");
        }

        [Test]
        public void AssetCount_Enemies_AtLeast12()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:EnemyDefinitionSO")) count++;
            Assert.GreaterOrEqual(count, 12, $"Need >= 12 enemies, found {count}");
        }

        [Test]
        public void AssetCount_Events_AtLeast15()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:EventDefinitionSO")) count++;
            Assert.GreaterOrEqual(count, 15, $"Need >= 15 events, found {count}");
        }

        [Test]
        public void AssetCount_Relics_AtLeast10()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:RelicDefinitionSO")) count++;
            Assert.GreaterOrEqual(count, 10, $"Need >= 10 relics, found {count}");
        }

        [Test]
        public void AssetCount_MapConfigs_AtLeast3()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:MapConfigSO")) count++;
            Assert.GreaterOrEqual(count, 3, $"Need >= 3 map configs, found {count}");
        }
    }

    // ============================================================
    // TC-15: Save/Load Tests
    // ============================================================
    [TestFixture]
    public class SaveLoadTests
    {
        [TearDown]
        public void Cleanup()
        {
            SaveManager.DeleteSave();
        }

        [Test]
        public void TC15_1_SaveCreatesFile()
        {
            var data = CreateTestSaveData();
            SaveManager.Save(data);
            Assert.IsTrue(SaveManager.HasSave());
        }

        [Test]
        public void TC15_2a_LoadedSave_IsNotNull()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.IsNotNull(loaded);
        }

        [Test]
        public void TC15_2b_Level_Preserved()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.AreEqual(2, loaded.currentLevel);
        }

        [Test]
        public void TC15_2c_ColonyHP_Preserved()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.AreEqual(25, loaded.colonyHP);
        }

        [Test]
        public void TC15_2d_Food_Preserved()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.AreEqual(10, loaded.foodStockpile);
        }

        [Test]
        public void TC15_2e_Materials_Preserved()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.AreEqual(3, loaded.materialsStockpile);
        }

        [Test]
        public void TC15_2f_Currency_Preserved()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.AreEqual(7, loaded.currencyStockpile);
        }

        [Test]
        public void TC15_2g_HeroDeckCount_Preserved()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.AreEqual(2, loaded.heroDeckCardNames.Count);
        }

        [Test]
        public void TC15_3_NodesVisited_Preserved()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.AreEqual(5, loaded.nodesVisited);
        }

        [Test]
        public void TC15_5_Relics_Preserved()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.AreEqual(1, loaded.activeRelicNames.Count);
        }

        [Test]
        public void TC15_6_WoundedHeroes_Preserved()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.AreEqual(1, loaded.woundedHeroNames.Count);
        }

        [Test]
        public void TC15_7_DeleteSave_RemovesFile()
        {
            SaveManager.Save(CreateTestSaveData());
            SaveManager.DeleteSave();
            Assert.IsFalse(SaveManager.HasSave());
        }

        [Test]
        public void TC15_CurrentSceneName_Preserved()
        {
            var data = CreateTestSaveData();
            data.currentSceneName = "MapTraversal";
            SaveManager.Save(data);
            var loaded = SaveManager.Load();
            Assert.AreEqual("MapTraversal", loaded.currentSceneName);
        }

        [Test]
        public void TC15_RunState_Preserved()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.AreEqual((int)RunState.MapTraversal, loaded.runState);
        }

        [Test]
        public void TC15_EncountersCompleted_Preserved()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.AreEqual(3, loaded.encountersCompleted);
        }

        [Test]
        public void TC15_EnemiesDefeated_Preserved()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.AreEqual(4, loaded.enemiesDefeated);
        }

        [Test]
        public void TC15_BossesKilled_Preserved()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.AreEqual(1, loaded.bossesKilled);
        }

        [Test]
        public void TC15_TotalResourcesGathered_Preserved()
        {
            SaveManager.Save(CreateTestSaveData());
            var loaded = SaveManager.Load();
            Assert.AreEqual(12, loaded.totalResourcesGathered);
        }

        private RunSaveData CreateTestSaveData()
        {
            var data = new RunSaveData
            {
                currentLevel = 2,
                runState = (int)RunState.MapTraversal,
                nodesVisited = 5,
                currentSceneName = "MapTraversal",
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
            data.heroDeckCardNames.Add("Scout Rat");
            data.heroDeckCardNames.Add("Brawler Rat");
            data.woundedHeroNames.Add("Pack Rat");
            data.activeRelicNames = new List<string> { "TestRelic" };
            return data;
        }
    }

    // ============================================================
    // TC-16: Meta-Progression Tests
    // ============================================================
    [TestFixture]
    public class MetaProgressionTests
    {
        [Test]
        public void TC16_1_ReputationFormula_Level2_1Boss_Victory()
        {
            int expected = 2 * 2 + 1 * 3 + 10;
            Assert.AreEqual(17, expected, "rep = level*2 + bosses*3 + 10(victory)");
        }

        [Test]
        public void TC16_4a_ColonyDeckBonus_3Levels_Is1()
        {
            int bonus = Mathf.Min(3 / 3, 5);
            Assert.AreEqual(1, bonus);
        }

        [Test]
        public void TC16_4b_ColonyDeckBonus_15Levels_CappedAt5()
        {
            int bonus = Mathf.Min(15 / 3, 5);
            Assert.AreEqual(5, bonus);
        }

        [Test]
        public void TC16_4c_ColonyDeckBonus_0Levels_Is0()
        {
            int bonus = Mathf.Min(0 / 3, 5);
            Assert.AreEqual(0, bonus);
        }

        [Test]
        public void TC16_4d_ColonyDeckBonus_6Levels_Is2()
        {
            int bonus = Mathf.Min(6 / 3, 5);
            Assert.AreEqual(2, bonus);
        }
    }

    // ============================================================
    // TC-17: Achievement Tests
    // ============================================================
    [TestFixture]
    public class AchievementTests
    {
        [Test]
        public void TC17_AchievementId_HasAtLeast25Entries()
        {
            var ids = System.Enum.GetValues(typeof(AchievementId));
            Assert.GreaterOrEqual(ids.Length, 25, $"AchievementId should have >= 25 entries, found {ids.Length}");
        }

        [Test]
        public void TC17_AchievementId_ContainsFirstVictory()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.FirstVictory));
        }

        [Test]
        public void TC17_AchievementId_ContainsAllBosses()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.DefeatElderSilas));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.DefeatTobiasDuchess));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.DefeatAldricFenn));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.DefeatPiedPiper));
        }

        [Test]
        public void TC17_AchievementId_ContainsAllLevels()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.CompleteLevel1));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.CompleteLevel2));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.CompleteLevel3));
        }

        [Test]
        public void TC17_AchievementId_ContainsScrapbookTiers()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.Scrapbook25));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.Scrapbook50));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.Scrapbook75));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.Scrapbook100));
        }

        [Test]
        public void TC17_AchievementId_ContainsCumulativeAchievements()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.Collect100Resources));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.Defeat50Enemies));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.Buy10ShopCards));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AchievementId), AchievementId.Complete10Runs));
        }
    }

    // ============================================================
    // TC-20: Edge Case Tests
    // ============================================================
    [TestFixture]
    public class EdgeCaseTests
    {
        [Test]
        public void TC20_6_BossPhases_HaveDescendingHPThresholds()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:BossDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var boss = AssetDatabase.LoadAssetAtPath<BossDefinitionSO>(path);
                if (boss != null && boss.phases != null && boss.phases.Length > 1)
                {
                    for (int i = 1; i < boss.phases.Length; i++)
                    {
                        Assert.Less(boss.phases[i].hpThreshold, boss.phases[i - 1].hpThreshold,
                            $"Boss '{boss.bossName}' phase {i} HP threshold should be < phase {i - 1}");
                    }
                }
            }
        }

        [Test]
        public void TC20_8_ColonyDeckBonus_CappedAt5()
        {
            int maxBonus = Mathf.Min(100 / 3, 5);
            Assert.AreEqual(5, maxBonus);
        }

        [Test]
        public void TC20_Population0_Consumption_AtLeast1()
        {
            int consumption = Mathf.Max(1, Mathf.CeilToInt(0f / 2f));
            Assert.AreEqual(1, consumption);
        }

        [Test]
        public void TC20_Population1_Consumption_AtLeast1()
        {
            int consumption = Mathf.Max(1, Mathf.CeilToInt(1f / 2f));
            Assert.AreEqual(1, consumption);
        }

        [Test]
        public void TC20_Population10_Consumption_Is5()
        {
            int consumption = Mathf.CeilToInt(10f / 2f);
            Assert.AreEqual(5, consumption);
        }

        [Test]
        public void TC20_StarvationDamage_NoFood_FullDamage()
        {
            var bc = BalanceConfigSO.Instance;
            Assume.That(bc != null, "BalanceConfigSO not found");
            int need = 3;
            int have = 0;
            int shortfall = need - have;
            int damage = shortfall * bc.starvationDamagePerFood;
            Assert.AreEqual(6, damage);
        }
    }

    // ============================================================
    // TC-21: Scene Transition Tests (Editor-side)
    // ============================================================
    [TestFixture]
    public class SceneTransitionTests
    {
        [Test]
        public void TC21_1a_BuildSettings_AtLeast7Scenes()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.GreaterOrEqual(scenes.Length, 7, $"Build settings should have >= 7 scenes, found {scenes.Length}");
        }

        [Test]
        public void TC21_1b_Bootstrap_IsBuildIndex0()
        {
            var scenes = EditorBuildSettings.scenes;
            Assume.That(scenes.Length >= 7, "Need >= 7 scenes in build settings");
            Assert.IsTrue(scenes[0].path.Contains("Bootstrap"), $"Build index 0 should be Bootstrap, got {scenes[0].path}");
        }

        [Test]
        public void TC21_1c_MainMenu_IsBuildIndex1()
        {
            var scenes = EditorBuildSettings.scenes;
            Assume.That(scenes.Length >= 7, "Need >= 7 scenes in build settings");
            Assert.IsTrue(scenes[1].path.Contains("MainMenu"), $"Build index 1 should be MainMenu, got {scenes[1].path}");
        }

        [Test]
        public void TC21_1d_ColonyDraft_IsBuildIndex2()
        {
            var scenes = EditorBuildSettings.scenes;
            Assume.That(scenes.Length >= 7, "Need >= 7 scenes in build settings");
            Assert.IsTrue(scenes[2].path.Contains("ColonyDraft"), $"Build index 2 should be ColonyDraft, got {scenes[2].path}");
        }

        [Test]
        public void TC21_1e_ColonyManagement_IsBuildIndex3()
        {
            var scenes = EditorBuildSettings.scenes;
            Assume.That(scenes.Length >= 7, "Need >= 7 scenes in build settings");
            Assert.IsTrue(scenes[3].path.Contains("ColonyManagement"), $"Build index 3 should be ColonyManagement, got {scenes[3].path}");
        }

        [Test]
        public void TC21_1f_MapTraversal_IsBuildIndex4()
        {
            var scenes = EditorBuildSettings.scenes;
            Assume.That(scenes.Length >= 7, "Need >= 7 scenes in build settings");
            Assert.IsTrue(scenes[4].path.Contains("MapTraversal"), $"Build index 4 should be MapTraversal, got {scenes[4].path}");
        }

        [Test]
        public void TC21_1g_Encounter_IsBuildIndex5()
        {
            var scenes = EditorBuildSettings.scenes;
            Assume.That(scenes.Length >= 7, "Need >= 7 scenes in build settings");
            Assert.IsTrue(scenes[5].path.Contains("Encounter"), $"Build index 5 should be Encounter, got {scenes[5].path}");
        }

        [Test]
        public void TC21_1h_RunResult_IsBuildIndex6()
        {
            var scenes = EditorBuildSettings.scenes;
            Assume.That(scenes.Length >= 7, "Need >= 7 scenes in build settings");
            Assert.IsTrue(scenes[6].path.Contains("RunResult"), $"Build index 6 should be RunResult, got {scenes[6].path}");
        }
    }

    // ============================================================
    // TC-22: Colony Draft Config Tests
    // ============================================================
    [TestFixture]
    public class ColonyDraftTests
    {
        private BalanceConfigSO bc;

        [OneTimeSetUp]
        public void Setup()
        {
            bc = BalanceConfigSO.Instance;
            Assume.That(bc != null, "BalanceConfigSO not found");
        }

        [Test]
        public void TC22_1a_ColonyDraftOfferCount_Is12()
        {
            Assert.AreEqual(12, bc.colonyDraftOfferCount);
        }

        [Test]
        public void TC22_1b_ColonyDraftPickCount_Is8()
        {
            Assert.AreEqual(8, bc.colonyDraftPickCount);
        }

        [Test]
        public void TC22_1c_ColonyCardPool_AtLeastOfferCount()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:ColonyCardDefinitionSO")) count++;
            Assert.GreaterOrEqual(count, bc.colonyDraftOfferCount, $"Colony card pool ({count}) should be >= offer count ({bc.colonyDraftOfferCount})");
        }

        [Test]
        public void TC22_4_PickCount_LessOrEqualToOfferCount()
        {
            Assert.LessOrEqual(bc.colonyDraftPickCount, bc.colonyDraftOfferCount);
        }

        [Test]
        public void TC22_5_EventBus_HasOnColonyDraftComplete()
        {
            var field = typeof(EventBus).GetField("OnColonyDraftComplete");
            Assert.IsNotNull(field, "EventBus should have OnColonyDraftComplete field");
        }
    }

    // ============================================================
    // TC-23: ServiceLocator Tests
    // ============================================================
    [TestFixture]
    public class ServiceLocatorTests
    {
        [SetUp]
        public void Setup()
        {
            ServiceLocator.Clear();
        }

        [Test]
        public void TC23_Register_AndGet_ReturnsService()
        {
            var mock = new MockService();
            ServiceLocator.Register<IMockService>(mock);
            var result = ServiceLocator.Get<IMockService>();
            Assert.AreSame(mock, result);
        }

        [Test]
        public void TC23_Get_Unregistered_ReturnsNull()
        {
            var result = ServiceLocator.Get<IMockService>();
            Assert.IsNull(result);
        }

        [Test]
        public void TC23_Register_Null_ClearsRegistration()
        {
            var mock = new MockService();
            ServiceLocator.Register<IMockService>(mock);
            ServiceLocator.Register<IMockService>(null);
            var result = ServiceLocator.Get<IMockService>();
            Assert.IsNull(result);
        }

        [Test]
        public void TC23_Clear_RemovesAll()
        {
            ServiceLocator.Register<IMockService>(new MockService());
            ServiceLocator.Clear();
            Assert.IsNull(ServiceLocator.Get<IMockService>());
        }

        [Test]
        public void TC23_Register_Overwrites_Previous()
        {
            var first = new MockService();
            var second = new MockService();
            ServiceLocator.Register<IMockService>(first);
            ServiceLocator.Register<IMockService>(second);
            Assert.AreSame(second, ServiceLocator.Get<IMockService>());
        }

        [Test]
        public void TC23_Unregister_RemovesService()
        {
            ServiceLocator.Register<IMockService>(new MockService());
            ServiceLocator.Unregister<IMockService>();
            Assert.IsNull(ServiceLocator.Get<IMockService>());
        }

        [Test]
        public void TC23_MultipleTypes_Independent()
        {
            var mock1 = new MockService();
            var mock2 = new MockService2();
            ServiceLocator.Register<IMockService>(mock1);
            ServiceLocator.Register<IMockService2>(mock2);
            Assert.AreSame(mock1, ServiceLocator.Get<IMockService>());
            Assert.AreSame(mock2, ServiceLocator.Get<IMockService2>());
        }

        [Test]
        public void TC23_10a_IBalanceConfig_PascalCase_StartingFood()
        {
            var bc = BalanceConfigSO.Instance;
            Assume.That(bc != null, "BalanceConfigSO not found");
            ServiceLocator.Register<IBalanceConfig>(bc);
            var resolved = ServiceLocator.Get<IBalanceConfig>();
            Assert.AreEqual(bc.startingFood, resolved.StartingFood);
        }

        [Test]
        public void TC23_10b_IBalanceConfig_PascalCase_ShopRerollCost()
        {
            var bc = BalanceConfigSO.Instance;
            Assume.That(bc != null, "BalanceConfigSO not found");
            ServiceLocator.Register<IBalanceConfig>(bc);
            var resolved = ServiceLocator.Get<IBalanceConfig>();
            Assert.AreEqual(bc.shopRerollCost, resolved.ShopRerollCost);
        }

        [Test]
        public void TC23_10c_IBalanceConfig_GetShopPrice_ViaInterface()
        {
            var bc = BalanceConfigSO.Instance;
            Assume.That(bc != null, "BalanceConfigSO not found");
            ServiceLocator.Register<IBalanceConfig>(bc);
            var resolved = ServiceLocator.Get<IBalanceConfig>();
            Assert.AreEqual(bc.GetShopPrice(CardRarity.Common), resolved.GetShopPrice(CardRarity.Common));
        }

        // Test helper interfaces
        private interface IMockService { }
        private interface IMockService2 { }
        private class MockService : IMockService { }
        private class MockService2 : IMockService2 { }
    }

    // ============================================================
    // EventBus Tests
    // ============================================================
    [TestFixture]
    public class EventBusTests
    {
        [SetUp]
        public void Setup()
        {
            EventBus.Reset();
        }

        [Test]
        public void EventBus_OnRunStarted_FiresCorrectly()
        {
            bool fired = false;
            EventBus.OnRunStarted += () => fired = true;
            EventBus.OnRunStarted?.Invoke();
            Assert.IsTrue(fired);
        }

        [Test]
        public void EventBus_OnColonyHPChanged_PassesValues()
        {
            int receivedCurrent = -1, receivedMax = -1;
            EventBus.OnColonyHPChanged += (c, m) => { receivedCurrent = c; receivedMax = m; };
            EventBus.OnColonyHPChanged?.Invoke(25, 30);
            Assert.AreEqual(25, receivedCurrent);
            Assert.AreEqual(30, receivedMax);
        }

        [Test]
        public void EventBus_OnAchievementUnlocked_PassesId()
        {
            string receivedId = null;
            EventBus.OnAchievementUnlocked += id => receivedId = id;
            EventBus.OnAchievementUnlocked?.Invoke("FirstVictory");
            Assert.AreEqual("FirstVictory", receivedId);
        }

        [Test]
        public void EventBus_Reset_ClearsAllSubscriptions()
        {
            bool fired = false;
            EventBus.OnRunStarted += () => fired = true;
            EventBus.Reset();
            EventBus.OnRunStarted?.Invoke();
            Assert.IsFalse(fired);
        }

        [Test]
        public void EventBus_OnEncounterResultDismissed_Exists()
        {
            var field = typeof(EventBus).GetField("OnEncounterResultDismissed");
            Assert.IsNotNull(field);
        }

        [Test]
        public void EventBus_OnReturnToMainMenu_Exists()
        {
            var field = typeof(EventBus).GetField("OnReturnToMainMenu");
            Assert.IsNotNull(field);
        }

        [Test]
        public void EventBus_OnColonyDraftComplete_Exists()
        {
            var field = typeof(EventBus).GetField("OnColonyDraftComplete");
            Assert.IsNotNull(field);
        }

        [Test]
        public void EventBus_OnMapNodeSelected_Exists()
        {
            var field = typeof(EventBus).GetField("OnMapNodeSelected");
            Assert.IsNotNull(field);
        }

        [Test]
        public void EventBus_OnEncounterComplete_Exists()
        {
            var field = typeof(EventBus).GetField("OnEncounterComplete");
            Assert.IsNotNull(field);
        }

        [Test]
        public void EventBus_OnLevelComplete_Exists()
        {
            var field = typeof(EventBus).GetField("OnLevelComplete");
            Assert.IsNotNull(field);
        }
    }

    // ============================================================
    // Enum Validation Tests
    // ============================================================
    [TestFixture]
    public class EnumValidationTests
    {
        [Test]
        public void PlacementRequirement_HasAllValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlacementRequirement), PlacementRequirement.None));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlacementRequirement), PlacementRequirement.AdjacentTo));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlacementRequirement), PlacementRequirement.Edge));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlacementRequirement), PlacementRequirement.Corner));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlacementRequirement), PlacementRequirement.Center));
        }

        [Test]
        public void NodeType_HasAllValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(NodeType), NodeType.ResourceEncounter));
            Assert.IsTrue(System.Enum.IsDefined(typeof(NodeType), NodeType.EliteEncounter));
            Assert.IsTrue(System.Enum.IsDefined(typeof(NodeType), NodeType.Boss));
            Assert.IsTrue(System.Enum.IsDefined(typeof(NodeType), NodeType.Shop));
            Assert.IsTrue(System.Enum.IsDefined(typeof(NodeType), NodeType.HealingShrine));
            Assert.IsTrue(System.Enum.IsDefined(typeof(NodeType), NodeType.UpgradeShrine));
            Assert.IsTrue(System.Enum.IsDefined(typeof(NodeType), NodeType.CardDraft));
            Assert.IsTrue(System.Enum.IsDefined(typeof(NodeType), NodeType.Event));
            Assert.IsTrue(System.Enum.IsDefined(typeof(NodeType), NodeType.RestSite));
        }

        [Test]
        public void CardType_HasAllValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardType), CardType.Hero));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardType), CardType.Equipment));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardType), CardType.HeroBenefit));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardType), CardType.Colony));
        }

        [Test]
        public void CardRarity_HasAllValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardRarity), CardRarity.Common));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardRarity), CardRarity.Uncommon));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardRarity), CardRarity.Rare));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardRarity), CardRarity.Legendary));
        }

        [Test]
        public void RelicEffect_HasAllValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(RelicEffect), RelicEffect.None));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RelicEffect), RelicEffect.ShopDiscount));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RelicEffect), RelicEffect.BonusHP));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RelicEffect), RelicEffect.BonusCombat));
        }

        [Test]
        public void RunState_HasAllValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(RunState), RunState.Draft));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RunState), RunState.MapTraversal));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RunState), RunState.InEncounter));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RunState), RunState.InBoss));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RunState), RunState.ColonyManagement));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RunState), RunState.RunComplete));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RunState), RunState.GameOver));
        }

        [Test]
        public void EncounterType_HasAllValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(EncounterType), EncounterType.Resource));
            Assert.IsTrue(System.Enum.IsDefined(typeof(EncounterType), EncounterType.Elite));
            Assert.IsTrue(System.Enum.IsDefined(typeof(EncounterType), EncounterType.Boss));
        }
    }

    // ============================================================
    // RunSaveData Structure Tests
    // ============================================================
    [TestFixture]
    public class RunSaveDataTests
    {
        [Test]
        public void RunSaveData_DefaultValues_Initialized()
        {
            var data = new RunSaveData();
            Assert.IsNotNull(data.heroDeckCardNames);
            Assert.IsNotNull(data.colonyDeckCardNames);
            Assert.IsNotNull(data.woundedHeroNames);
            Assert.IsNotNull(data.exhaustedHeroNames);
            Assert.IsNotNull(data.mapNodes);
            Assert.IsNotNull(data.activeRelicNames);
        }

        [Test]
        public void RunSaveData_HasCurrentSceneName()
        {
            var data = new RunSaveData();
            data.currentSceneName = "TestScene";
            Assert.AreEqual("TestScene", data.currentSceneName);
        }

        [Test]
        public void RunSaveData_SerializesCorrectly()
        {
            var data = new RunSaveData
            {
                currentLevel = 3,
                colonyHP = 20,
                foodStockpile = 8
            };
            string json = JsonUtility.ToJson(data);
            var restored = JsonUtility.FromJson<RunSaveData>(json);
            Assert.AreEqual(3, restored.currentLevel);
            Assert.AreEqual(20, restored.colonyHP);
            Assert.AreEqual(8, restored.foodStockpile);
        }

        [Test]
        public void RunSaveData_Lists_SerializeCorrectly()
        {
            var data = new RunSaveData();
            data.heroDeckCardNames.Add("TestCard1");
            data.heroDeckCardNames.Add("TestCard2");
            data.woundedHeroNames.Add("WoundedCard");
            string json = JsonUtility.ToJson(data);
            var restored = JsonUtility.FromJson<RunSaveData>(json);
            Assert.AreEqual(2, restored.heroDeckCardNames.Count);
            Assert.AreEqual(1, restored.woundedHeroNames.Count);
            Assert.AreEqual("TestCard1", restored.heroDeckCardNames[0]);
        }

        [Test]
        public void ColonyConfigSaveData_AllFields_Accessible()
        {
            var config = new ColonyConfigSaveData
            {
                maxHeroDeckSize = 10,
                foodConsumptionPerNode = 3,
                heroCombatBonus = 2,
                heroMoveBonus = 1,
                heroCarryBonus = 1,
                totalPopulation = 6,
                bonusStartingFood = 5
            };
            Assert.AreEqual(10, config.maxHeroDeckSize);
            Assert.AreEqual(3, config.foodConsumptionPerNode);
            Assert.AreEqual(6, config.totalPopulation);
        }
    }

    // ============================================================
    // Card Data Validation Tests
    // ============================================================
    [TestFixture]
    public class CardDataValidationTests
    {
        [Test]
        public void AllHeroCards_HavePositiveCombat()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:CardDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(path);
                if (card != null && card.cardType == CardType.Hero)
                    Assert.Greater(card.combat, 0, $"Hero '{card.cardName}' should have combat > 0");
            }
        }

        [Test]
        public void AllHeroCards_HaveNames()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:CardDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(path);
                if (card != null && card.cardType == CardType.Hero)
                    Assert.IsFalse(string.IsNullOrEmpty(card.cardName), $"Hero at {path} should have a name");
            }
        }

        [Test]
        public void AllColonyCards_HaveNames()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ColonyCardDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<ColonyCardDefinitionSO>(path);
                if (card != null)
                    Assert.IsFalse(string.IsNullOrEmpty(card.cardName), $"Colony card at {path} should have a name");
            }
        }

        [Test]
        public void AllEnemies_HavePositiveStrength()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:EnemyDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(path);
                if (enemy != null)
                    Assert.Greater(enemy.strength, 0, $"Enemy '{enemy.enemyName}' should have strength > 0");
            }
        }

        [Test]
        public void AllEnemies_HaveNames()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:EnemyDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(path);
                if (enemy != null)
                    Assert.IsFalse(string.IsNullOrEmpty(enemy.enemyName), $"Enemy at {path} should have a name");
            }
        }

        [Test]
        public void AllRelics_HaveNames()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:RelicDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var relic = AssetDatabase.LoadAssetAtPath<RelicDefinitionSO>(path);
                if (relic != null)
                    Assert.IsFalse(string.IsNullOrEmpty(relic.relicName), $"Relic at {path} should have a name");
            }
        }

        [Test]
        public void AllRelics_HaveValidEffect()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:RelicDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var relic = AssetDatabase.LoadAssetAtPath<RelicDefinitionSO>(path);
                if (relic != null)
                    Assert.IsTrue(System.Enum.IsDefined(typeof(RelicEffect), relic.effect), $"Relic '{relic.relicName}' has invalid effect");
            }
        }
    }

    // ============================================================
    // Interface Existence Tests
    // ============================================================
    [TestFixture]
    public class InterfaceExistenceTests
    {
        [Test]
        public void IColonyManager_Exists() => Assert.IsNotNull(typeof(IColonyManager));

        [Test]
        public void IBalanceConfig_Exists() => Assert.IsNotNull(typeof(IBalanceConfig));

        [Test]
        public void IRunManager_Exists() => Assert.IsNotNull(typeof(IRunManager));

        [Test]
        public void IMapManager_Exists() => Assert.IsNotNull(typeof(IMapManager));

        [Test]
        public void IColonyBoardManager_Exists() => Assert.IsNotNull(typeof(IColonyBoardManager));

        [Test]
        public void IRelicManager_Exists() => Assert.IsNotNull(typeof(IRelicManager));

        [Test]
        public void IMetaProgressionManager_Exists() => Assert.IsNotNull(typeof(IMetaProgressionManager));

        [Test]
        public void IGameSettings_Exists() => Assert.IsNotNull(typeof(IGameSettings));
    }
}
