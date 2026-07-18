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

        public static readonly BodyPart[] Bodies =
        {
            new BodyPart { Id = "vanguard", Name = "Legionnaire", Blurb = "Balanced all-rounder. Two air-dashes, no weaknesses, no edges.", HpMult = 1.0f, DefMult = 1.0f, AtkMult = 1.0f, DashType = DashType.Normal, DashCount = 2, SpeedMult = 1.0f },
            new BodyPart { Id = "skylance", Name = "Valkyrie", Blurb = "Glass-cannon flier. One long dash, hits hard, folds fast.", HpMult = 0.8f, DefMult = 1.2f, AtkMult = 1.25f, DashType = DashType.Long, DashCount = 1, SpeedMult = 1.05f },
            new BodyPart { Id = "wraith", Name = "Shinobi", Blurb = "Evader. Three short vanish-dashes that phase through shots.", HpMult = 0.9f, DefMult = 1.1f, AtkMult = 0.9f, DashType = DashType.Vanish, DashCount = 3, SpeedMult = 1.0f },
            new BodyPart { Id = "bulwark", Name = "Crusader Knight", Blurb = "Slow tank. One dash, huge health pool, shrugs off hits.", HpMult = 1.45f, DefMult = 0.75f, AtkMult = 1.0f, DashType = DashType.Normal, DashCount = 1, SpeedMult = 0.8f }
        };

        public static readonly GunPart[] Guns =
        {
            new GunPart { Id = "blaster", Name = "Longbow", Blurb = "The baseline. Honest damage, honest tracking.", Damage = 35f, EnduranceDamage = 16f, FireInterval = 0.38f, ProjectileSpeed = 32f, HomingTurnRate = 2.2f },
            new GunPart { Id = "needler", Name = "Chu-Ko-Nu", Blurb = "Rapid stream of weak, hard-curving darts. Death by pressure.", Damage = 14f, EnduranceDamage = 7f, FireInterval = 0.13f, ProjectileSpeed = 36f, HomingTurnRate = 3.4f },
            new GunPart { Id = "ram-cannon", Name = "Ballista", Blurb = "Slow, straight, brutal. One hit shreds endurance.", Damage = 90f, EnduranceDamage = 48f, FireInterval = 1.15f, ProjectileSpeed = 26f, HomingTurnRate = 0.6f }
        };

        public static readonly MeleeWeaponPart[] MeleeWeapons =
        {
            new MeleeWeaponPart { Id = "saber", Name = "Saber", Blurb = "Balanced blade. No glaring weakness, no standout edge.", Damage = 130f, EnduranceDamage = 55f, HitRange = 3.0f, HitArcDegrees = 70f, SwingActiveTime = 0.18f, HitRecovery = 0.45f, WhiffRecovery = 0.95f, KnockbackSpeed = 10f },
            new MeleeWeaponPart { Id = "warhammer", Name = "Warhammer", Blurb = "Massive damage and knockback, but whiff this and you're standing there a long time.", Damage = 210f, EnduranceDamage = 90f, HitRange = 3.4f, HitArcDegrees = 80f, SwingActiveTime = 0.3f, HitRecovery = 0.75f, WhiffRecovery = 1.4f, KnockbackSpeed = 16f },
            new MeleeWeaponPart { Id = "twin-fang", Name = "Khopesh", Blurb = "Fast, light, low-commitment. Weaker per hit, but barely punishable.", Damage = 85f, EnduranceDamage = 35f, HitRange = 2.6f, HitArcDegrees = 70f, SwingActiveTime = 0.12f, HitRecovery = 0.28f, WhiffRecovery = 0.6f, KnockbackSpeed = 7f }
        };

        public static readonly BombPart[] Bombs =
        {
            new BombPart { Id = "impact", Name = "Greek Fire Pot", Blurb = "Standard lobbed shell. Reticule tracks the enemy -- hold to aim, release to throw.", Damage = 80f, EnduranceDamage = 35f, Cooldown = 5f, BlastRadius = 3.2f, ArcHeight = 5f, ReticuleAnchor = ReticuleAnchor.Target, ReticuleRange = 20f },
            new BombPart { Id = "quake", Name = "Zhen Tian Lei", Blurb = "Huge blast, heavy endurance crush, long rearm. Reticule fixed just ahead of you -- close-range, high commitment.", Damage = 120f, EnduranceDamage = 70f, Cooldown = 9f, BlastRadius = 4.5f, ArcHeight = 6.5f, ReticuleAnchor = ReticuleAnchor.Self, ReticuleRange = 4f }
        };

        public static readonly ShieldPart[] Shields =
        {
            new ShieldPart { Id = "aegis", Name = "Aegis Barrier", Blurb = "Energy shield: fast regen, blocks ~75% up front, ~25% behind. Hold to raise -- rooted while up.", ShieldHp = 180f, RegenPerSec = 25f, RegenDelay = 2.0f, FrontBlockPercent = 0.75f, BackBlockPercent = 0.25f, MeleeParryEnduranceDamage = 20f },
            new ShieldPart { Id = "bastion", Name = "Bastion Plate", Blurb = "Physical plate: bigger buffer, blocks ~92% up front, recharges slowly. Hold to raise -- rooted while up.", ShieldHp = 260f, RegenPerSec = 6f, RegenDelay = 3.5f, FrontBlockPercent = 0.92f, BackBlockPercent = 0.4f, MeleeParryEnduranceDamage = 32f }
        };

        public static readonly PodPart[] Pods =
        {
            new PodPart { Id = "sentry", Name = "Terracotta Sentinel", Blurb = "Steady chip fire. Keeps them honest while you reposition.", Damage = 8f, EnduranceDamage = 5f, FireInterval = 0.8f, EnergyMax = 100f, EnergyPerShot = 12f, EnergyRegenPerSec = 9f },
            new PodPart { Id = "hornet", Name = "War Kite", Blurb = "Fast bursts that drain its cell quickly. Feast then famine.", Damage = 6f, EnduranceDamage = 8f, FireInterval = 0.35f, EnergyMax = 80f, EnergyPerShot = 16f, EnergyRegenPerSec = 7f }
        };

        public static readonly LegsPart[] Legs =
        {
            new LegsPart { Id = "strider", Name = "Traveler's Boots", Blurb = "Neutral gait. Nothing gained, nothing owed.", SpeedMult = 1.0f, JumpMult = 1.0f, ExtraDashes = 0, LandRecoveryMult = 1.0f },
            new LegsPart { Id = "cheetah", Name = "Numidian Boots", Blurb = "Fast and low. Ground speed up, jump suffers.", SpeedMult = 1.3f, JumpMult = 0.85f, ExtraDashes = 0, LandRecoveryMult = 1.1f },
            new LegsPart { Id = "cricket", Name = "Winged Sandals", Blurb = "Sky rig: extra dash and clean landings, sluggish on foot.", SpeedMult = 0.85f, JumpMult = 1.15f, ExtraDashes = 1, LandRecoveryMult = 0.7f }
        };

        public static Loadout DefaultLoadout() => new Loadout
        {
            Body = Bodies[0],
            Gun = Guns[0],
            Bomb = Bombs[0], // prototype default: Longbow + Greek Fire Pot
            Legs = Legs[0],
            Pod = Pods[0]
        };
    }
}
