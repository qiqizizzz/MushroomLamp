/*
* ┌──────────────────────────────────┐
* │  描    述: Art 目录资源加载（基于 Resources）
* │  类    名: ArtAssetLoader.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    public static class ArtAssetLoader
    {
        // 已加载图片缓存，按 Resources 路径复用
        private static readonly Dictionary<string, Sprite> _sprites = new();

        // 路径相对 Assets 根目录，如 Art/Card_img/carrot（不含扩展名）
        // logOnFail=false 时静默失败（用于“有就用、没有就白膜”的可选美术，避免污染日志）
        public static Sprite LoadSprite(string assetsRelativePath, bool logOnFail = true)
        {
            string address = normalizeAddress(assetsRelativePath);
            if (string.IsNullOrEmpty(address))
                return null;

            if (_sprites.TryGetValue(address, out Sprite cached))
                return cached;

            Sprite sprite = Resources.Load<Sprite>(address);
            if (sprite == null)
            {
                if (logOnFail) QLog.Error($"[{nameof(ArtAssetLoader)}] Resources 图片加载失败：Resources/{address}");
                return null;
            }

            _sprites[address] = sprite;
            return sprite;
        }

        // 异步回调加载，Resources 版本立即返回结果
        public static void LoadSpriteAsync(string assetsRelativePath, Action<Sprite> onCompleted, bool logOnFail = true)
        {
            onCompleted?.Invoke(LoadSprite(assetsRelativePath, logOnFail));
        }

        // 清理图片缓存引用
        public static void ReleaseAll()
        {
            _sprites.Clear();
        }

        // 规范化为 Resources 路径：去掉前缀 Assets/Resources/、Assets/ 与图片扩展名
        private static string normalizeAddress(string assetsRelativePath)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativePath))
                return string.Empty;

            string path = assetsRelativePath.StartsWith("Assets/Resources/")
                ? assetsRelativePath.Substring("Assets/Resources/".Length)
                : assetsRelativePath.StartsWith("Assets/")
                ? assetsRelativePath.Substring("Assets/".Length)
                : assetsRelativePath;

            string ext = System.IO.Path.GetExtension(path);
            if (!string.IsNullOrEmpty(ext))
            {
                string lower = ext.ToLowerInvariant();
                if (lower == ".png" || lower == ".jpg" || lower == ".jpeg")
                    path = path.Substring(0, path.Length - ext.Length);
            }

            return path;
        }
    }
}
