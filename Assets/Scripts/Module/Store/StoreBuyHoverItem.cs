using MVC.View;
using UnityEngine;

namespace Module.Store
{
    // 购买卡牌悬停：仅放大上方图标，不影响下方材料介绍框
    public class StoreBuyHoverItem : BaseItem
    {
        private const float HoverScale = 1.2f;
        private const float ScaleLerpSpeed = 12f;

        private RectTransform _iconRect;
        private bool _interactable = true;
        private bool _isPointerInside;
        private float _baseScale = 1f;
        private float _targetScale = 1f;

        public void Setup(RectTransform iconRect)
        {
            _iconRect = iconRect;
            captureBaseScale();
        }

        public void SetInteractable(bool value)
        {
            _interactable = value;
            if (value) return;

            _isPointerInside = false;
            _targetScale = _baseScale;
            resetIconScale();
        }

        protected override void OnAwake()
        {
            if (_iconRect == null)
            {
                Transform iconTf = transform.Find("Img_Icon");
                if (iconTf != null) _iconRect = iconTf as RectTransform;
            }

            captureBaseScale();
            _targetScale = _baseScale;
        }

        protected override void OnUpdate()
        {
            if (_iconRect == null) return;

            updatePointerHover();

            Vector3 current = _iconRect.localScale;
            Vector3 target = Vector3.one * _targetScale;
            if ((current - target).sqrMagnitude > 0.0001f)
                _iconRect.localScale = Vector3.Lerp(current, target, Time.deltaTime * ScaleLerpSpeed);
            else
                _iconRect.localScale = target;
        }

        private void updatePointerHover()
        {
            if (!_interactable)
            {
                _targetScale = _baseScale;
                return;
            }

            bool isInside = isPointerOverIcon();
            if (isInside)
            {
                if (!_isPointerInside) _isPointerInside = true;
                _targetScale = _baseScale * HoverScale;
                return;
            }

            if (!_isPointerInside) return;

            _isPointerInside = false;
            _targetScale = _baseScale;
        }

        private bool isPointerOverIcon()
        {
            if (_iconRect == null) return false;

            Camera camera = resolveHoverCamera();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _iconRect,
                    Input.mousePosition,
                    camera,
                    out Vector2 localPoint))
                return false;

            Rect rect = _iconRect.rect;
            Vector2 halfSize = new Vector2(rect.width * 0.5f, rect.height * 0.5f);
            return localPoint.x >= -halfSize.x && localPoint.x <= halfSize.x
                && localPoint.y >= -halfSize.y && localPoint.y <= halfSize.y;
        }

        private Camera resolveHoverCamera()
        {
            Canvas canvas = _iconRect != null ? _iconRect.GetComponentInParent<Canvas>() : null;
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        private void resetIconScale()
        {
            if (_iconRect != null)
                _iconRect.localScale = Vector3.one * _baseScale;
        }

        private void captureBaseScale()
        {
            if (_iconRect == null) return;
            float s = _iconRect.localScale.x;
            if (s > 0.01f) _baseScale = s;
        }
    }
}
