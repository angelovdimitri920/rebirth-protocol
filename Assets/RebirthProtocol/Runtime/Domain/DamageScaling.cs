using System;

namespace RebirthProtocol.Domain
{
    // Distance/speed/lunge damage scaling (ARMORY_REFERENCE §13.1, Pass H).
    // Two pure-C# value types the presentation layer feeds a live scalar
    // (distance flown, wielder speed, lunge length) and reads a damage
    // multiplier back. Kept in the domain, so the curves are EditMode-
    // testable without a scene. Every existing part uses the neutral
    // variant (Flat / None), which returns 1.0 — zero behavior change.

    public enum RangeProfileKind
    {
        Flat,       // same damage in every band (every gun before Pass H)
        Rangecraft, // grows with distance flown (Pilgrim)
        BurstPoint  // peaks at a detonation distance, weak early/late (Beacon)
    }

    /// A gun round's damage-vs-distance curve. The projectile's stored Damage
    /// is the REFERENCE value; `FactorAt(distanceTraveled)` scales it. The
    /// scale is applied at IMPACT off the distance the round has actually
    /// flown, so a Rangecraft round rewards the long field and a Burst-point
    /// round has to be spaced to its bloom.
    public struct RangeScaling
    {
        public RangeProfileKind Kind;

        // Rangecraft: factor lerps NearFactor -> FarFactor across
        // [0, FarDistance] metres, then holds at FarFactor.
        public float NearFactor;
        public float FarFactor;
        public float FarDistance;

        // Burst-point: PeakFactor within +/- BurstWindow of BurstDistance,
        // OffFactor everywhere else.
        public float PeakFactor;
        public float OffFactor;
        public float BurstDistance;
        public float BurstWindow;

        public static RangeScaling Flat => new RangeScaling { Kind = RangeProfileKind.Flat };

        public static RangeScaling Rangecraft(float nearFactor, float farFactor, float farDistance) =>
            new RangeScaling
            {
                Kind = RangeProfileKind.Rangecraft,
                NearFactor = nearFactor,
                FarFactor = farFactor,
                FarDistance = farDistance
            };

        public static RangeScaling BurstPoint(float peakFactor, float offFactor, float burstDistance, float burstWindow) =>
            new RangeScaling
            {
                Kind = RangeProfileKind.BurstPoint,
                PeakFactor = peakFactor,
                OffFactor = offFactor,
                BurstDistance = burstDistance,
                BurstWindow = burstWindow
            };

        public readonly float FactorAt(float distanceTraveled)
        {
            switch (Kind)
            {
                case RangeProfileKind.Rangecraft:
                {
                    var t = FarDistance <= 0f ? 1f : Math.Clamp(distanceTraveled / FarDistance, 0f, 1f);
                    return NearFactor + (FarFactor - NearFactor) * t;
                }
                case RangeProfileKind.BurstPoint:
                    return MathF.Abs(distanceTraveled - BurstDistance) <= BurstWindow ? PeakFactor : OffFactor;
                default:
                    return 1f;
            }
        }
    }

    public enum MeleeScaleMode
    {
        None,         // no scaling (every weapon before Pass H)
        Speed,        // scales with the wielder's speed at commit (Courser Saber)
        LungeDistance,// scales with how far the lunge charged (Tilt Lance)
        Tip           // scales with distance to the target: power in the tip (Crowbeak Pick)
    }

    /// A melee swing's damage-vs-<something> curve. The avatar supplies the
    /// mode-appropriate live input (speed / lunge length / hit distance) and
    /// multiplies the swing's base damage by `FactorAt(input)`. Linear ramp
    /// from MinFactor (at/below LowInput) to MaxFactor (at/above HighInput).
    public struct MeleeScaling
    {
        public MeleeScaleMode Mode;
        public float MinFactor;
        public float MaxFactor;
        public float LowInput;
        public float HighInput;

        public static MeleeScaling None => new MeleeScaling { Mode = MeleeScaleMode.None };

        public static MeleeScaling Ramp(MeleeScaleMode mode, float minFactor, float maxFactor, float lowInput, float highInput) =>
            new MeleeScaling
            {
                Mode = mode,
                MinFactor = minFactor,
                MaxFactor = maxFactor,
                LowInput = lowInput,
                HighInput = highInput
            };

        public readonly float FactorAt(float input)
        {
            if (Mode == MeleeScaleMode.None)
            {
                return 1f;
            }

            var span = HighInput - LowInput;
            var t = span <= 0f ? 1f : Math.Clamp((input - LowInput) / span, 0f, 1f);
            return MinFactor + (MaxFactor - MinFactor) * t;
        }
    }
}
