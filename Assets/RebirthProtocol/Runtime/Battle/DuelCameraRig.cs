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
        private Vector3 _lookAt; // smoothed aim point — raw fighter positions never steer the lens directly
        private float _orthoSize;
        private float _shake;
        private float _shakeTime;

        /// Flattened view direction, for camera-relative movement input.
        public Vector3 FlatForward => new Vector3(Mathf.Sin(_yaw), 0f, Mathf.Cos(_yaw));

        /// Kick the camera; the offset decays over ~0.4s. Explosions and
        /// heavy hits call this. Multiple kicks take the strongest.
        public void AddShake(float amount)
        {
            _shake = Mathf.Max(_shake, amount);
        }

        public void Init(Camera cam, RoboAvatar player, RoboAvatar enemy)
        {
            _camera = cam;
            _player = player;
            _enemy = enemy;
            _camera.orthographic = true;
            _orthoSize = CombatTuning.Camera.FrustumSize * 0.5f;
            _camera.orthographicSize = _orthoSize;

            _yaw = DesiredYaw(_yaw);
            _lookAt = LookAtPoint();
            _position = TargetPosition();
            Apply();
        }

        public void Tick(float dt)
        {
            // Slow-and-deliberate rule: every input to the lens is smoothed
            // — the aim point, the yaw (with a hard deg/sec cap), the dolly,
            // and the zoom each glide at their own rate. Dashes and
            // knockbacks must read on the fighters, never on the frame.
            _yaw = DampAngle(_yaw, DesiredYaw(_yaw), CombatTuning.Camera.RotateLerp,
                CombatTuning.Camera.MaxYawSpeedDeg * Mathf.Deg2Rad, dt);

            _lookAt = Vector3.Lerp(_lookAt, LookAtPoint(), Mathf.Min(1f, CombatTuning.Camera.LookAtLerp * dt));

            var followT = Mathf.Min(1f, CombatTuning.Camera.FollowLerp * dt);
            _position = Vector3.Lerp(_position, TargetPosition(), followT);

            var separation = Vector3.Distance(_player.Position, _enemy.Position);
            var zoomT = Mathf.Clamp01((separation - CombatTuning.Camera.ZoomStartDistance) / CombatTuning.Camera.ZoomRange);
            var targetSize = CombatTuning.Camera.FrustumSize * 0.5f * (1f + CombatTuning.Camera.ZoomMax * zoomT);
            _orthoSize = Mathf.Lerp(_orthoSize, targetSize, Mathf.Min(1f, CombatTuning.Camera.ZoomLerp * dt));

            _shake = Mathf.Max(0f, _shake - dt * 2.5f);
            _shakeTime += dt;

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
            // Dolly chases the SMOOTHED aim point, so position and rotation
            // glide together instead of fighting.
            var back = new Vector3(Mathf.Sin(_yaw), 0f, Mathf.Cos(_yaw)) * CombatTuning.Camera.Back;
            return _lookAt - back + Vector3.up * CombatTuning.Camera.Height;
        }

        private void Apply()
        {
            // Trauma-style shake: two out-of-phase sines per axis so it reads
            // as a jolt, not a wobble; amplitude fades with _shake.
            var offset = Vector3.zero;
            if (_shake > 0f)
            {
                var t = _shakeTime * 38f;
                offset = new Vector3(
                    Mathf.Sin(t) * 0.6f + Mathf.Sin(t * 1.7f) * 0.4f,
                    Mathf.Sin(t * 1.3f + 2f) * 0.6f + Mathf.Sin(t * 2.1f) * 0.4f,
                    0f) * _shake;
                // Shake in screen space (camera-local), not world.
                offset = _camera.transform.right * offset.x + _camera.transform.up * offset.y;
            }

            _camera.transform.position = _position + offset;
            _camera.transform.rotation = Quaternion.LookRotation(_lookAt - _position);
            _camera.orthographicSize = _orthoSize;
        }

        /// Proportional damping with a hard angular-speed ceiling: big
        /// errors (fighters crossing) turn at maxSpeed, never faster.
        private static float DampAngle(float from, float to, float rate, float maxSpeed, float dt)
        {
            var diff = Mathf.DeltaAngle(from * Mathf.Rad2Deg, to * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            var step = diff * Mathf.Min(1f, rate * dt);
            var cap = maxSpeed * dt;
            return from + Mathf.Clamp(step, -cap, cap);
        }
    }
}
