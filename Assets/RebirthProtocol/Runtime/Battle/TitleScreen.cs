using System;
using RebirthProtocol.Battle.Audio;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RebirthProtocol.Battle
{
    // Boot title card (user playtest request, 2026-07-18): game title over a
    // slowly turning harness on the dais, PRESS START to proceed to the
    // hangar. Code-built uGUI like every other screen; DuelManager owns the
    // flow (InTitle → InHangar → fight) and skips this screen for
    // -autodeploy runs, PlayMode tests, and return-to-hangar reloads.
    public sealed class TitleScreen : MonoBehaviour
    {
        private Action _onStart;
        private GameObject _canvasRoot;
        private Transform _previewRoot;
        private Transform _previewTilt;
        private Text _prompt;
        private float _promptTime;

        public void Init(Action onStart)
        {
            _onStart = onStart;

            // The displayed harness: the player's stored loadout, turning
            // slowly on the same dais spot the hangar preview uses.
            _previewRoot = new GameObject("Title Preview").transform;
            _previewRoot.SetParent(transform, false);
            _previewTilt = new GameObject("Title Tilt").transform;
            _previewTilt.SetParent(_previewRoot, false);
            _previewTilt.localPosition = new Vector3(0f, 1f, 0f);
            RoboVisual.Build(_previewTilt, LoadoutStore.Load(), new Color(0.2f, 0.55f, 1f));

            BuildUi();
        }

        public void Tick()
        {
            _previewTilt.Rotate(0f, 24f * Time.deltaTime, 0f, Space.World);

            // PRESS START pulse.
            _promptTime += Time.deltaTime;
            _prompt.color = new Color(1f, 0.83f, 0.37f, 0.55f + 0.45f * Mathf.Sin(_promptTime * 2.6f));

            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            var start = (keyboard?.enterKey.wasPressedThisFrame ?? false)
                || (keyboard?.spaceKey.wasPressedThisFrame ?? false)
                || (gamepad?.startButton.wasPressedThisFrame ?? false)
                || (gamepad?.buttonSouth.wasPressedThisFrame ?? false);
            if (start)
            {
                GameAudio.Sfx?.UiClick();
                _onStart?.Invoke();
            }
        }

        public void Show(bool visible)
        {
            _canvasRoot.SetActive(visible);
            _previewRoot.gameObject.SetActive(visible);
        }

        private void BuildUi()
        {
            _canvasRoot = new GameObject("Title Canvas");
            _canvasRoot.transform.SetParent(transform, false);
            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12; // over the hangar canvas, under nothing
            _canvasRoot.AddComponent<CanvasScaler>();

            // Dim the arena hard but leave the turning harness readable.
            var dim = new GameObject("Dim");
            dim.transform.SetParent(_canvasRoot.transform, false);
            var dimImage = dim.AddComponent<Image>();
            dimImage.color = new Color(0.01f, 0.015f, 0.03f, 0.6f);
            var dimRect = dim.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.sizeDelta = Vector2.zero;

            MakeText("REBIRTH", 92, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Color(0.93f, 0.95f, 1f), FontStyle.Bold);
            MakeText("PROTOCOL", 92, new Vector2(0.5f, 1f), new Vector2(0f, -244f), new Color(1f, 0.83f, 0.37f), FontStyle.Bold);
            MakeText("a  p a s s a g e   o f   a r m s", 20, new Vector2(0.5f, 1f), new Vector2(0f, -318f), new Color(0.55f, 0.58f, 0.7f), FontStyle.Normal);

            _prompt = MakeText("PRESS  START", 30, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Color(1f, 0.83f, 0.37f), FontStyle.Bold);
            MakeText("Enter / Space on keyboard", 14, new Vector2(0.5f, 0f), new Vector2(0f, 76f), new Color(0.45f, 0.48f, 0.58f), FontStyle.Normal);
        }

        private Text MakeText(string content, int size, Vector2 anchor, Vector2 offset, Color color, FontStyle style)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(_canvasRoot.transform, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            // uGUI Text default is Truncate — a 92px line vanishes inside a
            // 100px rect (caught by the uxshot pass).
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = color;
            text.text = content;
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, anchor.y);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(1200f, 100f);
            return text;
        }
    }
}
