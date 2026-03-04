using UnityEngine;

namespace Scurry.Core
{
    public static class SpriteHelper
    {
        private static Sprite cachedWhiteSprite;

        public static Sprite GetWhiteSprite()
        {
            if (cachedWhiteSprite == null)
            {
                Debug.Log("[SpriteHelper] GetWhiteSprite: creating new 4x4 white texture (PPU=4)");
                var tex = new Texture2D(4, 4);
                var pixels = new Color[16];
                for (int i = 0; i < 16; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.Apply();
                cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                Debug.Log($"[SpriteHelper] GetWhiteSprite: sprite created (sprite={cachedWhiteSprite}, texture={tex})");
            }
            return cachedWhiteSprite;
        }

        public static void EnsureSprite(SpriteRenderer sr)
        {
            if (sr == null)
            {
                Debug.LogWarning("[SpriteHelper] EnsureSprite: SpriteRenderer is null — cannot assign sprite");
                return;
            }
            if (sr.sprite == null)
            {
                Debug.Log($"[SpriteHelper] EnsureSprite: sprite was null on '{sr.gameObject.name}', assigning white sprite");
                sr.sprite = GetWhiteSprite();
            }
        }
    }
}
