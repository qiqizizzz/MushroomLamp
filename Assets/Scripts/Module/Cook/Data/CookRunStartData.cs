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
    }
}
