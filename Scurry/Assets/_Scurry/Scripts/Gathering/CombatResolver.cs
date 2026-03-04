using UnityEngine;

namespace Scurry.Gathering
{
    public static class CombatResolver
    {
        public static bool Resolve(int heroCombat, int enemyStrength)
        {
            bool won = heroCombat >= enemyStrength;
            Debug.Log($"[CombatResolver] Resolve: heroCombat={heroCombat}, enemyStrength={enemyStrength}, result={( won ? "HERO WINS" : "HERO LOSES" )}");
            return won;
        }
    }
}
