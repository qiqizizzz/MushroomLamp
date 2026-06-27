using System;

namespace Module.Card
{
    [Serializable]
    public class CardDataCatalog
    {
        public CardDataEntry[] cards;
    }

    // 数据层单条记录：每张卡只填写自己需要的字段，其余默认 0
    [Serializable]
    public class CardDataEntry
    {
        public string id;           // 唯一标识符，如 "herb_carrot"
        public string name;         // 材料名，与 Box JSON 中 label 保持一致，用作注册表 key
        public string abilityType;  // 对应行为类型：carrot / potato / mushroom / pumpkin / mineral / spice
        public int    baseValue;
        public float  requiredCookValue;
        public string tag;

        // ── 各卡专属字段（未填写时默认 0）──
        public int bonusPerRootInPot;   // carrot：锅中每个同标签材料获得加分
        public int processBonus;         // potato：研磨加值（覆盖标准值）
        public int centerSlotIndex;      // mushroom：触发额外加分的法阵槽位
        public int centerSlotBonus;      // mushroom：放入中心槽位时的额外加分
        public int submitBonus;          // pumpkin：入锅时固定额外加分
        public int targetExpand;         // mineral：入锅时目标上限扩展值
        public int crossTagBonus;        // spice：锅中每个非同标签材料获得的加分
    }
}
