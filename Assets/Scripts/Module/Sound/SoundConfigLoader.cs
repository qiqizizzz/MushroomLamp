/*
* ┌──────────────────────────────────┐
* │  描    述: 声音配置读取器，负责音频 id 与路径映射查询
* │  类    名: SoundConfigLoader.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common;
using Common.Defines;

namespace Sound
{
    public static class SoundConfigLoader
    {
        private static SoundCatalogJsonConfig _catalog;
        private static Dictionary<string, SoundClipJsonData> _clipsById;
        private static Dictionary<string, SoundViewBindingJsonData> _viewsByName;

        // 加载声音配置目录
        public static SoundCatalogJsonConfig LoadCatalog()
        {
            if (_catalog != null) return _catalog;

            _catalog = JsonConfigLoader.LoadFromConfig<SoundCatalogJsonConfig>(AddressDefines.Config_SoundCatalog);
            if (_catalog == null)
                _catalog = new SoundCatalogJsonConfig();

            buildIndex();
            return _catalog;
        }

        // 重新加载声音配置
        public static void Reload()
        {
            _catalog = null;
            _clipsById = null;
            _viewsByName = null;
            LoadCatalog();
        }

        // 查询音频 id，找不到时按原始 Resources/Sounds 路径兜底
        public static bool TryResolveClip(string idOrPath, out SoundClipResolveData result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(idOrPath)) return false;

            LoadCatalog();
            if (_clipsById != null && _clipsById.TryGetValue(idOrPath, out SoundClipJsonData clipData))
            {
                if (string.IsNullOrWhiteSpace(clipData.path)) return false;

                result = new SoundClipResolveData
                {
                    Id = clipData.id,
                    Path = normalizeSoundPath(clipData.path),
                    VolumeScale = normalizeVolume(clipData.volume)
                };
                return true;
            }

            result = new SoundClipResolveData
            {
                Id = idOrPath,
                Path = normalizeSoundPath(idOrPath),
                VolumeScale = 1f
            };
            return true;
        }

        // 获取普通界面 BGM 轮播列表
        public static string[] GetBgmPlaylist(string[] fallback)
        {
            LoadCatalog();
            string[] playlist = _catalog?.defaults?.bgmPlaylist;
            return playlist != null && playlist.Length > 0 ? playlist : fallback;
        }

        // 获取烹饪玩法 BGM
        public static string GetGameplayBgm(string fallback)
        {
            LoadCatalog();
            string bgm = _catalog?.defaults?.gameplayBgm;
            return string.IsNullOrWhiteSpace(bgm) ? fallback : bgm;
        }

        // 获取全局默认按钮点击音效
        public static string GetDefaultButtonClick()
        {
            LoadCatalog();
            return _catalog?.defaults?.buttonClick;
        }

        // 获取全局默认按钮悬停音效
        public static string GetDefaultButtonHover()
        {
            LoadCatalog();
            return _catalog?.defaults?.buttonHover;
        }

        // 按 View 类名获取声音绑定配置
        public static SoundViewBindingJsonData GetViewBinding(string viewName)
        {
            LoadCatalog();
            if (string.IsNullOrEmpty(viewName) || _viewsByName == null) return null;
            return _viewsByName.TryGetValue(viewName, out SoundViewBindingJsonData binding) ? binding : null;
        }

        // 查询 View 内指定按钮路径的覆盖配置
        public static SoundButtonBindingJsonData FindButtonBinding(SoundViewBindingJsonData viewBinding, string buttonPath)
        {
            if (viewBinding?.buttons == null || string.IsNullOrEmpty(buttonPath)) return null;

            foreach (SoundButtonBindingJsonData binding in viewBinding.buttons)
            {
                if (binding == null || string.IsNullOrEmpty(binding.path)) continue;
                if (binding.path == buttonPath)
                    return binding;
            }

            return null;
        }

        private static void buildIndex()
        {
            _clipsById = new Dictionary<string, SoundClipJsonData>();
            SoundClipJsonData[] clips = _catalog?.clips;
            if (clips != null)
            {
                foreach (SoundClipJsonData clip in clips)
                {
                    if (clip == null || string.IsNullOrWhiteSpace(clip.id)) continue;
                    _clipsById[clip.id] = clip;
                }
            }

            _viewsByName = new Dictionary<string, SoundViewBindingJsonData>();
            SoundViewBindingJsonData[] views = _catalog?.viewBindings;
            if (views == null) return;

            foreach (SoundViewBindingJsonData view in views)
            {
                if (view == null || string.IsNullOrWhiteSpace(view.view)) continue;
                _viewsByName[view.view] = view;
            }
        }

        private static float normalizeVolume(float volume)
        {
            return volume <= 0f ? 1f : volume;
        }

        private static string normalizeSoundPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;

            string result = path.Trim();
            if (result.StartsWith("Sounds/"))
                result = result.Substring("Sounds/".Length);

            string ext = System.IO.Path.GetExtension(result);
            if (!string.IsNullOrEmpty(ext))
            {
                string lower = ext.ToLowerInvariant();
                if (lower == ".wav" || lower == ".mp3" || lower == ".ogg" || lower == ".aif" || lower == ".aiff")
                    result = result.Substring(0, result.Length - ext.Length);
            }

            return result;
        }
    }
}
