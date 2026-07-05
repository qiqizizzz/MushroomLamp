/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪玩法运行时材料数据
* │          静态配置统一放 Config(MaterialJsonData)，运行时状态放本类
* │  类    名: CookMaterialData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Card;
using Module.Material;
using UnityEngine;

namespace Module.Cook
{
    // 烹饪玩法运行时材料数据：静态配置走 Config，动态状态（熟度/加工/当前值）放本类
    public class CookMaterialData
    {
        // ── 静态配置（来自 MaterialCatalog，运行时不变）外部直接访问 Config.xxx ──
        public MaterialJsonData Config { get; private set; }

        // ── 动态运行时状态（局内会变）──
        public int RuntimeId { get; private set; }
        public int CurrentValue { get; private set; }   // 加工后会变
        public string TagText { get; private set; }      // 初始来自配置，加工后追加
        public bool IsProcessed { get; private set; }
        public float CookProgress { get; private set; }
        public Sprite Icon { get; private set; }
        public CardAbility Ability { get; private set; }

        public string ValueText => IsProcessed ? $"{CurrentValue}*" : CurrentValue.ToString();
        public string CookProgressText => $"{CookRoundResultData.FormatScore(CookProgress)}/{CookRoundResultData.FormatScore(Config?.requiredCookValue ?? 0)}";

        // 用配置 + 运行时上下文构造
        public CookMaterialData(int runtimeId, MaterialJsonData config, Sprite icon, CardAbility ability = null)
        {
            RuntimeId = runtimeId;
            Config = config;
            CurrentValue = config?.baseValue ?? 0;
            TagText = (config?.tags != null && config.tags.Length > 0) ? config.tags[0] : "素材";
            Icon = icon;
            Ability = ability ?? CardAbility.Default;
        }

        // 标记材料进入加工状态（无对应产物配置时的兜底）
        public void MarkProcessed(int valueDelta, string extraTag)
        {
            if (Config == null || !Config.canProcess || IsProcessed) return;

            IsProcessed = true;
            CurrentValue = Mathf.Max(0, CurrentValue + valueDelta);
            if (!string.IsNullOrWhiteSpace(extraTag))
                TagText = $"{TagText}/{extraTag}";
        }

        // 加工完成后替换为产物配置（如土豆 → 土豆泥）
        public void TransformTo(MaterialJsonData resultConfig, Sprite icon, string processMethodLabel)
        {
            if (resultConfig == null) return;

            Config = resultConfig;
            CurrentValue = resultConfig.baseValue;
            IsProcessed = true;
            CookProgress = 0f;
            if (icon != null)
                Icon = icon;

            TagText = resultConfig.tags != null && resultConfig.tags.Length > 0
                ? resultConfig.tags[0]
                : "素材";

            if (!string.IsNullOrWhiteSpace(processMethodLabel))
                TagText = $"{TagText}/{processMethodLabel}";
        }

        // 增加材料在法阵中的烹饪熟度
        public void AddCookProgress(float value)
        {
            CookProgress = Mathf.Max(0f, CookProgress + value);
        }

        // 回收进弃牌堆时重置每局过程状态（熟度），避免洗回再抽时残留上次的烹饪进度
        public void ResetForRecycle()
        {
            CookProgress = 0f;
        }
    }
}
