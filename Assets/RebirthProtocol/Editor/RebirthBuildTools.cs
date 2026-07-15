using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace RebirthProtocol.Editor
{
    public static class RebirthBuildTools
    {
        private static readonly string[] Scenes =
        {
            "Assets/RebirthProtocol/Scenes/Bootstrap.unity"
        };

        public static void BuildWindowsDevelopment()
        {
            Directory.CreateDirectory("Builds/Windows");

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = "Builds/Windows/RebirthProtocolUnity.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows development build failed: {report.summary.result}");
            }
        }
    }
}
