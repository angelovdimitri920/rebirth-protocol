using System.Collections.Generic;
using RebirthProtocol.Domain;
using UnityEngine;

namespace RebirthProtocol.Battle
{
    // Homing gun projectiles: kinematic spheres stepped by DuelManager.
    // Obstacles block shots (raycast per step); hits land on proximity to
    // the target's capsule center.
    public sealed class ProjectileSystem : MonoBehaviour
    {
        private sealed class Projectile
        {
            public Transform Tf;
            public Vector3 Velocity;
            public float Life;
            public RoboAvatar Owner;
            public RoboAvatar Target;
        }

        private readonly List<Projectile> _active = new List<Projectile>();
        private readonly RaycastHit[] _hits = new RaycastHit[8];

        public void Spawn(RoboAvatar owner, RoboAvatar target, Vector3 muzzle, Vector3 aimPoint)
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
                Velocity = (aimPoint - muzzle).normalized * CombatTuning.Gun.ProjectileSpeed,
                Life = CombatTuning.Gun.ProjectileLifetime,
                Owner = owner,
                Target = target
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
                if (p.Target != null && p.Target.Health.State != HealthState.Dead)
                {
                    var toTarget = (p.Target.Center - p.Tf.position).normalized;
                    var maxRadians = CombatTuning.Gun.HomingTurnRate * dt;
                    p.Velocity = Vector3.RotateTowards(p.Velocity, toTarget * p.Velocity.magnitude, maxRadians, 0f);
                }

                var step = p.Velocity * dt;
                var stepLen = step.magnitude;
                var from = p.Tf.position;

                // Obstacle / robo intercept along this step. RaycastNonAlloc
                // results are unsorted, so take the nearest non-owner hit.
                var count = Physics.RaycastNonAlloc(from, step / stepLen, _hits, stepLen);
                var blocked = false;
                var nearest = float.MaxValue;
                RoboAvatar struckAvatar = null;
                for (var h = 0; h < count; h++)
                {
                    var hitAvatar = _hits[h].collider.GetComponent<RoboAvatar>();
                    if (hitAvatar == p.Owner || _hits[h].distance >= nearest)
                    {
                        continue; // never hit your own shot
                    }

                    nearest = _hits[h].distance;
                    struckAvatar = hitAvatar;
                    blocked = true;
                }

                if (struckAvatar != null)
                {
                    struckAvatar.ReceiveHit(CombatTuning.Gun.Damage, CombatTuning.Gun.EnduranceDamage, p.Velocity.normalized);
                }

                if (blocked)
                {
                    Despawn(i);
                    continue;
                }

                p.Tf.position = from + step;

                // Proximity hit: homing shots that drift past the raycast line.
                if (p.Target != null && p.Target.Health.State != HealthState.Dead)
                {
                    var toCenter = p.Target.Center - p.Tf.position;
                    if (toCenter.sqrMagnitude < 0.7f * 0.7f)
                    {
                        p.Target.ReceiveHit(CombatTuning.Gun.Damage, CombatTuning.Gun.EnduranceDamage, p.Velocity.normalized);
                        Despawn(i);
                    }
                }
            }
        }

        private void Despawn(int index)
        {
            Destroy(_active[index].Tf.gameObject);
            _active.RemoveAt(index);
        }
    }
}
