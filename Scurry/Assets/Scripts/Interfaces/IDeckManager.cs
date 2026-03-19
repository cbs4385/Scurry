using System.Collections.Generic;
using Scurry.Colony;
using Scurry.Data;

namespace Scurry.Interfaces
{
    /// <summary>
    /// v2.0 deck manager interface. Manages the player's constructed deck
    /// across all card types: Heroes, Equipment, Tactical, and Colony cards.
    /// Deck size is 10-30 cards with multiplicity rules based on cost.
    /// </summary>
    public interface IDeckManager
    {
        /// <summary>Hero cards available for deployment this turn.</summary>
        IReadOnlyList<CardDefinitionSO> AvailableHeroes { get; }

        /// <summary>Equipment cards available for attachment this turn.</summary>
        IReadOnlyList<CardDefinitionSO> AvailableEquipment { get; }

        /// <summary>Tactical cards available for use in combat.</summary>
        IReadOnlyList<CardDefinitionSO> AvailableTactical { get; }

        /// <summary>Colony cards available for placement during the Colony phase.</summary>
        IReadOnlyList<ColonyCardDefinitionSO> AvailableColonyCards { get; }

        /// <summary>
        /// Initializes the deck with the player's constructed card selections.
        /// Called once at the start of a run after deck construction.
        /// </summary>
        /// <param name="cards">Hero, Equipment, and Tactical cards selected by the player.</param>
        /// <param name="colonyCards">Colony cards selected by the player.</param>
        void InitializeDeck(List<CardDefinitionSO> cards, List<ColonyCardDefinitionSO> colonyCards);

        /// <summary>
        /// Deploys a hero card from the available pool onto the map.
        /// Removes the card from AvailableHeroes.
        /// </summary>
        /// <param name="hero">The hero card to deploy.</param>
        void DeployHero(CardDefinitionSO hero);

        /// <summary>
        /// Attaches an equipment card to a deployed hero.
        /// Removes the card from AvailableEquipment.
        /// </summary>
        /// <param name="equipment">The equipment card to attach.</param>
        void AttachEquipment(CardDefinitionSO equipment);

        /// <summary>
        /// Plays a colony card onto the colony graph.
        /// Removes the card from AvailableColonyCards.
        /// </summary>
        /// <param name="card">The colony card to play.</param>
        void PlayColonyCard(ColonyCardDefinitionSO card);

        /// <summary>
        /// Uses a tactical card during combat. Single-use; the card is consumed.
        /// Removes the card from AvailableTactical.
        /// </summary>
        /// <param name="card">The tactical card to use.</param>
        void UseTacticalCard(CardDefinitionSO card);
    }
}
