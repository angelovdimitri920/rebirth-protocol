using RebirthProtocol.Battle.Audio;
using RebirthProtocol.Battle.Effects;
using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RebirthProtocol.Battle
{
    // Scene entry point: builds the whole run (arena, robos, brains,
    // camera, HUD, hangar, draft) from code in Awake, then drives
    // everything from one Update in a fixed order — hangar OR draft OR
    // (brains, clash, melee, motors, projectiles, bombs, pods, effects,
    // pickups, camera, HUD) — so simulation order never depends on Unity
    // script ordering.
    //
    // Run flow (GAME_DESIGN §4, Stage 3): deploy starts a 5-fight run
    // against escalating Order rivals. Victory opens a boon/spoils draft,
    // then the next fight rolls a fresh arena; HP carries with a 15% heal.
    // Defeat (or clearing fight 5) ends the run — R returns to the hangar.
    public sealed class DuelManager : MonoBehaviour
    {
        public RoboAvatar Player { get; private set; }
        public RoboAvatar Enemy { get; private set; }
        public bool IsOver { get; private set; }
        public bool PlayerWon { get; private set; }

        /// True while the pre-run hangar (loadout select) is open: the
        /// simulation is held until the player deploys.
        public bool InHangar { get; private set; } = true;

        /// True while the between-fights draft is open (sim held).
        public bool InDraft { get; private set; }

        /// The run ended — by defeat, or by clearing the final fight.
        public bool RunOver { get; private set; }
        public bool RunWon { get; private set; }

        /// Fight paused (P / Start): simulation ticks are skipped entirely.
        public bool Paused { get; private set; }

        /// Turn off both brains for a training-dummy duel (and for
        /// deterministic PlayMode tests): the simulation keeps running, but
        /// nobody feeds the avatars intent.
        public bool BrainsEnabled = true;

        /// Nonzero pins the run's RNG (arena rolls, drafts, item drops) for
        /// deterministic PlayMode tests.
        public static int RunSeedOverride;

        public RunEffects Effects => _effects;
        public DraftScreen Draft => _draft;
        public int FightNumber => _run.FightIndex + 1;
        public string RivalTitle => $"{_rival.PilotName} · {_rival.OrderName}";

        private RunState _run;
        private RunEffects _effects;
        private System.Random _runRng;
        private Loadout _playerLoadout;
        private RivalPreset _rival;
        private DraftScreen _draft;
        private float _victoryTimer;
        private AfterimageSystem _afterimages;

        private sealed class ItemPickup
        {
            public Transform Tf;
            public Item Item;
            public float Spin;
        }

        private readonly System.Collections.Generic.List<ItemPickup> _pickups
            = new System.Collections.Generic.List<ItemPickup>();

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
        private Transform _arenaRoot;
        private Transform _lockReticle;
        private Renderer _lockReticleRenderer;

        /// Name of the currently active Holosseum layout (Depot/Colonnade/
        /// Frostfield/Cinderfield) — exposed for the HUD and tests.
        public string ArenaName => _arena.Name;

        private void Awake()
        {
            // Force borderless fullscreen at native resolution. The player
            // setting already defaults to this, but a standalone player
            // PERSISTS the last window mode to the registry — so once anyone
            // (or a -screen-fullscreen 0 test run) launches windowed, later
            // launches stay windowed until overridden here. Skip the force
            // when -screen-fullscreen is on the command line so the
            // screenshot smoke tests can still run windowed.
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-screen-fullscreen") < 0)
            {
                var res = Screen.currentResolution;
                Screen.SetResolution(res.width, res.height, FullScreenMode.FullScreenWindow);
            }

            var audioGo = new GameObject("Audio");
            audioGo.transform.SetParent(transform, false);
            GameAudio.Sfx = audioGo.AddComponent<SfxPlayer>();
            _music = audioGo.AddComponent<MusicSequencer>();
            _music.Play(MusicMode.Hangar);

            _arenaRoot = new GameObject("Arena").transform;
            _arenaRoot.SetParent(transform, false);

            _projectiles = new GameObject("Projectiles").AddComponent<ProjectileSystem>();
            _projectiles.transform.SetParent(transform, false);

            _afterimages = new GameObject("Afterimages").AddComponent<AfterimageSystem>();
            _afterimages.transform.SetParent(transform, false);

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

            // VFX: world-space effects (muzzle/impact/explosion + camera
            // shake) and screen-space post (flash / grain / vignette).
            var fxGo = new GameObject("Effects");
            fxGo.transform.SetParent(transform, false);
            var screenFx = fxGo.AddComponent<ScreenFx>();
            screenFx.Init();
            GameEffects.Fx = fxGo.AddComponent<EffectsSystem>();
            GameEffects.Fx.Init(_cameraRig, screenFx);

            StartRun(LoadoutStore.Load());

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
            StartRun(playerLoadout);
            CloseHangar();
        }

        /// Fresh run: reset run state and effects, reseed the run RNG, and
        /// spawn fight 1. Also runs once in Awake (with the stored loadout)
        /// so the world exists behind the hangar and PlayMode tests that
        /// CloseHangar() without deploying land in a live run.
        private void StartRun(Loadout playerLoadout)
        {
            _playerLoadout = playerLoadout;
            _run = new RunState();
            _runRng = new System.Random(
                RunSeedOverride != 0 ? RunSeedOverride : System.Environment.TickCount);
            _effects = new RunEffects(_runRng);
            StartFight();
        }

        /// Spawn the current fight: roll the arena, spawn the rival at the
        /// run's escalating power, carry the player's HP forward.
        private void StartFight()
        {
            _rival = RunOpponents.ForFight(_run.FightIndex);

            // Arena modifier roll (§4): fight 1 is always hazard-free so
            // run openings stay readable — Depot/Colonnade only.
            RebuildArena(_run.FightIndex == 0 ? _runRng.Next(2) : _runRng.Next(4));

            SpawnCombatants(_playerLoadout, _rival.BuildLoadout(),
                RunState.EnemyPowerMult(_run.FightIndex));

            if (_run.CarriedHp.HasValue)
            {
                Player.Health.SetHp(_run.StartingHp(Player.Health.MaxHp));
            }

            if (!InHangar)
            {
                _music.Play(MusicMode.Combat);
            }
        }

        /// Respawn both sides with explicit loadouts — used by PlayMode
        /// tests to pin matchups outside the run's rival rotation.
        public void RespawnWithLoadouts(Loadout playerLoadout, Loadout enemyLoadout)
        {
            SpawnCombatants(playerLoadout, enemyLoadout);
        }

        /// Rebuild the arena with a specific layout index — used by
        /// PlayMode tests so hazard behavior isn't at the mercy of the
        /// per-fight roll. Not used by real gameplay.
        public void ForceArenaLayout(int layoutIndex)
        {
            RebuildArena(layoutIndex);
        }

        private void RebuildArena(int layoutIndex)
        {
            for (var i = _arenaRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_arenaRoot.GetChild(i).gameObject);
            }

            _arena = ArenaBuilder.Build(_arenaRoot, layoutIndex);

            // Destroyed crates may drop item pickups (§4: destructible
            // cover doubles as the run's economy).
            foreach (var crate in _arenaRoot.GetComponentsInChildren<CrateHealth>())
            {
                crate.OnDestroyed = MaybeDropItem;
            }

            ClearPickups();
        }

        /// (Re)spawn both robos and everything attached to them. Safe to
        /// call again from the hangar: previous instances are destroyed.
        private void SpawnCombatants(Loadout playerLoadout, Loadout enemyLoadout, float enemyPowerMult = 1f)
        {
            _projectiles.Clear(); // no in-flight shots with stale owner/target refs
            _afterimages.Clear();

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
            Enemy = SpawnRobo("Enemy", enemyLoadout, new Vector3(8f, 0f, 0f), -0.5f * Mathf.PI,
                new Color(0.45f, 0.22f, 0.22f), new Color(1f, 0.25f, 0.2f), enemyPowerMult);

            _playerBomb = SpawnSystem<BombSystem>("Player Bomb");
            _playerBomb.Init(Player);
            _enemyBomb = SpawnSystem<BombSystem>("Enemy Bomb");
            _enemyBomb.Init(Enemy);
            _playerPod = SpawnSystem<PodSystem>("Player Pod");
            _playerPod.Init(Player, _projectiles, new Color(0.2f, 0.55f, 1f));
            _enemyPod = SpawnSystem<PodSystem>("Enemy Pod");
            _enemyPod.Init(Enemy, _projectiles, new Color(1f, 0.25f, 0.2f));

            // Run effects belong to the player only (§4) and rebind to the
            // fresh avatar each fight.
            Player.Effects = _effects;
            Player.OnAfterimageSpawn = _afterimages.Spawn;
            _effects.Bind(Player.Health);
            Player.Health.IncreaseMaxHp(_effects.PlatingMaxHpBonus);
            _effects.ResetGunCooldown = Player.Gun.ResetCooldown;
            _effects.ResetBombCooldown = _playerBomb.ResetCooldown;

            _cameraRig.Init(_cameraRig.GetComponent<Camera>(), Player, Enemy);

            _playerBrain = gameObject.AddComponent<PlayerBrain>();
            _playerBrain.Init(Player, Enemy, _cameraRig, _playerBomb, _playerPod);
            _enemyBrain = gameObject.AddComponent<EnemyBrain>();
            _enemyBrain.Init(Enemy, Player, _enemyBomb, _enemyPod, seed: 1337 + _run.FightIndex);

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

        // --- Run interlude: draft, spoils, next fight ---

        private void AdvanceRun()
        {
            _run.CarriedHp = Player.Health.Hp;
            _run.FightIndex += 1;

            if (_run.FightIndex >= RunState.FightsPerRun)
            {
                RunOver = true;
                RunWon = true;
                return;
            }

            InDraft = true;
            _draft = new GameObject("Draft").AddComponent<DraftScreen>();
            _draft.transform.SetParent(transform, false);
            _draft.Init(
                DraftRoll.Offer(_effects, _runRng),
                _rival, // the rival just felled: their arm is the spoils offer
                _run.RerollsLeft,
                onPick: boon =>
                {
                    _effects.AddBoon(boon);
                    CloseDraftAndStartNextFight();
                },
                onSpoils: () =>
                {
                    // Spoils of War (SETTING_AND_FACTIONS.md): run-scoped —
                    // the hangar loadout saved to PlayerPrefs is untouched.
                    _rival.ApplySpoils(_playerLoadout);
                    CloseDraftAndStartNextFight();
                },
                onReroll: () =>
                {
                    _run.RerollsLeft -= 1;
                    return DraftRoll.Offer(_effects, _runRng);
                },
                onSkip: CloseDraftAndStartNextFight);
        }

        private void CloseDraftAndStartNextFight()
        {
            Destroy(_draft.gameObject);
            _draft = null;
            InDraft = false;
            StartFight();
        }

        // --- Item drops (§4): destroyed crates roll a walk-over pickup ---

        private void MaybeDropItem(Vector3 at)
        {
            if (IsOver || _runRng.NextDouble() > 0.3)
            {
                return;
            }

            SpawnItemPickup(at);
        }

        /// PlayMode-test hook (ForceArenaLayout's precedent): drop without
        /// the 30% roll.
        public void DebugForceItemDrop(Vector3 at)
        {
            SpawnItemPickup(at);
        }

        private void SpawnItemPickup(Vector3 at)
        {
            var item = RunCatalog.Items[_runRng.Next(RunCatalog.Items.Length)];
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.name = "Item Pickup";
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(at.x, 0.7f, at.z);
            go.transform.localScale = Vector3.one * 0.5f;
            go.GetComponent<Renderer>().material = BattleMaterials.Unlit(new Color(0.2f, 1f, 0.8f));
            _pickups.Add(new ItemPickup { Tf = go.transform, Item = item });
        }

        private void TickPickups(float dt)
        {
            for (var i = _pickups.Count - 1; i >= 0; i--)
            {
                var p = _pickups[i];
                p.Spin += 2.5f * dt;
                p.Tf.rotation = Quaternion.Euler(0f, p.Spin * Mathf.Rad2Deg, 0f);
                p.Tf.position = new Vector3(p.Tf.position.x, 0.7f + Mathf.Sin(p.Spin) * 0.12f, p.Tf.position.z);

                var to = Player.Position - p.Tf.position;
                to.y = 0f;
                if (to.magnitude < 1.4f)
                {
                    _effects.AddItem(p.Item); // plating lands on the bound health immediately
                    GameAudio.Sfx?.Pickup(p.Tf.position);
                    _hud.Toast($"+ {p.Item.Name}");
                    Destroy(p.Tf.gameObject);
                    _pickups.RemoveAt(i);
                }
            }
        }

        private void ClearPickups()
        {
            foreach (var p in _pickups)
            {
                Destroy(p.Tf.gameObject);
            }

            _pickups.Clear();
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

            if (InDraft)
            {
                _draft.Tick(); // resolving mid-tick destroys the draft and respawns the fight
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
            ApplyLava(Player, dt);
            ApplyLava(Enemy, dt);
            Player.TickMotor(dt);
            Enemy.TickMotor(dt);

            _projectiles.Tick(dt);
            _playerBomb.Tick(dt, Player, Enemy);
            _enemyBomb.Tick(dt, Player, Enemy);
            _playerPod.Tick(dt, Enemy);
            _enemyPod.Tick(dt, Player);

            _effects.Tick(dt);
            _afterimages.Tick(dt, Enemy, _effects);
            TickPickups(dt);

            _cameraRig.Tick(dt);
            TickLockReticle(dt);
            _hud.Tick();

            if (!IsOver)
            {
                if (Enemy.Health.State == HealthState.Dead)
                {
                    IsOver = true;
                    PlayerWon = true;
                    _victoryTimer = 0f;
                    GameAudio.Sfx?.Victory();
                    _music.Play(MusicMode.Hangar);
                }
                else if (Player.Health.State == HealthState.Dead)
                {
                    // Death ends the run — no retry mid-run (§4 Stage 3).
                    IsOver = true;
                    PlayerWon = false;
                    RunOver = true;
                    RunWon = false;
                    GameAudio.Sfx?.Defeat();
                    _music.Play(MusicMode.Hangar);
                }
            }
            else if (PlayerWon && !RunOver)
            {
                // Let the kill land visually before the draft opens.
                _victoryTimer += dt;
                if (_victoryTimer >= 1.4f)
                {
                    AdvanceRun();
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

        /// Lava pools (Cinderfield): continuous DoT while grounded inside
        /// one, ported straight from Arena.ts's applyHazards — bypasses the
        /// shield entirely (environmental, not directional) by hitting
        /// CombatantHealth directly instead of routing through ReceiveHit.
        private void ApplyLava(RoboAvatar avatar, float dt)
        {
            avatar.LavaSoundCooldown -= dt;
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

                avatar.Health.TakeHit(24f * dt, 14f * dt);
                if (avatar.LavaSoundCooldown <= 0f)
                {
                    GameAudio.Sfx?.HazardSizzle(avatar.Position);
                    avatar.LavaSoundCooldown = 0.4f;
                }
            }
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
            GameAudio.Sfx?.Clash(Vector3.Lerp(Player.Position, Enemy.Position, 0.5f));

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

        private RoboAvatar SpawnRobo(string name, Loadout loadout, Vector3 spawn, float facing, Color hull, Color accent,
            float powerMult = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = spawn;
            var avatar = go.AddComponent<RoboAvatar>();
            avatar.Init(loadout, hull, accent, _projectiles, facing, powerMult);
            return avatar;
        }

    }
}
