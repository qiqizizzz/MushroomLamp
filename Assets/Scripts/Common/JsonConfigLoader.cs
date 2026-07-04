/*
* ┌──────────────────────────────────┐
* │  描    述: JSON 配置通用读取工具
* │  类    名: JsonConfigLoader.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;

namespace Common
{
    public static class JsonConfigLoader
    {
        private const string CONFIG_RESOURCE_ROOT = "Config/";

        // 从 Resources/Config 读取 JSON 配置（address 即文件名，如 "CardConfig_Data"、"Boxes/SelectBox_herb"）
        public static T LoadFromConfig<T>(string fileName) where T : class
        {
            string json = readConfigJsonText(fileName);
            if (json == null) return null;

            return LoadFromJsonText<T>(json);
        }

        // 从 Resources 加载 TextAsset 并反序列化
        public static T LoadFromResources<T>(string resourcePath) where T : class
        {
            TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null)
            {
                QLog.Error($"[{nameof(JsonConfigLoader)}] 未找到配置文件：Resources/{resourcePath}");
                return null;
            }

            return LoadFromJsonText<T>(textAsset.text);
        }

        // 从 TextAsset 反序列化
        public static T LoadFromTextAsset<T>(TextAsset textAsset) where T : class
        {
            if (textAsset == null)
            {
                QLog.Error($"[{nameof(JsonConfigLoader)}] TextAsset 为空");
                return null;
            }

            return LoadFromJsonText<T>(textAsset.text);
        }

        // 从 JSON 字符串反序列化
        public static T LoadFromJsonText<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                QLog.Error($"[{nameof(JsonConfigLoader)}] JSON 内容为空");
                return null;
            }

            try
            {
                T result = JsonUtility.FromJson<T>(json);
                if (result == null)
                    QLog.Error($"[{nameof(JsonConfigLoader)}] 反序列化结果为 null：{typeof(T).Name}");

                return result;
            }
            catch (System.Exception ex)
            {
                QLog.Error($"[{nameof(JsonConfigLoader)}] JSON 解析失败：{ex.Message}");
                return null;
            }
        }

        // 尝试从 Assets/Config/ 读取 JSON
        public static bool TryLoadFromConfig<T>(string fileName, out T result) where T : class
        {
            result = LoadFromConfig<T>(fileName);
            return result != null;
        }

        // 尝试从 Resources 加载，失败时返回 false
        public static bool TryLoadFromResources<T>(string resourcePath, out T result) where T : class
        {
            result = LoadFromResources<T>(resourcePath);
            return result != null;
        }

        // 读取配置 JSON 文本
        private static string readConfigJsonText(string fileName)
        {
            string address = normalizeAddress(fileName);
            if (string.IsNullOrEmpty(address))
                return null;

            TextAsset resourceText = Resources.Load<TextAsset>($"{CONFIG_RESOURCE_ROOT}{address}");
            if (resourceText != null)
                return resourceText.text;

            QLog.Error($"[{nameof(JsonConfigLoader)}] 配置未找到：Resources/{CONFIG_RESOURCE_ROOT}{address}");
            return null;
        }

        // 规范化为 address：去掉可能的 .json 后缀
        private static string normalizeAddress(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            return fileName.EndsWith(".json") ? fileName.Substring(0, fileName.Length - ".json".Length) : fileName;
        }
    }
}
