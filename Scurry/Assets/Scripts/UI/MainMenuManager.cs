using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Scurry.Core;

namespace Scurry.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        private Button newRunButton;
        private Button continueButton;

        private void Awake()
        {
            Debug.Log("[MainMenuManager] Awake: building main menu UI");
            BuildMenu();
        }

        private void BuildMenu()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[MainMenuManager] BuildMenu: no Canvas component found!");
                return;
            }

            // Background
            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(transform, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgGO.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.12f);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -60);
            titleRect.sizeDelta = new Vector2(600, 80);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "SCURRY";
            titleTmp.fontSize = 72;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.95f, 0.85f, 0.5f);
            titleTmp.fontStyle = FontStyles.Bold;

            // Subtitle
            var subGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(transform, false);
            var subRect = subGO.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1f);
            subRect.anchorMax = new Vector2(0.5f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0, -140);
            subRect.sizeDelta = new Vector2(600, 40);
            var subTmp = subGO.GetComponent<TextMeshProUGUI>();
            subTmp.text = "Tales of the Rat Pack";
            subTmp.fontSize = 28;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.7f, 0.65f, 0.5f);
            subTmp.fontStyle = FontStyles.Italic;

            // Button container
            float buttonY = -40f;
            float buttonSpacing = 70f;

            // New Run button
            newRunButton = CreateMenuButton("NewRunButton", "NEW RUN", buttonY, new Color(0.2f, 0.5f, 0.2f));
            newRunButton.onClick.AddListener(OnNewRunClicked);
            buttonY -= buttonSpacing;

            // Continue button
            continueButton = CreateMenuButton("ContinueButton", "CONTINUE", buttonY, new Color(0.2f, 0.35f, 0.55f));
            continueButton.onClick.AddListener(OnContinueClicked);
            buttonY -= buttonSpacing;

            // Update continue button visibility
            bool hasSave = SaveManager.HasSave();
            continueButton.gameObject.SetActive(hasSave);
            Debug.Log($"[MainMenuManager] BuildMenu: hasSave={hasSave}, continue button active={hasSave}");

            // Version text
            var versionGO = new GameObject("Version", typeof(RectTransform), typeof(TextMeshProUGUI));
            versionGO.transform.SetParent(transform, false);
            var verRect = versionGO.GetComponent<RectTransform>();
            verRect.anchorMin = new Vector2(1f, 0f);
            verRect.anchorMax = new Vector2(1f, 0f);
            verRect.pivot = new Vector2(1f, 0f);
            verRect.anchoredPosition = new Vector2(-15, 10);
            verRect.sizeDelta = new Vector2(200, 25);
            var verTmp = versionGO.GetComponent<TextMeshProUGUI>();
            verTmp.text = "M0 Prototype";
            verTmp.fontSize = 14;
            verTmp.alignment = TextAlignmentOptions.BottomRight;
            verTmp.color = new Color(0.4f, 0.4f, 0.4f);

            Debug.Log("[MainMenuManager] BuildMenu: complete");
        }

        private Button CreateMenuButton(string name, string label, float yOffset, Color bgColor)
        {
            var btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(transform, false);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(0, yOffset);
            btnRect.sizeDelta = new Vector2(300, 55);
            btnGO.GetComponent<Image>().color = bgColor;

            var txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(btnGO.transform, false);
            var txtRect = txtGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;
            var tmp = txtGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            Debug.Log($"[MainMenuManager] CreateMenuButton: '{label}' at y={yOffset}");
            return btnGO.GetComponent<Button>();
        }

        private void OnNewRunClicked()
        {
            Debug.Log("[MainMenuManager] OnNewRunClicked: deleting save and loading prototype scene");
            SaveManager.DeleteSave();
            SceneManager.LoadScene("M0_Prototype");
        }

        private void OnContinueClicked()
        {
            Debug.Log("[MainMenuManager] OnContinueClicked: loading prototype scene (will auto-load save)");
            SceneManager.LoadScene("M0_Prototype");
        }
    }
}
