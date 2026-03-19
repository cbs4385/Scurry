using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Data;

namespace Scurry.UI
{
    /// <summary>
    /// Programmatic overlay for post-combat card rewards (1-of-3 draft + Skip).
    /// Same pattern as CombatUI/ColonyOverlayUI.
    /// </summary>
    public class CardRewardUI : MonoBehaviour
    {
        private const float CardWidth = 220f;
        private const float CardHeight = 320f;
        private const float CardSpacing = 30f;

        private Canvas canvas;
        private GameObject mainPanel;
        private readonly List<GameObject> cardGOs = new List<GameObject>();

        private Action<CardDefinitionSO> onCardSelected;
        private Action onSkip;

        private void Awake()
        {
            Debug.Log("[CardRewardUI] Awake: initializing");
            BuildUI();
        }

        private void BuildUI()
        {
            Debug.Log("[CardRewardUI] BuildUI: starting construction");
            UIHelper.EnsureEventSystem();

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

            // Main panel
            mainPanel = new GameObject("RewardPanel", typeof(RectTransform), typeof(Image));
            mainPanel.transform.SetParent(transform, false);
            var mainRect = mainPanel.GetComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            mainRect.sizeDelta = Vector2.zero;
            mainPanel.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.05f, 0.92f);

            mainPanel.SetActive(false);
            Debug.Log("[CardRewardUI] BuildUI: complete (panel hidden)");
        }

        /// <summary>
        /// Shows the card reward overlay with the given card choices.
        /// </summary>
        public void Show(List<CardDefinitionSO> cards, Action<CardDefinitionSO> onCardSelected, Action onSkip)
        {
            Debug.Log($"[CardRewardUI] Show: displaying {cards?.Count ?? 0} reward cards");

            this.onCardSelected = onCardSelected;
            this.onSkip = onSkip;

            // Clear old cards
            foreach (var go in cardGOs)
            {
                if (go != null) Destroy(go);
            }
            cardGOs.Clear();

            // Destroy old dynamic children (title, skip btn)
            for (int i = mainPanel.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(mainPanel.transform.GetChild(i).gameObject);
            }

            mainPanel.SetActive(true);

            // Title
            CreateText(mainPanel.transform, "Choose a Reward Card",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -30), new Vector2(600, 50),
                32, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.3f));

            // Subtitle
            CreateText(mainPanel.transform, "Select a card to add to your deck, or skip",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -70), new Vector2(600, 30),
                16, FontStyles.Italic, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.8f));

            // Build card choices
            if (cards != null)
            {
                float totalWidth = cards.Count * (CardWidth + CardSpacing) - CardSpacing;
                float startX = -totalWidth * 0.5f + CardWidth * 0.5f;

                for (int i = 0; i < cards.Count; i++)
                {
                    var card = cards[i];
                    float xPos = startX + i * (CardWidth + CardSpacing);
                    var cardGO = CreateRewardCard(card, xPos);
                    cardGOs.Add(cardGO);
                    Debug.Log($"[CardRewardUI] Show: created card '{card.cardName}' at x={xPos}");
                }
            }

            // Skip button
            var skipBtnGO = new GameObject("SkipButton", typeof(RectTransform), typeof(Image), typeof(Button));
            skipBtnGO.transform.SetParent(mainPanel.transform, false);
            var skipRect = skipBtnGO.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(0.35f, 0.05f);
            skipRect.anchorMax = new Vector2(0.65f, 0.12f);
            skipRect.sizeDelta = Vector2.zero;
            skipBtnGO.GetComponent<Image>().color = new Color(0.4f, 0.25f, 0.25f);
            skipBtnGO.GetComponent<Button>().onClick.AddListener(OnSkipClicked);

            CreateText(skipBtnGO.transform, "Skip (Keep Deck Small)",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                18, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            Debug.Log("[CardRewardUI] Show: reward panel displayed");
        }

        /// <summary>
        /// Hides the reward overlay.
        /// </summary>
        public void Hide()
        {
            Debug.Log("[CardRewardUI] Hide: hiding reward panel");
            if (mainPanel != null) mainPanel.SetActive(false);
            onCardSelected = null;
            onSkip = null;
        }

        private GameObject CreateRewardCard(CardDefinitionSO card, float xPos)
        {
            var cardGO = new GameObject($"Reward_{card.cardName}", typeof(RectTransform), typeof(Image), typeof(Button));
            cardGO.transform.SetParent(mainPanel.transform, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(xPos, 20);
            cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);

            Color bgColor = GetCardTypeColor(card.cardType);
            cardGO.GetComponent<Image>().color = bgColor;

            // Card name
            CreateText(cardGO.transform, card.cardName,
                new Vector2(0, 0.82f), new Vector2(1, 0.95f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                18, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            // Type + rarity badge
            string typeBadge = $"{card.cardType} - {card.rarity}";
            CreateText(cardGO.transform, typeBadge,
                new Vector2(0, 0.74f), new Vector2(1, 0.82f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                12, FontStyles.Italic, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.6f));

            // Stats / description
            string statsText = GetCardStatsText(card);
            CreateText(cardGO.transform, statsText,
                new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.73f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                13, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.9f, 0.9f, 0.95f));

            // Cost badge
            CreateText(cardGO.transform, $"Cost: {card.deckCost}",
                new Vector2(0, 0.05f), new Vector2(1, 0.15f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                12, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.9f, 0.7f, 0.2f));

            // Click handler
            var capturedCard = card;
            cardGO.GetComponent<Button>().onClick.AddListener(() => OnCardClicked(capturedCard));

            return cardGO;
        }

        private void OnCardClicked(CardDefinitionSO card)
        {
            Debug.Log($"[CardRewardUI] OnCardClicked: selected '{card?.cardName}'");
            onCardSelected?.Invoke(card);
        }

        private void OnSkipClicked()
        {
            Debug.Log("[CardRewardUI] OnSkipClicked: player chose to skip reward");
            onSkip?.Invoke();
        }

        private Color GetCardTypeColor(CardType type)
        {
            switch (type)
            {
                case CardType.Hero: return new Color(0.12f, 0.20f, 0.35f, 0.95f);
                case CardType.Equipment: return new Color(0.25f, 0.15f, 0.10f, 0.95f);
                case CardType.Tactical: return new Color(0.10f, 0.15f, 0.30f, 0.95f);
                default: return new Color(0.15f, 0.15f, 0.15f, 0.95f);
            }
        }

        private string GetCardStatsText(CardDefinitionSO card)
        {
            switch (card.cardType)
            {
                case CardType.Hero:
                    return $"Role: {card.heroRole}\n" +
                           $"Combat: {card.combat}  Move: {card.move}\n" +
                           $"HP: {card.hp}  Carry: {card.carry}\n" +
                           (card.specialAbility != SpecialAbility.None ? $"\n{card.specialAbilityDescription}" : "");
                case CardType.Equipment:
                    return $"Slot: {card.equipmentSlot}\n\n{card.equipmentEffectDescription}";
                case CardType.Tactical:
                    return $"Type: {card.tacticalType}\n\n{card.tacticalEffectDescription}";
                default:
                    return "";
            }
        }

        private TextMeshProUGUI CreateText(Transform parent, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta,
            float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
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
            return tmp;
        }

        private void OnDestroy()
        {
            Debug.Log("[CardRewardUI] OnDestroy: cleaning up");
            foreach (var go in cardGOs)
            {
                if (go != null) Destroy(go);
            }
            cardGOs.Clear();
        }
    }
}
