using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Data;
using Scurry.Core;

namespace Scurry.UI
{
    public class RewardSelectionUI : MonoBehaviour
    {
        private GameObject rewardPanel;
        private readonly List<GameObject> rewardSlots = new List<GameObject>();
        private List<CardDefinitionSO> rewards;
        private RelicDefinitionSO rewardRelic;
        private System.Action<CardDefinitionSO> onCardSelected;

        private void Awake()
        {
            BuildRewardPanel();
        }

        public void ShowRewards(List<CardDefinitionSO> rewardCards, System.Action<CardDefinitionSO> callback, RelicDefinitionSO relic = null)
        {
            rewards = rewardCards;
            rewardRelic = relic;
            onCardSelected = callback;
            Debug.Log($"[RewardSelectionUI] ShowRewards: showing {rewardCards?.Count ?? 0} reward cards, relic={relic?.relicName ?? "none"}");
            RenderRewards();
            rewardPanel.SetActive(true);
        }

        private void BuildRewardPanel()
        {
            rewardPanel = new GameObject("RewardPanel", typeof(RectTransform), typeof(Image));
            rewardPanel.transform.SetParent(transform, false);
            var panelRect = rewardPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(500, 300);
            rewardPanel.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.15f, 0.95f);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(rewardPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 40);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Choose Your Reward!";
            titleTmp.fontSize = 24;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1f, 0.85f, 0.3f);

            // Skip button
            var skipGO = new GameObject("SkipBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            skipGO.transform.SetParent(rewardPanel.transform, false);
            var skipRect = skipGO.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(0.5f, 0);
            skipRect.anchorMax = new Vector2(0.5f, 0);
            skipRect.pivot = new Vector2(0.5f, 0);
            skipRect.anchoredPosition = new Vector2(0, 10);
            skipRect.sizeDelta = new Vector2(120, 35);
            skipGO.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.5f);

            var skipTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            skipTextGO.transform.SetParent(skipGO.transform, false);
            var stRect = skipTextGO.GetComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.sizeDelta = Vector2.zero;
            var stTmp = skipTextGO.GetComponent<TextMeshProUGUI>();
            stTmp.text = "Skip";
            stTmp.fontSize = 14;
            stTmp.alignment = TextAlignmentOptions.Center;
            stTmp.color = Color.white;
            skipGO.GetComponent<Button>().onClick.AddListener(OnSkipClicked);

            rewardPanel.SetActive(false);
            Debug.Log("[RewardSelectionUI] BuildRewardPanel: complete (hidden)");
        }

        private void RenderRewards()
        {
            foreach (var slot in rewardSlots) Destroy(slot);
            rewardSlots.Clear();

            if ((rewards == null || rewards.Count == 0) && rewardRelic == null) return;

            int totalItems = (rewards?.Count ?? 0) + (rewardRelic != null ? 1 : 0);
            float cardWidth = 130f;
            float spacing = 20f;
            float totalWidth = totalItems * (cardWidth + spacing) - spacing;
            float startX = -totalWidth / 2f;

            // Relic reward slot (displayed first if present)
            if (rewardRelic != null)
            {
                var relicGO = new GameObject("Reward_Relic", typeof(RectTransform), typeof(Image), typeof(Button));
                relicGO.transform.SetParent(rewardPanel.transform, false);
                var relicRect = relicGO.GetComponent<RectTransform>();
                relicRect.anchorMin = new Vector2(0.5f, 0.5f);
                relicRect.anchorMax = new Vector2(0.5f, 0.5f);
                relicRect.pivot = new Vector2(0.5f, 0.5f);
                relicRect.anchoredPosition = new Vector2(startX + cardWidth / 2, 10);
                relicRect.sizeDelta = new Vector2(cardWidth, 170);
                relicGO.GetComponent<Image>().color = rewardRelic.placeholderColor;

                var relicTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                relicTextGO.transform.SetParent(relicGO.transform, false);
                var rtRect = relicTextGO.GetComponent<RectTransform>();
                rtRect.anchorMin = Vector2.zero;
                rtRect.anchorMax = Vector2.one;
                rtRect.offsetMin = new Vector2(5, 5);
                rtRect.offsetMax = new Vector2(-5, -5);
                var rtTmp = relicTextGO.GetComponent<TextMeshProUGUI>();
                rtTmp.text = $"<b>RELIC</b>\n{rewardRelic.relicName}\n{rewardRelic.effect}\n+{rewardRelic.effectValue}";
                rtTmp.fontSize = 12;
                rtTmp.alignment = TextAlignmentOptions.TopLeft;
                rtTmp.color = new Color(1f, 0.9f, 0.5f);

                relicGO.GetComponent<Button>().onClick.AddListener(OnRelicSelected);
                rewardSlots.Add(relicGO);

                startX += cardWidth + spacing;
            }

            if (rewards == null || rewards.Count == 0) return;

            for (int i = 0; i < rewards.Count; i++)
            {
                var card = rewards[i];
                var cardGO = new GameObject($"Reward_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardGO.transform.SetParent(rewardPanel.transform, false);
                var cardRect = cardGO.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing) + cardWidth / 2, 10);
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
                cardGO.GetComponent<Button>().onClick.AddListener(() => OnRewardSelected(capturedIndex));
                rewardSlots.Add(cardGO);
            }
        }

        private void OnRewardSelected(int index)
        {
            if (rewards == null || index < 0 || index >= rewards.Count) return;
            var card = rewards[index];
            Debug.Log($"[RewardSelectionUI] OnRewardSelected: picked '{card.cardName}'");

            rewardPanel.SetActive(false);
            onCardSelected?.Invoke(card);
        }

        private void OnRelicSelected()
        {
            if (rewardRelic == null) return;
            Debug.Log($"[RewardSelectionUI] OnRelicSelected: picked relic '{rewardRelic.relicName}'");

            var relicMgr = Core.RelicManager.Instance;
            if (relicMgr != null)
                relicMgr.AddRelic(rewardRelic);

            rewardPanel.SetActive(false);
            onCardSelected?.Invoke(null);
        }

        private void OnSkipClicked()
        {
            Debug.Log("[RewardSelectionUI] OnSkipClicked: skipping reward");
            rewardPanel.SetActive(false);
            onCardSelected?.Invoke(null);
        }
    }
}
