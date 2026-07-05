/*
* ┌──────────────────────────────────┐
* │  描    述: 详情页 JSON 目录配置结构与查询入口
* │  类    名: DetailCatologJsonConfig.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Common;
using Common.Defines;
using MVC;

namespace Module.View
{
    // 详情页 JSON 目录配置结构与查询入口
    [Serializable]
    public class DetailCatologJsonConfig
    {
        public DetailItemJsonData[] items;

        private static DetailCatologJsonConfig _cache;

        // 根据界面类型查找详情配置
        public static bool TryGetItem(ViewType viewType, out DetailItemJsonData item)
        {
            item = null;
            DetailCatologJsonConfig config = loadConfig();
            if (config?.items == null) return false;

            for (int i = 0; i < config.items.Length; i++)
            {
                DetailItemJsonData current = config.items[i];
                if (current == null || current.viewType != viewType) continue;

                item = current;
                return true;
            }

#if UNITY_EDITOR
            QLog.Warning($"[{nameof(DetailCatologJsonConfig)}] 未找到详情配置：{viewType}");
#endif
            return false;
        }

        // 清理详情配置缓存
        public static void ClearCache()
        {
            _cache = null;
        }

        // 读取详情配置 JSON
        private static DetailCatologJsonConfig loadConfig()
        {
            if (_cache != null) return _cache;

            _cache = JsonConfigLoader.LoadFromConfig<DetailCatologJsonConfig>(AddressDefines.Config_DetailCatolog);
            return _cache;
        }
    }

    // 详情条目 JSON 数据
    [Serializable]
    public class DetailItemJsonData
    {
        public ViewType viewType;
        public string title;
        public string content;
    }
}
