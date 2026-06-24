/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪加工区 UI 项，负责接收材料拖拽并触发研磨
* │  类    名: CookProcessAreaItem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.Cook
{
    // 烹饪加工区 UI 项，负责接收材料拖拽并触发研磨
    public class CookProcessAreaItem : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private readonly Color _idleColor = new Color(0.24f, 0.18f, 0.29f, 0.88f);
        private readonly Color _highlightColor = new Color(0.43f, 0.29f, 0.52f, 0.96f);

        private CookView _view;
        private Image _imgBackground;

        private void Awake()
        {
            _imgBackground = GetComponent<Image>();
        }

        // 初始化加工区归属视图
        public void Init(CookView view)
        {
            _view = view;
            _imgBackground ??= GetComponent<Image>();
            if (_imgBackground != null)
                _imgBackground.color = _idleColor;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_view == null || eventData.pointerDrag == null) return;

            CookMaterialItem materialItem = eventData.pointerDrag.GetComponent<CookMaterialItem>();
            if (materialItem == null) return;

            if (_view.TryProcessMaterial(materialItem))
                materialItem.AcceptDropAndDestroy();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_imgBackground != null)
                _imgBackground.color = _highlightColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_imgBackground != null)
                _imgBackground.color = _idleColor;
        }
    }
}
