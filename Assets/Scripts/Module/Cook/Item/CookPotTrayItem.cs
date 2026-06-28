/*
* ┌──────────────────────────────────┐
* │  描    述: Pot 暂存槽 UI 项，接收法阵材料并支持槽间换位
* │  类    名: CookPotTrayItem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using DG.Tweening;
using Module.Cook;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.View
{
    // Pot 暂存槽：接收法阵槽拖来的材料；自身可拖出与其它暂存槽换位
    public class CookPotTrayItem : BaseItem,
        IDropHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private readonly Color _emptyColor = new Color(0.90f, 0.74f, 0.50f, 0.85f);
        private readonly Color _highlightColor = new Color(0.99f, 0.90f, 0.42f, 1f);
        private readonly Color _occupiedColor = new Color(0.80f, 0.56f, 0.34f, 1f);

        private CookView _view;
        private int _trayIndex;
        private bool _hasMaterial;
        private bool _dropAccepted;
        private Tweener _flashTween;

        private Image _imgBackground;
        private Image _imgFlash;
        private Image _imgIcon;
        private TextMeshProUGUI _txtName;
        private TextMeshProUGUI _txtScore;
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
            applyFont(view == null ? null : view.GetFontAsset());
        }

        // 绑定暂存槽材料；isFull=整个 PotTray 已集满
        public void Bind(CookMaterialData material, bool isFull = false, float trayPreviewScore = 0f)
        {
            ensureReferences();

            _hasMaterial = material != null;

            if (_imgIcon != null)
            {
                _imgIcon.sprite = material?.Icon;
                _imgIcon.enabled = material?.Icon != null;
            }

            if (_txtName != null)
                _txtName.text = material?.Config?.name ?? "空";

            if (_imgBackground != null)
                _imgBackground.color = _hasMaterial ? _occupiedColor : _emptyColor;

            if (isFull && _hasMaterial)
            {
                startFlash();
                if (_txtScore != null)
                {
                    _txtScore.gameObject.SetActive(true);
                    _txtScore.text = $"+{trayPreviewScore:0.#}";
                }
            }
            else
            {
                stopFlash();
                if (_txtScore != null)
                    _txtScore.gameObject.SetActive(false);
            }
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
            if (_view == null || eventData.pointerDrag == null) return;

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
                _imgBackground.color = _highlightColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_imgBackground != null && !_hasMaterial)
                _imgBackground.color = _emptyColor;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_hasMaterial || _view == null) return;
            if (_imgIcon == null || _imgIcon.sprite == null) return;

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
            _txtName = getOrCreateText("Txt_Name", 18);
            _txtScore = getOrCreateText("Txt_Score", 22);

            _txtScore.color = new Color(0.1f, 0.55f, 0.1f, 1f);
            _txtScore.fontStyle = FontStyles.Bold;

            setupRect(_imgBackground.rectTransform, Vector2.zero, Vector2.one);
            setupRect(_imgFlash.rectTransform, Vector2.zero, Vector2.one);
            setupRect(_imgIcon.rectTransform, new Vector2(0.18f, 0.28f), new Vector2(0.82f, 0.85f));
            setupRect(_txtName.rectTransform, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.24f));
            setupRect(_txtScore.rectTransform, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.98f));

            _imgFlash.raycastTarget = false;
            _imgFlash.gameObject.SetActive(false);
            _txtScore.gameObject.SetActive(false);
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

        private TextMeshProUGUI getOrCreateText(string childName, float fontSize)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                GameObject obj = new GameObject(childName, typeof(RectTransform));
                obj.transform.SetParent(transform, false);
                child = obj.transform;
            }

            TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
            if (text == null)
                text = child.gameObject.AddComponent<TextMeshProUGUI>();

            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.16f, 0.09f, 0.05f, 1f);
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            return text;
        }

        private void applyFont(TMP_FontAsset fontAsset)
        {
            if (fontAsset != null && _txtName != null)
                _txtName.font = fontAsset;
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
            if (_txtName != null)
                _txtName.enabled = isVisible;
        }

        private static void setupRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
