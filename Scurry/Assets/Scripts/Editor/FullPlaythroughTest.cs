using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Scurry.Cards;
using Scurry.Core;
using Scurry.Data;
using Scurry.UI;

/// <summary>
/// Editor script that auto-plays through multiple full turns, clicking Continue at each phase,
/// taking screenshots, and logging issues. Advances hero targets toward wilderness.
/// </summary>
public class FullPlaythroughTest
{
    private static int frameCount;
    private static bool running;
    private static bool deckFilled;
    private static bool deckConfirmed;
    private static int heroesStaged;
    private static bool deployEndPhaseClicked;
    private static int continueClicks;
    private static int screenshotsTaken;
    private static string lastPhase = "";

    [MenuItem("Scurry/Full Playthrough Test")]
    public static void Run()
    {
        if (running) return;
        running = true;
        frameCount = 0;
        deckFilled = false;
        deckConfirmed = false;
        heroesStaged = 0;
        deployEndPhaseClicked = false;
        continueClicks = 0;
        screenshotsTaken = 0;
        lastPhase = "";

        Debug.Log("[FullPlaythroughTest] Starting full playthrough — will advance through multiple turns");

        if (!EditorApplication.isPlaying)
            EditorApplication.isPlaying = true;

        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { frameCount = 0; return; }
        frameCount++;
        if (frameCount % 15 != 0) return; // check ~4x/sec

        var tm = TurnManager.Instance;

        // Stop after turn 3 or 60 seconds
        if ((tm != null && tm.CurrentTurn > 3) || frameCount > 3600)
        {
            Debug.Log($"[FullPlaythroughTest] COMPLETE — turn={tm?.CurrentTurn ?? 0}, screenshots={screenshotsTaken}, continues={continueClicks}");
            EditorApplication.update -= Tick;
            running = false;
            return;
        }

        // Log phase transitions
        if (tm != null)
        {
            string currentPhase = $"T{tm.CurrentTurn}_{tm.CurrentPhase}";
            if (currentPhase != lastPhase)
            {
                Debug.Log($"[FullPlaythroughTest] Phase change: {lastPhase} -> {currentPhase}");
                lastPhase = currentPhase;
            }
        }

        // 1. MainMenu — click NEW RUN
        var mainMenu = Object.FindAnyObjectByType<MainMenuManager>();
        if (mainMenu != null)
        {
            var btn = GameObject.Find("NewRunButton")?.GetComponent<Button>();
            if (btn != null) { btn.onClick.Invoke(); Debug.Log("[FullPlaythroughTest] Clicked NEW RUN"); }
            return;
        }

        // 2. DeckConstruction — auto-fill and confirm
        var dcm = Object.FindAnyObjectByType<DeckConstructionManager>();
        if (dcm != null)
        {
            if (!deckFilled)
            {
                var pool = dcm.CardPool;
                if (pool != null)
                    foreach (var card in pool)
                    {
                        if (dcm.IsDeckValid) break;
                        while (dcm.CanAddCard(card)) { dcm.AddCard(card); if (dcm.IsDeckValid) break; }
                    }
                deckFilled = true;
                Debug.Log($"[FullPlaythroughTest] Deck filled: size={dcm.DeckSize}");
                return;
            }
            if (!deckConfirmed && dcm.IsDeckValid) { dcm.ConfirmDeck(); deckConfirmed = true; return; }
            return;
        }

        // 3. Deploy phase — stage heroes one per tick
        var deployUI = Object.FindAnyObjectByType<DeploymentUI>();
        if (deployUI != null && heroesStaged < 2)
        {
            var dm = DeckManager.Instance ?? Object.FindAnyObjectByType<DeckManager>();
            if (dm != null && dm.AvailableHeroes.Count > 0)
            {
                var hero = dm.AvailableHeroes[0];
                deployUI.OnHeroSelected(hero, 0);
                deployUI.OnDeployClicked();
                heroesStaged++;
                Debug.Log($"[FullPlaythroughTest] Staged hero #{heroesStaged}: {hero.cardName}");
            }
            return;
        }
        if (deployUI != null && heroesStaged >= 2 && !deployEndPhaseClicked)
        {
            var endBtn = GameObject.Find("EndPhaseBtn")?.GetComponent<Button>();
            if (endBtn != null) { endBtn.onClick.Invoke(); deployEndPhaseClicked = true; }
            else if (tm != null) { tm.PlayerEndPhase(); deployEndPhaseClicked = true; }
            Debug.Log("[FullPlaythroughTest] Ended Deploy phase");
            return;
        }

        // 5. On GameMap — set hero targets then click Continue
        if (tm != null && deployEndPhaseClicked)
        {
            // Set hero targets toward first wilderness neighbor of colony
            if (tm.DeployedHeroes != null && tm.DeployedHeroes.Count > 0)
            {
                var mapGraph = tm.MapGraph;
                if (mapGraph != null)
                {
                    int colonyId = mapGraph.ColonyNodeId;
                    var neighbors = mapGraph.GetNeighbors(colonyId);
                    if (neighbors != null && neighbors.Count > 0)
                    {
                        foreach (var hero in tm.DeployedHeroes)
                        {
                            if (hero.IsAlive && hero.isDeployed &&
                                (hero.targetNodeId < 0 || hero.targetNodeId == hero.currentNodeId))
                            {
                                int targetId = neighbors[Mathf.Min(hero.tokenId, neighbors.Count - 1)].nodeId;
                                hero.targetNodeId = targetId;
                                Debug.Log($"[FullPlaythroughTest] Set target for {hero.cardDef.cardName} (tokenId={hero.tokenId}) -> node {targetId}");
                            }
                        }
                    }
                }
            }

            // Click Continue button if visible
            var hud = Object.FindAnyObjectByType<HUDManager>();
            if (hud != null)
            {
                tm.PlayerContinue();
                continueClicks++;
                Debug.Log($"[FullPlaythroughTest] Clicked Continue (#{continueClicks}, phase={tm.CurrentPhase})");
            }
        }

        // Safety
        if (frameCount > 3600)
        {
            Debug.LogWarning("[FullPlaythroughTest] Timeout");
            EditorApplication.update -= Tick;
            running = false;
        }
    }
}
