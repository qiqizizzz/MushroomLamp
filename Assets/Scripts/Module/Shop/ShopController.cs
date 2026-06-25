using Common.Defines;
using MVC;
using MVC.Controller;
using MVC.View;

namespace Module.Shop
{
    public class ShopController : BaseController
    {
        private ShopModel _shopModel;

        public ShopController()
        {
            _shopModel = new ShopModel();
            SetModel(_shopModel);

            GameApp.ViewManager.Register((int)ViewType.ShopView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_ShopView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 10
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc("OpenShopView", OnOpenShopView);
            RegisterFunc("Shop.Refresh", OnRefresh);
            RegisterFunc("Shop.Recycle", OnRecycle);
            RegisterFunc("Shop.Continue", OnContinue);
        }

        private void OnOpenShopView(object[] args)
        {
            int? gold = null;
            if (args != null && args.Length > 0 && args[0] is int value)
                gold = value;

            _shopModel.Refresh(gold);
            GameApp.ViewManager.Open((int)ViewType.ShopView, args);
            RefreshView();
        }

        private void OnRefresh(object[] args)
        {
            _shopModel.Refresh();
            RefreshView();
        }

        private void OnRecycle(object[] args) { }
        private void OnContinue(object[] args) { }

        private void RefreshView()
        {
            var view = GameApp.ViewManager.GetView((int)ViewType.ShopView);
            if (view is ShopView shopView)
                shopView.Refresh(_shopModel);
        }
    }
}
