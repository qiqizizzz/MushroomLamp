/*
* ┌──────────────────────────────────┐
* │  描    述: 二次确认弹窗控制器
* │  类    名: ConfirmController.cs
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.Controller;

namespace Module.Confirm
{
    public class ConfirmController : BaseController
    {
        private ConfirmModel _currentModel;

        public ConfirmController()
        {
            GameApp.ViewManager.Register(ViewType.ConfirmView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_ConfirmView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 100
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenConfirmView, openConfirmView);
            RegisterFunc(EventDefines.ConfirmViewConfirm, onConfirm);
            RegisterFunc(EventDefines.ConfirmViewCancel, onCancel);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenConfirmView, openConfirmView);
            UnRegisterFunc(EventDefines.ConfirmViewConfirm, onConfirm);
            UnRegisterFunc(EventDefines.ConfirmViewCancel, onCancel);
        }

        private void openConfirmView(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not ConfirmModel model) return;
            _currentModel = model;
            GameApp.ViewManager.Open(ViewType.ConfirmView, args);
        }

        private void onConfirm(object[] args)
        {
            GameApp.ViewManager.Close(ViewType.ConfirmView);
            var cb = _currentModel?.onConfirm;
            _currentModel = null;
            cb?.Invoke();
        }

        private void onCancel(object[] args)
        {
            GameApp.ViewManager.Close(ViewType.ConfirmView);
            var cb = _currentModel?.onCancel;
            _currentModel = null;
            cb?.Invoke();
        }

        /// <summary>
        /// 静态便捷入口，任意位置直接调用
        /// </summary>
        public static void Show(ConfirmModel model)
        {
            GameApp.ControllerManager.ApplyFunc((int)ControllerType.Confirm, EventDefines.OpenConfirmView, model);
        }
    }
}
