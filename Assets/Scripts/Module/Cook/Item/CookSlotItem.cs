/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪法阵槽位 UI 项，负责展示槽位状态并接收材料拖拽
* │  类    名: CookSlotItem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common;
using Module.Cook;
using MVC.View;
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
        private const string SLOT_CENTER_SPRITE = "Art/CookView/大圆";
        private const string SLOT_EDGE_SPRITE = "Art/CookView/小圆";
        private const string SLOT_CORNER_SPRITE = "Art/CookView/方框";

        private static Sprite S_CenterBackground;
        private static Sprite S_EdgeBackground;
        private static Sprite S_CornerBackground;

        private readonly Color _emptyColor = new Color(0.96f, 0.82f, 0.58f, 0.92f);
        private readonly Color _highlightColor = new Color(1f, 0.96f, 0.72f, 1f);
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
            ensureReferences();
            applySlotBackground(null);
        }

        // 绑定槽位数据
        public void Bind(CookSlotData slotData)
        {
            ensureReferences();

            _hasMaterial = slotData != null && slotData.HasMaterial;
            CookMaterialData material = slotData?.Material;
            _materialData = material;

            applySlotBackground(slotData);

            if (_imgIcon != null)
            {
                _imgIcon.sprite = material?.Icon;
                _imgIcon.enabled = material?.Icon != null;
                CookMaterialIconVisual.Apply(_imgIcon, new Vector2(80f, 80f), _imgIcon.rectTransform);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            _view?.HideItemTooltip();
            if (_view == null || eventData.pointerDrag == null) return;

            CookMaterialItem materialItem = eventData.pointerDrag.GetComponent<CookMaterialItem>();
            if (materialItem != null && _view.TryPlaceMaterial(materialItem, _slotIndex))
                return;

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

            bool isIconGenerated = transform.Find("Img_Icon") == null;
            _imgBackground = getOrCreateBackgroundImage(transform, _emptyColor);
            _imgIcon = getOrCreateImage("Img_Icon", transform, Color.white, isIconGenerated);
            removeGeneratedParameterTexts();

            if (_imgBackground.transform != transform)
                setupChildRect(_imgBackground.rectTransform, Vector2.zero, Vector2.one);

            _imgBackground.type = Image.Type.Simple;
            _imgBackground.preserveAspect = true;
            _imgBackground.raycastTarget = true;
            if (isIconGenerated)
            {
                setupChildRect(_imgIcon.rectTransform, new Vector2(0.22f, 0.3f), new Vector2(0.78f, 0.78f));
                _imgIcon.preserveAspect = true;
                _imgIcon.raycastTarget = false;
            }

            applySlotBackground(null);
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
            _dragIconRect.localRotation = Quaternion.identity;

            Image dragImage = _dragIconObject.GetComponent<Image>();
            dragImage.sprite = sprite;
            dragImage.preserveAspect = true;
            dragImage.raycastTarget = false;
            CookMaterialIconVisual.Apply(dragImage, _dragIconRect.sizeDelta);

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

        private static Image getOrCreateImage(string childName, Transform parent, Color color, bool applyDefaultColor = true)
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

            if (applyDefaultColor)
                image.color = color;

            return image;
        }

        // 获取槽位背景图层，优先复用槽位自身 Image 以兼容预制体编辑
        private static Image getOrCreateBackgroundImage(Transform parent, Color color)
        {
            Transform child = parent.Find("Img_Background");
            if (child != null)
                return getOrCreateImage("Img_Background", parent, color);

            Image image = parent.GetComponent<Image>();
            if (image == null)
                image = parent.gameObject.AddComponent<Image>();

            image.color = color;
            return image;
        }

        private static void setupChildRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rectTransform == null) return;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        // 清理旧版本自动生成的槽位参数文本
        private void removeGeneratedParameterTexts()
        {
            removeChildIfExists("Txt_Order");
            removeChildIfExists("Txt_Enchant");
            removeChildIfExists("Txt_Name");
            removeChildIfExists("Txt_Value");
        }

        // 删除指定子节点，兼容运行时与编辑态初始化
        private void removeChildIfExists(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null) return;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        // 应用当前槽位类型对应的九宫格背景
        private void applySlotBackground(CookSlotData slotData)
        {
            if (_imgBackground == null) return;

            CookSlotType slotType = getSlotType(slotData);
            Sprite background = getSlotBackgroundSprite(slotType);
            _imgBackground.sprite = background;
            _imgBackground.preserveAspect = true;
            _imgBackground.type = Image.Type.Simple;

            _currentEmptyColor = background == null ? getFallbackColor(slotType) : Color.white;
            _imgBackground.color = _currentEmptyColor;
        }

        // 获取槽位类型，初始化数据未绑定时用索引兜底
        private CookSlotType getSlotType(CookSlotData slotData)
        {
            return slotData == null ? resolveSlotTypeByIndex(_slotIndex) : slotData.SlotType;
        }

        // 根据九宫格索引解析槽位类型
        private static CookSlotType resolveSlotTypeByIndex(int slotIndex)
        {
            if (slotIndex == 0)
                return CookSlotType.Center;

            if (slotIndex >= 1 && slotIndex <= 4)
                return CookSlotType.Edge;

            return CookSlotType.Corner;
        }

        // 根据槽位类型获取背景图
        private static Sprite getSlotBackgroundSprite(CookSlotType slotType)
        {
            switch (slotType)
            {
                case CookSlotType.Center:
                    return S_CenterBackground ??= ArtAssetLoader.LoadSprite(SLOT_CENTER_SPRITE, false);
                case CookSlotType.Edge:
                    return S_EdgeBackground ??= ArtAssetLoader.LoadSprite(SLOT_EDGE_SPRITE, false);
                default:
                    return S_CornerBackground ??= ArtAssetLoader.LoadSprite(SLOT_CORNER_SPRITE, false);
            }
        }

        // 资源未登记时保留旧底色兜底，避免槽位不可见
        private Color getFallbackColor(CookSlotType slotType)
        {
            return slotType switch
            {
                CookSlotType.Center => _centerColor,
                CookSlotType.Edge => _edgeColor,
                CookSlotType.Corner => _cornerColor,
                _ => _emptyColor
            };
        }
    }
}
