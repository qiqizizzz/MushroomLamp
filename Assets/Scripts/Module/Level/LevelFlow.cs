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

        // 缓存当前箱子的材料种子（小局推进时沿用同一箱材料，不依赖 SelectBox）
        private readonly List<CookMaterialSeedData> _materials = new();

        public bool HasFlow => !string.IsNullOrEmpty(BoxId) && StageCount > 0;
        public bool IsLastStage => HasStageConfig && StageCount > 0 && StageIndex >= StageCount - 1;

        // 开始一个大局（在 SelectBox 点开始时调用），定位到第一小局
        public void Begin(string boxId, string boxName, SelectDifficulty difficulty, IEnumerable<CookMaterialSeedData> materials)
        {
            BoxId = boxId;
            BoxName = boxName;
            Difficulty = difficulty;
            StageIndex = 0;

            _materials.Clear();
            if (materials != null)
                _materials.AddRange(materials);

            // 用 boxId 找大局，读该难度的小局总数
            LevelEntryJsonData level = findLevel(boxId);
            StageCount = level != null ? LevelConfigLoader.GetStageCount(level, difficulty) : 0;
            refreshCurrentStageConfig();
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
            PotTrayCapacity = 3;
            HandCount = 6;
            AngelRescueCount = difficulty == SelectDifficulty.Hard ? 1 : 2;
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
