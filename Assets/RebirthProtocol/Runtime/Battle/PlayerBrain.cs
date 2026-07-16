using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RebirthProtocol.Battle
{
    // Reads keyboard + gamepad and drives the player avatar's intent.
    // Mapping per the settled prototype scheme (DESIGN_HANDOFF §5):
    //   A/Space = jump-hover (and mash while down), B/J = gun (hold),
    //   X/Shift = dash, Y/K = melee, RT/Q = left arm (bomb aim / shield
    //   hold), LT/E = pod toggle, R/Start = rematch.
    // Movement is derived from the live camera orientation every frame —
    // never a hardcoded world axis (the prototype's inverted-controls
    // lesson). Lock-on is automatic: one enemy, always locked.
    public sealed class PlayerBrain : MonoBehaviour
    {
        private RoboAvatar _avatar;
        private RoboAvatar _enemy;
        private DuelCameraRig _camera;
        private BombSystem _bomb;
        private PodSystem _pod;

        public void Init(RoboAvatar avatar, RoboAvatar enemy, DuelCameraRig cameraRig, BombSystem bomb, PodSystem pod)
        {
            _avatar = avatar;
            _enemy = enemy;
            _camera = cameraRig;
            _bomb = bomb;
            _pod = pod;
        }

        public void Tick(float dt)
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            var move = Vector2.zero;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
            }

            if (gamepad != null)
            {
                move += gamepad.leftStick.ReadValue();
            }

            move = Vector2.ClampMagnitude(move, 1f);

            var thrustHeld = (keyboard?.spaceKey.isPressed ?? false) || (gamepad?.buttonSouth.isPressed ?? false);
            var thrustPressed = (keyboard?.spaceKey.wasPressedThisFrame ?? false) || (gamepad?.buttonSouth.wasPressedThisFrame ?? false);
            var dashPressed = (keyboard?.leftShiftKey.wasPressedThisFrame ?? false) || (gamepad?.buttonWest.wasPressedThisFrame ?? false);
            var firingHeld = (keyboard?.jKey.isPressed ?? false) || (gamepad?.buttonEast.isPressed ?? false);
            var meleePressed = (keyboard?.kKey.wasPressedThisFrame ?? false) || (gamepad?.buttonNorth.wasPressedThisFrame ?? false);
            var leftArmHeld = (keyboard?.qKey.isPressed ?? false) || (gamepad?.rightTrigger.isPressed ?? false);
            var podPressed = (keyboard?.eKey.wasPressedThisFrame ?? false) || (gamepad?.leftTrigger.wasPressedThisFrame ?? false);

            // Camera-relative world movement.
            var forward = _camera.FlatForward;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var worldMove = right * move.x + forward * move.y;

            var enemyAlive = _enemy.Health.State != HealthState.Dead;
            var toEnemy = _enemy.Position - _avatar.Position;
            toEnemy.y = 0f;

            // Left arm: shield is a plain hold; bomb is hold-to-aim,
            // release-to-throw.
            var shieldHeld = _avatar.Loadout.HasShield && leftArmHeld && _avatar.Health.State == HealthState.Active;
            if (_avatar.Loadout.HasBomb)
            {
                if (leftArmHeld && !_bomb.Aiming)
                {
                    _bomb.StartAim(_enemy);
                }
                else if (leftArmHeld && _bomb.Aiming)
                {
                    _bomb.UpdateAim(_enemy);
                }
                else if (!leftArmHeld && _bomb.Aiming)
                {
                    _bomb.Release();
                }
            }

            var firing = firingHeld && _avatar.Loadout.HasGun && !_avatar.Melee.Busy;

            _avatar.Intent = new RoboIntent
            {
                MoveDir = worldMove,
                ThrustHeld = thrustHeld,
                DashRequested = dashPressed,
                MashPressed = thrustPressed,
                FiringGun = firing,
                ShieldHeld = shieldHeld,
                LeftArmActive = shieldHeld || _bomb.Aiming,
                // Free facing: face movement normally, square up while attacking.
                HasFaceYaw = (firing || _avatar.Melee.Busy || shieldHeld || _bomb.Aiming) && enemyAlive && toEnemy.sqrMagnitude > 0.0001f,
                FaceYaw = Mathf.Atan2(toEnemy.x, toEnemy.z),
                HasDashHoming = enemyAlive,
                DashHomingPoint = _enemy.Position
            };

            _avatar.TickGun(dt, firing, enemyAlive ? _enemy : null);

            if (podPressed)
            {
                _pod.Toggle();
            }

            if (meleePressed && enemyAlive)
            {
                if (_avatar.Melee.Busy)
                {
                    _avatar.TryMeleeChain(_enemy);
                }
                else
                {
                    _avatar.TryMelee(_enemy);
                }
            }
        }
    }
}
