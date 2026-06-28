/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪材料 UI 项，负责展示材料并处理拖拽输入
* │  类    名: CookMaterialItem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Cook;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.View
{
    // 烹饪材料 UI 项，负责展示材料并处理拖拽输入
    public class CookMaterialItem : BaseItem, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float CardWidth = 150f;
        private const float CardHeight = 180f;

        private CookView _view;
        private CookMaterialData _materialData;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Transform _originalParent;
        private Vector2 _originalAnchoredPosition;
        private int _originalSiblingIndex;
        private bool _dropAccepted;
        private float _displayWidth = CardWidth;
        private float _displayHeight = CardHeight;

        private Image _imgBackground;
        private Image _imgIcon;
        private TextMeshProUGUI _txtName;
        private TextMeshProUGUI _txtValue;
        private TextMeshProUGUI _txtTag;

        public int MaterialId => _materialData?.RuntimeId ?? -1;

        protected override void OnAwake()
        {
            ensureReferences();
        }

        // 绑定材料数据
        public void Bind(CookMaterialData materialData, CookView view)
        {
            ensureReferences();

            _materialData = materialData;
            _view = view;
            _dropAccepted = false;
            applyFont(view == null ? null : view.GetFontAsset());

            if (_txtName != null)
                _txtName.text = materialData?.MaterialName ?? string.Empty;

            if (_txtValue != null)
                _txtValue.text = materialData == null ? string.Empty : materialData.ValueText;

            if (_txtTag != null)
                _txtTag.text = materialData?.TagText ?? string.Empty;

            if (_imgIcon != null)
            {
                _imgIcon.sprite = materialData?.Icon;
                _imgIcon.enabled = materialData?.Icon != null;
            }
        }

        // 设置材料卡在当前区域的显示尺寸
        public void SetDisplaySize(float width, float height)
        {
            _displayWidth = Mathf.Max(1f, width);
            _displayHeight = Mathf.Max(1f, height);
            applyDisplaySize(_displayWidth, _displayHeight);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_materialData == null || _view == null) return;

            _originalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();
            _originalAnchoredPosition = _rectTransform.anchoredPosition;
            _dropAccepted = false;

            Transform dragRoot = _view.GetDragRoot();
            if (dragRoot != null)
                transform.SetParent(dragRoot, false);

            transform.SetAsLastSibling();
            _rectTransform.localScale = Vector3.one;
            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);
            moveToPointer(eventData);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0.88f;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            moveToPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dropAccepted) return;

            restoreToOriginalParent();
        }

        // 标记拖拽已被目标区域接收
        public void MarkDropAccepted()
        {
            _dropAccepted = true;
        }

        // 接受拖拽放置并销毁拖拽中的临时 UI
        public void AcceptDropAndDestroy()
        {
            _dropAccepted = true;
            Destroy(gameObject);
        }

        private void ensureReferences()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
                _rectTransform = gameObject.AddComponent<RectTransform>();

            applyDisplaySize(_displayWidth, _displayHeight);

            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = gameObject.AddComponent<LayoutElement>();

            layoutElement.preferredWidth = _displayWidth;
            layoutElement.preferredHeight = _displayHeight;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _imgBackground = getOrCreateImage("Img_Background", transform, new Color(0.95f, 0.89f, 0.74f, 1f));
            _imgIcon = getOrCreateImage("Img_Icon", transform, Color.white);
            _txtName = getOrCreateText("Txt_Name", transform, 22, TextAlignmentOptions.Center);
            _txtValue = getOrCreateText("Txt_Value", transform, 30, TextAlignmentOptions.Center);
            _txtTag = getOrCreateText("Txt_Tag", transform, 18, TextAlignmentOptions.Center);

            setupChildRect(_imgBackground.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            setupChildRect(_imgIcon.rectTransform, new Vector2(0.2f, 0.38f), new Vector2(0.8f, 0.82f), Vector2.zero, Vector2.zero);
            setupChildRect(_txtName.rectTransform, new Vector2(0.06f, 0.76f), new Vector2(0.94f, 0.98f), Vector2.zero, Vector2.zero);
            setupChildRect(_txtValue.rectTransform, new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.42f), Vector2.zero, Vector2.zero);
            setupChildRect(_txtTag.rectTransform, new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.2f), Vector2.zero, Vector2.zero);
        }

        private void restoreToOriginalParent()
        {
            if (_originalParent == null) return;

            transform.SetParent(_originalParent, false);
            transform.SetSiblingIndex(_originalSiblingIndex);
            _rectTransform.anchoredPosition = _originalAnchoredPosition;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }

            applyDisplaySize(_displayWidth, _displayHeight);
        }

        // 应用材料卡尺寸到 RectTransform 与布局组件
        private void applyDisplaySize(float width, float height)
        {
            if (_rectTransform != null)
                _rectTransform.sizeDelta = new Vector2(width, height);

            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = gameObject.AddComponent<LayoutElement>();

            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = height;
        }

        private void moveToPointer(PointerEventData eventData)
        {
            RectTransform dragRoot = _view == null ? null : _view.GetDragRoot() as RectTransform;
            if (dragRoot == null || _rectTransform == null || eventData == null) return;

            Camera eventCamera = resolveEventCamera(eventData);
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    dragRoot,
                    eventData.position,
                    eventCamera,
                    out Vector3 worldPoint))
            {
                _rectTransform.position = worldPoint;
            }
        }

        private Camera resolveEventCamera(PointerEventData eventData)
        {
            Canvas canvas = _view == null ? null : _view.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    return null;

                if (canvas.worldCamera != null)
                    return canvas.worldCamera;
            }

            if (eventData.pressEventCamera != null)
                return eventData.pressEventCamera;

            if (eventData.enterEventCamera != null)
                return eventData.enterEventCamera;

            return Camera.main;
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

        private static TextMeshProUGUI getOrCreateText(
            string childName,
            Transform parent,
            int fontSize,
            TextAlignmentOptions alignment)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                GameObject obj = new GameObject(childName, typeof(RectTransform));
                obj.transform.SetParent(parent, false);
                child = obj.transform;
            }

            TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
            if (text == null)
                text = child.gameObject.AddComponent<TextMeshProUGUI>();

            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.18f, 0.13f, 0.09f, 1f);
            text.enableWordWrapping = false;
            return text;
        }

        private void applyFont(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return;

            if (_txtName != null)
                _txtName.font = fontAsset;

            if (_txtValue != null)
                _txtValue.font = fontAsset;

            if (_txtTag != null)
                _txtTag.font = fontAsset;
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
