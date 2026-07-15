using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace RebirthProtocol.Battle
{
    // Code-built uGUI overlay: player HP/endurance/boost (bottom-left),
    // enemy HP/endurance (top-right), center state banner.
    public sealed class DuelHud : MonoBehaviour
    {
        private RoboAvatar _player;
        private RoboAvatar _enemy;
        private DuelManager _duel;

        private Transform _playerHpFill;
        private Transform _playerEnduranceFill;
        private Transform _playerBoostFill;
        private Transform _enemyHpFill;
        private Transform _enemyEnduranceFill;
        private Text _banner;

        public void Init(RoboAvatar player, RoboAvatar enemy, DuelManager duel)
        {
            _player = player;
            _enemy = enemy;
            _duel = duel;

            var canvasGo = new GameObject("HUD Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();

            // Player panel, bottom-left.
            _playerHpFill = Bar(canvasGo.transform, new Vector2(0f, 0f), new Vector2(24f, 84f), new Vector2(320f, 18f), new Color(0.25f, 0.9f, 0.45f));
            _playerEnduranceFill = Bar(canvasGo.transform, new Vector2(0f, 0f), new Vector2(24f, 58f), new Vector2(320f, 12f), new Color(1f, 0.75f, 0.25f));
            _playerBoostFill = Bar(canvasGo.transform, new Vector2(0f, 0f), new Vector2(24f, 36f), new Vector2(320f, 8f), new Color(0.35f, 0.75f, 1f));

            // Enemy panel, top-right (fills anchored right so they drain leftward).
            _enemyHpFill = Bar(canvasGo.transform, new Vector2(1f, 1f), new Vector2(-344f, -44f), new Vector2(320f, 18f), new Color(0.95f, 0.35f, 0.3f));
            _enemyEnduranceFill = Bar(canvasGo.transform, new Vector2(1f, 1f), new Vector2(-344f, -66f), new Vector2(320f, 12f), new Color(1f, 0.75f, 0.25f));

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
            SetFill(_enemyHpFill, _enemy.Health.Hp / _enemy.Health.MaxHp);
            SetFill(_enemyEnduranceFill, _enemy.Health.Endurance / _enemy.Health.MaxEndurance);

            if (_duel.IsOver)
            {
                _banner.text = _duel.PlayerWon
                    ? "VICTORY\n<size=22>R / Start — rematch</size>"
                    : "DEFEAT\n<size=22>R / Start — rematch</size>";
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
