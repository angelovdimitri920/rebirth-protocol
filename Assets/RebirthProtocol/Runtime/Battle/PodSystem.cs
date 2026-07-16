using RebirthProtocol.Domain;
using UnityEngine;

namespace RebirthProtocol.Battle
{
    // Pod slot (GAME_DESIGN §2.1): deployable drone with its OWN energy
    // pool, independent of gun/bomb — always-on pressure rather than a
    // burst opportunity cost. Deploy drops it at your position; it hovers
    // and fires weak homing shots while its cell holds charge. Port of
    // Pod.ts (Stage-4 slice: auto-aim only, no manual launch steering yet).
    public sealed class PodSystem : MonoBehaviour
    {
        public bool Deployed { get; private set; }
        public float Energy { get; private set; }

        private RoboAvatar _owner;
        private ProjectileSystem _projectiles;
        private Transform _body;
        private float _fireCooldown;
        private float _bobTime;

        public void Init(RoboAvatar owner, ProjectileSystem projectiles, Color accent)
        {
            _owner = owner;
            _projectiles = projectiles;
            Energy = owner.Loadout.Pod.EnergyMax;

            var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(shell.GetComponent<Collider>());
            shell.name = "Pod";
            shell.transform.SetParent(transform, false);
            shell.transform.localScale = Vector3.one * 0.55f;
            shell.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            shell.GetComponent<Renderer>().material = BattleMaterials.Lit(new Color(0.2f, 0.2f, 0.27f));

            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(eye.GetComponent<Collider>());
            eye.transform.SetParent(shell.transform, false);
            eye.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            eye.transform.localScale = Vector3.one * 0.4f;
            eye.GetComponent<Renderer>().material = BattleMaterials.Unlit(accent);

            _body = shell.transform;
            _body.gameObject.SetActive(false);
        }

        /// Deploy at the owner's position, or recall if already out.
        public void Toggle()
        {
            if (_owner.ControlLocked)
            {
                return;
            }

            if (Deployed)
            {
                Deployed = false;
                _body.gameObject.SetActive(false);
                return;
            }

            Deployed = true;
            _body.gameObject.SetActive(true);
            _body.position = _owner.Position + Vector3.up * CombatTuning.Pod.HoverHeight;
        }

        public void Tick(float dt, RoboAvatar target)
        {
            var part = _owner.Loadout.Pod;

            // Energy regenerates whether deployed or not (its own pool).
            Energy = Mathf.Min(part.EnergyMax, Energy + part.EnergyRegenPerSec * dt);

            if (!Deployed)
            {
                return;
            }

            // Owner dead: pod powers off.
            if (_owner.Health.State == HealthState.Dead)
            {
                Deployed = false;
                _body.gameObject.SetActive(false);
                return;
            }

            _bobTime += dt;
            _body.position += Vector3.up * (Mathf.Sin(_bobTime * 2.5f) * 0.003f);
            _body.Rotate(0f, dt * 85f, 0f, Space.World);

            _fireCooldown -= dt;
            var targetAlive = target != null && target.Health.State != HealthState.Dead;
            var inRange = targetAlive
                && Vector3.Distance(_body.position, target.Position) <= CombatTuning.Pod.FireRange;

            if (targetAlive && inRange && _fireCooldown <= 0f && Energy >= part.EnergyPerShot)
            {
                _fireCooldown = part.FireInterval;
                Energy -= part.EnergyPerShot;
                var aim = target.Position + Vector3.up * 1.0f;
                _projectiles.Spawn(_owner, target, _body.position, aim,
                    part.Damage * _owner.Stats.AtkMult, part.EnduranceDamage,
                    CombatTuning.Pod.ProjectileSpeed, CombatTuning.Pod.HomingTurnRate);
            }
        }
    }
}
