/*
* ┌──────────────────────────────────┐
* │  描    述: 材料三选一弹层控制器
* │  类    名: MaterialPickController.cs
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.Controller;
using MVC.View;

namespace Module.MaterialPick
{
    public class MaterialPickController : BaseController
    {
        private MaterialPickModel _currentModel;

        public MaterialPickController()
        {
            GameApp.ViewManager.Register(ViewType.MaterialPickView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_MaterialPickView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 50,
                IsOverlay = true
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenMaterialPickView, openMaterialPickView);
            RegisterFunc(EventDefines.MaterialPickSelect, onMaterialSelected);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenMaterialPickView, openMaterialPickView);
            UnRegisterFunc(EventDefines.MaterialPickSelect, onMaterialSelected);
        }

        public static void Show(MaterialPickModel model)
        {
            GameApp.ControllerManager.ApplyFunc(
                (int)ControllerType.MaterialPick,
                EventDefines.OpenMaterialPickView,
                model);
        }

        private void openMaterialPickView(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not MaterialPickModel model) return;
            _currentModel = model;
            GameApp.ViewManager.Open(ViewType.MaterialPickView, model);
        }

        private void onMaterialSelected(object[] args)
        {
            if (_currentModel == null) return;

            int index = resolveIndex(args);
            if (index < 0) return;

            MaterialPickModel model = _currentModel;
            _currentModel = null;

            MaterialPickView view = GameApp.ViewManager.GetView<MaterialPickView>(ViewType.MaterialPickView);
            if (view == null)
            {
                finishPick(model, index);
                return;
            }

            view.PlayCloseAnimation(() => finishPick(model, index));
        }

        private void finishPick(MaterialPickModel model, int index)
        {
            if (GameApp.ViewManager.IsOpen((int)ViewType.MaterialPickView))
                GameApp.ViewManager.Close(ViewType.MaterialPickView);

            model.onPicked?.Invoke(index);
        }

        private static int resolveIndex(object[] args)
        {
            if (args == null || args.Length == 0) return -1;
            return args[0] is int index ? index : -1;
        }
    }
}
