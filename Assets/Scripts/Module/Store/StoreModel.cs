/*
* ┌──────────────────────────────────┐
* │  描    述: 商店子页面数据模型（购买槽 + 玩家背包卡牌）
* │  类    名: StoreModel.cs
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common;
using Common.Defines;
using Module.Material;
using Module.Player;
using Module.Shop;
using MVC.Model;

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

        private CardParamCatalogJsonConfig _cardConfig;

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
            refreshBuySlotsFromPool(null);
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
        }

        private void refreshBuySlotsFromPool(HashSet<string> iconFilter, int overridePrice = -1)
        {
            EnsureConfig();
            BuySlots.Clear();

            var source = _cardConfig?.cards;
            if (source == null || source.Length == 0) return;

            var pool = new List<CardParamJsonData>();
            foreach (CardParamJsonData data in source)
            {
                if (data == null) continue;
                if (iconFilter != null && iconFilter.Count > 0)
                {
                    if (string.IsNullOrEmpty(data.iconPath) || !iconFilter.Contains(data.iconPath))
                        continue;
                }
                pool.Add(data);
            }

            if (pool.Count == 0)
            {
                foreach (CardParamJsonData data in source)
                    if (data != null) pool.Add(data);
            }

            for (int i = 0; i < BuySlotCount && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                CardParamJsonData data = pool[index];
                pool.RemoveAt(index);

                int price = overridePrice >= 0 ? overridePrice : data.price;
                BuySlots.Add(new StoreBuySlotData
                {
                    id = data.id,
                    name = data.name,
                    iconPath = data.iconPath,
                    description = string.IsNullOrEmpty(CurrentBoxName)
                        ? data.description
                        : $"来自「{CurrentBoxName}」\n{data.description}",
                    price = price
                });
            }
        }

        public void RefreshBag()
        {
            EnsureConfig();
            BagEntries.Clear();

            if (OverrideBagCount > 0)
            {
                BuildMockBag(OverrideBagCount);
                return;
            }

            foreach (PlayerCardState card in PlayerDataManager.Instance.GetAllCards())
            {
                fillBagEntryMeta(card.cardId, card.count);
            }
        }

        private void fillBagEntryMeta(string id, int count)
        {
            MaterialJsonData material = MaterialCatalogLoader.GetById(id);
            if (material != null)
            {
                BagEntries.Add(new StoreBagEntryData
                {
                    id = material.id,
                    name = material.name,
                    iconPath = material.iconPath,
                    count = count
                });
                return;
            }

            CardParamJsonData meta = FindCardMeta(id);
            BagEntries.Add(new StoreBagEntryData
            {
                id = id,
                name = meta != null ? meta.name : id,
                iconPath = meta != null ? meta.iconPath : string.Empty,
                count = count
            });
        }

        private void BuildMockBag(int count)
        {
            var source = _cardConfig?.cards;
            for (int i = 0; i < count; i++)
            {
                if (source != null && source.Length > 0)
                {
                    CardParamJsonData data = source[i % source.Length];
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

        private CardParamJsonData FindCardMeta(string id)
        {
            var source = _cardConfig?.cards;
            if (source == null || string.IsNullOrEmpty(id)) return null;

            foreach (CardParamJsonData data in source)
                if (data != null && data.id == id)
                    return data;

            return null;
        }

        private void EnsureConfig()
        {
            if (_cardConfig != null) return;
            _cardConfig = JsonConfigLoader.LoadFromConfig<CardParamCatalogJsonConfig>(AddressDefines.Config_CardParamCatalog);
        }
    }
}
