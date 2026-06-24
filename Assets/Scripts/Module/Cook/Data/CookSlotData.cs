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
        public bool HasMaterial => Material != null;

        public CookSlotData(int slotIndex)
        {
            SlotIndex = slotIndex;
        }

        // 放入材料并记录顺序
        public void Place(CookMaterialData material, int order)
        {
            Material = material;
            Order = order;
        }

        // 清空槽位
        public CookMaterialData Clear()
        {
            CookMaterialData material = Material;
            Material = null;
            Order = 0;
            return material;
        }
    }
}
