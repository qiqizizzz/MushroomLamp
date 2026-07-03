/*
* ┌──────────────────────────────────┐
* │  描    述: 声音管理器
* │  类    名: SoundManager.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common;
using UnityEngine;

namespace Sound
{
    public class SoundManager
    {
        private static readonly string[] S_DefaultBgmPlaylist =
        {
            "bgm_alchemical_clockwork",
            "bgm_cinder_crucible",
            "bgm_murmur_vatcall"
        };

        private readonly Dictionary<string, AudioClip> _clips;
        private readonly Transform _audioRootTf;
        private readonly AudioSource _bgmSource;
        private readonly List<AudioSource> _extraBgmSources = new();
        private readonly List<float> _extraBgmVolumeScales = new();

        private readonly string[] _bgmPlaylist;
        private bool _isStop;
        private bool _isPlaylistMode;
        private bool _isApplicationPaused;
        private bool _wasBgmPlayingBeforePause;
        private readonly List<bool> _wasExtraBgmPlayingBeforePause = new();
        private float _bgmVolume;
        private float _effectVolume;
        private float _currentBgmVolumeScale = 1f;
        private int _lastPlaylistIndex = -1;
        private SoundViewBgmJsonData[] _activeViewBgms;

        public bool IsStop
        {
            get => _isStop;
            set
            {
                _isStop = value;
                if (_bgmSource == null) return;

                if (_isStop)
                {
                    _bgmSource.Pause();
                    pauseExtraBgms();
                }
                else
                {
                    resumeBgmAfterEnable();
                }
            }
        }

        public float BgmVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = Mathf.Clamp01(value);
                applyBgmVolume();
            }
        }

        public float EffectVolume
        {
            get => _effectVolume;
            set => _effectVolume = Mathf.Clamp01(value);
        }

        public bool EffectEnabled { get; set; }

        public bool BgmEnabled
        {
            get => !IsStop;
            set => IsStop = !value;
        }

        public SoundManager()
        {
            _clips = new Dictionary<string, AudioClip>();
            SoundConfigLoader.LoadCatalog();
            _bgmPlaylist = SoundConfigLoader.GetBgmPlaylist(S_DefaultBgmPlaylist);
            _audioRootTf = getOrCreateAudioRoot();
            _bgmSource = getOrCreateBgmSource();

            EffectEnabled = SettingsKeys.GetBool(SettingsKeys.SfxOn, true);
            EffectVolume = SettingsKeys.GetFloat(SettingsKeys.SfxVolume, 1f);
            BgmVolume = SettingsKeys.GetFloat(SettingsKeys.BgmVolume, 1f);
            IsStop = !SettingsKeys.GetBool(SettingsKeys.BgmOn, true);
        }

        public void OnUpdate(float dt)
        {
            if (IsStop || _isApplicationPaused || !_isPlaylistMode || _bgmSource == null) return;
            if (_bgmSource.isPlaying) return;

            playNextPlaylistBgm();
        }

        public void SetApplicationPaused(bool isPaused)
        {
            if (_isApplicationPaused == isPaused) return;

            _isApplicationPaused = isPaused;
            if (_bgmSource == null) return;

            if (_isApplicationPaused)
            {
                _wasBgmPlayingBeforePause = _bgmSource.isPlaying;
                if (_wasBgmPlayingBeforePause)
                    _bgmSource.Pause();

                _wasExtraBgmPlayingBeforePause.Clear();
                for (int i = 0; i < _extraBgmSources.Count; i++)
                {
                    AudioSource source = _extraBgmSources[i];
                    bool wasPlaying = source != null && source.isPlaying;
                    _wasExtraBgmPlayingBeforePause.Add(wasPlaying);
                    if (wasPlaying)
                        source.Pause();
                }
            }
            else
            {
                if (_wasBgmPlayingBeforePause && !IsStop && _bgmSource.clip != null)
                    _bgmSource.Play();

                for (int i = 0; i < _extraBgmSources.Count; i++)
                {
                    if (i >= _wasExtraBgmPlayingBeforePause.Count) break;
                    if (!_wasExtraBgmPlayingBeforePause[i] || IsStop) continue;

                    AudioSource source = _extraBgmSources[i];
                    if (source != null && source.clip != null)
                        source.Play();
                }
            }
        }

        // 播放单轨主 BGM（bgms 表 id）
        public void PlayBGM(string bgmId)
        {
            _activeViewBgms = null;
            _isPlaylistMode = false;
            StopExtraBgms();

            if (!SoundConfigLoader.TryResolveBgm(bgmId, out SoundClipResolveData data)) return;
            playMainBgm(data, true);
        }

        // 按 View 配置播放多轨 BGM（主轨 + 叠加层）
        public void PlayViewBgms(SoundViewBgmJsonData[] entries)
        {
            if (entries == null || entries.Length == 0) return;

            _activeViewBgms = entries;
            _isPlaylistMode = false;
            StopExtraBgms();

            bool mainAssigned = false;
            for (int i = 0; i < entries.Length; i++)
            {
                SoundViewBgmJsonData entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id)) continue;
                if (!SoundConfigLoader.TryResolveBgm(entry.id, out SoundClipResolveData data)) continue;

                bool useLayer = entry.layer || mainAssigned;
                if (!useLayer)
                {
                    playMainBgm(data, entry.loop);
                    mainAssigned = true;
                }
                else
                {
                    playExtraBgm(data, entry.loop);
                }
            }
        }

        public void PlayRandomBGM()
        {
            if (_isPlaylistMode && _bgmSource != null && _bgmSource.isPlaying) return;

            _activeViewBgms = null;
            _isPlaylistMode = true;
            StopExtraBgms();
            playNextPlaylistBgm();
        }

        public void ReloadConfig()
        {
            SoundConfigLoader.Reload();
        }

        public void PlayEffect(string name)
        {
            PlayEffect(name, Vector3.zero);
        }

        public void PlayEffect(string name, Vector3 pos)
        {
            if (!EffectEnabled) return;
            if (!SoundConfigLoader.TryResolveClip(name, out SoundClipResolveData clipData)) return;

            AudioClip clip = loadClip(clipData.Path);
            if (clip == null) return;

            GameObject effectObj = new GameObject($"Effect_{name}");
            effectObj.transform.SetParent(_audioRootTf, false);
            effectObj.transform.position = pos;

            AudioSource effectSource = effectObj.AddComponent<AudioSource>();
            effectSource.clip = clip;
            effectSource.volume = EffectVolume * clipData.VolumeScale;
            effectSource.Play();
            Object.Destroy(effectObj, clip.length);
        }

        private void applyBgmVolume()
        {
            if (_bgmSource != null)
                _bgmSource.volume = _bgmVolume * _currentBgmVolumeScale;

            for (int i = 0; i < _extraBgmSources.Count; i++)
            {
                AudioSource source = _extraBgmSources[i];
                if (source == null) continue;

                float scale = i < _extraBgmVolumeScales.Count ? _extraBgmVolumeScales[i] : 1f;
                source.volume = _bgmVolume * scale;
            }
        }

        private void pauseExtraBgms()
        {
            for (int i = 0; i < _extraBgmSources.Count; i++)
            {
                if (_extraBgmSources[i] != null)
                    _extraBgmSources[i].Pause();
            }
        }

        private void resumeExtraBgms()
        {
            for (int i = 0; i < _extraBgmSources.Count; i++)
            {
                AudioSource source = _extraBgmSources[i];
                if (source != null && source.clip != null)
                    source.Play();
            }
        }

        // BGM 从关闭恢复：有已加载 clip 则续播，否则按当前播放意图重新启动
        private void resumeBgmAfterEnable()
        {
            if (_bgmSource.clip != null)
            {
                if (!_bgmSource.isPlaying)
                    _bgmSource.Play();
                resumeExtraBgms();
                return;
            }

            if (_activeViewBgms != null && _activeViewBgms.Length > 0)
            {
                PlayViewBgms(_activeViewBgms);
                return;
            }

            if (_isPlaylistMode)
            {
                playNextPlaylistBgm();
                return;
            }

            PlayRandomBGM();
        }

        private void StopExtraBgms()
        {
            for (int i = 0; i < _extraBgmSources.Count; i++)
            {
                AudioSource source = _extraBgmSources[i];
                if (source == null) continue;
                source.Stop();
                source.clip = null;
            }

            _extraBgmVolumeScales.Clear();
        }

        private bool playMainBgm(SoundClipResolveData data, bool isLoop)
        {
            if (IsStop) return false;

            AudioClip clip = loadClip(data.Path);
            if (clip == null) return false;

            _currentBgmVolumeScale = data.VolumeScale;
            applyBgmVolume();

            if (_bgmSource.clip == clip && _bgmSource.isPlaying)
            {
                _bgmSource.loop = isLoop;
                return true;
            }

            _bgmSource.clip = clip;
            _bgmSource.loop = isLoop;
            _bgmSource.Play();
            return true;
        }

        private bool playExtraBgm(SoundClipResolveData data, bool isLoop)
        {
            if (IsStop) return false;

            AudioClip clip = loadClip(data.Path);
            if (clip == null) return false;

            AudioSource source = getOrCreateExtraBgmSource(_extraBgmSources.Count);
            _extraBgmVolumeScales.Add(data.VolumeScale);
            applyBgmVolume();

            if (source.clip == clip && source.isPlaying)
            {
                source.loop = isLoop;
                return true;
            }

            source.clip = clip;
            source.loop = isLoop;
            source.Play();
            return true;
        }

        private Transform getOrCreateAudioRoot()
        {
            Transform rootTf = GameApp.RootTf;
            Transform audioTf = rootTf == null ? null : rootTf.Find("Audio");
            if (audioTf != null)
                return audioTf;

            GameObject audioObj = GameObject.Find("Audio");
            if (audioObj == null || audioObj.transform.parent != null)
                audioObj = new GameObject("Audio");

            if (rootTf != null)
                audioObj.transform.SetParent(rootTf, false);

            audioObj.transform.localPosition = Vector3.zero;
            audioObj.transform.localRotation = Quaternion.identity;
            audioObj.transform.localScale = Vector3.one;
            return audioObj.transform;
        }

        private AudioSource getOrCreateBgmSource()
        {
            Transform bgmTf = _audioRootTf.Find("BGM");
            if (bgmTf == null)
            {
                GameObject bgmObj = new GameObject("BGM");
                bgmObj.transform.SetParent(_audioRootTf, false);
                bgmTf = bgmObj.transform;
            }

            AudioSource audioSource = bgmTf.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = bgmTf.gameObject.AddComponent<AudioSource>();

            return audioSource;
        }

        private AudioSource getOrCreateExtraBgmSource(int index)
        {
            while (_extraBgmSources.Count <= index)
            {
                string nodeName = $"BGM_Layer_{_extraBgmSources.Count}";
                GameObject layerObj = new GameObject(nodeName);
                layerObj.transform.SetParent(_audioRootTf, false);
                _extraBgmSources.Add(layerObj.AddComponent<AudioSource>());
            }

            return _extraBgmSources[index];
        }

        private void playNextPlaylistBgm()
        {
            if (_bgmPlaylist == null || _bgmPlaylist.Length == 0)
            {
                _isPlaylistMode = false;
                return;
            }

            int startIndex = getRandomPlaylistIndex();
            for (int i = 0; i < _bgmPlaylist.Length; i++)
            {
                int playlistIndex = (startIndex + i) % _bgmPlaylist.Length;
                if (!SoundConfigLoader.TryResolveBgm(_bgmPlaylist[playlistIndex], out SoundClipResolveData data))
                    continue;

                if (playMainBgm(data, false))
                {
                    _lastPlaylistIndex = playlistIndex;
                    return;
                }
            }

            // BGM 关闭时播放会失败，保留轮播模式以便重新打开设置后能续播
            if (IsStop) return;

            _isPlaylistMode = false;
        }

        private int getRandomPlaylistIndex()
        {
            if (_bgmPlaylist.Length <= 1)
                return 0;

            int playlistIndex = Random.Range(0, _bgmPlaylist.Length);
            if (playlistIndex == _lastPlaylistIndex)
                playlistIndex = (playlistIndex + 1) % _bgmPlaylist.Length;

            return playlistIndex;
        }

        private AudioClip loadClip(string name)
        {
            if (_clips.TryGetValue(name, out AudioClip clip))
                return clip;

            clip = Resources.Load<AudioClip>($"Sounds/{name}");
            if (clip == null)
            {
                QLog.Warning($"[{nameof(SoundManager)}] 音频加载失败：Sounds/{name}");
                return null;
            }

            _clips.Add(name, clip);
            return clip;
        }
    }
}
