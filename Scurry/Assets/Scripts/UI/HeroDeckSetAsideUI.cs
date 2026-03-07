using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Data;
using Scurry.Core;

namespace Scurry.UI
{
    public class HeroDeckSetAsideUI : MonoBehaviour
    {
        private GameObject panel;
        private TextMeshProUGUI counterText;
        private Button confirmButton;
        private readonly List<GameObject> cardSlots = new List<GameObject>();

        private List<CardDefinitionSO> fullDeck;
        private HashSet<CardDefinitionSO> setAsideCards = new HashSet<CardDefinitionSO>();
        private int cardsToSetAside;

        private System.Action<List<CardDefinitionSO>> onComplete;

        private void Awake()
        {
            Debug.Log("[HeroDeckSetAsideUI] Awake");
        }

        public void Open(List<CardDefinitionSO> heroDeck, int maxDeckSize, System.Action<List<CardDefinitionSO>> callback)
        {
            fullDeck = heroDeck;
            onComplete = callback;
            setAsideCards.Clear();

            int heroCount = 0;
            foreach (var card in heroDeck)
            {
                if (card.cardType == CardType.Hero) heroCount++;
            }
            cardsToSetAside = Mathf.Max(0, heroCount - maxDeckSize);

            Debug.Log($"[HeroDeckSetAsideUI] Open: deckSize={heroDeck.Count}, heroCount={heroCount}, maxDeckSize={maxDeckSize}, toSetAside={cardsToSetAside}");

            if (cardsToSetAside <= 0)
            {
                Debug.Log("[HeroDeckSetAsideUI] Open: no cards need to be set aside — completing immediately");
                onComplete?.Invoke(heroDeck);
                return;
            }

            BuildPanel();
            RenderCards();
            UpdateCounter();
        }

        private void BuildPanel()
        {
            if (panel != null) Destroy(panel);

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            panel = new GameObject("SetAsidePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.08f, 0.05f, 0.12f, 0.95f);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(panel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 40);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Set Aside Heroes";
            titleTmp.fontSize = 24;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.8f, 0.6f, 0.9f);

            // Counter
            var counterGO = new GameObject("Counter", typeof(RectTransform), typeof(TextMeshProUGUI));
            counterGO.transform.SetParent(panel.transform, false);
            var counterRect = counterGO.GetComponent<RectTransform>();
            counterRect.anchorMin = new Vector2(0, 1);
            counterRect.anchorMax = new Vector2(1, 1);
            counterRect.pivot = new Vector2(0.5f, 1);
            counterRect.anchoredPosition = new Vector2(0, -55);
            counterRect.sizeDelta = new Vector2(0, 25);
            counterText = counterGO.GetComponent<TextMeshProUGUI>();
            counterText.fontSize = 16;
            counterText.alignment = TextAlignmentOptions.Center;
            counterText.color = Color.white;

            // Confirm button
            var confirmGO = new GameObject("ConfirmBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            confirmGO.transform.SetParent(panel.transform, false);
            var confirmRect = confirmGO.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.5f, 0);
            confirmRect.anchorMax = new Vector2(0.5f, 0);
            confirmRect.pivot = new Vector2(0.5f, 0);
            confirmRect.anchoredPosition = new Vector2(0, 20);
            confirmRect.sizeDelta = new Vector2(200, 50);
            confirmGO.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.2f);

            var confirmTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            confirmTextGO.transform.SetParent(confirmGO.transform, false);
            var ctRect = confirmTextGO.GetComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;
            var ctTmp = confirmTextGO.GetComponent<TextMeshProUGUI>();
            ctTmp.text = "Confirm";
            ctTmp.fontSize = 18;
            ctTmp.fontStyle = FontStyles.Bold;
            ctTmp.alignment = TextAlignmentOptions.Center;
            ctTmp.color = Color.white;

            confirmButton = confirmGO.GetComponent<Button>();
            confirmButton.onClick.AddListener(OnConfirm);
            confirmButton.interactable = false;

            Debug.Log("[HeroDeckSetAsideUI] BuildPanel: complete");
        }

        private void RenderCards()
        {
            foreach (var slot in cardSlots) Destroy(slot);
            cardSlots.Clear();

            // Only show hero cards
            var heroCards = new List<CardDefinitionSO>();
            foreach (var card in fullDeck)
            {
                if (card.cardType == CardType.Hero) heroCards.Add(card);
            }

            float cardWidth = 100f;
            float spacing = 10f;
            float totalWidth = heroCards.Count * (cardWidth + spacing) - spacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < heroCards.Count; i++)
            {
                var card = heroCards[i];
                bool isSetAside = setAsideCards.Contains(card);

                var cardGO = new GameObject($"SetAsideCard_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardGO.transform.SetParent(panel.transform, false);
                var cardRect = cardGO.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing) + cardWidth / 2, 20);
                cardRect.sizeDelta = new Vector2(cardWidth, 140);

                Color bgColor = isSetAside ? new Color(0.5f, 0.2f, 0.2f) : card.placeholderColor;
                cardGO.GetComponent<Image>().color = bgColor;

                var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGO.transform.SetParent(cardGO.transform, false);
                var textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(4, 4);
                textRect.offsetMax = new Vector2(-4, -4);
                var tmp = textGO.GetComponent<TextMeshProUGUI>();
                string status = isSetAside ? "\n<color=red>[SET ASIDE]</color>" : "";
                tmp.text = $"<b>{card.cardName}</b>\nC:{card.combat} M:{card.movement} K:{card.carryCapacity}{status}";
                tmp.fontSize = 10;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.color = Color.white;

                CardDefinitionSO captured = card;
                cardGO.GetComponent<Button>().onClick.AddListener(() => ToggleSetAside(captured));
                cardSlots.Add(cardGO);
            }
        }

        private void ToggleSetAside(CardDefinitionSO card)
        {
            if (setAsideCards.Contains(card))
            {
                setAsideCards.Remove(card);
                Debug.Log($"[HeroDeckSetAsideUI] ToggleSetAside: restored '{card.cardName}' (setAside={setAsideCards.Count}/{cardsToSetAside})");
            }
            else
            {
                if (setAsideCards.Count >= cardsToSetAside)
                {
                    Debug.Log($"[HeroDeckSetAsideUI] ToggleSetAside: already selected enough cards ({setAsideCards.Count}/{cardsToSetAside})");
                    return;
                }
                setAsideCards.Add(card);
                Debug.Log($"[HeroDeckSetAsideUI] ToggleSetAside: set aside '{card.cardName}' (setAside={setAsideCards.Count}/{cardsToSetAside})");
            }

            RenderCards();
            UpdateCounter();
        }

        private void UpdateCounter()
        {
            int remaining = cardsToSetAside - setAsideCards.Count;
            counterText.text = remaining > 0
                ? $"Select {remaining} more hero(es) to set aside"
                : "Ready to confirm!";
            confirmButton.interactable = remaining == 0;
        }

        private void OnConfirm()
        {
            Debug.Log($"[HeroDeckSetAsideUI] OnConfirm: setting aside {setAsideCards.Count} heroes");
            var trimmedDeck = new List<CardDefinitionSO>();
            foreach (var card in fullDeck)
            {
                if (!setAsideCards.Contains(card))
                    trimmedDeck.Add(card);
            }
            Debug.Log($"[HeroDeckSetAsideUI] OnConfirm: trimmed deck from {fullDeck.Count} to {trimmedDeck.Count}");

            if (panel != null) Destroy(panel);
            onComplete?.Invoke(trimmedDeck);
        }
    }
}
