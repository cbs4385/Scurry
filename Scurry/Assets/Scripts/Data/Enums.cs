namespace Scurry.Data
{
    public enum GamePhase { DeckBuild, Draw, Deploy, Gather, Resolve }
    public enum CardType { Hero, Resource }
    public enum ResourceType { Food, Shelter, Equipment, Currency }
    public enum TileType { Normal, ResourceNode, EnemyPatrol, Hazard }
    public enum SpecialAbility { None, BonusFood, NoDamageOnWin, ExtraCarry, IgnoreFirstHazard, ShelterBoost }
    public enum CardRarity { Common, Uncommon, Rare, Legendary }
    public enum StepType { CardPlacement, Shop, Healing, CardAddRemove, BossFight }
    public enum RunState { Draft, InStage, StepTransition, BossFight, RunComplete, GameOver }
    public enum RelicEffect { None, IgnoreFirstPatrol, ShopDiscount, BonusHP, BonusMove, BonusCombat }
    public enum BossAbility { Swoop, Talons, AoEDamage, Summon, Stun }
}