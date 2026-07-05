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
        public float revealedPoint;   // 翻开后得到的点数
        public bool revealed;         // 是否已翻开
        public string faceSpriteKey;  // 牌面 Resources 名（如 7H），花色随机
    }

    public class BlackjackModel : BaseModel
    {
        public const int DefaultItemSlotCount = 3;
        public const int BustLimit = 21;
        public const float AcePoint = 0.5f;
        public const float FacePoint = 0.5f;

        public int EffectiveBustLimit => BustLimit + MagicBoxBuffManager.GetBustLimitBonus();

        private readonly List<string> _drawPile = new();
        private readonly HashSet<int> _usedItemSlots = new();
        private int _lastUsedItemSlot = -1;
        private int _lastUndoneItemSlot = -1;

        public int LastUndoneItemSlot => _lastUndoneItemSlot;

        public readonly List<BlackjackCardData> Cards = new();

        // 上方道具槽数量（= 小牌数量），由界面 Items 数量或加成决定
        public int ItemSlotCount { get; private set; }

        // 小牌张数与道具槽一一对应
        public int CardCount => ItemSlotCount;

        // 当前累计点数（已翻开牌之和）
        public float TotalPoint { get; private set; }

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
            _lastUndoneItemSlot = -1;

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
            _drawPile.Clear();
            _drawPile.AddRange(PokerCardSpriteLoader.CreateShuffledDeck());
        }

        private static int resolveItemSlotCount(int itemSlotCount)
        {
            if (itemSlotCount > 0)
                return itemSlotCount + ItemPassiveManager.GetMagicBoxOptionBonus();

            return DefaultItemSlotCount + ItemPassiveManager.GetMagicBoxOptionBonus();
        }

        // 从指定道具槽抽牌：Item 与小牌一一对应，翻开同索引小牌
        public bool TryDrawFromSlot(int slotIndex, out int cardIndex)
        {
            cardIndex = -1;
            if (!IsItemSlotAvailable(slotIndex)) return false;
            if (slotIndex >= Cards.Count || Cards[slotIndex].revealed) return false;

            _usedItemSlots.Add(slotIndex);
            _lastUsedItemSlot = slotIndex;

            if (!tryDrawNextCard(out string spriteKey, out float point))
            {
                _usedItemSlots.Remove(slotIndex);
                _lastUsedItemSlot = -1;
                return false;
            }

            BlackjackCardData card = Cards[slotIndex];
            card.revealedPoint = point;
            card.faceSpriteKey = spriteKey;
            card.revealed = true;
            TotalPoint += point;
            RevealedCount++;
            cardIndex = slotIndex;
            return true;
        }

        // 幸运兔脚：撤销最近一次翻牌，并恢复对应道具槽
        public bool UndoLastReveal()
        {
            if (_lastUsedItemSlot < 0 || _lastUsedItemSlot >= Cards.Count) return false;

            BlackjackCardData card = Cards[_lastUsedItemSlot];
            if (!card.revealed) return false;

            TotalPoint -= card.revealedPoint;
            card.revealed = false;
            card.revealedPoint = 0;
            if (!string.IsNullOrEmpty(card.faceSpriteKey))
                _drawPile.Add(card.faceSpriteKey);
            card.faceSpriteKey = null;
            RevealedCount--;
            _usedItemSlots.Remove(_lastUsedItemSlot);
            _lastUndoneItemSlot = _lastUsedItemSlot;
            _lastUsedItemSlot = -1;
            return true;
        }

        // 获取指定牌位的显示点数
        public float GetRevealedPoint(int index)
        {
            if (index < 0 || index >= Cards.Count) return 0f;
            return Cards[index].revealed ? Cards[index].revealedPoint : 0f;
        }

        public static string FormatPoint(float point)
        {
            if (Mathf.Abs(point - Mathf.Round(point)) < 0.001f)
                return Mathf.RoundToInt(point).ToString();
            return point.ToString("0.#");
        }

        public string GetFaceSpriteKey(int index)
        {
            if (index < 0 || index >= Cards.Count) return null;
            return Cards[index].revealed ? Cards[index].faceSpriteKey : null;
        }

        // 从牌堆抽一张未发过的牌（同点数同花色不重复）
        private bool tryDrawNextCard(out string spriteKey, out float point)
        {
            spriteKey = null;
            point = 0f;
            if (_drawPile.Count == 0) return false;

            if (ItemPassiveManager.IsPandoraSafeDrawActive)
                spriteKey = pickSafeCardFromPile();
            else
                spriteKey = _drawPile[UnityEngine.Random.Range(0, _drawPile.Count)];

            if (string.IsNullOrEmpty(spriteKey)) return false;

            _drawPile.Remove(spriteKey);
            point = PokerCardSpriteLoader.ResolvePointFromSpriteKey(spriteKey);
            return true;
        }

        // 潘多拉钥匙：从未发牌堆中抽一张保证累计仍低于爆牌线
        private string pickSafeCardFromPile()
        {
            var safeKeys = new List<string>();
            for (int i = 0; i < _drawPile.Count; i++)
            {
                string key = _drawPile[i];
                float nextPoint = PokerCardSpriteLoader.ResolvePointFromSpriteKey(key);
                if (TotalPoint + nextPoint < EffectiveBustLimit)
                    safeKeys.Add(key);
            }

            if (safeKeys.Count > 0)
                return safeKeys[UnityEngine.Random.Range(0, safeKeys.Count)];

            string lowestKey = _drawPile[0];
            float lowestPoint = PokerCardSpriteLoader.ResolvePointFromSpriteKey(lowestKey);
            for (int i = 1; i < _drawPile.Count; i++)
            {
                string key = _drawPile[i];
                float nextPoint = PokerCardSpriteLoader.ResolvePointFromSpriteKey(key);
                if (nextPoint >= lowestPoint) continue;
                lowestPoint = nextPoint;
                lowestKey = key;
            }

            return lowestKey;
        }
    }
}
