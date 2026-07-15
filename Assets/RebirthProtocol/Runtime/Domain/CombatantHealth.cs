using System;

namespace RebirthProtocol.Domain
{
    public enum CombatLifeState
    {
        Alive,
        Staggered,
        KnockedDown,
        Rebirthing,
        Destroyed
    }

    public readonly struct DamageEvent
    {
        public DamageEvent(float hitPoints, float stagger)
        {
            HitPoints = MathF.Max(0f, hitPoints);
            Stagger = MathF.Max(0f, stagger);
        }

        public float HitPoints { get; }
        public float Stagger { get; }
    }

    public sealed class CombatantHealth
    {
        public CombatantHealth(float maxHitPoints, float staggerLimit, int rebirthCharges)
        {
            if (maxHitPoints <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHitPoints));
            }

            if (staggerLimit <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(staggerLimit));
            }

            MaxHitPoints = maxHitPoints;
            StaggerLimit = staggerLimit;
            HitPoints = maxHitPoints;
            RebirthCharges = Math.Max(0, rebirthCharges);
            State = CombatLifeState.Alive;
        }

        public float MaxHitPoints { get; }
        public float StaggerLimit { get; }
        public float HitPoints { get; private set; }
        public float Stagger { get; private set; }
        public int RebirthCharges { get; private set; }
        public CombatLifeState State { get; private set; }

        public bool CanAct => State is CombatLifeState.Alive or CombatLifeState.Staggered;

        public void ApplyDamage(DamageEvent damage)
        {
            if (State is CombatLifeState.Destroyed or CombatLifeState.KnockedDown)
            {
                return;
            }

            HitPoints = MathF.Max(0f, HitPoints - damage.HitPoints);
            Stagger = MathF.Min(StaggerLimit, Stagger + damage.Stagger);

            if (HitPoints <= 0f)
            {
                State = CombatLifeState.KnockedDown;
                return;
            }

            State = Stagger >= StaggerLimit ? CombatLifeState.Staggered : CombatLifeState.Alive;
        }

        public void RecoverStagger(float amount)
        {
            if (amount <= 0f || State is CombatLifeState.KnockedDown or CombatLifeState.Destroyed)
            {
                return;
            }

            Stagger = MathF.Max(0f, Stagger - amount);
            if (State == CombatLifeState.Staggered && Stagger < StaggerLimit)
            {
                State = CombatLifeState.Alive;
            }
        }

        public bool TryStartRebirth()
        {
            if (State != CombatLifeState.KnockedDown || RebirthCharges <= 0)
            {
                State = State == CombatLifeState.KnockedDown ? CombatLifeState.Destroyed : State;
                return false;
            }

            RebirthCharges -= 1;
            State = CombatLifeState.Rebirthing;
            return true;
        }

        public void CompleteRebirth(float restoredFraction = 0.5f)
        {
            if (State != CombatLifeState.Rebirthing)
            {
                return;
            }

            var clampedFraction = Math.Clamp(restoredFraction, 0.05f, 1f);
            HitPoints = MaxHitPoints * clampedFraction;
            Stagger = 0f;
            State = CombatLifeState.Alive;
        }
    }
}
