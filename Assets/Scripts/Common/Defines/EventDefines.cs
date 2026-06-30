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
        public const string CookMoveToPotTray = "CookMoveToPotTray";
        public const string CookSwapPotTray = "CookSwapPotTray";
        public const string CookReturnPotTray = "CookReturnPotTray";
        public const string CookSubmitPotTray = "CookSubmitPotTray";
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

        // 二次确认弹窗
        public const string OpenConfirmView = "OpenConfirmView";
        public const string ConfirmViewConfirm = "ConfirmViewConfirm";
        public const string ConfirmViewCancel = "ConfirmViewCancel";

        // 总结算
        public const string OpenSummaryView = "OpenSummaryView";

        // 小局结算
        public const string OpenStageSettleView = "OpenStageSettleView";
        public const string StageSettleToShop = "StageSettleToShop";

        // 21 点玩法
        public const string OpenBlackjackView = "OpenBlackjackView";
        public const string BlackjackDraw = "BlackjackDraw";
        public const string BlackjackReturn = "BlackjackReturn";
        public const string BlackjackRestart = "BlackjackRestart";

        // 商店子页面（购买 + 背包）
        public const string OpenStoreView = "OpenStoreView";
        public const string StoreReturn = "StoreReturn";
        public const string StoreBuy = "StoreBuy";
        public const string StoreSetBagCount = "StoreSetBagCount";

        // 回收界面
        public const string OpenRecycleView = "OpenRecycleView";
        public const string RecycleReturn = "RecycleReturn";
        public const string RecycleSellSelected = "RecycleSellSelected";

        // 设置
        public const string OpenSettingsView = "OpenSettingsView";
        public const string SettingsClose = "SettingsClose";
        public const string SettingsSetSfxOn = "SettingsSetSfxOn";
        public const string SettingsSetSfxVolume = "SettingsSetSfxVolume";
        public const string SettingsSetBgmOn = "SettingsSetBgmOn";
        public const string SettingsSetBgmVolume = "SettingsSetBgmVolume";
    }
}
