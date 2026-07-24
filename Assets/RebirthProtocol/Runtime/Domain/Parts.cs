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

    // Trajectory suite (ARMORY §4, Pass I1): the path a gun round flies.
    // Direct (every gun before this pass) is the straight/homing + optional
    // Vigil-hang behavior. The three new shapes each ignore arena geometry
    // (walls are "negotiable"), resolving hits only against robos and crates.
    public enum ProjectilePath
    {
        Direct, // straight, optionally homing/hanging (every existing gun)
        Vault,  // up-launched parabola that clears walls, falling toward the target (Mangonel)
        Drop,   // materializes above the target's mark and descends, bypassing cover (Skysword, Evenfall)
        Loop    // curves at a constant yaw, tracing a wide loop that returns (Falconet aloft, Volant Falx)
    }

    // Stance-split (ARMORY §13.1): which stance the special Path applies in.
    // Aloft/grounded outside that stance falls back to Direct — matching the
    // G/A-differs idiom already used across the roster (Vigil, Pincer, etc.).
    public enum ProjectilePathStance
    {
        Always,       // the Path applies in both stances (Mangonel, Skysword)
        GroundedOnly, // special grounded, Direct aloft (Evenfall's "(G only)")
        AloftOnly     // special aloft, Direct grounded (Falconet's "aloft ... glide")
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

        // Trajectory suite (ARMORY §4, Pass I1): the flight path and the
        // stance it applies in. Default Direct/Always is every gun before this
        // pass, unchanged. See ProjectilePath.
        public ProjectilePath Path = ProjectilePath.Direct;
        public ProjectilePathStance PathStance = ProjectilePathStance.Always;

        // Mixed-path guns (Mangonel, "two straight, two vaulting"): the first
        // PlainStreams of ProjectileCount streams fly Direct regardless of
        // Path; the rest take Path. 0 (every other gun) = all streams take
        // Path. Only meaningful with ProjectileCount > PlainStreams.
        public int PlainStreams;

        // Vault: initial upward launch speed. Gravity (CombatTuning.Move)
        // brings the round back down past wall height, over cover, onto the
        // target it homes toward horizontally.
        public float VaultRise;

        // Drop: how far above the target's mark the round materializes before
        // descending (Skysword straight down; Evenfall after its even-fall
        // hang, then descending homing).
        public float DropHeight;

        // Loop: constant yaw rate (rad/s) that bends the round's heading into
        // a wide returning loop. Multiple Loop streams mirror the sign so they
        // fan out symmetrically.
        public float LoopTurnRate;

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

        // Range-profile damage scaling (ARMORY §13.1, Pass H): the round's
        // damage scales with how far it has flown, evaluated at impact.
        // Default (Flat) returns 1.0 at every distance, so every gun before
        // Pilgrim/Beacon is unchanged. Damage stays the REFERENCE value; the
        // profile scales it (Rangecraft ramps up, Burst-point peaks at a set
        // distance).
        public RangeScaling RangeScaling;

        // Trap-hang (Vigil, ARMORY §13.1 Stance-split, Pass H): fired
        // GROUNDED, the round flies HangDistance metres, hovers in place for
        // HangDuration seconds ("keeps watch, near-invisible"), then
        // re-accelerates homing at the target ("then strike"). Fired aloft it
        // stays an ordinary straight/homing shot. Both 0 (every gun before
        // Vigil) = no hang, unchanged.
        public float HangDistance;
        public float HangDuration;
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

        // Damage scaling (ARMORY §13.1, Pass H): Courser Saber (wielder
        // speed at commit), Tilt Lance (lunge distance charged), Crowbeak
        // Pick (distance to target -- power in the tip). None (default,
        // every weapon before this pass) is a flat 1.0.
        public MeleeScaling Scaling;

        // Late-strike (Penitent Flail, Pass H): the swing's hit only
        // registers in the final (1 - StrikeDelayFraction) of its active
        // window -- "the arc is unreadable and the timing is late." 0 (every
        // weapon before it) registers from the first active frame, unchanged.
        public float StrikeDelayFraction;

        // Casting wave (Volant Falx, ARMORY §5, Pass I1): the first melee that
        // spawns a projectile. When WaveSpeed > 0 a connecting-or-not swing
        // also casts a looping crescent wave (ProjectilePath.Loop, HitSource
        // .Melee) in the facing direction -- it reaches past the blade and
        // curls back for a second pass. The normal contact hit still lands at
        // melee range; the wave is the ranged extension. 0 (every weapon
        // before it) casts nothing.
        public float WaveSpeed;
        public float WaveLoopTurnRate;
        public float WaveDamage;
        public float WaveEnduranceDamage;

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
            Scaling = Scaling,
            StrikeDelayFraction = StrikeDelayFraction,
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

    // Trajectory suite (ARMORY §6, Pass I2): the shape a thrown bomb's flight
    // traces between the hand and the mark. Lob (every bomb before this pass)
    // is the straight-line lerp under a symmetric sine bump. Every path lands
    // on the SAME reticule mark -- the reticule never lies about where the
    // blast will be; only the route there differs. All three are evaluated
    // analytically from the flight parameter T (a pure function of T, never
    // integrated step-by-step), so a coarse frame can't drift the landing.
    public enum BombPath
    {
        Lob,     // straight lerp + symmetric sine arc (every existing bomb)
        Steeple, // finishes its ground travel by the apex, then falls straight down (Steeplefall)
        Bend     // bows wide to the named side and converges back on the mark (Oxbow Charge)
    }

    // Heraldic mirror (ARMORY §1.1): which side a Bend path bows toward.
    // Mirrored parts are ONE entry with a side variant, not two catalog rows.
    public enum BombBendSide
    {
        Dexter,  // bows to the thrower's right
        Sinister // bows to the thrower's left
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

        // Trajectory suite (ARMORY §6, Pass I2). Default Lob is every bomb
        // before this pass, unchanged. See BombPath.
        public BombPath Path = BombPath.Lob;

        // Multiplier on the distance-derived flight time. 1 (every bomb
        // before this pass) is the plain dist/LobSpeed lob. Steeplefall runs
        // long: a throw that "climbs past steeple height" has to spend the
        // seconds to get up there and back down, and that hang IS the weapon
        // (its LINGER bar) -- a 14m climb crammed into a half-second flight
        // would read as a blur, not a steeple.
        public float FlightTimeMult = 1f;

        // Bend: how far the bow swings off the straight throw line at its
        // widest (meters), and which side it swings to. Only read by
        // BombPath.Bend.
        public float BendWidth;
        public BombBendSide BendSide = BombBendSide.Dexter;

        // Contact detonation: a bomb in flight blows on the first ENEMY robo
        // it sweeps within this many meters of, instead of riding out its
        // flight to the mark. 0 (every bomb before this pass) means the
        // flight is untouchable and the blast always lands on the reticule.
        //
        // This is what gives Oxbow's bow teeth. Bomb flight ignores arena
        // geometry entirely -- no bomb has ever been stopped by a wall -- so
        // a curving path with no contact check would be pure decoration: the
        // blast would land on the same mark a straight lob reaches. With
        // contact, "bends around cover from the named side" means what it
        // says: cover still doesn't stop it (crates and walls are NOT
        // contact-eligible, only robos), but a foe hugging cover on the
        // NAMED flank is swept up on the way past, where a straight lob
        // would have sailed over their head onto the mark behind them. The
        // owner is never contact-eligible -- the bomb has to leave the hand.
        public float ContactRadius;

        // Dwelling mines (ARMORY §6, Pass I2): on landing the bomb does not
        // detonate. It settles near-invisible and waits, blowing when a robo
        // comes within DwellTriggerRadius or when the wait runs out -- "lands,
        // waits near-invisible, remembers." 0 (every bomb before this pass)
        // detonates on landing as always.
        public float DwellSeconds;
        public float DwellTriggerRadius;

        // Oubliette Twin: one throw that plants MineCount separate pits,
        // spaced MineSpacing apart across the throw line. Each flies, lands,
        // dwells, and triggers INDEPENDENTLY -- which is what separates this
        // from BlastPattern.Split, where one bomb detonates at several points
        // at the same instant. 1 (every other bomb) throws a single bomb.
        public int MineCount = 1;
        public float MineSpacing;
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
            new GunPart { Id = "auger", Name = "Auger", Blurb = "A grinding stream that drags the victim up its own thread.", Damage = 20f, EnduranceDamage = 16f, FireInterval = 0.12f, ProjectileSpeed = 30f, HomingTurnRate = 2.5f, PullSpeed = 6f },
            // Rangecraft profile (ARMORY §4/§13.1, Pass H): Courser Saber's
            // counterpart. "Rounds that grow stronger the farther they
            // travel" (volley 53→114). Bars 3/3/2/3/4. The reference Damage
            // is scaled by distance flown -- weak point-blank (0.55x), strong
            // at range (1.7x at 26m), rewarding the long field. Hedge
            // Errantry's own gun.
            new GunPart { Id = "pilgrim", Name = "Pilgrim", Blurb = "Flame-rounds that grow stronger the farther they travel. Reward for keeping the long field.", Damage = 34f, EnduranceDamage = 14f, FireInterval = 0.42f, ProjectileSpeed = 30f, HomingTurnRate = 1.8f, RangeScaling = RangeScaling.Rangecraft(0.55f, 1.7f, 26f) },
            // Trap / stance-split (ARMORY §4/§13.1, Pass H): Penitent Flail's
            // counterpart. "Rounds keep watch, near-invisible, then strike
            // (G); straight aloft" (228 G / 74 A). Bars 3/3/2/4/1. Fired
            // grounded the round flies out, hovers ~1.1s keeping watch, then
            // re-accelerates homing hard. Fired aloft it's a plain straight
            // shot (the hang is gated on Grounded in TickGun). Scoping call:
            // the trap-vs-straight BEHAVIOR is the stance-split identity; the
            // numeric G/A damage difference (228 vs 74) is deferred, same as
            // Winterwatch's G/A split in Pass F. Umbral Concordat's.
            new GunPart { Id = "vigil", Name = "Vigil", Blurb = "Rounds that keep watch where you place them, near-invisible, then strike. Straight-fired aloft.", Damage = 85f, EnduranceDamage = 20f, FireInterval = 0.9f, ProjectileSpeed = 28f, HomingTurnRate = 2.5f, HangDistance = 9f, HangDuration = 1.1f },
            // Burst-point profile (ARMORY §4/§13.1, Pass H): Crowbeak Pick's
            // counterpart. "Bursts at a set distance; time the blossom or
            // waste the shot" (124 at burst, 47 past it). Bars 4/4/2/3/3.
            // Reference Damage 75 x 1.65 at the 18m bloom (~124), x 0.6
            // early/late (~45) -- the round has to be spaced to its burst.
            // Solarian Talon's.
            new GunPart { Id = "beacon", Name = "Beacon", Blurb = "A flare that blooms at a set distance. Time the blossom or waste the shot.", Damage = 75f, EnduranceDamage = 30f, FireInterval = 0.7f, ProjectileSpeed = 34f, HomingTurnRate = 1.2f, RangeScaling = RangeScaling.BurstPoint(1.65f, 0.6f, 18f, 3.5f) },
            // Vault trajectory (ARMORY §4, Pass I1): "Two straight, two
            // vaulting -- the vault clears walls." Bars 2/3/3/2/4 (MIGHT/BOLT/
            // SEEK/CADENCE/REND) -- slow cadence, heavy REND. 4 streams, the
            // first 2 plain homing shots, the last 2 up-launched vaults that
            // arc over cover (PlainStreams = 2). Damage is PER STREAM (pillar
            // 3); volley 76 ~= 4 x 19. Estoc's counterpart (built Pass G).
            new GunPart { Id = "mangonel", Name = "Mangonel", Blurb = "Two rounds fly straight, two vault the wall between you. Cover only helps them.", Damage = 19f, EnduranceDamage = 22f, FireInterval = 0.7f, ProjectileSpeed = 30f, HomingTurnRate = 1.6f, ProjectileCount = 4, Path = ProjectilePath.Vault, PlainStreams = 2, VaultRise = 13f },
            // Drop trajectory / stance-split (ARMORY §4/§13.1, Pass I1): "Four
            // rounds hang at even-fall, then descend homing (G only)." Bars
            // 3/3/4/2/2 -- strong SEEK (the descent tracks). Fired grounded,
            // 4 rounds materialize above the mark, hang ~0.7s at an even
            // height, then descend homing. Fired aloft they are plain shots
            // (PathStance GroundedOnly). Volley 84 ~= 4 x 21. Pendulum
            // Glaive's counterpart. Scoping call: the numeric G/A split the
            // doc omits is deferred, same as Vigil/Winterwatch before it.
            new GunPart { Id = "evenfall", Name = "Evenfall", Blurb = "Four rounds hang at even-fall, then descend homing. Plain-fired aloft.", Damage = 21f, EnduranceDamage = 9f, FireInterval = 0.85f, ProjectileSpeed = 26f, HomingTurnRate = 2.6f, ProjectileCount = 4, Path = ProjectilePath.Drop, PathStance = ProjectilePathStance.GroundedOnly, DropHeight = 10f, HangDuration = 0.7f },
            // Drop trajectory (ARMORY §4, Pass I1): "Blades cast heavenward
            // that fall on the mark -- cover is negotiable." Bars 3/5/2/4/3 --
            // BOLT 5, heavy per-blade damage. 3 blades materialize above the
            // target's fire-time position and fall STRAIGHT down (no homing):
            // they hit the mark, not the mover, but no horizontal wall can
            // stop them. Volley 95 ~= 3 x 32. Steeple Strike's counterpart.
            new GunPart { Id = "skysword", Name = "Skysword", Blurb = "Blades cast heavenward that fall on the mark. Cover is negotiable.", Damage = 32f, EnduranceDamage = 16f, FireInterval = 0.55f, ProjectileSpeed = 30f, HomingTurnRate = 0f, ProjectileCount = 3, Path = ProjectilePath.Drop, DropHeight = 11f },
            // Loop trajectory / stance-split (ARMORY §4/§13.1, Pass I1):
            // "Aloft, its rounds glide wide loops and return for a second
            // pass" (56 G / 113 A). Bars 3/2/5/3/1 -- max SEEK, lowest REND.
            // Fired grounded it is a plain homing pair (PathStance AloftOnly);
            // aloft the two rounds bend into mirrored wide loops that sweep
            // the target line out and back. Volant Falx's counterpart.
            new GunPart { Id = "falconet", Name = "Falconet", Blurb = "Afoot, a plain pair of shots. Aloft, they glide wide loops and return for a second pass.", Damage = 28f, EnduranceDamage = 6f, FireInterval = 0.6f, ProjectileSpeed = 30f, HomingTurnRate = 3.4f, ProjectileCount = 2, Path = ProjectilePath.Loop, PathStance = ProjectilePathStance.AloftOnly, LoopTurnRate = 2.6f }
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
            new MeleeWeaponPart { Id = "sawtooth-espadon", Name = "Sawtooth Espadon", Blurb = "A grinding hold that ticks damage and drags the foe up the blade.", Damage = 150f, EnduranceDamage = 65f, HitRange = 3.0f, HitArcDegrees = 60f, SwingActiveTime = 0.26f, HitRecovery = 0.6f, WhiffRecovery = 1.15f, PullSpeed = 9f },
            // Speed scaling (ARMORY §5/§13.1, Pass H): Pilgrim's counterpart.
            // "Damage scales with your current speed — never swing standing
            // still." Bars 3/3/4/3/2. The wielder's horizontal speed at the
            // instant the swing commits scales the hit: 0.5x barely moving,
            // 1.5x at a full run (RunSpeed 9). Winter Wing rides fast.
            new MeleeWeaponPart { Id = "courser-saber", Name = "Courser Saber", Blurb = "A running cut that scales with your speed. Never swing it standing still.", Damage = 115f, EnduranceDamage = 48f, HitRange = 3.0f, HitArcDegrees = 70f, SwingActiveTime = 0.16f, HitRecovery = 0.4f, WhiffRecovery = 0.85f, KnockbackSpeed = 10f, Scaling = MeleeScaling.Ramp(MeleeScaleMode.Speed, 0.5f, 1.5f, 1.5f, 9f) },
            // Lunge-distance scaling (ARMORY §5/§13.1, Pass H): Longshrift's
            // counterpart. "The joust: damage scales with lunge distance
            // (60→190)." Bars 5/4/2/1/4 -- longest reach, worst GRACE (a
            // whiffed joust is ruinous). The farther the gap-closer charged,
            // the harder it lands: 0.5x on a standing thrust, 1.55x on a full
            // ~12m joust. Kurultai Vanguard's -- the joust is theirs.
            new MeleeWeaponPart { Id = "tilt-lance", Name = "Tilt Lance", Blurb = "The joust: the farther you charge before it lands, the harder it hits. Ruinous to whiff.", Damage = 125f, EnduranceDamage = 52f, HitRange = 3.6f, HitArcDegrees = 45f, SwingActiveTime = 0.2f, HitRecovery = 0.5f, WhiffRecovery = 1.5f, KnockbackSpeed = 15f, Scaling = MeleeScaling.Ramp(MeleeScaleMode.LungeDistance, 0.5f, 1.55f, 0f, 12f) },
            // Late-strike (ARMORY §5, Pass H): Vigil's counterpart. "The arc
            // is unreadable and the timing is late." Bars 4/3/2/2/3. A long
            // (0.4s) wide swing whose hit only registers in the final 40% of
            // its active window -- you cannot read when it will land. Umbral
            // Concordat's.
            new MeleeWeaponPart { Id = "penitent-flail", Name = "Penitent Flail", Blurb = "A wide, late arc you cannot read. It lands when you have stopped expecting it.", Damage = 135f, EnduranceDamage = 55f, HitRange = 3.2f, HitArcDegrees = 90f, SwingActiveTime = 0.4f, HitRecovery = 0.5f, WhiffRecovery = 1.1f, KnockbackSpeed = 12f, StrikeDelayFraction = 0.6f },
            // Tip scaling (ARMORY §5/§13.1, Pass H): Beacon's counterpart.
            // "All the power lives in the beak's tip — space it or waste it."
            // Bars 4/3/3/3/3. Damage scales with distance to the target:
            // 0.35x smothered up close, 1.6x at the far edge of its reach.
            // Solarian Talon's.
            new MeleeWeaponPart { Id = "crowbeak-pick", Name = "Crowbeak Pick", Blurb = "All the power lives in the beak's tip. Space it exactly or waste it.", Damage = 120f, EnduranceDamage = 50f, HitRange = 3.2f, HitArcDegrees = 45f, SwingActiveTime = 0.16f, HitRecovery = 0.42f, WhiffRecovery = 0.9f, KnockbackSpeed = 11f, Scaling = MeleeScaling.Ramp(MeleeScaleMode.Tip, 0.35f, 1.6f, 1.4f, 3.2f) },
            // Casting wave (ARMORY §5, Pass I1): Falconet's counterpart and
            // the first melee that spawns a projectile. "A looping crescent
            // wave that returns for a second pass." Bars 3/4/3/2/1 -- long
            // REACH (the wave extends it), lowest REND. A normal swing lands
            // its contact hit at melee range AND casts a looping wave
            // (ProjectilePath.Loop) that curls out past the blade and back.
            // Wave damage is deliberately below the contact hit -- the reach
            // is the payload, not the raw number.
            new MeleeWeaponPart { Id = "volant-falx", Name = "Volant Falx", Blurb = "A looping crescent wave that returns for a second pass.", Damage = 90f, EnduranceDamage = 30f, HitRange = 3.6f, HitArcDegrees = 80f, SwingActiveTime = 0.2f, HitRecovery = 0.5f, WhiffRecovery = 1.0f, KnockbackSpeed = 9f, WaveSpeed = 26f, WaveLoopTurnRate = 2.2f, WaveDamage = 42f, WaveEnduranceDamage = 14f }
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
            new BombPart { Id = "rime-charge", Name = "Rime Charge", Blurb = "Almost harmless on its own -- it holds the foe still for the real blow.", Damage = 8f, EnduranceDamage = 10f, Cooldown = 6f, BlastRadius = 2.6f, ArcHeight = 5f, ReticuleAnchor = ReticuleAnchor.Target, ReticuleRange = 18f, FetterSeconds = 1.2f },
            // Steeple trajectory (ARMORY §6, Pass I2): "Climbs past steeple
            // height, falls straight down." Bars 3/2/4/4/3, Dmg 64-66.
            // ArcHeight 14 is roughly three times the roster's lob apex --
            // the "past steeple height" climb -- and FlightTimeMult 1.8
            // buys the seconds to make that climb read as a climb. The
            // payoff for the telegraph is a blast that arrives vertically:
            // BombPath.Steeple finishes ALL its ground travel by the apex,
            // so the last half of the flight is a straight drop onto the
            // mark (LINGER 4, the delayed-threat bar).
            new BombPart { Id = "steeplefall", Name = "Steeplefall", Blurb = "Climbs past steeple height, then falls straight down on the mark.", Damage = 66f, EnduranceDamage = 40f, Cooldown = 7f, BlastRadius = 3.6f, ArcHeight = 14f, ReticuleAnchor = ReticuleAnchor.Target, ReticuleRange = 20f, Path = BombPath.Steeple, FlightTimeMult = 1.8f },
            // Bend trajectory (ARMORY §6, Pass I2): "Bends around cover from
            // the named side." Bars 3/3/4/2/3, Dmg 63/44. One entry with a
            // side flag per the §1.1 mirror convention -- the Sinister
            // variant is this same part with BendSide flipped, not a second
            // catalog row. Flies LOW (ArcHeight 1.8, barely over head
            // height) because the bow only means anything at a height where
            // it can actually sweep a foe: ContactRadius 2.2 blows it on the
            // first enemy the bow passes, so hugging cover on the dexter
            // flank is punished while the wall itself never stops it.
            new BombPart { Id = "oxbow-charge", Name = "Oxbow Charge (Dexter)", Blurb = "Swings wide to the right and comes back in -- cover on that flank is no cover at all.", Damage = 63f, EnduranceDamage = 30f, Cooldown = 6f, BlastRadius = 3.4f, ArcHeight = 1.8f, ReticuleAnchor = ReticuleAnchor.Target, ReticuleRange = 18f, Path = BombPath.Bend, BendWidth = 5f, BendSide = BombBendSide.Dexter, ContactRadius = 2.2f },
            // Dwelling mine (ARMORY §6, Pass I2): "Lands, waits near-
            // invisible, remembers." Bars 4/3/4/5/3 (LINGER 5, the roster's
            // longest), Dmg 79/55. It does not detonate on landing: it
            // settles, dims, and waits up to 8s, blowing when anyone strays
            // within 3m -- or on its own when the wait runs out. It never
            // simply forgets, which is the "remembers" in the blurb.
            new BombPart { Id = "oubliette-mine", Name = "Oubliette Mine", Blurb = "Lands quiet and waits. It does not forget who walks over it.", Damage = 79f, EnduranceDamage = 45f, Cooldown = 8f, BlastRadius = 3.6f, ArcHeight = 4f, ReticuleAnchor = ReticuleAnchor.Target, ReticuleRange = 18f, DwellSeconds = 8f, DwellTriggerRadius = 3f },
            // Twin mines (ARMORY §6, Pass I2): "Two forgotten pits for the
            // price of one throw." Bars 2/3/3/3/1, Dmg 32 x2 -- Damage is
            // per pit, per DOCTRINE pillar 3. MineCount 2 plants two
            // genuinely independent bombs across the throw line, each with
            // its own flight, its own wait, and its own trigger; that
            // independence is what makes this a different weapon from
            // Pincer Charge, whose two points are one blast at one instant.
            new BombPart { Id = "oubliette-twin", Name = "Oubliette Twin", Blurb = "Two forgotten pits for the price of one throw. Step wrong twice.", Damage = 32f, EnduranceDamage = 16f, Cooldown = 6f, BlastRadius = 2.6f, ArcHeight = 4f, ReticuleAnchor = ReticuleAnchor.Target, ReticuleRange = 18f, DwellSeconds = 6f, DwellTriggerRadius = 2.6f, MineCount = 2, MineSpacing = 3f }
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
