using System;

namespace RebirthProtocol.Domain
{
    // Boost economy per GAME_DESIGN.md §3.3: jump/hover/air-dash share one
    // gauge that only refills on landing; landing recovery scales with how
    // much was spent; fully draining it adds an overheat penalty.
    public sealed class BoostGauge
    {
        public float Value { get; private set; } = CombatTuning.Boost.Max;
        public bool Overheated { get; private set; }
        public int AirDashesUsed { get; private set; }

        /// Seconds of post-landing control lockout remaining.
        public float LandingRecovery { get; private set; }

        public float Max => CombatTuning.Boost.Max;
        public bool CanBoost => !Overheated && Value > 0f && LandingRecovery <= 0f;

        public void Tick(float dt)
        {
            if (LandingRecovery > 0f)
            {
                LandingRecovery -= dt;
            }
        }

        public void SpendThrust(float dt)
        {
            Spend(CombatTuning.Boost.ThrustDrainPerSec * dt);
        }

        public bool TrySpendAirDash()
        {
            if (Value < CombatTuning.Dash.Cost || AirDashesUsed >= CombatTuning.Dash.MaxAirDashes)
            {
                return false;
            }

            AirDashesUsed += 1;
            Spend(CombatTuning.Dash.Cost);
            return true;
        }

        /// Landing: recovery scales with the fraction of gauge spent, plus an
        /// overheat surcharge; then the gauge refills and dashes reset.
        public void NotifyLanded(float recoveryMult = 1f)
        {
            var spentFraction = 1f - Value / Max;
            LandingRecovery =
                (CombatTuning.Boost.LandRecoveryBase + CombatTuning.Boost.LandRecoveryScale * spentFraction) * recoveryMult
                + (Overheated ? CombatTuning.Boost.OverheatExtraRecovery : 0f);
            Value = Max;
            Overheated = false;
            AirDashesUsed = 0;
        }

        private void Spend(float amount)
        {
            Value = MathF.Max(0f, Value - amount);
            if (Value <= 0f)
            {
                Overheated = true;
            }
        }
    }
}
