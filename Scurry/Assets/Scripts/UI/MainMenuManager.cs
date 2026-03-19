using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Scurry.Core;

namespace Scurry.UI
{
    /// <summary>
    /// Main menu screen. Builds all UI programmatically (no prefabs, per project convention).
    /// Provides New Run, Continue Run, Settings, and Quit buttons.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        private RunManager runManager;

        // UI elements
        private Button newRunButton;
        private Button continueButton;
        private Button settingsButton;
        private Button quitButton;
        private GameObject settingsPanel;
        private bool settingsPanelVisible;

        private void Awake()
        {
            Debug.Log("[MainMenuManager] Awake: checking for RunManager");

            // If Bootstrap never ran, redirect to it so persistent managers initialize
            if (RunManager.Instance == null)
            {
                Debug.Log("[MainMenuManager] Awake: RunManager not found — loading Bootstrap scene to initialize persistent managers");
                SceneManager.LoadScene("Bootstrap");
                return;
            }

            Debug.Log("[MainMenuManager] Awake: RunManager found, building main menu UI");
            BuildUI();
        }

        private void Start()
        {
            Debug.Log("[MainMenuManager] Start: resolving RunManager");

            runManager = RunManager.Instance;
            if (runManager == null)
            {
                runManager = ServiceLocator.Get<RunManager>();
            }

            Debug.Log($"[MainMenuManager] Start: runManager={(runManager != null ? "OK" : "NULL")}");

            // Check for existing save
            bool hasSave = SaveManager.HasSave();
            Debug.Log($"[MainMenuManager] Start: hasSave={hasSave}");

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(hasSave);
                Debug.Log($"[MainMenuManager] Start: continue button active={hasSave}");
            }
        }

        /// <summary>
        /// Builds the complete main menu UI programmatically.
        /// </summary>
        private void BuildUI()
        {
            Debug.Log("[MainMenuManager] BuildUI: starting UI construction");

            UIHelper.EnsureEventSystem();

            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[MainMenuManager] BuildUI: no Canvas component found on this GameObject!");
                return;
            }

            // Background panel
            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(transform, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgGO.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.12f);
            Debug.Log("[MainMenuManager] BuildUI: background panel created");

            // Title text: "SCURRY"
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -60);
            titleRect.sizeDelta = new Vector2(700, 90);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "SCURRY";
            titleTmp.fontSize = 72;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.95f, 0.85f, 0.5f);
            titleTmp.fontStyle = FontStyles.Bold;
            Debug.Log("[MainMenuManager] BuildUI: title text created");

            // Subtitle: "Tales of the Rat Pack"
            var subGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(transform, false);
            var subRect = subGO.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1f);
            subRect.anchorMax = new Vector2(0.5f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0, -150);
            subRect.sizeDelta = new Vector2(700, 45);
            var subTmp = subGO.GetComponent<TextMeshProUGUI>();
            subTmp.text = "Tales of the Rat Pack";
            subTmp.fontSize = 28;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.7f, 0.65f, 0.5f);
            subTmp.fontStyle = FontStyles.Italic;
            Debug.Log("[MainMenuManager] BuildUI: subtitle text created");

            // Button layout
            float buttonStartY = 20f;
            float buttonSpacing = 75f;
            float currentY = buttonStartY;

            // New Run button
            newRunButton = CreateMenuButton("NewRunButton", "NEW RUN", currentY, new Color(0.2f, 0.5f, 0.2f));
            newRunButton.onClick.AddListener(OnNewRun);
            Debug.Log($"[MainMenuManager] BuildUI: New Run button created at y={currentY}");
            currentY -= buttonSpacing;

            // Continue Run button (hidden if no save)
            continueButton = CreateMenuButton("ContinueButton", "CONTINUE RUN", currentY, new Color(0.2f, 0.35f, 0.55f));
            continueButton.onClick.AddListener(OnContinueRun);
            continueButton.gameObject.SetActive(false); // Will be shown in Start() if save exists
            Debug.Log($"[MainMenuManager] BuildUI: Continue Run button created at y={currentY} (initially hidden)");
            currentY -= buttonSpacing;

            // Settings button
            settingsButton = CreateMenuButton("SettingsButton", "SETTINGS", currentY, new Color(0.35f, 0.35f, 0.35f));
            settingsButton.onClick.AddListener(OnSettings);
            Debug.Log($"[MainMenuManager] BuildUI: Settings button created at y={currentY}");
            currentY -= buttonSpacing;

            // Quit button
            quitButton = CreateMenuButton("QuitButton", "QUIT", currentY, new Color(0.5f, 0.2f, 0.2f));
            quitButton.onClick.AddListener(OnQuit);
            Debug.Log($"[MainMenuManager] BuildUI: Quit button created at y={currentY}");

            // Settings panel (hidden by default)
            BuildSettingsPanel();

            // Version text
            var versionGO = new GameObject("Version", typeof(RectTransform), typeof(TextMeshProUGUI));
            versionGO.transform.SetParent(transform, false);
            var verRect = versionGO.GetComponent<RectTransform>();
            verRect.anchorMin = new Vector2(1f, 0f);
            verRect.anchorMax = new Vector2(1f, 0f);
            verRect.pivot = new Vector2(1f, 0f);
            verRect.anchoredPosition = new Vector2(-15, 10);
            verRect.sizeDelta = new Vector2(250, 25);
            var verTmp = versionGO.GetComponent<TextMeshProUGUI>();
            verTmp.text = "v2.0 Strategic Overhaul";
            verTmp.fontSize = 14;
            verTmp.alignment = TextAlignmentOptions.BottomRight;
            verTmp.color = new Color(0.4f, 0.4f, 0.4f);
            Debug.Log("[MainMenuManager] BuildUI: version text created");

            Debug.Log("[MainMenuManager] BuildUI: UI construction complete");
        }

        /// <summary>
        /// Creates a styled menu button.
        /// </summary>
        private Button CreateMenuButton(string name, string label, float yOffset, Color bgColor)
        {
            Debug.Log($"[MainMenuManager] CreateMenuButton: creating '{label}' at y={yOffset}");

            var btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(transform, false);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(0, yOffset);
            btnRect.sizeDelta = new Vector2(320, 55);

            var btnImage = btnGO.GetComponent<Image>();
            btnImage.color = bgColor;

            // Button hover color setup
            var button = btnGO.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.2f;
            colors.pressedColor = bgColor * 0.8f;
            colors.selectedColor = bgColor;
            colors.disabledColor = bgColor * 0.5f;
            button.colors = colors;

            // Button label
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

            Debug.Log($"[MainMenuManager] CreateMenuButton: '{label}' created successfully");
            return button;
        }

        /// <summary>
        /// Builds the settings panel (hidden by default).
        /// </summary>
        private void BuildSettingsPanel()
        {
            Debug.Log("[MainMenuManager] BuildSettingsPanel: creating settings panel");

            settingsPanel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            settingsPanel.transform.SetParent(transform, false);
            var panelRect = settingsPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(500, 400);
            settingsPanel.GetComponent<Image>().color = new Color(0.12f, 0.1f, 0.18f, 0.95f);

            // Settings title
            var settingsTitleGO = new GameObject("SettingsTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            settingsTitleGO.transform.SetParent(settingsPanel.transform, false);
            var settingsTitleRect = settingsTitleGO.GetComponent<RectTransform>();
            settingsTitleRect.anchorMin = new Vector2(0.5f, 1f);
            settingsTitleRect.anchorMax = new Vector2(0.5f, 1f);
            settingsTitleRect.pivot = new Vector2(0.5f, 1f);
            settingsTitleRect.anchoredPosition = new Vector2(0, -20);
            settingsTitleRect.sizeDelta = new Vector2(400, 50);
            var settingsTmp = settingsTitleGO.GetComponent<TextMeshProUGUI>();
            settingsTmp.text = "SETTINGS";
            settingsTmp.fontSize = 36;
            settingsTmp.alignment = TextAlignmentOptions.Center;
            settingsTmp.color = new Color(0.95f, 0.85f, 0.5f);
            settingsTmp.fontStyle = FontStyles.Bold;

            // Placeholder text
            var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderGO.transform.SetParent(settingsPanel.transform, false);
            var placeholderRect = placeholderGO.GetComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0.5f, 0.5f);
            placeholderRect.anchorMax = new Vector2(0.5f, 0.5f);
            placeholderRect.pivot = new Vector2(0.5f, 0.5f);
            placeholderRect.anchoredPosition = Vector2.zero;
            placeholderRect.sizeDelta = new Vector2(400, 100);
            var placeholderTmp = placeholderGO.GetComponent<TextMeshProUGUI>();
            placeholderTmp.text = "Settings will be available in a future update.";
            placeholderTmp.fontSize = 20;
            placeholderTmp.alignment = TextAlignmentOptions.Center;
            placeholderTmp.color = new Color(0.6f, 0.6f, 0.6f);

            // Close button
            var closeBtnGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnGO.transform.SetParent(settingsPanel.transform, false);
            var closeBtnRect = closeBtnGO.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(0.5f, 0f);
            closeBtnRect.anchorMax = new Vector2(0.5f, 0f);
            closeBtnRect.pivot = new Vector2(0.5f, 0f);
            closeBtnRect.anchoredPosition = new Vector2(0, 20);
            closeBtnRect.sizeDelta = new Vector2(150, 45);
            closeBtnGO.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f);

            var closeTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeTxtGO.transform.SetParent(closeBtnGO.transform, false);
            var closeTxtRect = closeTxtGO.GetComponent<RectTransform>();
            closeTxtRect.anchorMin = Vector2.zero;
            closeTxtRect.anchorMax = Vector2.one;
            closeTxtRect.sizeDelta = Vector2.zero;
            var closeTmp = closeTxtGO.GetComponent<TextMeshProUGUI>();
            closeTmp.text = "CLOSE";
            closeTmp.fontSize = 22;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = Color.white;

            closeBtnGO.GetComponent<Button>().onClick.AddListener(OnSettingsClose);

            settingsPanel.SetActive(false);
            settingsPanelVisible = false;
            Debug.Log("[MainMenuManager] BuildSettingsPanel: settings panel created (hidden)");
        }

        // ── Button Handlers ──────────────────────────────────────────────

        /// <summary>
        /// Starts a new run via RunManager.
        /// </summary>
        private void OnNewRun()
        {
            Debug.Log("[MainMenuManager] OnNewRun: player clicked New Run");

            if (runManager == null)
            {
                Debug.LogError("[MainMenuManager] OnNewRun: runManager is null — cannot start run");
                return;
            }

            Debug.Log("[MainMenuManager] OnNewRun: calling RunManager.StartNewRun()");
            runManager.StartNewRun();
        }

        /// <summary>
        /// Continues a saved run via RunManager.
        /// </summary>
        private void OnContinueRun()
        {
            Debug.Log("[MainMenuManager] OnContinueRun: player clicked Continue Run");

            if (runManager == null)
            {
                Debug.LogError("[MainMenuManager] OnContinueRun: runManager is null — cannot continue");
                return;
            }

            if (!SaveManager.HasSave())
            {
                Debug.LogWarning("[MainMenuManager] OnContinueRun: no save found — cannot continue");
                EventBus.OnNotification?.Invoke("No saved run found!", Color.red);
                return;
            }

            Debug.Log("[MainMenuManager] OnContinueRun: calling RunManager.ContinueRun()");
            runManager.ContinueRun();
        }

        /// <summary>
        /// Shows the settings panel.
        /// </summary>
        private void OnSettings()
        {
            Debug.Log("[MainMenuManager] OnSettings: player clicked Settings");

            if (settingsPanel != null)
            {
                settingsPanelVisible = !settingsPanelVisible;
                settingsPanel.SetActive(settingsPanelVisible);
                Debug.Log($"[MainMenuManager] OnSettings: settings panel visible={settingsPanelVisible}");
            }
            else
            {
                Debug.LogWarning("[MainMenuManager] OnSettings: settingsPanel is null");
            }
        }

        /// <summary>
        /// Closes the settings panel.
        /// </summary>
        private void OnSettingsClose()
        {
            Debug.Log("[MainMenuManager] OnSettingsClose: closing settings panel");

            if (settingsPanel != null)
            {
                settingsPanelVisible = false;
                settingsPanel.SetActive(false);
                Debug.Log("[MainMenuManager] OnSettingsClose: settings panel hidden");
            }
        }

        /// <summary>
        /// Quits the application.
        /// </summary>
        private void OnQuit()
        {
            Debug.Log("[MainMenuManager] OnQuit: player clicked Quit");

#if UNITY_EDITOR
            Debug.Log("[MainMenuManager] OnQuit: in editor — stopping play mode");
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Debug.Log("[MainMenuManager] OnQuit: calling Application.Quit()");
            Application.Quit();
#endif
        }
    }
}
