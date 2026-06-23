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
using Module.View;

namespace Module.Cook
{
    // 烹饪核心玩法控制器，负责局内流程与事件分发
    public class CookController : BaseController
    {
        public CookController()
        {
            SetModel(new CookModel());

            GameApp.ViewManager.Register(ViewType.CookView, new ViewInfo
            {
                PrefabName = string.Empty,
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
            RegisterFunc(EventDefines.AdvanceCookTurn, advanceCookTurn);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenCookView, openCookView);
            UnRegisterFunc(EventDefines.StartCookRun, startCookRun);
            UnRegisterFunc(EventDefines.AdvanceCookTurn, advanceCookTurn);
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
            cookModel.StartRun();

            GameApp.ViewManager.Open(ViewType.CookView, args);
            refreshCookView();

            QLog.Info($"[{nameof(CookController)}] 开始烹饪玩法");
        }

        // 推进烹饪回合
        private void advanceCookTurn(object[] args)
        {
            CookModel cookModel = GetCookModel();
            bool canContinue = cookModel.AdvanceTurn();
            refreshCookView();

            if (!canContinue)
                QLog.Info($"[{nameof(CookController)}] 当天烹饪结束，分数：{cookModel.GetScoreText()}");
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
