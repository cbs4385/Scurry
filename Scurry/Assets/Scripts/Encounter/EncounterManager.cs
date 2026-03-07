using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Core;
using Scurry.Board;
using Scurry.Colony;
using Scurry.Gathering;

namespace Scurry.Encounter
{
    public class EncounterManager : MonoBehaviour
    {
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private GatheringManager gatheringManager;
        [SerializeField] private GameObject heroTokenPrefab;

        private void Awake()
        {
            if (boardManager == null) boardManager = FindAnyObjectByType<BoardManager>();
            if (gatheringManager == null) gatheringManager = FindAnyObjectByType<GatheringManager>();
            Debug.Log($"[EncounterManager] Awake: boardManager={boardManager != null}, gatheringManager={gatheringManager != null}, heroTokenPrefab={heroTokenPrefab != null}");
        }

        private EncounterDefinitionSO currentEncounter;
        private ColonyConfig colonyConfig;
        private List<CardDefinitionSO> heroCards;
        private List<CardDefinitionSO> equipmentCards;
        private List<CardDefinitionSO> benefitCards;
        private EncounterResult currentResult;
        private bool recallRequested;
        private bool recallActive; // True on the turn after recall is pressed
        private bool encounterRunning;
        private List<HeroAgent> deployedHeroes = new List<HeroAgent>();
        private int encounterDifficulty = 1;

        // Wound tracking: heroes wounded before this encounter (they sit out)
        private HashSet<CardDefinitionSO> woundedHeroesBefore = new HashSet<CardDefinitionSO>();

        public bool IsEncounterRunning => encounterRunning;
        public bool IsRecallAvailable => currentEncounter != null && currentEncounter.allowRecall && !recallRequested;
        public EncounterResult CurrentResult => currentResult;

        public void StartEncounter(EncounterDefinitionSO encounter, List<CardDefinitionSO> heroDeck, ColonyConfig config, HashSet<CardDefinitionSO> woundedHeroes, int difficulty = 1)
        {
            currentEncounter = encounter;
            colonyConfig = config;
            encounterDifficulty = Mathf.Max(1, difficulty);
            recallRequested = false;
            recallActive = false;
            encounterRunning = true;
            currentResult = new EncounterResult();
            deployedHeroes.Clear();
            woundedHeroesBefore = woundedHeroes ?? new HashSet<CardDefinitionSO>();

            // Separate hero deck into heroes, equipment, and benefits
            heroCards = new List<CardDefinitionSO>();
            equipmentCards = new List<CardDefinitionSO>();
            benefitCards = new List<CardDefinitionSO>();

            foreach (var card in heroDeck)
            {
                switch (card.cardType)
                {
                    case CardType.Hero:
                        if (!woundedHeroesBefore.Contains(card))
                            heroCards.Add(card);
                        else
                            Debug.Log($"[EncounterManager] StartEncounter: skipping wounded hero '{card.cardName}'");
                        break;
                    case CardType.Equipment:
                        equipmentCards.Add(card);
                        break;
                    case CardType.HeroBenefit:
                        benefitCards.Add(card);
                        break;
                }
            }

            Debug.Log($"[EncounterManager] StartEncounter: encounter='{encounter.encounterName}', type={encounter.encounterType}, " +
                      $"heroes={heroCards.Count}, equipment={equipmentCards.Count}, benefits={benefitCards.Count}, " +
                      $"allowRecall={encounter.allowRecall}, difficulty={encounter.difficulty}");

            // Set up the board
            SetupBoard();

            // Deploy heroes
            AutoDeployHeroes();

            // Assign equipment
            AutoAssignEquipment();

            // Start auto-battle loop
            StartCoroutine(RunAutoBattle());
        }

        private void SetupBoard()
        {
            Debug.Log($"[EncounterManager] SetupBoard: encounter='{currentEncounter.encounterName}'");

            // Reset board
            boardManager.ResetBoardForNewGame();

            // If encounter has a board layout, use it
            if (currentEncounter.boardLayout != null)
            {
                boardManager.InitializeBoard(currentEncounter.boardLayout);
            }

            // Spawn enemies from encounter definition
            foreach (var spawn in currentEncounter.enemySpawns)
            {
                if (spawn.enemyDefinition != null)
                {
                    gatheringManager.SpawnEnemyFromDefinition(spawn.enemyDefinition, spawn.gridPosition);
                    Debug.Log($"[EncounterManager] SetupBoard: spawned enemy '{spawn.enemyDefinition.enemyName}' at {spawn.gridPosition}");
                }
            }

            // Place resources from encounter definition (scale value by difficulty)
            var bc = Data.BalanceConfigSO.Instance;
            float scalingFactor = bc != null ? bc.difficultyScalingFactor : 0.15f;
            float difficultyMultiplier = 1f + (encounterDifficulty - 1) * scalingFactor;
            foreach (var resNode in currentEncounter.resourceNodes)
            {
                var tile = boardManager.GetTile(resNode.gridPosition);
                if (tile != null)
                {
                    int scaledValue = Mathf.Max(1, Mathf.RoundToInt(resNode.value * difficultyMultiplier));
                    tile.SetAsResourceNode(resNode.resourceType, scaledValue);
                    Debug.Log($"[EncounterManager] SetupBoard: placed resource {resNode.resourceType}x{scaledValue} (base={resNode.value}, diff={encounterDifficulty}, mult={difficultyMultiplier:F2}) at {resNode.gridPosition}");
                }
            }

            Debug.Log("[EncounterManager] SetupBoard: complete");
        }

        private void AutoDeployHeroes()
        {
            Debug.Log($"[EncounterManager] AutoDeployHeroes: deploying {heroCards.Count} heroes on leftmost column");

            // Get valid deployment tiles (leftmost column, top to bottom)
            var deployTiles = new List<Tile>();
            int rows = boardManager.Rows;
            for (int r = 0; r < rows; r++)
            {
                var tile = boardManager.GetTile(new Vector2Int(r, 0));
                if (tile != null && tile.TileType != TileType.Hazard && tile.TileType != TileType.EnemyPatrol && !tile.HasHero)
                {
                    deployTiles.Add(tile);
                }
            }

            int deployed = 0;
            for (int i = 0; i < heroCards.Count && i < deployTiles.Count; i++)
            {
                var card = heroCards[i];
                var tile = deployTiles[i];
                var hero = SpawnHeroToken(card, tile);
                if (hero != null)
                {
                    // Apply colony bonuses
                    hero.ApplyColonyBonuses(colonyConfig.heroCombatBonus, colonyConfig.heroMoveBonus, colonyConfig.heroCarryBonus);
                    deployedHeroes.Add(hero);
                    deployed++;
                    Debug.Log($"[EncounterManager] AutoDeployHeroes: deployed '{card.cardName}' at ({tile.GridPosition}) with colony bonuses (combat+{colonyConfig.heroCombatBonus}, move+{colonyConfig.heroMoveBonus}, carry+{colonyConfig.heroCarryBonus})");
                }
            }

            Debug.Log($"[EncounterManager] AutoDeployHeroes: deployed {deployed}/{heroCards.Count} heroes");
            EventBus.OnAutoDeployComplete?.Invoke();
        }

        private HeroAgent SpawnHeroToken(CardDefinitionSO card, Tile tile)
        {
            Vector3 worldPos = boardManager.GetWorldPosition(tile.GridPosition);
            worldPos.z = -0.1f;

            GameObject token;
            if (heroTokenPrefab != null)
            {
                token = Instantiate(heroTokenPrefab, worldPos, Quaternion.identity, boardManager.transform);
            }
            else
            {
                // Create token at runtime if no prefab assigned
                token = new GameObject($"Hero_{card.cardName}", typeof(SpriteRenderer), typeof(HeroAgent));
                token.transform.SetParent(boardManager.transform);
                token.transform.position = worldPos;
            }
            token.name = $"Hero_{card.cardName}";

            var sr = token.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                SpriteHelper.EnsureSprite(sr);
                sr.color = card.placeholderColor;
                sr.sortingOrder = 5;
            }

            SpriteHelper.AddOutline(token, 5);

            var hero = token.GetComponent<HeroAgent>();
            if (hero != null)
            {
                hero.Initialize(card, tile.GridPosition);
                tile.HasHero = true;
            }

            return hero;
        }

        private void AutoAssignEquipment()
        {
            if (equipmentCards.Count == 0 || deployedHeroes.Count == 0) return;

            Debug.Log($"[EncounterManager] AutoAssignEquipment: assigning {equipmentCards.Count} equipment to {deployedHeroes.Count} heroes");

            // Sort equipment by slot type for priority assignment
            var combatEquip = new List<CardDefinitionSO>();
            var moveEquip = new List<CardDefinitionSO>();
            var carryEquip = new List<CardDefinitionSO>();
            var specialEquip = new List<CardDefinitionSO>();

            foreach (var equip in equipmentCards)
            {
                switch (equip.equipmentSlot)
                {
                    case EquipmentSlot.Combat: combatEquip.Add(equip); break;
                    case EquipmentSlot.Movement: moveEquip.Add(equip); break;
                    case EquipmentSlot.Carry: carryEquip.Add(equip); break;
                    case EquipmentSlot.Special: specialEquip.Add(equip); break;
                }
            }

            // Sort heroes by relevant stat (descending) for each equipment type
            // Combat equipment -> highest combat hero
            AssignEquipmentByPriority(combatEquip, deployedHeroes, h => h.Combat, (h, val) => h.AddEquipmentBonus(val, 0, 0), "combat");
            // Movement equipment -> highest movement hero
            AssignEquipmentByPriority(moveEquip, deployedHeroes, h => h.Movement, (h, val) => h.AddEquipmentBonus(0, val, 0), "movement");
            // Carry equipment -> highest carry hero
            AssignEquipmentByPriority(carryEquip, deployedHeroes, h => h.CarryCapacity, (h, val) => h.AddEquipmentBonus(0, 0, val), "carry");
            // Special equipment -> highest combat hero (default)
            AssignEquipmentByPriority(specialEquip, deployedHeroes, h => h.Combat, (h, val) => h.AddEquipmentBonus(val, 0, 0), "special");

            EventBus.OnEquipmentAssigned?.Invoke();
        }

        private void AssignEquipmentByPriority(List<CardDefinitionSO> equipment, List<HeroAgent> heroes,
            System.Func<HeroAgent, int> statSelector, System.Action<HeroAgent, int> applyBonus, string slotName)
        {
            if (equipment.Count == 0) return;

            // Sort heroes by stat descending
            var sortedHeroes = new List<HeroAgent>(heroes);
            sortedHeroes.Sort((a, b) => statSelector(b).CompareTo(statSelector(a)));

            for (int i = 0; i < equipment.Count && i < sortedHeroes.Count; i++)
            {
                applyBonus(sortedHeroes[i], equipment[i].equipmentBonusValue);
                Debug.Log($"[EncounterManager] AutoAssignEquipment: '{equipment[i].cardName}' ({slotName} +{equipment[i].equipmentBonusValue}) -> '{sortedHeroes[i].CardData.cardName}'");
            }
        }

        private IEnumerator RunAutoBattle()
        {
            Debug.Log("[EncounterManager] RunAutoBattle: starting auto-battle loop");
            float speedMult = GameSettings.Instance != null ? GameSettings.Instance.BattleWaitMultiplier : 1f;
            yield return new WaitForSeconds(0.5f * speedMult);

            int turnCount = 0;

            while (encounterRunning)
            {
                turnCount++;
                Debug.Log($"[EncounterManager] RunAutoBattle: ===== Turn {turnCount} =====");

                // Check recall state
                if (recallRequested && !recallActive)
                {
                    recallActive = true;
                    Debug.Log("[EncounterManager] RunAutoBattle: recall activated — heroes will retreat this turn");
                    EventBus.OnRecallInitiated?.Invoke();
                }

                // Run one gathering turn
                yield return StartCoroutine(gatheringManager.RunGathering());

                // Tally resources collected this turn
                TallyResources();

                // Check end conditions
                if (CheckEndConditions(turnCount))
                    break;

                yield return new WaitForSeconds(0.3f * speedMult);
            }
        }

        private void TallyResources()
        {
            // Resources are collected by HeroAgents during gathering and reported via events
            // The EventBus.OnResourceCollected fires for each collection — we track in result
        }

        private bool CheckEndConditions(int turnCount)
        {
            // Count alive heroes
            int aliveHeroes = 0;
            foreach (var hero in deployedHeroes)
            {
                if (hero != null && !hero.IsWounded)
                    aliveHeroes++;
            }

            bool hasResources = boardManager.HasAnyResources();
            bool hasEnemies = boardManager.HasAnyEnemies();

            Debug.Log($"[EncounterManager] CheckEndConditions: turn={turnCount}, aliveHeroes={aliveHeroes}, hasResources={hasResources}, hasEnemies={hasEnemies}, recallActive={recallActive}");

            // All heroes defeated
            if (aliveHeroes == 0)
            {
                Debug.Log("[EncounterManager] CheckEndConditions: ALL HEROES DEFEATED — encounter failed");
                currentResult.success = false;
                currentResult.recalled = false;
                // Clear gathered resources on failure
                currentResult.resourcesGathered.Clear();
                EndEncounter();
                return true;
            }

            // Recall active — heroes have retreated
            if (recallActive)
            {
                Debug.Log("[EncounterManager] CheckEndConditions: RECALL COMPLETE — partial success");
                currentResult.success = true;
                currentResult.recalled = true;
                EndEncounter();
                return true;
            }

            // All resources gathered (resource encounters)
            if (currentEncounter.encounterType == EncounterType.Resource && !hasResources)
            {
                Debug.Log("[EncounterManager] CheckEndConditions: ALL RESOURCES GATHERED — success");
                currentResult.success = true;
                EndEncounter();
                return true;
            }

            // Boss/Elite: all enemies defeated
            if ((currentEncounter.encounterType == EncounterType.Elite || currentEncounter.encounterType == EncounterType.Boss) && !hasEnemies)
            {
                Debug.Log("[EncounterManager] CheckEndConditions: ALL ENEMIES DEFEATED — boss/elite victory");
                currentResult.success = true;
                currentResult.rewardCards = new List<CardDefinitionSO>(currentEncounter.rewardCards);
                currentResult.rewardRelic = currentEncounter.rewardRelic;
                EndEncounter();
                return true;
            }

            return false;
        }

        public void OnRecallPressed()
        {
            if (!IsRecallAvailable)
            {
                Debug.Log("[EncounterManager] OnRecallPressed: recall not available");
                return;
            }
            recallRequested = true;
            Debug.Log("[EncounterManager] OnRecallPressed: recall requested — will activate next turn (1-turn delay)");
        }

        private void EndEncounter()
        {
            encounterRunning = false;

            // Track wounded heroes
            foreach (var hero in deployedHeroes)
            {
                if (hero == null) continue;
                if (hero.IsWounded)
                {
                    if (woundedHeroesBefore.Contains(hero.CardData))
                    {
                        // Was already wounded before — now exhausted
                        currentResult.exhaustedHeroes.Add(hero.CardData);
                        Debug.Log($"[EncounterManager] EndEncounter: '{hero.CardData.cardName}' EXHAUSTED (wounded twice)");
                    }
                    else
                    {
                        currentResult.woundedHeroes.Add(hero.CardData);
                        Debug.Log($"[EncounterManager] EndEncounter: '{hero.CardData.cardName}' WOUNDED");
                    }
                }
            }

            Debug.Log($"[EncounterManager] EndEncounter: {currentResult}");
            EventBus.OnEncounterComplete?.Invoke(currentResult);
        }
    }
}
