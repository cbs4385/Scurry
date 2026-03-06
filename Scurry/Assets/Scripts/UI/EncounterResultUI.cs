using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Data;
using Scurry.Core;
using Scurry.Encounter;

namespace Scurry.UI
{
    public class EncounterResultUI : MonoBehaviour
    {
        private GameObject resultPanel;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI detailsText;
        private Button continueButton;

        private void OnEnable()
        {
            EventBus.OnEncounterComplete += ShowResult;
        }

        private void OnDisable()
        {
            EventBus.OnEncounterComplete -= ShowResult;
        }

        private void Awake()
        {
            BuildResultPanel();
        }

        private void BuildResultPanel()
        {
            resultPanel = new GameObject("EncounterResultPanel", typeof(RectTransform), typeof(Image));
            resultPanel.transform.SetParent(transform, false);
            var panelRect = resultPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400, 300);
            resultPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(resultPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -15);
            titleRect.sizeDelta = new Vector2(0, 40);
            titleText = titleGO.GetComponent<TextMeshProUGUI>();
            titleText.fontSize = 26;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;

            // Details
            var detailsGO = new GameObject("Details", typeof(RectTransform), typeof(TextMeshProUGUI));
            detailsGO.transform.SetParent(resultPanel.transform, false);
            var detailsRect = detailsGO.GetComponent<RectTransform>();
            detailsRect.anchorMin = new Vector2(0, 0.2f);
            detailsRect.anchorMax = new Vector2(1, 0.85f);
            detailsRect.offsetMin = new Vector2(20, 0);
            detailsRect.offsetMax = new Vector2(-20, 0);
            detailsText = detailsGO.GetComponent<TextMeshProUGUI>();
            detailsText.fontSize = 16;
            detailsText.alignment = TextAlignmentOptions.TopLeft;
            detailsText.color = Color.white;

            // Continue button
            var btnGO = new GameObject("ContinueBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(resultPanel.transform, false);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0);
            btnRect.anchorMax = new Vector2(0.5f, 0);
            btnRect.pivot = new Vector2(0.5f, 0);
            btnRect.anchoredPosition = new Vector2(0, 15);
            btnRect.sizeDelta = new Vector2(150, 45);
            btnGO.GetComponent<Image>().color = new Color(0.3f, 0.5f, 0.7f);

            var btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGO.transform.SetParent(btnGO.transform, false);
            var btRect = btnTextGO.GetComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;
            var btTmp = btnTextGO.GetComponent<TextMeshProUGUI>();
            btTmp.text = "Continue";
            btTmp.fontSize = 20;
            btTmp.alignment = TextAlignmentOptions.Center;
            btTmp.color = Color.white;

            continueButton = btnGO.GetComponent<Button>();
            continueButton.onClick.AddListener(OnContinueClicked);

            resultPanel.SetActive(false);
            Debug.Log("[EncounterResultUI] BuildResultPanel: complete (hidden)");
        }

        private void ShowResult(EncounterResult result)
        {
            Debug.Log($"[EncounterResultUI] ShowResult: {result}");

            if (result.success)
            {
                titleText.text = result.recalled ? "Recall Successful" : "Encounter Complete!";
                titleText.color = new Color(0.3f, 0.9f, 0.3f);
            }
            else
            {
                titleText.text = "Encounter Failed";
                titleText.color = new Color(0.9f, 0.3f, 0.3f);
            }

            string details = "";
            if (result.success)
            {
                details += "<b>Resources Gathered:</b>\n";
                if (result.resourcesGathered.Count == 0)
                    details += "  None\n";
                foreach (var kvp in result.resourcesGathered)
                    details += $"  {kvp.Key}: {kvp.Value}\n";
            }
            else
            {
                details += "All gathered resources lost!\n";
            }

            if (result.woundedHeroes.Count > 0)
            {
                details += "\n<b>Wounded:</b>\n";
                foreach (var hero in result.woundedHeroes)
                    details += $"  {hero.cardName} (sits out next encounter)\n";
            }

            if (result.exhaustedHeroes.Count > 0)
            {
                details += "\n<b>Exhausted:</b>\n";
                foreach (var hero in result.exhaustedHeroes)
                    details += $"  {hero.cardName} (out for rest of level)\n";
            }

            detailsText.text = details;
            resultPanel.SetActive(true);
        }

        private void OnContinueClicked()
        {
            Debug.Log("[EncounterResultUI] OnContinueClicked: hiding result panel");
            resultPanel.SetActive(false);
        }
    }
}
