namespace RebirthProtocol.Domain
{
    public enum ChargePhase
    {
        Idle,
        Windup,
        Strike,
        Recovery
    }

    public enum ChargeTickEvent
    {
        None,
        WindupActive,
        EnteredStrike,
        StrikeActive,
        EnteredRecovery,
        RecoveryActive,
        Ended
    }

    // Garniture charge attack (COMBAT_DOCTRINE §4.5): a committed body-
    // strike. idle -> windup (rooted, vulnerable) -> strike (moving,
    // i-frames unless the spec says "no guard") -> recovery (rooted,
    // vulnerable). Repeating specs (Strikes > 1) chain straight into the
    // next strike; every strike lands at most one hit. Pure state machine
    // like MeleeAction: the avatar applies movement, facing, and damage.
    public sealed class ChargeAction
    {
        private readonly ChargeSpec _spec;
        private float _timer;
        private bool _didHit;

        public ChargeAction(ChargeSpec spec)
        {
            _spec = spec;
        }

        public ChargeSpec Spec => _spec;
        public ChargePhase Phase { get; private set; } = ChargePhase.Idle;
        public bool Busy => Phase != ChargePhase.Idle;

        /// 0-based index of the current strike (repeating charges).
        public int StrikeIndex { get; private set; }

        /// I-frames during the strike window only — and never for a
        /// "no guard" charge (Duskmantle's repeating short charges).
        public bool IFramesActive => Phase == ChargePhase.Strike && _spec.GrantsIFrames;

        public bool TryStart()
        {
            if (Phase != ChargePhase.Idle)
            {
                return false;
            }

            Phase = ChargePhase.Windup;
            _timer = _spec.WindupTime;
            StrikeIndex = 0;
            _didHit = false;
            return true;
        }

        public ChargeTickEvent Tick(float dt)
        {
            switch (Phase)
            {
                case ChargePhase.Windup:
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        EnterStrike(0);
                        return ChargeTickEvent.EnteredStrike;
                    }

                    return ChargeTickEvent.WindupActive;

                case ChargePhase.Strike:
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        if (StrikeIndex + 1 < _spec.Strikes)
                        {
                            EnterStrike(StrikeIndex + 1);
                            return ChargeTickEvent.EnteredStrike;
                        }

                        Phase = ChargePhase.Recovery;
                        _timer = _spec.RecoveryTime;
                        return ChargeTickEvent.EnteredRecovery;
                    }

                    return ChargeTickEvent.StrikeActive;

                case ChargePhase.Recovery:
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        Phase = ChargePhase.Idle;
                        return ChargeTickEvent.Ended;
                    }

                    return ChargeTickEvent.RecoveryActive;

                default:
                    return ChargeTickEvent.None;
            }
        }

        /// The presentation layer calls this when the strike's contact
        /// check passes. Returns true only once per strike.
        public bool TryRegisterHit()
        {
            if (Phase != ChargePhase.Strike || _didHit)
            {
                return false;
            }

            _didHit = true;
            return true;
        }

        /// Knocked down mid-charge: cancel everything.
        public void Cancel()
        {
            Phase = ChargePhase.Idle;
        }

        private void EnterStrike(int index)
        {
            Phase = ChargePhase.Strike;
            _timer = _spec.StrikeTime;
            StrikeIndex = index;
            _didHit = false;
        }
    }
}
