/*
 * ┌──────────────────────────────────┐
 * │  描    述: 主界面                      
 * │  类    名: MainMenuView.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Common;
using MVC.View;
using UnityEngine.UI;

namespace Module.View
{
    public class MainMenuView : BaseView
    {
        private Button _startButton;

        public override void InitUI()
        {
            _startButton = Find<Button>("Btn_Start");
        }

        public override void InitData()
        {
            base.InitData();
            _startButton.onClick.AddListener(onStartClick);
        }

        protected override void OnDestroy()
        {
            if (_startButton != null)
                _startButton.onClick.RemoveListener(onStartClick);

            base.OnDestroy();
        }

        // 处理开始按钮点击
        private void onStartClick()
        {
            QLog.Info($"[{nameof(MainMenuView)}] 开始按钮点击");
        }
    }
}