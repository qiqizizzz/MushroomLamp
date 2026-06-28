/*
 * ┌──────────────────────────────────┐
 * │  描    述: 材料箱 JSON 配置结构（主表 + 子表）
 * │  类    名: SelectBoxJsonConfig.cs
 * └──────────────────────────────────┘
 */

using System;
using Common;
using UnityEngine;

namespace Module.Select
{
    [Serializable]
    public class SelectBoxCatalogEntry
    {
        public string id;
        public string displayName;
        // 相对 Assets/Config/ 的路径（不含 .json）
        public string configFile;
    }

    [Serializable]
    public class SelectBoxCatalogJsonConfig
    {
        public string defaultBoxId;
        public SelectBoxCatalogEntry[] boxes;
    }

    [Serializable]
    public class SelectMaterialLineJsonData
    {
        public string materialId;   // 材料配置 id（VEG_xxx）
        // Assets/Art 下的 Sprite 路径（不含扩展名），如 Art/Card_img/carrot
        public string iconPath;
        public string label;
        public int count = 1;

        public SelectMaterialLineData ToRuntime()
        {
            Sprite icon = null;
            if (!string.IsNullOrEmpty(iconPath))
                icon = ArtAssetLoader.LoadSprite(iconPath);

            return new SelectMaterialLineData
            {
                materialId = materialId,
                icon = icon,
                label = label,
                count = count
            };
        }
    }

    [Serializable]
    public class SelectBoxDetailJsonConfig
    {
        // 背景图路径，相对 Assets，如 Art/BG_img/bg_SelectView
        public string backgroundPath;
        public string summaryTitle = "简介";
        public SelectMaterialLineJsonData[] lines;

        public SelectMaterialLineData[] ToRuntimeLines()
        {
            if (lines == null || lines.Length == 0)
                return Array.Empty<SelectMaterialLineData>();

            SelectMaterialLineData[] result = new SelectMaterialLineData[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                result[i] = lines[i]?.ToRuntime();

            return result;
        }
    }
}
