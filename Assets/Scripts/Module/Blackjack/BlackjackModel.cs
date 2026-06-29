/*
* ┌──────────────────────────────────┐
* │  描    述: 21 点玩法数据模型
* │           4 张牌，点道具翻开下一张，翻开时随机点数并累加；
* │           累计达到/超过 21 点视为爆牌，触发结算
* │  类    名: BlackjackModel.cs
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
        public int point;             // 牌面点数（翻开后才有效）
        public bool revealed;         // 是否已翻开
    }

    public class BlackjackModel : BaseModel
    {
        public const int CardCount = 4;   // 下方小牌数量（固定 4 张）
        public const int BustLimit = 21;  // 达到/超过即爆牌

        private const int MinPoint = 1;   // 单张牌随机点数下限
        private const int MaxPoint = 11;  // 单张牌随机点数上限（含）

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
                Cards.Add(new BlackjackCardData { point = 0, revealed = false });

            TotalPoint = 0;
            RevealedCount = 0;
        }

        /// <summary>
        /// 翻开下一张牌：随机点数并累加。返回刚翻开的牌索引；无可翻牌返回 -1。
        /// </summary>
        public int RevealNext()
        {
            if (!CanDraw) return -1;

            int index = RevealedCount;
            int point = UnityEngine.Random.Range(MinPoint, MaxPoint + 1);

            BlackjackCardData card = Cards[index];
            card.point = point;
            card.revealed = true;

            TotalPoint += point;
            RevealedCount++;

            return index;
        }
    }
}
