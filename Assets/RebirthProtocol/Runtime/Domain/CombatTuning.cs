namespace RebirthProtocol.Domain
{
    // Every gameplay-feel number lives here, mirroring the Three.js
    // prototype's src/core/tuning.ts. These are playtest-validated starting
    // values, not gospel. Stage 1 slice hardcodes the baseline loadout
    // (vanguard body / striders legs / Arbalest gun / Oathblade melee); the
    // five-slot loadout system replaces these constants in a later stage.
    public static class CombatTuning
    {
        public static class Arena
        {
            public const float Size = 32f;
            public const float WallHeight = 34f;
            public const float VisibleWallHeight = 1.5f;
        }

        public static class Move
        {
            public const float RunSpeed = 9f;
            public const float AirControlSpeed = 6f;
            public const float AirSteerRate = 4f;
            public const float Gravity = -28f;
            public const float TurnRate = 14f;
            public const float FireSlideCorrection = 3.2f;
            public const float FireAirHaltRate = 9f;
            public const float JumpThrust = 13f;
            public const float KnockbackDecayRate = 6f;
            public const float NoControlDamping = 0.85f;
        }

        public static class Boost
        {
            public const float Max = 100f;
            public const float ThrustDrainPerSec = 45f;
            public const float LandRecoveryBase = 0.1f;
            public const float LandRecoveryScale = 0.55f;
            public const float OverheatExtraRecovery = 0.5f;
        }

        // Shared dash mechanics; per-archetype speed/duration/cost live in
        // DashProfile.For(DashType).
        public static class Dash
        {
            public const float HomingTurnRate = 3.5f;
            public const float GroundDashHop = 3f;
        }

        // Shared gun mechanics; per-part damage/cadence/speed/homing live on
        // each GunPart in PartsCatalog.
        public static class Gun
        {
            public const float ProjectileLifetime = 2.0f;
            public const float MuzzleHeight = 1.2f;
        }

        // Shared bomb/pod mechanics (per-part numbers in PartsCatalog).
        public static class Bomb
        {
            public const float LobSpeed = 18f; // flightTime = dist / LobSpeed
            public const float MinFlightTime = 0.5f;
        }

        // Shared shield mechanics (per-part guard/toll numbers in PartsCatalog).
        public static class Shield
        {
            public const float AirDropSpeed = -30f; // Air-drop raise: slam velocity toward the ground
        }

        public static class Pod
        {
            public const float HoverHeight = 2.4f;
            public const float FireRange = 22f;
            public const float ProjectileSpeed = 26f;
            public const float HomingTurnRate = 1.6f;
        }

        public static class Ai
        {
            public const float OrbitRadiusMin = 8f;
            public const float OrbitRadiusMax = 14f;
            public const float FireInterval = 0.9f;
            public const float BurstDuration = 1.2f;
            public const float JumpChancePerSec = 0.35f;
            public const float DashChancePerSec = 0.4f;
            public const float DecisionInterval = 1.2f;
        }

        // Slow-and-deliberate doctrine (user playtest 2026-07-18): the rig
        // smooths its look-at point, caps yaw speed, and glides position and
        // zoom — the fight moves fast, the eye watching it must not.
        public static class Camera
        {
            public const float Height = 16f;
            public const float Back = 16f;
            public const float TargetBias = 0.4f;
            public const float FollowLerp = 3f;
            public const float LookAtLerp = 3.5f; // smoothing on the aim point itself
            public const float RotateLerp = 1.1f;
            public const float MaxYawSpeedDeg = 45f; // hard cap, degrees/sec
            public const float FrustumSize = 20f;
            public const float ZoomStartDistance = 10f; // zoom out a little earlier: slow response must never lose a fighter
            public const float ZoomRange = 16f;
            public const float ZoomMax = 0.7f;
            public const float ZoomLerp = 2f;
        }
    }
}
