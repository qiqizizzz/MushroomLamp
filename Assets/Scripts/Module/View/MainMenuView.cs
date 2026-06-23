/*
 * ┌──────────────────────────────────┐
 * │  描    述: 主菜单（开始）界面
 * │  类    名: MainMenuView.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Common.Defines;
using MVC.View;
using UnityEngine.UI;

namespace Module.View
{
    public class MainMenuView : BaseView
    {
        private Button _btnStart;
        private Button _btnSettings;
        private Button _btnGallery;
        private Button _btnExit;

        public override void InitUI()
        {
            _btnStart = Find<Button>("ButtonGroup/Btn_Start");
            _btnSettings = Find<Button>("ButtonGroup/Btn_Settings");
            _btnGallery = Find<Button>("ButtonGroup/Btn_Gallery");
            _btnExit = Find<Button>("ButtonGroup/Btn_Exit");
        }

        public override void InitData()
        {
            base.InitData();

            if (_btnStart != null)
                _btnStart.onClick.AddListener(onStartClick);

            if (_btnSettings != null)
                _btnSettings.onClick.AddListener(onSettingsClick);

            if (_btnExit != null)
                _btnExit.onClick.AddListener(onExitClick);
        }

        protected override void OnDestroy()
        {
            if (_btnStart != null)
                _btnStart.onClick.RemoveListener(onStartClick);

            if (_btnSettings != null)
                _btnSettings.onClick.RemoveListener(onSettingsClick);

            if (_btnExit != null)
                _btnExit.onClick.RemoveListener(onExitClick);

            base.OnDestroy();
        }

        private void onStartClick()
        {
            ApplyFunc(EventDefines.MainMenuStart);
        }

        private void onSettingsClick()
        {
            ApplyFunc(EventDefines.MainMenuOpenSettings);
        }

        private void onExitClick()
        {
            ApplyFunc(EventDefines.MainMenuExit);
        }
    }
}
