/*
* ┌──────────────────────────────────┐
* │  描    述: 二次确认弹窗视图
* │  类    名: ConfirmView.cs
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Confirm
{
    public class ConfirmView : BaseView
    {
        private GameObject _goTitle;
        private TextMeshProUGUI _txtTitle;
        private TextMeshProUGUI _txtMessage;
        private Button _btnConfirm;
        private TextMeshProUGUI _txtConfirm;
        private Button _btnCancel;
        private TextMeshProUGUI _txtCancel;

        public override void InitUI()
        {
            _goTitle    = Find("Window/Text_Title");
            _txtTitle   = Find<TextMeshProUGUI>("Window/Text_Title");
            _txtMessage = Find<TextMeshProUGUI>("Window/Text_Message");
            _btnConfirm = Find<Button>("Window/ButtonGroup/Btn_Confirm");
            _txtConfirm = Find<TextMeshProUGUI>("Window/ButtonGroup/Btn_Confirm/Text");
            _btnCancel  = Find<Button>("Window/ButtonGroup/Btn_Cancel");
            _txtCancel  = Find<TextMeshProUGUI>("Window/ButtonGroup/Btn_Cancel/Text");
        }

        public override void InitData()
        {
            base.InitData();
            _btnConfirm?.onClick.AddListener(onConfirmClick);
            _btnCancel?.onClick.AddListener(onCancelClick);
        }

        public override void Open(params object[] args)
        {
            SetVisible(true);
            if (args == null || args.Length == 0 || args[0] is not ConfirmModel model) return;
            refresh(model);
        }

        private void refresh(ConfirmModel model)
        {
            bool hasTitle = !string.IsNullOrEmpty(model.title);
            _goTitle?.SetActive(hasTitle);
            if (hasTitle && _txtTitle != null)
                _txtTitle.text = model.title;

            if (_txtMessage != null)
                _txtMessage.text = model.message ?? string.Empty;

            if (_txtConfirm != null)
                _txtConfirm.text = model.confirmText;

            bool showCancel = model.mode == ConfirmModel.Mode.ConfirmCancel;
            _btnCancel?.gameObject.SetActive(showCancel);
            if (showCancel && _txtCancel != null)
                _txtCancel.text = model.cancelText;
        }

        private void onConfirmClick() => ApplyFunc(EventDefines.ConfirmViewConfirm);
        private void onCancelClick()  => ApplyFunc(EventDefines.ConfirmViewCancel);
    }
}
