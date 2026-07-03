/*
* ┌──────────────────────────────────┐
* │  描    述: 声音配置 JSON 数据结构
* │  类    名: SoundConfig.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;

namespace Sound
{
    [Serializable]
    public class SoundCatalogJsonConfig
    {
        public SoundDefaultsJsonData defaults;
        public SoundBgmJsonData[] bgms;
        public SoundClipJsonData[] clips;
        public SoundViewBindingJsonData[] viewBindings;
    }

    [Serializable]
    public class SoundDefaultsJsonData
    {
        public string buttonClick;
        public string buttonHover;
        public string[] bgmPlaylist;
    }

    [Serializable]
    public class SoundBgmJsonData
    {
        public string id;
        public string path;
        public float volume = 1f;
    }

    [Serializable]
    public class SoundClipJsonData
    {
        public string id;
        public string path;
        public float volume = 1f;
    }

    [Serializable]
    public class SoundViewBindingJsonData
    {
        public string view;
        public SoundViewBgmJsonData[] bgms;
        public string buttonClick;
        public string buttonHover;
        public bool disableAutoButtonSound;
        public SoundButtonBindingJsonData[] buttons;
    }

    [Serializable]
    public class SoundViewBgmJsonData
    {
        public string id;
        public bool loop = true;
        public bool layer;
    }

    [Serializable]
    public class SoundButtonBindingJsonData
    {
        public string path;
        public string click;
        public string hover;
        public bool muteClick;
        public bool muteHover;
    }

    public struct SoundClipResolveData
    {
        public string Id;
        public string Path;
        public float VolumeScale;
    }
}
