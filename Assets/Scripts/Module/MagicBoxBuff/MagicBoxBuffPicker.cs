using System.Collections.Generic;
using UnityEngine;

namespace Module.MagicBoxBuff
{
    // 按配置权重抽取魔盒 Buff 候选（尽量不同类别）
    public static class MagicBoxBuffPicker
    {
        public static List<MagicBoxBuffJsonData> RollCandidates(int count = 0)
        {
            MagicBoxBuffCatalogLoader.EnsureLoaded();
            if (count <= 0)
                count = MagicBoxBuffCatalogLoader.PickCandidateCount;

            IReadOnlyList<MagicBoxBuffJsonData> pool = MagicBoxBuffCatalogLoader.GetAll();
            var result = new List<MagicBoxBuffJsonData>(count);
            if (pool.Count == 0) return result;

            var usedCategories = new HashSet<string>();
            var pickedIds = new HashSet<string>();

            for (int i = 0; i < count; i++)
            {
                MagicBoxBuffJsonData pick = rollOne(pool, usedCategories, pickedIds);
                if (pick == null) break;

                result.Add(pick);
                pickedIds.Add(pick.id);
                if (!string.IsNullOrWhiteSpace(pick.category))
                    usedCategories.Add(pick.category);
            }

            return result;
        }

        private static MagicBoxBuffJsonData rollOne(
            IReadOnlyList<MagicBoxBuffJsonData> pool,
            HashSet<string> usedCategories,
            HashSet<string> pickedIds)
        {
            int totalWeight = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                MagicBoxBuffJsonData buff = pool[i];
                if (buff == null || pickedIds.Contains(buff.id)) continue;
                if (!string.IsNullOrWhiteSpace(buff.category) && usedCategories.Contains(buff.category))
                    continue;
                totalWeight += Mathf.Max(1, buff.baseWeight);
            }

            if (totalWeight <= 0)
            {
                for (int i = 0; i < pool.Count; i++)
                {
                    MagicBoxBuffJsonData buff = pool[i];
                    if (buff == null || pickedIds.Contains(buff.id)) continue;
                    totalWeight += Mathf.Max(1, buff.baseWeight);
                }
            }

            if (totalWeight <= 0) return null;

            int roll = Random.Range(0, totalWeight);
            int cursor = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                MagicBoxBuffJsonData buff = pool[i];
                if (buff == null || pickedIds.Contains(buff.id)) continue;
                if (!string.IsNullOrWhiteSpace(buff.category) && usedCategories.Contains(buff.category)
                    && totalWeight > Mathf.Max(1, buff.baseWeight))
                    continue;

                cursor += Mathf.Max(1, buff.baseWeight);
                if (roll < cursor) return buff;
            }

            for (int i = 0; i < pool.Count; i++)
            {
                MagicBoxBuffJsonData buff = pool[i];
                if (buff != null && !pickedIds.Contains(buff.id))
                    return buff;
            }

            return null;
        }
    }
}
