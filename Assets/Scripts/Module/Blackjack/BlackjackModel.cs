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
using Module.MagicBoxBuff;
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
        public const int DefaultItemSlotCount = 3;
        public const int BustLimit = 21;

        public int EffectiveBustLimit => BustLimit + MagicBoxBuffManager.GetBustLimitBonus();

        private const int MinPoint = 1;
        private const int MaxPoint = 11;

        private readonly HashSet<int> _usedItemSlots = new();
        private int _lastUsedItemSlot = -1;

        public readonly List<BlackjackCardData> Cards = new();

        // 上方道具槽数量（= 小牌数量），由界面 Items 数量或加成决定
        public int ItemSlotCount { get; private set; }

        // 小牌张数与道具槽一一对应
        public int CardCount => ItemSlotCount;

        // 当前累计点数（已翻开牌之和）
        public int TotalPoint { get; private set; }

        // 已翻开数量
        public int RevealedCount { get; private set; }

        // 是否爆牌（达到/超过 21）
        public bool IsBusted => TotalPoint >= EffectiveBustLimit;

        // 牌是否已全部翻开
        public bool AllRevealed => RevealedCount >= CardCount;

        // 是否还能继续抽（未爆且还有未翻开的牌）
        public bool CanDraw => !IsBusted && !AllRevealed;

        // 指定上方道具槽是否仍可点击
        public bool IsItemSlotAvailable(int slotIndex)
        {
            if (!CanDraw || slotIndex < 0 || slotIndex >= ItemSlotCount) return false;
            return !_usedItemSlots.Contains(slotIndex);
        }

        // 计算小牌居中布局（anchoredPosition，相对 SmallCards 容器）
        public IReadOnlyList<Vector2> GetSmallCardLayout(float containerWidth, float cardWidth, float spacing)
        {
            return BlackjackCardLayout.ComputeCenteredAnchoredPositions(
                ItemSlotCount, containerWidth, cardWidth, spacing);
        }

        // 开局/重开：itemSlotCount 为界面 Items 子节点数量；<=0 时用默认 + 道具加成
        public void Reset(int itemSlotCount = 0)
        {
            ItemSlotCount = resolveItemSlotCount(itemSlotCount);
            Cards.Clear();
            _usedItemSlots.Clear();
            _lastUsedItemSlot = -1;

            for (int i = 0; i < ItemSlotCount; i++)
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

        private static int resolveItemSlotCount(int itemSlotCount)
        {
            if (itemSlotCount > 0)
                return itemSlotCount + ItemPassiveManager.GetMagicBoxOptionBonus();

            return DefaultItemSlotCount + ItemPassiveManager.GetMagicBoxOptionBonus();
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

        // 从指定道具槽抽牌：该槽位仅可使用一次
        public bool TryDrawFromSlot(int slotIndex, out int cardIndex)
        {
            cardIndex = -1;
            if (!IsItemSlotAvailable(slotIndex)) return false;

            _usedItemSlots.Add(slotIndex);
            _lastUsedItemSlot = slotIndex;
            cardIndex = RevealNext();
            if (cardIndex >= 0) return true;

            _usedItemSlots.Remove(slotIndex);
            _lastUsedItemSlot = -1;
            return false;
        }

        // 幸运兔脚：撤销最近一次翻牌，并恢复对应道具槽
        public bool UndoLastReveal()
        {
            if (RevealedCount <= 0) return false;

            RevealedCount--;
            BlackjackCardData card = Cards[RevealedCount];
            TotalPoint -= card.revealedPoint;
            card.revealed = false;
            card.revealedPoint = 0;

            if (_lastUsedItemSlot >= 0)
            {
                _usedItemSlots.Remove(_lastUsedItemSlot);
                _lastUsedItemSlot = -1;
            }

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
            int maxSafe = EffectiveBustLimit - 1 - TotalPoint;
            if (maxSafe < MinPoint)
                return MinPoint;

            int upper = Math.Min(MaxPoint, maxSafe);
            return UnityEngine.Random.Range(MinPoint, upper + 1);
        }
    }
}
