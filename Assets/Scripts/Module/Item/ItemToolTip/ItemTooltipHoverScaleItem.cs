/*
* ┌──────────────────────────────────┐
* │  描    述: 通用悬停组件，负责放大目标并显示详情浮层
* │  类    名: ItemTooltipHoverScaleItem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using MVC.View;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Module.Item
{
    public class ItemTooltipHoverScaleItem : BaseItem, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverScale = 1.2f;
        private const float ScaleLerpSpeed = 12f;

        private RectTransform _rectTransform;
        private bool _interactable = true;
        private bool _isPointerInside;
        private float _baseScale = 1f;
        private float _targetScale = 1f;
        private float _width = 200f;
        private float _height = 200f;
        private IItemTooltipDataHost _view;
        private ItemTooltipData _tooltipData;
        private bool _isTooltipVisible;

        public void SetHitSize(float width, float height)
        {
            _width = Mathf.Max(1f, width);
            _height = Mathf.Max(1f, height);
        }

        public void SetInteractable(bool value)
        {
            _interactable = value;
            if (value) return;

            hideTooltip();
            _isPointerInside = false;
            _targetScale = _baseScale;
            if (_rectTransform != null)
                _rectTransform.localScale = Vector3.one * _baseScale;
        }

        public void BindTooltip(IItemTooltipDataHost view, ItemTooltipData tooltipData)
        {
            hideTooltip();
            _view = view;
            _tooltipData = tooltipData;
        }

        protected override void OnAwake()
        {
            _rectTransform = transform as RectTransform;
            if (_rectTransform == null) return;

            float scale = _rectTransform.localScale.x;
            if (Mathf.Abs(scale) > 0.01f)
                _baseScale = scale;

            _targetScale = _baseScale;
            _width = _rectTransform.rect.width > 1f ? _rectTransform.rect.width : _width;
            _height = _rectTransform.rect.height > 1f ? _rectTransform.rect.height : _height;
        }

        private void OnDisable()
        {
            clearPointerHoverState();
        }

        protected override void OnUpdate()
        {
            if (_rectTransform == null) return;

            updateTooltipPosition();

            Vector3 current = _rectTransform.localScale;
            Vector3 target = Vector3.one * _targetScale;
            if ((current - target).sqrMagnitude > 0.0001f)
                _rectTransform.localScale = Vector3.Lerp(current, target, Time.deltaTime * ScaleLerpSpeed);
            else
                _rectTransform.localScale = target;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_interactable || _view == null || _tooltipData == null || eventData == null) return;

            _isPointerInside = true;
            _targetScale = _baseScale * HoverScale;
            showTooltip(eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            clearPointerHoverState();
        }

        private void updateTooltipPosition()
        {
            if (!_isPointerInside || !_isTooltipVisible) return;

            _view?.MoveItemTooltipData(Input.mousePosition);
        }

        private void showTooltip(Vector2 screenPosition)
        {
            if (_view == null || _tooltipData == null) return;

            _isTooltipVisible = true;
            _view.ShowItemTooltipData(this, _tooltipData, screenPosition);
        }

        private void clearPointerHoverState()
        {
            if (!_isPointerInside && !_isTooltipVisible)
            {
                _targetScale = _baseScale;
                return;
            }

            _isPointerInside = false;
            _targetScale = _baseScale;
            hideTooltip();
        }

        private void hideTooltip()
        {
            if (!_isTooltipVisible) return;

            _isTooltipVisible = false;
            _view?.HideItemTooltipData(this);
        }

        protected override void OnDestroy()
        {
            hideTooltip();
            base.OnDestroy();
        }
    }
}
