using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Data;
using Scurry.Core;
using Scurry.Colony;
using Scurry.Interfaces;

namespace Scurry.UI
{
    public class ColonyUI : MonoBehaviour
    {
        private IColonyBoardManager colonyBoardManager;

        private GameObject colonyPanel;
        private GameObject gridContainer;
        private GameObject handPanel;
        private TextMeshProUGUI statsText;
        private TextMeshProUGUI handCounterText;
        private TextMeshProUGUI instructionText;
        private Button finishButton;
        private Button nextHandButton;

        private readonly List<GameObject> gridSlots = new List<GameObject>();
        private readonly List<GameObject> handCards = new List<GameObject>();

        private ColonyCardDefinitionSO selectedCard;

        private void OnEnable()
        {
            EventBus.OnColonyManagementComplete += OnColonyComplete;
            EventBus.OnLevelStarted += OnLevelStarted;
        }

        private void OnDisable()
        {
            EventBus.OnColonyManagementComplete -= OnColonyComplete;
            EventBus.OnLevelStarted -= OnLevelStarted;
        }

        private void Awake()
        {
            BuildColonyPanel();
        }

        private void Start()
        {
            colonyBoardManager = ServiceLocator.Get<IColonyBoardManager>();
            Debug.Log($"[ColonyUI] Start: colonyBoardManager={(colonyBoardManager != null ? "OK" : "NULL")}");
        }

        private void OnLevelStarted(int level)
        {
            Debug.Log($"[ColonyUI] OnLevelStarted: level={level}, showing colony management");
            ShowColony();
        }

        private void OnColonyComplete(ColonyConfig config)
        {
            Debug.Log($"[ColonyUI] OnColonyComplete: hiding colony panel — {config}");
            colonyPanel.SetActive(false);
        }

        private void BuildColonyPanel()
        {
            colonyPanel = new GameObject("ColonyPanel", typeof(RectTransform), typeof(Image));
            colonyPanel.transform.SetParent(transform, false);
            var panelRect = colonyPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            colonyPanel.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.04f, 0.95f);

            // Title — top center
            var titleGO = new GameObject("ColonyTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(colonyPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -5);
            titleRect.sizeDelta = new Vector2(0, 35);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Colony Management";
            titleTmp.fontSize = 24;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.9f, 0.7f, 0.3f);

            // Hand counter — below title
            var counterGO = new GameObject("HandCounter", typeof(RectTransform), typeof(TextMeshProUGUI));
            counterGO.transform.SetParent(colonyPanel.transform, false);
            var counterRect = counterGO.GetComponent<RectTransform>();
            counterRect.anchorMin = new Vector2(0, 1);
            counterRect.anchorMax = new Vector2(1, 1);
            counterRect.pivot = new Vector2(0.5f, 1);
            counterRect.anchoredPosition = new Vector2(0, -38);
            counterRect.sizeDelta = new Vector2(0, 22);
            handCounterText = counterGO.GetComponent<TextMeshProUGUI>();
            handCounterText.fontSize = 14;
            handCounterText.alignment = TextAlignmentOptions.Center;
            handCounterText.color = Color.white;

            // Instruction text — below hand counter
            var instrGO = new GameObject("Instructions", typeof(RectTransform), typeof(TextMeshProUGUI));
            instrGO.transform.SetParent(colonyPanel.transform, false);
            var instrRect = instrGO.GetComponent<RectTransform>();
            instrRect.anchorMin = new Vector2(0, 1);
            instrRect.anchorMax = new Vector2(1, 1);
            instrRect.pivot = new Vector2(0.5f, 1);
            instrRect.anchoredPosition = new Vector2(0, -58);
            instrRect.sizeDelta = new Vector2(0, 22);
            instructionText = instrGO.GetComponent<TextMeshProUGUI>();
            instructionText.fontSize = 14;
            instructionText.fontStyle = FontStyles.Italic;
            instructionText.alignment = TextAlignmentOptions.Center;
            instructionText.color = new Color(0.7f, 0.7f, 0.6f);
            instructionText.text = "Click a card below, then click a grid slot to place it.";

            // Grid container — centered, between top labels and hand area
            // Uses anchor-based region: top 18% to top 75% of panel
            gridContainer = new GameObject("GridContainer", typeof(RectTransform));
            gridContainer.transform.SetParent(colonyPanel.transform, false);
            var gcRect = gridContainer.GetComponent<RectTransform>();
            gcRect.anchorMin = new Vector2(0.1f, 0.32f);
            gcRect.anchorMax = new Vector2(0.7f, 0.88f);
            gcRect.offsetMin = Vector2.zero;
            gcRect.offsetMax = Vector2.zero;

            // Stats panel — right side
            var statsGO = new GameObject("Stats", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsGO.transform.SetParent(colonyPanel.transform, false);
            var statsRect = statsGO.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.72f, 0.35f);
            statsRect.anchorMax = new Vector2(0.98f, 0.88f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;
            statsText = statsGO.GetComponent<TextMeshProUGUI>();
            statsText.fontSize = 14;
            statsText.alignment = TextAlignmentOptions.TopLeft;
            statsText.color = new Color(0.8f, 0.8f, 0.7f);

            // Hand panel — bottom area, spanning most of the width
            handPanel = new GameObject("HandPanel", typeof(RectTransform));
            handPanel.transform.SetParent(colonyPanel.transform, false);
            var hpRect = handPanel.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0.02f, 0.12f);
            hpRect.anchorMax = new Vector2(0.7f, 0.30f);
            hpRect.offsetMin = Vector2.zero;
            hpRect.offsetMax = Vector2.zero;

            // Finish button — bottom right
            var finishGO = new GameObject("FinishButton", typeof(RectTransform), typeof(Image), typeof(Button));
            finishGO.transform.SetParent(colonyPanel.transform, false);
            var finishRect = finishGO.GetComponent<RectTransform>();
            finishRect.anchorMin = new Vector2(1, 0);
            finishRect.anchorMax = new Vector2(1, 0);
            finishRect.pivot = new Vector2(1, 0);
            finishRect.anchoredPosition = new Vector2(-20, 20);
            finishRect.sizeDelta = new Vector2(180, 50);
            finishGO.GetComponent<Image>().color = new Color(0.3f, 0.6f, 0.3f);

            var finishTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            finishTextGO.transform.SetParent(finishGO.transform, false);
            var ftRect = finishTextGO.GetComponent<RectTransform>();
            ftRect.anchorMin = Vector2.zero;
            ftRect.anchorMax = Vector2.one;
            ftRect.sizeDelta = Vector2.zero;
            var ftTmp = finishTextGO.GetComponent<TextMeshProUGUI>();
            ftTmp.text = "Finish Colony";
            ftTmp.fontSize = 18;
            ftTmp.alignment = TextAlignmentOptions.Center;
            ftTmp.color = Color.white;

            finishButton = finishGO.GetComponent<Button>();
            finishButton.onClick.AddListener(OnFinishClicked);

            // Next Hand button — bottom center-right
            var nextGO = new GameObject("NextHandButton", typeof(RectTransform), typeof(Image), typeof(Button));
            nextGO.transform.SetParent(colonyPanel.transform, false);
            var nextRect = nextGO.GetComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(1, 0);
            nextRect.anchorMax = new Vector2(1, 0);
            nextRect.pivot = new Vector2(1, 0);
            nextRect.anchoredPosition = new Vector2(-20, 80);
            nextRect.sizeDelta = new Vector2(180, 50);
            nextGO.GetComponent<Image>().color = new Color(0.5f, 0.4f, 0.2f);

            var nextTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            nextTextGO.transform.SetParent(nextGO.transform, false);
            var ntRect = nextTextGO.GetComponent<RectTransform>();
            ntRect.anchorMin = Vector2.zero;
            ntRect.anchorMax = Vector2.one;
            ntRect.sizeDelta = Vector2.zero;
            var ntTmp = nextTextGO.GetComponent<TextMeshProUGUI>();
            ntTmp.text = "Next Hand";
            ntTmp.fontSize = 18;
            ntTmp.alignment = TextAlignmentOptions.Center;
            ntTmp.color = Color.white;

            nextHandButton = nextGO.GetComponent<Button>();
            nextHandButton.onClick.AddListener(OnNextHandClicked);

            colonyPanel.SetActive(false);
            Debug.Log("[ColonyUI] BuildColonyPanel: complete (hidden)");
        }

        private void ShowColony()
        {
            colonyPanel.SetActive(true);
            selectedCard = null;
            UpdateInstructions();
            RenderGrid();
            RenderHand();
            UpdateStats();
            UpdateHandCounter();
        }

        private void UpdateInstructions()
        {
            if (instructionText == null) return;
            if (selectedCard != null)
                instructionText.text = $"Selected: <b>{selectedCard.cardName}</b> — Click a green slot on the grid to place it. Click the card again to deselect.";
            else if (colonyBoardManager.CurrentHand.Count > 0)
                instructionText.text = "Click a card below to select it, then click a grid slot to place it.";
            else
                instructionText.text = "All cards placed! Click 'Finish Colony' when ready, or click a placed card to remove it.";
        }

        private void RenderGrid()
        {
            foreach (var slot in gridSlots) Destroy(slot);
            gridSlots.Clear();

            int size = colonyBoardManager.BoardSize;

            // Calculate slot size to fit within gridContainer with gaps
            float gap = 4f;
            // Use anchors to distribute slots evenly within the grid container
            float cellSize = 1f / size;

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var slotGO = new GameObject($"Slot_{r}_{c}", typeof(RectTransform), typeof(Image), typeof(Button));
                    slotGO.transform.SetParent(gridContainer.transform, false);
                    var slotRect = slotGO.GetComponent<RectTransform>();
                    // Anchor each slot to its grid cell within the container
                    slotRect.anchorMin = new Vector2(c * cellSize, 1f - (r + 1) * cellSize);
                    slotRect.anchorMax = new Vector2((c + 1) * cellSize, 1f - r * cellSize);
                    slotRect.offsetMin = new Vector2(gap / 2, gap / 2);
                    slotRect.offsetMax = new Vector2(-gap / 2, -gap / 2);

                    var card = colonyBoardManager.GetCardAt(new Vector2Int(r, c));
                    if (card != null)
                    {
                        slotGO.GetComponent<Image>().color = card.placeholderColor;
                        // Card name label
                        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                        labelGO.transform.SetParent(slotGO.transform, false);
                        var labelRect = labelGO.GetComponent<RectTransform>();
                        labelRect.anchorMin = Vector2.zero;
                        labelRect.anchorMax = Vector2.one;
                        labelRect.sizeDelta = Vector2.zero;
                        var tmp = labelGO.GetComponent<TextMeshProUGUI>();
                        tmp.text = card.cardName;
                        tmp.fontSize = 10;
                        tmp.alignment = TextAlignmentOptions.Center;
                        tmp.color = Color.white;

                        // Click to remove
                        Vector2Int pos = new Vector2Int(r, c);
                        slotGO.GetComponent<Button>().onClick.AddListener(() => OnGridSlotClicked(pos));
                    }
                    else
                    {
                        // Empty slot — click to place selected card
                        bool valid = selectedCard != null && colonyBoardManager.IsValidPlacement(selectedCard, new Vector2Int(r, c));
                        slotGO.GetComponent<Image>().color = valid
                            ? new Color(0.3f, 0.6f, 0.3f, 0.7f)
                            : new Color(0.2f, 0.2f, 0.2f, 0.4f);

                        // Show "+" hint on valid slots
                        if (valid)
                        {
                            var hintGO = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
                            hintGO.transform.SetParent(slotGO.transform, false);
                            var hintRect = hintGO.GetComponent<RectTransform>();
                            hintRect.anchorMin = Vector2.zero;
                            hintRect.anchorMax = Vector2.one;
                            hintRect.sizeDelta = Vector2.zero;
                            var hintTmp = hintGO.GetComponent<TextMeshProUGUI>();
                            hintTmp.text = "+";
                            hintTmp.fontSize = 20;
                            hintTmp.alignment = TextAlignmentOptions.Center;
                            hintTmp.color = new Color(1f, 1f, 1f, 0.6f);
                        }

                        Vector2Int pos = new Vector2Int(r, c);
                        slotGO.GetComponent<Button>().onClick.AddListener(() => OnGridSlotClicked(pos));
                    }

                    gridSlots.Add(slotGO);
                }
            }
        }

        private void RenderHand()
        {
            foreach (var card in handCards) Destroy(card);
            handCards.Clear();

            var hand = colonyBoardManager.CurrentHand;
            if (hand.Count == 0) return;

            float cardWidth = 120f;
            float cardHeight = 90f;
            float spacing = 8f;

            for (int i = 0; i < hand.Count; i++)
            {
                var cardDef = hand[i];
                var cardGO = new GameObject($"HandCard_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardGO.transform.SetParent(handPanel.transform, false);
                var cardRect = cardGO.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0, 0.5f);
                cardRect.anchorMax = new Vector2(0, 0.5f);
                cardRect.pivot = new Vector2(0, 0.5f);
                cardRect.anchoredPosition = new Vector2(i * (cardWidth + spacing), 0);
                cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);

                bool isSelected = (selectedCard == cardDef);
                Color cardColor = isSelected ? cardDef.placeholderColor * 1.5f : cardDef.placeholderColor;
                cardGO.GetComponent<Image>().color = cardColor;

                // Selection border indicator
                if (isSelected)
                {
                    var outline = cardGO.AddComponent<UnityEngine.UI.Outline>();
                    outline.effectColor = Color.yellow;
                    outline.effectDistance = new Vector2(3, 3);
                }

                // Card info
                var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGO.transform.SetParent(cardGO.transform, false);
                var textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(6, 6);
                textRect.offsetMax = new Vector2(-6, -6);
                var tmp = textGO.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b>{cardDef.cardName}</b>\n{cardDef.colonyEffect} +{cardDef.effectValue}\nPop: {cardDef.populationCost}";
                tmp.fontSize = 14;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.color = Color.white;
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.overflowMode = TextOverflowModes.Truncate;

                ColonyCardDefinitionSO captured = cardDef;
                cardGO.GetComponent<Button>().onClick.AddListener(() => OnHandCardClicked(captured));

                handCards.Add(cardGO);
            }
        }

        private void OnHandCardClicked(ColonyCardDefinitionSO card)
        {
            Debug.Log($"[ColonyUI] OnHandCardClicked: card='{card.cardName}', wasSelected={selectedCard == card}");
            selectedCard = (selectedCard == card) ? null : card;
            UpdateInstructions();
            RenderGrid();
            RenderHand();
        }

        private void OnGridSlotClicked(Vector2Int pos)
        {
            var existingCard = colonyBoardManager.GetCardAt(pos);
            if (existingCard != null)
            {
                // Remove card from grid back to hand
                Debug.Log($"[ColonyUI] OnGridSlotClicked: removing card at {pos}");
                colonyBoardManager.RemoveCard(pos);
                selectedCard = null;
            }
            else if (selectedCard != null)
            {
                // Place selected card
                Debug.Log($"[ColonyUI] OnGridSlotClicked: placing '{selectedCard.cardName}' at {pos}");
                if (colonyBoardManager.TryPlaceCard(selectedCard, pos))
                {
                    selectedCard = null;
                }
            }

            UpdateInstructions();
            RenderGrid();
            RenderHand();
            UpdateStats();
        }

        private void OnNextHandClicked()
        {
            if (!colonyBoardManager.HasMoreHands)
            {
                Debug.Log("[ColonyUI] OnNextHandClicked: no more hands");
                return;
            }

            Debug.Log("[ColonyUI] OnNextHandClicked: drawing next hand");
            selectedCard = null;
            colonyBoardManager.DrawHand();
            RenderGrid();
            RenderHand();
            UpdateHandCounter();
        }

        private void OnFinishClicked()
        {
            Debug.Log("[ColonyUI] OnFinishClicked: finishing colony management");
            colonyBoardManager.FinishColonyManagement();
        }

        private void UpdateStats()
        {
            var config = colonyBoardManager.CalculateColonyEffects();
            statsText.text = $"<b>Colony Stats</b>\n" +
                           $"Hero Deck Size: {config.maxHeroDeckSize}\n" +
                           $"Food/Node: {config.foodConsumptionPerNode}\n" +
                           $"Combat Bonus: +{config.heroCombatBonus}\n" +
                           $"Move Bonus: +{config.heroMoveBonus}\n" +
                           $"Carry Bonus: +{config.heroCarryBonus}\n" +
                           $"Population: {config.totalPopulation}\n" +
                           $"Bonus Food: +{config.bonusStartingFood}";
        }

        private void UpdateHandCounter()
        {
            handCounterText.text = $"Hand {colonyBoardManager.CurrentHandIndex} of {colonyBoardManager.TotalHands}";
            nextHandButton.interactable = colonyBoardManager.HasMoreHands;
            nextHandButton.gameObject.SetActive(colonyBoardManager.TotalHands > 1);
        }
    }
}
