/*
* ┌──────────────────────────────────┐
* │  描    述: 二次确认弹窗数据模型
* │  类    名: ConfirmModel.cs
* └──────────────────────────────────┘
*/

using System;

namespace Module.Confirm
{
    public class ConfirmModel
    {
        public enum Mode { ConfirmOnly, ConfirmCancel }

        public Mode mode = Mode.ConfirmCancel;
        public string title;
        public string message;
        public string confirmText = "确认";
        public string cancelText = "取消";
        public Action onConfirm;
        public Action onCancel;
    }
}
