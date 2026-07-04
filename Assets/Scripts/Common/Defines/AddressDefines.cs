/*
* ┌──────────────────────────────────┐
* │  描    述: 资源路径定义类
* │  类    名: AddressDefines.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Common.Defines
{
    public static class AddressDefines
    {
        // UI预制体，对应 Resources 下的路径
        public const string UI_LoadingView = "UI/LoadingView";
        public const string UI_MainMenuView = "UI/View/MainMenuView";
        public const string UI_SelectBoxView = "UI/View/SelectBoxView";
        public const string UI_CookView = "UI/View/CookView";
        public const string UI_TextLine = "UI/TextLine/TextLine";
        public const string UI_MenuButton = "UI/Button/MenuButton";
        public const string UI_ShopView = "UI/View/ShopView";
        public const string UI_AlmanacView = "UI/View/AlmanacView";
        public const string UI_ShopCardSlot = "UI/Shop/ShopCardSlot";
        public const string UI_ShopPropSlot = "UI/Shop/ShopPropSlot";
        public const string UI_StoreView = "UI/View/StoreView";
        public const string UI_StoreBagItem = "UI/Store/StoreBagItem";
        public const string UI_StoreBuyItem = "UI/Store/StoreBuyItem";
        public const string UI_CookOwnedItemSlot = "UI/Cook/CookOwnedItemSlot";
        public const string UI_BlackjackView = "UI/View/BlackjackView";
        public const string UI_ConfirmView = "UI/View/ConfirmView";
        public const string UI_SummaryView = "UI/View/SummaryView";
        public const string UI_SettingsView = "UI/View/SettingsView";
        public const string UI_StageSettleView = "UI/View/StageSettleView";
        public const string UI_RecycleView = "UI/View/RecycleView";

        // JSON 配置，对应 Assets/Config/（不含扩展名）
        public const string Config_SelectBoxCatalog = "SelectBoxCatalog";
        public const string Config_ItemParamCatalog = "ItemParamCatalog";
        public const string Config_BlackjackDialogCatalog = "BlackjackDialogCatalog";
        public const string Config_ShopCatalog = "ShopCatalog";
        public const string Config_SoundCatalog = "Sound/SoundCatalog";

        // Art 与字体（Resources 路径，相对 Resources/）
        public const string Art_ShopMaterialBoxSample = "Art/ShopView/材料箱样本";
        public const string Art_ShopItemSample = "Art/ShopView/道具样本";
        public const string Art_ShopPriceTag = "Art/ShopView/价格贴";
        public const string Art_ShopContinueHover = "Art/ShopView/IMG_9173";
        public const string Art_SelectBoxStartHover = "Art/SelectBoxView/IMG_9171";
        public const string Art_SelectBoxReturnHover = "Art/SelectBoxView/IMG_9177";
        public const string Art_SelectBoxDifficultyEasy = "Art/SelectBoxView/5";
        public const string Art_SelectBoxDifficultyEasyHover = "Art/SelectBoxView/6";
        public const string Art_SelectBoxDifficultyNormal = "Art/SelectBoxView/3";
        public const string Art_SelectBoxDifficultyNormalHover = "Art/SelectBoxView/4";
        public const string Art_SelectBoxDifficultyHard = "Art/SelectBoxView/1";
        public const string Art_SelectBoxDifficultyHardHover = "Art/SelectBoxView/2";
        public const string Art_MainMenuStartHover = "Art/MainMenuView/开始游戏_点击";
        public const string Art_MainMenuSettingsHover = "Art/MainMenuView/设置_点击";
        public const string Art_MainMenuExitHover = "Art/MainMenuView/退出_点击";
        public const string Art_SummaryNextRoundHover = "Art/SummaryView/BtnNextRoundHover";
        public const string Art_SummaryBackHomeHover = "Art/SummaryView/BtnBackHomeHover";
        public const string Font_SourceHanSansSdf = "Fonts/jingnan/荆南缘默体 SDF";
    }
}
