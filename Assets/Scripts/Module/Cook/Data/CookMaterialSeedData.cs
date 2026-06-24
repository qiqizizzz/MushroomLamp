/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪玩法材料初始数据，承接选择界面传入的材料
* │  类    名: CookMaterialSeedData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using UnityEngine;

namespace Module.Cook
{
    // 烹饪玩法材料初始数据，承接选择界面传入的材料
    [Serializable]
    public class CookMaterialSeedData
    {
        public string MaterialName;
        public int Count;
        public Sprite Icon;
    }
}
