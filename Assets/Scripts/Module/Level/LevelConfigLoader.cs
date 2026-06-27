/*
* ┌──────────────────────────────────┐
* │  描    述: 关卡配置读表入口（大局目录 + 小局加载）
* │  类    名: LevelConfigLoader.cs
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common;
using Module.Select;

namespace Module.Level
{
    // 关卡配置读表框架：当前仅负责加载与查询，未来由玩法流程接入生效
    public static class LevelConfigLoader
    {
        public const string LevelCatalogAddress = "Levels/LevelCatalog";

        private static LevelCatalogJsonConfig _catalog;
        private static readonly Dictionary<string, StageGroupJsonConfig> _stageGroupCache = new();

        // 加载大局目录（带缓存）
        public static LevelCatalogJsonConfig LoadCatalog()
        {
            if (_catalog != null) return _catalog;

            _catalog = JsonConfigLoader.LoadFromConfig<LevelCatalogJsonConfig>(LevelCatalogAddress);
            if (_catalog?.levels == null || _catalog.levels.Length == 0)
                QLog.Error($"[{nameof(LevelConfigLoader)}] 大局目录加载失败或为空：{LevelCatalogAddress}");

            return _catalog;
        }

        // 按 id 获取大局；为空时返回默认大局
        public static LevelEntryJsonData GetLevel(string levelId)
        {
            LevelCatalogJsonConfig catalog = LoadCatalog();
            if (catalog?.levels == null) return null;

            string targetId = string.IsNullOrEmpty(levelId) ? catalog.defaultLevelId : levelId;
            foreach (LevelEntryJsonData level in catalog.levels)
            {
                if (level != null && level.id == targetId)
                    return level;
            }

            // 找不到则退回第一个
            return catalog.levels.Length > 0 ? catalog.levels[0] : null;
        }

        // 加载某种类的小局集合文件（带缓存）。stageFile 如 "Levels/Stages/Stage_herb"
        public static StageGroupJsonConfig LoadStageGroup(string stageFile)
        {
            if (string.IsNullOrEmpty(stageFile)) return null;

            if (_stageGroupCache.TryGetValue(stageFile, out StageGroupJsonConfig cached))
                return cached;

            StageGroupJsonConfig group = JsonConfigLoader.LoadFromConfig<StageGroupJsonConfig>(stageFile);
            if (group?.stages == null)
            {
                QLog.Error($"[{nameof(LevelConfigLoader)}] 小局集合加载失败或为空：{stageFile}");
                return null;
            }

            _stageGroupCache[stageFile] = group;
            return group;
        }

        // 加载某大局指定难度的全部小局
        public static List<StageJsonConfig> LoadStagesOf(LevelEntryJsonData level, SelectDifficulty difficulty)
        {
            List<StageJsonConfig> stages = new List<StageJsonConfig>();
            if (level == null) return stages;

            StageGroupJsonConfig group = LoadStageGroup(level.GetStageFile(difficulty));
            if (group?.stages != null)
                stages.AddRange(group.stages);

            return stages;
        }

        // 获取某大局指定难度、指定序号的小局（stageIndex 从 0 起）
        public static StageJsonConfig GetStage(LevelEntryJsonData level, SelectDifficulty difficulty, int stageIndex)
        {
            StageGroupJsonConfig group = LoadStageGroup(level?.GetStageFile(difficulty));
            if (group?.stages == null || stageIndex < 0 || stageIndex >= group.stages.Length)
                return null;
            return group.stages[stageIndex];
        }

        // 获取某大局指定难度的小局总数
        public static int GetStageCount(LevelEntryJsonData level, SelectDifficulty difficulty)
        {
            StageGroupJsonConfig group = LoadStageGroup(level?.GetStageFile(difficulty));
            return group?.stages?.Length ?? 0;
        }

        // 清理缓存（用于重载配置）
        public static void ClearCache()
        {
            _catalog = null;
            _stageGroupCache.Clear();
        }
    }
}
