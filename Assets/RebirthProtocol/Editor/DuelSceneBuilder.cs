using System.IO;
using RebirthProtocol.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RebirthProtocol.Editor
{
    public static class DuelSceneBuilder
    {
        public const string DuelScenePath = "Assets/RebirthProtocol/Scenes/Duel.unity";

        // The scene is deliberately minimal: one bootstrap object. DuelManager
        // builds the whole duel from code at runtime, so gameplay iteration
        // never requires editing scene YAML.
        [MenuItem("Rebirth Protocol/Scenes/Create Duel Scene")]
        public static void CreateDuelScene()
        {
            if (File.Exists(DuelScenePath))
            {
                return;
            }

            Directory.CreateDirectory("Assets/RebirthProtocol/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Duel";

            var bootstrap = new GameObject("Duel");
            bootstrap.AddComponent<DuelManager>();

            EditorSceneManager.SaveScene(scene, DuelScenePath);
        }
    }
}
