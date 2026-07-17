using System.IO;
using UnityEditor;
using UnityEngine;

namespace RebirthProtocol.Editor
{
    // The Duel scene builds everything from code, so nothing references
    // KnightRobot_Body.prefab from an included scene/asset -- Unity would
    // strip it out of a player build. Copying it into a Resources/ folder
    // makes it loadable at runtime via Resources.Load. This is a one-time
    // duplicate, not a link: if Codex's asset pipeline regenerates the
    // source prefab later (KnightRobotAssetBuilder.BuildKnightRobotPrefabs),
    // this copy needs re-running to pick up the change.
    public static class CobaltKnightResourceLinker
    {
        private const string SourcePrefab = "Assets/RebirthProtocol/Art/Mechs/KnightRobot/Prefabs/KnightRobot_Body.prefab";
        private const string ResourceDir = "Assets/RebirthProtocol/Resources/Mechs";
        private const string ResourcePrefab = ResourceDir + "/CobaltKnightBody.prefab";

        [MenuItem("Rebirth Protocol/Assets/Link Cobalt Knight Into Resources")]
        public static void CreateResourceCopy()
        {
            if (!AssetDatabase.IsValidFolder("Assets/RebirthProtocol/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/RebirthProtocol", "Resources");
            }

            if (!AssetDatabase.IsValidFolder(ResourceDir))
            {
                AssetDatabase.CreateFolder("Assets/RebirthProtocol/Resources", "Mechs");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ResourcePrefab) != null)
            {
                AssetDatabase.DeleteAsset(ResourcePrefab);
            }

            if (!AssetDatabase.CopyAsset(SourcePrefab, ResourcePrefab))
            {
                throw new System.InvalidOperationException($"Failed to copy {SourcePrefab} to {ResourcePrefab}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Copied {SourcePrefab} -> {ResourcePrefab}");
        }
    }
}
