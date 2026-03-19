using Scurry.Data;

namespace Scurry.Interfaces
{
    /// <summary>
    /// v2.0 resource manager interface. Tracks colony stockpiles for
    /// Food, Materials, and Currency. Resources must be physically carried
    /// back to the colony by heroes before they enter the stockpile.
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// Returns the current stockpile amount for a given resource type.
        /// </summary>
        /// <param name="type">The resource type to query.</param>
        /// <returns>The current amount in the colony stockpile.</returns>
        int GetStockpile(ResourceType type);

        /// <summary>
        /// Adds resources to the colony stockpile (e.g., when a hero deposits carried resources).
        /// </summary>
        /// <param name="type">The resource type to add.</param>
        /// <param name="amount">The amount to add (must be positive).</param>
        void AddToStockpile(ResourceType type, int amount);

        /// <summary>
        /// Consumes resources from the colony stockpile (e.g., food consumption, building costs).
        /// </summary>
        /// <param name="type">The resource type to consume.</param>
        /// <param name="amount">The amount to consume (must be positive, must not exceed current stockpile).</param>
        void ConsumeFromStockpile(ResourceType type, int amount);
    }
}
