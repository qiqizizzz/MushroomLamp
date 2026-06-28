/*
* ┌──────────────────────────────────┐
* │  描    述: 小局结算界面，覆盖在 CookView 之上展示结算信息
* │  类    名: StageSettleView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using Module.Cook;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.StageSettle
{
    // 小局结算界面，展示当前小局目标、得分与奖励
    public class StageSettleView : BaseView
    {
        private Button _btnToShop;
        private TextMeshProUGUI _txtButtonLabel;
        private TextMeshProUGUI _txtTitle;
        private TextMeshProUGUI _txtSubtitle;
        private TextMeshProUGUI _txtResult;
        private TextMeshProUGUI _txtStage;
        private TextMeshProUGUI _txtTurn;
        private TextMeshProUGUI _txtTarget;
        private TextMeshProUGUI _txtScore;
        private TextMeshProUGUI _txtCoin;
        private TextMeshProUGUI _txtNext;
        private TextMeshProUGUI _txtTip;
        private Image _imgResultBadge;

        // 初始化小局结算界面节点引用
        public override void InitUI()
        {
            _btnToShop = Find<Button>("Btn_ToShop");
            _txtButtonLabel = Find<TextMeshProUGUI>("Btn_ToShop/Txt_Label");
            _txtTitle = Find<TextMeshProUGUI>("Img_MagicCircle/InfoContainer/Txt_Title");
            _txtSubtitle = Find<TextMeshProUGUI>("Img_MagicCircle/InfoContainer/Txt_Subtitle");
            _txtResult = Find<TextMeshProUGUI>("Img_MagicCircle/InfoContainer/Badge_Result/Txt_Result");
            _txtStage = Find<TextMeshProUGUI>("Img_MagicCircle/InfoContainer/LeftStats/Row_Stage/Txt_Value");
            _txtTurn = Find<TextMeshProUGUI>("Img_MagicCircle/InfoContainer/LeftStats/Row_Turn/Txt_Value");
            _txtTarget = Find<TextMeshProUGUI>("Img_MagicCircle/InfoContainer/LeftStats/Row_Target/Txt_Value");
            _txtScore = Find<TextMeshProUGUI>("Img_MagicCircle/InfoContainer/RightStats/Row_Score/Txt_Value");
            _txtCoin = Find<TextMeshProUGUI>("Img_MagicCircle/InfoContainer/RightStats/Row_Coin/Txt_Value");
            _txtNext = Find<TextMeshProUGUI>("Img_MagicCircle/InfoContainer/RightStats/Row_Next/Txt_Value");
            _txtTip = Find<TextMeshProUGUI>("Img_MagicCircle/InfoContainer/Txt_Tip");
            _imgResultBadge = Find<Image>("Img_MagicCircle/InfoContainer/Badge_Result");
        }

        // 绑定小局结算界面按钮事件
        public override void InitData()
        {
            base.InitData();
            if (_btnToShop != null)
                _btnToShop.onClick.AddListener(() => ApplyFunc(EventDefines.StageSettleToShop));
        }

        // 打开小局结算界面并刷新展示数据
        public override void Open(params object[] args)
        {
            SetVisible(true);
            StageSettleData data = args != null && args.Length > 0 ? args[0] as StageSettleData : null;
            Refresh(data);
        }

        // 刷新小局结算文本
        public void Refresh(StageSettleData data)
        {
            if (data == null)
            {
                setText(_txtTitle, "小局结算");
                setText(_txtSubtitle, "暂无结算数据");
                setText(_txtResult, "等待数据");
                setText(_txtTip, "结算数据缺失，请检查小局结束流程");
                return;
            }

            string stageText = data.StageCount > 0 ? $"{data.StageIndex + 1}/{data.StageCount}" : "-";
            string resultText = data.IsTargetReached ? "目标达成" : "目标未达成";
            string nextText = data.GoToFinalSummary ? "最终结算" : "进入商店";

            setText(_txtTitle, "小局结算");
            setText(_txtSubtitle, $"{data.BoxName} · 小局 {stageText}");
            setText(_txtResult, resultText);
            setText(_txtStage, stageText);
            setText(_txtTurn, data.TurnCount.ToString());
            setText(_txtTarget, $"{data.TargetMin}~{data.TargetMax}");
            setText(_txtScore, CookRoundResultData.FormatScore(data.CurrentScore));
            setText(_txtCoin, data.Coin.ToString());
            setText(_txtNext, nextText);
            setText(_txtTip, data.GoToFinalSummary
                ? (data.IsTargetReached ? "已通关全部小局，进入最终结算" : "本小局未达到目标，进入最终结算")
                : "本小局目标已完成，进入商店准备下一小局");

            if (_imgResultBadge != null)
                _imgResultBadge.color = data.IsTargetReached
                    ? new Color(0.55f, 0.32f, 0.13f, 0.92f)
                    : new Color(0.50f, 0.16f, 0.12f, 0.92f);

            if (_txtButtonLabel != null)
                _txtButtonLabel.text = nextText;
        }

        private static void setText(TextMeshProUGUI text, string value)
        {
            if (text != null)
                text.text = value;
        }
    }
}
