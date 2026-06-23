/*
 * ┌──────────────────────────────────┐
 * │  描    述: 材料箱 JSON 配置结构
 * │  类    名: SelectBoxJsonConfig.cs
 * └──────────────────────────────────┘
 */

using System;
using Common;
using UnityEngine;

namespace Module.Select
{
    [Serializable]
    public class SelectMaterialLineJsonData
    {
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
                icon = icon,
                label = label,
                count = count
            };
        }
    }

    [Serializable]
    public class SelectBoxJsonConfig
    {
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
