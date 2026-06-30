using Common.Defines;
using Module.Confirm;
using Module.Level;
using Module.Player;
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
            _shopModel.Refresh();
            GameApp.ViewManager.Open((int)ViewType.ShopView, args);
            RefreshView();
        }

        private const int RefreshCost = 5;   // 刷新货架费用（固定）

        private void OnRefresh(object[] args)
        {
            if (PlayerDataManager.Instance.Money < RefreshCost)
            {
                ConfirmController.Show(new ConfirmModel
                {
                    mode = ConfirmModel.Mode.ConfirmOnly,
                    title = "金币不足",
                    message = $"刷新货架需要 {RefreshCost} 金币\n当前金币 {PlayerDataManager.Instance.Money}。",
                    confirmText = "知道了"
                });
                return;
            }

            PlayerDataManager.Instance.SpendMoney(RefreshCost);
            _shopModel.Refresh();
            RefreshView();
        }

        // 打开回收界面
        private void OnRecycle(object[] args)
        {
            ApplyControllerFunc(ControllerType.Recycle, EventDefines.OpenRecycleView);
        }

        // 继续：推进到下一小局；若已是最后小局则进入最终结算
        private void OnContinue(object[] args)
        {
            GameApp.ViewManager.Close((int)ViewType.ShopView);

            if (LevelFlow.Instance.AdvanceStage())
            {
                // 还有下一小局 → 用新小局参数重开 Cook
                ApplyControllerFunc(ControllerType.Cook, EventDefines.StartCookRun, LevelFlow.Instance.BuildStartData());
            }
            else
            {
                // 已是最后小局 → 进入最终结算
                ApplyControllerFunc(ControllerType.Summary, EventDefines.OpenSummaryView);
            }
        }

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
                    if (PlayerDataManager.Instance.SpendMoney(slotData.price))
                    {
                        if (slotData.isCard)
                            PlayerDataManager.Instance.AddCard(slotData.id);
                        slotData.isPurchased = true;
                    }
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

