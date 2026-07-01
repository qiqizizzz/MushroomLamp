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
        private const string IN_GAME_BGM = "BGM/ingame";

        private static readonly string[] S_DefaultBgmPlaylist =
        {
            "BGM/Alchemical Clockwork",
            "BGM/Cinder Crucible",
            "BGM/Murmur Vatcall"
        };

        private readonly Dictionary<string, AudioClip> _clips;
        private readonly Transform _audioRootTf;
        private readonly AudioSource _bgmSource;

        private readonly string[] _bgmPlaylist;
        private bool _isStop;
        private bool _isPlaylistMode;
        private bool _isApplicationPaused;
        private bool _wasBgmPlayingBeforePause;
        private float _bgmVolume;
        private float _effectVolume;
        private float _currentBgmVolumeScale = 1f;
        private int _lastPlaylistIndex = -1;

        public bool IsStop
        {
            get => _isStop;
            set
            {
                _isStop = value;
                if (_bgmSource == null) return;

                if (_isStop)
                    _bgmSource.Pause();
                else if (!_bgmSource.isPlaying && _bgmSource.clip != null)
                    _bgmSource.Play();
            }
        }

        public float BgmVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = Mathf.Clamp01(value);
                if (_bgmSource != null)
                    _bgmSource.volume = _bgmVolume * _currentBgmVolumeScale;
            }
        }

        public float EffectVolume
        {
            get => _effectVolume;
            set => _effectVolume = Mathf.Clamp01(value);
        }

        // 音效总开关，关闭后 PlayEffect 不发声
        public bool EffectEnabled { get; set; }

        // 背景音乐总开关（语义化封装 IsStop：开=不停，关=停）
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

            // 从存档读取设置初值（默认开、音量 1）
            EffectEnabled = SettingsKeys.GetBool(SettingsKeys.SfxOn, true);
            EffectVolume = SettingsKeys.GetFloat(SettingsKeys.SfxVolume, 1f);
            BgmVolume = SettingsKeys.GetFloat(SettingsKeys.BgmVolume, 1f);
            IsStop = !SettingsKeys.GetBool(SettingsKeys.BgmOn, true);
        }

        // 每帧检测轮播音乐是否需要切到下一首
        public void OnUpdate(float dt)
        {
            if (IsStop || _isApplicationPaused || !_isPlaylistMode || _bgmSource == null) return;
            if (_bgmSource.isPlaying) return;

            playNextPlaylistBgm();
        }

        // 设置应用暂停状态，避免最小化时误判 BGM 播放结束
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
            }
            else if (_wasBgmPlayingBeforePause && !IsStop && _bgmSource.clip != null)
            {
                _bgmSource.Play();
            }
        }

        // 播放背景音乐，资源路径对应 Resources/Sounds
        public void PlayBGM(string res)
        {
            _isPlaylistMode = false;
            playBgm(res, true);
        }

        // 播放烹饪玩法背景音乐
        public void PlayInGameBGM()
        {
            _isPlaylistMode = false;
            playBgm(SoundConfigLoader.GetGameplayBgm(IN_GAME_BGM), true);
        }

        // 随机播放普通界面背景音乐
        public void PlayRandomBGM()
        {
            if (_isPlaylistMode && _bgmSource != null && _bgmSource.isPlaying) return;

            _isPlaylistMode = true;
            playNextPlaylistBgm();
        }

        // 播放指定背景音乐
        private bool playBgm(string res, bool isLoop)
        {
            if (IsStop) return false;
            if (!SoundConfigLoader.TryResolveClip(res, out SoundClipResolveData clipData)) return false;

            AudioClip clip = loadClip(clipData.Path);
            if (clip == null) return false;

            _currentBgmVolumeScale = clipData.VolumeScale;
            _bgmSource.volume = BgmVolume * _currentBgmVolumeScale;

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

        // 播放音效，资源路径或音频 id 对应 Resources/Sounds
        public void PlayEffect(string name)
        {
            PlayEffect(name, Vector3.zero);
        }

        // 播放音效，资源路径对应 Resources/Sounds
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

        // 重新加载声音配置
        public void ReloadConfig()
        {
            SoundConfigLoader.Reload();
        }

        // 获取或创建音频根节点
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

        // 获取或创建 BGM 音源
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

        // 播放下一首轮播音乐
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
                if (playBgm(_bgmPlaylist[playlistIndex], false))
                {
                    _lastPlaylistIndex = playlistIndex;
                    return;
                }
            }

            _isPlaylistMode = false;
        }

        // 获取随机轮播索引，尽量避免连续重复
        private int getRandomPlaylistIndex()
        {
            if (_bgmPlaylist.Length <= 1)
                return 0;

            int playlistIndex = Random.Range(0, _bgmPlaylist.Length);
            if (playlistIndex == _lastPlaylistIndex)
                playlistIndex = (playlistIndex + 1) % _bgmPlaylist.Length;

            return playlistIndex;
        }

        // 加载并缓存音频
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
