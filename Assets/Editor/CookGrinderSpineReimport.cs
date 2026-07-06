#if UNITY_EDITOR
using Spine.Unity;
using Spine.Unity.Editor;
using UnityEditor;
using UnityEngine;

public static class CookGrinderSpineReimport
{
    private static readonly string[] SkeletonDataPaths =
    {
        "Assets/Art/Spine/CookGrinderEffect/CookGrinderEffect_SkeletonData.asset",
        "Assets/Art/Spine/ShopCrow/ShopCrow_SkeletonData.asset",
        "Assets/Art/Spine/MainMenuBegin/MainMenuBegin_SkeletonData.asset",
        "Assets/Art/Spine/CookPageAngel/CookPageAngel_converted_SkeletonData.asset",
        "Assets/Art/Spine/CookPageDevil/CookPageDevil_converted_SkeletonData.asset",
        "Assets/Art/Spine/CookPageSteam/CookPageSteam_SkeletonData.asset",
        "Assets/Art/Spine/CookScrollEffect/CookScrollEffect_SkeletonData.asset",
    };

    [MenuItem("Tools/Spine/Reimport CookGrinderEffect")]
    public static void ReimportCookGrinderEffect()
    {
        ReimportSkeleton(SkeletonDataPaths[0]);
    }

    [MenuItem("Tools/Spine/Reimport All Cook Spine")]
    public static void ReimportAll()
    {
        foreach (string path in SkeletonDataPaths)
            ReimportSkeleton(path);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CookGrinderSpineReimport] 已重新导入全部 Cook 相关 Spine 资源");
    }

    private static void ReimportSkeleton(string skeletonDataPath)
    {
        SkeletonDataAsset skeletonData = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(skeletonDataPath);
        if (skeletonData == null)
        {
            Debug.LogWarning($"[CookGrinderSpineReimport] 未找到 {skeletonDataPath}");
            return;
        }

        string folder = System.IO.Path.GetDirectoryName(skeletonDataPath)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(folder)) return;

        foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { folder }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (assetPath.EndsWith(".png") || assetPath.EndsWith(".json") || assetPath.EndsWith(".atlas.txt"))
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        SpineEditorUtilities.ReloadSkeletonDataAsset(skeletonData, true);
    }
}
#endif
