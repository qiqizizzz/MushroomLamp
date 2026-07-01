/*
* ┌──────────────────────────────────┐
* │  描    述: UI 声音自动绑定器，负责按配置为 View 中按钮追加音效
* │  类    名: UISoundAutoBinder.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Sound
{
    public static class UISoundAutoBinder
    {
        // 为 View 下所有按钮追加点击和悬停音效
        public static void Bind(IBaseView view)
        {
            if (view is not MonoBehaviour viewBehaviour) return;

            string viewName = viewBehaviour.GetType().Name;
            SoundViewBindingJsonData viewBinding = SoundConfigLoader.GetViewBinding(viewName);
            if (viewBinding != null && viewBinding.disableAutoButtonSound) return;

            string viewClick = string.IsNullOrWhiteSpace(viewBinding?.buttonClick)
                ? SoundConfigLoader.GetDefaultButtonClick()
                : viewBinding.buttonClick;
            string viewHover = string.IsNullOrWhiteSpace(viewBinding?.buttonHover)
                ? SoundConfigLoader.GetDefaultButtonHover()
                : viewBinding.buttonHover;

            Button[] buttons = viewBehaviour.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button == null) continue;

                string buttonPath = getRelativePath(viewBehaviour.transform, button.transform);
                SoundButtonBindingJsonData buttonBinding = SoundConfigLoader.FindButtonBinding(viewBinding, buttonPath);

                string click = buttonBinding != null && !string.IsNullOrWhiteSpace(buttonBinding.click)
                    ? buttonBinding.click
                    : viewClick;
                string hover = buttonBinding != null && !string.IsNullOrWhiteSpace(buttonBinding.hover)
                    ? buttonBinding.hover
                    : viewHover;

                if (buttonBinding != null && buttonBinding.muteClick)
                    click = string.Empty;
                if (buttonBinding != null && buttonBinding.muteHover)
                    hover = string.Empty;

                if (string.IsNullOrWhiteSpace(click) && string.IsNullOrWhiteSpace(hover)) continue;

                UIButtonSoundHandler handler = button.GetComponent<UIButtonSoundHandler>();
                if (handler == null)
                    handler = button.gameObject.AddComponent<UIButtonSoundHandler>();

                handler.Configure(click, hover);
            }

            if (!string.IsNullOrWhiteSpace(viewBinding?.bgm))
                GameApp.SoundManager?.PlayBGM(viewBinding.bgm);
        }

        private static string getRelativePath(Transform root, Transform target)
        {
            if (root == null || target == null || target == root) return string.Empty;

            string path = target.name;
            Transform current = target.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
