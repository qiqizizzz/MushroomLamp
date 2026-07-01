using Common;
using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Common.UI
{
    // 按钮悬停：可选换图 + 平滑放大（禁用 Button 默认 Transition）
    public class UIButtonHoverItem : BaseItem
    {
        private const float DefaultHoverScaleMultiplier = 1.15f;
        private const float DefaultScaleLerpSpeed = 12f;

        private Image _image;
        private RectTransform _rectTransform;
        private Sprite _normalSprite;
        private Sprite _hoverSprite;
        private float _baseScale = 1f;
        private float _targetScale = 1f;
        private float _hoverScaleMultiplier = DefaultHoverScaleMultiplier;
        private float _scaleLerpSpeed = DefaultScaleLerpSpeed;
        private float _width = 200f;
        private float _height = 200f;
        private bool _interactable = true;
        private bool _isPointerInside;
        private bool _selectedVisual;

        public void Setup(Button button, string hoverSpriteAddress = null, float hoverScaleMultiplier = DefaultHoverScaleMultiplier)
        {
            if (button == null) return;

            _hoverScaleMultiplier = hoverScaleMultiplier > 0f ? hoverScaleMultiplier : DefaultHoverScaleMultiplier;
            _image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            _rectTransform = button.transform as RectTransform;

            if (_image != null)
            {
                _normalSprite = _image.sprite;
                if (!string.IsNullOrEmpty(hoverSpriteAddress))
                    _hoverSprite = ArtAssetLoader.LoadSprite(hoverSpriteAddress, logOnFail: false);
            }

            button.transition = Selectable.Transition.None;
            captureBaseScale();
            updateHitSize();
            applyVisual(false);
        }

        public void SetInteractable(bool value)
        {
            _interactable = value;
            if (value) return;

            _isPointerInside = false;
            _targetScale = _baseScale;
            applyVisual(shouldUseHoverVisual());
            if (_rectTransform != null)
                _rectTransform.localScale = Vector3.one * _baseScale;
        }

        // 选中态沿用 hover 图，但不强制放大（放大仍由鼠标悬停触发）
        public void SetSelectedVisual(bool selected)
        {
            _selectedVisual = selected;
            applyVisual(shouldUseHoverVisual());
        }

        protected override void OnAwake()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            if (_image == null)
                _image = GetComponent<Image>();

            if (_image != null && _normalSprite == null)
                _normalSprite = _image.sprite;

            captureBaseScale();
            updateHitSize();
        }

        protected override void OnUpdate()
        {
            if (_rectTransform == null) return;

            updatePointerHover();

            Vector3 current = _rectTransform.localScale;
            Vector3 target = Vector3.one * _targetScale;
            if ((current - target).sqrMagnitude > 0.0001f)
                _rectTransform.localScale = Vector3.Lerp(current, target, Time.deltaTime * _scaleLerpSpeed);
            else
                _rectTransform.localScale = target;
        }

        private void updatePointerHover()
        {
            if (!_interactable)
            {
                applyVisual(shouldUseHoverVisual());
                return;
            }

            bool isInside = isPointerOver();
            if (isInside)
            {
                if (!_isPointerInside)
                {
                    _isPointerInside = true;
                    applyVisual(shouldUseHoverVisual());
                }

                _targetScale = _baseScale * _hoverScaleMultiplier;
                return;
            }

            if (!_isPointerInside) return;

            _isPointerInside = false;
            applyVisual(shouldUseHoverVisual());
            _targetScale = _baseScale;
        }

        private bool shouldUseHoverVisual() => _selectedVisual || _isPointerInside;

        private void applyVisual(bool useHoverVisual)
        {
            if (_image == null) return;

            if (useHoverVisual && _hoverSprite != null)
                _image.sprite = _hoverSprite;
            else if (_normalSprite != null)
                _image.sprite = _normalSprite;
        }

        private bool isPointerOver()
        {
            if (_rectTransform == null) return false;

            Camera camera = resolveHoverCamera();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform,
                    Input.mousePosition,
                    camera,
                    out Vector2 localPoint))
                return false;

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

        private void captureBaseScale()
        {
            if (_rectTransform == null) return;
            float s = _rectTransform.localScale.x;
            if (s > 0.001f) _baseScale = s;
            _targetScale = _baseScale;
        }

        private void updateHitSize()
        {
            if (_rectTransform == null) return;
            if (_rectTransform.rect.width > 1f) _width = _rectTransform.rect.width;
            if (_rectTransform.rect.height > 1f) _height = _rectTransform.rect.height;
        }
    }
}
