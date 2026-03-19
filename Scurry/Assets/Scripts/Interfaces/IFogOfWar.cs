using System.Collections.Generic;
using Scurry.Map;
using Scurry.Data;

namespace Scurry.Interfaces
{
    public interface IFogOfWar
    {
        void RecalculateVisibility(MapGraph graph, List<HeroFogInfo> heroes, HashSet<ColonyEffect> activeEffects);
        bool AreEnemiesVisible(MapNode node);
        bool AreResourcesVisible(MapNode node);
        HashSet<int> GetVisibleNodeIds(MapGraph graph);
        HashSet<int> GetKnownNodeIds(MapGraph graph);
    }
}
