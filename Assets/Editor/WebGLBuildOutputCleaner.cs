/*
* ┌──────────────────────────────────┐
* │  描    述: WebGL 构建输出清理，移除未使用的 AA 输出目录
* │  类    名: WebGLBuildOutputCleaner.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// WebGL 构建输出清理，避免未使用的 Addressables 文件进入最终上传包
public sealed class WebGLBuildOutputCleaner : IPostprocessBuildWithReport
{
    public int callbackOrder => 1000;

    // WebGL 构建完成后清理输出目录
    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL)
            return;

        string outputPath = report.summary.outputPath;
        if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
            return;

        deleteAddressablesOutput(outputPath);
    }

    // 删除 WebGL 输出目录中残留的 Addressables 本地包
    private static void deleteAddressablesOutput(string outputPath)
    {
        string addressablesPath = Path.Combine(outputPath, "StreamingAssets", "aa");
        if (!Directory.Exists(addressablesPath))
            return;

        Directory.Delete(addressablesPath, true);

#if UNITY_EDITOR
        UnityEngine.Debug.Log($"[{nameof(WebGLBuildOutputCleaner)}] 已清理 WebGL 输出目录：{addressablesPath}");
#endif
    }
}
