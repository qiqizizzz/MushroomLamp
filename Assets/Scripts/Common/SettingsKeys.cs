/*
* ┌──────────────────────────────────┐
* │  描    述: 设置存档键与 PlayerPrefs 读写封装
* │  类    名: SettingsKeys.cs
* └──────────────────────────────────┘
*/

using UnityEngine;

namespace Common
{
    // 设置项存档（PlayerPrefs），改动即存
    public static class SettingsKeys
    {
        public const string SfxOn = "setting_sfx_on";
        public const string SfxVolume = "setting_sfx_volume";
        public const string BgmOn = "setting_bgm_on";
        public const string BgmVolume = "setting_bgm_volume";

        public static bool GetBool(string key, bool defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;
        }

        public static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static float GetFloat(string key, float defaultValue)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        public static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }
    }
}
