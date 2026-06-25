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
        private readonly List<CookMaterialData> _processedMaterials = new();
        private readonly List<CookPotEntryData> _potEntries = new();
        private readonly CookSlotData[] _slots = new CookSlotData[GRID_SIZE];
        private readonly List<int> _placeHistory = new();
        private readonly System.Random _random = new System.Random();

        private int _nextMaterialId;
        private int _nextPlaceOrder;
        private int _nextSubmitOrder;
        private float _magicBoxBonus;
        private float _devilRisk;
        private bool _hasPlacedHandThisTurn;

        public SelectDifficulty Difficulty { get; private set; }
        public string BoxId { get; private set; }
        public string BoxName { get; private set; }
        public int TurnIndex { get; private set; }
        public int MaxTurn { get; private set; }
        public int TargetMin { get; private set; }
        public int TargetMax { get; private set; }
        public float CurrentScore { get; private set; }
        public int Coin { get; private set; }
        public float PreviewValue { get; private set; }
        public bool IsRunActive { get; private set; }
        public CookRoundStateType RoundState { get; private set; }
        public string LastTip { get; private set; }
        public string PreviewBreakdownText { get; private set; }
        public CookRoundResultData LastRoundResult { get; private set; }
        public bool IsMagicBoxUsed { get; private set; }
        public CookMagicBoxEffectType LastMagicBoxEffect { get; private set; }
        public int AngelRescueCount { get; private set; }
        public float DevilRisk => _devilRisk;
        public string MagicBoxStatusText { get; private set; }
        public IReadOnlyList<CookMaterialData> HandMaterials => _handMaterials;
        public IReadOnlyList<CookMaterialData> ProcessedMaterials => _processedMaterials;
        public IReadOnlyList<CookPotEntryData> PotEntries => _potEntries;
        public IReadOnlyList<CookSlotData> Slots => _slots;
        public bool HasPlacedMaterial => _placeHistory.Count > 0;
        public bool HasCookingMaterial => hasAnySlotMaterial();
        public bool HasPotMaterial => _potEntries.Count > 0;
        public bool CanPlaceHandThisTurn => IsRunActive && !_hasPlacedHandThisTurn;
        public bool CanSettle => IsRunActive && (HasCookingMaterial || HasPotMaterial) && RoundState != CookRoundStateType.Finished;
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
            TargetMin = getBaseTarget(Difficulty);
            TargetMax = TargetMin + (Difficulty == SelectDifficulty.Hard ? 3 : 4);
            AngelRescueCount = getStartAngelRescueCount(Difficulty);
            IsRunActive = true;
            RoundState = CookRoundStateType.RoundStart;
            LastTip = "每回合选择一个材料放入法阵，熟后拖入锅中";
            LastRoundResult = null;
            clearRunBoard();

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

            if (_hasPlacedHandThisTurn)
            {
                LastTip = "本回合已经放入过一个材料";
                return false;
            }

            CookMaterialData material = findAvailableMaterial(materialId);
            if (material == null)
            {
                LastTip = "材料已不在可用区域中";
                return false;
            }

            removeAvailableMaterial(material);
            slot.Place(material, _nextPlaceOrder++);
            _placeHistory.Add(slotIndex);
            _hasPlacedHandThisTurn = true;

            RoundState = CookRoundStateType.ReadyToSettle;
            refreshPreviewValue();
            LastTip = $"已放入 {material.MaterialName}，结束回合后获得熟度 +{slot.EnchantText}";
            return true;
        }

        // 移动或交换法阵槽位中的材料
        public bool MoveSlotMaterial(int fromSlotIndex, int toSlotIndex)
        {
            if (!IsRunActive)
            {
                LastTip = "当前烹饪已结束";
                return false;
            }

            if (!isValidSlotIndex(fromSlotIndex) || !isValidSlotIndex(toSlotIndex))
            {
                LastTip = "法阵槽位不存在";
                return false;
            }

            if (fromSlotIndex == toSlotIndex)
                return false;

            CookSlotData fromSlot = _slots[fromSlotIndex];
            CookSlotData toSlot = _slots[toSlotIndex];
            if (!fromSlot.HasMaterial)
            {
                LastTip = "起始槽位没有材料";
                return false;
            }

            if (toSlot.HasMaterial)
            {
                fromSlot.SwapWith(toSlot);
                updatePlaceHistorySlot(fromSlotIndex, toSlotIndex);
                LastTip = "已交换法阵材料位置";
            }
            else
            {
                toSlot.MoveFrom(fromSlot);
                updatePlaceHistorySlot(fromSlotIndex, toSlotIndex);
                LastTip = "已移动法阵材料位置";
            }

            refreshPreviewValue();
            return true;
        }

        // 将法阵中的材料提交到锅中
        public bool SubmitSlotToPot(int slotIndex)
        {
            if (!IsRunActive)
            {
                LastTip = "当前烹饪已结束";
                return false;
            }

            if (!isValidSlotIndex(slotIndex))
            {
                LastTip = "法阵槽位不存在";
                return false;
            }

            CookSlotData slot = _slots[slotIndex];
            if (!slot.HasMaterial)
            {
                LastTip = "槽位中没有可入锅的材料";
                return false;
            }

            CookMaterialData material = slot.Clear();
            CookPotEntryData potEntry = new CookPotEntryData(_nextSubmitOrder++, slotIndex, material);
            _potEntries.Add(potEntry);
            removePlaceHistory(slotIndex);

            refreshPreviewValue();
            LastTip = $"已将 {potEntry.MaterialName} 放入锅中，状态：{potEntry.CookStateText}";
            return true;
        }

        // 加工手牌中的材料
        public bool ProcessMaterial(int materialId)
        {
            if (!IsRunActive)
            {
                LastTip = "当前烹饪已结束";
                return false;
            }

            CookMaterialData material = findHandMaterial(materialId);
            if (material == null)
            {
                LastTip = "材料已不在传送带中";
                return false;
            }

            if (!material.CanProcess)
            {
                LastTip = $"{material.MaterialName} 不可研磨";
                return false;
            }

            if (material.IsProcessed)
            {
                LastTip = $"{material.MaterialName} 已加工过";
                return false;
            }

            material.MarkProcessed(2, "研磨");
            _handMaterials.Remove(material);
            _processedMaterials.Add(material);
            LastTip = $"已研磨 {material.MaterialName}，请从研磨器出口拖入法阵";
            return true;
        }

        // 触碰魔盒并获得一次风险收益
        public bool TouchMagicBox()
        {
            if (!IsRunActive)
            {
                LastTip = "当前烹饪已结束";
                return false;
            }

            if (IsMagicBoxUsed)
            {
                LastTip = "本回合已经触碰过魔盒";
                return false;
            }

            IsMagicBoxUsed = true;
            LastMagicBoxEffect = (CookMagicBoxEffectType)_random.Next(1, 4);

            switch (LastMagicBoxEffect)
            {
                case CookMagicBoxEffectType.AddScore:
                    _magicBoxBonus += 4f;
                    _devilRisk += 2f;
                    LastTip = "魔盒赐予火候 +4，但恶魔风险 +2";
                    break;
                case CookMagicBoxEffectType.ExpandTarget:
                    TargetMax += 3;
                    _devilRisk += 1f;
                    LastTip = "魔盒扩大安全上限 +3，但恶魔风险 +1";
                    break;
                case CookMagicBoxEffectType.CopyMaterial:
                    copyFirstHandMaterial();
                    _devilRisk += 2f;
                    LastTip = "魔盒复制了一份手牌材料，但恶魔风险 +2";
                    break;
            }

            refreshPreviewValue();
            refreshMagicBoxStatusText();
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
                returnMaterialToAvailableArea(material);

            _hasPlacedHandThisTurn = false;
            refreshPreviewValue();
            RoundState = CanSettle ? CookRoundStateType.ReadyToSettle : CookRoundStateType.Operating;
            LastTip = material == null ? "槽位已清空" : $"已撤回 {material.MaterialName}";
            return true;
        }

        // 清空本回合放入法阵内的材料
        public void ClearPlacedMaterials()
        {
            for (int i = _placeHistory.Count - 1; i >= 0; i--)
            {
                int slotIndex = _placeHistory[i];
                if (!isValidSlotIndex(slotIndex)) continue;

                CookMaterialData material = _slots[slotIndex].Clear();
                if (material != null)
                    returnMaterialToAvailableArea(material);
            }

            _placeHistory.Clear();
            _hasPlacedHandThisTurn = false;
            refreshPreviewValue();
            RoundState = CookRoundStateType.Operating;
            LastTip = "已清空本回合放入的材料";
        }

        // 跳过当前回合
        public bool SkipTurn()
        {
            if (!IsRunActive) return false;

            LastTip = "已跳过本回合";
            return advanceTurn();
        }

        // 结算当前回合
        public CookRoundResultData SettleTurn()
        {
            if (!CanSettle)
            {
                LastTip = "法阵中没有有效材料，无法结算";
                return null;
            }

            applyCookingProgress();
            CookRoundResultData result = calculateRoundResult(true);

            CurrentScore = result.FinalScore;
            Coin = result.CoinReward;
            RoundState = CookRoundStateType.Settled;
            LastRoundResult = result;

            LastTip = getSettleTip(result);
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
            return $"得分 {CookRoundResultData.FormatScore(CurrentScore)}";
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

        // 获取锅区预估拆分文本
        public string GetPreviewText()
        {
            return $"锅内火候\n{CookRoundResultData.FormatScore(PreviewValue)}\n{PreviewBreakdownText}";
        }

        // 获取锅内材料顺序文本
        public string GetPotText()
        {
            if (_potEntries.Count == 0)
                return $"{BoxName}\n{GetTargetText()}\n锅中暂无材料\n把熟好的材料拖到这里";

            List<string> lines = new List<string>
            {
                "锅中顺序",
                GetTargetText()
            };

            for (int i = 0; i < _potEntries.Count; i++)
                lines.Add(_potEntries[i].DisplayText);

            return string.Join("\n", lines);
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
            _handMaterials.Clear();
            _placeHistory.Clear();
            _processedMaterials.Clear();
            _hasPlacedHandThisTurn = false;
            _magicBoxBonus = 0;
            _devilRisk = 0;
            IsMagicBoxUsed = false;
            LastMagicBoxEffect = CookMagicBoxEffectType.None;
            refreshMagicBoxStatusText();
            dealHandMaterials();
            refreshPreviewValue();
            RoundState = CookRoundStateType.Operating;
        }

        private void clearRunBoard()
        {
            _potEntries.Clear();
            _processedMaterials.Clear();
            _placeHistory.Clear();
            _nextPlaceOrder = 1;
            _nextSubmitOrder = 1;
            _hasPlacedHandThisTurn = false;

            for (int i = 0; i < _slots.Length; i++)
                _slots[i].Clear();
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
            float requiredCookValue = getRequiredCookValue(materialName);
            return new CookMaterialData(_nextMaterialId++, materialName, value, tag, canProcess, requiredCookValue, seed.Icon);
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

        // 查找研磨器出口中的材料
        private CookMaterialData findProcessedMaterial(int materialId)
        {
            for (int i = 0; i < _processedMaterials.Count; i++)
            {
                if (_processedMaterials[i].RuntimeId == materialId)
                    return _processedMaterials[i];
            }

            return null;
        }

        // 查找当前可拖拽使用的材料
        private CookMaterialData findAvailableMaterial(int materialId)
        {
            return findHandMaterial(materialId) ?? findProcessedMaterial(materialId);
        }

        // 从可用材料区域移除指定材料
        private void removeAvailableMaterial(CookMaterialData material)
        {
            if (material == null) return;

            if (_handMaterials.Remove(material)) return;

            _processedMaterials.Remove(material);
        }

        // 将撤回材料返回对应可用区域
        private void returnMaterialToAvailableArea(CookMaterialData material)
        {
            if (material == null) return;

            if (material.IsProcessed)
                _processedMaterials.Add(material);
            else
                _handMaterials.Add(material);
        }

        // 同步本回合放置记录中的槽位位置
        private void updatePlaceHistorySlot(int fromSlotIndex, int toSlotIndex)
        {
            for (int i = 0; i < _placeHistory.Count; i++)
            {
                if (_placeHistory[i] == fromSlotIndex)
                    _placeHistory[i] = toSlotIndex;
            }
        }

        private void refreshPreviewValue()
        {
            CookRoundResultData result = calculateRoundResult(false);
            PreviewValue = result.RoundScore;
            PreviewBreakdownText = result.GetBreakdownText();
        }

        private CookRoundResultData calculateRoundResult(bool includePenalty)
        {
            float baseScore = 0f;
            float processBonus = 0f;
            float slotBonus = 0f;
            for (int i = 0; i < _potEntries.Count; i++)
            {
                CookPotEntryData potEntry = _potEntries[i];
                baseScore += potEntry.BaseValue;
                processBonus += Mathf.Max(0, potEntry.CurrentValue - potEntry.BaseValue);
                slotBonus += potEntry.CookScoreDelta;
            }

            int adjacentComboCount = calculatePotAdjacentComboCount();
            int orderComboCount = calculateOrderComboCount();
            int comboCount = adjacentComboCount + orderComboCount;
            float comboBonus = comboCount * 2f;
            float roundScore = baseScore + processBonus + slotBonus + comboBonus + _magicBoxBonus;
            bool isTargetMatched = roundScore >= TargetMin && roundScore <= TargetMax;
            bool isOverHeat = roundScore > TargetMax;
            bool isAngelRescued = includePenalty && isOverHeat && AngelRescueCount > 0;
            float rawPenalty = isOverHeat ? 3f + _devilRisk : 0f;
            float penaltyScore = includePenalty ? rawPenalty : 0f;
            if (isAngelRescued)
                penaltyScore = Mathf.Ceil(penaltyScore * 0.5f);

            if (isAngelRescued)
                AngelRescueCount--;

            int coinReward = isTargetMatched ? 3 : 1;
            string comboText = buildComboText(adjacentComboCount, orderComboCount);

            return new CookRoundResultData(
                TurnIndex,
                baseScore,
                processBonus,
                slotBonus,
                comboBonus,
                comboCount,
                orderComboCount,
                _magicBoxBonus,
                _devilRisk,
                penaltyScore,
                coinReward,
                isAngelRescued,
                isTargetMatched,
                isOverHeat,
                comboText);
        }

        // 计算锅内相邻提交材料的同标签连携数量
        private int calculatePotAdjacentComboCount()
        {
            int comboCount = 0;
            for (int i = 0; i < _potEntries.Count - 1; i++)
            {
                if (isSamePrimaryTag(_potEntries[i].TagText, _potEntries[i + 1].TagText))
                    comboCount++;
            }

            return comboCount;
        }

        // 计算依赖放置顺序的连携数量
        private int calculateOrderComboCount()
        {
            int comboCount = 0;
            for (int i = 0; i < _potEntries.Count; i++)
            {
                CookPotEntryData firstEntry = _potEntries[i];
                if (!isHerbBeforePotatoSource(firstEntry.MaterialName, firstEntry.TagText)) continue;

                for (int j = 0; j < _potEntries.Count; j++)
                {
                    CookPotEntryData nextEntry = _potEntries[j];
                    if (firstEntry.SubmitOrder >= nextEntry.SubmitOrder) continue;
                    if (isPotatoMaterial(nextEntry.MaterialName))
                        comboCount++;
                }
            }

            return comboCount;
        }

        // 生成连携说明文本
        private static string buildComboText(int adjacentComboCount, int orderComboCount)
        {
            List<string> comboTexts = new List<string>();
            if (adjacentComboCount > 0)
                comboTexts.Add($"邻接同标签 x{adjacentComboCount}");

            if (orderComboCount > 0)
                comboTexts.Add($"草药先于土豆 x{orderComboCount}");

            return comboTexts.Count == 0 ? "暂无连携" : string.Join(" / ", comboTexts);
        }

        private static bool isSamePrimaryTag(string leftTagText, string rightTagText)
        {
            if (string.IsNullOrWhiteSpace(leftTagText) || string.IsNullOrWhiteSpace(rightTagText)) return false;

            return getPrimaryTag(leftTagText) == getPrimaryTag(rightTagText);
        }

        private static string getPrimaryTag(string tagText)
        {
            if (string.IsNullOrWhiteSpace(tagText))
                return string.Empty;

            int splitIndex = tagText.IndexOf('/');
            return splitIndex < 0 ? tagText : tagText[..splitIndex];
        }

        // 判断材料是否可触发草药先于土豆的顺序连携
        private static bool isHerbBeforePotatoSource(string materialName, string tagText)
        {
            if (string.IsNullOrWhiteSpace(materialName)) return false;

            return materialName.Contains("草")
                || materialName.Contains("胡萝卜")
                || getPrimaryTag(tagText) == "香料";
        }

        // 判断材料是否为土豆
        private static bool isPotatoMaterial(string materialName)
        {
            return !string.IsNullOrWhiteSpace(materialName) && materialName.Contains("土豆");
        }

        private bool advanceTurn()
        {
            if (TurnIndex >= MaxTurn)
            {
                IsRunActive = false;
                RoundState = CookRoundStateType.Finished;
                LastTip = $"{LastTip}，整局结束";
                return false;
            }

            TurnIndex++;
            startRound();
            return true;
        }

        // 给法阵中仍在烹饪的材料累积熟度
        private void applyCookingProgress()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                CookSlotData slot = _slots[i];
                if (!slot.HasMaterial) continue;

                slot.Material.AddCookProgress(slot.EnchantValue);
            }
        }

        // 判断法阵中是否存在材料
        private bool hasAnySlotMaterial()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].HasMaterial)
                    return true;
            }

            return false;
        }

        // 判断槽位索引是否合法
        private bool isValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _slots.Length;
        }

        // 移除指定槽位的本回合放置记录
        private void removePlaceHistory(int slotIndex)
        {
            for (int i = _placeHistory.Count - 1; i >= 0; i--)
            {
                if (_placeHistory[i] == slotIndex)
                    _placeHistory.RemoveAt(i);
            }
        }

        private static int getMaxTurn(SelectDifficulty difficulty)
        {
            return difficulty == SelectDifficulty.Easy ? 5 : 6;
        }

        // 根据难度获取当天目标下限
        private static int getBaseTarget(SelectDifficulty difficulty)
        {
            return difficulty switch
            {
                SelectDifficulty.Easy => 14,
                SelectDifficulty.Hard => 22,
                _ => 18
            };
        }

        private static int getStartAngelRescueCount(SelectDifficulty difficulty)
        {
            return difficulty == SelectDifficulty.Hard ? 1 : 2;
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

        // 根据材料名称获取需要的熟度
        private static float getRequiredCookValue(string materialName)
        {
            if (materialName.Contains("胡萝卜")) return 3f;
            if (materialName.Contains("土豆")) return 4f;
            if (materialName.Contains("蘑菇")) return 3f;
            if (materialName.Contains("南瓜")) return 5f;
            if (materialName.Contains("矿")) return 5f;
            if (materialName.Contains("香")) return 2f;
            return 3f;
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

        private static string getSettleTip(CookRoundResultData result)
        {
            string angelText = result.IsAngelRescued ? "，天使救援已减半惩罚" : string.Empty;

            if (result.IsOverHeat)
                return $"火候 {CookRoundResultData.FormatScore(result.RoundScore)} 超出目标{angelText}，{result.GetBreakdownText()}，获得金币 {result.CoinReward}";

            if (result.IsTargetMatched)
                return $"命中目标火候 {CookRoundResultData.FormatScore(result.RoundScore)}，{result.GetBreakdownText()}，获得金币 {result.CoinReward}";

            return $"火候 {CookRoundResultData.FormatScore(result.RoundScore)} 未命中目标，{result.GetBreakdownText()}，获得金币 {result.CoinReward}";
        }

        private void addFallbackSeeds()
        {
            _materialSeeds.Add(new CookMaterialSeedData { MaterialName = "胡萝卜", Count = 2 });
            _materialSeeds.Add(new CookMaterialSeedData { MaterialName = "土豆", Count = 2 });
            _materialSeeds.Add(new CookMaterialSeedData { MaterialName = "蘑菇", Count = 1 });
            _materialSeeds.Add(new CookMaterialSeedData { MaterialName = "南瓜", Count = 1 });
        }

        private void copyFirstHandMaterial()
        {
            if (_handMaterials.Count == 0)
            {
                _magicBoxBonus += 2f;
                return;
            }

            CookMaterialData source = _handMaterials[0];
            _handMaterials.Add(new CookMaterialData(
                _nextMaterialId++,
                source.MaterialName,
                source.BaseValue,
                source.TagText,
                source.CanProcess,
                source.RequiredCookValue,
                source.Icon));
        }

        private void refreshMagicBoxStatusText()
        {
            string boxState = IsMagicBoxUsed ? "魔盒已触碰" : "魔盒未触碰";
            string angelState = AngelRescueCount > 0 ? $"天使救援 {AngelRescueCount}" : "天使救援 0";
            string devilState = _devilRisk > 0 ? $"恶魔风险 +{CookRoundResultData.FormatScore(_devilRisk)}" : "恶魔风险 0";
            MagicBoxStatusText = $"{boxState}\n{angelState}\n{devilState}";
        }
    }
}
