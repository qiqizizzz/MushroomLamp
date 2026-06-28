/*
* ┌──────────────────────────────────┐
* │  描    述: 小局结算界面控制器
* │  类    名: StageSettleController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.Controller;

namespace Module.StageSettle
{
    // 小局结算界面控制器，负责打开结算界面与后续跳转
    public class StageSettleController : BaseController
    {
        public StageSettleController()
        {
            GameApp.ViewManager.Register(ViewType.StageSettleView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_StageSettleView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 50   // 覆盖在 CookView(10) 之上
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
            GameApp.ViewManager.Open(ViewType.StageSettleView, args);
        }

        // 进入商店：关结算界面 + 关 CookView + 开 ShopView
        private void onToShop(object[] args)
        {
            GameApp.ViewManager.Close(ViewType.StageSettleView);
            GameApp.ViewManager.Close(ViewType.CookView);
            ApplyControllerFunc(ControllerType.Shop, "OpenShopView");
        }
    }
}
