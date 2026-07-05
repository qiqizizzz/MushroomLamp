using MVC.View;
using UnityEngine;

namespace Module.Store
{
    // 材料卡牌悬停：放大图标 + 显示与 CookView 相同的详情浮层
    public class StoreBuyHoverItem : BaseItem
    {
        private const float HoverScale = 1.2f;
        private const float ScaleLerpSpeed = 12f;

        private IStoreMaterialTooltipHost _host;
        private RectTransform _iconRect;
        private RectTransform _hitRect;
        private string _materialId;
        private bool _interactable = true;
        private bool _isPointerInside;
        private float _baseScale = 1f;
        private float _targetScale = 1f;

        public void Setup(RectTransform iconRect)
        {
            Setup(null, iconRect, null);
        }

        public void Setup(IStoreMaterialTooltipHost host, RectTransform iconRect, string materialId)
        {
            _host = host;
            _materialId = materialId;
            _iconRect = iconRect;
            _hitRect = transform as RectTransform;
            captureBaseScale();
        }

        public void SetHoverEnabled(bool value)
        {
            _interactable = value;
            if (value) return;

            _isPointerInside = false;
            _targetScale = _baseScale;
            resetIconScale();
            _host?.HideMaterialTooltip(this);
        }

        public void SetInteractable(bool value) => SetHoverEnabled(value);

        protected override void OnAwake()
        {
            _hitRect = transform as RectTransform;

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
            updatePointerHover();

            if (_iconRect == null) return;

            Vector3 current = _iconRect.localScale;
            Vector3 target = Vector3.one * _targetScale;
            if ((current - target).sqrMagnitude > 0.0001f)
                _iconRect.localScale = Vector3.Lerp(current, target, Time.deltaTime * ScaleLerpSpeed);
            else
                _iconRect.localScale = target;
        }

        protected override void OnDestroy()
        {
            _host?.HideMaterialTooltip(this);
            base.OnDestroy();
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
                if (!_isPointerInside)
                {
                    _isPointerInside = true;
                    _targetScale = _baseScale * HoverScale;
                    if (_host != null && !string.IsNullOrWhiteSpace(_materialId))
                        _host.ShowMaterialTooltip(this, _materialId, Input.mousePosition);
                    return;
                }

                _host?.MoveMaterialTooltip(Input.mousePosition);
                return;
            }

            if (!_isPointerInside) return;

            _isPointerInside = false;
            _targetScale = _baseScale;
            _host?.HideMaterialTooltip(this);
        }

        private bool isPointerOverIcon()
        {
            RectTransform target = _hitRect != null ? _hitRect : _iconRect;
            if (target == null) return false;

            Camera camera = resolveHoverCamera();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    target,
                    Input.mousePosition,
                    camera,
                    out Vector2 localPoint))
                return false;

            Rect rect = target.rect;
            Vector2 halfSize = new Vector2(rect.width * 0.5f, rect.height * 0.5f);
            return localPoint.x >= -halfSize.x && localPoint.x <= halfSize.x
                && localPoint.y >= -halfSize.y && localPoint.y <= halfSize.y;
        }

        private Camera resolveHoverCamera()
        {
            RectTransform probe = _hitRect != null ? _hitRect : _iconRect;
            Canvas canvas = probe != null ? probe.GetComponentInParent<Canvas>() : null;
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
