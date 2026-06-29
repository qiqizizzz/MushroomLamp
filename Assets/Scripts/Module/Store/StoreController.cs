/*
* ┌──────────────────────────────────┐
* │  描    述: 商店子页面控制器（购买卡牌 + 背包展示）
* │  类    名: StoreController.cs
* └──────────────────────────────────┘
*/

using Common.Defines;
using Module.Confirm;
using Module.Player;
using MVC;
using MVC.Controller;
using MVC.View;

namespace Module.Store
{
    public class StoreController : BaseController
    {
        private StoreModel _model;

        public StoreController()
        {
            _model = new StoreModel();
            SetModel(_model);

            GameApp.ViewManager.Register((int)ViewType.StoreView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_StoreView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 20
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenStoreView, OnOpenStoreView);
            RegisterFunc(EventDefines.StoreReturn, OnReturn);
            RegisterFunc(EventDefines.StoreBuy, OnBuy);
            RegisterFunc(EventDefines.StoreSetBagCount, OnSetBagCount);
        }

        // 手动设置背包卡牌数量（args[0] 为 int，<=0 恢复读真实背包），随后刷新背包与界面
        private void OnSetBagCount(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not int count) return;
            _model.SetBagCount(count);
            _model.RefreshBag();
            RefreshView();
        }

        private void OnOpenStoreView(object[] args)
        {
            _model.RefreshAll();
            GameApp.ViewManager.Open((int)ViewType.StoreView, args);
            RefreshView();
        }

        private void OnReturn(object[] args)
        {
            GameApp.ViewManager.Close((int)ViewType.StoreView);
        }

        private void OnBuy(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not StoreBuySlotData slot) return;
            if (slot.isPurchased) return;

            if (_model.Gold < slot.price)
            {
                ConfirmController.Show(new ConfirmModel
                {
                    mode = ConfirmModel.Mode.ConfirmOnly,
                    title = "金币不足",
                    message = $"购买「{slot.name}」需要 {slot.price} 金币\n当前金币 {_model.Gold}，差 {slot.price - _model.Gold} 枚。",
                    confirmText = "知道了"
                });
                return;
            }

            ConfirmController.Show(new ConfirmModel
            {
                mode = ConfirmModel.Mode.ConfirmCancel,
                title = "确认购买",
                message = $"购买「{slot.name}」\n花费 {slot.price} 金币，剩余 {_model.Gold - slot.price} 金币。",
                confirmText = "购买",
                cancelText = "取消",
                onConfirm = () =>
                {
                    if (PlayerDataManager.Instance.SpendMoney(slot.price))
                    {
                        PlayerDataManager.Instance.AddCard(slot.id);
                        slot.isPurchased = true;
                        _model.RefreshBag();
                    }
                    RefreshView();
                }
            });
        }

        private void RefreshView()
        {
            var view = GameApp.ViewManager.GetView((int)ViewType.StoreView);
            if (view is StoreView storeView)
                storeView.Refresh(_model);
        }
    }
}
