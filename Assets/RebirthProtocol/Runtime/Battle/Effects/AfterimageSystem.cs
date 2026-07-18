using System.Collections.Generic;
using RebirthProtocol.Domain;
using UnityEngine;

namespace RebirthProtocol.Battle.Effects
{
    // Afterimage boon (RunCatalog): dashing leaves a crackling copy that
    // detonates on enemy contact. Build-safety note (GAME_DESIGN §22):
    // the fade-out is a scale shrink on an OPAQUE unlit primitive, not a
    // transparency fade — the URP transparent-unlit variant strips from
    // player builds.
    public sealed class AfterimageSystem : MonoBehaviour
    {
        private const float Lifetime = 1.5f;
        private const float DetonateRange = 1.6f;

        private sealed class Afterimage
        {
            public Transform Tf;
            public float Timer;
            public bool Armed;
        }

        private readonly List<Afterimage> _active = new List<Afterimage>();

        public void Spawn(Vector3 at)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "Afterimage";
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            go.transform.localScale = Vector3.one * 1.2f;
            go.GetComponent<Renderer>().material = BattleMaterials.Unlit(new Color(0.2f, 0.88f, 1f));
            _active.Add(new Afterimage { Tf = go.transform, Timer = Lifetime, Armed = true });
        }

        public void Tick(float dt, RoboAvatar enemy, RunEffects effects)
        {
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var a = _active[i];
                a.Timer -= dt;
                a.Tf.localScale = Vector3.one * Mathf.Max(0.15f, a.Timer / Lifetime) * 1.2f;

                if (a.Armed && enemy != null && enemy.Health.State != HealthState.Dead
                    && Vector3.Distance(a.Tf.position, enemy.Position + Vector3.up) < DetonateRange)
                {
                    a.Armed = false;
                    a.Timer = Mathf.Min(a.Timer, 0.15f);
                    var dir = enemy.Position + Vector3.up - a.Tf.position;
                    enemy.ReceiveHit(
                        RunEffects.AfterimageDamage + effects.FlatDamageBonus(),
                        RunEffects.AfterimageEnduranceDamage,
                        dir.normalized);
                    GameEffects.Fx?.ImpactSpark(a.Tf.position, -dir.normalized, new Color(0.3f, 0.9f, 1f), 0.2f);
                }

                if (a.Timer <= 0f)
                {
                    Destroy(a.Tf.gameObject);
                    _active.RemoveAt(i);
                }
            }
        }

        /// Fight transitions must not carry live afterimages across.
        public void Clear()
        {
            foreach (var a in _active)
            {
                Destroy(a.Tf.gameObject);
            }

            _active.Clear();
        }
    }
}
