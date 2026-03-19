using UnityEngine;
using Scurry.Data;
using Scurry.Map;
using Scurry.Interfaces;

namespace Scurry.Core
{
    public class EnemyTokenFactory : IEnemyTokenFactory
    {
        public EnemyToken Create(int tokenId, string name, int strength, int hp, int speed, EnemyBehavior behavior, NodeType zone, int nodeId)
        {
            Debug.Log($"[EnemyTokenFactory] Create: creating enemy token (tokenId={tokenId}, name={name}, strength={strength}, hp={hp}, speed={speed}, behavior={behavior}, zone={zone}, nodeId={nodeId})");
            return new EnemyToken(tokenId, name, strength, hp, speed, behavior, zone, nodeId);
        }
    }
}
