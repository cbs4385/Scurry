using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Scurry.Cards;
using Scurry.Core;
using Scurry.Data;
using Scurry.UI;

/// <summary>
/// Editor helper to auto-navigate the game through deck construction, colony, deployment,
/// and stop at GameMap during the Hero Move phase so the tester can see deployed heroes on the map.
/// Use Scurry/Quick Deploy Test menu item.
/// </summary>
public class QuickDeployTest
{
    private static int frameCount;
    private static bool running;
    private static bool deckFilled;
    private static bool deckConfirmed;
    private static int heroesStaged;
    private static bool deployEndPhaseClicked;

    [MenuItem("Scurry/Quick Deploy Test")]
    public static void Run()
    {
        if (running) return;
        running = true;
        frameCount = 0;
        deckFilled = false;
        deckConfirmed = false;
        heroesStaged = 0;
        deployEndPhaseClicked = false;

        Debug.Log("[QuickDeployTest] Starting — will auto-advance to GameMap with deployed heroes");

        if (!EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = true;
        }

        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            frameCount = 0;
            return;
        }

        frameCount++;
        if (frameCount % 20 != 0) return;

        // Goal: stop at GameMap during Hero Move phase (after deploy)
        var tm = TurnManager.Instance;

        // Check if we've reached HeroMove phase — DONE
        if (tm != null && tm.RunActive && deployEndPhaseClicked &&
            (tm.CurrentPhase == GamePhase.HeroMove || tm.CurrentPhase == GamePhase.EnemyMove ||
             tm.CurrentPhase == GamePhase.Combat || tm.CurrentPhase == GamePhase.Gather))
        {
            Debug.Log($"[QuickDeployTest] REACHED POST-DEPLOY PHASE ({tm.CurrentPhase}) — stopping");
            Debug.Log($"[QuickDeployTest] DeployedHeroes={tm.DeployedHeroes.Count}, Turn={tm.CurrentTurn}");
            EditorApplication.update -= Tick;
            running = false;
            return;
        }

        // 1. MainMenu — click NEW RUN
        var mainMenu = Object.FindAnyObjectByType<MainMenuManager>();
        if (mainMenu != null)
        {
            var newRunBtn = GameObject.Find("NewRunButton");
            if (newRunBtn != null)
            {
                var btn = newRunBtn.GetComponent<Button>();
                if (btn != null)
                {
                    Debug.Log("[QuickDeployTest] Clicking NEW RUN");
                    btn.onClick.Invoke();
                    return;
                }
            }
        }

        // 2. DeckConstruction — auto-fill deck then confirm
        var dcm = Object.FindAnyObjectByType<DeckConstructionManager>();
        if (dcm != null)
        {
            if (!deckFilled)
            {
                Debug.Log($"[QuickDeployTest] Filling deck (current={dcm.DeckSize})");
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
                Debug.Log($"[QuickDeployTest] Deck filled: size={dcm.DeckSize}, valid={dcm.IsDeckValid}");
                return;
            }

            if (!deckConfirmed && dcm.IsDeckValid)
            {
                Debug.Log("[QuickDeployTest] Confirming deck");
                dcm.ConfirmDeck();
                deckConfirmed = true;
                return;
            }
            return;
        }

        // 3. Deploy phase — stage one hero per tick (max 2)
        var deployUI = Object.FindAnyObjectByType<DeploymentUI>();
        if (deployUI != null && heroesStaged < 2)
        {
            var deckManager = DeckManager.Instance ?? Object.FindAnyObjectByType<DeckManager>();
            if (deckManager != null && deckManager.AvailableHeroes.Count > 0)
            {
                var hero = deckManager.AvailableHeroes[0];
                deployUI.OnHeroSelected(hero, 0);
                deployUI.OnDeployClicked();
                heroesStaged++;
                Debug.Log($"[QuickDeployTest] Staged hero #{heroesStaged}: {hero.cardName}");
            }
            return;
        }

        if (deployUI != null && heroesStaged >= 2 && !deployEndPhaseClicked)
        {
            Debug.Log("[QuickDeployTest] Deploy complete — clicking End Phase");
            // Find and click End Phase button in DeploymentUI
            var endBtn = GameObject.Find("EndPhaseBtn");
            if (endBtn != null)
            {
                var btn = endBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.Invoke();
                    deployEndPhaseClicked = true;
                    return;
                }
            }
            // Fallback: use TurnManager directly
            if (tm != null)
            {
                tm.PlayerEndPhase();
                deployEndPhaseClicked = true;
            }
            return;
        }

        // 5. If waiting for Continue button (post Hero Move, etc.), click it
        if (tm != null && deployEndPhaseClicked)
        {
            var hud = Object.FindAnyObjectByType<HUDManager>();
            if (hud != null)
            {
                // Don't auto-continue — let the tester see the map
                // Just stop here
                Debug.Log($"[QuickDeployTest] On GameMap, phase={tm.CurrentPhase} — stopping for inspection");
                EditorApplication.update -= Tick;
                running = false;
                return;
            }
        }

        // Safety timeout
        if (frameCount > 3600)
        {
            Debug.LogWarning("[QuickDeployTest] Timeout — stopping");
            EditorApplication.update -= Tick;
            running = false;
        }
    }
}
