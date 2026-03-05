using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Scurry.Data;
using Scurry.Core;

namespace Scurry.Cards
{
    public class DeckBuildingManager : MonoBehaviour
    {
        [SerializeField] private DeckManager deckManager;
        [SerializeField] private int minDeckSize = 10;
        [SerializeField] private int maxDeckSize = 20;

        private GameObject deckBuildPanel;
        private TextMeshProUGUI deckCountText;
        private Button confirmButton;
        private readonly Dictionary<CardDefinitionSO, int> selectedCounts = new Dictionary<CardDefinitionSO, int>();
        private readonly List<TextMeshProUGUI> countLabels = new List<TextMeshProUGUI>();
        private GameObject detailPanel;
        private Image detailSwatch;
        private TextMeshProUGUI detailName;
        private TextMeshProUGUI detailType;
        private TextMeshProUGUI detailStats;
        private TextMeshProUGUI detailAbility;

        private void Awake()
        {
            Debug.Log($"[DeckBuildingManager] Awake: deckManager={deckManager?.name ?? "NULL"}, minDeckSize={minDeckSize}, maxDeckSize={maxDeckSize}");
        }

        private void OnEnable()
        {
            Debug.Log("[DeckBuildingManager] OnEnable: subscribing to EventBus.OnPhaseChanged");
            EventBus.OnPhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            Debug.Log("[DeckBuildingManager] OnDisable: unsubscribing from EventBus.OnPhaseChanged");
            EventBus.OnPhaseChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            Debug.Log($"[DeckBuildingManager] OnPhaseChanged: phase={phase}");
            if (phase == GamePhase.DeckBuild)
                ShowDeckBuildUI();
            else if (deckBuildPanel != null)
                deckBuildPanel.SetActive(false);
        }

        private int GetMaxCopies(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return 3;
                case CardRarity.Uncommon: return 2;
                case CardRarity.Rare: return 2;
                case CardRarity.Legendary: return 1;
                default: return 1;
            }
        }

        private void ShowDeckBuildUI()
        {
            Debug.Log("[DeckBuildingManager] ShowDeckBuildUI: building deck selection UI");
            selectedCounts.Clear();
            countLabels.Clear();

            if (deckBuildPanel != null)
                Destroy(deckBuildPanel);

            // Find or create a Canvas
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[DeckBuildingManager] ShowDeckBuildUI: no Canvas found!");
                return;
            }

            // Full-screen overlay panel
            deckBuildPanel = new GameObject("DeckBuildPanel", typeof(RectTransform), typeof(Image));
            deckBuildPanel.transform.SetParent(canvas.transform, false);
            var panelRect = deckBuildPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            deckBuildPanel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(deckBuildPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(600, 50);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = Loc.Get("ui.deckbuild.title");
            titleTmp.fontSize = 36;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;
            titleTmp.fontStyle = FontStyles.Bold;

            // Subtitle with instructions
            var subtitleGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subtitleGO.transform.SetParent(deckBuildPanel.transform, false);
            var subRect = subtitleGO.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1);
            subRect.anchorMax = new Vector2(0.5f, 1);
            subRect.pivot = new Vector2(0.5f, 1);
            subRect.anchoredPosition = new Vector2(0, -70);
            subRect.sizeDelta = new Vector2(600, 30);
            var subTmp = subtitleGO.GetComponent<TextMeshProUGUI>();
            subTmp.text = Loc.Format("ui.deckbuild.subtitle", minDeckSize, maxDeckSize);
            subTmp.fontSize = 18;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.7f, 0.7f, 0.7f);

            // Card detail panel — right side
            CreateDetailPanel(deckBuildPanel.transform);

            // Scrollable card list area — left side
            var scrollAreaGO = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollAreaGO.transform.SetParent(deckBuildPanel.transform, false);
            var scrollAreaRect = scrollAreaGO.GetComponent<RectTransform>();
            scrollAreaRect.anchorMin = new Vector2(0.02f, 0.15f);
            scrollAreaRect.anchorMax = new Vector2(0.55f, 0.85f);
            scrollAreaRect.sizeDelta = Vector2.zero;
            scrollAreaGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f); // near-transparent, needed for scroll input
            var scrollComp = scrollAreaGO.GetComponent<ScrollRect>();
            scrollComp.horizontal = false;
            scrollComp.vertical = true;
            scrollComp.movementType = ScrollRect.MovementType.Clamped;
            scrollComp.scrollSensitivity = 30f;

            // Viewport with mask
            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGO.transform.SetParent(scrollAreaGO.transform, false);
            var viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f); // required by Mask
            viewportGO.GetComponent<Mask>().showMaskGraphic = false;
            scrollComp.viewport = viewportRect;

            // Content container
            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            scrollComp.content = contentRect;

            // Create card entries
            var cards = deckManager.AllCards;
            Debug.Log($"[DeckBuildingManager] ShowDeckBuildUI: displaying {cards.Length} available cards");
            float entryHeight = 50f;
            float spacing = 5f;

            // Set content height based on card count
            float totalHeight = cards.Length * (entryHeight + spacing);
            contentRect.sizeDelta = new Vector2(0, totalHeight);

            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                int maxCopies = GetMaxCopies(card.rarity);
                selectedCounts[card] = 0;

                float yPos = -(i * (entryHeight + spacing));

                // Entry row background
                var rowGO = new GameObject($"Row_{card.cardName}", typeof(RectTransform), typeof(Image));
                rowGO.transform.SetParent(contentGO.transform, false);
                var rowRect = rowGO.GetComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0, 1);
                rowRect.anchorMax = new Vector2(1, 1);
                rowRect.pivot = new Vector2(0.5f, 1);
                rowRect.anchoredPosition = new Vector2(0, yPos);
                rowRect.sizeDelta = new Vector2(0, entryHeight);
                rowGO.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

                // Color swatch
                var swatchGO = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
                swatchGO.transform.SetParent(rowGO.transform, false);
                var swatchRect = swatchGO.GetComponent<RectTransform>();
                swatchRect.anchorMin = new Vector2(0, 0.5f);
                swatchRect.anchorMax = new Vector2(0, 0.5f);
                swatchRect.pivot = new Vector2(0, 0.5f);
                swatchRect.anchoredPosition = new Vector2(10, 0);
                swatchRect.sizeDelta = new Vector2(30, 30);
                swatchGO.GetComponent<Image>().color = card.placeholderColor;

                // Card name + type + rarity
                string displayName = !string.IsNullOrEmpty(card.localizationKey) ? Loc.Get(card.localizationKey + ".name") : card.cardName;
                string cardInfo = $"{displayName}  <size=14><color=#888>[{card.cardType} | {card.rarity} | max {maxCopies}]</color></size>";

                var nameGO = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
                nameGO.transform.SetParent(rowGO.transform, false);
                var nameRect = nameGO.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 0);
                nameRect.anchorMax = new Vector2(0.6f, 1);
                nameRect.offsetMin = new Vector2(50, 5);
                nameRect.offsetMax = new Vector2(0, -5);
                var nameTmp = nameGO.GetComponent<TextMeshProUGUI>();
                nameTmp.text = cardInfo;
                nameTmp.fontSize = 18;
                nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
                nameTmp.color = Color.white;
                nameTmp.richText = true;

                // Count label
                var countGO = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
                countGO.transform.SetParent(rowGO.transform, false);
                var countRect = countGO.GetComponent<RectTransform>();
                countRect.anchorMin = new Vector2(0.7f, 0);
                countRect.anchorMax = new Vector2(0.8f, 1);
                countRect.sizeDelta = Vector2.zero;
                var countTmp = countGO.GetComponent<TextMeshProUGUI>();
                countTmp.text = "0";
                countTmp.fontSize = 24;
                countTmp.alignment = TextAlignmentOptions.Center;
                countTmp.color = Color.white;
                countLabels.Add(countTmp);

                // Minus button
                int cardIndex = i;
                CreateCardButton(rowGO.transform, "-", new Vector2(0.62f, 0.15f), new Vector2(0.68f, 0.85f),
                    new Color(0.6f, 0.2f, 0.2f), () => AdjustCount(cards[cardIndex], -1, cardIndex));

                // Plus button
                CreateCardButton(rowGO.transform, "+", new Vector2(0.82f, 0.15f), new Vector2(0.88f, 0.85f),
                    new Color(0.2f, 0.6f, 0.2f), () => AdjustCount(cards[cardIndex], 1, cardIndex));

                // Max button
                CreateCardButton(rowGO.transform, Loc.Get("ui.deckbuild.max"), new Vector2(0.90f, 0.15f), new Vector2(0.98f, 0.85f),
                    new Color(0.3f, 0.3f, 0.6f), () => AdjustCount(cards[cardIndex], maxCopies, cardIndex, true));

                // Hover triggers for detail panel (lightweight — does not block ScrollRect drag)
                var hover = rowGO.AddComponent<CardRowHover>();
                hover.Init(card, ShowCardDetail, HideCardDetail);

                Debug.Log($"[DeckBuildingManager] ShowDeckBuildUI: card row '{card.cardName}' — rarity={card.rarity}, maxCopies={maxCopies}");
            }

            // Deck count text - bottom left area
            var deckCountGO = new GameObject("DeckCount", typeof(RectTransform), typeof(TextMeshProUGUI));
            deckCountGO.transform.SetParent(deckBuildPanel.transform, false);
            var dcRect = deckCountGO.GetComponent<RectTransform>();
            dcRect.anchorMin = new Vector2(0, 0);
            dcRect.anchorMax = new Vector2(0, 0);
            dcRect.pivot = new Vector2(0, 0);
            dcRect.anchoredPosition = new Vector2(40, 35);
            dcRect.sizeDelta = new Vector2(350, 40);
            deckCountText = deckCountGO.GetComponent<TextMeshProUGUI>();
            deckCountText.fontSize = 22;
            deckCountText.alignment = TextAlignmentOptions.Left;
            deckCountText.color = Color.white;
            UpdateDeckCountText();

            // Confirm button - bottom right area
            var confirmGO = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
            confirmGO.transform.SetParent(deckBuildPanel.transform, false);
            var cfRect = confirmGO.GetComponent<RectTransform>();
            cfRect.anchorMin = new Vector2(1, 0);
            cfRect.anchorMax = new Vector2(1, 0);
            cfRect.pivot = new Vector2(1, 0);
            cfRect.anchoredPosition = new Vector2(-40, 25);
            cfRect.sizeDelta = new Vector2(220, 55);
            confirmGO.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.2f);

            var cfTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            cfTextGO.transform.SetParent(confirmGO.transform, false);
            var cfTextRect = cfTextGO.GetComponent<RectTransform>();
            cfTextRect.anchorMin = Vector2.zero;
            cfTextRect.anchorMax = Vector2.one;
            cfTextRect.sizeDelta = Vector2.zero;
            var cfTmp = cfTextGO.GetComponent<TextMeshProUGUI>();
            cfTmp.text = Loc.Get("ui.deckbuild.confirm");
            cfTmp.fontSize = 24;
            cfTmp.alignment = TextAlignmentOptions.Center;
            cfTmp.color = Color.white;

            confirmButton = confirmGO.GetComponent<Button>();
            confirmButton.onClick.AddListener(OnConfirmClicked);
            confirmButton.interactable = false;

            Debug.Log("[DeckBuildingManager] ShowDeckBuildUI: deck build UI complete");
        }

        private void CreateDetailPanel(Transform parent)
        {
            Debug.Log("[DeckBuildingManager] CreateDetailPanel: building card detail panel");
            detailPanel = new GameObject("CardDetailPanel", typeof(RectTransform), typeof(Image));
            detailPanel.transform.SetParent(parent, false);
            var panelRect = detailPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.58f, 0.15f);
            panelRect.anchorMax = new Vector2(0.98f, 0.85f);
            panelRect.sizeDelta = Vector2.zero;
            detailPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Large color swatch (card preview)
            var swatchGO = new GameObject("DetailSwatch", typeof(RectTransform), typeof(Image));
            swatchGO.transform.SetParent(detailPanel.transform, false);
            var swatchRect = swatchGO.GetComponent<RectTransform>();
            swatchRect.anchorMin = new Vector2(0.15f, 0.55f);
            swatchRect.anchorMax = new Vector2(0.85f, 0.92f);
            swatchRect.sizeDelta = Vector2.zero;
            detailSwatch = swatchGO.GetComponent<Image>();
            detailSwatch.color = Color.gray;

            // Card name on the swatch
            var nameGO = new GameObject("DetailName", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGO.transform.SetParent(swatchGO.transform, false);
            var nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.6f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.sizeDelta = Vector2.zero;
            detailName = nameGO.GetComponent<TextMeshProUGUI>();
            detailName.fontSize = 22;
            detailName.fontStyle = FontStyles.Bold;
            detailName.alignment = TextAlignmentOptions.Center;
            detailName.color = Color.black;

            // Stats text on the swatch
            var statsOnSwatchGO = new GameObject("DetailSwatchStats", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsOnSwatchGO.transform.SetParent(swatchGO.transform, false);
            var sosRect = statsOnSwatchGO.GetComponent<RectTransform>();
            sosRect.anchorMin = new Vector2(0, 0);
            sosRect.anchorMax = new Vector2(1, 0.5f);
            sosRect.sizeDelta = Vector2.zero;
            detailStats = statsOnSwatchGO.GetComponent<TextMeshProUGUI>();
            detailStats.fontSize = 20;
            detailStats.alignment = TextAlignmentOptions.Center;
            detailStats.color = Color.black;

            // Type + rarity line below the swatch
            var typeGO = new GameObject("DetailType", typeof(RectTransform), typeof(TextMeshProUGUI));
            typeGO.transform.SetParent(detailPanel.transform, false);
            var typeRect = typeGO.GetComponent<RectTransform>();
            typeRect.anchorMin = new Vector2(0.05f, 0.42f);
            typeRect.anchorMax = new Vector2(0.95f, 0.52f);
            typeRect.sizeDelta = Vector2.zero;
            detailType = typeGO.GetComponent<TextMeshProUGUI>();
            detailType.fontSize = 16;
            detailType.alignment = TextAlignmentOptions.Center;
            detailType.color = new Color(0.7f, 0.7f, 0.7f);

            // Ability / description text
            var abilityGO = new GameObject("DetailAbility", typeof(RectTransform), typeof(TextMeshProUGUI));
            abilityGO.transform.SetParent(detailPanel.transform, false);
            var abilityRect = abilityGO.GetComponent<RectTransform>();
            abilityRect.anchorMin = new Vector2(0.08f, 0.05f);
            abilityRect.anchorMax = new Vector2(0.92f, 0.40f);
            abilityRect.sizeDelta = Vector2.zero;
            detailAbility = abilityGO.GetComponent<TextMeshProUGUI>();
            detailAbility.fontSize = 16;
            detailAbility.alignment = TextAlignmentOptions.Top;
            detailAbility.color = Color.white;
            detailAbility.textWrappingMode = TextWrappingModes.Normal;

            // Start hidden — show "hover over a card" hint
            ShowDetailHint();
            Debug.Log("[DeckBuildingManager] CreateDetailPanel: complete");
        }

        private void ShowDetailHint()
        {
            if (detailSwatch != null) detailSwatch.color = new Color(0.2f, 0.2f, 0.25f);
            if (detailName != null) detailName.text = "";
            if (detailStats != null) detailStats.text = "";
            if (detailType != null) detailType.text = "";
            if (detailAbility != null)
            {
                detailAbility.text = "<i>Hover over a card to see details</i>";
                detailAbility.color = new Color(0.5f, 0.5f, 0.5f);
            }
        }

        private void ShowCardDetail(CardDefinitionSO card)
        {
            if (card == null || detailPanel == null) return;
            Debug.Log($"[DeckBuildingManager] ShowCardDetail: card='{card.cardName}', type={card.cardType}, rarity={card.rarity}");

            string displayName = !string.IsNullOrEmpty(card.localizationKey)
                ? Loc.Get(card.localizationKey + ".name") : card.cardName;

            // Swatch color
            detailSwatch.color = card.placeholderColor;

            // Name on swatch
            detailName.text = displayName;

            // Stats on swatch
            if (card.cardType == CardType.Hero)
                detailStats.text = Loc.Format("card.stat.hero", card.movement, card.combat, card.carryCapacity);
            else
                detailStats.text = Loc.Format("card.stat.resource", card.resourceType, card.value);

            // Type + rarity line
            string rarityColor = card.rarity switch
            {
                CardRarity.Common => "#AAAAAA",
                CardRarity.Uncommon => "#55CC55",
                CardRarity.Rare => "#5599FF",
                CardRarity.Legendary => "#FF9922",
                _ => "#FFFFFF"
            };
            detailType.text = $"{card.cardType}  |  <color={rarityColor}>{card.rarity}</color>";
            detailType.richText = true;

            // Ability / description
            string abilityText = "";
            if (card.cardType == CardType.Hero)
            {
                if (card.specialAbility != SpecialAbility.None)
                {
                    string abilityLoc = !string.IsNullOrEmpty(card.localizationKey)
                        ? Loc.Get(card.localizationKey + ".ability") : "";
                    if (string.IsNullOrEmpty(abilityLoc) || abilityLoc.StartsWith("?"))
                        abilityLoc = card.specialAbility.ToString();
                    abilityText = $"<b>Ability:</b> {card.specialAbility}\n{abilityLoc}";
                }
                abilityText += $"\n\n<b>Movement:</b> {card.movement}\n<b>Combat:</b> {card.combat}\n<b>Carry:</b> {card.carryCapacity}";
            }
            else
            {
                abilityText = $"<b>Resource Type:</b> {card.resourceType}\n<b>Value:</b> {card.value}";
            }
            abilityText += $"\n\n<b>Max Copies:</b> {GetMaxCopies(card.rarity)}";

            detailAbility.text = abilityText;
            detailAbility.richText = true;
            detailAbility.color = Color.white;
        }

        private void HideCardDetail()
        {
            Debug.Log("[DeckBuildingManager] HideCardDetail");
            ShowDetailHint();
        }

        private void CreateCardButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var btnGO = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = anchorMin;
            btnRect.anchorMax = anchorMax;
            btnRect.sizeDelta = Vector2.zero;
            btnGO.GetComponent<Image>().color = color;

            var txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(btnGO.transform, false);
            var txtRect = txtGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;
            var tmp = txtGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            btnGO.GetComponent<Button>().onClick.AddListener(onClick);
        }

        private void AdjustCount(CardDefinitionSO card, int delta, int cardIndex, bool setToMax = false)
        {
            int maxCopies = GetMaxCopies(card.rarity);
            int current = selectedCounts[card];

            if (setToMax)
            {
                selectedCounts[card] = maxCopies;
                Debug.Log($"[DeckBuildingManager] AdjustCount: '{card.cardName}' set to max={maxCopies}");
            }
            else
            {
                int newCount = Mathf.Clamp(current + delta, 0, maxCopies);
                selectedCounts[card] = newCount;
                Debug.Log($"[DeckBuildingManager] AdjustCount: '{card.cardName}' {current} -> {newCount} (delta={delta}, max={maxCopies})");
            }

            // Update count label
            if (cardIndex >= 0 && cardIndex < countLabels.Count)
                countLabels[cardIndex].text = selectedCounts[card].ToString();

            UpdateDeckCountText();
        }

        private int GetTotalSelected()
        {
            int total = 0;
            foreach (var kvp in selectedCounts)
                total += kvp.Value;
            return total;
        }

        private void UpdateDeckCountText()
        {
            int total = GetTotalSelected();
            bool valid = total >= minDeckSize && total <= maxDeckSize;

            if (deckCountText != null)
            {
                deckCountText.text = Loc.Format("ui.deckbuild.count", total, minDeckSize, maxDeckSize);
                deckCountText.color = valid ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
            }

            if (confirmButton != null)
                confirmButton.interactable = valid;

            Debug.Log($"[DeckBuildingManager] UpdateDeckCountText: total={total}, valid={valid}");
        }

        private void OnConfirmClicked()
        {
            int total = GetTotalSelected();
            Debug.Log($"[DeckBuildingManager] OnConfirmClicked: total selected={total}");

            if (total < minDeckSize || total > maxDeckSize)
            {
                Debug.LogWarning($"[DeckBuildingManager] OnConfirmClicked: invalid deck size {total} (need {minDeckSize}-{maxDeckSize})");
                return;
            }

            // Build the selected deck list
            var selectedDeck = new List<CardDefinitionSO>();
            foreach (var kvp in selectedCounts)
            {
                for (int i = 0; i < kvp.Value; i++)
                    selectedDeck.Add(kvp.Key);
                if (kvp.Value > 0)
                    Debug.Log($"[DeckBuildingManager] OnConfirmClicked: '{kvp.Key.cardName}' x{kvp.Value}");
            }

            Debug.Log($"[DeckBuildingManager] OnConfirmClicked: deck built with {selectedDeck.Count} cards, firing OnDeckBuildComplete");
            deckBuildPanel.SetActive(false);
            EventBus.OnDeckBuildComplete?.Invoke(selectedDeck);
        }
    }

    public class CardRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private CardDefinitionSO card;
        private System.Action<CardDefinitionSO> onEnter;
        private System.Action onExit;

        public void Init(CardDefinitionSO card, System.Action<CardDefinitionSO> onEnter, System.Action onExit)
        {
            this.card = card;
            this.onEnter = onEnter;
            this.onExit = onExit;
        }

        public void OnPointerEnter(PointerEventData eventData) => onEnter?.Invoke(card);
        public void OnPointerExit(PointerEventData eventData) => onExit?.Invoke();
    }
}
