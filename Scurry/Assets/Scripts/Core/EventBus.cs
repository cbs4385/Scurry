using System;
using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Colony;
using Scurry.Encounter;

namespace Scurry.Core
{
    public static class EventBus
    {
        // --- Legacy M0 events (kept for backward compatibility until full rewrite) ---
        public static Action<GamePhase> OnPhaseChanged;
        public static Action<CardDefinitionSO> OnCardDrawn;
        public static Action<CardDefinitionSO, Vector2Int> OnCardPlaced;
        public static Action<Vector2Int> OnHeroMoved;
        public static Action<Vector2Int> OnEnemyMoved;
        public static Action<int, int, bool> OnCombatResolved; // heroCombat, enemyStrength, won
        public static Action<ResourceType, int> OnResourceCollected;
        public static Action<int, int> OnColonyHPChanged; // current, max
        public static Action OnTurnEnded;
        public static Action OnUndoPlacement;
        public static Action OnGatheringComplete;
        public static Action<List<CardDefinitionSO>> OnDeckBuildComplete;
        public static Action<string, Color> OnGatheringNotification; // message, color
        public static Action<string> OnTileHovered; // tooltip text
        public static Action OnTileUnhovered;
        public static Action<string> OnResourceTokenCollected; // token name
        public static Action<bool> OnCardPlacementGameComplete; // heroesLost
        public static Action<StepType> OnStepStarted;
        public static Action<int, int> OnStageProgress; // currentStep, totalSteps
        public static Action<StepType[]> OnStepChoicePresented;
        public static Action<StepType> OnStepChosen;

        // --- M1 Colony Management events ---
        public static Action<ColonyConfig> OnColonyManagementComplete;
        public static Action<List<CardDefinitionSO>> OnHeroDeckReady;

        // --- M1 Map events ---
        public static Action OnMapReady;
        public static Action<Map.MapNode> OnMapNodeSelected;
        public static Action OnMapNodeComplete;
        public static Action OnLevelComplete;

        // --- M1 Encounter events ---
        public static Action<EncounterResult> OnEncounterComplete;
        public static Action OnRecallInitiated;
        public static Action OnAutoDeployComplete;
        public static Action OnEquipmentAssigned;

        // --- M1 Boss events ---
        public static Action<string> OnBossPhaseChanged;
        public static Action<int, int> OnBossHPChanged; // current, max
        public static Action OnBossDefeated;

        // --- M3 Achievement events ---
        public static Action<string> OnAchievementUnlocked;

        // --- M1 Run events ---
        public static Action OnRunStarted;
        public static Action<int> OnLevelStarted;
        public static Action<int> OnFoodConsumed; // remaining food
        public static Action<int> OnStarvationDamage; // damage amount
        public static Action<bool> OnRunComplete_M1; // victory
        public static Action<int> OnLevelAdvanced; // new level number
        public static Action OnRunFailed_M1;

        // Legacy run events
        public static Action OnRunComplete;
        public static Action OnRunFailed;

        // --- M1 Node handler events ---
        public static Action OnShopComplete;
        public static Action OnHealingComplete;
        public static Action OnUpgradeComplete;
        public static Action OnDraftComplete;
        public static Action OnEventComplete;
        public static Action OnRestComplete;

        // --- M1 Card management events ---
        public static Action<CardDefinitionSO> OnCardPurchased;
        public static Action<CardDefinitionSO> OnCardDrafted;
        public static Action<CardDefinitionSO> OnCardRemoved;
        public static Action OnEventWoundHero;

        public static void Reset()
        {
            Debug.Log("[EventBus] Reset: clearing all event subscriptions");
            OnPhaseChanged = null;
            OnCardDrawn = null;
            OnCardPlaced = null;
            OnHeroMoved = null;
            OnEnemyMoved = null;
            OnCombatResolved = null;
            OnResourceCollected = null;
            OnColonyHPChanged = null;
            OnTurnEnded = null;
            OnUndoPlacement = null;
            OnGatheringComplete = null;
            OnDeckBuildComplete = null;
            OnGatheringNotification = null;
            OnTileHovered = null;
            OnTileUnhovered = null;
            OnResourceTokenCollected = null;
            OnCardPlacementGameComplete = null;
            OnStepStarted = null;
            OnStageProgress = null;
            OnStepChoicePresented = null;
            OnStepChosen = null;

            // M1 events
            OnColonyManagementComplete = null;
            OnHeroDeckReady = null;
            OnMapReady = null;
            OnMapNodeSelected = null;
            OnMapNodeComplete = null;
            OnLevelComplete = null;
            OnEncounterComplete = null;
            OnRecallInitiated = null;
            OnAutoDeployComplete = null;
            OnEquipmentAssigned = null;
            OnBossPhaseChanged = null;
            OnBossHPChanged = null;
            OnBossDefeated = null;
            OnRunStarted = null;
            OnLevelStarted = null;
            OnFoodConsumed = null;
            OnStarvationDamage = null;
            OnRunComplete_M1 = null;
            OnLevelAdvanced = null;
            OnRunFailed_M1 = null;
            OnRunComplete = null;
            OnRunFailed = null;
            OnShopComplete = null;
            OnHealingComplete = null;
            OnUpgradeComplete = null;
            OnDraftComplete = null;
            OnEventComplete = null;
            OnRestComplete = null;
            OnCardPurchased = null;
            OnCardDrafted = null;
            OnCardRemoved = null;
            OnEventWoundHero = null;
            OnAchievementUnlocked = null;

            Debug.Log("[EventBus] Reset: complete — all events nulled");
        }

        public static void LogSubscriberCounts()
        {
            int Count(Delegate d) => d?.GetInvocationList().Length ?? 0;
            Debug.Log($"[EventBus] Subscriber counts: " +
                $"OnPhaseChanged={Count(OnPhaseChanged)}, " +
                $"OnColonyManagementComplete={Count(OnColonyManagementComplete)}, " +
                $"OnMapNodeSelected={Count(OnMapNodeSelected)}, " +
                $"OnEncounterComplete={Count(OnEncounterComplete)}, " +
                $"OnRunStarted={Count(OnRunStarted)}, " +
                $"OnLevelComplete={Count(OnLevelComplete)}");
        }
    }
}
