/*
* ┌──────────────────────────────────┐
* │  描    述: 小局结算界面，覆盖在 CookView 之上展示结算信息
* │  类    名: StageSettleView.cs
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC.View;
using UnityEngine.UI;

namespace Module.StageSettle
{
    public class StageSettleView : BaseView
    {
        private Button _btnToShop;

        public override void InitUI()
        {
            _btnToShop = Find<Button>("Btn_ToShop");
        }

        public override void InitData()
        {
            base.InitData();
            if (_btnToShop != null)
                _btnToShop.onClick.AddListener(() => ApplyFunc(EventDefines.StageSettleToShop));
        }

        public override void Open(params object[] args)
        {
            SetVisible(true);
        }
    }
}
