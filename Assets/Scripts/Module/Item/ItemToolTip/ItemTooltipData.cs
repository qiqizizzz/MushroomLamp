/*
* ┌──────────────────────────────────┐
* │  描    述: 道具详情浮层展示数据，负责从材料运行时数据提取字段
* │  类    名: ItemTooltipData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Cook;
using Module.Material;
using UnityEngine;

namespace Module.Item
{
    // 道具详情浮层展示数据，负责从材料运行时数据提取字段
    public class ItemTooltipData
    {
        public string Name;
        public string Subtitle;
        public string PriceText;
        public string Desc;
        public string ProcessText;
        public string EffectText;
        public Sprite Icon;
        public readonly List<string> Tags = new();
        public readonly List<ItemTooltipFieldData> Fields = new();

        // 从烹饪材料构建 Tooltip 展示数据
        public static ItemTooltipData FromMaterial(CookMaterialData material, ItemTooltipMode mode)
        {
            MaterialJsonData config = material?.Config;
            ItemTooltipData data = new ItemTooltipData
            {
                Name = config?.name ?? "未知材料",
                Icon = material?.Icon,
                Subtitle = buildSubtitle(config),
                Desc = config?.desc
            };

            if (mode != ItemTooltipMode.Cook && config != null)
                data.PriceText = $"价格 {config.price}";

            addTags(data, material, config);
            addBasicFields(data, material, config);
            data.ProcessText = buildProcessText(config);
            data.EffectText = buildEffectText(config);
            return data;
        }

        // 添加字段
        public void AddField(string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            Fields.Add(new ItemTooltipFieldData(label, value));
        }

        // 构建副标题
        private static string buildSubtitle(MaterialJsonData config)
        {
            if (config == null) return string.Empty;

            List<string> parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(config.quality)) parts.Add(config.quality);
            if (!string.IsNullOrWhiteSpace(config.category)) parts.Add(config.category);
            if (!string.IsNullOrWhiteSpace(config.faction)) parts.Add(config.faction);
            return string.Join(" / ", parts);
        }

        // 添加运行时标签与配置标签
        private static void addTags(ItemTooltipData data, CookMaterialData material, MaterialJsonData config)
        {
            if (!string.IsNullOrWhiteSpace(material?.TagText))
            {
                string[] runtimeTags = material.TagText.Split('/');
                for (int i = 0; i < runtimeTags.Length; i++)
                    if (!string.IsNullOrWhiteSpace(runtimeTags[i]) && !data.Tags.Contains(runtimeTags[i]))
                        data.Tags.Add(runtimeTags[i]);
            }

            if (config?.tags == null) return;

            for (int i = 0; i < config.tags.Length; i++)
                if (!string.IsNullOrWhiteSpace(config.tags[i]) && !data.Tags.Contains(config.tags[i]))
                    data.Tags.Add(config.tags[i]);
        }

        // 添加基础状态字段
        private static void addBasicFields(ItemTooltipData data, CookMaterialData material, MaterialJsonData config)
        {
            if (material != null)
            {
                string valueText = config == null
                    ? material.CurrentValue.ToString()
                    : $"{material.CurrentValue} / 基础 {config.baseValue}";
                data.AddField("数值", valueText);
                data.AddField("状态", material.IsProcessed ? "已研磨" : string.IsNullOrWhiteSpace(config?.initialState) ? "原始材料" : config.initialState);
                data.AddField("熟度", material.CookProgressText);
            }

            if (config == null) return;

            data.AddField("加工", config.canProcess ? "可加工" : "不可加工");
            if (!string.IsNullOrWhiteSpace(config.triggerTiming))
                data.AddField("触发", config.triggerTiming);
        }

        // 构建加工信息文本
        private static string buildProcessText(MaterialJsonData config)
        {
            if (config == null || (!config.canProcess && string.IsNullOrWhiteSpace(config.processMethods) && string.IsNullOrWhiteSpace(config.processResult)))
                return string.Empty;

            List<string> lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(config.processMethods))
                lines.Add($"加工方式：{config.processMethods}");
            if (!string.IsNullOrWhiteSpace(config.processResult))
                lines.Add($"加工结果：{config.processResult}");
            return string.Join("\n", lines);
        }

        // 构建效果信息文本
        private static string buildEffectText(MaterialJsonData config)
        {
            if (config == null) return string.Empty;

            List<string> lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(config.triggerCondition))
                lines.Add($"条件：{config.triggerCondition}");
            if (!string.IsNullOrWhiteSpace(config.effectType))
                lines.Add($"效果：{config.effectType}");
            if (!string.IsNullOrWhiteSpace(config.effectTarget))
                lines.Add($"目标：{config.effectTarget}");
            if (!string.IsNullOrWhiteSpace(config.effectParam))
                lines.Add($"参数：{config.effectParam}");
            if (!string.IsNullOrWhiteSpace(config.multiplierParam))
                lines.Add($"倍率：{config.multiplierParam}");
            return string.Join("\n", lines);
        }
    }
}
