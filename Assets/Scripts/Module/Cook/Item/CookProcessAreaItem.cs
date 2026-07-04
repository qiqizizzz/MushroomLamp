/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪加工区 UI 项，负责接收材料拖拽并触发研磨
* │  类    名: CookProcessAreaItem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.View;
using MVC.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.Cook
{
    // 烹饪加工区 UI 项，负责接收材料拖拽并触发研磨
    public class CookProcessAreaItem : BaseItem, IDropHandler
    {
        private CookView _view;
        private Image _imgBackground;

        protected override void OnAwake()
        {
            _imgBackground = GetComponent<Image>();
        }

        // 初始化加工区归属视图
        public void Init(CookView view)
        {
            _view = view;
            _imgBackground ??= GetComponent<Image>();
            if (_imgBackground != null)
            {
                Color clearColor = _imgBackground.color;
                clearColor.a = 0f;
                _imgBackground.color = clearColor;
                _imgBackground.raycastTarget = true;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_view == null || eventData.pointerDrag == null) return;

            CookMaterialItem materialItem = eventData.pointerDrag.GetComponent<CookMaterialItem>();
            if (materialItem == null) return;

            if (_view.TryProcessMaterial(materialItem))
                materialItem.AcceptDropAndDestroy();
        }
    }
}
