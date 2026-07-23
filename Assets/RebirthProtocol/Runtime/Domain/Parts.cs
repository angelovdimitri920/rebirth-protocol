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

    // Charge kinds (COMBAT_DOCTRINE §4.5). Only the kinds the built bodies'
    // Field garnitures need exist yet — Movement (wall-clearing) and Evasion
    // charges arrive with the War/Chase garnitures (Pass M).
    public enum ChargeKind
    {
        Attack, // straight/diagonal ground strike
        Air     // rising strike that contests the sky
    }

    // One garniture's charge attack (ARMORY_REFERENCE §3 "Charge" column):
    // a committed body-strike — vulnerable windup, i-frames during the
    // strike (unless GrantsIFrames is off: the source's "no guard"),
    // vulnerable recovery. Ground-only. Plain data the ChargeAction state
    // machine and the avatar read.
    public sealed class ChargeSpec
    {
        public ChargeKind Kind;
        public float Damage;
        public float EnduranceDamage;
        public float Speed;
        public float StrikeTime; // active travel time per strike
        public int Strikes = 1; // >1 = repeating short charges (Duskmantle)
        public float WindupTime; // rooted, vulnerable, readable
        public float RecoveryTime; // rooted, vulnerable — the whiff price
        public float RiseSpeed; // vertical climb during the strike (Air kind)
        public bool GrantsIFrames = true; // false = the source's "no guard"
        public float KnockbackSpeed;
        public float HitRange; // body-strike contact radius
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
        public ChargeSpec Charge; // every garniture lists one (DOCTRINE §4.5)
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

        // Volley capability (ARMORY §4, Pass E): a spread gun fires
        // ProjectileCount independently-homing streams per trigger-pull,
        // fanned evenly across SpreadDegrees and centered on the aimed
        // heading. 1/0 (every gun before Trefoil) is exactly today's
        // single-shot behavior. DOCTRINE §13 pillar 3 "volley truth": Damage
        // here is PER STREAM, not a headline volley total assuming every
        // stream connects — the balance harness measures the real number.
        public int ProjectileCount = 1;
        public float SpreadDegrees;

        // Fetter capability (ARMORY_REFERENCE; DOCTRINE §13 pillar 9, Pass
        // F): a hit from this gun also applies the Fetter status for this
        // many seconds. 0 (every gun before Fetterlock) applies nothing --
        // RoboAvatar.ApplyFetter is a no-op below/at zero.
        public float FetterSeconds;

        // Pull capability (ARMORY_REFERENCE, Pass G): a landed hit hauls the
        // victim toward the shooter instead of away, at this speed --
        // ProjectileSystem.ApplyAvatarHit reuses RoboAvatar.ApplyKnockback's
        // existing decay math by aiming the impulse at the owner's CURRENT
        // position (a homing round curves in, so its heading at impact needn't
        // point back at the shooter). Suppressed when a raised shield
        // intercepts the shot -- a guard defeats the grab. 0 (every gun
        // before Grapnel/Auger) is today's ordinary small hit-flinch,
        // unchanged.
        public float PullSpeed;
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

        // Volley capability (ARMORY §5, Pass E) for multi-angle melee (Hydra
        // Flail): each entry is a hit-check center, in degrees relative to
        // facing, each HitArcDegrees wide; a hit lands if the target falls
        // within ANY prong. Null/empty (every weapon before Hydra Flail) is
        // exactly today's single wide-arc-around-facing behavior — still one
        // hit per swing (MeleeAction.TryRegisterHit's budget is unchanged),
        // just a more forgiving angle-of-attack, not extra hits.
        public float[] ProngAngles;

        // Fetter capability (ARMORY_REFERENCE; DOCTRINE §13 pillar 9, Pass
        // F): a connecting swing also applies Fetter for this many seconds,
        // flat regardless of combo stage. 0 (every weapon before Knell
        // Maul/Tocsin Mace) applies nothing.
        public float FetterSeconds;

        // Pull capability (ARMORY_REFERENCE, Pass G): a connecting swing
        // hauls the victim toward the wielder instead of away, at this
        // speed -- overrides KnockbackSpeed entirely when set (a weapon
        // either shoves or hauls, never both). 0 (every weapon before
        // Hookbill/Sawtooth Espadon) is today's ordinary push, unchanged.
        public float PullSpeed;

        // Guard-piercing capability (ARMORY_REFERENCE, Pass G): a connecting
        // swing against a RAISED shield reduces its effective block% by
        // this fraction before the damage/drain split (0.6 = "pierces 60%
        // of a raised shield's GUARD"). 0 (every weapon before Estoc) is
        // today's unpierced block, unchanged.
        public float GuardPierce;

        public MeleeTuning ToTuning() => new MeleeTuning
        {
            Damage = Damage,
            EnduranceDamage = EnduranceDamage,
            HitRange = HitRange,
            HitArcDegrees = HitArcDegrees,
            SwingActiveTime = SwingActiveTime,
            HitRecovery = HitRecovery,
            WhiffRecovery = WhiffRecovery,
            KnockbackSpeed = KnockbackSpeed,
            ProngAngles = ProngAngles,
            FetterSeconds = FetterSeconds,
            PullSpeed = PullSpeed,
            GuardPierce = GuardPierce,
            // Codex PR #21 finding: the shared 2.6 default (every weapon
            // before Tocsin Mace has HitRange >= 2.6, so it never mattered)
            // can exceed a shorter blade's own reach, letting the lunge
            // decide "close enough to stop" before the swing's own hit-
            // range check would agree — a repeatable whiff at exactly the
            // wrong distance. Clamping to HitRange minus a small contact
            // margin guarantees the lunge always stops comfortably inside
            // the blade's real reach.
            LungeReachDistance = MathF.Min(MeleeTuning.DefaultLungeReachDistance, HitRange - 0.2f)
        };
    }

    public enum ReticuleAnchor
    {
        Target,
        Self
    }

    // Volley capability (ARMORY §6, Pass E): how a multi-point bomb's blast
    // centers are laid out relative to the impact point. Single (every bomb
    // before this pass) is the existing one-explosion behavior, unchanged.
    public enum BlastPattern
    {
        Single,
        Line, // a row straddling the impact, along the throw direction (Palisade)
        Split // two points offset from the impact (Pincer Charge)
    }

    public sealed class BombPart
    {
        public string Id;
        public string Name;
        public string Blurb;
        public float Damage; // PER BLAST POINT — DOCTRINE §13 pillar 3 "volley truth"
        public float EnduranceDamage;
        public float Cooldown;
        public float BlastRadius;
        public float ArcHeight; // lob apex above the midpoint
        public ReticuleAnchor ReticuleAnchor;
        public float ReticuleRange; // clamp for Target, fixed distance for Self

        public BlastPattern Pattern = BlastPattern.Single;
        public int BlastPoints = 1; // Line: total points in the row; Split: always 2
        public float BlastSpacing; // Line: meters between adjacent points; Split: offset from center to each side

        // Fetter capability (ARMORY_REFERENCE; DOCTRINE §13 pillar 9, Pass
        // F): every robo the blast actually hits also gets Fetter for this
        // many seconds -- flat, not scaled by the cluster-mini-blast
        // damage scale. 0 (every bomb before Rime Charge) applies nothing.
        public float FetterSeconds;
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

        // Fetter capability (ARMORY_REFERENCE; DOCTRINE §13 pillar 9, Pass
        // F): a melee attacker who gets PARRIED by this shield (Shielded or
        // GuardBreak) is fettered for this many seconds -- the parry punish,
        // shorter than a dedicated Fetter weapon's own hold. 0 (every
        // shield before Hoarfrost Ward) applies nothing.
        public float ParryFetterSeconds;
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

        // Fetter capability (ARMORY_REFERENCE; DOCTRINE §13 pillar 9, Pass
        // F): Winterwatch's "patient rime-ward that fetters whoever comes
        // near" is a proximity payload, not a normal ranged auto-fire --
        // ProximityRange > 0 swaps the pod's fire-range check from
        // CombatTuning.Pod.FireRange down to this tight radius (reusing the
        // existing fire pipeline, not a new aura/tick system), and
        // FetterSeconds carries on the shot the same way a gun's does. Both
        // 0 (every pod before Winterwatch) is today's unchanged behavior.
        public float ProximityRange;
        public float FetterSeconds;
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
        // Field-garniture charges per ARMORY_REFERENCE §3: Bannerman =
        // straight (the baseline), Vesper = slow gliding (hits hardest,
        // whiffs worst), Duskmantle = repeating short charges with NO
        // i-frames (the source's "no guard"), Cobalt Knight = diagonal
        // rising strike (Air kind — contests the sky).
        public static readonly BodyPart[] Bodies =
        {
            new BodyPart { Id = "vanguard", Name = "Bannerman", Blurb = "The Aureate Legion's standard-bearer pattern. Two air-dashes, no weaknesses, no edges.", HpMult = 1.0f, DefMult = 1.0f, AtkMult = 1.0f, DashType = DashType.Normal, DashCount = 2, SpeedMult = 1.0f,
                Charge = new ChargeSpec { Kind = ChargeKind.Attack, Damage = 110f, EnduranceDamage = 50f, Speed = 22f, StrikeTime = 0.35f, WindupTime = 0.25f, RecoveryTime = 0.8f, KnockbackSpeed = 12f, HitRange = 2.2f } },
            new BodyPart { Id = "skylance", Name = "Vesper", Blurb = "The Winter Wing's evening star: one long dash, hits hard, folds fast.", HpMult = 0.8f, DefMult = 1.2f, AtkMult = 1.25f, DashType = DashType.Long, DashCount = 1, SpeedMult = 1.05f,
                Charge = new ChargeSpec { Kind = ChargeKind.Attack, Damage = 175f, EnduranceDamage = 75f, Speed = 13f, StrikeTime = 0.8f, WindupTime = 0.35f, RecoveryTime = 1.25f, KnockbackSpeed = 15f, HitRange = 2.2f } },
            new BodyPart { Id = "wraith", Name = "Duskmantle", Blurb = "The Umbral Concordat's cowled evader. Two short vanish-dashes that phase through shots.", HpMult = 0.9f, DefMult = 1.1f, AtkMult = 0.9f, DashType = DashType.Vanish, DashCount = 2, SpeedMult = 1.0f,
                Charge = new ChargeSpec { Kind = ChargeKind.Attack, Damage = 50f, EnduranceDamage = 20f, Speed = 26f, StrikeTime = 0.16f, Strikes = 3, WindupTime = 0.2f, RecoveryTime = 0.9f, GrantsIFrames = false, KnockbackSpeed = 5f, HitRange = 2.0f } },
            new BodyPart { Id = "bulwark", Name = "Cobalt Knight", Blurb = "The Rust Cross's ancestral wall. One dash, huge health pool, shrugs off hits.", HpMult = 1.45f, DefMult = 0.75f, AtkMult = 1.0f, DashType = DashType.Normal, DashCount = 1, SpeedMult = 0.8f,
                Charge = new ChargeSpec { Kind = ChargeKind.Air, Damage = 140f, EnduranceDamage = 65f, Speed = 15f, StrikeTime = 0.5f, WindupTime = 0.3f, RecoveryTime = 1.0f, RiseSpeed = 9f, KnockbackSpeed = 13f, HitRange = 2.4f } }
        };

        public static readonly GunPart[] Guns =
        {
            new GunPart { Id = "blaster", Name = "Arbalest", Blurb = "The armory's workhorse. Honest damage, honest tracking.", Damage = 35f, EnduranceDamage = 16f, FireInterval = 0.38f, ProjectileSpeed = 32f, HomingTurnRate = 2.2f },
            new GunPart { Id = "needler", Name = "Litany", Blurb = "A recited pressure of weak, hard-curving darts. Death by repetition.", Damage = 14f, EnduranceDamage = 7f, FireInterval = 0.13f, ProjectileSpeed = 36f, HomingTurnRate = 3.4f },
            new GunPart { Id = "ram-cannon", Name = "Bombard", Blurb = "Siege-shot: slow, straight, brutal. One hit shreds endurance.", Damage = 90f, EnduranceDamage = 48f, FireInterval = 1.15f, ProjectileSpeed = 26f, HomingTurnRate = 0.6f },
            // ARMORY §4: "Three streams in a heraldic fan; better from afar."
            // Weak homing (SEEK 3, not maxed) so the fan visibly diverges
            // instead of instantly folding onto one point -- three streams
            // that gradually converge read as "better from afar" exactly
            // per its own identity. Damage is PER STREAM (pillar 3).
            new GunPart { Id = "trefoil", Name = "Trefoil", Blurb = "Three streams cast in a heraldic fan. Wide up close, converges kindly at range.", Damage = 22f, EnduranceDamage = 10f, FireInterval = 0.5f, ProjectileSpeed = 30f, HomingTurnRate = 1.0f, ProjectileCount = 3, SpreadDegrees = 24f },
            // Fetter capability (ARMORY §4, Pass F): "Two shackle-rounds;
            // near-guaranteed down up close." Bars 2/5/1/5/4 (MIGHT/BOLT/
            // SEEK/CADENCE/REND) -- low raw damage but max CADENCE (fast
            // FireInterval), high REND (heavy EnduranceDamage), low SEEK
            // (weak homing). The shackle payload is the 1s Fetter itself,
            // not the per-shot damage.
            new GunPart { Id = "fetterlock", Name = "Fetterlock", Blurb = "Two shackle-rounds fired short and fast. Near-guaranteed down up close.", Damage = 18f, EnduranceDamage = 40f, FireInterval = 0.24f, ProjectileSpeed = 40f, HomingTurnRate = 0.8f, FetterSeconds = 1.0f },
            // Pull capability (ARMORY §4, Pass G): "Barbed lines that haul
            // the target off their aim." Bars 1/3/5/4/1 (MIGHT/BOLT/SEEK/
            // CADENCE/REND) -- lowest MIGHT in the roster, near-max SEEK
            // (the claw tracks hard to keep the line taut). Flat damage
            // profile (§13.1) -- low but honest, the haul is the payload.
            new GunPart { Id = "grapnel", Name = "Grapnel", Blurb = "Barbed lines that haul the target off their aim.", Damage = 19f, EnduranceDamage = 12f, FireInterval = 0.55f, ProjectileSpeed = 34f, HomingTurnRate = 3.0f, PullSpeed = 11f },
            // Pull capability (ARMORY §4, Pass G): Sawtooth Espadon's
            // counterpart. "A grinding stream that drags the victim up its
            // own thread." Bars 3/5/2/3/4 (MIGHT/BOLT/SEEK/CADENCE/REND) --
            // Falloff profile (brutal close, per §13.1), Wrightsguild's one
            // relic art. Scoping call: "channeled hold" reads in code as a
            // fast grinding cadence (FireInterval 0.12s) where every
            // connecting round hauls -- not a new maintained-contact state
            // machine (flagged in the Task G handoff as its own possible
            // design pass; this pass reuses the existing per-shot pull
            // capability instead, matching the Task-ladder practice of
            // scoping down rather than building bespoke infra when the
            // existing shape already reads the identity honestly).
            new GunPart { Id = "auger", Name = "Auger", Blurb = "A grinding stream that drags the victim up its own thread.", Damage = 20f, EnduranceDamage = 16f, FireInterval = 0.12f, ProjectileSpeed = 30f, HomingTurnRate = 2.5f, PullSpeed = 6f }
        };

        public static readonly MeleeWeaponPart[] MeleeWeapons =
        {
            new MeleeWeaponPart { Id = "saber", Name = "Oathblade", Blurb = "The knight's standard. Balanced in every line.", Damage = 130f, EnduranceDamage = 55f, HitRange = 3.0f, HitArcDegrees = 70f, SwingActiveTime = 0.18f, HitRecovery = 0.45f, WhiffRecovery = 0.95f, KnockbackSpeed = 10f },
            new MeleeWeaponPart { Id = "warhammer", Name = "Dolorous Maul", Blurb = "The dolorous stroke: massive damage and knockback, ruinous to whiff.", Damage = 210f, EnduranceDamage = 90f, HitRange = 3.4f, HitArcDegrees = 80f, SwingActiveTime = 0.3f, HitRecovery = 0.75f, WhiffRecovery = 1.4f, KnockbackSpeed = 16f },
            new MeleeWeaponPart { Id = "twin-fang", Name = "Misericorde", Blurb = "The mercy-dagger: fast, light, barely punishable. Finishes what poise-loss starts.", Damage = 85f, EnduranceDamage = 35f, HitRange = 2.6f, HitArcDegrees = 70f, SwingActiveTime = 0.12f, HitRecovery = 0.28f, WhiffRecovery = 0.6f, KnockbackSpeed = 7f },
            // Trefoil's counterpart (ARMORY §5): "Wide crescent sweep (140°)
            // — punishes strafing." Longest reach in the roster (REACH 4);
            // GRACE 2 means a whiff costs more than Oathblade's.
            new MeleeWeaponPart { Id = "longglaive", Name = "Longglaive", Blurb = "A wide crescent sweep that answers strafing with reach. Whiff it and pay for the width.", Damage = 140f, EnduranceDamage = 58f, HitRange = 3.8f, HitArcDegrees = 140f, SwingActiveTime = 0.22f, HitRecovery = 0.55f, WhiffRecovery = 1.1f, KnockbackSpeed = 11f },
            // Thornswarm's counterpart (ARMORY §5): "Five heads strike five
            // angles at once; some always connect." Average reach (REACH 3,
            // unlike Longglaive) -- its forgiveness is angular, not distance.
            // 5 overlapping 40°-wide prongs 30° apart span -80..+80 -- wide,
            // continuous coverage delivered as five named strikes.
            new MeleeWeaponPart { Id = "hydra-flail", Name = "Hydra Flail", Blurb = "Five heads strike five angles in one motion. Somewhere in front of it, something connects.", Damage = 125f, EnduranceDamage = 55f, HitRange = 3.0f, HitArcDegrees = 40f, SwingActiveTime = 0.2f, HitRecovery = 0.5f, WhiffRecovery = 1.0f, KnockbackSpeed = 10f, ProngAngles = new[] { -60f, -30f, 0f, 30f, 60f } },
            // Fetter capability (ARMORY §5, Pass F): Fetterlock's counterpart.
            // "A bell-hammer that tolls through poise (REND 130)" -- REND
            // 130 is a literal EnduranceDamage callout in the doc. Bars
            // 2/3/3/3/5 (MIGHT/REACH/TEMPO/GRACE/REND): below-average damage,
            // average reach/tempo/grace, maxed REND.
            new MeleeWeaponPart { Id = "knell-maul", Name = "Knell Maul", Blurb = "A bell-hammer that tolls through poise. Fetters whatever it rings.", Damage = 85f, EnduranceDamage = 130f, HitRange = 3.0f, HitArcDegrees = 70f, SwingActiveTime = 0.18f, HitRecovery = 0.45f, WhiffRecovery = 0.95f, KnockbackSpeed = 10f, FetterSeconds = 0.8f },
            // Fetter capability (ARMORY §5, Pass F): Aspergill's counterpart.
            // "Light stopping taps; rings a foe still for the follow-up."
            // Bars 1/2/5/4/3: lowest MIGHT and REACH, near-max TEMPO (fast
            // swing) and GRACE (cheap recovery) -- a quick tap, not a
            // committed swing.
            new MeleeWeaponPart { Id = "tocsin-mace", Name = "Tocsin Mace", Blurb = "Light stopping taps that ring a foe still for the follow-up.", Damage = 55f, EnduranceDamage = 30f, HitRange = 2.4f, HitArcDegrees = 70f, SwingActiveTime = 0.10f, HitRecovery = 0.30f, WhiffRecovery = 0.5f, KnockbackSpeed = 6f, FetterSeconds = 0.7f },
            // Pull capability (ARMORY §5, Pass G): Grapnel's counterpart.
            // "The billhook that dragged knights from horses; hauls them to
            // you." Bars 2/4/3/3/1 (MIGHT/REACH/TEMPO/GRACE/REND) -- longer
            // reach than Oathblade (the hook needs to catch at range),
            // lowest REND (the haul is the point, not the damage). PullSpeed
            // set, KnockbackSpeed left at 0 -- a weapon either shoves or
            // hauls.
            new MeleeWeaponPart { Id = "hookbill", Name = "Hookbill", Blurb = "The billhook that dragged knights from horses. Hauls them to you.", Damage = 95f, EnduranceDamage = 40f, HitRange = 3.4f, HitArcDegrees = 65f, SwingActiveTime = 0.22f, HitRecovery = 0.5f, WhiffRecovery = 1.0f, PullSpeed = 14f },
            // Guard-piercing capability (ARMORY §5, Pass G): Mangonel's
            // counterpart. "Narrow thrust that pierces 60% of a raised
            // shield's GUARD." Bars 3/3/3/3/2 (MIGHT/REACH/TEMPO/GRACE/REND)
            // -- an even, unremarkable spread everywhere except the pierce,
            // which is the entire identity.
            new MeleeWeaponPart { Id = "estoc", Name = "Estoc", Blurb = "A narrow thrust that pierces 60% of a raised shield's guard.", Damage = 100f, EnduranceDamage = 45f, HitRange = 3.0f, HitArcDegrees = 50f, SwingActiveTime = 0.16f, HitRecovery = 0.4f, WhiffRecovery = 0.85f, KnockbackSpeed = 9f, GuardPierce = 0.6f },
            // Pull capability (ARMORY §5, Pass G): Auger's counterpart. "A
            // grinding hold that ticks damage and drags the foe up the
            // blade." Bars 3/3/3/2/4 (MIGHT/REACH/TEMPO/GRACE/REND) --
            // Wrightsguild's one relic melee art alongside Auger. Same
            // scoping call as Auger: the "grinding hold" reads as a heavy
            // single connecting hit with a strong haul (PullSpeed), not a
            // new maintained-contact/tick-damage state machine -- flagged in
            // the Task G handoff as a candidate for its own future design
            // pass if the feel doesn't read as "grinding" enough.
            new MeleeWeaponPart { Id = "sawtooth-espadon", Name = "Sawtooth Espadon", Blurb = "A grinding hold that ticks damage and drags the foe up the blade.", Damage = 150f, EnduranceDamage = 65f, HitRange = 3.0f, HitArcDegrees = 60f, SwingActiveTime = 0.26f, HitRecovery = 0.6f, WhiffRecovery = 1.15f, PullSpeed = 9f }
        };

        public static readonly BombPart[] Bombs =
        {
            new BombPart { Id = "impact", Name = "Censer", Blurb = "The swung vessel of fire. Reticule tracks the enemy -- hold to aim, release to throw.", Damage = 80f, EnduranceDamage = 35f, Cooldown = 5f, BlastRadius = 3.2f, ArcHeight = 5f, ReticuleAnchor = ReticuleAnchor.Target, ReticuleRange = 20f },
            new BombPart { Id = "quake", Name = "Anathema Charge", Blurb = "The great condemnation: huge blast, heavy endurance crush, long rearm. Fixed just ahead of you -- close-range, high commitment.", Damage = 120f, EnduranceDamage = 70f, Cooldown = 9f, BlastRadius = 4.5f, ArcHeight = 6.5f, ReticuleAnchor = ReticuleAnchor.Self, ReticuleRange = 4f },
            // ARMORY §6: "A stake-wall of blasts before you; charges die on
            // it." Self-anchored like Anathema (a placed defensive line, not
            // a thrown-at-them shot) -- 3 points spaced 2.4m apart, each a
            // modest 79/55 blast per DOCTRINE pillar 3 (Damage is per point).
            new BombPart { Id = "palisade", Name = "Palisade", Blurb = "A stake-wall of blasts, planted just ahead of you. Charges die on it.", Damage = 79f, EnduranceDamage = 55f, Cooldown = 8f, BlastRadius = 2.6f, ArcHeight = 5f, ReticuleAnchor = ReticuleAnchor.Self, ReticuleRange = 5.5f, Pattern = BlastPattern.Line, BlastPoints = 3, BlastSpacing = 2.4f },
            // ARMORY §6: "Splits to blast both sides at once — sides afoot,
            // fore-and-aft aloft." Target-anchored like Censer. The split
            // axis (BombSystem.BlastPoints) reads the thrower's grounded
            // state AT RELEASE: perpendicular to the throw line grounded
            // ("sides"), along it airborne ("fore-and-aft") -- matching the
            // G/A-differs idiom already used across the gun roster.
            new BombPart { Id = "pincer-charge", Name = "Pincer Charge", Blurb = "Splits apart to blast both sides at once. Grounded, it opens wide; aloft, it closes the loops fore and aft.", Damage = 42f, EnduranceDamage = 26f, Cooldown = 6f, BlastRadius = 2.4f, ArcHeight = 5f, ReticuleAnchor = ReticuleAnchor.Target, ReticuleRange = 18f, Pattern = BlastPattern.Split, BlastPoints = 2, BlastSpacing = 3.2f },
            // Fetter capability (ARMORY §6, Pass F): "Almost harmless; holds
            // the foe for the real blow." Bars 1/3/2/4/1 (MIGHT/LOFT/BREADTH/
            // LINGER/REND), Dmg 8 -- both directly from the doc. The Fetter
            // hold is the actual payload, not the pittance blast.
            new BombPart { Id = "rime-charge", Name = "Rime Charge", Blurb = "Almost harmless on its own -- it holds the foe still for the real blow.", Damage = 8f, EnduranceDamage = 10f, Cooldown = 6f, BlastRadius = 2.6f, ArcHeight = 5f, ReticuleAnchor = ReticuleAnchor.Target, ReticuleRange = 18f, FetterSeconds = 1.2f }
        };

        // Shield toll/raise data per ARMORY_REFERENCE §7 (GUARD front/back,
        // SOAK = ShieldHp, MEND = RegenPerSec, TOLL, RIPOSTE = parry drain).
        public static readonly ShieldPart[] Shields =
        {
            new ShieldPart { Id = "aegis", Name = "Ward Veil", Blurb = "Light energy veil: fast mend, blocks ~75% up front, ~25% behind. Raised midair it holds you hovering -- the flier's shield.", ShieldHp = 180f, RegenPerSec = 25f, RegenDelay = 2.0f, FrontBlockPercent = 0.75f, BackBlockPercent = 0.25f, MeleeParryEnduranceDamage = 20f, TollSeconds = 2.5f, GroundRaise = ShieldGroundRaise.Root, AirRaise = ShieldAirRaise.Hold },
            new ShieldPart { Id = "bastion", Name = "Pavise", Blurb = "The great standing wall-shield: bigger buffer, blocks ~92% up front, mends slowly, long toll. Raised midair it slams you to the ground.", ShieldHp = 260f, RegenPerSec = 6f, RegenDelay = 3.5f, FrontBlockPercent = 0.92f, BackBlockPercent = 0.4f, MeleeParryEnduranceDamage = 32f, TollSeconds = 6f, GroundRaise = ShieldGroundRaise.Root, AirRaise = ShieldAirRaise.Drop },
            new ShieldPart { Id = "targe", Name = "Targe", Blurb = "The only shield you can advance behind: march at 40% speed while raised. Thin plate, quick mend, the shortest toll.", ShieldHp = 110f, RegenPerSec = 30f, RegenDelay = 1.5f, FrontBlockPercent = 0.60f, BackBlockPercent = 0.15f, MeleeParryEnduranceDamage = 16f, TollSeconds = 1.5f, GroundRaise = ShieldGroundRaise.March, AirRaise = ShieldAirRaise.None, MarchSpeedMult = 0.4f },
            new ShieldPart { Id = "kite-ward", Name = "Kite Ward", Blurb = "The knight's standard; balanced in every line. Hold to raise -- rooted while up.", ShieldHp = 200f, RegenPerSec = 14f, RegenDelay = 2.5f, FrontBlockPercent = 0.80f, BackBlockPercent = 0.30f, MeleeParryEnduranceDamage = 24f, TollSeconds = 3.5f, GroundRaise = ShieldGroundRaise.Root, AirRaise = ShieldAirRaise.None },
            new ShieldPart { Id = "quiet-bell", Name = "Quiet Bell", Blurb = "A dome of hush: for a breath after raising, blasts and through-wall harm are met with the full guard from every side.", ShieldHp = 150f, RegenPerSec = 16f, RegenDelay = 2.5f, FrontBlockPercent = 0.65f, BackBlockPercent = 0.35f, MeleeParryEnduranceDamage = 18f, TollSeconds = 4f, GroundRaise = ShieldGroundRaise.Root, AirRaise = ShieldAirRaise.None, BlastMuffleSeconds = 1.5f },
            // Fetter capability (ARMORY §7, Pass F): Rime Charge's
            // counterpart. "Melee against it leaves the attacker rimed and
            // slowed." GUARD 70/25%, SOAK 170, MEND 15/s, TOLL 4, RIPOSTE
            // 22 -- all straight from the doc. ParryFetterSeconds is
            // deliberately shorter than a dedicated Fetter weapon's own
            // hold (0.6s vs Fetterlock/Rime's ~1-1.2s): this is a parry
            // punish, not a primary lock tool.
            new ShieldPart { Id = "hoarfrost-ward", Name = "Hoarfrost Ward", Blurb = "Melee against it leaves the attacker rimed and slowed.", ShieldHp = 170f, RegenPerSec = 15f, RegenDelay = 2.5f, FrontBlockPercent = 0.70f, BackBlockPercent = 0.25f, MeleeParryEnduranceDamage = 22f, TollSeconds = 4f, GroundRaise = ShieldGroundRaise.Root, AirRaise = ShieldAirRaise.None, ParryFetterSeconds = 0.6f }
        };

        public static readonly PodPart[] Pods =
        {
            new PodPart { Id = "sentry", Name = "Iron Squire", Blurb = "The loyal retainer: steady chip fire while you reposition.", Damage = 8f, EnduranceDamage = 5f, FireInterval = 0.8f, EnergyMax = 100f, EnergyPerShot = 12f, EnergyRegenPerSec = 9f },
            new PodPart { Id = "hornet", Name = "Kestrel", Blurb = "The cast hawk: fast stooping bursts, then an empty glove.", Damage = 6f, EnduranceDamage = 8f, FireInterval = 0.35f, EnergyMax = 80f, EnergyPerShot = 16f, EnergyRegenPerSec = 7f },
            // Fetter capability (ARMORY §8, Pass F): "A patient rime-ward
            // that fetters whoever comes near." Bars 1/2/3/4/4 (MIGHT/HASTE/
            // SEEK/BREADTH/LINGER) -- lowest MIGHT (near-harmless chip) and
            // below-average HASTE (patient, doesn't chase). ProximityRange
            // is far tighter than the normal 22m pod FireRange -- it waits
            // for the target to close, not the other way round. The Aloft/
            // Afoot G/A split named in the doc isn't mechanically
            // differentiated in this pass (scoping call, TASK_LADDER Pass F
            // handoff): one uniform proximity behavior regardless of
            // grounded state, matching the built-Kestrel/Iron-Squire
            // precedent of a single PodPart entry per weapon identity.
            new PodPart { Id = "winterwatch", Name = "Winterwatch", Blurb = "A patient rime-ward that fetters whoever comes near.", Damage = 4f, EnduranceDamage = 6f, FireInterval = 1.1f, EnergyMax = 90f, EnergyPerShot = 14f, EnergyRegenPerSec = 8f, ProximityRange = 6f, FetterSeconds = 1.0f }
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
