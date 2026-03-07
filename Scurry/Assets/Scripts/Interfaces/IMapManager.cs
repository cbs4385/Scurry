using System.Collections.Generic;
using Scurry.Map;

namespace Scurry.Interfaces
{
    public interface IMapManager
    {
        List<List<MapNode>> Map { get; }
        MapNode CurrentNode { get; }
        int CurrentRow { get; }
        List<MapNode> GetAvailableNodes();
        void SelectNode(MapNode node);
    }
}
