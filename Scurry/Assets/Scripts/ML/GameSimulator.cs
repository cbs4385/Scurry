using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scurry.Core;
using Scurry.Data;
using Scurry.Map;
using Scurry.Colony;
using Scurry.Combat;
using Scurry.AI;

namespace Scurry.ML
{
    /// <summary>
    /// Headless game simulator that runs a complete game without Unity's MonoBehaviour/coroutine overhead.
    /// Reuses all existing game logic (MapGenerator, CombatResolver, CleanupPhase, etc.) but
    /// drives the turn loop synchronously. Decisions are made by DecisionWeights instead of player input.
    /// </summary>
    public class GameSimulator
    {
        private const int MAX_TURNS = 60; // Safety valve for simulations — generous but prevents runaway loops
        private const int PIPER_COUNTDOWN = 15; // Turns after Town zone entered before Pied Piper marches
        private const int STALL_TURNS = 10; // End sim if no combat, no deployment, no zone progress for this many turns

        // Shared across all simulations (loaded once on main thread)
        private readonly IReadOnlyList<CardDefinitionSO> allCards;
        private readonly IReadOnlyList<ColonyCardDefinitionSO> allColonyCards;
        private readonly IReadOnlyList<EnemyDefinitionSO> allEnemies;
        private readonly MapConfigSO mapConfig;

        // Cached balance config values (avoid Resources.Load on background thread)
        private readonly int cachedStartingFood;
        private readonly int cachedStartingMaterials;
        private readonly int cachedStartingCurrency;
        private readonly int cachedStarvationDamage;
        private readonly float cachedEnemySpawnChance;
        private readonly int cachedBossesRequiredForPiper;
        private readonly int cachedBaseFoodProduction;
        // Colony card costs by tier: (food, materials)
        private readonly int cachedColonyFoodStorageFoodCost;
        private readonly int cachedColonyFoodStorageMatCost;
        private readonly int cachedColonyStructDefFoodCost;
        private readonly int cachedColonyStructDefMatCost;
        private readonly int cachedColonyAdvancedFoodCost;
        private readonly int cachedColonyAdvancedMatCost;
        // Food scaling
        private readonly int cachedFoodBonusPerNHeroes;
        private readonly bool cachedHeroesAtColonyFreeFood;
        // Difficulty scaling
        private readonly DifficultyLevel cachedDifficulty;
        private readonly float cachedEnemyStrengthMultiplier;

        public GameSimulator(
            IReadOnlyList<CardDefinitionSO> cards,
            IReadOnlyList<ColonyCardDefinitionSO> colonyCards,
            IReadOnlyList<EnemyDefinitionSO> enemies,
            MapConfigSO config,
            DifficultyLevel difficulty = DifficultyLevel.Normal)
        {
            allCards = cards;
            allColonyCards = colonyCards;
            allEnemies = enemies;
            mapConfig = config;
            cachedDifficulty = difficulty;

            // Cache balance config on construction (must be on main thread)
            var bc = BalanceConfigSO.Instance;
            cachedStartingFood = bc != null ? bc.startingFood : 20;
            cachedStartingMaterials = bc != null ? bc.startingMaterials : 5;
            cachedStartingCurrency = bc != null ? bc.startingCurrency : 5;
            cachedStarvationDamage = bc != null ? bc.starvationDamagePerFood : 2;
            cachedBossesRequiredForPiper = bc != null ? bc.bossesRequiredForPiper : 2;
            cachedBaseFoodProduction = bc != null ? bc.baseFoodProduction : 3;
            cachedColonyFoodStorageFoodCost = bc != null ? bc.colonyFoodStorageFoodCost : 1;
            cachedColonyFoodStorageMatCost = bc != null ? bc.colonyFoodStorageMaterialsCost : 1;
            cachedColonyStructDefFoodCost = bc != null ? bc.colonyStructureDefenseFoodCost : 1;
            cachedColonyStructDefMatCost = bc != null ? bc.colonyStructureDefenseMaterialsCost : 2;
            cachedColonyAdvancedFoodCost = bc != null ? bc.colonyAdvancedFoodCost : 1;
            cachedColonyAdvancedMatCost = bc != null ? bc.colonyAdvancedMaterialsCost : 2;

            // Food scaling
            cachedFoodBonusPerNHeroes = bc != null ? bc.GetFoodBonusPerNHeroes() : 3;
            cachedHeroesAtColonyFreeFood = bc != null ? bc.heroesAtColonyFreeFood : true;

            // Difficulty-aware values
            cachedEnemyStrengthMultiplier = bc != null ? bc.GetEnemyStrengthMultiplier() : 1f;
            cachedEnemySpawnChance = bc != null ? bc.GetEnemySpawnChance() : 0.65f;

            // Easy mode starting bonuses
            if (difficulty == DifficultyLevel.Easy && bc != null)
            {
                cachedStartingFood += bc.easyBonusFood;
                cachedStartingMaterials += bc.easyBonusMaterials;
            }
        }

        /// <summary>
        /// Returns (foodCost, materialsCost) for placing a colony card of the given tier.
        /// </summary>
        private (int food, int materials) GetColonyCardCost(ColonyTier tier)
        {
            return tier switch
            {
                ColonyTier.FoodStorage => (cachedColonyFoodStorageFoodCost, cachedColonyFoodStorageMatCost),
                ColonyTier.StructureDefense => (cachedColonyStructDefFoodCost, cachedColonyStructDefMatCost),
                ColonyTier.Advanced => (cachedColonyAdvancedFoodCost, cachedColonyAdvancedMatCost),
                _ => (0, 0)
            };
        }

        /// <summary>
        /// Picks a random reward card weighted by zone (Wilderness=Common, Farmland=Uncommon, Town=Rare+).
        /// </summary>
        private CardDefinitionSO PickRandomRewardCard(NodeType zone, int rewardSeed)
        {
            if (allCards.Count == 0) return null;

            CardRarity preferred = zone switch
            {
                NodeType.Wilderness => CardRarity.Common,
                NodeType.Farmland => CardRarity.Uncommon,
                NodeType.Town => CardRarity.Rare,
                _ => CardRarity.Common
            };

            var rng = new System.Random(rewardSeed);
            float totalWeight = 0;
            var weights = new List<float>();
            foreach (var card in allCards)
            {
                float w = card.rarity == preferred ? 3f : 1f;
                weights.Add(w);
                totalWeight += w;
            }

            if (totalWeight <= 0) return null;

            float roll = (float)(rng.NextDouble() * totalWeight);
            float cumulative = 0;
            for (int i = 0; i < allCards.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative) return allCards[i];
            }
            return allCards[allCards.Count - 1];
        }

        /// <summary>
        /// Runs a complete game and returns the result. Fully deterministic for a given seed + weights.
        /// </summary>
        public SimulationResult Simulate(DecisionWeights weights, int seed)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            const long MAX_SIM_MS = 500; // Kill any simulation taking >500ms

            SeededRandom.Initialize(seed);

            // ── Build deck ──────────────────────────────────────────────
            var deckBuilder = new SimulatedDeckBuilder(allCards, allColonyCards);
            var (heroDeck, colonyDeck, equipDeck, tacticalDeck) = deckBuilder.BuildDeck(weights);

            int totalDeckSize = heroDeck.Count + colonyDeck.Count + equipDeck.Count + tacticalDeck.Count;

            // ── Generate map ────────────────────────────────────────────
            MapGraph mapGraph = MapGenerator.GenerateMap(mapConfig, seed);
            mapGraph.BossesRequiredForPiper = cachedBossesRequiredForPiper;
            FogOfWar fogOfWar = new FogOfWar();

            // ── Initialize colony ───────────────────────────────────────
            ColonyGraph colony = new ColonyGraph();
            var entranceDef = allColonyCards.FirstOrDefault(c => c.cardName == "Entrance");
            var burrowDef = allColonyCards.FirstOrDefault(c => c.cardName == "Basic Burrow");
            if (entranceDef != null && burrowDef != null)
                colony.Initialize(entranceDef, burrowDef);
            else
                colony.Initialize();

            // ── Initialize resources ────────────────────────────────────
            int foodStockpile = cachedStartingFood;
            int materialsStockpile = cachedStartingMaterials;
            int currencyStockpile = cachedStartingCurrency;
            int foodStorageCapacity = 99;

            // ── Spawn enemies ───────────────────────────────────────────
            var enemyTokens = SpawnEnemies(mapGraph);

            // ── Game state ──────────────────────────────────────────────
            var availableHeroes = new List<CardDefinitionSO>(heroDeck);
            var availableColony = new List<ColonyCardDefinitionSO>(colonyDeck);
            var availableEquipment = new List<CardDefinitionSO>(equipDeck);
            var availableTactical = new List<CardDefinitionSO>(tacticalDeck);
            var deployedHeroes = new List<HeroToken>();
            var allHeroTokens = new List<HeroToken>();
            var injuredHeroes = new HashSet<int>();
            int heroTokenIdCounter = 0;

            // Stats
            int totalEnemiesDefeated = 0;
            int zoneBossesDefeated = 0;
            bool piedPiperDefeated = false;
            int totalResourcesGathered = 0;
            int colonyCardsPlayed = 0;
            var heroesEverInjured = new HashSet<int>();
            var zonesReached = new HashSet<NodeType> { NodeType.Colony };
            var nodesExplored = new HashSet<int> { mapGraph.ColonyNodeId };
            int totalHeroesDeployed = 0;
            int combatsWon = 0;
            int peakDeployedCount = 0;

            var combatResolver = new CombatResolver();
            var decisionMaker = new SimulatedDecisionMaker(weights);

            // Pied Piper countdown — triggered when Town zone first entered
            bool piperCountdownActive = false;
            int piperCountdownRemaining = -1;

            // Stall detection — end sim early if nothing is happening
            int lastProgressTurn = 0;
            int lastProgressEnemies = 0;
            int lastProgressZones = 0;
            int lastProgressDeploy = 0;

            // ── Turn loop ───────────────────────────────────────────────
            int turn = 0;
            bool runActive = true;
            bool victory = false;

            while (runActive && turn < MAX_TURNS)
            {
                // Bail out if this simulation is taking too long
                if (stopwatch.ElapsedMilliseconds > MAX_SIM_MS)
                {
                    runActive = false;
                    break;
                }

                turn++;
                colony.ResetTurnCardCount();

                // ── Colony Phase ────────────────────────────────────────
                if (colony.CanPlayCard() && availableColony.Count > 0)
                {
                    var cardToPlay = decisionMaker.ChooseColonyCard(
                        availableColony, colony, deployedHeroes.Count, turn,
                        foodStockpile, materialsStockpile, this);
                    if (cardToPlay != null)
                    {
                        // Check resource cost
                        var (foodCost, matCost) = GetColonyCardCost(cardToPlay.colonyTier);
                        if (foodStockpile >= foodCost && materialsStockpile >= matCost)
                        {
                            var placed = colony.GetPlacedCards();
                            if (placed.Count > 0)
                            {
                                int attachTo = placed.Keys.FirstOrDefault();
                                int placedId = colony.AddCard(cardToPlay, attachTo);
                                if (placedId >= 0)
                                {
                                    foodStockpile -= foodCost;
                                    materialsStockpile -= matCost;
                                    availableColony.Remove(cardToPlay);
                                    colonyCardsPlayed++;
                                }
                            }
                        }
                    }
                }

                // Colony production + scaling bonus from deployed heroes
                int foodProd = colony.CalculateFoodProduction();
                if (cachedFoodBonusPerNHeroes > 0)
                    foodProd += deployedHeroes.Count(h => !h.isInjured) / cachedFoodBonusPerNHeroes;
                foodStockpile = Mathf.Min(foodStockpile + foodProd, foodStorageCapacity);

                // Mushroom farm bonus
                if (colony.HasEffect(ColonyEffect.MushFoodProduction))
                {
                    int mushFood = colony.GetEffectValue(ColonyEffect.MushFoodProduction);
                    foodStockpile = Mathf.Min(foodStockpile + mushFood, foodStorageCapacity);
                }

                // Update storage capacity
                if (colony.HasEffect(ColonyEffect.FoodStorageCapacity))
                {
                    int bonusCap = colony.GetEffectValue(ColonyEffect.FoodStorageCapacity);
                    foodStorageCapacity = 99 + bonusCap;
                    foodStockpile = Mathf.Min(foodStockpile, foodStorageCapacity);
                }

                // ── Deploy Phase ────────────────────────────────────────
                if (turn >= weights.DeployStartTurn)
                {
                    int maxDeploy = decisionMaker.GetMaxDeploy(weights, foodProd, deployedHeroes.Count, turn);
                    int deployed = 0;

                    // Sort heroes by weights
                    var sortedHeroes = decisionMaker.SortHeroesForDeploy(availableHeroes, weights);
                    var targets = decisionMaker.GetDeployTargets(mapGraph, deployedHeroes, enemyTokens, turn, weights);

                    int groupSize = weights.HeroGroupSize;
                    int targetIndex = 0;
                    int heroesOnTarget = 0;

                    foreach (var heroDef in sortedHeroes)
                    {
                        if (deployed >= maxDeploy) break;

                        int targetNode = targets.Count > 0
                            ? targets[targetIndex % targets.Count]
                            : mapGraph.ColonyNodeId;

                        // Create hero token
                        var heroToken = new HeroToken(heroDef, heroTokenIdCounter++);
                        heroToken.currentNodeId = mapGraph.ColonyNodeId;
                        heroToken.targetNodeId = targetNode;

                        // Equip hero
                        decisionMaker.EquipHero(heroDef, heroToken, availableEquipment, weights);

                        deployedHeroes.Add(heroToken);
                        allHeroTokens.Add(heroToken);
                        availableHeroes.Remove(heroDef);

                        // Track on node
                        var colonyNode = mapGraph.GetNode(mapGraph.ColonyNodeId);
                        if (colonyNode != null)
                            colonyNode.heroTokenIds.Add(heroToken.tokenId);

                        deployed++;
                        totalHeroesDeployed++;
                        heroesOnTarget++;
                        if (heroesOnTarget >= groupSize)
                        {
                            targetIndex++;
                            heroesOnTarget = 0;
                        }
                    }

                    peakDeployedCount = Mathf.Max(peakDeployedCount, deployedHeroes.Count);
                }

                // ── Pre-move fog snapshot (for ambush detection) ─────────
                var preMoveNodeFog = new Dictionary<int, FogState>();
                foreach (var node in mapGraph.GetAllNodes())
                    preMoveNodeFog[node.nodeId] = node.fogState;

                // ── Hero Move Phase ─────────────────────────────────────
                var moveOrder = deployedHeroes
                    .Where(h => !h.isInjured)
                    .OrderByDescending(h => h.EffectiveInitiative)
                    .ToList();

                foreach (var hero in moveOrder)
                {
                    // Retarget if needed
                    hero.targetNodeId = decisionMaker.ChooseTarget(hero, mapGraph, colony, enemyTokens, turn, weights, deployedHeroes);

                    // Move toward target (cap at 20 to prevent runaway loops)
                    int movesLeft = Mathf.Min(hero.EffectiveMove, 20);
                    while (movesLeft > 0 && hero.currentNodeId != hero.targetNodeId)
                    {
                        var path = mapGraph.ShortestPath(hero.currentNodeId, hero.targetNodeId);
                        if (path == null || path.Count < 2) break;

                        int nextNode = path[1];

                        // Update node tracking
                        var fromNode = mapGraph.GetNode(hero.currentNodeId);
                        fromNode?.heroTokenIds.Remove(hero.tokenId);

                        hero.currentNodeId = nextNode;

                        var toNode = mapGraph.GetNode(nextNode);
                        toNode?.heroTokenIds.Add(hero.tokenId);
                        if (toNode != null)
                        {
                            zonesReached.Add(toNode.zone);
                            nodesExplored.Add(nextNode);
                        }

                        movesLeft--;
                    }
                }

                // Recalculate fog after all hero moves
                {
                    var heroInfos = deployedHeroes.Where(h => !h.isInjured).Select(h => new HeroFogInfo
                    {
                        nodeId = h.currentNodeId,
                        bonusRevealRange = 0,
                        revealsEntireZone = false
                    }).ToList();
                    var activeEffects = new HashSet<ColonyEffect>(colony.GetActiveEffects().Keys);
                    fogOfWar.RecalculateVisibility(mapGraph, heroInfos, activeEffects);
                }

                // Check if any hero entered Town zone (triggers Pied Piper countdown)
                if (!piperCountdownActive && zonesReached.Contains(NodeType.Town))
                {
                    piperCountdownActive = true;
                    piperCountdownRemaining = PIPER_COUNTDOWN;
                }

                // ── Enemy Move Phase ────────────────────────────────────
                var enemyMoves = EnemyAI.ProcessAllEnemies(
                    enemyTokens.Where(e => !e.isDefeated).ToList(),
                    mapGraph.GetAllNodes().ToList(),
                    deployedHeroes.Where(h => !h.isInjured).ToList());

                foreach (var move in enemyMoves)
                {
                    var enemy = enemyTokens.Find(e => e.tokenId == move.enemyTokenId);
                    if (enemy == null) continue;

                    var fromNode = mapGraph.GetNode(move.fromNodeId);
                    fromNode?.enemyTokenIds.Remove(enemy.tokenId);

                    enemy.currentNodeId = move.toNodeId;

                    var toNode = mapGraph.GetNode(move.toNodeId);
                    toNode?.enemyTokenIds.Add(enemy.tokenId);
                }

                // ── Combat Phase ────────────────────────────────────────
                var allNodes = mapGraph.GetAllNodes();
                foreach (var node in allNodes)
                {
                    bool hasHeroes = node.heroTokenIds.Any(id =>
                        deployedHeroes.Any(h => h.tokenId == id && !h.isInjured));
                    bool hasEnemies = node.enemyTokenIds.Any(id =>
                        enemyTokens.Any(e => e.tokenId == id && !e.isDefeated));

                    if (!hasHeroes || !hasEnemies) continue;

                    var heroesAtNode = deployedHeroes
                        .Where(h => h.currentNodeId == node.nodeId && !h.isInjured)
                        .ToList();
                    var enemiesAtNode = enemyTokens
                        .Where(e => e.currentNodeId == node.nodeId && !e.isDefeated)
                        .ToList();

                    if (heroesAtNode.Count == 0 || enemiesAtNode.Count == 0) continue;

                    // Ambush: if heroes moved into a node that was fogged, enemies get a free round
                    bool isAmbush = preMoveNodeFog.TryGetValue(node.nodeId, out var prevFog) && prevFog == FogState.Hidden;

                    // Use stepped combat API to allow tactical card play between rounds
                    var (ctx, result) = combatResolver.BeginCombat(heroesAtNode, enemiesAtNode, node.nodeId, isAmbush);

                    // Play a tactical card before first combat round
                    if (availableTactical.Count > 0 && heroesAtNode.Any(h => h.IsAlive) && enemiesAtNode.Any(e => e.IsAlive))
                    {
                        bool isBossFight = enemiesAtNode.Any(e => e.strength >= 8);
                        var tacticalCard = decisionMaker.ChooseTacticalCard(
                            availableTactical, heroesAtNode, enemiesAtNode, isBossFight, turn);
                        if (tacticalCard != null)
                        {
                            TacticalEffectProcessor.ApplyTacticalCard(tacticalCard, heroesAtNode, enemiesAtNode, node.nodeId, ctx);
                            availableTactical.Remove(tacticalCard);
                        }
                    }

                    // Combat rounds
                    if (heroesAtNode.Any(h => h.IsAlive) && enemiesAtNode.Any(e => e.IsAlive))
                    {
                        bool combatContinues = true;
                        while (combatContinues && result.roundsFought < 100)
                        {
                            if (ctx.retreatTriggered) break;
                            var roundResult = combatResolver.ExecuteOneRound(heroesAtNode, enemiesAtNode, ctx, result);
                            combatContinues = roundResult.combatContinues;
                        }
                    }

                    combatResolver.EndCombat(heroesAtNode, enemiesAtNode, result, ctx);

                    if (result.heroesWon) combatsWon++;

                    // Process combat results
                    foreach (var enemy in enemiesAtNode)
                    {
                        if (enemy.isDefeated)
                        {
                            totalEnemiesDefeated++;
                            node.enemyTokenIds.Remove(enemy.tokenId);

                            bool isBoss = enemy.strength >= 8;
                            if (isBoss)
                            {
                                zoneBossesDefeated++;
                                mapGraph.ZoneBossesDefeated = zoneBossesDefeated;
                            }
                            if (enemy.homeZone == NodeType.PiedPiper)
                            {
                                piedPiperDefeated = true;
                                victory = true;
                            }
                        }
                    }

                    foreach (var hero in heroesAtNode)
                    {
                        if (hero.isInjured)
                        {
                            heroesEverInjured.Add(hero.cardDef.cardId);
                            deployedHeroes.Remove(hero);
                            node.heroTokenIds.Remove(hero.tokenId);

                            // Drop resources on node
                            if (hero.TotalCarried > 0)
                            {
                                var dropped = hero.DropAllResources();
                                foreach (var kvp in dropped)
                                {
                                    if (!node.resources.ContainsKey(kvp.Key))
                                        node.resources[kvp.Key] = 0;
                                    node.resources[kvp.Key] += kvp.Value;
                                }
                            }
                        }
                    }

                    // Mid-run card reward: after combat victory, add a random card to the pool
                    if (result.heroesWon && result.defeatedEnemyTokenIds.Count > 0)
                    {
                        var rewardCard = PickRandomRewardCard(node.zone, seed + turn * 100 + node.nodeId);
                        if (rewardCard != null)
                        {
                            switch (rewardCard.cardType)
                            {
                                case CardType.Hero: heroDeck.Add(rewardCard); break;
                                case CardType.Equipment: equipDeck.Add(rewardCard); break;
                                case CardType.Tactical: availableTactical.Add(rewardCard); break;
                            }
                            totalDeckSize++;
                        }
                    }
                }

                // Check victory
                if (victory)
                {
                    runActive = false;
                    break;
                }

                // ── Gather Phase ────────────────────────────────────────
                var gatherResult = GatherPhase.Execute(deployedHeroes, mapGraph, colony);
                totalResourcesGathered += gatherResult.totalGathered;

                // Auto-deposit at colony
                foreach (var hero in deployedHeroes)
                {
                    if (hero.isInjured) continue;
                    if (hero.currentNodeId == mapGraph.ColonyNodeId && hero.TotalCarried > 0)
                    {
                        var deposited = hero.DepositResources();
                        foreach (var kvp in deposited)
                        {
                            if (kvp.Key == ResourceType.Food)
                                foodStockpile = Mathf.Min(foodStockpile + kvp.Value, foodStorageCapacity);
                            else if (kvp.Key == ResourceType.Materials)
                                materialsStockpile += kvp.Value;
                            else if (kvp.Key == ResourceType.Currency)
                                currencyStockpile += kvp.Value;
                        }
                    }
                }

                // ── Cleanup Phase ───────────────────────────────────────
                // Food consumption: 1 per non-injured deployed hero NOT at colony
                int heroesRequiringFood = deployedHeroes.Count(h => !h.isInjured && h.currentNodeId != mapGraph.ColonyNodeId);
                int foodToConsume = Mathf.Min(heroesRequiringFood, foodStockpile);
                int deficit = heroesRequiringFood - foodToConsume;
                foodStockpile -= foodToConsume;

                // Surplus food heals damaged heroes
                if (deficit == 0 && foodStockpile > 0)
                {
                    var damaged = deployedHeroes
                        .Where(h => !h.isInjured && h.currentHP < h.EffectiveHP)
                        .OrderBy(h => h.currentHP)
                        .ToList();
                    foreach (var hero in damaged)
                    {
                        if (foodStockpile <= 0) break;
                        int hpMissing = hero.EffectiveHP - hero.currentHP;
                        int heal = Mathf.Min(hpMissing, foodStockpile);
                        if (heal > 0)
                        {
                            hero.Heal(heal);
                            foodStockpile -= heal;
                        }
                    }
                }

                // Starvation damage
                if (deficit > 0)
                {
                    int starvDmg = cachedStarvationDamage;
                    var starveOrder = deployedHeroes
                        .Where(h => !h.isInjured && h.currentNodeId != mapGraph.ColonyNodeId)
                        .OrderBy(h => h.EffectiveCombat)
                        .ToList();

                    int fedCount = heroesRequiringFood - deficit;
                    for (int i = fedCount; i < starveOrder.Count; i++)
                    {
                        starveOrder[i].TakeDamage(starvDmg);
                        if (starveOrder[i].isInjured)
                        {
                            heroesEverInjured.Add(starveOrder[i].cardDef.cardId);
                            deployedHeroes.Remove(starveOrder[i]);
                            var node = mapGraph.GetNode(starveOrder[i].currentNodeId);
                            node?.heroTokenIds.Remove(starveOrder[i].tokenId);
                        }
                    }
                }

                // Enemy respawn tick
                foreach (var enemy in enemyTokens)
                {
                    if (!enemy.isDefeated) continue;
                    enemy.respawnTimer--;
                    if (enemy.respawnTimer <= 0)
                    {
                        int homeNode = FindEnemyHomeNode(enemy, mapGraph);
                        if (homeNode >= 0)
                            enemy.Respawn(homeNode);
                    }
                }

                // Injury recovery
                foreach (var hero in allHeroTokens)
                {
                    if (!hero.isInjured) continue;
                    bool hasShrineOfHeroes = colony.HasEffect(ColonyEffect.ImmediateHealReturn);
                    if (hasShrineOfHeroes)
                    {
                        hero.isInjured = false;
                        hero.currentHP = hero.EffectiveHP;
                        hero.turnsUntilRecovery = 0;
                        availableHeroes.Add(hero.cardDef);
                        continue;
                    }
                    int healBonus = colony.HasEffect(ColonyEffect.HealInjured) ? colony.GetEffectValue(ColonyEffect.HealInjured) : 0;
                    hero.turnsUntilRecovery -= (1 + healBonus);
                    if (hero.turnsUntilRecovery <= 0)
                    {
                        hero.isInjured = false;
                        hero.currentHP = hero.EffectiveHP;
                        hero.turnsUntilRecovery = 0;
                        availableHeroes.Add(hero.cardDef);
                    }
                }

                // ── Pied Piper Countdown Tick ──────────────────────────────
                if (piperCountdownActive && piperCountdownRemaining > 0)
                {
                    piperCountdownRemaining--;
                    if (piperCountdownRemaining <= 0)
                    {
                        // Move Pied Piper to colony node
                        var piperToken = enemyTokens.Find(e => e.homeZone == NodeType.PiedPiper && !e.isDefeated);
                        if (piperToken != null)
                        {
                            var fromNode = mapGraph.GetNode(piperToken.currentNodeId);
                            fromNode?.enemyTokenIds.Remove(piperToken.tokenId);
                            piperToken.currentNodeId = mapGraph.ColonyNodeId;
                            var colonyNode = mapGraph.GetNode(mapGraph.ColonyNodeId);
                            colonyNode?.enemyTokenIds.Add(piperToken.tokenId);
                        }
                    }
                }

                // ── Stall Detection ─────────────────────────────────────
                bool madeProgress = totalEnemiesDefeated > lastProgressEnemies
                    || zonesReached.Count > lastProgressZones
                    || totalHeroesDeployed > lastProgressDeploy;
                if (madeProgress)
                {
                    lastProgressTurn = turn;
                    lastProgressEnemies = totalEnemiesDefeated;
                    lastProgressZones = zonesReached.Count;
                    lastProgressDeploy = totalHeroesDeployed;
                }
                else if (turn - lastProgressTurn >= STALL_TURNS)
                {
                    runActive = false;
                    break;
                }

                // ── Check Defeat ────────────────────────────────────────
                bool colonyOverrun = mapGraph.GetNode(mapGraph.ColonyNodeId)?.enemyTokenIds
                    .Any(id => enemyTokens.Any(e => e.tokenId == id && !e.isDefeated)) ?? false;
                bool allHeroesDown = deployedHeroes.Count == 0 && allHeroTokens.Count > 0 &&
                                     allHeroTokens.All(h => h.isInjured);

                if (colonyOverrun || allHeroesDown)
                {
                    runActive = false;
                    break;
                }
            }

            // ── Calculate fitness ────────────────────────────────────────
            int heroesNeverInjured = allHeroTokens.Count - heroesEverInjured.Count;
            float fitness = CalculateFitness(victory, turn, totalEnemiesDefeated, zoneBossesDefeated,
                piedPiperDefeated, totalResourcesGathered, colonyCardsPlayed, heroesNeverInjured,
                totalDeckSize, deployedHeroes.Count, zonesReached.Count, nodesExplored.Count,
                totalHeroesDeployed, combatsWon, peakDeployedCount);

            return new SimulationResult
            {
                victory = victory,
                turnsUsed = turn,
                enemiesDefeated = totalEnemiesDefeated,
                zoneBossesDefeated = zoneBossesDefeated,
                piedPiperDefeated = piedPiperDefeated,
                totalResourcesGathered = totalResourcesGathered,
                colonyCardsPlayed = colonyCardsPlayed,
                heroesNeverInjured = heroesNeverInjured,
                totalHeroes = allHeroTokens.Count,
                deckSize = totalDeckSize,
                foodRemaining = foodStockpile,
                materialsRemaining = materialsStockpile,
                currencyRemaining = currencyStockpile,
                fitness = fitness
            };
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private List<EnemyToken> SpawnEnemies(MapGraph mapGraph)
        {
            var tokens = new List<EnemyToken>();
            int tokenId = 0;

            foreach (var node in mapGraph.GetAllNodes())
            {
                if (node.zone == NodeType.Colony) continue;

                var zoneEnemies = allEnemies.Where(e => e.homeZone == node.zone).ToList();
                if (zoneEnemies.Count == 0) continue;

                // Spawn 0-1 enemies per node — Pied Piper always spawns, others based on spawn chance
                int spawnCount = node.zone == NodeType.PiedPiper ? 1 :
                    SeededRandom.Range(0, 100) < (int)(cachedEnemySpawnChance * 100) ? 1 : 0;

                for (int i = 0; i < spawnCount; i++)
                {
                    var def = zoneEnemies[SeededRandom.Range(0, zoneEnemies.Count)];
                    var enemy = new EnemyToken(tokenId++, def.enemyName, def.strength, def.hp,
                        def.speed, def.behavior, def.homeZone, node.nodeId);
                    tokens.Add(enemy);
                    node.enemyTokenIds.Add(enemy.tokenId);
                }
            }

            return tokens;
        }

        private int FindEnemyHomeNode(EnemyToken enemy, MapGraph mapGraph)
        {
            var homeNodes = mapGraph.GetAllNodes()
                .Where(n => n.zone == enemy.homeZone && n.heroTokenIds.Count == 0)
                .ToList();
            if (homeNodes.Count == 0) return -1;
            return homeNodes[SeededRandom.Range(0, homeNodes.Count)].nodeId;
        }

        private float CalculateFitness(bool victory, int turnsUsed, int enemiesDefeated,
            int zoneBossesDefeated, bool piedPiperDefeated, int resourcesGathered,
            int colonyCardsPlayed, int heroesNeverInjured, int deckSize, int heroesAlive,
            int zonesReached, int nodesExplored, int totalHeroesDeployed, int combatsWon,
            int peakDeployedCount)
        {
            float fitness = 0f;

            // ══════════════════════════════════════════════════════════════
            // FITNESS v2: Strong intermediate gradients so the optimizer
            // can differentiate "survived 5 turns and built colony" from
            // "died turn 2 doing nothing." Each tier is large enough to
            // create selection pressure BEFORE combat/boss tiers kick in.
            // Target: Victory ~15000+, good non-victory run ~2000-4000.
            // ══════════════════════════════════════════════════════════════

            // ── Tier 1: Victory ──────────────────────────────────────────
            if (victory) fitness += 5000f;
            if (piedPiperDefeated) fitness += 3000f;

            // ── Tier 2: Boss progression (the critical gate) ─────────────
            for (int i = 0; i < zoneBossesDefeated; i++)
                fitness += (i + 1) * 1500f; // 1st=1500, 2nd=3000, 3rd=4500

            // ── Tier 3: Combat engagement ────────────────────────────────
            fitness += combatsWon * 300f;
            fitness += enemiesDefeated * 75f;

            // ── Tier 4: Exploration & zone progression ───────────────────
            // These are large enough to dominate before combat success
            fitness += Mathf.Min(nodesExplored, 20) * 25f;   // max 500
            if (zonesReached >= 2) fitness += 200f;  // reached Wilderness
            if (zonesReached >= 3) fitness += 300f;  // reached Farmland
            if (zonesReached >= 4) fitness += 400f;  // reached Town
            if (zonesReached >= 5) fitness += 500f;  // reached Pied Piper area

            // ── Tier 5: Economy & colony development ─────────────────────
            // Resource gathering is the gateway to colony building
            fitness += Mathf.Min(resourcesGathered, 40) * 15f;  // max 600
            fitness += Mathf.Min(colonyCardsPlayed, 10) * 50f;   // max 500
            // Survival turns (you stayed alive = you're doing something right)
            fitness += Mathf.Min(turnsUsed, 20) * 20f;           // max 400

            // ── Tier 6: Deployment & force projection ────────────────────
            fitness += Mathf.Min(peakDeployedCount, 8) * 40f;    // max 320
            fitness += Mathf.Min(totalHeroesDeployed, 15) * 15f;  // max 225

            // ── Tier 7: Preservation (rewards careful play) ──────────────
            fitness += heroesAlive * 30f;
            fitness += heroesNeverInjured * 20f;

            // ── Stagnation penalties ───────────────────────────────────────
            if (turnsUsed >= 6 && combatsWon == 0)
                fitness -= (turnsUsed - 5) * 40f;

            if (turnsUsed >= 12 && zoneBossesDefeated == 0)
                fitness -= (turnsUsed - 11) * 60f;

            if (turnsUsed >= 18 && zonesReached < 4)
                fitness -= (turnsUsed - 17) * 40f;

            if (!victory && turnsUsed >= MAX_TURNS)
                fitness -= 500f;

            // ── Victory bonuses (reward efficiency) ──────────────────────
            if (victory)
            {
                fitness += Mathf.Max(0, (30 - deckSize)) * 50f;
                fitness += Mathf.Max(0, (50 - turnsUsed)) * 100f;
            }

            return fitness;
        }
    }
}
