/*
* ┌──────────────────────────────────┐
* │  描    述: 21 点玩法控制器
* │           点道具抽牌；达到/超过 21 点弹确认框并提示 debuff
* │  类    名: BlackjackController.cs
* └──────────────────────────────────┘
*/

using Common.Defines;
using Module.Confirm;
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
                Sorting_Order = 20
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenBlackjackView, OnOpen);
            RegisterFunc(EventDefines.BlackjackDraw, OnDraw);
            RegisterFunc(EventDefines.BlackjackRestart, OnRestart);
            RegisterFunc(EventDefines.BlackjackReturn, OnReturn);
        }

        private void OnOpen(object[] args)
        {
            _model.Reset();
            GameApp.ViewManager.Open((int)ViewType.BlackjackView, args);
            RefreshView();
        }

        private void OnRestart(object[] args)
        {
            _model.Reset();
            RefreshView();
        }

        private void OnReturn(object[] args)
        {
            GameApp.ViewManager.Close((int)ViewType.BlackjackView);
        }

        // 点道具抽牌：翻开下一张，刷新界面；达/超 21 触发结算
        private void OnDraw(object[] args)
        {
            if (!_model.CanDraw) return;

            int index = _model.RevealNext();
            if (index < 0) return;

            RefreshView();

            if (_model.IsBusted)
                ShowBustResult();
            else if (_model.AllRevealed)
                ShowSafeResult();
        }

        // 爆牌：弹确认框提示触发超级不好的 debuff（具体数值后续再接）
        private void ShowBustResult()
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
                    _model.Reset();
                    RefreshView();
                }
            });
        }

        // 安全翻完 4 张未爆牌
        private void ShowSafeResult()
        {
            ConfirmController.Show(new ConfirmModel
            {
                mode = ConfirmModel.Mode.ConfirmOnly,
                title = "安全过关",
                message = $"四张牌翻完，累计 {_model.TotalPoint} 点，未爆牌。",
                confirmText = "再来一局",
                onConfirm = () =>
                {
                    _model.Reset();
                    RefreshView();
                }
            });
        }

        private void RefreshView()
        {
            var view = GameApp.ViewManager.GetView((int)ViewType.BlackjackView);
            if (view is BlackjackView blackjackView)
                blackjackView.Refresh(_model);
        }
    }
}
