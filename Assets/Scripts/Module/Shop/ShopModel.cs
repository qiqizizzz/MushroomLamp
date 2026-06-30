/*
* ┌──────────────────────────────────┐
* │  描    述: 商店数据模型，负责货架内容与本轮回收状态
* │  类    名: ShopModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

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
        public bool isCard;
        public bool isPurchased;
    }

    public class ShopModel : BaseModel
    {
        // 金币直接读玩家单例，不再随机
        public int Gold => PlayerDataManager.Instance.Money;
        public bool HasRecycled { get; private set; }
        public bool CanRecycle => !HasRecycled;

        public readonly List<ShopSlotData> CardSlots = new();
        public readonly List<ShopSlotData> ItemSlots = new();

        // 刷新商店货架
        public void Refresh()
        {
            CardSlots.Clear();
            ItemSlots.Clear();
            CardSlots.AddRange(ShopCatalog.RandomCards(3));
            ItemSlots.AddRange(ShopCatalog.RandomItems(3));
        }

        // 重置本次进入商店的回收状态
        public void ResetRecycleState()
        {
            HasRecycled = false;
        }

        // 标记本次商店已经完成回收
        public void MarkRecycled()
        {
            HasRecycled = true;
        }
    }
}
