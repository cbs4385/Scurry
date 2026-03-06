using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Core;
using Scurry.Colony;

namespace Scurry.UI
{
    public class ResourceUI : MonoBehaviour
    {
        [SerializeField] private ColonyManager colonyManager;

        private TextMeshProUGUI foodText;
        private TextMeshProUGUI materialsText;
        private TextMeshProUGUI currencyText;
        private TextMeshProUGUI hpText;
        private Image hpFill;

        private void Awake()
        {
            if (colonyManager == null) colonyManager = FindObjectOfType<ColonyManager>();
            BuildResourceHUD();
        }

        private void BuildResourceHUD()
        {
            // Background panel - top left
            var panelGO = new GameObject("ResourceHUD", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(transform, false);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(240, 130);
            panelGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);

            // Colony HP
            hpText = CreateLabel(panelGO.transform, "HPLabel", new Vector2(10, -8), "Colony HP: 30/50");

            // HP bar
            var hpBarBG = new GameObject("HPBarBG", typeof(RectTransform), typeof(Image));
            hpBarBG.transform.SetParent(panelGO.transform, false);
            var bgRect = hpBarBG.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 1);
            bgRect.anchorMax = new Vector2(0, 1);
            bgRect.pivot = new Vector2(0, 1);
            bgRect.anchoredPosition = new Vector2(10, -30);
            bgRect.sizeDelta = new Vector2(220, 12);
            hpBarBG.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            var hpFillGO = new GameObject("HPFill", typeof(RectTransform), typeof(Image));
            hpFillGO.transform.SetParent(hpBarBG.transform, false);
            var fillRect = hpFillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            hpFill = hpFillGO.GetComponent<Image>();
            hpFill.color = new Color(0.2f, 0.8f, 0.2f);

            // Resources
            foodText = CreateLabel(panelGO.transform, "FoodLabel", new Vector2(10, -50), "Food: 0");
            materialsText = CreateLabel(panelGO.transform, "MatLabel", new Vector2(10, -70), "Materials: 0");
            currencyText = CreateLabel(panelGO.transform, "CurrLabel", new Vector2(10, -90), "Currency: 0");

            Debug.Log("[ResourceUI] BuildResourceHUD: complete");
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 pos, string text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(-20, 20);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = Color.white;
            return tmp;
        }

        private void Update()
        {
            if (colonyManager == null) return;

            hpText.text = $"Colony HP: {colonyManager.CurrentHP}/{colonyManager.MaxHP}";
            foodText.text = $"Food: {colonyManager.FoodStockpile}";
            materialsText.text = $"Materials: {colonyManager.MaterialsStockpile}";
            currencyText.text = $"Currency: {colonyManager.CurrencyStockpile}";

            if (hpFill != null)
            {
                var fillRect = hpFill.GetComponent<RectTransform>();
                float ratio = (float)colonyManager.CurrentHP / colonyManager.MaxHP;
                fillRect.anchorMax = new Vector2(ratio, 1f);

                hpFill.color = ratio > 0.5f ? new Color(0.2f, 0.8f, 0.2f) :
                               ratio > 0.25f ? new Color(0.8f, 0.8f, 0.2f) :
                               new Color(0.8f, 0.2f, 0.2f);
            }
        }
    }
}
