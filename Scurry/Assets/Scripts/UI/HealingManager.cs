using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Data;
using Scurry.Core;
using Scurry.Colony;

namespace Scurry.UI
{
    public class HealingManager : MonoBehaviour
    {
        [SerializeField] private ColonyManager colonyManager;

        private GameObject healingPanel;
        private Button minorHealBtn;
        private Button majorHealBtn;
        private Button resupplyBtn;
        private Button leaveBtn;
        private TextMeshProUGUI statusText;

        // Wounded heroes passed in from RunManager
        private HashSet<CardDefinitionSO> woundedHeroes;

        private void Awake()
        {
            if (colonyManager == null) colonyManager = FindObjectOfType<ColonyManager>();
            BuildHealingPanel();
        }

        public void OpenHealing(HashSet<CardDefinitionSO> wounded)
        {
            woundedHeroes = wounded;
            Debug.Log($"[HealingManager] OpenHealing: woundedCount={wounded?.Count ?? 0}, food={colonyManager.FoodStockpile}, colonyHP={colonyManager.CurrentHP}/{colonyManager.MaxHP}");
            healingPanel.SetActive(true);
            UpdateButtons();
        }

        private void BuildHealingPanel()
        {
            healingPanel = new GameObject("HealingPanel", typeof(RectTransform), typeof(Image));
            healingPanel.transform.SetParent(transform, false);
            var panelRect = healingPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400, 350);
            healingPanel.GetComponent<Image>().color = new Color(0.05f, 0.12f, 0.05f, 0.95f);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(healingPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 40);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Healing Shrine";
            titleTmp.fontSize = 24;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.3f, 0.9f, 0.3f);

            // Status
            var statusGO = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusGO.transform.SetParent(healingPanel.transform, false);
            var statusRect = statusGO.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 1);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.pivot = new Vector2(0.5f, 1);
            statusRect.anchoredPosition = new Vector2(0, -50);
            statusRect.sizeDelta = new Vector2(-20, 30);
            statusText = statusGO.GetComponent<TextMeshProUGUI>();
            statusText.fontSize = 14;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.white;

            var bc = BalanceConfigSO.Instance;
            int minC = bc != null ? bc.minorHealCost : 2;
            int minA = bc != null ? bc.minorHealAmount : 5;
            int majC = bc != null ? bc.majorHealCost : 5;
            int majA = bc != null ? bc.majorHealAmount : 15;
            int resC = bc != null ? bc.resupplyCost : 3;

            minorHealBtn = CreateOptionButton(healingPanel.transform, "MinorHeal", new Vector2(0, -100),
                $"Minor Heal ({minC} Food -> +{minA} HP)", OnMinorHeal);

            majorHealBtn = CreateOptionButton(healingPanel.transform, "MajorHeal", new Vector2(0, -160),
                $"Major Heal ({majC} Food -> +{majA} HP)", OnMajorHeal);

            resupplyBtn = CreateOptionButton(healingPanel.transform, "Resupply", new Vector2(0, -220),
                $"Resupply ({resC} Food -> Heal Wounded Hero)", OnResupply);

            // Leave
            leaveBtn = CreateOptionButton(healingPanel.transform, "Leave", new Vector2(0, -290),
                "Leave Shrine", OnLeaveClicked);
            leaveBtn.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.5f);

            healingPanel.SetActive(false);
            Debug.Log("[HealingManager] BuildHealingPanel: complete (hidden)");
        }

        private Button CreateOptionButton(Transform parent, string name, Vector2 pos, string text, UnityEngine.Events.UnityAction action)
        {
            var btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 1);
            btnRect.anchorMax = new Vector2(0.5f, 1);
            btnRect.pivot = new Vector2(0.5f, 1);
            btnRect.anchoredPosition = pos;
            btnRect.sizeDelta = new Vector2(350, 45);
            btnGO.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.2f);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(btnGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            var btn = btnGO.GetComponent<Button>();
            btn.onClick.AddListener(action);
            return btn;
        }

        private void OnMinorHeal()
        {
            var bc = BalanceConfigSO.Instance;
            int cost = bc != null ? bc.minorHealCost : 2;
            int amount = bc != null ? bc.minorHealAmount : 5;
            if (colonyManager.FoodStockpile < cost)
            {
                Debug.Log($"[HealingManager] OnMinorHeal: insufficient food (need={cost}, have={colonyManager.FoodStockpile})");
                return;
            }
            Debug.Log($"[HealingManager] OnMinorHeal: spending {cost} food, healing {amount} HP (before: HP={colonyManager.CurrentHP})");
            colonyManager.SpendFood(cost);
            colonyManager.Heal(amount);
            Debug.Log($"[HealingManager] OnMinorHeal: after: HP={colonyManager.CurrentHP}, food={colonyManager.FoodStockpile}");
            UpdateButtons();
        }

        private void OnMajorHeal()
        {
            var bc = BalanceConfigSO.Instance;
            int cost = bc != null ? bc.majorHealCost : 5;
            int amount = bc != null ? bc.majorHealAmount : 15;
            if (colonyManager.FoodStockpile < cost)
            {
                Debug.Log($"[HealingManager] OnMajorHeal: insufficient food (need={cost}, have={colonyManager.FoodStockpile})");
                return;
            }
            Debug.Log($"[HealingManager] OnMajorHeal: spending {cost} food, healing {amount} HP (before: HP={colonyManager.CurrentHP})");
            colonyManager.SpendFood(cost);
            colonyManager.Heal(amount);
            Debug.Log($"[HealingManager] OnMajorHeal: after: HP={colonyManager.CurrentHP}, food={colonyManager.FoodStockpile}");
            UpdateButtons();
        }

        private void OnResupply()
        {
            var bc = BalanceConfigSO.Instance;
            int cost = bc != null ? bc.resupplyCost : 3;
            if (colonyManager.FoodStockpile < cost)
            {
                Debug.Log($"[HealingManager] OnResupply: insufficient food (need={cost}, have={colonyManager.FoodStockpile})");
                return;
            }
            if (woundedHeroes == null || woundedHeroes.Count == 0)
            {
                Debug.Log("[HealingManager] OnResupply: no wounded heroes");
                return;
            }

            colonyManager.SpendFood(cost);

            // Heal first wounded hero found
            CardDefinitionSO healed = null;
            foreach (var hero in woundedHeroes)
            {
                healed = hero;
                break;
            }
            if (healed != null)
            {
                woundedHeroes.Remove(healed);
                Debug.Log($"[HealingManager] OnResupply: healed '{healed.cardName}', remaining wounded={woundedHeroes.Count}");
            }
            UpdateButtons();
        }

        private void OnLeaveClicked()
        {
            Debug.Log("[HealingManager] OnLeaveClicked: leaving healing shrine");
            healingPanel.SetActive(false);
            EventBus.OnHealingComplete?.Invoke();
        }

        private void UpdateButtons()
        {
            int food = colonyManager != null ? colonyManager.FoodStockpile : 0;
            int hp = colonyManager != null ? colonyManager.CurrentHP : 0;
            int maxHp = colonyManager != null ? colonyManager.MaxHP : 1;
            int woundCount = woundedHeroes?.Count ?? 0;

            statusText.text = $"Colony HP: {hp}/{maxHp}  |  Food: {food}  |  Wounded Heroes: {woundCount}";

            var bc = BalanceConfigSO.Instance;
            minorHealBtn.interactable = food >= (bc != null ? bc.minorHealCost : 2) && hp < maxHp;
            majorHealBtn.interactable = food >= (bc != null ? bc.majorHealCost : 5) && hp < maxHp;
            resupplyBtn.interactable = food >= (bc != null ? bc.resupplyCost : 3) && woundCount > 0;
        }
    }
}
