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

        public int Money { get; private set; }

        public IReadOnlyDictionary<string, int> CardCounts => _cardCounts;

        public void Init()
        {
            Money = 0;
            _cardCounts.Clear();
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
