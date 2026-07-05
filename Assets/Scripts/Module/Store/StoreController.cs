/*

* ┌──────────────────────────────────┐

* │  描    述: 商店子页面控制器（购箱后选卡 + 背包展示）

* │  类    名: StoreController.cs

* └──────────────────────────────────┘

*/



using Common.Defines;

using Module.Confirm;

using Module.Level;
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



        private void OnSetBagCount(object[] args)

        {

            if (args == null || args.Length == 0 || args[0] is not int count) return;

            _model.SetBagCount(count);

            _model.RefreshBag();

            RefreshView();

        }



        private void OnOpenStoreView(object[] args)

        {

            if (args != null && args.Length > 0 && args[0] is StoreOpenContext context)

                _model.SetupForBox(context);

            else

                _model.RefreshBuySlots();



            _model.RefreshBag();

            GameApp.ViewManager.Open((int)ViewType.StoreView, args);

            RefreshView();

        }



        private void OnReturn(object[] args)
        {
            returnToShop();
        }

        private void returnToShop()
        {
            _model.ClearBoxContext();
            GameApp.ViewManager.Close((int)ViewType.StoreView);
            ApplyControllerFunc(ControllerType.Shop, "OpenShopView", true);
        }

        private void OnBuy(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not StoreBuySlotData slot) return;
            if (slot.isPurchased) return;

            bool freePick = _model.CardsIncludedInBoxPrice && slot.price <= 0;
            if (freePick && _model.HasBoxPickCompleted()) return;



            if (!freePick && _model.Gold < slot.price)

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

                title = freePick ? "加入牌组" : "确认购买",

                message = buildBuyMessage(slot, freePick),

                confirmText = freePick ? "加入" : "购买",

                cancelText = "取消",

                onConfirm = () =>
                {
                    if (!freePick && !PlayerDataManager.Instance.SpendMoney(slot.price)) return;

                    PlayerDataManager.Instance.AddCard(slot.id);
                    LevelFlow.Instance.AddMaterial(slot.id);
                    slot.isPurchased = true;

                    if (freePick)
                    {
                        returnToShop();
                        return;
                    }

                    _model.RefreshBag();
                    RefreshView();
                }

            });

        }



        private string buildBuyMessage(StoreBuySlotData slot, bool freePick)

        {

            if (freePick)
            {
                return $"将「{slot.name}」加入牌组？\n（三选一，已含在材料箱价格内）";
            }



            return $"购买「{slot.name}」\n花费 {slot.price} 金币，剩余 {_model.Gold - slot.price} 金币。";

        }



        private void RefreshView()

        {

            var view = GameApp.ViewManager.GetView((int)ViewType.StoreView);

            if (view is StoreView storeView)

                storeView.Refresh(_model);

        }

    }

}


