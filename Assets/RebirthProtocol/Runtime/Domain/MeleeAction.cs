using System;

namespace RebirthProtocol.Domain
{
    public enum MeleePhase
    {
        Idle,
        Lunge,
        Swing,
        Recovery
    }

    public enum MeleeTickEvent
    {
        None,
        LungeActive,
        EnteredSwing,
        SwingActive,
        EnteredRecovery,
        RecoveryActive,
        Ended
    }

    // Oathblade (prototype id "saber") + the shared gap-closer mechanics.
    public sealed class MeleeTuning
    {
        public float CloseRange = 4f;
        public float LungeRange = 15f;
        public float LungeSpeed = 26f;
        public float LungeMaxDuration = 0.65f;
        public float LungeReachDistance = 2.6f;
        public float Damage = 130f;
        public float EnduranceDamage = 55f;
        public float HitRange = 3.0f;
        public float HitArcDegrees = 70f;
        public float SwingActiveTime = 0.18f;
        public float HitRecovery = 0.45f;
        public float WhiffRecovery = 0.95f;
        public float KnockbackSpeed = 10f;

        // Volley capability (ARMORY §5, Pass E): multi-angle hit checks for
        // weapons like Hydra Flail. Null = the default single arc around
        // facing (every weapon before it). See MeleeWeaponPart.ProngAngles.
        public float[] ProngAngles;
    }

    // Melee with a gap-closer (GAME_DESIGN.md §3.1): high commitment,
    // punishable recovery on whiff. idle -> lunge -> swing -> recovery.
    // Within CloseRange the lunge is skipped. Pressing melee again during a
    // CONNECTED swing's recovery chains into swing 2, then a heavier
    // finisher; whiffs always end the string in full punishable recovery.
    // Pure state machine: geometry (distances) comes in, phase events come
    // out; the presentation layer applies movement, facing, and damage.
    public sealed class MeleeAction
    {
        private static readonly float[] ComboDamage = { 1f, 0.85f, 1.4f };
        private static readonly float[] ComboEndurance = { 1f, 0.8f, 1.5f };
        private static readonly float[] ComboKnockback = { 1f, 0.8f, 1.8f };

        private readonly MeleeTuning _tuning;
        private float _timer;
        private bool _didHit;
        private int _comboIndex;

        public MeleeAction(MeleeTuning tuning = null)
        {
            _tuning = tuning ?? new MeleeTuning();
        }

        public MeleeTuning Tuning => _tuning;
        public MeleePhase Phase { get; private set; } = MeleePhase.Idle;
        public bool Busy => Phase != MeleePhase.Idle;

        /// True while this melee could clash (attacking, not recovering).
        public bool Attacking => Phase is MeleePhase.Lunge or MeleePhase.Swing;

        public float ComboDamageMult => ComboDamage[_comboIndex];
        public float ComboEnduranceMult => ComboEndurance[_comboIndex];
        public float ComboKnockbackMult => ComboKnockback[_comboIndex];

        public bool TryStart(float distToTarget)
        {
            if (Phase != MeleePhase.Idle || distToTarget > _tuning.LungeRange)
            {
                return false;
            }

            _didHit = false;
            _comboIndex = 0;
            Begin(distToTarget);
            return true;
        }

        /// Chain into the next hit of the string: only during the recovery of
        /// a swing that CONNECTED, up to the 3-hit finisher. Reuses the
        /// gap-closer, since knockback routinely pushes the target just past
        /// melee range.
        public bool TryChain(float distToTarget)
        {
            if (Phase != MeleePhase.Recovery || !_didHit || _comboIndex >= ComboDamage.Length - 1)
            {
                return false;
            }

            _comboIndex += 1;
            _didHit = false;
            Begin(distToTarget);
            return true;
        }

        public MeleeTickEvent Tick(float dt, float distToTarget)
        {
            switch (Phase)
            {
                case MeleePhase.Lunge:
                    _timer -= dt;
                    if (distToTarget <= _tuning.LungeReachDistance)
                    {
                        EnterSwing();
                        return MeleeTickEvent.EnteredSwing;
                    }

                    if (_timer <= 0f)
                    {
                        // Lunge expired without reaching: whiff recovery.
                        Phase = MeleePhase.Recovery;
                        _timer = _tuning.WhiffRecovery;
                        return MeleeTickEvent.EnteredRecovery;
                    }

                    return MeleeTickEvent.LungeActive;

                case MeleePhase.Swing:
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        Phase = MeleePhase.Recovery;
                        _timer = _didHit ? _tuning.HitRecovery : _tuning.WhiffRecovery;
                        return MeleeTickEvent.EnteredRecovery;
                    }

                    return MeleeTickEvent.SwingActive;

                case MeleePhase.Recovery:
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        Phase = MeleePhase.Idle;
                        return MeleeTickEvent.Ended;
                    }

                    return MeleeTickEvent.RecoveryActive;

                default:
                    return MeleeTickEvent.None;
            }
        }

        /// The presentation layer calls this when the swing's range/arc check
        /// passes. Returns true only once per swing.
        public bool TryRegisterHit()
        {
            if (Phase != MeleePhase.Swing || _didHit)
            {
                return false;
            }

            _didHit = true;
            return true;
        }

        /// Knocked down mid-melee (or a clash): cancel everything.
        public void Cancel()
        {
            Phase = MeleePhase.Idle;
        }

        private void Begin(float distToTarget)
        {
            if (distToTarget <= _tuning.CloseRange)
            {
                EnterSwing();
            }
            else
            {
                Phase = MeleePhase.Lunge;
                _timer = _tuning.LungeMaxDuration;
            }
        }

        private void EnterSwing()
        {
            Phase = MeleePhase.Swing;
            _timer = _tuning.SwingActiveTime;
        }
    }
}
