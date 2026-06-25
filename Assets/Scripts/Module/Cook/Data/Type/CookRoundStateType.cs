/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪回合状态枚举，限制操作界面当前可执行行为
* │  类    名: CookRoundStateType.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Cook
{
    public enum CookRoundStateType
    {
        RoundStart = 0,
        Operating = 1,
        ReadyToSettle = 2,
        Settled = 3,
        Finished = 4
    }
}
