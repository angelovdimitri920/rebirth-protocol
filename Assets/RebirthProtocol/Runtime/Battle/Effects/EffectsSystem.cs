using System.Collections.Generic;
using UnityEngine;

namespace RebirthProtocol.Battle.Effects
{
    // World-space combat VFX: muzzle flashes, projectile-impact sparks, and
    // layered explosions, plus camera-shake dispatch. Everything is built
    // from opaque scale-animated primitives (bright unlit) rather than
    // transparent/additive materials — the URP transparent-unlit variant is
    // prone to being stripped from player builds when no asset references it
    // (the same variant-stripping class of bug that once made Shader.Find
    // return null). Screen-space transparency (flash, grain, vignette) lives
    // in ScreenFx instead, on the always-present UI shader.
    public sealed class EffectsSystem : MonoBehaviour
    {
        private sealed class Particle
        {
            public Transform Tf;
            public Vector3 Velocity;
            public Vector3 AngularVelocity;
            public Vector3 ScaleShape; // per-axis multiplier (flat discs etc.)
            public float Gravity;
            public float Life;
            public float MaxLife;
            public float StartScale;
            public float EndScale;
        }

        private const int MaxLiveParticles = 400;

        private readonly List<Particle> _active = new List<Particle>();
        private DuelCameraRig _rig;
        private ScreenFx _screen;

        public void Init(DuelCameraRig rig, ScreenFx screen)
        {
            _rig = rig;
            _screen = screen;
        }

        private void OnDestroy()
        {
            // Clear the static so it becomes genuine C# null after teardown —
            // otherwise it points at a Unity-destroyed object and `?.` (which
            // only checks C# null) would let a stray VFX call through to a
            // dead MonoBehaviour. ReferenceEquals so a newer instance that
            // already took the slot isn't clobbered.
            if (ReferenceEquals(GameEffects.Fx, this))
            {
                GameEffects.Fx = null;
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var p = _active[i];
                p.Life -= dt;
                if (p.Life <= 0f || p.Tf == null)
                {
                    if (p.Tf != null)
                    {
                        Destroy(p.Tf.gameObject);
                    }

                    _active.RemoveAt(i);
                    continue;
                }

                p.Velocity += Vector3.up * (p.Gravity * dt);
                p.Tf.position += p.Velocity * dt;
                if (p.AngularVelocity != Vector3.zero)
                {
                    p.Tf.Rotate(p.AngularVelocity * dt, Space.Self);
                }

                var k = p.Life / p.MaxLife; // 1 -> 0 over the lifetime
                var scale = Mathf.Lerp(p.EndScale, p.StartScale, k);
                p.Tf.localScale = p.ScaleShape * scale;
            }
        }

        // --- Public VFX API ---

        public void MuzzleFlash(Vector3 pos, Vector3 dir, Color color)
        {
            Spawn(PrimitiveType.Sphere, pos, 0.55f, 0f, Bright(color), Vector3.zero, Vector3.zero, 0f, 0.06f);
            for (var i = 0; i < 3; i++)
            {
                var v = dir.normalized * Random.Range(6f, 11f) + Random.insideUnitSphere * 2f;
                Spawn(PrimitiveType.Cube, pos, 0.14f, 0f, Bright(color), v, RandomSpin(), -6f, 0.1f);
            }
        }

        public void ImpactSpark(Vector3 pos, Vector3 normal, Color color, float shake = 0f)
        {
            Spawn(PrimitiveType.Sphere, pos, 0.4f, 0f, Bright(color), Vector3.zero, Vector3.zero, 0f, 0.07f);
            var count = Random.Range(4, 7);
            for (var i = 0; i < count; i++)
            {
                var dir = (normal + Random.insideUnitSphere * 0.9f).normalized;
                var v = dir * Random.Range(4f, 9f);
                Spawn(PrimitiveType.Cube, pos, Random.Range(0.08f, 0.16f), 0f, Bright(color), v, RandomSpin(), -14f, Random.Range(0.18f, 0.32f));
            }

            if (shake > 0f)
            {
                _rig?.AddShake(shake);
            }
        }

        public void Explosion(Vector3 pos, float radius)
        {
            var ground = new Vector3(pos.x, 0.05f, pos.z);

            // Initial white-hot flash, then a fireball core that shrinks.
            Spawn(PrimitiveType.Sphere, pos, radius * 0.9f, 0f, new Color(1f, 0.95f, 0.7f), Vector3.zero, Vector3.zero, 0f, 0.12f);
            Spawn(PrimitiveType.Sphere, pos, radius * 0.7f, radius * 0.2f, new Color(1f, 0.5f, 0.12f), Vector3.zero, Vector3.zero, 0f, 0.28f);

            // Ground shockwave: a flat disc (kept flat via ScaleShape) that
            // expands outward across the floor, then vanishes.
            Spawn(PrimitiveType.Cylinder, ground, radius * 0.5f, radius * 2.4f, new Color(1f, 0.7f, 0.3f),
                Vector3.zero, Vector3.zero, 0f, 0.3f, new Vector3(1f, 0.02f, 1f));

            // Ember debris flung outward and up, tumbling under gravity.
            var debris = Random.Range(12, 18);
            for (var i = 0; i < debris; i++)
            {
                var dir = (Random.insideUnitSphere + Vector3.up * 0.6f).normalized;
                var v = dir * Random.Range(5f, 12f);
                var ember = Random.value < 0.5f ? new Color(1f, 0.45f, 0.1f) : new Color(0.25f, 0.22f, 0.2f);
                Spawn(PrimitiveType.Cube, pos, Random.Range(0.12f, 0.28f), 0f, ember, v, RandomSpin(), -18f, Random.Range(0.4f, 0.8f));
            }

            // Rising smoke puffs that grow as they climb (start small, end big).
            for (var i = 0; i < 5; i++)
            {
                var v = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(2f, 4f), Random.Range(-1.5f, 1.5f));
                Spawn(PrimitiveType.Sphere, pos + Random.insideUnitSphere * 0.4f, radius * 0.3f, radius * 0.6f, new Color(0.22f, 0.2f, 0.2f), v, Vector3.zero, 1.5f, Random.Range(0.5f, 0.9f));
            }

            _rig?.AddShake(Mathf.Clamp(radius * 0.22f, 0.25f, 0.7f));
            _screen?.Flash(new Color(1f, 0.55f, 0.2f), 0.5f);
        }

        public void Shake(float amount)
        {
            _rig?.AddShake(amount);
        }

        // --- Helpers ---

        private void Spawn(PrimitiveType type, Vector3 pos, float startScale, float endScale, Color color,
            Vector3 velocity, Vector3 angularVelocity, float gravity, float life, Vector3? scaleShape = null)
        {
            if (_active.Count >= MaxLiveParticles)
            {
                return;
            }

            var shape = scaleShape ?? Vector3.one;
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.name = "Vfx";
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.rotation = type == PrimitiveType.Cube ? Random.rotation : Quaternion.identity;
            go.transform.localScale = shape * startScale;
            go.GetComponent<Renderer>().material = BattleMaterials.Unlit(color);

            _active.Add(new Particle
            {
                Tf = go.transform,
                Velocity = velocity,
                AngularVelocity = angularVelocity,
                ScaleShape = shape,
                Gravity = gravity,
                Life = life,
                MaxLife = life,
                StartScale = startScale,
                EndScale = endScale
            });
        }

        private static Vector3 RandomSpin()
        {
            return new Vector3(Random.Range(-720f, 720f), Random.Range(-720f, 720f), Random.Range(-720f, 720f));
        }

        private static Color Bright(Color c)
        {
            return Color.Lerp(c, Color.white, 0.5f); // push toward white so sparks read as hot light
        }
    }

    /// Static access so combat code can fire VFX without threading a
    /// reference everywhere. Genuinely null-safe: EffectsSystem.OnDestroy
    /// clears this on teardown, so `?.` never dispatches to a destroyed
    /// object (Unity's fake-null wouldn't be caught by `?.` otherwise).
    public static class GameEffects
    {
        public static EffectsSystem Fx;
    }
}
