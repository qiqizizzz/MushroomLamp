using Common.Defines;
using Module.Confirm;
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
            RegisterFunc("Shop.Refresh",  OnRefresh);
            RegisterFunc("Shop.Recycle",  OnRecycle);
            RegisterFunc("Shop.Continue", OnContinue);
            RegisterFunc("Shop.BuyItem",  OnBuyItem);
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

        private void OnBuyItem(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not ShopSlotData slotData) return;

            if (_shopModel.Gold < slotData.price)
            {
                ConfirmController.Show(new ConfirmModel
                {
                    mode = ConfirmModel.Mode.ConfirmOnly,
                    title = "金币不足",
                    message = $"购买「{slotData.name}」需要 {slotData.price} 金币\n当前金币 {_shopModel.Gold}，差 {slotData.price - _shopModel.Gold} 枚。",
                    confirmText = "知道了"
                });
                return;
            }

            ConfirmController.Show(new ConfirmModel
            {
                mode = ConfirmModel.Mode.ConfirmCancel,
                title = "确认购买",
                message = $"购买「{slotData.name}」\n花费 {slotData.price} 金币，剩余 {_shopModel.Gold - slotData.price} 金币。",
                confirmText = "购买",
                cancelText = "取消",
                onConfirm = () =>
                {
                    _shopModel.SetGold(_shopModel.Gold - slotData.price);
                    slotData.isPurchased = true;
                    RefreshView();
                }
            });
        }

        private void RefreshView()
        {
            var view = GameApp.ViewManager.GetView((int)ViewType.ShopView);
            if (view is ShopView shopView)
                shopView.Refresh(_shopModel);
        }
    }
}

