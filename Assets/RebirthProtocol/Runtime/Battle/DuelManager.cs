using RebirthProtocol.Domain;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RebirthProtocol.Battle
{
    // Scene entry point: builds the whole duel (arena, robos, brains,
    // camera, HUD) from code in Awake, then drives everything from one
    // Update in a fixed order — brains, melee, motors, projectiles, camera,
    // HUD — so simulation order never depends on Unity script ordering.
    public sealed class DuelManager : MonoBehaviour
    {
        public RoboAvatar Player { get; private set; }
        public RoboAvatar Enemy { get; private set; }
        public bool IsOver { get; private set; }
        public bool PlayerWon { get; private set; }

        private PlayerBrain _playerBrain;
        private EnemyBrain _enemyBrain;
        private ProjectileSystem _projectiles;
        private DuelCameraRig _cameraRig;
        private DuelHud _hud;

        private void Awake()
        {
            BuildArena();

            _projectiles = new GameObject("Projectiles").AddComponent<ProjectileSystem>();
            _projectiles.transform.SetParent(transform, false);

            Player = SpawnRobo("Player", new Vector3(-8f, 0f, 0f), 0.5f * Mathf.PI,
                new Color(0.28f, 0.38f, 0.55f), new Color(0.2f, 0.55f, 1f));
            Player.gameObject.tag = "Player";
            Enemy = SpawnRobo("Enemy", new Vector3(8f, 0f, 0f), -0.5f * Mathf.PI,
                new Color(0.45f, 0.22f, 0.22f), new Color(1f, 0.25f, 0.2f));

            var camGo = new GameObject("Duel Camera");
            camGo.transform.SetParent(transform, false);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.025f, 0.035f);
            camGo.AddComponent<AudioListener>();
            _cameraRig = camGo.AddComponent<DuelCameraRig>();
            _cameraRig.Init(cam, Player, Enemy);

            var lightGo = new GameObject("Key Light");
            lightGo.transform.SetParent(transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            _playerBrain = gameObject.AddComponent<PlayerBrain>();
            _playerBrain.Init(Player, Enemy, _cameraRig);
            _enemyBrain = gameObject.AddComponent<EnemyBrain>();
            _enemyBrain.Init(Enemy, Player, seed: 1337);

            _hud = new GameObject("HUD").AddComponent<DuelHud>();
            _hud.transform.SetParent(transform, false);
            _hud.Init(Player, Enemy, this);
        }

        private void Update()
        {
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

            _playerBrain.Tick(dt);
            _enemyBrain.Tick(dt);

            Player.TickMelee(dt, Enemy);
            Enemy.TickMelee(dt, Player);

            Player.TickMotor(dt);
            Enemy.TickMotor(dt);

            _projectiles.Tick(dt);
            _cameraRig.Tick(dt);
            _hud.Tick();

            if (!IsOver)
            {
                if (Enemy.Health.State == HealthState.Dead)
                {
                    IsOver = true;
                    PlayerWon = true;
                }
                else if (Player.Health.State == HealthState.Dead)
                {
                    IsOver = true;
                    PlayerWon = false;
                }
            }
        }

        private static bool RestartPressed()
        {
            return (Keyboard.current?.rKey.wasPressedThisFrame ?? false)
                || (Gamepad.current?.startButton.wasPressedThisFrame ?? false);
        }

        private RoboAvatar SpawnRobo(string name, Vector3 spawn, float facing, Color hull, Color accent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = spawn;
            var avatar = go.AddComponent<RoboAvatar>();
            avatar.Init(hull, accent, _projectiles, facing);
            return avatar;
        }

        // Sealed Holosseum volume: floor, low visible walls, tall invisible
        // boundary + ceiling (a maxed hover reaches ~29m), and cover crates.
        private void BuildArena()
        {
            var size = CombatTuning.Arena.Size;
            var half = size * 0.5f;

            Block("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(size, 1f, size), new Color(0.16f, 0.17f, 0.2f));

            var wallColor = new Color(0.3f, 0.32f, 0.38f);
            var visH = CombatTuning.Arena.VisibleWallHeight;
            Block("Wall N", new Vector3(0f, visH * 0.5f, half + 0.5f), new Vector3(size + 2f, visH, 1f), wallColor);
            Block("Wall S", new Vector3(0f, visH * 0.5f, -half - 0.5f), new Vector3(size + 2f, visH, 1f), wallColor);
            Block("Wall E", new Vector3(half + 0.5f, visH * 0.5f, 0f), new Vector3(1f, visH, size + 2f), wallColor);
            Block("Wall W", new Vector3(-half - 0.5f, visH * 0.5f, 0f), new Vector3(1f, visH, size + 2f), wallColor);

            var wallH = CombatTuning.Arena.WallHeight;
            InvisibleBlock("Bound N", new Vector3(0f, wallH * 0.5f, half + 0.5f), new Vector3(size + 2f, wallH, 1f));
            InvisibleBlock("Bound S", new Vector3(0f, wallH * 0.5f, -half - 0.5f), new Vector3(size + 2f, wallH, 1f));
            InvisibleBlock("Bound E", new Vector3(half + 0.5f, wallH * 0.5f, 0f), new Vector3(1f, wallH, size + 2f));
            InvisibleBlock("Bound W", new Vector3(-half - 0.5f, wallH * 0.5f, 0f), new Vector3(1f, wallH, size + 2f));
            InvisibleBlock("Ceiling", new Vector3(0f, wallH, 0f), new Vector3(size + 2f, 1f, size + 2f));

            var crateColor = new Color(0.45f, 0.36f, 0.24f);
            var crateSize = new Vector3(1.6f, 1.6f, 1.6f);
            Vector3[] cratePositions =
            {
                new Vector3(5f, 0.8f, 5f),
                new Vector3(-5f, 0.8f, -5f),
                new Vector3(-7f, 0.8f, 6f),
                new Vector3(7f, 0.8f, -5f),
                new Vector3(0f, 0.8f, 10f),
                new Vector3(-2f, 0.8f, -11f)
            };
            foreach (var pos in cratePositions)
            {
                Block("Crate", pos, crateSize, crateColor);
            }
        }

        private void Block(string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = BattleMaterials.Lit(color);
        }

        private void InvisibleBlock(string name, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            Destroy(go.GetComponent<Renderer>());
        }
    }
}
