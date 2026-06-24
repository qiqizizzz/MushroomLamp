/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪锅区域 UI 项，负责接收法阵材料入锅
* │  类    名: CookPotAreaItem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.View
{
    // 烹饪锅区域 UI 项，负责接收法阵材料入锅
    public class CookPotAreaItem : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private readonly Color _normalColor = new Color(0.95f, 0.52f, 0.18f, 0.85f);
        private readonly Color _highlightColor = new Color(1f, 0.7f, 0.28f, 1f);

        private CookView _view;
        private Image _imgBackground;

        private void Awake()
        {
            _imgBackground = GetComponent<Image>();
        }

        // 初始化锅区域所属界面
        public void Init(CookView view)
        {
            _view = view;
            _imgBackground = GetComponent<Image>();
            if (_imgBackground != null)
                _imgBackground.color = _normalColor;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_view == null || eventData.pointerDrag == null) return;

            CookSlotItem slotItem = eventData.pointerDrag.GetComponent<CookSlotItem>();
            if (slotItem == null || !slotItem.HasMaterial) return;

            if (_view.TrySubmitSlotToPot(slotItem.SlotIndex))
                slotItem.MarkDropAccepted();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_imgBackground != null)
                _imgBackground.color = _highlightColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_imgBackground != null)
                _imgBackground.color = _normalColor;
        }
    }
}
