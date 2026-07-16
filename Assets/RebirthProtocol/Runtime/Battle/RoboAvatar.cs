using RebirthProtocol.Domain;
using UnityEngine;

namespace RebirthProtocol.Battle
{
    public struct RoboIntent
    {
        public Vector3 MoveDir; // world-space, horizontal, magnitude <= 1
        public bool ThrustHeld;
        public bool DashRequested;
        public bool MashPressed;
        public bool FiringGun;
        public bool ShieldHeld; // only meaningful with a shield left arm

        /// Shield held or a bomb being aimed: hard-halts all horizontal
        /// momentum (even mid-air) and blocks dashing entirely.
        public bool LeftArmActive;
        public bool HasFaceYaw;
        public float FaceYaw;
        public bool HasDashHoming;
        public Vector3 DashHomingPoint;
    }

    public enum ReceiveResult
    {
        Hit,
        Knockdown,
        Killed,
        Invulnerable,
        Evaded,
        Shielded,
        GuardBreak
    }

    // One robo: CharacterController motor + boost economy + health + the
    // five-slot loadout's weapons. Port of the prototype's Robo.ts. Driven
    // manually by DuelManager.Tick so update order stays deterministic.
    public sealed class RoboAvatar : MonoBehaviour
    {
        private const float CenterHeight = 1f;

        public Loadout Loadout { get; private set; }
        public RoboStats Stats { get; private set; }
        public CombatantHealth Health { get; private set; }
        public BoostGauge Boost { get; private set; }
        public GunCycle Gun { get; private set; }
        public MeleeAction Melee { get; private set; }
        public RoboIntent Intent;
        public bool Grounded { get; private set; } = true;

        // Shield state (§3.2: engaged-only, regenerating, breaks into knockdown).
        public float ShieldHp { get; private set; }
        private float _shieldHitTimer = float.PositiveInfinity;

        private CharacterController _cc;
        private ProjectileSystem _projectiles;
        private DashProfile _dashProfile;
        private Vector3 _velocity;
        private Vector3 _dashDir = Vector3.forward;
        private float _dashTimer;
        private Vector3 _knockback;
        private Vector3? _externalMove;
        private float _actionLock;
        private float _facing;
        private float _flashTime;

        private Transform _visualRoot;
        private Transform _tiltRoot;
        private Renderer[] _renderers;
        private Transform _blade;

        public Vector3 Position => transform.position;
        public Vector3 Center => transform.position + Vector3.up * CenterHeight;
        public float Facing => _facing;
        public Vector3 FacingDir => new Vector3(Mathf.Sin(_facing), 0f, Mathf.Cos(_facing));

        public bool ControlLocked =>
            Boost.LandingRecovery > 0f
            || _actionLock > 0f
            || Health.State == HealthState.KnockedDown
            || Health.State == HealthState.Dead
            || _externalMove.HasValue;

        /// Vanish-dash i-frames: attacks pass straight through.
        public bool Intangible => Stats.DashType == DashType.Vanish && _dashTimer > 0f;

        public void Init(Loadout loadout, Color hull, Color accent, ProjectileSystem projectiles, float spawnFacing)
        {
            Loadout = loadout;
            Stats = PartsCatalog.ComputeStats(loadout);
            _dashProfile = DashProfile.For(Stats.DashType);
            _projectiles = projectiles;
            _facing = spawnFacing;
            Health = new CombatantHealth(new HealthTuning { MaxHp = Stats.MaxHp });
            Boost = new BoostGauge();
            Gun = new GunCycle(loadout.HasGun ? loadout.Gun.FireInterval : 0.38f);
            Melee = new MeleeAction(loadout.HasMelee ? loadout.Melee.ToTuning() : null);
            ShieldHp = loadout.HasShield ? loadout.Shield.ShieldHp : 0f;

            _cc = gameObject.AddComponent<CharacterController>();
            _cc.height = 2f;
            _cc.radius = 0.5f;
            _cc.center = new Vector3(0f, CenterHeight, 0f);
            _cc.skinWidth = 0.08f;
            _cc.stepOffset = 0.5f;

            BuildVisual(hull, accent);
        }

        public float FlatDistanceTo(RoboAvatar other)
        {
            var to = other.Position - Position;
            to.y = 0f;
            return to.magnitude;
        }

        /// All incoming damage routes through here so the shield can
        /// intercept. fromDir: attack's direction of travel (attacker ->
        /// victim). Port of Robo.ts::receiveHit.
        public ReceiveResult ReceiveHit(float damage, float enduranceDamage, Vector3 fromDir)
        {
            if (Intangible)
            {
                return ReceiveResult.Evaded;
            }

            if (Health.State != HealthState.Active)
            {
                return ReceiveResult.Invulnerable;
            }

            var scaledDamage = damage * Stats.DefMult;

            // Shield (§3.2): must be actively engaged to block at all. Even
            // engaged, block is never 100% (chip always lands), and a hit
            // from behind blocks far less than one from the front.
            if (Loadout.HasShield && Intent.ShieldHeld && ShieldHp > 0f)
            {
                var shield = Loadout.Shield;
                var incoming = -new Vector3(fromDir.x, 0f, fromDir.z).normalized;
                var isFront = Vector3.Angle(FacingDir, incoming) <= 90f;
                var blockPercent = isFront ? shield.FrontBlockPercent : shield.BackBlockPercent;

                var chipResult = Health.TakeHit(
                    scaledDamage * (1f - blockPercent),
                    enduranceDamage * (1f - blockPercent));
                _shieldHitTimer = 0f;

                if (chipResult is HitResult.Killed or HitResult.Knockdown)
                {
                    return chipResult == HitResult.Killed ? ReceiveResult.Killed : ReceiveResult.Knockdown;
                }

                ShieldHp -= scaledDamage * blockPercent;
                if (ShieldHp <= 0f)
                {
                    // Guard break feeds the existing knockdown state -- no
                    // second free defense stacked on rebirth (§3.2).
                    ShieldHp = 0f;
                    Health.ForceKnockdown();
                    return ReceiveResult.GuardBreak;
                }

                return ReceiveResult.Shielded;
            }

            var result = Health.TakeHit(scaledDamage, enduranceDamage);
            if (result != HitResult.Invulnerable)
            {
                ApplyKnockback(fromDir, 2f);
            }

            return result switch
            {
                HitResult.Killed => ReceiveResult.Killed,
                HitResult.Knockdown => ReceiveResult.Knockdown,
                HitResult.Invulnerable => ReceiveResult.Invulnerable,
                _ => ReceiveResult.Hit
            };
        }

        public void ApplyKnockback(Vector3 dir, float speed)
        {
            dir.y = 0f;
            _knockback = dir.normalized * speed;
        }

        public void SetFacing(float yaw)
        {
            _facing = yaw;
        }

        // --- Gun ---

        public void TickGun(float dt, bool firing, RoboAvatar target)
        {
            Gun.Tick(dt);
            if (!Loadout.HasGun || !firing || Melee.Busy || ControlLocked)
            {
                return;
            }

            if (!Gun.TryFire())
            {
                return;
            }

            var part = Loadout.Gun;
            var muzzle = Position + FacingDir * 0.8f + Vector3.up * CombatTuning.Gun.MuzzleHeight;
            var targetAlive = target != null && target.Health.State != HealthState.Dead;
            var aim = targetAlive
                ? target.Position + Vector3.up * 1.0f
                : Position + FacingDir * 10f + Vector3.up * CombatTuning.Gun.MuzzleHeight;
            _projectiles.Spawn(this, targetAlive ? target : null, muzzle, aim,
                part.Damage * Stats.AtkMult, part.EnduranceDamage, part.ProjectileSpeed,
                targetAlive ? part.HomingTurnRate : 0f);
        }

        // --- Melee ---

        public void TryMelee(RoboAvatar target)
        {
            if (!Loadout.HasMelee || ControlLocked || Melee.Busy)
            {
                return;
            }

            if (Melee.TryStart(FlatDistanceTo(target)))
            {
                OnMeleePhaseEntered(target);
            }
        }

        /// Chains are allowed during recovery (which control-locks), so this
        /// deliberately skips the ControlLocked gate — MeleeAction itself
        /// only permits chaining after a connected swing.
        public void TryMeleeChain(RoboAvatar target)
        {
            if (!Loadout.HasMelee)
            {
                return;
            }

            if (Melee.TryChain(FlatDistanceTo(target)))
            {
                OnMeleePhaseEntered(target);
            }
        }

        public void TickMelee(float dt, RoboAvatar target)
        {
            if (!Melee.Busy)
            {
                return;
            }

            if (Health.State is HealthState.KnockedDown or HealthState.Dead)
            {
                CancelMelee();
                return;
            }

            var to = target.Position - Position;
            to.y = 0f;
            var dist = to.magnitude;

            if (Melee.Phase == MeleePhase.Lunge)
            {
                // Lunge owns movement: track the target, close the gap.
                if (to.sqrMagnitude > 0.0001f)
                {
                    _facing = Mathf.Atan2(to.x, to.z);
                }

                _externalMove = to.normalized * Melee.Tuning.LungeSpeed;
            }
            else if (Melee.Phase == MeleePhase.Swing)
            {
                // Hit check BEFORE the timer decrements (matching Melee.ts):
                // a hitch frame with dt >= SwingActiveTime must not let an
                // in-range swing expire into recovery unchecked.
                TryApplyMeleeHit(target);
            }

            var ev = Melee.Tick(dt, dist);
            switch (ev)
            {
                case MeleeTickEvent.EnteredSwing:
                    _externalMove = null;
                    _actionLock = 10f; // held while swing+recovery run
                    TryApplyMeleeHit(target);
                    break;
                case MeleeTickEvent.EnteredRecovery:
                    _externalMove = null;
                    _actionLock = 10f;
                    break;
                case MeleeTickEvent.Ended:
                    _externalMove = null;
                    _actionLock = 0f;
                    break;
            }
        }

        private void OnMeleePhaseEntered(RoboAvatar target)
        {
            if (Melee.Phase == MeleePhase.Lunge)
            {
                var to = target.Position - Position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.0001f)
                {
                    _facing = Mathf.Atan2(to.x, to.z);
                }
            }
            else if (Melee.Phase == MeleePhase.Swing)
            {
                _externalMove = null;
                _actionLock = 10f;
            }
        }

        private void TryApplyMeleeHit(RoboAvatar target)
        {
            var to = target.Position - Position;
            to.y = 0f;
            if (to.magnitude > Melee.Tuning.HitRange)
            {
                return;
            }

            var angleTo = Mathf.Atan2(to.x, to.z);
            var diff = Mathf.DeltaAngle(_facing * Mathf.Rad2Deg, angleTo * Mathf.Rad2Deg);
            if (Mathf.Abs(diff) > Melee.Tuning.HitArcDegrees * 0.5f)
            {
                return;
            }

            if (!Melee.TryRegisterHit())
            {
                return;
            }

            var dir = to.normalized;
            var result = target.ReceiveHit(
                Melee.Tuning.Damage * Melee.ComboDamageMult * Stats.AtkMult,
                Melee.Tuning.EnduranceDamage * Melee.ComboEnduranceMult,
                dir);
            if (result is not ReceiveResult.Invulnerable and not ReceiveResult.Evaded)
            {
                target.ApplyKnockback(dir, Melee.Tuning.KnockbackSpeed * Melee.ComboKnockbackMult);
            }

            // Shield parry (§3.2): a melee hit blocked by an ENGAGED shield
            // drains the attacker's own endurance.
            if (result is ReceiveResult.Shielded or ReceiveResult.GuardBreak
                && target.Loadout.HasShield && target.Intent.ShieldHeld)
            {
                Health.DrainEndurance(target.Loadout.Shield.MeleeParryEnduranceDamage);
            }
        }

        private void CancelMelee()
        {
            Melee.Cancel();
            _externalMove = null;
            _actionLock = 0f;
        }

        /// Melee clash (GAME_DESIGN §3.1): both attacks cancel into a short
        /// step-cancel window — whoever re-engages faster wins the exchange.
        public void ClashCancel()
        {
            Melee.Cancel();
            _externalMove = null;
            _actionLock = 0.25f;
        }

        // --- Motor: port of Robo.update ---

        public void TickMotor(float dt)
        {
            Health.Tick(dt);
            if (Intent.MashPressed)
            {
                Health.Mash();
            }

            var downed = Health.State is HealthState.KnockedDown or HealthState.Dead;

            // Shield regen: stops while recently hit, never mid-knockdown.
            _shieldHitTimer += dt;
            if (Loadout.HasShield)
            {
                var shield = Loadout.Shield;
                if (Health.State == HealthState.Active && _shieldHitTimer > shield.RegenDelay && ShieldHp < shield.ShieldHp)
                {
                    ShieldHp = Mathf.Min(shield.ShieldHp, ShieldHp + shield.RegenPerSec * dt);
                }
            }

            Boost.Tick(dt);
            if (_actionLock > 0f)
            {
                _actionLock -= dt;
            }

            // --- Horizontal movement ---
            Vector3 horiz;
            if (_externalMove.HasValue)
            {
                // Melee lunge owns movement this step.
                horiz = _externalMove.Value;
                _velocity.y = 0f;
            }
            else if (downed || Boost.LandingRecovery > 0f || _actionLock > 0f)
            {
                // No control: drift to a stop.
                _velocity.x *= CombatTuning.Move.NoControlDamping;
                _velocity.z *= CombatTuning.Move.NoControlDamping;
                horiz = new Vector3(_velocity.x, 0f, _velocity.z);
            }
            else if (_dashTimer > 0f)
            {
                _dashTimer -= dt;
                if (Intent.HasDashHoming)
                {
                    // Homing dash: curve toward the locked target mid-dash.
                    var toTarget = Intent.DashHomingPoint - Position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 1f)
                    {
                        _dashDir = RotateFlat(_dashDir, toTarget.normalized, CombatTuning.Dash.HomingTurnRate * dt);
                    }
                }

                horiz = _dashDir * _dashProfile.Speed;
                _velocity.y = 0f; // dashes are horizontal; gravity suspended
                _velocity.x = horiz.x;
                _velocity.z = horiz.z;
            }
            else if (Intent.LeftArmActive)
            {
                // Bomb aiming / shield engaged: instantly halts all
                // horizontal momentum, even mid-air. Rooted and vulnerable.
                _velocity.x = 0f;
                _velocity.z = 0f;
                horiz = Vector3.zero;
            }
            else if (Grounded)
            {
                var desired = Intent.MoveDir * Stats.RunSpeed;
                if (Intent.FiringGun)
                {
                    // Firing on the ground: momentum carries, a "slide".
                    var t = Mathf.Min(1f, CombatTuning.Move.FireSlideCorrection * dt);
                    _velocity.x += (desired.x - _velocity.x) * t;
                    _velocity.z += (desired.z - _velocity.z) * t;
                }
                else
                {
                    _velocity.x = desired.x;
                    _velocity.z = desired.z;
                }

                horiz = new Vector3(_velocity.x, 0f, _velocity.z);
            }
            else
            {
                // Air steering; firing mid-air halts drift instead.
                var desired = Intent.FiringGun ? Vector3.zero : Intent.MoveDir * CombatTuning.Move.AirControlSpeed;
                var rate = Intent.FiringGun ? CombatTuning.Move.FireAirHaltRate : CombatTuning.Move.AirSteerRate;
                var t = Mathf.Min(1f, rate * dt);
                _velocity.x += (desired.x - _velocity.x) * t;
                _velocity.z += (desired.z - _velocity.z) * t;
                horiz = new Vector3(_velocity.x, 0f, _velocity.z);
            }

            // --- Knockback decay on top of movement ---
            horiz += _knockback;
            _knockback *= Mathf.Max(0f, 1f - CombatTuning.Move.KnockbackDecayRate * dt);

            // --- Boost: thrust / dash ---
            var canBoost = !downed && Boost.CanBoost && _actionLock <= 0f && !_externalMove.HasValue;

            if (canBoost && Intent.ThrustHeld && _dashTimer <= 0f)
            {
                _velocity.y = Stats.JumpThrust;
                Boost.SpendThrust(dt);
            }

            if (canBoost && Intent.DashRequested && _dashTimer <= 0f && !Intent.LeftArmActive
                && Boost.TrySpendAirDash(_dashProfile.Cost, Stats.DashCount))
            {
                _dashDir = Intent.MoveDir.sqrMagnitude > 0.01f ? Intent.MoveDir.normalized : FacingDir;
                _dashTimer = _dashProfile.Duration;
                if (Grounded)
                {
                    _velocity.y = CombatTuning.Dash.GroundDashHop; // ground dash lifts into a hop
                }
            }

            // --- Gravity ---
            if (_dashTimer <= 0f && !_externalMove.HasValue)
            {
                _velocity.y += CombatTuning.Move.Gravity * dt;
                if (Grounded && _velocity.y < 0f)
                {
                    _velocity.y = -2f; // stick to the ground
                }
            }

            // --- Move ---
            var wasGrounded = Grounded;
            _cc.Move(new Vector3(horiz.x, _velocity.y, horiz.z) * dt);
            Grounded = _cc.isGrounded;
            if (Grounded && _velocity.y < 0f)
            {
                _velocity.y = 0f;
            }

            // --- Landing: recovery scales with spend, gauge refills ---
            if (!wasGrounded && Grounded)
            {
                Boost.NotifyLanded(Stats.LandRecoveryMult);
                _dashTimer = 0f;
            }

            // --- Facing (frozen mid-swing/recovery: commitment is punishable) ---
            if (!downed && !_externalMove.HasValue && _actionLock <= 0f)
            {
                var target = Intent.HasFaceYaw
                    ? Intent.FaceYaw
                    : Intent.MoveDir.sqrMagnitude > 0.01f
                        ? Mathf.Atan2(Intent.MoveDir.x, Intent.MoveDir.z)
                        : _facing;
                _facing = DampAngle(_facing, target, CombatTuning.Move.TurnRate, dt);
            }

            SyncVisual(dt);
        }

        // --- Visuals: primitive-body slice, real art comes later ---

        private void BuildVisual(Color hull, Color accent)
        {
            _visualRoot = new GameObject("Visual").transform;
            _visualRoot.SetParent(transform, false);
            _tiltRoot = new GameObject("Tilt").transform;
            _tiltRoot.SetParent(_visualRoot, false);
            _tiltRoot.localPosition = new Vector3(0f, CenterHeight, 0f);

            AddPart(PrimitiveType.Capsule, new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f), hull);
            AddPart(PrimitiveType.Cube, new Vector3(0f, 0.75f, 0.1f), new Vector3(0.5f, 0.35f, 0.5f), hull);
            AddPart(PrimitiveType.Cube, new Vector3(0f, 0.78f, 0.32f), new Vector3(0.36f, 0.1f, 0.05f), accent); // visor
            AddPart(PrimitiveType.Cube, new Vector3(-0.62f, 0.35f, 0f), new Vector3(0.3f, 0.45f, 0.35f), hull); // pauldrons
            AddPart(PrimitiveType.Cube, new Vector3(0.62f, 0.35f, 0f), new Vector3(0.3f, 0.45f, 0.35f), hull);
            AddPart(PrimitiveType.Cube, new Vector3(0f, 0.1f, 0.42f), new Vector3(0.4f, 0.5f, 0.12f), accent); // chest panel

            _blade = AddPart(PrimitiveType.Cube, new Vector3(0f, 0.2f, 1.4f), new Vector3(0.15f, 0.5f, 2.2f), new Color(1f, 0.93f, 0.4f)).transform;
            _blade.gameObject.SetActive(false);

            _renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);
        }

        private GameObject AddPart(PrimitiveType type, Vector3 localPos, Vector3 scale, Color color)
        {
            var part = GameObject.CreatePrimitive(type);
            Destroy(part.GetComponent<Collider>()); // CharacterController is the only collider
            part.transform.SetParent(_tiltRoot, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<Renderer>();
            renderer.material = BattleMaterials.Lit(color);
            return part;
        }

        private void SyncVisual(float dt)
        {
            _visualRoot.localRotation = Quaternion.Euler(0f, _facing * Mathf.Rad2Deg, 0f);

            // Knockdown: fall over backward. Otherwise stand.
            var targetTilt = Health.State == HealthState.KnockedDown ? -90f : 0f;
            var current = _tiltRoot.localEulerAngles.x;
            if (current > 180f)
            {
                current -= 360f;
            }

            _tiltRoot.localRotation = Quaternion.Euler(
                current + (targetTilt - current) * Mathf.Min(1f, 10f * dt), 0f, 0f);

            // Rebirth / knockdown / vanish-dash: blink.
            _flashTime += dt;
            var flashing = Health.State is HealthState.Rebirth or HealthState.KnockedDown || Intangible;
            var visible = !flashing || Mathf.Sin(_flashTime * 25f) > 0f;
            foreach (var r in _renderers)
            {
                if (r.transform != _blade)
                {
                    r.enabled = visible;
                }
            }

            _blade.gameObject.SetActive(Melee.Phase == MeleePhase.Swing);

            // Dead: sink and fade.
            if (Health.State == HealthState.Dead)
            {
                _visualRoot.localScale *= Mathf.Max(0f, 1f - 1.5f * dt);
            }
        }

        private static float DampAngle(float from, float to, float rate, float dt)
        {
            var diff = Mathf.DeltaAngle(from * Mathf.Rad2Deg, to * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            return from + diff * Mathf.Min(1f, rate * dt);
        }

        private static Vector3 RotateFlat(Vector3 current, Vector3 desired, float maxAngle)
        {
            var cur = Mathf.Atan2(current.x, current.z);
            var des = Mathf.Atan2(desired.x, desired.z);
            var diff = Mathf.DeltaAngle(cur * Mathf.Rad2Deg, des * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            var angle = cur + Mathf.Clamp(diff, -maxAngle, maxAngle);
            return new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        }
    }
}
