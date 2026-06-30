using System;
using System.Collections.Generic;
using Module.Player;
using MVC.Model;

namespace Module.Shop
{
    [Serializable]
    public class ShopSlotData
    {
        public string id;
        public string name;
        public string iconPath;
        public string description;
        public int price;
        public bool isBox;
        public bool isCard;
        public bool isPurchased;
    }

    public class ShopModel : BaseModel
    {
        // 金币直接读玩家单例，不再随机
        public int Gold => PlayerDataManager.Instance.Money;
        // 上排：材料箱；下排：道具
        public readonly List<ShopSlotData> BoxSlots = new();
        public readonly List<ShopSlotData> ItemSlots = new();

        public void Refresh()
        {
            BoxSlots.Clear();
            ItemSlots.Clear();
            BoxSlots.AddRange(ShopCatalog.RandomBoxes(3));
            ItemSlots.AddRange(ShopCatalog.RandomItems(3));
        }
    }
}
