/*
* ┌──────────────────────────────────┐
* │  描    述: 设置界面，音效/背景音乐 开关与音量
* │  类    名: SettingsView.cs
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC;
using MVC.View;
using TMPro;
using UnityEngine.UI;

namespace Module.Settings
{
    public class SettingsView : BaseView
    {
        private Toggle _toggleSfx;
        private Slider _sliderSfx;
        private Toggle _toggleBgm;
        private Slider _sliderBgm;
        private Button _btnClose;
        private Button _btnHome;

        // 抑制初始化赋值时触发回调
        private bool _suppress;

        public override void InitUI()
        {
            _toggleSfx = Find<Toggle>("Panel/Row_Sfx/Toggle_Sfx");
            _sliderSfx = Find<Slider>("Panel/Row_Sfx/Slider_Sfx");
            _toggleBgm = Find<Toggle>("Panel/Row_Bgm/Toggle_Bgm");
            _sliderBgm = Find<Slider>("Panel/Row_Bgm/Slider_Bgm");
            _btnClose = Find<Button>("Panel/Btn_Close");
            _btnHome = Find<Button>("Panel/Btn_home");
        }

        public override void InitData()
        {
            base.InitData();

            if (_toggleSfx != null)
                _toggleSfx.onValueChanged.AddListener(v => { if (!_suppress) ApplyFunc(EventDefines.SettingsSetSfxOn, v); });
            if (_sliderSfx != null)
                _sliderSfx.onValueChanged.AddListener(v => { if (!_suppress) ApplyFunc(EventDefines.SettingsSetSfxVolume, v); });
            if (_toggleBgm != null)
                _toggleBgm.onValueChanged.AddListener(v => { if (!_suppress) ApplyFunc(EventDefines.SettingsSetBgmOn, v); });
            if (_sliderBgm != null)
                _sliderBgm.onValueChanged.AddListener(v => { if (!_suppress) ApplyFunc(EventDefines.SettingsSetBgmVolume, v); });
            if (_btnClose != null)
                _btnClose.onClick.AddListener(() => ApplyFunc(EventDefines.SettingsClose));
            if(_btnHome != null)
                _btnHome.onClick.AddListener(() =>
                {
                    ApplyFunc(EventDefines.SettingsClose);
                    ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView);
                });
            
        }

        public override void Open(params object[] args)
        {
            SetVisible(true);
        }

        // 用当前设置值刷新控件（不触发回调）
        public void Refresh(bool sfxOn, float sfxVolume, bool bgmOn, float bgmVolume)
        {
            _suppress = true;
            if (_toggleSfx != null) _toggleSfx.isOn = sfxOn;
            if (_sliderSfx != null) _sliderSfx.value = sfxVolume;
            if (_toggleBgm != null) _toggleBgm.isOn = bgmOn;
            if (_sliderBgm != null) _sliderBgm.value = bgmVolume;
            _suppress = false;

            applyInteractable(sfxOn, bgmOn);
        }

        // 开关关闭时，对应音量条置灰
        public void SetSfxInteractable(bool on)
        {
            if (_sliderSfx != null) _sliderSfx.interactable = on;
        }

        public void SetBgmInteractable(bool on)
        {
            if (_sliderBgm != null) _sliderBgm.interactable = on;
        }

        private void applyInteractable(bool sfxOn, bool bgmOn)
        {
            SetSfxInteractable(sfxOn);
            SetBgmInteractable(bgmOn);
        }
    }
}
