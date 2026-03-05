using System.Collections.Generic;

namespace Scurry.Data
{
    [System.Serializable]
    public class RunSaveData
    {
        public int turnNumber;
        public int colonyHP;
        public int currencyStockpile;
        public int foodStockpile;
        public List<string> drawPileCardNames = new List<string>();
        public List<string> discardPileCardNames = new List<string>();
        public List<string> woundedHeroCardNames = new List<string>();
        public List<string> livingEnemyPositions = new List<string>(); // "row,col,strength" format
    }
}
