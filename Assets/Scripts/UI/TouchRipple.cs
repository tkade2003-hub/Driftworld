using UnityEngine;
using UnityEngine.UI;

namespace Driftworld
{
    // MECH-001 presentation: while the movement touch is held, a soft ripple
    // sits at the anchor. Nothing is drawn when the thumb is up. The ripple
    // texture is generated at runtime so the greybox needs no art assets.
    [RequireComponent(typeof(Image))]
    public class TouchRipple : MonoBehaviour
    {
        public DragInput input;

        private Image image;
        private RectTransform rect;
        private float alpha;

        private void Awake()
        {
            image = GetComponent<Image>();
            rect = GetComponent<RectTransform>();
            image.sprite = MakeRingSprite();
            image.raycastTarget = false;
            SetAlpha(0f);
        }

        private static Sprite MakeRingSprite()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    float band = Mathf.SmoothStep(0.62f, 0.78f, d) * (1f - Mathf.SmoothStep(0.9f, 1f, d));
                    float core = (1f - Mathf.SmoothStep(0f, 0.2f, d)) * 0.5f;
                    pixels[y * size + x] = new Color(0.65f, 0.95f, 1f, Mathf.Clamp01(band + core));
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void Update()
        {
            if (input == null) return;

            float target = 0f;
            if (input.Active)
            {
                rect.position = input.Anchor;
                float pulse = 1f + 0.05f * Mathf.Sin(Time.time * 4f);
                rect.localScale = Vector3.one * pulse;
                target = 0.55f;
            }

            float step = Time.deltaTime / Mathf.Max(Tuning.Movement.ripple_fade_s, 0.01f);
            alpha = Mathf.MoveTowards(alpha, target, step * 0.55f);
            SetAlpha(alpha);
        }

        private void SetAlpha(float a)
        {
            Color c = image.color;
            c.a = a;
            image.color = c;
        }
    }
}
