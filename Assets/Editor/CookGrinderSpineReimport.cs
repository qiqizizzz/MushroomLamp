#if UNITY_EDITOR
using Spine.Unity;
using Spine.Unity.Editor;
using UnityEditor;
using UnityEngine;

public static class CookGrinderSpineReimport
{
    private const string JsonPath = "Assets/Art/Spine/CookGrinderEffect/CookGrinderEffect.json";
    private const string AtlasPath = "Assets/Art/Spine/CookGrinderEffect/CookGrinderEffect.atlas.txt";
    private const string PngPath = "Assets/Art/Spine/CookGrinderEffect/CookGrinderEffect.png";
    private const string SkeletonDataPath = "Assets/Art/Spine/CookGrinderEffect/CookGrinderEffect_SkeletonData.asset";

    [MenuItem("Tools/Spine/Reimport CookGrinderEffect")]
    public static void Reimport()
    {
        AssetDatabase.ImportAsset(PngPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(JsonPath, ImportAssetOptions.ForceUpdate);

        SkeletonDataAsset skeletonData = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(SkeletonDataPath);
        if (skeletonData != null)
            SpineEditorUtilities.ReloadSkeletonDataAsset(skeletonData, true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CookGrinderSpineReimport] CookGrinderEffect 已重新导入");
    }
}
#endif
