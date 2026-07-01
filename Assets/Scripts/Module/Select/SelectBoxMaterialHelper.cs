/*
* ┌──────────────────────────────────┐
* │  描    述: 从材料箱配置提取烹饪材料种子
* │  类    名: SelectBoxMaterialHelper.cs
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Cook;

namespace Module.Select
{
    public static class SelectBoxMaterialHelper
    {
        public static List<CookMaterialSeedData> CollectMaterials(SelectBoxDetailJsonConfig detail)
        {
            var result = new List<CookMaterialSeedData>();
            SelectMaterialLineData[] lines = detail?.ToRuntimeLines();
            if (lines == null) return result;

            for (int i = 0; i < lines.Length; i++)
            {
                SelectMaterialLineData line = lines[i];
                if (line == null || (string.IsNullOrWhiteSpace(line.materialId) && string.IsNullOrWhiteSpace(line.label)))
                    continue;

                result.Add(new CookMaterialSeedData
                {
                    MaterialId = line.materialId,
                    MaterialName = line.label,
                    Count = line.count,
                    Icon = line.icon
                });
            }

            return result;
        }
    }
}
