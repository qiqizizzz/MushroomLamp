/*
* ┌──────────────────────────────────┐
* │  描    述: 调试功能总开关（打包前可在此关闭 GM 等）
* │  类    名: GameDebugSettings.cs
* └──────────────────────────────────┘
*/

namespace Common.Defines
{
    public static class GameDebugSettings
    {
        // 打包发布前改为 false，即可禁用 F1 GM 面板（GM 代码仍保留）
        public const bool EnableGMPanel = true;
    }
}
