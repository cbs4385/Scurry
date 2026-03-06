using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Data;
using Scurry.Core;
using Scurry.Colony;

namespace Scurry.UI
{
    public class ColonyUI : MonoBehaviour
    {
        [SerializeField] private ColonyBoardManager colonyBoardManager;

        private GameObject colonyPanel;
        private GameObject handPanel;
        private TextMeshProUGUI statsText;
        private TextMeshProUGUI handCounterText;
        private Button finishButton;
        private Button nextHandButton;

        private readonly List<GameObject> gridSlots = new List<GameObject>();
        private readonly List<GameObject> handCards = new List<GameObject>();

        private ColonyCardDefinitionSO selectedCard;
        private bool colonyActive;

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
            if (colonyBoardManager == null) colonyBoardManager = FindObjectOfType<ColonyBoardManager>();
            BuildColonyPanel();
        }

        private void OnLevelStarted(int level)
        {
            Debug.Log($"[ColonyUI] OnLevelStarted: level={level}, showing colony management");
            colonyActive = true;
            ShowColony();
        }

        private void OnColonyComplete(ColonyConfig config)
        {
            Debug.Log($"[ColonyUI] OnColonyComplete: hiding colony panel — {config}");
            colonyActive = false;
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

            // Title
            var titleGO = new GameObject("ColonyTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(colonyPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 40);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Colony Management";
            titleTmp.fontSize = 28;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.9f, 0.7f, 0.3f);

            // Hand counter
            var counterGO = new GameObject("HandCounter", typeof(RectTransform), typeof(TextMeshProUGUI));
            counterGO.transform.SetParent(colonyPanel.transform, false);
            var counterRect = counterGO.GetComponent<RectTransform>();
            counterRect.anchorMin = new Vector2(0, 1);
            counterRect.anchorMax = new Vector2(1, 1);
            counterRect.pivot = new Vector2(0.5f, 1);
            counterRect.anchoredPosition = new Vector2(0, -50);
            counterRect.sizeDelta = new Vector2(0, 30);
            handCounterText = counterGO.GetComponent<TextMeshProUGUI>();
            handCounterText.fontSize = 18;
            handCounterText.alignment = TextAlignmentOptions.Center;
            handCounterText.color = Color.white;

            // Stats panel - right side
            var statsGO = new GameObject("Stats", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsGO.transform.SetParent(colonyPanel.transform, false);
            var statsRect = statsGO.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(1, 0.3f);
            statsRect.anchorMax = new Vector2(1, 0.8f);
            statsRect.pivot = new Vector2(1, 0.5f);
            statsRect.anchoredPosition = new Vector2(-20, 0);
            statsRect.sizeDelta = new Vector2(250, 0);
            statsText = statsGO.GetComponent<TextMeshProUGUI>();
            statsText.fontSize = 14;
            statsText.alignment = TextAlignmentOptions.TopLeft;
            statsText.color = new Color(0.8f, 0.8f, 0.7f);

            // Finish button - bottom right
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

            // Next Hand button - bottom center
            var nextGO = new GameObject("NextHandButton", typeof(RectTransform), typeof(Image), typeof(Button));
            nextGO.transform.SetParent(colonyPanel.transform, false);
            var nextRect = nextGO.GetComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(0.5f, 0);
            nextRect.anchorMax = new Vector2(0.5f, 0);
            nextRect.pivot = new Vector2(0.5f, 0);
            nextRect.anchoredPosition = new Vector2(0, 20);
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

            // Hand panel - bottom area
            handPanel = new GameObject("HandPanel", typeof(RectTransform));
            handPanel.transform.SetParent(colonyPanel.transform, false);
            var hpRect = handPanel.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0, 0);
            hpRect.anchorMax = new Vector2(0.6f, 0);
            hpRect.pivot = new Vector2(0, 0);
            hpRect.anchoredPosition = new Vector2(20, 80);
            hpRect.sizeDelta = new Vector2(0, 120);

            colonyPanel.SetActive(false);
            Debug.Log("[ColonyUI] BuildColonyPanel: complete (hidden)");
        }

        private void ShowColony()
        {
            colonyPanel.SetActive(true);
            selectedCard = null;
            RenderGrid();
            RenderHand();
            UpdateStats();
            UpdateHandCounter();
        }

        private void RenderGrid()
        {
            foreach (var slot in gridSlots) Destroy(slot);
            gridSlots.Clear();

            int size = colonyBoardManager.BoardSize;
            float slotSize = 60f;
            float gridWidth = size * (slotSize + 5);
            float startX = -gridWidth / 2f;
            float startY = 100f; // Center vertically (rough)

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    var slotGO = new GameObject($"Slot_{r}_{c}", typeof(RectTransform), typeof(Image), typeof(Button));
                    slotGO.transform.SetParent(colonyPanel.transform, false);
                    var slotRect = slotGO.GetComponent<RectTransform>();
                    slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                    slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                    slotRect.pivot = new Vector2(0.5f, 0.5f);
                    slotRect.anchoredPosition = new Vector2(startX + c * (slotSize + 5) + slotSize / 2, startY - r * (slotSize + 5));
                    slotRect.sizeDelta = new Vector2(slotSize, slotSize);

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
                        tmp.fontSize = 9;
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
                        slotGO.GetComponent<Image>().color = valid ? new Color(0.3f, 0.5f, 0.3f, 0.5f) : new Color(0.2f, 0.2f, 0.2f, 0.5f);

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
            float cardWidth = 90f;
            float spacing = 10f;

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
                cardRect.sizeDelta = new Vector2(cardWidth, 100);

                bool isSelected = (selectedCard == cardDef);
                cardGO.GetComponent<Image>().color = isSelected ? cardDef.placeholderColor * 1.4f : cardDef.placeholderColor;

                // Card info
                var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGO.transform.SetParent(cardGO.transform, false);
                var textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(4, 4);
                textRect.offsetMax = new Vector2(-4, -4);
                var tmp = textGO.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b>{cardDef.cardName}</b>\n{cardDef.colonyEffect} +{cardDef.effectValue}\nPop: {cardDef.populationCost}";
                tmp.fontSize = 10;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.color = Color.white;

                ColonyCardDefinitionSO captured = cardDef;
                cardGO.GetComponent<Button>().onClick.AddListener(() => OnHandCardClicked(captured));

                handCards.Add(cardGO);
            }
        }

        private void OnHandCardClicked(ColonyCardDefinitionSO card)
        {
            Debug.Log($"[ColonyUI] OnHandCardClicked: card='{card.cardName}', wasSelected={selectedCard == card}");
            selectedCard = (selectedCard == card) ? null : card;
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
        }
    }
}
