/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪玩法数据模型，保存局内回合与分数状态
* │  类    名: CookModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using MVC.Model;
using Module.Select;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Module.Cook
{
    // 烹饪玩法数据模型，保存局内回合、材料、法阵与结算状态
    public class CookModel : BaseModel
    {
        private const int GRID_SIZE = 9;
        private const int HAND_COUNT = 6;

        private readonly List<CookMaterialSeedData> _materialSeeds = new();
        private readonly List<CookMaterialData> _handMaterials = new();
        private readonly CookSlotData[] _slots = new CookSlotData[GRID_SIZE];
        private readonly List<int> _placeHistory = new();
        private readonly System.Random _random = new System.Random();

        private int _nextMaterialId;
        private int _nextPlaceOrder;

        public SelectDifficulty Difficulty { get; private set; }
        public string BoxId { get; private set; }
        public string BoxName { get; private set; }
        public int TurnIndex { get; private set; }
        public int MaxTurn { get; private set; }
        public int TargetMin { get; private set; }
        public int TargetMax { get; private set; }
        public int CurrentScore { get; private set; }
        public int Coin { get; private set; }
        public int PreviewValue { get; private set; }
        public bool IsRunActive { get; private set; }
        public CookRoundState RoundState { get; private set; }
        public string LastTip { get; private set; }
        public IReadOnlyList<CookMaterialData> HandMaterials => _handMaterials;
        public IReadOnlyList<CookSlotData> Slots => _slots;
        public bool HasPlacedMaterial => _placeHistory.Count > 0;
        public bool CanSettle => IsRunActive && HasPlacedMaterial && RoundState != CookRoundState.Finished;
        public bool IsOverHeatRisk => PreviewValue > TargetMax;

        public CookModel()
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = new CookSlotData(i);
        }

        // 开始新一局烹饪
        public void StartRun(CookRunStartData startData = null)
        {
            setupStartData(startData);

            TurnIndex = 1;
            MaxTurn = getMaxTurn(Difficulty);
            CurrentScore = 0;
            Coin = 0;
            IsRunActive = true;
            RoundState = CookRoundState.RoundStart;
            LastTip = "查看目标后，把材料拖入法阵";

            startRound();
        }

        // 放置材料到法阵槽位
        public bool PlaceMaterial(int materialId, int slotIndex)
        {
            if (!IsRunActive)
            {
                LastTip = "当前烹饪已结束";
                return false;
            }

            if (slotIndex < 0 || slotIndex >= _slots.Length)
            {
                LastTip = "法阵槽位不存在";
                return false;
            }

            CookSlotData slot = _slots[slotIndex];
            if (slot.HasMaterial)
            {
                LastTip = "槽位已占用";
                return false;
            }

            CookMaterialData material = findHandMaterial(materialId);
            if (material == null)
            {
                LastTip = "材料已不在传送带中";
                return false;
            }

            _handMaterials.Remove(material);
            slot.Place(material, _nextPlaceOrder++);
            _placeHistory.Add(slotIndex);

            RoundState = CookRoundState.ReadyToSettle;
            refreshPreviewValue();
            LastTip = $"已放入 {material.MaterialName}，当前预估火候 {PreviewValue}";
            return true;
        }

        // 撤回最近一次放置的材料
        public bool UndoLastPlace()
        {
            if (_placeHistory.Count == 0)
            {
                LastTip = "没有可撤回的材料";
                return false;
            }

            int slotIndex = _placeHistory[^1];
            _placeHistory.RemoveAt(_placeHistory.Count - 1);

            CookMaterialData material = _slots[slotIndex].Clear();
            if (material != null)
                _handMaterials.Add(material);

            refreshPreviewValue();
            RoundState = HasPlacedMaterial ? CookRoundState.ReadyToSettle : CookRoundState.Operating;
            LastTip = material == null ? "槽位已清空" : $"已撤回 {material.MaterialName}";
            return true;
        }

        // 清空当前法阵内的所有材料
        public void ClearPlacedMaterials()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                CookMaterialData material = _slots[i].Clear();
                if (material != null)
                    _handMaterials.Add(material);
            }

            _placeHistory.Clear();
            _nextPlaceOrder = 1;
            refreshPreviewValue();
            RoundState = CookRoundState.Operating;
            LastTip = "已清空法阵";
        }

        // 跳过当前回合
        public bool SkipTurn()
        {
            if (!IsRunActive) return false;

            LastTip = "已跳过本回合";
            return advanceTurn();
        }

        // 结算当前回合
        public CookRoundResult SettleTurn()
        {
            if (!CanSettle)
            {
                LastTip = "法阵中没有有效材料，无法结算";
                return null;
            }

            int roundScore = PreviewValue;
            bool isTargetMatched = roundScore >= TargetMin && roundScore <= TargetMax;
            bool isOverHeat = roundScore > TargetMax;
            int coinReward = isTargetMatched ? 3 : 1;

            CurrentScore += isOverHeat ? Mathf.Max(0, roundScore - 3) : roundScore;
            Coin += coinReward;
            RoundState = CookRoundState.Settled;

            CookRoundResult result = new CookRoundResult(TurnIndex, roundScore, isTargetMatched, isOverHeat);
            LastTip = getSettleTip(result, coinReward);
            advanceTurn();
            return result;
        }

        // 获取当前回合进度文本
        public string GetTurnText()
        {
            return $"回合 {TurnIndex}/{MaxTurn}";
        }

        // 获取当前总分文本
        public string GetScoreText()
        {
            return $"得分 {CurrentScore}";
        }

        // 获取目标区间文本
        public string GetTargetText()
        {
            return $"目标 {TargetMin}~{TargetMax}";
        }

        // 获取金币文本
        public string GetCoinText()
        {
            return $"金币 {Coin}";
        }

        private void setupStartData(CookRunStartData startData)
        {
            Difficulty = startData?.Difficulty ?? SelectDifficulty.Normal;
            BoxId = startData?.BoxId ?? string.Empty;
            BoxName = string.IsNullOrWhiteSpace(startData?.BoxName) ? "默认药箱" : startData.BoxName;

            _materialSeeds.Clear();
            if (startData?.Materials != null)
            {
                for (int i = 0; i < startData.Materials.Count; i++)
                {
                    CookMaterialSeedData seed = startData.Materials[i];
                    if (seed == null || string.IsNullOrWhiteSpace(seed.MaterialName)) continue;

                    _materialSeeds.Add(seed);
                }
            }

            if (_materialSeeds.Count == 0)
                addFallbackSeeds();
        }

        private void startRound()
        {
            clearRoundBoard();
            setupRoundTarget();
            dealHandMaterials();
            refreshPreviewValue();
            RoundState = CookRoundState.Operating;
        }

        private void clearRoundBoard()
        {
            _handMaterials.Clear();
            _placeHistory.Clear();
            _nextPlaceOrder = 1;

            for (int i = 0; i < _slots.Length; i++)
                _slots[i].Clear();
        }

        private void setupRoundTarget()
        {
            int baseTarget = Difficulty switch
            {
                SelectDifficulty.Easy => 14,
                SelectDifficulty.Hard => 22,
                _ => 18
            };

            int turnOffset = Mathf.Max(0, TurnIndex - 1) * 2;
            TargetMin = baseTarget + turnOffset;
            TargetMax = TargetMin + (Difficulty == SelectDifficulty.Hard ? 3 : 4);
        }

        private void dealHandMaterials()
        {
            List<CookMaterialSeedData> pool = buildSeedPool();
            if (pool.Count == 0) return;

            int count = Mathf.Min(HAND_COUNT, Mathf.Max(pool.Count, HAND_COUNT));
            for (int i = 0; i < count; i++)
            {
                CookMaterialSeedData seed = pool[_random.Next(pool.Count)];
                _handMaterials.Add(createMaterial(seed));
            }
        }

        private List<CookMaterialSeedData> buildSeedPool()
        {
            List<CookMaterialSeedData> pool = new List<CookMaterialSeedData>();
            for (int i = 0; i < _materialSeeds.Count; i++)
            {
                CookMaterialSeedData seed = _materialSeeds[i];
                int count = Mathf.Max(1, seed.Count);
                for (int c = 0; c < count; c++)
                    pool.Add(seed);
            }

            return pool;
        }

        private CookMaterialData createMaterial(CookMaterialSeedData seed)
        {
            string materialName = seed.MaterialName;
            int value = getBaseValue(materialName);
            string tag = getDefaultTag(materialName);
            bool canProcess = value >= 5;
            return new CookMaterialData(_nextMaterialId++, materialName, value, tag, canProcess, seed.Icon);
        }

        private CookMaterialData findHandMaterial(int materialId)
        {
            for (int i = 0; i < _handMaterials.Count; i++)
            {
                if (_handMaterials[i].RuntimeId == materialId)
                    return _handMaterials[i];
            }

            return null;
        }

        private void refreshPreviewValue()
        {
            PreviewValue = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Material != null)
                    PreviewValue += _slots[i].Material.CurrentValue;
            }
        }

        private bool advanceTurn()
        {
            if (TurnIndex >= MaxTurn)
            {
                IsRunActive = false;
                RoundState = CookRoundState.Finished;
                LastTip = $"{LastTip}，整局结束";
                return false;
            }

            TurnIndex++;
            startRound();
            return true;
        }

        private static int getMaxTurn(SelectDifficulty difficulty)
        {
            return difficulty == SelectDifficulty.Easy ? 5 : 6;
        }

        private static int getBaseValue(string materialName)
        {
            if (materialName.Contains("胡萝卜")) return 4;
            if (materialName.Contains("土豆")) return 5;
            if (materialName.Contains("蘑菇")) return 6;
            if (materialName.Contains("南瓜")) return 8;
            if (materialName.Contains("矿")) return 7;
            if (materialName.Contains("香")) return 3;
            return 5;
        }

        private static string getDefaultTag(string materialName)
        {
            if (materialName.Contains("胡萝卜") || materialName.Contains("土豆") || materialName.Contains("南瓜"))
                return "根茎";

            if (materialName.Contains("蘑菇"))
                return "菌菇";

            if (materialName.Contains("矿"))
                return "矿物";

            if (materialName.Contains("香"))
                return "香料";

            return "素材";
        }

        private static string getSettleTip(CookRoundResult result, int coinReward)
        {
            if (result.IsOverHeat)
                return $"火候 {result.RoundScore} 超出目标，获得金币 {coinReward}";

            if (result.IsTargetMatched)
                return $"命中目标火候 {result.RoundScore}，获得金币 {coinReward}";

            return $"火候 {result.RoundScore} 未命中目标，获得金币 {coinReward}";
        }

        private void addFallbackSeeds()
        {
            _materialSeeds.Add(new CookMaterialSeedData { MaterialName = "胡萝卜", Count = 2 });
            _materialSeeds.Add(new CookMaterialSeedData { MaterialName = "土豆", Count = 2 });
            _materialSeeds.Add(new CookMaterialSeedData { MaterialName = "蘑菇", Count = 1 });
            _materialSeeds.Add(new CookMaterialSeedData { MaterialName = "南瓜", Count = 1 });
        }
    }
}
