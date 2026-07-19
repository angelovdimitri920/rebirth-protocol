using System;

namespace RebirthProtocol.Domain
{
    // Shield toll + raise state machine (ARMORY_REFERENCE §2.3 directive,
    // §7 table). Plain-C# domain core: RoboAvatar feeds it the held intent
    // each motor tick and reads Raised back; all block/drain math for a
    // raised shield routes through here. The toll is the bomb rearm's twin:
    // it starts the moment the shield is LOWERED or BROKEN (never while it
    // stays up), and the shield cannot be raised again until it runs out.
    public sealed class ShieldRig
    {
        private readonly ShieldPart _part;
        private float _hitTimer = float.PositiveInfinity;

        public ShieldRig(ShieldPart part)
        {
            _part = part ?? throw new ArgumentNullException(nameof(part));
            Hp = part.ShieldHp;
        }

        public ShieldPart Part => _part;
        public float Hp { get; private set; }
        public bool Raised { get; private set; }
        public float TollRemaining { get; private set; }

        /// Quiet Bell special: seconds left in the muffle window that opened
        /// when the shield was raised. Zero for every other shield.
        public float MuffleRemaining { get; private set; }

        public bool Ready => TollRemaining <= 0f && Hp > 0f;

        /// One motor tick: toll counts down, mend runs, and the raised state
        /// follows the held intent. Holding through a toll re-raises the
        /// moment it expires (release is the decision; the hold is a plain
        /// "up whenever allowed"). canAct false (knockdown/death/rebirth-less
        /// states) force-lowers — and that lowering starts the toll too.
        public void Tick(float dt, bool wantRaised, bool canAct)
        {
            TollRemaining = MathF.Max(0f, TollRemaining - dt);
            _hitTimer += dt;

            if (Raised)
            {
                MuffleRemaining = MathF.Max(0f, MuffleRemaining - dt);
                if (!wantRaised || !canAct)
                {
                    Lower();
                }
            }
            else if (wantRaised && canAct && Ready)
            {
                Raised = true;
                MuffleRemaining = _part.BlastMuffleSeconds;
            }

            // Mend pauses while recently struck and never runs mid-knockdown
            // (same rule the pre-rig avatar code enforced).
            if (canAct && _hitTimer > _part.RegenDelay && Hp < _part.ShieldHp)
            {
                Hp = MathF.Min(_part.ShieldHp, Hp + _part.RegenPerSec * dt);
            }
        }

        /// Block percent for an incoming hit against the RAISED shield.
        /// Quiet Bell's dome: while the muffle window is open, blast damage
        /// is met with the full front guard from every direction.
        public float BlockPercent(bool isFront, bool isBlast)
        {
            if (isBlast && MuffleRemaining > 0f)
            {
                return _part.FrontBlockPercent;
            }

            return isFront ? _part.FrontBlockPercent : _part.BackBlockPercent;
        }

        /// A hit landed on the raised shield: resets the mend delay whether
        /// or not the pool ends up drained (chip-only outcomes still count
        /// as being struck).
        public void NotifyBlockedHit()
        {
            _hitTimer = 0f;
        }

        /// Drains the shield pool by the blocked portion. Returns true on a
        /// guard break: pool emptied, shield forced down, toll started.
        public bool Drain(float amount)
        {
            Hp -= MathF.Max(0f, amount);
            if (Hp > 0f)
            {
                return false;
            }

            Hp = 0f;
            if (Raised)
            {
                Lower();
            }

            return true;
        }

        private void Lower()
        {
            Raised = false;
            MuffleRemaining = 0f;
            TollRemaining = _part.TollSeconds;
        }
    }
}
