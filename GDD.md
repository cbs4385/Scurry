# SCURRY
# Tales of the Rat Pack

## Game Design Document
Version 1.0 -- Design Rework
Platform: Unity 6 -- Windows / macOS / Linux + SteamOS

---

# 1. Game Overview

## 1.1 High Concept
Scurry: Tales of the Rat Pack is a cute, cartoony roguelike auto-battler with colony management. Players lead a plucky colony of rats through increasingly dangerous territory -- from the open wilds to a bustling human village -- managing their colony, gathering resources, recruiting heroes, and outfitting their pack for survival. Every run ends in a climactic confrontation with the legendary Pied Piper.

The player's primary role is strategic: build and manage the colony, choose which encounters to tackle on a branching map, and decide when to press forward or retreat. Combat and gathering are fully automated -- the player watches their heroes execute based on the strategy they've set up.

## 1.2 Genre & Platform
- Roguelike auto-battler with colony management
- Single-player, PC (Windows / macOS / Linux + SteamOS)

## 1.3 Core Pillars
- **Auto-Battler First** -- Combat and gathering are fully automated. The player's skill is expressed through strategic decisions (colony layout, deck building, map pathing), not real-time micro-management.
- **Colony as Engine** -- The colony is the player's power base. Colony cards control hero deck size, provide passive bonuses, and shape the run. Rebuilding the colony each level creates fresh strategic puzzles.
- **Charming World** -- Adorable rat heroes, hand-drawn cartoon aesthetic, warm colour palette.
- **Strategic Depth** -- Every colony placement and map path choice matters; no two runs are alike.
- **Replayability** -- Branching maps, draft variability, and procedural encounters keep runs fresh.
- **Accessibility** -- Easy to learn in the wilderness; mastery rewards veteran players in town.

---

# 2. World & Lore

## 2.1 Setting
The world of Scurry is a high-medieval fantasy land called Hearthshire -- a patchwork of enchanted forests, rolling farmlands, and cobblestone villages. To the humans who live here it is an idyllic realm of gentle magic and honest toil. To the rats who scurry beneath it, it is a land of giants, traps, and terrible music.

## 2.2 The Colony
The player leads a colony of rats known simply as The Pack. After their burrow is flooded, the Pack must carve a new home through three increasingly dangerous regions, gathering the resources needed to survive and grow stronger. The colony is represented by its Colony Deck -- infrastructure cards that define the colony's capabilities each level.

## 2.3 The Villain -- The Pied Piper
The Pied Piper is the ultimate antagonist: a tall, gaunt figure in motley rags who plays an enchanted flute that can command any rat. He works at the behest of the village Mayor and serves as the final, climactic boss. Throughout the run, the player will hear rumours of his approach -- first as distant music, then as something more sinister. Defeating him means freedom for the colony forever.

## 2.4 World Levels
The game consists of three levels of increasing difficulty:
1. **The Wilderness** -- Forests, meadows, and streams. Low-threat enemies, abundant resources.
2. **The Rural Village** -- Farms, barns, and fields. Organised enemies, moderate resources.
3. **The Town** -- Cobblestone streets, markets, and the Rat-Catcher Guild. Dangerous, scarce resources.

---

# 3. Card Categories

Cards exist in six categories, split across two decks:

## 3.1 Colony Deck (Fixed Size)
Used during Colony Management at the start of each level.

### Colony Cards
Infrastructure cards placed on the colony board to provide in-game effects.
- Each colony card may have **placement requirements** (e.g., must be adjacent to a specific card type)
- Colony cards provide colony-wide effects: increase hero deck size, reduce resource consumption, provide passive combat bonuses, etc.
- The colony deck size is fixed and adjusted based on overall player performance after gameplay (meta-progression)
- Colony cards are shuffled once at the start of each level; discarded colony cards are not available until the next level
- Colony effects do NOT persist between levels -- the colony must be rebuilt at each level change

### Colony Benefit Cards
Bonus cards that enhance colony-wide capabilities.
- Provide passive effects when placed (e.g., "All heroes gain +1 movement", "Resource consumption reduced by 1")
- May have synergy requirements (e.g., "Only active when adjacent to a Shelter colony card")

## 3.2 Hero Deck (Variable Size)
Used during auto-battle encounters. Deck size is controlled by the colony.

### Hero Cards
Rat characters that fight and gather automatically during encounters.
- Each hero has: movement, combat, carry capacity, special ability
- Heroes are deployed automatically at the start of each encounter
- Heroes act autonomously during the auto-battle: pathfinding, fighting, gathering

### Equipment Cards
Gear that enhances heroes during encounters.
- Applied to heroes automatically based on priority rules (strongest hero gets best equipment, etc.)
- Provide combat bonuses, movement bonuses, or special effects
- Persist for the duration of an encounter

### Hero Benefit Cards
Cards that provide temporary bonuses during encounters.
- One-shot effects that trigger during the auto-battle (e.g., "Heal all heroes for 2 HP", "Next combat: double damage")
- Drawn and activated automatically as part of the auto-battle flow

## 3.3 Resource Cards
Resources exist as encounter rewards, not as deck cards. Resources are gathered during encounters and added to the colony stockpile. Resource types:
- **Food** -- Consumed by the colony each turn based on population. Also used at healing nodes.
- **Materials** -- Used for upgrades at shrine nodes and in shops.
- **Currency** -- Spent in shops for new cards, equipment, and relics.

---

# 4. Core Game Loop

## 4.1 Run Structure
A run of Scurry progresses through three levels. Within each level, the player:
1. **Manages the Colony** -- Plays colony cards to build infrastructure
2. **Traverses a Branching Map** -- Chooses encounters and events on a Slay the Spire-style node map
3. **Watches Auto-Battles** -- Heroes fight and gather automatically during encounters

The game ends when:
- **Victory**: The Pied Piper (final boss) is defeated
- **Defeat**: Colony HP reaches 0

## 4.2 Run Flow
```
RUN START
  > Level 1: Wilderness
    > Colony Management (1 hand of colony cards)
    > Branching Map Traversal (encounters, shops, events, mini-boss, boss)
  > Level 2: Rural Village
    > Colony Management (2 hands of colony cards)
    > Branching Map Traversal
  > Level 3: Town
    > Colony Management (3 hands of colony cards)
    > Branching Map Traversal
  > Final Boss: The Pied Piper
RUN END
```

## 4.3 Colony Management Phase
At the start of each level, the player enters Colony Management:

1. The colony board is presented -- a grid that scales with level:
   - Level 1 (Wilderness): 3x3 board
   - Level 2 (Rural Village): 4x4 board
   - Level 3 (Town): 5x5 board
2. The colony deck is shuffled (once per level)
3. The player draws a number of hands equal to the current level number, with **5 cards per hand**:
   - Level 1: 1 hand (5 cards for 9 slots -- fill just over half the board)
   - Level 2: 2 hands (10 cards for 16 slots -- fill ~63% of the board)
   - Level 3: 3 hands (15 cards for 25 slots -- fill 60% of the board)
4. For each hand, the player draws colony cards and places them on the colony board
5. Colony cards may have placement requirements:
   - Adjacency requirements (must be next to a specific card type)
   - Position requirements (must be on an edge, corner, or center)
6. The player has **unlimited time** to deliberate on colony card placement -- this is the primary strategic decision point and should not be rushed
7. Once all hands are played, the colony configuration is locked for the level
8. Colony effects are calculated:
   - Maximum hero deck size (base 8 + colony bonuses)
   - Resource consumption rate (based on colony population)
   - Passive bonuses for heroes
   - Any special level-wide effects

**Colony Population**: Each placed colony card has a population cost. The total population is the sum of all placed colony cards' population costs. Population determines food consumption rate (see 4.5).

**Colony Deck Size**: The colony deck has a fixed size that is adjusted based on overall player performance after gameplay (meta-progression). Discarded colony cards are not available again until the next level.

**Colony Reset**: Colony effects do NOT persist between levels. The colony must be rebuilt at each level change, creating fresh strategic decisions.

## 4.4 Hero Deck Management
The hero deck has a **base maximum size of 8 cards**. Colony cards can increase this limit.

- The colony determines the maximum hero deck size the colony can support (base 8 + colony bonuses, typically reaching 10-12)
- If the player has more cards in the hero deck than the colony-supported maximum, the **player chooses** which cards to set aside until the deck size is at the maximum
- Set-aside cards are not lost permanently -- they return to the player's card pool at the end of the level
- Colony cards that increase deck capacity are therefore highly valuable
- The set-aside selection screen appears after colony management is complete, before map traversal begins

## 4.5 Resource Consumption
Each map node traversal, the colony consumes food:
- **Food consumption per node** = colony population / 2 (rounded up, minimum 1). A colony with population 6 consumes 3 food per node.
- Colony population is the sum of all placed colony cards' population costs (hero count does NOT factor in)
- If the food stockpile cannot cover the consumption cost, colony HP takes **2 damage per unpaid food**
- Resources can be **stockpiled** so that players can afford to visit shops, upgrade shrines, and other non-combat nodes
- Strategic resource management is key: push into dangerous encounters for more resources, or play it safe and risk starvation
- More powerful colony cards have higher population costs, creating a direct tradeoff: powerful colony effects vs. higher resource drain

---

# 5. Branching Map (Slay the Spire Style)

## 5.1 Map Structure
Each level features a procedurally generated branching map, similar to Slay the Spire:
- The map consists of interconnected nodes arranged in rows
- Each row offers 2-4 node choices
- Paths branch and converge, offering the player meaningful route decisions
- The **full map is revealed** at the start of each level -- route planning IS the strategy
- The level boss is always the final node
- **Encounter difficulty scales by map row**: earlier rows (near start) have easier encounters, later rows (near boss) have harder ones

Reference implementation: https://github.com/silverua/slay-the-spire-map-in-unity

## 5.2 Node Types

### Resource Encounter (Common)
A standard auto-battle encounter focused on gathering resources.
- Board is populated with resource nodes and enemies
- Heroes deploy and gather automatically
- The encounter continues until:
  - All resources have been gathered (success)
  - The player presses the **Recall** button to abandon the fight with resources gathered so far
  - All heroes are defeated (failure -- gathered resources are lost)
- Resources gathered are added to the colony stockpile

### Elite / Mini-Boss Encounter (Uncommon)
A tougher encounter with a powerful enemy and better rewards.
- Stronger enemies, more complex board layouts
- Continues until either player or mini-boss is defeated (no recall option)
- Rewards: rare cards, equipment, or relics

### Boss Encounter (Once per Level)
The final node of each level's map.
- A powerful boss with phased abilities
- Continues until either player or boss is defeated
- Rewards: choice of rare/legendary cards + relic
- Must be defeated to advance to the next level

### Shop (Uncommon)
Purchase new cards, equipment, and relics using Currency resources.
- 4-6 cards and 1-2 relics available
- Prices scale with level difficulty
- One reroll per visit (small Currency cost)

### Healing Shrine (Uncommon)
Spend Food to restore Colony HP or heal wounded heroes.
- Minor Heal: 2 Food -> 5 Colony HP
- Major Heal: 5 Food -> 15 Colony HP
- Resupply: 3 Food -> fully heal one wounded hero

### Upgrade Shrine (Rare)
Spend Materials to permanently upgrade a card.
- Upgrade one hero card (improve stats) or one colony card (improve effect)
- Cost scales with card rarity

### Card Draft / Removal (Uncommon)
- **Draft**: Choose 1 of 3 offered cards to add to hero or colony deck
- **Removal**: Remove 1 card permanently from any deck

### Event (Uncommon)
A narrative encounter with choices and consequences.
- Multiple choice outcomes (risk/reward)
- May grant cards, resources, relics, or inflict penalties

### Rest Site (Uncommon)
Choose one:
- Heal Colony HP by a percentage
- Upgrade one card (as Upgrade Shrine, but free)

---

# 6. Auto-Battle Encounters

## 6.1 Overview
Encounters are the core gameplay loop of Scurry. They play out automatically on a tile grid. The player's only interaction during an encounter is the **Recall** button (resource encounters only).

## 6.2 The Board
The board is a grid of tiles:
- Grid size scales with level: 4x4 (Wilderness) -> 5x5 (Rural Village) -> 6x6 (Town)
- Tile types: Normal, Resource Node, Enemy Patrol, Hazard
- Resource Nodes generate resources at encounter setup only -- **no mid-encounter regeneration**
- Enemy Patrol tiles indicate starting enemy positions

## 6.3 Encounter Flow
1. **Setup**: Board is populated with resource nodes, enemies, and hazards based on encounter definition
2. **Hero Deployment**: Heroes from the hero deck are automatically deployed on the **leftmost column** of valid (non-enemy, non-hazard) tiles, top to bottom in deck order. Wounded heroes are skipped (they are sitting out this encounter to heal).
3. **Equipment Application**: Equipment cards are automatically assigned to heroes by priority: highest-combat hero receives combat equipment, highest-carry hero receives carry equipment, highest-movement hero receives movement equipment. Assignments are displayed before the auto-battle begins.
4. **Hero Benefits**: Hero benefit cards are drawn from the hero deck at encounter start and queued for automatic activation. Each has a trigger condition:
   - "When a hero is wounded" -> heal that hero for X
   - "When combat begins" -> all heroes gain +1 combat this fight
   - "First enemy defeated" -> gain bonus resources
   - Cards fire automatically when their condition is met. If the condition never triggers, the card is discarded unused.
5. **Auto-Battle Loop**: Repeats until an end condition is met. **No turn limit** -- encounters end only via end conditions:
   a. Heroes act in initiative order (fastest first)
   b. Each hero pathfinds toward nearest resource/enemy, moves, and engages
   c. Enemies act: patrol, chase, or engage heroes
   d. Combat resolves automatically (hero combat vs enemy strength)
   e. Collected resources are tallied
   f. Hero benefit cards trigger when conditions are met
6. **Resolution**: End condition reached, rewards distributed

## 6.4 End Conditions

### Resource Encounters
- **All resources gathered** -- Success. All gathered resources added to stockpile.
- **Recall** -- Player presses Recall button. Heroes retreat with whatever resources they've gathered so far. Partial success.
- **All heroes defeated** -- Failure. All gathered resources for this encounter are lost.

### Boss and Mini-Boss Encounters
- **Boss/Mini-Boss defeated** -- Victory. Rewards granted.
- **All heroes defeated** -- Colony takes damage equal to remaining boss HP (or a fixed amount). No recall option.

## 6.5 Combat System
- Heroes and enemies have combat values
- When a hero enters an enemy tile, combat resolves automatically
- Hero combat vs enemy strength: higher value wins
- Ties: hero wins (home advantage)
- Losing hero is **wounded** (skips next encounter to heal)
- If wounded again before healing: hero is **exhausted** (removed from deck for rest of level)
- Shelter adjacency provides defensive bonus to heroes

## 6.6 The Recall Button
During resource encounters, the player can press Recall at any time:
- When Recall is pressed, heroes **finish their current action**, then retreat on the next turn (1-turn delay)
- This prevents exploit-style "grab one resource and instantly recall" patterns while keeping it responsive
- Resources gathered up to the moment of recall are kept
- This is the primary moment-to-moment player decision during encounters
- Strategic use: retreat when heroes are taking too many wounds, or when enough resources have been gathered to cover colony consumption
- Recall is NOT available during boss or mini-boss encounters

---

# 7. Zones, Enemies & Bosses

## 7.1 Level 1 -- The Wilderness
The wild countryside surrounding the Pack's flooded burrow. Ancient trees, tall grass, babbling streams, and the constant threat from above and below.

### Enemies
| Name | Strength | Speed | Behavior |
|------|----------|-------|----------|
| Field Mouse | 1 | 2 | Patrol |
| Grass Snake | 2 | 3 | Chase |
| Hawk Scout | 3 | 4 | Ambush |
| Badger | 4 | 1 | Guard |

### Level 1 Boss -- Elder Silas, the Great Horned Owl
A massive, ancient owl who has hunted the meadows for decades.
- HP: 20 | Attack: 3 | Phases: 2
- Phase 1 (HP 20-11): Swoops -- targets the highest-carry hero, stunning them for 1 round.
- Phase 2 (HP 10-0): Talons -- attacks all heroes simultaneously for 2 damage each.
- Reward: 1 Rare Hero or Rare Equipment card + Feather Relic (heroes ignore the first Patrol tile each encounter).

## 7.2 Level 2 -- The Rural Village
A sprawling fantasy farmstead with a stone barn, grain silos, vegetable gardens, and a farmhouse.

### Enemies
| Name | Strength | Speed | Behavior |
|------|----------|-------|----------|
| Farm Cat | 3 | 3 | Chase |
| Rat Trap | 4 | 0 | Ambush |
| Terrier | 5 | 4 | Chase |
| Farmhand | 3 | 2 | Patrol |

### Level 2 Boss -- Head Farmer Tobias & Duchess
Tobias is a cunning old farmer. Duchess is his prize-winning mouser.
- HP: 35 (Tobias 20 / Duchess 15) | Attack: varies
- Must defeat Duchess first -- while alive, Duchess intercepts all attacks aimed at Tobias.
- Tobias: pitchfork sweep attacks all heroes in a row for 2 damage.
- Duchess: pounces on the hero with the lowest combat for 4 damage.
- Reward: 1 Legendary card or 2 Rare cards + Barn Key Relic (shop prices reduced by 2 Currency).

## 7.3 Level 3 -- The Town
A bustling medieval fantasy town with cobblestone streets, market stalls, taverns, and the Rat-Catcher Guild HQ.

### Enemies
| Name | Strength | Speed | Behavior |
|------|----------|-------|----------|
| Guild Apprentice | 4 | 3 | Patrol |
| Alley Cat | 5 | 5 | Chase |
| Rat-Catcher | 6 | 3 | Chase |
| Poison Trap | 7 | 0 | Ambush |

### Level 3 Boss -- Guildmaster Aldric Fenn
Aldric Fenn built the Rat-Catcher Guild from nothing.
- HP: 50 | Attack: 5 | Phases: 3
- Phase 1 (HP 50-35): Deploys 2 Guild Apprentices each round as summons.
- Phase 2 (HP 34-15): Sprays smoke -- all heroes lose 1 Move for 2 rounds.
- Phase 3 (HP 14-0): Rage -- attack increases to 8; attacks twice per round.
- Reward: Unlocks the Final Boss + guaranteed Legendary card.

## 7.4 The Final Boss -- The Pied Piper
The village square at midnight. The Piper stands on the fountain plinth, his flute gleaming.
- HP: 80 | Attack: varies | Phases: 4
- Phase 1 (HP 80-61): The Melody -- all heroes lose 1 Combat. Passive aura.
- Phase 2 (HP 60-41): Charmed Summons -- converts 1 friendly hero per round to fight against the player.
- Phase 3 (HP 40-21): The High Note -- deals 10 damage to all heroes simultaneously. One-time.
- Phase 4 (HP 20-0): Discordant Finale -- attack becomes 10. Heroes with Musical Instrument relic are immune to charm.
- Victory: The Piper's flute shatters. The Pack is free. Run complete.

---

# 8. Colony Health & Meta-Progression

## 8.1 Colony HP
Colony HP represents the overall vitality of the Pack. It persists across the entire run.
- Starting HP: 30
- Maximum HP: 50 (can be increased via certain relics or Legendary cards)
- Colony HP reaches 0 -> the run ends immediately
- Colony HP does NOT reset between levels -- only healing nodes restore it
- Food starvation (insufficient food for colony consumption) deals damage to Colony HP

## 8.2 Hero Wound System
Individual hero cards can become Wounded during encounters:
- A Wounded hero skips the next encounter to heal (they are excluded from auto-deployment)
- After sitting out one encounter, the hero is fully healed and available again
- If a hero is wounded a second time before healing, they are **exhausted** -- removed from the hero deck for the rest of the level
- Exhausted heroes return at the start of the next level
- **Wounds persist across map nodes** -- they do NOT auto-heal between encounters. Only sitting out an encounter, visiting a healing shrine, or resting at a rest site heals wounds.
- This makes healing/rest node choices on the map strategically important

## 8.3 Meta-Progression (Runs Across Sessions)

### Colony Deck Adjustment
- Colony deck size is adjusted based on overall player performance
- Strong performance: deck grows slightly, offering more colony placement options
- Weak performance: deck may shrink or change composition

### The Rattery (Unlockable Cards)
- Winning a run with a particular hero card unlocks a new, more powerful version for future draft pools
- Discovering a new enemy type adds it to the Bestiary and slightly expands the shop pool

### Colony Reputation
- At the end of each run (win or lose), the player earns Reputation based on levels completed, resources gathered, and bosses defeated
- Reputation is a persistent currency for unlocking new starting relics and colony cards

### The Scrapbook
- A flavour-text journal that fills in automatically as the player discovers new cards, enemies, and lore events
- Purely cosmetic -- a satisfying collection goal

---

# 9. User Interface & Visual Direction

## 9.1 Visual Style
Warm, hand-drawn cartoon aesthetic inspired by illustrated children's books. Soft pencil outlines, watercolour-adjacent textures, and exaggerated character proportions.
- Colour palette: warm browns, forest greens, harvest golds, and soft creams -- with pops of red for danger.
- Characters: anthropomorphic rats with distinct silhouettes and personality-driven idle animations.
- Environments: lush, layered parallax backgrounds that breathe life into each zone.

## 9.2 Card Visual Design
- Cards resemble aged parchment with hand-inked borders appropriate to their type.
- Hero cards: portrait of the rat character with stat icons below.
- Colony cards: illustration of the infrastructure (burrow entrance, food store, training ground).
- Equipment cards: vignette illustration of the item.
- Rarity communicated via border colour and gem/star icon.

## 9.3 Key Screens
1. **Main Menu** -- New Run, Continue Run, Collection (Scrapbook/Bestiary), Settings
2. **Colony Management** -- Colony board grid, hand of colony cards, placement UI, colony stats summary
3. **Branching Map** -- Slay the Spire-style node map with paths, node type icons, current position indicator
4. **Encounter Board** -- Tile grid with heroes, enemies, resources; Recall button; resource tally; hero status
5. **Shop** -- Card/relic display with prices, currency counter, reroll button
6. **Boss Fight** -- Same as encounter board but with boss HP bar and phase indicator
7. **Run End** -- Victory/defeat screen with score summary, rewards, meta-progression updates

---

# 10. Technical Specifications

## 10.1 Unity 6 Architecture
- Render Pipeline: Universal Render Pipeline (URP) 2D
- UI System: Programmatic Unity UI (Canvas) for HUD, panels, and menus. World-space elements for boards.
- Save System: JSON serialisation to Application.persistentDataPath
- Card Data: ScriptableObject-based card definitions (CardDefinitionSO)
- Board Logic: Grid-based 2D Tile[,] array; A* pathfinding for hero movement
- Event System: Static EventBus with Action delegates
- Localisation: ScriptableObject-based localisation tables (5 languages)
- Map Generation: Procedural branching map based on Slay the Spire reference implementation

## 10.2 Data Architecture -- Key Systems
- **CardDefinitionSO** -- ScriptableObject for all card data (hero, equipment, colony, colony benefit, hero benefit)
- **EncounterDefinitionSO** -- ScriptableObject defining an encounter (board layout, enemies, resources, difficulty)
- **MapConfigSO** -- ScriptableObject defining map generation parameters per level (node counts, node type weights, path branching)
- **ColonyCardDefinitionSO** -- ScriptableObject for colony-specific cards (placement requirements, effects, population cost)
- **BoardLayoutSO** -- ScriptableObject defining grid dimensions, tile types, resource node config
- **LocalizationTableSO** -- ScriptableObject holding key-value string pairs per language
- **RunSaveData** -- JSON-serializable: level progress, map state, colony layout, hero deck, resource stockpiles, colony HP
- **DeckManager** -- Runtime deck management for both hero and colony decks
- **EventBus** -- Static event hub for decoupled communication
- **MapManager** -- Branching map generation, traversal, and node resolution

## 10.3 Core Namespaces
- `Scurry.Core` -- GameManager, RunManager, EventBus, SaveManager
- `Scurry.Data` -- ScriptableObjects, enums, save data
- `Scurry.Board` -- BoardManager, Tile, Pathfinding
- `Scurry.Cards` -- DeckManager, HandManager, CardView
- `Scurry.Colony` -- ColonyManager, ColonyBoardManager
- `Scurry.Encounter` -- EncounterManager, GatheringManager, CombatResolver
- `Scurry.Map` -- MapManager, MapNode, MapGenerator
- `Scurry.UI` -- UIManager, ShopManager, MapUI, ColonyUI

---

# 11. Roadmap & Milestones

## M0 -- Core Prototype (Completed, v0.5.0)
- 4x4 board with 4 tile types and dynamic tile transitions
- Placeholder cards with rarity system
- Deploy -> Gather loop with hero/resource token persistence
- A* pathfinding, initiative-based auto-battle, shelter adjacency defense
- Enemy agents with chase/patrol AI
- Colony HP system
- Run save/load (JSON)
- Localisation (5 languages)

## M1 -- Design Rework: Auto-Battler Core (Current)
Rework the game to emphasize auto-battler gameplay. Remove manual card placement per turn. Implement colony management, branching map, and encounter system.

### M1.1 -- Card System Rework
- Implement six card categories: Hero, Equipment, Colony, Colony Benefit, Hero Benefit, Resource
- Split into two decks: Hero Deck and Colony Deck
- Update CardDefinitionSO for new card types
- Create ColonyCardDefinitionSO for colony-specific data
- Update enums and data structures

### M1.2 -- Colony Management System
- Colony board (small grid for colony card placement)
- Colony card placement with adjacency requirements
- Colony effects calculation (hero deck size, consumption rate, passive bonuses)
- Colony UI for card placement and stats display
- Colony reset between levels

### M1.3 -- Branching Map System
- Procedural map generation (Slay the Spire style)
- Node types: Resource Encounter, Elite, Boss, Shop, Healing, Upgrade, Draft/Remove, Event, Rest
- Map traversal UI with path selection
- Map state persistence in save data

### M1.4 -- Encounter System Rework
- Auto-deployment of heroes (no manual placement)
- Auto-equipment application
- Recall button for resource encounters
- Encounter definitions (ScriptableObject-based)
- Resource gathering -> stockpile flow
- Resource consumption per turn

### M1.5 -- Boss Fight System
- Boss framework: HP, phases, abilities
- Elder Silas implementation (Level 1)
- Boss reward selection UI
- Colony HP integration

### M1.6 -- Map Node Implementations
- Shop node (buy cards/relics with Currency)
- Healing shrine (spend Food for Colony HP)
- Upgrade shrine (spend Materials to upgrade cards)
- Card draft/removal node
- Event node (narrative choices)
- Rest site

### M1.7 -- Integration & Polish
- Full run flow: Colony -> Map -> Encounters -> Boss -> Next Level
- Run start/end screens
- Save/load for new run structure
- Resource consumption balancing
- Level difficulty scaling

## M2 -- Content Expansion (Planned)
- Levels 2 and 3 fully playable
- Expanded card pool (60+ cards)
- All bosses implemented
- Meta-progression system
- The Pied Piper final boss

## M3 -- Polish & Release (Planned)
- Art and audio
- Balance tuning
- Accessibility features
- Steam integration

---

# 12. Resolved Design Decisions
The following questions were resolved during the v1.0 design process. Answers are documented here for reference and are reflected in the relevant sections above.

1. **Colony board size**: Scales with level -- 3x3 (Wilderness), 4x4 (Rural Village), 5x5 (Town). See 4.3.
2. **Hero deck base maximum**: 8 cards before colony bonuses. Colony cards can push to 10-12. See 4.4.
3. **Colony card hand size**: 5 cards per hand. See 4.3.
4. **Resource consumption formula**: Flat rate = population / 2 (rounded up, min 1) food per map node. Starvation: 2 Colony HP damage per unpaid food. See 4.5.
5. **Recall timing**: Instant press, 1-turn delay before heroes retreat. See 6.6.
6. **Equipment auto-assignment**: Fully automated by priority (combat gear -> highest-combat hero, etc.). Displayed before encounter. See 6.3.
7. **Colony card placement**: Unlimited deliberation time. See 4.3.
8. **Map visibility**: Full map revealed at level start. See 5.1.
9. **Encounter scaling**: Scales within a level by map row (earlier = easier, later = harder). Level transitions are the biggest jumps. See 5.1.
10. **Set-aside hero cards**: Player chooses which cards to set aside when over deck limit. See 4.4.
11. **Hero auto-deployment**: Leftmost column of valid tiles, top to bottom in deck order. See 6.3.
12. **Hero Benefit triggers**: Condition-based auto-triggers (drawn at encounter start, fire when condition met, discarded if unused). See 6.3.
13. **Resource node regeneration**: No mid-encounter regeneration. Resources placed at setup only. See 6.2.
14. **Encounter turn limit**: No hard turn limit. Encounters end via end conditions only. See 6.3.
15. **Colony population**: Sum of placed colony cards' population costs. Hero count does not factor in. See 4.3, 4.5.
16. **Wound persistence**: Wounds persist across map nodes. Only healed by sitting out an encounter, healing shrines, or rest sites. See 8.2.
