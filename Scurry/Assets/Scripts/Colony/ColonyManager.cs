using UnityEngine;
using Scurry.Data;
using Scurry.Core;

namespace Scurry.Colony
{
    public class ColonyManager : MonoBehaviour
    {
        [SerializeField] private int startingHP = 30;
        [SerializeField] private int maxHP = 50;

        public int CurrentHP { get; private set; }
        public int MaxHP => maxHP;
        public bool IsAlive => CurrentHP > 0;
        public int CurrencyStockpile { get; private set; }

        private void Awake()
        {
            Debug.Log($"[ColonyManager] Awake: startingHP={startingHP}, maxHP={maxHP}");
        }

        private void OnEnable()
        {
            Debug.Log("[ColonyManager] OnEnable: subscribing to EventBus.OnColonyHPChanged and OnResourceCollected");
            EventBus.OnColonyHPChanged += HandleHPChange;
            EventBus.OnResourceCollected += HandleResourceCollected;
        }

        private void OnDisable()
        {
            Debug.Log("[ColonyManager] OnDisable: unsubscribing from EventBus events");
            EventBus.OnColonyHPChanged -= HandleHPChange;
            EventBus.OnResourceCollected -= HandleResourceCollected;
        }

        public void InitializeHP()
        {
            CurrentHP = startingHP;
            Debug.Log($"[ColonyManager] InitializeHP: HP set to {CurrentHP}/{maxHP}");
            BroadcastHP();
        }

        public void RestoreState(int hp, int currency)
        {
            CurrentHP = Mathf.Clamp(hp, 0, maxHP);
            CurrencyStockpile = currency;
            Debug.Log($"[ColonyManager] RestoreState: HP={CurrentHP}/{maxHP}, currency={CurrencyStockpile}");
            BroadcastHP();
        }

        public void TakeDamage(int amount)
        {
            int previousHP = CurrentHP;
            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            Debug.Log($"[ColonyManager] TakeDamage: amount={amount}, HP {previousHP} -> {CurrentHP}/{maxHP}");
            BroadcastHP();
            if (CurrentHP <= 0)
                Debug.Log("[ColonyManager] TakeDamage: COLONY HAS FALLEN — HP reached 0! Game Over.");
        }

        public void Heal(int amount)
        {
            int previousHP = CurrentHP;
            CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
            Debug.Log($"[ColonyManager] Heal: amount={amount}, HP {previousHP} -> {CurrentHP}/{maxHP}");
            BroadcastHP();
        }

        private void HandleHPChange(int delta, int unused)
        {
            Debug.Log($"[ColonyManager] HandleHPChange: delta={delta}");
            if (delta < 0)
                TakeDamage(-delta);
            else if (delta > 0)
                Heal(delta);
        }

        private void HandleResourceCollected(ResourceType type, int value)
        {
            Debug.Log($"[ColonyManager] HandleResourceCollected: type={type}, value={value}");
            switch (type)
            {
                case ResourceType.Food:
                    int healAmount = value * 2;
                    Debug.Log($"[ColonyManager] HandleResourceCollected: Food — healing {healAmount} HP");
                    Heal(healAmount);
                    break;
                case ResourceType.Shelter:
                    int shelterHeal = value;
                    Debug.Log($"[ColonyManager] HandleResourceCollected: Shelter — healing {shelterHeal} HP");
                    Heal(shelterHeal);
                    break;
                case ResourceType.Equipment:
                    Debug.Log($"[ColonyManager] HandleResourceCollected: Equipment — buff applied at hero level, no colony effect");
                    break;
                case ResourceType.Currency:
                    int oldCurrency = CurrencyStockpile;
                    CurrencyStockpile += value;
                    Debug.Log($"[ColonyManager] HandleResourceCollected: Currency — stockpile {oldCurrency} -> {CurrencyStockpile}");
                    break;
            }
        }

        private void BroadcastHP()
        {
            Debug.Log($"[ColonyManager] BroadcastHP: currentHP={CurrentHP}, maxHP={maxHP}, isAlive={IsAlive}");
        }
    }
}
