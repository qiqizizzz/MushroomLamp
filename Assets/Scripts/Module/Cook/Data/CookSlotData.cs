/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪玩法法阵槽位数据，记录材料位置与放置顺序
* │  类    名: CookSlotData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Cook
{
    // 烹饪玩法法阵槽位数据，记录材料位置与放置顺序
    public class CookSlotData
    {
        public int SlotIndex { get; private set; }
        public CookMaterialData Material { get; private set; }
        public int Order { get; private set; }
        public CookSlotType SlotType { get; private set; }
        public float EnchantValue { get; private set; }
        public bool HasMaterial => Material != null;
        public string EnchantText => EnchantValue.ToString("0.#");

        public CookSlotData(int slotIndex)
        {
            SlotIndex = slotIndex;
            SlotType = resolveSlotType(slotIndex);
            EnchantValue = resolveEnchantValue(SlotType);
        }

        // 放入材料并记录顺序
        public void Place(CookMaterialData material, int order)
        {
            Material = material;
            Order = order;
        }

        // 将另一个槽位的材料移动到当前槽位
        public void MoveFrom(CookSlotData source)
        {
            if (source == null) return;

            Material = source.Material;
            Order = source.Order;
            source.Material = null;
            source.Order = 0;
        }

        // 与另一个槽位交换材料和放置顺序
        public void SwapWith(CookSlotData other)
        {
            if (other == null) return;

            CookMaterialData material = Material;
            int order = Order;

            Material = other.Material;
            Order = other.Order;
            other.Material = material;
            other.Order = order;
        }

        // 清空槽位
        public CookMaterialData Clear()
        {
            CookMaterialData material = Material;
            Material = null;
            Order = 0;
            return material;
        }

        // 根据槽位索引解析类型：0=最大格，1~4=中格，5~8=小格
        private static CookSlotType resolveSlotType(int slotIndex)
        {
            if (slotIndex == 0)
                return CookSlotType.Center;

            if (slotIndex >= 1 && slotIndex <= 4)
                return CookSlotType.Edge;

            return CookSlotType.Corner;
        }

        // 根据槽位类型解析附魔加成值：最大格 +2，中格 +1，小格 +0.5
        private static float resolveEnchantValue(CookSlotType slotType)
        {
            return slotType switch
            {
                CookSlotType.Center => 2f,
                CookSlotType.Edge => 1f,
                _ => 0.5f
            };
        }
    }
}
