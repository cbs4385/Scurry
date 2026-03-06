namespace Scurry.Data
{
    // --- M1 GamePhase (new values used by EncounterManager) ---
    // Old values kept for M0 compatibility until GameManager is rewritten in Phase B3
    public enum GamePhase { DeckBuild, Draw, Deploy, Gather, Resolve, Setup, AutoBattle, Recall, Resolution }

    // --- M1 CardType (Hero stays, Resource removed from decks but kept for tile data) ---
    public enum CardType { Hero, Resource, Equipment, Colony, ColonyBenefit, HeroBenefit }

    // --- M1 ResourceType (Shelter and Equipment removed as card types; kept as legacy values until ColonyManager update) ---
    public enum ResourceType { Food, Shelter, Equipment, Currency, Materials }

    public enum TileType { Normal, ResourceNode, EnemyPatrol, Hazard }

    public enum SpecialAbility { None, BonusFood, NoDamageOnWin, ExtraCarry, IgnoreFirstHazard, ShelterBoost, HealAlly, TrapDisarm, Frenzy, StealthMove, RallyAll }

    public enum CardRarity { Common, Uncommon, Rare, Legendary }

    // --- M1 NodeType (replaces StepType for branching map) ---
    public enum NodeType { ResourceEncounter, EliteEncounter, Boss, Shop, HealingShrine, UpgradeShrine, CardDraft, Event, RestSite }

    // Old StepType kept until RunManager rewrite in Phase B4
    public enum StepType { CardPlacement, Shop, Healing, CardAddRemove, BossFight }

    public enum EncounterType { Resource, Elite, Boss }

    // --- M1 RunState (new values alongside old for transition) ---
    public enum RunState { Draft, InStage, StepTransition, BossFight, RunComplete, GameOver, ColonyManagement, MapTraversal, InEncounter, InBoss, LevelComplete }

    public enum RelicEffect { None, IgnoreFirstPatrol, ShopDiscount, BonusHP, BonusMove, BonusCombat }

    public enum BossAbility { Swoop, Talons, AoEDamage, Summon, Stun }

    public enum EnemyBehavior { Patrol, Chase, Ambush, Guard }

    // --- New M1 enums ---

    public enum ColonyEffect
    {
        IncreaseDeckSize,
        ReduceConsumption,
        HeroCombatBonus,
        HeroMoveBonus,
        HeroCarryBonus,
        ReducePopulation,
        BonusStartingFood
    }

    public enum PlacementRequirement { None, AdjacentTo, Edge, Corner, Center }

    public enum EquipmentSlot { Combat, Movement, Carry, Special }

    public enum BenefitTrigger { OnCombatStart, OnHeroWounded, OnFirstEnemyDefeated, OnEncounterStart, OnRecall }
}
