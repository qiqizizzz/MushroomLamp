using System;
using System.Collections.Generic;
using MVC.Model;
using UnityEngine;

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
        public int Gold { get; private set; }
        public readonly List<ShopSlotData> CardSlots = new();
        public readonly List<ShopSlotData> ItemSlots = new();

        public void Refresh(int? gold = null)
        {
            Gold = gold ?? UnityEngine.Random.Range(18, 48);
            CardSlots.Clear();
            ItemSlots.Clear();
            CardSlots.AddRange(ShopCatalog.RandomCards(3));
            ItemSlots.AddRange(ShopCatalog.RandomItems(3));
        }

        public void SetGold(int gold)
        {
            Gold = Mathf.Max(0, gold);
        }
    }
}
