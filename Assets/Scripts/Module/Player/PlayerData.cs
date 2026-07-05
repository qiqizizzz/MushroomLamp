/*
* ┌──────────────────────────────────┐
* │  描    述: 玩家数据单例，保存卡牌与金钱状态
* │  类    名: PlayerData.cs
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common;
using Common.Defines;
using Module.Cook;

namespace Module.Player
{
    [Serializable]
    public class PlayerCardState
    {
        public string cardId;
        public int count = 1;
    }

    [Serializable]
    public class PlayerData
    {
        private readonly Dictionary<string, int> _cardCounts = new();
        private readonly HashSet<string> _ownedItemIds = new();

        public int Money { get; private set; }

        public IReadOnlyDictionary<string, int> CardCounts => _cardCounts;

        public void Init()
        {
            Money = 0;
            _cardCounts.Clear();
            _ownedItemIds.Clear();
        }

        public void ClearItemsForNewRun()
        {
            _ownedItemIds.Clear();
        }

        // 新大局开始时，用选择页/箱子配置的材料池重置牌组
        public void ResetCardsFromMaterialPool(IEnumerable<CookMaterialSeedData> materials)
        {
            _cardCounts.Clear();
            if (materials == null) return;

            foreach (CookMaterialSeedData seed in materials)
            {
                if (seed == null || string.IsNullOrWhiteSpace(seed.MaterialId) || seed.Count <= 0) continue;
                _cardCounts[seed.MaterialId] = seed.Count;
            }
        }

        public bool AddItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return false;

            ItemParamJsonData cfg = Module.Item.ItemParamCatalogLoader.GetById(itemId);
            if (cfg != null && !cfg.stackable && _ownedItemIds.Contains(itemId))
                return false;

            _ownedItemIds.Add(itemId);
            return true;
        }

        public bool RemoveItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return false;
            return _ownedItemIds.Remove(itemId);
        }

        public void ClearAllItems()
        {
            _ownedItemIds.Clear();
        }

        public int OwnedItemCount => _ownedItemIds.Count;

        public bool HasItem(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && _ownedItemIds.Contains(itemId);
        }

        public IEnumerable<string> GetOwnedItemIds()
        {
            foreach (string itemId in _ownedItemIds)
                yield return itemId;
        }

        public void AddMoney(int amount)
        {
            if (amount == 0) return;
            Money += amount;
            if (Money < 0) Money = 0;
        }

        public bool SpendMoney(int amount)
        {
            if (amount <= 0) return true;
            if (Money < amount) return false;

            Money -= amount;
            return true;
        }

        public void AddCard(string cardId, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(cardId) || count <= 0) return;

            if (_cardCounts.TryGetValue(cardId, out int current))
                _cardCounts[cardId] = current + count;
            else
                _cardCounts[cardId] = count;
        }

        public bool HasCard(string cardId)
        {
            return !string.IsNullOrWhiteSpace(cardId) && _cardCounts.ContainsKey(cardId);
        }

        public int GetCardCount(string cardId)
        {
            return string.IsNullOrWhiteSpace(cardId) || !_cardCounts.TryGetValue(cardId, out int count)
                ? 0
                : count;
        }

        public IEnumerable<PlayerCardState> GetAllCards()
        {
            foreach (KeyValuePair<string, int> pair in _cardCounts)
            {
                yield return new PlayerCardState
                {
                    cardId = pair.Key,
                    count = pair.Value
                };
            }
        }
    }
}
