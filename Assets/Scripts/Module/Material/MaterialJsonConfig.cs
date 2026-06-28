/*
* ┌──────────────────────────────────┐
* │  描    述: 材料卡牌配置 JSON 结构（来自飞书卡牌配置表）
* │  类    名: MaterialJsonConfig.cs
* └──────────────────────────────────┘
*/

using System;

namespace Module.Material
{
    [Serializable]
    public class MaterialCatalogJsonConfig
    {
        public MaterialJsonData[] materials;
    }

    // 单条材料卡牌配置。效果相关字段（触发条件/效果参数等）当前仅做数据承载，逻辑后续再实现
    [Serializable]
    public class MaterialJsonData
    {
        public string id;                  // 材料ID，如 VEG_001
        public string name;                // 名称
        public string iconPath;            // 图标路径（Addressable address）
        public string category;            // 大类：蔬菜/道具/废料
        public string[] tags;              // 标签：根茎/叶菜/果实/烧焦...
        public string initialState;        // 初始状态：原始材料/研磨状态/切碎状态/完美加工状态/烧焦状态/无
        public string faction;             // 所属流派
        public int baseValue;              // 基础分值
        public int requiredCookValue;      // 所需熟度（煮够这个值才算熟）
        public string quality;             // 品质：普通/优秀/稀有/核心/负面
        public int price;                  // 价格

        public bool canProcess;            // 是否可加工
        public string processMethods;      // 可加工方式（文本）
        public string processResult;       // 加工结果（文本）

        public string triggerTiming;       // 触发时机：放入时/结算时/加工时/回合结算时/使用时/无
        public string triggerCondition;    // 触发条件（自然语言，暂作数据承载）
        public string effectType;          // 效果类型：加分/倍率/加工/状态变化/加分复制/减分...
        public string effectTarget;        // 效果目标：自身/下一个材料/最终分数/指定材料
        public string effectParam;         // 效果参数（如 +2）
        public string multiplierParam;     // 倍率参数（如 ×1.5）

        public bool isCore;                // 是否核心牌
        public string priority;            // 优先级：高/中/低
        public string desc;                // 说明

        // 标签包含判断
        public bool HasTag(string tag)
        {
            if (tags == null || string.IsNullOrEmpty(tag)) return false;
            foreach (string t in tags)
                if (t == tag) return true;
            return false;
        }
    }
}
