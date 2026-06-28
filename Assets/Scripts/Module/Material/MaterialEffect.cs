/*
* ┌──────────────────────────────────┐
* │  描    述: 材料卡牌效果（按 id 硬编码触发条件），当前实现「加分」类
* │  类    名: MaterialEffect.cs
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Cook;

namespace Module.Material
{
    // 计算一批投入材料中，单个材料的额外效果加分
    // batch 为本批投入材料（按暂存槽顺序），index 为当前材料在 batch 中的位置
    public static class MaterialEffect
    {
        // 返回该材料在本批中触发的额外加分（仅「加分」类，结算时生效）
        public static int CalcBonus(IReadOnlyList<CookMaterialData> batch, int index)
        {
            if (batch == null || index < 0 || index >= batch.Count) return 0;

            CookMaterialData self = batch[index];
            if (self == null || string.IsNullOrEmpty(self.Config.id)) return 0;

            CookMaterialData prev = index > 0 ? batch[index - 1] : null;
            CookMaterialData next = index < batch.Count - 1 ? batch[index + 1] : null;

            switch (self.Config.id)
            {
                // 胡萝卜：前一个材料标签含「叶菜」→ +2
                case "VEG_002":
                case "VEG_202":   // 胡萝卜丁，同条件
                    return (prev != null && prev.Config.HasTag("叶菜")) ? 2 : 0;

                // 白菜叶：后一个材料大类为「蔬菜」→ +2
                case "VEG_003":
                    return (next != null && next.Config.category == "蔬菜") ? 2 : 0;

                // 生菜叶：前一个材料大类为「蔬菜」→ +2
                case "VEG_004":
                    return (prev != null && prev.Config.category == "蔬菜") ? 2 : 0;

                // 小番茄：本批已放过「叶菜」标签材料（自己之前）→ +1
                case "VEG_005":
                    return hasTagBefore(batch, index, "叶菜") ? 1 : 0;

                // 洋葱：前或后材料标签含「根茎」→ +2
                case "VEG_006":
                    return ((prev != null && prev.Config.HasTag("根茎")) || (next != null && next.Config.HasTag("根茎"))) ? 2 : 0;

                // 蘑菇：本批锅中已有 2 个及以上「蔬菜」大类 → +2
                case "VEG_007":
                    return countCategory(batch, "蔬菜") >= 2 ? 2 : 0;

                // 香草叶：后一个材料标签含「根茎」→ +3
                case "VEG_102":
                    return (next != null && next.Config.HasTag("根茎")) ? 3 : 0;

                // 藤蔓叶：前一个和后一个材料大类都为「蔬菜」→ +4
                case "VEG_103":
                    return (prev != null && prev.Config.category == "蔬菜" && next != null && next.Config.category == "蔬菜") ? 4 : 0;

                // 完美萝卜块 VEG_205：本回合加工 ≥2 次 → +4（加工系统未实现，暂返回 0）
                default:
                    return 0;
            }
        }

        private static bool hasTagBefore(IReadOnlyList<CookMaterialData> batch, int index, string tag)
        {
            for (int i = 0; i < index; i++)
                if (batch[i] != null && batch[i].Config.HasTag(tag)) return true;
            return false;
        }

        private static int countCategory(IReadOnlyList<CookMaterialData> batch, string category)
        {
            int count = 0;
            for (int i = 0; i < batch.Count; i++)
                if (batch[i] != null && batch[i].Config.category == category) count++;
            return count;
        }
    }
}
