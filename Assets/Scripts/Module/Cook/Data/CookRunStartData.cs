/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪玩法启动数据，保存选择界面传入的难度与药箱材料
* │  类    名: CookRunStartData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Select;

namespace Module.Cook
{
    // 烹饪玩法启动数据，保存选择界面传入的难度与药箱材料
    public class CookRunStartData
    {
        public SelectDifficulty Difficulty;
        public string BoxId;
        public string BoxName;
        public List<CookMaterialSeedData> Materials = new();

        // 小局配置参数（来自关卡配置表，HasStageConfig=false 时 CookModel 走旧难度硬编码兜底）
        public bool HasStageConfig;
        public string StageId;
        public int TurnCount;
        public int PotTrayCapacity;
        public int TargetMin;
        public int TargetMax;
        public int HandCount;
        public int AngelRescueCount;
    }
}
