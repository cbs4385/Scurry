using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Core;
using Scurry.Data;
using Scurry.Combat;
using Scurry.Map;

namespace Scurry.UI
{
    /// <summary>
    /// Full-screen overlay for combat resolution during the Combat phase.
    /// Shows heroes vs enemies with HP bars, strength comparison, tactical card hand, and round controls.
    /// </summary>
    public class CombatUI : MonoBehaviour
    {
        // ── Structs ─────────────────────────────────────────────────────

        /// <summary>
        /// Describes a single damage event in combat.
        /// </summary>
        public struct DamageEvent
        {
            public int targetId;
            public int damage;
            public bool isHero;
            public bool isDefeated;

            public override string ToString()
            {
                return $"DamageEvent(targetId={targetId}, damage={damage}, isHero={isHero}, isDefeated={isDefeated})";
            }
        }

        // ── Constants ───────────────────────────────────────────────────
        private const float UnitCardWidth = 160f;
        private const float UnitCardHeight = 200f;
        private const float UnitSpacing = 12f;
        private const float TacticalCardWidth = 120f;
        private const float HPBarHeight = 12f;

        // ── UI References ───────────────────────────────────────────────
        private Canvas canvas;
        private GameObject mainPanel;

        // Hero side (left)
        private RectTransform heroContainer;
        private readonly List<GameObject> heroUnitCards = new List<GameObject>();
        private readonly Dictionary<int, Image> heroHPBars = new Dictionary<int, Image>();
        private readonly Dictionary<int, TextMeshProUGUI> heroHPTexts = new Dictionary<int, TextMeshProUGUI>();
        private readonly Dictionary<int, TextMeshProUGUI> heroStatTexts = new Dictionary<int, TextMeshProUGUI>();

        // Enemy side (right)
        private RectTransform enemyContainer;
        private readonly List<GameObject> enemyUnitCards = new List<GameObject>();
        private readonly Dictionary<int, Image> enemyHPBars = new Dictionary<int, Image>();
        private readonly Dictionary<int, TextMeshProUGUI> enemyHPTexts = new Dictionary<int, TextMeshProUGUI>();

        // Center info
        private TextMeshProUGUI roundText;
        private TextMeshProUGUI heroPowerText;
        private TextMeshProUGUI enemyPowerText;
        private TextMeshProUGUI vsText;

        // Tactical cards (bottom)
        private RectTransform tacticalContainer;
        private readonly List<GameObject> tacticalCardGOs = new List<GameObject>();

        // Controls
        private Button continueBtn;
        private Button autoBtn;
        private TextMeshProUGUI autoBtnText;

        // Tactical window prompt
        private TextMeshProUGUI tacticalPromptText;
        private bool tacticalWindowActive;

        // ── State ───────────────────────────────────────────────────────
        private bool autoAdvance;
        private int currentNodeId;
        private IReadOnlyList<CardDefinitionSO> currentTacticalCards;
        private System.Action onContinue;
        private System.Action<CardDefinitionSO> onTacticalPlayed;

        // ── Cached hero/enemy references for HP updates ─────────────────
        private List<HeroToken> cachedHeroes;
        private List<EnemyToken> cachedEnemies;

        private void Awake()
        {
            Debug.Log("[CombatUI] Awake: initializing");
            BuildUI();
        }

        // ── Build ───────────────────────────────────────────────────────

        private void BuildUI()
        {
            Debug.Log("[CombatUI] BuildUI: starting construction");
            UIHelper.EnsureEventSystem();

            // Background camera — clears the screen so old scene frames don't bleed through
            var camGO = new GameObject("BackgroundCamera", typeof(Camera));
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 1f);
            cam.cullingMask = 0;
            cam.depth = -100;
            Debug.Log("[CombatUI] BuildUI: background camera created");

            // Canvas
            canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // Main panel - full screen with dimmed background
            mainPanel = new GameObject("CombatPanel", typeof(RectTransform), typeof(Image));
            mainPanel.transform.SetParent(transform, false);
            var mainRect = mainPanel.GetComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            mainRect.sizeDelta = Vector2.zero;
            mainPanel.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.05f, 0.92f);

            // Title
            CreateText(mainPanel.transform, "CombatTitle",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -15), new Vector2(400, 45),
                "COMBAT", 36, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.3f, 0.2f));

            // Hero container (left side)
            BuildUnitContainer("HeroContainer", new Vector2(0.02f, 0.30f), new Vector2(0.42f, 0.85f), out heroContainer);
            Debug.Log("[CombatUI] BuildUI: hero container created");

            // Hero label
            CreateText(mainPanel.transform, "HeroLabel",
                new Vector2(0.02f, 0.86f), new Vector2(0.42f, 0.92f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                "HEROES", 20, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.4f, 0.8f, 1f));

            // Enemy container (right side)
            BuildUnitContainer("EnemyContainer", new Vector2(0.58f, 0.30f), new Vector2(0.98f, 0.85f), out enemyContainer);
            Debug.Log("[CombatUI] BuildUI: enemy container created");

            // Enemy label
            CreateText(mainPanel.transform, "EnemyLabel",
                new Vector2(0.58f, 0.86f), new Vector2(0.98f, 0.92f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                "ENEMIES", 20, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.4f, 0.3f));

            // Center info
            BuildCenterInfo();

            // Tactical card hand (bottom)
            BuildTacticalHand();

            // Tactical prompt text
            tacticalPromptText = CreateText(mainPanel.transform, "TacticalPrompt",
                new Vector2(0.3f, 0.23f), new Vector2(0.7f, 0.28f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                "Play Tactical Cards or Continue", 18, FontStyles.Bold, TextAlignmentOptions.Center,
                new Color(1f, 0.9f, 0.3f));
            tacticalPromptText.gameObject.SetActive(false);

            // Controls
            BuildControls();

            mainPanel.SetActive(false);
            Debug.Log("[CombatUI] BuildUI: complete (panel hidden)");
        }

        private void BuildUnitContainer(string name, Vector2 anchorMin, Vector2 anchorMax, out RectTransform container)
        {
            var containerGO = new GameObject(name, typeof(RectTransform));
            containerGO.transform.SetParent(mainPanel.transform, false);
            container = containerGO.GetComponent<RectTransform>();
            container.anchorMin = anchorMin;
            container.anchorMax = anchorMax;
            container.pivot = new Vector2(0.5f, 0.5f);
            container.anchoredPosition = Vector2.zero;
            container.sizeDelta = Vector2.zero;
        }

        private void BuildCenterInfo()
        {
            Debug.Log("[CombatUI] BuildCenterInfo: creating center display");

            // Round counter
            roundText = CreateText(mainPanel.transform, "RoundText",
                new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(200, 0),
                "Round 1", 28, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            // Hero power (left of VS)
            heroPowerText = CreateText(mainPanel.transform, "HeroPowerText",
                new Vector2(0.42f, 0.55f), new Vector2(0.48f, 0.70f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                "0", 40, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.4f, 0.8f, 1f));

            // VS text
            vsText = CreateText(mainPanel.transform, "VSText",
                new Vector2(0.48f, 0.55f), new Vector2(0.52f, 0.70f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                "VS", 48, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.4f, 0.2f));

            // Enemy power (right of VS)
            enemyPowerText = CreateText(mainPanel.transform, "EnemyPowerText",
                new Vector2(0.52f, 0.55f), new Vector2(0.58f, 0.70f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                "0", 40, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.4f, 0.3f));

            Debug.Log("[CombatUI] BuildCenterInfo: complete");
        }

        private void BuildTacticalHand()
        {
            Debug.Log("[CombatUI] BuildTacticalHand: creating tactical card area");

            // Label
            CreateText(mainPanel.transform, "TacticalLabel",
                new Vector2(0.05f, 0.22f), new Vector2(0.5f, 0.27f), new Vector2(0, 0.5f),
                Vector2.zero, Vector2.zero,
                "Tactical Cards", 16, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.6f, 0.8f, 1f));

            // Container
            var containerGO = new GameObject("TacticalContainer", typeof(RectTransform));
            containerGO.transform.SetParent(mainPanel.transform, false);
            tacticalContainer = containerGO.GetComponent<RectTransform>();
            tacticalContainer.anchorMin = new Vector2(0.05f, 0.04f);
            tacticalContainer.anchorMax = new Vector2(0.75f, 0.22f);
            tacticalContainer.pivot = new Vector2(0, 0.5f);
            tacticalContainer.anchoredPosition = Vector2.zero;
            tacticalContainer.sizeDelta = Vector2.zero;

            Debug.Log("[CombatUI] BuildTacticalHand: complete");
        }

        private void BuildControls()
        {
            Debug.Log("[CombatUI] BuildControls: creating Continue and Auto buttons");

            // Continue button
            var continueBtnGO = CreateButton(mainPanel.transform, "ContinueButton",
                new Vector2(0.78f, 0.10f), new Vector2(0.95f, 0.20f),
                "Continue", 22, new Color(0.3f, 0.5f, 0.3f));
            continueBtn = continueBtnGO.GetComponent<Button>();
            continueBtn.onClick.AddListener(OnContinueClicked);

            // Auto button
            var autoBtnGO = CreateButton(mainPanel.transform, "AutoButton",
                new Vector2(0.78f, 0.02f), new Vector2(0.95f, 0.09f),
                "Auto: OFF", 16, new Color(0.3f, 0.3f, 0.4f));
            autoBtn = autoBtnGO.GetComponent<Button>();
            autoBtnText = autoBtnGO.GetComponentInChildren<TextMeshProUGUI>();
            autoBtn.onClick.AddListener(OnAutoClicked);

            Debug.Log("[CombatUI] BuildControls: complete");
        }

        // ── Public Methods ──────────────────────────────────────────────

        /// <summary>
        /// Shows the combat overlay with hero and enemy participants.
        /// </summary>
        public void ShowCombat(List<HeroToken> heroes, List<EnemyToken> enemies, int nodeId,
            IReadOnlyList<CardDefinitionSO> tacticalCards,
            System.Action onContinueCallback = null,
            System.Action<CardDefinitionSO> onTacticalCallback = null)
        {
            Debug.Log($"[CombatUI] ShowCombat: heroes={heroes?.Count ?? 0}, enemies={enemies?.Count ?? 0}, nodeId={nodeId}, tacticalCards={tacticalCards?.Count ?? 0}");

            currentNodeId = nodeId;
            currentTacticalCards = tacticalCards;
            onContinue = onContinueCallback;
            onTacticalPlayed = onTacticalCallback;
            cachedHeroes = heroes;
            cachedEnemies = enemies;
            autoAdvance = false;

            if (autoBtnText != null)
                autoBtnText.text = "Auto: OFF";

            mainPanel.SetActive(true);

            // Build hero unit cards
            BuildHeroUnits(heroes);

            // Build enemy unit cards
            BuildEnemyUnits(enemies);

            // Build tactical card hand
            BuildTacticalCards(tacticalCards);

            // Initial state
            UpdateRound(1, 0, 0);

            Debug.Log("[CombatUI] ShowCombat: combat panel displayed");
        }

        /// <summary>
        /// Updates the round display and strength comparison.
        /// </summary>
        public void UpdateRound(int round, int heroPower, int enemyPower)
        {
            Debug.Log($"[CombatUI] UpdateRound: round={round}, heroPower={heroPower}, enemyPower={enemyPower}");

            if (roundText != null)
                roundText.text = $"Round {round}";

            if (heroPowerText != null)
            {
                heroPowerText.text = heroPower.ToString();
                heroPowerText.color = heroPower >= enemyPower
                    ? new Color(0.4f, 1f, 0.4f)
                    : new Color(1f, 0.5f, 0.5f);
            }

            if (enemyPowerText != null)
            {
                enemyPowerText.text = enemyPower.ToString();
                enemyPowerText.color = enemyPower >= heroPower
                    ? new Color(1f, 0.5f, 0.5f)
                    : new Color(0.4f, 1f, 0.4f);
            }
        }

        /// <summary>
        /// Shows damage results from a combat round by updating HP bars and showing effects.
        /// </summary>
        public void ShowDamageResult(List<DamageEvent> events)
        {
            Debug.Log($"[CombatUI] ShowDamageResult: events={events?.Count ?? 0}");

            if (events == null) return;

            foreach (var evt in events)
            {
                Debug.Log($"[CombatUI] ShowDamageResult: processing {evt}");

                if (evt.isHero)
                {
                    if (heroHPBars.TryGetValue(evt.targetId, out Image hpBar) &&
                        heroHPTexts.TryGetValue(evt.targetId, out TextMeshProUGUI hpText))
                    {
                        // Find actual HP from cached heroes
                        HeroToken hero = null;
                        if (cachedHeroes != null)
                        {
                            for (int i = 0; i < cachedHeroes.Count; i++)
                            {
                                if (cachedHeroes[i].tokenId == evt.targetId)
                                {
                                    hero = cachedHeroes[i];
                                    break;
                                }
                            }
                        }

                        if (hero != null)
                        {
                            float fill = hero.maxHP > 0 ? (float)hero.currentHP / hero.maxHP : 0f;
                            UpdateHPBarDirect(hpBar, hpText, fill, $"{hero.currentHP}/{hero.maxHP}", evt.isDefeated);
                        }
                        else
                        {
                            UpdateHPBarEstimate(hpBar, hpText, evt.damage, evt.isDefeated);
                        }

                        // Update stat text if tracked
                        if (hero != null && heroStatTexts.TryGetValue(evt.targetId, out TextMeshProUGUI statText))
                        {
                            statText.text = $"CMB: {hero.EffectiveCombat}\nHP: {hero.currentHP}/{hero.maxHP}\nRole: {hero.cardDef.heroRole}";
                        }
                    }
                }
                else
                {
                    if (enemyHPBars.TryGetValue(evt.targetId, out Image hpBar) &&
                        enemyHPTexts.TryGetValue(evt.targetId, out TextMeshProUGUI hpText))
                    {
                        // Find actual HP from cached enemies
                        EnemyToken enemy = null;
                        if (cachedEnemies != null)
                        {
                            for (int i = 0; i < cachedEnemies.Count; i++)
                            {
                                if (cachedEnemies[i].tokenId == evt.targetId)
                                {
                                    enemy = cachedEnemies[i];
                                    break;
                                }
                            }
                        }

                        if (enemy != null)
                        {
                            float fill = enemy.maxHP > 0 ? (float)enemy.currentHP / enemy.maxHP : 0f;
                            UpdateHPBarDirect(hpBar, hpText, fill, $"{enemy.currentHP}/{enemy.maxHP}", evt.isDefeated);
                        }
                        else
                        {
                            UpdateHPBarEstimate(hpBar, hpText, evt.damage, evt.isDefeated);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when a tactical card in the hand is clicked.
        /// </summary>
        public void OnTacticalCardClicked(CardDefinitionSO card)
        {
            Debug.Log($"[CombatUI] OnTacticalCardClicked: card={card?.cardName ?? "NULL"}, type={card?.tacticalType}, effect={card?.tacticalEffectDescription}");

            if (onTacticalPlayed != null)
            {
                Debug.Log($"[CombatUI] OnTacticalCardClicked: invoking onTacticalPlayed callback for '{card?.cardName}'");
                onTacticalPlayed.Invoke(card);
            }
            else
            {
                Debug.Log("[CombatUI] OnTacticalCardClicked: no callback set, firing EventBus.OnTacticalCardPlayed");
                EventBus.OnTacticalCardPlayed?.Invoke(card);
            }
        }

        /// <summary>
        /// Called when the Continue button is clicked to advance to the next combat round.
        /// </summary>
        public void OnContinueClicked()
        {
            Debug.Log($"[CombatUI] OnContinueClicked: nodeId={currentNodeId}, autoAdvance={autoAdvance}");

            if (onContinue != null)
            {
                Debug.Log("[CombatUI] OnContinueClicked: invoking onContinue callback");
                onContinue.Invoke();
            }
            else
            {
                Debug.Log("[CombatUI] OnContinueClicked: no callback set, firing EventBus.OnCombatRound");
                EventBus.OnCombatRound?.Invoke(currentNodeId, 0, 0, 0);
            }
        }

        /// <summary>
        /// Called when the Auto button is clicked to toggle auto-advance.
        /// </summary>
        public void OnAutoClicked()
        {
            autoAdvance = !autoAdvance;
            Debug.Log($"[CombatUI] OnAutoClicked: autoAdvance={autoAdvance}");

            if (autoBtnText != null)
                autoBtnText.text = autoAdvance ? "Auto: ON" : "Auto: OFF";

            if (autoBtn != null)
                autoBtn.GetComponent<Image>().color = autoAdvance
                    ? new Color(0.2f, 0.5f, 0.2f)
                    : new Color(0.3f, 0.3f, 0.4f);

            if (continueBtn != null)
                continueBtn.interactable = !autoAdvance;
        }

        /// <summary>
        /// Returns whether auto-advance mode is active.
        /// </summary>
        public bool IsAutoAdvance => autoAdvance;

        /// <summary>
        /// Enables or disables the tactical card play window.
        /// When active, tactical cards are clickable and the prompt is shown.
        /// </summary>
        public void SetTacticalWindowActive(bool active)
        {
            Debug.Log($"[CombatUI] SetTacticalWindowActive: active={active}");
            tacticalWindowActive = active;

            // Show/hide prompt
            if (tacticalPromptText != null)
                tacticalPromptText.gameObject.SetActive(active);

            // Enable/disable tactical card buttons
            foreach (var cardGO in tacticalCardGOs)
            {
                if (cardGO != null)
                {
                    var btn = cardGO.GetComponent<Button>();
                    if (btn != null) btn.interactable = active;

                    // Visual dim when inactive
                    var img = cardGO.GetComponent<Image>();
                    if (img != null)
                        img.color = active
                            ? new Color(0.1f, 0.15f, 0.3f, 0.9f)
                            : new Color(0.1f, 0.1f, 0.15f, 0.5f);
                }
            }

            // Continue button is always enabled (to advance past tactical window)
            if (continueBtn != null)
                continueBtn.interactable = true;
        }

        /// <summary>
        /// Removes a tactical card from the hand display (after it has been played).
        /// </summary>
        public void RemoveTacticalCard(CardDefinitionSO card)
        {
            Debug.Log($"[CombatUI] RemoveTacticalCard: removing '{card?.cardName ?? "NULL"}' from hand display");

            for (int i = tacticalCardGOs.Count - 1; i >= 0; i--)
            {
                if (tacticalCardGOs[i] != null && tacticalCardGOs[i].name == $"Tactical_{card?.cardName}")
                {
                    Destroy(tacticalCardGOs[i]);
                    tacticalCardGOs.RemoveAt(i);
                    Debug.Log($"[CombatUI] RemoveTacticalCard: destroyed card GO at index={i}");
                    break;
                }
            }
        }

        /// <summary>
        /// Hides the combat overlay.
        /// </summary>
        public void HideCombat()
        {
            Debug.Log("[CombatUI] HideCombat: hiding combat panel");
            mainPanel.SetActive(false);
            ClearUnits();
            onContinue = null;
            onTacticalPlayed = null;
            cachedHeroes = null;
            cachedEnemies = null;
        }

        // ── Private Helpers ─────────────────────────────────────────────

        private void BuildHeroUnits(List<HeroToken> heroes)
        {
            Debug.Log($"[CombatUI] BuildHeroUnits: building {heroes?.Count ?? 0} hero cards");

            ClearUnitList(heroUnitCards);
            heroHPBars.Clear();
            heroHPTexts.Clear();
            heroStatTexts.Clear();

            if (heroes == null || heroes.Count == 0) return;

            for (int i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                string statsStr = $"CMB: {hero.EffectiveCombat}\nHP: {hero.currentHP}/{hero.maxHP}\nRole: {hero.cardDef.heroRole}";
                var cardGO = CreateUnitCard(
                    heroContainer,
                    $"Hero_{hero.cardDef.cardName}",
                    hero.cardDef.cardName,
                    statsStr,
                    new Color(0.15f, 0.25f, 0.35f, 0.9f),
                    i,
                    heroes.Count
                );

                // Track stat text for updates
                var statsTmp = cardGO.transform.Find("Stats")?.GetComponent<TextMeshProUGUI>();
                if (statsTmp != null)
                    heroStatTexts[hero.tokenId] = statsTmp;

                // HP bar
                float fillRatio = hero.maxHP > 0 ? (float)hero.currentHP / hero.maxHP : 1f;
                CreateHPBar(cardGO, hero.tokenId, fillRatio, $"{hero.currentHP}/{hero.maxHP}",
                    heroHPBars, heroHPTexts);

                heroUnitCards.Add(cardGO);
                Debug.Log($"[CombatUI] BuildHeroUnits: created card for hero={hero.cardDef.cardName}, tokenId={hero.tokenId}, hp={hero.currentHP}/{hero.maxHP}, combat={hero.EffectiveCombat}");
            }
        }

        private void BuildEnemyUnits(List<EnemyToken> enemies)
        {
            Debug.Log($"[CombatUI] BuildEnemyUnits: building {enemies?.Count ?? 0} enemy cards");

            ClearUnitList(enemyUnitCards);
            enemyHPBars.Clear();
            enemyHPTexts.Clear();

            if (enemies == null || enemies.Count == 0) return;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                var cardGO = CreateUnitCard(
                    enemyContainer,
                    $"Enemy_{enemy.definition.enemyName}",
                    enemy.definition.enemyName,
                    $"STR: {enemy.strength}\nHP: {enemy.currentHP}/{enemy.maxHP}\nBehavior: {enemy.behavior}",
                    new Color(0.35f, 0.12f, 0.12f, 0.9f),
                    i,
                    enemies.Count
                );

                // HP bar
                float fillRatio = enemy.maxHP > 0 ? (float)enemy.currentHP / enemy.maxHP : 1f;
                CreateHPBar(cardGO, enemy.tokenId, fillRatio, $"{enemy.currentHP}/{enemy.maxHP}",
                    enemyHPBars, enemyHPTexts);

                enemyUnitCards.Add(cardGO);
                Debug.Log($"[CombatUI] BuildEnemyUnits: created card for enemy={enemy.definition.enemyName}, tokenId={enemy.tokenId}, hp={enemy.currentHP}/{enemy.maxHP}, strength={enemy.strength}");
            }
        }

        private GameObject CreateUnitCard(RectTransform parent, string name, string unitName, string stats, Color bgColor, int index, int totalCount)
        {
            Debug.Log($"[CombatUI] CreateUnitCard: name={name}, index={index}, totalCount={totalCount}");

            float totalWidth = totalCount * (UnitCardWidth + UnitSpacing) - UnitSpacing;
            float startX = -totalWidth * 0.5f + UnitCardWidth * 0.5f;

            var cardGO = new GameObject(name, typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(parent, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(startX + index * (UnitCardWidth + UnitSpacing), 0);
            cardRect.sizeDelta = new Vector2(UnitCardWidth, UnitCardHeight);
            cardGO.GetComponent<Image>().color = bgColor;

            // Name
            var nameGO = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGO.transform.SetParent(cardGO.transform, false);
            var nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.75f);
            nameRect.anchorMax = new Vector2(1, 0.95f);
            nameRect.sizeDelta = Vector2.zero;
            nameRect.anchoredPosition = Vector2.zero;
            var nameTmp = nameGO.GetComponent<TextMeshProUGUI>();
            nameTmp.text = unitName;
            nameTmp.fontSize = 16;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = Color.white;

            // Stats
            var statsGO = new GameObject("Stats", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsGO.transform.SetParent(cardGO.transform, false);
            var statsRect = statsGO.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 0.20f);
            statsRect.anchorMax = new Vector2(1, 0.72f);
            statsRect.sizeDelta = Vector2.zero;
            statsRect.anchoredPosition = Vector2.zero;
            var statsTmp = statsGO.GetComponent<TextMeshProUGUI>();
            statsTmp.text = stats;
            statsTmp.fontSize = 13;
            statsTmp.alignment = TextAlignmentOptions.Center;
            statsTmp.color = new Color(0.9f, 0.9f, 0.9f);

            return cardGO;
        }

        private void CreateHPBar(GameObject parent, int tokenId, float fillRatio, string hpText,
            Dictionary<int, Image> hpBars, Dictionary<int, TextMeshProUGUI> hpTexts)
        {
            // HP bar background
            var barBG = new GameObject("HPBarBG", typeof(RectTransform), typeof(Image));
            barBG.transform.SetParent(parent.transform, false);
            var bgRect = barBG.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.05f, 0.05f);
            bgRect.anchorMax = new Vector2(0.95f, 0.05f);
            bgRect.pivot = new Vector2(0.5f, 0);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(0, HPBarHeight);
            barBG.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            // HP bar fill
            var barFill = new GameObject("HPBarFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBG.transform, false);
            var fillRect = barFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(fillRatio), 1f);
            fillRect.sizeDelta = Vector2.zero;
            var fillImage = barFill.GetComponent<Image>();
            fillImage.color = GetHPBarColor(fillRatio);

            hpBars[tokenId] = fillImage;

            // HP text
            var textGO = new GameObject("HPText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(barBG.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = hpText;
            tmp.fontSize = 10;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            hpTexts[tokenId] = tmp;

            Debug.Log($"[CombatUI] CreateHPBar: tokenId={tokenId}, fill={fillRatio:F2}, text='{hpText}'");
        }

        private void UpdateHPBarDirect(Image hpBar, TextMeshProUGUI hpText, float fillRatio, string text, bool isDefeated)
        {
            if (hpBar != null)
            {
                var fillRect = hpBar.GetComponent<RectTransform>();
                float prevFill = fillRect.anchorMax.x;
                float newFill = isDefeated ? 0f : Mathf.Clamp01(fillRatio);
                fillRect.anchorMax = new Vector2(newFill, 1f);
                hpBar.color = GetHPBarColor(newFill);
                Debug.Log($"[CombatUI] UpdateHPBarDirect: fill={prevFill:F2}->{newFill:F2}, defeated={isDefeated}");
            }

            if (hpText != null)
            {
                if (isDefeated)
                {
                    hpText.text = "DEFEATED";
                    hpText.color = Color.red;
                }
                else
                {
                    hpText.text = text;
                    hpText.color = Color.white;
                }
            }
        }

        private void UpdateHPBarEstimate(Image hpBar, TextMeshProUGUI hpText, int damage, bool isDefeated)
        {
            if (hpBar != null)
            {
                var fillRect = hpBar.GetComponent<RectTransform>();
                float currentFill = fillRect.anchorMax.x;
                float newFill = isDefeated ? 0f : Mathf.Max(0f, currentFill - 0.2f);
                fillRect.anchorMax = new Vector2(newFill, 1f);
                hpBar.color = GetHPBarColor(newFill);
                Debug.Log($"[CombatUI] UpdateHPBarEstimate: damage={damage}, fill={currentFill:F2}->{newFill:F2}, defeated={isDefeated}");
            }

            if (hpText != null && isDefeated)
            {
                hpText.text = "DEFEATED";
                hpText.color = Color.red;
            }
        }

        private void BuildTacticalCards(IReadOnlyList<CardDefinitionSO> cards)
        {
            Debug.Log($"[CombatUI] BuildTacticalCards: building {cards?.Count ?? 0} tactical cards");

            ClearUnitList(tacticalCardGOs);

            if (cards == null || cards.Count == 0) return;

            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];

                var cardGO = new GameObject($"Tactical_{card.cardName}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardGO.transform.SetParent(tacticalContainer, false);
                var cardRect = cardGO.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0, 0);
                cardRect.anchorMax = new Vector2(0, 1);
                cardRect.pivot = new Vector2(0, 0.5f);
                cardRect.anchoredPosition = new Vector2(i * (TacticalCardWidth + 8), 0);
                cardRect.sizeDelta = new Vector2(TacticalCardWidth, 0);
                cardGO.GetComponent<Image>().color = new Color(0.1f, 0.15f, 0.3f, 0.9f);

                // Card name
                var nameGO = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
                nameGO.transform.SetParent(cardGO.transform, false);
                var nameRect = nameGO.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 0.65f);
                nameRect.anchorMax = new Vector2(1, 0.95f);
                nameRect.sizeDelta = Vector2.zero;
                var nameTmp = nameGO.GetComponent<TextMeshProUGUI>();
                nameTmp.text = card.cardName;
                nameTmp.fontSize = 12;
                nameTmp.fontStyle = FontStyles.Bold;
                nameTmp.alignment = TextAlignmentOptions.Center;
                nameTmp.color = Color.white;

                // Tactical type badge
                var typeBadge = new GameObject("TypeBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
                typeBadge.transform.SetParent(cardGO.transform, false);
                var badgeRect = typeBadge.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(0, 0.55f);
                badgeRect.anchorMax = new Vector2(1, 0.65f);
                badgeRect.sizeDelta = Vector2.zero;
                var badgeTmp = typeBadge.GetComponent<TextMeshProUGUI>();
                badgeTmp.text = card.tacticalType.ToString();
                badgeTmp.fontSize = 9;
                badgeTmp.fontStyle = FontStyles.Italic;
                badgeTmp.alignment = TextAlignmentOptions.Center;
                badgeTmp.color = GetTacticalTypeColor(card.tacticalType);

                // Effect description
                var descGO = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
                descGO.transform.SetParent(cardGO.transform, false);
                var descRect = descGO.GetComponent<RectTransform>();
                descRect.anchorMin = new Vector2(0.05f, 0.05f);
                descRect.anchorMax = new Vector2(0.95f, 0.55f);
                descRect.sizeDelta = Vector2.zero;
                descRect.anchoredPosition = Vector2.zero;
                var descTmp = descGO.GetComponent<TextMeshProUGUI>();
                descTmp.text = card.tacticalEffectDescription;
                descTmp.fontSize = 10;
                descTmp.alignment = TextAlignmentOptions.Center;
                descTmp.color = new Color(0.8f, 0.85f, 1f);
                descTmp.richText = true;

                var capturedCard = card;
                cardGO.GetComponent<Button>().onClick.AddListener(() => OnTacticalCardClicked(capturedCard));

                tacticalCardGOs.Add(cardGO);
                Debug.Log($"[CombatUI] BuildTacticalCards: added tactical card={card.cardName}, type={card.tacticalType}");
            }
        }

        private Color GetTacticalTypeColor(TacticalType type)
        {
            switch (type)
            {
                case TacticalType.CombatTactic: return new Color(1f, 0.5f, 0.3f);
                case TacticalType.SupportTactic: return new Color(0.3f, 1f, 0.5f);
                case TacticalType.PowerTactic: return new Color(0.8f, 0.5f, 1f);
                default: return Color.white;
            }
        }

        private Color GetHPBarColor(float fillRatio)
        {
            if (fillRatio > 0.6f)
                return new Color(0.2f, 0.8f, 0.2f);
            if (fillRatio > 0.3f)
                return new Color(0.9f, 0.7f, 0.1f);
            return new Color(0.9f, 0.2f, 0.1f);
        }

        private void ClearUnitList(List<GameObject> list)
        {
            Debug.Log($"[CombatUI] ClearUnitList: destroying {list.Count} GameObjects");
            foreach (var go in list)
            {
                if (go != null) Destroy(go);
            }
            list.Clear();
        }

        private void ClearUnits()
        {
            ClearUnitList(heroUnitCards);
            ClearUnitList(enemyUnitCards);
            ClearUnitList(tacticalCardGOs);
            heroHPBars.Clear();
            heroHPTexts.Clear();
            heroStatTexts.Clear();
            enemyHPBars.Clear();
            enemyHPTexts.Clear();
            Debug.Log("[CombatUI] ClearUnits: all unit and tactical card GOs destroyed");
        }

        // ── UI Factory Helpers ──────────────────────────────────────────

        private TextMeshProUGUI CreateText(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta,
            string text, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            return tmp;
        }

        private GameObject CreateButton(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            string label, float fontSize, Color bgColor)
        {
            var btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = anchorMin;
            btnRect.anchorMax = anchorMax;
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = Vector2.zero;
            btnRect.sizeDelta = Vector2.zero;
            btnGO.GetComponent<Image>().color = bgColor;

            var btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGO.transform.SetParent(btnGO.transform, false);
            var btnTextRect = btnTextGO.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;
            var btnTmp = btnTextGO.GetComponent<TextMeshProUGUI>();
            btnTmp.text = label;
            btnTmp.fontSize = fontSize;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;

            return btnGO;
        }

        // ── Combat Spectacle (Phase 5) ──────────────────────────────────

        /// <summary>
        /// Spawns a floating damage number at the position of a unit card.
        /// Red for damage, green for heals.
        /// </summary>
        public void SpawnFloatingNumber(int targetId, int amount, bool isHero, bool isHeal = false)
        {
            Debug.Log($"[CombatUI] SpawnFloatingNumber: targetId={targetId}, amount={amount}, isHero={isHero}, isHeal={isHeal}");

            if (amount == 0) return;

            // Find the unit card's position
            RectTransform parent = isHero ? heroContainer : enemyContainer;
            if (parent == null) return;

            string prefix = isHeal ? "+" : "-";
            Color color = isHeal ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.2f, 0.2f);

            var floatGO = new GameObject("FloatingDmg", typeof(RectTransform), typeof(TextMeshProUGUI));
            floatGO.transform.SetParent(mainPanel.transform, false);
            var floatRect = floatGO.GetComponent<RectTransform>();
            // Position near center of the unit container
            floatRect.anchorMin = new Vector2(isHero ? 0.22f : 0.78f, 0.55f);
            floatRect.anchorMax = new Vector2(isHero ? 0.22f : 0.78f, 0.55f);
            floatRect.pivot = new Vector2(0.5f, 0.5f);
            floatRect.anchoredPosition = new Vector2(Random.Range(-30f, 30f), Random.Range(-10f, 10f));
            floatRect.sizeDelta = new Vector2(100, 40);

            var tmp = floatGO.GetComponent<TextMeshProUGUI>();
            tmp.text = $"{prefix}{amount}";
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;

            // Animate: float up and fade out
            StartCoroutine(AnimateFloatingNumber(floatGO, 1.2f));
        }

        /// <summary>
        /// Shows an ability callout banner (e.g., "FIRST STRIKE!", "CLEAVE!").
        /// </summary>
        public void ShowAbilityCallout(string abilityName)
        {
            Debug.Log($"[CombatUI] ShowAbilityCallout: '{abilityName}'");

            if (string.IsNullOrEmpty(abilityName)) return;

            var calloutGO = new GameObject("AbilityCallout", typeof(RectTransform), typeof(TextMeshProUGUI));
            calloutGO.transform.SetParent(mainPanel.transform, false);
            var calloutRect = calloutGO.GetComponent<RectTransform>();
            calloutRect.anchorMin = new Vector2(0.3f, 0.45f);
            calloutRect.anchorMax = new Vector2(0.7f, 0.55f);
            calloutRect.sizeDelta = Vector2.zero;

            var tmp = calloutGO.GetComponent<TextMeshProUGUI>();
            tmp.text = abilityName.ToUpper() + "!";
            tmp.fontSize = 32;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.9f, 0.3f);

            StartCoroutine(AnimateCallout(calloutGO, 1.5f));
        }

        /// <summary>
        /// Flashes the VS text with round outcome color.
        /// </summary>
        public void FlashRoundOutcome(bool heroesWonRound)
        {
            Debug.Log($"[CombatUI] FlashRoundOutcome: heroesWonRound={heroesWonRound}");

            if (vsText != null)
            {
                Color flashColor = heroesWonRound
                    ? new Color(0.3f, 1f, 0.3f)
                    : new Color(1f, 0.3f, 0.3f);
                StartCoroutine(FlashText(vsText, flashColor, 0.5f));
            }
        }

        /// <summary>
        /// Triggers screen shake on the combat panel for large damage events.
        /// </summary>
        public void TriggerScreenShake(float intensity = 5f)
        {
            Debug.Log($"[CombatUI] TriggerScreenShake: intensity={intensity}");

            var shake = mainPanel.GetComponent<ScreenShake>();
            if (shake == null)
                shake = mainPanel.AddComponent<ScreenShake>();
            shake.Shake(intensity, 0.25f);
        }

        private System.Collections.IEnumerator AnimateFloatingNumber(GameObject go, float duration)
        {
            if (go == null) yield break;
            var rect = go.GetComponent<RectTransform>();
            var tmp = go.GetComponent<TextMeshProUGUI>();
            float elapsed = 0f;
            Vector2 startPos = rect.anchoredPosition;

            while (elapsed < duration && go != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rect.anchoredPosition = startPos + new Vector2(0, 40 * t);
                if (tmp != null)
                {
                    var c = tmp.color;
                    c.a = 1f - t;
                    tmp.color = c;
                }
                yield return null;
            }

            if (go != null) Destroy(go);
        }

        private System.Collections.IEnumerator AnimateCallout(GameObject go, float duration)
        {
            if (go == null) yield break;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            float elapsed = 0f;

            while (elapsed < duration && go != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Scale up quickly then fade out
                float scale = t < 0.2f ? Mathf.Lerp(0.5f, 1.2f, t / 0.2f) : Mathf.Lerp(1.2f, 1f, (t - 0.2f) / 0.8f);
                go.transform.localScale = Vector3.one * scale;

                if (tmp != null && t > 0.5f)
                {
                    var c = tmp.color;
                    c.a = 1f - ((t - 0.5f) / 0.5f);
                    tmp.color = c;
                }
                yield return null;
            }

            if (go != null) Destroy(go);
        }

        private System.Collections.IEnumerator FlashText(TextMeshProUGUI text, Color flashColor, float duration)
        {
            if (text == null) yield break;
            Color originalColor = text.color;
            text.color = flashColor;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                text.color = Color.Lerp(flashColor, originalColor, t);
                yield return null;
            }
            text.color = originalColor;
        }

        private void OnDestroy()
        {
            Debug.Log("[CombatUI] OnDestroy: cleaning up");
            ClearUnits();
        }
    }
}
