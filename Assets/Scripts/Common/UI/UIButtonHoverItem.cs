using Common;
using MVC.View;
using Spine.Unity;
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
        private RectTransform _scaleTarget;
        private SkeletonGraphic _spineGraphic;
        private float _spineNormalTimeScale = 1f;
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
            _scaleTarget = _rectTransform;
            captureBaseScale();
            updateHitSize();
            applyVisual(false);
        }

        // Spine 魔盒按钮：悬停放大/暂停 + 点击打开 Blackjack
        public void SetupSpineButton(Button button, SkeletonGraphic spine, float hoverScaleMultiplier = DefaultHoverScaleMultiplier)
        {
            if (button == null) return;

            if (spine != null)
            {
                spine.raycastTarget = true;
                button.targetGraphic = spine;
            }

            button.transition = Selectable.Transition.None;
            SetupSpineHover(button.transform as RectTransform, spine, hoverScaleMultiplier);
        }

        // Spine 展示悬停：放大目标 Rect，并在悬停时暂停动画
        public void SetupSpineHover(RectTransform target, SkeletonGraphic spine, float hoverScaleMultiplier = DefaultHoverScaleMultiplier)
        {
            if (target == null) return;

            _rectTransform = target;
            _scaleTarget = target;
            _spineGraphic = spine;
            _hoverScaleMultiplier = hoverScaleMultiplier > 0f ? hoverScaleMultiplier : DefaultHoverScaleMultiplier;
            if (_spineGraphic != null)
                _spineNormalTimeScale = _spineGraphic.timeScale;

            captureBaseScale();
            updateHitSize();
        }

        public void SetInteractable(bool value)
        {
            _interactable = value;
            if (value) return;

            _isPointerInside = false;
            _targetScale = _baseScale;
            setSpinePaused(false);
            applyVisual(shouldUseHoverVisual());
            applyScaleImmediate(_baseScale);
        }

        // 选中态沿用 hover 图，并保持最大 scale
        public void SetSelectedVisual(bool selected)
        {
            _selectedVisual = selected;
            applyVisual(shouldUseHoverVisual());
            if (!_isPointerInside)
                _targetScale = getRestScale();
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
            RectTransform scaleRt = _scaleTarget != null ? _scaleTarget : _rectTransform;
            if (scaleRt == null) return;

            updatePointerHover();

            Vector3 current = scaleRt.localScale;
            Vector3 target = Vector3.one * _targetScale;
            if ((current - target).sqrMagnitude > 0.0001f)
                scaleRt.localScale = Vector3.Lerp(current, target, Time.deltaTime * _scaleLerpSpeed);
            else
                scaleRt.localScale = target;
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
                    setSpinePaused(true);
                }

                _targetScale = _baseScale * _hoverScaleMultiplier;
                return;
            }

            if (!_isPointerInside) return;

            _isPointerInside = false;
            applyVisual(shouldUseHoverVisual());
            setSpinePaused(false);
            _targetScale = getRestScale();
        }

        private float getRestScale()
        {
            return _selectedVisual ? _baseScale * _hoverScaleMultiplier : _baseScale;
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
            RectTransform scaleRt = _scaleTarget != null ? _scaleTarget : _rectTransform;
            if (scaleRt == null) return;
            float s = scaleRt.localScale.x;
            if (s > 0.001f) _baseScale = s;
            _targetScale = _baseScale;
        }

        private void applyScaleImmediate(float scale)
        {
            RectTransform scaleRt = _scaleTarget != null ? _scaleTarget : _rectTransform;
            if (scaleRt != null)
                scaleRt.localScale = Vector3.one * scale;
        }

        private void setSpinePaused(bool paused)
        {
            if (_spineGraphic == null) return;
            _spineGraphic.timeScale = paused ? 0f : _spineNormalTimeScale;
        }

        private void updateHitSize()
        {
            if (_rectTransform == null) return;
            if (_rectTransform.rect.width > 1f) _width = _rectTransform.rect.width;
            if (_rectTransform.rect.height > 1f) _height = _rectTransform.rect.height;
        }
    }
}
