using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Scurry.Core;
using Scurry.Data;
using Scurry.Cards;

namespace Scurry.UI
{
    /// <summary>
    /// Full-screen deck building interface for the DeckConstruction scene.
    /// Left 60%: filterable/scrollable card pool grid (6 columns).
    /// Right 40%: current deck list with remove buttons.
    /// Bottom: card detail panel on hover. Bottom-right: Start Run / Clear buttons.
    /// All UI built programmatically (no prefabs).
    /// </summary>
    public class DeckConstructionUI : MonoBehaviour
    {
        // ── Constants ───────────────────────────────────────────────────
        private const int PoolColumns = 6;
        private const float PoolCardWidth = 150f;
        private const float PoolCardHeight = 200f;
        private const float PoolCardSpacing = 8f;
        private const float DeckEntryHeight = 56f;
        private const float DeckEntrySpacing = 4f;

        // ── UI References ───────────────────────────────────────────────
        private Canvas canvas;
        private DeckConstructionManager manager;

        // Panels
        private GameObject rootPanel;
        private GameObject cardPoolPanel;       // content GO inside scroll
        private RectTransform poolContentRect;
        private GameObject currentDeckPanel;    // content GO inside scroll
        private RectTransform deckContentRect;
        private GameObject cardDetailPanel;
        private TextMeshProUGUI cardDetailText;

        // Header
        private TextMeshProUGUI deckSizeText;
        private TextMeshProUGUI validationText;

        // Buttons
        private Button startRunButton;
        private Image startRunBtnImage;
        private Button clearButton;

        // Filter tabs
        private readonly List<Image> filterBGImages = new List<Image>();
        private CardType? activeFilter;
        private bool showColonyFilter;

        // Dynamic GO tracking
        private readonly List<GameObject> poolCardGOs = new List<GameObject>();
        private readonly List<GameObject> deckEntryGOs = new List<GameObject>();

        private void Awake()
        {
            Debug.Log("[DeckConstructionUI] Awake: initializing deck construction UI");
            BuildUI();
        }

        private void Start()
        {
            // Do NOT build the pool here — wait for DeckConstructionManager.Start() to call
            // ui.Initialize(this) after it has loaded the card database and starter cards.
            Debug.Log("[DeckConstructionUI] Start: waiting for DeckConstructionManager.Initialize() callback");
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Initialize with a specific DeckConstructionManager reference.
        /// </summary>
        public void Initialize(DeckConstructionManager mgr)
        {
            Debug.Log($"[DeckConstructionUI] Initialize: manager={mgr != null}, this.instanceId={GetInstanceID()}, gameObject={gameObject?.name ?? "NULL"}");
            manager = mgr;
            RefreshCardPool();
            RefreshCurrentDeck();
        }

        /// <summary>
        /// Rebuilds the card pool display with the current filter applied.
        /// </summary>
        public void RefreshCardPool()
        {
            Debug.Log($"[DeckConstructionUI] RefreshCardPool: activeFilter={activeFilter?.ToString() ?? "All"}, showColony={showColonyFilter}");

            ClearList(poolCardGOs);

            if (manager == null)
            {
                Debug.LogWarning("[DeckConstructionUI] RefreshCardPool: manager is null, cannot populate pool");
                return;
            }

            float x = 0f;
            float y = 0f;
            int col = 0;

            if (!showColonyFilter)
            {
                // Show main card pool (Heroes, Equipment, Tactical)
                List<CardDefinitionSO> cards;
                if (activeFilter.HasValue)
                {
                    cards = manager.GetCardsByType(activeFilter.Value);
                    Debug.Log($"[DeckConstructionUI] RefreshCardPool: filtered by type={activeFilter.Value}, count={cards.Count}");
                }
                else
                {
                    cards = new List<CardDefinitionSO>(manager.CardPool);
                    Debug.Log($"[DeckConstructionUI] RefreshCardPool: showing all cards, count={cards.Count}");
                }

                foreach (var card in cards)
                {
                    var cardGO = CreatePoolCard(card, x, y);
                    poolCardGOs.Add(cardGO);

                    col++;
                    if (col >= PoolColumns)
                    {
                        col = 0;
                        x = 0f;
                        y -= (PoolCardHeight + PoolCardSpacing);
                    }
                    else
                    {
                        x += (PoolCardWidth + PoolCardSpacing);
                    }
                }
            }
            else
            {
                // Show colony card pool
                var colonyCards = new List<ColonyCardDefinitionSO>(manager.ColonyCardPool);
                Debug.Log($"[DeckConstructionUI] RefreshCardPool: showing colony cards, count={colonyCards.Count}");

                foreach (var card in colonyCards)
                {
                    var cardGO = CreateColonyPoolCard(card, x, y);
                    poolCardGOs.Add(cardGO);

                    col++;
                    if (col >= PoolColumns)
                    {
                        col = 0;
                        x = 0f;
                        y -= (PoolCardHeight + PoolCardSpacing);
                    }
                    else
                    {
                        x += (PoolCardWidth + PoolCardSpacing);
                    }
                }
            }

            // Resize content rect for scrolling
            int totalRows = Mathf.CeilToInt((float)poolCardGOs.Count / PoolColumns);
            float contentHeight = totalRows * (PoolCardHeight + PoolCardSpacing) + PoolCardSpacing;
            poolContentRect.sizeDelta = new Vector2(poolContentRect.sizeDelta.x, contentHeight);

            Debug.Log($"[DeckConstructionUI] RefreshCardPool: displayed {poolCardGOs.Count} cards, rows={totalRows}, contentHeight={contentHeight:F0}");
        }

        /// <summary>
        /// Rebuilds the current deck list display.
        /// </summary>
        public void RefreshCurrentDeck()
        {
            Debug.Log("[DeckConstructionUI] RefreshCurrentDeck: rebuilding deck list");

            ClearList(deckEntryGOs);

            if (manager == null)
            {
                Debug.LogWarning("[DeckConstructionUI] RefreshCurrentDeck: manager is null");
                return;
            }

            float y = 0f;

            // Main deck cards
            for (int i = 0; i < manager.CurrentDeck.Count; i++)
            {
                var card = manager.CurrentDeck[i];
                if (card == null) { Debug.LogWarning($"[DeckConstructionUI] RefreshCurrentDeck: skipping null card at index {i}"); continue; }
                var entryGO = CreateDeckEntry(card, null, y);
                deckEntryGOs.Add(entryGO);
                y -= (DeckEntryHeight + DeckEntrySpacing);
                Debug.Log($"[DeckConstructionUI] RefreshCurrentDeck: added deck entry '{card.cardName}' at y={y:F0}");
            }

            // Colony deck cards
            for (int i = 0; i < manager.CurrentColonyDeck.Count; i++)
            {
                var card = manager.CurrentColonyDeck[i];
                var entryGO = CreateDeckEntry(null, card, y);
                deckEntryGOs.Add(entryGO);
                y -= (DeckEntryHeight + DeckEntrySpacing);
                Debug.Log($"[DeckConstructionUI] RefreshCurrentDeck: added colony entry '{card.cardName}' at y={y:F0}");
            }

            // Resize content for scrolling
            float contentHeight = Mathf.Abs(y) + DeckEntrySpacing;
            deckContentRect.sizeDelta = new Vector2(deckContentRect.sizeDelta.x, contentHeight);

            // Update deck size text
            UpdateDeckSizeDisplay();

            Debug.Log($"[DeckConstructionUI] RefreshCurrentDeck: {deckEntryGOs.Count} entries, deckSize={manager.DeckSize}, contentHeight={contentHeight:F0}");
        }

        /// <summary>
        /// Called when a card pool item is clicked to add it to the deck.
        /// </summary>
        public void OnCardPoolItemClicked(CardDefinitionSO card)
        {
            Debug.Log($"[DeckConstructionUI] OnCardPoolItemClicked: card={card?.cardName ?? "NULL"}, id={card?.cardId}, type={card?.cardType}, cost={card?.deckCost}");

            if (manager == null || card == null)
            {
                Debug.LogWarning($"[DeckConstructionUI] OnCardPoolItemClicked: FAILED — manager={manager != null}, card={card != null}, this.instanceId={GetInstanceID()}, gameObject={gameObject?.name ?? "NULL"}");
                return;
            }

            bool added = manager.AddCard(card);
            Debug.Log($"[DeckConstructionUI] OnCardPoolItemClicked: addResult={added}, deckSize={manager.DeckSize}");

            if (added)
            {
                RefreshCurrentDeck();
                RefreshCardPool(); // Update copy count badges
            }
            else
            {
                Debug.Log("[DeckConstructionUI] OnCardPoolItemClicked: card could not be added, showing notification");
                EventBus.OnNotification?.Invoke("Cannot add card - at copy limit or deck full", new Color(1f, 0.5f, 0.3f));
            }
        }

        /// <summary>
        /// Called when a colony card pool item is clicked to add it to the deck.
        /// </summary>
        public void OnColonyCardPoolItemClicked(ColonyCardDefinitionSO card)
        {
            Debug.Log($"[DeckConstructionUI] OnColonyCardPoolItemClicked: card={card?.cardName ?? "NULL"}, id={card?.cardId}, tier={card?.colonyTier}");

            if (manager == null || card == null)
            {
                Debug.LogWarning("[DeckConstructionUI] OnColonyCardPoolItemClicked: manager or card is null");
                return;
            }

            bool added = manager.AddColonyCard(card);
            Debug.Log($"[DeckConstructionUI] OnColonyCardPoolItemClicked: addResult={added}, deckSize={manager.DeckSize}");

            if (added)
            {
                RefreshCurrentDeck();
                RefreshCardPool();
            }
            else
            {
                Debug.Log("[DeckConstructionUI] OnColonyCardPoolItemClicked: colony card could not be added");
                EventBus.OnNotification?.Invoke("Cannot add colony card - at copy limit or deck full", new Color(1f, 0.5f, 0.3f));
            }
        }

        /// <summary>
        /// Called when a deck item is clicked to remove it from the deck.
        /// </summary>
        public void OnDeckItemClicked(CardDefinitionSO card)
        {
            Debug.Log($"[DeckConstructionUI] OnDeckItemClicked: removing card={card?.cardName ?? "NULL"}, id={card?.cardId}");

            if (manager == null || card == null)
            {
                Debug.LogWarning("[DeckConstructionUI] OnDeckItemClicked: manager or card is null");
                return;
            }

            bool removed = manager.RemoveCard(card);
            Debug.Log($"[DeckConstructionUI] OnDeckItemClicked: removeResult={removed}, deckSize={manager.DeckSize}");

            if (removed)
            {
                RefreshCurrentDeck();
                RefreshCardPool();
            }
        }

        /// <summary>
        /// Called when a colony deck item is clicked to remove it from the deck.
        /// </summary>
        public void OnColonyDeckItemClicked(ColonyCardDefinitionSO card)
        {
            Debug.Log($"[DeckConstructionUI] OnColonyDeckItemClicked: removing colony card={card?.cardName ?? "NULL"}, id={card?.cardId}, isStarter={card?.isStarter}");

            if (manager == null || card == null)
            {
                Debug.LogWarning("[DeckConstructionUI] OnColonyDeckItemClicked: manager or card is null");
                return;
            }

            if (card.isStarter)
            {
                Debug.Log("[DeckConstructionUI] OnColonyDeckItemClicked: cannot remove starter card, showing notification");
                EventBus.OnNotification?.Invoke("Starter cards cannot be removed", new Color(1f, 0.5f, 0.3f));
                return;
            }

            bool removed = manager.RemoveColonyCard(card);
            Debug.Log($"[DeckConstructionUI] OnColonyDeckItemClicked: removeResult={removed}, deckSize={manager.DeckSize}");

            if (removed)
            {
                RefreshCurrentDeck();
                RefreshCardPool();
            }
        }

        /// <summary>
        /// Changes the active card type filter. Null for All.
        /// </summary>
        public void OnFilterChanged(CardType? filter)
        {
            Debug.Log($"[DeckConstructionUI] OnFilterChanged: oldFilter={activeFilter?.ToString() ?? "All"}, newFilter={filter?.ToString() ?? "All"}");
            activeFilter = filter;
            showColonyFilter = false;
            UpdateFilterHighlights();
            RefreshCardPool();
        }

        /// <summary>
        /// Switches to the colony card filter view.
        /// </summary>
        public void OnColonyFilterSelected()
        {
            Debug.Log("[DeckConstructionUI] OnColonyFilterSelected: switching to colony card view");
            activeFilter = null;
            showColonyFilter = true;
            UpdateFilterHighlights();
            RefreshCardPool();
        }

        /// <summary>
        /// Called when Start Run button is clicked.
        /// </summary>
        public void OnStartRunClicked()
        {
            Debug.Log($"[DeckConstructionUI] OnStartRunClicked: deckSize={manager?.DeckSize}, isValid={manager?.IsDeckValid}");

            if (manager == null)
            {
                Debug.LogError("[DeckConstructionUI] OnStartRunClicked: manager is null, cannot start run");
                return;
            }

            if (!manager.IsDeckValid)
            {
                Debug.Log($"[DeckConstructionUI] OnStartRunClicked: deck invalid, size={manager.DeckSize}, required={DeckConstructionManager.MIN_DECK_SIZE}-{DeckConstructionManager.MAX_DECK_SIZE}");
                EventBus.OnNotification?.Invoke(
                    $"Deck must contain {DeckConstructionManager.MIN_DECK_SIZE}-{DeckConstructionManager.MAX_DECK_SIZE} cards (current: {manager.DeckSize})",
                    Color.red);
                return;
            }

            Debug.Log("[DeckConstructionUI] OnStartRunClicked: deck valid, calling manager.ConfirmDeck");
            manager.ConfirmDeck();
        }

        /// <summary>
        /// Called when Clear button is clicked.
        /// </summary>
        public void OnClearClicked()
        {
            Debug.Log("[DeckConstructionUI] OnClearClicked: clearing deck via manager");

            if (manager == null)
            {
                Debug.LogWarning("[DeckConstructionUI] OnClearClicked: manager is null");
                return;
            }

            manager.ClearDeck();
            RefreshCurrentDeck();
            RefreshCardPool();
            Debug.Log($"[DeckConstructionUI] OnClearClicked: deck cleared, deckSize={manager.DeckSize}");
        }

        /// <summary>
        /// Shows full card details in the detail panel.
        /// </summary>
        public void ShowCardDetail(CardDefinitionSO card)
        {
            Debug.Log($"[DeckConstructionUI] ShowCardDetail: card={card?.cardName ?? "NULL"}, type={card?.cardType}, cost={card?.deckCost}");

            if (card == null || cardDetailPanel == null) return;

            cardDetailPanel.SetActive(true);

            string detail = $"<b>{card.cardName}</b>  |  {card.cardType}  |  Cost: {card.deckCost}  |  {card.rarity}\n";

            switch (card.cardType)
            {
                case CardType.Hero:
                    detail += $"Role: {card.heroRole}  |  Combat: {card.combat}  |  Move: {card.move}  |  HP: {card.hp}  |  Carry: {card.carry}  |  Init: {card.initiative}\n";
                    if (card.specialAbility != SpecialAbility.None)
                        detail += $"Ability: {card.specialAbility} - {card.specialAbilityDescription}";
                    break;
                case CardType.Equipment:
                    detail += $"Slot: {card.equipmentSlot}\n{card.equipmentEffectDescription}";
                    break;
                case CardType.Tactical:
                    detail += $"Type: {card.tacticalType}\n{card.tacticalEffectDescription}";
                    break;
                default:
                    detail += $"Effect: {card.effectValue1}";
                    break;
            }

            if (manager != null)
            {
                int copies = manager.GetCurrentCopies(card.cardId);
                int maxCopies = manager.GetMaxCopies(card.deckCost);
                detail += $"\n<color=#888888>In deck: {copies}/{maxCopies}</color>";
            }

            if (cardDetailText != null)
                cardDetailText.text = detail;

            Debug.Log($"[DeckConstructionUI] ShowCardDetail: detail panel updated for '{card.cardName}'");
        }

        /// <summary>
        /// Shows full colony card details in the detail panel.
        /// </summary>
        public void ShowColonyCardDetail(ColonyCardDefinitionSO card)
        {
            Debug.Log($"[DeckConstructionUI] ShowColonyCardDetail: card={card?.cardName ?? "NULL"}, tier={card?.colonyTier}");

            if (card == null || cardDetailPanel == null) return;

            cardDetailPanel.SetActive(true);

            string detail = $"<b>{card.cardName}</b>  |  Colony  |  Tier: {card.colonyTier}  |  Cost: {card.deckCost}  |  {card.rarity}\n";
            detail += $"Effect: {card.colonyEffect} ({card.effectValue})\n";
            detail += card.description;

            if (card.placementRequirement != PlacementRequirement.None)
            {
                detail += $"\nPlacement: {card.placementRequirement}";
                if (!string.IsNullOrEmpty(card.adjacencyCardName))
                    detail += $" (adjacent to {card.adjacencyCardName})";
            }

            if (manager != null)
            {
                int copies = manager.GetCurrentColonyCopies(card.cardId);
                int maxCopies = manager.GetMaxCopies(card.deckCost);
                detail += $"\n<color=#888888>In deck: {copies}/{maxCopies}</color>";
            }

            if (card.isStarter)
                detail += "\n<color=#FFCC00>Starter card (cannot be removed)</color>";

            if (cardDetailText != null)
                cardDetailText.text = detail;

            Debug.Log($"[DeckConstructionUI] ShowColonyCardDetail: detail panel updated for '{card.cardName}'");
        }

        /// <summary>
        /// Hides the card detail panel.
        /// </summary>
        public void HideCardDetail()
        {
            Debug.Log("[DeckConstructionUI] HideCardDetail: hiding detail panel");
            if (cardDetailPanel != null)
                cardDetailPanel.SetActive(false);
        }

        // ── Build UI ────────────────────────────────────────────────────

        private void BuildUI()
        {
            Debug.Log("[DeckConstructionUI] BuildUI: starting construction");

            UIHelper.EnsureEventSystem();

            // Canvas
            canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // Root panel
            rootPanel = new GameObject("DeckConstructionRoot", typeof(RectTransform), typeof(Image));
            rootPanel.transform.SetParent(transform, false);
            var rootRect = rootPanel.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;
            rootPanel.GetComponent<Image>().color = new Color(0.05f, 0.04f, 0.08f, 1f);

            BuildTopBar();
            BuildFilterTabs();
            BuildCardPool();
            BuildCurrentDeck();
            BuildCardDetail();
            BuildBottomButtons();

            Debug.Log("[DeckConstructionUI] BuildUI: complete");
        }

        private void BuildTopBar()
        {
            Debug.Log("[DeckConstructionUI] BuildTopBar: creating top bar");

            // Title
            CreateText(rootPanel.transform, "Title",
                new Vector2(0, 0.94f), new Vector2(0.4f, 1f), new Vector2(0, 1),
                new Vector2(20, -5), Vector2.zero,
                "DECK CONSTRUCTION", 32, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.75f, 0.1f));

            // Deck size display
            deckSizeText = CreateText(rootPanel.transform, "DeckSize",
                new Vector2(0.4f, 0.94f), new Vector2(0.7f, 1f), new Vector2(0.5f, 1),
                new Vector2(0, -5), Vector2.zero,
                "0 / 10-30 cards", 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            // Validation message
            validationText = CreateText(rootPanel.transform, "Validation",
                new Vector2(0.7f, 0.94f), new Vector2(1f, 1f), new Vector2(1, 1),
                new Vector2(-20, -5), Vector2.zero,
                "", 16, FontStyles.Italic, TextAlignmentOptions.MidlineRight, new Color(1f, 0.4f, 0.3f));

            Debug.Log("[DeckConstructionUI] BuildTopBar: complete");
        }

        private void BuildFilterTabs()
        {
            Debug.Log("[DeckConstructionUI] BuildFilterTabs: creating filter tabs");

            string[] filterLabels = { "All", "Heroes", "Colony", "Equipment", "Tactical" };
            float tabWidth = 0.117f;
            float startX = 0.01f;
            float tabY0 = 0.90f;
            float tabY1 = 0.94f;

            for (int i = 0; i < filterLabels.Length; i++)
            {
                float x0 = startX + i * tabWidth;
                float x1 = x0 + tabWidth - 0.005f;

                var tabGO = new GameObject($"Filter_{filterLabels[i]}", typeof(RectTransform), typeof(Image), typeof(Button));
                tabGO.transform.SetParent(rootPanel.transform, false);
                var tabRect = tabGO.GetComponent<RectTransform>();
                tabRect.anchorMin = new Vector2(x0, tabY0);
                tabRect.anchorMax = new Vector2(x1, tabY1);
                tabRect.pivot = new Vector2(0.5f, 0.5f);
                tabRect.anchoredPosition = Vector2.zero;
                tabRect.sizeDelta = Vector2.zero;

                var tabImage = tabGO.GetComponent<Image>();
                tabImage.color = i == 0 ? new Color(0.3f, 0.4f, 0.5f) : new Color(0.15f, 0.15f, 0.2f);
                filterBGImages.Add(tabImage);

                var tabTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                tabTextGO.transform.SetParent(tabGO.transform, false);
                var tabTextRect = tabTextGO.GetComponent<RectTransform>();
                tabTextRect.anchorMin = Vector2.zero;
                tabTextRect.anchorMax = Vector2.one;
                tabTextRect.sizeDelta = Vector2.zero;
                var tabTmp = tabTextGO.GetComponent<TextMeshProUGUI>();
                tabTmp.text = filterLabels[i];
                tabTmp.fontSize = 16;
                tabTmp.fontStyle = FontStyles.Bold;
                tabTmp.alignment = TextAlignmentOptions.Center;
                tabTmp.color = Color.white;

                int capturedIndex = i;
                tabGO.GetComponent<Button>().onClick.AddListener(() => OnFilterTabClicked(capturedIndex));

                Debug.Log($"[DeckConstructionUI] BuildFilterTabs: created tab '{filterLabels[i]}' at index={i}, x0={x0:F3}, x1={x1:F3}");
            }
        }

        private void OnFilterTabClicked(int index)
        {
            Debug.Log($"[DeckConstructionUI] OnFilterTabClicked: index={index}");

            switch (index)
            {
                case 0:
                    OnFilterChanged(null);
                    break;
                case 1:
                    OnFilterChanged(CardType.Hero);
                    break;
                case 2:
                    OnColonyFilterSelected();
                    break;
                case 3:
                    OnFilterChanged(CardType.Equipment);
                    break;
                case 4:
                    OnFilterChanged(CardType.Tactical);
                    break;
                default:
                    Debug.LogWarning($"[DeckConstructionUI] OnFilterTabClicked: unexpected index={index}");
                    break;
            }
        }

        private void UpdateFilterHighlights()
        {
            int activeIndex;
            if (showColonyFilter)
                activeIndex = 2;
            else if (!activeFilter.HasValue)
                activeIndex = 0;
            else if (activeFilter.Value == CardType.Hero)
                activeIndex = 1;
            else if (activeFilter.Value == CardType.Equipment)
                activeIndex = 3;
            else if (activeFilter.Value == CardType.Tactical)
                activeIndex = 4;
            else
                activeIndex = 0;

            Debug.Log($"[DeckConstructionUI] UpdateFilterHighlights: activeIndex={activeIndex}");

            for (int i = 0; i < filterBGImages.Count; i++)
            {
                filterBGImages[i].color = i == activeIndex
                    ? new Color(0.3f, 0.4f, 0.5f)
                    : new Color(0.15f, 0.15f, 0.2f);
            }
        }

        private void BuildCardPool()
        {
            Debug.Log("[DeckConstructionUI] BuildCardPool: creating scrollable card pool area");

            // Scroll view container
            var scrollGO = new GameObject("CardPoolScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(rootPanel.transform, false);
            var scrollRect = scrollGO.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.01f, 0.21f);
            scrollRect.anchorMax = new Vector2(0.59f, 0.90f);
            scrollRect.pivot = new Vector2(0, 1);
            scrollRect.anchoredPosition = Vector2.zero;
            scrollRect.sizeDelta = Vector2.zero;
            scrollGO.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.8f);

            // Mask
            var maskGO = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGO.transform.SetParent(scrollGO.transform, false);
            var maskRect = maskGO.GetComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.sizeDelta = Vector2.zero;
            maskGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            maskGO.GetComponent<Mask>().showMaskGraphic = false;

            // Content
            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(maskGO.transform, false);
            poolContentRect = contentGO.GetComponent<RectTransform>();
            poolContentRect.anchorMin = new Vector2(0, 1);
            poolContentRect.anchorMax = new Vector2(1, 1);
            poolContentRect.pivot = new Vector2(0, 1);
            poolContentRect.anchoredPosition = Vector2.zero;
            poolContentRect.sizeDelta = new Vector2(0, 1000);

            cardPoolPanel = contentGO;

            // Set up ScrollRect
            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.content = poolContentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.1f;
            scroll.scrollSensitivity = 75f;
            scroll.viewport = maskRect;

            Debug.Log("[DeckConstructionUI] BuildCardPool: complete");
        }

        private void BuildCurrentDeck()
        {
            Debug.Log("[DeckConstructionUI] BuildCurrentDeck: creating deck list panel");

            // Header
            CreateText(rootPanel.transform, "DeckHeader",
                new Vector2(0.61f, 0.87f), new Vector2(0.99f, 0.90f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                "CURRENT DECK", 18, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.9f, 0.75f, 0.1f));

            // Scroll view
            var scrollGO = new GameObject("DeckScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(rootPanel.transform, false);
            var scrollRect = scrollGO.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.61f, 0.16f);
            scrollRect.anchorMax = new Vector2(0.99f, 0.87f);
            scrollRect.pivot = new Vector2(0, 1);
            scrollRect.anchoredPosition = Vector2.zero;
            scrollRect.sizeDelta = Vector2.zero;
            scrollGO.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.8f);

            // Mask
            var maskGO = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGO.transform.SetParent(scrollGO.transform, false);
            var maskRect = maskGO.GetComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.sizeDelta = Vector2.zero;
            maskGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            maskGO.GetComponent<Mask>().showMaskGraphic = false;

            // Content
            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(maskGO.transform, false);
            deckContentRect = contentGO.GetComponent<RectTransform>();
            deckContentRect.anchorMin = new Vector2(0, 1);
            deckContentRect.anchorMax = new Vector2(1, 1);
            deckContentRect.pivot = new Vector2(0, 1);
            deckContentRect.anchoredPosition = Vector2.zero;
            deckContentRect.sizeDelta = new Vector2(0, 500);

            currentDeckPanel = contentGO;

            // Set up ScrollRect
            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.content = deckContentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.1f;
            scroll.scrollSensitivity = 75f;
            scroll.viewport = maskRect;

            Debug.Log("[DeckConstructionUI] BuildCurrentDeck: complete");
        }

        private void BuildCardDetail()
        {
            Debug.Log("[DeckConstructionUI] BuildCardDetail: creating card detail panel");

            cardDetailPanel = new GameObject("CardDetailPanel", typeof(RectTransform), typeof(Image));
            cardDetailPanel.transform.SetParent(rootPanel.transform, false);
            var detailRect = cardDetailPanel.GetComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.01f, 0.01f);
            detailRect.anchorMax = new Vector2(0.59f, 0.20f);
            detailRect.pivot = new Vector2(0, 0);
            detailRect.anchoredPosition = Vector2.zero;
            detailRect.sizeDelta = Vector2.zero;
            cardDetailPanel.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.14f, 0.98f);
            // Ensure detail panel renders above the card pool scroll area
            cardDetailPanel.transform.SetAsLastSibling();

            cardDetailText = CreateText(cardDetailPanel.transform, "DetailText",
                new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f), new Vector2(0, 0.5f),
                Vector2.zero, Vector2.zero,
                "Hover over a card to see details", 15, FontStyles.Normal,
                TextAlignmentOptions.TopLeft, new Color(0.85f, 0.85f, 0.85f));
            cardDetailText.richText = true;

            Debug.Log("[DeckConstructionUI] BuildCardDetail: complete");
        }

        private void BuildBottomButtons()
        {
            Debug.Log("[DeckConstructionUI] BuildBottomButtons: creating Start Run and Clear buttons");

            // Start Run button
            var startBtnGO = CreateButton(rootPanel.transform, "StartRunButton",
                new Vector2(0.75f, 0.02f), new Vector2(0.99f, 0.10f),
                "Start Run", 24, new Color(0.15f, 0.4f, 0.15f));
            startRunButton = startBtnGO.GetComponent<Button>();
            startRunBtnImage = startBtnGO.GetComponent<Image>();
            startRunButton.onClick.AddListener(OnStartRunClicked);

            // Clear button
            var clearBtnGO = CreateButton(rootPanel.transform, "ClearButton",
                new Vector2(0.61f, 0.02f), new Vector2(0.74f, 0.10f),
                "Clear", 18, new Color(0.5f, 0.2f, 0.2f));
            clearButton = clearBtnGO.GetComponent<Button>();
            clearButton.onClick.AddListener(OnClearClicked);

            Debug.Log("[DeckConstructionUI] BuildBottomButtons: complete");
        }

        // ── Card Creation Helpers ───────────────────────────────────────

        private GameObject CreatePoolCard(CardDefinitionSO card, float x, float y)
        {
            Debug.Log($"[DeckConstructionUI] CreatePoolCard: card={card.cardName}, id={card.cardId}, type={card.cardType}, pos=({x:F0},{y:F0})");

            var cardGO = new GameObject($"Pool_{card.cardName}", typeof(RectTransform), typeof(Image), typeof(Button));
            cardGO.transform.SetParent(cardPoolPanel.transform, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0, 1);
            cardRect.anchorMax = new Vector2(0, 1);
            cardRect.pivot = new Vector2(0, 1);
            cardRect.anchoredPosition = new Vector2(x + PoolCardSpacing, y - PoolCardSpacing);
            cardRect.sizeDelta = new Vector2(PoolCardWidth, PoolCardHeight);

            // Background color based on card type, dimmed if can't add
            Color bgColor = GetCardTypeColor(card.cardType);
            bool canAdd = manager != null && manager.CanAddCard(card);
            if (!canAdd)
            {
                bgColor *= 0.5f;
                Debug.Log($"[DeckConstructionUI] CreatePoolCard: card '{card.cardName}' at copy limit, dimming");
            }
            cardGO.GetComponent<Image>().color = bgColor;

            // Card name (top strip)
            CreateText(cardGO.transform, "Name",
                new Vector2(0, 0.88f), new Vector2(1, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                card.cardName, 12, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            // Card artwork — fills most of the card between name and type label
            if (card.artwork != null)
            {
                var imgGO = new GameObject("Artwork", typeof(RectTransform), typeof(Image));
                imgGO.transform.SetParent(cardGO.transform, false);
                var imgRect = imgGO.GetComponent<RectTransform>();
                imgRect.anchorMin = new Vector2(0f, 0.15f);
                imgRect.anchorMax = new Vector2(1f, 0.88f);
                imgRect.sizeDelta = Vector2.zero;
                var artImg = imgGO.GetComponent<Image>();
                artImg.sprite = card.artwork;
                artImg.preserveAspect = true;
                artImg.raycastTarget = false; // Let pointer events pass through to card's EventTrigger
            }

            // Type label (bottom strip, above cost badge)
            CreateText(cardGO.transform, "TypeLabel",
                new Vector2(0.2f, 0f), new Vector2(1, 0.15f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                card.cardType.ToString(), 10, FontStyles.Italic, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.8f));

            // Cost badge
            var costBadge = new GameObject("CostBadge", typeof(RectTransform), typeof(Image));
            costBadge.transform.SetParent(cardGO.transform, false);
            var costRect = costBadge.GetComponent<RectTransform>();
            costRect.anchorMin = Vector2.zero;
            costRect.anchorMax = Vector2.zero;
            costRect.pivot = Vector2.zero;
            costRect.anchoredPosition = new Vector2(4, 4);
            costRect.sizeDelta = new Vector2(28, 28);
            costBadge.GetComponent<Image>().color = new Color(0.9f, 0.75f, 0.2f);

            CreateText(costBadge.transform, "CostText",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                card.deckCost.ToString(), 14, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);

            // Copy count indicator
            if (manager != null)
            {
                int copies = manager.GetCurrentCopies(card.cardId);
                int maxCopies = manager.GetMaxCopies(card.deckCost);
                if (copies > 0)
                {
                    Color countColor = copies >= maxCopies ? new Color(1f, 0.3f, 0.3f) : new Color(0.4f, 1f, 0.4f);
                    CreateText(cardGO.transform, "CopyCount",
                        new Vector2(0.5f, 0.02f), new Vector2(1f, 0.18f), new Vector2(1, 0),
                        Vector2.zero, Vector2.zero,
                        $"{copies}/{maxCopies}", 11, FontStyles.Bold, TextAlignmentOptions.Center, countColor);
                    Debug.Log($"[DeckConstructionUI] CreatePoolCard: copy indicator for '{card.cardName}': {copies}/{maxCopies}");
                }
            }

            // Button click to add
            var capturedCard = card;
            cardGO.GetComponent<Button>().onClick.AddListener(() => OnCardPoolItemClicked(capturedCard));

            // Hover to show detail via EventTrigger
            var trigger = cardGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((_) => ShowCardDetail(capturedCard));
            trigger.triggers.Add(pointerEnter);

            var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((_) => HideCardDetail());
            trigger.triggers.Add(pointerExit);

            cardGO.AddComponent<ScrollForwarder>();
            return cardGO;
        }

        private GameObject CreateColonyPoolCard(ColonyCardDefinitionSO card, float x, float y)
        {
            Debug.Log($"[DeckConstructionUI] CreateColonyPoolCard: card={card.cardName}, id={card.cardId}, tier={card.colonyTier}, pos=({x:F0},{y:F0})");

            var cardGO = new GameObject($"Pool_{card.cardName}", typeof(RectTransform), typeof(Image), typeof(Button));
            cardGO.transform.SetParent(cardPoolPanel.transform, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0, 1);
            cardRect.anchorMax = new Vector2(0, 1);
            cardRect.pivot = new Vector2(0, 1);
            cardRect.anchoredPosition = new Vector2(x + PoolCardSpacing, y - PoolCardSpacing);
            cardRect.sizeDelta = new Vector2(PoolCardWidth, PoolCardHeight);

            // Background color for colony
            Color bgColor = new Color(0.25f, 0.18f, 0.10f, 0.9f);
            bool canAdd = manager != null && manager.CanAddColonyCard(card);
            if (!canAdd)
            {
                bgColor *= 0.5f;
                Debug.Log($"[DeckConstructionUI] CreateColonyPoolCard: card '{card.cardName}' at copy limit, dimming");
            }
            cardGO.GetComponent<Image>().color = bgColor;

            // Name (top strip)
            CreateText(cardGO.transform, "Name",
                new Vector2(0, 0.88f), new Vector2(1, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                card.cardName, 12, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            // Artwork — fills most of the card between name and tier label
            if (card.artwork != null)
            {
                var imgGO = new GameObject("Artwork", typeof(RectTransform), typeof(Image));
                imgGO.transform.SetParent(cardGO.transform, false);
                var imgRect = imgGO.GetComponent<RectTransform>();
                imgRect.anchorMin = new Vector2(0f, 0.15f);
                imgRect.anchorMax = new Vector2(1f, 0.88f);
                imgRect.sizeDelta = Vector2.zero;
                var artImg = imgGO.GetComponent<Image>();
                artImg.sprite = card.artwork;
                artImg.preserveAspect = true;
                artImg.raycastTarget = false; // Let pointer events pass through to card's EventTrigger
            }

            // Tier label (bottom strip)
            CreateText(cardGO.transform, "TierLabel",
                new Vector2(0.2f, 0f), new Vector2(1, 0.15f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                $"Colony - {card.colonyTier}", 9, FontStyles.Italic, TextAlignmentOptions.Center, new Color(0.85f, 0.75f, 0.55f));

            // Cost badge
            var costBadge = new GameObject("CostBadge", typeof(RectTransform), typeof(Image));
            costBadge.transform.SetParent(cardGO.transform, false);
            var costRect = costBadge.GetComponent<RectTransform>();
            costRect.anchorMin = Vector2.zero;
            costRect.anchorMax = Vector2.zero;
            costRect.pivot = Vector2.zero;
            costRect.anchoredPosition = new Vector2(4, 4);
            costRect.sizeDelta = new Vector2(28, 28);
            costBadge.GetComponent<Image>().color = new Color(0.7f, 0.55f, 0.3f);

            CreateText(costBadge.transform, "CostText",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                card.deckCost.ToString(), 14, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);

            // Copy count
            if (manager != null)
            {
                int copies = manager.GetCurrentColonyCopies(card.cardId);
                int maxCopies = manager.GetMaxCopies(card.deckCost);
                if (copies > 0)
                {
                    Color countColor = copies >= maxCopies ? new Color(1f, 0.3f, 0.3f) : new Color(0.4f, 1f, 0.4f);
                    CreateText(cardGO.transform, "CopyCount",
                        new Vector2(0.5f, 0.02f), new Vector2(1f, 0.18f), new Vector2(1, 0),
                        Vector2.zero, Vector2.zero,
                        $"{copies}/{maxCopies}", 11, FontStyles.Bold, TextAlignmentOptions.Center, countColor);
                }
            }

            // Starter badge
            if (card.isStarter)
            {
                CreateText(cardGO.transform, "StarterBadge",
                    new Vector2(0.6f, 0.82f), new Vector2(1f, 0.98f), new Vector2(1, 1),
                    new Vector2(-4, 0), Vector2.zero,
                    "STARTER", 9, FontStyles.Bold, TextAlignmentOptions.TopRight, new Color(1f, 0.85f, 0.2f));
            }

            // Button click to add
            var capturedCard = card;
            cardGO.GetComponent<Button>().onClick.AddListener(() => OnColonyCardPoolItemClicked(capturedCard));

            // Hover
            var trigger = cardGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((_) => ShowColonyCardDetail(capturedCard));
            trigger.triggers.Add(pointerEnter);

            var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((_) => HideCardDetail());
            trigger.triggers.Add(pointerExit);

            cardGO.AddComponent<ScrollForwarder>();
            return cardGO;
        }

        private GameObject CreateDeckEntry(CardDefinitionSO card, ColonyCardDefinitionSO colonyCard, float y)
        {
            bool isColony = colonyCard != null;
            string entryName = isColony ? colonyCard.cardName : card?.cardName ?? "Unknown";
            int deckCost = isColony ? colonyCard.deckCost : (card?.deckCost ?? 1);
            bool isStarter = isColony && colonyCard.isStarter;

            Debug.Log($"[DeckConstructionUI] CreateDeckEntry: name={entryName}, isColony={isColony}, isStarter={isStarter}, y={y:F0}");

            var entryGO = new GameObject($"Deck_{entryName}", typeof(RectTransform), typeof(Image), typeof(Button));
            entryGO.transform.SetParent(currentDeckPanel.transform, false);
            var entryRect = entryGO.GetComponent<RectTransform>();
            entryRect.anchorMin = new Vector2(0, 1);
            entryRect.anchorMax = new Vector2(1, 1);
            entryRect.pivot = new Vector2(0, 1);
            entryRect.anchoredPosition = new Vector2(4, y - 2);
            entryRect.sizeDelta = new Vector2(-8, DeckEntryHeight);

            Color bgColor = (isColony || card == null)
                ? new Color(0.2f, 0.15f, 0.08f, 0.8f)
                : GetCardTypeColor(card.cardType) * 0.7f;
            entryGO.GetComponent<Image>().color = bgColor;

            // Cost badge
            var costBadgeGO = new GameObject("Cost", typeof(RectTransform), typeof(Image));
            costBadgeGO.transform.SetParent(entryGO.transform, false);
            var costRect = costBadgeGO.GetComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0, 0.1f);
            costRect.anchorMax = new Vector2(0, 0.9f);
            costRect.pivot = new Vector2(0, 0.5f);
            costRect.anchoredPosition = new Vector2(4, 0);
            costRect.sizeDelta = new Vector2(24, 0);
            costBadgeGO.GetComponent<Image>().color = isColony
                ? new Color(0.7f, 0.55f, 0.3f)
                : new Color(0.9f, 0.75f, 0.2f);

            CreateText(costBadgeGO.transform, "CostNum",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                deckCost.ToString(), 12, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);

            // Artwork thumbnail
            Sprite artwork = isColony ? colonyCard?.artwork : card?.artwork;
            float nameOffsetX = 34f;
            if (artwork != null)
            {
                var thumbGO = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
                thumbGO.transform.SetParent(entryGO.transform, false);
                var thumbRect = thumbGO.GetComponent<RectTransform>();
                thumbRect.anchorMin = new Vector2(0, 0.05f);
                thumbRect.anchorMax = new Vector2(0, 0.95f);
                thumbRect.pivot = new Vector2(0, 0.5f);
                thumbRect.anchoredPosition = new Vector2(32, 0);
                thumbRect.sizeDelta = new Vector2(48, 0);
                var thumbImg = thumbGO.GetComponent<Image>();
                thumbImg.sprite = artwork;
                thumbImg.preserveAspect = true;
                thumbImg.raycastTarget = false;
                nameOffsetX = 84f;
            }

            // Card name with type
            string typeStr = (isColony || card == null) ? "[Colony]" : $"[{card.cardType}]";
            CreateText(entryGO.transform, "Name",
                new Vector2(0, 0), new Vector2(0.8f, 1), new Vector2(0, 0.5f),
                new Vector2(nameOffsetX, 0), Vector2.zero,
                $"{entryName}  <size=10>{typeStr}</size>", 14, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft, Color.white);

            // Remove button (X) or starter label
            if (!isStarter)
            {
                var removeBtnGO = new GameObject("RemoveBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                removeBtnGO.transform.SetParent(entryGO.transform, false);
                var removeRect = removeBtnGO.GetComponent<RectTransform>();
                removeRect.anchorMin = new Vector2(1, 0.1f);
                removeRect.anchorMax = new Vector2(1, 0.9f);
                removeRect.pivot = new Vector2(1, 0.5f);
                removeRect.anchoredPosition = new Vector2(-4, 0);
                removeRect.sizeDelta = new Vector2(28, 0);
                removeBtnGO.GetComponent<Image>().color = new Color(0.6f, 0.15f, 0.15f);

                CreateText(removeBtnGO.transform, "X",
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero,
                    "X", 14, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

                if (isColony)
                {
                    var capturedColony = colonyCard;
                    removeBtnGO.GetComponent<Button>().onClick.AddListener(() => OnColonyDeckItemClicked(capturedColony));
                }
                else
                {
                    var capturedCard = card;
                    removeBtnGO.GetComponent<Button>().onClick.AddListener(() => OnDeckItemClicked(capturedCard));
                }
            }
            else
            {
                CreateText(entryGO.transform, "StarterLabel",
                    new Vector2(0.8f, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                    new Vector2(-4, 0), Vector2.zero,
                    "Starter", 10, FontStyles.Italic, TextAlignmentOptions.MidlineRight, new Color(1f, 0.85f, 0.2f));
            }

            // Hover to show detail
            var trigger = entryGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            if (isColony)
            {
                var capturedColonyHover = colonyCard;
                pointerEnter.callback.AddListener((_) => ShowColonyCardDetail(capturedColonyHover));
            }
            else
            {
                var capturedCardHover = card;
                pointerEnter.callback.AddListener((_) => ShowCardDetail(capturedCardHover));
            }
            trigger.triggers.Add(pointerEnter);

            var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((_) => HideCardDetail());
            trigger.triggers.Add(pointerExit);

            entryGO.AddComponent<ScrollForwarder>();
            return entryGO;
        }

        // ── Utility ─────────────────────────────────────────────────────

        private void UpdateDeckSizeDisplay()
        {
            if (manager == null) return;

            int size = manager.DeckSize;
            bool valid = manager.IsDeckValid;

            Debug.Log($"[DeckConstructionUI] UpdateDeckSizeDisplay: size={size}, valid={valid}");

            if (deckSizeText != null)
            {
                deckSizeText.text = $"{size} / {DeckConstructionManager.MIN_DECK_SIZE}-{DeckConstructionManager.MAX_DECK_SIZE} cards";
                deckSizeText.color = valid ? new Color(0.4f, 1f, 0.4f) : Color.white;
            }

            if (validationText != null)
            {
                if (size < DeckConstructionManager.MIN_DECK_SIZE)
                {
                    int needed = DeckConstructionManager.MIN_DECK_SIZE - size;
                    validationText.text = $"Need {needed} more cards";
                    validationText.color = new Color(1f, 0.5f, 0.3f);
                    Debug.Log($"[DeckConstructionUI] UpdateDeckSizeDisplay: need {needed} more cards");
                }
                else if (size > DeckConstructionManager.MAX_DECK_SIZE)
                {
                    int excess = size - DeckConstructionManager.MAX_DECK_SIZE;
                    validationText.text = $"Remove {excess} cards";
                    validationText.color = new Color(1f, 0.3f, 0.3f);
                    Debug.Log($"[DeckConstructionUI] UpdateDeckSizeDisplay: need to remove {excess} cards");
                }
                else
                {
                    validationText.text = "Deck valid!";
                    validationText.color = new Color(0.4f, 1f, 0.4f);
                    Debug.Log("[DeckConstructionUI] UpdateDeckSizeDisplay: deck is valid");
                }
            }

            if (startRunBtnImage != null)
            {
                startRunBtnImage.color = valid
                    ? new Color(0.2f, 0.6f, 0.2f)
                    : new Color(0.3f, 0.3f, 0.3f);
            }
        }

        private Color GetCardTypeColor(CardType type)
        {
            switch (type)
            {
                case CardType.Hero: return new Color(0.15f, 0.25f, 0.4f, 0.9f);
                case CardType.Equipment: return new Color(0.3f, 0.25f, 0.15f, 0.9f);
                case CardType.Tactical: return new Color(0.15f, 0.2f, 0.35f, 0.9f);
                case CardType.Colony: return new Color(0.25f, 0.18f, 0.10f, 0.9f);
                default: return new Color(0.2f, 0.2f, 0.2f, 0.9f);
            }
        }

        private void ClearList(List<GameObject> list)
        {
            Debug.Log($"[DeckConstructionUI] ClearList: destroying {list.Count} GameObjects");
            foreach (var go in list)
            {
                if (go != null) Destroy(go);
            }
            list.Clear();
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
            tmp.richText = true;
            tmp.raycastTarget = false; // Don't block pointer events from reaching parent EventTriggers
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

        private void OnDestroy()
        {
            Debug.Log("[DeckConstructionUI] OnDestroy: cleaning up");
            ClearList(poolCardGOs);
            ClearList(deckEntryGOs);
        }
    }

    /// <summary>
    /// Forwards scroll events from child UI elements to the parent ScrollRect.
    /// Attach to any child that blocks scroll input (cards, buttons inside scroll views).
    /// </summary>
    public class ScrollForwarder : MonoBehaviour, IScrollHandler
    {
        private ScrollRect parentScrollRect;

        private void Awake()
        {
            parentScrollRect = GetComponentInParent<ScrollRect>();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (parentScrollRect != null)
                parentScrollRect.OnScroll(eventData);
        }
    }
}
