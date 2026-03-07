using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Data;
using Scurry.Core;
using Scurry.Interfaces;

namespace Scurry.UI
{
    public class UpgradeManager : MonoBehaviour
    {
        private IColonyManager colonyManager;
        private IBalanceConfig balanceConfig;

        private GameObject upgradePanel;
        private TextMeshProUGUI materialsText;
        private readonly List<GameObject> cardSlots = new List<GameObject>();

        private List<CardDefinitionSO> heroCards;
        private List<ColonyCardDefinitionSO> colonyCards;
        private bool isFreeUpgrade;

        private void Awake()
        {
            BuildUpgradePanel();
        }

        private void Start()
        {
            colonyManager = ServiceLocator.Get<IColonyManager>();
            balanceConfig = ServiceLocator.Get<IBalanceConfig>();
            Debug.Log($"[UpgradeManager] Start: colonyManager={(colonyManager != null ? "OK" : "NULL")}, balanceConfig={(balanceConfig != null ? "OK" : "NULL")}");
        }

        public void OpenUpgrade(List<CardDefinitionSO> heroes, List<ColonyCardDefinitionSO> colony, bool free = false)
        {
            heroCards = heroes;
            colonyCards = colony;
            isFreeUpgrade = free;
            Debug.Log($"[UpgradeManager] OpenUpgrade: heroCount={heroes?.Count ?? 0}, colonyCount={colony?.Count ?? 0}, free={free}, materials={colonyManager?.MaterialsStockpile ?? 0}");
            upgradePanel.SetActive(true);
            RenderCards();
            UpdateMaterialsDisplay();
        }

        private int GetUpgradeCost(CardRarity rarity)
        {
            if (isFreeUpgrade) return 0;
            var bc = balanceConfig;
            if (bc != null) return bc.GetUpgradeCost(rarity);
            return rarity switch
            {
                CardRarity.Common => 2,
                CardRarity.Uncommon => 4,
                CardRarity.Rare => 7,
                _ => 10
            };
        }

        private void BuildUpgradePanel()
        {
            upgradePanel = new GameObject("UpgradePanel", typeof(RectTransform), typeof(Image));
            upgradePanel.transform.SetParent(transform, false);
            var panelRect = upgradePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            upgradePanel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.12f, 0.95f);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(upgradePanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 40);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Upgrade Shrine";
            titleTmp.fontSize = 24;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.4f, 0.4f, 0.9f);

            // Materials display
            var matGO = new GameObject("MaterialsText", typeof(RectTransform), typeof(TextMeshProUGUI));
            matGO.transform.SetParent(upgradePanel.transform, false);
            var matRect = matGO.GetComponent<RectTransform>();
            matRect.anchorMin = new Vector2(0, 1);
            matRect.anchorMax = new Vector2(1, 1);
            matRect.pivot = new Vector2(0.5f, 1);
            matRect.anchoredPosition = new Vector2(0, -50);
            matRect.sizeDelta = new Vector2(0, 30);
            materialsText = matGO.GetComponent<TextMeshProUGUI>();
            materialsText.fontSize = 18;
            materialsText.alignment = TextAlignmentOptions.Center;
            materialsText.color = new Color(0.6f, 0.7f, 0.9f);

            // Leave button
            var leaveGO = new GameObject("LeaveBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            leaveGO.transform.SetParent(upgradePanel.transform, false);
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
            ltTmp.text = "Leave Shrine";
            ltTmp.fontSize = 16;
            ltTmp.alignment = TextAlignmentOptions.Center;
            ltTmp.color = Color.white;
            leaveGO.GetComponent<Button>().onClick.AddListener(OnLeaveClicked);

            upgradePanel.SetActive(false);
            Debug.Log("[UpgradeManager] BuildUpgradePanel: complete (hidden)");
        }

        private void RenderCards()
        {
            foreach (var slot in cardSlots) Destroy(slot);
            cardSlots.Clear();

            float cardWidth = 100f;
            float spacing = 10f;
            int totalCards = (heroCards?.Count ?? 0) + (colonyCards?.Count ?? 0);
            float totalWidth = totalCards * (cardWidth + spacing) - spacing;
            float startX = -totalWidth / 2f;
            int idx = 0;

            // Hero cards
            if (heroCards != null)
            {
                foreach (var card in heroCards)
                {
                    if (card.upgraded) { idx++; continue; }
                    int cost = GetUpgradeCost(card.rarity);
                    bool canAfford = isFreeUpgrade || (colonyManager != null && colonyManager.MaterialsStockpile >= cost);

                    var cardGO = CreateCardSlot(card.cardName, card.placeholderColor,
                        $"<b>{card.cardName}</b>\n{card.cardType}\nCost: {cost} Mat",
                        canAfford, startX + idx * (cardWidth + spacing), cardWidth);

                    CardDefinitionSO captured = card;
                    cardGO.GetComponent<Button>().onClick.AddListener(() => OnUpgradeHeroCard(captured));
                    cardSlots.Add(cardGO);
                    idx++;
                }
            }

            // Colony cards
            if (colonyCards != null)
            {
                foreach (var card in colonyCards)
                {
                    if (card.upgraded) { idx++; continue; }
                    int cost = GetUpgradeCost(card.rarity);
                    bool canAfford = isFreeUpgrade || (colonyManager != null && colonyManager.MaterialsStockpile >= cost);

                    var cardGO = CreateCardSlot(card.cardName, card.placeholderColor,
                        $"<b>{card.cardName}</b>\nColony\nCost: {cost} Mat",
                        canAfford, startX + idx * (cardWidth + spacing), cardWidth);

                    ColonyCardDefinitionSO captured = card;
                    cardGO.GetComponent<Button>().onClick.AddListener(() => OnUpgradeColonyCard(captured));
                    cardSlots.Add(cardGO);
                    idx++;
                }
            }
        }

        private GameObject CreateCardSlot(string name, Color color, string info, bool interactable, float x, float width)
        {
            var cardGO = new GameObject($"UpgradeCard_{name}", typeof(RectTransform), typeof(Image), typeof(Button));
            cardGO.transform.SetParent(upgradePanel.transform, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0, 0.5f);
            cardRect.anchoredPosition = new Vector2(x, 0);
            cardRect.sizeDelta = new Vector2(width, 130);
            cardGO.GetComponent<Image>().color = interactable ? color : color * 0.5f;
            cardGO.GetComponent<Button>().interactable = interactable;

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(cardGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4, 4);
            textRect.offsetMax = new Vector2(-4, -4);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = info;
            tmp.fontSize = 10;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.color = Color.white;

            return cardGO;
        }

        private void OnUpgradeHeroCard(CardDefinitionSO card)
        {
            int cost = GetUpgradeCost(card.rarity);
            if (!isFreeUpgrade && colonyManager.MaterialsStockpile < cost)
            {
                Debug.Log($"[UpgradeManager] OnUpgradeHeroCard: can't afford '{card.cardName}' (cost={cost}, have={colonyManager.MaterialsStockpile})");
                return;
            }

            if (!isFreeUpgrade)
                colonyManager.SpendMaterials(cost);

            card.Upgrade();
            Debug.Log($"[UpgradeManager] OnUpgradeHeroCard: upgraded '{card.cardName}', remaining materials={colonyManager.MaterialsStockpile}");

            // Close after one upgrade
            upgradePanel.SetActive(false);
            EventBus.OnUpgradeComplete?.Invoke();
        }

        private void OnUpgradeColonyCard(ColonyCardDefinitionSO card)
        {
            int cost = GetUpgradeCost(card.rarity);
            if (!isFreeUpgrade && colonyManager.MaterialsStockpile < cost)
            {
                Debug.Log($"[UpgradeManager] OnUpgradeColonyCard: can't afford '{card.cardName}' (cost={cost}, have={colonyManager.MaterialsStockpile})");
                return;
            }

            if (!isFreeUpgrade)
                colonyManager.SpendMaterials(cost);

            card.Upgrade();
            Debug.Log($"[UpgradeManager] OnUpgradeColonyCard: upgraded '{card.cardName}', remaining materials={colonyManager.MaterialsStockpile}");

            upgradePanel.SetActive(false);
            EventBus.OnUpgradeComplete?.Invoke();
        }

        private void OnLeaveClicked()
        {
            Debug.Log("[UpgradeManager] OnLeaveClicked: leaving upgrade shrine");
            upgradePanel.SetActive(false);
            EventBus.OnUpgradeComplete?.Invoke();
        }

        private void UpdateMaterialsDisplay()
        {
            if (materialsText != null && colonyManager != null)
            {
                string label = isFreeUpgrade ? "FREE UPGRADE" : $"Materials: {colonyManager.MaterialsStockpile}";
                materialsText.text = label;
            }
        }
    }
}
