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
        public const string FIELD_BASIC_SCORE = "BasicScore";
        public const string FIELD_STATE = "State";
        public const string FIELD_COOK_PROGRESS = "CookProgress";
        public const string FIELD_CAN_PROCESS = "CanProcess";
        public const string FIELD_PROCESS_METHOD = "ProcessMethod";
        public const string FIELD_TRIGGER_CONDITION = "TriggerCondition";
        public const string FIELD_EFFECT = "Effect";
        public const string FIELD_MULTIPLIER = "Multiplier";
        public const string FIELD_PROCESS_RESULT = "ProcessResult";
        private const string EMPTY_TEXT = "无";

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
            data.ProcessText = string.Empty;
            data.EffectText = string.Empty;
            return data;
        }

        // 添加字段
        public void AddField(string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            Fields.Add(new ItemTooltipFieldData(label, value));
        }

        // 添加带固定字段标识的字段
        public void AddField(string key, string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            Fields.Add(new ItemTooltipFieldData(key, label, value));
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
                data.AddField(FIELD_BASIC_SCORE, "基础分值", (config?.baseValue ?? material.CurrentValue).ToString());
                data.AddField(FIELD_STATE, "状态", material.IsProcessed ? "已研磨" : string.IsNullOrWhiteSpace(config?.initialState) ? "原始材料" : config.initialState);
                data.AddField(FIELD_COOK_PROGRESS, "熟度", material.CookProgressText);
            }

            if (config == null) return;

            data.AddField(FIELD_CAN_PROCESS, "是否可加工", config.canProcess ? "可加工" : "不可加工");
            data.AddField(FIELD_PROCESS_METHOD, "加工方式", formatOptionalText(config.processMethods));
            data.AddField(FIELD_TRIGGER_CONDITION, "触发条件", formatOptionalText(config.triggerCondition));
            data.AddField(FIELD_EFFECT, "效果", buildEffectSummary(config));
            data.AddField(FIELD_MULTIPLIER, "倍率", formatOptionalText(config.multiplierParam));
            data.AddField(FIELD_PROCESS_RESULT, "加工结果", formatOptionalText(config.processResult));
        }

        // 构建效果变量展示文本
        private static string buildEffectSummary(MaterialJsonData config)
        {
            string effectType = formatOptionalText(config.effectType);
            string effectTarget = formatOptionalText(config.effectTarget);
            string effectParam = formatOptionalText(config.effectParam);

            if (effectType == EMPTY_TEXT && effectTarget == EMPTY_TEXT && effectParam == EMPTY_TEXT)
                return EMPTY_TEXT;

            if (effectParam != EMPTY_TEXT && effectTarget != EMPTY_TEXT)
            {
                if (effectType == "加分" || effectType == "减分")
                    return $"{effectTarget}分数 {effectParam}";

                return $"{effectTarget} {effectParam}";
            }

            if (effectType != EMPTY_TEXT && effectTarget != EMPTY_TEXT)
                return $"{effectTarget} / {effectType}";

            if (effectParam != EMPTY_TEXT)
                return effectParam;

            return effectType;
        }

        // 统一把空值显示为无
        private static string formatOptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? EMPTY_TEXT : value;
        }
    }
}
