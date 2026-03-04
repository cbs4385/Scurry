using System;
using UnityEngine;
using Scurry.Data;

namespace Scurry.Core
{
    public static class EventBus
    {
        public static Action<GamePhase> OnPhaseChanged;
        public static Action<CardDefinitionSO> OnCardDrawn;
        public static Action<CardDefinitionSO, Vector2Int> OnCardPlaced;
        public static Action<Vector2Int> OnHeroMoved;
        public static Action<int, int, bool> OnCombatResolved; // heroCombat, enemyStrength, won
        public static Action<ResourceType, int> OnResourceCollected;
        public static Action<int, int> OnColonyHPChanged; // current, max
        public static Action OnTurnEnded;
        public static Action OnGatheringComplete;
        public static Action<string> OnTileHovered; // tooltip text
        public static Action OnTileUnhovered;

        public static void Reset()
        {
            Debug.Log("[EventBus] Reset: clearing all event subscriptions");
            OnPhaseChanged = null;
            OnCardDrawn = null;
            OnCardPlaced = null;
            OnHeroMoved = null;
            OnCombatResolved = null;
            OnResourceCollected = null;
            OnColonyHPChanged = null;
            OnTurnEnded = null;
            OnGatheringComplete = null;
            OnTileHovered = null;
            OnTileUnhovered = null;
            Debug.Log("[EventBus] Reset: complete — all events nulled");
        }

        public static void LogSubscriberCounts()
        {
            Debug.Log($"[EventBus] Subscriber counts: " +
                $"OnPhaseChanged={OnPhaseChanged?.GetInvocationList().Length ?? 0}, " +
                $"OnCardDrawn={OnCardDrawn?.GetInvocationList().Length ?? 0}, " +
                $"OnCardPlaced={OnCardPlaced?.GetInvocationList().Length ?? 0}, " +
                $"OnHeroMoved={OnHeroMoved?.GetInvocationList().Length ?? 0}, " +
                $"OnCombatResolved={OnCombatResolved?.GetInvocationList().Length ?? 0}, " +
                $"OnResourceCollected={OnResourceCollected?.GetInvocationList().Length ?? 0}, " +
                $"OnColonyHPChanged={OnColonyHPChanged?.GetInvocationList().Length ?? 0}, " +
                $"OnTurnEnded={OnTurnEnded?.GetInvocationList().Length ?? 0}, " +
                $"OnGatheringComplete={OnGatheringComplete?.GetInvocationList().Length ?? 0}, " +
                $"OnTileHovered={OnTileHovered?.GetInvocationList().Length ?? 0}, " +
                $"OnTileUnhovered={OnTileUnhovered?.GetInvocationList().Length ?? 0}");
        }
    }
}
