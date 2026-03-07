namespace Scurry.Interfaces
{
    public interface IColonyManager
    {
        int CurrentHP { get; }
        int MaxHP { get; }
        bool IsAlive { get; }
        int CurrencyStockpile { get; }
        int FoodStockpile { get; }
        int MaterialsStockpile { get; }
        bool SpendFood(int amount);
        bool SpendCurrency(int amount);
        bool SpendMaterials(int amount);
        void AddFood(int amount);
        void AddMaterials(int amount);
        void AddCurrency(int amount);
        void TakeDamage(int amount);
        void Heal(int amount);
        void InitializeHP();
        void RestoreState(int hp, int maxHp, int currency, int food, int materials);
    }
}
