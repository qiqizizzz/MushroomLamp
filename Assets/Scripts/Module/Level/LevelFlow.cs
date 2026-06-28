/*
* ┌──────────────────────────────────┐
* │  描    述: 关卡流程状态（当前大局/难度/第几小局），驱动小局推进
* │  类    名: LevelFlow.cs
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common;
using Module.Cook;
using Module.Select;

namespace Module.Level
{
    // 关卡流程单例：记录当前大局、难度、打到第几小局，提供推进与启动数据构建
    public class LevelFlow : Singleton<LevelFlow>
    {
        public string BoxId { get; private set; }
        public string BoxName { get; private set; }
        public SelectDifficulty Difficulty { get; private set; }
        public int StageIndex { get; private set; }
        public int StageCount { get; private set; }

        // 缓存当前箱子的材料种子（小局推进时沿用同一箱材料，不依赖 SelectBox）
        private readonly List<CookMaterialSeedData> _materials = new();

        public bool HasFlow => !string.IsNullOrEmpty(BoxId) && StageCount > 0;
        public bool IsLastStage => StageIndex >= StageCount - 1;

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
        }

        // 推进到下一小局；返回 false 表示已是最后小局（无下一个）
        public bool AdvanceStage()
        {
            if (StageIndex >= StageCount - 1) return false;
            StageIndex++;
            return true;
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

            LevelEntryJsonData level = findLevel(BoxId);
            StageJsonConfig stage = level != null
                ? LevelConfigLoader.GetStage(level, Difficulty, StageIndex)
                : null;

            if (stage == null)
            {
                QLog.Error($"[{nameof(LevelFlow)}] 小局配置缺失：boxId={BoxId} 难度={Difficulty} index={StageIndex}");
                return startData;
            }

            startData.HasStageConfig = true;
            startData.StageId = stage.stageId;
            startData.StageIndex = StageIndex;
            startData.StageCount = StageCount;
            startData.TurnCount = stage.turnCount;
            startData.PotTrayCapacity = stage.potTrayCapacity;
            startData.TargetMin = stage.targetMin;
            startData.TargetMax = stage.targetMax;
            startData.HandCount = stage.handCount;
            startData.AngelRescueCount = stage.angelRescueCount;
            return startData;
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
