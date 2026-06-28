/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪法阵槽位 UI 项，负责展示槽位状态并接收材料拖拽
* │  类    名: CookSlotItem.cs
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
    // 烹饪法阵槽位 UI 项，负责展示槽位状态并接收材料拖拽
    public class CookSlotItem : BaseItem, IDropHandler,
        IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
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
        private CookMaterialData _materialData;
        private Color _currentEmptyColor;
        private bool _dropAccepted;

        private Image _imgBackground;
        private Image _imgIcon;
        private RectTransform _rectTransform;
        private GameObject _dragIconObject;
        private RectTransform _dragIconRect;
        private TextMeshProUGUI _txtOrder;
        private TextMeshProUGUI _txtEnchant;
        private TextMeshProUGUI _txtName;
        private TextMeshProUGUI _txtValue;

        public int SlotIndex => _slotIndex;
        public bool HasMaterial => _hasMaterial;

        protected override void OnAwake()
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
            _materialData = material;

            if (_imgBackground != null)
                _imgBackground.color = _hasMaterial ? _occupiedColor : _currentEmptyColor;

            if (_imgIcon != null)
            {
                _imgIcon.sprite = material?.Icon;
                _imgIcon.enabled = material?.Icon != null;
            }

            if (_txtOrder != null)
            {
                _txtOrder.enabled = true;
                _txtOrder.text = _hasMaterial ? slotData.Order.ToString() : string.Empty;
            }

            if (_txtEnchant != null)
                _txtEnchant.text = slotData == null ? string.Empty : $"+{slotData.EnchantText}";

            if (_txtName != null)
            {
                _txtName.enabled = true;
                _txtName.text = material?.Config?.name ?? "空槽";
            }

            if (_txtValue != null)
            {
                _txtValue.enabled = true;
                _txtValue.text = material == null ? string.Empty : $"{material.ValueText}\n{material.CookProgressText}";
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            _view?.HideItemTooltip();
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

            if (_hasMaterial && _materialData != null)
                _view?.ShowItemTooltip(_materialData, eventData);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (_hasMaterial)
                _view?.MoveItemTooltip(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_imgBackground != null && !_hasMaterial)
                _imgBackground.color = _currentEmptyColor;

            _view?.HideItemTooltip();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_hasMaterial || _view == null) return;
            if (_imgIcon == null || _imgIcon.sprite == null) return;

            _view.HideItemTooltip();
            _dropAccepted = false;
            createDragIcon(_imgIcon.sprite);
            setMaterialVisualVisible(false);
            moveDragIconToPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_hasMaterial) return;

            moveDragIconToPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            destroyDragIcon();

            if (!_dropAccepted)
                setMaterialVisualVisible(true);

            _dropAccepted = false;
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

        // 创建仅包含食物图片的拖拽图标
        private void createDragIcon(Sprite sprite)
        {
            destroyDragIcon();

            Transform dragRoot = _view.GetDragRoot();
            if (dragRoot == null) return;

            _dragIconObject = new GameObject("Dragging_SlotFoodIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _dragIconObject.transform.SetParent(dragRoot, false);
            _dragIconObject.transform.SetAsLastSibling();

            _dragIconRect = _dragIconObject.GetComponent<RectTransform>();
            _dragIconRect.sizeDelta = _imgIcon.rectTransform.rect.size;
            if (_dragIconRect.sizeDelta.sqrMagnitude <= 0f)
                _dragIconRect.sizeDelta = new Vector2(80f, 80f);

            _dragIconRect.localScale = Vector3.one;

            Image dragImage = _dragIconObject.GetComponent<Image>();
            dragImage.sprite = sprite;
            dragImage.preserveAspect = true;
            dragImage.raycastTarget = false;

            CanvasGroup canvasGroup = _dragIconObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.9f;
        }

        // 销毁拖拽中的临时食物图标
        private void destroyDragIcon()
        {
            if (_dragIconObject != null)
            {
                Destroy(_dragIconObject);
                _dragIconObject = null;
                _dragIconRect = null;
            }
        }

        // 设置槽位内食物相关显示是否可见
        private void setMaterialVisualVisible(bool isVisible)
        {
            if (_imgIcon != null)
                _imgIcon.enabled = isVisible && _imgIcon.sprite != null;

            if (_txtOrder != null)
                _txtOrder.enabled = isVisible;

            if (_txtName != null)
                _txtName.enabled = isVisible;

            if (_txtValue != null)
                _txtValue.enabled = isVisible;
        }

        private void moveDragIconToPointer(PointerEventData eventData)
        {
            RectTransform dragRoot = _view == null ? null : _view.GetDragRoot() as RectTransform;
            if (dragRoot == null || _dragIconRect == null || eventData == null) return;

            Camera eventCamera = resolveEventCamera(eventData);
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    dragRoot,
                    eventData.position,
                    eventCamera,
                    out Vector3 worldPoint))
            {
                _dragIconRect.position = worldPoint;
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
