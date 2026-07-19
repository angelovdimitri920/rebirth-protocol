using System;
using RebirthProtocol.Battle.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RebirthProtocol.Battle
{
    // Start-button pause menu (user playtest request, 2026-07-18): Resume /
    // Return to Customization / Title Screen / Quit. Stick or d-pad to
    // move, A/Enter to confirm, Start or B to resume. Replaces the old
    // banner-only pause.
    public sealed class PauseMenu : MonoBehaviour
    {
        public const int OptionResume = 0;
        public const int OptionCustomization = 1;
        public const int OptionTitle = 2;
        public const int OptionQuit = 3;

        private static readonly string[] Options =
        {
            "RESUME",
            "RETURN TO CUSTOMIZATION",
            "TITLE SCREEN",
            "QUIT GAME"
        };

        private Action<int> _onPick;
        private GameObject _canvasRoot;
        private Text[] _rows;
        private int _selected;
        private float _repeatTimer;

        public void Init(Action<int> onPick)
        {
            _onPick = onPick;
            BuildUi();
            Show(false);
        }

        public void Show(bool visible)
        {
            _canvasRoot.SetActive(visible);
            if (visible)
            {
                _selected = 0;
                Refresh();
            }
        }

        public void Tick()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            var vertical = 0;
            if (keyboard != null)
            {
                if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) vertical -= 1;
                if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) vertical += 1;
            }

            if (gamepad != null)
            {
                if (gamepad.dpad.up.wasPressedThisFrame) vertical -= 1;
                if (gamepad.dpad.down.wasPressedThisFrame) vertical += 1;

                _repeatTimer -= Time.unscaledDeltaTime;
                var stick = gamepad.leftStick.ReadValue();
                if (_repeatTimer <= 0f && Mathf.Abs(stick.y) > 0.6f)
                {
                    vertical += stick.y > 0f ? -1 : 1;
                    _repeatTimer = 0.25f;
                }
            }

            if (vertical != 0)
            {
                _selected = (_selected + vertical + Options.Length) % Options.Length;
                GameAudio.Sfx?.UiClick();
                Refresh();
            }

            var confirm = (keyboard?.enterKey.wasPressedThisFrame ?? false)
                || (keyboard?.spaceKey.wasPressedThisFrame ?? false)
                || (gamepad?.buttonSouth.wasPressedThisFrame ?? false);
            // B backs straight out (Start is handled by DuelManager's toggle).
            var back = gamepad?.buttonEast.wasPressedThisFrame ?? false;

            if (confirm)
            {
                GameAudio.Sfx?.UiClick();
                _onPick?.Invoke(_selected);
            }
            else if (back)
            {
                GameAudio.Sfx?.UiClick();
                _onPick?.Invoke(OptionResume);
            }
        }

        private void BuildUi()
        {
            _canvasRoot = new GameObject("Pause Canvas");
            _canvasRoot.transform.SetParent(transform, false);
            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 14; // over HUD and hangar
            _canvasRoot.AddComponent<CanvasScaler>();

            var dim = new GameObject("Dim");
            dim.transform.SetParent(_canvasRoot.transform, false);
            var dimImage = dim.AddComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.7f);
            var dimRect = dim.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.sizeDelta = Vector2.zero;

            MakeText("PAUSED", 44, new Vector2(0f, 130f), TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;

            _rows = new Text[Options.Length];
            for (var i = 0; i < Options.Length; i++)
            {
                _rows[i] = MakeText("", 24, new Vector2(0f, 40f - i * 46f), TextAnchor.MiddleCenter);
            }

            MakeText("Stick / W·S — move      A / Enter — confirm      Start — resume", 14, new Vector2(0f, -170f), TextAnchor.MiddleCenter)
                .color = new Color(0.5f, 0.53f, 0.63f);
        }

        private Text MakeText(string content, int size, Vector2 offset, TextAnchor align)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(_canvasRoot.transform, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = Color.white;
            text.text = content;
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(900f, 44f);
            return text;
        }

        private void Refresh()
        {
            for (var i = 0; i < Options.Length; i++)
            {
                var marker = i == _selected ? "▶ " : "   ";
                var color = i == _selected ? "#ffd45f" : "#ccd";
                _rows[i].text = $"<color={color}>{marker}{Options[i]}</color>";
            }
        }
    }
}
