/*
* ┌──────────────────────────────────┐
* │  描    述: 21 点玩法数据模型
* │  类    名: BlackjackModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using MVC.Model;

namespace Module.Blackjack
{
    [Serializable]
    public class BlackjackCardData
    {
        public int point;             // 牌面点数
        public bool revealed;         // 是否已翻开
    }

    public class BlackjackModel : BaseModel
    {
        public const int CardCount = 4;
        public const int BustLimit = 21;

        private const int MinPoint = 1;
        private const int MaxPoint = 11;

        public readonly List<BlackjackCardData> Cards = new();

        // 当前累计点数（已翻开牌之和）
        public int TotalPoint { get; private set; }

        // 已翻开数量
        public int RevealedCount { get; private set; }

        // 是否爆牌（达到/超过 21）
        public bool IsBusted => TotalPoint >= BustLimit;

        // 牌是否已全部翻开
        public bool AllRevealed => RevealedCount >= CardCount;

        // 是否还能继续抽（未爆且还有未翻开的牌）
        public bool CanDraw => !IsBusted && !AllRevealed;

        // 开局/重开：重置所有牌为未翻开
        public void Reset()
        {
            Cards.Clear();
            for (int i = 0; i < CardCount; i++)
            {
                Cards.Add(new BlackjackCardData
                {
                    point = UnityEngine.Random.Range(MinPoint, MaxPoint + 1),
                    revealed = false
                });
            }

            TotalPoint = 0;
            RevealedCount = 0;
        }

        /// <summary>
        /// 翻开下一张牌：随机点数并累加。返回刚翻开的牌索引；无可翻牌返回 -1。
        /// </summary>
        public int RevealNext(int itemIndex = -1)
        {
            if (!CanDraw) return -1;

            int index = RevealedCount;
            BlackjackCardData card = Cards[index];
            card.revealed = true;

            TotalPoint += card.point;
            RevealedCount++;

            return index;
        }
    }
}
