using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Data;
using Scurry.Core;
using Scurry.Interfaces;

namespace Scurry.UI
{
    public class ShopManager : MonoBehaviour
    {
        private IColonyManager colonyManager;
        private IBalanceConfig balanceConfig;
        private IRelicManager relicManager;

        private GameObject shopPanel;
        private TextMeshProUGUI currencyText;
        private readonly List<GameObject> cardSlots = new List<GameObject>();
        private List<CardDefinitionSO> shopCards = new List<CardDefinitionSO>();
        private List<CardDefinitionSO> sourcePool;
        private bool hasRerolled;

        private void Awake()
        {
            BuildShopPanel();
        }

        private void Start()
        {
            colonyManager = ServiceLocator.Get<IColonyManager>();
            balanceConfig = ServiceLocator.Get<IBalanceConfig>();
            relicManager = ServiceLocator.Get<IRelicManager>();
            Debug.Log($"[ShopManager] Start: colonyManager={(colonyManager != null ? "OK" : "NULL")}, balanceConfig={(balanceConfig != null ? "OK" : "NULL")}, relicManager={(relicManager != null ? "OK" : "NULL")}");
        }

        public void OpenShop(List<CardDefinitionSO> cardPool)
        {
            Debug.Log($"[ShopManager] OpenShop: pool size={cardPool?.Count ?? 0}");
            sourcePool = cardPool;
            hasRerolled = false;
            GenerateShopOfferings();
            shopPanel.SetActive(true);
            UpdateCurrencyDisplay();
        }

        private void GenerateShopOfferings()
        {
            shopCards.Clear();
            if (sourcePool == null || sourcePool.Count == 0)
            {
                Debug.LogWarning("[ShopManager] GenerateShopOfferings: empty card pool");
                return;
            }

            var bc = balanceConfig;
            int shopSize = bc != null ? bc.ShopCardCount : 5;
            int count = Mathf.Min(shopSize, sourcePool.Count);
            var shuffled = new List<CardDefinitionSO>(sourcePool);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = SeededRandom.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            for (int i = 0; i < count; i++)
                shopCards.Add(shuffled[i]);

            Debug.Log($"[ShopManager] GenerateShopOfferings: generated {shopCards.Count} offerings");
            RenderShopCards();
        }

        private int GetCardPrice(CardRarity rarity)
        {
            var bc = balanceConfig;
            int price = bc != null ? bc.GetShopPrice(rarity) : rarity switch
            {
                CardRarity.Common => 2,
                CardRarity.Uncommon => 4,
                CardRarity.Rare => 7,
                CardRarity.Legendary => 12,
                _ => 3
            };

            // Apply shop discount relic
            if (relicManager != null)
            {
                int discount = relicManager.GetShopDiscount();
                if (discount > 0)
                {
                    price = Mathf.Max(1, price - discount);
                    Debug.Log($"[ShopManager] GetCardPrice: relic discount applied (-{discount}), final price={price}");
                }
            }
            return price;
        }

        private void BuildShopPanel()
        {
            shopPanel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
            shopPanel.transform.SetParent(transform, false);
            var panelRect = shopPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            shopPanel.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.05f, 0.95f);

            // Title
            var titleGO = new GameObject("ShopTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(shopPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 40);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Shop";
            titleTmp.fontSize = 28;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1f, 0.85f, 0.3f);

            // Currency display
            var currGO = new GameObject("CurrencyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            currGO.transform.SetParent(shopPanel.transform, false);
            var currRect = currGO.GetComponent<RectTransform>();
            currRect.anchorMin = new Vector2(0, 1);
            currRect.anchorMax = new Vector2(1, 1);
            currRect.pivot = new Vector2(0.5f, 1);
            currRect.anchoredPosition = new Vector2(0, -50);
            currRect.sizeDelta = new Vector2(0, 30);
            currencyText = currGO.GetComponent<TextMeshProUGUI>();
            currencyText.fontSize = 18;
            currencyText.alignment = TextAlignmentOptions.Center;
            currencyText.color = new Color(1f, 0.9f, 0.4f);

            // Reroll button
            var rerollGO = new GameObject("RerollBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            rerollGO.transform.SetParent(shopPanel.transform, false);
            var rerollRect = rerollGO.GetComponent<RectTransform>();
            rerollRect.anchorMin = new Vector2(0, 0);
            rerollRect.anchorMax = new Vector2(0, 0);
            rerollRect.pivot = new Vector2(0, 0);
            rerollRect.anchoredPosition = new Vector2(20, 20);
            rerollRect.sizeDelta = new Vector2(150, 45);
            rerollGO.GetComponent<Image>().color = new Color(0.5f, 0.4f, 0.2f);

            var rerollTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            rerollTextGO.transform.SetParent(rerollGO.transform, false);
            var rtRect = rerollTextGO.GetComponent<RectTransform>();
            rtRect.anchorMin = Vector2.zero;
            rtRect.anchorMax = Vector2.one;
            rtRect.sizeDelta = Vector2.zero;
            var rtTmp = rerollTextGO.GetComponent<TextMeshProUGUI>();
            var bcRef = balanceConfig;
            rtTmp.text = $"Reroll ({(bcRef != null ? bcRef.ShopRerollCost : 2)} Currency)";
            rtTmp.fontSize = 14;
            rtTmp.alignment = TextAlignmentOptions.Center;
            rtTmp.color = Color.white;
            rerollGO.GetComponent<Button>().onClick.AddListener(OnRerollClicked);

            // Leave button
            var leaveGO = new GameObject("LeaveBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            leaveGO.transform.SetParent(shopPanel.transform, false);
            var leaveRect = leaveGO.GetComponent<RectTransform>();
            leaveRect.anchorMin = new Vector2(1, 0);
            leaveRect.anchorMax = new Vector2(1, 0);
            leaveRect.pivot = new Vector2(1, 0);
            leaveRect.anchoredPosition = new Vector2(-20, 20);
            leaveRect.sizeDelta = new Vector2(150, 45);
            leaveGO.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.5f);

            var leaveTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            leaveTextGO.transform.SetParent(leaveGO.transform, false);
            var ltRect = leaveTextGO.GetComponent<RectTransform>();
            ltRect.anchorMin = Vector2.zero;
            ltRect.anchorMax = Vector2.one;
            ltRect.sizeDelta = Vector2.zero;
            var ltTmp = leaveTextGO.GetComponent<TextMeshProUGUI>();
            ltTmp.text = "Leave Shop";
            ltTmp.fontSize = 16;
            ltTmp.alignment = TextAlignmentOptions.Center;
            ltTmp.color = Color.white;
            leaveGO.GetComponent<Button>().onClick.AddListener(OnLeaveClicked);

            shopPanel.SetActive(false);
            Debug.Log("[ShopManager] BuildShopPanel: complete (hidden)");
        }

        private void RenderShopCards()
        {
            foreach (var slot in cardSlots) Destroy(slot);
            cardSlots.Clear();

            float cardWidth = 120f;
            float spacing = 15f;
            float totalWidth = shopCards.Count * (cardWidth + spacing) - spacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < shopCards.Count; i++)
            {
                var card = shopCards[i];
                int price = GetCardPrice(card.rarity);

                var cardGO = new GameObject($"ShopCard_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardGO.transform.SetParent(shopPanel.transform, false);
                var cardRect = cardGO.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing) + cardWidth / 2, 30);
                cardRect.sizeDelta = new Vector2(cardWidth, 160);
                cardGO.GetComponent<Image>().color = card.placeholderColor;

                // Card info text
                var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGO.transform.SetParent(cardGO.transform, false);
                var textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(5, 30);
                textRect.offsetMax = new Vector2(-5, -5);
                var tmp = textGO.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b>{card.cardName}</b>\n{card.rarity}\n{card.cardType}";
                if (card.cardType == CardType.Hero)
                    tmp.text += $"\nC:{card.combat} M:{card.movement} K:{card.carryCapacity}";
                else if (card.cardType == CardType.Equipment)
                    tmp.text += $"\n{card.equipmentSlot} +{card.equipmentBonusValue}";
                tmp.fontSize = 11;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.color = Color.white;

                // Price label
                var priceGO = new GameObject("Price", typeof(RectTransform), typeof(TextMeshProUGUI));
                priceGO.transform.SetParent(cardGO.transform, false);
                var priceRect = priceGO.GetComponent<RectTransform>();
                priceRect.anchorMin = new Vector2(0, 0);
                priceRect.anchorMax = new Vector2(1, 0);
                priceRect.pivot = new Vector2(0.5f, 0);
                priceRect.anchoredPosition = new Vector2(0, 5);
                priceRect.sizeDelta = new Vector2(0, 25);
                var priceTmp = priceGO.GetComponent<TextMeshProUGUI>();
                bool canAfford = colonyManager != null && colonyManager.CurrencyStockpile >= price;
                priceTmp.text = $"{price} Currency";
                priceTmp.fontSize = 14;
                priceTmp.fontStyle = FontStyles.Bold;
                priceTmp.alignment = TextAlignmentOptions.Center;
                priceTmp.color = canAfford ? new Color(1f, 0.9f, 0.3f) : new Color(0.6f, 0.3f, 0.3f);

                var btn = cardGO.GetComponent<Button>();
                btn.interactable = canAfford;
                int capturedIndex = i;
                btn.onClick.AddListener(() => OnBuyCard(capturedIndex));

                cardSlots.Add(cardGO);
            }
        }

        private void OnBuyCard(int index)
        {
            if (index < 0 || index >= shopCards.Count) return;
            var card = shopCards[index];
            int price = GetCardPrice(card.rarity);

            Debug.Log($"[ShopManager] OnBuyCard: card='{card.cardName}', price={price}, available={colonyManager.CurrencyStockpile}");

            if (colonyManager.CurrencyStockpile < price)
            {
                Debug.Log("[ShopManager] OnBuyCard: insufficient currency");
                return;
            }

            colonyManager.SpendCurrency(price);
            shopCards.RemoveAt(index);

            Debug.Log($"[ShopManager] OnBuyCard: purchased '{card.cardName}', remaining currency={colonyManager.CurrencyStockpile}");
            EventBus.OnCardPurchased?.Invoke(card);

            RenderShopCards();
            UpdateCurrencyDisplay();
        }

        private void OnRerollClicked()
        {
            if (hasRerolled)
            {
                Debug.Log("[ShopManager] OnRerollClicked: already rerolled this visit");
                return;
            }
            var bc = balanceConfig;
            int rerollCost = bc != null ? bc.ShopRerollCost : 2;
            if (colonyManager.CurrencyStockpile < rerollCost)
            {
                Debug.Log($"[ShopManager] OnRerollClicked: insufficient currency for reroll (need={rerollCost}, have={colonyManager.CurrencyStockpile})");
                return;
            }

            Debug.Log($"[ShopManager] OnRerollClicked: rerolling shop offerings (cost={rerollCost})");
            colonyManager.SpendCurrency(rerollCost);
            hasRerolled = true;
            GenerateShopOfferings();
            UpdateCurrencyDisplay();
        }

        private void OnLeaveClicked()
        {
            Debug.Log("[ShopManager] OnLeaveClicked: leaving shop");
            shopPanel.SetActive(false);
            EventBus.OnShopComplete?.Invoke();
        }

        private void UpdateCurrencyDisplay()
        {
            if (currencyText != null && colonyManager != null)
                currencyText.text = $"Currency: {colonyManager.CurrencyStockpile}";
        }
    }
}
