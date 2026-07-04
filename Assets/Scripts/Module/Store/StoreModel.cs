/*
* ┌──────────────────────────────────┐
* │  描    述: 商店子页面数据模型（购买槽 + 玩家背包卡牌）
* │  类    名: StoreModel.cs
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common;
using Module.Material;
using Module.Player;
using Module.Shop;
using MVC.Model;
using UnityEngine;

namespace Module.Store
{
    [Serializable]
    public class StoreBuySlotData
    {
        public string id;
        public string name;
        public string iconPath;
        public string description;
        public int price;
        public bool isPurchased;
    }

    [Serializable]
    public class StoreBagEntryData
    {
        public string id;
        public string name;
        public string iconPath;
        public int count;
    }

    public class StoreModel : BaseModel
    {
        public const int BuySlotCount = 3;

        public int Gold => PlayerDataManager.Instance.Money;

        public readonly List<StoreBuySlotData> BuySlots = new();
        public readonly List<StoreBagEntryData> BagEntries = new();

        public string CurrentBoxId { get; private set; }
        public string CurrentBoxName { get; private set; }
        public bool CardsIncludedInBoxPrice { get; private set; }

        public bool HasBoxPickCompleted()
        {
            if (!CardsIncludedInBoxPrice) return false;
            foreach (StoreBuySlotData slot in BuySlots)
            {
                if (slot != null && slot.isPurchased)
                    return true;
            }
            return false;
        }

        public int OverrideBagCount { get; private set; } = -1;

        public void SetupForBox(StoreOpenContext context)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.boxId))
            {
                ClearBoxContext();
                return;
            }

            CurrentBoxId = context.boxId;
            CurrentBoxName = context.boxName;
            CardsIncludedInBoxPrice = context.cardsIncludedInBoxPrice;
            RefreshBuySlotsFromBox(context.boxId);
        }

        public void ClearBoxContext()
        {
            CurrentBoxId = null;
            CurrentBoxName = null;
            CardsIncludedInBoxPrice = false;
        }

        public void SetBagCount(int count)
        {
            OverrideBagCount = count;
        }

        public void RefreshBuySlots()
        {
            ClearBoxContext();
            refreshBuySlotsFromMaterialCatalog(-1);
        }

        // 从商店材料箱池随机抽 3 张材料供玩家三选一
        public void RefreshBuySlotsFromBox(string boxId)
        {
            ShopBoxPoolJsonConfig pool = ShopCatalog.LoadBoxPoolByBoxId(boxId);
            refreshBuySlotsFromMaterialPool(pool?.materialIds, CardsIncludedInBoxPrice ? 0 : -1);
        }

        private void refreshBuySlotsFromMaterialPool(string[] materialIds, int overridePrice = -1)
        {
            BuySlots.Clear();
            if (materialIds == null || materialIds.Length == 0)
            {
                QLog.Warning($"[{nameof(StoreModel)}] 材料箱池为空：boxId={CurrentBoxId}");
                return;
            }

            var pool = new List<string>();
            foreach (string materialId in materialIds)
            {
                if (!string.IsNullOrWhiteSpace(materialId))
                    pool.Add(materialId);
            }

            for (int i = 0; i < BuySlotCount && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                string materialId = pool[index];
                pool.RemoveAt(index);

                MaterialJsonData meta = MaterialCatalogLoader.GetById(materialId);
                if (meta == null)
                {
                    QLog.Warning($"[{nameof(StoreModel)}] 材料配置缺失：{materialId}");
                    continue;
                }

                addBuySlot(meta, overridePrice);
            }
        }

        private void refreshBuySlotsFromMaterialCatalog(int overridePrice = -1)
        {
            BuySlots.Clear();

            var pool = new List<MaterialJsonData>();
            foreach (MaterialJsonData material in MaterialCatalogLoader.GetAll())
            {
                if (material != null && !string.IsNullOrWhiteSpace(material.id))
                    pool.Add(material);
            }

            for (int i = 0; i < BuySlotCount && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                MaterialJsonData meta = pool[index];
                pool.RemoveAt(index);
                addBuySlot(meta, overridePrice);
            }
        }

        private void addBuySlot(MaterialJsonData meta, int overridePrice)
        {
            if (meta == null) return;

            int price = overridePrice >= 0 ? overridePrice : meta.price;
            BuySlots.Add(new StoreBuySlotData
            {
                id = meta.id,
                name = meta.name,
                iconPath = meta.iconPath,
                description = string.IsNullOrEmpty(CurrentBoxName)
                    ? meta.desc
                    : $"来自「{CurrentBoxName}」\n{meta.desc}",
                price = price
            });
        }

        public void RefreshBag()
        {
            BagEntries.Clear();

            if (OverrideBagCount > 0)
            {
                BuildMockBag(OverrideBagCount);
                return;
            }

            foreach (PlayerCardState card in PlayerDataManager.Instance.GetAllCards())
                fillBagEntryMeta(card.cardId, card.count);
        }

        private void fillBagEntryMeta(string id, int count)
        {
            MaterialJsonData material = MaterialCatalogLoader.GetById(id);
            BagEntries.Add(new StoreBagEntryData
            {
                id = id,
                name = material != null ? material.name : id,
                iconPath = material != null ? material.iconPath : string.Empty,
                count = count
            });
        }

        private void BuildMockBag(int count)
        {
            IReadOnlyList<MaterialJsonData> source = MaterialCatalogLoader.GetAll();
            for (int i = 0; i < count; i++)
            {
                if (source != null && source.Count > 0)
                {
                    MaterialJsonData data = source[i % source.Count];
                    BagEntries.Add(new StoreBagEntryData
                    {
                        id = data.id,
                        name = data.name,
                        iconPath = data.iconPath,
                        count = i + 1
                    });
                }
                else
                {
                    BagEntries.Add(new StoreBagEntryData
                    {
                        id = "mock_" + i,
                        name = "卡" + (i + 1),
                        iconPath = string.Empty,
                        count = i + 1
                    });
                }
            }
        }

        public void RefreshAll()
        {
            if (!string.IsNullOrEmpty(CurrentBoxId))
                RefreshBuySlotsFromBox(CurrentBoxId);
            else
                RefreshBuySlots();
            RefreshBag();
        }
    }
}
