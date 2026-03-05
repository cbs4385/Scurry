using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;

namespace Scurry.Cards
{
    public class HandManager : MonoBehaviour
    {
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private float cardSpacing = 1.5f;
        [SerializeField] private float handY = -3.5f;

        private List<CardView> handCards = new List<CardView>();

        public System.Action<CardView, Vector3> OnCardDropped;

        private void Awake()
        {
            Debug.Log($"[HandManager] Awake: cardPrefab={cardPrefab?.name ?? "NULL"}, cardSpacing={cardSpacing}, handY={handY}");
        }

        public void AddCardsToHand(List<CardDefinitionSO> cards)
        {
            Debug.Log($"[HandManager] AddCardsToHand: adding {cards.Count} cards, current hand size={handCards.Count}");
            foreach (var card in cards)
            {
                Debug.Log($"[HandManager] AddCardsToHand: instantiating card='{card.cardName}', type={card.cardType}");
                GameObject cardGO = Instantiate(cardPrefab, transform);
                CardView view = cardGO.GetComponent<CardView>();
                if (view == null)
                {
                    Debug.LogError($"[HandManager] AddCardsToHand: cardPrefab has no CardView component!");
                    continue;
                }
                view.Initialize(card, HandleCardDrop);
                handCards.Add(view);
                Debug.Log($"[HandManager] AddCardsToHand: card '{card.cardName}' added to hand (hand size now={handCards.Count})");
            }
            ArrangeHand();
        }

        public void AddCardBackToHand(CardDefinitionSO card)
        {
            Debug.Log($"[HandManager] AddCardBackToHand: returning '{card.cardName}' to hand");
            GameObject cardGO = Instantiate(cardPrefab, transform);
            CardView view = cardGO.GetComponent<CardView>();
            view.Initialize(card, HandleCardDrop);
            handCards.Add(view);
            ArrangeHand();
            Debug.Log($"[HandManager] AddCardBackToHand: hand size now={handCards.Count}");
        }

        public void RemoveCardFromHand(CardView card)
        {
            Debug.Log($"[HandManager] RemoveCardFromHand: removing card='{card?.CardData?.cardName ?? "?"}', hand size before={handCards.Count}");
            handCards.Remove(card);
            Destroy(card.gameObject);
            Debug.Log($"[HandManager] RemoveCardFromHand: hand size after={handCards.Count}");
            ArrangeHand();
        }

        public void ClearHand()
        {
            Debug.Log($"[HandManager] ClearHand: clearing {handCards.Count} cards");
            foreach (var card in handCards)
            {
                if (card != null)
                {
                    Debug.Log($"[HandManager] ClearHand: destroying card='{card.CardData?.cardName ?? "?"}'");
                    Destroy(card.gameObject);
                }
            }
            handCards.Clear();
            Debug.Log("[HandManager] ClearHand: complete");
        }

        private void ArrangeHand()
        {
            float totalWidth = (handCards.Count - 1) * cardSpacing;
            float startX = -totalWidth * 0.5f;
            Debug.Log($"[HandManager] ArrangeHand: count={handCards.Count}, totalWidth={totalWidth}, startX={startX}, handY={handY}");

            for (int i = 0; i < handCards.Count; i++)
            {
                Vector3 pos = new Vector3(startX + i * cardSpacing, handY, 0f);
                handCards[i].SetPosition(pos);
                Debug.Log($"[HandManager] ArrangeHand: card[{i}]='{handCards[i].CardData?.cardName ?? "?"}' placed at {pos}");
            }
        }

        private void HandleCardDrop(CardView card, Vector3 worldPos)
        {
            Debug.Log($"[HandManager] HandleCardDrop: card='{card?.CardData?.cardName ?? "?"}', worldPos={worldPos}, OnCardDropped subscribers={OnCardDropped != null}");
            OnCardDropped?.Invoke(card, worldPos);
        }

        public List<CardView> GetHandCards() => handCards;
        public int CardCount => handCards.Count;
    }
}
