using System.Collections.Generic;
using Scurry.Map;
using Scurry.Data;

namespace Scurry.Interfaces
{
    public interface IMapGraph
    {
        int ColonyNodeId { get; }
        int PiedPiperNodeId { get; }
        void AddNode(MapNode node);
        MapNode GetNode(int nodeId);
        bool RemoveNode(int nodeId);
        void AddEdge(int nodeA, int nodeB);
        bool HasEdge(int nodeA, int nodeB);
        List<MapNode> GetNeighbors(int nodeId);
        List<MapNode> GetNodesInZone(NodeType zone);
        IReadOnlyCollection<MapNode> GetAllNodes();
        List<int> ShortestPath(int fromId, int toId);
        int GetDistance(int fromId, int toId);
    }
}
