using UnityEngine;
using Scurry.Data;
using Scurry.Core;

namespace Scurry.Map
{
    [System.Serializable]
    public class EnemyToken
    {
        public int tokenId;
        public int enemyDefId;
        public string enemyName;
        public int strength;
        public int hp;
        public int currentHP;
        public int speed;
        public EnemyBehavior behavior;
        public NodeType homeZone;
        public int currentNodeId;
        public bool isDefeated;
        public int respawnTimer;

        /// <summary>Max HP (alias for hp field).</summary>
        public int maxHP => hp;

        /// <summary>True if currentHP > 0 and not defeated.</summary>
        public bool IsAlive => currentHP > 0 && !isDefeated;

        /// <summary>Self-reference for combat code that accesses enemy.definition.enemyName.</summary>
        public EnemyToken definition => this;

        // --- Constructor ---

        public EnemyToken(int tokenId, string name, int strength, int hp, int speed, EnemyBehavior behavior, NodeType zone, int nodeId)
        {
            this.tokenId = tokenId;
            this.enemyDefId = -1;
            this.enemyName = name;
            this.strength = strength;
            this.hp = hp;
            this.currentHP = hp;
            this.speed = speed;
            this.behavior = behavior;
            this.homeZone = zone;
            this.currentNodeId = nodeId;
            this.isDefeated = false;
            this.respawnTimer = 0;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyToken] Constructor: created token (tokenId={tokenId}, name={name}, strength={strength}, hp={hp}, speed={speed}, behavior={behavior}, zone={zone}, nodeId={nodeId})");
        }

        /// <summary>
        /// Applies damage to this enemy. Returns true if the enemy is defeated (HP reaches 0 or below).
        /// </summary>
        public bool TakeDamage(int amount)
        {
            int previousHP = currentHP;
            currentHP -= amount;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyToken] TakeDamage: took {amount} damage (tokenId={tokenId}, name={enemyName}, hpBefore={previousHP}, hpAfter={currentHP}, maxHP={hp})");

            if (currentHP <= 0)
            {
                currentHP = 0;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyToken] TakeDamage: enemy defeated — calling Defeat (tokenId={tokenId}, name={enemyName})");
                Defeat();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Marks the enemy as defeated and sets a respawn timer of 3 turns.
        /// </summary>
        public void Defeat()
        {
            isDefeated = true;
            respawnTimer = 3;
            currentHP = 0;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyToken] Defeat: enemy defeated (tokenId={tokenId}, name={enemyName}, respawnTimer={respawnTimer}, nodeId={currentNodeId})");
        }

        /// <summary>
        /// Respawns the enemy at the specified node with full HP.
        /// </summary>
        public void Respawn(int nodeId)
        {
            int previousNodeId = currentNodeId;
            isDefeated = false;
            respawnTimer = 0;
            currentHP = hp;
            currentNodeId = nodeId;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[EnemyToken] Respawn: enemy respawned (tokenId={tokenId}, name={enemyName}, previousNode={previousNodeId}, newNode={nodeId}, hp={currentHP}/{hp})");
        }

        public override string ToString()
        {
            return $"EnemyToken(id={tokenId}, name={enemyName}, node={currentNodeId}, hp={currentHP}/{hp}, str={strength}, spd={speed}, behavior={behavior}, zone={homeZone}, defeated={isDefeated})";
        }
    }
}
