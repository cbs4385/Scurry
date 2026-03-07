using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Data;
using Scurry.Core;
using Scurry.Interfaces;

namespace Scurry.UI
{
    public class ColonyDraftUI : MonoBehaviour
    {
        private IRunManager runManager;
        private IBalanceConfig balanceConfig;

        private GameObject draftPanel;
        private GameObject cardGrid;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subtitleText;
        private Button confirmButton;
        private TextMeshProUGUI confirmButtonText;

        private List<ColonyCardDefinitionSO> offeredCards = new List<ColonyCardDefinitionSO>();
        private HashSet<ColonyCardDefinitionSO> selectedCards = new HashSet<ColonyCardDefinitionSO>();
        private List<GameObject> cardObjects = new List<GameObject>();
        private int maxPicks;

        private void Start()
        {
            runManager = ServiceLocator.Get<IRunManager>();
            balanceConfig = ServiceLocator.Get<IBalanceConfig>();
            Debug.Log($"[ColonyDraftUI] Start: runManager={(runManager != null ? "OK" : "NULL")}, balanceConfig={(balanceConfig != null ? "OK" : "NULL")}");
            Debug.Log("[ColonyDraftUI] Start: opening colony draft (scene-based initialization)");
            Open();
        }

        public void Open()
        {
            if (runManager == null || runManager.ColonyCardPool == null || runManager.ColonyCardPool.Count == 0)
            {
                Debug.LogWarning("[ColonyDraftUI] Open: no colony cards available — skipping draft");
                EventBus.OnColonyDraftComplete?.Invoke(new List<ColonyCardDefinitionSO>());
                return;
            }

            var bc = balanceConfig;
            int offerCount = bc != null ? bc.ColonyDraftOfferCount : 12;
            maxPicks = bc != null ? bc.ColonyDraftPickCount : 8;

            // Shuffle and offer a subset
            var pool = new List<ColonyCardDefinitionSO>(runManager.ColonyCardPool);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = SeededRandom.Range(0, i + 1);
                var temp = pool[i];
                pool[i] = pool[j];
                pool[j] = temp;
            }

            int toOffer = Mathf.Min(offerCount, pool.Count);
            offeredCards = pool.GetRange(0, toOffer);
            maxPicks = Mathf.Min(maxPicks, toOffer);
            selectedCards.Clear();

            Debug.Log($"[ColonyDraftUI] Open: offering {toOffer} cards, player picks {maxPicks}");

            if (draftPanel != null) Destroy(draftPanel);
            BuildPanel();
            RenderCards();
            UpdateConfirmButton();
        }

        public void Close()
        {
            Debug.Log("[ColonyDraftUI] Close: closing draft panel");
            if (draftPanel != null) Destroy(draftPanel);
            draftPanel = null;
        }

        private void BuildPanel()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[ColonyDraftUI] BuildPanel: no Canvas found");
                return;
            }

            draftPanel = new GameObject("ColonyDraftPanel", typeof(RectTransform), typeof(Image));
            draftPanel.transform.SetParent(canvas.transform, false);
            var panelRect = draftPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            draftPanel.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.04f, 0.97f);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(draftPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            titleText = titleGO.GetComponent<TextMeshProUGUI>();
            titleText.text = "Colony Card Draft";
            titleText.fontSize = 30;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.9f, 0.7f, 0.3f);

            // Subtitle
            var subGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(draftPanel.transform, false);
            var subRect = subGO.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0, 0.84f);
            subRect.anchorMax = new Vector2(1, 0.9f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;
            subtitleText = subGO.GetComponent<TextMeshProUGUI>();
            subtitleText.fontSize = 18;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.color = new Color(0.7f, 0.7f, 0.6f);

            // Card grid container
            cardGrid = new GameObject("CardGrid", typeof(RectTransform));
            cardGrid.transform.SetParent(draftPanel.transform, false);
            var gridRect = cardGrid.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.03f, 0.12f);
            gridRect.anchorMax = new Vector2(0.97f, 0.83f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            // Confirm button
            var confirmGO = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
            confirmGO.transform.SetParent(draftPanel.transform, false);
            var confirmRect = confirmGO.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.35f, 0.02f);
            confirmRect.anchorMax = new Vector2(0.65f, 0.09f);
            confirmRect.offsetMin = Vector2.zero;
            confirmRect.offsetMax = Vector2.zero;
            confirmGO.GetComponent<Image>().color = new Color(0.3f, 0.6f, 0.3f);

            var confirmTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            confirmTextGO.transform.SetParent(confirmGO.transform, false);
            var ctRect = confirmTextGO.GetComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;
            confirmButtonText = confirmTextGO.GetComponent<TextMeshProUGUI>();
            confirmButtonText.fontSize = 18;
            confirmButtonText.fontStyle = FontStyles.Bold;
            confirmButtonText.alignment = TextAlignmentOptions.Center;
            confirmButtonText.color = Color.white;

            confirmButton = confirmGO.GetComponent<Button>();
            confirmButton.onClick.AddListener(OnConfirmClicked);

            Debug.Log("[ColonyDraftUI] BuildPanel: complete");
        }

        private void RenderCards()
        {
            foreach (var obj in cardObjects) Destroy(obj);
            cardObjects.Clear();

            int count = offeredCards.Count;
            // Arrange in rows — 3 per row for readable card sizes
            int cols = Mathf.Min(3, count);
            int rows = Mathf.CeilToInt((float)count / cols);

            float cellW = 1f / cols;
            float cellH = 1f / rows;

            for (int i = 0; i < count; i++)
            {
                var cardDef = offeredCards[i];
                int col = i % cols;
                int row = i / cols;

                bool isSelected = selectedCards.Contains(cardDef);

                var cardGO = new GameObject($"DraftCard_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardGO.transform.SetParent(cardGrid.transform, false);
                var cardRect = cardGO.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(col * cellW, 1f - (row + 1) * cellH);
                cardRect.anchorMax = new Vector2((col + 1) * cellW, 1f - row * cellH);
                cardRect.offsetMin = new Vector2(5, 5);
                cardRect.offsetMax = new Vector2(-5, -5);

                Color bgColor = isSelected
                    ? new Color(cardDef.placeholderColor.r * 1.3f, cardDef.placeholderColor.g * 1.3f, cardDef.placeholderColor.b * 1.3f, 1f)
                    : cardDef.placeholderColor;
                cardGO.GetComponent<Image>().color = bgColor;

                // Selection border
                if (isSelected)
                {
                    var outline = cardGO.AddComponent<Outline>();
                    outline.effectColor = Color.yellow;
                    outline.effectDistance = new Vector2(3, 3);
                }

                // Card content text
                var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGO.transform.SetParent(cardGO.transform, false);
                var textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8, 8);
                textRect.offsetMax = new Vector2(-8, -8);
                var tmp = textGO.GetComponent<TextMeshProUGUI>();

                string effectLabel = FormatEffect(cardDef.colonyEffect, cardDef.effectValue);
                string placementLabel = cardDef.placementRequirement != PlacementRequirement.None
                    ? $"\nPlacement: {cardDef.placementRequirement}"
                    : "";
                string selectedLabel = isSelected ? "\n<color=yellow>[SELECTED]</color>" : "";

                tmp.text = $"<b>{cardDef.cardName}</b>\n{effectLabel}\nPop: {cardDef.populationCost}{placementLabel}{selectedLabel}";
                tmp.fontSize = 16;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.overflowMode = TextOverflowModes.Truncate;

                ColonyCardDefinitionSO captured = cardDef;
                cardGO.GetComponent<Button>().onClick.AddListener(() => OnCardClicked(captured));

                cardObjects.Add(cardGO);
            }

            Debug.Log($"[ColonyDraftUI] RenderCards: rendered {count} cards in {rows}x{cols} grid, selected={selectedCards.Count}");
        }

        private string FormatEffect(ColonyEffect effect, int value)
        {
            switch (effect)
            {
                case ColonyEffect.IncreaseDeckSize: return $"Deck Size +{value}";
                case ColonyEffect.ReduceConsumption: return $"Food Cost -{value}";
                case ColonyEffect.HeroCombatBonus: return $"Combat +{value}";
                case ColonyEffect.HeroMoveBonus: return $"Move +{value}";
                case ColonyEffect.HeroCarryBonus: return $"Carry +{value}";
                case ColonyEffect.BonusStartingFood: return $"Starting Food +{value}";
                case ColonyEffect.ReducePopulation: return $"Pop Cost -{value}";
                default: return $"{effect} +{value}";
            }
        }

        private void OnCardClicked(ColonyCardDefinitionSO card)
        {
            if (selectedCards.Contains(card))
            {
                selectedCards.Remove(card);
                Debug.Log($"[ColonyDraftUI] OnCardClicked: deselected '{card.cardName}', selected={selectedCards.Count}/{maxPicks}");
            }
            else if (selectedCards.Count < maxPicks)
            {
                selectedCards.Add(card);
                Debug.Log($"[ColonyDraftUI] OnCardClicked: selected '{card.cardName}', selected={selectedCards.Count}/{maxPicks}");
            }
            else
            {
                Debug.Log($"[ColonyDraftUI] OnCardClicked: cannot select '{card.cardName}' — already at max picks ({maxPicks})");
            }

            RenderCards();
            UpdateConfirmButton();
        }

        private void UpdateConfirmButton()
        {
            int remaining = maxPicks - selectedCards.Count;
            if (remaining > 0)
            {
                confirmButtonText.text = $"Select {remaining} more card{(remaining > 1 ? "s" : "")}";
                confirmButton.interactable = false;
                confirmButton.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
            }
            else
            {
                confirmButtonText.text = "Confirm Colony Deck";
                confirmButton.interactable = true;
                confirmButton.GetComponent<Image>().color = new Color(0.3f, 0.6f, 0.3f);
            }

            subtitleText.text = $"Choose {maxPicks} colony cards for your run. Selected: {selectedCards.Count}/{maxPicks}";
        }

        private void OnConfirmClicked()
        {
            if (selectedCards.Count < maxPicks)
            {
                Debug.Log($"[ColonyDraftUI] OnConfirmClicked: not enough cards selected ({selectedCards.Count}/{maxPicks})");
                return;
            }

            var drafted = new List<ColonyCardDefinitionSO>(selectedCards);
            Debug.Log($"[ColonyDraftUI] OnConfirmClicked: confirmed {drafted.Count} colony cards");
            foreach (var card in drafted)
            {
                Debug.Log($"[ColonyDraftUI] OnConfirmClicked: drafted card='{card.cardName}', effect={card.colonyEffect}, value={card.effectValue}");
            }

            Close();
            EventBus.OnColonyDraftComplete?.Invoke(drafted);
        }
    }
}
