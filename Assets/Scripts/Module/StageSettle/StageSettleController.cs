/*
* ┌──────────────────────────────────┐
* │  描    述: 小局结算界面控制器
* │  类    名: StageSettleController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using Module.Level;
using Module.Player;
using MVC;
using MVC.Controller;
using MVC.View;

namespace Module.StageSettle
{
    // 小局结算界面控制器，负责打开结算界面与后续跳转
    public class StageSettleController : BaseController
    {
        private StageSettleData _currentData;

        public StageSettleController()
        {
            GameApp.ViewManager.Register(ViewType.StageSettleView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_StageSettleView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 50,
                IsOverlay = true
            });

            InitModuleEvent();
        }

        // 注册小局结算界面事件
        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenStageSettleView, onOpen);
            RegisterFunc(EventDefines.StageSettleToShop, onToShop);
        }

        // 移除小局结算界面事件
        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenStageSettleView, onOpen);
            UnRegisterFunc(EventDefines.StageSettleToShop, onToShop);
        }

        // 打开小局结算界面并透传展示数据
        private void onOpen(object[] args)
        {
            _currentData = args != null && args.Length > 0 ? args[0] as StageSettleData : null;
            grantStageReward(_currentData);
            GameApp.ViewManager.Open(ViewType.StageSettleView, _currentData);
            refreshStageSettleView();
        }

        // 小局结算界面已打开时直接刷新展示数据
        private void refreshStageSettleView()
        {
            IBaseView view = GameApp.ViewManager.GetView(ViewType.StageSettleView);
            if (view is StageSettleView stageSettleView)
                stageSettleView.Refresh(_currentData);
        }

        // 记录小局结算并把本局金币发放到玩家数据
        private static void grantStageReward(StageSettleData data)
        {
            if (data == null) return;

            bool isFirstRecord = LevelFlow.Instance.RecordStageResult(
                data.StageIndex,
                data.TurnCount,
                data.CurrentScore,
                data.Coin,
                data.MaxRoundScore,
                data.ResonanceCount,
                data.AngelBlessCount,
                data.DevilDealCount);

            if (isFirstRecord)
                PlayerDataManager.Instance.AddMoney(data.Coin);
        }

        // 右下角按钮：根据数据决定去商店还是最终结算
        private void onToShop(object[] args)
        {
            GameApp.ViewManager.Close(ViewType.StageSettleView);

            if (_currentData != null && _currentData.GoToFinalSummary)
                ApplyControllerFunc(ControllerType.Summary, EventDefines.OpenSummaryView);
            else
                ApplyControllerFunc(ControllerType.Shop, "OpenShopView");
        }
    }
}
