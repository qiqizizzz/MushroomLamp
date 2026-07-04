using System;

namespace Module.MagicBoxBuff
{
    [Serializable]
    public class MagicBoxBuffCatalogJsonConfig
    {
        public int pickCandidateCount = 3;
        public MagicBoxBuffJsonData[] buffs;
    }

    [Serializable]
    public class MagicBoxBuffJsonData
    {
        public string id;
        public string name;
        public string rarity;
        public string category;
        public string effectType;
        public string effectTarget;
        public string description;
        public string durationType;
        public int baseWeight;
        public int sortOrder;
        public bool stackable;

        // add_round_score_flat（计分校准）：本回合最终分固定加分
        public float roundScoreFlatBonus;

        // add_per_vegetable_cap（星级加算）：每个蔬菜材料加分 + 总分上限
        public float perVegetableBonus;
        public float vegetableBonusCap;

        // modify_bust_limit（小火护锅）：21 点爆牌阈值偏移（如 +2 表示 21→23）
        public float bustLimitDelta;

        // reduce_bust_penalty（天使底护）：爆牌惩罚倍率（0.5 = 减半）
        public float bustPenaltyMultiplier;

        // pick_material_reward（幸运三选一）：展示几个 / 选几个 / 材料池
        public int materialChoiceCount;
        public int materialPickCount;
        public string materialPool;
    }
}
