using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Data;
using Scurry.Core;
using Scurry.Interfaces;

namespace Scurry.UI
{
    public class EventManager : MonoBehaviour
    {
        private IColonyManager colonyManager;

        private GameObject eventPanel;
        private TextMeshProUGUI eventTitle;
        private TextMeshProUGUI eventDescription;
        private TextMeshProUGUI outcomeText;
        private GameObject choicesContainer;
        private Button continueButton;
        private readonly List<GameObject> choiceButtons = new List<GameObject>();

        private EventDefinitionSO currentEvent;

        private void Awake()
        {
            BuildEventPanel();
        }

        private void Start()
        {
            colonyManager = ServiceLocator.Get<IColonyManager>();
            Debug.Log($"[EventManager] Start: colonyManager={(colonyManager != null ? "OK" : "NULL")}");
        }

        public void OpenEvent(EventDefinitionSO eventDef)
        {
            currentEvent = eventDef;
            Debug.Log($"[EventManager] OpenEvent: event='{eventDef.eventName}', choices={eventDef.choices.Count}");
            eventTitle.text = eventDef.eventName;
            eventDescription.text = eventDef.description;
            outcomeText.text = "";
            outcomeText.gameObject.SetActive(false);
            continueButton.gameObject.SetActive(false);
            choicesContainer.SetActive(true);
            RenderChoices();
            eventPanel.SetActive(true);
        }

        private void BuildEventPanel()
        {
            eventPanel = new GameObject("EventPanel", typeof(RectTransform), typeof(Image));
            eventPanel.transform.SetParent(transform, false);
            var panelRect = eventPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(500, 400);
            eventPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

            // Title
            var titleGO = new GameObject("EventTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(eventPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(-20, 35);
            eventTitle = titleGO.GetComponent<TextMeshProUGUI>();
            eventTitle.fontSize = 22;
            eventTitle.fontStyle = FontStyles.Bold;
            eventTitle.alignment = TextAlignmentOptions.Center;
            eventTitle.color = new Color(0.9f, 0.8f, 0.5f);

            // Description
            var descGO = new GameObject("EventDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descGO.transform.SetParent(eventPanel.transform, false);
            var descRect = descGO.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0.55f);
            descRect.anchorMax = new Vector2(1, 0.88f);
            descRect.offsetMin = new Vector2(20, 0);
            descRect.offsetMax = new Vector2(-20, 0);
            eventDescription = descGO.GetComponent<TextMeshProUGUI>();
            eventDescription.fontSize = 14;
            eventDescription.alignment = TextAlignmentOptions.TopLeft;
            eventDescription.color = Color.white;

            // Choices container
            choicesContainer = new GameObject("Choices", typeof(RectTransform));
            choicesContainer.transform.SetParent(eventPanel.transform, false);
            var choicesRect = choicesContainer.GetComponent<RectTransform>();
            choicesRect.anchorMin = new Vector2(0, 0.1f);
            choicesRect.anchorMax = new Vector2(1, 0.5f);
            choicesRect.offsetMin = new Vector2(20, 0);
            choicesRect.offsetMax = new Vector2(-20, 0);

            // Outcome text
            var outcomeGO = new GameObject("OutcomeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            outcomeGO.transform.SetParent(eventPanel.transform, false);
            var outcomeRect = outcomeGO.GetComponent<RectTransform>();
            outcomeRect.anchorMin = new Vector2(0, 0.2f);
            outcomeRect.anchorMax = new Vector2(1, 0.5f);
            outcomeRect.offsetMin = new Vector2(20, 0);
            outcomeRect.offsetMax = new Vector2(-20, 0);
            outcomeText = outcomeGO.GetComponent<TextMeshProUGUI>();
            outcomeText.fontSize = 16;
            outcomeText.alignment = TextAlignmentOptions.Center;
            outcomeText.color = new Color(0.8f, 0.9f, 0.5f);
            outcomeGO.SetActive(false);

            // Continue button
            var contGO = new GameObject("ContinueBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            contGO.transform.SetParent(eventPanel.transform, false);
            var contRect = contGO.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.5f, 0);
            contRect.anchorMax = new Vector2(0.5f, 0);
            contRect.pivot = new Vector2(0.5f, 0);
            contRect.anchoredPosition = new Vector2(0, 15);
            contRect.sizeDelta = new Vector2(150, 45);
            contGO.GetComponent<Image>().color = new Color(0.3f, 0.5f, 0.7f);

            var contTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            contTextGO.transform.SetParent(contGO.transform, false);
            var ctRect = contTextGO.GetComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;
            var ctTmp = contTextGO.GetComponent<TextMeshProUGUI>();
            ctTmp.text = "Continue";
            ctTmp.fontSize = 18;
            ctTmp.alignment = TextAlignmentOptions.Center;
            ctTmp.color = Color.white;

            continueButton = contGO.GetComponent<Button>();
            continueButton.onClick.AddListener(OnContinueClicked);
            contGO.SetActive(false);

            eventPanel.SetActive(false);
            Debug.Log("[EventManager] BuildEventPanel: complete (hidden)");
        }

        private void RenderChoices()
        {
            foreach (var btn in choiceButtons) Destroy(btn);
            choiceButtons.Clear();

            if (currentEvent == null) return;

            float btnHeight = 45f;
            float spacing = 10f;

            for (int i = 0; i < currentEvent.choices.Count; i++)
            {
                var choice = currentEvent.choices[i];
                var btnGO = new GameObject($"Choice_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGO.transform.SetParent(choicesContainer.transform, false);
                var btnRect = btnGO.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0, 1);
                btnRect.anchorMax = new Vector2(1, 1);
                btnRect.pivot = new Vector2(0.5f, 1);
                btnRect.anchoredPosition = new Vector2(0, -i * (btnHeight + spacing));
                btnRect.sizeDelta = new Vector2(0, btnHeight);
                btnGO.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.35f);

                var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGO.transform.SetParent(btnGO.transform, false);
                var textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10, 0);
                textRect.offsetMax = new Vector2(-10, 0);
                var tmp = textGO.GetComponent<TextMeshProUGUI>();
                // Use choiceTextKey as display text for now (localization not wired yet)
                tmp.text = GetChoiceDisplayText(choice);
                tmp.fontSize = 14;
                tmp.alignment = TextAlignmentOptions.Left;
                tmp.color = Color.white;

                int capturedIndex = i;
                btnGO.GetComponent<Button>().onClick.AddListener(() => OnChoiceSelected(capturedIndex));
                choiceButtons.Add(btnGO);
            }
        }

        private string GetChoiceDisplayText(EventChoice choice)
        {
            string outcomeHint = choice.outcomeType switch
            {
                EventOutcomeType.GainFood => $"(+{choice.outcomeValue} Food)",
                EventOutcomeType.GainMaterials => $"(+{choice.outcomeValue} Materials)",
                EventOutcomeType.GainCurrency => $"(+{choice.outcomeValue} Currency)",
                EventOutcomeType.LoseFood => $"(-{choice.outcomeValue} Food)",
                EventOutcomeType.LoseMaterials => $"(-{choice.outcomeValue} Materials)",
                EventOutcomeType.LoseCurrency => $"(-{choice.outcomeValue} Currency)",
                EventOutcomeType.GainColonyHP => $"(+{choice.outcomeValue} Colony HP)",
                EventOutcomeType.LoseColonyHP => $"(-{choice.outcomeValue} Colony HP)",
                EventOutcomeType.WoundRandomHero => "(Wound a hero)",
                _ => ""
            };
            return $"{choice.choiceTextKey} {outcomeHint}";
        }

        private void OnChoiceSelected(int index)
        {
            if (currentEvent == null || index < 0 || index >= currentEvent.choices.Count) return;

            var choice = currentEvent.choices[index];
            Debug.Log($"[EventManager] OnChoiceSelected: index={index}, outcomeType={choice.outcomeType}, value={choice.outcomeValue}");

            ApplyOutcome(choice);

            // Show outcome and hide choices
            choicesContainer.SetActive(false);
            outcomeText.gameObject.SetActive(true);
            outcomeText.text = GetOutcomeDescription(choice);
            continueButton.gameObject.SetActive(true);
        }

        private void ApplyOutcome(EventChoice choice)
        {
            if (colonyManager == null)
            {
                Debug.LogWarning("[EventManager] ApplyOutcome: colonyManager is null");
                return;
            }

            switch (choice.outcomeType)
            {
                case EventOutcomeType.GainFood:
                    colonyManager.AddFood(choice.outcomeValue);
                    Debug.Log($"[EventManager] ApplyOutcome: gained {choice.outcomeValue} food");
                    break;
                case EventOutcomeType.GainMaterials:
                    colonyManager.AddMaterials(choice.outcomeValue);
                    Debug.Log($"[EventManager] ApplyOutcome: gained {choice.outcomeValue} materials");
                    break;
                case EventOutcomeType.GainCurrency:
                    colonyManager.AddCurrency(choice.outcomeValue);
                    Debug.Log($"[EventManager] ApplyOutcome: gained {choice.outcomeValue} currency");
                    break;
                case EventOutcomeType.LoseFood:
                    colonyManager.SpendFood(Mathf.Min(choice.outcomeValue, colonyManager.FoodStockpile));
                    Debug.Log($"[EventManager] ApplyOutcome: lost {choice.outcomeValue} food");
                    break;
                case EventOutcomeType.LoseMaterials:
                    colonyManager.SpendMaterials(Mathf.Min(choice.outcomeValue, colonyManager.MaterialsStockpile));
                    Debug.Log($"[EventManager] ApplyOutcome: lost {choice.outcomeValue} materials");
                    break;
                case EventOutcomeType.LoseCurrency:
                    colonyManager.SpendCurrency(Mathf.Min(choice.outcomeValue, colonyManager.CurrencyStockpile));
                    Debug.Log($"[EventManager] ApplyOutcome: lost {choice.outcomeValue} currency");
                    break;
                case EventOutcomeType.GainColonyHP:
                    colonyManager.Heal(choice.outcomeValue);
                    Debug.Log($"[EventManager] ApplyOutcome: gained {choice.outcomeValue} colony HP");
                    break;
                case EventOutcomeType.LoseColonyHP:
                    colonyManager.TakeDamage(choice.outcomeValue);
                    Debug.Log($"[EventManager] ApplyOutcome: lost {choice.outcomeValue} colony HP");
                    break;
                case EventOutcomeType.WoundRandomHero:
                    Debug.Log("[EventManager] ApplyOutcome: wound random hero (delegated to RunManager via event)");
                    EventBus.OnEventWoundHero?.Invoke();
                    break;
                default:
                    Debug.Log($"[EventManager] ApplyOutcome: unhandled outcome type {choice.outcomeType}");
                    break;
            }
        }

        private string GetOutcomeDescription(EventChoice choice)
        {
            return choice.outcomeType switch
            {
                EventOutcomeType.GainFood => $"You gained {choice.outcomeValue} Food!",
                EventOutcomeType.GainMaterials => $"You gained {choice.outcomeValue} Materials!",
                EventOutcomeType.GainCurrency => $"You gained {choice.outcomeValue} Currency!",
                EventOutcomeType.LoseFood => $"You lost {choice.outcomeValue} Food.",
                EventOutcomeType.LoseMaterials => $"You lost {choice.outcomeValue} Materials.",
                EventOutcomeType.LoseCurrency => $"You lost {choice.outcomeValue} Currency.",
                EventOutcomeType.GainColonyHP => $"Colony healed for {choice.outcomeValue} HP!",
                EventOutcomeType.LoseColonyHP => $"Colony took {choice.outcomeValue} damage!",
                EventOutcomeType.WoundRandomHero => "One of your heroes was wounded!",
                _ => "Something happened..."
            };
        }

        private void OnContinueClicked()
        {
            Debug.Log("[EventManager] OnContinueClicked: closing event");
            eventPanel.SetActive(false);
            EventBus.OnEventComplete?.Invoke();
        }
    }
}
