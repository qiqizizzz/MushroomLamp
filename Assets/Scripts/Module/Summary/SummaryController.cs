using Common.Defines;
using MVC;
using MVC.Controller;
using MVC.View;

namespace Module.Summary
{
    public class SummaryController : BaseController
    {
        private SummaryModel _model;

        public SummaryController()
        {
            GameApp.ViewManager.Register(ViewType.SummaryView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_SummaryView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 8
            });
            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenSummaryView, onOpen);
            RegisterFunc("Summary.ViewAlmanac", onViewAlmanac);
            RegisterFunc("Summary.BackMenu", onBackMenu);
            RegisterFunc("Summary.CookAgain", onCookAgain);
        }

        private void onOpen(object[] args)
        {
            _model = new SummaryModel();
            _model.Randomize();
            GameApp.ViewManager.Open(ViewType.SummaryView);
        }

        public override void OpenView(IBaseView view)
        {
            if (view is not SummaryView summaryView) return;
            if (_model == null) { _model = new SummaryModel(); _model.Randomize(); }
            summaryView.Refresh(_model);
        }

        private void onViewAlmanac(object[] args)
        {
            ApplyControllerFunc(ControllerType.Almanac, EventDefines.OpenAlmanacView, ViewType.SummaryView);
        }

        private void onBackMenu(object[] args)
        {
            GameApp.ViewManager.Close(ViewType.SummaryView);
            ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView);
        }

        private void onCookAgain(object[] args)
        {
            GameApp.ViewManager.Close(ViewType.SummaryView);
            GameApp.ViewManager.Close(ViewType.MainMenuView);
            ApplyControllerFunc(ControllerType.SelectBox, EventDefines.OpenSelectBoxView);
        }
    }
}
