using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CloudRail.Editor
{
    public static class CloudRailPlayerBuilder
    {
        public const string MacOutput="Builds/macOS/CloudRail.app";
        [MenuItem("Cloud Rail/Build macOS Release (Universal)")]
        public static void BuildMac()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before building.");
            Directory.CreateDirectory("Builds/macOS");Directory.CreateDirectory("Builds/Reports");
            PlayerSettings.companyName="Hao2048";PlayerSettings.productName="CloudRail";
            PlayerSettings.bundleVersion="0.1.0";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"io.github.hao2048.cloudrail");
            EditorUserBuildSettings.SetPlatformSettings("Standalone","OSXUniversal","Architecture",UnityEditor.Build.OSArchitecture.x64ARM64.ToString());
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(CloudRailBuilder.VisualScenePath,true)};
            AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{CloudRailBuilder.VisualScenePath},locationPathName=MacOutput,target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
            var s=report.summary;
            File.WriteAllText("Builds/Reports/macOS-build.json",JsonUtility.ToJson(new BuildResult{result=s.result.ToString(),errors=s.totalErrors,warnings=s.totalWarnings,bytes=s.totalSize,seconds=s.totalTime.TotalSeconds,output=s.outputPath},true));
            if(s.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("macOS build failed: "+s.result);
        }
        [Serializable] sealed class BuildResult{public string result,output;public int errors,warnings;public ulong bytes;public double seconds;}
        public static string StartBuild(){EditorApplication.delayCall+=BuildMac;return "macOS Universal build scheduled; inspect Builds/Reports/macOS-build.json";}
    }
}
