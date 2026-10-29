using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TastePassport.EditorTools
{
    public static class TastePassportBuild
    {
        private const string MainScene = "Assets/_Game/Scenes/TastePassport_MVP.unity";

        [MenuItem("Tools/Taste Passport/Build Windows MVP")]
        public static void BuildWindows()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string output = Path.Combine(projectRoot, "Builds/Windows/TastePassport.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("Taste Passport Windows build ready: " + output);
        }

        [MenuItem("Tools/Taste Passport/Check Android Support")]
        public static void CheckAndroidSupport()
        {
            bool installed = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
            Debug.Log("Taste Passport Android Build Support installed: " + installed);
        }

        [MenuItem("Tools/Taste Passport/Build Android APK")]
        public static void BuildAndroid()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Install Android Build Support with SDK & NDK Tools and OpenJDK in Unity Hub first.");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Could not switch to Android build target.");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string output = Path.Combine(projectRoot, "Builds/Android/TastePassport.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + report.summary.result);
            Debug.Log("Taste Passport APK ready: " + output);
        }
    }
}
