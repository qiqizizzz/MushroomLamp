/*
* ┌──────────────────────────────────┐
* │  描    述: 制作人名单控制器，负责打开界面与返回来源
* │  类    名: AlmanacController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.Controller;
using MVC.View;

namespace Module.Almanac
{
    public class AlmanacController : BaseController
    {
        // ==================== 字段[私有] ====================
        private ViewType _returnView = ViewType.MainMenuView;

        // ==================== 生命周期 ====================
        public AlmanacController()
        {
            GameApp.ViewManager.Register(ViewType.AlmanacView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_AlmanacView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 10
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenAlmanacView, openAlmanacView);
            RegisterFunc(EventDefines.AlmanacReturn, onReturn);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenAlmanacView, openAlmanacView);
            UnRegisterFunc(EventDefines.AlmanacReturn, onReturn);
        }

        // ==================== Private Function ====================
        // 打开制作人名单并记录返回来源
        private void openAlmanacView(object[] args)
        {
            _returnView = resolveReturnView(args);
            GameApp.ViewManager.Open(ViewType.AlmanacView, args);
        }

        // 关闭制作人名单并回到来源界面
        private void onReturn(object[] args)
        {
            GameApp.ViewManager.Close(ViewType.AlmanacView);

            if (_returnView == ViewType.SummaryView)
                GameApp.ViewManager.Open(ViewType.SummaryView);
            else
                ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView, args);
        }

        // 解析制作人名单关闭后的返回界面
        private static ViewType resolveReturnView(object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is ViewType returnView)
                return returnView;

            return ViewType.MainMenuView;
        }
    }
}
