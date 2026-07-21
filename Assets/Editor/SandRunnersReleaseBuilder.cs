using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class SandRunnersReleaseBuilder
{
    public const string ReleaseVersion = "0.1.0-rc1";
    public const string RtsScenePath = "Assets/Scenes/SampleScene.unity";
    public const string DefaultOutputPath = "Builds/SandRunners_RC_0.1.0/SandRunners.exe";

    [MenuItem("Sand Runners/Build/Build Windows RTS Release Candidate")]
    public static void BuildWindowsRtsReleaseCandidate()
    {
        ConfigureRtsReleaseProfile();
        EnsureMcpRuntimeExcludedFromStandalone();

        SandRunnersSceneValidationReport validation = SandRunnersSceneValidator.ValidateStartupScenes();
        if (!validation.Passed)
            throw new BuildFailedException("SandRunners startup validation failed. " + validation.Summary);

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
        {
            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64);
            if (!switched)
                throw new BuildFailedException("Could not switch the project to Windows Standalone x64.");
        }

        string outputPath = Path.GetFullPath(DefaultOutputPath);
        string outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrEmpty(outputDirectory))
            throw new BuildFailedException("Invalid SandRunners build output path: " + outputPath);

        Directory.CreateDirectory(outputDirectory);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { RtsScenePath },
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.StrictMode | BuildOptions.CleanBuildCache
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException(
                "SandRunners Windows build failed: " + summary.result +
                ", errors: " + summary.totalErrors +
                ", warnings: " + summary.totalWarnings);
        }

        PruneDevelopmentAssemblies(outputDirectory);
        WriteReleaseManifest(outputDirectory, summary);
        Debug.Log(
            "SandRunners Windows RTS release candidate built successfully: " + outputPath +
            " (" + FormatBytes(summary.totalSize) + ", " + summary.totalTime + ")");
    }

    [MenuItem("Sand Runners/Build/Configure RTS Release Profile")]
    public static void ConfigureRtsReleaseProfile()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(RtsScenePath, true)
        };

        PlayerSettings.companyName = "Battle For Universe";
        PlayerSettings.productName = "SandRunners";
        PlayerSettings.bundleVersion = ReleaseVersion;
        AssetDatabase.SaveAssets();
    }

    private static void EnsureMcpRuntimeExcludedFromStandalone()
    {
        string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
        string[] tokens = defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (Array.Exists(tokens, token => token.Trim() == "UNITY_MCP_READY"))
        {
            throw new BuildFailedException(
                "UNITY_MCP_READY is active for Standalone. Build from the isolated release profile so MCP runtime cannot enter the Player.");
        }
    }

    private static void PruneDevelopmentAssemblies(string outputDirectory)
    {
        string managedDirectory = Path.Combine(outputDirectory, "SandRunners_Data", "Managed");
        string[] developmentAssemblies =
        {
            "com.IvanMurzak.Unity.MCP.Runtime.dll",
            "com.IvanMurzak.Unity.MCP.TestFiles.dll",
            "McpPlugin.Common.dll",
            "McpPlugin.dll"
        };

        for (int i = 0; i < developmentAssemblies.Length; i++)
        {
            string assemblyPath = Path.Combine(managedDirectory, developmentAssemblies[i]);
            if (File.Exists(assemblyPath))
                File.Delete(assemblyPath);
        }

        string burstDebugDirectory = Path.Combine(outputDirectory, "SandRunners_BurstDebugInformation_DoNotShip");
        if (Directory.Exists(burstDebugDirectory))
            Directory.Delete(burstDebugDirectory, true);
    }

    private static void WriteReleaseManifest(string outputDirectory, BuildSummary summary)
    {
        string manifestPath = Path.Combine(outputDirectory, "RELEASE_INFO.txt");
        string[] lines =
        {
            "SandRunners Windows RTS Release Candidate",
            "Version: " + ReleaseVersion,
            "Built UTC: " + DateTime.UtcNow.ToString("u"),
            "Unity: " + Application.unityVersion,
            "Scene: " + RtsScenePath,
            "Build result: " + summary.result,
            "Build size: " + summary.totalSize + " bytes",
            "Sebek prologue: excluded pending scene repair",
            "Runtime authored-art replacement: disabled; stable gameplay visuals retained",
            "MCP runtime: excluded"
        };
        File.WriteAllLines(manifestPath, lines);
    }

    private static string FormatBytes(ulong bytes)
    {
        const double megabyte = 1024d * 1024d;
        return (bytes / megabyte).ToString("0.0") + " MB";
    }
}