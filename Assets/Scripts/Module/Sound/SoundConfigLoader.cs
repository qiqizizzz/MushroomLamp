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
using UnityEngine;

namespace Sound
{
    public static class SoundConfigLoader
    {
        private static SoundCatalogJsonConfig _catalog;
        private static Dictionary<string, SoundClipJsonData> _clipsById;
        private static Dictionary<string, List<string>> _clipVariantsByBaseId;
        private static Dictionary<string, SoundBgmJsonData> _bgmsById;
        private static Dictionary<string, SoundViewBindingJsonData> _viewsByName;

        public static SoundCatalogJsonConfig LoadCatalog()
        {
            if (_catalog != null) return _catalog;

            _catalog = JsonConfigLoader.LoadFromConfig<SoundCatalogJsonConfig>(AddressDefines.Config_SoundCatalog);
            if (_catalog == null)
                _catalog = new SoundCatalogJsonConfig();

            buildIndex();
            return _catalog;
        }

        public static void Reload()
        {
            _catalog = null;
            _clipsById = null;
            _clipVariantsByBaseId = null;
            _bgmsById = null;
            _viewsByName = null;
            LoadCatalog();
        }

        // 查询音效（clips 表）；id 无 _N 后缀时从同组变体中随机选取
        public static bool TryResolveClip(string id, out SoundClipResolveData result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(id)) return false;

            LoadCatalog();
            if (_clipsById == null) return false;

            if (!tryGetClipData(id, out SoundClipJsonData clipData))
                return false;

            if (string.IsNullOrWhiteSpace(clipData.path)) return false;

            result = new SoundClipResolveData
            {
                Id = clipData.id,
                Path = normalizeSoundPath(clipData.path),
                VolumeScale = normalizeVolume(clipData.volume),
                Breakable = clipData.breakable
            };
            return true;
        }

        // 查询 BGM（bgms 表，通过 id 取 path）
        public static bool TryResolveBgm(string id, out SoundClipResolveData result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(id)) return false;

            LoadCatalog();
            if (_bgmsById == null || !_bgmsById.TryGetValue(id, out SoundBgmJsonData bgmData))
                return false;

            if (string.IsNullOrWhiteSpace(bgmData.path)) return false;

            result = new SoundClipResolveData
            {
                Id = bgmData.id,
                Path = normalizeSoundPath(bgmData.path),
                VolumeScale = normalizeVolume(bgmData.volume)
            };
            return true;
        }

        public static string[] GetBgmPlaylist(string[] fallback)
        {
            LoadCatalog();
            string[] playlist = _catalog?.defaults?.bgmPlaylist;
            return playlist != null && playlist.Length > 0 ? playlist : fallback;
        }

        public static string GetDefaultButtonClick()
        {
            LoadCatalog();
            return _catalog?.defaults?.buttonClick;
        }

        public static string GetDefaultButtonHover()
        {
            LoadCatalog();
            return _catalog?.defaults?.buttonHover;
        }

        public static SoundViewBindingJsonData GetViewBinding(string viewName)
        {
            LoadCatalog();
            if (string.IsNullOrEmpty(viewName) || _viewsByName == null) return null;
            return _viewsByName.TryGetValue(viewName, out SoundViewBindingJsonData binding) ? binding : null;
        }

        public static SoundButtonBindingJsonData FindButtonBinding(SoundViewBindingJsonData viewBinding, string buttonPath)
        {
            if (viewBinding?.buttons == null || string.IsNullOrEmpty(buttonPath)) return null;

            SoundButtonBindingJsonData prefixMatch = null;
            int prefixLength = 0;

            foreach (SoundButtonBindingJsonData binding in viewBinding.buttons)
            {
                if (binding == null || string.IsNullOrEmpty(binding.path)) continue;

                if (binding.path == buttonPath)
                    return binding;

                if (!binding.path.EndsWith("/")) continue;
                if (!buttonPath.StartsWith(binding.path)) continue;
                if (binding.path.Length <= prefixLength) continue;

                prefixMatch = binding;
                prefixLength = binding.path.Length;
            }

            return prefixMatch;
        }

        private static bool tryGetClipData(string id, out SoundClipJsonData clipData)
        {
            clipData = null;
            if (_clipsById.TryGetValue(id, out clipData))
                return clipData != null;

            if (_clipVariantsByBaseId == null
                || !_clipVariantsByBaseId.TryGetValue(id, out List<string> variants)
                || variants == null
                || variants.Count == 0)
                return false;

            string pickedId = variants[Random.Range(0, variants.Count)];
            return _clipsById.TryGetValue(pickedId, out clipData) && clipData != null;
        }

        private static void buildIndex()
        {
            _clipsById = new Dictionary<string, SoundClipJsonData>();
            _clipVariantsByBaseId = new Dictionary<string, List<string>>();
            SoundClipJsonData[] clips = _catalog?.clips;
            if (clips != null)
            {
                foreach (SoundClipJsonData clip in clips)
                {
                    if (clip == null || string.IsNullOrWhiteSpace(clip.id)) continue;
                    _clipsById[clip.id] = clip;

                    if (!tryGetVariantBaseId(clip.id, out string baseId)) continue;
                    if (!_clipVariantsByBaseId.TryGetValue(baseId, out List<string> variants))
                    {
                        variants = new List<string>();
                        _clipVariantsByBaseId[baseId] = variants;
                    }

                    variants.Add(clip.id);
                }
            }

            _bgmsById = new Dictionary<string, SoundBgmJsonData>();
            SoundBgmJsonData[] bgms = _catalog?.bgms;
            if (bgms != null)
            {
                foreach (SoundBgmJsonData bgm in bgms)
                {
                    if (bgm == null || string.IsNullOrWhiteSpace(bgm.id)) continue;
                    _bgmsById[bgm.id] = bgm;
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

        private static bool tryGetVariantBaseId(string id, out string baseId)
        {
            baseId = null;
            int lastUnderscore = id.LastIndexOf('_');
            if (lastUnderscore <= 0 || lastUnderscore >= id.Length - 1) return false;

            string suffix = id.Substring(lastUnderscore + 1);
            for (int i = 0; i < suffix.Length; i++)
            {
                if (suffix[i] < '0' || suffix[i] > '9')
                    return false;
            }

            baseId = id.Substring(0, lastUnderscore);
            return true;
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
