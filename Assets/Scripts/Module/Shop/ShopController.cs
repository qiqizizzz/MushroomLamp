/*
* ┌──────────────────────────────────┐
* │  描    述: 商店控制器，负责购买、刷新、回收入口与小局推进
* │  类    名: ShopController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common;
using Common.Defines;
using Module.Confirm;
using Module.Item;
using Module.Level;
using Module.Player;
using Module.Store;
using MVC;
using MVC.Controller;
using MVC.View;

namespace Module.Shop
{
    public class ShopController : BaseController
    {
        private const string ShopRecycleDone = "Shop.RecycleDone";

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
            RegisterFunc(ShopRecycleDone, OnRecycleDone);
            RegisterFunc("Shop.Continue", OnContinue);
            RegisterFunc("Shop.BuyItem",  OnBuyItem);
        }

        private void OnOpenShopView(object[] args)
        {
            // 从 Store 返回时仅重新打开界面，不刷新货架，保留已购箱子状态
            bool reopenOnly = args != null && args.Length > 0 && args[0] is true;
            if (!reopenOnly)
            {
                _shopModel.ResetRecycleState();
                _shopModel.Refresh();
            }

            GameApp.ViewManager.Open((int)ViewType.ShopView, args);
            RefreshView();
        }

        private const int RefreshCost = 5;   // 刷新货架费用（固定）

        private void OnRefresh(object[] args)
        {
            int gold = PlayerDataManager.Instance.Money;
            if (gold < RefreshCost)
            {
                ConfirmController.Show(new ConfirmModel
                {
                    mode = ConfirmModel.Mode.ConfirmOnly,
                    title = "金币不足",
                    message = $"刷新货架需要 {RefreshCost} 金币\n\n当前金币 {gold}，差 {RefreshCost - gold} 枚。",
                    confirmText = "知道了"
                });
                return;
            }

            ConfirmController.Show(new ConfirmModel
            {
                mode = ConfirmModel.Mode.ConfirmCancel,
                title = "刷新货架",
                message = $"花费 {RefreshCost} 金币重新随机货架上的材料箱与道具？\n\n当前金币 {gold}，刷新后剩余 {gold - RefreshCost} 金币。",
                confirmText = "刷新",
                cancelText = "取消",
                onConfirm = () =>
                {
                    if (!PlayerDataManager.Instance.SpendMoney(RefreshCost)) return;
                    _shopModel.Refresh();
                    RefreshView();
                }
            });
        }

        private void OnRecycle(object[] args)
        {
            if (!_shopModel.CanRecycle)
            {
                ConfirmController.Show(new ConfirmModel
                {
                    mode = ConfirmModel.Mode.ConfirmOnly,
                    title = "无法回收",
                    message = "本次商店已经回收过一次了。\n\n继续前进后，下次进入商店会刷新回收机会。",
                    confirmText = "知道了"
                });
                return;
            }

            ApplyControllerFunc(ControllerType.Recycle, EventDefines.OpenRecycleView);
        }

        // 标记本次商店已完成回收
        private void OnRecycleDone(object[] args)
        {
            _shopModel.MarkRecycled();
            RefreshView();
        }

        // 继续：推进到下一小局；若已是最后小局则进入最终结算
        private void OnContinue(object[] args)
        {
            GameApp.ViewManager.Close((int)ViewType.ShopView);

            if (LevelFlow.Instance.AdvanceStage())
            {
                ApplyControllerFunc(ControllerType.Cook, EventDefines.StartCookRun, LevelFlow.Instance.BuildStartData());
            }
            else
            {
                ApplyControllerFunc(ControllerType.Summary, EventDefines.OpenSummaryView, true);
            }
        }

        private void OnBuyItem(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not ShopSlotData slotData) return;
            if (slotData.isPurchased) return;

            if (_shopModel.Gold < slotData.price)
            {
                ConfirmController.Show(new ConfirmModel
                {
                    mode = ConfirmModel.Mode.ConfirmOnly,
                    title = "金币不足",
                    message = $"购买\"{slotData.name}\"需要{slotData.price}金币\n\n当前金币{_shopModel.Gold}，差{slotData.price - _shopModel.Gold}枚。",
                    confirmText = "知道了"
                });
                return;
            }

            ConfirmController.Show(new ConfirmModel
            {
                mode = ConfirmModel.Mode.ConfirmCancel,
                title = "确认购买",
                message = buildBuyMessage(slotData),
                confirmText = "购买",
                cancelText = "取消",
                onConfirm = () =>
                {
                    if (!PlayerDataManager.Instance.SpendMoney(slotData.price)) return;

                    if (!slotData.isBox && !slotData.isCard)
                    {
                        if (!PlayerDataManager.Instance.AddItem(slotData.id))
                        {
                            PlayerDataManager.Instance.AddMoney(slotData.price);
                            ConfirmController.Show(new ConfirmModel
                            {
                                mode = ConfirmModel.Mode.ConfirmOnly,
                                title = "无法购买",
                                message = $"你已经拥有\"{slotData.name}\"，该道具不可重复购买。",
                                confirmText = "知道了"
                            });
                            return;
                        }
                    }

                    slotData.isPurchased = true;
                    RefreshView();

                    if (slotData.isBox)
                        openStoreAfterBoxPurchase(slotData);
                    else if (slotData.isCard)
                    {
                        PlayerDataManager.Instance.AddCard(slotData.id);
                        LevelFlow.Instance.AddMaterial(slotData.id);
                    }
                }
            });
        }

        private static string buildBuyMessage(ShopSlotData slotData)
        {
            int remain = PlayerDataManager.Instance.Money - slotData.price;
            if (slotData.isBox)
            {
                string boxDesc = string.IsNullOrWhiteSpace(slotData.description)
                    ? string.Empty
                    : $"\n\n{slotData.description}";
                return $"购买\"{slotData.name}\"材料箱{boxDesc}\n\n花费{slotData.price}金币，剩余{remain}金币。";
            }

            return $"购买\"{slotData.name}\"\n\n{slotData.description}\n\n花费{slotData.price}金币，剩余{remain}金币。";
        }

        private void openStoreAfterBoxPurchase(ShopSlotData slotData)
        {
            var context = new StoreOpenContext
            {
                boxId = slotData.id,
                boxName = slotData.name,
                cardsIncludedInBoxPrice = true
            };
            ApplyControllerFunc(ControllerType.Store, EventDefines.OpenStoreView, context);
        }

        private void RefreshView()
        {
            var view = GameApp.ViewManager.GetView((int)ViewType.ShopView);
            if (view is ShopView shopView)
                shopView.Refresh(_shopModel);
        }
    }
}
