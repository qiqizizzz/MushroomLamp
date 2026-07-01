/*
* ┌──────────────────────────────────┐
* │  描    述: UI 按钮音效处理器，负责点击与悬停音效播放
* │  类    名: UIButtonSoundHandler.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sound
{
    // UI 按钮音效处理器，负责点击与悬停音效播放
    public class UIButtonSoundHandler : MonoBehaviour, IPointerEnterHandler
    {
        private Button _button;
        private UnityAction _clickAction;
        private string _clickSoundId;
        private string _hoverSoundId;

        // 配置按钮点击和悬停音效
        public void Configure(string clickSoundId, string hoverSoundId)
        {
            ensureButton();
            removeClickListener();

            _clickSoundId = clickSoundId;
            _hoverSoundId = hoverSoundId;

            if (_button == null || string.IsNullOrWhiteSpace(_clickSoundId)) return;

            _clickAction = playClickSound;
            _button.onClick.AddListener(_clickAction);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrWhiteSpace(_hoverSoundId)) return;
            GameApp.SoundManager?.PlayEffect(_hoverSoundId, Vector3.zero);
        }

        private void OnDestroy()
        {
            removeClickListener();
        }

        private void playClickSound()
        {
            if (string.IsNullOrWhiteSpace(_clickSoundId)) return;
            GameApp.SoundManager?.PlayEffect(_clickSoundId, Vector3.zero);
        }

        private void ensureButton()
        {
            if (_button == null)
                _button = GetComponent<Button>();
        }

        private void removeClickListener()
        {
            if (_button != null && _clickAction != null)
                _button.onClick.RemoveListener(_clickAction);

            _clickAction = null;
        }
    }
}
