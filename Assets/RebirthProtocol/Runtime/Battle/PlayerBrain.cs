using RebirthProtocol.Battle.Audio;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RebirthProtocol.Battle
{
    // Controller-first input, on the settled Three.js control scheme
    // (DESIGN_HANDOFF §5, prototype §12):
    //   Left stick  = move
    //   A           = jump/hover (double-tap airborne = air-dash), mash while down
    //   X           = dash airborne; grounded (lock is always-on in a duel)
    //                 it performs the garniture's charge attack instead
    //                 (DOCTRINE §11 directive — charges are ground-only, so
    //                 air X stays dash; ground dash retires from X)
    //   B           = RIGHT ARM: gun (held to fire) OR melee (pressed) — one
    //                 physical button, whichever the loadout equips
    //   Y           = lock-on / switch targets
    //   LT          = pod (toggle; hold to steer its launch heading)
    //   RT          = LEFT ARM: bomb (hold-to-aim / release-to-throw) OR shield (held)
    //   RB          = lock-on / switch targets (Y duplicate — thumb stays on the stick)
    //   LB          = dash (X duplicate; earmarked for the garniture charge
    //                 attack when Pass C lands, DOCTRINE §11)
    //   Start       = pause menu (handled in DuelManager)
    // Keyboard is kept as a 1:1 mirror so the game is playable and testable
    // without a pad (WASD move, Space/LShift/J/L/E/Q), but the controller
    // layout above is the canonical scheme.
    // Movement derives from the live camera orientation every frame — never
    // a hardcoded world axis (the prototype's inverted-controls lesson).
    public sealed class PlayerBrain : MonoBehaviour
    {
        private RoboAvatar _avatar;
        private RoboAvatar _enemy;
        private DuelCameraRig _camera;
        private BombSystem _bomb;
        private PodSystem _pod;
        private float _lastThrustPress = -10f; // for double-tap-A air dash

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

            // A = jump/hover/mash.
            var thrustHeld = (keyboard?.spaceKey.isPressed ?? false) || (gamepad?.buttonSouth.isPressed ?? false);
            var thrustPressed = (keyboard?.spaceKey.wasPressedThisFrame ?? false) || (gamepad?.buttonSouth.wasPressedThisFrame ?? false);
            // X = dash (LB duplicates it, so a claw grip never leaves the stick).
            var dashPressed = (keyboard?.leftShiftKey.wasPressedThisFrame ?? false)
                || (gamepad?.buttonWest.wasPressedThisFrame ?? false)
                || (gamepad?.leftShoulder.wasPressedThisFrame ?? false);
            // B = right arm: held drives gun fire, pressed edge-triggers melee.
            var rightArmHeld = (keyboard?.jKey.isPressed ?? false) || (gamepad?.buttonEast.isPressed ?? false);
            var rightArmPressed = (keyboard?.jKey.wasPressedThisFrame ?? false) || (gamepad?.buttonEast.wasPressedThisFrame ?? false);
            // Y = lock-on / switch targets (RB duplicates it).
            var lockPressed = (keyboard?.lKey.wasPressedThisFrame ?? false)
                || (gamepad?.buttonNorth.wasPressedThisFrame ?? false)
                || (gamepad?.rightShoulder.wasPressedThisFrame ?? false);
            // RT = left arm: bomb aim / shield hold.
            var leftArmHeld = (keyboard?.qKey.isPressed ?? false) || (gamepad?.rightTrigger.isPressed ?? false);
            // LT = pod: toggle on press, steer while held.
            var podPressed = (keyboard?.eKey.wasPressedThisFrame ?? false) || (gamepad?.leftTrigger.wasPressedThisFrame ?? false);
            var podHeld = (keyboard?.eKey.isPressed ?? false) || (gamepad?.leftTrigger.isPressed ?? false);

            // Double-tap A while airborne: alternate trigger for the SAME
            // dash system X uses (prototype §11 — not a separate mechanic).
            if (thrustPressed)
            {
                if (!_avatar.Grounded && Time.unscaledTime - _lastThrustPress < 0.3f)
                {
                    dashPressed = true;
                }

                _lastThrustPress = Time.unscaledTime;
            }

            // Camera-relative world movement.
            var forward = _camera.FlatForward;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var worldMove = right * move.x + forward * move.y;

            var enemyAlive = _enemy.Health.State != HealthState.Dead;
            var toEnemy = _enemy.Position - _avatar.Position;
            toEnemy.y = 0f;

            // Y: lock-on / switch. With a single opponent there is nobody to
            // switch to, so this just re-affirms the (always-on) lock with an
            // audible tick — kept for layout fidelity, invents no mechanic.
            if (lockPressed)
            {
                GameAudio.Sfx?.UiClick();
            }

            // X grounded = the garniture's charge attack (DOCTRINE §11; the
            // lock gate is moot in a duel — it's always on). Airborne X
            // falls through to the dash intent as before.
            if (dashPressed && _avatar.Grounded && enemyAlive && !_bomb.Aiming)
            {
                _avatar.TryCharge(_enemy);
                dashPressed = false;
            }

            // Left arm (RT): shield is a plain hold; bomb is hold-to-aim,
            // release-to-throw. Both are suppressed while melee or the
            // charge is busy (matching the prototype's !melee.busy gate) —
            // no shielding, parrying, or aiming mid-commitment.
            var actionBusy = _avatar.Melee.Busy || _avatar.Charge.Busy;
            var shieldHeld = _avatar.Loadout.HasShield && leftArmHeld && !actionBusy
                && _avatar.Health.State == HealthState.Active;
            if (_avatar.Loadout.HasBomb)
            {
                if (actionBusy && _bomb.Aiming)
                {
                    _bomb.CancelAim(); // melee/charge committed mid-aim: drop the reticule, no throw
                }
                else if (leftArmHeld && !_bomb.Aiming)
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

            // Right arm (B): gun fires while held; melee is edge-triggered.
            var firing = rightArmHeld && _avatar.Loadout.HasGun && !actionBusy;

            _avatar.Intent = new RoboIntent
            {
                MoveDir = worldMove,
                ThrustHeld = thrustHeld,
                DashRequested = dashPressed,
                MashPressed = thrustPressed,
                FiringGun = firing,
                ShieldHeld = shieldHeld,
                // Only a bomb mid-aim roots via LeftArmActive; the shield
                // roots through the rig's actual raised state, so holding
                // through the toll leaves you free to move.
                LeftArmActive = _bomb.Aiming,
                // Free facing: face movement normally, square up while attacking.
                HasFaceYaw = (firing || actionBusy || _avatar.ShieldRaised || _bomb.Aiming) && enemyAlive && toEnemy.sqrMagnitude > 0.0001f,
                FaceYaw = Mathf.Atan2(toEnemy.x, toEnemy.z),
                HasDashHoming = enemyAlive,
                DashHomingPoint = _enemy.Position
            };

            _avatar.TickGun(dt, firing, enemyAlive ? _enemy : null);

            if (podPressed)
            {
                _pod.Toggle();
            }

            // Manual aim steering: holding the pod or bomb input redirects
            // the stick to steer the launch direction / nudge the reticule.
            if (podHeld)
            {
                _pod.SteerAim(worldMove, dt);
            }
            else
            {
                _pod.ClearAim();
            }

            if (_bomb.Aiming)
            {
                _bomb.SteerAim(worldMove, dt);
            }

            // Melee on the same B button (edge-triggered): swing, or chain
            // the string if already mid-melee.
            if (rightArmPressed && _avatar.Loadout.HasMelee && enemyAlive)
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
