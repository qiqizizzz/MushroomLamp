using MVC.View;
using TMPro;
using UnityEngine;
namespace Module.Hint
{
    // 简化版快捷键提示浮层，仅显示标题与描述，复用材料介绍框背景
    public class HintTooltip : BaseItem
    {
        private RectTransform _rectTransform;
        private TextMeshProUGUI _txtTitle;
        private TextMeshProUGUI _txtDesc;
        private bool _isInitialized;

        protected override void OnAwake()
        {
            bindPrefabReferences();
            SetVisible(false);
        }

        public void Bind(HintTooltipJsonData data)
        {
            bindPrefabReferences();
            if (data == null)
            {
                SetVisible(false);
                return;
            }

            if (_txtTitle != null)
                _txtTitle.text = string.IsNullOrWhiteSpace(data.title) ? string.Empty : data.title;

            if (_txtDesc != null)
                _txtDesc.text = string.IsNullOrWhiteSpace(data.description) ? string.Empty : data.description;

            SetVisible(true);
        }

        public void SetScreenPosition(Vector2 screenPosition, RectTransform canvasRect, Vector2 offset)
        {
            bindPrefabReferences();
            if (_rectTransform == null) return;

            if (canvasRect == null)
            {
                _rectTransform.position = screenPosition + offset;
                return;
            }

            Camera eventCamera = resolveCanvasCamera(canvasRect);
            Vector2 resolvedOffset = resolveOffset(screenPosition, canvasRect, eventCamera, offset);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition + resolvedOffset, eventCamera, out Vector2 localPoint))
                return;

            Vector2 size = _rectTransform.rect.size;
            if (size.x <= 0f || size.y <= 0f)
                size = _rectTransform.sizeDelta;

            Rect rect = canvasRect.rect;
            localPoint.x = Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax - size.x);
            localPoint.y = Mathf.Clamp(localPoint.y, rect.yMin + size.y, rect.yMax);
            _rectTransform.position = canvasRect.TransformPoint(localPoint);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public override void SetVisible(bool isVisible)
        {
            if (gameObject.activeSelf != isVisible)
                gameObject.SetActive(isVisible);
        }

        private void bindPrefabReferences()
        {
            if (_isInitialized) return;

            _rectTransform = GetComponent<RectTransform>();
            _txtTitle = findOptional<TextMeshProUGUI>("Content/Txt_Title");
            _txtDesc = findOptional<TextMeshProUGUI>("Content/DescBlock/Txt_Desc");
            _isInitialized = true;
        }

        private T findOptional<T>(string path) where T : Component
        {
            Transform target = transform.Find(path);
            return target != null ? target.GetComponent<T>() : null;
        }

        private Vector2 resolveOffset(Vector2 screenPosition, RectTransform canvasRect, Camera eventCamera, Vector2 fallbackOffset)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, eventCamera, out Vector2 cursorLocalPoint))
                return fallbackOffset;

            float tooltipHeight = Mathf.Max(_rectTransform.rect.height, _rectTransform.sizeDelta.y);
            float verticalOffset = cursorLocalPoint.y < canvasRect.rect.center.y
                ? tooltipHeight + Mathf.Abs(fallbackOffset.y)
                : -Mathf.Abs(fallbackOffset.y);

            return new Vector2(Mathf.Abs(fallbackOffset.x), verticalOffset);
        }

        private static Camera resolveCanvasCamera(RectTransform canvasRect)
        {
            Canvas canvas = canvasRect.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }
    }
}
