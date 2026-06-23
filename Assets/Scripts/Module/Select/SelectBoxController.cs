/*
 * ┌──────────────────────────────────┐
 * │  描    述: 材料箱选择页控制器
 * │  类    名: SelectBoxController.cs
 * └──────────────────────────────────┘
 */

using Common;
using Common.Defines;
using Module.View;
using MVC;
using MVC.Controller;
using MVC.Extensions;
using MVC.View;

namespace Module.Select
{
    public class SelectBoxController : BaseController
    {
        public SelectBoxController()
        {
            GameApp.ViewManager.Register(ViewType.SelectBoxView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_SelectBoxView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 0
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenSelectBoxView, openSelectBoxView);
            RegisterFunc(EventDefines.SelectBoxReturn, onReturn);
            RegisterFunc(EventDefines.SelectBoxSetDifficulty, onSetDifficulty);
            RegisterFunc(EventDefines.SelectBoxChangeBox, onChangeBox);
            RegisterFunc(EventDefines.SelectBoxStart, onStart);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenSelectBoxView, openSelectBoxView);
            UnRegisterFunc(EventDefines.SelectBoxReturn, onReturn);
            UnRegisterFunc(EventDefines.SelectBoxSetDifficulty, onSetDifficulty);
            UnRegisterFunc(EventDefines.SelectBoxChangeBox, onChangeBox);
            UnRegisterFunc(EventDefines.SelectBoxStart, onStart);
        }

        public override void OpenView(IBaseView view)
        {
            refreshView(view as SelectBoxView);
        }

        private void openSelectBoxView(object[] args)
        {
            ensureModel();
            GameApp.ViewManager.Open(ViewType.SelectBoxView, args);
        }

        private void onSetDifficulty(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not SelectDifficulty difficulty)
                return;

            SelectBoxModel model = ensureModel();
            model.SetDifficulty(difficulty);
            QLog.Info($"[{nameof(SelectBoxController)}] 选择难度：{difficulty}");
            refreshView();
        }

        private void onReturn(object[] args)
        {
            GameApp.ViewManager.Close(ViewType.SelectBoxView);
            ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView, args);
        }

        private void onChangeBox(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not int delta)
                return;

            SelectBoxModel model = ensureModel();
            model.ChangeBoxIndex(delta);

            SelectBoxCatalogEntry entry = model.GetCurrentBoxEntry();
            QLog.Info(
                $"[{nameof(SelectBoxController)}] 切换药箱 index={model.SelectedBoxIndex} " +
                $"id={entry?.id} name={entry?.displayName} (delta={delta})");

            refreshView();
        }

        private void onStart(object[] args)
        {
            SelectBoxModel model = ensureModel();
            SelectBoxCatalogEntry entry = model.GetCurrentBoxEntry();

            QLog.Info(
                $"[{nameof(SelectBoxController)}] 开始游戏（占位） " +
                $"难度={model.Difficulty} boxId={entry?.id} boxName={entry?.displayName} " +
                $"boxIndex={model.SelectedBoxIndex}/{model.BoxCount}");

            // TODO: 下一模块就绪后，通过事件传递 difficulty + boxId
            // ApplyControllerFunc(ControllerType.XXX, EventDefines.SelectBoxStartGame, model.Difficulty, entry.id);
        }

        private SelectBoxModel ensureModel()
        {
            SelectBoxModel model = GetModel<SelectBoxModel>();
            if (model != null) return model;

            model = new SelectBoxModel();
            SetModel(model);
            model.EnsureCatalogLoaded();
            return model;
        }

        private void refreshView(SelectBoxView view = null)
        {
            view ??= GameApp.ViewManager.GetView<SelectBoxView>(ViewType.SelectBoxView);
            SelectBoxModel model = GetModel<SelectBoxModel>();
            view?.Refresh(model);
        }
    }
}
