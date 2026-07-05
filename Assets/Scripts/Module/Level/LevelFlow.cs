/*
* ┌──────────────────────────────────┐
* │  描    述: 关卡流程状态（当前大局/难度/第几小局），驱动小局推进
* │  类    名: LevelFlow.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Collections;
using Common;
using Module.Cook;
using Module.Item;
using Module.Material;
using Module.Select;

namespace Module.Level
{
    // 关卡流程单例：记录当前大局、难度、打到第几小局，提供推进与启动数据构建
    public class LevelFlow : Singleton<LevelFlow>
    {
        // 当前大局绑定的箱子 ID，用于从关卡配置目录定位小局配置
        public string BoxId { get; private set; }
        // 当前大局绑定的箱子展示名，用于 Cook 与结算界面展示
        public string BoxName { get; private set; }
        // 当前大局选择的难度
        public SelectDifficulty Difficulty { get; private set; }
        // 当前进行到的小局索引，从 0 开始
        public int StageIndex { get; private set; }
        // 当前难度下的小局总数
        public int StageCount { get; private set; }
        // 当前小局是否成功读取到关卡配置
        public bool HasStageConfig { get; private set; }
        // 当前小局配置 ID
        public string StageId { get; private set; }
        // 当前小局最大回合数
        public int MaxTurn { get; private set; }
        // 当前小局目标分下限
        public int TargetMin { get; private set; }
        // 当前小局目标分上限，可被魔盒等效果临时扩展
        public int TargetMax { get; private set; }
        // 当前小局锅暂存槽容量
        public int PotTrayCapacity { get; private set; }
        // 当前小局每回合发放的手牌数量
        public int HandCount { get; private set; }
        // 当前小局剩余天使救援次数
        public int AngelRescueCount { get; private set; }
        // 已完成小局累计得分
        public float TotalScore { get; private set; }
        // 已完成小局累计金币
        public int TotalCoinEarned { get; private set; }
        // 已完成小局累计回合数
        public int TotalTurnCount { get; private set; }
        // 已记录的小局数量
        public int CompletedStageCount { get; private set; }
        // 全局最高单次投锅得分
        public float MaxRoundScore { get; private set; }
        // 全局累计共鸣次数
        public int ResonanceCount { get; private set; }
        // 全局累计天使祝福次数
        public int AngelBlessCount { get; private set; }
        // 全局累计恶魔交易次数
        public int DevilDealCount { get; private set; }

        // 缓存当前箱子的材料种子（小局推进时沿用同一箱材料，不依赖 SelectBox）
        private readonly List<CookMaterialSeedData> _materials = new();
        private readonly HashSet<int> _recordedStageIndexes = new();

        public bool HasFlow => !string.IsNullOrEmpty(BoxId) && StageCount > 0;
        public bool IsLastStage => HasStageConfig && StageCount > 0 && StageIndex >= StageCount - 1;

        // 放弃当前未完成的大局进度，回到第一小局（选择页再次进入时用）
        public void AbandonInProgressRun()
        {
            if (!HasFlow) return;

            StageIndex = 0;
            resetRunSummary();
            refreshCurrentStageConfig();
        }

        // GM：从第一小局重新开始（保留当前箱子与材料池）
        public void GmRestartFromFirstStage()
        {
            if (!HasFlow) return;

            StageIndex = 0;
            resetRunSummary();
            refreshCurrentStageConfig();
        }

        // GM：跳转到指定难度的指定小局（0=第一关）
        public void GmJumpToStage(SelectDifficulty difficulty, int stageIndex)
        {
            if (!HasFlow) return;

            Difficulty = difficulty;
            LevelEntryJsonData level = findLevel(BoxId);
            StageCount = level != null ? LevelConfigLoader.GetStageCount(level, difficulty) : 0;

            if (StageCount <= 0)
            {
                applyFallbackConfig(difficulty);
                return;
            }

            StageIndex = UnityEngine.Mathf.Clamp(stageIndex, 0, StageCount - 1);
            resetRunSummary();
            refreshCurrentStageConfig();
        }

        // 开始一个大局（在 SelectBox 点开始时调用），定位到第一小局
        public void Begin(string boxId, string boxName, SelectDifficulty difficulty, IEnumerable<CookMaterialSeedData> materials)
        {
            LevelConfigLoader.ClearCache();

            BoxId = boxId;
            BoxName = boxName;
            Difficulty = difficulty;
            StageIndex = 0;

            _materials.Clear();
            if (materials != null)
                _materials.AddRange(materials);

            resetRunSummary();

            // 用 boxId 找大局，读该难度的小局总数
            LevelEntryJsonData level = findLevel(boxId);
            StageCount = level != null ? LevelConfigLoader.GetStageCount(level, difficulty) : 0;
            refreshCurrentStageConfig();
        }

        // 记录一个已完成小局的结算数据，返回是否首次记录
        public bool RecordStageResult(
            int stageIndex,
            int turnCount,
            float score,
            int coin,
            float maxRoundScore,
            int resonanceCount,
            int angelBlessCount,
            int devilDealCount)
        {
            if (!_recordedStageIndexes.Add(stageIndex)) return false;

            CompletedStageCount++;
            TotalTurnCount += turnCount;
            TotalScore += score;
            TotalCoinEarned += coin;
            if (maxRoundScore > MaxRoundScore)
                MaxRoundScore = maxRoundScore;
            ResonanceCount += resonanceCount;
            AngelBlessCount += angelBlessCount;
            DevilDealCount += devilDealCount;
            return true;
        }

        // 商店购买材料箱后，替换当前大局绑定的箱子与材料池（小局进度不变）
        public void SwitchBox(string boxId, string boxName, IEnumerable<CookMaterialSeedData> materials)
        {
            if (string.IsNullOrWhiteSpace(boxId)) return;

            BoxId = boxId;
            if (!string.IsNullOrWhiteSpace(boxName))
                BoxName = boxName;

            _materials.Clear();
            if (materials != null)
                _materials.AddRange(materials);

            QLog.Info($"[{nameof(LevelFlow)}] 切换材料箱：{BoxId} / {BoxName}，材料种类={_materials.Count}");
        }

        // 商店购箱三选一：将选中材料追加到当前大局材料池
        public void AddMaterial(string materialId, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(materialId) || count <= 0) return;

            for (int i = 0; i < _materials.Count; i++)
            {
                CookMaterialSeedData seed = _materials[i];
                if (seed == null || seed.MaterialId != materialId) continue;

                seed.Count += count;
                QLog.Info($"[{nameof(LevelFlow)}] 材料池追加：{materialId} x{count}（合计 {seed.Count}）");
                return;
            }

            MaterialJsonData cfg = MaterialCatalogLoader.GetById(materialId);
            _materials.Add(new CookMaterialSeedData
            {
                MaterialId = materialId,
                MaterialName = cfg?.name ?? materialId,
                Count = count
            });
            QLog.Info($"[{nameof(LevelFlow)}] 材料池新增：{materialId} x{count}");
        }

        // 推进到下一小局；返回 false 表示已是最后小局（无下一个）
        public bool AdvanceStage()
        {
            if (StageIndex >= StageCount - 1) return false;
            StageIndex++;
            refreshCurrentStageConfig();
            return true;
        }

        // 从指定已完成小局推进到下一小局，避免商店继续时使用旧索引
        public bool AdvanceStageAfter(int completedStageIndex)
        {
            if (!HasFlow) return false;

            int nextStageIndex = completedStageIndex + 1;
            if (nextStageIndex >= StageCount) return false;

            StageIndex = nextStageIndex;
            refreshCurrentStageConfig();
            return true;
        }

        // 准备一次烹饪启动，兼容调试入口直接传入 CookRunStartData 的情况
        public void PrepareRun(CookRunStartData startData)
        {
            if (startData == null)
            {
                applyFallbackConfig(SelectDifficulty.Normal);
                return;
            }

            Difficulty = startData.Difficulty;
            BoxId = startData.BoxId;
            BoxName = string.IsNullOrWhiteSpace(startData.BoxName) ? "默认药箱" : startData.BoxName;

            if (startData.HasStageConfig)
            {
                HasStageConfig = true;
                StageId = startData.StageId;
                StageIndex = startData.StageIndex;
                StageCount = startData.StageCount;
                MaxTurn = startData.TurnCount > 0 ? startData.TurnCount : 1;
                PotTrayCapacity = startData.PotTrayCapacity > 0 ? startData.PotTrayCapacity : 3;
                TargetMin = startData.TargetMin;
                TargetMax = startData.TargetMax;
                HandCount = startData.HandCount > 0 ? startData.HandCount : 6;
                AngelRescueCount = startData.AngelRescueCount > 0 ? startData.AngelRescueCount : 0;
                return;
            }

            applyFallbackConfig(startData.Difficulty);
        }

        // 获取指定序号小局配置
        public StageJsonConfig GetStageConfig(int stageIndex)
        {
            LevelEntryJsonData level = findLevel(BoxId);
            return level != null ? LevelConfigLoader.GetStage(level, Difficulty, stageIndex) : null;
        }

        // 扩展当前小局目标上限
        public void ExpandTarget(int value)
        {
            TargetMax += value;
        }

        // 消耗一次天使救援
        public void ConsumeAngelRescue()
        {
            if (AngelRescueCount > 0)
                AngelRescueCount--;
        }

        // 构建当前小局的烹饪启动数据（用缓存材料 + 当前 stageIndex 的小局参数）
        public CookRunStartData BuildStartData()
        {
            CookRunStartData startData = new CookRunStartData
            {
                Difficulty = Difficulty,
                BoxId = BoxId,
                BoxName = BoxName
            };

            foreach (CookMaterialSeedData seed in _materials)
                startData.Materials.Add(seed);

            if (!HasStageConfig)
            {
                QLog.Error($"[{nameof(LevelFlow)}] 小局配置缺失：boxId={BoxId} 难度={Difficulty} index={StageIndex}");
                return startData;
            }

            startData.HasStageConfig = true;
            startData.StageId = StageId;
            startData.StageIndex = StageIndex;
            startData.StageCount = StageCount;
            startData.TurnCount = MaxTurn;
            startData.PotTrayCapacity = PotTrayCapacity;
            startData.TargetMin = TargetMin;
            startData.TargetMax = TargetMax;
            startData.HandCount = HandCount;
            startData.AngelRescueCount = AngelRescueCount;
            return startData;
        }

        // 刷新当前小局配置
        private void refreshCurrentStageConfig()
        {
            StageJsonConfig stage = GetStageConfig(StageIndex);
            if (stage == null)
            {
                applyFallbackConfig(Difficulty);
                return;
            }

            HasStageConfig = true;
            StageId = stage.stageId;
            MaxTurn = stage.turnCount > 0 ? stage.turnCount : 1;
            MaxTurn += ItemPassiveManager.GetRoundCountBonus();
            PotTrayCapacity = stage.potTrayCapacity > 0 ? stage.potTrayCapacity : 3;
            TargetMin = stage.targetMin;
            TargetMax = stage.targetMax;
            HandCount = stage.handCount > 0 ? stage.handCount : 6;
            AngelRescueCount = stage.angelRescueCount > 0 ? stage.angelRescueCount : 0;
        }

        // 应用旧难度兜底配置
        private void applyFallbackConfig(SelectDifficulty difficulty)
        {
            HasStageConfig = false;
            StageId = string.Empty;
            StageIndex = 0;
            StageCount = 0;
            MaxTurn = difficulty == SelectDifficulty.Easy ? 5 : 6;
            TargetMin = difficulty switch
            {
                SelectDifficulty.Easy => 14,
                SelectDifficulty.Hard => 22,
                _ => 18
            };
            TargetMax = TargetMin + (difficulty == SelectDifficulty.Hard ? 3 : 4);
            PotTrayCapacity = difficulty == SelectDifficulty.Hard ? 4 : 3;
            HandCount = 6;
            AngelRescueCount = difficulty == SelectDifficulty.Hard ? 1 : 2;
        }

        // 重置当前大局的最终结算累计数据
        private void resetRunSummary()
        {
            TotalScore = 0;
            TotalCoinEarned = 0;
            TotalTurnCount = 0;
            CompletedStageCount = 0;
            MaxRoundScore = 0;
            ResonanceCount = 0;
            AngelBlessCount = 0;
            DevilDealCount = 0;
            _recordedStageIndexes.Clear();
        }

        private static LevelEntryJsonData findLevel(string boxId)
        {
            LevelCatalogJsonConfig catalog = LevelConfigLoader.LoadCatalog();
            if (catalog?.levels == null) return null;

            foreach (LevelEntryJsonData lv in catalog.levels)
                if (lv != null && lv.boxId == boxId) return lv;

            return null;
        }
    }
}
