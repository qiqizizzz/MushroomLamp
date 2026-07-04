/*
* ┌──────────────────────────────────┐
* │  描    述: 回收界面数据模型，负责随机材料候选与右侧清单数据
* │  类    名: RecycleModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Module.Material;
using Module.Player;
using MVC.Model;
using UnityEngine;

namespace Module.Recycle
{
    [Serializable]
    public class RecycleOfferData
    {
        public string id;
        public string name;
        public string iconPath;
        public string category;
        public string description;
        public int price;
    }

    [Serializable]
    public class RecycleInventoryEntryData
    {
        public string id;
        public string name;
        public string iconPath;
        public string category;
        public int count;
        public bool isCard;
    }

    // 回收界面数据模型，负责随机材料候选与右侧清单数据
    public class RecycleModel : BaseModel
    {
        public const int OfferCount = 5;

        public readonly List<RecycleOfferData> Offers = new();
        public readonly List<RecycleInventoryEntryData> InventoryEntries = new();

        public int Gold => PlayerDataManager.Instance.Money;

        // 刷新回收界面全部数据
        public void RefreshAll()
        {
            RefreshOffers();
            RefreshInventory();
        }

        // 从材料配置中随机取出本次可回收的 5 个材料
        public void RefreshOffers()
        {
            Offers.Clear();

            var allMaterials = MaterialCatalogLoader.GetAll();
            var pool = new List<MaterialJsonData>();
            foreach (MaterialJsonData material in allMaterials)
            {
                if (material == null || string.IsNullOrEmpty(material.id)) continue;
                if (material.price <= 0) continue;
                pool.Add(material);
            }

            for (int i = 0; i < OfferCount && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                MaterialJsonData material = pool[index];
                pool.RemoveAt(index);
                Offers.Add(buildOffer(material));
            }
        }

        // 刷新右侧清单：玩家卡牌 + 当前材料候选
        public void RefreshInventory()
        {
            InventoryEntries.Clear();
            foreach (PlayerCardState card in PlayerDataManager.Instance.GetAllCards())
            {
                MaterialJsonData meta = MaterialCatalogLoader.GetById(card.cardId);
                InventoryEntries.Add(new RecycleInventoryEntryData
                {
                    id = card.cardId,
                    name = meta != null ? meta.name : card.cardId,
                    iconPath = meta != null ? meta.iconPath : string.Empty,
                    category = meta != null && !string.IsNullOrEmpty(meta.category) ? meta.category : "卡牌",
                    count = card.count,
                    isCard = true
                });
            }

            foreach (RecycleOfferData offer in Offers)
            {
                InventoryEntries.Add(new RecycleInventoryEntryData
                {
                    id = offer.id,
                    name = offer.name,
                    iconPath = offer.iconPath,
                    category = string.IsNullOrEmpty(offer.category) ? "材料" : offer.category,
                    count = 1,
                    isCard = false
                });
            }
        }

        // 卖出一个候选材料并返回获得金币
        public bool SellOffer(RecycleOfferData offer, out int gold)
        {
            gold = 0;
            if (offer == null || string.IsNullOrEmpty(offer.id)) return false;

            for (int i = 0; i < Offers.Count; i++)
            {
                if (Offers[i].id != offer.id) continue;

                gold = Mathf.Max(0, Offers[i].price);
                Offers.RemoveAt(i);
                RefreshInventory();
                return true;
            }

            return false;
        }

        private static RecycleOfferData buildOffer(MaterialJsonData material)
        {
            return new RecycleOfferData
            {
                id = material.id,
                name = material.name,
                iconPath = material.iconPath,
                category = material.category,
                description = material.desc,
                price = material.price
            };
        }
    }
}
