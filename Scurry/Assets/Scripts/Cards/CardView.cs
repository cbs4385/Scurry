using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Scurry.Data;
using Scurry.Core;

namespace Scurry.Cards
{
    public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private SpriteRenderer cardBackground;
        private TextMeshPro nameText;
        private TextMeshPro statsText;

        public CardDefinitionSO CardData { get; private set; }
        public bool IsDragging { get; private set; }

        private Vector3 originalPosition;
        private int originalSortingOrder;
        private Camera mainCamera;
        private System.Action<CardView, Vector3> onDropCallback;

        private void Awake()
        {
            Debug.Log($"[CardView] Awake: gameObject='{gameObject.name}', instanceID={gameObject.GetInstanceID()}");
            EnsureVisuals();
        }

        private void EnsureVisuals()
        {
            Debug.Log($"[CardView] EnsureVisuals: starting for '{gameObject.name}'");

            // Ensure SpriteRenderer exists and has a sprite
            cardBackground = GetComponent<SpriteRenderer>();
            if (cardBackground == null)
            {
                Debug.Log($"[CardView] EnsureVisuals: no SpriteRenderer found, adding one");
                cardBackground = gameObject.AddComponent<SpriteRenderer>();
            }

            if (cardBackground.sprite == null)
            {
                Debug.Log($"[CardView] EnsureVisuals: sprite is null, creating white sprite");
                cardBackground.sprite = CreateWhiteSprite();
            }

            cardBackground.sortingOrder = 10;

            // Ensure BoxCollider2D for raycasting
            var col = GetComponent<BoxCollider2D>();
            if (col == null)
            {
                Debug.Log($"[CardView] EnsureVisuals: no BoxCollider2D found, adding one");
                col = gameObject.AddComponent<BoxCollider2D>();
            }

            // Create text children if they don't exist
            var existingTexts = GetComponentsInChildren<TextMeshPro>();
            Debug.Log($"[CardView] EnsureVisuals: found {existingTexts.Length} existing TextMeshPro children");
            if (existingTexts.Length >= 2)
            {
                nameText = existingTexts[0];
                statsText = existingTexts[1];
            }
            else
            {
                nameText = CreateTextChild("NameText", new Vector3(0, 0.3f, -0.01f));
                statsText = CreateTextChild("StatsText", new Vector3(0, -0.15f, -0.01f));
            }
            Debug.Log($"[CardView] EnsureVisuals: complete — cardBackground={cardBackground != null}, nameText={nameText != null}, statsText={statsText != null}, collider={col != null}");
        }

        private Sprite CreateWhiteSprite()
        {
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }

        private TextMeshPro CreateTextChild(string name, Vector3 localPos)
        {
            Debug.Log($"[CardView] CreateTextChild: name='{name}', localPos={localPos}, parentScale={transform.lossyScale}");
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            tmp.sortingOrder = 11;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(2, 1);

            return tmp;
        }

        public void Initialize(CardDefinitionSO card, System.Action<CardView, Vector3> onDrop)
        {
            Debug.Log($"[CardView] Initialize: card='{card?.cardName ?? "NULL"}', type={card?.cardType}, onDrop={onDrop != null}");
            CardData = card;
            onDropCallback = onDrop;

            mainCamera = Camera.main;
            Debug.Log($"[CardView] Initialize: Camera.main={mainCamera?.name ?? "NULL"} (tag={mainCamera?.tag ?? "N/A"})");

            if (mainCamera == null)
            {
                Debug.LogError($"[CardView] Initialize: Camera.main is NULL! Drag-and-drop will fail. Ensure camera has 'MainCamera' tag.");
            }

            if (cardBackground != null)
            {
                cardBackground.color = card.placeholderColor;
                Debug.Log($"[CardView] Initialize: set background color={card.placeholderColor}");
            }
            else
            {
                Debug.LogWarning("[CardView] Initialize: cardBackground is null — cannot set color");
            }

            if (nameText != null)
            {
                string displayName = !string.IsNullOrEmpty(card.localizationKey) ? Loc.Get(card.localizationKey + ".name") : card.cardName;
                nameText.text = displayName;
                Debug.Log($"[CardView] Initialize: set nameText='{displayName}' (locKey='{card.localizationKey}')");
            }
            else
            {
                Debug.LogWarning("[CardView] Initialize: nameText is null — cannot set name");
            }

            if (statsText != null)
            {
                if (card.cardType == CardType.Hero)
                    statsText.text = Loc.Format("card.stat.hero", card.movement, card.combat, card.carryCapacity);
                else
                    statsText.text = Loc.Format("card.stat.resource", card.resourceType, card.value);
                Debug.Log($"[CardView] Initialize: set statsText='{statsText.text}'");
            }
            else
            {
                Debug.LogWarning("[CardView] Initialize: statsText is null — cannot set stats");
            }
        }

        public void SetPosition(Vector3 pos)
        {
            Debug.Log($"[CardView] SetPosition: card='{CardData?.cardName ?? "?"}', pos={pos}");
            transform.position = pos;
            originalPosition = pos;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log($"[CardView] OnPointerEnter: card='{CardData?.cardName ?? "?"}', isDragging={IsDragging}");
            if (!IsDragging)
                transform.position = originalPosition + Vector3.up * 0.3f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log($"[CardView] OnPointerExit: card='{CardData?.cardName ?? "?"}', isDragging={IsDragging}");
            if (!IsDragging)
                transform.position = originalPosition;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log($"[CardView] OnBeginDrag: card='{CardData?.cardName ?? "?"}', mainCamera={mainCamera?.name ?? "NULL"}, position={transform.position}");
            IsDragging = true;
            originalSortingOrder = cardBackground != null ? cardBackground.sortingOrder : 0;
            if (cardBackground != null)
                cardBackground.sortingOrder = 100;
            if (nameText != null)
                nameText.sortingOrder = 101;
            if (statsText != null)
                statsText.sortingOrder = 101;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (mainCamera == null)
            {
                Debug.LogError($"[CardView] OnDrag: mainCamera is NULL — cannot convert screen to world. Attempting recovery via Camera.main...");
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("[CardView] OnDrag: Camera.main is still NULL. Cannot drag.");
                    return;
                }
                Debug.Log($"[CardView] OnDrag: recovered camera='{mainCamera.name}'");
            }

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(eventData.position);
            worldPos.z = 0;
            transform.position = worldPos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log($"[CardView] OnEndDrag: card='{CardData?.cardName ?? "?"}', mainCamera={mainCamera?.name ?? "NULL"}, screenPos={eventData.position}");
            IsDragging = false;
            if (cardBackground != null)
                cardBackground.sortingOrder = originalSortingOrder;
            if (nameText != null)
                nameText.sortingOrder = originalSortingOrder + 1;
            if (statsText != null)
                statsText.sortingOrder = originalSortingOrder + 1;

            if (mainCamera == null)
            {
                Debug.LogError("[CardView] OnEndDrag: mainCamera is NULL — attempting recovery via Camera.main...");
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("[CardView] OnEndDrag: Camera.main is still NULL. Snapping back.");
                    SnapBack();
                    return;
                }
                Debug.Log($"[CardView] OnEndDrag: recovered camera='{mainCamera.name}'");
            }

            Vector3 dropWorldPos = mainCamera.ScreenToWorldPoint(eventData.position);
            dropWorldPos.z = 0;
            Debug.Log($"[CardView] OnEndDrag: dropWorldPos={dropWorldPos}, invoking onDropCallback={onDropCallback != null}");
            onDropCallback?.Invoke(this, dropWorldPos);
        }

        public void SnapBack()
        {
            Debug.Log($"[CardView] SnapBack: card='{CardData?.cardName ?? "?"}', returning to originalPosition={originalPosition}");
            transform.position = originalPosition;
        }
    }
}
