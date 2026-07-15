using System.IO;
using RebirthProtocol.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RebirthProtocol.Editor
{
    public static class RebirthProjectSetup
    {
        private const string BootstrapScenePath = "Assets/RebirthProtocol/Scenes/Bootstrap.unity";

        public static void ConfigureProject()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            PlayerSettings.companyName = "Rebirth Protocol";
            PlayerSettings.productName = "Rebirth Protocol";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.rebirthprotocol.game");
            SetActiveInputHandling();
            EnsureBootstrapScene();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true)
            };
            AssetDatabase.SaveAssets();
        }

        public static void EnsureBootstrapScene()
        {
            Directory.CreateDirectory("Assets/RebirthProtocol/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Bootstrap";

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 7f, -9f);
            camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.025f, 0.03f);

            var lightObject = new GameObject("Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            var arena = GameObject.CreatePrimitive(PrimitiveType.Plane);
            arena.name = "Neutral Arena Floor";
            arena.transform.localScale = new Vector3(2.5f, 1f, 2.5f);

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Input Smoke Player";
            player.transform.position = new Vector3(0f, 1f, 0f);
            var probe = player.AddComponent<InputSmokeProbe>();

            var canvasObject = new GameObject("HUD Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var labelObject = new GameObject("Input Status");
            labelObject.transform.SetParent(canvasObject.transform, false);
            var label = labelObject.AddComponent<Text>();
            label.text = "Move 0.00, 0.00 | Dash False";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.color = Color.white;
            label.alignment = TextAnchor.UpperLeft;
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(16f, -16f);
            rect.sizeDelta = new Vector2(480f, 40f);
            probe.SetStatusLabel(label);

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            SceneManager.SetActiveScene(scene);
        }

        private static void SetActiveInputHandling()
        {
            var property = typeof(PlayerSettings).GetProperty("activeInputHandling");
            if (property == null)
            {
                return;
            }

            var enumValue = System.Enum.Parse(property.PropertyType, "InputSystemPackage");
            property.SetValue(null, enumValue);
        }
    }
}
