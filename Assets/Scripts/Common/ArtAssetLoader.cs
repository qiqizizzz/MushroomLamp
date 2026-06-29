/*
 * ┌──────────────────────────────────┐
 * │  描    述: Art 目录资源加载（基于 Addressables）
 * │  类    名: ArtAssetLoader.cs
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Common
{
    public static class ArtAssetLoader
    {
        // 已加载句柄缓存，按 address 复用，避免重复加载与句柄泄漏
        private static readonly Dictionary<string, AsyncOperationHandle<Sprite>> _handles = new();

        // 已知 key 是否存在的缓存：true=存在，false=不存在（避免重复探测）
        private static readonly Dictionary<string, bool> _keyExists = new();

        // 路径相对 Assets 根目录，如 Art/Card_img/carrot（不含扩展名）
        // 该路径即为资源的 Addressable address
        // logOnFail=false 时静默失败（用于“有就用、没有就白膜”的可选美术，避免污染日志）
        public static Sprite LoadSprite(string assetsRelativePath, bool logOnFail = true)
        {
            string address = normalizeAddress(assetsRelativePath);
            if (string.IsNullOrEmpty(address))
                return null;

            if (_handles.TryGetValue(address, out AsyncOperationHandle<Sprite> cached))
                return cached.IsValid() ? cached.Result : null;

            // 先探测 key 是否存在：不存在则直接返回 null，绝不调用 LoadAssetAsync，
            // 从源头避免 Addressables 抛 InvalidKeyException 并刷屏控制台
            if (!KeyExists(address))
            {
                if (logOnFail) QLog.Error($"[{nameof(ArtAssetLoader)}] Addressable 资源不存在：{address}");
                return null;
            }

            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);
            handle.WaitForCompletion();

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                if (logOnFail) QLog.Error($"[{nameof(ArtAssetLoader)}] Addressable 加载失败：{address}");
                if (handle.IsValid()) Addressables.Release(handle);
                return null;
            }

            _handles[address] = handle;
            return handle.Result;
        }

        // 异步加载（推荐：不阻塞主线程）
        public static void LoadSpriteAsync(string assetsRelativePath, Action<Sprite> onCompleted, bool logOnFail = true)
        {
            string address = normalizeAddress(assetsRelativePath);
            if (string.IsNullOrEmpty(address))
            {
                onCompleted?.Invoke(null);
                return;
            }

            if (_handles.TryGetValue(address, out AsyncOperationHandle<Sprite> cached))
            {
                onCompleted?.Invoke(cached.IsValid() ? cached.Result : null);
                return;
            }

            if (!KeyExists(address))
            {
                if (logOnFail) QLog.Error($"[{nameof(ArtAssetLoader)}] Addressable 资源不存在：{address}");
                onCompleted?.Invoke(null);
                return;
            }

            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);
            handle.Completed += op =>
            {
                if (op.Status != AsyncOperationStatus.Succeeded || op.Result == null)
                {
                    if (logOnFail) QLog.Error($"[{nameof(ArtAssetLoader)}] Addressable 异步加载失败：{address}");
                    if (op.IsValid())
                        Addressables.Release(op);
                    onCompleted?.Invoke(null);
                    return;
                }

                _handles[address] = op;
                onCompleted?.Invoke(op.Result);
            };
        }

        // 探测 address 是否为有效的 Addressable key（带缓存）；不会触发资源加载异常
        private static bool KeyExists(string address)
        {
            if (_keyExists.TryGetValue(address, out bool exists))
                return exists;

            bool result = false;
            try
            {
                AsyncOperationHandle<IList<IResourceLocation>> locHandle =
                    Addressables.LoadResourceLocationsAsync(address, typeof(Sprite));
                locHandle.WaitForCompletion();
                result = locHandle.Status == AsyncOperationStatus.Succeeded
                         && locHandle.Result != null && locHandle.Result.Count > 0;
                if (locHandle.IsValid()) Addressables.Release(locHandle);
            }
            catch (Exception)
            {
                result = false;
            }

            _keyExists[address] = result;
            return result;
        }

        // 释放所有缓存的资源句柄
        public static void ReleaseAll()
        {
            foreach (var kv in _handles)
            {
                if (kv.Value.IsValid())
                    Addressables.Release(kv.Value);
            }

            _handles.Clear();
        }

        // 规范化为 address：去掉前缀 Assets/ 与图片扩展名
        private static string normalizeAddress(string assetsRelativePath)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativePath))
                return string.Empty;

            string path = assetsRelativePath.StartsWith("Assets/")
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
