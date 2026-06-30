/*
* ┌──────────────────────────────────┐
* │  描    述: 设置界面控制器
* │  类    名: SettingsController.cs
* └──────────────────────────────────┘
*/

using Common;
using Common.Defines;
using MVC;
using MVC.Controller;
using MVC.View;

namespace Module.Settings
{
    public class SettingsController : BaseController
    {
        public SettingsController()
        {
            GameApp.ViewManager.Register(ViewType.SettingsView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_SettingsView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 90
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenSettingsView, onOpen);
            RegisterFunc(EventDefines.SettingsClose, onClose);
            RegisterFunc(EventDefines.SettingsSetSfxOn, onSetSfxOn);
            RegisterFunc(EventDefines.SettingsSetSfxVolume, onSetSfxVolume);
            RegisterFunc(EventDefines.SettingsSetBgmOn, onSetBgmOn);
            RegisterFunc(EventDefines.SettingsSetBgmVolume, onSetBgmVolume);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenSettingsView, onOpen);
            UnRegisterFunc(EventDefines.SettingsClose, onClose);
            UnRegisterFunc(EventDefines.SettingsSetSfxOn, onSetSfxOn);
            UnRegisterFunc(EventDefines.SettingsSetSfxVolume, onSetSfxVolume);
            UnRegisterFunc(EventDefines.SettingsSetBgmOn, onSetBgmOn);
            UnRegisterFunc(EventDefines.SettingsSetBgmVolume, onSetBgmVolume);
        }

        private void onOpen(object[] args)
        {
            GameApp.ViewManager.Open(ViewType.SettingsView);
        }

        // ViewManager 打开 View 后回调，用当前设置刷新界面
        public override void OpenView(IBaseView view)
        {
            if (view is not SettingsView settingsView) return;

            settingsView.Refresh(
                GameApp.SoundManager?.EffectEnabled ?? true,
                GameApp.SoundManager?.EffectVolume ?? 1f,
                GameApp.SoundManager?.BgmEnabled ?? true,
                GameApp.SoundManager?.BgmVolume ?? 1f);
        }

        private void onClose(object[] args)
        {
            GameApp.ViewManager.Close(ViewType.SettingsView);
            ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView);
        }

        private void onSetSfxOn(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not bool on) return;

            if (GameApp.SoundManager != null)
                GameApp.SoundManager.EffectEnabled = on;
            SettingsKeys.SetBool(SettingsKeys.SfxOn, on);
            getView()?.SetSfxInteractable(on);
        }

        private void onSetSfxVolume(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not float v) return;

            if (GameApp.SoundManager != null)
                GameApp.SoundManager.EffectVolume = v;
            SettingsKeys.SetFloat(SettingsKeys.SfxVolume, v);
        }

        private void onSetBgmOn(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not bool on) return;

            if (GameApp.SoundManager != null)
                GameApp.SoundManager.BgmEnabled = on;
            SettingsKeys.SetBool(SettingsKeys.BgmOn, on);
            getView()?.SetBgmInteractable(on);
        }

        private void onSetBgmVolume(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not float v) return;

            if (GameApp.SoundManager != null)
                GameApp.SoundManager.BgmVolume = v;
            SettingsKeys.SetFloat(SettingsKeys.BgmVolume, v);
        }

        private SettingsView getView()
        {
            return GameApp.ViewManager.GetView<SettingsView>(ViewType.SettingsView);
        }
    }
}
