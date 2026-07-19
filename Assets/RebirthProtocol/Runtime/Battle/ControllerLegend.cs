using UnityEngine;
using UnityEngine.UI;

namespace RebirthProtocol.Battle
{
    // Opaque bottom-right controller legend (user playtest request,
    // 2026-07-18): a small top-view Xbox pad drawn from code-built uGUI
    // primitives, with color-keyed binding rows beneath it. Shown while a
    // gamepad is connected (DuelHud toggles it); keyboard bindings stay in
    // the hangar footer.
    public static class ControllerLegend
    {
        // Xbox face-button colors, dimmed to sit on the dark panel.
        private static readonly Color FaceA = new Color(0.42f, 0.78f, 0.25f);
        private static readonly Color FaceB = new Color(0.9f, 0.32f, 0.28f);
        private static readonly Color FaceX = new Color(0.3f, 0.55f, 0.95f);
        private static readonly Color FaceY = new Color(0.95f, 0.78f, 0.25f);
        private static readonly Color Plastic = new Color(0.18f, 0.2f, 0.26f);
        private static readonly Color PlasticLight = new Color(0.28f, 0.31f, 0.4f);

        private static Sprite _circle;

        public static GameObject Build(Transform parent)
        {
            var panel = Panel(parent, "Controller Legend", new Vector2(1f, 0f), new Vector2(-16f, 16f), new Vector2(300f, 236f),
                new Color(0.055f, 0.063f, 0.086f, 1f)); // fully opaque by request

            Label(panel.transform, "CONTROLS", 12, new Vector2(0.5f, 1f), new Vector2(0f, -6f), TextAnchor.UpperCenter,
                new Color(0.55f, 0.58f, 0.68f));

            // --- The pad glyph (top half). Positions are panel-local, origin
            // at the pad body's center. ---
            var pad = new GameObject("Pad").AddComponent<RectTransform>();
            pad.SetParent(panel.transform, false);
            pad.anchorMin = pad.anchorMax = new Vector2(0.5f, 1f);
            pad.anchoredPosition = new Vector2(0f, -70f);
            pad.sizeDelta = Vector2.zero;

            // Body + grips.
            Rect(pad, new Vector2(0f, 0f), new Vector2(170f, 62f), Plastic);
            Circle(pad, new Vector2(-76f, -14f), 46f, Plastic);
            Circle(pad, new Vector2(76f, -14f), 46f, Plastic);

            // Triggers (upper, thin) and bumpers (below them, wider) peeking
            // over the body's top edge.
            Rect(pad, new Vector2(-62f, 40f), new Vector2(30f, 10f), PlasticLight);
            TinyLabel(pad, new Vector2(-96f, 40f), "LT");
            Rect(pad, new Vector2(62f, 40f), new Vector2(30f, 10f), PlasticLight);
            TinyLabel(pad, new Vector2(96f, 40f), "RT");
            Rect(pad, new Vector2(-62f, 28f), new Vector2(44f, 9f), PlasticLight);
            TinyLabel(pad, new Vector2(-96f, 28f), "LB");
            Rect(pad, new Vector2(62f, 28f), new Vector2(44f, 9f), PlasticLight);
            TinyLabel(pad, new Vector2(96f, 28f), "RB");

            // Left stick (upper-left), right stick (lower-middle-right).
            Circle(pad, new Vector2(-58f, 8f), 26f, PlasticLight);
            TinyLabel(pad, new Vector2(-58f, 8f), "L");
            Circle(pad, new Vector2(30f, -18f), 20f, PlasticLight);
            TinyLabel(pad, new Vector2(30f, -18f), "R");

            // D-pad cross (lower-left-middle).
            Rect(pad, new Vector2(-28f, -18f), new Vector2(24f, 8f), PlasticLight);
            Rect(pad, new Vector2(-28f, -18f), new Vector2(8f, 24f), PlasticLight);

            // ABXY diamond (right side of body).
            Circle(pad, new Vector2(58f, -4f), 15f, FaceA);
            TinyLabel(pad, new Vector2(58f, -4f), "A", Color.black);
            Circle(pad, new Vector2(74f, 8f), 15f, FaceB);
            TinyLabel(pad, new Vector2(74f, 8f), "B", Color.black);
            Circle(pad, new Vector2(42f, 8f), 15f, FaceX);
            TinyLabel(pad, new Vector2(42f, 8f), "X", Color.black);
            Circle(pad, new Vector2(58f, 20f), 15f, FaceY);
            TinyLabel(pad, new Vector2(58f, 20f), "Y", Color.black);

            // Start button (center).
            Rect(pad, new Vector2(12f, 6f), new Vector2(12f, 6f), PlasticLight);

            // --- Binding rows (bottom half), color-keyed to the glyph. ---
            var rows = new (string dot, Color color, string text)[]
            {
                ("●", PlasticLight, "L stick — move"),
                ("●", FaceA, "A — jump · hover · mash up"),
                ("●", FaceX, "X / LB — dash"),
                ("●", FaceB, "B — gun / melee"),
                ("●", FaceY, "Y / RB — lock-on"),
                ("●", PlasticLight, "RT — bomb / shield"),
                ("●", PlasticLight, "LT — pod"),
                ("●", PlasticLight, "Start — pause menu")
            };
            for (var i = 0; i < rows.Length; i++)
            {
                var col = i < 4 ? 0 : 1;
                var row = i % 4;
                var x = col == 0 ? 10f : 152f;
                var y = -128f - row * 16f;
                var text = Label(panel.transform, $"<color=#{ColorUtility.ToHtmlStringRGB(rows[i].color)}>{rows[i].dot}</color> {rows[i].text}",
                    10, new Vector2(0f, 1f), new Vector2(x, y), TextAnchor.UpperLeft, new Color(0.78f, 0.8f, 0.88f));
                text.rectTransform.sizeDelta = new Vector2(150f, 16f);
            }

            return panel;
        }

        private static GameObject Panel(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            return go;
        }

        private static void Rect(RectTransform parent, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("Rect");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        private static void Circle(RectTransform parent, Vector2 pos, float diameter, Color color)
        {
            var go = new GameObject("Circle");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = CircleSprite();
            image.color = color;
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(diameter, diameter);
        }

        private static Text TinyLabel(RectTransform parent, Vector2 pos, string content, Color? color = null)
        {
            var text = Label(parent, content, 9, new Vector2(0.5f, 0.5f), pos, TextAnchor.MiddleCenter,
                color ?? new Color(0.7f, 0.73f, 0.82f));
            text.rectTransform.sizeDelta = new Vector2(30f, 12f);
            text.fontStyle = FontStyle.Bold;
            return text;
        }

        private static Text Label(Transform parent, string content, int size, Vector2 anchor, Vector2 offset, TextAnchor align, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = align;
            text.color = color;
            text.text = content;
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(280f, 20f);
            return text;
        }

        /// 64px radial-alpha circle, generated once — no sprite assets exist
        /// in this code-built-UI project, and Unity's built-in UI sprites
        /// aren't reliably reachable from a player build without an asset
        /// reference.
        private static Sprite CircleSprite()
        {
            if (_circle != null)
            {
                return _circle;
            }

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;
            var radius = size * 0.5f - 1f;
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    var a = Mathf.Clamp01(radius - d + 0.5f); // 1px soft edge
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            _circle = Sprite.Create(tex, new UnityEngine.Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _circle;
        }
    }
}
