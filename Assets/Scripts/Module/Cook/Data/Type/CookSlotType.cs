/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪法阵槽位类型，区分中心、四边与四角
* │  类    名: CookSlotType.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Cook
{
    // 烹饪法阵槽位类型，决定附魔强度
    public enum CookSlotType
    {
        Corner = 0,
        Edge = 1,
        Center = 2
    }
}
