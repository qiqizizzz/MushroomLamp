/*
* ┌──────────────────────────────────┐
* │  描    述: 关卡配置 JSON 结构（大局表 + 小局表）
* │  类    名: LevelJsonConfig.cs
* └──────────────────────────────────┘
*/

using System;
using Module.Select;

namespace Module.Level
{
    // ── 大局表 LevelCatalog.json ──

    [Serializable]
    public class LevelCatalogJsonConfig
    {
        public string defaultLevelId;
        public LevelEntryJsonData[] levels;
    }

    [Serializable]
    public class LevelEntryJsonData
    {
        public string id;
        public string displayName;
        public string boxId;                         // 关联的卡牌箱（沿用现有 SelectBox 体系）
        public StageFilesByDifficulty stageFilesByDifficulty;

        // 按难度取对应的小局集合文件（相对 Config，不含 .json）
        public string GetStageFile(SelectDifficulty difficulty)
        {
            if (stageFilesByDifficulty == null) return null;
            return difficulty switch
            {
                SelectDifficulty.Easy => stageFilesByDifficulty.easy,
                SelectDifficulty.Hard => stageFilesByDifficulty.hard,
                _ => stageFilesByDifficulty.normal
            };
        }
    }

    [Serializable]
    public class StageFilesByDifficulty
    {
        public string easy;
        public string normal;
        public string hard;
    }

    [Serializable]
    public class StageGroupJsonConfig
    {
        public StageJsonConfig[] stages;
    }

    [Serializable]
    public class StageJsonConfig
    {
        public string stageId;
        public string displayName;
        public int turnCount = 5;          // 回合数
        public int potTrayCapacity = 3;    // Pot 暂存槽位数
        public int targetMin = 14;         // 目标分下限
        public int targetMax = 18;         // 目标分上限
        public int handCount = 6;          // 每回合手牌数
        public int angelRescueCount = 2;   // 天使救援次数（扩展）
    }
}
