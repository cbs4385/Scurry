using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Scurry.Core;
using Scurry.Data;
using Scurry.Cards;
using Scurry.Combat;
using Scurry.Map;
using Scurry.Interfaces;

namespace Scurry.UI
{
    /// <summary>
    /// Lightweight tag component to store a card reference and list index on a UI GameObject.
    /// Used for exact-match selection highlighting when multiple copies of the same card exist.
    /// </summary>
    public class CardTag : MonoBehaviour
    {
        public CardDefinitionSO card;
        public int index;
    }

    /// <summary>
    /// Full-screen Deploy phase UI showing available heroes with card art,
    /// hero detail with large artwork, equipment slots, and deployed heroes bar.
    /// </summary>
    public class DeploymentUI : MonoBehaviour
    {
        // --- Layout Constants ---
        private const float CardWidth = 140f;
        private const float CardHeight = 190f;
        private const float CardSpacing = 10f;
        private const float DeployedCardWidth = 100f;
        private const float DeployedCardHeight = 50f;
        private const float EquipSlotHeight = 70f;

        // --- Colors ---
        private static readonly Color BgColor = new Color(0.05f, 0.05f, 0.08f, 0.97f);
        private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.12f, 0.9f);
        private static readonly Color HeroCardColor = new Color(0.12f, 0.15f, 0.25f, 0.9f);
        private static readonly Color HeroCardSelectedColor = new Color(0.2f, 0.35f, 0.2f, 0.95f);
        private static readonly Color EquipSlotColor = new Color(0.15f, 0.12f, 0.08f, 0.85f);
        private static readonly Color EquipSlotEmptyColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);
        private static readonly Color DeployBtnColor = new Color(0.15f, 0.45f, 0.15f, 1f);
        private static readonly Color EndPhaseBtnColor = new Color(0.5f, 0.2f, 0.2f, 1f);
        private static readonly Color GoldText = new Color(0.9f, 0.75f, 0.1f);
        private static readonly Color BlueText = new Color(0.6f, 0.8f, 1f);
        private static readonly Color GreenText = new Color(0.6f, 0.9f, 0.6f);

        // --- UI References ---
        private Canvas canvas;
        private GameObject mainPanel;

        // Left: hero grid
        private RectTransform heroGridContainer;
        private ScrollRect heroScrollRect;
        private readonly List<GameObject> heroListItems = new List<GameObject>();

        // Center: detail panel
        private Image detailArtworkImage;
        private TextMeshProUGUI detailNameText;
        private TextMeshProUGUI detailRoleText;
        private TextMeshProUGUI detailStatsText;
        private TextMeshProUGUI detailAbilityText;

        // Right: equipment slots
        private RectTransform equipListContainer;
        private readonly List<GameObject> equipListItems = new List<GameObject>();
        private Image offensiveSlotImage;
        private TextMeshProUGUI offensiveSlotText;
        private Image defensiveSlotImage;
        private TextMeshProUGUI defensiveSlotText;
        private Image utilitySlotImage;
        private TextMeshProUGUI utilitySlotText;

        // Bottom: deployed bar
        private RectTransform deployedListContainer;
        private readonly List<GameObject> deployedListItems = new List<GameObject>();

        // Buttons
        private Button deployBtn;
        private Button endPhaseBtn;

        // --- State ---
        private DeckManager deckManager;
        private ITurnManager turnManager;
        private CardDefinitionSO selectedHero;
        private int selectedHeroIndex = -1;

        // Per-slot equipment selections for the hero being staged
        private CardDefinitionSO slottedOffensive;
        private CardDefinitionSO slottedDefensive;
        private CardDefinitionSO slottedUtility;

        // Staged deploys — buffered locally until End Phase is clicked
        private struct StagedDeploy
        {
            public CardDefinitionSO heroDef;
            public CardDefinitionSO offensive;
            public CardDefinitionSO defensive;
            public CardDefinitionSO utility;
        }
        private readonly List<StagedDeploy> stagedDeploys = new List<StagedDeploy>();

        // Callback references
        private System.Action<CardDefinitionSO, int> onDeployRequested;
        private System.Action<int> onRetargetRequested;

        private void Awake()
        {
            Debug.Log("[DeploymentUI] Awake: initializing");
            BuildUI();
        }

        // ─── Build ───────────────────────────────────────────────────────

        private void BuildUI()
        {
            Debug.Log("[DeploymentUI] BuildUI: starting construction");

            UIHelper.EnsureEventSystem();

            // Background camera
            var camGO = new GameObject("BackgroundCamera", typeof(Camera));
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f, 1f);
            cam.cullingMask = 0;
            cam.depth = -100;
            Debug.Log("[DeploymentUI] BuildUI: background camera created");

            // Canvas
            canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // Main panel — full screen
            mainPanel = new GameObject("DeployPanel", typeof(RectTransform), typeof(Image));
            mainPanel.transform.SetParent(transform, false);
            var mainRect = mainPanel.GetComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            mainRect.sizeDelta = Vector2.zero;
            mainPanel.GetComponent<Image>().color = BgColor;

            // --- Header bar ---
            BuildHeader();

            // --- Left: Hero grid (40% width) ---
            BuildHeroGrid();

            // --- Center: Hero detail (30% width) ---
            BuildDetailPanel();

            // --- Right: Equipment panel (30% width) ---
            BuildEquipmentPanel();

            // --- Bottom: Deployed heroes bar ---
            BuildDeployedBar();

            mainPanel.SetActive(false);
            Debug.Log("[DeploymentUI] BuildUI: complete (panel hidden)");
        }

        private void BuildHeader()
        {
            var header = CreatePanel(mainPanel.transform, "Header",
                new Vector2(0, 0.92f), new Vector2(1, 1), PanelColor);

            // Title
            var title = CreateText(header.transform, "Title", "DEPLOY HEROES", 28,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, GoldText);
            SetAnchors(title, new Vector2(0.02f, 0), new Vector2(0.5f, 1));

            // Turn info
            var turnInfo = CreateText(header.transform, "TurnInfo", "Turn 1", 18,
                FontStyles.Normal, TextAlignmentOptions.MidlineRight, BlueText);
            SetAnchors(turnInfo, new Vector2(0.5f, 0), new Vector2(0.82f, 1));

            // End Phase button
            var endBtnGO = CreateButton(header.transform, "EndPhaseBtn", "End Phase",
                new Vector2(0.84f, 0.15f), new Vector2(0.98f, 0.85f), EndPhaseBtnColor, 18);
            endPhaseBtn = endBtnGO.GetComponent<Button>();
            endPhaseBtn.onClick.AddListener(OnEndPhaseClicked);
            Debug.Log("[DeploymentUI] BuildHeader: complete");
        }

        private void BuildHeroGrid()
        {
            Debug.Log("[DeploymentUI] BuildHeroGrid: creating hero grid");

            // Section panel
            var section = CreatePanel(mainPanel.transform, "HeroSection",
                new Vector2(0.01f, 0.12f), new Vector2(0.40f, 0.91f), PanelColor);

            // Label
            var label = CreateText(section.transform, "Label", "Available Heroes", 18,
                FontStyles.Bold, TextAlignmentOptions.TopLeft, BlueText);
            SetAnchors(label, new Vector2(0.03f, 0.93f), new Vector2(0.97f, 1f));

            // Scroll area
            var scrollGO = new GameObject("HeroScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(section.transform, false);
            SetAnchors(scrollGO, new Vector2(0, 0), new Vector2(1, 0.92f));

            // Mask
            var maskGO = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGO.transform.SetParent(scrollGO.transform, false);
            SetAnchors(maskGO, Vector2.zero, Vector2.one);
            maskGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            maskGO.GetComponent<Mask>().showMaskGraphic = false;

            // Content container
            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(maskGO.transform, false);
            heroGridContainer = contentGO.GetComponent<RectTransform>();
            heroGridContainer.anchorMin = new Vector2(0, 1);
            heroGridContainer.anchorMax = new Vector2(1, 1);
            heroGridContainer.pivot = new Vector2(0.5f, 1);
            heroGridContainer.anchoredPosition = Vector2.zero;
            heroGridContainer.sizeDelta = new Vector2(0, 0);

            heroScrollRect = scrollGO.GetComponent<ScrollRect>();
            heroScrollRect.content = heroGridContainer;
            heroScrollRect.horizontal = false;
            heroScrollRect.vertical = true;
            heroScrollRect.viewport = maskGO.GetComponent<RectTransform>();

            Debug.Log("[DeploymentUI] BuildHeroGrid: complete");
        }

        private void BuildDetailPanel()
        {
            Debug.Log("[DeploymentUI] BuildDetailPanel: creating detail display");

            var section = CreatePanel(mainPanel.transform, "DetailSection",
                new Vector2(0.41f, 0.12f), new Vector2(0.70f, 0.91f), PanelColor);

            // Large artwork area (top 55%)
            var artBg = new GameObject("ArtworkBg", typeof(RectTransform), typeof(Image));
            artBg.transform.SetParent(section.transform, false);
            SetAnchors(artBg, new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.96f));
            artBg.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 1f);

            var artGO = new GameObject("Artwork", typeof(RectTransform), typeof(Image));
            artGO.transform.SetParent(artBg.transform, false);
            SetAnchors(artGO, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.98f));
            detailArtworkImage = artGO.GetComponent<Image>();
            detailArtworkImage.color = new Color(0.15f, 0.15f, 0.2f, 0.5f);
            detailArtworkImage.preserveAspect = true;

            // Name + Role
            detailNameText = CreateText(section.transform, "DetailName", "", 22,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white).GetComponent<TextMeshProUGUI>();
            SetAnchors(detailNameText.gameObject, new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.42f));

            detailRoleText = CreateText(section.transform, "DetailRole", "", 14,
                FontStyles.Italic, TextAlignmentOptions.MidlineLeft, BlueText).GetComponent<TextMeshProUGUI>();
            SetAnchors(detailRoleText.gameObject, new Vector2(0.05f, 0.29f), new Vector2(0.95f, 0.35f));

            // Stats grid
            detailStatsText = CreateText(section.transform, "DetailStats", "", 15,
                FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.9f, 0.9f, 0.9f)).GetComponent<TextMeshProUGUI>();
            SetAnchors(detailStatsText.gameObject, new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.30f));
            detailStatsText.richText = true;

            // Ability
            detailAbilityText = CreateText(section.transform, "DetailAbility", "", 13,
                FontStyles.Italic, TextAlignmentOptions.TopLeft, GreenText).GetComponent<TextMeshProUGUI>();
            SetAnchors(detailAbilityText.gameObject, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.14f));

            // Deploy button
            var deployBtnGO = CreateButton(section.transform, "DeployBtn", "DEPLOY HERO",
                new Vector2(0.1f, 0.01f), new Vector2(0.9f, 0.06f), DeployBtnColor, 18);
            deployBtn = deployBtnGO.GetComponent<Button>();
            deployBtn.onClick.AddListener(OnDeployClicked);

            Debug.Log("[DeploymentUI] BuildDetailPanel: complete");
        }

        private void BuildEquipmentPanel()
        {
            Debug.Log("[DeploymentUI] BuildEquipmentPanel: creating equipment panel");

            var section = CreatePanel(mainPanel.transform, "EquipSection",
                new Vector2(0.71f, 0.12f), new Vector2(0.99f, 0.91f), PanelColor);

            // Label
            var label = CreateText(section.transform, "Label", "Equipment", 18,
                FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(1f, 0.8f, 0.4f));
            SetAnchors(label, new Vector2(0.05f, 0.93f), new Vector2(0.95f, 1f));

            // Equipment scroll area (top portion for available equipment)
            var scrollGO = new GameObject("EquipScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(section.transform, false);
            SetAnchors(scrollGO, new Vector2(0, 0.40f), new Vector2(1, 0.92f));

            var maskGO = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGO.transform.SetParent(scrollGO.transform, false);
            SetAnchors(maskGO, Vector2.zero, Vector2.one);
            maskGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            maskGO.GetComponent<Mask>().showMaskGraphic = false;

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(maskGO.transform, false);
            equipListContainer = contentGO.GetComponent<RectTransform>();
            equipListContainer.anchorMin = new Vector2(0, 1);
            equipListContainer.anchorMax = new Vector2(1, 1);
            equipListContainer.pivot = new Vector2(0.5f, 1);
            equipListContainer.anchoredPosition = Vector2.zero;
            equipListContainer.sizeDelta = new Vector2(0, 0);

            var equipScroll = scrollGO.GetComponent<ScrollRect>();
            equipScroll.content = equipListContainer;
            equipScroll.horizontal = false;
            equipScroll.vertical = true;
            equipScroll.viewport = maskGO.GetComponent<RectTransform>();

            // Equipment slots label
            var slotsLabel = CreateText(section.transform, "SlotsLabel", "Equipment Slots", 15,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.8f, 0.7f, 0.5f));
            SetAnchors(slotsLabel, new Vector2(0.05f, 0.33f), new Vector2(0.95f, 0.39f));

            // 3 equipment slots (Offensive, Defensive, Utility)
            BuildEquipSlot(section.transform, "Offensive", "Offensive", 0.22f, 0.32f,
                out offensiveSlotImage, out offensiveSlotText);
            BuildEquipSlot(section.transform, "Defensive", "Defensive", 0.11f, 0.21f,
                out defensiveSlotImage, out defensiveSlotText);
            BuildEquipSlot(section.transform, "Utility", "Utility", 0.0f, 0.10f,
                out utilitySlotImage, out utilitySlotText);

            Debug.Log("[DeploymentUI] BuildEquipmentPanel: complete");
        }

        private void BuildEquipSlot(Transform parent, string name, string label,
            float yMin, float yMax, out Image slotImage, out TextMeshProUGUI slotText)
        {
            var slotGO = CreatePanel(parent, $"Slot_{name}",
                new Vector2(0.03f, yMin), new Vector2(0.97f, yMax), EquipSlotEmptyColor);

            // Slot label
            var labelTxt = CreateText(slotGO.transform, "SlotLabel", label, 12,
                FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.7f, 0.6f, 0.4f));
            SetAnchors(labelTxt, new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.95f));

            // Equipment name
            slotText = CreateText(slotGO.transform, "SlotContent", "— empty —", 13,
                FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.5f, 0.5f, 0.5f)).GetComponent<TextMeshProUGUI>();
            SetAnchors(slotText.gameObject, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.6f));

            slotImage = slotGO.GetComponent<Image>();
        }

        private void BuildDeployedBar()
        {
            Debug.Log("[DeploymentUI] BuildDeployedBar: creating deployed heroes bar");

            var bar = CreatePanel(mainPanel.transform, "DeployedBar",
                new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.11f), PanelColor);

            // Label
            var label = CreateText(bar.transform, "DeployedLabel", "Deployed Heroes", 16,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, GreenText);
            SetAnchors(label, new Vector2(0.01f, 0.6f), new Vector2(0.2f, 0.95f));

            // Container for deployed hero cards
            var containerGO = new GameObject("DeployedContainer", typeof(RectTransform));
            containerGO.transform.SetParent(bar.transform, false);
            deployedListContainer = containerGO.GetComponent<RectTransform>();
            SetAnchors(containerGO, new Vector2(0.01f, 0.05f), new Vector2(0.99f, 0.6f));

            Debug.Log("[DeploymentUI] BuildDeployedBar: complete");
        }

        // ─── Public Methods ──────────────────────────────────────────────

        /// <summary>
        /// Shows the deployment panel with hero and equipment data from the deck manager.
        /// </summary>
        public void Show(DeckManager deck, ITurnManager turnMgr)
        {
            Debug.Log($"[DeploymentUI] Show: deck={(deck != null ? "OK" : "NULL")}, turnMgr={(turnMgr != null ? "OK" : "NULL")}");

            deckManager = deck;
            turnManager = turnMgr;
            selectedHero = null;
            selectedHeroIndex = -1;
            slottedOffensive = null;
            slottedDefensive = null;
            slottedUtility = null;
            stagedDeploys.Clear();

            mainPanel.SetActive(true);
            RefreshDisplay();
            RefreshStagedBar();
            UpdateDeployButtonState();

            Debug.Log("[DeploymentUI] Show: panel activated");
        }

        /// <summary>
        /// Hides the deployment panel.
        /// </summary>
        public void Hide()
        {
            Debug.Log("[DeploymentUI] Hide: hiding panel");
            mainPanel.SetActive(false);
            selectedHero = null;
            selectedHeroIndex = -1;
            slottedOffensive = null;
            slottedDefensive = null;
            slottedUtility = null;
        }

        /// <summary>
        /// Called when a hero card is selected from the available list.
        /// </summary>
        public void OnHeroSelected(CardDefinitionSO hero, int index)
        {
            Debug.Log($"[DeploymentUI] OnHeroSelected: hero={hero?.cardName ?? "NULL"} (index={index}), role={hero?.heroRole}, combat={hero?.combat}, move={hero?.move}, hp={hero?.hp}, carry={hero?.carry}, init={hero?.initiative}");

            selectedHero = hero;
            selectedHeroIndex = index;

            // Update detail artwork
            if (detailArtworkImage != null)
            {
                if (hero.artwork != null)
                {
                    detailArtworkImage.sprite = hero.artwork;
                    detailArtworkImage.color = Color.white;
                }
                else
                {
                    detailArtworkImage.sprite = null;
                    detailArtworkImage.color = hero.placeholderColor != default
                        ? hero.placeholderColor : new Color(0.15f, 0.15f, 0.2f, 0.5f);
                }
            }

            // Update detail text
            if (detailNameText != null)
                detailNameText.text = hero.cardName;
            if (detailRoleText != null)
                detailRoleText.text = $"{hero.heroRole} Hero";
            if (detailStatsText != null)
                detailStatsText.text = $"<b>Combat:</b> {hero.combat}    <b>Move:</b> {hero.move}    <b>HP:</b> {hero.hp}\n<b>Carry:</b> {hero.carry}    <b>Initiative:</b> {hero.initiative}";
            if (detailAbilityText != null)
                detailAbilityText.text = hero.specialAbility != SpecialAbility.None
                    ? $"Ability: {hero.specialAbilityDescription}" : "";

            // Highlight selected hero in grid (by index to distinguish duplicates)
            HighlightSelectedItem(heroListItems, index);
        }

        /// <summary>
        /// Called when an equipment card is selected.
        /// </summary>
        public void OnEquipmentSelected(CardDefinitionSO equip, EquipmentSlot slot, int index)
        {
            Debug.Log($"[DeploymentUI] OnEquipmentSelected: equip={equip?.cardName ?? "NULL"} (index={index}), slot={slot}");

            // Store into the correct slot
            switch (slot)
            {
                case EquipmentSlot.Offensive:
                    slottedOffensive = equip;
                    break;
                case EquipmentSlot.Defensive:
                    slottedDefensive = equip;
                    break;
                case EquipmentSlot.Utility:
                    slottedUtility = equip;
                    break;
            }

            // Update the slot display
            Image slotImg = null;
            TextMeshProUGUI slotTxt = null;
            switch (slot)
            {
                case EquipmentSlot.Offensive:
                    slotImg = offensiveSlotImage;
                    slotTxt = offensiveSlotText;
                    break;
                case EquipmentSlot.Defensive:
                    slotImg = defensiveSlotImage;
                    slotTxt = defensiveSlotText;
                    break;
                case EquipmentSlot.Utility:
                    slotImg = utilitySlotImage;
                    slotTxt = utilitySlotText;
                    break;
            }

            if (slotImg != null) slotImg.color = EquipSlotColor;
            if (slotTxt != null) slotTxt.text = $"{equip.cardName}\n<size=11>{equip.equipmentEffectDescription}</size>";
            if (slotTxt != null) slotTxt.color = Color.white;

            Debug.Log($"[DeploymentUI] OnEquipmentSelected: slotted equipment " +
                $"(offensive={slottedOffensive?.cardName ?? "none"}, defensive={slottedDefensive?.cardName ?? "none"}, utility={slottedUtility?.cardName ?? "none"})");
        }

        /// <summary>
        /// Called when the Deploy button is clicked.
        /// </summary>
        public void OnDeployClicked()
        {
            Debug.Log($"[DeploymentUI] OnDeployClicked: selectedHero={(selectedHero != null ? selectedHero.cardName : "NULL")}");

            if (selectedHero == null)
            {
                Debug.LogWarning("[DeploymentUI] OnDeployClicked: no hero selected, cannot deploy");
                EventBus.OnNotification?.Invoke("Select a hero first!", Color.red);
                return;
            }

            if (selectedHero.cardType != CardType.Hero)
            {
                Debug.LogWarning($"[DeploymentUI] OnDeployClicked: selected card is not a hero (type={selectedHero.cardType})");
                return;
            }

            // Check deploy cap (including already-staged)
            int deployed = turnManager.HeroesDeployedThisTurn + stagedDeploys.Count;
            if (deployed >= turnManager.MaxHeroDeploysPerTurn)
            {
                Debug.LogWarning($"[DeploymentUI] OnDeployClicked: deploy cap reached ({deployed}/{turnManager.MaxHeroDeploysPerTurn})");
                EventBus.OnNotification?.Invoke("Deploy limit reached!", Color.red);
                return;
            }

            // Stage the deploy locally with equipped items
            var staged = new StagedDeploy
            {
                heroDef = selectedHero,
                offensive = slottedOffensive,
                defensive = slottedDefensive,
                utility = slottedUtility
            };
            stagedDeploys.Add(staged);

            string equipInfo = "";
            if (slottedOffensive != null) equipInfo += $" +{slottedOffensive.cardName}";
            if (slottedDefensive != null) equipInfo += $" +{slottedDefensive.cardName}";
            if (slottedUtility != null) equipInfo += $" +{slottedUtility.cardName}";
            Debug.Log($"[DeploymentUI] OnDeployClicked: staged hero={selectedHero.cardName}{equipInfo} (stagedCount={stagedDeploys.Count})");
            EventBus.OnNotification?.Invoke($"Staged {selectedHero.cardName} for deployment", new Color(0.7f, 0.85f, 1f));

            selectedHero = null;
            selectedHeroIndex = -1;
            slottedOffensive = null;
            slottedDefensive = null;
            slottedUtility = null;

            // Refresh UI to move hero + equipment from available to staged bar
            RefreshDisplay();
            RefreshStagedBar();
            UpdateDeployButtonState();
            ClearDetailPanel();
        }

        /// <summary>
        /// Removes a staged deploy, returning the hero to the available list.
        /// Called when clicking a hero in the staged/deployed bar.
        /// </summary>
        private void UnstageDeploy(int stagedIndex)
        {
            if (stagedIndex < 0 || stagedIndex >= stagedDeploys.Count) return;

            var unstaged = stagedDeploys[stagedIndex];
            Debug.Log($"[DeploymentUI] UnstageDeploy: removing staged hero={unstaged.heroDef.cardName} (index={stagedIndex})");
            stagedDeploys.RemoveAt(stagedIndex);

            EventBus.OnNotification?.Invoke($"Removed {unstaged.heroDef.cardName} from deployment", new Color(0.9f, 0.8f, 0.4f));

            RefreshDisplay();
            RefreshStagedBar();
            UpdateDeployButtonState();
        }

        /// <summary>
        /// Commits all staged deploys to TurnManager, then ends the phase.
        /// </summary>
        private void OnEndPhaseClicked()
        {
            Debug.Log($"[DeploymentUI] OnEndPhaseClicked: committing {stagedDeploys.Count} staged deploys");

            var tm = TurnManager.Instance;
            if (tm == null)
            {
                Debug.LogError("[DeploymentUI] OnEndPhaseClicked: TurnManager not found");
                return;
            }

            // Commit hero deploys
            int targetNodeId = tm.ColonyNodeId;
            foreach (var staged in stagedDeploys)
            {
                tm.PlayerDeployHero(staged.heroDef, targetNodeId, staged.offensive, staged.defensive, staged.utility);
                Debug.Log($"[DeploymentUI] OnEndPhaseClicked: committed hero={staged.heroDef.cardName}");
            }
            stagedDeploys.Clear();

            tm.PlayerEndPhase();
            Debug.Log("[DeploymentUI] OnEndPhaseClicked: phase end requested");
        }

        private void ClearDetailPanel()
        {
            if (detailNameText != null) detailNameText.text = "";
            if (detailRoleText != null) detailRoleText.text = "";
            if (detailStatsText != null) detailStatsText.text = "";
            if (detailAbilityText != null) detailAbilityText.text = "";
            if (detailArtworkImage != null)
            {
                detailArtworkImage.sprite = null;
                detailArtworkImage.color = new Color(0.15f, 0.15f, 0.2f, 0.5f);
            }
        }

        /// <summary>
        /// Called when a retarget button is clicked for a deployed hero.
        /// </summary>
        public void OnRetargetClicked(int heroTokenId)
        {
            Debug.Log($"[DeploymentUI] OnRetargetClicked: heroTokenId={heroTokenId}");
            EventBus.OnNotification?.Invoke("Click a map node to set new target.", new Color(0.9f, 0.8f, 0.4f));
        }

        /// <summary>
        /// Refreshes all lists and displays.
        /// </summary>
        public void RefreshDisplay()
        {
            Debug.Log("[DeploymentUI] RefreshDisplay: refreshing hero and equipment lists");

            if (deckManager != null)
            {
                // Filter out staged heroes from available list
                var availableHeroes = new List<CardDefinitionSO>();
                var stagedHeroDefs = new HashSet<CardDefinitionSO>();
                // Build a count-based filter so multiple copies are handled correctly
                var stagedCounts = new Dictionary<CardDefinitionSO, int>();
                foreach (var staged in stagedDeploys)
                {
                    if (!stagedCounts.ContainsKey(staged.heroDef))
                        stagedCounts[staged.heroDef] = 0;
                    stagedCounts[staged.heroDef]++;
                }

                var remainingCounts = new Dictionary<CardDefinitionSO, int>(stagedCounts);
                foreach (var hero in deckManager.AvailableHeroes)
                {
                    if (remainingCounts.ContainsKey(hero) && remainingCounts[hero] > 0)
                    {
                        remainingCounts[hero]--;
                        continue; // Skip this copy — it's staged
                    }
                    availableHeroes.Add(hero);
                }

                // Filter out staged equipment from available list
                var availableEquipment = new List<CardDefinitionSO>();
                var stagedEquipCounts = new Dictionary<CardDefinitionSO, int>();
                foreach (var staged in stagedDeploys)
                {
                    foreach (var eq in new[] { staged.offensive, staged.defensive, staged.utility })
                    {
                        if (eq == null) continue;
                        if (!stagedEquipCounts.ContainsKey(eq))
                            stagedEquipCounts[eq] = 0;
                        stagedEquipCounts[eq]++;
                    }
                }
                // Also filter currently slotted equipment (not yet staged but assigned to slots)
                foreach (var eq in new[] { slottedOffensive, slottedDefensive, slottedUtility })
                {
                    if (eq == null) continue;
                    if (!stagedEquipCounts.ContainsKey(eq))
                        stagedEquipCounts[eq] = 0;
                    stagedEquipCounts[eq]++;
                }

                var remainingEquipCounts = new Dictionary<CardDefinitionSO, int>(stagedEquipCounts);
                foreach (var equip in deckManager.AvailableEquipment)
                {
                    if (remainingEquipCounts.ContainsKey(equip) && remainingEquipCounts[equip] > 0)
                    {
                        remainingEquipCounts[equip]--;
                        continue;
                    }
                    availableEquipment.Add(equip);
                }

                PopulateHeroes(availableHeroes);
                PopulateEquipment(availableEquipment);
                Debug.Log($"[DeploymentUI] RefreshDisplay: populated heroes={availableHeroes.Count} (staged={stagedDeploys.Count}), equipment={availableEquipment.Count}");
            }
            else
            {
                ClearList(heroListItems, heroGridContainer);
                ClearList(equipListItems, equipListContainer);
                ClearList(deployedListItems, deployedListContainer);
                Debug.LogWarning("[DeploymentUI] RefreshDisplay: deckManager is null, lists cleared");
            }

            // Reset equipment slots
            ResetEquipSlots();
        }

        /// <summary>
        /// Populates the hero grid with the given hero cards.
        /// </summary>
        public void PopulateHeroes(IReadOnlyList<CardDefinitionSO> heroes)
        {
            Debug.Log($"[DeploymentUI] PopulateHeroes: heroes={heroes?.Count ?? 0}");

            ClearList(heroListItems, heroGridContainer);

            if (heroes == null || heroes.Count == 0)
            {
                Debug.Log("[DeploymentUI] PopulateHeroes: no heroes available");
                return;
            }

            // Grid layout: calculate columns based on container width
            int columns = 3;
            float cellW = CardWidth + CardSpacing;
            float cellH = CardHeight + CardSpacing;
            int rows = Mathf.CeilToInt((float)heroes.Count / columns);
            float contentHeight = rows * cellH + CardSpacing;
            heroGridContainer.sizeDelta = new Vector2(heroGridContainer.sizeDelta.x, contentHeight);

            for (int i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                int col = i % columns;
                int row = i / columns;

                var cardGO = CreateHeroCard(hero, i);
                cardGO.transform.SetParent(heroGridContainer, false);
                var cardRect = cardGO.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0, 1);
                cardRect.anchorMax = new Vector2(0, 1);
                cardRect.pivot = new Vector2(0, 1);
                cardRect.anchoredPosition = new Vector2(
                    CardSpacing + col * cellW,
                    -(CardSpacing + row * cellH));
                cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);

                heroListItems.Add(cardGO);
                Debug.Log($"[DeploymentUI] PopulateHeroes: added hero={hero.cardName}, combat={hero.combat}, move={hero.move}, hp={hero.hp}");
            }
        }

        /// <summary>
        /// Populates the equipment list with the given equipment cards.
        /// </summary>
        public void PopulateEquipment(IReadOnlyList<CardDefinitionSO> equipment)
        {
            Debug.Log($"[DeploymentUI] PopulateEquipment: equipment={equipment?.Count ?? 0}");

            ClearList(equipListItems, equipListContainer);

            if (equipment == null || equipment.Count == 0)
            {
                Debug.Log("[DeploymentUI] PopulateEquipment: no equipment available");
                return;
            }

            float rowH = 60f;
            float spacing = 4f;
            float contentHeight = equipment.Count * (rowH + spacing);
            equipListContainer.sizeDelta = new Vector2(equipListContainer.sizeDelta.x, contentHeight);

            for (int i = 0; i < equipment.Count; i++)
            {
                var equip = equipment[i];
                var itemGO = CreateEquipmentItem(equip, i, rowH, spacing);
                itemGO.transform.SetParent(equipListContainer, false);
                equipListItems.Add(itemGO);
                Debug.Log($"[DeploymentUI] PopulateEquipment: added equip={equip.cardName}, slot={equip.equipmentSlot}");
            }
        }

        /// <summary>
        /// Populates the deployed heroes bar.
        /// </summary>
        public void PopulateDeployed(IReadOnlyList<HeroToken> deployed)
        {
            Debug.Log($"[DeploymentUI] PopulateDeployed: deployed={deployed?.Count ?? 0}");
            ClearList(deployedListItems, deployedListContainer);

            if (deployed == null || deployed.Count == 0) return;

            for (int i = 0; i < deployed.Count; i++)
            {
                var hero = deployed[i];
                var itemGO = CreateDeployedHeroItem(hero, i);
                itemGO.transform.SetParent(deployedListContainer, false);
                deployedListItems.Add(itemGO);
                Debug.Log($"[DeploymentUI] PopulateDeployed: added deployed hero={hero.cardDef.cardName}, tokenId={hero.tokenId}, hp={hero.currentHP}/{hero.maxHP}");
            }
        }

        /// <summary>
        /// Refreshes the bottom bar to show both already-deployed heroes (from prior turns)
        /// and staged heroes (from this turn, clickable to unstage).
        /// </summary>
        private void RefreshStagedBar()
        {
            Debug.Log($"[DeploymentUI] RefreshStagedBar: staged={stagedDeploys.Count}");
            ClearList(deployedListItems, deployedListContainer);

            int itemIndex = 0;

            // Show already-committed deployed heroes (from prior turns, not removable)
            var tm = TurnManager.Instance;
            if (tm != null)
            {
                foreach (var hero in tm.DeployedHeroes)
                {
                    var itemGO = CreateDeployedHeroItem(hero, itemIndex);
                    itemGO.transform.SetParent(deployedListContainer, false);
                    deployedListItems.Add(itemGO);
                    itemIndex++;
                }
            }

            // Show staged heroes with equipment (clickable to unstage)
            for (int i = 0; i < stagedDeploys.Count; i++)
            {
                var staged = stagedDeploys[i];
                var itemGO = CreateStagedHeroItem(staged, itemIndex, i);
                itemGO.transform.SetParent(deployedListContainer, false);
                deployedListItems.Add(itemGO);
                itemIndex++;
            }
        }

        /// <summary>
        /// Creates a UI item for a staged (not yet committed) hero, with an unstage button.
        /// </summary>
        private GameObject CreateStagedHeroItem(StagedDeploy staged, int displayIndex, int stagedIndex)
        {
            var heroDef = staged.heroDef;
            var itemGO = new GameObject($"Staged_{heroDef.cardName}", typeof(RectTransform), typeof(Image), typeof(Button));
            var itemRect = itemGO.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0);
            itemRect.anchorMax = new Vector2(0, 1);
            itemRect.pivot = new Vector2(0, 0.5f);
            itemRect.anchoredPosition = new Vector2(displayIndex * (DeployedCardWidth + 8f), 0);
            itemRect.sizeDelta = new Vector2(DeployedCardWidth, 0);
            // Staged heroes have a distinct color (amber/pending)
            itemGO.GetComponent<Image>().color = new Color(0.25f, 0.2f, 0.08f, 0.9f);

            // Thumbnail (left)
            var thumbGO = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
            thumbGO.transform.SetParent(itemGO.transform, false);
            SetAnchors(thumbGO, new Vector2(0.02f, 0.1f), new Vector2(0.35f, 0.9f));
            var thumbImg = thumbGO.GetComponent<Image>();
            if (heroDef.artwork != null)
            {
                thumbImg.sprite = heroDef.artwork;
                thumbImg.color = Color.white;
                thumbImg.preserveAspect = true;
            }
            else
            {
                thumbImg.color = new Color(0.3f, 0.25f, 0.1f, 0.8f);
            }

            // Build equipment summary
            var equipParts = new List<string>();
            if (staged.offensive != null) equipParts.Add(staged.offensive.cardName);
            if (staged.defensive != null) equipParts.Add(staged.defensive.cardName);
            if (staged.utility != null) equipParts.Add(staged.utility.cardName);
            string equipSummary = equipParts.Count > 0
                ? $"<size=8>{string.Join(", ", equipParts)}</size>"
                : "";

            // Name + equipment + "Click to remove"
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(itemGO.transform, false);
            SetAnchors(textGO, new Vector2(0.37f, 0.05f), new Vector2(0.98f, 0.95f));
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{heroDef.cardName}</b>\n{equipSummary}\n<size=9><color=#FFCC44>Click to remove</color></size>";
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            tmp.richText = true;

            int capturedIndex = stagedIndex;
            itemGO.GetComponent<Button>().onClick.AddListener(() => UnstageDeploy(capturedIndex));

            Debug.Log($"[DeploymentUI] CreateStagedHeroItem: created staged item for hero={heroDef.cardName} " +
                $"(equip={equipParts.Count}, stagedIndex={stagedIndex})");
            return itemGO;
        }

        // ─── Card Creation Helpers ───────────────────────────────────────

        private GameObject CreateHeroCard(CardDefinitionSO hero, int index)
        {
            var cardGO = new GameObject($"HeroCard_{hero.cardName}_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
            cardGO.GetComponent<Image>().color = HeroCardColor;

            // Card art (top 60%)
            var artGO = new GameObject("Art", typeof(RectTransform), typeof(Image));
            artGO.transform.SetParent(cardGO.transform, false);
            SetAnchors(artGO, new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.95f));
            var artImg = artGO.GetComponent<Image>();
            if (hero.artwork != null)
            {
                artImg.sprite = hero.artwork;
                artImg.color = Color.white;
                artImg.preserveAspect = true;
            }
            else
            {
                artImg.color = hero.placeholderColor != default
                    ? hero.placeholderColor : new Color(0.2f, 0.2f, 0.3f, 0.8f);
            }

            // Name (below art)
            var nameGO = CreateText(cardGO.transform, "Name", hero.cardName, 13,
                FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetAnchors(nameGO, new Vector2(0.02f, 0.22f), new Vector2(0.98f, 0.38f));

            // Role tag
            var roleGO = CreateText(cardGO.transform, "Role", hero.heroRole.ToString(), 10,
                FontStyles.Italic, TextAlignmentOptions.Center, BlueText);
            SetAnchors(roleGO, new Vector2(0.02f, 0.14f), new Vector2(0.98f, 0.24f));

            // Stats bar (bottom)
            string stats = $"C:{hero.combat} M:{hero.move} H:{hero.hp}";
            var statsGO = CreateText(cardGO.transform, "Stats", stats, 11,
                FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.8f));
            SetAnchors(statsGO, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.14f));

            // Store reference and index for exact-match highlighting
            var cardTag = cardGO.AddComponent<CardTag>();
            cardTag.card = hero;
            cardTag.index = index;

            var capturedHero = hero;
            int capturedIndex = index;
            cardGO.GetComponent<Button>().onClick.AddListener(() => OnHeroSelected(capturedHero, capturedIndex));

            return cardGO;
        }

        private GameObject CreateEquipmentItem(CardDefinitionSO equip, int index, float rowH, float spacing)
        {
            var itemGO = new GameObject($"EquipItem_{equip.cardName}", typeof(RectTransform), typeof(Image), typeof(Button));
            var itemRect = itemGO.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 1);
            itemRect.anchorMax = new Vector2(1, 1);
            itemRect.pivot = new Vector2(0.5f, 1);
            itemRect.anchoredPosition = new Vector2(0, -index * (rowH + spacing));
            itemRect.sizeDelta = new Vector2(-8, rowH);
            itemGO.GetComponent<Image>().color = EquipSlotColor;

            // Thumbnail (left side)
            var thumbGO = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
            thumbGO.transform.SetParent(itemGO.transform, false);
            SetAnchors(thumbGO, new Vector2(0.02f, 0.1f), new Vector2(0.22f, 0.9f));
            var thumbImg = thumbGO.GetComponent<Image>();
            if (equip.artwork != null)
            {
                thumbImg.sprite = equip.artwork;
                thumbImg.color = Color.white;
                thumbImg.preserveAspect = true;
            }
            else
            {
                thumbImg.color = new Color(0.25f, 0.2f, 0.15f, 0.8f);
            }

            // Name + effect text
            var textGO = CreateText(itemGO.transform, "EquipInfo",
                $"<b>{equip.cardName}</b> [{equip.equipmentSlot}]\n<size=11>{equip.equipmentEffectDescription}</size>",
                12, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.white);
            SetAnchors(textGO, new Vector2(0.24f, 0.05f), new Vector2(0.98f, 0.95f));
            textGO.GetComponent<TextMeshProUGUI>().richText = true;

            // Store reference and index for exact-match highlighting
            var equipTag = itemGO.AddComponent<CardTag>();
            equipTag.card = equip;
            equipTag.index = index;

            var capturedEquip = equip;
            var capturedSlot = equip.equipmentSlot;
            int capturedIndex = index;
            itemGO.GetComponent<Button>().onClick.AddListener(() => OnEquipmentSelected(capturedEquip, capturedSlot, capturedIndex));

            return itemGO;
        }

        private GameObject CreateDeployedHeroItem(HeroToken hero, int index)
        {
            var itemGO = new GameObject($"Deployed_{hero.cardDef.cardName}", typeof(RectTransform), typeof(Image), typeof(Button));
            var itemRect = itemGO.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0);
            itemRect.anchorMax = new Vector2(0, 1);
            itemRect.pivot = new Vector2(0, 0.5f);
            itemRect.anchoredPosition = new Vector2(index * (DeployedCardWidth + 8f), 0);
            itemRect.sizeDelta = new Vector2(DeployedCardWidth, 0);
            itemGO.GetComponent<Image>().color = new Color(0.12f, 0.22f, 0.12f, 0.9f);

            // Thumbnail (left)
            var thumbGO = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
            thumbGO.transform.SetParent(itemGO.transform, false);
            SetAnchors(thumbGO, new Vector2(0.02f, 0.1f), new Vector2(0.35f, 0.9f));
            var thumbImg = thumbGO.GetComponent<Image>();
            if (hero.cardDef.artwork != null)
            {
                thumbImg.sprite = hero.cardDef.artwork;
                thumbImg.color = Color.white;
                thumbImg.preserveAspect = true;
            }
            else
            {
                thumbImg.color = new Color(0.2f, 0.25f, 0.2f, 0.8f);
            }

            // Name + HP
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(itemGO.transform, false);
            SetAnchors(textGO, new Vector2(0.37f, 0.05f), new Vector2(0.98f, 0.95f));
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            float hpPct = hero.maxHP > 0 ? (float)hero.currentHP / hero.maxHP : 1f;
            Color hpColor = hpPct > 0.5f ? GreenText : new Color(1f, 0.4f, 0.3f);
            tmp.text = $"<b>{hero.cardDef.cardName}</b>\n<color=#{ColorUtility.ToHtmlStringRGB(hpColor)}>HP:{hero.currentHP}/{hero.maxHP}</color>";
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            tmp.richText = true;

            int capturedId = hero.tokenId;
            itemGO.GetComponent<Button>().onClick.AddListener(() => OnRetargetClicked(capturedId));

            return itemGO;
        }

        // ─── UI Factory Helpers ──────────────────────────────────────────

        private GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            SetAnchors(go, anchorMin, anchorMax);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private GameObject CreateText(Transform parent, string name, string text, int fontSize,
            FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.richText = true;
            return go;
        }

        private GameObject CreateButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Color bgColor, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            SetAnchors(go, anchorMin, anchorMax);
            go.GetComponent<Image>().color = bgColor;

            var textGO = CreateText(go.transform, "Label", label, fontSize,
                FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetAnchors(textGO, Vector2.zero, Vector2.one);

            return go;
        }

        private void SetAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void ClearList(List<GameObject> items, RectTransform container)
        {
            foreach (var go in items)
            {
                if (go != null) Destroy(go);
            }
            items.Clear();
            if (container != null)
                container.sizeDelta = new Vector2(container.sizeDelta.x, 0);
        }

        private void HighlightSelectedItem(List<GameObject> items, int selectedIndex, Color defaultColor = default)
        {
            if (defaultColor == default) defaultColor = HeroCardColor;
            foreach (var item in items)
            {
                if (item == null) continue;
                var img = item.GetComponent<Image>();
                if (img == null) continue;

                var tag = item.GetComponent<CardTag>();
                bool isSelected = tag != null && tag.index == selectedIndex;
                img.color = isSelected ? HeroCardSelectedColor : defaultColor;
            }
        }

        private void UpdateDeployButtonState()
        {
            if (deployBtn == null || turnManager == null) return;

            int deployed = turnManager.HeroesDeployedThisTurn + stagedDeploys.Count;
            int max = turnManager.MaxHeroDeploysPerTurn;
            bool capReached = deployed >= max;
            deployBtn.interactable = !capReached;

            var label = deployBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                if (capReached)
                {
                    label.text = $"DEPLOY LIMIT ({deployed}/{max})";
                    label.color = new Color(0.5f, 0.5f, 0.5f);
                }
                else
                {
                    label.text = $"DEPLOY HERO ({deployed}/{max})";
                    label.color = Color.white;
                }
            }

            Debug.Log($"[DeploymentUI] UpdateDeployButtonState: capReached={capReached} " +
                $"(committed={turnManager.HeroesDeployedThisTurn}, staged={stagedDeploys.Count}, max={max})");
        }

        private void ResetEquipSlots()
        {
            if (offensiveSlotImage != null) offensiveSlotImage.color = EquipSlotEmptyColor;
            if (offensiveSlotText != null) { offensiveSlotText.text = "— empty —"; offensiveSlotText.color = new Color(0.5f, 0.5f, 0.5f); }
            if (defensiveSlotImage != null) defensiveSlotImage.color = EquipSlotEmptyColor;
            if (defensiveSlotText != null) { defensiveSlotText.text = "— empty —"; defensiveSlotText.color = new Color(0.5f, 0.5f, 0.5f); }
            if (utilitySlotImage != null) utilitySlotImage.color = EquipSlotEmptyColor;
            if (utilitySlotText != null) { utilitySlotText.text = "— empty —"; utilitySlotText.color = new Color(0.5f, 0.5f, 0.5f); }
        }
    }
}
