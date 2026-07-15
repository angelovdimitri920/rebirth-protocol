using RebirthProtocol.Domain;
using UnityEngine;

namespace RebirthProtocol.Battle
{
    // Port of the prototype's settled camera (GAME_DESIGN §11-12): true 45°
    // isometric pitch (height == back), orthographic, yaw rotates to keep a
    // perpendicular read on the line between both fighters (continuity-
    // guarded so it never snaps 180°), look-at biased toward the player,
    // dynamic zoom via FRUSTUM size — camera distance does nothing for an
    // orthographic projection.
    public sealed class DuelCameraRig : MonoBehaviour
    {
        private Camera _camera;
        private RoboAvatar _player;
        private RoboAvatar _enemy;
        private float _yaw;
        private Vector3 _position;
        private float _orthoSize;

        /// Flattened view direction, for camera-relative movement input.
        public Vector3 FlatForward => new Vector3(Mathf.Sin(_yaw), 0f, Mathf.Cos(_yaw));

        public void Init(Camera cam, RoboAvatar player, RoboAvatar enemy)
        {
            _camera = cam;
            _player = player;
            _enemy = enemy;
            _camera.orthographic = true;
            _orthoSize = CombatTuning.Camera.FrustumSize * 0.5f;
            _camera.orthographicSize = _orthoSize;

            _yaw = DesiredYaw(_yaw);
            _position = TargetPosition();
            Apply();
        }

        public void Tick(float dt)
        {
            _yaw = DampAngle(_yaw, DesiredYaw(_yaw), CombatTuning.Camera.RotateLerp, dt);

            var followT = Mathf.Min(1f, CombatTuning.Camera.FollowLerp * dt);
            _position = Vector3.Lerp(_position, TargetPosition(), followT);

            var separation = Vector3.Distance(_player.Position, _enemy.Position);
            var zoomT = Mathf.Clamp01((separation - CombatTuning.Camera.ZoomStartDistance) / CombatTuning.Camera.ZoomRange);
            var targetSize = CombatTuning.Camera.FrustumSize * 0.5f * (1f + CombatTuning.Camera.ZoomMax * zoomT);
            _orthoSize = Mathf.Lerp(_orthoSize, targetSize, followT);

            Apply();
        }

        private Vector3 LookAtPoint()
        {
            var from = _player.Center;
            var to = _enemy.Center;
            return Vector3.Lerp(from, to, CombatTuning.Camera.TargetBias);
        }

        /// Perpendicular to the fighter separation line; of the two
        /// perpendiculars, keep the one closest to the current yaw.
        private float DesiredYaw(float currentYaw)
        {
            var sep = _enemy.Position - _player.Position;
            sep.y = 0f;
            if (sep.sqrMagnitude < 0.25f)
            {
                return currentYaw;
            }

            var sepYaw = Mathf.Atan2(sep.x, sep.z);
            var a = sepYaw + Mathf.PI * 0.5f;
            var b = sepYaw - Mathf.PI * 0.5f;
            var diffA = Mathf.Abs(Mathf.DeltaAngle(currentYaw * Mathf.Rad2Deg, a * Mathf.Rad2Deg));
            var diffB = Mathf.Abs(Mathf.DeltaAngle(currentYaw * Mathf.Rad2Deg, b * Mathf.Rad2Deg));
            return diffA <= diffB ? a : b;
        }

        private Vector3 TargetPosition()
        {
            var lookAt = LookAtPoint();
            var back = new Vector3(Mathf.Sin(_yaw), 0f, Mathf.Cos(_yaw)) * CombatTuning.Camera.Back;
            return lookAt - back + Vector3.up * CombatTuning.Camera.Height;
        }

        private void Apply()
        {
            _camera.transform.position = _position;
            _camera.transform.rotation = Quaternion.LookRotation(LookAtPoint() - _position);
            _camera.orthographicSize = _orthoSize;
        }

        private static float DampAngle(float from, float to, float rate, float dt)
        {
            var diff = Mathf.DeltaAngle(from * Mathf.Rad2Deg, to * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            return from + diff * Mathf.Min(1f, rate * dt);
        }
    }
}
