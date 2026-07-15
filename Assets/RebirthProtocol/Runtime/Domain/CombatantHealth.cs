using System;

namespace RebirthProtocol.Domain
{
    public enum HealthState
    {
        Active,
        KnockedDown,
        Rebirth,
        Dead
    }

    public enum HitResult
    {
        Hit,
        Knockdown,
        Killed,
        Invulnerable
    }

    // Numbers carried over from the playtest-validated Three.js prototype
    // (src/core/tuning.ts `health` block) as starting values, not gospel.
    public sealed class HealthTuning
    {
        public float MaxHp = 1000f;
        public float MaxEndurance = 200f;
        public float EnduranceRegenPerSec = 35f;
        public float EnduranceRegenDelay = 1.8f;
        public float KnockdownDuration = 2.2f;
        public float KnockdownMashReduction = 0.12f;
        public float KnockdownMinDuration = 0.9f;
        public float RebirthDuration = 2.5f;
    }

    // Twin-bar system per GAME_DESIGN.md §2.2: a hit drains both the HP pool
    // and the endurance bar. Endurance emptying (not HP) triggers knockdown;
    // HP emptying is true death. Standing up from knockdown grants rebirth
    // invincibility every time — it is not a limited resource.
    // Stage 1 judgment call inherited from the prototype: robos are fully
    // invulnerable while downed/rebirthing, not just damage-reduced.
    public sealed class CombatantHealth
    {
        private readonly HealthTuning _tuning;
        private float _timeSinceHit = float.PositiveInfinity;
        private float _downElapsed;

        public CombatantHealth(HealthTuning tuning = null)
        {
            _tuning = tuning ?? new HealthTuning();
            if (_tuning.MaxHp <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(tuning), "MaxHp must be positive.");
            }

            if (_tuning.MaxEndurance <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(tuning), "MaxEndurance must be positive.");
            }

            Hp = _tuning.MaxHp;
            Endurance = _tuning.MaxEndurance;
            State = HealthState.Active;
        }

        public float MaxHp => _tuning.MaxHp;
        public float MaxEndurance => _tuning.MaxEndurance;
        public float Hp { get; private set; }
        public float Endurance { get; private set; }
        public HealthState State { get; private set; }

        /// Remaining time in knockdown or rebirth, whichever is active.
        public float StateTimer { get; private set; }

        public bool CanAct => State == HealthState.Active;

        public HitResult TakeHit(float damage, float enduranceDamage)
        {
            if (State != HealthState.Active)
            {
                return HitResult.Invulnerable;
            }

            Hp -= MathF.Max(0f, damage);
            _timeSinceHit = 0f;
            if (Hp <= 0f)
            {
                Hp = 0f;
                State = HealthState.Dead;
                return HitResult.Killed;
            }

            Endurance -= MathF.Max(0f, enduranceDamage);
            if (Endurance <= 0f)
            {
                ForceKnockdown();
                return HitResult.Knockdown;
            }

            return HitResult.Hit;
        }

        /// Direct endurance drain outside the normal hit pipeline (shield-parry
        /// punish) — still triggers knockdown if it empties the bar.
        public void DrainEndurance(float amount)
        {
            if (State != HealthState.Active)
            {
                return;
            }

            Endurance -= MathF.Max(0f, amount);
            if (Endurance <= 0f)
            {
                ForceKnockdown();
            }
        }

        /// Immediate knockdown regardless of endurance (shield guard-break).
        public void ForceKnockdown()
        {
            if (State != HealthState.Active)
            {
                return;
            }

            Endurance = 0f;
            State = HealthState.KnockedDown;
            StateTimer = _tuning.KnockdownDuration;
            _downElapsed = 0f;
        }

        /// Mash press while downed: shaves recovery time, floored so total
        /// downtime (time already spent down + remaining) never drops below
        /// KnockdownMinDuration no matter how fast the presses arrive.
        public void Mash()
        {
            if (State != HealthState.KnockedDown)
            {
                return;
            }

            StateTimer = MathF.Max(
                _tuning.KnockdownMinDuration - _downElapsed,
                StateTimer - _tuning.KnockdownMashReduction);
        }

        public void Tick(float dt)
        {
            if (State == HealthState.Dead)
            {
                return;
            }

            _timeSinceHit += dt;

            if (State == HealthState.KnockedDown)
            {
                StateTimer -= dt;
                _downElapsed += dt;
                if (StateTimer <= 0f)
                {
                    // Stand up: full endurance + rebirth invincibility window.
                    State = HealthState.Rebirth;
                    StateTimer = _tuning.RebirthDuration;
                    Endurance = _tuning.MaxEndurance;
                }
            }
            else if (State == HealthState.Rebirth)
            {
                StateTimer -= dt;
                if (StateTimer <= 0f)
                {
                    State = HealthState.Active;
                }
            }
            else if (_timeSinceHit > _tuning.EnduranceRegenDelay && Endurance < _tuning.MaxEndurance)
            {
                Endurance = MathF.Min(
                    _tuning.MaxEndurance,
                    Endurance + _tuning.EnduranceRegenPerSec * dt);
            }
        }
    }
}
