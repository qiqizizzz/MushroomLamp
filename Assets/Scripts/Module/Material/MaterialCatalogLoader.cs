/*
* ┌──────────────────────────────────┐
* │  描    述: 材料卡牌配置读表入口（带缓存与按 id 查询）
* │  类    名: MaterialCatalogLoader.cs
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common;

namespace Module.Material
{
    // 材料卡牌配置读表框架：当前仅负责加载与查询，卡牌效果逻辑后续实现
    public static class MaterialCatalogLoader
    {
        public const string CatalogAddress = "MaterialCatalog";

        private static MaterialCatalogJsonConfig _catalog;
        private static Dictionary<string, MaterialJsonData> _byId;

        // 加载材料目录（带缓存）
        public static MaterialCatalogJsonConfig LoadCatalog()
        {
            if (_catalog != null) return _catalog;

            _catalog = JsonConfigLoader.LoadFromConfig<MaterialCatalogJsonConfig>(CatalogAddress);
            if (_catalog?.materials == null || _catalog.materials.Length == 0)
            {
                QLog.Error($"[{nameof(MaterialCatalogLoader)}] 材料目录加载失败或为空：{CatalogAddress}");
                return _catalog;
            }

            buildIndex();
            return _catalog;
        }

        // 按材料ID查询（如 "VEG_001"）
        public static MaterialJsonData GetById(string id)
        {
            if (_byId == null) LoadCatalog();
            if (string.IsNullOrEmpty(id) || _byId == null) return null;
            return _byId.TryGetValue(id, out MaterialJsonData data) ? data : null;
        }

        // 获取全部材料
        public static IReadOnlyList<MaterialJsonData> GetAll()
        {
            LoadCatalog();
            return _catalog?.materials ?? System.Array.Empty<MaterialJsonData>();
        }

        public static void ClearCache()
        {
            _catalog = null;
            _byId = null;
        }

        private static void buildIndex()
        {
            _byId = new Dictionary<string, MaterialJsonData>();
            foreach (MaterialJsonData m in _catalog.materials)
            {
                if (m == null || string.IsNullOrEmpty(m.id)) continue;
                _byId[m.id] = m;
            }
        }
    }
}
