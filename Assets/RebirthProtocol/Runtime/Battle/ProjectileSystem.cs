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
        }

        private readonly List<Projectile> _active = new List<Projectile>();
        private readonly RaycastHit[] _hits = new RaycastHit[8];

        public void Spawn(RoboAvatar owner, RoboAvatar target, Vector3 muzzle, Vector3 aimPoint,
            float damage, float enduranceDamage, float speed, float homingTurnRate)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
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
                HomingTurnRate = homingTurnRate
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
                    struckAvatar.ReceiveHit(p.Damage, p.EnduranceDamage, p.Velocity.normalized);
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
                        p.Target.ReceiveHit(p.Damage, p.EnduranceDamage, p.Velocity.normalized);
                        Despawn(i);
                    }
                }
            }
        }

        private static bool TargetAlive(Projectile p)
        {
            return p.Target != null && p.Target.Health.State != HealthState.Dead;
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
