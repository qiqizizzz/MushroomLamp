/*
* ┌──────────────────────────────────┐
* │  描    述: Pot 暂存槽 UI 项，接收法阵材料并支持槽间换位
* │  类    名: CookPotTrayItem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using DG.Tweening;
using Common;
using Module.Cook;
using MVC.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.View
{
    // Pot 暂存槽：接收法阵槽拖来的材料；自身可拖出与其它暂存槽换位
    public class CookPotTrayItem : BaseItem,
        IDropHandler, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const string TRAY_SLOT_SPRITE = "Art/CookView/Pot/摆放小框";

        private static Sprite S_TraySlotSprite;
        private static readonly Vector2 S_TrayIconAnchorMin = new Vector2(0.02f, 0.04f);
        private static readonly Vector2 S_TrayIconAnchorMax = new Vector2(0.98f, 0.96f);

        private readonly Color _emptyColor = Color.white;
        private readonly Color _highlightColor = new Color(1f, 0.94f, 0.66f, 1f);

        private CookView _view;
        private int _trayIndex;
        private bool _hasMaterial;
        private CookMaterialData _materialData;
        private bool _dropAccepted;
        private Tweener _flashTween;

        private Image _imgBackground;
        private Image _imgFlash;
        private Image _imgIcon;
        private GameObject _dragIconObject;
        private RectTransform _dragIconRect;

        public int TrayIndex => _trayIndex;
        public bool HasMaterial => _hasMaterial;

        protected override void OnAwake()
        {
            ensureReferences();
        }

        public void Init(CookView view, int trayIndex)
        {
            _view = view;
            _trayIndex = trayIndex;
        }

        // 绑定暂存槽材料；isFull=整个 PotTray 已集满
        public void Bind(CookMaterialData material, bool isFull = false, float trayPreviewScore = 0f)
        {
            ensureReferences();

            _hasMaterial = material != null;
            _materialData = material;

            if (_imgIcon != null)
            {
                _imgIcon.sprite = material?.Icon;
                _imgIcon.enabled = material?.Icon != null;
            }

            if (_imgBackground != null)
                _imgBackground.color = _imgBackground.sprite == null ? Color.clear : _emptyColor;

            if (isFull && _hasMaterial)
                startFlash();
            else
                stopFlash();
        }

        // 投入锅中飞行动画：取图标与起点（DragRoot 局部坐标）
        public bool TryGetSubmitFlyData(RectTransform dragRoot, out Sprite sprite, out Vector2 anchoredPos, out Vector2 size)
        {
            sprite = null;
            anchoredPos = Vector2.zero;
            size = new Vector2(92f, 92f);
            ensureReferences();

            if (!_hasMaterial || _imgIcon == null || _imgIcon.sprite == null || dragRoot == null)
                return false;

            sprite = _imgIcon.sprite;
            RectTransform iconRt = _imgIcon.rectTransform;
            if (iconRt.rect.size.sqrMagnitude > 0f)
                size = iconRt.rect.size;

            Vector3 worldCenter = iconRt.TransformPoint(iconRt.rect.center);
            Vector3 local = dragRoot.InverseTransformPoint(worldCenter);
            anchoredPos = new Vector2(local.x, local.y);
            return true;
        }

        // 飞行动画期间隐藏槽位上的图标，避免与飞行副本重叠
        public void HideIconForSubmitFly()
        {
            stopFlash();
            if (_imgIcon != null)
                _imgIcon.enabled = false;
        }

        private void startFlash()
        {
            if (_imgFlash == null) return;
            _imgFlash.gameObject.SetActive(true);
            _flashTween?.Kill();
            _flashTween = _imgFlash
                .DOFade(0.7f, 0.5f)
                .From(0f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void stopFlash()
        {
            _flashTween?.Kill();
            _flashTween = null;
            if (_imgFlash != null)
                _imgFlash.gameObject.SetActive(false);
        }

        protected override void OnDestroy()
        {
            stopFlash();
            base.OnDestroy();
        }

        public void OnDrop(PointerEventData eventData)
        {
            _view?.HideItemTooltip();
            if (_view == null || eventData.pointerDrag == null) return;

            CookMaterialItem materialItem = eventData.pointerDrag.GetComponent<CookMaterialItem>();
            if (materialItem != null)
            {
                _view.ShowTip("先把材料放入法阵并结束回合煮过后，再拖入锅中");
                return;
            }

            // 来自法阵槽：移到暂存槽
            CookSlotItem slotItem = eventData.pointerDrag.GetComponent<CookSlotItem>();
            if (slotItem != null && slotItem.HasMaterial)
            {
                if (_view.TryMoveSlotToPotTray(slotItem.SlotIndex, _trayIndex))
                    slotItem.MarkDropAccepted();
                return;
            }

            // 来自另一个暂存槽：换位
            CookPotTrayItem trayItem = eventData.pointerDrag.GetComponent<CookPotTrayItem>();
            if (trayItem != null && trayItem != this)
            {
                if (_view.TrySwapPotTray(trayItem.TrayIndex, _trayIndex))
                    trayItem.MarkDropAccepted();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_imgBackground != null && !_hasMaterial)
                _imgBackground.color = _imgBackground.sprite == null ? Color.clear : _highlightColor;

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
                _imgBackground.color = _imgBackground.sprite == null ? Color.clear : _emptyColor;

            _view?.HideItemTooltip();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_hasMaterial || _view == null) return;
            if (_imgIcon == null || _imgIcon.sprite == null) return;

            _view.HideItemTooltip();
            _dropAccepted = false;
            createDragIcon(_imgIcon.sprite);
            setVisualVisible(false);
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
                setVisualVisible(true);
            _dropAccepted = false;
        }

        public void MarkDropAccepted()
        {
            _dropAccepted = true;
        }

        private void ensureReferences()
        {
            _imgBackground = getOrCreateImage("Img_Background", _emptyColor);
            _imgFlash = getOrCreateImage("Img_Flash", new Color(1f, 0.95f, 0.3f, 0f));
            _imgIcon = getOrCreateImage("Img_Icon", Color.white);
            removeGeneratedPlaceholderTexts();

            setupRect(_imgBackground.rectTransform, Vector2.zero, Vector2.one);
            setupRect(_imgFlash.rectTransform, Vector2.zero, Vector2.one);
            setupRect(_imgIcon.rectTransform, S_TrayIconAnchorMin, S_TrayIconAnchorMax);

            _imgBackground.sprite = S_TraySlotSprite ??= ArtAssetLoader.LoadSprite(TRAY_SLOT_SPRITE, false);
            _imgBackground.color = _imgBackground.sprite == null ? Color.clear : _emptyColor;
            _imgBackground.type = Image.Type.Simple;
            _imgBackground.preserveAspect = true;
            _imgBackground.raycastTarget = true;
            _imgFlash.raycastTarget = false;
            _imgFlash.gameObject.SetActive(false);
            _imgIcon.preserveAspect = true;
            _imgIcon.raycastTarget = false;
            _imgBackground.transform.SetAsFirstSibling();
            _imgIcon.transform.SetAsLastSibling();
        }

        private Image getOrCreateImage(string childName, Color color)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                GameObject obj = new GameObject(childName, typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(transform, false);
                child = obj.transform;
            }

            Image image = child.GetComponent<Image>();
            if (image == null)
                image = child.gameObject.AddComponent<Image>();

            image.color = color;
            if (childName != "Img_Background")
                image.raycastTarget = false;

            return image;
        }

        private void createDragIcon(Sprite sprite)
        {
            destroyDragIcon();

            Transform dragRoot = _view.GetDragRoot();
            if (dragRoot == null) return;

            _dragIconObject = new GameObject("Dragging_TrayIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _dragIconObject.transform.SetParent(dragRoot, false);
            _dragIconObject.transform.SetAsLastSibling();

            _dragIconRect = _dragIconObject.GetComponent<RectTransform>();
            _dragIconRect.sizeDelta = _imgIcon.rectTransform.rect.size;
            if (_dragIconRect.sizeDelta.sqrMagnitude <= 0f)
                _dragIconRect.sizeDelta = new Vector2(92f, 92f);
            _dragIconRect.localScale = Vector3.one;

            Image dragImage = _dragIconObject.GetComponent<Image>();
            dragImage.sprite = sprite;
            dragImage.preserveAspect = true;
            dragImage.raycastTarget = false;

            CanvasGroup canvasGroup = _dragIconObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.9f;
        }

        private void destroyDragIcon()
        {
            if (_dragIconObject != null)
            {
                Destroy(_dragIconObject);
                _dragIconObject = null;
                _dragIconRect = null;
            }
        }

        private void moveDragIconToPointer(PointerEventData eventData)
        {
            if (_dragIconRect == null) return;

            Transform dragRoot = _view.GetDragRoot();
            RectTransform dragRootRect = dragRoot as RectTransform;
            if (dragRootRect == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragRootRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                _dragIconRect.anchoredPosition = localPoint;
            }
        }

        private void setVisualVisible(bool isVisible)
        {
            if (_imgIcon != null && _imgIcon.sprite != null)
                _imgIcon.enabled = isVisible;
        }

        private static void setupRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // 清理旧版本自动生成的暂存槽文字占位
        private void removeGeneratedPlaceholderTexts()
        {
            removeChildIfExists("Txt_Name");
            removeChildIfExists("Txt_Score");
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
    }
}
