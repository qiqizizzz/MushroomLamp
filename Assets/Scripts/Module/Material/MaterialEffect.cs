/*
* ┌──────────────────────────────────┐
* │  描    述: 材料卡牌效果入口（兼容旧调用，内部走配置驱动计算器）
* │  类    名: MaterialEffect.cs
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Cook;

namespace Module.Material
{
    public static class MaterialEffect
    {
        // 返回该材料在本批中由自身配置产生的 flat 加分（用于旧预览逻辑）
        public static int CalcBonus(IReadOnlyList<CookMaterialData> batch, int index)
        {
            return MaterialBatchEffectCalculator.PreviewFlatBonus(batch, index);
        }
    }
}
