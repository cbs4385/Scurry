using System.Collections.Generic;

namespace Scurry.Combat
{
    [System.Serializable]
    public class CombatResult
    {
        public int nodeId;
        public bool heroesWon;
        public int roundsFought;
        public List<int> survivingHeroTokenIds;
        public List<int> injuredHeroTokenIds;
        public List<int> defeatedEnemyTokenIds;
        public List<int> survivingEnemyTokenIds;
        public Dictionary<int, int> damageDealtByHero;   // tokenId -> total damage dealt
        public Dictionary<int, int> damageDealtByEnemy;   // tokenId -> total damage dealt
        public List<string> tacticalCardsUsed;

        public static CombatResult Create(int nodeId)
        {
            return new CombatResult
            {
                nodeId = nodeId,
                heroesWon = false,
                roundsFought = 0,
                survivingHeroTokenIds = new List<int>(),
                injuredHeroTokenIds = new List<int>(),
                defeatedEnemyTokenIds = new List<int>(),
                survivingEnemyTokenIds = new List<int>(),
                damageDealtByHero = new Dictionary<int, int>(),
                damageDealtByEnemy = new Dictionary<int, int>(),
                tacticalCardsUsed = new List<string>()
            };
        }

        public override string ToString()
        {
            return $"CombatResult(nodeId={nodeId}, heroesWon={heroesWon}, rounds={roundsFought}, " +
                   $"survivingHeroes={survivingHeroTokenIds?.Count ?? 0}, injured={injuredHeroTokenIds?.Count ?? 0}, " +
                   $"defeatedEnemies={defeatedEnemyTokenIds?.Count ?? 0}, survivingEnemies={survivingEnemyTokenIds?.Count ?? 0}, " +
                   $"tacticalCards={tacticalCardsUsed?.Count ?? 0})";
        }
    }
}
