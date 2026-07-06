/*
* ┌──────────────────────────────────┐
* │  描    述: 按 MaterialCatalog 配置计算一批投锅材料的卡牌效果
* │  类    名: MaterialBatchEffectCalculator.cs
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Module.Cook;
using UnityEngine;

namespace Module.Material
{
    public static class MaterialBatchEffectCalculator
    {
        public struct EntryEffectModifiers
        {
            public float Multiplier;
            public float FlatBonus;
        }

        // 计算本批每条材料的倍率与 flat 加分（原始材料 + 加工态材料，triggerTiming=结算时）
        public static EntryEffectModifiers[] Calculate(
            IReadOnlyList<CookMaterialData> batch,
            IReadOnlyList<CookPotEntryData> entries,
            int turnProcessCount = 0)
        {
            if (batch == null || entries == null || batch.Count == 0)
                return Array.Empty<EntryEffectModifiers>();

            int count = Mathf.Min(batch.Count, entries.Count);
            EntryEffectModifiers[] mods = new EntryEffectModifiers[count];
            for (int i = 0; i < count; i++)
                mods[i].Multiplier = 1f;

            for (int i = 0; i < count; i++)
                applyStandardEffects(batch, mods, i, turnProcessCount);

            for (int i = 0; i < count; i++)
                applyCopyAddEffect(batch, mods, i, turnProcessCount);

            return mods;
        }

        // 预估某张牌在本批中的 flat 加分（不含倍率），供预览与复制效果使用
        public static int PreviewFlatBonus(IReadOnlyList<CookMaterialData> batch, int index, int turnProcessCount = 0)
        {
            if (batch == null || index < 0 || index >= batch.Count) return 0;

            MaterialJsonData config = batch[index]?.Config;
            if (!isSettleMaterial(config)) return 0;
            if (config.effectType != "加分" && config.effectType != "减分") return 0;
            if (!evaluateCondition(batch, index, config.triggerCondition, turnProcessCount)) return 0;

            int value = parseSignedInt(config.effectParam);
            return applyTargetSign(config.effectType, value);
        }

        private static void applyStandardEffects(
            IReadOnlyList<CookMaterialData> batch,
            EntryEffectModifiers[] mods,
            int index,
            int turnProcessCount)
        {
            MaterialJsonData config = batch[index]?.Config;
            if (!isSettleMaterial(config)) return;
            if (!string.Equals(config.triggerTiming, "结算时", StringComparison.Ordinal)) return;
            if (!evaluateCondition(batch, index, config.triggerCondition, turnProcessCount)) return;

            switch (config.effectType)
            {
                case "加分":
                case "减分":
                    applyFlatEffect(mods, index, config, parseSignedInt(config.effectParam));
                    break;
                case "倍率":
                    applyMultiplierEffect(mods, index, config, config.multiplierParam);
                    break;
            }
        }

        private static void applyCopyAddEffect(
            IReadOnlyList<CookMaterialData> batch,
            EntryEffectModifiers[] mods,
            int index,
            int turnProcessCount)
        {
            MaterialJsonData config = batch[index]?.Config;
            if (!isSettleMaterial(config)) return;
            if (config.effectType != "加分复制") return;
            if (!string.Equals(config.triggerTiming, "结算时", StringComparison.Ordinal)) return;
            if (!evaluateCondition(batch, index, config.triggerCondition, turnProcessCount)) return;

            int nextIndex = index + 1;
            if (nextIndex >= batch.Count) return;

            MaterialJsonData nextConfig = batch[nextIndex]?.Config;
            if (nextConfig == null || nextConfig.effectType != "加分") return;

            int copied = PreviewFlatBonus(batch, nextIndex, turnProcessCount);
            if (copied == 0) return;

            int half = Mathf.FloorToInt(Mathf.Abs(copied) * 0.5f);
            if (half == 0) return;

            half = copied > 0 ? half : -half;
            applyFlatEffect(mods, nextIndex, nextConfig, half);
        }

        private static void applyFlatEffect(
            EntryEffectModifiers[] mods,
            int sourceIndex,
            MaterialJsonData config,
            int value)
        {
            if (value == 0) return;

            int targetIndex = resolveTargetIndex(sourceIndex, config.effectTarget, mods.Length);
            if (targetIndex < 0) return;

            mods[targetIndex].FlatBonus += value;
        }

        private static void applyMultiplierEffect(
            EntryEffectModifiers[] mods,
            int sourceIndex,
            MaterialJsonData config,
            float multiplier)
        {
            if (multiplier <= 0f || Mathf.Approximately(multiplier, 1f)) return;

            int targetIndex = resolveTargetIndex(sourceIndex, config.effectTarget, mods.Length);
            if (targetIndex < 0) return;

            mods[targetIndex].Multiplier *= multiplier;
        }

        private static int resolveTargetIndex(int sourceIndex, string effectTarget, int count)
        {
            if (string.IsNullOrWhiteSpace(effectTarget)) return sourceIndex;

            if (effectTarget == "自身")
                return sourceIndex;

            if (effectTarget == "下一个材料")
            {
                int nextIndex = sourceIndex + 1;
                return nextIndex < count ? nextIndex : -1;
            }

            return sourceIndex;
        }

        private static bool isSettleMaterial(MaterialJsonData config)
        {
            if (config == null) return false;

            return config.initialState == "原始材料"
                || config.initialState == "研磨状态"
                || config.initialState == "切碎状态"
                || config.initialState == "完美加工状态";
        }

        private static bool evaluateCondition(
            IReadOnlyList<CookMaterialData> batch,
            int index,
            string condition,
            int turnProcessCount)
        {
            if (string.IsNullOrWhiteSpace(condition) || condition == "无") return false;

            CookMaterialData prev = index > 0 ? batch[index - 1] : null;
            CookMaterialData next = index < batch.Count - 1 ? batch[index + 1] : null;

            switch (condition)
            {
                case "前一个材料标签包含叶菜":
                    return prev != null && prev.Config.HasTag("叶菜");
                case "下一个材料大类为蔬菜":
                case "后一个材料大类为蔬菜":
                    return next != null && next.Config.category == "蔬菜";
                case "本回合已经放入过叶菜材料":
                    return hasTagBefore(batch, index, "叶菜");
                case "前一个或后一个材料标签包含根茎":
                    return (prev != null && prev.Config.HasTag("根茎"))
                        || (next != null && next.Config.HasTag("根茎"));
                case "本回合锅中已有 2 个及以上蔬菜材料":
                    return countCategory(batch, "蔬菜") >= 2;
                case "本回合存在 3 个及以上蔬菜材料":
                    return countCategory(batch, "蔬菜") >= 3;
                case "本回合加工过 2 次及以上":
                    return turnProcessCount >= 2;
                case "下一个材料标签包含根茎":
                    return next != null && next.Config.HasTag("根茎");
                case "前一个和后一个材料大类都为蔬菜":
                    return prev != null && prev.Config.category == "蔬菜"
                        && next != null && next.Config.category == "蔬菜";
                case "下一个材料拥有加法效果":
                    return next != null && next.Config.effectType == "加分";
                default:
                    return false;
            }
        }

        private static int applyTargetSign(string effectType, int value)
        {
            if (effectType == "减分")
                return -Mathf.Abs(value);
            return value;
        }

        private static int parseSignedInt(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            string trimmed = text.Trim();
            if (trimmed.StartsWith("+"))
                trimmed = trimmed.Substring(1);

            return int.TryParse(trimmed, out int value) ? value : 0;
        }

        private static bool hasTagBefore(IReadOnlyList<CookMaterialData> batch, int index, string tag)
        {
            for (int i = 0; i < index; i++)
            {
                if (batch[i] != null && batch[i].Config.HasTag(tag))
                    return true;
            }

            return false;
        }

        private static int countCategory(IReadOnlyList<CookMaterialData> batch, string category)
        {
            int count = 0;
            for (int i = 0; i < batch.Count; i++)
            {
                if (batch[i] != null && batch[i].Config.category == category)
                    count++;
            }

            return count;
        }
    }
}
