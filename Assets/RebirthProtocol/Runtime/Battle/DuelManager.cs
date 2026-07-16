using RebirthProtocol.Battle.Audio;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RebirthProtocol.Battle
{
    // Scene entry point: builds the whole duel (arena, robos, brains,
    // camera, HUD, hangar) from code in Awake, then drives everything from
    // one Update in a fixed order — hangar OR (brains, clash, melee,
    // motors, projectiles, bombs, pods, camera, HUD) — so simulation order
    // never depends on Unity script ordering.
    public sealed class DuelManager : MonoBehaviour
    {
        public RoboAvatar Player { get; private set; }
        public RoboAvatar Enemy { get; private set; }
        public bool IsOver { get; private set; }
        public bool PlayerWon { get; private set; }

        /// True while the pre-duel hangar (loadout select) is open: the
        /// simulation is held until the player deploys.
        public bool InHangar { get; private set; } = true;

        /// Fight paused (P / Start): simulation ticks are skipped entirely.
        public bool Paused { get; private set; }

        /// Turn off both brains for a training-dummy duel (and for
        /// deterministic PlayMode tests): the simulation keeps running, but
        /// nobody feeds the avatars intent.
        public bool BrainsEnabled = true;

        // Enemy builds rotate per launch/rematch so loadout tradeoffs get
        // tested against real variety.
        private static int _enemyBuildIndex;

        private PlayerBrain _playerBrain;
        private EnemyBrain _enemyBrain;
        private ProjectileSystem _projectiles;
        private DuelCameraRig _cameraRig;
        private DuelHud _hud;
        private HangarScreen _hangar;
        private BombSystem _playerBomb;
        private BombSystem _enemyBomb;
        private PodSystem _playerPod;
        private PodSystem _enemyPod;
        private MusicSequencer _music;
        private ArenaBuilder.Result _arena;
        private Transform _lockReticle;
        private Renderer _lockReticleRenderer;

        private void Awake()
        {
            var audioGo = new GameObject("Audio");
            audioGo.transform.SetParent(transform, false);
            GameAudio.Sfx = audioGo.AddComponent<SfxPlayer>();
            _music = audioGo.AddComponent<MusicSequencer>();
            _music.Play(MusicMode.Hangar);

            _arena = ArenaBuilder.Build(transform, _enemyBuildIndex);

            _projectiles = new GameObject("Projectiles").AddComponent<ProjectileSystem>();
            _projectiles.transform.SetParent(transform, false);

            var camGo = new GameObject("Duel Camera");
            camGo.transform.SetParent(transform, false);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.025f, 0.035f);
            camGo.AddComponent<AudioListener>();
            _cameraRig = camGo.AddComponent<DuelCameraRig>();

            var lightGo = new GameObject("Key Light");
            lightGo.transform.SetParent(transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            SpawnCombatants(LoadoutStore.Load());

            _hangar = new GameObject("Hangar").AddComponent<HangarScreen>();
            _hangar.transform.SetParent(transform, false);
            _hangar.Init(LoadoutStore.Load(), OnDeploy);

            // Automated smoke tests: boot straight into combat.
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-autodeploy") >= 0)
            {
                CloseHangar();
            }
        }

        /// Close the hangar without changing the spawned loadout (PlayMode
        /// tests, and any future skip-straight-to-fight flow).
        public void CloseHangar()
        {
            InHangar = false;
            _hangar.Show(false);
            _music.Play(MusicMode.Combat);
        }

        private void OnDeploy(Loadout playerLoadout)
        {
            LoadoutStore.Save(playerLoadout);
            SpawnCombatants(playerLoadout);
            CloseHangar();
        }

        /// Respawn both sides with explicit loadouts — used by PlayMode
        /// tests and, later, the run loop's scripted opponents.
        public void RespawnWithLoadouts(Loadout playerLoadout, Loadout enemyLoadout)
        {
            SpawnCombatants(playerLoadout, enemyLoadout);
        }

        /// (Re)spawn both robos and everything attached to them. Safe to
        /// call again from the hangar: previous instances are destroyed.
        private void SpawnCombatants(Loadout playerLoadout, Loadout enemyLoadout = null)
        {
            _projectiles.Clear(); // no in-flight shots with stale owner/target refs

            if (Player != null)
            {
                Destroy(Player.gameObject);
                Destroy(Enemy.gameObject);
                Destroy(_playerBomb.gameObject);
                Destroy(_enemyBomb.gameObject);
                Destroy(_playerPod.gameObject);
                Destroy(_enemyPod.gameObject);
                Destroy(_hud.gameObject);
                Destroy(_playerBrain);
                Destroy(_enemyBrain);
            }

            Player = SpawnRobo("Player", playerLoadout, new Vector3(-8f, 0f, 0f), 0.5f * Mathf.PI,
                new Color(0.28f, 0.38f, 0.55f), new Color(0.2f, 0.55f, 1f));
            Player.gameObject.tag = "Player";
            Enemy = SpawnRobo("Enemy", enemyLoadout ?? EnemyLoadout(), new Vector3(8f, 0f, 0f), -0.5f * Mathf.PI,
                new Color(0.45f, 0.22f, 0.22f), new Color(1f, 0.25f, 0.2f));

            _playerBomb = SpawnSystem<BombSystem>("Player Bomb");
            _playerBomb.Init(Player);
            _enemyBomb = SpawnSystem<BombSystem>("Enemy Bomb");
            _enemyBomb.Init(Enemy);
            _playerPod = SpawnSystem<PodSystem>("Player Pod");
            _playerPod.Init(Player, _projectiles, new Color(0.2f, 0.55f, 1f));
            _enemyPod = SpawnSystem<PodSystem>("Enemy Pod");
            _enemyPod.Init(Enemy, _projectiles, new Color(1f, 0.25f, 0.2f));

            _cameraRig.Init(_cameraRig.GetComponent<Camera>(), Player, Enemy);

            _playerBrain = gameObject.AddComponent<PlayerBrain>();
            _playerBrain.Init(Player, Enemy, _cameraRig, _playerBomb, _playerPod);
            _enemyBrain = gameObject.AddComponent<EnemyBrain>();
            _enemyBrain.Init(Enemy, Player, _enemyBomb, _enemyPod, seed: 1337 + _enemyBuildIndex);

            _hud = new GameObject("HUD").AddComponent<DuelHud>();
            _hud.transform.SetParent(transform, false);
            _hud.Init(Player, Enemy, this, _playerBomb, _playerPod);

            // Lock-on reticle: a diamond over the enemy, red while they can
            // be damaged, grey while downed/rebirthing (invulnerable).
            if (_lockReticle == null)
            {
                var reticle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(reticle.GetComponent<Collider>());
                reticle.name = "Lock Reticle";
                reticle.transform.SetParent(transform, false);
                reticle.transform.localScale = new Vector3(0.35f, 0.35f, 0.05f);
                _lockReticleRenderer = reticle.GetComponent<Renderer>();
                _lockReticleRenderer.material = BattleMaterials.Unlit(new Color(1f, 0.25f, 0.2f));
                _lockReticle = reticle.transform;
            }

            IsOver = false;
            PlayerWon = false;
        }

        private T SpawnSystem<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        private Loadout EnemyLoadout()
        {
            var c = PartsCatalog.Bodies;
            var builds = new[]
            {
                // Baseline mirror: Legionnaire gunner with a bomb.
                new Loadout { Body = c[0], Gun = PartsCatalog.Guns[0], Bomb = PartsCatalog.Bombs[0], Legs = PartsCatalog.Legs[0], Pod = PartsCatalog.Pods[0] },
                // Raider: Valkyrie saber rush with fast legs.
                new Loadout { Body = c[1], Melee = PartsCatalog.MeleeWeapons[0], Bomb = PartsCatalog.Bombs[0], Legs = PartsCatalog.Legs[1], Pod = PartsCatalog.Pods[1] },
                // Warden: Crusader Knight with a Ballista behind a Bastion shield.
                new Loadout { Body = c[3], Gun = PartsCatalog.Guns[2], Shield = PartsCatalog.Shields[1], Legs = PartsCatalog.Legs[0], Pod = PartsCatalog.Pods[0] }
            };
            var build = builds[_enemyBuildIndex % builds.Length];
            _enemyBuildIndex += 1;
            return build;
        }

        private void Update()
        {
            if (InHangar)
            {
                _hangar.Tick();

                // Deploying mid-tick (via _hangar.Tick() -> OnDeploy) already
                // re-initialized the camera rig into orthographic combat
                // mode — don't clobber that by applying the hangar's
                // perspective close-up afterward.
                if (!InHangar)
                {
                    return;
                }

                // Hangar camera: fixed close-up on the preview robo dais.
                _cameraRig.transform.position = new Vector3(0f, 2.4f, 5.2f);
                _cameraRig.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.3f, 0f) - _cameraRig.transform.position);
                _cameraRig.GetComponent<Camera>().orthographic = false;
                return;
            }

            if (!IsOver && PausePressed())
            {
                Paused = !Paused;
                GameAudio.Sfx?.UiClick();
            }

            if (Paused)
            {
                _hud.Tick(); // banner shows PAUSED
                return;
            }

            var dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            if (IsOver && RestartPressed())
            {
                SceneManager.LoadScene(gameObject.scene.buildIndex >= 0 ? gameObject.scene.buildIndex : 0);
                return;
            }

            if (BrainsEnabled)
            {
                _playerBrain.Tick(dt);
                _enemyBrain.Tick(dt);
            }

            CheckMeleeClash();

            Player.TickMelee(dt, Enemy);
            Enemy.TickMelee(dt, Player);

            ApplyIce(Player);
            ApplyIce(Enemy);
            Player.TickMotor(dt);
            Enemy.TickMotor(dt);

            _projectiles.Tick(dt);
            _playerBomb.Tick(dt, Player, Enemy);
            _enemyBomb.Tick(dt, Player, Enemy);
            _playerPod.Tick(dt, Enemy);
            _enemyPod.Tick(dt, Player);

            _cameraRig.Tick(dt);
            TickLockReticle(dt);
            _hud.Tick();

            if (!IsOver)
            {
                if (Enemy.Health.State == HealthState.Dead)
                {
                    IsOver = true;
                    PlayerWon = true;
                    GameAudio.Sfx?.Victory();
                    _music.Play(MusicMode.Hangar);
                }
                else if (Player.Health.State == HealthState.Dead)
                {
                    IsOver = true;
                    PlayerWon = false;
                    GameAudio.Sfx?.Defeat();
                    _music.Play(MusicMode.Hangar);
                }
            }
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

        private void TickLockReticle(float dt)
        {
            _lockReticle.position = Enemy.Position + Vector3.up * 2.7f;
            _lockReticle.rotation = Quaternion.LookRotation(_cameraRig.transform.forward) * Quaternion.Euler(0f, 0f, 45f);
            var targetable = Enemy.Health.State == HealthState.Active;
            _lockReticleRenderer.material.color = targetable
                ? new Color(1f, 0.25f, 0.2f)
                : new Color(0.5f, 0.5f, 0.5f, 0.6f);
        }

        private static bool PausePressed()
        {
            return (Keyboard.current?.pKey.wasPressedThisFrame ?? false)
                || (Gamepad.current?.startButton.wasPressedThisFrame ?? false);
        }

        /// Melee clash (GAME_DESIGN §3.1): simultaneous melee attacks in
        /// range cancel both into a short step-cancel rather than trading
        /// hits in whatever order the loop happens to tick them.
        private void CheckMeleeClash()
        {
            const float clashRange = 3.5f;
            const float clashKnockback = 9f;

            if (!Player.Melee.Attacking || !Enemy.Melee.Attacking)
            {
                return;
            }

            if (Vector3.Distance(Player.Position, Enemy.Position) > clashRange)
            {
                return;
            }

            Player.ClashCancel();
            Enemy.ClashCancel();
            GameAudio.Sfx?.Clash();

            var apart = Enemy.Position - Player.Position;
            apart.y = 0f;
            apart = apart.normalized;
            Enemy.ApplyKnockback(apart, clashKnockback);
            Player.ApplyKnockback(-apart, clashKnockback);
        }

        private static bool RestartPressed()
        {
            return (Keyboard.current?.rKey.wasPressedThisFrame ?? false)
                || (Gamepad.current?.startButton.wasPressedThisFrame ?? false);
        }

        private RoboAvatar SpawnRobo(string name, Loadout loadout, Vector3 spawn, float facing, Color hull, Color accent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = spawn;
            var avatar = go.AddComponent<RoboAvatar>();
            avatar.Init(loadout, hull, accent, _projectiles, facing);
            return avatar;
        }

    }
}
