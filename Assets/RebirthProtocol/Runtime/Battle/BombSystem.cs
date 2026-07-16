using System.Collections.Generic;
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
        }

        private sealed class Blast
        {
            public Transform Tf;
            public float Timer;
        }

        public float CooldownRemaining { get; private set; }

        /// True while the reticule is open (button held): rooted and
        /// vulnerable while aiming.
        public bool Aiming { get; private set; }

        private RoboAvatar _owner;
        private Transform _reticule;
        private readonly List<LiveBomb> _live = new List<LiveBomb>();
        private readonly List<Blast> _blasts = new List<Blast>();

        public bool Ready => CooldownRemaining <= 0f;

        public void Init(RoboAvatar owner)
        {
            _owner = owner;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(ring.GetComponent<Collider>());
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
            UpdateAim(target);
            _reticule.gameObject.SetActive(true);
            return true;
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
            var part = _owner.Loadout.Bomb;
            CooldownRemaining = part.Cooldown;

            var start = _owner.Position + Vector3.up * 0.8f;
            var end = _reticule.position;
            var dist = Vector3.Distance(start, end);

            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(shell.GetComponent<Collider>());
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
                ArcHeight = part.ArcHeight
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
            if (Aiming && _owner.Health.State is HealthState.KnockedDown or HealthState.Dead)
            {
                CancelAim();
            }

            for (var i = _live.Count - 1; i >= 0; i--)
            {
                var b = _live[i];
                b.T += dt / b.FlightTime;
                if (b.T >= 1f)
                {
                    Detonate(b.End, player, enemy);
                    Destroy(b.Tf.gameObject);
                    _live.RemoveAt(i);
                    continue;
                }

                // Parabolic arc: lerp + sine bump.
                var pos = Vector3.Lerp(b.Start, b.End, b.T);
                pos.y += Mathf.Sin(b.T * Mathf.PI) * b.ArcHeight;
                b.Tf.position = pos;
            }

            // Blast visuals expand then vanish.
            for (var i = _blasts.Count - 1; i >= 0; i--)
            {
                var blast = _blasts[i];
                blast.Timer -= dt;
                blast.Tf.localScale *= 1f + 4f * dt;
                if (blast.Timer <= 0f)
                {
                    Destroy(blast.Tf.gameObject);
                    _blasts.RemoveAt(i);
                }
            }
        }

        private void Detonate(Vector3 at, RoboAvatar player, RoboAvatar enemy)
        {
            var part = _owner.Loadout.Bomb;

            var blast = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(blast.GetComponent<Collider>());
            blast.name = "Blast";
            blast.transform.SetParent(transform, false);
            blast.transform.position = at;
            blast.transform.localScale = Vector3.one * part.BlastRadius;
            blast.GetComponent<Renderer>().material = BattleMaterials.Unlit(new Color(1f, 0.67f, 0.27f));
            _blasts.Add(new Blast { Tf = blast.transform, Timer = 0.35f });

            // AoE hits BOTH robos -- your own bomb can knock you down.
            foreach (var robo in new[] { player, enemy })
            {
                var toRobo = robo.Position - at;
                if (toRobo.magnitude <= part.BlastRadius + 0.5f)
                {
                    toRobo.y = 0f;
                    robo.ReceiveHit(
                        part.Damage * _owner.Stats.AtkMult,
                        part.EnduranceDamage,
                        toRobo.sqrMagnitude > 0.0001f ? toRobo.normalized : Vector3.forward);
                }
            }
        }
    }
}
