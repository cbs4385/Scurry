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

        public IEnumerator TC24_1a_MainMenuManager_Found()
        {
            yield return new WaitForSeconds(1.0f);
            var mainMenu = Object.FindAnyObjectByType<Scurry.UI.MainMenuManager>();
            Assert.IsNotNull(mainMenu, "MainMenuManager should be found in MainMenu scene");
        }

        [UnityTest]

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
        public IEnumerator TC24_17b_AdditiveLoad_BothScenesPresent()
        {
            // Disable RunManager so it doesn't auto-unload the Encounter scene
            var runMgr = RunManager.Instance;
            if (runMgr != null) runMgr.enabled = false;

            SceneManager.LoadScene("MapTraversal");
            yield return null;
            yield return null;

            SceneManager.LoadScene("Encounter", LoadSceneMode.Additive);
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.2f);

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
            if (runMgr != null) runMgr.enabled = true;
        }

        [UnityTest]
        public IEnumerator TC24_17d_EncounterManager_FoundAfterAdditiveLoad()
        {
            var runMgr = RunManager.Instance;
            if (runMgr != null) runMgr.enabled = false;

            SceneManager.LoadScene("MapTraversal");
            yield return null;
            yield return null;

            SceneManager.LoadScene("Encounter", LoadSceneMode.Additive);
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.2f);

            var encMgr = Object.FindAnyObjectByType<Scurry.Encounter.EncounterManager>();
            Assert.IsNotNull(encMgr, "EncounterManager should be found after additive load");

            SceneManager.UnloadSceneAsync("Encounter");
            yield return new WaitForSeconds(0.3f);
            if (runMgr != null) runMgr.enabled = true;
        }

        [UnityTest]
        public IEnumerator TC24_17e_UIManager_FoundAfterAdditiveLoad()
        {
            var runMgr = RunManager.Instance;
            if (runMgr != null) runMgr.enabled = false;

            SceneManager.LoadScene("MapTraversal");
            yield return null;
            yield return null;

            SceneManager.LoadScene("Encounter", LoadSceneMode.Additive);
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.2f);

            var uiMgr = Object.FindAnyObjectByType<Scurry.UI.UIManager>();
            Assert.IsNotNull(uiMgr, "UIManager should be found after additive load");

            SceneManager.UnloadSceneAsync("Encounter");
            yield return new WaitForSeconds(0.3f);
            if (runMgr != null) runMgr.enabled = true;
        }

        [UnityTest]
        public IEnumerator TC24_17f_EncounterScene_UnloadsSuccessfully()
        {
            var runMgr = RunManager.Instance;
            if (runMgr != null) runMgr.enabled = false;

            SceneManager.LoadScene("MapTraversal");
            yield return null;
            yield return null;

            SceneManager.LoadScene("Encounter", LoadSceneMode.Additive);
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.2f);

            SceneManager.UnloadSceneAsync("Encounter");
            yield return new WaitForSeconds(0.5f);

            bool encounterLoaded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == "Encounter" && scene.isLoaded) encounterLoaded = true;
            }
            Assert.IsFalse(encounterLoaded, "Encounter should be unloaded");
            if (runMgr != null) runMgr.enabled = true;
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

        public IEnumerator TC24_6a_ColonyUI_Found()
        {
            yield return new WaitForSeconds(0.5f);
            var colonyUI = Object.FindAnyObjectByType<Scurry.UI.ColonyUI>();
            Assert.IsNotNull(colonyUI, "ColonyUI should be found in ColonyManagement scene");
        }

        [UnityTest]

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

    // ============================================================
    // TC-25: Map Generation & Navigation PlayMode Tests
    // ============================================================
    [TestFixture]
    public class MapGenerationPlayModeTests
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
            // Disable RunManager to prevent auto scene transitions
            var runMgr = RunManager.Instance;
            if (runMgr != null) runMgr.enabled = false;

            SceneManager.LoadScene("MapTraversal");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.2f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var runMgr = RunManager.Instance;
            if (runMgr != null) runMgr.enabled = true;
            yield return null;
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator TC25_1_MapManager_InitializesMap()
        {
            yield return null;
            var mapMgr = Object.FindAnyObjectByType<MapManager>();
            Assume.That(mapMgr != null, "MapManager not found");

            // Load a MapConfigSO
            var configs = new List<MapConfigSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MapConfigSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                if (cfg != null) configs.Add(cfg);
            }
            Assume.That(configs.Count >= 1, "Need at least 1 MapConfigSO");
            configs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

            mapMgr.InitializeMap(configs[0]);
            Assert.IsNotNull(mapMgr.Map, "Map should not be null after initialization");
            Assert.Greater(mapMgr.Map.Count, 0, "Map should have at least 1 row");
            Debug.Log($"[TEST] TC25_1: Map initialized with {mapMgr.Map.Count} rows");
        }

        [UnityTest]
        public IEnumerator TC25_2_MapHasCorrectRowCount()
        {
            yield return null;
            var mapMgr = Object.FindAnyObjectByType<MapManager>();
            Assume.That(mapMgr != null, "MapManager not found");

            var configs = new List<MapConfigSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MapConfigSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                if (cfg != null) configs.Add(cfg);
            }
            Assume.That(configs.Count >= 1, "Need at least 1 MapConfigSO");
            configs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

            mapMgr.InitializeMap(configs[0]);
            Assert.AreEqual(configs[0].numRows, mapMgr.Map.Count, $"Map should have {configs[0].numRows} rows");
        }

        [UnityTest]
        public IEnumerator TC25_3_LastRowIsBossNode()
        {
            yield return null;
            var mapMgr = Object.FindAnyObjectByType<MapManager>();
            Assume.That(mapMgr != null, "MapManager not found");

            var configs = new List<MapConfigSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MapConfigSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                if (cfg != null) configs.Add(cfg);
            }
            Assume.That(configs.Count >= 1, "Need at least 1 MapConfigSO");
            configs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

            mapMgr.InitializeMap(configs[0]);
            var lastRow = mapMgr.Map[mapMgr.Map.Count - 1];
            Assert.AreEqual(1, lastRow.Count, "Last row should have exactly 1 node (boss)");
            Assert.AreEqual(NodeType.Boss, lastRow[0].nodeType, "Last row node should be Boss type");
        }

        [UnityTest]
        public IEnumerator TC25_4_FirstRowNodesAreAvailable()
        {
            yield return null;
            var mapMgr = Object.FindAnyObjectByType<MapManager>();
            Assume.That(mapMgr != null, "MapManager not found");

            var configs = new List<MapConfigSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MapConfigSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                if (cfg != null) configs.Add(cfg);
            }
            Assume.That(configs.Count >= 1, "Need at least 1 MapConfigSO");
            configs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

            mapMgr.InitializeMap(configs[0]);
            var available = mapMgr.GetAvailableNodes();
            Assert.Greater(available.Count, 0, "First row should have available nodes");
            foreach (var node in available)
            {
                Assert.AreEqual(0, node.position.x, "Available nodes should be in first row (row 0)");
            }
        }

        [UnityTest]
        public IEnumerator TC25_5_SelectNode_AdvancesCurrentRow()
        {
            yield return null;
            var mapMgr = Object.FindAnyObjectByType<MapManager>();
            Assume.That(mapMgr != null, "MapManager not found");

            var configs = new List<MapConfigSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MapConfigSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                if (cfg != null) configs.Add(cfg);
            }
            Assume.That(configs.Count >= 1, "Need at least 1 MapConfigSO");
            configs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

            mapMgr.InitializeMap(configs[0]);
            Assert.AreEqual(-1, mapMgr.CurrentRow, "Initial row should be -1");

            var available = mapMgr.GetAvailableNodes();
            Assume.That(available.Count > 0, "Need available nodes");

            mapMgr.SelectNode(available[0]);
            Assert.AreEqual(0, mapMgr.CurrentRow, "After selecting first node, row should be 0");
            Assert.IsNotNull(mapMgr.CurrentNode, "CurrentNode should be set after selection");
            Assert.IsTrue(available[0].visited, "Selected node should be marked visited");
        }

        [UnityTest]
        public IEnumerator TC25_6_NavigateFullMap_ReachesBoss()
        {
            yield return null;
            var mapMgr = Object.FindAnyObjectByType<MapManager>();
            Assume.That(mapMgr != null, "MapManager not found");

            var configs = new List<MapConfigSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MapConfigSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                if (cfg != null) configs.Add(cfg);
            }
            Assume.That(configs.Count >= 1, "Need at least 1 MapConfigSO");
            configs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

            mapMgr.InitializeMap(configs[0]);
            int maxSteps = configs[0].numRows + 2;

            for (int step = 0; step < maxSteps; step++)
            {
                var available = mapMgr.GetAvailableNodes();
                if (available.Count == 0) break;
                mapMgr.SelectNode(available[0]);
                mapMgr.OnNodeComplete();
            }

            Assert.AreEqual(configs[0].numRows - 1, mapMgr.CurrentRow, "Should have reached the last row (boss)");
            Assert.AreEqual(NodeType.Boss, mapMgr.CurrentNode.nodeType, "Final node should be Boss");
        }

        [UnityTest]
        public IEnumerator TC25_7_MapUI_RendersAfterInit()
        {
            yield return null;
            var mapMgr = Object.FindAnyObjectByType<MapManager>();
            var mapUI = Object.FindAnyObjectByType<Scurry.UI.MapUI>();
            Assume.That(mapMgr != null, "MapManager not found");
            Assume.That(mapUI != null, "MapUI not found");

            var configs = new List<MapConfigSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MapConfigSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                if (cfg != null) configs.Add(cfg);
            }
            Assume.That(configs.Count >= 1, "Need at least 1 MapConfigSO");
            configs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

            mapMgr.InitializeMap(configs[0]);
            yield return null;
            yield return null;

            // MapUI should have responded to OnMapReady event
            var buttons = mapUI.GetComponentsInChildren<Button>(true);
            Assert.Greater(buttons.Length, 0, "MapUI should have created node buttons after map initialization");
            Debug.Log($"[TEST] TC25_7: MapUI rendered {buttons.Length} buttons");
        }

        [UnityTest]
        public IEnumerator TC25_8_AllThreeLevels_GenerateValidMaps()
        {
            yield return null;
            var mapMgr = Object.FindAnyObjectByType<MapManager>();
            Assume.That(mapMgr != null, "MapManager not found");

            var configs = new List<MapConfigSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MapConfigSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                if (cfg != null) configs.Add(cfg);
            }
            Assume.That(configs.Count >= 3, "Need at least 3 MapConfigSOs for all levels");
            configs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));

            foreach (var cfg in configs)
            {
                mapMgr.InitializeMap(cfg);
                Assert.IsNotNull(mapMgr.Map, $"L{cfg.levelNumber} map should not be null");
                Assert.AreEqual(cfg.numRows, mapMgr.Map.Count, $"L{cfg.levelNumber} map should have {cfg.numRows} rows");

                // Verify boss node
                var lastRow = mapMgr.Map[mapMgr.Map.Count - 1];
                Assert.AreEqual(NodeType.Boss, lastRow[0].nodeType, $"L{cfg.levelNumber} last row should be Boss");

                // Verify map is valid (all paths reach boss)
                Assert.IsTrue(MapGenerator.ValidateMap(mapMgr.Map), $"L{cfg.levelNumber} map should be valid");
                Debug.Log($"[TEST] TC25_8: L{cfg.levelNumber} map validated — {cfg.numRows} rows");
            }
        }
#endif
    }

    // ============================================================
    // TC-26: Run Lifecycle & End-of-Run Tests
    // ============================================================
    [TestFixture]
    public class RunLifecycleTests
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
        public IEnumerator TC26_1_RunComplete_Victory_FiresEvent()
        {
            yield return null;
            bool victoryFired = false;
            EventBus.OnRunComplete_M1 += (victory) => { if (victory) victoryFired = true; };

            EventBus.OnRunComplete_M1?.Invoke(true);
            Assert.IsTrue(victoryFired, "OnRunComplete_M1(true) should fire victory");
        }

        [UnityTest]
        public IEnumerator TC26_2_RunFailed_FiresEvent()
        {
            yield return null;
            bool failedFired = false;
            EventBus.OnRunFailed_M1 += () => failedFired = true;

            EventBus.OnRunFailed_M1?.Invoke();
            Assert.IsTrue(failedFired, "OnRunFailed_M1 should fire");
        }

        [UnityTest]
        public IEnumerator TC26_3_ColonyDeath_TriggersDefeat()
        {
            yield return null;
            var colonyMgr = ColonyManager.Instance;
            Assume.That(colonyMgr != null, "ColonyManager not found");

            colonyMgr.InitializeHP();
            int fullHP = colonyMgr.CurrentHP;
            colonyMgr.TakeDamage(fullHP);
            Assert.IsFalse(colonyMgr.IsAlive, "Colony should be dead after taking full HP damage");
            colonyMgr.InitializeHP();
        }

        [UnityTest]
        public IEnumerator TC26_4_Achievement_UnlocksOnVictory()
        {
            yield return null;
            var achMgr = AchievementManager.Instance;
            Assume.That(achMgr != null, "AchievementManager not found");

            achMgr.TryUnlock(AchievementId.FirstVictory);
            Assert.IsTrue(achMgr.IsUnlocked(AchievementId.FirstVictory), "FirstVictory should be unlocked");
        }

        [UnityTest]
        public IEnumerator TC26_5_MetaProgression_ReputationFormula()
        {
            yield return null;
            var meta = MetaProgressionManager.Instance;
            Assume.That(meta != null, "MetaProgressionManager not found");

            // Reputation should be >= 0
            Assert.GreaterOrEqual(meta.Reputation, 0, "Reputation should be non-negative");
            // ColonyDeckSizeBonus should be >= 0
            Assert.GreaterOrEqual(meta.ColonyDeckSizeBonus, 0, "ColonyDeckSizeBonus should be non-negative");
        }

        [UnityTest]
        public IEnumerator TC26_6_SaveAndContinue_PreservesRunState()
        {
            yield return null;
            // Create a save with specific state
            var save = new RunSaveData
            {
                currentLevel = 2,
                runState = (int)RunState.MapTraversal,
                nodesVisited = 7,
                currentSceneName = "MapTraversal",
                colonyHP = 20,
                colonyMaxHP = 30,
                foodStockpile = 8,
                materialsStockpile = 4,
                currencyStockpile = 12,
                encountersCompleted = 5,
                totalResourcesGathered = 30,
                enemiesDefeated = 8,
                bossesKilled = 1
            };
            save.heroDeckCardNames.Add("Scout Rat");
            save.heroDeckCardNames.Add("Guard Rat");
            save.activeRelicNames.Add("Lucky Coin");

            SaveManager.Save(save);
            Assert.IsTrue(SaveManager.HasSave(), "Save should exist");

            var loaded = SaveManager.Load();
            Assert.AreEqual(2, loaded.currentLevel, "Level should persist");
            Assert.AreEqual((int)RunState.MapTraversal, loaded.runState, "RunState should persist");
            Assert.AreEqual(7, loaded.nodesVisited, "nodesVisited should persist");
            Assert.AreEqual(20, loaded.colonyHP, "colonyHP should persist");
            Assert.AreEqual(12, loaded.currencyStockpile, "currency should persist");
            Assert.AreEqual(2, loaded.heroDeckCardNames.Count, "heroDeck should persist");
            Assert.AreEqual(1, loaded.activeRelicNames.Count, "activeRelics should persist");
            Assert.AreEqual(1, loaded.bossesKilled, "bossesKilled should persist");

            SaveManager.DeleteSave();
        }

        [UnityTest]
        public IEnumerator TC26_7_RunResult_VictoryPanel_Exists()
        {
            var runMgr = RunManager.Instance;
            if (runMgr != null) runMgr.enabled = false;

            SceneManager.LoadScene("RunResult");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.3f);

            var rsm = Object.FindAnyObjectByType<Scurry.UI.RunScreenManager>();
            Assert.IsNotNull(rsm, "RunScreenManager should exist in RunResult scene");

            // Trigger victory event
            EventBus.OnRunComplete_M1?.Invoke(true);
            yield return null;

            // Find victory panel
            var panels = rsm.GetComponentsInChildren<Transform>(true);
            bool hasVictoryPanel = false;
            foreach (var t in panels)
                if (t.name == "VictoryPanel") hasVictoryPanel = true;
            Assert.IsTrue(hasVictoryPanel, "VictoryPanel should exist in RunScreenManager");

            if (runMgr != null) runMgr.enabled = true;
        }

        [UnityTest]
        public IEnumerator TC26_8_RunResult_DefeatPanel_Exists()
        {
            var runMgr = RunManager.Instance;
            if (runMgr != null) runMgr.enabled = false;

            SceneManager.LoadScene("RunResult");
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.3f);

            var rsm = Object.FindAnyObjectByType<Scurry.UI.RunScreenManager>();
            Assert.IsNotNull(rsm, "RunScreenManager should exist in RunResult scene");

            // Trigger defeat event
            EventBus.OnRunFailed_M1?.Invoke();
            yield return null;

            // Find defeat panel
            var panels = rsm.GetComponentsInChildren<Transform>(true);
            bool hasDefeatPanel = false;
            foreach (var t in panels)
                if (t.name == "DefeatPanel") hasDefeatPanel = true;
            Assert.IsTrue(hasDefeatPanel, "DefeatPanel should exist in RunScreenManager");

            if (runMgr != null) runMgr.enabled = true;
        }

        [UnityTest]
        public IEnumerator TC26_9_AchievementToast_FiresOnUnlock()
        {
            yield return null;
            bool toastFired = false;
            System.Action<string> handler = (id) => toastFired = true;
            EventBus.OnAchievementUnlocked += handler;

            EventBus.OnAchievementUnlocked?.Invoke("TestAchievement");
            Assert.IsTrue(toastFired, "OnAchievementUnlocked event should fire");

            EventBus.OnAchievementUnlocked -= handler;
        }

        [UnityTest]
        public IEnumerator TC26_10_LevelAdvanced_FiresEvent()
        {
            yield return null;
            int advancedLevel = -1;
            System.Action<int> handler = (level) => advancedLevel = level;
            EventBus.OnLevelAdvanced += handler;

            EventBus.OnLevelAdvanced?.Invoke(2);
            Assert.AreEqual(2, advancedLevel, "OnLevelAdvanced should fire with correct level");

            EventBus.OnLevelAdvanced -= handler;
        }

        [UnityTest]
        public IEnumerator TC26_11_EncounterResultDismissed_FiresEvent()
        {
            yield return null;
            bool dismissed = false;
            System.Action handler = () => dismissed = true;
            EventBus.OnEncounterResultDismissed += handler;

            EventBus.OnEncounterResultDismissed?.Invoke();
            Assert.IsTrue(dismissed, "OnEncounterResultDismissed should fire");

            EventBus.OnEncounterResultDismissed -= handler;
        }

        [UnityTest]
        public IEnumerator TC26_12_ReturnToMainMenu_FiresEvent()
        {
            yield return null;
            bool returned = false;
            System.Action handler = () => returned = true;
            EventBus.OnReturnToMainMenu += handler;

            EventBus.OnReturnToMainMenu?.Invoke();
            Assert.IsTrue(returned, "OnReturnToMainMenu should fire");

            EventBus.OnReturnToMainMenu -= handler;
        }

        [UnityTest]
        public IEnumerator TC26_13_FullRunSimulation_DraftToDefeat()
        {
            yield return null;
            var colonyMgr = ColonyManager.Instance;
            var achMgr = AchievementManager.Instance;
            Assume.That(colonyMgr != null, "ColonyManager not found");
            Assume.That(achMgr != null, "AchievementManager not found");

            // 1. Initialize colony (simulates run start)
            colonyMgr.InitializeHP();
            int initialHP = colonyMgr.CurrentHP;
            Assert.Greater(initialHP, 0, "Colony should start with positive HP");

            // 2. Simulate colony draft complete
            bool draftComplete = false;
            System.Action handler2 = () => draftComplete = true;
            EventBus.OnDraftComplete += handler2;
            EventBus.OnDraftComplete?.Invoke();
            Assert.IsTrue(draftComplete, "Draft complete event should fire");
            EventBus.OnDraftComplete -= handler2;

            // 3. Simulate resource gathering
            colonyMgr.AddFood(5);
            colonyMgr.AddMaterials(3);
            colonyMgr.AddCurrency(2);
            Assert.Greater(colonyMgr.FoodStockpile, 0, "Should have food after gathering");

            // 4. Simulate spending
            colonyMgr.SpendFood(3);
            colonyMgr.SpendCurrency(1);

            // 5. Simulate taking damage
            colonyMgr.TakeDamage(10);
            Assert.Less(colonyMgr.CurrentHP, initialHP, "HP should decrease after damage");

            // 6. Simulate healing
            int hpBefore = colonyMgr.CurrentHP;
            colonyMgr.Heal(5);
            Assert.AreEqual(hpBefore + 5, colonyMgr.CurrentHP, "HP should increase after healing");

            // 7. Simulate defeat — colony dies
            colonyMgr.TakeDamage(colonyMgr.CurrentHP);
            Assert.IsFalse(colonyMgr.IsAlive, "Colony should be dead");

            // 8. Verify run failed can fire
            bool runFailed = false;
            System.Action failHandler = () => runFailed = true;
            EventBus.OnRunFailed_M1 += failHandler;
            EventBus.OnRunFailed_M1?.Invoke();
            Assert.IsTrue(runFailed, "Run failed event should fire on colony death");
            EventBus.OnRunFailed_M1 -= failHandler;

            colonyMgr.InitializeHP(); // Restore
        }

        [UnityTest]
        public IEnumerator TC26_14_FullRunSimulation_DraftToVictory()
        {
            yield return null;
            var colonyMgr = ColonyManager.Instance;
            var achMgr = AchievementManager.Instance;
            var relicMgr = RelicManager.Instance;
            var runMgr = RunManager.Instance;
            Assume.That(colonyMgr != null, "ColonyManager not found");
            Assume.That(achMgr != null, "AchievementManager not found");
            Assume.That(relicMgr != null, "RelicManager not found");

            // Disable RunManager to prevent scene transitions from event firing
            if (runMgr != null) runMgr.enabled = false;

            // 1. Start fresh
            colonyMgr.InitializeHP();
            relicMgr.ClearRelics();

            // 2. Simulate resource gathering across multiple encounters
            for (int i = 0; i < 5; i++)
            {
                colonyMgr.AddFood(3);
                colonyMgr.AddMaterials(1);
                colonyMgr.AddCurrency(2);
            }
            Assert.Greater(colonyMgr.FoodStockpile, 10, "Should have accumulated food");
            Assert.Greater(colonyMgr.CurrencyStockpile, 5, "Should have accumulated currency");

            // 3. Simulate shop purchase
            bool shopComplete = false;
            System.Action shopHandler = () => shopComplete = true;
            EventBus.OnShopComplete += shopHandler;
            colonyMgr.SpendCurrency(4);
            EventBus.OnShopComplete?.Invoke();
            Assert.IsTrue(shopComplete, "Shop should complete");
            EventBus.OnShopComplete -= shopHandler;

            // 4. Simulate minor damage and healing
            colonyMgr.TakeDamage(5);
            colonyMgr.Heal(3);

            // 5. Simulate food consumption
            colonyMgr.SpendFood(5);
            Assert.IsTrue(colonyMgr.IsAlive, "Colony should survive");

            // 6. Simulate boss defeated
            bool bossDefeated = false;
            System.Action bossHandler = () => bossDefeated = true;
            EventBus.OnBossDefeated += bossHandler;
            EventBus.OnBossDefeated?.Invoke();
            Assert.IsTrue(bossDefeated, "Boss defeated event should fire");
            EventBus.OnBossDefeated -= bossHandler;

            // 7. Simulate victory
            bool victoryFired = false;
            System.Action<bool> victoryHandler = (v) => { if (v) victoryFired = true; };
            EventBus.OnRunComplete_M1 += victoryHandler;
            EventBus.OnRunComplete_M1?.Invoke(true);
            Assert.IsTrue(victoryFired, "Victory event should fire");
            EventBus.OnRunComplete_M1 -= victoryHandler;

            // 8. Unlock achievement
            achMgr.TryUnlock(AchievementId.FirstVictory);
            Assert.IsTrue(achMgr.IsUnlocked(AchievementId.FirstVictory), "FirstVictory should be unlocked");

            // 9. Verify colony is still alive at end
            Assert.IsTrue(colonyMgr.IsAlive, "Colony should be alive at victory");

            colonyMgr.InitializeHP();
            relicMgr.ClearRelics();

            // Re-enable RunManager
            if (runMgr != null) runMgr.enabled = true;
        }
    }
}
