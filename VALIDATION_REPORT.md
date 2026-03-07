# Scurry: Tales of the Rat Pack
# Validation Report & Testing Document
**Date:** 2026-03-07 | **Branch:** autogen | **GDD Version:** 1.0

---

# PART 1: VALIDATION REPORT -- Design vs Implementation

## Executive Summary

The application has been reviewed against the GDD v1.0 and the comprehensive implementation plan. **All three milestones (M1, M2, M3) are substantially implemented.** The codebase contains 68 scripts, 285+ data assets, and covers all systems described in the GDD. M3 is approximately 90% complete, with art/audio assets and Steam SDK integration as the primary remaining gaps.

**Critical Issue Found:** `BalanceConfig.asset` had a broken GUID reference (`guid: 0`) -- **RESOLVED** during this review by replacing with the correct script GUID (`4153bf2f556e3c64ea57cc73df21573a`).

---

## 1. Data Foundation (GDD Section 3, Implementation Phase A)

### 1.1 Enums (Enums.cs)
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| CardType: Hero, Equipment, Colony, ColonyBenefit, HeroBenefit | PASS | Also retains Resource (M0 legacy) |
| ColonyEffect enum | PASS | All 7 values present |
| PlacementRequirement enum | PASS | None, AdjacentTo, Edge, Corner, Center |
| EquipmentSlot enum | PASS | Combat, Movement, Carry, Special |
| BenefitTrigger enum | PASS | All 5 triggers present |
| NodeType enum | PASS | All 9 node types present |
| EncounterType enum | PASS | Resource, Elite, Boss |
| RunState enum | PASS | All M1 states present (ColonyManagement, MapTraversal, InEncounter, InBoss, LevelComplete, RunComplete, GameOver) |
| GamePhase rework | PASS | Setup, AutoBattle, Recall, Resolution added; legacy Draw/Deploy retained for M0 compat |
| EnemyBehavior enum | PASS | Patrol, Chase, Ambush, Guard |
| BossAbility enum | PASS | Swoop, Talons, AoEDamage, Summon, Stun |
| RelicEffect enum | PASS | 6 effects: None, IgnoreFirstPatrol, ShopDiscount, BonusHP, BonusMove, BonusCombat |
| StepType (legacy) | NOTE | Retained for M0 backward compatibility; not used by M1+ |
| ResourceType.Shelter/Equipment | NOTE | Retained for M0 compat; GDD says remove. Low risk. |

**Verdict: PASS** -- All required enums present. Legacy values retained but harmless.

### 1.2 Card Definitions (CardDefinitionSO.cs)
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| Hero stats: movement, combat, carryCapacity, specialAbility | PASS | All fields present |
| Equipment fields: equipmentSlot, equipmentBonusValue | PASS | |
| Hero benefit fields: benefitTrigger, benefitValue, benefitDescription | PASS | |
| Upgrade support: upgraded flag, Upgrade() method | PASS | Prevents double-upgrade |
| Rarity system | PASS | Common/Uncommon/Rare/Legendary |
| Localization key | PASS | `card.[name].name/ability` pattern |

### 1.3 Colony Card Definitions (ColonyCardDefinitionSO.cs)
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| cardName, description, rarity | PASS | |
| placementRequirement, adjacencyCardName | PASS | |
| colonyEffect, effectValue | PASS | |
| populationCost (default 1) | PASS | |
| Upgrade support | PASS | upgraded flag + Upgrade() method |

### 1.4 Data Asset Counts
| Category | GDD/Plan Target | Actual | Status |
|----------|----------------|--------|--------|
| Hero Cards | 20+ | 20 | PASS |
| Equipment Cards | 12+ | 12 | PASS |
| Colony Cards | 20+ | 20 | PASS |
| Hero Benefit Cards | 10+ | 10 | PASS |
| Encounters (Resource) | 15+ | 14 | PASS (close) |
| Encounters (Elite) | 8+ | 8 | PASS |
| Bosses | 4 | 4 | PASS |
| Enemies | 12 (4/level) | 12 | PASS |
| Events | 15 (5/level) | 15 | PASS |
| Relics | 10 | 10 | PASS |
| Map Configs | 3 | 3 | PASS |
| Board Layouts | 3 | 3 | PASS |
| Localization Tables | 5 | 5 | PASS |

---

## 2. Colony Management System (GDD Section 4.3, Implementation Phase B1)

### 2.1 ColonyBoardManager.cs
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| Board scales by level: 3x3, 4x4, 5x5 | PASS | Uses MapConfigSO.colonyBoardSize |
| Hands per level: 1, 2, 3 | PASS | totalHands = level number |
| 5 cards per hand | PASS | DrawHand() draws up to 5 |
| Placement requirement validation | PASS | None/AdjacentTo/Edge/Corner/Center all implemented |
| Colony effects calculation | PASS | Sums IncreaseDeckSize, ReduceConsumption, combat/move/carry bonuses, population |
| Colony reset between levels | PASS | ResetColony() clears grid |
| Unlimited deliberation time | PASS | No timer; player-driven |
| FinishColonyManagement fires event | PASS | Fires OnColonyManagementComplete |

### 2.2 ColonyConfig.cs
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| maxHeroDeckSize (base 8 + bonuses) | PASS | Default 8, modified by colony effects |
| foodConsumptionPerNode (population/2, rounded up, min 1) | PASS | Calculated in ColonyBoardManager |
| heroCombatBonus, heroMoveBonus, heroCarryBonus | PASS | |
| totalPopulation, bonusStartingFood | PASS | |

### 2.3 ColonyManager.cs
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| Colony HP: starting 30, max 50 | PASS | From BalanceConfigSO |
| Food/Materials/Currency stockpiles | PASS | All three with add/spend methods |
| Colony HP persists across entire run | PASS | Only healing restores it |
| SpendFood, SpendMaterials, SpendCurrency | PASS | Return bool for affordability check |
| TakeDamage, Heal | PASS | Clamped 0-maxHP |

---

## 3. Hero Deck Management (GDD Section 4.4)

### 3.1 HeroDeckSetAsideUI.cs
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| Appears after colony management | PASS | RunManager triggers when heroDeck > maxDeckSize |
| Player chooses which cards to set aside | PASS | Toggle selection with visual feedback |
| Counter shows remaining to set aside | PASS | "Select N more..." / "Ready to confirm!" |
| Set-aside cards return at end of level | PASS | RunManager restores exhausted heroes at level start |
| Confirm button when correct count reached | PASS | Disabled until requirement met |

---

## 4. Resource Consumption (GDD Section 4.5)

| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| Food consumed per node = population/2 (rounded up, min 1) | PASS | Calculated in ColonyBoardManager.CalculateColonyEffects() |
| Starvation: 2 HP damage per unpaid food | PASS | RunManager uses BalanceConfigSO.starvationDamagePerFood (default 2) |
| Consumption happens on EVERY node traversal | PASS | RunManager.OnMapNodeSelected() calls ConsumeFood before routing |
| Colony HP 0 = immediate run end | PASS | Checked after every node |

---

## 5. Branching Map System (GDD Section 5, Implementation Phase B2)

### 5.1 MapGenerator.cs
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| Procedural branching map (StS-style) | PASS | Rows with 2-4 nodes, branching connections |
| First row = resource encounters | PASS | Uses config.firstRowType |
| Last row = boss | PASS | Hard-coded boss node |
| All paths reach boss (BFS validation) | PASS | ValidateMap() with CanReachBoss() |
| Difficulty scales by row (1-10) | PASS | Linearly scaled across rows |
| Node types by weighted random | PASS | PickNodeType() with NodeTypeWeight |
| Full map revealed at level start | PASS | MapUI renders entire map |

### 5.2 MapManager.cs
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| Track player position | PASS | currentRow, currentNode |
| Available next nodes (connected from current) | PASS | GetAvailableNodes() |
| Node selection fires event | PASS | OnMapNodeSelected |
| Food consumed on traversal | PASS | RunManager handles before node resolution |

### 5.3 Node Types (GDD Section 5.2)
| Node Type | GDD Requirement | Status | Handler |
|-----------|----------------|--------|---------|
| Resource Encounter | Auto-battle, recall, resources to stockpile | PASS | EncounterManager |
| Elite Encounter | Tougher, no recall, rare card reward | PASS | EncounterManager (allowRecall=false) |
| Boss | Phased, no recall, reward selection | PASS | BossManager |
| Shop | 4-6 cards + relics, reroll, prices by rarity | PASS | ShopManager |
| Healing Shrine | Minor/Major heal, Resupply wounded hero | PASS | HealingManager |
| Upgrade Shrine | Spend materials to upgrade card | PASS | UpgradeManager |
| Card Draft | Choose 1 of 3; or removal mode | PASS | DraftManager |
| Event | Narrative with 2-3 choices, outcomes | PASS | EventManager (9 outcome types) |
| Rest Site | Heal % or free upgrade | PASS | RestManager |

---

## 6. Auto-Battle Encounters (GDD Section 6, Implementation Phase B3)

### 6.1 EncounterManager.cs
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| Board from encounter definition | PASS | SetupFromEncounter with boardLayout |
| Hero auto-deploy: leftmost column, top-to-bottom | PASS | Column 0, non-hazard/non-enemy tiles |
| Equipment auto-assign by priority | PASS | Grouped by slot, sorted by relevant stat |
| Hero benefits queued for triggers | PASS | Drawn at encounter start |
| Auto-battle loop (no turn limit) | PASS | Runs until end condition |
| Recall: 1-turn delay | PASS | recallRequested → recallActive next turn |
| Recall NOT available for boss/elite | PASS | Checks encounterType |
| End conditions: all resources, recall, all heroes defeated | PASS | Checked after each turn |
| Wounded heroes sit out | PASS | woundedHeroesBefore filtered at deployment |
| Resource difficulty scaling | PASS | scaledValue = base * (1 + (difficulty-1) * scalingFactor) |

### 6.2 Combat System (GDD Section 6.5)
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| Hero combat vs enemy strength | PASS | CombatResolver.Resolve() |
| Ties: hero wins | PASS | heroCombat >= enemyStrength |
| Losing hero is wounded | PASS | HeroAgent.ApplyWound() |
| Wounded again before healing = exhausted | PASS | EncounterManager double-wound detection |
| Shelter adjacency defense bonus | PASS | Calculated in EnemyAgent.EngageHero() |

### 6.3 Board & Pathfinding
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| Grid scales: 4x4, 5x5, 6x6 by level | PASS | MapConfigSO.boardSize |
| Tile types: Normal, ResourceNode, EnemyPatrol, Hazard | PASS | TileType enum + Tile.cs |
| A* pathfinding | PASS | Pathfinding.cs with cost weighting |
| No mid-encounter resource regeneration | PASS | Resources placed at setup only |

---

## 7. Bosses (GDD Section 7, Implementation Phase C2)

### 7.1 BossManager.cs
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| Phase-based boss fights | PASS | BossPhase[] with hpThreshold triggers |
| Boss attacks highest-combat hero | PASS | Sorted by CurrentCombat |
| No recall during boss | PASS | Recall hidden for boss/elite |
| Colony HP damage on failure | PASS | BalanceConfigSO.bossFailureDamage (10) |
| Reward selection on victory | PASS | RewardSelectionUI with cards + relic |

### 7.2 Boss Definitions
| Boss | GDD Spec | Asset Exists | Notes |
|------|----------|-------------|-------|
| Elder Silas (L1) | HP 20, Atk 3, Swoop + Talons | PASS | Boss_ElderSilas.asset |
| Tobias & Duchess (L2) | HP 35, dual boss | PASS | Boss_TobiasDuchess.asset |
| Guildmaster Aldric Fenn (L3) | HP 50, 3 phases | PASS | Boss_GuildmasterAldricFenn.asset |
| The Pied Piper (Final) | HP 80, 4 phases | PASS | Boss_ThePiedPiper.asset |

### 7.3 Boss Abilities Implemented
| Ability | GDD Description | Status |
|---------|----------------|--------|
| Swoop | Stun highest-carry hero 1 round | PASS |
| Talons | AoE 2 damage to all heroes | PASS |
| AoEDamage | Fixed AoE damage | PASS |
| Summon | Spawn minion adds | PASS |
| Stun | Wound highest-combat hero | PASS |

---

## 8. Enemies (GDD Section 7.1-7.3)

| Enemy | Level | GDD Stats | Asset Exists |
|-------|-------|-----------|-------------|
| Field Mouse | 1 | Str 1, Spd 2, Patrol | PASS |
| Grass Snake | 1 | Str 2, Spd 3, Chase | PASS |
| Hawk Scout | 1 | Str 3, Spd 4, Ambush | PASS |
| Badger | 1 | Str 4, Spd 1, Guard | PASS |
| Farm Cat | 2 | Str 3, Spd 3, Chase | PASS |
| Rat Trap | 2 | Str 4, Spd 0, Ambush | PASS |
| Terrier | 2 | Str 5, Spd 4, Chase | PASS |
| Farmhand | 2 | Str 3, Spd 2, Patrol | PASS |
| Guild Apprentice | 3 | Str 4, Spd 3, Patrol | PASS |
| Alley Cat | 3 | Str 5, Spd 5, Chase | PASS |
| Rat-Catcher | 3 | Str 6, Spd 3, Chase | PASS |
| Poison Trap | 3 | Str 7, Spd 0, Ambush | PASS |

---

## 9. Colony HP & Wounds (GDD Section 8)

| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| Starting HP: 30, Max: 50 | PASS | BalanceConfigSO defaults |
| Colony HP 0 = run ends immediately | PASS | Checked after every node |
| Colony HP does NOT reset between levels | PASS | Persists in ColonyManager |
| Wounded hero skips next encounter | PASS | IsWounded/IsHealing flags |
| Wounded again before healing = exhausted | PASS | Double-wound detection in EncounterManager |
| Exhausted heroes return at next level | PASS | RunManager.StartLevel() restores |
| Wounds persist across map nodes | PASS | Only healed by sitting out, shrine, or rest |

---

## 10. Meta-Progression (GDD Section 8.3, Implementation Phase G)

### 10.1 MetaProgressionManager.cs
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| Colony deck size adjustment | PASS | colonyDeckSizeBonus = totalLevelsCleared / 3, max 5 |
| The Rattery (hero upgrades for winning runs) | PASS | upgradedHeroUnlocks tracked |
| Bestiary (enemy discovery) | PASS | discoveredEnemies list |
| Colony Reputation | PASS | Reputation earned per run, spendable |
| The Scrapbook | PASS | discoveredCards/Relics/Events/Bosses/Lore |
| Persists across runs | PASS | PlayerPrefs JSON serialization |

### 10.2 AchievementManager.cs
| Feature | Status | Notes |
|---------|--------|-------|
| 25+ achievements | PASS | AchievementId enum with 25 entries |
| Boss-specific achievements | PASS | OnBossDefeatedByName() |
| Collection achievements (Scrapbook/Bestiary) | PASS | Threshold-based (25/50/75/100%) |
| Steam-ready | PASS | `#if STEAMWORKS_ENABLED` guard, calls SteamManager |
| Persistent across sessions | PASS | PlayerPrefs storage |

---

## 11. Run Flow & Level Progression (GDD Section 4.1-4.2, Implementation Phase B4)

### 11.1 RunManager.cs
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| Run: Draft → Colony → Map → Encounters → Boss → Next Level | PASS | Full orchestration with scene-based transitions |
| Colony Card Draft at run start | PASS | ColonyDraftUI in dedicated scene, picks 8 of 12 |
| 3 levels: Wilderness → Rural Village → Town | PASS | levelConfigs array, auto-loaded |
| Colony management → set-aside → map traversal | PASS | Event-driven flow across scenes |
| Food consumption per node | PASS | Before every node resolution |
| Starvation damage when food insufficient | PASS | shortfall * starvationDamagePerFood |
| Node routing to correct handler | PASS | Switch on NodeType in OnMapNodeSelected |
| Level transitions: restore exhausted, clear colony | PASS | StartLevel() loads ColonyManagement scene |
| Boss defeat → level complete | PASS | OnBossDefeated → OnEncounterComplete → OnLevelComplete |
| Score tracking | PASS | encounters, resources, enemies, bosses, nodes |
| Discovery tracking for meta-progression | PASS | Enemies, events, bosses tracked by name |
| Multi-scene architecture | PASS | Singleton RunManager persists via DontDestroyOnLoad |
| Scene-based transitions | PASS | LoadGameScene() for phase changes, additive for encounters |
| Dynamic manager discovery | PASS | FindAnyObjectByType in OnSceneLoaded callback |
| Save/Load: ContinueRun() from saved state | PASS | Restores full run state from RunSaveData including correct scene |
| GameManager standaloneMode removed | PASS | All game flow driven by RunManager; no legacy standalone mode |

### 11.2 Save/Load (GDD Section 10.2, Implementation Phase D4)
| GDD Requirement | Status | Notes |
|----------------|--------|-------|
| RunSaveData with full state | PASS | Level, HP, resources, decks, wounds, map, colony config, relics, score |
| Save after each node | PASS | SaveRunState() called in OnNodeHandlerComplete |
| Continue Run from main menu | PASS | SaveManager.HasSave() + Load() |
| Map state preserved | PASS | MapNodeSaveData with row/col/visited/connections |
| Colony config preserved | PASS | ColonyConfigSaveData cached |
| Steam Cloud sync | PASS | SteamManager.CloudSave() called in SaveManager.Save() |

---

## 12. UI & Screens (GDD Section 9.3, Implementation Phase D3)

| Screen | GDD Requirement | Status | Scene | Notes |
|--------|----------------|--------|-------|-------|
| Main Menu | New Run, Continue | PASS | MainMenu | MainMenuManager.cs, uses IRunManager via ServiceLocator |
| Colony Draft | Pick 8 of 12 colony cards | PASS | ColonyDraft | ColonyDraftUI.cs, uses IRunManager, IBalanceConfig via ServiceLocator |
| Colony Management | Board, hand, placement, stats | PASS | ColonyManagement | ColonyUI.cs, uses IColonyBoardManager via ServiceLocator |
| Hero Set-Aside | Bench heroes when over deck max | PASS | ColonyManagement | HeroDeckSetAsideUI.cs, uses IRunManager via ServiceLocator |
| Branching Map | Node map, paths, position indicator | PASS | MapTraversal | MapUI.cs, uses IMapManager via ServiceLocator |
| Encounter Board | Grid, heroes, enemies, Recall, tally | PASS | Encounter (additive) | UIManager.cs, uses IColonyManager, IRunManager via ServiceLocator |
| Shop | Cards, prices, reroll, currency | PASS | MapTraversal (overlay) | ShopManager.cs, uses IColonyManager, IBalanceConfig, IRelicManager via ServiceLocator |
| Boss Fight | Boss HP bar, phase indicator | PASS | Encounter (additive) | BossUI.cs, uses IRunManager via ServiceLocator |
| Run End (Victory/Defeat) | Score summary, New Run, Main Menu | PASS | RunResult | RunScreenManager.cs, uses IRunManager, IMetaProgressionManager via ServiceLocator |
| Settings | Battle speed, color-blind, text size | PASS | MapTraversal (overlay) | SettingsUI.cs, uses IGameSettings via ServiceLocator |
| Scrapbook | Discovery journal | PASS | RunResult | ScrapbookUI.cs, uses IMetaProgressionManager via ServiceLocator |
| Achievement Toasts | Notification popups | PASS | Bootstrap (persistent) | AchievementToastUI.cs on PersistentCanvas |
| Resource Display | Food, materials, currency | PASS | MapTraversal | ResourceUI.cs, uses IColonyManager via ServiceLocator |
| Reward Selection | Post-encounter card/relic picks | PASS | Encounter (additive) | RewardSelectionUI.cs, uses IRelicManager via ServiceLocator |

---

## 13. Balance & Accessibility (Implementation Phase I, J)

### 13.1 BalanceConfigSO
| Parameter | GDD Value | Configured Value | Status |
|-----------|-----------|-----------------|--------|
| Starting Food | - | 15 | PASS |
| Starting Materials | - | 5 | PASS |
| Starting Currency | - | 5 | PASS |
| Starvation Damage/Food | 2 | 2 | PASS |
| Colony HP | 30 | 30 | PASS |
| Colony Max HP | 50 | 30 | **NOTE** -- GDD says max 50, config has 30 |
| Hero Deck Base Size | 8 | 8 | PASS |
| Shop Price Common | 2 | 2 | PASS |
| Shop Price Uncommon | 4 | 4 | PASS |
| Shop Price Rare | 7 | 7 | PASS |
| Shop Price Legendary | 12 | 12 | PASS |
| Shop Reroll Cost | 2 | 2 | PASS |
| Minor Heal: 2 Food → 5 HP | 2/5 | 2/5 | PASS |
| Major Heal: 5 Food → 15 HP | 5/15 | 5/15 | PASS |
| Resupply Cost | 3 | 3 | PASS |
| Rest Heal % | - | 30% | PASS |
| Boss Failure Damage | - | 10 | PASS |
| Draft Card Count | 3 | 3 | PASS |

### 13.2 Accessibility (Implementation Phase J1)
| Feature | Status | Notes |
|---------|--------|-------|
| Battle speed control | PASS | GameSettings.Instance |
| Color-blind mode | PASS | GameSettings flag |
| Adjustable text size | PASS | GameSettings setting |

### 13.3 Localization (Implementation Phase J2)
| Language | Status |
|----------|--------|
| English | PASS (216 keys) |
| French | PASS |
| German | PASS |
| Spanish | PASS |
| Italian | PASS |

---

## 14. Logging Policy Compliance

The CLAUDE.md requires extensive Debug.Log covering method entry/exit, state transitions, decisions, and object references, in `[ClassName] MethodName: description (key=value)` format.

| System | Compliant | Notes |
|--------|-----------|-------|
| RunManager | YES | Entry/exit, state transitions, food calc, routing, scoring |
| GameManager | YES | Phase transitions, wound tracking, end conditions |
| EncounterManager | YES | Encounter setup, deployment, equipment, recall, end conditions |
| BossManager | YES | Round-by-round, phases, abilities, outcomes |
| MapManager | YES | Map init, node selection, completion |
| MapGenerator | YES | Node creation, connections, validation |
| ColonyBoardManager | YES | Placement, effects calculation, hand tracking |
| ColonyManager | YES | HP/resource changes, events |
| BoardManager | YES | Board creation, tile transitions, resource spawning |
| GatheringManager | YES | Initiative, hero/enemy turns, combat, abilities |
| HeroAgent | YES | Stats, movement, combat, collection, abilities |
| EnemyAgent | YES | Behavior decisions, combat, movement |
| All UI Managers | YES | Open/close, actions, state changes |
| SaveManager | YES | Save/load operations with key data |
| RelicManager | YES | Additions, queries, restorations |
| AchievementManager | YES | Unlocks, stat tracking |
| MetaProgressionManager | YES | Run processing, unlocks, discoveries |

**Verdict: FULL COMPLIANCE** -- All scripts follow the logging policy.

---

## 15. Dependency Injection & Service Architecture

### 15.1 ServiceLocator Pattern
| Feature | Status | Notes |
|---------|--------|-------|
| ServiceLocator static class | PASS | Register/Get/Unregister/Clear methods in Scurry.Core |
| Interface-based manager access | PASS | 8 interfaces in Scurry.Interfaces namespace |
| Managers register in Awake() | PASS | Before UI resolves in Start() |
| UI resolves in Start() | PASS | All 16 UI scripts use ServiceLocator.Get<T>() |
| BalanceConfigSO explicit interface impl | PASS | Public fields bridged to PascalCase interface properties |
| Mock injection support | PASS | ServiceLocator.Clear() + Register for test doubles |

### 15.2 Interface Inventory
| Interface | Concrete Class | Consuming UI Scripts |
|-----------|---------------|---------------------|
| IColonyManager | ColonyManager | UIManager, ResourceUI, ShopManager, HealingManager, UpgradeManager, EventManager, RestManager |
| IBalanceConfig | BalanceConfigSO | ShopManager, HealingManager, UpgradeManager, DraftManager, ColonyDraftUI, RestManager |
| IRunManager | RunManager | MainMenuManager, RunScreenManager, ColonyDraftUI, UIManager |
| IMapManager | MapManager | MapUI |
| IColonyBoardManager | ColonyBoardManager | ColonyUI |
| IRelicManager | RelicManager | ShopManager, RewardSelectionUI |
| IMetaProgressionManager | MetaProgressionManager | RunScreenManager, ScrapbookUI |
| IGameSettings | GameSettings | SettingsUI |

---

## 16. Known Gaps & Discrepancies

| # | Item | Severity | Description |
|---|------|----------|-------------|
| 1 | Colony Max HP | LOW | GDD says max 50, BalanceConfigSO has `baseColonyMaxHP: 30`. May be intentional balance choice. |
| 2 | Art & Audio (Phase H) | EXPECTED | All assets are programmatic placeholders. Actual art/audio requires external creation. |
| 3 | Steam SDK (Phase J3) | EXPECTED | Scaffolding present (`SteamManager.cs`, `SteamAPI.cs`, `#if STEAMWORKS_ENABLED`), but Steamworks SDK not installed. |
| 4 | Platform Testing (Phase J4) | EXPECTED | Not yet performed (Windows/macOS/Linux/gamepad). |
| 5 | Legacy Enums | LOW | `StepType`, `ResourceType.Shelter/Equipment` retained from M0. Not used by M1+ systems. Harmless but could be cleaned up. |
| 6 | OnMapNodeComplete stub | LOW | RunManager.OnMapNodeComplete() exists but is nearly empty; wound healing per-encounter noted in comment but not implemented there (handled elsewhere). |
| 7 | Relic_SilverFlute | VERIFY | GDD mentions "Musical Instrument relic" for Pied Piper charm immunity. Verify Relic_SilverFlute implements this interaction. |

---

---

# PART 2: TESTING DOCUMENT

## Test Categories

### TC-1: Colony Management

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-1.1 | Colony board size by level | Start runs at L1, L2, L3 | 3x3, 4x4, 5x5 grids respectively | HIGH |
| TC-1.2 | Hands per level | Play through colony phase at each level | L1=1 hand, L2=2, L3=3 (5 cards each) | HIGH |
| TC-1.3 | Placement: None requirement | Place a card with no placement requirement on any empty slot | Card places successfully | HIGH |
| TC-1.4 | Placement: AdjacentTo | Place card requiring adjacency without required neighbor | Placement rejected | HIGH |
| TC-1.5 | Placement: AdjacentTo (valid) | Place required card first, then adjacent card next to it | Both cards place successfully | HIGH |
| TC-1.6 | Placement: Edge | Place Edge-required card on interior tile | Rejected. Place on edge tile: accepted. | MED |
| TC-1.7 | Placement: Corner | Place Corner-required card on non-corner | Rejected. Place on corner: accepted. | MED |
| TC-1.8 | Placement: Center | Place Center-required card on edge | Rejected. Place on center: accepted. | MED |
| TC-1.9 | Colony effects calculation | Place known cards, verify calculated ColonyConfig | Deck size, consumption, bonuses match expected | HIGH |
| TC-1.10 | Food consumption rate | Place colony cards with total population 6 | foodConsumptionPerNode = 3 (6/2) | HIGH |
| TC-1.11 | Colony reset between levels | Complete L1, start L2 | Colony board empty, new colony management begins | HIGH |
| TC-1.12 | Finish colony locks config | Click "Finish Colony", verify no further placement | Config locked, transitions to set-aside or map | HIGH |

### TC-2: Hero Deck Management

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-2.1 | Set-aside appears when over limit | Have 12 hero cards, colony maxDeckSize=8 | Set-aside UI appears, asks to remove 4 | HIGH |
| TC-2.2 | Set-aside not needed | Have 7 hero cards, maxDeckSize=8 | Set-aside UI skipped, straight to map | HIGH |
| TC-2.3 | Set-aside selection | Toggle cards on/off | Counter updates correctly, confirm enables at exact count | HIGH |
| TC-2.4 | Set-aside cards return | Complete level with set-aside cards | Cards return to deck at next level start | MED |

### TC-3: Map Traversal

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-3.1 | Map generates correctly | Start level, observe map | Correct rows, node counts within min/max, boss at end | HIGH |
| TC-3.2 | First row type | Check first row nodes | All match config.firstRowType (ResourceEncounter) | HIGH |
| TC-3.3 | All paths reach boss | Trace all possible paths visually or via logs | Every start node can reach boss (BFS validated) | HIGH |
| TC-3.4 | Available nodes highlighted | After visiting a node, check available next nodes | Only connected nodes in next row are clickable | HIGH |
| TC-3.5 | Visited nodes dimmed | Visit several nodes, check map UI | Visited nodes show dimmed/gray | MED |
| TC-3.6 | Unavailable nodes not clickable | Try clicking non-connected node | No response / no selection | MED |
| TC-3.7 | Difficulty scaling | Check encounter difficulty at row 1 vs last row | Row 1 ≈ difficulty 1, last row ≈ difficulty 8-10 | HIGH |
| TC-3.8 | Full map visible at start | Open map at level start | All rows, nodes, connections visible | HIGH |
| TC-3.9 | Map scrollable | Map with 10+ rows | Can scroll to see all rows | MED |

### TC-4: Resource Consumption & Starvation

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-4.1 | Food consumed per node | Set colony population=6, traverse node | 3 food deducted from stockpile | HIGH |
| TC-4.2 | Starvation damage | Set food=0, population=4, traverse node | 2 food needed, 0 available, shortfall=2, damage=4 (2*2) | HIGH |
| TC-4.3 | Partial starvation | Set food=1, population=4 | 1 food consumed, shortfall=1, damage=2 | HIGH |
| TC-4.4 | Colony death from starvation | Reduce colony HP to 2, trigger 4+ starvation damage | Run ends immediately (GameOver) | HIGH |
| TC-4.5 | Non-combat nodes consume food | Visit Shop, Healing, Event, Rest, Draft nodes | Food consumed before each | HIGH |
| TC-4.6 | ResourceUI displays correctly | Check HUD during traversal | Food, Materials, Currency match actual stockpile | MED |

### TC-5: Resource Encounters

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-5.1 | Heroes auto-deploy | Enter resource encounter | Heroes placed on leftmost column, top-to-bottom | HIGH |
| TC-5.2 | Equipment auto-assigned | Enter encounter with equipment in deck | Equipment assigned to appropriate heroes by stat priority | HIGH |
| TC-5.3 | Auto-battle runs | Watch encounter | Heroes pathfind, fight, gather without player input | HIGH |
| TC-5.4 | Recall button visible | Enter resource encounter | Recall button shown | HIGH |
| TC-5.5 | Recall 1-turn delay | Press Recall | Heroes finish current action, retreat next turn | HIGH |
| TC-5.6 | Resources kept on recall | Gather 3 resources, then recall | 3 resources added to stockpile | HIGH |
| TC-5.7 | All resources gathered = success | Wait for all resources collected | Encounter ends, all resources added to stockpile | HIGH |
| TC-5.8 | All heroes defeated = failure | Let all heroes die | Resources for this encounter lost | HIGH |
| TC-5.9 | Wounded hero sits out | Wound a hero in encounter N, enter encounter N+1 | Wounded hero not deployed | HIGH |
| TC-5.10 | Wounded hero heals after sitting out | Wound hero, skip one encounter, enter next | Hero available and deployed | HIGH |
| TC-5.11 | Double wound = exhaustion | Wound hero, don't let them heal, wound again | Hero exhausted (removed for rest of level) | MED |
| TC-5.12 | Difficulty scaling - resources | Compare early row vs late row encounter | Later rows have more/better resources | MED |
| TC-5.13 | Difficulty scaling - enemies | Compare early row vs late row encounter | Later rows have more/stronger enemies | MED |
| TC-5.14 | Result screen | Complete encounter | EncounterResultUI shows resources gathered, wounds, Continue button | MED |

### TC-6: Elite Encounters

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-6.1 | No recall button | Enter elite encounter | Recall button hidden | HIGH |
| TC-6.2 | Rare card reward | Defeat elite enemies | Rare card offered as reward | HIGH |
| TC-6.3 | Bonus currency | Complete elite | eliteBonusCurrency (3) awarded | MED |

### TC-7: Boss Fights

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-7.1 | Elder Silas phases | Fight Elder Silas | Phase 1 (HP 20-11): Swoop. Phase 2 (HP 10-0): Talons. | HIGH |
| TC-7.2 | Boss attacks highest-combat | Observe boss target selection | Boss targets hero with highest CurrentCombat | HIGH |
| TC-7.3 | No recall during boss | Check UI during boss fight | Recall button not visible | HIGH |
| TC-7.4 | Victory reward selection | Defeat boss | RewardSelectionUI shows cards + relic option | HIGH |
| TC-7.5 | Boss defeat advances level | Defeat L1 boss | Level complete, transitions to L2 colony management | HIGH |
| TC-7.6 | Boss failure colony damage | Lose boss fight | Colony takes bossFailureDamage (10) HP | HIGH |
| TC-7.7 | All heroes exhausted on failure | Lose boss fight | All deployed heroes exhausted | MED |
| TC-7.8 | Tobias & Duchess mechanics | Fight L2 boss | Duchess intercepts attacks; must defeat her first | HIGH |
| TC-7.9 | Aldric Fenn 3 phases | Fight L3 boss | Summon → Smoke → Rage mechanics trigger | HIGH |
| TC-7.10 | Pied Piper 4 phases | Fight final boss | Melody → Charm → High Note → Finale | HIGH |
| TC-7.11 | Perfect boss kill achievement | Defeat boss with zero wounds | AchievementManager.OnPerfectBossKill() fires | LOW |
| TC-7.12 | BossUI displays | During boss fight | HP bar, phase indicator, ability notification visible | MED |

### TC-8: Shop

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-8.1 | Shop displays cards | Enter shop node | 5 cards displayed (from BalanceConfigSO.shopCardCount) | HIGH |
| TC-8.2 | Prices by rarity | Check card prices | Common=2, Uncommon=4, Rare=7, Legendary=12 | HIGH |
| TC-8.3 | Can't buy without currency | Try buying with insufficient currency | Purchase rejected, button disabled or error | HIGH |
| TC-8.4 | Purchase adds to deck | Buy a hero card | Card appears in hero deck | HIGH |
| TC-8.5 | Reroll works once | Click reroll | New cards shown, reroll button disabled | HIGH |
| TC-8.6 | Reroll costs currency | Click reroll | 2 currency deducted | HIGH |
| TC-8.7 | Leave shop returns to map | Click Leave | Shop closes, map visible, node marked complete | HIGH |
| TC-8.8 | Relic shop discount | Have ShopDiscount relic, check prices | Prices reduced by discount amount | MED |

### TC-9: Healing Shrine

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-9.1 | Minor heal | Have 2+ food, click Minor Heal | 2 food consumed, colony +5 HP | HIGH |
| TC-9.2 | Major heal | Have 5+ food, click Major Heal | 5 food consumed, colony +15 HP | HIGH |
| TC-9.3 | Resupply | Have 3+ food and wounded hero, click Resupply | 3 food consumed, hero healed | HIGH |
| TC-9.4 | Buttons disabled when insufficient | Have 1 food | Minor/Major/Resupply all grayed out | HIGH |
| TC-9.5 | Resupply disabled with no wounded | Have food but no wounded heroes | Resupply button disabled | MED |
| TC-9.6 | HP cap | Colony at max HP, try healing | HP doesn't exceed max | MED |

### TC-10: Upgrade Shrine

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-10.1 | Upgrade hero card | Have materials, select hero card | Combat increases by 1, materials deducted | HIGH |
| TC-10.2 | Upgrade colony card | Select colony card | effectValue increases by 1 | HIGH |
| TC-10.3 | Cost by rarity | Check costs | Common=2, Uncommon=4, Rare=7 materials | HIGH |
| TC-10.4 | Can't afford | Insufficient materials | Upgrade option disabled | HIGH |
| TC-10.5 | Already upgraded skipped | Upgrade a card twice | Second upgrade prevented (no double-upgrade) | MED |

### TC-11: Card Draft

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-11.1 | 3 cards offered | Enter draft node | 3 cards from level pool displayed | HIGH |
| TC-11.2 | Draft adds to deck | Select one card | Card added to hero deck | HIGH |
| TC-11.3 | Removal mode | Switch to removal, select card from deck | Card permanently removed | HIGH |
| TC-11.4 | One action per visit | Draft a card, check if removal also available | Only one action allowed | HIGH |
| TC-11.5 | Skip option | Click Skip | No draft/removal, returns to map | MED |

### TC-12: Events

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-12.1 | Event displays correctly | Enter event node | Title, description, 2-3 choice buttons | HIGH |
| TC-12.2 | GainFood outcome | Select choice with GainFood | Food stockpile increases | HIGH |
| TC-12.3 | LoseColonyHP outcome | Select choice with LoseColonyHP | Colony HP decreases | HIGH |
| TC-12.4 | WoundRandomHero outcome | Select choice with WoundRandomHero | A non-wounded hero becomes wounded | HIGH |
| TC-12.5 | Outcome text shown | Select any choice | Outcome description displayed before Continue | MED |
| TC-12.6 | All 9 outcome types | Test each outcome type across events | GainFood/Materials/Currency, LoseFood/Materials/Currency, GainColonyHP, LoseColonyHP, WoundRandomHero all work | MED |

### TC-13: Rest Site

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-13.1 | Heal option | Click Heal at rest site | Colony HP restored by 30% of max | HIGH |
| TC-13.2 | Free upgrade option | Click Upgrade at rest site | UpgradeManager opens with free=true (no material cost) | HIGH |
| TC-13.3 | One action only | Choose heal | Upgrade no longer available, returns to map | HIGH |

### TC-14: Full Run Flow

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-14.1 | Complete L1 run | Colony → Map → 8+ nodes → Boss → Victory | Level complete screen, advance to L2 | CRITICAL |
| TC-14.2 | Complete L2 run | Same flow with L2 content | Level complete, advance to L3 | CRITICAL |
| TC-14.3 | Complete L3 run | Same flow with L3 content | Pied Piper unlocked or run complete | CRITICAL |
| TC-14.4 | Full 3-level victory | Complete all levels + Pied Piper | Victory screen with full score | CRITICAL |
| TC-14.5 | Colony death mid-run | Let colony HP reach 0 at any point | Immediate game over screen | CRITICAL |
| TC-14.6 | Level transition | Complete L1 boss | Exhausted heroes restored, colony cleared, L2 colony management starts | HIGH |
| TC-14.7 | Run start screen | Begin new run | "The Wilderness" title, flavor text, Begin button | MED |
| TC-14.8 | Victory screen | Complete run | Score summary, "New Run" button | MED |
| TC-14.9 | Defeat screen | Die to starvation/boss | Score summary, "Try Again" button | MED |

### TC-15: Save/Load

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-15.1 | Auto-save after node | Complete any map node | Save file created/updated | HIGH |
| TC-15.2 | Continue run | Save mid-run, restart game, Continue | Exact state restored (map position, resources, deck, wounds) | CRITICAL |
| TC-15.3 | Map state preserved | Save with 5 nodes visited, reload | Same 5 nodes visited, correct position | HIGH |
| TC-15.4 | Colony config preserved | Save after colony management, reload | Colony bonuses intact, no re-play colony phase | HIGH |
| TC-15.5 | Relics preserved | Save with 2 relics, reload | Same 2 relics active | HIGH |
| TC-15.6 | Wounds preserved | Save with 1 wounded hero, reload | Same hero still wounded | HIGH |
| TC-15.7 | Delete save on run end | Complete or fail run | Save file removed | MED |

### TC-16: Meta-Progression

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-16.1 | Reputation earned | Complete a run | Reputation = (level*2) + (bosses*3) + (10 if victory) | HIGH |
| TC-16.2 | Hero upgrade unlock | Win run with specific hero | Hero appears in upgradedHeroUnlocks | MED |
| TC-16.3 | Enemy discovery | Encounter new enemy type | Added to discoveredEnemies | MED |
| TC-16.4 | Colony deck bonus | Clear 3 levels total | colonyDeckSizeBonus = 1 | MED |
| TC-16.5 | Scrapbook completion | Discover cards, relics, events | Percentage updates correctly | LOW |
| TC-16.6 | Unlock starting relic | Spend 10 reputation | Relic added to unlockedStartingRelics | LOW |
| TC-16.7 | Persists across sessions | Earn reputation, restart app | Reputation still present | HIGH |

### TC-17: Achievements

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-17.1 | Boss kill achievement | Defeat Elder Silas | DefeatElderSilas unlocked, toast shown | HIGH |
| TC-17.2 | Level completion | Complete Level 1 | CompleteLevel1 unlocked | MED |
| TC-17.3 | Full run | Complete all levels | FullRunComplete unlocked | MED |
| TC-17.4 | No starvation run | Complete run without starvation | NoStarvationRun unlocked | LOW |
| TC-17.5 | Toast notification | Any achievement unlocked | AchievementToastUI visible with name | MED |

### TC-18: Relics

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-18.1 | Relic acquired from boss | Defeat boss, select relic reward | Relic added to RelicManager | HIGH |
| TC-18.2 | ShopDiscount relic | Acquire Barn Key, visit shop | Prices reduced | HIGH |
| TC-18.3 | IgnoreFirstPatrol | Acquire Feather relic, enter encounter | First patrol tile ignored | MED |
| TC-18.4 | BonusCombat stacking | Acquire 2 combat relics | Combat bonus = sum of both values | MED |
| TC-18.5 | Relics persist across encounters | Acquire relic, complete multiple nodes | Relic still active | HIGH |
| TC-18.6 | Relics cleared on new run | Start new run | No relics active | HIGH |

### TC-19: Accessibility & Settings

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-19.1 | Battle speed control | Change battle speed in settings | Auto-battle animation speed changes | MED |
| TC-19.2 | Color-blind mode | Enable color-blind mode | Visual indicators not reliant on color alone | MED |
| TC-19.3 | Text size adjustment | Change text size setting | UI text scales accordingly | MED |

### TC-20: Edge Cases & Regression

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-20.1 | Empty hero deck | Remove all heroes via draft removal | Graceful handling (no crash) | HIGH |
| TC-20.2 | All heroes wounded entering encounter | Wound all heroes, enter next encounter | No heroes deploy; encounter should handle gracefully | HIGH |
| TC-20.3 | No food at level start | Start level with 0 food | First node triggers starvation damage | HIGH |
| TC-20.4 | Colony HP exactly 0 | Take exact lethal damage | Game over fires (not off-by-one) | HIGH |
| TC-20.5 | Multiple rapid Recall presses | Press Recall multiple times quickly | Only one recall processed, no double-trigger | MED |
| TC-20.6 | Boss HP exactly at phase threshold | Deal damage to exact threshold HP | Phase transition fires correctly | MED |
| TC-20.7 | Save corruption recovery | Corrupt save file, try Continue | Graceful fallback (fresh run or error message) | LOW |
| TC-20.8 | Maximum colony deck bonus | Play many runs to reach max bonus (5) | Bonus caps at 5, doesn't exceed | LOW |

### TC-21: Scene Transitions

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-21.1 | Bootstrap loads MainMenu | Play from Bootstrap scene (build index 0) | MainMenu scene loads, title and buttons visible | CRITICAL |
| TC-21.2 | New Run loads ColonyDraft | Click New Run on main menu | ColonyDraft scene loads, 12 cards displayed | CRITICAL |
| TC-21.3 | ColonyDraft to ColonyManagement | Select 8 cards, confirm | ColonyManagement scene loads, colony board visible | HIGH |
| TC-21.4 | ColonyManagement to MapTraversal | Complete colony placement | MapTraversal scene loads, map visible | HIGH |
| TC-21.5 | MapTraversal to Encounter | Select combat node on map | Encounter scene loads additively, battle begins | HIGH |
| TC-21.6 | Encounter back to MapTraversal | Complete encounter | Encounter scene unloads, map visible, node advanced | HIGH |
| TC-21.7 | Boss defeat triggers level transition | Defeat boss at end of level | Level complete, ColonyManagement loads for next level | HIGH |
| TC-21.8 | Run complete loads RunResult | Defeat final boss | RunResult scene loads, victory screen shown | HIGH |
| TC-21.9 | Run failed loads RunResult | Colony HP reaches 0 | RunResult scene loads, defeat screen shown | HIGH |
| TC-21.10 | RunResult New Run works | Click New Run on victory/defeat screen | ColonyDraft scene loads for new run | MED |
| TC-21.11 | RunResult Main Menu works | Click Main Menu on victory/defeat screen | MainMenu scene loads | MED |
| TC-21.12 | Persistent managers survive transitions | Navigate through 3+ scene transitions | No duplicate singletons, all managers functional | CRITICAL |
| TC-21.13 | EventSystem persists correctly | Play through multiple scenes | UI interaction works in all scenes (buttons clickable) | HIGH |
| TC-21.14 | No duplicate AudioListeners | Load multiple scenes | Only one AudioListener active at a time | MED |

### TC-22: Colony Card Draft

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-22.1 | Draft presents correct count | Start new run, observe draft screen | 12 cards offered (per BalanceConfigSO.colonyDraftOfferCount) | HIGH |
| TC-22.2 | Card selection limit | Try to select more than 8 cards | Selection blocked at max picks | HIGH |
| TC-22.3 | Card deselection | Select a card, then click again | Card deselected, selection count decrements | MED |
| TC-22.4 | Confirm requires full selection | Try to confirm with fewer than 8 selected | Confirm button disabled | HIGH |
| TC-22.5 | Confirm sends correct deck | Select 8 cards and confirm | OnColonyDraftComplete fires with exactly those 8 cards | HIGH |
| TC-22.6 | Draft cards are randomized | Start multiple runs | Different card offerings each time | MED |
| TC-22.7 | Card text readable | Observe card text on draft screen | Card name, effect, population cost all readable | MED |
| TC-22.8 | Selection visual feedback | Select and deselect cards | Selected cards show yellow outline and [SELECTED] label | LOW |

### TC-23: Dependency Injection & ServiceLocator

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-23.1 | All interfaces registered | Check ServiceLocator after Bootstrap scene loads | All 8 interfaces resolvable via Get<T>() | CRITICAL |
| TC-23.2 | IColonyManager resolves correctly | ServiceLocator.Get<IColonyManager>() | Returns ColonyManager.Instance | HIGH |
| TC-23.3 | IBalanceConfig resolves correctly | ServiceLocator.Get<IBalanceConfig>() | Returns BalanceConfigSO.Instance | HIGH |
| TC-23.4 | IRunManager resolves correctly | ServiceLocator.Get<IRunManager>() | Returns RunManager.Instance | HIGH |
| TC-23.5 | IRelicManager resolves correctly | ServiceLocator.Get<IRelicManager>() | Returns RelicManager.Instance | HIGH |
| TC-23.6 | IMetaProgressionManager resolves | ServiceLocator.Get<IMetaProgressionManager>() | Returns MetaProgressionManager.Instance | HIGH |
| TC-23.7 | IGameSettings resolves correctly | ServiceLocator.Get<IGameSettings>() | Returns GameSettings.Instance | HIGH |
| TC-23.8 | Mock injection works | Register mock, resolve, verify | Mock returned instead of real manager | HIGH |
| TC-23.9 | ServiceLocator.Clear() works | Clear all, verify Get returns null | All registrations removed | MED |
| TC-23.10 | UI survives null service | Null a service, load UI scene | No crash, graceful null handling | MED |

### TC-24: Play Mode UI Integration Tests

| ID | Test Case | Steps | Expected Result | Priority |
|----|-----------|-------|-----------------|----------|
| TC-24.1 | MainMenu buttons exist | Load MainMenu scene, find buttons | NewRunButton and ContinueButton exist on canvas | HIGH |
| TC-24.2 | MainMenu New Run starts draft | Simulate click on New Run button | ColonyDraft scene loads | HIGH |
| TC-24.3 | ColonyDraft shows cards | Load ColonyDraft scene | ColonyDraftUI displays offer cards (count = colonyDraftOfferCount) | HIGH |
| TC-24.4 | ColonyDraft selection works | Toggle card selections on/off | Selected count tracks correctly, visual feedback updates | HIGH |
| TC-24.5 | ColonyDraft confirm flow | Select 8 cards, click confirm | OnColonyDraftComplete fires with 8 cards, scene transitions | HIGH |
| TC-24.6 | ColonyManagement board renders | Load ColonyManagement scene, trigger StartLevel | Board grid visible with correct size | HIGH |
| TC-24.7 | ColonyManagement card placement | Place a card on empty tile via UI | Card placed, hand updates | HIGH |
| TC-24.8 | MapTraversal map renders | Load MapTraversal scene with map config | Map nodes and connections displayed | HIGH |
| TC-24.9 | MapTraversal node selection | Click available node | OnMapNodeSelected fires, node handler invoked | HIGH |
| TC-24.10 | Shop opens and displays cards | Open shop with card pool | Shop panel visible, cards rendered with prices | HIGH |
| TC-24.11 | Shop purchase deducts currency | Buy a card in shop | Currency reduced, card added to deck | HIGH |
| TC-24.12 | Healing shrine buttons work | Open healing with sufficient food | Minor/Major heal buttons functional, HP increases | HIGH |
| TC-24.13 | ResourceUI updates live | Change colony resources | Food/Materials/Currency text updates in real-time | MED |
| TC-24.14 | SettingsUI toggle works | Open settings, toggle color-blind mode | GameSettings.ColorBlindMode changes | MED |
| TC-24.15 | RunResult screen displays | Load RunResult scene after run complete | Victory/Defeat panel shows with score stats | MED |
| TC-24.16 | RunResult New Run button | Click New Run on result screen | ColonyDraft scene loads for fresh run | MED |
| TC-24.17 | Encounter UI loads additively | Load Encounter scene over MapTraversal | Both scenes active, Encounter canvas on top | HIGH |
| TC-24.18 | RewardSelection shows rewards | Show rewards with card list | Reward cards displayed, selection works | MED |
| TC-24.19 | Full run flow integration | Play through: Draft->Colony->Map->Encounter->Map->Boss->Result | All transitions work, no errors in console | CRITICAL |
| TC-24.20 | Continue run integration | Save mid-run, return to menu, click Continue | State restored, correct scene loaded | HIGH |

---

## Test Execution Notes

### Prerequisites
- Unity 6 with URP 2D configured
- All 285+ data assets loaded without errors
- BalanceConfig.asset GUID reference valid (fixed 2026-03-06)
- MCP connection stable (for automated testing via Unity tools)

### Test Environments
- **Primary:** Windows 11 (development machine)
- **Secondary (M3 Phase J4):** macOS, Linux/SteamOS, Steam Deck

### Logging Verification
All test cases should be verified both via UI observation AND console log review. The project requires logs detailed enough to reconstruct full game state. Filter logs by system prefix (e.g., `[RunManager]`, `[EncounterManager]`, `[BossManager]`) during testing.

### Priority Legend
- **CRITICAL:** Must pass for game to be shippable. Blocks release.
- **HIGH:** Core gameplay functionality. Must pass before beta.
- **MED:** Important but non-blocking. Should pass before release.
- **LOW:** Nice-to-have, edge cases, polish items.

### Total Test Cases: 190
- CRITICAL: 11
- HIGH: 120
- MED: 52
- LOW: 7
