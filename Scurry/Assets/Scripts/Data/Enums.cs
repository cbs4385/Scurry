namespace Scurry.Data
{
    public enum GamePhase { ColonyDeploy, Deploy, HeroMove, EnemyMove, Combat, Gather, Cleanup }

    public enum CardType { Hero, Colony, Equipment, Tactical }

    public enum ResourceType { Food, Materials, Currency }

    public enum CardRarity { Common, Uncommon, Rare, Legendary }

    public enum HeroRole { Recon, Ranged, Fast, Melee, Tank, Gather, Support, Leader }

    public enum SpecialAbility
    {
        None,
        // Recon
        ExtendedFogReveal,
        MapOnMove,
        ScoutReport,
        Torchlight,
        // Ranged
        RangedStrike,
        // Fast
        SwiftDelivery,
        // Melee
        Riposte,
        DualWield,
        Cleave,
        FirstStrike,
        // Tank
        Intercept,
        Fortify,
        Taunt,
        // Gather
        EfficientGather,
        BulkHaul,
        // Support
        FieldMedic,
        Tinker,
        Inspire,
        WiseCounsel,
        // Leader
        Rally,
        BattleCry
    }

    public enum EquipmentSlot { Offensive, Defensive, Utility }

    public enum ColonyTier { FoodStorage, StructureDefense, Advanced }

    public enum ColonyEffect
    {
        FoodProduction,
        ResourceProtection,
        FoodSpoilageImmunity,
        MaxHeroDeployment,
        HeroDeployNode,
        BaseProduction,
        DeployMoveBuff,
        EquippedCombatBuff,
        EquippedHPBuff,
        ColonyDefenseWall,
        FogReveal,
        PitTrapDamage,
        ColonyDefenseBonus,
        Retarget,
        HealInjured,
        FirstCombatBuff,
        AllHeroCombatBuff,
        AllHeroMoveBuff,
        DoubleProduction,
        FoodStorageCapacity,
        ImmediateHealReturn,
        DoubleColonyCard,
        ForwardDeploy,
        MessengerRetarget,
        EquippedWeaponBuff,
        EquippedUtilityDouble,
        FogRevealHeroes,
        MushFoodProduction
    }

    public enum TacticalType { CombatTactic, SupportTactic, PowerTactic }

    public enum PlacementRequirement { None, AdjacentTo, Edge, Corner, Center }

    public enum NodeType { Wilderness, Farmland, Town, Colony, PiedPiper }

    public enum RunState { DeckConstruction, InRun, RunComplete, GameOver }

    public enum EnemyBehavior { Patrol, Chase, Ambush, Guard }

    public enum DifficultyLevel { Easy, Normal, Hard }
}
