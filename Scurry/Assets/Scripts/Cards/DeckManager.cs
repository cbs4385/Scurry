using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scurry.Data;
using Scurry.Core;

namespace Scurry.Cards
{
    public class DeckManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────
        private static DeckManager _instance;
        public static DeckManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Log("[DeckManager] Awake: duplicate instance — destroying self");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Debug.Log("[DeckManager] Awake: singleton set, DontDestroyOnLoad applied");
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ── Card pools (v2.0 state tracking) ─────────────────────────────
        private List<CardDefinitionSO> availableHeroes = new List<CardDefinitionSO>();
        private List<CardDefinitionSO> deployedHeroes = new List<CardDefinitionSO>();
        private List<CardDefinitionSO> injuredHeroes = new List<CardDefinitionSO>();
        private List<CardDefinitionSO> availableEquipment = new List<CardDefinitionSO>();
        private List<CardDefinitionSO> deployedEquipment = new List<CardDefinitionSO>();
        private List<CardDefinitionSO> availableTactical = new List<CardDefinitionSO>();
        private List<CardDefinitionSO> usedTactical = new List<CardDefinitionSO>(); // removed from game
        private List<ColonyCardDefinitionSO> availableColonyCards = new List<ColonyCardDefinitionSO>();
        private List<ColonyCardDefinitionSO> playedColonyCards = new List<ColonyCardDefinitionSO>();

        // ── Public properties ────────────────────────────────────────────
        public IReadOnlyList<CardDefinitionSO> AvailableHeroes => availableHeroes;
        public IReadOnlyList<CardDefinitionSO> DeployedHeroes => deployedHeroes;
        public IReadOnlyList<CardDefinitionSO> InjuredHeroes => injuredHeroes;
        public IReadOnlyList<CardDefinitionSO> AvailableEquipment => availableEquipment;
        public IReadOnlyList<CardDefinitionSO> DeployedEquipment => deployedEquipment;
        public IReadOnlyList<CardDefinitionSO> AvailableTactical => availableTactical;
        public IReadOnlyList<CardDefinitionSO> UsedTactical => usedTactical;
        public IReadOnlyList<ColonyCardDefinitionSO> AvailableColonyCards => availableColonyCards;
        public IReadOnlyList<ColonyCardDefinitionSO> PlayedColonyCards => playedColonyCards;
        public int TotalDeckSize { get; private set; }

        // ── Initialization ───────────────────────────────────────────────

        /// <summary>
        /// Initializes the deck from a constructed deck. Sorts cards into hero/equipment/tactical
        /// pools based on cardType. Colony cards are handled separately.
        /// </summary>
        public void InitializeDeck(List<CardDefinitionSO> cards, List<ColonyCardDefinitionSO> colonyCards)
        {
            Debug.Log($"[DeckManager] InitializeDeck: initializing v2.0 deck " +
                      $"(cards={cards?.Count ?? 0}, colonyCards={colonyCards?.Count ?? 0})");

            // Clear all pools
            availableHeroes.Clear();
            deployedHeroes.Clear();
            injuredHeroes.Clear();
            availableEquipment.Clear();
            deployedEquipment.Clear();
            availableTactical.Clear();
            usedTactical.Clear();
            availableColonyCards.Clear();
            playedColonyCards.Clear();

            int totalCards = 0;

            // Sort cards into pools
            if (cards != null)
            {
                foreach (var card in cards)
                {
                    if (card == null)
                    {
                        Debug.LogWarning("[DeckManager] InitializeDeck: null card in list — skipping");
                        continue;
                    }

                    totalCards++;

                    switch (card.cardType)
                    {
                        case CardType.Hero:
                            availableHeroes.Add(card);
                            Debug.Log($"[DeckManager] InitializeDeck: added hero " +
                                      $"(name={card.cardName}, combat={card.combat}, move={card.move}, " +
                                      $"hp={card.hp}, carry={card.carry})");
                            break;

                        case CardType.Equipment:
                            availableEquipment.Add(card);
                            Debug.Log($"[DeckManager] InitializeDeck: added equipment " +
                                      $"(name={card.cardName}, slot={card.equipmentSlot}, " +
                                      $"effectValue1={card.effectValue1})");
                            break;

                        case CardType.Tactical:
                            availableTactical.Add(card);
                            Debug.Log($"[DeckManager] InitializeDeck: added tactical " +
                                      $"(name={card.cardName}, type={card.tacticalType}, " +
                                      $"effectValue1={card.effectValue1})");
                            break;

                        default:
                            Debug.LogWarning($"[DeckManager] InitializeDeck: unexpected card type " +
                                             $"(name={card.cardName}, type={card.cardType}) — adding to heroes as fallback");
                            availableHeroes.Add(card);
                            break;
                    }
                }
            }

            // Add colony cards
            if (colonyCards != null)
            {
                foreach (var colonyCard in colonyCards)
                {
                    if (colonyCard == null)
                    {
                        Debug.LogWarning("[DeckManager] InitializeDeck: null colony card — skipping");
                        continue;
                    }

                    totalCards++;
                    availableColonyCards.Add(colonyCard);
                    Debug.Log($"[DeckManager] InitializeDeck: added colony card " +
                              $"(name={colonyCard.cardName}, tier={colonyCard.colonyTier}, " +
                              $"effect={colonyCard.colonyEffect}, effectValue={colonyCard.effectValue})");
                }
            }

            TotalDeckSize = totalCards;

            Debug.Log($"[DeckManager] InitializeDeck: complete " +
                      $"(totalDeckSize={TotalDeckSize}, heroes={availableHeroes.Count}, " +
                      $"equipment={availableEquipment.Count}, tactical={availableTactical.Count}, " +
                      $"colony={availableColonyCards.Count})");
        }

        // ── Hero management ──────────────────────────────────────────────

        /// <summary>
        /// Moves a hero from available to deployed pool.
        /// </summary>
        public void DeployHero(CardDefinitionSO hero)
        {
            Debug.Log($"[DeckManager] DeployHero: deploying hero " +
                      $"(name={hero?.cardName ?? "NULL"})");

            if (hero == null)
            {
                Debug.LogWarning("[DeckManager] DeployHero: hero is null — skipping");
                return;
            }

            if (!availableHeroes.Remove(hero))
            {
                Debug.LogWarning($"[DeckManager] DeployHero: hero not found in available pool " +
                                 $"(name={hero.cardName})");
                return;
            }

            deployedHeroes.Add(hero);
            Debug.Log($"[DeckManager] DeployHero: hero deployed " +
                      $"(name={hero.cardName}, availableHeroes={availableHeroes.Count}, " +
                      $"deployedHeroes={deployedHeroes.Count})");
        }

        /// <summary>
        /// Returns a hero from deployed to available (or injured if isInjured).
        /// </summary>
        public void ReturnHero(CardDefinitionSO hero)
        {
            Debug.Log($"[DeckManager] ReturnHero: returning hero " +
                      $"(name={hero?.cardName ?? "NULL"})");

            if (hero == null)
            {
                Debug.LogWarning("[DeckManager] ReturnHero: hero is null — skipping");
                return;
            }

            if (!deployedHeroes.Remove(hero))
            {
                Debug.LogWarning($"[DeckManager] ReturnHero: hero not found in deployed pool " +
                                 $"(name={hero.cardName})");
                return;
            }

            availableHeroes.Add(hero);
            Debug.Log($"[DeckManager] ReturnHero: hero returned to available " +
                      $"(name={hero.cardName}, availableHeroes={availableHeroes.Count}, " +
                      $"deployedHeroes={deployedHeroes.Count})");
        }

        /// <summary>
        /// Moves a hero to the injured pool. Removes from deployed if present.
        /// </summary>
        public void InjureHero(CardDefinitionSO hero)
        {
            Debug.Log($"[DeckManager] InjureHero: injuring hero " +
                      $"(name={hero?.cardName ?? "NULL"})");

            if (hero == null)
            {
                Debug.LogWarning("[DeckManager] InjureHero: hero is null — skipping");
                return;
            }

            // Try to remove from deployed first, then available
            bool removed = deployedHeroes.Remove(hero);
            if (!removed)
            {
                removed = availableHeroes.Remove(hero);
            }

            if (!removed)
            {
                Debug.LogWarning($"[DeckManager] InjureHero: hero not found in any pool " +
                                 $"(name={hero.cardName})");
                return;
            }

            injuredHeroes.Add(hero);
            Debug.Log($"[DeckManager] InjureHero: hero moved to injured " +
                      $"(name={hero.cardName}, injuredHeroes={injuredHeroes.Count}, " +
                      $"availableHeroes={availableHeroes.Count}, deployedHeroes={deployedHeroes.Count})");
        }

        /// <summary>
        /// Recovers a hero from injured to available pool.
        /// </summary>
        public void RecoverHero(CardDefinitionSO hero)
        {
            Debug.Log($"[DeckManager] RecoverHero: recovering hero " +
                      $"(name={hero?.cardName ?? "NULL"})");

            if (hero == null)
            {
                Debug.LogWarning("[DeckManager] RecoverHero: hero is null — skipping");
                return;
            }

            if (!injuredHeroes.Remove(hero))
            {
                Debug.LogWarning($"[DeckManager] RecoverHero: hero not found in injured pool " +
                                 $"(name={hero.cardName})");
                return;
            }

            availableHeroes.Add(hero);
            Debug.Log($"[DeckManager] RecoverHero: hero recovered to available " +
                      $"(name={hero.cardName}, availableHeroes={availableHeroes.Count}, " +
                      $"injuredHeroes={injuredHeroes.Count})");
        }

        // ── Equipment management ─────────────────────────────────────────

        /// <summary>
        /// Moves equipment from available to deployed pool.
        /// </summary>
        public void AttachEquipment(CardDefinitionSO equip)
        {
            Debug.Log($"[DeckManager] AttachEquipment: attaching equipment " +
                      $"(name={equip?.cardName ?? "NULL"})");

            if (equip == null)
            {
                Debug.LogWarning("[DeckManager] AttachEquipment: equip is null — skipping");
                return;
            }

            if (!availableEquipment.Remove(equip))
            {
                Debug.LogWarning($"[DeckManager] AttachEquipment: equipment not found in available pool " +
                                 $"(name={equip.cardName})");
                return;
            }

            deployedEquipment.Add(equip);
            Debug.Log($"[DeckManager] AttachEquipment: equipment deployed " +
                      $"(name={equip.cardName}, slot={equip.equipmentSlot}, " +
                      $"availableEquipment={availableEquipment.Count}, " +
                      $"deployedEquipment={deployedEquipment.Count})");
        }

        /// <summary>
        /// Returns equipment from deployed to available pool.
        /// </summary>
        public void ReturnEquipment(CardDefinitionSO equip)
        {
            Debug.Log($"[DeckManager] ReturnEquipment: returning equipment " +
                      $"(name={equip?.cardName ?? "NULL"})");

            if (equip == null)
            {
                Debug.LogWarning("[DeckManager] ReturnEquipment: equip is null — skipping");
                return;
            }

            if (!deployedEquipment.Remove(equip))
            {
                Debug.LogWarning($"[DeckManager] ReturnEquipment: equipment not found in deployed pool " +
                                 $"(name={equip.cardName})");
                return;
            }

            availableEquipment.Add(equip);
            Debug.Log($"[DeckManager] ReturnEquipment: equipment returned " +
                      $"(name={equip.cardName}, availableEquipment={availableEquipment.Count}, " +
                      $"deployedEquipment={deployedEquipment.Count})");
        }

        // ── Colony card management ───────────────────────────────────────

        /// <summary>
        /// Moves a colony card from available to played pool. Permanent placement.
        /// </summary>
        public void PlayColonyCard(ColonyCardDefinitionSO card)
        {
            Debug.Log($"[DeckManager] PlayColonyCard: playing colony card " +
                      $"(name={card?.cardName ?? "NULL"})");

            if (card == null)
            {
                Debug.LogWarning("[DeckManager] PlayColonyCard: card is null — skipping");
                return;
            }

            if (!availableColonyCards.Remove(card))
            {
                Debug.LogWarning($"[DeckManager] PlayColonyCard: card not found in available pool " +
                                 $"(name={card.cardName})");
                return;
            }

            playedColonyCards.Add(card);
            Debug.Log($"[DeckManager] PlayColonyCard: colony card played " +
                      $"(name={card.cardName}, tier={card.colonyTier}, effect={card.colonyEffect}, " +
                      $"availableColony={availableColonyCards.Count}, playedColony={playedColonyCards.Count})");
        }

        // ── Tactical card management ─────────────────────────────────────

        /// <summary>
        /// Uses a tactical card. Moves from available to used (permanently removed from game).
        /// </summary>
        public void UseTacticalCard(CardDefinitionSO card)
        {
            Debug.Log($"[DeckManager] UseTacticalCard: using tactical card " +
                      $"(name={card?.cardName ?? "NULL"})");

            if (card == null)
            {
                Debug.LogWarning("[DeckManager] UseTacticalCard: card is null — skipping");
                return;
            }

            if (!availableTactical.Remove(card))
            {
                Debug.LogWarning($"[DeckManager] UseTacticalCard: card not found in available pool " +
                                 $"(name={card.cardName})");
                return;
            }

            usedTactical.Add(card);
            Debug.Log($"[DeckManager] UseTacticalCard: tactical card used (permanently removed) " +
                      $"(name={card.cardName}, type={card.tacticalType}, " +
                      $"availableTactical={availableTactical.Count}, usedTactical={usedTactical.Count})");
        }

        // ── Mid-run card acquisition (Phase 3) ───────────────────────────

        /// <summary>
        /// Adds a reward card to the appropriate pool. Used for mid-run card rewards.
        /// </summary>
        public void AddCardToPool(CardDefinitionSO card)
        {
            Debug.Log($"[DeckManager] AddCardToPool: adding card " +
                      $"(name={card?.cardName ?? "NULL"}, type={card?.cardType})");

            if (card == null)
            {
                Debug.LogWarning("[DeckManager] AddCardToPool: card is null — skipping");
                return;
            }

            TotalDeckSize++;

            switch (card.cardType)
            {
                case CardType.Hero:
                    availableHeroes.Add(card);
                    Debug.Log($"[DeckManager] AddCardToPool: added hero to available pool (name={card.cardName}, availableHeroes={availableHeroes.Count})");
                    break;
                case CardType.Equipment:
                    availableEquipment.Add(card);
                    Debug.Log($"[DeckManager] AddCardToPool: added equipment to available pool (name={card.cardName}, availableEquipment={availableEquipment.Count})");
                    break;
                case CardType.Tactical:
                    availableTactical.Add(card);
                    Debug.Log($"[DeckManager] AddCardToPool: added tactical to available pool (name={card.cardName}, availableTactical={availableTactical.Count})");
                    break;
                default:
                    Debug.LogWarning($"[DeckManager] AddCardToPool: unexpected card type {card.cardType} for '{card.cardName}' — adding to heroes as fallback");
                    availableHeroes.Add(card);
                    break;
            }
        }

        /// <summary>
        /// Adds a colony card to the available pool. Used for mid-run card rewards.
        /// </summary>
        public void AddColonyCardToPool(ColonyCardDefinitionSO card)
        {
            Debug.Log($"[DeckManager] AddColonyCardToPool: adding colony card " +
                      $"(name={card?.cardName ?? "NULL"})");

            if (card == null)
            {
                Debug.LogWarning("[DeckManager] AddColonyCardToPool: card is null — skipping");
                return;
            }

            TotalDeckSize++;
            availableColonyCards.Add(card);
            Debug.Log($"[DeckManager] AddColonyCardToPool: added colony card (name={card.cardName}, availableColonyCards={availableColonyCards.Count})");
        }

        // ── Utility ──────────────────────────────────────────────────────

        /// <summary>
        /// Logs the current state of all card pools for debugging.
        /// </summary>
        public void LogDeckState()
        {
            Debug.Log($"[DeckManager] LogDeckState: === DECK STATE ===");
            Debug.Log($"[DeckManager] LogDeckState: TotalDeckSize={TotalDeckSize}");
            Debug.Log($"[DeckManager] LogDeckState: Heroes — available={availableHeroes.Count}, " +
                      $"deployed={deployedHeroes.Count}, injured={injuredHeroes.Count}");
            Debug.Log($"[DeckManager] LogDeckState: Equipment — available={availableEquipment.Count}, " +
                      $"deployed={deployedEquipment.Count}");
            Debug.Log($"[DeckManager] LogDeckState: Tactical — available={availableTactical.Count}, " +
                      $"used={usedTactical.Count}");
            Debug.Log($"[DeckManager] LogDeckState: Colony — available={availableColonyCards.Count}, " +
                      $"played={playedColonyCards.Count}");

            foreach (var h in availableHeroes)
                Debug.Log($"[DeckManager] LogDeckState: availableHero — {h.cardName}");
            foreach (var h in deployedHeroes)
                Debug.Log($"[DeckManager] LogDeckState: deployedHero — {h.cardName}");
            foreach (var h in injuredHeroes)
                Debug.Log($"[DeckManager] LogDeckState: injuredHero — {h.cardName}");
            foreach (var e in availableEquipment)
                Debug.Log($"[DeckManager] LogDeckState: availableEquip — {e.cardName} ({e.equipmentSlot})");
            foreach (var e in deployedEquipment)
                Debug.Log($"[DeckManager] LogDeckState: deployedEquip — {e.cardName} ({e.equipmentSlot})");
            foreach (var t in availableTactical)
                Debug.Log($"[DeckManager] LogDeckState: availableTactical — {t.cardName} ({t.tacticalType})");
            foreach (var t in usedTactical)
                Debug.Log($"[DeckManager] LogDeckState: usedTactical — {t.cardName} ({t.tacticalType})");
            foreach (var c in availableColonyCards)
                Debug.Log($"[DeckManager] LogDeckState: availableColony — {c.cardName} ({c.colonyEffect})");
            foreach (var c in playedColonyCards)
                Debug.Log($"[DeckManager] LogDeckState: playedColony — {c.cardName} ({c.colonyEffect})");
        }
    }
}
