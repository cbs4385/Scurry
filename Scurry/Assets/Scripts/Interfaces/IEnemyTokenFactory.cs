using Scurry.Data;
using Scurry.Map;

namespace Scurry.Interfaces
{
    public interface IEnemyTokenFactory
    {
        EnemyToken Create(int tokenId, string name, int strength, int hp, int speed, EnemyBehavior behavior, NodeType zone, int nodeId);
    }
}
