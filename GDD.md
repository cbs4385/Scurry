# SCURRY
# Tales of the Rat Pack

## Game Design Document
Version 2.0 -- Strategic Overhaul
Platform: Unity 6 -- Windows / macOS / Linux + SteamOS

---

# 1. Game Overview

## 1.1 High Concept
Scurry: Tales of the Rat Pack is a cute, cartoony deck-building strategy game with colony management and auto-resolving combat. Players lead a plucky colony of rats through increasingly dangerous territory -- from the open wilds to a bustling human village -- constructing a deck before each run, deploying hero tokens across a fog-shrouded map, building up their colony, and fighting their way to a climactic confrontation with the legendary Pied Piper.

The player constructs a deck of 10-30 cards before each run, then deploys heroes, expands the colony, gathers resources, and battles enemies across a persistent procedurally generated map. There is no fixed turn limit -- but once a hero enters the Town zone (zone 3), a 15-turn countdown begins. When it expires, the Pied Piper leaves his lair and marches directly on the colony. Combat resolves automatically with the player influencing outcomes through tactical card plays. Every card in the deck matters -- smaller decks and faster victories earn higher scores.

## 1.2 Genre & Platform
- Deck-building strategy game with colony management and auto-resolving combat
- Single-player, PC (Windows / macOS / Linux + SteamOS)

## 1.3 Core Pillars
- **Strategic Deck Construction** -- Every card in the deck counts. Smaller decks score higher, but larger decks offer more options. Building the right deck for the right strategy is the meta-game.
- **Colony as Engine** -- The colony is the player's power base. It produces food, enables retargeting, and provides upgrades. Expanding the colony is essential but consumes precious turns and cards.
- **Fog of War Exploration** -- The map is hidden. Scouting reveals the path forward but exposes heroes to danger. Information is power.
- **Tactical Combat** -- Combat auto-resolves but the player plays tactical cards between rounds to influence outcomes. Knowing when to commit resources and when to retreat is key.
- **Logistics & Territory** -- Resources must be physically carried back to the colony. Heroes need food to stay deployed. Every node traversed is a commitment.
- **Charming World** -- Adorable rat heroes, hand-drawn cartoon aesthetic, warm colour palette.
- **Replayability** -- Procedurally generated maps, deck construction variety, and enemy patrol randomness ensure no two runs play the same.

---

# 2. World & Lore

## 2.1 Setting
The world of Scurry is a high-medieval fantasy land called Hearthshire -- a patchwork of enchanted forests, rolling farmlands, and cobblestone villages. To the humans who live here it is an idyllic realm of gentle magic and honest toil. To the rats who scurry beneath it, it is a land of giants, traps, and terrible music.

## 2.2 The Colony
The player leads a colony of rats known simply as The Pack. After their burrow is flooded, the Pack must carve a new home and fight their way through three increasingly dangerous regions to confront the Pied Piper and secure their freedom. The colony starts as a simple entrance and burrow, growing over the course of a run into a thriving underground settlement -- if the player invests the cards and turns to build it.

## 2.3 The Villain -- The Pied Piper
The Pied Piper is the ultimate antagonist: a tall, gaunt figure in motley rags who plays an enchanted flute that can command any rat. He works at the behest of the village Mayor and waits at the far end of the map, in the heart of the town. Throughout the run, the player will push closer to his domain -- first through the wilds, then the farmland, and finally into the dangerous urban zone where the Piper holds court. Defeating him means freedom for the colony forever.

## 2.4 World Zones
The map consists of three zones of increasing difficulty, layered geographically from the colony (south) to the Pied Piper (north):

1. **The Wilderness** -- Forests, meadows, and streams. Low-threat enemies, abundant resources (2-4 per node).
2. **The Farmland** -- Farms, barns, and fields. Organised enemies, moderate resources (1-3 per node).
3. **The Town** -- Cobblestone streets, markets, and the Rat-Catcher Guild. Dangerous enemies, scarce resources (0-2 per node).

---

# 3. Card System

All cards in Scurry belong to a single constructed deck. There are five card categories:

## 3.1 Hero Cards (20 in pool)
Rat characters that deploy as tokens on the map. Each hero has:
- **Combat** -- Fighting strength. Contributes to pooled combat on a node.
- **Move** -- Number of nodes the hero can traverse per turn.
- **HP** -- Hit points. Persistent damage across rounds and turns. Reduced to 0 = injured.
- **Carry** -- Number of resource units the hero can carry simultaneously.
- **Initiative** -- Tie-breaker for damage application (lower initiative takes damage first in ties).
- **Special Ability** -- Unique per-hero effect (TBD -- Design Team).

Hero tokens are deployed from the colony node during the Hero Deployment phase. They auto-path toward an assigned target node. Heroes on the colony node can be reassigned new targets at any time; heroes elsewhere require a colony upgrade to retarget.

### Hero Roster

| # | Name | Role | Description |
|---|---|---|---|
| 1 | Scout | Recon | Nimble rat with leaf cloak and acorn helmet, needle spear |
| 2 | Explorer | Recon | Curious rat studying leaf parchment maps |
| 3 | Ranger | Ranged | Stealthy rat with moss cloak and thorn bow |
| 4 | Messenger | Fast | Swift rat sprinting along tree roots with satchel |
| 5 | Warrior | Melee | Battle-ready rat with bark armor, needle sword, button shield |
| 6 | Guardian | Tank | Sturdy rat protecting burrow with thimble shield and pin mace |
| 7 | Bulwark | Tank | Large armored rat with acorn shield at wooden barricade |
| 8 | Sentinel | Melee | Stoic rat on watch rock overlooking the meadow |
| 9 | Forager | Gather | Happy rat dragging a huge berry through tall grass |
| 10 | Healer | Support | Gentle rat mixing glowing herbs in a wooden bowl |
| 11 | Inventor | Support | Clever rat building gadgets from gears and springs |
| 12 | Rogue | Melee | Playful rat balancing on mushroom, juggling buttons |
| 13 | Captain | Leader | Heroic rat raising cloth banner, rallying fellow rats |
| 14 | Commander | Leader | Battle-hardened rat leading a charge through tall grass |
| 15 | Elder | Support | Wise elder seated in hollow root with glowing mushrooms |
| 16 | Assassin | Melee | Shadowy rat leaping between roots with twin pin daggers |
| 17 | Quartermaster | Gather | Resourceful rat in granary filled with acorns and seeds |
| 18 | Torchbearer | Recon | Fast rat running with a glowing twig-and-ember torch |
| 19 | Knight | Melee | Armored rat knight wielding large thorn blade |
| 20 | Scholar | Support | Scholar rat studying maps and exploration routes |

*Stats TBD -- Design Team. Stats should reflect role: Scouts/Messengers have high Move, Warriors/Knights have high Combat, Foragers/Quartermasters have high Carry.*

## 3.2 Colony Cards (30 in pool)
Infrastructure cards played during the Colony Card phase to expand the colony's freeform graph. Each colony card provides a specific benefit when attached to the colony.

Colony cards are divided into three tiers:

### Food & Storage (10 cards)
Production and resource infrastructure.

| # | Name | Effect |
|---|---|---|
| 1 | Underground Storage | +1 food production per turn |
| 2 | Berry Drying Racks | +1 food production per turn |
| 3 | Seed Vault | Stored resources cannot be lost if colony is attacked |
| 4 | Cold Storage | +1 food production per turn, food spoilage immunity |
| 5 | Colony Entrance | *Starter card (free)* -- Required. Enables hero deployment. |
| 6 | Basic Burrow | *Starter card (free)* -- +2 food production per turn (base). |
| 7 | Nursery | +1 max hero deployment per turn (TBD) |
| 8 | Tunnel Network | Heroes deploying from colony get +1 Move on their first turn |
| 9 | Workshop | Equipped heroes get +1 Combat |
| 10 | Tailory | Equipped heroes get +1 HP |

### Structure & Defense (10 cards)
Colony defenses and structural upgrades.

| # | Name | Effect |
|---|---|---|
| 11 | Thorn Wall | Enemies attacking colony must defeat wall (X Combat) first |
| 12 | Watchtower | Reveals all nodes within 2 edges of colony (pierces fog) |
| 13 | Pit Traps | Enemies entering colony node take X damage before combat |
| 14 | Wooden Gate | +X Combat to all heroes defending colony node |
| 15 | Council Hall | Enables retargeting of deployed heroes anywhere on map |
| 16 | Campfire | Injured heroes heal 1 HP per turn while in available pool |
| 17 | Training Ground | Newly deployed heroes get +1 Combat for their first combat |
| 18 | War Hall | +1 Combat to all hero tokens on the map |
| 19 | Mushroom Farm | +2 food production per turn |
| 20 | Fresh Water Pool | +1 food production per turn, injured heroes heal +1 faster |

### Advanced Upgrades (10 cards)
Powerful late-game colony improvements.

| # | Name | Effect |
|---|---|---|
| 21 | Signal Tower | Reveals all nodes within 3 edges of any hero token |
| 22 | Great City | All colony production effects doubled |
| 23 | Grand Stores | +5 food storage capacity (food carries over between turns) |
| 24 | War Room | All hero tokens get +1 Move |
| 25 | Shrine of Heroes | Injured heroes return to available pool immediately (no turn delay) |
| 26 | Throne Room | Enables playing 2 colony cards per turn instead of 1 |
| 27 | Forward Camp | Heroes can also deploy from the furthest-forward friendly node |
| 28 | Messenger Post | Enables retargeting of all heroes each turn (no Council Hall needed) |
| 29 | Forge | Equipped weapons grant +1 additional Combat |
| 30 | Crafting Table | Equipped utility items grant double effect |

*Exact numeric values TBD -- Design Team. Effects listed are directional; balancing required.*

## 3.3 Equipment Cards (40 in pool)
Gear attached to hero tokens at deployment. Each hero has three equipment slots:
- **Offensive** (1 slot) -- Weapons. Increase Combat and/or add offensive effects.
- **Defensive** (1 slot) -- Armor, shields, cloaks. Increase HP and/or add defensive effects.
- **Utility** (1 slot) -- Tools and accessories. Increase Move, Carry, or add special effects.

Legendary equipment occupies the appropriate offensive or defensive slot.

Equipment is attached from the player's deck during the Hero Deployment phase and remains on the hero for as long as the hero is in play. If a hero is injured, their equipment returns to the available deck.

### Weapons -- Offensive Slot (10 cards)

| # | Name | Rarity | Effect |
|---|---|---|---|
| 1 | Needle Sword | Common | +1 Combat |
| 2 | Thorn Spear | Common | +1 Combat, +1 range (can strike before melee) |
| 3 | Pin Dagger | Common | +1 Combat, +1 Initiative |
| 4 | Splinter Staff | Common | +1 Combat to all friendly tokens on same node |
| 5 | Needle Lance | Uncommon | +2 Combat |
| 6 | Pebble Sling | Uncommon | +1 Combat, ranged (strikes before melee round) |
| 7 | Thorn Whip | Uncommon | +2 Combat, damages lowest-HP enemy first |
| 8 | Bone Knife | Uncommon | +2 Combat, +1 Initiative |
| 9 | Razor Leaf Blade | Rare | +3 Combat |
| 10 | Acorn Hammer | Rare | +3 Combat, stuns target for 1 round |

### Armor -- Defensive Slot (10 cards)

| # | Name | Rarity | Effect |
|---|---|---|---|
| 1 | Thimble Helmet | Common | +1 HP |
| 2 | Button Shield | Common | +1 HP, blocks 1 damage per round |
| 3 | Bark Armor | Common | +2 HP |
| 4 | Moss Cloak | Common | Enemies cannot target this hero first |
| 5 | Feather Cape | Uncommon | +1 HP, +1 Move |
| 6 | Leaf Armor | Uncommon | +2 HP, +1 Combat |
| 7 | Beetle Shell Shield | Uncommon | +2 HP, blocks 1 damage per round |
| 8 | Acorn Helm | Rare | +3 HP |
| 9 | Grapple Hook | Rare | +1 HP, can traverse 2 edges as 1 Move |
| 10 | Thread Rope | Rare | +1 HP, can pull 1 friendly token to same node |

### Utility -- Utility Slot (10 cards)

| # | Name | Rarity | Effect |
|---|---|---|---|
| 1 | Bead Lantern | Common | Reveals fog 1 additional node beyond this hero |
| 2 | Button Compass | Common | +1 Move |
| 3 | Cloth Satchel | Common | +2 Carry |
| 4 | Matchstick Ladder | Common | Ignores terrain movement penalties |
| 5 | Berry Basket | Uncommon | +3 Carry |
| 6 | Map Scroll | Uncommon | Reveals all nodes along hero's path to target |
| 7 | Healing Herb Kit | Uncommon | Heal 1 HP per turn while moving |
| 8 | Snail Shell Horn | Uncommon | Adjacent friendly heroes get +1 Combat |
| 9 | Flint Firestarter | Rare | Enemies on this node take 1 damage at start of combat |
| 10 | Climbing Claws | Rare | +2 Move, can traverse any edge regardless of restrictions |

### Legendary Equipment (10 cards)

| # | Name | Slot | Rarity | Effect |
|---|---|---|---|---|
| 1 | Storm Needle | Offensive | Legendary | +4 Combat, deals 1 splash damage to all enemies on node |
| 2 | Ember Torch | Offensive | Legendary | +3 Combat, reveals fog 2 nodes in all directions |
| 3 | Frost Thread Cloak | Defensive | Legendary | +3 HP, enemies on node lose 1 Combat |
| 4 | Golden Acorn Charm | Defensive | Legendary | +2 HP, +1 food production while hero is deployed |
| 5 | War Banner Pin | Offensive | Legendary | +2 Combat to all friendly tokens on this node |
| 6 | Thorn Crown | Offensive | Legendary | +3 Combat, takes 1 self-damage per round |
| 7 | Root Armor | Defensive | Legendary | +5 HP, -1 Move |
| 8 | Moonlight Dagger | Offensive | Legendary | +3 Combat, +2 Initiative, double damage from fog |
| 9 | Lantern of the Deep | Defensive | Legendary | +2 HP, reveals entire zone this hero is in |
| 10 | Banner of the Rat Pack | Offensive | Legendary | +2 Combat to ALL friendly tokens on the map |

*Exact stat values TBD -- Design Team. Effects listed are directional.*

## 3.4 Tactical Cards (30 in pool)
Cards played by the player during the Combat phase, before the first round and between subsequent rounds. Tactical cards are the primary way the player influences combat outcomes in real time.

Tactical cards are divided into three tiers:

### Combat Tactics (10 cards)

| # | Name | Rarity | Effect |
|---|---|---|---|
| 1 | Pack Ambush | Common | +2 Combat to all heroes on this node for 1 round |
| 2 | Rally the Pack | Common | +1 Combat to all heroes on this node for rest of combat |
| 3 | Defensive Formation | Common | All heroes on this node block 1 damage this round |
| 4 | Last Stand | Uncommon | One hero with 1 HP gets +5 Combat this round |
| 5 | Rapid Strike | Uncommon | One hero attacks twice this round |
| 6 | Tactical Retreat | Uncommon | Remove all heroes from this node (they move to nearest friendly-occupied node; keep carried resources) |
| 7 | Reinforcements | Rare | Deploy 1 hero from available pool directly to this node |
| 8 | Hidden Tunnel | Rare | Move all heroes on this node to any adjacent node (escape or reposition) |
| 9 | Map Advantage | Rare | Reveal all enemies on the current zone; heroes on this node get +2 Combat |
| 10 | Scout Ahead | Common | Reveal all nodes within 2 edges of this combat; no combat effect |

### Support Tactics (10 cards)

| # | Name | Rarity | Effect |
|---|---|---|---|
| 1 | Gather Seeds | Common | Heroes on this node gather +1 resource from this node |
| 2 | Harvest Bounty | Common | Double resource gathering on this node this turn |
| 3 | Emergency Rations | Common | Feed all heroes on the map this turn (no food cost in clean-up) |
| 4 | Salvage Scrap | Uncommon | Recover resources from this node (even if previously gathered) |
| 5 | Trade Caravan | Uncommon | Convert 3 of any resource to 3 of any other resource |
| 6 | Herbal Remedy | Common | Heal 2 HP to one hero on this node |
| 7 | Warm Burrow | Uncommon | All injured heroes in available pool heal immediately |
| 8 | Rest and Recover | Common | Heal 1 HP to all heroes on this node |
| 9 | Medic Call | Uncommon | Heal 3 HP to one hero anywhere on the map |
| 10 | Renew Strength | Rare | Fully heal one hero on this node |

### Power Tactics (10 cards)

| # | Name | Rarity | Effect |
|---|---|---|---|
| 1 | Heroic Charge | Rare | All heroes on this node get +3 Combat and +1 Move this turn |
| 2 | Pack Unity | Rare | All heroes on the map get +1 Combat this turn |
| 3 | Burrow Defense | Uncommon | Colony node gets +5 Combat defense this turn |
| 4 | Final Gambit | Legendary | One hero gets +10 Combat this round; hero is injured after combat |
| 5 | Hidden Kingdom | Legendary | Reveal the entire map (all fog removed permanently) |
| 6 | Surprise Raid | Rare | One hero can move to any node on the map and attack this turn |
| 7 | Poison Thorn | Uncommon | Deal 3 damage to one enemy on this node before combat begins |
| 8 | Moonlight Strike | Rare | One hero deals double damage this round |
| 9 | Rat Swarm | Legendary | Deploy ALL available heroes to this node immediately |
| 10 | Victory Feast | Legendary | Heal all heroes to full HP; +2 food production this turn |

*Tactical cards are single-use: played once, then removed from the game for the rest of the run. Exact values TBD -- Design Team.*

## 3.5 Resources
Resources exist on map nodes and must be physically carried back to the colony by hero tokens before they can be used. Resource types:

- **Food** -- Consumed each turn to keep heroes deployed (1 food per hero on the map). Also produced by the colony. The primary survival resource.
- **Materials** -- Used for colony card activation costs (TBD) and other upgrades.
- **Currency** -- Spent on special node effects or card upgrades (TBD).

Resources are placed on nodes at map generation:
- **Wilderness nodes**: 2-4 resources each
- **Farmland nodes**: 1-3 resources each
- **Town nodes**: 0-2 resources each

*Resource type distribution per node TBD -- Design Team (e.g., Wilderness mostly Food, Town mostly Currency).*

---

# 4. Pre-Game: Deck Construction

## 4.1 Deck Building
Before each run, the player constructs a deck from the full card pool (120 cards across all categories). The deck must contain between **10 and 30 cards**.

### Card Cost & Multiples
Each card has a **deck cost** (1, 2, or 3+). The cost determines how many copies can be included:

| Deck Cost | Max Copies |
|---|---|
| 1 | 3 |
| 2 | 2 |
| 3+ | 1 |

The deck cost is not a resource spent -- it is a deckbuilding constraint that represents the card's power level. Cheap cards (cost 1) are weaker but allow multiple copies for consistency. Expensive cards (cost 3+) are powerful but limited to singletons.

### Starter Cards
Every deck automatically includes (free, not counting toward deck size):
- **Colony Entrance** -- Enables hero deployment.
- **Basic Burrow** -- Provides base 2 food production per turn.

These cannot be removed from the deck.

## 4.2 Scoring
Final score is calculated based on:
- **Turns taken** -- Fewer turns = higher score. Winning on turn 5 scores far higher than turn 14.
- **Starting deck size** -- Smaller deck = higher score multiplier. A 10-card deck winning on turn 8 massively outscores a 30-card deck winning on turn 12.
- **Enemies defeated** -- Bonus points for combat victories.
- **Resources gathered** -- Bonus points for total resources collected.
- **Colony size** -- Bonus points for colony cards played.

*Exact scoring formula TBD -- Design Team.*

---

# 5. Map Generation

## 5.1 Map Structure
At the start of each run, a persistent map is procedurally generated. The map consists of **30 nodes** plus the colony node and the Pied Piper node (32 total), arranged in three geographic zones.

The map is a **graph** (not a grid): nodes are connected by edges, and the layout forms an irregular network. The full map exists from the start but is hidden by **fog of war**.

### Zone Layout
- **Wilderness** (10 nodes) -- Closest to the colony. Connected directly to the colony node.
- **Farmland** (10 nodes) -- Middle zone. Connected to Wilderness and Town.
- **Town** (10 nodes) -- Farthest from colony. Connected to Farmland and the Pied Piper node.

### Zone Connectivity
- Each zone is a **partially connected graph** of 10 nodes (not every node connects to every other node, but the zone forms a connected subgraph).
- **At least 2 edges** connect adjacent zones (Wilderness↔Farmland, Farmland↔Town).
- The **colony node** connects to 2-3 Wilderness nodes.
- The **Pied Piper node** connects to 2-3 Town nodes.

### Node Contents
Each node has:
- **Resources** -- Gatherable resources placed at map generation (see Section 3.5 for quantities per zone).
- **Enemies** -- Zero or more enemy tokens placed at map generation (see Section 10).
- **Terrain type** -- Cosmetic, determined by zone.

*Node generation parameters (enemy density, resource distribution) TBD -- Design Team.*

## 5.2 Fog of War
The map starts fully obscured except for the colony node and its adjacent nodes.

### Visibility Rules
- The player can see:
  - The **colony node** (always visible)
  - Any node **occupied by a friendly hero token**
  - All nodes **adjacent to a friendly hero token** (connected by one edge)
  - Additional nodes revealed by **colony upgrades** (Watchtower, Signal Tower) or **equipment** (Bead Lantern, Lantern of the Deep)
- Everything else is hidden in fog
- **Enemy tokens in fog are invisible** -- the player does not know where enemies are until a hero gets close enough to reveal them
- Once a node has been **visited** by a hero, its terrain and resource count remain visible even after the hero leaves (fog only re-hides enemy positions)

---

# 6. Colony System

## 6.1 Colony Structure
The colony is a **freeform graph** of colony cards. It starts with two cards (Entrance + Basic Burrow) and grows as the player plays colony cards during the Colony Card phase.

New colony cards **attach to existing colony cards**, forming a connected graph. There is no grid or fixed layout -- the colony grows organically based on the player's choices.

### Colony Node
The colony exists as a special node on the map, connected to the Wilderness zone. Heroes deploy from the colony node and must return to it to deposit gathered resources.

## 6.2 Colony Production
Each turn during the Colony Card phase, the colony produces resources:
- **Base production**: 2 food per turn (from Basic Burrow)
- **Additional production**: Colony cards can increase food production and produce other resource types
- Production occurs automatically at the start of each turn

## 6.3 Colony Card Placement
During the Colony Card phase, the player may play **0 or 1 colony card** from their deck (or 2 if the Throne Room upgrade is active).

Colony cards are single-use: once played, they become a permanent part of the colony graph and are removed from the available deck for the rest of the run.

### Adjacency & Placement
Colony cards attach to existing colony cards. Some colony cards may have **placement requirements** (must be adjacent to a specific card type). The player chooses where in the colony graph to attach each new card.

*Specific adjacency requirements per card TBD -- Design Team.*

## 6.4 Key Colony Upgrades
The colony provides both passive and active benefits:
- **Food production** -- Keeps heroes fed and deployed
- **Fog piercing** -- Watchtower, Signal Tower reveal more of the map
- **Retargeting** -- Council Hall / Messenger Post allow redirecting heroes already on the map
- **Hero deployment** -- Forward Camp enables deploying from a forward position
- **Combat bonuses** -- Workshop, War Hall, Forge buff hero stats
- **Colony defense** -- Thorn Wall, Pit Traps, Wooden Gate protect against enemy attacks
- **Healing** -- Campfire, Shrine of Heroes accelerate hero recovery

---

# 7. Turn Structure

A game of Scurry has **no fixed turn limit**. However, once any hero enters the **Town zone** (zone 3), a **15-turn Pied Piper countdown** begins. When the countdown expires, the Pied Piper leaves his lair and moves directly to attack the colony. Each turn consists of seven phases executed in order:

## 7.1 Colony Card Phase
1. The colony produces resources (food and any other colony-generated resources are added to the stockpile)
2. The player may play **0 or 1 colony card** from their deck, attaching it to the colony graph
3. New colony effects take effect immediately

## 7.2 Hero Deployment Phase
1. The player may deploy **any number of hero tokens** from their available deck to the **colony node** (or Forward Camp node if that colony upgrade is active)
2. For each deployed hero, the player may attach **up to 3 equipment cards** from their deck:
   - 1 Offensive equipment (weapon)
   - 1 Defensive equipment (armor/shield/cloak)
   - 1 Utility equipment (tool/accessory)
3. For each deployed hero, the player **assigns a target node**:
   - If the target node is visible, the hero will auto-path toward it
   - If the target node is not visible (fog), the hero paths toward the closest visible node in that direction
   - The colony node is a valid target (hero stays at colony)
4. Heroes already on the **colony node** may be given **new target assignments**
5. Heroes elsewhere on the map retain their current target unless the player has the **Council Hall** or **Messenger Post** colony upgrade, which enables retargeting any deployed hero

## 7.3 Hero Movement Phase
- Each hero token moves toward its assigned target
- Movement distance = hero's **Move** stat (modified by equipment and colony bonuses)
- Movement is measured in **edges traversed** (1 Move = 1 edge = 1 node hop)
- Heroes stop moving when they reach their target, run out of Move, or enter a node with enemy tokens
- **A hero entering a node with enemy tokens stops immediately** (combat will resolve in the Combat phase)
- Heroes carrying resources move normally (no movement penalty for carrying)

## 7.4 Enemy Movement Phase
- All enemy tokens on the map move simultaneously
- **Default behavior**: Random patrol -- enemy selects a random connected edge and moves along it
  - **Zone preference**: Edges that cross zone boundaries are selected at **half the probability** of same-zone edges (enemies prefer to stay in their zone)
- **Aggro behavior**: If an enemy is **adjacent to a hero token** (connected by one edge), the enemy moves to that hero's node to attack
- **Colony aggro**: If an enemy is **adjacent to the colony node**, the enemy can see the colony and will move to attack it
- Enemy movement is hidden from the player unless the destination node is visible

## 7.5 Combat Phase
Combat occurs on every node where both hero tokens and enemy tokens are present. For each such node:

1. **Pre-combat**: The player may play **Tactical cards** from their deck before combat begins
2. **Combat round**: All tokens on the node participate
   a. **Pool combat strength**: Sum all hero Combat values on one side, all enemy Strength values on the other
   b. **Compare**: The side with lower total takes damage equal to the difference
   c. **Apply damage**: Damage is applied to the **lowest-Combat token first** on the losing side. If combat values are tied, damage is applied to the **lowest-Initiative token first**
   d. **Overflow**: If damage exceeds a token's remaining HP, the excess carries to the next-lowest-Combat token
   e. Tokens reduced to **0 HP** are removed from the node:
      - Hero tokens become **injured** -- removed from map, equipment returns to deck, carried resources dropped on the node
      - Enemy tokens are **defeated** -- respawn on a random unoccupied node in their zone
3. **Between rounds**: The player may play additional **Tactical cards**
4. **Repeat**: Combat rounds continue until only one side remains on the node
5. **Resolution**: If heroes survive, they remain on the node. If all heroes are defeated, all carried resources are dropped.

### Combat Example
*3 heroes (Combat 3, 2, 1) vs 2 enemies (Strength 4, 2). Hero total: 6. Enemy total: 6. Tied -- damage applied to lowest-Initiative token on each side. If Hero(Combat 1, Initiative 3) and Enemy(Strength 2, Initiative 2): enemy takes 0 damage first (tie rules), hero takes 0 damage. Next round, player plays Rally the Pack (+1 Combat to all heroes). Hero total: 9 vs Enemy total: 6. Enemies take 3 damage, applied to the Strength 2 enemy first.*

*Detailed combat resolution TBD -- Design Team. The above reflects directional intent; exact damage formulas need playtesting.*

## 7.6 Gather Phase
- Each hero token on a node with available resources may **gather resources** up to their **Carry capacity**
- Gathered resources are held by the hero token -- they are NOT yet in the colony stockpile
- Resources must be **physically carried back to the colony node** to be deposited into the stockpile
- When a hero carrying resources reaches the colony node, carried resources are automatically deposited
- If a carrying hero is injured or starved, carried resources are **dropped on the node** the hero occupied
- Dropped resources can be picked up by any hero that visits that node later

## 7.7 Clean-up Phase
1. **Food consumption**: The colony pays **1 food per hero token currently on the map** (not at colony -- heroes at the colony node are also on the map and consume food)
2. **Starvation**: If the colony cannot pay the full food cost:
   - The player chooses which heroes to feed (if insufficient food)
   - Unfed heroes are **removed from the map and injured** (returned to available pool)
   - Unfed heroes drop any carried resources on their current node
3. **Injury recovery**: Injured heroes are returned to the **available pool** and can be redeployed on the next turn
   - Heroes injured this turn are available for redeployment next turn
   - Colony upgrades (Campfire, Shrine of Heroes) may modify recovery mechanics
4. **Enemy respawn check**: Any defeated enemies that haven't yet respawned are placed on random unoccupied nodes in their zone
5. **Turn counter advances**

---

# 8. Combat System (Detailed)

## 8.1 Overview
Combat in Scurry is **automatic** once it begins. The player's influence comes from:
- **Strategic positioning** -- Choosing where to send heroes, how many to group together
- **Equipment** -- Attached at deployment, modifying hero stats
- **Tactical cards** -- Played before and between combat rounds

The player does NOT control individual hero actions during combat. Combat resolves round by round until one side is eliminated on the node.

## 8.2 Damage Resolution
Each combat round:
1. Sum hero Combat on the node = **Hero Strength**
2. Sum enemy Strength on the node = **Enemy Strength**
3. The **weaker side** takes damage equal to the difference
4. Damage is applied token-by-token, starting with the **lowest-Combat** token on the losing side
5. **Ties in Combat**: Damage goes to the token with **lower Initiative** first
6. **Exact ties** (same Combat and Initiative): TBD -- Design Team (coin flip, or alphabetical, or simultaneous)
7. Damage **carries over**: If a token takes lethal damage, excess flows to the next target
8. Tokens at **0 HP** are removed immediately (mid-round)
9. After removals, if both sides still have tokens, a new round begins

## 8.3 Persistent Damage
Hero HP damage **persists across combat rounds AND across turns**. A hero with 5 max HP who takes 2 damage in combat ends the turn at 3 HP. They remain at 3 HP on the next turn unless healed.

This makes healing (via Tactical cards, colony upgrades, or equipment like Healing Herb Kit) strategically important.

## 8.4 Tactical Card Timing
Tactical cards can be played:
- **Before the first combat round** on a node (after seeing enemy composition)
- **Between any two combat rounds** on the same node
- **NOT during** a round (rounds resolve automatically)

Multiple tactical cards can be played in the same window. Tactical cards are **single-use** -- once played, they are removed from the game for the rest of the run.

*Note: Tactical cards played during combat affect only the combat they are played in (unless the card text specifies otherwise, e.g., Pack Unity affects all heroes on the map).*

---

# 9. Resource & Logistics

## 9.1 Food Economy
Food is the critical survival resource:
- **Production**: Colony produces food each turn (base 2, increased by colony cards)
- **Consumption**: 1 food per hero token on the map per turn
- **Deficit**: Unfed heroes are injured and removed

The food economy creates the core tension: deploying more heroes lets you explore faster and fight harder, but each hero costs 1 food per turn. The player must balance colony food production against the number of deployed heroes.

### Food Math Example
*Turn 3: Colony has Basic Burrow (2 food) + Underground Storage (1 food) = 3 food/turn. Player has 4 heroes on the map. Cost: 4 food. Deficit: 1. Player must either recall a hero, play Emergency Rations, or accept 1 hero being starved.*

## 9.2 Carry Capacity & Transport
Heroes have a **Carry** stat determining how many resource units they can hold simultaneously. Resources are gathered during the Gather phase and must be physically transported back to the colony node.

### Logistics Decisions
- **Dedicated gatherers**: High-Carry heroes (Forager, Quartermaster) can haul large loads but may be weak in combat
- **Escort missions**: Protect resource-laden heroes with combat heroes
- **Dropped loot**: Injured heroes drop resources -- creating recovery missions
- **Round trips**: Heroes must travel to resource-rich nodes and back, consuming multiple turns

## 9.3 Resource Persistence
- Resources on nodes are **placed at map generation** and do not regenerate
- Gathered resources are held by hero tokens until deposited at colony
- Dropped resources remain on their node indefinitely and can be picked up by any hero
- Colony stockpile resources persist across turns

---

# 10. Enemies & Bosses

## 10.1 Enemy Tokens
Enemy tokens are placed on the map at generation. They have:
- **Strength** -- Combat value (equivalent to hero Combat)
- **HP** -- Hit points
- **Speed** -- Movement per turn (in edges)
- **Behavior** -- Patrol (random) or Chase (aggro range)
- **Zone** -- Which zone they belong to (determines respawn location)

### Enemy Respawn
When an enemy token is defeated, it **respawns on a random unoccupied node** within its zone. Nodes are never permanently cleared -- enemies represent an ongoing territorial threat.

## 10.2 Zone 1 -- The Wilderness

| Name | Strength | HP | Speed | Behavior |
|------|----------|-----|-------|----------|
| Field Mouse | 1 | 2 | 2 | Patrol |
| Grass Snake | 2 | 3 | 3 | Chase |
| Hawk Scout | 3 | 4 | 4 | Ambush |
| Badger | 4 | 6 | 1 | Guard |

**Zone Boss -- Elder Silas, the Great Horned Owl**
A massive, ancient owl who hunts the meadows.
- Strength: 8 | HP: 20 | Speed: 2
- **Ability**: Swoops -- targets the highest-Carry hero first (tries to intercept gatherers)
- Roams the Wilderness zone. Does not respawn if defeated.

## 10.3 Zone 2 -- The Farmland

| Name | Strength | HP | Speed | Behavior |
|------|----------|-----|-------|----------|
| Farm Cat | 3 | 4 | 3 | Chase |
| Rat Trap | 4 | 5 | 0 | Ambush |
| Terrier | 5 | 6 | 4 | Chase |
| Farmhand | 3 | 5 | 2 | Patrol |

**Zone Boss -- Head Farmer Tobias & Duchess**
Tobias is a cunning old farmer. Duchess is his prize-winning mouser.
- Combined Strength: 10 | HP: 35 (Tobias 20 / Duchess 15) | Speed: 2
- **Ability**: Duchess must be defeated first -- she intercepts damage aimed at Tobias.
- Roams the Farmland zone. Does not respawn if defeated.

## 10.4 Zone 3 -- The Town

| Name | Strength | HP | Speed | Behavior |
|------|----------|-----|-------|----------|
| Guild Apprentice | 4 | 5 | 3 | Patrol |
| Alley Cat | 5 | 5 | 5 | Chase |
| Rat-Catcher | 6 | 7 | 3 | Chase |
| Poison Trap | 7 | 8 | 0 | Ambush |

**Zone Boss -- Guildmaster Aldric Fenn**
Leader of the Rat-Catcher Guild.
- Strength: 12 | HP: 50 | Speed: 1
- **Ability**: Summons -- each round, if Guild Apprentice enemies are on adjacent nodes, they move to join the fight.
- Roams the Town zone. Does not respawn if defeated.

## 10.5 The Final Boss -- The Pied Piper
The Pied Piper waits at the **final node** at the top of the map. He is **static** -- he does not move.

- Strength: 15 | HP: 80
- The Piper node is connected to 2-3 Town nodes.

### Phases
The Piper fight uses a phase system based on remaining HP:

| Phase | HP Range | Effect |
|---|---|---|
| 1 -- The Melody | 80-61 | All heroes on the node lose 1 Combat (passive aura). |
| 2 -- Charmed Summons | 60-41 | Each round, 1 hero token is **charmed** (switches sides for 1 round). |
| 3 -- The High Note | 40-21 | Deals 10 damage to all heroes simultaneously (one-time trigger on entering this phase). |
| 4 -- Discordant Finale | 20-0 | Strength increases to 20. |

- **Victory**: The Piper is defeated. The Pack is free. Run complete.
- The Piper does not respawn.

*Phase effects and Strength values TBD -- Design Team. Values above are starting points for playtesting.*

## 10.6 Enemy Placement at Map Generation
At map generation, enemy tokens are distributed across the map:
- **Wilderness**: Lower density, weaker enemies. ~4-6 regular enemies + 1 zone boss.
- **Farmland**: Moderate density, mid-strength enemies. ~5-7 regular enemies + 1 zone boss.
- **Town**: High density, strong enemies. ~6-8 regular enemies + 1 zone boss.
- **Colony node**: Always starts empty.
- **Pied Piper node**: The Piper only.

*Exact enemy counts and distribution TBD -- Design Team.*

---

# 11. Win Conditions & Loss Conditions

## 11.1 Victory
The player wins by **defeating the Pied Piper** — either at his lair on the final map node, or when he marches on the colony.

## 11.2 Defeat
The player loses if either condition is met:
- **Colony falls** -- The Pied Piper (or other enemies) reaches the colony node and defeats all hero defenders. If the colony has no defenders, any enemy at the colony node triggers defeat.
- **All heroes down** -- Every hero is injured with none available to deploy.

## 11.3 Pied Piper Countdown
Once any hero enters the **Town zone** (zone 3), a **15-turn countdown** begins. When the countdown reaches zero, the Pied Piper abandons his lair and moves directly to the colony node. If the player has not defeated him by then, they must defend the colony against the Pied Piper's full-strength assault. This creates escalating pressure: players can take their time building up in the early zones, but once they push into Town territory, the clock starts ticking.

## 11.4 Scoring
| Factor | Effect on Score |
|---|---|
| Turns taken (fewer = better) | Major multiplier |
| Starting deck size (smaller = better) | Major multiplier |
| Enemies defeated | Bonus points |
| Resources gathered | Bonus points |
| Colony cards played | Bonus points |
| Zone bosses defeated | Large bonus |
| Heroes injured (fewer = better) | Minor bonus |

*Exact formula TBD -- Design Team.*

---

# 12. Meta-Progression (Runs Across Sessions)

## 12.1 The Rattery (Unlockable Cards)
- Winning a run with a particular hero card unlocks a new, more powerful version for future deck construction
- Defeating a zone boss for the first time unlocks new equipment cards in the pool

## 12.2 Colony Reputation
- At the end of each run (win or lose), the player earns Reputation based on score
- Reputation is a persistent currency for unlocking new cards and cosmetics

## 12.3 The Scrapbook
- A flavour-text journal that fills in automatically as the player discovers new enemies, clears nodes, and finds events
- Purely cosmetic -- a satisfying collection goal

## 12.4 Leaderboard
- Online leaderboard ranked by score
- Separate categories for different deck sizes (10-15, 16-20, 21-25, 26-30)

*Meta-progression details TBD -- Design Team.*

---

# 13. User Interface & Visual Direction

## 13.1 Visual Style
Warm, hand-drawn cartoon aesthetic inspired by illustrated children's books. Soft pencil outlines, watercolour-adjacent textures, and exaggerated character proportions.
- Colour palette: warm browns, forest greens, harvest golds, and soft creams -- with pops of red for danger.
- Characters: anthropomorphic rats with distinct silhouettes and personality-driven idle animations.
- Environments: lush, layered backgrounds that breathe life into each zone.

## 13.2 Card Visual Design
- Cards resemble aged parchment with hand-inked borders appropriate to their type.
- **Hero cards**: Portrait of the rat character with stat icons (Combat, Move, HP, Carry) below.
- **Colony cards**: Illustration of the infrastructure with effect text.
- **Equipment cards**: Vignette illustration of the item with slot icon (sword/shield/tool) and stat bonuses.
- **Tactical cards**: Action illustration with effect text.
- Rarity communicated via border colour: Common (brown), Uncommon (green), Rare (blue), Legendary (gold).
- Deck cost displayed as pips in the upper-left corner.

## 13.3 Key Screens
1. **Main Menu** -- New Run, Continue Run, Collection (Scrapbook/Rattery), Leaderboard, Settings
2. **Deck Construction** -- Full card pool browser, deck builder, deck cost/size display, score multiplier preview
3. **Map View** -- The main gameplay screen. Fog-shrouded graph map with zone colouring, hero tokens, visible enemies, colony node, resource indicators. Turn counter and phase indicator prominent.
4. **Colony View** -- Freeform graph view of the colony. Shows all placed colony cards, production stats, active effects. Loaded as a dedicated additive scene during Colony Card phase.
5. **Deployment View** -- Hero selection, equipment attachment, target assignment. Loaded as a dedicated additive scene during Hero Deployment phase.
6. **Combat View** -- Zoomed-in node showing all participating tokens, HP bars, combat strength totals. Tactical card hand displayed at bottom. Round counter. Loaded as a dedicated additive scene during Combat phase.
7. **Run End** -- Victory/defeat screen with score breakdown, turn count, deck size, achievements, meta-progression rewards.

## 13.4 Map View Detail
The map is the primary gameplay screen:
- **Visible nodes** shown with zone-appropriate icons and colours (green circles for Wilderness, brown hexagons for Farmland, blue stars for Town)
- **Fog nodes** shown as dark silhouettes or question marks
- **Hero tokens** displayed on their current nodes with HP bars
- **Enemy tokens** shown on visible nodes; hidden in fog
- **Resources** shown as small icons on visible nodes
- **Colony node** always visible at bottom with production summary
- **Pied Piper node** known position at top but details hidden until adjacent
- **Phase indicator** shows current turn phase with action prompts
- **Turn counter** shows current turn out of 15
- **Edge connections** shown as paths between nodes; zone-crossing edges visually distinct

---

# 14. Technical Specifications

## 14.1 Unity 6 Architecture
- Render Pipeline: Universal Render Pipeline (URP) 2D
- UI System: Programmatic Unity UI (Canvas) for HUD, panels, and menus. World-space elements for map and colony views.
- Save System: JSON serialisation to Application.persistentDataPath
- Card Data: ScriptableObject-based card definitions
- Map Logic: Graph-based node/edge data structure; A* or Dijkstra pathfinding for hero movement
- Event System: Static EventBus with Action delegates
- Localisation: ScriptableObject-based localisation tables (5 languages)
- Map Generation: Procedural graph generation with zone connectivity constraints

## 14.2 Scene Architecture

The game uses a multi-scene architecture with persistent managers surviving across scene transitions via `DontDestroyOnLoad`.

| Build Index | Scene | Purpose |
|---|---|---|
| 0 | **Bootstrap** | Creates PersistentManagers root (RunManager, ColonyManager, GameSettings, RelicManager, AchievementManager, MetaProgressionManager, LocalizationManager, EventSystem, PersistentCanvas). Loads MainMenu on startup. |
| 1 | **MainMenu** | Title screen, New Run / Continue buttons, Collection, Leaderboard. |
| 2 | **DeckConstruction** | Deck building screen. Full card pool, deck builder, validation, score preview. |
| 3 | **GameMap** | Primary gameplay scene. Map view, turn management, all 7 phases. Hub scene for additive UI scenes. |
| 4 | **Colony** | Loaded **additively** on GameMap during Colony Card phase. Colony graph view, card placement, production summary. |
| 5 | **Deployment** | Loaded **additively** on GameMap during Hero Deployment phase. Hero selection, equipment attachment, target assignment. |
| 6 | **Combat** | Loaded **additively** on GameMap during Combat phase. Combat view, tactical card play, round resolution. |
| 7 | **RunResult** | Victory/defeat screen. Score breakdown, meta-progression rewards. |

### Scene Transition Flow
```
Bootstrap -> MainMenu
MainMenu -> DeckConstruction (via New Run)
DeckConstruction -> GameMap (via Start Run)
GameMap + Colony (additive, during Colony phase)
Colony unloads -> GameMap continues
GameMap + Deployment (additive, during Deploy phase)
Deployment unloads -> GameMap continues
GameMap + Combat (additive, during Combat phase)
Combat unloads -> GameMap continues
GameMap -> RunResult (via Victory or Defeat)
RunResult -> DeckConstruction (New Run) or MainMenu (Main Menu)
MainMenu -> GameMap (via Continue Run, loads save)
```

### Removed Scenes
The following scenes from v1.0 are no longer needed:
- **ColonyDraft** -- Replaced by DeckConstruction
- **ColonyManagement** -- Replaced by Colony (additive on GameMap)
- **MapTraversal** -- Merged into GameMap
- **Encounter** -- Replaced by Combat (additive on GameMap)

## 14.3 Data Architecture -- Key Systems

### ScriptableObjects
- **CardDefinitionSO** -- Base card data. Updated to include: card type (Hero/Colony/Equipment/Tactical), deck cost, rarity, stats, effect text, equipment slot (if applicable), card image reference.
- **HeroCardSO** (extends CardDefinitionSO) -- Hero-specific: Combat, Move, HP, Carry, Initiative, special ability.
- **ColonyCardSO** (extends CardDefinitionSO) -- Colony-specific: production bonus, effect type, placement requirements, tier.
- **EquipmentCardSO** (extends CardDefinitionSO) -- Equipment-specific: slot type (Offensive/Defensive/Utility), stat bonuses, special effect.
- **TacticalCardSO** (extends CardDefinitionSO) -- Tactical-specific: effect, target scope (node/map/hero), timing restrictions.
- **EnemyDefinitionSO** -- Enemy data: Strength, HP, Speed, behavior type, zone.
- **BossDefinitionSO** (extends EnemyDefinitionSO) -- Boss-specific: phases, abilities, rewards.
- **MapConfigSO** -- Map generation parameters: nodes per zone, edge density, zone crossing count, resource distribution.

### Runtime Systems
- **MapGraph** -- Runtime graph data structure: nodes, edges, token positions, fog state, resource state.
- **TurnManager** -- Orchestrates the 7 phases of each turn. Drives phase transitions and turn counting.
- **ColonyGraph** -- Runtime colony structure: placed cards, production calculation, active effects.
- **DeckManager** -- Manages the player's constructed deck: available cards, deployed cards, removed cards (played tacticals, played colony cards).
- **CombatResolver** -- Handles combat rounds: strength pooling, damage distribution, tactical card application, token removal.
- **EnemyAI** -- Enemy movement: random patrol with zone-preference weighting, aggro detection, colony detection.
- **FogOfWar** -- Visibility calculation based on hero positions, colony upgrades, and equipment effects.
- **PathfindingService** -- Graph-based shortest-path calculation for hero auto-movement.
- **ResourceManager** -- Tracks colony stockpile, hero-carried resources, node resource state.
- **EventBus** -- Static event hub for decoupled communication.
- **SaveManager** -- JSON serialisation of full game state (map, tokens, colony, deck, turn).

### Key Namespaces
- `Scurry.Core` -- TurnManager, RunManager, EventBus, SaveManager
- `Scurry.Data` -- ScriptableObjects, enums, save data
- `Scurry.Map` -- MapGraph, MapGenerator, FogOfWar, PathfindingService
- `Scurry.Colony` -- ColonyGraph, ColonyManager
- `Scurry.Cards` -- DeckManager, CardDefinitions
- `Scurry.Combat` -- CombatResolver, DamageCalculator
- `Scurry.AI` -- EnemyAI, HeroMovement
- `Scurry.UI` -- All UI managers and views
- `Scurry.Logistics` -- ResourceManager, CarrySystem

## 14.4 Persistent Managers (DontDestroyOnLoad)
- **RunManager** -- Master orchestrator, scene loading, run state
- **ColonyManager** -- Colony graph, production, effects
- **GameSettings** -- Battle speed, accessibility settings
- **AchievementManager** -- Achievements, Steam integration
- **MetaProgressionManager** -- Cross-run progression, reputation, unlocks
- **LocalizationManager** -- Multi-language string lookup

---

# 15. Roadmap & Milestones

## M1 -- v2.0 Core Systems (Current)
Rebuild the game around the new strategic structure.

### M1.1 -- Card System Overhaul
- New ScriptableObjects: HeroCardSO, ColonyCardSO, EquipmentCardSO, TacticalCardSO
- Define all 120 cards with stats, costs, effects
- Deck construction validation (10-30 cards, copy limits)

### M1.2 -- Map Generation
- Graph-based map generator (30 nodes, 3 zones)
- Zone connectivity constraints (at least 2 cross-zone edges)
- Resource placement per zone
- Enemy token placement
- Colony and Pied Piper node placement

### M1.3 -- Fog of War
- Visibility system based on hero positions
- Colony upgrade visibility bonuses
- Equipment visibility bonuses
- Node memory (visited nodes stay partially visible)

### M1.4 -- Turn System
- TurnManager with 7-phase orchestration
- Colony Card phase (play colony cards, produce resources)
- Hero Deployment phase (deploy heroes, attach equipment, assign targets)
- Hero Movement phase (auto-pathing toward targets)
- Enemy Movement phase (patrol + aggro + colony detection)
- Combat phase (auto-resolve with tactical card windows)
- Gather phase (resource collection by heroes)
- Clean-up phase (food consumption, starvation, injury recovery, respawn)

### M1.5 -- Combat System
- Pooled combat strength resolution
- Damage cascade (lowest-combat first, initiative tie-breaking)
- Persistent damage across rounds and turns
- Tactical card integration (play before/between rounds)
- Enemy respawn on defeat

### M1.6 -- Colony System
- Freeform colony graph
- Colony card placement and attachment
- Production calculation
- Colony upgrade effects (retargeting, fog piercing, forward deployment, combat bonuses, defenses)

### M1.7 -- Resource & Logistics
- Hero carry capacity
- Resource transport (carry to colony to deposit)
- Dropped resources on hero defeat
- Colony stockpile management
- Food consumption and starvation

### M1.8 -- Deck Construction Screen
- Card pool browser
- Deck builder with validation
- Copy limit enforcement
- Score preview (deck size multiplier)

### M1.9 -- Win/Loss & Scoring
- Victory detection (Pied Piper defeated)
- Defeat detection (turn timer, colony fall)
- Score calculation
- Run result screen

## M2 -- Content & Balance
- All 120 cards fully statted and balanced
- All enemy stat balancing
- Zone boss tuning
- Pied Piper fight tuning
- Scoring formula finalization

## M3 -- Polish & Release
- Art and audio integration (120 card images ready)
- UI polish and animation
- Save/load for new game structure
- Accessibility features
- Steam integration
- Meta-progression implementation
- Leaderboard
- Localization updates
- Platform testing

---

# 16. Resolved Design Decisions

The following decisions were resolved during the v2.0 design process:

1. **Game structure**: Single persistent map per run with no fixed turn limit. Pied Piper countdown (15 turns) begins when Town zone entered. No discrete levels or level transitions. (v1.0: 3 separate levels with colony rebuild each level.)
2. **Colony system**: Freeform graph that grows incrementally. Starts with Entrance + Basic Burrow. One colony card played per turn. (v1.0: Grid-based colony rebuilt from scratch each level.)
3. **Map type**: Procedural graph with 30 nodes across 3 zones. Fog of war. (v1.0: Per-level Slay the Spire branching maps, fully visible.)
4. **Deck construction**: Pre-game constructed deck of 10-30 cards. All card types in one deck. (v1.0: Colony draft of 8 from 12 at run start; hero cards acquired during run.)
5. **Card multiples**: 1-cost = 3 copies, 2-cost = 2 copies, 3+ cost = 1 copy.
6. **Hero deployment**: Heroes deploy from colony as tokens on the map. Assign targets at deployment. Auto-path toward targets. (v1.0: Heroes auto-deployed per encounter.)
7. **Hero retargeting**: Heroes at colony can always be retargeted. Heroes elsewhere require Council Hall or Messenger Post colony upgrade.
8. **Stay command**: Current node is a valid target, allowing "guard this position" orders.
9. **Equipment slots**: 3 slots per hero -- Offensive, Defensive, Utility. Legendary items occupy Offensive or Defensive slot.
10. **Combat resolution**: Pooled strength per side. Damage to lowest-combat first, ties broken by lowest initiative. Persistent damage across rounds and turns. (v1.0: Individual hero vs enemy combat with wound/exhaustion system.)
11. **Tactical cards**: Played by the player during combat phase, before and between rounds. Single-use (removed from game after play). (v1.0: Auto-triggered hero benefit cards.)
12. **Food consumption**: 1 food per hero on map per turn. Colony base production: 2 food/turn. Unfed heroes injured and removed. (v1.0: Population-based consumption per map node.)
13. **Enemy movement**: Random patrol with 0.5x weight on zone-crossing edges. Aggro if adjacent to hero. Colony aggro if adjacent to colony node. (v1.0: Enemies only existed within encounters.)
14. **Enemy respawn**: Defeated enemies respawn on random unoccupied node in their zone. (v1.0: No respawn; encounters were discrete.)
15. **Resources**: Must be physically carried back to colony. Dropped on hero defeat. (v1.0: Automatically added to stockpile on gathering.)
16. **Node resources**: Placed at generation, no regeneration. Wilderness 2-4, Farmland 1-3, Town 0-2.
17. **Win condition**: Defeat the Pied Piper (static at final node).
18. **Loss conditions**: Turn 15 expires OR enemies defeat all heroes on colony node.
19. **Scoring**: Fewer turns + smaller deck = higher score.
20. **Zone bosses**: Roaming powerful enemies in their zones. Do not respawn if defeated. (v1.0: Static boss at end of each level map.)
21. **Pied Piper**: Static at final node. Phase-based fight. Does not respawn.
22. **Combat continuation**: Combat continues until one side is eliminated on the node. No recall button. (v1.0: Recall button for resource encounters.)

---

# Appendix A: Card Image Reference

120 card images exist at `Assets/Resources/Card Images/` (001.png through 120.png). Mapping:

| Images | Category | Count |
|---|---|---|
| 001-020 | Hero Cards | 20 |
| 021-030 | Colony: Food & Storage | 10 |
| 031-040 | Colony: Structure & Defense | 10 |
| 041-050 | Colony: Advanced Upgrades | 10 |
| 051-060 | Equipment: Weapons (Offensive) | 10 |
| 061-070 | Equipment: Armor (Defensive) | 10 |
| 071-080 | Equipment: Utility | 10 |
| 081-090 | Equipment: Legendary | 10 |
| 091-100 | Tactical: Combat | 10 |
| 101-110 | Tactical: Support | 10 |
| 111-120 | Tactical: Power | 10 |
