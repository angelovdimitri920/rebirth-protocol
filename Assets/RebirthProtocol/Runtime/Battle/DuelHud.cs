using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace RebirthProtocol.Battle
{
    // Code-built uGUI overlay: player HP/endurance/shield/boost/pod/bomb
    // (bottom-left), enemy HP/endurance/shield (top-right), center banner,
    // and both loadouts' part names.
    public sealed class DuelHud : MonoBehaviour
    {
        private RoboAvatar _player;
        private RoboAvatar _enemy;
        private DuelManager _duel;
        private BombSystem _playerBomb;
        private PodSystem _playerPod;

        private Transform _playerHpFill;
        private Transform _playerEnduranceFill;
        private Transform _playerShieldFill;
        private Transform _playerBoostFill;
        private Transform _playerPodFill;
        private Transform _playerBombFill;
        private Transform _enemyHpFill;
        private Transform _enemyEnduranceFill;
        private Transform _enemyShieldFill;
        private Text _banner;

        public void Init(RoboAvatar player, RoboAvatar enemy, DuelManager duel, BombSystem playerBomb, PodSystem playerPod)
        {
            _player = player;
            _enemy = enemy;
            _duel = duel;
            _playerBomb = playerBomb;
            _playerPod = playerPod;

            var canvasGo = new GameObject("HUD Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();

            // Player panel, bottom-left (stacked upward).
            var y = 36f;
            if (player.Loadout.HasBomb)
            {
                _playerBombFill = Bar(canvasGo.transform, new Vector2(0f, 0f), new Vector2(24f, y), new Vector2(320f, 6f), new Color(1f, 0.55f, 0.2f));
                y += 14f;
            }

            _playerPodFill = Bar(canvasGo.transform, new Vector2(0f, 0f), new Vector2(24f, y), new Vector2(320f, 6f), new Color(0.65f, 0.5f, 1f));
            y += 14f;
            _playerBoostFill = Bar(canvasGo.transform, new Vector2(0f, 0f), new Vector2(24f, y), new Vector2(320f, 8f), new Color(0.35f, 0.75f, 1f));
            y += 18f;
            if (player.Loadout.HasShield)
            {
                _playerShieldFill = Bar(canvasGo.transform, new Vector2(0f, 0f), new Vector2(24f, y), new Vector2(320f, 10f), new Color(0.6f, 0.95f, 1f));
                y += 18f;
            }

            _playerEnduranceFill = Bar(canvasGo.transform, new Vector2(0f, 0f), new Vector2(24f, y), new Vector2(320f, 12f), new Color(1f, 0.75f, 0.25f));
            y += 22f;
            _playerHpFill = Bar(canvasGo.transform, new Vector2(0f, 0f), new Vector2(24f, y), new Vector2(320f, 18f), new Color(0.25f, 0.9f, 0.45f));

            // Enemy panel, top-right.
            _enemyHpFill = Bar(canvasGo.transform, new Vector2(1f, 1f), new Vector2(-344f, -44f), new Vector2(320f, 18f), new Color(0.95f, 0.35f, 0.3f));
            _enemyEnduranceFill = Bar(canvasGo.transform, new Vector2(1f, 1f), new Vector2(-344f, -66f), new Vector2(320f, 12f), new Color(1f, 0.75f, 0.25f));
            if (enemy.Loadout.HasShield)
            {
                _enemyShieldFill = Bar(canvasGo.transform, new Vector2(1f, 1f), new Vector2(-344f, -84f), new Vector2(320f, 10f), new Color(0.6f, 0.95f, 1f));
            }

            // Loadout labels.
            Label(canvasGo.transform, new Vector2(0f, 0f), new Vector2(24f, y + 26f), TextAnchor.LowerLeft, DescribeLoadout(player.Loadout));
            Label(canvasGo.transform, new Vector2(1f, 1f), new Vector2(-344f, -104f), TextAnchor.UpperLeft, DescribeLoadout(enemy.Loadout));

            // Controller legend, bottom-right — shown while a gamepad is
            // connected (keyboard bindings live in the hangar footer).
            _padLegend = Label(canvasGo.transform, new Vector2(1f, 0f), new Vector2(-24f, 24f), TextAnchor.LowerRight,
                "<size=14><color=#99a>A jump · B gun · X dash · Y melee · RT bomb/shield · LT pod · Start pause</color></size>");

            // Center banner.
            var bannerGo = new GameObject("Banner");
            bannerGo.transform.SetParent(canvasGo.transform, false);
            _banner = bannerGo.AddComponent<Text>();
            _banner.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _banner.fontSize = 40;
            _banner.alignment = TextAnchor.MiddleCenter;
            _banner.color = Color.white;
            var rect = _banner.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 120f);
            rect.sizeDelta = new Vector2(900f, 120f);
        }

        private static string DescribeLoadout(Loadout l)
        {
            var rightArm = l.HasGun ? l.Gun.Name : l.Melee.Name;
            var leftArm = l.HasBomb ? l.Bomb.Name : l.Shield.Name;
            return $"<size=14><color=#99a>{l.Body.Name} · {rightArm} · {leftArm} · {l.Legs.Name} · {l.Pod.Name}</color></size>";
        }

        private Text _padLegend;

        private static Text Label(Transform parent, Vector2 anchor, Vector2 offset, TextAnchor align, string content)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = align;
            text.color = Color.white;
            text.text = content;
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(520f, 22f);
            return text;
        }

        private static Transform Bar(Transform parent, Vector2 anchor, Vector2 offset, Vector2 size, Color color)
        {
            var bg = new GameObject("Bar");
            bg.transform.SetParent(parent, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.55f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = anchor;
            bgRect.anchorMax = anchor;
            bgRect.pivot = anchor;
            bgRect.anchoredPosition = offset;
            bgRect.sizeDelta = size;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(bg.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = color;
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(1f, 0f);
            fillRect.sizeDelta = new Vector2(size.x - 2f, -2f);

            return fill.transform;
        }

        public void Tick()
        {
            SetFill(_playerHpFill, _player.Health.Hp / _player.Health.MaxHp);
            SetFill(_playerEnduranceFill, _player.Health.Endurance / _player.Health.MaxEndurance);
            SetFill(_playerBoostFill, _player.Boost.Value / _player.Boost.Max);
            SetFill(_playerPodFill, _playerPod.Energy / _player.Loadout.Pod.EnergyMax);
            if (_playerShieldFill != null)
            {
                SetFill(_playerShieldFill, _player.ShieldHp / _player.Loadout.Shield.ShieldHp);
            }

            if (_playerBombFill != null)
            {
                SetFill(_playerBombFill, 1f - Mathf.Clamp01(_playerBomb.CooldownRemaining / _player.Loadout.Bomb.Cooldown));
            }

            SetFill(_enemyHpFill, _enemy.Health.Hp / _enemy.Health.MaxHp);
            SetFill(_enemyEnduranceFill, _enemy.Health.Endurance / _enemy.Health.MaxEndurance);
            if (_enemyShieldFill != null)
            {
                SetFill(_enemyShieldFill, _enemy.ShieldHp / _enemy.Loadout.Shield.ShieldHp);
            }

            _padLegend.enabled = UnityEngine.InputSystem.Gamepad.current != null;

            if (_duel.IsOver)
            {
                _banner.text = _duel.PlayerWon
                    ? "VICTORY\n<size=22>R / Start — rematch</size>"
                    : "DEFEAT\n<size=22>R / Start — rematch</size>";
            }
            else if (_duel.Paused)
            {
                _banner.text = "PAUSED\n<size=22>P / Start — resume</size>";
            }
            else if (_player.Health.State == HealthState.KnockedDown)
            {
                _banner.text = "DOWN — MASH SPACE / A";
            }
            else
            {
                _banner.text = "";
            }
        }

        private static void SetFill(Transform fill, float fraction)
        {
            var scale = fill.localScale;
            scale.x = Mathf.Clamp01(fraction);
            fill.localScale = scale;
        }
    }
}
