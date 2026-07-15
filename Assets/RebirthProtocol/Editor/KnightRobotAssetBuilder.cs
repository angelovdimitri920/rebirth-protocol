using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RebirthProtocol.Editor
{
    public static class KnightRobotAssetBuilder
    {
        private const string Root = "Assets/RebirthProtocol/Art/Mechs/KnightRobot";
        private const string FbxFolder = Root + "/FBX";
        private const string MaterialFolder = Root + "/Materials";
        private const string PrefabFolder = Root + "/Prefabs";

        private static readonly string[] FbxNames =
        {
            "KnightRobot_Loadout",
            "KnightRobot_Body",
            "KnightRobot_BasicGun",
            "KnightRobot_BasicBomb",
            "KnightRobot_BasicLegs",
            "KnightRobot_BasicPod"
        };

        [MenuItem("Rebirth Protocol/Assets/Build Knight Robot Prefabs")]
        public static void BuildKnightRobotPrefabs()
        {
            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory(PrefabFolder);

            var materials = EnsureMaterials();
            foreach (var fbxName in FbxNames)
            {
                ConfigureModelImporter($"{FbxFolder}/{fbxName}.fbx");
                BuildPrefab(fbxName, materials);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigureModelImporter(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                throw new InvalidOperationException($"Expected a ModelImporter at {path}");
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.optimizeGameObjects = false;
            importer.SaveAndReimport();
        }

        private static Dictionary<string, Material> EnsureMaterials()
        {
            return new Dictionary<string, Material>
            {
                ["dark"] = CreateOrUpdateMaterial("KR_Mat_DarkIron", new Color(0.035f, 0.04f, 0.045f), 0.85f, 0.68f),
                ["steel"] = CreateOrUpdateMaterial("KR_Mat_BurnishedSteel", new Color(0.48f, 0.51f, 0.52f), 0.75f, 0.73f),
                ["ivory"] = CreateOrUpdateMaterial("KR_Mat_IvoryCeramic", new Color(0.82f, 0.78f, 0.66f), 0.18f, 0.62f),
                ["cobalt"] = CreateOrUpdateMaterial("KR_Mat_CobaltArmor", new Color(0.045f, 0.13f, 0.42f), 0.42f, 0.66f),
                ["aurum"] = CreateOrUpdateMaterial("KR_Mat_AurumCore", new Color(0.95f, 0.63f, 0.08f), 0.58f, 0.73f),
                ["blue"] = CreateOrUpdateMaterial("KR_Mat_RoyalBlueGlow", new Color(0.05f, 0.22f, 0.85f), 0.2f, 0.82f, new Color(0f, 0.28f, 1f)),
                ["ember"] = CreateOrUpdateMaterial("KR_Mat_EmberCore", new Color(1f, 0.28f, 0.06f), 0.1f, 0.76f, new Color(1f, 0.22f, 0.02f)),
                ["joint"] = CreateOrUpdateMaterial("KR_Mat_RubberJoint", new Color(0.012f, 0.013f, 0.014f), 0.2f, 0.28f),
                ["copper"] = CreateOrUpdateMaterial("KR_Mat_CopperMuzzle", new Color(0.86f, 0.42f, 0.18f), 0.65f, 0.69f),
                ["teal"] = CreateOrUpdateMaterial("KR_Mat_PodTeal", new Color(0.02f, 0.55f, 0.62f), 0.35f, 0.76f, new Color(0f, 0.55f, 0.72f))
            };
        }

        private static Material CreateOrUpdateMaterial(string name, Color baseColor, float metallic, float smoothness, Color? emission = null)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(FindLitShader());
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = name;
            SetColor(material, "_BaseColor", "_Color", baseColor);
            SetFloat(material, "_Metallic", metallic);
            SetFloat(material, "_Smoothness", smoothness);
            SetFloat(material, "_Glossiness", smoothness);

            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", emission.Value);
                }
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", Color.black);
                }
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Shader FindLitShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                   ?? Shader.Find("Standard")
                   ?? throw new InvalidOperationException("Could not find a Lit or Standard shader.");
        }

        private static void SetColor(Material material, string preferredProperty, string fallbackProperty, Color value)
        {
            if (material.HasProperty(preferredProperty))
            {
                material.SetColor(preferredProperty, value);
            }
            else if (material.HasProperty(fallbackProperty))
            {
                material.SetColor(fallbackProperty, value);
            }
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static void BuildPrefab(string fbxName, IReadOnlyDictionary<string, Material> materials)
        {
            var fbxPath = $"{FbxFolder}/{fbxName}.fbx";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (source == null)
            {
                throw new InvalidOperationException($"Could not load FBX GameObject at {fbxPath}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(source);
            }

            instance.name = fbxName;
            AssignProjectMaterials(instance, materials);
            ConfigurePrefabRoot(instance, fbxName);

            var prefabPath = $"{PrefabFolder}/{fbxName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void AssignProjectMaterials(GameObject root, IReadOnlyDictionary<string, Material> materials)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var shared = renderer.sharedMaterials;
                for (var i = 0; i < shared.Length; i++)
                {
                    shared[i] = ResolveMaterial(shared[i]?.name ?? renderer.name, materials);
                }

                renderer.sharedMaterials = shared;
            }
        }

        private static Material ResolveMaterial(string materialName, IReadOnlyDictionary<string, Material> materials)
        {
            var name = materialName.ToLowerInvariant();
            if (name.Contains("ivory"))
            {
                return materials["ivory"];
            }
            if (name.Contains("cobalt"))
            {
                return materials["cobalt"];
            }
            if (name.Contains("aurum"))
            {
                return materials["aurum"];
            }
            if (name.Contains("royalblue") || name.Contains("blue"))
            {
                return materials["blue"];
            }
            if (name.Contains("ember"))
            {
                return materials["ember"];
            }
            if (name.Contains("rubber") || name.Contains("joint"))
            {
                return materials["joint"];
            }
            if (name.Contains("copper") || name.Contains("muzzle"))
            {
                return materials["copper"];
            }
            if (name.Contains("podteal") || name.Contains("teal"))
            {
                return materials["teal"];
            }
            if (name.Contains("dark"))
            {
                return materials["dark"];
            }

            return materials["steel"];
        }

        private static void ConfigurePrefabRoot(GameObject instance, string fbxName)
        {
            if (instance.GetComponent<Animator>() == null)
            {
                instance.AddComponent<Animator>();
            }

            if (fbxName == "KnightRobot_Loadout" && instance.GetComponent<CapsuleCollider>() == null)
            {
                var capsule = instance.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0f, 1.05f, 0f);
                capsule.height = 2.2f;
                capsule.radius = 0.55f;
            }
        }
    }
}
