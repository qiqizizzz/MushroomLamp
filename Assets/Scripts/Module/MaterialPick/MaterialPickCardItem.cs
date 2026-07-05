/*
* ┌──────────────────────────────────┐
* │  描    述: 材料三选一候选卡，交互对齐 CookView 手牌悬停
* │  类    名: MaterialPickCardItem.cs
* └──────────────────────────────────┘
*/

using System;
using Common;
using Module.Cook;
using Module.View;
using MVC.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.MaterialPick
{
    public class MaterialPickCardItem : BaseItem, IPointerClickHandler
    {
        private const float HoverScale = 1.2f;
        private const float ScaleLerpSpeed = 12f;
        private const float OutlineWidth = 3f;

        private IMaterialPickCardHost _host;
        private CookMaterialData _materialData;
        private RectTransform _rectTransform;
        private Image _imgBackground;
        private Image _imgIcon;
        private UnityEngine.Material _outlineMaterial;

        private bool _interactable = true;
        private bool _isPointerInside;
        private float _targetScale = 1f;
        private float _displayWidth;
        private float _displayHeight;
        private Action _onClick;

        public static Vector2 DefaultCardSize => CookMaterialItem.CardSize;

        public void Setup(
            CookMaterialData materialData,
            IMaterialPickCardHost host,
            Action onClick,
            float width,
            float height)
        {
            ensureReferences(width, height);
            _materialData = materialData;
            _host = host;
            _onClick = onClick;
            _interactable = true;
            _isPointerInside = false;
            _targetScale = 1f;
            setOutline(false);

            if (_imgIcon != null)
            {
                _imgIcon.sprite = materialData?.Icon;
                _imgIcon.enabled = materialData?.Icon != null;
                _imgIcon.preserveAspect = true;
            }
        }

        public void SetInteractable(bool value)
        {
            _interactable = value;
            if (value) return;

            clearPointerHoverState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable || _materialData == null) return;
            _onClick?.Invoke();
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

        protected override void OnDestroy()
        {
            _host?.HideCardTooltip(this);
            if (_outlineMaterial != null)
            {
                Destroy(_outlineMaterial);
                _outlineMaterial = null;
            }

            base.OnDestroy();
        }

        private void updatePointerHover()
        {
            if (_host == null || _materialData == null || !isActiveAndEnabled)
                return;

            if (!_interactable)
            {
                clearPointerHoverState();
                return;
            }

            Vector2 screenPosition = Input.mousePosition;
            bool isInside = isPointerOverCard(screenPosition);

            if (isInside)
            {
                if (!_isPointerInside)
                {
                    _isPointerInside = true;
                    _targetScale = HoverScale;
                    setOutline(true);
                    _host.ShowCardTooltip(this, _materialData, screenPosition);
                    return;
                }

                _host.MoveCardTooltip(screenPosition);
                return;
            }

            if (!_isPointerInside) return;
            clearPointerHoverState();
        }

        private void clearPointerHoverState()
        {
            if (!_isPointerInside) return;

            _isPointerInside = false;
            _targetScale = 1f;
            setOutline(false);
            _host?.HideCardTooltip(this);
        }

        private bool isPointerOverCard(Vector2 screenPosition)
        {
            Camera camera = resolveHoverCamera();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform,
                    screenPosition,
                    camera,
                    out Vector2 localPoint))
                return false;

            Vector3 scale = _rectTransform.localScale;
            if (Mathf.Abs(scale.x) > 0.001f)
                localPoint.x /= scale.x;
            if (Mathf.Abs(scale.y) > 0.001f)
                localPoint.y /= scale.y;

            Vector2 halfSize = new Vector2(_displayWidth * 0.5f, _displayHeight * 0.5f);
            return localPoint.x >= -halfSize.x && localPoint.x <= halfSize.x
                && localPoint.y >= -halfSize.y && localPoint.y <= halfSize.y;
        }

        private Camera resolveHoverCamera()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        private void setOutline(bool on)
        {
            if (_imgIcon == null) return;

            if (on)
            {
                if (_outlineMaterial == null)
                {
                    Shader shader = Shader.Find("UI/Outline");
                    if (shader == null) return;
                    _outlineMaterial = new UnityEngine.Material(shader);
                    _outlineMaterial.SetColor("_OutlineColor", Color.white);
                    _outlineMaterial.SetFloat("_OutlineWidth", OutlineWidth);
                }

                _outlineMaterial.SetFloat("_OutlineEnabled", 1f);
                _imgIcon.material = _outlineMaterial;
            }
            else
            {
                if (_outlineMaterial != null)
                    _outlineMaterial.SetFloat("_OutlineEnabled", 0f);
                _imgIcon.material = null;
            }
        }

        private void ensureReferences(float width, float height)
        {
            _displayWidth = Mathf.Max(1f, width);
            _displayHeight = Mathf.Max(1f, height);

            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
                _rectTransform = gameObject.AddComponent<RectTransform>();

            _rectTransform.sizeDelta = new Vector2(_displayWidth, _displayHeight);

            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = gameObject.AddComponent<LayoutElement>();

            layoutElement.preferredWidth = _displayWidth;
            layoutElement.preferredHeight = _displayHeight;

            _imgBackground = getOrCreateImage("Img_Background", transform, new Color(0f, 0f, 0f, 0f));
            _imgBackground.raycastTarget = true;
            _imgIcon = getOrCreateImage("Img_Icon", transform, Color.white);
            _imgIcon.preserveAspect = true;
            _imgIcon.raycastTarget = false;

            setupChildRect(_imgBackground.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            setupChildRect(_imgIcon.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static Image getOrCreateImage(string childName, Transform parent, Color color)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                GameObject obj = new GameObject(childName, typeof(RectTransform));
                obj.transform.SetParent(parent, false);
                child = obj.transform;
            }

            Image image = child.GetComponent<Image>();
            if (image == null)
                image = child.gameObject.AddComponent<Image>();

            image.color = color;
            return image;
        }

        private static void setupChildRect(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            if (rectTransform == null) return;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}
