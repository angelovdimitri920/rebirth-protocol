using RebirthProtocol.Battle.Audio;
using RebirthProtocol.Battle.Effects;
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

        /// Desire to raise the shield (only meaningful with a shield left
        /// arm). Whether it actually comes up is the ShieldRig's call — the
        /// toll gates it (ARMORY_REFERENCE §2.3).
        public bool ShieldHeld;
        public bool FiringGun;

        /// A bomb being aimed: hard-halts all horizontal momentum (even
        /// mid-air) and blocks dashing entirely. A raised shield roots via
        /// ShieldRaised instead, so a hold during the toll costs nothing.
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

        /// The player's run boons/items; null for the enemy (boons apply to
        /// the player only, GAME_DESIGN §4 Stage 3).
        public RunEffects Effects;

        /// Afterimage boon hook: the run flow points this at the
        /// AfterimageSystem so a dash can drop one at the launch position.
        public System.Action<Vector3> OnAfterimageSpawn;

        /// Standing on an ice hazard (set by DuelManager per frame):
        /// momentum carries and steering becomes a slow correction.
        public bool OnIce;

        /// Cooldown for the lava hazard's sizzle cue (DuelManager-owned,
        /// mirrors the prototype's per-robo WeakMap cooldown).
        public float LavaSoundCooldown;

        // Shield state (§3.2 + ARMORY §2.3): engaged-only, regenerating,
        // breaks into knockdown, tolls on lower/break. Null without a shield
        // left arm.
        public ShieldRig Shield { get; private set; }
        public float ShieldHp => Shield?.Hp ?? 0f;
        public bool ShieldRaised => Shield != null && Shield.Raised;
        public bool ShieldReady => Shield != null && Shield.Ready;

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
        private Transform _shieldPlate;
        private Vector3 _shieldRestPos;
        private bool _wasThrusting;
        private bool _wasOverheated;

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

        public void Init(Loadout loadout, Color hull, Color accent, ProjectileSystem projectiles, float spawnFacing,
            float powerMult = 1f)
        {
            Loadout = loadout;
            Stats = PartsCatalog.ComputeStats(loadout, powerMult);
            _dashProfile = DashProfile.For(Stats.DashType);
            _projectiles = projectiles;
            _facing = spawnFacing;
            Health = new CombatantHealth(new HealthTuning { MaxHp = Stats.MaxHp });
            Boost = new BoostGauge();
            Gun = new GunCycle(loadout.HasGun ? loadout.Gun.FireInterval : 0.38f);
            Melee = new MeleeAction(loadout.HasMelee ? loadout.Melee.ToTuning() : null);
            Shield = loadout.HasShield ? new ShieldRig(loadout.Shield) : null;

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
        /// victim). isBlast marks AoE/through-wall damage (bomb blasts) for
        /// the Quiet Bell's all-sides muffle. Port of Robo.ts::receiveHit.
        public ReceiveResult ReceiveHit(float damage, float enduranceDamage, Vector3 fromDir,
            float shieldDamageMult = 1f, bool isBlast = false)
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

            // Shield (§3.2): must be actively RAISED to block at all — the
            // rig owns that state, so a held input during the toll blocks
            // nothing. Even raised, block is never 100% (chip always lands),
            // and a hit from behind blocks far less than one from the front.
            if (ShieldRaised)
            {
                var incoming = -new Vector3(fromDir.x, 0f, fromDir.z).normalized;
                var isFront = Vector3.Angle(FacingDir, incoming) <= 90f;
                var blockPercent = Shield.BlockPercent(isFront, isBlast);

                var chipResult = Health.TakeHit(
                    scaledDamage * (1f - blockPercent),
                    enduranceDamage * (1f - blockPercent));
                Shield.NotifyBlockedHit();

                if (chipResult is HitResult.Killed or HitResult.Knockdown)
                {
                    if (chipResult == HitResult.Killed)
                    {
                        GameAudio.Sfx?.Eliminate(Position);
                        GameEffects.Fx?.Explosion(Center, 2.4f);
                        return ReceiveResult.Killed;
                    }

                    GameAudio.Sfx?.Knockdown(Position);
                    GameEffects.Fx?.ImpactSpark(Center, incoming, new Color(1f, 0.7f, 0.3f), 0.28f);
                    return ReceiveResult.Knockdown;
                }

                // Guard Crusher boon: multiplies only how fast the shield's
                // own pool drains, never the chip that gets through.
                if (Shield.Drain(scaledDamage * blockPercent * shieldDamageMult))
                {
                    // Guard break feeds the existing knockdown state -- no
                    // second free defense stacked on rebirth (§3.2). The
                    // break also started the shield's toll.
                    Health.ForceKnockdown();
                    GameAudio.Sfx?.GuardBreak(Position);
                    GameEffects.Fx?.ImpactSpark(Center + FacingDir, incoming, new Color(1f, 0.85f, 0.4f), 0.3f);
                    return ReceiveResult.GuardBreak;
                }

                GameAudio.Sfx?.Shielded(Position);
                GameEffects.Fx?.ImpactSpark(Center + FacingDir * 0.7f + Vector3.up * 0.1f, incoming, new Color(0.6f, 0.9f, 1f));
                return ReceiveResult.Shielded;
            }

            var result = Health.TakeHit(scaledDamage, enduranceDamage);
            if (result != HitResult.Invulnerable)
            {
                ApplyKnockback(fromDir, 2f);
            }

            var back = -new Vector3(fromDir.x, 0f, fromDir.z).normalized;
            switch (result)
            {
                case HitResult.Killed:
                    GameAudio.Sfx?.Eliminate(Position);
                    GameEffects.Fx?.Explosion(Center, 2.4f); // death blast
                    return ReceiveResult.Killed;
                case HitResult.Knockdown:
                    GameAudio.Sfx?.Knockdown(Position);
                    GameEffects.Fx?.ImpactSpark(Center, back, new Color(1f, 0.7f, 0.3f), 0.3f);
                    return ReceiveResult.Knockdown;
                case HitResult.Invulnerable:
                    return ReceiveResult.Invulnerable;
                default:
                    GameAudio.Sfx?.Hit(Position);
                    GameEffects.Fx?.ImpactSpark(Center, back, new Color(1f, 0.82f, 0.45f), 0.05f);
                    return ReceiveResult.Hit;
            }
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

            GameAudio.Sfx?.Shot(Position);
            var part = Loadout.Gun;
            var muzzle = Position + FacingDir * 0.8f + Vector3.up * CombatTuning.Gun.MuzzleHeight;
            GameEffects.Fx?.MuzzleFlash(muzzle, FacingDir,
                CompareTag("Player") ? new Color(0.55f, 0.85f, 1f) : new Color(1f, 0.6f, 0.35f));
            var targetAlive = target != null && target.Health.State != HealthState.Dead;
            var aim = targetAlive
                ? target.Position + Vector3.up * 1.0f
                : Position + FacingDir * 10f + Vector3.up * CombatTuning.Gun.MuzzleHeight;
            var damage = (part.Damage * Stats.AtkMult + (Effects?.FlatDamageBonus() ?? 0f))
                * (Effects?.GunDamageMult(Boost.Value) ?? 1f);
            _projectiles.Spawn(this, targetAlive ? target : null, muzzle, aim,
                damage, part.EnduranceDamage, part.ProjectileSpeed,
                targetAlive ? part.HomingTurnRate : 0f, HitSource.Gun);
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
                    GameAudio.Sfx?.MeleeSwing(Position);
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
                // Already-in-range starts/chains enter Swing directly here —
                // the EnteredSwing tick event (and its own cue) only fires
                // for a lunge that closes the gap, so this is the only place
                // a direct close-range swing plays its start-up sound.
                GameAudio.Sfx?.MeleeSwing(Position);
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
            var damage = (Melee.Tuning.Damage * Melee.ComboDamageMult * Stats.AtkMult
                    + (Effects?.FlatDamageBonus() ?? 0f))
                * (Effects?.MeleeDamageMult() ?? 1f);
            var result = target.ReceiveHit(
                damage,
                Melee.Tuning.EnduranceDamage * Melee.ComboEnduranceMult,
                dir,
                Effects?.MeleeShieldMult() ?? 1f);
            if (result is not ReceiveResult.Invulnerable and not ReceiveResult.Evaded)
            {
                GameAudio.Sfx?.MeleeHit(target.Position);
                target.ApplyKnockback(dir, Melee.Tuning.KnockbackSpeed * Melee.ComboKnockbackMult);
                if (Effects != null)
                {
                    Effects.OnHit(HitSource.Melee);
                    if (result is ReceiveResult.Knockdown or ReceiveResult.GuardBreak)
                    {
                        Effects.OnKnockdown();
                    }
                }
            }

            // Shield parry (§3.2): a melee hit blocked by a RAISED shield
            // drains the attacker's own endurance. (A GuardBreak result
            // means the shield was raised at contact even though the rig
            // has already forced it down by now.)
            if (result is ReceiveResult.Shielded or ReceiveResult.GuardBreak
                && target.Loadout.HasShield)
            {
                var wasActive = Health.State == HealthState.Active;
                Health.DrainEndurance(target.Loadout.Shield.MeleeParryEnduranceDamage);
                if (wasActive && Health.State == HealthState.KnockedDown)
                {
                    GameAudio.Sfx?.Knockdown(Position);
                }
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
            var prevHealthState = Health.State;
            Health.Tick(dt);
            if (Intent.MashPressed)
            {
                if (Health.State == HealthState.KnockedDown)
                {
                    GameAudio.Sfx?.MashTick(Position);
                }

                Health.Mash();
            }

            // Knockdown/death cues fire at the hit site (ReceiveHit); the
            // stand-up transition is the only one that happens inside Tick.
            if (prevHealthState == HealthState.KnockedDown && Health.State == HealthState.Rebirth)
            {
                GameAudio.Sfx?.Rebirth(Position);
            }

            var downed = Health.State is HealthState.KnockedDown or HealthState.Dead;

            // Shield rig: raise/lower per intent, toll countdown, mend. A
            // knockdown mid-raise lowers the shield here — and that lowering
            // starts the toll like any other.
            Shield?.Tick(dt, Intent.ShieldHeld, Health.State == HealthState.Active);

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
            else if (Intent.LeftArmActive || ShieldRaised)
            {
                // Left arm committed: bomb aiming or shield raised. The
                // default (Root, and every bomb) instantly halts all
                // horizontal momentum, even mid-air — rooted and vulnerable.
                // Targe's March raise (ARMORY §7) instead walks at a
                // fraction of run speed while the shield is up.
                var shield = ShieldRaised ? Loadout.Shield : null;
                if (shield != null && Grounded && shield.GroundRaise == ShieldGroundRaise.March)
                {
                    var desired = Intent.MoveDir * (Stats.RunSpeed * shield.MarchSpeedMult);
                    _velocity.x = desired.x;
                    _velocity.z = desired.z;
                    horiz = desired;
                }
                else
                {
                    _velocity.x = 0f;
                    _velocity.z = 0f;
                    horiz = Vector3.zero;
                }
            }
            else if (Grounded)
            {
                var desired = Intent.MoveDir * Stats.RunSpeed;
                if (OnIce)
                {
                    // Ice: momentum carries; steering is a slow correction.
                    var t = Mathf.Min(1f, 2.5f * dt);
                    _velocity.x += (desired.x - _velocity.x) * t;
                    _velocity.z += (desired.z - _velocity.z) * t;
                }
                else if (Intent.FiringGun)
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
                if (!_wasThrusting)
                {
                    GameAudio.Sfx?.Thrust(Position);
                }

                _wasThrusting = true;
                _velocity.y = Stats.JumpThrust;
                Boost.SpendThrust(dt);
            }
            else
            {
                _wasThrusting = false;
            }

            if (Boost.Overheated && !_wasOverheated)
            {
                GameAudio.Sfx?.Overheat(Position);
            }

            _wasOverheated = Boost.Overheated;

            if (canBoost && Intent.DashRequested && _dashTimer <= 0f && !Intent.LeftArmActive && !ShieldRaised
                && Boost.TrySpendAirDash(_dashProfile.Cost * (Effects?.DashCostMult() ?? 1f), Stats.DashCount))
            {
                _dashDir = Intent.MoveDir.sqrMagnitude > 0.01f ? Intent.MoveDir.normalized : FacingDir;
                _dashTimer = _dashProfile.Duration;
                GameAudio.Sfx?.Dash(Position);
                if (Grounded)
                {
                    _velocity.y = CombatTuning.Dash.GroundDashHop; // ground dash lifts into a hop
                }

                if (Effects != null && Effects.OnDash().SpawnAfterimage)
                {
                    OnAfterimageSpawn?.Invoke(Center);
                }
            }

            // --- Gravity (and the shield's air raise behaviors, §2.3) ---
            if (_dashTimer <= 0f && !_externalMove.HasValue)
            {
                if (ShieldRaised && !Grounded && Loadout.Shield.AirRaise == ShieldAirRaise.Hold)
                {
                    _velocity.y = 0f; // Air-hold: raising midair hovers you
                }
                else
                {
                    if (ShieldRaised && !Grounded && Loadout.Shield.AirRaise == ShieldAirRaise.Drop)
                    {
                        // Air-drop: raising midair slams you groundward.
                        _velocity.y = Mathf.Min(_velocity.y, CombatTuning.Shield.AirDropSpeed);
                    }

                    _velocity.y += CombatTuning.Move.Gravity * dt;
                    if (Grounded && _velocity.y < 0f)
                    {
                        _velocity.y = -2f; // stick to the ground
                    }
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
                var spentFraction = 1f - Boost.Value / Boost.Max;
                Boost.NotifyLanded(Stats.LandRecoveryMult);
                _dashTimer = 0f;
                if (spentFraction > 0.15f)
                {
                    GameAudio.Sfx?.Land(Position); // skip tiny hops, keep real landings audible
                }
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

            var parts = RoboVisual.Build(_tiltRoot, Loadout, accent);
            _blade = parts.Blade;
            _shieldPlate = parts.ShieldPlate;
            if (_shieldPlate != null)
            {
                _shieldRestPos = _shieldPlate.localPosition;
            }

            _renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);
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

            // Shield raise: plate swings forward while actually raised so
            // the opponent can read the block state at a glance (a hold
            // during the toll shows nothing — the shield really is down).
            if (_shieldPlate != null)
            {
                var raised = ShieldRaised;
                var targetPos = raised ? new Vector3(0f, 0.1f, 0.75f) : _shieldRestPos;
                var targetRot = raised ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
                var t = Mathf.Min(1f, 14f * dt);
                _shieldPlate.localPosition = Vector3.Lerp(_shieldPlate.localPosition, targetPos, t);
                _shieldPlate.localRotation = Quaternion.Slerp(_shieldPlate.localRotation, targetRot, t);
            }

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
