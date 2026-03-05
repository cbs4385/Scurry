# Changelog

All notable changes to Scurry: Tales of the Rat Pack will be documented in this file.

## [0.5.0] - 2026-03-05

### Added
- Enemy definition system: EnemyDefinitionSO ScriptableObject with name, strength, speed, behavior, token color
- 4 enemy types for Zone 1 "The Wilds": Field Mouse (Patrol), Grass Snake (Chase), Hawk Scout (Ambush), Badger (Guard)
- EnemyBehavior enum with 4 AI patterns: Patrol (wander range 2), Chase (pursue heroes range 3), Ambush (wait then chase), Guard (hold position)
- 5 hero special abilities now functional: HealAlly (heal adjacent wounded), TrapDisarm (convert adjacent hazards), Frenzy (+2 combat, no carry), StealthMove (ignore first patrol), RallyAll (buff all allies +1 combat)
- Equipment combat bonus notification during gathering phase
- Food dual-tracking: FoodStockpile accumulates alongside existing HP heal for future Healing step
- SpendFood and SpendCurrency methods on ColonyManager for future Shop/Healing steps
- Enemy names localized in all 5 languages (en/fr/de/it/es)
- 9 new gathering notification localization keys across all 5 languages (equip, heal, disarm, rally, stealth)

### Changed
- EnemyAgent rewritten to support EnemyDefinitionSO with backward-compatible legacy Initialize
- GatheringManager supports spawning enemies from EnemyDefinitionSO with per-type token colors
- RunSaveData now tracks foodStockpile for save/load persistence

## [0.4.0] - 2026-03-05

### Added
- Card pool expanded from 10 to 30 cards (8 new heroes + 12 new resources)
- 5 new SpecialAbility enum values: HealAlly, TrapDisarm, Frenzy, StealthMove, RallyAll (placeholder — logic in future session)
- 8 new hero cards: Scavenger Rat, Tunnel Rat, Healer Rat, Sapper Rat, Forager Rat, Berserker Rat, Shadow Rat, Elder Rat
- 12 new resource cards across all 4 types: Berry Bush, Grain Stash, Cheese Wedge, Feast Scraps (Food); Leaf Canopy, Hollow Log, Stone Burrow (Shelter); Thorn Shield, Rusty Nail (Equipment); Copper Ring, Silver Thimble, Gold Button (Currency)
- Localization keys for all 20 new cards in 5 languages (en/fr/de/it/es) — 89 total entries per table
- Scrollable card list in deck building UI (ScrollRect + Mask)
- Card detail panel on hover during deck building showing name, type/rarity, stats, and ability description
- Rarity color coding in detail panel (white/green/blue/purple)

### Fixed
- Deck build UI overflow with 30 cards — converted to proper ScrollRect
- ScrollRect drag blocked by EventTrigger on card rows — replaced with lightweight IPointerEnterHandler/IPointerExitHandler
- Missing localization keys: card.guardrat.ability, card.nimblerat.ability added to all 5 languages
- Incorrect ability text: card.scoutrat.ability corrected from "Ignores first hazard tile" to "+1 food value on collection"

## [0.3.0] - 2026-03-05

### Added
- M1 data foundation: ZoneSO, BossDefinitionSO, RelicDefinitionSO ScriptableObject types
- New enums: StepType (CardPlacement, Shop, Healing, CardAddRemove, BossFight), RunState, RelicEffect, BossAbility
- Zone 1 "The Wilds" definition asset with 3-stage structure, step pool weights, and boss reference
- Boss "Elder Silas" definition asset (HP 20, Attack 3, 2 phases: Swoop at HP 20, Talons at HP 10)
- Feather Relic definition asset (IgnoreFirstPatrol effect)
- BossPhase serializable class for multi-phase boss encounters

### Fixed
- Obsolete TMP API warning: replaced enableWordWrapping with textWrappingMode in UIManager
- Unused field warning: removed cardsPerTurn from GameManager

## [0.2.0] - 2026-03-05

### Added
- Deck building phase with copy limits per rarity (Common 3, Uncommon 2, Rare 2, Legendary 1)
- Card rarity system: Common, Uncommon, Rare, Legendary assigned to all 10 cards
- Persistent wound tracking: wounded heroes skip gathering to heal, permanently exhausted on wound/death
- Run save/load system (JSON to persistent data path) tracking turn, colony HP, deck state, wounded heroes, living enemy positions
- Hero token persistence between turns with stat reset each turn
- Resource token persistence until collected by heroes or auto-collected
- Resource card recycling: player-placed resource cards return to discard pile when collected by heroes
- Three end conditions: (A) no heroes available, (B) all resources collected, (C) all enemies defeated with auto-collection of remaining resources
- Dynamic tile transitions between turns: enemy-vacated tiles revert to Normal (green), enemy-occupied tiles become Enemy Patrol (red)
- Enemy agents with chase/patrol AI and initiative-based turn order
- Shelter adjacency defense in combat (reduces effective enemy strength)
- Localization system with 5 languages (English, French, German, Italian, Spanish) and ~60 keys
- Gathering phase notifications for hero movement, combat, and enemy actions
- Undo button for card placement during Deploy phase

### Changed
- Game phase flow: DeckBuild → Draw → Deploy → Gather → Resolve (added DeckBuild)
- Colony HP starting value 30 (was 20)
- Unplayed cards go to discard pile (not draw pile) to prevent infinite draw loops
- Deploy phase continues until player ends turn (no longer limited to single hand)

### Fixed
- Resource tokens and tile data no longer cleared between turns (was falsely triggering "all resources collected")
- Enemy defeat tracking now uses living enemy positions instead of tile-based flags (fixes save/load respawn bug)
- Tile transitions correctly convert all vacated enemy tiles to Normal (including originally-EnemyPatrol tiles)

## [0.1.0] - 2026-03-04

### Added
- 4x4 tile grid board with four tile types: Normal, Resource Node, Enemy Patrol, Hazard
- 10 placeholder cards: 5 hero rats (Scout, Brawler, Pack, Nimble, Guard) and 5 resource cards (Food Scrap, Crumb Pile, Cardboard Shelter, Bottle Cap Armor, Shiny Coin)
- Card draw and hand display (5 cards per turn) with shuffle and discard cycling
- Drag-and-drop card placement: heroes on Normal/Resource Node tiles, resources on Normal tiles
- Game phase state machine: Draw, Deploy, Gather, Resolve
- Automated gathering phase: heroes pathfind toward nearest resource via A*
- Combat resolution: hero combat vs enemy strength, colony HP damage on loss
- Colony HP system (starting 20, max 50) with healing from food collection
- Game over detection when colony HP reaches 0
- HUD with phase label, colony HP bar, and End Turn button
- Tile legend panel showing tile type colors and descriptions
- Hover tooltip on tiles displaying type, stats, and current status
- Comprehensive diagnostic logging across all scripts
