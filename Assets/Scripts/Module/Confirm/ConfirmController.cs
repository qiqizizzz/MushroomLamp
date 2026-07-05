/*
* ┌──────────────────────────────────┐
* │  描    述: 二次确认弹窗控制器
* │  类    名: ConfirmController.cs
* └──────────────────────────────────┘
*/

using System.Collections;
using Common;
using Common.Defines;
using Common.UI;
using MVC;
using MVC.Controller;

namespace Module.Confirm
{
    public class ConfirmController : BaseController
    {
        private ConfirmModel _currentModel;
        private bool _isClosing;

        public static bool IsVisible =>
            GameApp.ViewManager != null && GameApp.ViewManager.IsOpen((int)ViewType.ConfirmView);

        public ConfirmController()
        {
            GameApp.ViewManager.Register(ViewType.ConfirmView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_ConfirmView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 100,
                IsOverlay = true
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
            _isClosing = false;
            _currentModel = model;
            GameApp.ViewManager.Open(ViewType.ConfirmView, args);
        }

        private void onConfirm(object[] args) => dispatchChoice(true);

        private void onCancel(object[] args) => dispatchChoice(false);

        private void dispatchChoice(bool confirmed)
        {
            if (_isClosing) return;

            ConfirmModel model = _currentModel;
            _currentModel = null;
            _isClosing = true;
            UiClickGuard.BlockForFrames(10);

            if (confirmed)
                model?.onConfirm?.Invoke();
            else
                model?.onCancel?.Invoke();

            model?.onResult?.Invoke(confirmed);
            GameAppRunner.Run(deferredClose());
        }

        private static IEnumerator deferredClose()
        {
            yield return null;
            GameApp.ViewManager.Close(ViewType.ConfirmView);
        }

        public static void Show(ConfirmModel model)
        {
            if (model == null) return;
            GameAppRunner.Run(showNextFrame(model));
        }

        private static IEnumerator showNextFrame(ConfirmModel model)
        {
            yield return null;
            UiClickGuard.BlockForFrames(3);
            GameApp.ControllerManager.ApplyFunc((int)ControllerType.Confirm, EventDefines.OpenConfirmView, model);
        }
    }
}
