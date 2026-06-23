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
        public const string UI_TextLine = "UI/TextLine/TextLine";
        public const string UI_MenuButton = "UI/Button/MenuButton";

        // JSON 配置，对应 Assets/Config/（不含扩展名）
        public const string Config_SelectBoxCatalog = "SelectBoxCatalog";
    }
}
