
SCURRY
Tales of the Rat Pack

Game Design Document
Version 0.2 — M0 Prototype
Platform: Unity 6  •  Windows / macOS / Linux + SteamOS

# 1. Game Overview
## 1.1 High Concept
Scurry: Tales of the Rat Pack is a cute, cartoony roguelike deck-builder / auto-battler set in a richly detailed fantasy medieval world. Players lead a plucky colony of rats through increasingly dangerous territory — from the open wilds to a bustling human village — gathering resources, recruiting heroes, and outfitting their colony for survival. Every run ends in a climactic confrontation with the legendary Pied Piper.
## 1.2 Genre & Platform

## 1.3 Core Pillars
- Charming World — Adorable rat heroes, hand-drawn cartoon aesthetic, warm colour palette.
- Strategic Depth — Every card placement and hero assignment matters; no two runs are alike.
- Emergent Narrative — The lore unfolds through card flavour text, boss speeches, and colony events.
- Accessibility — Easy to learn in the first two zones; mastery rewards veteran players in the village arc.
- Replayability — Draft variability, random stage layouts, and procedural shops keep runs fresh.

# 2. World & Lore
## 2.1 Setting
The world of Scurry is a high-medieval fantasy land called Hearthshire — a patchwork of enchanted forests, rolling farmlands, and cobblestone villages. To the humans who live here it is an idyllic realm of gentle magic and honest toil. To the rats who scurry beneath it, it is a land of giants, traps, and terrible music.
## 2.2 The Colony
The player leads a colony of rats known simply as The Pack. After their burrow is flooded, the Pack must carve a new home through three increasingly dangerous regions, gathering the resources needed to survive and grow stronger. The colony is represented by its deck — each card is a rat, a tool, a stash of food, or a fragment of shelter — and must be carefully curated over the course of each run.
## 2.3 The Villain — The Pied Piper
The Pied Piper is the ultimate antagonist: a tall, gaunt figure in motley rags who plays an enchanted flute that can command any rat. He works at the behest of the village Mayor and serves as the final, climactic boss. Throughout the run, the player will hear rumours of his approach — first as distant music, then as something more sinister. Defeating him means freedom for the colony forever.

## 2.4 World Zones

# 3. Core Game Loop
## 3.1 Run Structure
Each run of Scurry consists of three Zones. Each Zone comprises a series of Stages. Each Stage is a sequence of Steps followed by a mandatory Boss Fight. The player progresses through each step in order, making strategic decisions that compound over time.

## 3.2 Run Flow Diagram
RUN START → Deck Draft → [ Zone 1 → Zone 2 → Zone 3 ] → Final Boss → RUN END
Each Zone: Stage 1 → Stage 2 → Stage 3 → Boss Stage
Each Stage: [ Step A → Step B → Step C → ... ] → Boss Fight
## 3.3 Step Types
Each stage consists of 3–5 steps drawn from the following pool. Steps are weighted and procedurally selected to ensure a balanced run.


## 3.4 Boss Fight
Every stage ends with a Boss Fight — a special auto-battle encounter using the hero cards the player has deployed during that stage's Card Placement Games. The boss has a fixed set of abilities that interact with the resources and heroes on the board. Defeating the boss awards a rare card or relic and unlocks the next stage.

# 4. Card Placement Game
## 4.1 Overview
The Card Placement Game is the heart of Scurry. It plays out over multiple turns, each consisting of a Deployment Phase followed by a Gathering Phase. The game continues until one of three end conditions is met (see §4.6).

## 4.2 The Board
The board is a grid of tiles representing the current terrain (forest clearing, barn interior, village alley, etc.). Some tiles contain Resource Nodes — locations where specific resources can be found. Other tiles may contain Enemy Patrol zones, Hazards, or Neutral areas.
- Grid size scales with stage difficulty: 4×4 (Wilds) → 5×5 (Farm) → 6×6 (Village)
- Each Resource Node has a type (Food, Shelter, Equipment, Currency) and a yield value. Resource Nodes auto-generate their resource at the start of each turn if empty.
- Enemy Patrol tiles (red) indicate tiles occupied by enemy agents. Tile colours update dynamically between turns to reflect enemy movement (see §4.5).

## 4.3 Phase 1 — Deployment
The player draws a hand of 5 cards from their deck. They may play any number of cards from their hand onto the board, subject to placement rules. The deployment phase continues until the player presses "End Turn" — they are not limited to a single hand. Unplayed cards go to the discard pile (not back to the draw pile).

An undo button allows the player to reverse card placements during the Deploy phase before ending the turn.

### Resource Cards
Resource cards are placed on valid board tiles (Normal or Resource Node) to mark them as 'targeted for gathering.' Each resource card type corresponds to one of the five core resource categories:
- Food Cards — placed near food nodes (berries, grain, scraps). Consumed to restore Colony HP and feed hero cards.
- Shelter Cards — placed on safe tiles. Provide a defensive bonus to adjacent heroes and reduce damage taken during combat (shelter adjacency defense).
- Equipment Cards — placed on or near heroes. Buff the hero's combat or gathering stats.
- Currency Cards — placed on merchant nodes or held in reserve. Spent in the Shop step.

Resource cards placed by the player persist on the board until collected by a hero during the Gathering Phase. When collected, resource cards return to the player's discard pile and are recycled back into the deck.

### Hero Cards
Hero cards are rat characters placed on the board to carry out the actual gathering. Each hero has:
- A movement value — how many tiles they can cross in Phase 2.
- A combat value — their ability to fight through enemy patrol tiles.
- A carry capacity — how many resource cards they can collect in one Gathering Phase.
- A special ability — a unique passive or active skill that triggers during Phase 2.

Hero tokens persist on the board between turns. Their stats reset each turn but their position is retained. Heroes that are wounded skip the next gathering phase to heal (see §8.2).

## 4.4 Phase 2 — Gathering (Auto-Battle)
Once the player ends Deployment, the Gathering Phase resolves automatically, playing out like an auto-battler:
- Each hero activates in initiative order (fastest first).
- Heroes move along the shortest path (A* pathfinding) toward their nearest target resource card.
- If a hero crosses an Enemy Patrol tile, combat is resolved automatically using the hero's combat value vs. the enemy's strength. Shelter adjacency provides a defensive bonus: heroes adjacent to a placed shelter card take reduced effective enemy strength in combat.
- If the hero wins, the enemy is defeated and the hero continues moving. If they lose, the hero is wounded and the resource is not gathered.
- Successfully reaching a resource card collects it — the resource is added to the colony's stockpile.
- Enemy agents also act during the Gathering Phase: they chase nearby heroes or patrol between positions based on proximity.

The Gathering Phase displays real-time notifications for hero movement, combat outcomes, resource collection, and enemy actions.

💡 The player cannot intervene during Phase 2 — positioning and card choices during Deployment are everything.

## 4.5 Dynamic Tile Transitions
Between turns, the board updates to reflect enemy movement:
- Red (Enemy Patrol) tiles that no longer have an enemy revert to green (Normal), regardless of whether they were originally enemy tiles in the board layout.
- Non-red tiles that now have an enemy on them become red (Enemy Patrol).
- This creates a dynamic, shifting threat landscape where safe routes change each turn.

## 4.6 End Conditions
Each Card Placement Game ends when one of three conditions is met:
- **End Condition A — No Heroes Available**: The player has no hero cards left to deploy (all wounded or exhausted). The game ends; any remaining resources are lost.
- **End Condition B — All Resources Collected**: Every resource on the board has been gathered by heroes. The game ends successfully.
- **End Condition C — All Enemies Defeated**: Every enemy agent on the board has been defeated. All remaining uncollected resources are automatically gathered for the colony.

## 4.7 Card Resolution Summary

# 5. Deck Building & Cards
## 5.1 Deck Building Phase
At the start of each Card Placement Game, the player enters a Deck Building phase. The full card pool is presented and the player selects cards to add to their deck, subject to copy limits per rarity (see §5.4). The deck building UI displays each card's current count and maximum allowed copies. Once the player confirms their deck, the game proceeds to the first Draw phase.

### Starting Draft (Future)
In the full game, runs will begin with a Draft before the first zone:
- A pool of cards is assembled from a randomised selection of the base card set.
- The player is presented with 3 cards at a time and picks 1 to add to their deck.
- This repeats for 8 rounds, resulting in a starting deck of 8 chosen cards plus 4 fixed starter cards (2× Basic Scavenger Hero, 1× Stale Crust Food, 1× Pebble Shelter).
- Draft options lean toward the first zone's enemy types to give the player a relevant starting set.

## 5.2 Card Rarity
Cards are assigned one of four rarity tiers that affect copy limits and drop rates:
- **Common** — Baseline cards. Plentiful and reliable. (e.g. Food Scrap, Crumb Pile, Cardboard Shelter)
- **Uncommon** — Slightly stronger or more specialised. (e.g. Scout Rat, Nimble Rat, Bottle Cap Armor, Shiny Coin)
- **Rare** — Powerful cards that define strategies. (e.g. Brawler Rat, Guard Rat, Pack Rat)
- **Legendary** — Run-defining cards. Extremely limited. (None in M0 card pool)

## 5.3 Card Categories
### Hero Cards (Rat Characters)
- **Scout Rat** (Uncommon) — Move 4, Combat 1, Carry 1. Fast reconnaissance hero.
- **Brawler Rat** (Rare) — Move 2, Combat 4, Carry 1. Heavy combat specialist.
- **Pack Rat** (Rare) — Move 3, Combat 2, Carry 3. High carry capacity for resource runs.
- **Nimble Rat** (Uncommon) — Move 5, Combat 1, Carry 1. Fastest hero, fragile in combat.
- **Guard Rat** (Rare) — Move 2, Combat 3, Carry 1. Defensive hero with strong combat.

### Resource Cards
- **Food Scrap** (Common) — Food resource, value 2. Heals colony HP when gathered.
- **Crumb Pile** (Common) — Food resource, value 3. Larger food cache.
- **Cardboard Shelter** (Common) — Shelter resource, value 1. Provides adjacency defense bonus.
- **Bottle Cap Armor** (Uncommon) — Equipment resource, value 2. Buffs hero stats.
- **Shiny Coin** (Uncommon) — Currency resource, value 3. Used in shops.

## 5.4 Deck Limits
- Minimum deck size: 10 cards.
- Maximum deck size: 20 cards.
- Maximum 3 copies of any Common card.
- Maximum 2 copies of any Uncommon card.
- Maximum 2 copies of any Rare card.
- Maximum 1 copy of any Legendary card.
💡 The Card Add/Removal step allows pruning bad cards — keeping the deck lean is often more powerful than bloating it.

# 6. Stage Steps — Detailed Rules
## 6.1 Shop
The shop presents 4–6 cards and 1–2 relics for purchase using the Currency resource stockpile accumulated in the most recent Card Placement Game step. Cards are drawn from a zone-appropriate pool weighted toward the current zone's rarity.
- Cards can be purchased and immediately added to the deck (subject to deck limits).
- The shop can be 'rerolled' once per visit for a small Currency cost.
- Relics are passive items that provide persistent run-wide bonuses (e.g. 'All heroes gain +1 Move').
- Unsold cards are discarded at the end of the shop step.

## 6.2 Healing / Resupply
The colony's HP can be restored by spending Food resources. This step presents the player with a simple menu:
- Minor Heal — Spend 2 Food: restore 5 Colony HP.
- Major Heal — Spend 5 Food: restore 15 Colony HP.
- Resupply — Spend 3 Food: fully heal one wounded hero card.
💡 Food gathered during the previous Card Placement Game step is automatically converted to healing during this step at no additional action cost.

## 6.3 Card Addition / Removal
This step gives the player one of two options (one per step visit):
### Card Addition
- A mini-draft of 3 cards is presented.
- The player picks 1 card to add to the deck for free (subject to deck limits).
- Cards offered are weighted toward the upcoming boss's weakness.
### Card Removal (Purge)
- The player may remove any 1 card from their current deck, permanently, at no cost.
- Removing starter cards (e.g. Stale Crust) is often optimal mid-run to increase consistency.

## 6.4 Boss Fight
Boss fights are auto-battle encounters that use the heroes currently deployed on the board from the most recent Card Placement Game. The boss occupies a central node on a special Boss Board.
- The boss has HP, attack patterns, and phase abilities that trigger at HP thresholds.
- Heroes auto-attack the boss in initiative order. The boss attacks the highest-combat hero each round.
- Shelter cards on the board during Phase 1 of the preceding Card Placement Game carry over as defensive structures.
- Winning the boss fight awards a choice of 1 rare or 2 uncommon cards, plus a fixed amount of Currency.
- If the Colony HP reaches 0 during a boss fight, the run ends immediately.

# 7. Zones, Enemies & Bosses
## 7.1 Zone 1 — The Wilds
The wild countryside surrounding the Pack's flooded burrow. Ancient trees, tall grass, babbling streams, and the constant threat from above and below.
### Enemies

### Zone 1 Boss — Elder Silas, the Great Horned Owl
A massive, ancient owl who has hunted the meadows for decades. His hollow is filled with the bones of a hundred rats.
- HP: 20 | Attack: 3 | Phases: 2
- Phase 1 (HP 20–11): Swoops — targets the highest-carry hero, stunning them for 1 round.
- Phase 2 (HP 10–0): Talons — attacks all heroes simultaneously for 2 damage each.
- Reward: 1 Rare Hero or Rare Equipment card + Feather Relic (heroes ignore the first Patrol tile each Gathering Phase).

## 7.2 Zone 2 — The Farm
A sprawling fantasy farmstead with a stone barn, grain silos, vegetable gardens, and a farmhouse full of suspicious smells. Treasure in abundance — but the dangers are far more organised.
### Enemies

### Zone 2 Boss — Head Farmer Tobias & Duchess
Tobias is a cunning old farmer who never trusted the peace. He fights alongside Duchess, his beloved prize-winning mouser.
- HP: 35 (Tobias 20 / Duchess 15) | Attack: varies
- Must defeat Duchess first — while alive, Duchess intercepts all attacks aimed at Tobias.
- Tobias: pitchfork sweep attacks all heroes in a row for 2 damage.
- Duchess: pounces on the hero with the lowest combat for 4 damage.
- Reward: 1 Legendary card or 2 Rare cards + Barn Key Relic (shop prices reduced by 2 Currency).

## 7.3 Zone 3 — The Village
A bustling medieval fantasy town with cobblestone streets, market stalls, taverns, and the dreaded Rat-Catcher Guild headquarters. The Pack must outwit organised human opposition.
### Enemies

### Zone 3 Boss — Guildmaster Aldric Fenn
Aldric Fenn built the Rat-Catcher Guild from nothing. He is methodical, ruthless, and personally affronted by the Pack's existence.
- HP: 50 | Attack: 5 | Phases: 3
- Phase 1 (HP 50–35): Deploys 2 Guild Apprentices each round as summons.
- Phase 2 (HP 34–15): Sprays smoke — all heroes lose 1 Move for 2 rounds.
- Phase 3 (HP 14–0): Rage — his own attack increases to 8; attacks twice per round.
- Reward: Unlocks the Final Stage + guaranteed Legendary card.

## 7.4 The Final Stage — The Pied Piper
The village square at midnight. The Piper stands on the fountain plinth, his flute gleaming. Around him, rats from other colonies dance helplessly in his thrall. This is the last battle — and the most important.
- HP: 80 | Attack: varies | Phases: 4
- Phase 1 (HP 80–61): The Melody — all heroes lose 1 Combat. Passive aura.
- Phase 2 (HP 60–41): Charmed Summons — converts 1 friendly hero card per round to fight against the player.
- Phase 3 (HP 40–21): The High Note — deals 10 damage to all heroes simultaneously. One-time.
- Phase 4 (HP 20–0): Discordant Finale — his attack becomes 10. However, any hero with a Musical Instrument relic is immune to his charm.
💡 Musical Instrument relics can only be found in Zone 3 shops and the Guildmaster reward — another reason to push forward.
- Victory: The Piper's flute shatters. The Pack is free. Run complete — score tallied and recorded.

# 8. Colony Health & Meta-Progression
## 8.1 Colony HP
Colony HP represents the overall vitality of the Pack. It persists across all steps within a run (not just individual battles).
- Starting HP: 30
- Maximum HP: 50 (can be increased via certain relics or Legendary cards)
- Colony HP reaches 0 → the run ends immediately (no mid-run saving)
- Colony HP does NOT reset between stages — only the Healing/Resupply step restores it

## 8.2 Hero Wound System
Individual hero cards can become Wounded during Phase 2 combat. A Wounded hero:
- Is marked as wounded and skips the next Gathering Phase to heal (they can still be deployed but will not move or gather)
- After sitting out one deployed turn, the hero is fully healed and available again
- If a hero is wounded a second time (or defeated outright), they are permanently exhausted — their card is removed from the deck for the rest of the run

## 8.3 Meta-Progression (Runs Across Sessions)
Scurry features a light meta-progression system that rewards repeated play without making any individual run trivially easy:
### The Rattery (Unlockable Cards)
- Winning a run with a particular hero card unlocks a new, more powerful version of that hero for future draft pools.
- Discovering a new enemy type for the first time adds their card to the 'Bestiary' and slightly expands the shop pool.
### Colony Reputation
- At the end of each run (win or lose), the player earns Reputation based on stages completed, cards drafted, and boss kills.
- Reputation is a persistent currency used to unlock new starting relics for future runs (cosmetic and minor gameplay modifiers).
### The Scrapbook
- A flavour-text journal that fills in automatically as the player discovers new cards, enemies, and lore events.
- Purely cosmetic — but a satisfying collection goal.

# 9. User Interface & Visual Direction
## 9.1 Visual Style
Scurry uses a warm, hand-drawn cartoon aesthetic inspired by illustrated children's books. Think soft pencil outlines, watercolour-adjacent textures, and exaggerated character proportions (big expressive eyes, tiny paws). The UI should feel like a storybook come to life.
- Colour palette: warm browns, forest greens, harvest golds, and soft creams — with pops of red for danger.
- Characters: anthropomorphic rats with distinct silhouettes and personality-driven idle animations.
- Environments: lush, layered parallax backgrounds that breathe life into each zone.
## 9.2 Card Visual Design
- Cards should resemble aged parchment with hand-inked borders appropriate to their type.
- Hero cards feature a portrait of the rat character with their stat icons arranged clearly below.
- Resource cards show a small vignette illustration of the item (a gnawed crust of bread, a glittering coin, etc.).
- Rarity is communicated via border colour and a small gem/star icon in the corner.
## 9.3 Key Screens

# 10. Technical Specifications
## 10.1 Unity 6 Architecture
- Render Pipeline: Universal Render Pipeline (URP) 2D — supports all three target platforms efficiently.
- UI System: Programmatically-built Unity UI (Canvas) for HUD, phase labels, HP bars, and deck building. World-space elements for board tiles, cards, and tokens.
- Save System: JSON serialisation to Application.persistentDataPath. Tracks turn number, colony HP, currency, draw/discard pile contents, wounded heroes, and living enemy positions with strengths.
- Card Data: ScriptableObject-based card definitions (CardDefinitionSO) — designer-friendly, hot-reloadable in editor. Board layouts defined via BoardLayoutSO using TileType enum arrays.
- Board Logic: Grid-based system using a 2D Tile[,] array; A* pathfinding for Phase 2 hero movement. Dynamic tile type transitions between turns.
- Event System: Static EventBus with Action delegates for decoupled communication between systems.
- Localisation: ScriptableObject-based localisation tables (LocalizationTableSO) with 5 languages (English, French, German, Italian, Spanish) and ~60 keys. Static Loc helper class for runtime string lookup and formatting.
- Animation: 2D sprite animations using the Unity Animation system; DOTween for UI transitions and card interactions (planned).

## 10.2 Platform-Specific Notes

## 10.3 Data Architecture — Key Systems
- **CardDefinitionSO** — ScriptableObject containing all card data (name, card type, resource type, rarity, stats: move/combat/carry/value, flavour text).
- **BoardLayoutSO** — ScriptableObject defining grid dimensions, tile type array, and resource node configuration (type + yield per node).
- **LocalizationTableSO** — ScriptableObject holding key-value string pairs for a single language.
- **RunSaveData** — JSON-serializable class persisting run state: turn number, colony HP, currency, draw/discard pile card names, wounded hero names, and living enemy positions with strengths.
- **DeckManager** — Runtime deck management: draw pile, discard pile, shuffle, draw with auto-reshuffle.
- **EventBus** — Static class with Action delegates for system events (phase changes, resource collection, combat, tile hover, etc.).
- MetaProgressionData — persists across runs: Reputation total, unlocked cards, Scrapbook completion (planned).

## 10.4 Minimum System Requirements (estimated)

# 11. Roadmap & Milestones

## M0 — Core Prototype ✅ (v0.2.0)
- 4×4 board with 4 tile types and dynamic tile transitions
- 10 placeholder cards (5 hero, 5 resource) with rarity system
- Multi-turn Deploy → Gather loop with hero/resource token persistence
- A* pathfinding, initiative-based auto-battle, shelter adjacency defense
- Enemy agents with chase/patrol AI
- Deck building with copy limits per rarity
- Persistent wound tracking and hero exhaustion
- Three end conditions (no heroes / all resources / all enemies defeated)
- Colony HP system (30 start, 50 max)
- Run save/load (JSON)
- Localisation (5 languages, ~60 keys)
- Undo button for card placement

## M1 — Vertical Slice (planned)
Zone 1 (The Wilds) fully playable as a single-zone run with art, enemies, a boss, and all step types.

### M1.1 — Run Structure & Stage Flow
- Implement Zone → Stage → Step progression system per §3.1
- Zone 1: 3 stages, each with 3–5 procedurally-selected steps + boss fight
- Step type pool: Card Placement Game, Shop, Healing/Resupply, Card Addition/Removal
- Step weighting and procedural selection to ensure balanced stages
- Run start screen and run end screen (victory / defeat) with score summary

### M1.2 — Starting Draft
- Pre-run draft system per §5.1: 8 rounds of pick-1-from-3
- 4 fixed starter cards (2× Basic Scavenger Hero, 1× Stale Crust Food, 1× Pebble Shelter)
- Draft pool weighted toward Zone 1 enemy types
- Replace current deck building phase with draft for run start (keep deck building for debug/testing)

### M1.3 — Card Pool Expansion (30 cards)
- Expand from 10 → 30 cards across all rarities including Legendary tier
- Zone 1–appropriate hero cards, resource cards, and equipment
- Card art (placeholder or first-pass illustrations)
- Hero special abilities that trigger during Phase 2

### M1.3b — Resource Type Effects
- Implement distinct gameplay effects for each resource type per §5.1:
  - **Food**: collected value goes to Food Stockpile (spent at Healing steps or auto-heals if no Healing step)
  - **Shelter**: adjacency defense bonus scales with shelter card value (value > 1 provides stronger defense)
  - **Equipment**: hero gains temporary combat bonus equal to Equipment value for the current Gathering phase; stacks with multiple equipment
  - **Currency**: collected to Currency Stockpile (spent in Shops)
- Add gathering notification for equipment pickup

### M1.4 — Zone 1 Enemies
- Define and implement Zone 1 enemy types (§7.1 Enemies — currently empty in GDD)
- Enemy variety: different strengths, movement patterns, and behaviours
- Enemy spawning rules per stage difficulty
- Enemy scaling across stages 1–3

### M1.5 — Boss Fight System
- Boss fight framework per §6.4: special Boss Board, HP thresholds, phase abilities
- Elder Silas, the Great Horned Owl (§7.1): HP 20, Attack 3, 2 phases
  - Phase 1: Swoops (stun highest-carry hero)
  - Phase 2: Talons (AoE 2 damage to all heroes)
- Boss reward: choice of 1 Rare card or Rare Equipment + Feather Relic
- Colony HP check — run ends if HP reaches 0 during boss fight

### M1.6 — Shop Step
- Shop UI presenting 4–6 cards + 1–2 relics per §6.1
- Currency spending from colony stockpile
- Shop reroll (once per visit, small Currency cost)
- Deck limit enforcement on purchase

### M1.7 — Healing / Resupply Step
- Healing menu per §6.2: Minor Heal (2 Food → 5 HP), Major Heal (5 Food → 15 HP)
- Resupply: spend 3 Food to fully heal one wounded hero
- Food auto-conversion from previous Card Placement Game

### M1.8 — Card Addition / Removal Step
- Mini-draft (pick 1 of 3) for free card addition per §6.3
- Card removal (purge) option: remove 1 card permanently
- Offerings weighted toward upcoming boss weakness

### M1.9 — Relic System
- Relic data structure (persistent run-wide passive bonuses)
- Feather Relic (Zone 1 boss reward): heroes ignore first Patrol tile each Gathering Phase
- Relic UI display and effect application

### M1.10 — Art & Visual Polish
- First-pass card illustrations (hero portraits, resource vignettes)
- Zone 1 board tileset (forest clearing, tall grass, streams)
- Card rarity border colours and gem/star icons
- Parallax background for The Wilds
- Hero idle animations (placeholder)

### M1.11 — Audio (First Pass)
- Background music for Zone 1
- SFX: card placement, combat, resource collection, phase transitions
- Boss encounter music/sting

💡 All dates TBD — to be confirmed once prototype scope is validated.

# 12. Open Design Questions
The following questions remain open for discussion and playtesting:
- Board tile count per zone — is 4×4 to 6×6 the right progression, or should it scale within zones too?
- Phase 2 speed control — should it be a slider or discrete speed steps (Normal / Fast / Instant)?
- ~~Hero defeat permanence — is permanent loss of a hero card too punishing?~~ **Resolved in M0**: heroes are wounded first (sit out one turn to heal); only permanently exhausted on a second wound or outright defeat.
- Currency bleed — if the player skips the shop, does unspent currency carry over, or is it lost? Carrying over may trivialise early shops.
- Boss carry-over — do hero wounds inflicted during a boss fight carry into the next stage, or reset? Current design says they persist.
- Multiplayer potential — local co-op where one player handles Deployment and one controls a 'support' deck?
- Accessibility options — colour-blind mode (card type icons should never rely on colour alone), adjustable text size, input remapping. Localisation for 5 languages already implemented.
- Mobile port — low priority, but the card-based UI would adapt reasonably well to touchscreen. Keep in mind during UI architecture.
