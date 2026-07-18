using System;
using RebirthProtocol.Battle.Audio;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RebirthProtocol.Battle
{
    // Post-fight draft (GAME_DESIGN §4): pick one boon, claim the felled
    // rival's Spoils of War part, reroll (once per run), or skip. Same
    // code-built uGUI + up/down + Enter/A navigation as the hangar.
    public sealed class DraftScreen : MonoBehaviour
    {
        private Boon[] _offer;
        private RivalPreset _spoils;
        private int _rerollsLeft;
        private Action<Boon> _onPick;
        private Action _onSpoils;
        private Func<Boon[]> _onReroll; // returns the fresh offer, null if no reroll left
        private Action _onSkip;

        private GameObject _canvasRoot;
        private Text[] _rowTexts;
        private int _selected;
        private float _repeatTimer;

        public void Init(Boon[] offer, RivalPreset spoils, int rerollsLeft,
            Action<Boon> onPick, Action onSpoils, Func<Boon[]> onReroll, Action onSkip)
        {
            _offer = offer;
            _spoils = spoils;
            _rerollsLeft = rerollsLeft;
            _onPick = onPick;
            _onSpoils = onSpoils;
            _onReroll = onReroll;
            _onSkip = onSkip;
            BuildUi();
            Refresh();
        }

        private int OptionCount => _offer.Length + (_spoils != null ? 1 : 0) + 2; // + reroll + skip
        private int SpoilsIndex => _spoils != null ? _offer.Length : -1;
        private int RerollIndex => _offer.Length + (_spoils != null ? 1 : 0);
        private int SkipIndex => RerollIndex + 1;

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

                _repeatTimer -= Time.deltaTime;
                var stick = gamepad.leftStick.ReadValue();
                if (_repeatTimer <= 0f && Mathf.Abs(stick.y) > 0.6f)
                {
                    vertical += stick.y > 0f ? -1 : 1;
                    _repeatTimer = 0.25f;
                }
            }

            if (vertical != 0)
            {
                _selected = (_selected + vertical + OptionCount) % OptionCount;
                GameAudio.Sfx?.UiClick();
                Refresh();
            }

            var confirm = (keyboard?.enterKey.wasPressedThisFrame ?? false)
                || (keyboard?.spaceKey.wasPressedThisFrame ?? false)
                || (gamepad?.buttonSouth.wasPressedThisFrame ?? false);
            if (confirm)
            {
                Activate(_selected);
            }
        }

        /// Public for PlayMode tests (mirrors ForceArenaLayout's precedent):
        /// activates an option exactly as Enter/A would.
        public void Activate(int index)
        {
            if (index < _offer.Length)
            {
                GameAudio.Sfx?.DraftPick();
                _onPick(_offer[index]);
            }
            else if (index == SpoilsIndex)
            {
                GameAudio.Sfx?.DraftPick();
                _onSpoils();
            }
            else if (index == RerollIndex)
            {
                if (_rerollsLeft <= 0)
                {
                    return; // disabled row: selectable but inert
                }

                GameAudio.Sfx?.UiClick();
                _rerollsLeft -= 1;
                _offer = _onReroll();
                _selected = Mathf.Min(_selected, OptionCount - 1);
                Refresh();
            }
            else
            {
                GameAudio.Sfx?.UiClick();
                _onSkip();
            }
        }

        public void Skip() => Activate(SkipIndex);

        private void BuildUi()
        {
            _canvasRoot = new GameObject("Draft Canvas");
            _canvasRoot.transform.SetParent(transform, false);
            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12;
            _canvasRoot.AddComponent<CanvasScaler>();

            var dim = new GameObject("Dim");
            dim.transform.SetParent(_canvasRoot.transform, false);
            var dimImage = dim.AddComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.8f);
            var dimRect = dim.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.sizeDelta = Vector2.zero;

            MakeText($"{_spoils?.PilotName ?? "RIVAL"} FELLED", 34, new Vector2(0f, -56f), TextAnchor.MiddleCenter);
            MakeText("install one boon — or claim the spoils", 16, new Vector2(0f, -96f), TextAnchor.MiddleCenter);

            _rowTexts = new Text[OptionCount];
            for (var i = 0; i < OptionCount; i++)
            {
                _rowTexts[i] = MakeText("", 20, new Vector2(0f, -156f - i * 84f), TextAnchor.UpperCenter);
            }

            MakeText("W/S · Stick — select      Enter / A — confirm", 15,
                new Vector2(0f, -156f - OptionCount * 84f - 10f), TextAnchor.MiddleCenter);
        }

        private Text MakeText(string content, int size, Vector2 offset, TextAnchor align)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(_canvasRoot.transform, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            text.text = content;
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(1100f, 80f);
            return text;
        }

        private void Refresh()
        {
            for (var i = 0; i < OptionCount; i++)
            {
                var marker = i == _selected ? "▶ " : "   ";
                var color = i == _selected ? "#ffd45f" : "#dde";
                string body;
                if (i < _offer.Length)
                {
                    var boon = _offer[i];
                    body = $"[{boon.Slot.ToString().ToUpperInvariant()}]  {boon.Name}\n<size=14><color=#aab>{boon.Blurb}</color></size>";
                }
                else if (i == SpoilsIndex)
                {
                    body = $"[SPOILS OF WAR]  Claim the {_spoils.SpoilsName}\n<size=14><color=#aab>Take the felled rival's arm for the rest of the run — replaces your {SpoilsSlotLabel()}.</color></size>";
                }
                else if (i == RerollIndex)
                {
                    var disabled = _rerollsLeft <= 0 ? " <color=#667>(none left)</color>" : "";
                    body = $"REROLL ({_rerollsLeft}){disabled}";
                }
                else
                {
                    body = "SKIP";
                }

                _rowTexts[i].text = $"<color={color}>{marker}{body}</color>";
            }
        }

        private string SpoilsSlotLabel()
        {
            if (_spoils.SpoilsGun != null || _spoils.SpoilsMelee != null)
            {
                return "right arm";
            }

            return "left arm";
        }
    }
}
