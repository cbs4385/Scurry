using System;
using UnityEngine;
using Scurry.Data;
using Scurry.Map;

namespace Scurry.Core
{
    /// <summary>
    /// Central event bus for Scurry v2.0. All events are static Actions.
    /// Subscribe in OnEnable, unsubscribe in OnDisable. Call Reset() on scene teardown.
    /// </summary>
    public static class EventBus
    {
        // ── Turn Flow ──────────────────────────────────────────────────
        public static Action<int> OnTurnStarted;
        public static Action<GamePhase> OnPhaseChanged;
        public static Action OnTurnEnded;

        // ── Colony ─────────────────────────────────────────────────────
        public static Action<ColonyCardDefinitionSO> OnColonyCardPlayed;
        public static Action OnColonyProductionComplete;

        // ── Deployment ─────────────────────────────────────────────────
        public static Action<HeroToken, int> OnHeroDeployed;
        public static Action<CardDefinitionSO, HeroToken> OnEquipmentAttached;
        public static Action<HeroToken, int> OnTargetAssigned;

        // ── Movement ───────────────────────────────────────────────────
        public static Action<HeroToken, int, int> OnHeroMoved;
        public static Action<int, int, int> OnEnemyMoved;

        // ── Combat ─────────────────────────────────────────────────────
        public static Action<int> OnCombatStarted;
        public static Action<int, int, int, int> OnCombatRound;
        public static Action<int, bool> OnCombatEnded;
        public static Action<CardDefinitionSO> OnTacticalCardPlayed;

        // ── Resources ──────────────────────────────────────────────────
        public static Action<HeroToken, ResourceType, int> OnResourceGathered;
        public static Action<HeroToken, ResourceType, int> OnResourceDeposited;
        public static Action<int, ResourceType, int> OnResourceDropped;

        // ── Fog of War ─────────────────────────────────────────────────
        public static Action<int> OnNodeRevealed;
        public static Action<int> OnNodeHidden;

        // ── Card Rewards ──────────────────────────────────────────────
        public static Action<CardDefinitionSO> OnCardRewardSelected;
        public static Action OnCardRewardSkipped;

        // ── Pied Piper Countdown ─────────────────────────────────────
        public static Action<int> OnPiperCountdownStarted;    // int = total turns
        public static Action<int> OnPiperCountdownTick;       // int = turns remaining
        public static Action OnPiperMarchesOnColony;

        // ── Run ────────────────────────────────────────────────────────
        public static Action OnRunStarted;
        public static Action<bool> OnRunComplete;
        public static Action<int> OnScoreCalculated;

        // ── UI ─────────────────────────────────────────────────────────
        public static Action<string, Color> OnNotification;
        public static Action<string> OnTooltipShow;
        public static Action OnTooltipHide;

        // ── Scene ──────────────────────────────────────────────────────
        public static Action OnReturnToMainMenu;

        // ── Achievements ───────────────────────────────────────────────
        public static Action<string> OnAchievementUnlocked;

        // ── Utility ────────────────────────────────────────────────────

        /// <summary>
        /// Nulls every event delegate. Call on scene teardown to prevent stale subscriptions.
        /// </summary>
        public static void Reset()
        {
            Debug.Log("[EventBus] Reset: clearing all event subscriptions");

            OnTurnStarted = null;
            OnPhaseChanged = null;
            OnTurnEnded = null;
            OnColonyCardPlayed = null;
            OnColonyProductionComplete = null;
            OnHeroDeployed = null;
            OnEquipmentAttached = null;
            OnTargetAssigned = null;
            OnHeroMoved = null;
            OnEnemyMoved = null;
            OnCombatStarted = null;
            OnCombatRound = null;
            OnCombatEnded = null;
            OnTacticalCardPlayed = null;
            OnResourceGathered = null;
            OnResourceDeposited = null;
            OnResourceDropped = null;
            OnNodeRevealed = null;
            OnNodeHidden = null;
            OnCardRewardSelected = null;
            OnCardRewardSkipped = null;
            OnPiperCountdownStarted = null;
            OnPiperCountdownTick = null;
            OnPiperMarchesOnColony = null;
            OnRunStarted = null;
            OnRunComplete = null;
            OnScoreCalculated = null;
            OnNotification = null;
            OnTooltipShow = null;
            OnTooltipHide = null;
            OnReturnToMainMenu = null;
            OnAchievementUnlocked = null;

            Debug.Log("[EventBus] Reset: complete");
        }

        /// <summary>
        /// Logs the subscriber count for every event.
        /// </summary>
        public static void LogSubscriberCounts()
        {
            int Count(Delegate d) => d?.GetInvocationList().Length ?? 0;

            Debug.Log("[EventBus] LogSubscriberCounts: Turn flow — " +
                $"OnTurnStarted={Count(OnTurnStarted)}, " +
                $"OnPhaseChanged={Count(OnPhaseChanged)}, " +
                $"OnTurnEnded={Count(OnTurnEnded)}");

            Debug.Log("[EventBus] LogSubscriberCounts: Colony — " +
                $"OnColonyCardPlayed={Count(OnColonyCardPlayed)}, " +
                $"OnColonyProductionComplete={Count(OnColonyProductionComplete)}");

            Debug.Log("[EventBus] LogSubscriberCounts: Deployment — " +
                $"OnHeroDeployed={Count(OnHeroDeployed)}, " +
                $"OnEquipmentAttached={Count(OnEquipmentAttached)}, " +
                $"OnTargetAssigned={Count(OnTargetAssigned)}");

            Debug.Log("[EventBus] LogSubscriberCounts: Movement — " +
                $"OnHeroMoved={Count(OnHeroMoved)}, " +
                $"OnEnemyMoved={Count(OnEnemyMoved)}");

            Debug.Log("[EventBus] LogSubscriberCounts: Combat — " +
                $"OnCombatStarted={Count(OnCombatStarted)}, " +
                $"OnCombatRound={Count(OnCombatRound)}, " +
                $"OnCombatEnded={Count(OnCombatEnded)}, " +
                $"OnTacticalCardPlayed={Count(OnTacticalCardPlayed)}");

            Debug.Log("[EventBus] LogSubscriberCounts: Resources — " +
                $"OnResourceGathered={Count(OnResourceGathered)}, " +
                $"OnResourceDeposited={Count(OnResourceDeposited)}, " +
                $"OnResourceDropped={Count(OnResourceDropped)}");

            Debug.Log("[EventBus] LogSubscriberCounts: Fog — " +
                $"OnNodeRevealed={Count(OnNodeRevealed)}, " +
                $"OnNodeHidden={Count(OnNodeHidden)}");

            Debug.Log("[EventBus] LogSubscriberCounts: Card Rewards — " +
                $"OnCardRewardSelected={Count(OnCardRewardSelected)}, " +
                $"OnCardRewardSkipped={Count(OnCardRewardSkipped)}");

            Debug.Log("[EventBus] LogSubscriberCounts: Pied Piper — " +
                $"OnPiperCountdownStarted={Count(OnPiperCountdownStarted)}, " +
                $"OnPiperCountdownTick={Count(OnPiperCountdownTick)}, " +
                $"OnPiperMarchesOnColony={Count(OnPiperMarchesOnColony)}");

            Debug.Log("[EventBus] LogSubscriberCounts: Run — " +
                $"OnRunStarted={Count(OnRunStarted)}, " +
                $"OnRunComplete={Count(OnRunComplete)}, " +
                $"OnScoreCalculated={Count(OnScoreCalculated)}");

            Debug.Log("[EventBus] LogSubscriberCounts: UI — " +
                $"OnNotification={Count(OnNotification)}, " +
                $"OnTooltipShow={Count(OnTooltipShow)}, " +
                $"OnTooltipHide={Count(OnTooltipHide)}");

            Debug.Log("[EventBus] LogSubscriberCounts: Scene — " +
                $"OnReturnToMainMenu={Count(OnReturnToMainMenu)}");
        }
    }
}
