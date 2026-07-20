using System.Collections.Generic;
using RebirthProtocol.Battle.Effects;
using RebirthProtocol.Domain;
using UnityEngine;

namespace RebirthProtocol.Battle
{
    // Homing projectiles (guns and pods): kinematic spheres stepped by
    // DuelManager. Obstacles block shots (raycast per step); hits land on
    // raycast intercept or proximity to the target's capsule center.
    public sealed class ProjectileSystem : MonoBehaviour
    {
        private sealed class Projectile
        {
            public Transform Tf;
            public Vector3 Velocity;
            public float Life;
            public RoboAvatar Owner;
            public RoboAvatar Target;
            public float Damage;
            public float EnduranceDamage;
            public float HomingTurnRate;
            public HitSource Source;
            public bool SurvivesKnockdown;
        }

        private readonly List<Projectile> _active = new List<Projectile>();
        private readonly RaycastHit[] _hits = new RaycastHit[8];

        public void Spawn(RoboAvatar owner, RoboAvatar target, Vector3 muzzle, Vector3 aimPoint,
            float damage, float enduranceDamage, float speed, float homingTurnRate,
            HitSource source = HitSource.None, bool survivesKnockdown = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            // Immediate, not deferred: a deferred Destroy leaves the sphere
            // collider live until end of frame, where this very system's
            // raycasts can strike it — one quirk frame in the real game, but
            // hundreds of sim steps in the balance harness's batched frames.
            DestroyImmediate(go.GetComponent<Collider>());
            go.name = "Projectile";
            go.transform.SetParent(transform, false);
            go.transform.position = muzzle;
            go.transform.localScale = Vector3.one * 0.25f;
            go.GetComponent<Renderer>().material = BattleMaterials.Unlit(
                owner.CompareTag("Player") ? new Color(0.4f, 0.8f, 1f) : new Color(1f, 0.45f, 0.3f));

            _active.Add(new Projectile
            {
                Tf = go.transform,
                Velocity = (aimPoint - muzzle).normalized * speed,
                Life = CombatTuning.Gun.ProjectileLifetime,
                Owner = owner,
                Target = target,
                Damage = damage,
                EnduranceDamage = enduranceDamage,
                HomingTurnRate = homingTurnRate,
                Source = source,
                SurvivesKnockdown = survivesKnockdown
            });
        }

        public void Tick(float dt)
        {
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var p = _active[i];
                p.Life -= dt;
                if (p.Life <= 0f)
                {
                    Despawn(i);
                    continue;
                }

                // Homing: curve toward the locked target's center.
                if (p.HomingTurnRate > 0f && TargetAlive(p))
                {
                    var toTarget = (p.Target.Center - p.Tf.position).normalized;
                    var maxRadians = p.HomingTurnRate * dt;
                    p.Velocity = Vector3.RotateTowards(p.Velocity, toTarget * p.Velocity.magnitude, maxRadians, 0f);
                }

                var step = p.Velocity * dt;
                var stepLen = step.magnitude;
                var from = p.Tf.position;

                // Obstacle / robo intercept along this step. RaycastNonAlloc
                // results are unsorted, so take the nearest non-owner hit.
                // Vanish-dashing robos are intangible: shots pass through.
                var count = Physics.RaycastNonAlloc(from, step / stepLen, _hits, stepLen);
                var blocked = false;
                var nearest = float.MaxValue;
                RoboAvatar struckAvatar = null;
                CrateHealth struckCrate = null;
                var hitPoint = Vector3.zero;
                var hitNormal = Vector3.up;
                for (var h = 0; h < count; h++)
                {
                    var hitAvatar = _hits[h].collider.GetComponent<RoboAvatar>();
                    if (hitAvatar == p.Owner || (hitAvatar != null && hitAvatar.Intangible) || _hits[h].distance >= nearest)
                    {
                        continue;
                    }

                    nearest = _hits[h].distance;
                    struckAvatar = hitAvatar;
                    struckCrate = hitAvatar == null ? _hits[h].collider.GetComponent<CrateHealth>() : null;
                    hitPoint = _hits[h].point;
                    hitNormal = _hits[h].normal;
                    blocked = true;
                }

                if (struckAvatar != null)
                {
                    ApplyAvatarHit(p, struckAvatar);
                }
                else if (struckCrate != null)
                {
                    struckCrate.Damage();
                    GameEffects.Fx?.ImpactSpark(hitPoint, hitNormal, new Color(1f, 0.75f, 0.4f));
                    Audio.GameAudio.Sfx?.SparkTick(hitPoint);
                }
                else if (blocked)
                {
                    // Spray off a wall / pillar / arena geometry.
                    GameEffects.Fx?.ImpactSpark(hitPoint, hitNormal, new Color(1f, 0.85f, 0.55f));
                    Audio.GameAudio.Sfx?.SparkTick(hitPoint);
                }

                if (blocked)
                {
                    Despawn(i);
                    continue;
                }

                p.Tf.position = from + step;

                // Proximity hit: homing shots that drift past the raycast line.
                if (TargetAlive(p) && !p.Target.Intangible)
                {
                    var toCenter = p.Target.Center - p.Tf.position;
                    if (toCenter.sqrMagnitude < 0.7f * 0.7f)
                    {
                        ApplyAvatarHit(p, p.Target);
                        Despawn(i);
                    }
                }
            }
        }

        private static bool TargetAlive(Projectile p)
        {
            return p.Target != null && p.Target.Health.State != HealthState.Dead;
        }

        /// Land the hit and fire the owner's run-effect trigger verbs
        /// (splinter darts, trigger-coil reloads, vampiric pod feeds).
        private void ApplyAvatarHit(Projectile p, RoboAvatar victim)
        {
            var result = victim.ReceiveHit(p.Damage, p.EnduranceDamage, p.Velocity.normalized);
            var effects = p.Owner != null ? p.Owner.Effects : null;
            if (effects == null || p.Source == HitSource.None
                || result is ReceiveResult.Invulnerable or ReceiveResult.Evaded)
            {
                return;
            }

            var outcome = effects.OnHit(p.Source);
            for (var d = 0; d < outcome.SplinterDarts; d++)
            {
                // Splinter Rounds: two weak darts spawn near the hit point
                // and curve back in. Source None so darts can't chain darts.
                // Jitter is drawn from the run's seeded RNG (not
                // UnityEngine.Random) so RunSeedOverride pins it too.
                var jitter = new Vector3(
                    effects.NextFloat(-1.5f, 1.5f),
                    1f + effects.NextFloat(0f, 1f),
                    effects.NextFloat(-1.5f, 1.5f));
                Spawn(p.Owner, victim, victim.Center + jitter, victim.Center,
                    RunEffects.SplinterDamage + effects.FlatDamageBonus(),
                    RunEffects.SplinterEnduranceDamage, 22f, 4f);
            }

            if (result is ReceiveResult.Knockdown or ReceiveResult.GuardBreak)
            {
                effects.OnKnockdown();
            }
        }

        /// The overload rule (COMBAT_DOCTRINE §4.3): a knockdown wipes the
        /// downed pilot's own gun rounds still in flight. Only gunfire —
        /// bomb and pod shots stay live — and rounds flagged
        /// SurvivesKnockdown (the scrapwright exemption) ride it out.
        /// This can fire reentrantly from inside Tick (a projectile hit
        /// causes the knockdown mid-loop), so wiped rounds are only marked
        /// dead and hidden here; the cull at the top of each Tick step
        /// removes them before they can move or land a hit.
        public void ClearGunRoundsOwnedBy(RoboAvatar owner)
        {
            foreach (var p in _active)
            {
                if (p.Owner != owner || p.Source != HitSource.Gun || p.SurvivesKnockdown || p.Life <= 0f)
                {
                    continue;
                }

                p.Life = 0f;
                p.Tf.gameObject.SetActive(false);
                GameEffects.Fx?.ImpactSpark(p.Tf.position, Vector3.up, new Color(0.7f, 0.85f, 1f));
            }
        }

        /// Rounds still live (not wiped) for this owner and source.
        public int CountLiveRounds(RoboAvatar owner, HitSource source)
        {
            var count = 0;
            foreach (var p in _active)
            {
                if (p.Owner == owner && p.Source == source && p.Life > 0f)
                {
                    count++;
                }
            }

            return count;
        }

        /// Remove every live shot — combatant respawns must not leave
        /// projectiles in flight holding stale owner/target references.
        public void Clear()
        {
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                Despawn(i);
            }
        }

        private void Despawn(int index)
        {
            Destroy(_active[index].Tf.gameObject);
            _active.RemoveAt(index);
        }
    }
}
