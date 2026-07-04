/*
* ┌──────────────────────────────────┐
* │  描    述: 总结算界面控制器，负责打开界面与处理跳转
* │  类    名: SummaryController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.Controller;
using MVC.View;

namespace Module.Summary
{
    // 总结算界面控制器，负责生成展示数据与按钮跳转
    public class SummaryController : BaseController
    {
        private SummaryModel _model;

        public SummaryController()
        {
            _model = new SummaryModel();
            SetModel(_model);

            GameApp.ViewManager.Register(ViewType.SummaryView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_SummaryView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 8
            });
            InitModuleEvent();
        }

        // 注册总结算界面事件
        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenSummaryView, onOpen);
            RegisterFunc("Summary.ViewAlmanac", onViewAlmanac);
            RegisterFunc("Summary.BackMenu", onBackMenu);
            RegisterFunc("Summary.CookAgain", onCookAgain);
        }

        // 打开总结算界面并刷新大局汇总数据
        private void onOpen(object[] args)
        {
            _model.LoadFromCurrentRun();
            GameApp.ViewManager.Open(ViewType.SummaryView);
            refreshSummaryView();
        }

        // SummaryView 打开后刷新显示数据
        public override void OpenView(IBaseView view)
        {
            if (view is SummaryView summaryView)
                summaryView.Refresh(_model);
        }

        // SummaryView 已打开时直接刷新显示数据
        private void refreshSummaryView()
        {
            IBaseView view = GameApp.ViewManager.GetView(ViewType.SummaryView);
            if (view is SummaryView summaryView)
                summaryView.Refresh(_model);
        }

        // 打开图鉴界面
        private void onViewAlmanac(object[] args)
        {
            ApplyControllerFunc(ControllerType.Almanac, EventDefines.OpenAlmanacView, ViewType.SummaryView);
        }

        // 返回主菜单
        private void onBackMenu(object[] args)
        {
            GameApp.ViewManager.Close(ViewType.SummaryView);
            ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView);
        }

        // 重新开始一局
        private void onCookAgain(object[] args)
        {
            GameApp.ViewManager.Close(ViewType.SummaryView);
            GameApp.ViewManager.Close(ViewType.MainMenuView);
            ApplyControllerFunc(ControllerType.SelectBox, EventDefines.OpenSelectBoxView);
        }
    }
}
