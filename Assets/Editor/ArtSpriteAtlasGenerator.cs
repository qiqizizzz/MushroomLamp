/*
* ┌──────────────────────────────────┐
* │  描    述: Art 美术图集生成器，负责维护 WebGL 小图资源图集
* │  类    名: ArtSpriteAtlasGenerator.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

// Art 美术图集生成器，排除背景大图后按界面归组打包
public sealed class ArtSpriteAtlasGenerator : IPreprocessBuildWithReport
{
    private const string ATLAS_ROOT = "Assets/Art/Atlases";
    private const string RESOURCES_ATLAS_ROOT = "Assets/Resources/Art/Atlases";
    private const int DEFAULT_MAX_TEXTURE_SIZE = 4096;
    private const int SMALL_MAX_TEXTURE_SIZE = 2048;
    private const int COMPRESSION_QUALITY = 50;

    private static readonly AtlasConfig[] S_AtlasConfigs =
    {
        new AtlasConfig("ArtCards", DEFAULT_MAX_TEXTURE_SIZE, new[] { "Assets/Art/Card_img" }),
        new AtlasConfig("ArtCookButtons", DEFAULT_MAX_TEXTURE_SIZE, new[] { "Assets/Art/CookView/UI", "Assets/Art/Button" }),
        new AtlasConfig("ArtMenuSummary", DEFAULT_MAX_TEXTURE_SIZE, new[] { "Assets/Art/MainMenuView", "Assets/Art/SummaryView" }, "Background", "背景"),
        new AtlasConfig("ArtShopStore", DEFAULT_MAX_TEXTURE_SIZE, new[] { "Assets/Art/ShopView", "Assets/Art/StoreView" }, "Background", "背景", "预览"),
        new AtlasConfig("ArtRecycleBlackjack", DEFAULT_MAX_TEXTURE_SIZE, new[] { "Assets/Art/RecycleView", "Assets/Art/BlackjackView" }, "Background", "背景")
    };

    public int callbackOrder => 900;

    // WebGL 构建前自动刷新小图图集
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL)
            return;

        GenerateArtAtlases();
    }

    // 手动刷新 Art 小图图集
    [MenuItem("Tools/MushroomLamp/刷新 Art 图集")]
    public static void GenerateArtAtlases()
    {
        Directory.CreateDirectory(ATLAS_ROOT);

        foreach (AtlasConfig config in S_AtlasConfigs)
            createOrUpdateAtlas(config);

        optimizeExistingAtlas("Assets/Art/CookView/CookView.spriteatlas", SMALL_MAX_TEXTURE_SIZE);
        optimizeExistingAtlasesInFolder(RESOURCES_ATLAS_ROOT);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

#if UNITY_EDITOR
        Debug.Log($"[{nameof(ArtSpriteAtlasGenerator)}] Art 图集已刷新");
#endif
    }

    // 创建或更新指定图集
    private static void createOrUpdateAtlas(AtlasConfig config)
    {
        string atlasPath = $"{ATLAS_ROOT}/{config.Name}.spriteatlas";
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        if (atlas == null)
        {
            atlas = new SpriteAtlas();
            AssetDatabase.CreateAsset(atlas, atlasPath);
        }

        UnityEngine.Object[] oldPackables = SpriteAtlasExtensions.GetPackables(atlas);
        if (oldPackables.Length > 0)
            SpriteAtlasExtensions.Remove(atlas, oldPackables);

        UnityEngine.Object[] textures = collectTextures(config);
        if (textures.Length > 0)
            SpriteAtlasExtensions.Add(atlas, textures);

        applyAtlasSettings(atlas, config.MaxTextureSize);
        EditorUtility.SetDirty(atlas);
    }

    // 收集图集需要包含的非背景小图
    private static UnityEngine.Object[] collectTextures(AtlasConfig config)
    {
        List<UnityEngine.Object> textures = new List<UnityEngine.Object>();
        HashSet<string> added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string folder in config.Folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                continue;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (shouldSkipTexture(path, config.ExcludeKeywords) || !added.Add(path))
                    continue;

                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                    textures.Add(texture);
            }
        }

        textures.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        return textures.ToArray();
    }

    // 判断纹理是否应该排除出图集
    private static bool shouldSkipTexture(string path, string[] excludeKeywords)
    {
        string extension = Path.GetExtension(path);
        if (!string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            return true;

        string fileName = Path.GetFileNameWithoutExtension(path);
        foreach (string keyword in excludeKeywords)
        {
            if (fileName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    // 优化已存在的单个图集
    private static void optimizeExistingAtlas(string atlasPath, int maxTextureSize)
    {
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        if (atlas == null)
            return;

        applyAtlasSettings(atlas, maxTextureSize);
        EditorUtility.SetDirty(atlas);
    }

    // 优化目录下已有的图集
    private static void optimizeExistingAtlasesInFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
            return;

        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            optimizeExistingAtlas(path, SMALL_MAX_TEXTURE_SIZE);
        }
    }

    // 应用 WebGL 纹理压缩与图集打包配置
    private static void applyAtlasSettings(SpriteAtlas atlas, int maxTextureSize)
    {
        SpriteAtlasPackingSettings packingSettings = SpriteAtlasExtensions.GetPackingSettings(atlas);
        packingSettings.enableRotation = false;
        packingSettings.enableTightPacking = false;
        packingSettings.enableAlphaDilation = false;
        packingSettings.padding = 4;
        SpriteAtlasExtensions.SetPackingSettings(atlas, packingSettings);

        SpriteAtlasTextureSettings textureSettings = SpriteAtlasExtensions.GetTextureSettings(atlas);
        textureSettings.generateMipMaps = false;
        textureSettings.readable = false;
        textureSettings.sRGB = true;
        textureSettings.filterMode = FilterMode.Bilinear;
        SpriteAtlasExtensions.SetTextureSettings(atlas, textureSettings);

        TextureImporterPlatformSettings platformSettings = new TextureImporterPlatformSettings
        {
            name = "WebGL",
            overridden = true,
            maxTextureSize = maxTextureSize,
            textureCompression = TextureImporterCompression.Compressed,
            compressionQuality = COMPRESSION_QUALITY,
            crunchedCompression = true
        };
        SpriteAtlasExtensions.SetPlatformSettings(atlas, platformSettings);
    }

    private readonly struct AtlasConfig
    {
        public readonly string Name;
        public readonly int MaxTextureSize;
        public readonly string[] Folders;
        public readonly string[] ExcludeKeywords;

        public AtlasConfig(string name, int maxTextureSize, string[] folders, params string[] excludeKeywords)
        {
            Name = name;
            MaxTextureSize = maxTextureSize;
            Folders = folders;
            ExcludeKeywords = excludeKeywords ?? Array.Empty<string>();
        }
    }
}

