using System;

namespace RebirthProtocol.Domain
{
    public enum FetterPhase
    {
        Free,
        Fettered,
        Immune
    }

    public sealed class FetterTuning
    {
        // COMBAT_DOCTRINE §13 pillar 9's degenerate-watchlist fix: a fetter
        // that just ended grants a window where no new fetter can take hold.
        public float ImmunitySeconds = 2f;
    }

    // Fetter status (ARMORY_REFERENCE; DOCTRINE §13 pillar 9): a full
    // immobilize distinct from KnockedDown -- it doesn't wipe in-flight gun
    // rounds (the overload rule only listens to CombatantHealth.KnockedDown)
    // and doesn't touch the knockdown/rebirth cycle at all, it just holds.
    // free -> fettered (duration set by whatever applied it) -> immune (the
    // pillar-9 window) -> free. TryApply only takes effect from Free: a hit
    // landing mid-fetter or during the immunity window is ignored outright
    // rather than refreshing/extending the hold -- that's what actually
    // bounds "Fetter chains" (the named degenerate pattern), since every
    // application is capped at its own duration and is always followed by a
    // guaranteed immune window before the next one can land. Pure state
    // machine like CombatantHealth: RoboAvatar ticks it and reads
    // IsFettered; presentation hangs off the Fettered event.
    public sealed class FetterState
    {
        private readonly FetterTuning _tuning;
        private float _timer;

        public FetterState(FetterTuning tuning = null)
        {
            _tuning = tuning ?? new FetterTuning();
        }

        public FetterPhase Phase { get; private set; } = FetterPhase.Free;
        public bool IsFettered => Phase == FetterPhase.Fettered;
        public bool IsImmune => Phase == FetterPhase.Immune;

        /// Fired the instant a fetter takes hold (Free -> Fettered). The
        /// avatar hangs its "cancel whatever I was doing" reaction here.
        public event Action Fettered;

        public bool TryApply(float durationSeconds)
        {
            if (Phase != FetterPhase.Free || durationSeconds <= 0f)
            {
                return false;
            }

            Phase = FetterPhase.Fettered;
            _timer = durationSeconds;
            Fettered?.Invoke();
            return true;
        }

        public void Tick(float dt)
        {
            if (Phase == FetterPhase.Free)
            {
                return;
            }

            _timer -= dt;
            if (_timer > 0f)
            {
                return;
            }

            if (Phase == FetterPhase.Fettered)
            {
                Phase = FetterPhase.Immune;
                _timer = _tuning.ImmunitySeconds;
            }
            else
            {
                Phase = FetterPhase.Free;
            }
        }

        /// Knockdown (or death) supersedes a fetter outright -- no immunity
        /// window tacked on top; the knockdown/rebirth cycle already grants
        /// its own invincibility, and stacking Fetter's immunity on top of
        /// that would be an unpriced extra grace period.
        public void Cancel()
        {
            Phase = FetterPhase.Free;
            _timer = 0f;
        }
    }
}
