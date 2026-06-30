/*
* ┌──────────────────────────────────┐
* │  描    述: 图鉴控制器
* │  类    名: AlmanacController.cs
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
        private ViewType _returnView = ViewType.MainMenuView;

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
            RegisterFunc(EventDefines.AlmanacSwitchTab, onSwitchTab);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenAlmanacView, openAlmanacView);
            UnRegisterFunc(EventDefines.AlmanacReturn, onReturn);
            UnRegisterFunc(EventDefines.AlmanacSwitchTab, onSwitchTab);
        }

        private void openAlmanacView(object[] args)
        {
            _returnView = resolveReturnView(args);
            GameApp.ViewManager.Open(ViewType.AlmanacView, args);
        }

        private void onReturn(object[] args)
        {
            GameApp.ViewManager.Close(ViewType.AlmanacView);

            if (_returnView == ViewType.SummaryView)
                GameApp.ViewManager.Open(ViewType.SummaryView);
            else
                ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView, args);
        }

        private static ViewType resolveReturnView(object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is ViewType returnView)
                return returnView;

            return ViewType.MainMenuView;
        }

        private void onSwitchTab(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not bool isCard) return;

            AlmanacView view = GameApp.ViewManager.GetView<AlmanacView>(ViewType.AlmanacView);
            view?.SwitchTab(isCard);
        }
    }
}
