using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Shop
{
    // 商店货架悬停放大（与 CookMaterialItem 卡牌悬停参数一致）
    public class ShopHoverScaleItem : BaseItem
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

        public void SetHitSize(float width, float height)
        {
            _width = Mathf.Max(1f, width);
            _height = Mathf.Max(1f, height);
        }

        public void SetInteractable(bool value)
        {
            _interactable = value;
            if (value) return;

            _isPointerInside = false;
            _targetScale = _baseScale;
            if (_rectTransform != null)
                _rectTransform.localScale = Vector3.one * _baseScale;
        }

        protected override void OnAwake()
        {
            _rectTransform = transform as RectTransform;
            if (_rectTransform != null)
            {
                float scale = _rectTransform.localScale.x;
                if (Mathf.Abs(scale) > 0.01f)
                    _baseScale = scale;

                _targetScale = _baseScale;
                _width = _rectTransform.rect.width > 1f ? _rectTransform.rect.width : _width;
                _height = _rectTransform.rect.height > 1f ? _rectTransform.rect.height : _height;
            }
        }

        protected override void OnUpdate()
        {
            if (_rectTransform == null) return;

            updatePointerHover();

            Vector3 current = _rectTransform.localScale;
            Vector3 target = Vector3.one * _targetScale;
            if ((current - target).sqrMagnitude > 0.0001f)
                _rectTransform.localScale = Vector3.Lerp(current, target, Time.deltaTime * ScaleLerpSpeed);
            else
                _rectTransform.localScale = target;
        }

        private void updatePointerHover()
        {
            if (!_interactable)
            {
                _targetScale = _baseScale;
                return;
            }

            bool isInside = isPointerOver();
            if (isInside)
            {
                _isPointerInside = true;
                _targetScale = _baseScale * HoverScale;
                return;
            }

            if (!_isPointerInside) return;

            _isPointerInside = false;
            _targetScale = _baseScale;
        }

        private bool isPointerOver()
        {
            Camera camera = resolveHoverCamera();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform,
                    Input.mousePosition,
                    camera,
                    out Vector2 localPoint))
                return false;

            Vector3 scale = _rectTransform.localScale;
            if (Mathf.Abs(scale.x) > 0.001f)
                localPoint.x /= scale.x;
            if (Mathf.Abs(scale.y) > 0.001f)
                localPoint.y /= scale.y;

            Vector2 halfSize = new Vector2(_width * 0.5f, _height * 0.5f);
            return localPoint.x >= -halfSize.x && localPoint.x <= halfSize.x
                && localPoint.y >= -halfSize.y && localPoint.y <= halfSize.y;
        }

        private Camera resolveHoverCamera()
        {
            Canvas canvas = _rectTransform != null ? _rectTransform.GetComponentInParent<Canvas>() : null;
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }
    }
}
