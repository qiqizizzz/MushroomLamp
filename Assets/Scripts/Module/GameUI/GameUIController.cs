/*
* ┌──────────────────────────────────┐
* │  描    述: 游戏通用 UI 控制器，作为业务 UI 注册入口
* │  类    名: GameUIController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.Controller;

namespace Module.GameUI
{
    public class GameUIController : BaseController
    {
        public GameUIController()
        {
            GameApp.ViewManager.Register(ViewType.MainMenuView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_MainMenuView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 0
            });

            InitModuleEvent();
            InitGlobalEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenMainMenuView, openMainMenuView);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenMainMenuView, openMainMenuView);
        }

        // 打开主菜单界面
        private void openMainMenuView(object[] args)
        {
            GameApp.ViewManager.Open(ViewType.MainMenuView, args);
        }
    }
}
