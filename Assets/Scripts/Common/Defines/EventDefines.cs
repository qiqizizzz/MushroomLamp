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
        public const string CookPlaceMaterial = "CookPlaceMaterial";
        public const string CookMoveSlotMaterial = "CookMoveSlotMaterial";
        public const string CookReturnSlotMaterial = "CookReturnSlotMaterial";
        public const string CookSubmitToPot = "CookSubmitToPot";
        public const string CookProcessMaterial = "CookProcessMaterial";
        public const string CookTouchMagicBox = "CookTouchMagicBox";
        public const string CookUndoMaterial = "CookUndoMaterial";
        public const string CookClearMaterials = "CookClearMaterials";
        public const string CookSkipTurn = "CookSkipTurn";
        public const string CookSettleTurn = "CookSettleTurn";
        public const string CookReturnToSelect = "CookReturnToSelect";

        // 材料箱选择
        public const string OpenSelectBoxView = "OpenSelectBoxView";
        public const string SelectBoxReturn = "SelectBoxReturn";
        public const string SelectBoxSetDifficulty = "SelectBoxSetDifficulty";
        public const string SelectBoxChangeBox = "SelectBoxChangeBox";
        public const string SelectBoxStart = "SelectBoxStart";
        // 下一模块就绪后使用
        public const string SelectBoxStartGame = "SelectBoxStartGame";

        // 图鉴
        public const string OpenAlmanacView = "OpenAlmanacView";
        public const string AlmanacReturn = "AlmanacReturn";
        public const string AlmanacSwitchTab = "AlmanacSwitchTab";

        // 场景事件
        public const string LoadingScene = "LoadingScene";
    }
}
