/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪核心玩法控制器，负责局内流程与视图刷新
* │  类    名: CookController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common;
using Common.Defines;
using MVC;
using MVC.Controller;
using MVC.View;
using Module.StageSettle;
using Module.View;

namespace Module.Cook
{
    // 烹饪核心玩法控制器，负责局内流程与事件分发
    public class CookController : BaseController
    {
        private bool _hasOpenedStageEndView;

        public CookController()
        {
            SetModel(new CookModel());

            GameApp.ViewManager.Register(ViewType.CookView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_CookView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 10
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenCookView, openCookView);
            RegisterFunc(EventDefines.StartCookRun, startCookRun);
            RegisterFunc(EventDefines.CookPlaceMaterial, placeMaterial);
            RegisterFunc(EventDefines.CookMoveSlotMaterial, moveSlotMaterial);
            RegisterFunc(EventDefines.CookReturnSlotMaterial, returnSlotMaterial);
            RegisterFunc(EventDefines.CookMoveToPotTray, moveToPotTray);
            RegisterFunc(EventDefines.CookSwapPotTray, swapPotTray);
            RegisterFunc(EventDefines.CookReturnPotTray, returnPotTray);
            RegisterFunc(EventDefines.CookSubmitPotTray, submitPotTray);
            RegisterFunc(EventDefines.CookProcessMaterial, processMaterial);
            RegisterFunc(EventDefines.CookTouchMagicBox, touchMagicBox);
            RegisterFunc(EventDefines.CookUndoMaterial, undoMaterial);
            RegisterFunc(EventDefines.CookClearMaterials, clearMaterials);
            RegisterFunc(EventDefines.CookSkipTurn, skipTurn);
            RegisterFunc(EventDefines.CookSettleTurn, settleTurn);
            RegisterFunc(EventDefines.CookReturnToSelect, returnToSelect);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenCookView, openCookView);
            UnRegisterFunc(EventDefines.StartCookRun, startCookRun);
            UnRegisterFunc(EventDefines.CookPlaceMaterial, placeMaterial);
            UnRegisterFunc(EventDefines.CookMoveSlotMaterial, moveSlotMaterial);
            UnRegisterFunc(EventDefines.CookReturnSlotMaterial, returnSlotMaterial);
            UnRegisterFunc(EventDefines.CookMoveToPotTray, moveToPotTray);
            UnRegisterFunc(EventDefines.CookSwapPotTray, swapPotTray);
            UnRegisterFunc(EventDefines.CookReturnPotTray, returnPotTray);
            UnRegisterFunc(EventDefines.CookSubmitPotTray, submitPotTray);
            UnRegisterFunc(EventDefines.CookProcessMaterial, processMaterial);
            UnRegisterFunc(EventDefines.CookTouchMagicBox, touchMagicBox);
            UnRegisterFunc(EventDefines.CookUndoMaterial, undoMaterial);
            UnRegisterFunc(EventDefines.CookClearMaterials, clearMaterials);
            UnRegisterFunc(EventDefines.CookSkipTurn, skipTurn);
            UnRegisterFunc(EventDefines.CookSettleTurn, settleTurn);
            UnRegisterFunc(EventDefines.CookReturnToSelect, returnToSelect);
        }

        public override void OpenView(IBaseView view)
        {
            refreshCookView();
        }

        // 获取烹饪玩法模型
        public CookModel GetCookModel()
        {
            return GetModel<CookModel>();
        }

        // 打开烹饪玩法界面
        private void openCookView(object[] args)
        {
            GameApp.ViewManager.Open(ViewType.CookView, args);
        }

        // 开始一局烹饪玩法
        private void startCookRun(object[] args)
        {
            CookModel cookModel = GetCookModel();
            CookRunStartData startData = args != null && args.Length > 0 ? args[0] as CookRunStartData : null;
            cookModel.StartRun(startData);
            _hasOpenedStageEndView = false;

            GameApp.ViewManager.Open(ViewType.CookView, args);
            refreshCookView();

            QLog.Info($"[{nameof(CookController)}] 开始烹饪玩法");
        }

        // 放置材料到法阵
        private void placeMaterial(object[] args)
        {
            CookModel cookModel = GetCookModel();
            if (args == null || args.Length < 2) return;
            if (args[0] is not int materialId || args[1] is not int slotIndex) return;

            cookModel.PlaceMaterial(materialId, slotIndex);
            refreshCookView();
        }

        // 移动或交换法阵槽位材料
        private void moveSlotMaterial(object[] args)
        {
            CookModel cookModel = GetCookModel();
            if (args == null || args.Length < 2) return;
            if (args[0] is not int fromSlotIndex || args[1] is not int toSlotIndex) return;

            cookModel.MoveSlotMaterial(fromSlotIndex, toSlotIndex);
            refreshCookView();
        }

        // 将本回合法阵材料撤回到可用区域
        private void returnSlotMaterial(object[] args)
        {
            CookModel cookModel = GetCookModel();
            if (args == null || args.Length < 1) return;
            if (args[0] is not int slotIndex) return;

            cookModel.ReturnSlotMaterial(slotIndex);
            refreshCookView();
        }

        // 将法阵材料移到 Pot 暂存槽
        private void moveToPotTray(object[] args)
        {
            CookModel cookModel = GetCookModel();
            if (args == null || args.Length < 2) return;
            if (args[0] is not int slotIndex || args[1] is not int trayIndex) return;

            cookModel.MoveSlotToPotTray(slotIndex, trayIndex);
            refreshCookView();
        }

        // 交换暂存槽顺序
        private void swapPotTray(object[] args)
        {
            CookModel cookModel = GetCookModel();
            if (args == null || args.Length < 2) return;
            if (args[0] is not int fromTrayIndex || args[1] is not int toTrayIndex) return;

            cookModel.SwapPotTray(fromTrayIndex, toTrayIndex);
            refreshCookView();
        }

        // 从暂存槽撤回到法阵
        private void returnPotTray(object[] args)
        {
            CookModel cookModel = GetCookModel();
            if (args == null || args.Length < 1) return;
            if (args[0] is not int trayIndex) return;

            cookModel.ReturnPotTraySlot(trayIndex);
            refreshCookView();
        }

        // 集满后投入锅中
        private void submitPotTray(object[] args)
        {
            GetCookModel().SubmitPotTray();
            refreshCookView();
            openStageEndViewIfNeeded();
        }

        // 加工手牌材料
        private void processMaterial(object[] args)
        {
            CookModel cookModel = GetCookModel();
            if (args == null || args.Length < 1) return;
            if (args[0] is not int materialId) return;

            cookModel.ProcessMaterial(materialId);
            refreshCookView();
        }

        // 触碰魔盒
        private void touchMagicBox(object[] args)
        {
            GetCookModel().TouchMagicBox();
            refreshCookView();
        }

        // 撤回最近一次放置
        private void undoMaterial(object[] args)
        {
            GetCookModel().UndoLastPlace();
            refreshCookView();
        }

        // 清空法阵材料
        private void clearMaterials(object[] args)
        {
            GetCookModel().ClearPlacedMaterials();
            refreshCookView();
        }

        // 跳过当前回合
        private void skipTurn(object[] args)
        {
            GetCookModel().SkipTurn();
            refreshCookView();
            openStageEndViewIfNeeded();
        }

        // 结束当前回合（煮熟法阵材料 + 推进回合，不计分）
        private void settleTurn(object[] args)
        {
            CookModel cookModel = GetCookModel();
            cookModel.SettleTurn();
            refreshCookView();
            openStageEndViewIfNeeded();
        }

        // 小局结束后根据目标分与小局进度进入对应结算界面
        private void openStageEndViewIfNeeded()
        {
            CookModel cookModel = GetCookModel();
            if (_hasOpenedStageEndView || !cookModel.IsStageFinished) return;

            _hasOpenedStageEndView = true;
            if (cookModel.ShouldOpenFinalSummary)
            {
                QLog.Info($"[{nameof(CookController)}] 小局结束，进入最终结算，分数：{cookModel.GetScoreText()}");
                GameApp.ViewManager.Close(ViewType.CookView);
                ApplyControllerFunc(ControllerType.Summary, EventDefines.OpenSummaryView);
                return;
            }

            if (!cookModel.ShouldOpenStageSettle) return;

            QLog.Info($"[{nameof(CookController)}] 小局达标，进入小局结算，分数：{cookModel.GetScoreText()}");
            ApplyControllerFunc(ControllerType.StageSettle, EventDefines.OpenStageSettleView, buildStageSettleData(cookModel));
        }

        // 构建小局结算界面展示数据
        private static StageSettleData buildStageSettleData(CookModel cookModel)
        {
            return new StageSettleData
            {
                BoxName = cookModel.BoxName,
                StageId = cookModel.StageId,
                StageIndex = cookModel.StageIndex,
                StageCount = cookModel.StageCount,
                TurnCount = cookModel.MaxTurn,
                TargetMin = cookModel.TargetMin,
                TargetMax = cookModel.TargetMax,
                CurrentScore = cookModel.CurrentScore,
                Coin = cookModel.Coin,
                IsTargetReached = cookModel.IsStageTargetReached,
                IsFinalStage = cookModel.IsFinalStage
            };
        }

        // 返回材料选择界面
        private void returnToSelect(object[] args)
        {
            GameApp.ViewManager.Close(ViewType.CookView);
            ApplyControllerFunc(ControllerType.SelectBox, EventDefines.OpenSelectBoxView, args);
        }

        // 刷新烹饪玩法视图
        private void refreshCookView()
        {
            CookView cookView = GameApp.ViewManager.GetView<CookView>(ViewType.CookView);
            if (cookView == null) return;

            cookView.Refresh(GetCookModel());
        }
    }
}
