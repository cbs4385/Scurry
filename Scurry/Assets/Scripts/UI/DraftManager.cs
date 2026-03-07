using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Data;
using Scurry.Core;
using Scurry.Interfaces;

namespace Scurry.UI
{
    public class DraftManager : MonoBehaviour
    {
        private IBalanceConfig balanceConfig;

        private GameObject draftPanel;
        private GameObject removalPanel;
        private readonly List<GameObject> cardSlots = new List<GameObject>();
        private readonly List<GameObject> removalSlots = new List<GameObject>();

        private List<CardDefinitionSO> draftOptions = new List<CardDefinitionSO>();
        private List<CardDefinitionSO> playerDeck;

        private void Awake()
        {
            BuildDraftPanel();
        }

        private void Start()
        {
            balanceConfig = ServiceLocator.Get<IBalanceConfig>();
            Debug.Log($"[DraftManager] Start: balanceConfig={(balanceConfig != null ? "OK" : "NULL")}");
        }

        public void OpenDraft(List<CardDefinitionSO> cardPool, List<CardDefinitionSO> deck)
        {
            playerDeck = deck;
            Debug.Log($"[DraftManager] OpenDraft: poolSize={cardPool?.Count ?? 0}, deckSize={deck?.Count ?? 0}");

            GenerateDraftOptions(cardPool);
            draftPanel.SetActive(true);
            if (removalPanel != null) removalPanel.SetActive(false);
        }

        private void GenerateDraftOptions(List<CardDefinitionSO> pool)
        {
            draftOptions.Clear();
            if (pool == null || pool.Count == 0)
            {
                Debug.LogWarning("[DraftManager] GenerateDraftOptions: empty pool");
                return;
            }

            var shuffled = new List<CardDefinitionSO>(pool);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = SeededRandom.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            var bc = balanceConfig;
            int draftSize = bc != null ? bc.DraftCardCount : 3;
            int count = Mathf.Min(draftSize, shuffled.Count);
            for (int i = 0; i < count; i++)
                draftOptions.Add(shuffled[i]);

            Debug.Log($"[DraftManager] GenerateDraftOptions: offering {draftOptions.Count} cards");
            RenderDraftCards();
        }

        private void BuildDraftPanel()
        {
            draftPanel = new GameObject("DraftPanel", typeof(RectTransform), typeof(Image));
            draftPanel.transform.SetParent(transform, false);
            var panelRect = draftPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            draftPanel.GetComponent<Image>().color = new Color(0.08f, 0.05f, 0.1f, 0.95f);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(draftPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 40);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Card Draft";
            titleTmp.fontSize = 24;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.7f, 0.4f, 0.8f);

            // Subtitle
            var subGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(draftPanel.transform, false);
            var subRect = subGO.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0, 1);
            subRect.anchorMax = new Vector2(1, 1);
            subRect.pivot = new Vector2(0.5f, 1);
            subRect.anchoredPosition = new Vector2(0, -50);
            subRect.sizeDelta = new Vector2(0, 25);
            var subTmp = subGO.GetComponent<TextMeshProUGUI>();
            subTmp.text = "Pick one card to add, or switch to removal mode";
            subTmp.fontSize = 14;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = Color.white;

            // Switch to removal mode button
            var removeToggleGO = new GameObject("RemoveToggleBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            removeToggleGO.transform.SetParent(draftPanel.transform, false);
            var rtRect = removeToggleGO.GetComponent<RectTransform>();
            rtRect.anchorMin = new Vector2(0, 0);
            rtRect.anchorMax = new Vector2(0, 0);
            rtRect.pivot = new Vector2(0, 0);
            rtRect.anchoredPosition = new Vector2(20, 20);
            rtRect.sizeDelta = new Vector2(200, 45);
            removeToggleGO.GetComponent<Image>().color = new Color(0.6f, 0.2f, 0.2f);

            var removeTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            removeTextGO.transform.SetParent(removeToggleGO.transform, false);
            var rvRect = removeTextGO.GetComponent<RectTransform>();
            rvRect.anchorMin = Vector2.zero;
            rvRect.anchorMax = Vector2.one;
            rvRect.sizeDelta = Vector2.zero;
            var rvTmp = removeTextGO.GetComponent<TextMeshProUGUI>();
            rvTmp.text = "Remove a Card Instead";
            rvTmp.fontSize = 14;
            rvTmp.alignment = TextAlignmentOptions.Center;
            rvTmp.color = Color.white;
            removeToggleGO.GetComponent<Button>().onClick.AddListener(OnSwitchToRemoval);

            // Skip button
            var skipGO = new GameObject("SkipBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            skipGO.transform.SetParent(draftPanel.transform, false);
            var skipRect = skipGO.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(1, 0);
            skipRect.anchorMax = new Vector2(1, 0);
            skipRect.pivot = new Vector2(1, 0);
            skipRect.anchoredPosition = new Vector2(-20, 20);
            skipRect.sizeDelta = new Vector2(150, 45);
            skipGO.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.5f);

            var skipTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            skipTextGO.transform.SetParent(skipGO.transform, false);
            var stRect = skipTextGO.GetComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.sizeDelta = Vector2.zero;
            var stTmp = skipTextGO.GetComponent<TextMeshProUGUI>();
            stTmp.text = "Skip";
            stTmp.fontSize = 16;
            stTmp.alignment = TextAlignmentOptions.Center;
            stTmp.color = Color.white;
            skipGO.GetComponent<Button>().onClick.AddListener(OnSkipClicked);

            draftPanel.SetActive(false);
            Debug.Log("[DraftManager] BuildDraftPanel: complete (hidden)");
        }

        private void RenderDraftCards()
        {
            foreach (var slot in cardSlots) Destroy(slot);
            cardSlots.Clear();

            float cardWidth = 130f;
            float spacing = 20f;
            float totalWidth = draftOptions.Count * (cardWidth + spacing) - spacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < draftOptions.Count; i++)
            {
                var card = draftOptions[i];
                var cardGO = new GameObject($"DraftCard_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardGO.transform.SetParent(draftPanel.transform, false);
                var cardRect = cardGO.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing) + cardWidth / 2, 30);
                cardRect.sizeDelta = new Vector2(cardWidth, 170);
                cardGO.GetComponent<Image>().color = card.placeholderColor;

                var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGO.transform.SetParent(cardGO.transform, false);
                var textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(5, 5);
                textRect.offsetMax = new Vector2(-5, -5);
                var tmp = textGO.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b>{card.cardName}</b>\n{card.rarity} {card.cardType}";
                if (card.cardType == CardType.Hero)
                    tmp.text += $"\nC:{card.combat} M:{card.movement} K:{card.carryCapacity}";
                else if (card.cardType == CardType.Equipment)
                    tmp.text += $"\n{card.equipmentSlot} +{card.equipmentBonusValue}";
                tmp.fontSize = 12;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.color = Color.white;

                int capturedIndex = i;
                cardGO.GetComponent<Button>().onClick.AddListener(() => OnDraftCard(capturedIndex));
                cardSlots.Add(cardGO);
            }
        }

        private void OnDraftCard(int index)
        {
            if (index < 0 || index >= draftOptions.Count) return;
            var card = draftOptions[index];
            Debug.Log($"[DraftManager] OnDraftCard: selected '{card.cardName}' to add to deck");

            EventBus.OnCardDrafted?.Invoke(card);

            draftPanel.SetActive(false);
            EventBus.OnDraftComplete?.Invoke();
        }

        private void OnSwitchToRemoval()
        {
            Debug.Log("[DraftManager] OnSwitchToRemoval: switching to removal mode");
            RenderRemovalCards();
        }

        private void RenderRemovalCards()
        {
            // Clear draft cards
            foreach (var slot in cardSlots) Destroy(slot);
            cardSlots.Clear();

            if (playerDeck == null || playerDeck.Count == 0)
            {
                Debug.Log("[DraftManager] RenderRemovalCards: empty deck");
                return;
            }

            float cardWidth = 90f;
            float spacing = 8f;
            float totalWidth = playerDeck.Count * (cardWidth + spacing) - spacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < playerDeck.Count; i++)
            {
                var card = playerDeck[i];
                var cardGO = new GameObject($"RemoveCard_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardGO.transform.SetParent(draftPanel.transform, false);
                var cardRect = cardGO.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing) + cardWidth / 2, 30);
                cardRect.sizeDelta = new Vector2(cardWidth, 120);
                cardGO.GetComponent<Image>().color = card.placeholderColor;

                var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGO.transform.SetParent(cardGO.transform, false);
                var textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(3, 3);
                textRect.offsetMax = new Vector2(-3, -3);
                var tmp = textGO.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b>{card.cardName}</b>\n{card.cardType}";
                tmp.fontSize = 10;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.color = Color.white;

                int capturedIndex = i;
                cardGO.GetComponent<Button>().onClick.AddListener(() => OnRemoveCard(capturedIndex));
                cardSlots.Add(cardGO);
            }
        }

        private void OnRemoveCard(int index)
        {
            if (playerDeck == null || index < 0 || index >= playerDeck.Count) return;
            var card = playerDeck[index];
            Debug.Log($"[DraftManager] OnRemoveCard: removing '{card.cardName}' from deck");

            EventBus.OnCardRemoved?.Invoke(card);

            draftPanel.SetActive(false);
            EventBus.OnDraftComplete?.Invoke();
        }

        private void OnSkipClicked()
        {
            Debug.Log("[DraftManager] OnSkipClicked: skipping draft");
            draftPanel.SetActive(false);
            EventBus.OnDraftComplete?.Invoke();
        }
    }
}
