namespace Scurry.Colony
{
    [System.Serializable]
    public class ColonyConfig
    {
        public int maxHeroDeckSize = 8;
        public int foodConsumptionPerNode = 1;
        public int heroCombatBonus;
        public int heroMoveBonus;
        public int heroCarryBonus;
        public int totalPopulation;
        public int bonusStartingFood;

        public override string ToString()
        {
            return $"ColonyConfig(deckSize={maxHeroDeckSize}, consumption={foodConsumptionPerNode}, " +
                   $"combat=+{heroCombatBonus}, move=+{heroMoveBonus}, carry=+{heroCarryBonus}, " +
                   $"pop={totalPopulation}, bonusFood={bonusStartingFood})";
        }
    }
}
