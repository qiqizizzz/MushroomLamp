/*
* ┌──────────────────────────────────┐
* │  描    述: 材料三选一弹层数据模型
* │  类    名: MaterialPickModel.cs
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Module.Material;

namespace Module.MaterialPick
{
    public class MaterialPickModel
    {
        public string title = "幸运三选一";
        public IReadOnlyList<MaterialJsonData> candidates;
        public Action<int> onPicked;
    }
}
