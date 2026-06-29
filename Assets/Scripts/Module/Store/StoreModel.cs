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
using Module.Player;
using MVC.Model;

namespace Module.Store
{
    // 中间三个定位点上的购买卡牌数据
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

    // 底部背包格子数据（玩家已拥有的卡牌 + 数量）
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
        // 中间购买槽数量（设计图为 3 个定位点）
        public const int BuySlotCount = 3;

        // 金币直接读玩家单例
        public int Gold => PlayerDataManager.Instance.Money;

        public readonly List<StoreBuySlotData> BuySlots = new();
        public readonly List<StoreBagEntryData> BagEntries = new();

        // 手动指定背包卡牌数量（>0 时生效，用配置表卡牌循环填充，覆盖真实背包）；<=0 表示读真实背包
        public int OverrideBagCount { get; private set; } = -1;

        private CardParamCatalogJsonConfig _cardConfig;

        // 手动设置背包卡牌数量（传 <=0 可恢复读真实背包）
        public void SetBagCount(int count)
        {
            OverrideBagCount = count;
        }

        // 刷新购买槽（随机抽取卡牌）
        public void RefreshBuySlots()
        {
            EnsureConfig();
            BuySlots.Clear();

            var source = _cardConfig?.cards;
            if (source == null || source.Length == 0) return;

            var pool = new List<CardParamJsonData>(source);
            for (int i = 0; i < BuySlotCount && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                var data = pool[index];
                pool.RemoveAt(index);

                BuySlots.Add(new StoreBuySlotData
                {
                    id = data.id,
                    name = data.name,
                    iconPath = data.iconPath,
                    description = data.description,
                    price = data.price
                });
            }
        }

        // 刷新背包：OverrideBagCount>0 时生成指定数量的占位卡（用于测试复用）；否则读玩家真实背包
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
                CardParamJsonData meta = FindCardMeta(card.cardId);
                BagEntries.Add(new StoreBagEntryData
                {
                    id = card.cardId,
                    name = meta != null ? meta.name : card.cardId,
                    iconPath = meta != null ? meta.iconPath : string.Empty,
                    count = card.count
                });
            }
        }

        // 用配置表卡牌循环填充指定数量；无配置时退化为编号占位卡
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
