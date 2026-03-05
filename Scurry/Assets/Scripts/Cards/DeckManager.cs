using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Core;

namespace Scurry.Cards
{
    public class DeckManager : MonoBehaviour
    {
        [SerializeField] private CardDefinitionSO[] allCards;
        [SerializeField] private int handSize = 5;

        private List<CardDefinitionSO> drawPile = new List<CardDefinitionSO>();
        private List<CardDefinitionSO> discardPile = new List<CardDefinitionSO>();

        private void Awake()
        {
            Debug.Log($"[DeckManager] Awake: allCards count={allCards?.Length ?? 0}, handSize={handSize}");
            if (allCards != null)
            {
                for (int i = 0; i < allCards.Length; i++)
                    Debug.Log($"[DeckManager] Awake: allCards[{i}]={allCards[i]?.cardName ?? "NULL"}");
            }
        }

        public void InitializeDeck()
        {
            Debug.Log($"[DeckManager] InitializeDeck: clearing piles and loading {allCards?.Length ?? 0} cards");
            drawPile.Clear();
            discardPile.Clear();
            drawPile.AddRange(allCards);
            Shuffle(drawPile);
            Debug.Log($"[DeckManager] InitializeDeck: drawPile={drawPile.Count}, order=[{string.Join(", ", drawPile.ConvertAll(c => c.cardName))}]");
        }

        public void InitializeDeck(List<CardDefinitionSO> selectedCards)
        {
            Debug.Log($"[DeckManager] InitializeDeck(custom): clearing piles and loading {selectedCards.Count} selected cards");
            drawPile.Clear();
            discardPile.Clear();
            drawPile.AddRange(selectedCards);
            Shuffle(drawPile);
            Debug.Log($"[DeckManager] InitializeDeck(custom): drawPile={drawPile.Count}, order=[{string.Join(", ", drawPile.ConvertAll(c => c.cardName))}]");
        }

        public List<CardDefinitionSO> DrawCards(int count)
        {
            Debug.Log($"[DeckManager] DrawCards: requested={count}, drawPile={drawPile.Count}, discardPile={discardPile.Count}");
            var drawn = new List<CardDefinitionSO>();
            for (int i = 0; i < count; i++)
            {
                if (drawPile.Count == 0)
                {
                    if (discardPile.Count == 0)
                    {
                        Debug.LogWarning("[DeckManager] DrawCards: both piles empty — cannot draw more");
                        break;
                    }
                    ReshuffleDiscard();
                }
                if (drawPile.Count > 0)
                {
                    var card = drawPile[0];
                    drawPile.RemoveAt(0);
                    drawn.Add(card);
                    Debug.Log($"[DeckManager] DrawCards: drew '{card.cardName}' (drawPile remaining={drawPile.Count})");
                    EventBus.OnCardDrawn?.Invoke(card);
                }
            }
            Debug.Log($"[DeckManager] DrawCards: total drawn={drawn.Count}, names=[{string.Join(", ", drawn.ConvertAll(c => c.cardName))}]");
            return drawn;
        }

        public List<CardDefinitionSO> DrawHand()
        {
            Debug.Log($"[DeckManager] DrawHand: drawing {handSize} cards");
            return DrawCards(handSize);
        }

        public void DiscardCard(CardDefinitionSO card)
        {
            Debug.Log($"[DeckManager] DiscardCard: '{card?.cardName ?? "NULL"}' (discardPile size now={discardPile.Count + 1})");
            discardPile.Add(card);
        }

        public void ReturnToDeck(CardDefinitionSO card)
        {
            Debug.Log($"[DeckManager] ReturnToDeck: '{card?.cardName ?? "NULL"}' (drawPile size now={drawPile.Count + 1})");
            drawPile.Add(card);
        }

        private void ReshuffleDiscard()
        {
            Debug.Log($"[DeckManager] ReshuffleDiscard: moving {discardPile.Count} cards from discard to draw pile");
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
            Debug.Log($"[DeckManager] ReshuffleDiscard: drawPile now={drawPile.Count}");
        }

        private void Shuffle(List<CardDefinitionSO> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public void RestorePiles(List<CardDefinitionSO> draw, List<CardDefinitionSO> discard)
        {
            drawPile.Clear();
            discardPile.Clear();
            drawPile.AddRange(draw);
            discardPile.AddRange(discard);
            Debug.Log($"[DeckManager] RestorePiles: drawPile={drawPile.Count}, discardPile={discardPile.Count}");
        }

        public CardDefinitionSO FindCardByName(string cardName)
        {
            foreach (var card in allCards)
            {
                if (card.cardName == cardName)
                    return card;
            }
            Debug.LogWarning($"[DeckManager] FindCardByName: card '{cardName}' not found in allCards");
            return null;
        }

        public bool HasHeroCards()
        {
            foreach (var card in drawPile)
                if (card.cardType == CardType.Hero) { Debug.Log($"[DeckManager] HasHeroCards: found '{card.cardName}' in drawPile"); return true; }
            foreach (var card in discardPile)
                if (card.cardType == CardType.Hero) { Debug.Log($"[DeckManager] HasHeroCards: found '{card.cardName}' in discardPile"); return true; }
            Debug.Log("[DeckManager] HasHeroCards: no hero cards in deck");
            return false;
        }

        public bool CanDraw()
        {
            bool canDraw = drawPile.Count > 0 || discardPile.Count > 0;
            Debug.Log($"[DeckManager] CanDraw: drawPile={drawPile.Count}, discardPile={discardPile.Count}, canDraw={canDraw}");
            return canDraw;
        }

        public int DrawPileCount => drawPile.Count;
        public int DiscardPileCount => discardPile.Count;
        public int HandSize => handSize;
        public CardDefinitionSO[] AllCards => allCards;
        public List<CardDefinitionSO> DrawPile => drawPile;
        public List<CardDefinitionSO> DiscardPile => discardPile;
    }
}
