using RebirthProtocol.Battle;
using RebirthProtocol.Battle.Audio;
using RebirthProtocol.Battle.Effects;
using RebirthProtocol.Domain;
using UnityEngine;

namespace RebirthProtocol.Tests.BalanceHarness
{
    // One headless AI-vs-AI duel: arena + two avatars + weapon systems +
    // two EnemyBrains, no camera/HUD/audio/title rig. Step() replicates
    // DuelManager.Update's simulation order EXACTLY (brains → TickShield →
    // charge-request resolution → CheckMeleeClash → TickMelee → TickCharge
    // → hazards → TickMotor → projectiles → bombs → pods) — several Codex
    // findings in Pass B/C were specifically about that ordering, so it is
    // copied, not re-derived. Stepped many ticks per rendered frame by the
    // balance harness; deferred-Destroy cleanup is why the battle systems
    // strip colliders with DestroyImmediate.
    public sealed class HeadlessDuel : System.IDisposable
    {
        private const float MeleeClashRange = 3.5f; // DuelManager.CheckMeleeClash
        private const float MeleeClashKnockback = 9f;

        private readonly GameObject _root;
        private readonly SimulationMode _prevSimulationMode;
        private readonly ArenaBuilder.Result _arena;
        private readonly ProjectileSystem _projectiles;
        private readonly RoboAvatar _a;
        private readonly RoboAvatar _b;
        private readonly BombSystem _bombA;
        private readonly BombSystem _bombB;
        private readonly PodSystem _podA;
        private readonly PodSystem _podB;
        private readonly EnemyBrain _brainA;
        private readonly EnemyBrain _brainB;

        public int KnockdownsA { get; private set; }
        public int KnockdownsB { get; private set; }
        public float Elapsed { get; private set; }
        public RoboAvatar A => _a;
        public RoboAvatar B => _b;

        /// A always spawns on the -x side (ticked first each frame); B on
        /// +x (ticked second). The runner alternates which BUILD is slot A
        /// across a pairing's fights, so this fixed A-first ordering never
        /// biases a build's aggregate win rate.
        public HeadlessDuel(Loadout loadoutA, Loadout loadoutB, int arenaLayout,
            int seedA, int seedB)
        {
            // The battle code's audio/VFX hooks are all null-conditional
            // statics; make sure nothing stale from another test leaks in.
            GameAudio.Sfx = null;
            GameEffects.Fx = null;

            // Determinism: put physics under script control. The harness
            // batches hundreds of Step()s between rendered frames, and Unity
            // otherwise runs auto-simulation a WALL-CLOCK-VARIABLE number of
            // times at each frame boundary — enough to make two identical
            // runs diverge (8/20 fights, before this). Nothing in the sim
            // needs dynamics stepping (avatars are kinematic
            // CharacterControllers, shots are raycasts), so Step() never
            // calls Physics.Simulate — it only Physics.SyncTransforms so the
            // moved controllers are visible to the next step's raycasts.
            _prevSimulationMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            _root = new GameObject("HeadlessDuel");

            var arenaRoot = new GameObject("Arena").transform;
            arenaRoot.SetParent(_root.transform, false);
            _arena = ArenaBuilder.Build(arenaRoot, arenaLayout);

            _projectiles = new GameObject("Projectiles").AddComponent<ProjectileSystem>();
            _projectiles.transform.SetParent(_root.transform, false);

            // DuelManager spawn geometry: ±8 on x, facing each other.
            _a = SpawnRobo("A", loadoutA, new Vector3(-8f, 0f, 0f), 0.5f * Mathf.PI);
            _b = SpawnRobo("B", loadoutB, new Vector3(8f, 0f, 0f), -0.5f * Mathf.PI);

            _bombA = SpawnSystem<BombSystem>("Bomb A");
            _bombA.Init(_a);
            _bombB = SpawnSystem<BombSystem>("Bomb B");
            _bombB.Init(_b);
            _podA = SpawnSystem<PodSystem>("Pod A");
            _podA.Init(_a, _projectiles, Color.white);
            _podB = SpawnSystem<PodSystem>("Pod B");
            _podB.Init(_b, _projectiles, Color.white);

            _brainA = _root.AddComponent<EnemyBrain>();
            _brainA.Init(_a, _b, _bombA, _podA, seedA);
            _brainB = _root.AddComponent<EnemyBrain>();
            _brainB.Init(_b, _a, _bombB, _podB, seedB);

            _a.Health.KnockedDown += () => KnockdownsA++;
            _b.Health.KnockedDown += () => KnockdownsB++;
        }

        public bool IsOver =>
            _a.Health.State == HealthState.Dead || _b.Health.State == HealthState.Dead;

        /// Reproduces DuelManager.Update's EXACT same-frame double-KO
        /// resolution rather than inventing a fairer-looking rule: it checks
        /// Enemy.Health.State == Dead first and unconditionally awards
        /// Player the win in that branch, only falling through to an `else
        /// if` on Player's own death — so a same-frame bomb trade that kills
        /// both sides is scored as a PLAYER win in the real game, never a
        /// draw. A spawns at -8 (Player's slot), B at +8 (Enemy's slot), so
        /// B is checked first here too (Codex PR #16 finding: the harness
        /// must measure the game's actual scoring rule, not a rule that
        /// merely sounds more symmetric). A true draw is now ONLY a timeout
        /// at the fight cap — see BalanceHarnessRun's maxFightSeconds loop.
        public FightOutcome OutcomeNow()
        {
            if (_b.Health.State == HealthState.Dead)
            {
                return FightOutcome.WinA;
            }

            return _a.Health.State == HealthState.Dead ? FightOutcome.WinB : FightOutcome.Draw;
        }

        public void Step(float dt)
        {
            _brainA.Tick(dt);
            _brainB.Tick(dt);

            _a.TickShield(dt);
            _b.TickShield(dt);

            if (_a.Intent.ChargeRequested)
            {
                _a.TryCharge(_b);
            }

            if (_b.Intent.ChargeRequested)
            {
                _b.TryCharge(_a);
            }

            CheckMeleeClash();

            _a.TickMelee(dt, _b);
            _b.TickMelee(dt, _a);

            _a.TickCharge(dt, _b);
            _b.TickCharge(dt, _a);

            ApplyIce(_a);
            ApplyIce(_b);
            ApplyLava(_a, dt);
            ApplyLava(_b, dt);
            _a.TickMotor(dt);
            _b.TickMotor(dt);

            // Many sim steps run inside one rendered frame: make the moved
            // CharacterController colliders visible to this step's raycasts
            // instead of trusting end-of-frame auto-sync.
            Physics.SyncTransforms();

            _projectiles.Tick(dt);
            _bombA.Tick(dt, _a, _b);
            _bombB.Tick(dt, _a, _b);
            _podA.Tick(dt, _b);
            _podB.Tick(dt, _a);

            Elapsed += dt;
        }

        private void CheckMeleeClash()
        {
            if (!_a.Melee.Attacking || !_b.Melee.Attacking)
            {
                return;
            }

            if (Vector3.Distance(_a.Position, _b.Position) > MeleeClashRange)
            {
                return;
            }

            _a.ClashCancel();
            _b.ClashCancel();

            var apart = _b.Position - _a.Position;
            apart.y = 0f;
            apart = apart.normalized;
            _b.ApplyKnockback(apart, MeleeClashKnockback);
            _a.ApplyKnockback(-apart, MeleeClashKnockback);
        }

        private void ApplyIce(RoboAvatar avatar)
        {
            var onIce = false;
            foreach (var region in _arena.IceRegions)
            {
                if (region.Contains(new Vector2(avatar.Position.x, avatar.Position.z)))
                {
                    onIce = true;
                    break;
                }
            }

            avatar.OnIce = onIce;
        }

        private void ApplyLava(RoboAvatar avatar, float dt)
        {
            if (!avatar.Grounded || avatar.Health.State == HealthState.Dead)
            {
                return;
            }

            var pos = new Vector2(avatar.Position.x, avatar.Position.z);
            foreach (var pool in _arena.LavaPools)
            {
                if (Vector2.Distance(pos, pool.Center) > pool.Radius)
                {
                    continue;
                }

                avatar.Health.TakeHit(24f * dt, 14f * dt); // DuelManager.ApplyLava
            }
        }

        private RoboAvatar SpawnRobo(string name, Loadout loadout, Vector3 spawn, float facing)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            go.transform.position = spawn;
            var avatar = go.AddComponent<RoboAvatar>();
            avatar.Init(loadout, Color.gray, Color.white, _projectiles, facing);
            return avatar;
        }

        private T SpawnSystem<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            return go.AddComponent<T>();
        }

        /// Destruction is deferred to end of frame — the caller must yield a
        /// frame before building the next duel, or the old arena's colliders
        /// would still block the new fight's first shots.
        public void Dispose()
        {
            Physics.simulationMode = _prevSimulationMode;
            Object.Destroy(_root);
        }
    }
}
