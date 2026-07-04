/*
* ┌──────────────────────────────────┐
* │  描    述: 21 点玩法控制器
* │  类    名: BlackjackController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using Module.Confirm;
using Module.Item;
using MVC;
using MVC.Controller;
using MVC.View;

namespace Module.Blackjack
{
    public class BlackjackController : BaseController
    {
        private BlackjackModel _model;

        public BlackjackController()
        {
            _model = new BlackjackModel();
            SetModel(_model);

            GameApp.ViewManager.Register((int)ViewType.BlackjackView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_BlackjackView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 20,
                IsOverlay = true
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenBlackjackView, onOpen);
            RegisterFunc(EventDefines.BlackjackDraw, onDraw);
            RegisterFunc(EventDefines.BlackjackRestart, onRestart);
            RegisterFunc(EventDefines.BlackjackReturn, onReturn);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenBlackjackView, onOpen);
            UnRegisterFunc(EventDefines.BlackjackDraw, onDraw);
            UnRegisterFunc(EventDefines.BlackjackRestart, onRestart);
            UnRegisterFunc(EventDefines.BlackjackReturn, onReturn);
        }

        private void onOpen(object[] args)
        {
            _model.Reset();
            GameApp.ViewManager.Open((int)ViewType.BlackjackView, args);
            refreshView();
        }

        private void onRestart(object[] args)
        {
            _model.Reset();
            refreshView();
        }

        private void onReturn(object[] args)
        {
            returnToCookView();
        }

        // 点道具抽牌：翻开下一张，刷新界面；达/超 21 触发结算
        private void onDraw(object[] args)
        {
            if (!_model.CanDraw) return;

            int index = _model.RevealNext();
            if (index < 0) return;

            refreshView();

            if (_model.IsBusted)
            {
                if (ItemPassiveManager.TryConsumeRabbitFootReroll() && _model.UndoLastReveal())
                {
                    refreshView();
                    ConfirmController.Show(new ConfirmModel
                    {
                        mode = ConfirmModel.Mode.ConfirmOnly,
                        title = "幸运兔脚",
                        message = "首次凑出 21 点，已为你重抽这张牌。\n请再选一张牌试试手气。",
                        confirmText = "继续"
                    });
                    return;
                }

                showBustResult();
            }
            else if (_model.AllRevealed)
            {
                showSafeResult();
            }
        }

        // 爆牌：弹确认框提示触发超级不好的 debuff（具体数值后续再接）
        private void showBustResult()
        {
            ConfirmController.Show(new ConfirmModel
            {
                mode = ConfirmModel.Mode.ConfirmOnly,
                title = "爆牌！",
                message = $"累计 {_model.TotalPoint} 点，达到/超过 21 点。\n触发超级不好的 debuff！",
                confirmText = "认栽",
                onConfirm = () =>
                {
                    // TODO: 接入具体 debuff 数值效果
                    returnToCookView();
                }
            });
        }

        // 安全翻完所有牌未爆牌
        private void showSafeResult()
        {
            ConfirmController.Show(new ConfirmModel
            {
                mode = ConfirmModel.Mode.ConfirmOnly,
                title = "安全过关",
                message = $"全部牌翻完，累计 {_model.TotalPoint} 点，未爆牌。",
                confirmText = "收手",
                onConfirm = () =>
                {
                    returnToCookView();
                }
            });
        }

        // 关闭 21 点并恢复烹饪界面
        private void returnToCookView()
        {
            ItemPassiveManager.EndMagicBoxSession();
            GameApp.ViewManager.Close((int)ViewType.BlackjackView);
            ApplyControllerFunc(ControllerType.Cook, EventDefines.OpenCookView);
        }

        private void refreshView()
        {
            var view = GameApp.ViewManager.GetView((int)ViewType.BlackjackView);
            if (view is BlackjackView blackjackView)
                blackjackView.Refresh(_model);
        }
    }
}
