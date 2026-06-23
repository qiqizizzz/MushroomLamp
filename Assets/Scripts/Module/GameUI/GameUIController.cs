/*
* ┌──────────────────────────────────┐
* │  描    述: 游戏通用 UI 控制器，作为业务 UI 注册入口
* │  类    名: GameUIController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common;
using Common.Defines;
using MVC;
using MVC.Controller;
using MVC.Extensions;
using UnityEngine;

namespace Module.GameUI
{
    public class GameUIController : BaseController
    {
        public GameUIController()
        {
            GameApp.ViewManager.Register(ViewType.MainMenuView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_MainMenuView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 0
            });

            GameApp.ViewManager.Register(ViewType.SelectBoxView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_SelectBoxView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 0
            });

            InitModuleEvent();
            InitGlobalEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenMainMenuView, openMainMenuView);
            RegisterFunc(EventDefines.MainMenuStart, onMainMenuStart);
            RegisterFunc(EventDefines.MainMenuOpenSettings, onMainMenuOpenSettings);
            RegisterFunc(EventDefines.MainMenuExit, onMainMenuExit);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenMainMenuView, openMainMenuView);
            UnRegisterFunc(EventDefines.MainMenuStart, onMainMenuStart);
            UnRegisterFunc(EventDefines.MainMenuOpenSettings, onMainMenuOpenSettings);
            UnRegisterFunc(EventDefines.MainMenuExit, onMainMenuExit);
        }

        private void openMainMenuView(object[] args)
        {
            GameApp.ViewManager.Open(ViewType.MainMenuView, args);
        }

        private void onMainMenuStart(object[] args)
        {
            GameApp.ViewManager.Close(ViewType.MainMenuView);
            GameApp.ViewManager.Open(ViewType.SelectBoxView, args);
        }

        private void onMainMenuOpenSettings(object[] args)
        {
            QLog.Info($"[{nameof(GameUIController)}] 设置（占位）");
        }

        private void onMainMenuExit(object[] args)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
