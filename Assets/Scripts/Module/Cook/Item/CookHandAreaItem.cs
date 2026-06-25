/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪材料区 UI 项，负责接收本回合法阵材料撤回
* │  类    名: CookHandAreaItem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.Cook
{
    // 烹饪材料区 UI 项，负责接收本回合法阵材料撤回
    public class CookHandAreaItem : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private readonly Color _highlightColor = new Color(1f, 0.92f, 0.58f, 0.28f);

        private CookView _view;
        private Image _imgBackground;
        private Color _normalColor;

        private void Awake()
        {
            _imgBackground = GetComponent<Image>();
            if (_imgBackground != null)
                _normalColor = _imgBackground.color;
        }

        // 初始化材料区所属界面
        public void Init(CookView view)
        {
            _view = view;
            _imgBackground ??= GetComponent<Image>();
            if (_imgBackground != null)
                _normalColor = _imgBackground.color;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_view == null || eventData.pointerDrag == null) return;

            CookSlotItem slotItem = eventData.pointerDrag.GetComponent<CookSlotItem>();
            if (slotItem == null || !slotItem.HasMaterial) return;

            if (_view.TryReturnSlotMaterial(slotItem.SlotIndex))
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
