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
        private readonly Dictionary<string, AudioClip> _clips;
        private readonly Transform _audioRootTf;
        private readonly AudioSource _bgmSource;

        private bool _isStop;
        private float _bgmVolume;
        private float _effectVolume;

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
                    _bgmSource.volume = _bgmVolume;
            }
        }

        public float EffectVolume
        {
            get => _effectVolume;
            set => _effectVolume = Mathf.Clamp01(value);
        }

        public SoundManager()
        {
            _clips = new Dictionary<string, AudioClip>();
            _audioRootTf = getOrCreateAudioRoot();
            _bgmSource = getOrCreateBgmSource();
            IsStop = false;
            BgmVolume = 1f;
            EffectVolume = 1f;
        }

        // 播放背景音乐，资源路径对应 Resources/Sounds
        public void PlayBGM(string res)
        {
            if (IsStop) return;

            AudioClip clip = loadClip(res);
            if (clip == null) return;

            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            _bgmSource.clip = clip;
            _bgmSource.loop = true;
            _bgmSource.Play();
        }

        // 播放音效，资源路径对应 Resources/Sounds
        public void PlayEffect(string name, Vector3 pos)
        {
            if (IsStop) return;

            AudioClip clip = loadClip(name);
            if (clip == null) return;

            GameObject effectObj = new GameObject($"Effect_{name}");
            effectObj.transform.SetParent(_audioRootTf, false);
            effectObj.transform.position = pos;

            AudioSource effectSource = effectObj.AddComponent<AudioSource>();
            effectSource.clip = clip;
            effectSource.volume = EffectVolume;
            effectSource.Play();
            Object.Destroy(effectObj, clip.length);
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
