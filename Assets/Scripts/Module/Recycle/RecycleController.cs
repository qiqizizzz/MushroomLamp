/*
* ┌──────────────────────────────────┐
* │  描    述: 回收界面控制器，负责打开界面与结算回收金币
* │  类    名: RecycleController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using Module.Player;
using MVC;
using MVC.Controller;
using MVC.View;

namespace Module.Recycle
{
    // 回收界面控制器，负责打开界面与结算回收金币
    public class RecycleController : BaseController
    {
        private readonly RecycleModel _model;

        public RecycleController()
        {
            _model = new RecycleModel();
            SetModel(_model);

            GameApp.ViewManager.Register((int)ViewType.RecycleView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_RecycleView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 30
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenRecycleView, openRecycleView);
            RegisterFunc(EventDefines.RecycleReturn, returnToShop);
            RegisterFunc(EventDefines.RecycleSellSelected, sellSelected);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenRecycleView, openRecycleView);
            UnRegisterFunc(EventDefines.RecycleReturn, returnToShop);
            UnRegisterFunc(EventDefines.RecycleSellSelected, sellSelected);
        }

        // 打开回收界面并刷新本次随机材料
        private void openRecycleView(object[] args)
        {
            _model.RefreshAll();
            GameApp.ViewManager.Open((int)ViewType.RecycleView, args);
            refreshView();
        }

        // 返回商店界面
        private void returnToShop(object[] args)
        {
            GameApp.ViewManager.Close((int)ViewType.RecycleView);
        }

        // 卖出当前选中的材料并关闭回收界面
        private void sellSelected(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not RecycleOfferData offer) return;
            if (!_model.SellOffer(offer, out int gold)) return;

            PlayerDataManager.Instance.AddMoney(gold);
            GameApp.ViewManager.Close((int)ViewType.RecycleView);
            ApplyControllerFunc(ControllerType.Shop, "Shop.RecycleDone");
        }

        private void refreshView()
        {
            var view = GameApp.ViewManager.GetView((int)ViewType.RecycleView);
            if (view is RecycleView recycleView)
                recycleView.Refresh(_model);
        }
    }
}
