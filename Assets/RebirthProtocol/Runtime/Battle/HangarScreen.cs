using System;
using RebirthProtocol.Battle.Audio;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RebirthProtocol.Battle
{
    // Pre-duel loadout selection: five slots, up/down to pick a slot,
    // left/right to cycle its parts, Enter/A to deploy. Code-built uGUI.
    public sealed class HangarScreen : MonoBehaviour
    {
        private sealed class Row
        {
            public string Label;
            public Func<int, string> Describe;
            public Func<int> Count;
            public int Index;
            public Text Text;
        }

        private Row[] _rows;
        private int _selectedRow;
        private Action<Loadout> _onDeploy;
        private GameObject _canvasRoot;
        private float _repeatTimer;
        private Transform _previewRoot;
        private Transform _previewTilt;

        public void Init(Loadout initial, Action<Loadout> onDeploy)
        {
            _onDeploy = onDeploy;

            _rows = new[]
            {
                new Row
                {
                    Label = "BODY",
                    Count = () => PartsCatalog.Bodies.Length,
                    Describe = i => Named(PartsCatalog.Bodies[i].Name, PartsCatalog.Bodies[i].Blurb),
                    Index = Array.IndexOf(PartsCatalog.Bodies, initial.Body)
                },
                new Row
                {
                    Label = "RIGHT ARM",
                    Count = () => PartsCatalog.Guns.Length + PartsCatalog.MeleeWeapons.Length,
                    Describe = i => i < PartsCatalog.Guns.Length
                        ? Named(PartsCatalog.Guns[i].Name + " (gun)", PartsCatalog.Guns[i].Blurb)
                        : Named(PartsCatalog.MeleeWeapons[i - PartsCatalog.Guns.Length].Name + " (melee)", PartsCatalog.MeleeWeapons[i - PartsCatalog.Guns.Length].Blurb),
                    Index = initial.HasMelee
                        ? PartsCatalog.Guns.Length + Array.IndexOf(PartsCatalog.MeleeWeapons, initial.Melee)
                        : Array.IndexOf(PartsCatalog.Guns, initial.Gun)
                },
                new Row
                {
                    Label = "LEFT ARM",
                    Count = () => PartsCatalog.Bombs.Length + PartsCatalog.Shields.Length,
                    Describe = i => i < PartsCatalog.Bombs.Length
                        ? Named(PartsCatalog.Bombs[i].Name + " (bomb)", PartsCatalog.Bombs[i].Blurb)
                        : Named(PartsCatalog.Shields[i - PartsCatalog.Bombs.Length].Name + " (shield)", PartsCatalog.Shields[i - PartsCatalog.Bombs.Length].Blurb),
                    Index = initial.HasShield
                        ? PartsCatalog.Bombs.Length + Array.IndexOf(PartsCatalog.Shields, initial.Shield)
                        : Array.IndexOf(PartsCatalog.Bombs, initial.Bomb)
                },
                new Row
                {
                    Label = "LEGS",
                    Count = () => PartsCatalog.Legs.Length,
                    Describe = i => Named(PartsCatalog.Legs[i].Name, PartsCatalog.Legs[i].Blurb),
                    Index = Array.IndexOf(PartsCatalog.Legs, initial.Legs)
                },
                new Row
                {
                    Label = "POD",
                    Count = () => PartsCatalog.Pods.Length,
                    Describe = i => Named(PartsCatalog.Pods[i].Name, PartsCatalog.Pods[i].Blurb),
                    Index = Array.IndexOf(PartsCatalog.Pods, initial.Pod)
                }
            };

            // Live 3D preview on a dais at arena center: rebuilt on every
            // part pick, slow turntable spin so all sides read.
            _previewRoot = new GameObject("Hangar Preview").transform;
            _previewRoot.SetParent(transform, false);
            _previewRoot.position = new Vector3(0f, 0f, 0f);
            _previewTilt = new GameObject("Preview Tilt").transform;
            _previewTilt.SetParent(_previewRoot, false);
            _previewTilt.localPosition = new Vector3(0f, 1f, 0f);

            BuildUi();
            Refresh();
        }

        private static string Named(string name, string blurb) => $"< {name} >\n<size=14><color=#aab>{blurb}</color></size>";

        public void Tick()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            var vertical = 0;
            var horizontal = 0;
            if (keyboard != null)
            {
                if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) vertical -= 1;
                if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) vertical += 1;
                if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) horizontal -= 1;
                if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) horizontal += 1;
            }

            if (gamepad != null)
            {
                if (gamepad.dpad.up.wasPressedThisFrame) vertical -= 1;
                if (gamepad.dpad.down.wasPressedThisFrame) vertical += 1;
                if (gamepad.dpad.left.wasPressedThisFrame) horizontal -= 1;
                if (gamepad.dpad.right.wasPressedThisFrame) horizontal += 1;

                // Stick navigation with a simple repeat gate.
                _repeatTimer -= Time.deltaTime;
                var stick = gamepad.leftStick.ReadValue();
                if (_repeatTimer <= 0f && stick.magnitude > 0.6f)
                {
                    if (Mathf.Abs(stick.y) > Mathf.Abs(stick.x))
                    {
                        vertical += stick.y > 0f ? -1 : 1;
                    }
                    else
                    {
                        horizontal += stick.x > 0f ? 1 : -1;
                    }

                    _repeatTimer = 0.25f;
                }
            }

            if (vertical != 0)
            {
                _selectedRow = (_selectedRow + vertical + _rows.Length) % _rows.Length;
                GameAudio.Sfx?.UiClick();
                Refresh();
            }

            if (horizontal != 0)
            {
                var row = _rows[_selectedRow];
                row.Index = (row.Index + horizontal + row.Count()) % row.Count();
                GameAudio.Sfx?.UiClick();
                Refresh();
            }

            // Turntable spin so the preview reads from all sides.
            if (_previewTilt != null)
            {
                _previewTilt.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
            }

            var deploy = (keyboard?.enterKey.wasPressedThisFrame ?? false)
                || (keyboard?.spaceKey.wasPressedThisFrame ?? false)
                || (gamepad?.buttonSouth.wasPressedThisFrame ?? false)
                || (gamepad?.startButton.wasPressedThisFrame ?? false);
            if (deploy)
            {
                GameAudio.Sfx?.UiClick();
                _onDeploy?.Invoke(BuildLoadout());
            }
        }

        public Loadout BuildLoadout()
        {
            var rightArm = _rows[1].Index;
            var leftArm = _rows[2].Index;
            return new Loadout
            {
                Body = PartsCatalog.Bodies[_rows[0].Index],
                Gun = rightArm < PartsCatalog.Guns.Length ? PartsCatalog.Guns[rightArm] : null,
                Melee = rightArm >= PartsCatalog.Guns.Length ? PartsCatalog.MeleeWeapons[rightArm - PartsCatalog.Guns.Length] : null,
                Bomb = leftArm < PartsCatalog.Bombs.Length ? PartsCatalog.Bombs[leftArm] : null,
                Shield = leftArm >= PartsCatalog.Bombs.Length ? PartsCatalog.Shields[leftArm - PartsCatalog.Bombs.Length] : null,
                Legs = PartsCatalog.Legs[_rows[3].Index],
                Pod = PartsCatalog.Pods[_rows[4].Index]
            };
        }

        public void Show(bool visible)
        {
            _canvasRoot.SetActive(visible);
            _previewRoot.gameObject.SetActive(visible);
        }

        private void RebuildPreview()
        {
            for (var i = _previewTilt.childCount - 1; i >= 0; i--)
            {
                Destroy(_previewTilt.GetChild(i).gameObject);
            }

            RoboVisual.Build(_previewTilt, BuildLoadout(), new Color(0.2f, 0.55f, 1f));
        }

        private void BuildUi()
        {
            _canvasRoot = new GameObject("Hangar Canvas");
            _canvasRoot.transform.SetParent(transform, false);
            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            _canvasRoot.AddComponent<CanvasScaler>();

            var dim = new GameObject("Dim");
            dim.transform.SetParent(_canvasRoot.transform, false);
            var dimImage = dim.AddComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.72f);
            var dimRect = dim.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.sizeDelta = Vector2.zero;

            MakeText("HANGAR — ASSEMBLE YOUR ROBO", 34, new Vector2(0.5f, 1f), new Vector2(0f, -60f), TextAnchor.MiddleCenter);

            for (var i = 0; i < _rows.Length; i++)
            {
                _rows[i].Text = MakeText("", 22, new Vector2(0.5f, 1f), new Vector2(0f, -150f - i * 92f), TextAnchor.UpperCenter);
            }

            MakeText("W/S — slot   A/D — part   ENTER / A — deploy", 18, new Vector2(0.5f, 0f), new Vector2(0f, 46f), TextAnchor.MiddleCenter);
        }

        private Text MakeText(string content, int size, Vector2 anchor, Vector2 offset, TextAnchor align)
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
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, anchor.y);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(1100f, 88f);
            return text;
        }

        private void Refresh()
        {
            for (var i = 0; i < _rows.Length; i++)
            {
                var row = _rows[i];
                var marker = i == _selectedRow ? "▶ " : "   ";
                var color = i == _selectedRow ? "#ffd45f" : "#dde";
                row.Text.text = $"<color={color}>{marker}{row.Label}   {row.Describe(row.Index)}</color>";
            }

            RebuildPreview();
        }
    }
}
