/*
 * ┌──────────────────────────────────┐
 * │  描    述: Art 目录资源加载（非 Resources）
 * │  类    名: ArtAssetLoader.cs
 * └──────────────────────────────────┘
 */

using System.IO;
using UnityEngine;

namespace Common
{
    public static class ArtAssetLoader
    {
        // 路径相对 Assets 根目录，如 Art/Card_img/carrot（不含扩展名）
        public static Sprite LoadSprite(string assetsRelativePath)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativePath))
                return null;

#if UNITY_EDITOR
            return loadFromAssetDatabase(assetsRelativePath);
#else
            return loadFromStreamingAssets(assetsRelativePath);
#endif
        }

#if UNITY_EDITOR
        private static Sprite loadFromAssetDatabase(string assetsRelativePath)
        {
            string assetPath = assetsRelativePath.StartsWith("Assets/")
                ? assetsRelativePath
                : $"Assets/{assetsRelativePath}";

            if (!hasImageExtension(assetPath))
                assetPath += ".png";

            Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
                QLog.Error($"[{nameof(ArtAssetLoader)}] 未找到 Art 资源：{assetPath}");

            return sprite;
        }
#endif

        private static Sprite loadFromStreamingAssets(string assetsRelativePath)
        {
            string relativePath = assetsRelativePath.StartsWith("Assets/")
                ? assetsRelativePath.Substring("Assets/".Length)
                : assetsRelativePath;

            string filePath = Path.Combine(Application.streamingAssetsPath, relativePath);
            if (!hasImageExtension(filePath))
                filePath += ".png";

            if (!File.Exists(filePath))
            {
                QLog.Error($"[{nameof(ArtAssetLoader)}] 打包后请将 Art 同步到 StreamingAssets：{filePath}");
                return null;
            }

            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                QLog.Error($"[{nameof(ArtAssetLoader)}] 图片解码失败：{filePath}");
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }

        private static bool hasImageExtension(string path)
        {
            string ext = Path.GetExtension(path);
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
        }
    }
}
