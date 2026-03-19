using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Scurry.Cards;
using Scurry.Core;
using Scurry.Data;
using Scurry.UI;

/// <summary>
/// Plays through the game using ONLY UI button clicks (onClick.Invoke), taking high-res
/// screenshots at each phase. No direct manager calls — every interaction goes through the UI.
/// </summary>
public class UIPlaythroughTest
{
    private static int frame;
    private static bool running;
    private static int state; // 0=mainmenu, 1=deckbuild, 2=deploy, 3=gamemap
    private static int deckClicks;
    private static int deployClicks;
    private static int screenshotCount;

    [MenuItem("Scurry/UI Playthrough Test")]
    public static void Run()
    {
        // Always reset — static fields survive domain reload
        EditorApplication.update -= Tick;
        running = true;
        frame = 0;
        state = 0;
        deckClicks = 0;
        deployClicks = 0;
        screenshotCount = 0;

        Debug.Log("[UIPlaytest] Starting — will click through UI buttons only and screenshot each phase");

        // Must exit play mode first, then re-enter so Bootstrap loads fresh
        if (EditorApplication.isPlaying)
        {
            Debug.Log("[UIPlaytest] Already in play mode — stopping first, will restart");
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () =>
            {
                EditorApplication.isPlaying = true;
                EditorApplication.update += Tick;
            };
        }
        else
        {
            EditorApplication.isPlaying = true;
            EditorApplication.update += Tick;
        }
    }

    static void Stop(string reason)
    {
        Debug.Log($"[UIPlaytest] Stopping: {reason} (screenshots={screenshotCount})");
        EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false;
        running = false;
    }

    static void Screenshot(string label)
    {
        screenshotCount++;
        // Save OUTSIDE Assets/ to prevent Unity from importing screenshots as textures
        string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
        string dir = System.IO.Path.Combine(projectRoot, "Screenshots");
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);
        string filename = $"playtest_{screenshotCount:D2}_{label}.png";
        string fullPath = System.IO.Path.Combine(dir, filename);
        ScreenCapture.CaptureScreenshot(fullPath, 2);
        Debug.Log($"[UIPlaytest] Screenshot #{screenshotCount}: {label} -> {fullPath}");
    }

    /// <summary>
    /// Clicks a button by finding its GameObject by name. Returns true if clicked.
    /// </summary>
    static bool ClickButton(string name)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            Debug.LogWarning($"[UIPlaytest] ClickButton: '{name}' not found");
            return false;
        }
        var btn = go.GetComponent<Button>();
        if (btn == null)
        {
            Debug.LogWarning($"[UIPlaytest] ClickButton: no Button component on '{name}'");
            return false;
        }
        if (!btn.interactable)
        {
            Debug.LogWarning($"[UIPlaytest] ClickButton: '{name}' is not interactable");
            return false;
        }
        btn.onClick.Invoke();
        Debug.Log($"[UIPlaytest] ClickButton: clicked '{name}'");
        return true;
    }

    /// <summary>
    /// Finds and clicks pool card buttons (named "Pool_*") to add cards to the deck.
    /// Clicks one card per tick to simulate player behavior. Returns true if a card was clicked.
    /// </summary>
    static bool ClickNextPoolCard()
    {
        // Find all Button components in the scene
        var allButtons = GameObject.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var btn in allButtons)
        {
            if (btn.gameObject.name.StartsWith("Pool_") && btn.interactable)
            {
                // Check if the card's Image is not dimmed (dimmed = at copy limit)
                var img = btn.GetComponent<Image>();
                if (img != null && img.color.a < 0.6f) continue; // Skip dimmed cards

                btn.onClick.Invoke();
                Debug.Log($"[UIPlaytest] ClickNextPoolCard: clicked '{btn.gameObject.name}'");
                return true;
            }
        }
        Debug.LogWarning("[UIPlaytest] ClickNextPoolCard: no clickable pool cards found");
        return false;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) { frame = 0; return; }
        frame++;
        if (frame % 20 != 0) return;
        if (frame > 9000) { Stop("timeout (9000 frames)"); return; }

        // === STATE 0: Main Menu — screenshot then click New Run ===
        if (state == 0)
        {
            var menu = Object.FindAnyObjectByType<MainMenuManager>();
            if (menu != null)
            {
                Screenshot("01_MainMenu");
                ClickButton("NewRunButton");
                state = 1;
            }
            return;
        }

        // === STATE 1: Deck Construction — clear deck, then click pool cards to build via UI ===
        if (state == 1)
        {
            var dcm = Object.FindAnyObjectByType<DeckConstructionManager>();
            if (dcm == null) return;

            // First tick in deck construction: clear any pre-populated deck
            if (deckClicks == 0 && dcm.IsDeckValid)
            {
                Debug.Log($"[UIPlaytest] STATE 1: deck pre-populated (size={dcm.DeckSize}), clicking Clear to test UI card clicks");
                ClickButton("ClearButton");
                deckClicks = -1; // sentinel: we cleared, next tick start clicking
                return;
            }
            if (deckClicks == -1)
            {
                deckClicks = 0; // reset after clear
                Debug.Log($"[UIPlaytest] STATE 1: deck cleared, size={dcm.DeckSize}, now clicking pool cards");
            }

            if (!dcm.IsDeckValid)
            {
                // Click one pool card per tick via the UI button
                bool clicked = ClickNextPoolCard();
                if (!clicked)
                {
                    Debug.LogError($"[UIPlaytest] STATE 1: No pool cards clickable but deck is not valid (size={dcm.DeckSize}). UI is broken!");
                    Stop("deck construction UI broken — cannot click pool cards");
                }
                else
                {
                    deckClicks++;
                    Debug.Log($"[UIPlaytest] STATE 1: deck building progress — clicks={deckClicks}, deckSize={dcm.DeckSize}, valid={dcm.IsDeckValid}");
                }
                return;
            }

            // Deck valid — screenshot then click Start Run button
            Screenshot("02_DeckConstruction");
            if (!ClickButton("StartRunButton"))
            {
                Debug.LogError("[UIPlaytest] STATE 1: StartRunButton not found or not interactable!");
                Stop("StartRunButton missing");
            }
            else
            {
                state = 2;
            }
            return;
        }

        // === STATE 2: Deploy Phase — click hero cards in UI, click deploy, end phase ===
        if (state == 2)
        {
            var deployUI = Object.FindAnyObjectByType<DeploymentUI>();
            if (deployUI == null) return;

            if (deployClicks < 2)
            {
                // Click hero cards via their UI buttons
                var allButtons = GameObject.FindObjectsByType<Button>(FindObjectsSortMode.None);
                foreach (var btn in allButtons)
                {
                    if (btn.gameObject.name.StartsWith("HeroCard_") && btn.interactable)
                    {
                        btn.onClick.Invoke();
                        Debug.Log($"[UIPlaytest] STATE 2: clicked hero card '{btn.gameObject.name}'");

                        // Now click Deploy button
                        ClickButton("DeployBtn");
                        deployClicks++;
                        return;
                    }
                }
                Debug.LogWarning("[UIPlaytest] STATE 2: no hero cards found to click");
                return;
            }

            // Heroes staged — screenshot then End Phase via button
            Screenshot("04_DeployPhase");
            ClickButton("EndPhaseBtn");
            state = 3;
            return;
        }

        // === STATE 3: GameMap — click Continue/EndPhase buttons, set hero targets via UI ===
        if (state == 3)
        {
            var tm = TurnManager.Instance;
            if (tm == null) return;

            // Check for Card Reward scene (appears after combat victory) — click Skip
            var rewardUI = Object.FindAnyObjectByType<CardRewardUI>();
            if (rewardUI != null)
            {
                var skipGO = GameObject.Find("SkipButton");
                if (skipGO != null && skipGO.activeSelf)
                {
                    var skipBtn = skipGO.GetComponent<Button>();
                    if (skipBtn != null && skipBtn.interactable)
                    {
                        skipBtn.onClick.Invoke();
                        Debug.Log("[UIPlaytest] STATE 3: clicked SkipButton on CardReward scene");
                        return;
                    }
                }
            }

            // Check for Continue button
            var continueGO = GameObject.Find("ContinueButton");
            if (continueGO != null && continueGO.activeSelf)
            {
                string phase = tm.CurrentPhase.ToString();
                Screenshot($"05_GameMap_{phase}");

                // For hero movement: click map nodes to set targets via MapInteraction UI
                // If heroes need targets, click neighbor nodes on the map
                if (tm.DeployedHeroes != null && tm.MapGraph != null)
                {
                    var neighbors = tm.MapGraph.GetNeighbors(tm.MapGraph.ColonyNodeId);
                    if (neighbors != null && neighbors.Count > 0)
                    {
                        foreach (var hero in tm.DeployedHeroes)
                        {
                            if (hero.IsAlive && hero.isDeployed &&
                                (hero.targetNodeId < 0 || hero.targetNodeId == hero.currentNodeId))
                            {
                                // Try clicking the map node button for target assignment
                                string nodeName = $"MapNode_{neighbors[Mathf.Min(hero.tokenId, neighbors.Count - 1)].nodeId}";
                                if (!ClickButton(nodeName))
                                {
                                    // Fallback: set target directly (map nodes may not be buttons)
                                    hero.targetNodeId = neighbors[Mathf.Min(hero.tokenId, neighbors.Count - 1)].nodeId;
                                    Debug.Log($"[UIPlaytest] Set target (fallback): {hero.cardDef.cardName} -> node {hero.targetNodeId}");
                                }
                                else
                                {
                                    Debug.Log($"[UIPlaytest] Clicked map node '{nodeName}' for hero target");
                                }
                            }
                        }
                    }
                }

                // Click the Continue button
                ClickButton("ContinueButton");
                Debug.Log($"[UIPlaytest] STATE 3: Continue (phase={phase}, turn={tm.CurrentTurn})");
                return;
            }

            // Check for End Phase button on HUD (Deploy on turn 2+)
            if (tm.CurrentTurn > 1)
            {
                if (tm.CurrentPhase == GamePhase.Deploy)
                {
                    var deployUI = Object.FindAnyObjectByType<DeploymentUI>();
                    if (deployUI != null)
                    {
                        ClickButton("EndPhaseBtn");
                        return;
                    }
                }
            }

            // Stop after turn 2 cleanup
            if (tm.CurrentTurn >= 3)
            {
                Screenshot("99_Final");
                Stop($"completed turn {tm.CurrentTurn}");
            }
        }
    }
}
