using System.Collections.Generic;
using Scurry.Colony;
using Scurry.Data;

namespace Scurry.Interfaces
{
    /// <summary>
    /// v2.0 colony manager interface. Manages the freeform colony graph,
    /// resource stockpiles, and colony card effects.
    /// </summary>
    public interface IColonyManager
    {
        /// <summary>Current colony HP.</summary>
        int CurrentHP { get; }

        /// <summary>Maximum colony HP.</summary>
        int MaxHP { get; }

        /// <summary>Current food stockpile in the colony.</summary>
        int FoodStockpile { get; }

        /// <summary>Current materials stockpile in the colony.</summary>
        int MaterialsStockpile { get; }

        /// <summary>Current currency stockpile in the colony.</summary>
        int CurrencyStockpile { get; }

        /// <summary>Spend materials from the stockpile. Returns false if insufficient.</summary>
        bool SpendMaterials(int amount);

        /// <summary>Spend food from the stockpile. Returns false if insufficient.</summary>
        bool SpendFood(int amount);

        /// <summary>Spend currency from the stockpile. Returns false if insufficient.</summary>
        bool SpendCurrency(int amount);

        /// <summary>Heal the colony by the given amount (clamped to MaxHP).</summary>
        void Heal(int amount);

        /// <summary>Deal damage to the colony.</summary>
        void TakeDamage(int amount);

        /// <summary>Add food to the stockpile.</summary>
        void AddFood(int amount);

        /// <summary>Add materials to the stockpile.</summary>
        void AddMaterials(int amount);

        /// <summary>Add currency to the stockpile.</summary>
        void AddCurrency(int amount);

        /// <summary>
        /// Runs the colony production step for the current turn.
        /// Base production is 2 food/turn, modified by colony card effects.
        /// </summary>
        void ProduceResources();

        /// <summary>
        /// Plays a colony card onto the colony graph, attaching it to an existing card.
        /// </summary>
        /// <param name="card">The colony card definition to play.</param>
        /// <param name="targetNodeId">The map node ID to place the colony card on.</param>
        /// <returns>True if the card was successfully placed; false if placement was invalid.</returns>
        bool PlayColonyCard(ColonyCardDefinitionSO card, int targetNodeId);

        /// <summary>
        /// Checks whether a specific colony effect is currently active.
        /// </summary>
        /// <param name="effect">The colony effect to check.</param>
        /// <returns>True if at least one active colony card provides this effect.</returns>
        bool HasEffect(ColonyEffect effect);

        /// <summary>
        /// Returns the aggregate value for a colony effect across all active cards.
        /// </summary>
        /// <param name="effect">The colony effect to query.</param>
        /// <returns>The summed value, or 0 if the effect is not active.</returns>
        int GetEffectValue(ColonyEffect effect);

        /// <summary>
        /// Returns all currently active colony effects from placed colony cards.
        /// </summary>
        /// <returns>List of active colony effects (may contain duplicates if multiple cards provide the same effect).</returns>
        List<ColonyEffect> GetActiveEffects();
    }
}
