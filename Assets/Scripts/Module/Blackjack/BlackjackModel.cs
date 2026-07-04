/*
* ┌──────────────────────────────────┐
* │  描    述: 21 点玩法数据模型
* │  类    名: BlackjackModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Module.Item;
using MVC.Model;
using UnityEngine;

namespace Module.Blackjack
{
    [Serializable]
    public class BlackjackCardData
    {
        public int revealedPoint;     // 翻开后得到的点数
        public bool revealed;         // 是否已翻开
    }

    public class BlackjackModel : BaseModel
    {
        public const int BaseCardCount = 4;
        public const int BustLimit = 21;

        private const int MinPoint = 1;
        private const int MaxPoint = 11;

        public readonly List<BlackjackCardData> Cards = new();

        public int CardCount => BaseCardCount + ItemPassiveManager.GetMagicBoxOptionBonus();

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
            int cardCount = CardCount;
            for (int i = 0; i < cardCount; i++)
            {
                Cards.Add(new BlackjackCardData
                {
                    revealedPoint = 0,
                    revealed = false
                });
            }

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
            BlackjackCardData card = Cards[index];
            int point = rollNextPoint();
            card.revealedPoint = point;
            card.revealed = true;

            TotalPoint += point;
            RevealedCount++;

            return index;
        }

        // 幸运兔脚：撤销最近一次翻牌
        public bool UndoLastReveal()
        {
            if (RevealedCount <= 0) return false;

            RevealedCount--;
            BlackjackCardData card = Cards[RevealedCount];
            TotalPoint -= card.revealedPoint;
            card.revealed = false;
            card.revealedPoint = 0;
            return true;
        }

        // 获取指定牌位的显示点数
        public int GetRevealedPoint(int index)
        {
            if (index < 0 || index >= Cards.Count) return 0;
            return Cards[index].revealed ? Cards[index].revealedPoint : 0;
        }

        private int rollNextPoint()
        {
            if (ItemPassiveManager.IsPandoraSafeDrawActive)
                return rollSafePoint();

            return UnityEngine.Random.Range(MinPoint, MaxPoint + 1);
        }

        // 潘多拉钥匙：抽牌点数保证累计仍低于 21
        private int rollSafePoint()
        {
            int maxSafe = BustLimit - 1 - TotalPoint;
            if (maxSafe < MinPoint)
                return MinPoint;

            int upper = Math.Min(MaxPoint, maxSafe);
            return UnityEngine.Random.Range(MinPoint, upper + 1);
        }
    }
}
