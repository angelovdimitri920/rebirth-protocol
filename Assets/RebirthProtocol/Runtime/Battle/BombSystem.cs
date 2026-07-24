using System.Collections.Generic;
using RebirthProtocol.Battle.Audio;
using RebirthProtocol.Battle.Effects;
using RebirthProtocol.Domain;
using UnityEngine;

namespace RebirthProtocol.Battle
{
    // Bomb slot (GAME_DESIGN §2.1): slower AoE secondary on a cooldown,
    // aimed with a hold-to-aim / release-to-throw reticule. Port of Bomb.ts
    // (Stage-4 slice: no manual reticule steering or cluster boons yet).
    // One instance per robo with a bomb left arm; ticked by DuelManager.
    public sealed class BombSystem : MonoBehaviour
    {
        private sealed class LiveBomb
        {
            public Transform Tf;
            public Vector3 Start;
            public Vector3 End;
            public float T; // 0..1 along the arc
            public float FlightTime;
            public float ArcHeight;

            // Volley capability (ARMORY §6, Palisade/Pincer Charge): both
            // captured at RELEASE, never read at detonation. Release()
            // un-roots the thrower immediately and flight is >=0.5s, so by
            // impact the thrower may have strafed, dashed, taken knockback,
            // or died — none of that may retroactively rotate or re-stance
            // a bomb already in the air (Codex PR #18 finding: the original
            // code re-derived the throw direction from the owner's CURRENT
            // position at detonation, silently rotating Line/Split patterns
            // mid-flight). GroundedAtRelease matches the G/A-differs idiom
            // used across the gun roster, which also reads at fire time.
            public bool GroundedAtRelease;
            public Vector3 ForwardAtRelease;

            // Trajectory suite (ARMORY §6, Pass I2). Path is evaluated as a
            // pure function of T in PathPosition -- never integrated step by
            // step -- so the route is identical at any frame rate and the
            // landing always lands exactly on End.
            public BombPath Path;

            // Bend: the full lateral swing vector at the bow's widest,
            // resolved to a world direction AT RELEASE from the throw line
            // and the part's Dexter/Sinister side. Captured with
            // ForwardAtRelease and for the same reason: the bow's side is
            // decided when the bomb leaves the hand, and nothing the thrower
            // does afterward may re-aim a bomb already in the air.
            public Vector3 BendOffset;

            // Contact detonation: blow on the first enemy robo swept within
            // this radius mid-flight. 0 = fly through everything to the mark.
            // The check runs against the arc actually traced between two
            // frames (TrySweepContact resamples it), never a single point.
            public float ContactRadius;

            // Oubliette Twin: the sibling pits of ONE throw share this, so
            // Cluster Shell's once-per-throw contract survives a throw that
            // plants several independently-detonating bombs (Codex PR #25
            // finding 4). Never null.
            public ThrowState Throw;

            // Dwell: a landed mine parks and waits instead of detonating.
            // DwellSeconds 0 means "detonate at landing" (every bomb before
            // this pass); otherwise Dwelling flips true on landing and
            // DwellTimer counts the remaining patience down.
            public float DwellSeconds;
            public float DwellTriggerRadius;
            public bool Dwelling;
            public float DwellTimer;
        }

        public float CooldownRemaining { get; private set; }

        /// True while the reticule is open (button held): rooted and
        /// vulnerable while aiming.
        public bool Aiming { get; private set; }

        private sealed class PendingCluster
        {
            public Vector3 At;
            public float Timer;
        }

        /// State shared by every bomb from a SINGLE throw. Cluster Shell's
        /// contract is once per throw, not once per blast -- which used to
        /// be trivially true because a throw was one bomb. Oubliette Twin
        /// breaks that assumption (two pits, detonating seconds apart), so
        /// the allowance has to live on the throw rather than the bomb.
        private sealed class ThrowState
        {
            public bool ClustersSpent;
        }

        private RoboAvatar _owner;
        private Transform _reticule;
        private Vector3 _manualOffset;
        private readonly List<LiveBomb> _live = new List<LiveBomb>();
        private readonly List<PendingCluster> _pendingClusters = new List<PendingCluster>();

        // Where each robo stood at the end of last tick, so a dwelling mine
        // can test the span a robo CROSSED rather than only where it ended
        // up -- a dash covers more ground in one coarse step than a trigger
        // radius is wide (Codex PR #25 finding 5).
        private Vector3 _prevPlayerPos;
        private Vector3 _prevEnemyPos;
        private bool _haveRoboPrev;

        public bool Ready => CooldownRemaining <= 0f;

        /// Trajectory introspection (Pass I2 PlayMode tests): a bomb's SHAPE
        /// is the thing this pass builds, and shape can only be asserted by
        /// sampling the route in flight -- an outcome check ("the target took
        /// damage") is satisfied by a straight lob too.
        public int LiveBombCount => _live.Count;

        public Vector3 LiveBombPosition(int index) => _live[index].Tf.position;

        public bool LiveBombDwelling(int index) => _live[index].Dwelling;

        /// Cluster Shell follow-ups currently queued. Exposed so a test can
        /// assert the once-per-THROW allowance directly, rather than trying
        /// to infer it from the scatter's random damage (Codex PR #25
        /// finding 4).
        public int PendingClusterCount => _pendingClusters.Count;

        /// Rearm Protocol boon: a knockdown wipes the remaining cooldown.
        public void ResetCooldown()
        {
            CooldownRemaining = 0f;
        }

        public void Init(RoboAvatar owner)
        {
            _owner = owner;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            // Immediate: visual-only primitives must never spend a frame
            // (or a batched harness frame's worth of sim steps) blocking
            // ProjectileSystem raycasts.
            DestroyImmediate(ring.GetComponent<Collider>());
            ring.name = "Bomb Reticule";
            ring.transform.SetParent(transform, false);
            ring.transform.localScale = new Vector3(1.4f, 0.02f, 1.4f);
            ring.GetComponent<Renderer>().material = BattleMaterials.Unlit(new Color(1f, 0.75f, 0.2f));
            ring.SetActive(false);
            _reticule = ring.transform;
        }

        /// Opens the reticule. Call once when the bomb input is first pressed.
        public bool StartAim(RoboAvatar target)
        {
            if (!Ready || _owner.ControlLocked || !_owner.Loadout.HasBomb)
            {
                return false;
            }

            Aiming = true;
            _manualOffset = Vector3.zero;
            UpdateAim(target);
            _reticule.gameObject.SetActive(true);
            return true;
        }

        /// Call each frame the bomb input is held with stick deflection, to
        /// nudge the reticule away from its default aim point.
        public void SteerAim(Vector3 dir, float dt)
        {
            if (!Aiming || dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            _manualOffset += dir * (6f * dt); // aimSteer.bombOffsetSpeed
        }

        /// Call every frame the bomb input is held: the reticule tracks its
        /// default aim point (the enemy, or a fixed point ahead of self).
        public void UpdateAim(RoboAvatar target)
        {
            if (!Aiming)
            {
                return;
            }

            var part = _owner.Loadout.Bomb;
            var start = _owner.Position;
            Vector3 point;
            var targetAlive = target != null && target.Health.State != HealthState.Dead;

            if (part.ReticuleAnchor == ReticuleAnchor.Target && targetAlive)
            {
                var toTarget = target.Position - start;
                toTarget.y = 0f;
                point = toTarget.sqrMagnitude > 0.0001f
                    ? start + toTarget.normalized * Mathf.Min(toTarget.magnitude, part.ReticuleRange)
                    : start;
            }
            else
            {
                // Self-anchored: fixed point straight ahead at ReticuleRange.
                point = start + _owner.FacingDir * part.ReticuleRange;
            }

            // Manual offset on top, then clamp the TOTAL distance from the
            // robo to ReticuleRange — steering can't out-range the weapon.
            point += _manualOffset;
            var fromSelf = point - start;
            fromSelf.y = 0f;
            if (fromSelf.magnitude > part.ReticuleRange)
            {
                fromSelf = fromSelf.normalized * part.ReticuleRange;
                point = start + fromSelf;
            }

            point.y = 0.1f;
            _reticule.position = point;
        }

        /// Deploys at the current reticule position and closes it. Call when
        /// the bomb input is released. No-ops if not currently aiming.
        public bool Release()
        {
            if (!Aiming)
            {
                return false;
            }

            // Codex PR #21 finding: Tick()'s own ControlLocked-driven
            // CancelAim() runs too late to catch this. DuelManager.Update
            // ticks brains BEFORE BombSystem.Tick each frame, so a fetter
            // that lands mid-frame (e.g. from _projectiles.Tick, which also
            // runs after brains) isn't reflected until the FOLLOWING
            // frame's Tick call -- but a brain calling Release() at the top
            // of that following frame runs before Tick gets there, slipping
            // a throw out under a fetter/knockdown that's already active.
            // Same gate StartAim already uses, checked at the point of
            // action instead of relying solely on the next Tick.
            if (_owner.ControlLocked)
            {
                CancelAim();
                return false;
            }

            Aiming = false;
            _reticule.gameObject.SetActive(false);
            GameAudio.Sfx?.BombThrow(_owner.Position);
            var part = _owner.Loadout.Bomb;
            CooldownRemaining = part.Cooldown;

            var start = _owner.Position + Vector3.up * 0.8f;
            var end = _reticule.position;

            // Volley capability: the flattened throw direction, captured
            // NOW -- see LiveBomb.ForwardAtRelease. Same near-zero fallback
            // BlastPoints used to fall back on (a Target-anchored throw at
            // essentially zero range, e.g. Pincer Charge with the enemy
            // standing on top of the thrower).
            var throwDirAtRelease = end - start;
            throwDirAtRelease.y = 0f;
            var forwardAtRelease = throwDirAtRelease.sqrMagnitude > 0.0001f
                ? throwDirAtRelease.normalized
                : _owner.FacingDir;

            // The thrower's right at release (Dexter); Sinister is its
            // negation. Also the axis Oubliette Twin's pits are spread
            // along, so the two pits straddle the throw line rather than
            // stacking one behind the other.
            var rightAtRelease = new Vector3(forwardAtRelease.z, 0f, -forwardAtRelease.x);
            var bendOffset = part.Path == BombPath.Bend
                ? rightAtRelease * (part.BendSide == BombBendSide.Sinister ? -part.BendWidth : part.BendWidth)
                : Vector3.zero;

            // Oubliette Twin: one throw, several independent pits abreast.
            // Every other bomb is MineCount 1 and takes the single-throw
            // path unchanged.
            var count = Mathf.Max(1, part.MineCount);
            var halfSpread = (count - 1) * 0.5f;
            var throwState = new ThrowState(); // shared by every pit of THIS throw
            for (var i = 0; i < count; i++)
            {
                var mineEnd = count > 1
                    ? end + rightAtRelease * ((i - halfSpread) * part.MineSpacing)
                    : end;
                SpawnBomb(part, start, mineEnd, forwardAtRelease, bendOffset, throwState);
            }

            return true;
        }

        private void SpawnBomb(BombPart part, Vector3 start, Vector3 end, Vector3 forwardAtRelease,
            Vector3 bendOffset, ThrowState throwState)
        {
            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(shell.GetComponent<Collider>());
            shell.name = "Bomb";
            shell.transform.SetParent(transform, false);
            shell.transform.position = start;
            shell.transform.localScale = Vector3.one * 0.55f;
            shell.GetComponent<Renderer>().material = BattleMaterials.Unlit(new Color(1f, 0.4f, 0.15f));

            var dist = Vector3.Distance(start, end);
            _live.Add(new LiveBomb
            {
                Tf = shell.transform,
                Start = start,
                End = end,
                T = 0f,
                FlightTime = Mathf.Max(CombatTuning.Bomb.MinFlightTime, dist / CombatTuning.Bomb.LobSpeed)
                    * Mathf.Max(0.01f, part.FlightTimeMult),
                ArcHeight = part.ArcHeight,
                GroundedAtRelease = _owner.Grounded,
                ForwardAtRelease = forwardAtRelease,
                Path = part.Path,
                BendOffset = bendOffset,
                ContactRadius = part.ContactRadius,
                Throw = throwState,
                DwellSeconds = part.DwellSeconds,
                DwellTriggerRadius = part.DwellTriggerRadius
            });
        }

        /// Closes the reticule without throwing (knockdown/death mid-aim).
        public void CancelAim()
        {
            Aiming = false;
            _reticule.gameObject.SetActive(false);
        }

        public void Tick(float dt, RoboAvatar player, RoboAvatar enemy)
        {
            CooldownRemaining -= dt;
            if (Aiming && (_owner.Health.State is HealthState.KnockedDown or HealthState.Dead || _owner.Fetter.IsFettered))
            {
                CancelAim();
            }

            // Cluster Shell follow-ups land a beat after the main blast.
            for (var i = _pendingClusters.Count - 1; i >= 0; i--)
            {
                _pendingClusters[i].Timer -= dt;
                if (_pendingClusters[i].Timer <= 0f)
                {
                    Detonate(_pendingClusters[i].At, player, enemy, isCluster: true);
                    _pendingClusters.RemoveAt(i);
                }
            }

            for (var i = _live.Count - 1; i >= 0; i--)
            {
                var b = _live[i];

                // Dwelling mines (Pass I2) are done flying: they sit on the
                // mark and wait, blowing on the first robo that strays
                // within reach or when their patience runs out. A mine never
                // just expires -- "it remembers."
                if (b.Dwelling)
                {
                    b.DwellTimer -= dt;
                    if (b.DwellTimer <= 0f || Disturbed(b, player, enemy))
                    {
                        DetonateAndClear(b, b.End, player, enemy, i);
                    }

                    continue;
                }

                // The interval this frame covers, CLAMPED at the landing:
                // the final slice of a flight is swept like every other one
                // (Codex PR #25 finding 1 -- the original code branched to
                // landing before moving or checking contact, leaving the last
                // stretch before the mark unswept, so at a coarse dt a foe
                // standing in it was passed through untouched AND was outside
                // the endpoint blast).
                var fromT = b.T;
                b.T += dt / b.FlightTime;
                var landing = b.T >= 1f;
                if (landing)
                {
                    b.T = 1f;
                }

                b.Tf.position = PathPosition(b, b.T);

                // Contact detonation (Oxbow Charge): a bomb whose ROUTE is
                // the weapon blows on the enemy it sweeps past, rather than
                // sailing over them to the mark behind. Owner-exempt,
                // intangible-exempt and geometry-exempt by design -- see
                // BombPart.ContactRadius.
                if (b.ContactRadius > 0f && TrySweepContact(b, fromT, b.T, player, enemy, out var contactAt))
                {
                    DetonateAndClear(b, contactAt, player, enemy, i);
                    continue;
                }

                if (landing)
                {
                    if (b.DwellSeconds > 0f)
                    {
                        BeginDwell(b);
                        continue;
                    }

                    DetonateAndClear(b, b.End, player, enemy, i);
                }
            }

            // Robo sweep bookkeeping for dwelling mines (Codex PR #25 finding
            // 5): captured AFTER the bombs are processed, so next frame's
            // Disturbed() sees exactly the span each robo covered.
            _prevPlayerPos = player != null ? player.Position : _prevPlayerPos;
            _prevEnemyPos = enemy != null ? enemy.Position : _prevEnemyPos;
            _haveRoboPrev = true;
        }

        /// Parks a landed mine: it stops moving, shrinks to a dark lump on
        /// the floor, and starts counting its patience down.
        private static void BeginDwell(LiveBomb b)
        {
            b.Dwelling = true;
            b.DwellTimer = b.DwellSeconds;
            var rest = b.End;
            rest.y = CombatTuning.Bomb.MineRestHeight;
            b.End = rest; // the blast center is the pit itself, at floor level
            b.Tf.position = rest;
            b.Tf.localScale = Vector3.one * CombatTuning.Bomb.MineDwellScale;
            b.Tf.GetComponent<Renderer>().material = BattleMaterials.Unlit(new Color(0.18f, 0.1f, 0.08f));
        }

        /// True once any live robo is close enough to a dwelling mine to set
        /// it off. Unlike contact detonation this reads the OWNER too: a
        /// planted mine is a hazard on the floor, and DOCTRINE is consistent
        /// that your own blast can knock you down.
        ///
        /// Swept, not sampled (Codex PR #25 finding 5): a dash covers 6-9 m
        /// in a coarse step -- more than the whole trigger diameter -- so a
        /// point check on the post-motor position alone let a robo cross
        /// clean over a mine without arming it.
        private bool Disturbed(LiveBomb b, RoboAvatar player, RoboAvatar enemy)
        {
            foreach (var robo in new[] { player, enemy })
            {
                if (robo == null || robo.Health.State == HealthState.Dead)
                {
                    continue;
                }

                var from = _haveRoboPrev
                    ? (robo == player ? _prevPlayerPos : _prevEnemyPos)
                    : robo.Position;
                var closest = ClosestPointOnSegment(b.End, from, robo.Position);
                if (Vector3.Distance(closest, b.End) <= b.DwellTriggerRadius)
                {
                    return true;
                }
            }

            return false;
        }

        /// True when the arc a flying bomb actually traced over [fromT, toT]
        /// passes within its contact radius of an ENEMY robo. Reports the
        /// point of closest approach, so the blast centers where the bomb met
        /// the foe rather than wherever the frame boundary happened to fall.
        ///
        /// SUBSTEPPED rather than one chord (Codex PR #25 finding 2): Bend is
        /// a strongly curved path, and a single chord across a coarse frame
        /// both cuts the corner (false-triggering on a foe standing inside
        /// the bow who the bomb visibly curved around) and misses foes on the
        /// outside of the curve. The step count is driven by how far the bomb
        /// can actually have travelled -- the chord plus the most the bow and
        /// the arc can add over that slice, since both are sine-weighted and
        /// so bounded by amplitude*pi*dT. At 60 fps this is one step, exactly
        /// as before; it only subdivides when a frame is coarse enough to
        /// need it.
        private bool TrySweepContact(LiveBomb b, float fromT, float toT, RoboAvatar player, RoboAvatar enemy,
            out Vector3 contactAt)
        {
            var from = PathPosition(b, fromT);
            var to = PathPosition(b, toT);
            contactAt = to;

            var slack = (b.BendOffset.magnitude + b.ArcHeight) * Mathf.PI * (toT - fromT);
            var travel = Vector3.Distance(from, to) + slack;
            var steps = Mathf.Clamp(
                Mathf.CeilToInt(travel / CombatTuning.Bomb.ContactSweepStep), 1,
                CombatTuning.Bomb.MaxContactSweepSteps);

            var segmentStart = from;
            for (var s = 1; s <= steps; s++)
            {
                var segmentEnd = s == steps ? to : PathPosition(b, Mathf.Lerp(fromT, toT, s / (float)steps));
                foreach (var robo in new[] { player, enemy })
                {
                    // Intangible robos are passed straight through, matching
                    // ProjectileSystem's raycast filter -- a vanish dash or a
                    // charge's i-frames let attacks through, and a bomb that
                    // detonated on them would be consumed for an Evaded hit
                    // (Codex PR #25 finding 3).
                    if (robo == null || robo == _owner || robo.Intangible
                        || robo.Health.State == HealthState.Dead)
                    {
                        continue;
                    }

                    var closest = ClosestPointOnSegment(robo.Center, segmentStart, segmentEnd);
                    if (Vector3.Distance(robo.Center, closest) <= b.ContactRadius)
                    {
                        contactAt = closest;
                        return true;
                    }
                }

                segmentStart = segmentEnd;
            }

            return false;
        }

        private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            var lengthSq = ab.sqrMagnitude;
            if (lengthSq < 0.000001f)
            {
                return a;
            }

            return a + ab * Mathf.Clamp01(Vector3.Dot(point - a, ab) / lengthSq);
        }

        private void DetonateAndClear(LiveBomb b, Vector3 at, RoboAvatar player, RoboAvatar enemy, int index)
        {
            Detonate(at, player, enemy, groundedAtRelease: b.GroundedAtRelease,
                forwardAtRelease: b.ForwardAtRelease, throwState: b.Throw);
            Destroy(b.Tf.gameObject);
            _live.RemoveAt(index);
        }

        /// The bomb's world position at flight parameter `t` -- a PURE
        /// FUNCTION of t, deliberately: every path is evaluated from the
        /// release state rather than integrated frame by frame, so a coarse
        /// or uneven frame can never drift the route or the landing (the
        /// class of bug that put Pass I1's Vault arc below its mark). Taking
        /// t as an argument is also what lets the contact sweep resample the
        /// real curve between two frames instead of approximating it.
        private static Vector3 PathPosition(LiveBomb b, float t)
        {
            switch (b.Path)
            {
                case BombPath.Steeple:
                {
                    // All the ground travel is spent climbing; past the apex
                    // it is a straight vertical drop onto the mark. Both
                    // halves use a quadratic so the rise decelerates and the
                    // fall accelerates, the way a thrown weight actually
                    // behaves -- a symmetric sine bump reads as a float.
                    var climb = CombatTuning.Bomb.SteepleClimbFraction;
                    var ground = Mathf.Min(t / climb, 1f);
                    var pos = Vector3.Lerp(b.Start, b.End, ground);
                    if (t <= climb)
                    {
                        var v = t / climb;
                        pos.y += (1f - (1f - v) * (1f - v)) * b.ArcHeight;
                    }
                    else
                    {
                        var u = (t - climb) / (1f - climb);
                        pos.y += (1f - u * u) * b.ArcHeight;
                    }

                    return pos;
                }

                case BombPath.Bend:
                {
                    // A bow off the straight throw line: zero swing at the
                    // hand and at the mark, widest at the midpoint, so the
                    // bomb goes around by the named side and still lands
                    // exactly where the reticule promised.
                    var swing = Mathf.Sin(t * Mathf.PI);
                    var pos = Vector3.Lerp(b.Start, b.End, t) + b.BendOffset * swing;
                    pos.y += swing * b.ArcHeight;
                    return pos;
                }

                default:
                {
                    // Lob: the original parabola -- lerp + symmetric sine bump.
                    var pos = Vector3.Lerp(b.Start, b.End, t);
                    pos.y += Mathf.Sin(t * Mathf.PI) * b.ArcHeight;
                    return pos;
                }
            }
        }

        private void Detonate(Vector3 at, RoboAvatar player, RoboAvatar enemy, bool isCluster = false,
            bool groundedAtRelease = true, Vector3 forwardAtRelease = default, ThrowState throwState = null)
        {
            var part = _owner.Loadout.Bomb;
            var effects = _owner.Effects;
            var scale = isCluster ? 0.6f : 1f; // mini-blasts are weaker and smaller

            // Volley capability (ARMORY §6, Pass E): Palisade/Pincer Charge
            // detonate at MULTIPLE points instead of one. A cluster mini-
            // blast is always single-point regardless of the parent bomb's
            // own pattern -- letting a multi-point bomb's own Cluster Shell
            // procs multiply combinatorially would turn one throw into a
            // small fireworks show, not a mini-blast.
            var points = isCluster ? SinglePoint(at) : BlastPoints(at, part, groundedAtRelease, forwardAtRelease);
            foreach (var point in points)
            {
                DetonateAt(point, part, effects, scale, player, enemy);
            }

            // Cluster Shell boon: follow-up mini-blasts scatter around the
            // main detonation (never off a mini-blast itself, and once per
            // THROW even for a multi-point bomb, not once per point).
            // Scatter is drawn from the run's seeded RNG (not
            // UnityEngine.Random) so RunSeedOverride pins it too.
            //
            // The allowance is consumed on the FIRST detonation of the throw
            // and lives on the shared ThrowState, so Oubliette Twin's two
            // independently-detonating pits still yield one set of follow-ups
            // between them rather than one set each (Codex PR #25 finding 4:
            // the once-per-throw promise above used to be enforced only by
            // the accident that a throw was always a single bomb).
            if (!isCluster && effects != null && throwState is { ClustersSpent: false })
            {
                throwState.ClustersSpent = true;
                for (var i = 0; i < effects.ClusterBlasts; i++)
                {
                    _pendingClusters.Add(new PendingCluster
                    {
                        At = at + new Vector3(effects.NextFloat(-2.5f, 2.5f), 0f, effects.NextFloat(-2.5f, 2.5f)),
                        Timer = 0.3f
                    });
                }
            }
        }

        private void DetonateAt(Vector3 at, BombPart part, RunEffects effects, float scale,
            RoboAvatar player, RoboAvatar enemy)
        {
            GameEffects.Fx?.Explosion(at, part.BlastRadius * scale);
            GameAudio.Sfx?.Explosion(at);

            // AoE hits BOTH robos -- your own bomb can knock you down.
            foreach (var robo in new[] { player, enemy })
            {
                var toRobo = robo.Position - at;
                if (toRobo.magnitude <= part.BlastRadius * scale + 0.5f)
                {
                    toRobo.y = 0f;
                    var result = robo.ReceiveHit(
                        (part.Damage * _owner.Stats.AtkMult + (effects?.FlatDamageBonus() ?? 0f)) * scale,
                        part.EnduranceDamage * scale,
                        toRobo.sqrMagnitude > 0.0001f ? toRobo.normalized : Vector3.forward,
                        isBlast: true); // AoE: the Quiet Bell's muffle reads this
                    if (result is not ReceiveResult.Invulnerable and not ReceiveResult.Evaded)
                    {
                        // Fetter capability (Rime Charge, Pass F): flat
                        // duration, not scaled by the cluster-mini-blast
                        // damage scale -- a mini-blast either fetters for the
                        // full duration or (Damage=0-ish sources aside) not
                        // at all, never a partial hold.
                        robo.ApplyFetter(part.FetterSeconds);
                    }

                    if (effects != null && robo != _owner
                        && result is not ReceiveResult.Invulnerable and not ReceiveResult.Evaded)
                    {
                        effects.OnHit(HitSource.Bomb);
                        if (result is ReceiveResult.Knockdown or ReceiveResult.GuardBreak)
                        {
                            effects.OnKnockdown();
                        }
                    }
                }
            }

            // Crates inside the blast are destroyed outright.
            var overlaps = Physics.OverlapSphere(at, part.BlastRadius * scale);
            foreach (var overlap in overlaps)
            {
                var crate = overlap.GetComponent<CrateHealth>();
                if (crate != null)
                {
                    crate.DestroyOutright();
                }
            }
        }

        /// Blast centers relative to the impact point `at`, oriented along
        /// `forward` -- the flattened throw direction CAPTURED AT RELEASE
        /// (LiveBomb.ForwardAtRelease), never re-derived from the owner's
        /// position at detonation time. Release() un-roots the thrower
        /// immediately and flight is >=0.5s, so reading the owner's CURRENT
        /// position here would let strafing, dashing, knockback, or death
        /// mid-flight silently rotate a Line/Split pattern after the throw
        /// already committed (Codex PR #18 finding). Single-pattern bombs
        /// (every bomb before this pass) are the original one-point
        /// behavior and never look at `forward` at all.
        private List<Vector3> BlastPoints(Vector3 at, BombPart part, bool groundedAtRelease, Vector3 forward)
        {
            if (part.Pattern == BlastPattern.Single || part.BlastPoints <= 1)
            {
                return SinglePoint(at);
            }

            var points = new List<Vector3>();
            switch (part.Pattern)
            {
                case BlastPattern.Line:
                    // A row straddling the impact point along the throw
                    // line -- "a stake-wall of blasts before you" (Palisade).
                    var half = (part.BlastPoints - 1) * 0.5f;
                    for (var i = 0; i < part.BlastPoints; i++)
                    {
                        points.Add(at + forward * ((i - half) * part.BlastSpacing));
                    }

                    break;

                case BlastPattern.Split:
                    // Two points offset from the impact: lateral ("sides")
                    // if the thrower was grounded at release, fore-and-aft
                    // if airborne (Pincer Charge, ARMORY §6).
                    var axis = groundedAtRelease
                        ? new Vector3(-forward.z, 0f, forward.x) // perpendicular: sides
                        : forward;                               // along the throw: fore/aft
                    points.Add(at + axis * part.BlastSpacing);
                    points.Add(at - axis * part.BlastSpacing);
                    break;

                default:
                    points.Add(at);
                    break;
            }

            return points;
        }

        private static List<Vector3> SinglePoint(Vector3 at) => new List<Vector3> { at };
    }
}
