using RebirthProtocol.Domain;
using UnityEngine;

namespace RebirthProtocol.Battle
{
    // Port of the prototype's DummyAI: orbits at mid range, strafes, jumps
    // and dashes occasionally, fires in bursts, lobs its bomb off cooldown
    // (or raises its shield periodically at close range if that's its left
    // arm instead), keeps its pod deployed, and goes for melee when the
    // player is close or landing-recovery vulnerable. Deterministic: all
    // randomness from one seeded Random.
    public sealed class EnemyBrain : MonoBehaviour
    {
        private RoboAvatar _avatar;
        private RoboAvatar _player;
        private BombSystem _bomb;
        private PodSystem _pod;
        private System.Random _rng;

        private float _strafeSign = 1f;
        private float _decisionTimer;
        private bool _firing;
        private float _fireTimer;
        private float _meleeTimer = 2f;
        private float _chargeTimer = 4f; // don't open with a charge
        private bool _thrustHeld;
        private float _bombTimer = 3f; // don't open with a bomb
        private bool _bombAiming;
        private float _bombAimTimer;
        private float _shieldTimer = 1f;
        private bool _shieldEngaged;

        public void Init(RoboAvatar avatar, RoboAvatar player, BombSystem bomb, PodSystem pod, int seed)
        {
            _avatar = avatar;
            _player = player;
            _bomb = bomb;
            _pod = pod;
            _rng = new System.Random(seed);
        }

        private float NextFloat() => (float)_rng.NextDouble();

        public void Tick(float dt)
        {
            var toPlayer = _player.Position - _avatar.Position;
            toPlayer.y = 0f;
            var dist = toPlayer.magnitude;
            var dirToPlayer = dist > 0.0001f ? toPlayer / dist : Vector3.forward;

            // Rethink strafe direction periodically.
            _decisionTimer -= dt;
            if (_decisionTimer <= 0f)
            {
                _decisionTimer = CombatTuning.Ai.DecisionInterval * (0.6f + NextFloat() * 0.8f);
                if (NextFloat() < 0.4f)
                {
                    _strafeSign = -_strafeSign;
                }
            }

            // Movement: hold the orbit distance band, strafe around the player.
            var move = Vector3.zero;
            var strafe = new Vector3(-dirToPlayer.z * _strafeSign, 0f, dirToPlayer.x * _strafeSign);
            if (dist > CombatTuning.Ai.OrbitRadiusMax)
            {
                move += dirToPlayer;
            }
            else if (dist < CombatTuning.Ai.OrbitRadiusMin)
            {
                move -= dirToPlayer;
            }

            move = (move + strafe).normalized;

            // Occasional jump / dash to be a harder target.
            _thrustHeld = _thrustHeld && _avatar.Boost.Value > 20f
                ? NextFloat() > 0.1f // keep short hops short
                : NextFloat() < CombatTuning.Ai.JumpChancePerSec * dt;

            var playerAlive = _player.Health.State != HealthState.Dead;

            // Melee: punish a close player, especially during landing recovery.
            _meleeTimer -= dt;
            if (_avatar.Loadout.HasMelee && playerAlive && !_avatar.Melee.Busy && _meleeTimer <= 0f && dist < 10f
                && (_player.Boost.LandingRecovery > 0f || NextFloat() < 0.5f))
            {
                _avatar.TryMelee(_player);
                _meleeTimer = 2.5f + NextFloat() * 2f;
            }

            // Sometimes press the string through (~7 attempts/s in recovery).
            if (_avatar.Melee.Busy && NextFloat() < 7f * dt)
            {
                _avatar.TryMeleeChain(_player);
            }

            // Occasional garniture charge from mid range (DOCTRINE §4.5) —
            // enough for the mechanic to show up in a fight; real charge
            // discipline is the Pass O AI-archetype work.
            _chargeTimer -= dt;
            if (playerAlive && _chargeTimer <= 0f && _avatar.Grounded
                && !_avatar.Melee.Busy && !_avatar.Charge.Busy
                && dist > 3f && dist < 11f && NextFloat() < 0.5f)
            {
                _avatar.TryCharge(_player);
                _chargeTimer = 5f + NextFloat() * 4f;
            }

            // Fire the gun in bursts.
            _fireTimer -= dt;
            if (_fireTimer <= 0f)
            {
                _firing = !_firing;
                _fireTimer = _firing ? CombatTuning.Ai.BurstDuration : CombatTuning.Ai.FireInterval;
            }

            var gunFiring = _avatar.Loadout.HasGun && _firing && playerAlive && !_avatar.Melee.Busy;

            // Left arm: bomb OR shield, whichever this build actually has.
            var shieldHeld = false;
            if (_avatar.Loadout.HasShield)
            {
                _shieldTimer -= dt;
                if (_shieldTimer <= 0f)
                {
                    if (!_shieldEngaged && !_avatar.ShieldReady)
                    {
                        _shieldTimer = 0.3f; // toll still running: check back shortly
                    }
                    else
                    {
                        _shieldEngaged = !_shieldEngaged;
                        _shieldTimer = _shieldEngaged
                            ? 1.0f + NextFloat() // hold it up for a beat
                            : 0.6f + NextFloat() * 0.8f; // then rest -- the toll makes lowering a commitment
                    }
                }

                shieldHeld = _shieldEngaged && dist < 10f && !_avatar.Melee.Busy
                    && _avatar.Health.State == HealthState.Active;
            }
            else if (_avatar.Loadout.HasBomb)
            {
                _bombTimer -= dt;
                if (!_bombAiming && _bombTimer <= 0f && playerAlive && _bomb.Ready && dist < 18f)
                {
                    if (_bomb.StartAim(_player))
                    {
                        _bombAiming = true;
                        _bombAimTimer = 0.25f + NextFloat() * 0.3f; // hold, then release
                    }
                }

                if (_bombAiming)
                {
                    _bomb.UpdateAim(_player);
                    _bombAimTimer -= dt;
                    if (_bombAimTimer <= 0f || !playerAlive)
                    {
                        _bomb.Release();
                        _bombAiming = false;
                        _bombTimer = 2f + NextFloat() * 3f;
                    }
                }

                if (_bombAiming && !_bomb.Aiming)
                {
                    _bombAiming = false; // knocked down mid-aim: bomb canceled itself
                }
            }

            _avatar.Intent = new RoboIntent
            {
                MoveDir = move,
                ThrustHeld = _thrustHeld,
                DashRequested = NextFloat() < CombatTuning.Ai.DashChancePerSec * dt,
                MashPressed = NextFloat() < 8f * dt, // ~8 mash/s
                FiringGun = gunFiring,
                ShieldHeld = shieldHeld,
                LeftArmActive = _bombAiming, // shields root via the rig's raised state

                HasFaceYaw = true,
                FaceYaw = Mathf.Atan2(dirToPlayer.x, dirToPlayer.z),
                HasDashHoming = playerAlive,
                DashHomingPoint = _player.Position
            };

            _avatar.TickGun(dt, gunFiring, playerAlive ? _player : null);

            // Keep the pod out.
            if (!_pod.Deployed && _avatar.Health.State == HealthState.Active)
            {
                _pod.Toggle();
            }
        }
    }
}
