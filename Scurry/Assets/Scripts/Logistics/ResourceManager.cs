using UnityEngine;
using System.Collections.Generic;
using Scurry.Data;
using Scurry.Map;
using Scurry.Colony;
using Scurry.Core;

namespace Scurry.Logistics
{
    public class ResourceManager : MonoBehaviour
    {
        // Colony stockpile
        private Dictionary<ResourceType, int> stockpile = new Dictionary<ResourceType, int>();
        private int foodStorageCapacity = 99; // increased by Grand Stores

        public int FoodStockpile => GetStockpile(ResourceType.Food);
        public int MaterialsStockpile => GetStockpile(ResourceType.Materials);
        public int CurrencyStockpile => GetStockpile(ResourceType.Currency);
        public int FoodStorageCapacity => foodStorageCapacity;

        // ── Initialization ───────────────────────────────────────────────

        public void Initialize()
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log("[ResourceManager] Initialize: setting up resource manager");

            stockpile.Clear();
            foodStorageCapacity = 99;

            // Apply starting resources from BalanceConfigSO
            var bc = BalanceConfigSO.Instance;
            int startFood = bc != null ? bc.startingFood : 0;
            int startMaterials = bc != null ? bc.startingMaterials : 0;
            int startCurrency = bc != null ? bc.startingCurrency : 0;

            stockpile[ResourceType.Food] = startFood;
            stockpile[ResourceType.Materials] = startMaterials;
            stockpile[ResourceType.Currency] = startCurrency;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] Initialize: complete " +
                      $"(food={stockpile[ResourceType.Food]}, materials={stockpile[ResourceType.Materials]}, " +
                      $"currency={stockpile[ResourceType.Currency]}, foodCap={foodStorageCapacity}, " +
                      $"balanceConfig={bc != null})");
        }

        // ── Stockpile accessors ──────────────────────────────────────────

        public int GetStockpile(ResourceType type)
        {
            int value = stockpile.ContainsKey(type) ? stockpile[type] : 0;
            // No log — called frequently by HUD refresh
            return value;
        }

        public void AddToStockpile(ResourceType type, int amount)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] AddToStockpile: adding resources (type={type}, amount={amount})");

            if (amount <= 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[ResourceManager] AddToStockpile: non-positive amount ignored " +
                                 $"(type={type}, amount={amount})");
                return;
            }

            if (!stockpile.ContainsKey(type))
            {
                stockpile[type] = 0;
            }

            int previousValue = stockpile[type];
            stockpile[type] += amount;

            // Enforce food storage cap
            if (type == ResourceType.Food && stockpile[type] > foodStorageCapacity)
            {
                int overflow = stockpile[type] - foodStorageCapacity;
                stockpile[type] = foodStorageCapacity;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] AddToStockpile: food capped at capacity " +
                          $"(overflow={overflow}, capacity={foodStorageCapacity})");
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] AddToStockpile: complete " +
                      $"(type={type}, before={previousValue}, after={stockpile[type]}, added={amount})");
        }

        public bool ConsumeFromStockpile(ResourceType type, int amount)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] ConsumeFromStockpile: attempting consumption " +
                      $"(type={type}, amount={amount})");

            if (amount <= 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[ResourceManager] ConsumeFromStockpile: non-positive amount " +
                                 $"(type={type}, amount={amount})");
                return true;
            }

            int available = stockpile.ContainsKey(type) ? stockpile[type] : 0;
            if (available < amount)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] ConsumeFromStockpile: insufficient resources " +
                          $"(type={type}, available={available}, requested={amount})");
                return false;
            }

            int previousValue = stockpile[type];
            stockpile[type] -= amount;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] ConsumeFromStockpile: consumed successfully " +
                      $"(type={type}, before={previousValue}, after={stockpile[type]}, consumed={amount})");
            return true;
        }

        public void SetStorageCapacity(int capacity)
        {
            int previousCapacity = foodStorageCapacity;
            foodStorageCapacity = Mathf.Max(1, capacity);

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] SetStorageCapacity: updated " +
                      $"(before={previousCapacity}, after={foodStorageCapacity})");

            // Clamp existing food to new capacity
            if (stockpile.ContainsKey(ResourceType.Food) && stockpile[ResourceType.Food] > foodStorageCapacity)
            {
                int overflow = stockpile[ResourceType.Food] - foodStorageCapacity;
                stockpile[ResourceType.Food] = foodStorageCapacity;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] SetStorageCapacity: clamped food " +
                          $"(overflow={overflow}, newFood={stockpile[ResourceType.Food]})");
            }
        }

        // ── Colony production ────────────────────────────────────────────

        public void ProduceColonyResources(ColonyGraph colony)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log("[ResourceManager] ProduceColonyResources: calculating colony production");

            if (colony == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning("[ResourceManager] ProduceColonyResources: colony is null — skipping");
                return;
            }

            int foodProduction = colony.CalculateFoodProduction();

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] ProduceColonyResources: production calculated " +
                      $"(food={foodProduction})");

            if (foodProduction > 0)
            {
                AddToStockpile(ResourceType.Food, foodProduction);
            }

            // Check for mushroom farm food production
            if (colony.HasEffect(ColonyEffect.MushFoodProduction))
            {
                int mushFood = colony.GetEffectValue(ColonyEffect.MushFoodProduction);
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] ProduceColonyResources: mushroom farm bonus " +
                          $"(mushFood={mushFood})");
                if (mushFood > 0)
                {
                    AddToStockpile(ResourceType.Food, mushFood);
                }
            }

            // Update storage capacity from colony effects
            if (colony.HasEffect(ColonyEffect.FoodStorageCapacity))
            {
                int bonusCapacity = colony.GetEffectValue(ColonyEffect.FoodStorageCapacity);
                int newCapacity = 99 + bonusCapacity;
                SetStorageCapacity(newCapacity);
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] ProduceColonyResources: storage capacity updated from colony " +
                          $"(bonusCapacity={bonusCapacity}, totalCapacity={newCapacity})");
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] ProduceColonyResources: complete " +
                      $"(food={FoodStockpile}, materials={MaterialsStockpile}, currency={CurrencyStockpile})");
        }

        // ── Hero resource operations ─────────────────────────────────────

        public void DepositHeroResources(HeroToken hero)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] DepositHeroResources: depositing for hero " +
                      $"(tokenId={hero?.tokenId ?? -1}, totalCarried={hero?.TotalCarried ?? 0})");

            if (hero == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning("[ResourceManager] DepositHeroResources: hero is null — skipping");
                return;
            }

            if (hero.TotalCarried <= 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] DepositHeroResources: hero carrying nothing " +
                          $"(tokenId={hero.tokenId})");
                return;
            }

            var deposited = hero.DepositResources();

            foreach (var kvp in deposited)
            {
                if (kvp.Value > 0)
                {
                    AddToStockpile(kvp.Key, kvp.Value);

                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] DepositHeroResources: deposited to stockpile " +
                              $"(tokenId={hero.tokenId}, type={kvp.Key}, amount={kvp.Value})");

                    EventBus.OnResourceDeposited?.Invoke(hero, kvp.Key, kvp.Value);
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] DepositHeroResources: deposit complete " +
                      $"(tokenId={hero.tokenId}, food={FoodStockpile}, " +
                      $"materials={MaterialsStockpile}, currency={CurrencyStockpile})");
        }

        public void DropHeroResources(HeroToken hero, MapNode node)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] DropHeroResources: dropping resources " +
                      $"(tokenId={hero?.tokenId ?? -1}, nodeId={node?.nodeId ?? -1}, " +
                      $"totalCarried={hero?.TotalCarried ?? 0})");

            if (hero == null || node == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning("[ResourceManager] DropHeroResources: hero or node is null — skipping");
                return;
            }

            if (hero.TotalCarried <= 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] DropHeroResources: hero carrying nothing " +
                          $"(tokenId={hero.tokenId})");
                return;
            }

            var dropped = hero.DropAllResources();

            foreach (var kvp in dropped)
            {
                if (kvp.Value > 0)
                {
                    if (!node.resources.ContainsKey(kvp.Key))
                    {
                        node.resources[kvp.Key] = 0;
                    }
                    node.resources[kvp.Key] += kvp.Value;

                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] DropHeroResources: resources dropped on node " +
                              $"(tokenId={hero.tokenId}, nodeId={node.nodeId}, type={kvp.Key}, " +
                              $"amount={kvp.Value}, nodeTotal={node.resources[kvp.Key]})");

                    EventBus.OnResourceDropped?.Invoke(node.nodeId, kvp.Key, kvp.Value);
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[ResourceManager] DropHeroResources: drop complete " +
                      $"(tokenId={hero.tokenId}, nodeId={node.nodeId})");
        }
    }
}
