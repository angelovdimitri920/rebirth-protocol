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

        private RoboAvatar _owner;
        private Transform _reticule;
        private Vector3 _manualOffset;
        private readonly List<LiveBomb> _live = new List<LiveBomb>();
        private readonly List<PendingCluster> _pendingClusters = new List<PendingCluster>();

        public bool Ready => CooldownRemaining <= 0f;

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

            Aiming = false;
            _reticule.gameObject.SetActive(false);
            GameAudio.Sfx?.BombThrow(_owner.Position);
            var part = _owner.Loadout.Bomb;
            CooldownRemaining = part.Cooldown;

            var start = _owner.Position + Vector3.up * 0.8f;
            var end = _reticule.position;
            var dist = Vector3.Distance(start, end);

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

            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(shell.GetComponent<Collider>());
            shell.name = "Bomb";
            shell.transform.SetParent(transform, false);
            shell.transform.position = start;
            shell.transform.localScale = Vector3.one * 0.55f;
            shell.GetComponent<Renderer>().material = BattleMaterials.Unlit(new Color(1f, 0.4f, 0.15f));

            _live.Add(new LiveBomb
            {
                Tf = shell.transform,
                Start = start,
                End = end,
                T = 0f,
                FlightTime = Mathf.Max(CombatTuning.Bomb.MinFlightTime, dist / CombatTuning.Bomb.LobSpeed),
                ArcHeight = part.ArcHeight,
                GroundedAtRelease = _owner.Grounded,
                ForwardAtRelease = forwardAtRelease
            });
            return true;
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
                b.T += dt / b.FlightTime;
                if (b.T >= 1f)
                {
                    Detonate(b.End, player, enemy, groundedAtRelease: b.GroundedAtRelease, forwardAtRelease: b.ForwardAtRelease);
                    Destroy(b.Tf.gameObject);
                    _live.RemoveAt(i);
                    continue;
                }

                // Parabolic arc: lerp + sine bump.
                var pos = Vector3.Lerp(b.Start, b.End, b.T);
                pos.y += Mathf.Sin(b.T * Mathf.PI) * b.ArcHeight;
                b.Tf.position = pos;
            }
        }

        private void Detonate(Vector3 at, RoboAvatar player, RoboAvatar enemy, bool isCluster = false,
            bool groundedAtRelease = true, Vector3 forwardAtRelease = default)
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
            if (!isCluster && effects != null)
            {
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
