using System.Collections.Generic;
using Module.Material;
using UnityEngine;

namespace Module.MagicBoxBuff
{
    // 魔盒 Buff 运行时：持有、查询、生效（与商店道具 ItemPassiveManager 分离）
    public static class MagicBoxBuffManager
    {
        public const string EffectAddRoundScoreFlat = "add_round_score_flat";
        public const string EffectAddPerVegetableCap = "add_per_vegetable_cap";
        public const string EffectModifyBustLimit = "modify_bust_limit";
        public const string EffectReduceBustPenalty = "reduce_bust_penalty";
        public const string EffectPickMaterialReward = "pick_material_reward";

        public const string DurationPerRound = "per_round";
        public const string DurationMagicBoxSession = "magic_box_session";
        public const string DurationImmediate = "immediate";

        private static readonly List<string> _roundBuffIds = new();
        private static readonly List<string> _sessionBuffIds = new();

        public static IReadOnlyList<string> RoundBuffIds => _roundBuffIds;
        public static IReadOnlyList<string> SessionBuffIds => _sessionBuffIds;

        public static void ResetRound()
        {
            _roundBuffIds.Clear();
        }

        public static void BeginMagicBoxSession()
        {
            _sessionBuffIds.Clear();
        }

        public static void EndMagicBoxSession()
        {
            _sessionBuffIds.Clear();
        }

        public static void ClearAll()
        {
            _roundBuffIds.Clear();
            _sessionBuffIds.Clear();
        }

        public static bool HasBuff(string buffId)
        {
            return _roundBuffIds.Contains(buffId) || _sessionBuffIds.Contains(buffId);
        }

        public static bool GrantBuff(string buffId)
        {
            MagicBoxBuffJsonData cfg = MagicBoxBuffCatalogLoader.GetById(buffId);
            if (cfg == null) return false;

            List<string> bucket = resolveBucket(cfg.durationType);
            if (bucket == null) return true;

            if (!cfg.stackable && bucket.Contains(buffId))
                return false;

            if (!bucket.Contains(buffId))
                bucket.Add(buffId);

            return true;
        }

        public static bool RemoveBuff(string buffId)
        {
            bool removed = _roundBuffIds.Remove(buffId) | _sessionBuffIds.Remove(buffId);
            return removed;
        }

        public static float GetRoundScoreFlatBonus()
        {
            return sumRoundScoreFlatBonus(DurationPerRound);
        }

        public static float GetVegetableScoreBonus(int vegetableCount)
        {
            if (vegetableCount <= 0) return 0f;

            float perUnit = 0f;
            float cap = float.MaxValue;
            accumulatePerVegetableParams(ref perUnit, ref cap);
            if (perUnit <= 0f) return 0f;

            return Mathf.Min(vegetableCount * perUnit, cap);
        }

        public static bool TryGetPerVegetableBonusParams(out float perUnit, out float cap)
        {
            perUnit = 0f;
            cap = float.MaxValue;
            accumulatePerVegetableParams(ref perUnit, ref cap);
            return perUnit > 0f;
        }

        public static int GetBustLimitBonus()
        {
            return Mathf.RoundToInt(sumBustLimitDelta(DurationMagicBoxSession));
        }

        public static float GetBlackjackBustPenaltyMultiplier()
        {
            float multiplier = 1f;
            foreach (string buffId in _sessionBuffIds)
            {
                MagicBoxBuffJsonData cfg = MagicBoxBuffCatalogLoader.GetById(buffId);
                if (cfg == null || cfg.effectType != EffectReduceBustPenalty) continue;
                if (cfg.bustPenaltyMultiplier > 0f && cfg.bustPenaltyMultiplier < 1f)
                    multiplier *= cfg.bustPenaltyMultiplier;
            }

            return multiplier;
        }

        public static bool NeedsMaterialPick(out MagicBoxBuffJsonData cfg)
        {
            cfg = null;
            foreach (string buffId in EnumerateAllActive())
            {
                MagicBoxBuffJsonData data = MagicBoxBuffCatalogLoader.GetById(buffId);
                if (data == null || data.effectType != EffectPickMaterialReward) continue;
                cfg = data;
                return true;
            }

            return false;
        }

        public static List<MaterialJsonData> RollMaterialRewardCandidates(MagicBoxBuffJsonData cfg)
        {
            var result = new List<MaterialJsonData>();
            if (cfg == null) return result;

            int count = cfg.materialChoiceCount > 0 ? cfg.materialChoiceCount : 3;
            IReadOnlyList<MaterialJsonData> pool = filterMaterialPool(cfg.materialPool);
            if (pool.Count == 0) return result;

            var picked = new HashSet<string>();
            int guard = 0;
            while (result.Count < count && guard++ < pool.Count * 3)
            {
                MaterialJsonData material = pool[Random.Range(0, pool.Count)];
                if (material == null || string.IsNullOrWhiteSpace(material.id)) continue;
                if (!picked.Add(material.id)) continue;
                result.Add(material);
            }

            return result;
        }

        private static IEnumerable<string> EnumerateAllActive()
        {
            for (int i = 0; i < _roundBuffIds.Count; i++)
                yield return _roundBuffIds[i];
            for (int i = 0; i < _sessionBuffIds.Count; i++)
                yield return _sessionBuffIds[i];
        }

        private static List<string> resolveBucket(string durationType)
        {
            return durationType switch
            {
                DurationPerRound => _roundBuffIds,
                DurationMagicBoxSession => _sessionBuffIds,
                DurationImmediate => null,
                _ => _roundBuffIds
            };
        }

        private static float sumRoundScoreFlatBonus(string durationType)
        {
            float sum = 0f;
            List<string> bucket = resolveBucket(durationType);
            if (bucket == null) return 0f;

            foreach (string buffId in bucket)
            {
                MagicBoxBuffJsonData cfg = MagicBoxBuffCatalogLoader.GetById(buffId);
                if (cfg == null || cfg.effectType != EffectAddRoundScoreFlat) continue;
                sum += cfg.roundScoreFlatBonus;
            }

            return sum;
        }

        private static float sumBustLimitDelta(string durationType)
        {
            float sum = 0f;
            List<string> bucket = resolveBucket(durationType);
            if (bucket == null) return 0f;

            foreach (string buffId in bucket)
            {
                MagicBoxBuffJsonData cfg = MagicBoxBuffCatalogLoader.GetById(buffId);
                if (cfg == null || cfg.effectType != EffectModifyBustLimit) continue;
                sum += cfg.bustLimitDelta;
            }

            return sum;
        }

        private static void accumulatePerVegetableParams(ref float perUnit, ref float cap)
        {
            foreach (string buffId in _roundBuffIds)
            {
                MagicBoxBuffJsonData cfg = MagicBoxBuffCatalogLoader.GetById(buffId);
                if (cfg == null || cfg.effectType != EffectAddPerVegetableCap) continue;
                perUnit += cfg.perVegetableBonus;
                if (cfg.vegetableBonusCap > 0f)
                    cap = Mathf.Min(cap, cfg.vegetableBonusCap);
            }
        }

        private static IReadOnlyList<MaterialJsonData> filterMaterialPool(string poolKey)
        {
            var filtered = new List<MaterialJsonData>();
            foreach (MaterialJsonData material in MaterialCatalogLoader.GetAll())
            {
                if (material == null) continue;
                if (poolKey == "basic_vegetable")
                {
                    if (material.category == "蔬菜" && material.quality is "普通" or "优秀")
                        filtered.Add(material);
                    continue;
                }

                filtered.Add(material);
            }

            return filtered;
        }
    }
}
