using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Core;

namespace Scurry.Map
{
    [System.Serializable]
    public class HeroToken
    {
        public int tokenId;
        public CardDefinitionSO cardDef;
        public int currentNodeId;
        public int targetNodeId = -1;
        public int currentHP;
        public int maxHP;
        public bool isInjured;
        public bool isDeployed;
        public int turnsUntilRecovery;

        // Equipment slots (legacy per-slot)
        public CardDefinitionSO offensiveEquipment;
        public CardDefinitionSO defensiveEquipment;
        public CardDefinitionSO utilityEquipment;

        // Equipment list used by v2.0 combat processors
        public List<CardDefinitionSO> equippedItems = new List<CardDefinitionSO>();

        // Carried resources
        public Dictionary<ResourceType, int> carriedResources = new Dictionary<ResourceType, int>();
        // Alias used by v2.0 combat code
        public Dictionary<ResourceType, int> carriedResourcesByType => carriedResources;

        // Colony bonus fields (set externally by ColonyManager each turn)
        public int colonyBonusCombat;
        public int colonyBonusMove;
        public int colonyBonusHP;

        // --- Base stats exposed for v2.0 combat code ---
        public int baseCombat => cardDef != null ? cardDef.combat : 0;
        public int baseInitiative => cardDef != null ? cardDef.initiative : 0;
        public int baseMove
        {
            get => cardDef != null ? cardDef.move + _baseMoveModifier : _baseMoveModifier;
            set => _baseMoveModifier = value - (cardDef != null ? cardDef.move : 0);
        }
        private int _baseMoveModifier;

        /// <summary>True if currentHP > 0 and not injured.</summary>
        public bool IsAlive => currentHP > 0 && !isInjured;

        // --- Computed effective stats ---

        // Override value for EffectiveCombat set by combat processor
        private int _effectiveCombatOverride = -1;

        public int EffectiveCombat
        {
            get
            {
                if (_effectiveCombatOverride >= 0) return _effectiveCombatOverride;
                int bc = cardDef != null ? cardDef.combat : 0;
                int equipBonus = 0;
                if (offensiveEquipment != null)
                {
                    equipBonus = offensiveEquipment.effectValue1;
                }
                int total = bc + equipBonus + colonyBonusCombat;
                return Mathf.Max(0, total);
            }
            set
            {
                _effectiveCombatOverride = value;
            }
        }

        public int EffectiveMove
        {
            get
            {
                int baseMove = cardDef != null ? cardDef.move : 0;
                int equipBonus = 0;
                if (utilityEquipment != null)
                {
                    string name = utilityEquipment.cardName ?? "";
                    if (name.Contains("Compass") || name.Contains("Claws") || name.Contains("Cape"))
                    {
                        equipBonus = utilityEquipment.effectValue1;
                    }
                }
                int total = baseMove + equipBonus + colonyBonusMove;
                return Mathf.Max(0, total);
            }
        }

        public int EffectiveHP
        {
            get
            {
                int baseHP = cardDef != null ? cardDef.hp : 0;
                int equipBonus = 0;
                if (defensiveEquipment != null)
                {
                    equipBonus = defensiveEquipment.effectValue1;
                }
                int total = baseHP + equipBonus + colonyBonusHP;
                return Mathf.Max(1, total);
            }
        }

        public int EffectiveCarry
        {
            get
            {
                int baseCarry = cardDef != null ? cardDef.carry : 0;
                int equipBonus = 0;
                if (utilityEquipment != null)
                {
                    string name = utilityEquipment.cardName ?? "";
                    if (name.Contains("Satchel") || name.Contains("Basket"))
                    {
                        equipBonus = utilityEquipment.effectValue1;
                    }
                }
                int total = baseCarry + equipBonus;
                return Mathf.Max(0, total);
            }
        }

        public int EffectiveInitiative
        {
            get
            {
                int baseInit = cardDef != null ? cardDef.initiative : 0;
                int equipBonus = 0;
                if (offensiveEquipment != null)
                {
                    equipBonus = offensiveEquipment.effectValue2;
                }
                int total = baseInit + equipBonus;
                return total;
            }
        }

        public int TotalCarried
        {
            get
            {
                int total = 0;
                foreach (var kvp in carriedResources)
                {
                    total += kvp.Value;
                }
                return total;
            }
        }

        // --- Constructor ---

        public HeroToken(CardDefinitionSO def, int tokenId)
        {
            this.tokenId = tokenId;
            this.cardDef = def;
            this.currentNodeId = -1;
            this.targetNodeId = -1;
            this.isInjured = false;
            this.turnsUntilRecovery = 0;
            this.colonyBonusCombat = 0;
            this.colonyBonusMove = 0;
            this.colonyBonusHP = 0;

            if (def != null)
            {
                this.maxHP = def.hp;
                this.currentHP = def.hp;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] Constructor: created token (tokenId={tokenId}, name={def.cardName}, combat={def.combat}, move={def.move}, hp={def.hp}, carry={def.carry}, initiative={def.initiative})");
            }
            else
            {
                this.maxHP = 1;
                this.currentHP = 1;
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[HeroToken] Constructor: created token with null cardDef (tokenId={tokenId})");
            }
        }

        // --- Equipment ---

        /// <summary>
        /// Equips an item to the appropriate slot. Returns true if successful, false if slot is occupied or invalid.
        /// </summary>
        public bool EquipItem(CardDefinitionSO item)
        {
            if (item == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[HeroToken] EquipItem: null item passed (tokenId={tokenId})");
                return false;
            }

            if (item.cardType != CardType.Equipment)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[HeroToken] EquipItem: card '{item.cardName}' is not equipment (type={item.cardType}, tokenId={tokenId})");
                return false;
            }

            string heroName = cardDef != null ? cardDef.cardName : "unknown";

            switch (item.equipmentSlot)
            {
                case EquipmentSlot.Offensive:
                    if (offensiveEquipment != null)
                    {
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] EquipItem: offensive slot occupied by '{offensiveEquipment.cardName}' — cannot equip '{item.cardName}' (tokenId={tokenId}, hero={heroName})");
                        return false;
                    }
                    offensiveEquipment = item;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] EquipItem: equipped '{item.cardName}' to offensive slot (tokenId={tokenId}, hero={heroName}, effectValue1={item.effectValue1}, effectValue2={item.effectValue2})");
                    return true;

                case EquipmentSlot.Defensive:
                    if (defensiveEquipment != null)
                    {
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] EquipItem: defensive slot occupied by '{defensiveEquipment.cardName}' — cannot equip '{item.cardName}' (tokenId={tokenId}, hero={heroName})");
                        return false;
                    }
                    defensiveEquipment = item;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] EquipItem: equipped '{item.cardName}' to defensive slot (tokenId={tokenId}, hero={heroName}, effectValue1={item.effectValue1}, effectValue2={item.effectValue2})");
                    return true;

                case EquipmentSlot.Utility:
                    if (utilityEquipment != null)
                    {
                        if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] EquipItem: utility slot occupied by '{utilityEquipment.cardName}' — cannot equip '{item.cardName}' (tokenId={tokenId}, hero={heroName})");
                        return false;
                    }
                    utilityEquipment = item;
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] EquipItem: equipped '{item.cardName}' to utility slot (tokenId={tokenId}, hero={heroName}, effectValue1={item.effectValue1}, effectValue2={item.effectValue2})");
                    return true;

                default:
                    if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[HeroToken] EquipItem: unhandled equipment slot '{item.equipmentSlot}' for '{item.cardName}' (tokenId={tokenId}, hero={heroName})");
                    return false;
            }
        }

        /// <summary>
        /// Removes all equipment and returns the removed items.
        /// </summary>
        public List<CardDefinitionSO> UnequipAll()
        {
            string heroName = cardDef != null ? cardDef.cardName : "unknown";
            var removed = new List<CardDefinitionSO>();

            if (offensiveEquipment != null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] UnequipAll: removing offensive '{offensiveEquipment.cardName}' (tokenId={tokenId}, hero={heroName})");
                removed.Add(offensiveEquipment);
                offensiveEquipment = null;
            }
            if (defensiveEquipment != null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] UnequipAll: removing defensive '{defensiveEquipment.cardName}' (tokenId={tokenId}, hero={heroName})");
                removed.Add(defensiveEquipment);
                defensiveEquipment = null;
            }
            if (utilityEquipment != null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] UnequipAll: removing utility '{utilityEquipment.cardName}' (tokenId={tokenId}, hero={heroName})");
                removed.Add(utilityEquipment);
                utilityEquipment = null;
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] UnequipAll: removed {removed.Count} items (tokenId={tokenId}, hero={heroName})");
            return removed;
        }

        // --- Resource management ---

        /// <summary>
        /// Gathers resources up to remaining carry capacity. Returns actual amount gathered.
        /// </summary>
        public int GatherResource(ResourceType type, int amount)
        {
            string heroName = cardDef != null ? cardDef.cardName : "unknown";
            int remainingCapacity = EffectiveCarry - TotalCarried;
            int actualGathered = Mathf.Min(amount, Mathf.Max(0, remainingCapacity));

            if (actualGathered <= 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] GatherResource: cannot gather — at capacity (tokenId={tokenId}, hero={heroName}, carry={EffectiveCarry}, totalCarried={TotalCarried}, requested={amount}, type={type})");
                return 0;
            }

            if (!carriedResources.ContainsKey(type))
            {
                carriedResources[type] = 0;
            }
            carriedResources[type] += actualGathered;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] GatherResource: gathered {actualGathered} {type} (tokenId={tokenId}, hero={heroName}, requested={amount}, totalCarried={TotalCarried}, capacity={EffectiveCarry})");
            return actualGathered;
        }

        /// <summary>
        /// Drops all carried resources. Returns what was dropped.
        /// </summary>
        public Dictionary<ResourceType, int> DropAllResources()
        {
            string heroName = cardDef != null ? cardDef.cardName : "unknown";
            var dropped = new Dictionary<ResourceType, int>(carriedResources);
            int totalDropped = TotalCarried;
            carriedResources.Clear();

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] DropAllResources: dropped {totalDropped} total resources (tokenId={tokenId}, hero={heroName}, nodeId={currentNodeId})");
            foreach (var kvp in dropped)
            {
                if (kvp.Value > 0)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] DropAllResources: dropped {kvp.Value} {kvp.Key} (tokenId={tokenId}, hero={heroName})");
                }
            }
            return dropped;
        }

        /// <summary>
        /// Deposits all carried resources at colony. Returns what was deposited and clears carried.
        /// </summary>
        public Dictionary<ResourceType, int> DepositResources()
        {
            string heroName = cardDef != null ? cardDef.cardName : "unknown";
            var deposited = new Dictionary<ResourceType, int>(carriedResources);
            int totalDeposited = TotalCarried;
            carriedResources.Clear();

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] DepositResources: deposited {totalDeposited} total resources at colony (tokenId={tokenId}, hero={heroName})");
            foreach (var kvp in deposited)
            {
                if (kvp.Value > 0)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] DepositResources: deposited {kvp.Value} {kvp.Key} (tokenId={tokenId}, hero={heroName})");
                }
            }
            return deposited;
        }

        // --- Combat / Health ---

        /// <summary>
        /// Applies damage. Returns true if the hero is injured (HP reaches 0 or below).
        /// </summary>
        public bool TakeDamage(int amount)
        {
            string heroName = cardDef != null ? cardDef.cardName : "unknown";
            int previousHP = currentHP;
            currentHP -= amount;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] TakeDamage: took {amount} damage (tokenId={tokenId}, hero={heroName}, hpBefore={previousHP}, hpAfter={currentHP}, maxHP={EffectiveHP})");

            if (currentHP <= 0)
            {
                currentHP = 0;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] TakeDamage: hero defeated — calling Injure (tokenId={tokenId}, hero={heroName})");
                Injure();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Heals the hero by the specified amount, up to effective max HP.
        /// </summary>
        public void Heal(int amount)
        {
            string heroName = cardDef != null ? cardDef.cardName : "unknown";
            int previousHP = currentHP;
            currentHP = Mathf.Min(currentHP + amount, EffectiveHP);

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] Heal: healed {currentHP - previousHP} (tokenId={tokenId}, hero={heroName}, hpBefore={previousHP}, hpAfter={currentHP}, maxHP={EffectiveHP}, requestedHeal={amount})");
        }

        /// <summary>
        /// Marks the hero as injured. Drops all resources, unequips all equipment.
        /// Sets recovery timer to 2 turns.
        /// </summary>
        public void Injure()
        {
            string heroName = cardDef != null ? cardDef.cardName : "unknown";
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] Injure: hero injured (tokenId={tokenId}, hero={heroName}, nodeId={currentNodeId})");

            isInjured = true;
            turnsUntilRecovery = 2;
            currentHP = 0;

            var droppedResources = DropAllResources();
            var removedEquipment = UnequipAll();

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[HeroToken] Injure: injury complete — droppedResourceTypes={droppedResources.Count}, removedEquipment={removedEquipment.Count}, turnsUntilRecovery={turnsUntilRecovery} (tokenId={tokenId}, hero={heroName})");
        }

        public override string ToString()
        {
            string heroName = cardDef != null ? cardDef.cardName : "null";
            return $"HeroToken(id={tokenId}, name={heroName}, node={currentNodeId}, hp={currentHP}/{EffectiveHP}, combat={EffectiveCombat}, move={EffectiveMove}, carry={TotalCarried}/{EffectiveCarry}, injured={isInjured})";
        }
    }
}
