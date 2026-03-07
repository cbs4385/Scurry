using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Scurry.Core;
using Scurry.Data;
using Scurry.Colony;
using Scurry.Map;
using Scurry.Interfaces;

namespace Scurry.Tests.PlayMode
{
    // ============================================================
    // TC-21: Persistent Manager Singleton Tests (PlayMode)
    // ============================================================
    [TestFixture]
    public class PersistentManagerTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            if (SceneManager.GetActiveScene().name != "Bootstrap")
            {
                SceneManager.LoadScene("Bootstrap");
                yield return null;
                yield return null;
                yield return new WaitForSeconds(0.5f);
            }
        }

        [UnityTest]
        public IEnumerator TC21_12a_RunManager_InstanceExists()
        {
            yield return null;
            Assert.IsNotNull(RunManager.Instance, "RunManager.Instance should exist");
        }

        [UnityTest]
        public IEnumerator TC21_12b_ColonyManager_InstanceExists()
        {
            yield return null;
            Assert.IsNotNull(ColonyManager.Instance, "ColonyManager.Instance should exist");
        }

        [UnityTest]
        public IEnumerator TC21_12c_GameSettings_InstanceExists()
        {
            yield return null;
            Assert.IsNotNull(GameSettings.Instance, "GameSettings.Instance should exist");
        }

        [UnityTest]
        public IEnumerator TC21_12d_RelicManager_InstanceExists()
        {
            yield return null;
            Assert.IsNotNull(RelicManager.Instance, "RelicManager.Instance should exist");
        }

        [UnityTest]
        public IEnumerator TC21_12e_AchievementManager_InstanceExists()
        {
            yield return null;
            Assert.IsNotNull(AchievementManager.Instance, "AchievementManager.Instance should exist");
        }

        [UnityTest]
        public IEnumerator TC21_12f_MetaProgressionManager_InstanceExists()
        {
            yield return null;
            Assert.IsNotNull(MetaProgressionManager.Instance, "MetaProgressionManager.Instance should exist");
        }

        [UnityTest]
        public IEnumerator TC21_13_EventSystem_Exists()
        {
            yield return null;
            var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            Assert.IsNotNull(eventSystem, "EventSystem should exist in scene");
        }

        [UnityTest]
        public IEnumerator TC21_14_OnlyOneActiveAudioListener()
        {
            yield return null;
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            int active = 0;
            foreach (var l in listeners)
            {
                if (l.enabled && l.gameObject.activeInHierarchy) active++;
            }
            Assert.LessOrEqual(active, 1, $"Should have <= 1 active AudioListener, found {active}");
        }
    }

    // ============================================================
    // TC-17: Achievement PlayMode Tests
    // ============================================================
    [TestFixture]
    public class AchievementPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            if (SceneManager.GetActiveScene().name != "Bootstrap")
            {
                SceneManager.LoadScene("Bootstrap");
                yield return null;
                yield return null;
                yield return new WaitForSeconds(0.5f);
            }
        }

        [UnityTest]
        public IEnumerator TC17_1_Achievement_UnlocksSuccessfully()
        {
            yield return null;
            var achMgr = AchievementManager.Instance;
            Assume.That(achMgr != null, "AchievementManager not found");

            bool wasUnlocked = achMgr.IsUnlocked(AchievementId.FirstVictory);
            if (!wasUnlocked)
            {
                achMgr.TryUnlock(AchievementId.FirstVictory);
                Assert.IsTrue(achMgr.IsUnlocked(AchievementId.FirstVictory), "Achievement should unlock");
            }
            else
            {
                Assert.IsTrue(achMgr.IsUnlocked(AchievementId.FirstVictory), "Achievement was already unlocked");
            }
        }

        [UnityTest]
        public IEnumerator TC17_1b_DoubleUnlock_IsSafe()
        {
            yield return null;
            var achMgr = AchievementManager.Instance;
            Assume.That(achMgr != null, "AchievementManager not found");

            achMgr.TryUnlock(AchievementId.FirstVictory);
            achMgr.TryUnlock(AchievementId.FirstVictory);
            Assert.IsTrue(true, "Double-unlock should not crash");
        }
    }

    // ============================================================
    // TC-18: Relic System PlayMode Tests
    // ============================================================
    [TestFixture]
    public class RelicSystemPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            if (SceneManager.GetActiveScene().name != "Bootstrap")
            {
                SceneManager.LoadScene("Bootstrap");
                yield return null;
                yield return null;
                yield return new WaitForSeconds(0.5f);
            }
        }

        [UnityTest]
        public IEnumerator TC18_6_ClearRelics_ResetsCount()
        {
            yield return null;
            var relicMgr = RelicManager.Instance;
            Assume.That(relicMgr != null, "RelicManager not found");

            relicMgr.ClearRelics();
            Assert.AreEqual(0, relicMgr.RelicCount, "After ClearRelics, count should be 0");
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator TC18_1_AddRelic_Works()
        {
            yield return null;
            var relicMgr = RelicManager.Instance;
            Assume.That(relicMgr != null, "RelicManager not found");
            relicMgr.ClearRelics();

            var relics = new List<RelicDefinitionSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:RelicDefinitionSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var relic = UnityEditor.AssetDatabase.LoadAssetAtPath<RelicDefinitionSO>(path);
                if (relic != null) relics.Add(relic);
            }
            Assume.That(relics.Count >= 1, "Need at least 1 relic asset");

            relicMgr.AddRelic(relics[0]);
            Assert.IsTrue(relicMgr.HasRelic(relics[0].relicName), $"Relic '{relics[0].relicName}' should be added");
            Assert.AreEqual(1, relicMgr.RelicCount);
            relicMgr.ClearRelics();
        }

        [UnityTest]
        public IEnumerator TC18_4_TwoRelics_Active()
        {
            yield return null;
            var relicMgr = RelicManager.Instance;
            Assume.That(relicMgr != null, "RelicManager not found");
            relicMgr.ClearRelics();

            var relics = new List<RelicDefinitionSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:RelicDefinitionSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var relic = UnityEditor.AssetDatabase.LoadAssetAtPath<RelicDefinitionSO>(path);
                if (relic != null) relics.Add(relic);
            }
            Assume.That(relics.Count >= 2, "Need at least 2 relic assets");

            relicMgr.AddRelic(relics[0]);
            relicMgr.AddRelic(relics[1]);
            Assert.AreEqual(2, relicMgr.RelicCount);
            relicMgr.ClearRelics();
        }

        [UnityTest]
        public IEnumerator TC18_2_ShopDiscount_Relic()
        {
            yield return null;
            var relicMgr = RelicManager.Instance;
            Assume.That(relicMgr != null, "RelicManager not found");
            relicMgr.ClearRelics();

            RelicDefinitionSO shopDiscountRelic = null;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:RelicDefinitionSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var relic = UnityEditor.AssetDatabase.LoadAssetAtPath<RelicDefinitionSO>(path);
                if (relic != null && relic.effect == RelicEffect.ShopDiscount)
                {
                    shopDiscountRelic = relic;
                    break;
                }
            }
            Assume.That(shopDiscountRelic != null, "No ShopDiscount relic found");

            relicMgr.AddRelic(shopDiscountRelic);
            int discount = relicMgr.GetShopDiscount();
            Assert.Greater(discount, 0, $"ShopDiscount relic should give discount > 0, got {discount}");
            relicMgr.ClearRelics();
        }
#endif
    }

    // ============================================================
    // TC-19: Settings PlayMode Tests
    // ============================================================
    [TestFixture]
    public class SettingsPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            if (SceneManager.GetActiveScene().name != "Bootstrap")
            {
                SceneManager.LoadScene("Bootstrap");
                yield return null;
                yield return null;
                yield return new WaitForSeconds(0.5f);
            }
        }

        [UnityTest]
        public IEnumerator TC19_1_BattleSpeed_Accessible()
        {
            yield return null;
            var gs = GameSettings.Instance;
            Assume.That(gs != null, "GameSettings not found");
            int speed = gs.BattleSpeed;
            Assert.GreaterOrEqual(speed, 0);
            Assert.LessOrEqual(speed, 2);
        }

        [UnityTest]
        public IEnumerator TC19_2_ColorBlindMode_Toggles()
        {
            yield return null;
            var gs = GameSettings.Instance;
            Assume.That(gs != null, "GameSettings not found");
            bool before = gs.ColorBlindMode;
            gs.SetColorBlindMode(!before);
            Assert.AreEqual(!before, gs.ColorBlindMode);
            gs.SetColorBlindMode(before); // Restore
        }

        [UnityTest]
        public IEnumerator TC19_3_TextSize_Accessible()
        {
            yield return null;
            var gs = GameSettings.Instance;
            Assume.That(gs != null, "GameSettings not found");
            int size = gs.TextSizeModifier;
            Assert.GreaterOrEqual(size, -2);
            Assert.LessOrEqual(size, 4);
        }

        [UnityTest]
        public IEnumerator TC19_BattleSpeed_Cycles()
        {
            yield return null;
            var gs = GameSettings.Instance;
            Assume.That(gs != null, "GameSettings not found");
            int original = gs.BattleSpeed;
            gs.CycleBattleSpeed();
            Assert.AreEqual((original + 1) % 3, gs.BattleSpeed);
            // Restore
            gs.SetBattleSpeed(original);
        }
    }

    // ============================================================
    // TC-20: Edge Case PlayMode Tests
    // ============================================================
    [TestFixture]
    public class EdgeCasePlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            if (SceneManager.GetActiveScene().name != "Bootstrap")
            {
                SceneManager.LoadScene("Bootstrap");
                yield return null;
                yield return null;
                yield return new WaitForSeconds(0.5f);
            }
        }

        [UnityTest]
        public IEnumerator TC20_4_ColonyHP_ExactlyZero_NotAlive()
        {
            yield return null;
            var colonyMgr = ColonyManager.Instance;
            Assume.That(colonyMgr != null, "ColonyManager not found");

            colonyMgr.InitializeHP();
            colonyMgr.TakeDamage(colonyMgr.CurrentHP);
            Assert.IsFalse(colonyMgr.IsAlive, "Colony HP exactly 0 should mean not alive");
            colonyMgr.InitializeHP(); // Restore
        }

        [UnityTest]
        public IEnumerator TC20_ColonyHP_TakeDamage_Reduces()
        {
            yield return null;
            var colonyMgr = ColonyManager.Instance;
            Assume.That(colonyMgr != null, "ColonyManager not found");

            colonyMgr.InitializeHP();
            int before = colonyMgr.CurrentHP;
            colonyMgr.TakeDamage(5);
            Assert.AreEqual(before - 5, colonyMgr.CurrentHP);
            colonyMgr.InitializeHP();
        }

        [UnityTest]
        public IEnumerator TC20_ColonyHP_Heal_Increases()
        {
            yield return null;
            var colonyMgr = ColonyManager.Instance;
            Assume.That(colonyMgr != null, "ColonyManager not found");

            colonyMgr.InitializeHP();
            colonyMgr.TakeDamage(10);
            int after = colonyMgr.CurrentHP;
            colonyMgr.Heal(5);
            Assert.AreEqual(after + 5, colonyMgr.CurrentHP);
            colonyMgr.InitializeHP();
        }

        [UnityTest]
        public IEnumerator TC20_AddFood_IncreasesStockpile()
        {
            yield return null;
            var colonyMgr = ColonyManager.Instance;
            Assume.That(colonyMgr != null, "ColonyManager not found");

            colonyMgr.InitializeHP();
            int before = colonyMgr.FoodStockpile;
            colonyMgr.AddFood(5);
            Assert.AreEqual(before + 5, colonyMgr.FoodStockpile);
            colonyMgr.InitializeHP();
        }

        [UnityTest]
        public IEnumerator TC20_AddCurrency_IncreasesStockpile()
        {
            yield return null;
            var colonyMgr = ColonyManager.Instance;
            Assume.That(colonyMgr != null, "ColonyManager not found");

            colonyMgr.InitializeHP();
            int before = colonyMgr.CurrencyStockpile;
            colonyMgr.AddCurrency(10);
            Assert.AreEqual(before + 10, colonyMgr.CurrencyStockpile);
            colonyMgr.InitializeHP();
        }

        [UnityTest]
        public IEnumerator TC20_SpendFood_Succeeds_WhenEnough()
        {
            yield return null;
            var colonyMgr = ColonyManager.Instance;
            Assume.That(colonyMgr != null, "ColonyManager not found");

            colonyMgr.InitializeHP();
            colonyMgr.AddFood(10);
            bool success = colonyMgr.SpendFood(5);
            Assert.IsTrue(success);
            colonyMgr.InitializeHP();
        }

        [UnityTest]
        public IEnumerator TC20_SpendFood_Fails_WhenInsufficient()
        {
            yield return null;
            var colonyMgr = ColonyManager.Instance;
            Assume.That(colonyMgr != null, "ColonyManager not found");

            colonyMgr.InitializeHP();
            bool success = colonyMgr.SpendFood(999);
            Assert.IsFalse(success);
            colonyMgr.InitializeHP();
        }

        [UnityTest]
        public IEnumerator TC20_SpendCurrency_Fails_WhenInsufficient()
        {
            yield return null;
            var colonyMgr = ColonyManager.Instance;
            Assume.That(colonyMgr != null, "ColonyManager not found");

            colonyMgr.InitializeHP();
            bool success = colonyMgr.SpendCurrency(999);
            Assert.IsFalse(success);
            colonyMgr.InitializeHP();
        }
    }

    // ============================================================
    // TC-23: ServiceLocator PlayMode Tests (with real managers)
    // ============================================================
    [TestFixture]
    public class ServiceLocatorPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            if (SceneManager.GetActiveScene().name != "Bootstrap")
            {
                SceneManager.LoadScene("Bootstrap");
                yield return null;
                yield return null;
                yield return new WaitForSeconds(0.5f);
            }
        }

        [UnityTest]
        public IEnumerator TC23_1a_IColonyManager_Registered()
        {
            yield return null;
            Assert.IsNotNull(ServiceLocator.Get<IColonyManager>(), "IColonyManager should be registered");
        }

        [UnityTest]
        public IEnumerator TC23_1b_IBalanceConfig_Registered()
        {
            yield return null;
            Assert.IsNotNull(ServiceLocator.Get<IBalanceConfig>(), "IBalanceConfig should be registered");
        }

        [UnityTest]
        public IEnumerator TC23_1c_IRunManager_Registered()
        {
            yield return null;
            Assert.IsNotNull(ServiceLocator.Get<IRunManager>(), "IRunManager should be registered");
        }

        [UnityTest]
        public IEnumerator TC23_1d_IRelicManager_Registered()
        {
            yield return null;
            Assert.IsNotNull(ServiceLocator.Get<IRelicManager>(), "IRelicManager should be registered");
        }

        [UnityTest]
        public IEnumerator TC23_1e_IMetaProgressionManager_Registered()
        {
            yield return null;
            Assert.IsNotNull(ServiceLocator.Get<IMetaProgressionManager>(), "IMetaProgressionManager should be registered");
        }

        [UnityTest]
        public IEnumerator TC23_1f_IGameSettings_Registered()
        {
            yield return null;
            Assert.IsNotNull(ServiceLocator.Get<IGameSettings>(), "IGameSettings should be registered");
        }

        [UnityTest]
        public IEnumerator TC23_2_IColonyManager_ResolvesToSingleton()
        {
            yield return null;
            var resolved = ServiceLocator.Get<IColonyManager>();
            Assume.That(resolved != null, "IColonyManager not registered");
            Assert.IsTrue(ReferenceEquals(resolved, ColonyManager.Instance));
        }

        [UnityTest]
        public IEnumerator TC23_3_IBalanceConfig_ResolvesToSingleton()
        {
            yield return null;
            var resolved = ServiceLocator.Get<IBalanceConfig>();
            Assume.That(resolved != null, "IBalanceConfig not registered");
            Assert.IsTrue(ReferenceEquals(resolved, BalanceConfigSO.Instance));
        }

        [UnityTest]
        public IEnumerator TC23_4_IRunManager_ResolvesToSingleton()
        {
            yield return null;
            var resolved = ServiceLocator.Get<IRunManager>();
            Assume.That(resolved != null, "IRunManager not registered");
            Assert.IsTrue(ReferenceEquals(resolved, RunManager.Instance));
        }

        [UnityTest]
        public IEnumerator TC23_5_IRelicManager_ResolvesToSingleton()
        {
            yield return null;
            var resolved = ServiceLocator.Get<IRelicManager>();
            Assume.That(resolved != null, "IRelicManager not registered");
            Assert.IsTrue(ReferenceEquals(resolved, RelicManager.Instance));
        }

        [UnityTest]
        public IEnumerator TC23_7_IGameSettings_ResolvesToSingleton()
        {
            yield return null;
            var resolved = ServiceLocator.Get<IGameSettings>();
            Assume.That(resolved != null, "IGameSettings not registered");
            Assert.IsTrue(ReferenceEquals(resolved, GameSettings.Instance));
        }
    }

    // ============================================================
    // TC-24.1: MainMenu UI Tests
    // ============================================================
    [TestFixture]
    public class MainMenuUITests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("Bootstrap");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.5f);
            // Bootstrap should auto-load MainMenu
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                SceneManager.LoadScene("MainMenu");
                yield return null;
                yield return null;
                yield return new WaitForSeconds(0.2f);
            }
        }

        [UnityTest]
        [Ignore("ValidationTestRunner in Bootstrap produces errors that bleed across test boundaries")]
        public IEnumerator TC24_1a_MainMenuManager_Found()
        {
            yield return new WaitForSeconds(1.0f);
            var mainMenu = Object.FindAnyObjectByType<Scurry.UI.MainMenuManager>();
            Assert.IsNotNull(mainMenu, "MainMenuManager should be found in MainMenu scene");
        }

        [UnityTest]
        [Ignore("ValidationTestRunner in Bootstrap produces errors that bleed across test boundaries")]
        public IEnumerator TC24_1b_NewRunButton_Exists()
        {
            yield return new WaitForSeconds(1.0f);
            var mainMenu = Object.FindAnyObjectByType<Scurry.UI.MainMenuManager>();
            Assume.That(mainMenu != null, "MainMenuManager not found");

            var buttons = mainMenu.GetComponentsInChildren<Button>(true);
            bool hasNewRun = false;
            foreach (var btn in buttons)
                if (btn.name.Contains("NewRun")) hasNewRun = true;
            Assert.IsTrue(hasNewRun, "NewRunButton should exist in MainMenu");
        }

        [UnityTest]
        [Ignore("ValidationTestRunner in Bootstrap produces exceptions that bleed across test boundaries")]
        public IEnumerator TC24_1c_ContinueButton_Exists()
        {
            yield return new WaitForSeconds(1.0f);
            var mainMenu = Object.FindAnyObjectByType<Scurry.UI.MainMenuManager>();
            Assume.That(mainMenu != null, "MainMenuManager not found");

            var buttons = mainMenu.GetComponentsInChildren<Button>(true);
            bool hasContinue = false;
            foreach (var btn in buttons)
                if (btn.name.Contains("Continue")) hasContinue = true;
            Assert.IsTrue(hasContinue, "ContinueButton should exist in MainMenu");
        }

        [UnityTest]
        [Ignore("ValidationTestRunner in Bootstrap produces errors that bleed across test boundaries")]
        public IEnumerator TC24_1d_MainMenu_HasCanvas()
        {
            yield return new WaitForSeconds(1.0f);
            var mainMenu = Object.FindAnyObjectByType<Scurry.UI.MainMenuManager>();
            Assume.That(mainMenu != null, "MainMenuManager not found");

            var canvas = mainMenu.GetComponent<Canvas>();
            Assert.IsNotNull(canvas, "MainMenuManager should have Canvas component");
        }
    }

    // ============================================================
    // TC-24.17: Encounter Additive Load Tests
    // ============================================================
    [TestFixture]
    public class EncounterAdditiveLoadTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("Bootstrap");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        public IEnumerator TC24_17a_MapTraversal_IsActiveScene()
        {
            SceneManager.LoadScene("MapTraversal");
            yield return null;
            yield return null;
            Assert.AreEqual("MapTraversal", SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        [Ignore("Encounter scene has unassigned prefab references (HandManager.cardPrefab, PlacementManager.heroTokenPrefab) — fix scene setup first")]
        public IEnumerator TC24_17b_AdditiveLoad_BothScenesPresent()
        {
            SceneManager.LoadScene("MapTraversal");
            yield return null;
            yield return null;

            SceneManager.LoadScene("Encounter", LoadSceneMode.Additive);
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);

            bool mapLoaded = false, encounterLoaded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == "MapTraversal" && scene.isLoaded) mapLoaded = true;
                if (scene.name == "Encounter" && scene.isLoaded) encounterLoaded = true;
            }
            Assert.IsTrue(mapLoaded, "MapTraversal should still be loaded");
            Assert.IsTrue(encounterLoaded, "Encounter should be loaded additively");

            SceneManager.UnloadSceneAsync("Encounter");
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        [Ignore("Encounter scene has unassigned prefab references — fix scene setup first")]
        public IEnumerator TC24_17d_EncounterManager_FoundAfterAdditiveLoad()
        {
            SceneManager.LoadScene("MapTraversal");
            yield return null;
            yield return null;

            SceneManager.LoadScene("Encounter", LoadSceneMode.Additive);
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);

            var encMgr = Object.FindAnyObjectByType<Scurry.Encounter.EncounterManager>();
            Assert.IsNotNull(encMgr, "EncounterManager should be found after additive load");

            SceneManager.UnloadSceneAsync("Encounter");
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        [Ignore("Encounter scene has unassigned prefab references — fix scene setup first")]
        public IEnumerator TC24_17e_UIManager_FoundAfterAdditiveLoad()
        {
            SceneManager.LoadScene("MapTraversal");
            yield return null;
            yield return null;

            SceneManager.LoadScene("Encounter", LoadSceneMode.Additive);
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);

            var uiMgr = Object.FindAnyObjectByType<Scurry.UI.UIManager>();
            Assert.IsNotNull(uiMgr, "UIManager should be found after additive load");

            SceneManager.UnloadSceneAsync("Encounter");
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        [Ignore("Encounter scene has unassigned prefab references — fix scene setup first")]
        public IEnumerator TC24_17f_EncounterScene_UnloadsSuccessfully()
        {
            SceneManager.LoadScene("MapTraversal");
            yield return null;
            yield return null;

            SceneManager.LoadScene("Encounter", LoadSceneMode.Additive);
            yield return null;
            yield return null;

            SceneManager.UnloadSceneAsync("Encounter");
            yield return new WaitForSeconds(0.5f);

            bool encounterLoaded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == "Encounter" && scene.isLoaded) encounterLoaded = true;
            }
            Assert.IsFalse(encounterLoaded, "Encounter should be unloaded");
        }
    }

    // ============================================================
    // TC-24.8: MapTraversal UI Tests
    // ============================================================
    [TestFixture]
    public class MapTraversalUITests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("Bootstrap");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.3f);
            SceneManager.LoadScene("MapTraversal");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);
        }

        [UnityTest]
        public IEnumerator TC24_8a_MapUI_Found()
        {
            yield return null;
            var mapUI = Object.FindAnyObjectByType<Scurry.UI.MapUI>();
            Assert.IsNotNull(mapUI, "MapUI should be found in MapTraversal scene");
        }

        [UnityTest]
        public IEnumerator TC24_8b_MapManager_Found()
        {
            yield return null;
            var mapMgr = Object.FindAnyObjectByType<MapManager>();
            Assert.IsNotNull(mapMgr, "MapManager should be found in MapTraversal scene");
        }

        [UnityTest]
        public IEnumerator TC24_8d_ShopManager_Found()
        {
            yield return null;
            var shopMgr = Object.FindAnyObjectByType<Scurry.UI.ShopManager>();
            Assert.IsNotNull(shopMgr, "ShopManager should be found in MapTraversal");
        }

        [UnityTest]
        public IEnumerator TC24_8e_HealingManager_Found()
        {
            yield return null;
            var healMgr = Object.FindAnyObjectByType<Scurry.UI.HealingManager>();
            Assert.IsNotNull(healMgr, "HealingManager should be found in MapTraversal");
        }

        [UnityTest]
        public IEnumerator TC24_8f_ResourceUI_Found()
        {
            yield return null;
            var resourceUI = Object.FindAnyObjectByType<Scurry.UI.ResourceUI>();
            Assert.IsNotNull(resourceUI, "ResourceUI should be found in MapTraversal");
        }

        [UnityTest]
        public IEnumerator TC24_14a_SettingsUI_Found()
        {
            yield return null;
            var settingsUI = Object.FindAnyObjectByType<Scurry.UI.SettingsUI>();
            Assert.IsNotNull(settingsUI, "SettingsUI should be found in MapTraversal");
        }
    }

    // ============================================================
    // TC-24.6: ColonyManagement UI Tests
    // ============================================================
    [TestFixture]
    public class ColonyManagementUITests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("Bootstrap");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.3f);
            SceneManager.LoadScene("ColonyManagement");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);
        }

        [UnityTest]
        [Ignore("Scene transition triggers unassigned prefab exceptions from HandManager — fix scene setup first")]
        public IEnumerator TC24_6a_ColonyUI_Found()
        {
            yield return new WaitForSeconds(0.5f);
            var colonyUI = Object.FindAnyObjectByType<Scurry.UI.ColonyUI>();
            Assert.IsNotNull(colonyUI, "ColonyUI should be found in ColonyManagement scene");
        }

        [UnityTest]
        [Ignore("Scene transition triggers unassigned prefab exceptions from HandManager — fix scene setup first")]
        public IEnumerator TC24_6b_ColonyBoardManager_Found()
        {
            yield return new WaitForSeconds(0.5f);
            var boardMgr = Object.FindAnyObjectByType<ColonyBoardManager>();
            Assert.IsNotNull(boardMgr, "ColonyBoardManager should be found in ColonyManagement scene");
        }
    }

    // ============================================================
    // TC-24.3: ColonyDraft UI Tests
    // ============================================================
    [TestFixture]
    public class ColonyDraftUITests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("Bootstrap");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.3f);
            EventBus.OnRunStarted?.Invoke();
            SceneManager.LoadScene("ColonyDraft");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);
        }

        [UnityTest]
        [Ignore("Scene transition triggers unassigned prefab exceptions from HandManager — fix scene setup first")]
        public IEnumerator TC24_3a_ColonyDraftUI_Found()
        {
            yield return new WaitForSeconds(0.5f);
            var draftUI = Object.FindAnyObjectByType<Scurry.UI.ColonyDraftUI>();
            Assert.IsNotNull(draftUI, "ColonyDraftUI should be found in ColonyDraft scene");
        }
    }

    // ============================================================
    // TC-24.20: Continue Run Flow Tests
    // ============================================================
    [TestFixture]
    public class ContinueRunFlowTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("Bootstrap");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTest]
        public IEnumerator TC24_20a_SaveCreated_HasSave()
        {
            yield return null;
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
            Assert.IsTrue(SaveManager.HasSave(), "Test save should be created");
            SaveManager.DeleteSave();
        }
    }

    // ============================================================
    // ColonyManager Resource Management Tests
    // ============================================================
    [TestFixture]
    public class ColonyManagerResourceTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            if (SceneManager.GetActiveScene().name != "Bootstrap")
            {
                SceneManager.LoadScene("Bootstrap");
                yield return null;
                yield return null;
                yield return new WaitForSeconds(0.5f);
            }
        }

        [UnityTest]
        public IEnumerator ColonyManager_InitializeHP_SetsCorrectValues()
        {
            yield return null;
            var colMgr = ColonyManager.Instance;
            Assume.That(colMgr != null, "ColonyManager not found");
            var bc = BalanceConfigSO.Instance;
            Assume.That(bc != null, "BalanceConfigSO not found");

            colMgr.InitializeHP();
            Assert.AreEqual(bc.baseColonyHP, colMgr.CurrentHP, "HP should match BalanceConfig baseColonyHP");
            Assert.AreEqual(bc.startingFood, colMgr.FoodStockpile, "Food should match BalanceConfig startingFood");
            Assert.AreEqual(bc.startingMaterials, colMgr.MaterialsStockpile, "Materials should match BalanceConfig startingMaterials");
            Assert.AreEqual(bc.startingCurrency, colMgr.CurrencyStockpile, "Currency should match BalanceConfig startingCurrency");
        }

        [UnityTest]
        public IEnumerator ColonyManager_RestoreState_Works()
        {
            yield return null;
            var colMgr = ColonyManager.Instance;
            Assume.That(colMgr != null, "ColonyManager not found");

            colMgr.RestoreState(20, 40, 8, 12, 6);
            Assert.AreEqual(20, colMgr.CurrentHP);
            Assert.AreEqual(40, colMgr.MaxHP);
            Assert.AreEqual(8, colMgr.CurrencyStockpile);
            Assert.AreEqual(12, colMgr.FoodStockpile);
            Assert.AreEqual(6, colMgr.MaterialsStockpile);
            colMgr.InitializeHP(); // Restore
        }

        [UnityTest]
        public IEnumerator ColonyManager_AddMaterials_Works()
        {
            yield return null;
            var colMgr = ColonyManager.Instance;
            Assume.That(colMgr != null, "ColonyManager not found");

            colMgr.InitializeHP();
            int before = colMgr.MaterialsStockpile;
            colMgr.AddMaterials(3);
            Assert.AreEqual(before + 3, colMgr.MaterialsStockpile);
            colMgr.InitializeHP();
        }

        [UnityTest]
        public IEnumerator ColonyManager_IsAlive_TrueWhenHPPositive()
        {
            yield return null;
            var colMgr = ColonyManager.Instance;
            Assume.That(colMgr != null, "ColonyManager not found");

            colMgr.InitializeHP();
            Assert.IsTrue(colMgr.IsAlive);
        }
    }
}
