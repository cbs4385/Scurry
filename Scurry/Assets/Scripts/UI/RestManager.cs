using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Data;
using Scurry.Core;
using Scurry.Colony;

namespace Scurry.UI
{
    public class RestManager : MonoBehaviour
    {
        [SerializeField] private ColonyManager colonyManager;

        private GameObject restPanel;
        private Button healButton;
        private Button upgradeButton;
        private Button leaveButton;
        private TextMeshProUGUI statusText;

        private List<CardDefinitionSO> heroCards;
        private List<ColonyCardDefinitionSO> colonyCards;
        private UpgradeManager upgradeManager;

        private void Awake()
        {
            if (colonyManager == null) colonyManager = FindObjectOfType<ColonyManager>();
            BuildRestPanel();
            upgradeManager = GetComponent<UpgradeManager>();
            if (upgradeManager == null) upgradeManager = FindObjectOfType<UpgradeManager>();
        }

        public void OpenRestSite(List<CardDefinitionSO> heroes, List<ColonyCardDefinitionSO> colony)
        {
            heroCards = heroes;
            colonyCards = colony;
            Debug.Log($"[RestManager] OpenRestSite: colonyHP={colonyManager?.CurrentHP ?? 0}/{colonyManager?.MaxHP ?? 0}");
            restPanel.SetActive(true);
            UpdateStatus();
        }

        private void BuildRestPanel()
        {
            restPanel = new GameObject("RestPanel", typeof(RectTransform), typeof(Image));
            restPanel.transform.SetParent(transform, false);
            var panelRect = restPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400, 300);
            restPanel.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.95f);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(restPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 35);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Rest Site";
            titleTmp.fontSize = 24;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.3f, 0.6f, 0.9f);

            // Status
            var statusGO = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusGO.transform.SetParent(restPanel.transform, false);
            var statusRect = statusGO.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 1);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.pivot = new Vector2(0.5f, 1);
            statusRect.anchoredPosition = new Vector2(0, -50);
            statusRect.sizeDelta = new Vector2(-20, 25);
            statusText = statusGO.GetComponent<TextMeshProUGUI>();
            statusText.fontSize = 14;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.white;

            var bc = BalanceConfigSO.Instance;
            int pct = bc != null ? bc.restHealPercent : 30;
            healButton = CreateButton(restPanel.transform, "HealBtn", new Vector2(0, -100),
                $"Rest & Heal ({pct}% Colony HP)", new Color(0.2f, 0.6f, 0.3f), OnHealClicked);

            // Upgrade button: Free upgrade
            upgradeButton = CreateButton(restPanel.transform, "UpgradeBtn", new Vector2(0, -160),
                "Upgrade a Card (Free)", new Color(0.3f, 0.3f, 0.6f), OnUpgradeClicked);

            // Leave button
            leaveButton = CreateButton(restPanel.transform, "LeaveBtn", new Vector2(0, -230),
                "Leave", new Color(0.4f, 0.4f, 0.5f), OnLeaveClicked);

            restPanel.SetActive(false);
            Debug.Log("[RestManager] BuildRestPanel: complete (hidden)");
        }

        private Button CreateButton(Transform parent, string name, Vector2 pos, string text, Color color, UnityEngine.Events.UnityAction action)
        {
            var btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 1);
            btnRect.anchorMax = new Vector2(0.5f, 1);
            btnRect.pivot = new Vector2(0.5f, 1);
            btnRect.anchoredPosition = pos;
            btnRect.sizeDelta = new Vector2(350, 45);
            btnGO.GetComponent<Image>().color = color;

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

        private void OnHealClicked()
        {
            if (colonyManager == null) return;

            var bc = BalanceConfigSO.Instance;
            int pct = bc != null ? bc.restHealPercent : 30;
            int healAmount = Mathf.CeilToInt(colonyManager.MaxHP * pct / 100f);
            Debug.Log($"[RestManager] OnHealClicked: healing {healAmount} HP ({pct}% of {colonyManager.MaxHP}), before={colonyManager.CurrentHP}");
            colonyManager.Heal(healAmount);
            Debug.Log($"[RestManager] OnHealClicked: after={colonyManager.CurrentHP}");

            restPanel.SetActive(false);
            EventBus.OnRestComplete?.Invoke();
        }

        private void OnUpgradeClicked()
        {
            Debug.Log("[RestManager] OnUpgradeClicked: opening free upgrade");
            restPanel.SetActive(false);

            if (upgradeManager != null)
            {
                // Subscribe to upgrade complete to fire rest complete
                EventBus.OnUpgradeComplete += OnFreeUpgradeDone;
                upgradeManager.OpenUpgrade(heroCards, colonyCards, free: true);
            }
            else
            {
                Debug.LogWarning("[RestManager] OnUpgradeClicked: no UpgradeManager found — completing rest");
                EventBus.OnRestComplete?.Invoke();
            }
        }

        private void OnFreeUpgradeDone()
        {
            Debug.Log("[RestManager] OnFreeUpgradeDone: free upgrade complete, firing rest complete");
            EventBus.OnUpgradeComplete -= OnFreeUpgradeDone;
            EventBus.OnRestComplete?.Invoke();
        }

        private void OnLeaveClicked()
        {
            Debug.Log("[RestManager] OnLeaveClicked: leaving rest site");
            restPanel.SetActive(false);
            EventBus.OnRestComplete?.Invoke();
        }

        private void UpdateStatus()
        {
            if (statusText != null && colonyManager != null)
            {
                var bc = BalanceConfigSO.Instance;
                int pctVal = bc != null ? bc.restHealPercent : 30;
                int healAmount = Mathf.CeilToInt(colonyManager.MaxHP * pctVal / 100f);
                statusText.text = $"Colony HP: {colonyManager.CurrentHP}/{colonyManager.MaxHP} (heal would restore {healAmount})";
            }
        }
    }
}
