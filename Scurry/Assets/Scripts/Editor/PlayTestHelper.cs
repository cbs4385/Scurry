using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Scurry.Cards;
using Scurry.Core;

/// <summary>
/// Editor helper to auto-navigate the game to the Deploy phase for visual testing.
/// Use Scurry/Test Deploy Phase menu item.
/// </summary>
public class PlayTestHelper
{
    private static int frameCount;
    private static bool running;
    private static bool deckFilled;
    private static bool deckConfirmed;

    [MenuItem("Scurry/Test Deploy Phase")]
    public static void TestDeployPhase()
    {
        if (running) return;
        running = true;
        frameCount = 0;
        deckFilled = false;
        deckConfirmed = false;

        if (!EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = true;
        }

        EditorApplication.update += WaitAndClickButtons;
    }

    private static void WaitAndClickButtons()
    {
        if (!EditorApplication.isPlaying)
        {
            frameCount = 0;
            return;
        }

        frameCount++;

        // Check every 20 frames (~3x/sec at 60fps)
        if (frameCount % 20 != 0) return;

        // 1. Check if we reached Deployment scene — DONE
        var deployUI = Object.FindAnyObjectByType<Scurry.UI.DeploymentUI>();
        if (deployUI != null)
        {
            Debug.Log("[PlayTestHelper] REACHED DEPLOYMENT SCENE — stopping auto-navigation");
            EditorApplication.update -= WaitAndClickButtons;
            running = false;
            return;
        }

        // 2. MainMenu — click NEW RUN
        var newRunBtn = GameObject.Find("NewRunButton");
        if (newRunBtn != null)
        {
            var btn = newRunBtn.GetComponent<Button>();
            if (btn != null)
            {
                Debug.Log("[PlayTestHelper] Clicking NEW RUN");
                btn.onClick.Invoke();
                return;
            }
        }

        // 3. DeckConstruction — auto-fill deck then confirm
        var dcm = Object.FindAnyObjectByType<DeckConstructionManager>();
        if (dcm != null)
        {
            if (!deckFilled)
            {
                Debug.Log($"[PlayTestHelper] Filling deck (current={dcm.DeckSize})");
                var pool = dcm.CardPool;
                if (pool != null)
                {
                    foreach (var card in pool)
                    {
                        if (dcm.IsDeckValid) break;
                        while (dcm.CanAddCard(card))
                        {
                            dcm.AddCard(card);
                            if (dcm.IsDeckValid) break;
                        }
                    }
                }
                deckFilled = true;
                Debug.Log($"[PlayTestHelper] Deck filled: size={dcm.DeckSize}, valid={dcm.IsDeckValid}");
                return;
            }

            if (!deckConfirmed && dcm.IsDeckValid)
            {
                Debug.Log("[PlayTestHelper] Confirming deck");
                dcm.ConfirmDeck();
                deckConfirmed = true;
                return;
            }
            return;
        }

        // Safety timeout after ~60 seconds
        if (frameCount > 3600)
        {
            Debug.LogWarning("[PlayTestHelper] Timeout — stopping");
            EditorApplication.update -= WaitAndClickButtons;
            running = false;
        }
    }
}
