using UnityEditor;
using UnityEditor.Build.Reporting;

/// <summary>
/// Command-line build entry point:
/// Unity.exe -batchmode -quit -projectPath <path> -executeMethod BuildScript.BuildWin64
/// </summary>
public static class BuildScript
{
    public static void BuildWin64()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MainScene.unity" },
            locationPathName = "Builds/CyberEscapeRunner/CyberEscapeRunner.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        UnityEngine.Debug.Log($"[BuildScript] result={report.summary.result}, size={report.summary.totalSize} bytes");

        if (report.summary.result != BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
        }
    }
}
