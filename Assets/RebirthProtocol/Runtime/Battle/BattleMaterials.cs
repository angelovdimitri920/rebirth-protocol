using UnityEngine;

namespace RebirthProtocol.Battle
{
    // Shared runtime material helpers for the primitive-visual slice.
    public static class BattleMaterials
    {
        private static Shader _lit;
        private static Shader _unlit;

        public static Material Lit(Color color)
        {
            _lit ??= Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(_lit);
            mat.color = color;
            return mat;
        }

        public static Material Unlit(Color color)
        {
            // Runtime-created materials can only use shaders that shipped in
            // the build. URP/Unlit gets stripped (no asset references it), so
            // fall back to URP/Lit — always present via the default primitive
            // material. Editor play mode would find any shader; players don't.
            _unlit ??= Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");
            var mat = new Material(_unlit);
            mat.color = color;
            return mat;
        }
    }
}
