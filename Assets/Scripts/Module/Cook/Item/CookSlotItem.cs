/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪法阵槽位 UI 项，负责展示槽位状态并接收材料拖拽
* │  类    名: CookSlotItem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Cook;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.View
{
    // 烹饪法阵槽位 UI 项，负责展示槽位状态并接收材料拖拽
    public class CookSlotItem : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private readonly Color _emptyColor = new Color(0.96f, 0.82f, 0.58f, 0.92f);
        private readonly Color _highlightColor = new Color(0.99f, 0.94f, 0.42f, 1f);
        private readonly Color _occupiedColor = new Color(0.78f, 0.55f, 0.32f, 1f);
        private readonly Color _cornerColor = new Color(0.88f, 0.72f, 0.48f, 0.92f);
        private readonly Color _edgeColor = new Color(0.94f, 0.78f, 0.48f, 0.95f);
        private readonly Color _centerColor = new Color(0.98f, 0.67f, 0.36f, 1f);

        private CookView _view;
        private int _slotIndex;
        private bool _hasMaterial;
        private Color _currentEmptyColor;
        private Transform _originalParent;
        private Vector2 _originalAnchoredPosition;
        private int _originalSiblingIndex;
        private bool _dropAccepted;

        private Image _imgBackground;
        private Image _imgIcon;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _txtOrder;
        private TextMeshProUGUI _txtEnchant;
        private TextMeshProUGUI _txtName;
        private TextMeshProUGUI _txtValue;

        public int SlotIndex => _slotIndex;
        public bool HasMaterial => _hasMaterial;

        private void Awake()
        {
            ensureReferences();
        }

        // 初始化槽位索引
        public void Init(CookView view, int slotIndex)
        {
            _view = view;
            _slotIndex = slotIndex;
            applyFont(view == null ? null : view.GetFontAsset());
        }

        // 绑定槽位数据
        public void Bind(CookSlotData slotData)
        {
            ensureReferences();

            _hasMaterial = slotData != null && slotData.HasMaterial;
            _currentEmptyColor = getEmptyColor(slotData);
            CookMaterialData material = slotData?.Material;

            if (_imgBackground != null)
                _imgBackground.color = _hasMaterial ? _occupiedColor : _currentEmptyColor;

            if (_imgIcon != null)
            {
                _imgIcon.sprite = material?.Icon;
                _imgIcon.enabled = material?.Icon != null;
            }

            if (_txtOrder != null)
                _txtOrder.text = _hasMaterial ? slotData.Order.ToString() : string.Empty;

            if (_txtEnchant != null)
                _txtEnchant.text = slotData == null ? string.Empty : $"+{slotData.EnchantText}";

            if (_txtName != null)
                _txtName.text = material?.MaterialName ?? "空槽";

            if (_txtValue != null)
                _txtValue.text = material == null ? string.Empty : $"{material.ValueText}\n{material.CookProgressText}";
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_view == null || eventData.pointerDrag == null) return;

            CookMaterialItem materialItem = eventData.pointerDrag.GetComponent<CookMaterialItem>();
            if (materialItem != null && _view.TryPlaceMaterial(materialItem, _slotIndex))
            {
                materialItem.AcceptDropAndDestroy();
                return;
            }

            CookSlotItem slotItem = eventData.pointerDrag.GetComponent<CookSlotItem>();
            if (slotItem == null || slotItem == this) return;

            if (_view.TryMoveSlotMaterial(slotItem.SlotIndex, _slotIndex))
                slotItem.MarkDropAccepted();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_imgBackground != null && !_hasMaterial)
                _imgBackground.color = _highlightColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_imgBackground != null && !_hasMaterial)
                _imgBackground.color = _currentEmptyColor;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_hasMaterial || _view == null) return;

            _originalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();
            _originalAnchoredPosition = _rectTransform.anchoredPosition;
            _dropAccepted = false;

            Transform dragRoot = _view.GetDragRoot();
            if (dragRoot != null)
                transform.SetParent(dragRoot, false);

            transform.SetAsLastSibling();
            moveToPointer(eventData);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0.82f;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_hasMaterial) return;

            moveToPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dropAccepted)
                _dropAccepted = false;

            restoreToOriginalParent();
        }

        // 标记槽位拖拽已被目标接收
        public void MarkDropAccepted()
        {
            _dropAccepted = true;
        }

        private void ensureReferences()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
                _rectTransform = gameObject.AddComponent<RectTransform>();

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _imgBackground = getOrCreateImage("Img_Background", transform, _emptyColor);
            _imgIcon = getOrCreateImage("Img_Icon", transform, Color.white);
            _txtOrder = getOrCreateText("Txt_Order", transform, 26, TextAlignmentOptions.Center);
            _txtEnchant = getOrCreateText("Txt_Enchant", transform, 22, TextAlignmentOptions.Center);
            _txtName = getOrCreateText("Txt_Name", transform, 18, TextAlignmentOptions.Center);
            _txtValue = getOrCreateText("Txt_Value", transform, 22, TextAlignmentOptions.Center);

            setupChildRect(_imgBackground.rectTransform, Vector2.zero, Vector2.one);
            setupChildRect(_imgIcon.rectTransform, new Vector2(0.22f, 0.3f), new Vector2(0.78f, 0.78f));
            setupChildRect(_txtOrder.rectTransform, new Vector2(0.02f, 0.72f), new Vector2(0.28f, 0.98f));
            setupChildRect(_txtEnchant.rectTransform, new Vector2(0.34f, 0.72f), new Vector2(0.66f, 0.98f));
            setupChildRect(_txtName.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.3f));
            setupChildRect(_txtValue.rectTransform, new Vector2(0.72f, 0.72f), new Vector2(0.98f, 0.98f));
        }

        private void restoreToOriginalParent()
        {
            if (_originalParent != null)
            {
                transform.SetParent(_originalParent, false);
                transform.SetSiblingIndex(_originalSiblingIndex);
                _rectTransform.anchoredPosition = _originalAnchoredPosition;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }
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
            text.color = new Color(0.16f, 0.09f, 0.05f, 1f);
            text.enableWordWrapping = false;
            return text;
        }

        private void applyFont(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return;

            if (_txtOrder != null)
                _txtOrder.font = fontAsset;

            if (_txtEnchant != null)
                _txtEnchant.font = fontAsset;

            if (_txtName != null)
                _txtName.font = fontAsset;

            if (_txtValue != null)
                _txtValue.font = fontAsset;
        }

        private static void setupChildRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rectTransform == null) return;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        // 根据槽位类型获取空槽底色
        private Color getEmptyColor(CookSlotData slotData)
        {
            if (slotData == null)
                return _emptyColor;

            return slotData.SlotType switch
            {
                CookSlotType.Center => _centerColor,
                CookSlotType.Edge => _edgeColor,
                _ => _cornerColor
            };
        }
    }
}
