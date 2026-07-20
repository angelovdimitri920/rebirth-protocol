using System;

namespace RebirthProtocol.Domain
{
    // Five-slot loadout (GAME_DESIGN §2.1, extended): Body + Right Arm +
    // Left Arm + Legs + Pod. Right Arm is a mutually exclusive choice
    // between a gun and a melee weapon; Left Arm between a bomb and a
    // shield. ComputeStats flattens a Loadout into the derived numbers the
    // avatar runs on. All numbers ported from the prototype's parts.ts.

    public enum DashType
    {
        Normal,
        Long,
        Vanish
    }

    public sealed class DashProfile
    {
        public DashProfile(float speed, float duration, float cost)
        {
            Speed = speed;
            Duration = duration;
            Cost = cost;
        }

        public float Speed { get; }
        public float Duration { get; }
        public float Cost { get; }

        public static DashProfile For(DashType type) => type switch
        {
            DashType.Long => new DashProfile(21f, 0.55f, 40f),
            DashType.Vanish => new DashProfile(27f, 0.15f, 22f),
            _ => new DashProfile(24f, 0.22f, 28f)
        };
    }

    public sealed class BodyPart
    {
        public string Id;
        public string Name;
        public string Blurb;
        public float HpMult;
        public float DefMult; // incoming damage multiplier (lower = tankier)
        public float AtkMult; // outgoing damage multiplier
        public DashType DashType;
        public int DashCount; // air dashes per airborne stretch
        public float SpeedMult; // body weight affects ground speed
    }

    public sealed class GunPart
    {
        public string Id;
        public string Name;
        public string Blurb;
        public float Damage;
        public float EnduranceDamage;
        public float FireInterval;
        public float ProjectileSpeed;
        public float HomingTurnRate;

        // Scrapwright exemption (DOCTRINE §4.3): rounds from a gun with this
        // set ride out their wielder's knockdown instead of being wiped by
        // the overload rule. Reserved for the scrapwright line — no built
        // gun sets it; Matchlock will when the Pass P wave lands.
        public bool SurvivesKnockdown;
    }

    public sealed class MeleeWeaponPart
    {
        public string Id;
        public string Name;
        public string Blurb;
        public float Damage;
        public float EnduranceDamage;
        public float HitRange;
        public float HitArcDegrees;
        public float SwingActiveTime;
        public float HitRecovery; // recovery after a CONNECTING swing
        public float WhiffRecovery; // recovery after a MISS -- the real cooldown
        public float KnockbackSpeed;

        public MeleeTuning ToTuning() => new MeleeTuning
        {
            Damage = Damage,
            EnduranceDamage = EnduranceDamage,
            HitRange = HitRange,
            HitArcDegrees = HitArcDegrees,
            SwingActiveTime = SwingActiveTime,
            HitRecovery = HitRecovery,
            WhiffRecovery = WhiffRecovery,
            KnockbackSpeed = KnockbackSpeed
        };
    }

    public enum ReticuleAnchor
    {
        Target,
        Self
    }

    public sealed class BombPart
    {
        public string Id;
        public string Name;
        public string Blurb;
        public float Damage;
        public float EnduranceDamage;
        public float Cooldown;
        public float BlastRadius;
        public float ArcHeight; // lob apex above the midpoint
        public ReticuleAnchor ReticuleAnchor;
        public float ReticuleRange; // clamp for Target, fixed distance for Self
    }

    // Raise behaviors (ARMORY_REFERENCE §2.3, §7): what raising the shield
    // does to your movement. Ground and air are independent axes — every
    // shield has a ground behavior; most have no special air behavior
    // (raising midair just halts horizontal drift and you fall normally).
    public enum ShieldGroundRaise
    {
        Root, // halt in place while raised (the default)
        March // walk at MarchSpeedMult while raised (Targe only)
    }

    public enum ShieldAirRaise
    {
        None, // halt horizontal drift, fall normally
        Hold, // halt AND hover: gravity suspended while raised
        Drop  // slam straight to the ground
    }

    public sealed class ShieldPart
    {
        public string Id;
        public string Name;
        public string Blurb;
        public float ShieldHp;
        public float RegenPerSec;
        public float RegenDelay; // seconds unhit before regen resumes
        public float FrontBlockPercent;
        public float BackBlockPercent;
        public float MeleeParryEnduranceDamage;
        public float TollSeconds; // cooldown started on lower/break (§7 TOLL)
        public ShieldGroundRaise GroundRaise;
        public ShieldAirRaise AirRaise;
        public float MarchSpeedMult; // ground speed while raised, March only
        public float BlastMuffleSeconds; // Quiet Bell: all-sides blast guard window after raising
    }

    public sealed class PodPart
    {
        public string Id;
        public string Name;
        public string Blurb;
        public float Damage;
        public float EnduranceDamage;
        public float FireInterval;
        public float EnergyMax; // pods run on their own pool (§2.1)
        public float EnergyPerShot;
        public float EnergyRegenPerSec;
    }

    public sealed class LegsPart
    {
        public string Id;
        public string Name;
        public string Blurb;
        public float SpeedMult;
        public float JumpMult;
        public int ExtraDashes; // added to body DashCount
        public float LandRecoveryMult;
    }

    public sealed class Loadout
    {
        public BodyPart Body;
        public GunPart Gun; // exactly one of Gun/Melee is set (right arm)
        public MeleeWeaponPart Melee;
        public BombPart Bomb; // exactly one of Bomb/Shield is set (left arm)
        public ShieldPart Shield;
        public LegsPart Legs;
        public PodPart Pod;

        public bool HasGun => Gun != null;
        public bool HasMelee => Melee != null;
        public bool HasBomb => Bomb != null;
        public bool HasShield => Shield != null;
    }

    /// Flattened stats the avatar actually runs on.
    public sealed class RoboStats
    {
        public float MaxHp;
        public float DefMult;
        public float AtkMult;
        public float RunSpeed;
        public float JumpThrust;
        public DashType DashType;
        public int DashCount;
        public float LandRecoveryMult;
    }

    public static class PartsCatalog
    {
        /// powerMult: the run's flat per-fight enemy escalation — scales HP
        /// and outgoing damage together (RunState.EnemyPowerMult).
        public static RoboStats ComputeStats(Loadout l, float powerMult = 1f) => new RoboStats
        {
            MaxHp = MathF.Round(1000f * l.Body.HpMult * powerMult),
            DefMult = l.Body.DefMult,
            AtkMult = l.Body.AtkMult * powerMult,
            RunSpeed = CombatTuning.Move.RunSpeed * l.Body.SpeedMult * l.Legs.SpeedMult,
            JumpThrust = CombatTuning.Move.JumpThrust * l.Legs.JumpMult,
            DashType = l.Body.DashType,
            DashCount = l.Body.DashCount + l.Legs.ExtraDashes,
            LandRecoveryMult = l.Legs.LandRecoveryMult
        };

        // Display names follow docs/ARMORY_REFERENCE.md (neo-feudal canon,
        // 2026-07-18); .id fields stay frozen for save compatibility.
        // Standing rule (ARMORY_REFERENCE §2.2): no body grants more than
        // 2 air dashes -- extra dashes come only from legs.
        public static readonly BodyPart[] Bodies =
        {
            new BodyPart { Id = "vanguard", Name = "Bannerman", Blurb = "The Aureate Legion's standard-bearer pattern. Two air-dashes, no weaknesses, no edges.", HpMult = 1.0f, DefMult = 1.0f, AtkMult = 1.0f, DashType = DashType.Normal, DashCount = 2, SpeedMult = 1.0f },
            new BodyPart { Id = "skylance", Name = "Vesper", Blurb = "The Winter Wing's evening star: one long dash, hits hard, folds fast.", HpMult = 0.8f, DefMult = 1.2f, AtkMult = 1.25f, DashType = DashType.Long, DashCount = 1, SpeedMult = 1.05f },
            new BodyPart { Id = "wraith", Name = "Duskmantle", Blurb = "The Umbral Concordat's cowled evader. Two short vanish-dashes that phase through shots.", HpMult = 0.9f, DefMult = 1.1f, AtkMult = 0.9f, DashType = DashType.Vanish, DashCount = 2, SpeedMult = 1.0f },
            new BodyPart { Id = "bulwark", Name = "Cobalt Knight", Blurb = "The Rust Cross's ancestral wall. One dash, huge health pool, shrugs off hits.", HpMult = 1.45f, DefMult = 0.75f, AtkMult = 1.0f, DashType = DashType.Normal, DashCount = 1, SpeedMult = 0.8f }
        };

        public static readonly GunPart[] Guns =
        {
            new GunPart { Id = "blaster", Name = "Arbalest", Blurb = "The armory's workhorse. Honest damage, honest tracking.", Damage = 35f, EnduranceDamage = 16f, FireInterval = 0.38f, ProjectileSpeed = 32f, HomingTurnRate = 2.2f },
            new GunPart { Id = "needler", Name = "Litany", Blurb = "A recited pressure of weak, hard-curving darts. Death by repetition.", Damage = 14f, EnduranceDamage = 7f, FireInterval = 0.13f, ProjectileSpeed = 36f, HomingTurnRate = 3.4f },
            new GunPart { Id = "ram-cannon", Name = "Bombard", Blurb = "Siege-shot: slow, straight, brutal. One hit shreds endurance.", Damage = 90f, EnduranceDamage = 48f, FireInterval = 1.15f, ProjectileSpeed = 26f, HomingTurnRate = 0.6f }
        };

        public static readonly MeleeWeaponPart[] MeleeWeapons =
        {
            new MeleeWeaponPart { Id = "saber", Name = "Oathblade", Blurb = "The knight's standard. Balanced in every line.", Damage = 130f, EnduranceDamage = 55f, HitRange = 3.0f, HitArcDegrees = 70f, SwingActiveTime = 0.18f, HitRecovery = 0.45f, WhiffRecovery = 0.95f, KnockbackSpeed = 10f },
            new MeleeWeaponPart { Id = "warhammer", Name = "Dolorous Maul", Blurb = "The dolorous stroke: massive damage and knockback, ruinous to whiff.", Damage = 210f, EnduranceDamage = 90f, HitRange = 3.4f, HitArcDegrees = 80f, SwingActiveTime = 0.3f, HitRecovery = 0.75f, WhiffRecovery = 1.4f, KnockbackSpeed = 16f },
            new MeleeWeaponPart { Id = "twin-fang", Name = "Misericorde", Blurb = "The mercy-dagger: fast, light, barely punishable. Finishes what poise-loss starts.", Damage = 85f, EnduranceDamage = 35f, HitRange = 2.6f, HitArcDegrees = 70f, SwingActiveTime = 0.12f, HitRecovery = 0.28f, WhiffRecovery = 0.6f, KnockbackSpeed = 7f }
        };

        public static readonly BombPart[] Bombs =
        {
            new BombPart { Id = "impact", Name = "Censer", Blurb = "The swung vessel of fire. Reticule tracks the enemy -- hold to aim, release to throw.", Damage = 80f, EnduranceDamage = 35f, Cooldown = 5f, BlastRadius = 3.2f, ArcHeight = 5f, ReticuleAnchor = ReticuleAnchor.Target, ReticuleRange = 20f },
            new BombPart { Id = "quake", Name = "Anathema Charge", Blurb = "The great condemnation: huge blast, heavy endurance crush, long rearm. Fixed just ahead of you -- close-range, high commitment.", Damage = 120f, EnduranceDamage = 70f, Cooldown = 9f, BlastRadius = 4.5f, ArcHeight = 6.5f, ReticuleAnchor = ReticuleAnchor.Self, ReticuleRange = 4f }
        };

        // Shield toll/raise data per ARMORY_REFERENCE §7 (GUARD front/back,
        // SOAK = ShieldHp, MEND = RegenPerSec, TOLL, RIPOSTE = parry drain).
        public static readonly ShieldPart[] Shields =
        {
            new ShieldPart { Id = "aegis", Name = "Ward Veil", Blurb = "Light energy veil: fast mend, blocks ~75% up front, ~25% behind. Raised midair it holds you hovering -- the flier's shield.", ShieldHp = 180f, RegenPerSec = 25f, RegenDelay = 2.0f, FrontBlockPercent = 0.75f, BackBlockPercent = 0.25f, MeleeParryEnduranceDamage = 20f, TollSeconds = 2.5f, GroundRaise = ShieldGroundRaise.Root, AirRaise = ShieldAirRaise.Hold },
            new ShieldPart { Id = "bastion", Name = "Pavise", Blurb = "The great standing wall-shield: bigger buffer, blocks ~92% up front, mends slowly, long toll. Raised midair it slams you to the ground.", ShieldHp = 260f, RegenPerSec = 6f, RegenDelay = 3.5f, FrontBlockPercent = 0.92f, BackBlockPercent = 0.4f, MeleeParryEnduranceDamage = 32f, TollSeconds = 6f, GroundRaise = ShieldGroundRaise.Root, AirRaise = ShieldAirRaise.Drop },
            new ShieldPart { Id = "targe", Name = "Targe", Blurb = "The only shield you can advance behind: march at 40% speed while raised. Thin plate, quick mend, the shortest toll.", ShieldHp = 110f, RegenPerSec = 30f, RegenDelay = 1.5f, FrontBlockPercent = 0.60f, BackBlockPercent = 0.15f, MeleeParryEnduranceDamage = 16f, TollSeconds = 1.5f, GroundRaise = ShieldGroundRaise.March, AirRaise = ShieldAirRaise.None, MarchSpeedMult = 0.4f },
            new ShieldPart { Id = "kite-ward", Name = "Kite Ward", Blurb = "The knight's standard; balanced in every line. Hold to raise -- rooted while up.", ShieldHp = 200f, RegenPerSec = 14f, RegenDelay = 2.5f, FrontBlockPercent = 0.80f, BackBlockPercent = 0.30f, MeleeParryEnduranceDamage = 24f, TollSeconds = 3.5f, GroundRaise = ShieldGroundRaise.Root, AirRaise = ShieldAirRaise.None },
            new ShieldPart { Id = "quiet-bell", Name = "Quiet Bell", Blurb = "A dome of hush: for a breath after raising, blasts and through-wall harm are met with the full guard from every side.", ShieldHp = 150f, RegenPerSec = 16f, RegenDelay = 2.5f, FrontBlockPercent = 0.65f, BackBlockPercent = 0.35f, MeleeParryEnduranceDamage = 18f, TollSeconds = 4f, GroundRaise = ShieldGroundRaise.Root, AirRaise = ShieldAirRaise.None, BlastMuffleSeconds = 1.5f }
        };

        public static readonly PodPart[] Pods =
        {
            new PodPart { Id = "sentry", Name = "Iron Squire", Blurb = "The loyal retainer: steady chip fire while you reposition.", Damage = 8f, EnduranceDamage = 5f, FireInterval = 0.8f, EnergyMax = 100f, EnergyPerShot = 12f, EnergyRegenPerSec = 9f },
            new PodPart { Id = "hornet", Name = "Kestrel", Blurb = "The cast hawk: fast stooping bursts, then an empty glove.", Damage = 6f, EnduranceDamage = 8f, FireInterval = 0.35f, EnergyMax = 80f, EnergyPerShot = 16f, EnergyRegenPerSec = 7f }
        };

        public static readonly LegsPart[] Legs =
        {
            new LegsPart { Id = "strider", Name = "Wayfarer Greaves", Blurb = "Neutral gait. Nothing gained, nothing owed.", SpeedMult = 1.0f, JumpMult = 1.0f, ExtraDashes = 0, LandRecoveryMult = 1.0f },
            new LegsPart { Id = "cheetah", Name = "Courser Greaves", Blurb = "The running horse: fast and low. Ground speed up, jump suffers.", SpeedMult = 1.3f, JumpMult = 0.85f, ExtraDashes = 0, LandRecoveryMult = 1.1f },
            new LegsPart { Id = "cricket", Name = "Gryphon Greaves", Blurb = "The sky rig: extra dash and clean landings, sluggish on foot.", SpeedMult = 0.85f, JumpMult = 1.15f, ExtraDashes = 1, LandRecoveryMult = 0.7f }
        };

        public static Loadout DefaultLoadout() => new Loadout
        {
            Body = Bodies[0],
            Gun = Guns[0],
            Bomb = Bombs[0], // prototype default: Arbalest + Censer
            Legs = Legs[0],
            Pod = Pods[0]
        };
    }
}
