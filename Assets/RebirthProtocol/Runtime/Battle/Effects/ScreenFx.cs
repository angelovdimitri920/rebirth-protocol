using UnityEngine;
using UnityEngine.UI;

namespace RebirthProtocol.Battle.Effects
{
    // Screen-space post effects built on the UI Canvas (whose shader is
    // always present in a build, so alpha transparency is safe here where it
    // is risky for world materials): an event-driven full-screen flash, a
    // subtle animated film grain ("visual noise"), and a static vignette for
    // a grittier used-future look. All code-built, no assets.
    public sealed class ScreenFx : MonoBehaviour
    {
        private const int GrainFrames = 6;
        private const int GrainSize = 96;

        private Image _flash;
        private Image _vignette;
        private RawImage _grain;
        private Texture2D[] _grainTextures;
        private int _grainIndex;
        private float _grainSwapTimer;
        private Color _flashColor;
        private float _flashAlpha;

        public void Init()
        {
            var canvasGo = new GameObject("ScreenFx Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5; // above the HUD (0), below the hangar (10)
            canvasGo.AddComponent<CanvasScaler>();

            _vignette = FullScreen(canvasGo.transform, "Vignette");
            _vignette.sprite = BuildVignetteSprite();
            _vignette.color = new Color(1f, 1f, 1f, 0.85f);
            _vignette.raycastTarget = false;

            _grainTextures = BuildGrainTextures();
            var grainGo = new GameObject("Grain");
            grainGo.transform.SetParent(canvasGo.transform, false);
            _grain = grainGo.AddComponent<RawImage>();
            StretchFull(_grain.rectTransform);
            _grain.texture = _grainTextures[0];
            _grain.uvRect = new Rect(0f, 0f, 12f, 8f); // tile the small noise texture
            _grain.color = new Color(1f, 1f, 1f, 0.05f);
            _grain.raycastTarget = false;

            _flash = FullScreen(canvasGo.transform, "Flash");
            _flash.color = new Color(1f, 1f, 1f, 0f);
            _flash.raycastTarget = false;
        }

        public void Flash(Color color, float intensity)
        {
            // Keep the strongest flash currently fading out.
            if (intensity >= _flashAlpha)
            {
                _flashColor = color;
                _flashAlpha = Mathf.Clamp01(intensity);
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime;

            if (_flashAlpha > 0f)
            {
                _flashAlpha = Mathf.Max(0f, _flashAlpha - dt * 3.2f);
                _flash.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, _flashAlpha);
            }

            // Flipbook the pre-generated grain frames + jitter the tiling
            // offset so the noise reads as animated without per-frame alloc.
            _grainSwapTimer -= dt;
            if (_grainSwapTimer <= 0f)
            {
                _grainSwapTimer = 0.05f;
                _grainIndex = (_grainIndex + 1) % _grainTextures.Length;
                _grain.texture = _grainTextures[_grainIndex];
                _grain.uvRect = new Rect(Random.value, Random.value, 12f, 8f);
            }
        }

        private static Image FullScreen(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            StretchFull(image.rectTransform);
            return image;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Texture2D[] BuildGrainTextures()
        {
            var frames = new Texture2D[GrainFrames];
            for (var f = 0; f < GrainFrames; f++)
            {
                var tex = new Texture2D(GrainSize, GrainSize, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Point
                };
                var pixels = new Color32[GrainSize * GrainSize];
                for (var i = 0; i < pixels.Length; i++)
                {
                    var v = (byte)Random.Range(0, 256);
                    pixels[i] = new Color32(v, v, v, 255);
                }

                tex.SetPixels32(pixels);
                tex.Apply();
                frames[f] = tex;
            }

            return frames;
        }

        private static Sprite BuildVignetteSprite()
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var maxDist = size * 0.72f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    var a = (byte)(Mathf.Clamp01(Mathf.SmoothStep(0f, 1f, d)) * 150f);
                    pixels[y * size + x] = new Color32(0, 0, 0, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
