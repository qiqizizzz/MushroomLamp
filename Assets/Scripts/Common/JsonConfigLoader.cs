/*
 * ┌──────────────────────────────────┐
 * │  描    述: JSON 配置通用读取工具
 * │  类    名: JsonConfigLoader.cs
 * └──────────────────────────────────┘
 */

using System.IO;
using UnityEngine;

namespace Common
{
    public static class JsonConfigLoader
    {
        private const string ConfigFolderName = "Config";

        // 从 Assets/Config/ 读取 JSON（fileName 不含扩展名）
        public static T LoadFromConfig<T>(string fileName) where T : class
        {
            string json = readConfigJsonText(fileName);
            if (json == null) return null;

            return LoadFromJsonText<T>(json);
        }

        // 从 Resources 加载 TextAsset 并反序列化（路径不含扩展名）
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

        public static bool TryLoadFromConfig<T>(string fileName, out T result) where T : class
        {
            result = LoadFromConfig<T>(fileName);
            return result != null;
        }

        // 尝试从 Resources 加载，失败时不报错，仅返回 false
        public static bool TryLoadFromResources<T>(string resourcePath, out T result) where T : class
        {
            result = LoadFromResources<T>(resourcePath);
            return result != null;
        }

        private static string readConfigJsonText(string fileName)
        {
            string jsonFileName = normalizeJsonFileName(fileName);

            string projectPath = Path.Combine(Application.dataPath, ConfigFolderName, jsonFileName);
            if (File.Exists(projectPath))
                return File.ReadAllText(projectPath);

            string streamingPath = Path.Combine(Application.streamingAssetsPath, ConfigFolderName, jsonFileName);
            if (File.Exists(streamingPath))
                return File.ReadAllText(streamingPath);

            QLog.Error($"[{nameof(JsonConfigLoader)}] 未找到配置文件：Assets/{ConfigFolderName}/{jsonFileName}");
            return null;
        }

        private static string normalizeJsonFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            return fileName.EndsWith(".json") ? fileName : $"{fileName}.json";
        }
    }
}
