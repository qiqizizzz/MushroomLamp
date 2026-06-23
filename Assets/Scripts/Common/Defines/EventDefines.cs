/*
* ┌──────────────────────────────────┐
* │  描    述: 事件定义类
* │  类    名: EventDefines.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Common.Defines
{
    public static class EventDefines
    {
        // UI事件
        public const string OpenMainMenuView = "OpenMainMenuView";
        public const string MainMenuStart = "MainMenuStart";
        public const string MainMenuOpenSettings = "MainMenuOpenSettings";
        public const string MainMenuExit = "MainMenuExit";

        // 烹饪玩法事件
        public const string OpenCookView = "OpenCookView";
        public const string StartCookRun = "StartCookRun";
        public const string AdvanceCookTurn = "AdvanceCookTurn";

        // 材料箱选择
        public const string OpenSelectBoxView = "OpenSelectBoxView";
        public const string SelectBoxReturn = "SelectBoxReturn";
        public const string SelectBoxSetDifficulty = "SelectBoxSetDifficulty";
        public const string SelectBoxChangeBox = "SelectBoxChangeBox";
        public const string SelectBoxStart = "SelectBoxStart";
        // 下一模块就绪后使用
        public const string SelectBoxStartGame = "SelectBoxStartGame";

        // 场景事件
        public const string LoadingScene = "LoadingScene";
    }
}
