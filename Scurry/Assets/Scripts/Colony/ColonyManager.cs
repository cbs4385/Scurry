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
        public int FoodStockpile { get; private set; }
        public int MaterialsStockpile { get; private set; }

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
            var bc = BalanceConfigSO.Instance;
            if (bc != null)
            {
                startingHP = bc.baseColonyHP;
                maxHP = bc.baseColonyMaxHP;
            }
            CurrentHP = startingHP;
            FoodStockpile = bc != null ? bc.startingFood : 0;
            MaterialsStockpile = bc != null ? bc.startingMaterials : 0;
            CurrencyStockpile = bc != null ? bc.startingCurrency : 0;
            Debug.Log($"[ColonyManager] InitializeHP: HP set to {CurrentHP}/{maxHP}, food={FoodStockpile}, materials={MaterialsStockpile}, currency={CurrencyStockpile}");
            BroadcastHP();
        }

        public void RestoreState(int hp, int currency, int food = 0)
        {
            CurrentHP = Mathf.Clamp(hp, 0, maxHP);
            CurrencyStockpile = currency;
            FoodStockpile = food;
            Debug.Log($"[ColonyManager] RestoreState: HP={CurrentHP}/{maxHP}, currency={CurrencyStockpile}, food={FoodStockpile}");
            BroadcastHP();
        }

        public bool SpendFood(int amount)
        {
            if (FoodStockpile < amount)
            {
                Debug.Log($"[ColonyManager] SpendFood: insufficient food — have={FoodStockpile}, need={amount}");
                return false;
            }
            int prev = FoodStockpile;
            FoodStockpile -= amount;
            Debug.Log($"[ColonyManager] SpendFood: spent {amount} food — stockpile {prev} -> {FoodStockpile}");
            return true;
        }

        public bool SpendCurrency(int amount)
        {
            if (CurrencyStockpile < amount)
            {
                Debug.Log($"[ColonyManager] SpendCurrency: insufficient currency — have={CurrencyStockpile}, need={amount}");
                return false;
            }
            int prev = CurrencyStockpile;
            CurrencyStockpile -= amount;
            Debug.Log($"[ColonyManager] SpendCurrency: spent {amount} currency — stockpile {prev} -> {CurrencyStockpile}");
            return true;
        }

        public bool SpendMaterials(int amount)
        {
            if (MaterialsStockpile < amount)
            {
                Debug.Log($"[ColonyManager] SpendMaterials: insufficient materials — have={MaterialsStockpile}, need={amount}");
                return false;
            }
            int prev = MaterialsStockpile;
            MaterialsStockpile -= amount;
            Debug.Log($"[ColonyManager] SpendMaterials: spent {amount} materials — stockpile {prev} -> {MaterialsStockpile}");
            return true;
        }

        public void AddFood(int amount)
        {
            int prev = FoodStockpile;
            FoodStockpile += amount;
            Debug.Log($"[ColonyManager] AddFood: +{amount} food — stockpile {prev} -> {FoodStockpile}");
        }

        public void AddMaterials(int amount)
        {
            int prev = MaterialsStockpile;
            MaterialsStockpile += amount;
            Debug.Log($"[ColonyManager] AddMaterials: +{amount} materials — stockpile {prev} -> {MaterialsStockpile}");
        }

        public void AddCurrency(int amount)
        {
            int prev = CurrencyStockpile;
            CurrencyStockpile += amount;
            Debug.Log($"[ColonyManager] AddCurrency: +{amount} currency — stockpile {prev} -> {CurrencyStockpile}");
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
                    int prevFood = FoodStockpile;
                    FoodStockpile += value;
                    Debug.Log($"[ColonyManager] HandleResourceCollected: Food — FoodStockpile {prevFood} -> {FoodStockpile}");
                    break;
                case ResourceType.Materials:
                    int prevMat = MaterialsStockpile;
                    MaterialsStockpile += value;
                    Debug.Log($"[ColonyManager] HandleResourceCollected: Materials — stockpile {prevMat} -> {MaterialsStockpile}");
                    break;
                case ResourceType.Shelter:
                    int shelterHeal = value;
                    Debug.Log($"[ColonyManager] HandleResourceCollected: Shelter — healing {shelterHeal} HP (legacy)");
                    Heal(shelterHeal);
                    break;
                case ResourceType.Equipment:
                    Debug.Log($"[ColonyManager] HandleResourceCollected: Equipment — buff applied at hero level, no colony effect (legacy)");
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
