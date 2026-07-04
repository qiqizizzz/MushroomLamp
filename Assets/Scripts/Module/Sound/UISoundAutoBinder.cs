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
        public static void Bind(IBaseView view)
        {
            if (view is not MonoBehaviour viewBehaviour) return;

            string viewName = viewBehaviour.GetType().Name;
            SoundViewBindingJsonData viewBinding = SoundConfigLoader.GetViewBinding(viewName);

            if (viewBinding == null || !viewBinding.disableAutoButtonSound)
                bindButtons(viewBehaviour, viewBinding);

            if (viewBinding?.bgms != null && viewBinding.bgms.Length > 0)
                GameApp.SoundManager?.PlayViewBgms(viewBinding.bgms);
        }

        private static void bindButtons(MonoBehaviour viewBehaviour, SoundViewBindingJsonData viewBinding)
        {
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

                string click = resolveButtonSound(
                    buttonBinding?.click,
                    buttonBinding?.muteClick ?? false,
                    viewClick);
                string hover = resolveButtonSound(
                    buttonBinding?.hover,
                    buttonBinding?.muteHover ?? false,
                    viewHover);

                if (string.IsNullOrWhiteSpace(click) && string.IsNullOrWhiteSpace(hover)) continue;

                UIButtonSoundHandler handler = button.GetComponent<UIButtonSoundHandler>();
                if (handler == null)
                    handler = button.gameObject.AddComponent<UIButtonSoundHandler>();

                handler.Configure(click, hover);
            }
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

        // buttonField 为 null 表示 JSON 未配置，继承 View/全局默认；显式 "" 或 mute 表示静音
        private static string resolveButtonSound(string buttonField, bool mute, string fallback)
        {
            if (mute)
                return string.Empty;

            if (buttonField != null)
                return string.IsNullOrWhiteSpace(buttonField) ? string.Empty : buttonField;

            return fallback;
        }
    }
}
