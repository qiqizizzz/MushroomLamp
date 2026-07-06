/*
* ┌──────────────────────────────────┐
* │  描    述: 材料图标统一加载（只认 MaterialCatalog.iconPath）
* │  类    名: MaterialIconLoader.cs
* └──────────────────────────────────┘
*/

using Common;
using UnityEngine;

namespace Module.Material
{
    public static class MaterialIconLoader
    {
        // 按材料 ID（VEG_xxx）从 MaterialCatalog 加载图标
        public static Sprite LoadSprite(string materialId, bool logOnFail = false)
        {
            if (string.IsNullOrWhiteSpace(materialId)) return null;
            return LoadSprite(MaterialCatalogLoader.GetById(materialId), logOnFail);
        }

        // 按材料配置加载图标
        public static Sprite LoadSprite(MaterialJsonData config, bool logOnFail = false)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.iconPath)) return null;
            return ArtAssetLoader.LoadSprite(config.iconPath, logOnFail);
        }

        // 优先 MaterialCatalog，失败时可选兜底路径（如 SelectBox 旧字段）
        public static Sprite LoadSpriteOrFallback(string materialId, string fallbackIconPath, bool logOnFail = false)
        {
            Sprite icon = LoadSprite(materialId, logOnFail: false);
            if (icon != null) return icon;

            if (!string.IsNullOrWhiteSpace(fallbackIconPath))
                return ArtAssetLoader.LoadSprite(fallbackIconPath, logOnFail);

            return null;
        }
    }
}
