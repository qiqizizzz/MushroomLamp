/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪材料 UI 项，负责展示材料并处理拖拽输入
* │  类    名: CookMaterialItem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using DG.Tweening;
using Module.Cook;
using MVC.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.View
{
    // 烹饪材料 UI 项，负责展示材料并处理拖拽输入
    public class CookMaterialItem : BaseItem,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float CardWidth = 160f;
        private const float CardHeight = 192f;

        // 悬停效果参数（缩放可在此微调）
        private const float HoverScale = 1.2f;      // 悬停目标缩放
        private const float ScaleLerpSpeed = 12f;   // 缩放过渡速度
        private const float OutlineWidth = 3f;      // 描边宽度（像素）
        private const string SfxHandDragSelect = "sfx_ingame_select";

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

        private bool _isDragging;
        private bool _isPointerInside;
        private bool _interactable = true;   // 动画期间锁住拖拽
        private float _targetScale = 1f;
        private UnityEngine.Material _outlineMaterial;
        private Tween _flyTween;

        // 供 CookView 编排飞行动画用
        public RectTransform Rect { get { ensureReferences(); return _rectTransform; } }
        public CanvasGroup Group { get { ensureReferences(); return _canvasGroup; } }
        public static Vector2 CardSize => new Vector2(CardWidth, CardHeight);

        // 动画期间锁住交互（禁止拖拽/悬停响应）
        public void SetInteractable(bool value)
        {
            _interactable = value;
            if (!value)
            {
                if (_isPointerInside)
                {
                    _isPointerInside = false;
                    _view?.HideItemTooltip(this);
                }
                _targetScale = 1f;
                setOutline(false);
            }
        }

        // 杀掉正在进行的飞行动画
        public void KillFlyTween()
        {
            if (_flyTween != null) { _flyTween.Kill(); _flyTween = null; }
        }

        // 记录飞行 Tween（CookView 创建后回传，便于统一清理）
        public void SetFlyTween(Tween tween)
        {
            KillFlyTween();
            _flyTween = tween;
        }

        private Image _imgBackground;
        private Image _imgIcon;

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

            // 手牌表现层不显示任何文本，只展示图标
            if (_imgIcon != null)
            {
                _imgIcon.sprite = materialData?.Icon;
                _imgIcon.enabled = materialData?.Icon != null;
                _imgIcon.preserveAspect = true;
            }
        }

        // 设置材料卡在当前区域的显示尺寸
        public void SetDisplaySize(float width, float height)
        {
            _displayWidth = Mathf.Max(1f, width);
            _displayHeight = Mathf.Max(1f, height);
            applyDisplaySize(_displayWidth, _displayHeight);
        }

        // 每帧检测悬停（使用未放大的逻辑尺寸，避免 HoverScale 导致邻牌切换失效）
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

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_materialData == null || _view == null || !_interactable) return;

            KillFlyTween();
            _view.HideItemTooltip(this);
            _isPointerInside = false;
            _isDragging = true;
            _targetScale = 1f;
            setOutline(false);

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

            GameApp.SoundManager?.PlayEffect(SfxHandDragSelect);
        }

        public void OnDrag(PointerEventData eventData)
        {
            moveToPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            if (_dropAccepted) return;

            restoreToOriginalParent();
        }

        // 标记拖拽已被目标区域接收
        public void MarkDropAccepted()
        {
            _dropAccepted = true;
        }

        // 接受拖拽放置。注意：本 item 来自对象池，绝不能销毁（会留野指针导致 MissingReference）；
        // 这里只标记接收并隐藏，放置后 model 数据变化会触发 refreshHand 统一复用/布局
        public void AcceptDropAndDestroy()
        {
            _dropAccepted = true;
            _isDragging = false;
            KillFlyTween();
            // 收回手牌容器并隐藏，等待 refreshHand 复用
            if (_view != null)
            {
                Transform handContent = _view.GetHandContent();
                if (handContent != null) transform.SetParent(handContent, false);
            }
            if (_canvasGroup != null) { _canvasGroup.alpha = 1f; _canvasGroup.blocksRaycasts = true; }
            gameObject.SetActive(false);
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

            // 背景不再高亮，设为完全透明（保留节点以兼容布局）
            _imgBackground = getOrCreateImage("Img_Background", transform, new Color(0f, 0f, 0f, 0f));
            _imgBackground.raycastTarget = true;   // 仍负责接收悬停/点击
            _imgIcon = getOrCreateImage("Img_Icon", transform, Color.white);
            _imgIcon.preserveAspect = true;
            _imgIcon.raycastTarget = false;

            setupChildRect(_imgBackground.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // 无文本，图标铺满整张卡
            setupChildRect(_imgIcon.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        // 切换图标描边（悬停时贴合轮廓的白描边）
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
                _imgIcon.material = null;   // 还原默认材质
            }
        }

        protected override void OnDestroy()
        {
            KillFlyTween();
            _view?.HideItemTooltip(this);
            if (_outlineMaterial != null)
            {
                Destroy(_outlineMaterial);
                _outlineMaterial = null;
            }
            base.OnDestroy();
        }

        // 主动检测鼠标是否经过材料卡，避免 Pointer 事件被子节点或布局组件截断
        private void updatePointerHover()
        {
            if (_view == null || _materialData == null || _isDragging || !isActiveAndEnabled)
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
                    _view.ShowItemTooltip(this, _materialData, screenPosition);
                    return;
                }

                _view.MoveItemTooltip(screenPosition);
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
            _view?.HideItemTooltip(this);
        }

        // 使用卡牌逻辑尺寸做命中检测，不受 HoverScale 放大影响
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

        // 获取当前 UI 检测需要的相机
        private Camera resolveHoverCamera()
        {
            Canvas canvas = _view == null ? null : _view.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
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
