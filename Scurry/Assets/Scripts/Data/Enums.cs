namespace Scurry.Data
{
    public enum GamePhase { DeckBuild, Draw, Deploy, Gather, Resolve }
    public enum CardType { Hero, Resource }
    public enum ResourceType { Food, Shelter, Equipment, Currency }
    public enum TileType { Normal, ResourceNode, EnemyPatrol, Hazard }
    public enum SpecialAbility { None, BonusFood, NoDamageOnWin, ExtraCarry, IgnoreFirstHazard, ShelterBoost }
    public enum CardRarity { Common, Uncommon, Rare, Legendary }
}