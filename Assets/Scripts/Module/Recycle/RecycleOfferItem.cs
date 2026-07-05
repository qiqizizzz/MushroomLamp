/*
* ┌──────────────────────────────────┐
* │  描    述: 回收材料格子，负责展示材料、选中状态与卖出动画
* │  类    名: RecycleOfferItem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Common;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.Recycle
{
    // 回收材料格子，负责展示材料、选中状态与卖出动画
    public class RecycleOfferItem : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
    {
        private const float HOVER_SCALE = 1.12f;
        private const float SELECTED_SCALE = 1.18f;
        private const float SCALE_DURATION = 0.12f;
        private const float OUTLINE_WIDTH = 3f;

        private RectTransform _rectTransform;
        private Image _imgIcon;
        private Image _imgSelected;
        private TextMeshProUGUI _txtName;
        private TextMeshProUGUI _txtPrice;
        private Button _button;
        private CanvasGroup _canvasGroup;
        private RecycleOfferData _data;
        private RecycleView _view;
        private Action<RecycleOfferData, RecycleOfferItem> _onClick;
        private Tween _scaleTween;
        private UnityEngine.Material _outlineMaterial;
        private bool _isSelected;
        private bool _isPointerInside;
        private bool _isPlayingSold;

        public RecycleOfferData Data => _data;

        // 绑定材料数据和点击回调
        public void Bind(RecycleOfferData data, RecycleView view, Action<RecycleOfferData, RecycleOfferItem> onClick)
        {
            ensureReferences();

            _data = data;
            _view = view;
            _onClick = onClick;
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            transform.localScale = Vector3.one;
            _isPointerInside = false;
            _isPlayingSold = false;

            if (_txtName != null)
            {
                _txtName.text = data != null ? data.name : string.Empty;
                _txtName.gameObject.SetActive(false);
            }
            if (_txtPrice != null)
                _txtPrice.text = data != null ? $"+{data.price}" : "+0";

            if (_imgIcon != null)
            {
                Sprite sprite = data == null || string.IsNullOrEmpty(data.iconPath)
                    ? null
                    : ArtAssetLoader.LoadSprite(data.iconPath, false);
                _imgIcon.sprite = sprite;
                _imgIcon.enabled = sprite != null;
                _imgIcon.preserveAspect = true;
            }

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(handleClick);
                _button.interactable = data != null;
            }

            SetSelected(false);
        }

        // 设置选中视觉
        public void SetSelected(bool selected)
        {
            ensureReferences();
            if (_isPlayingSold) return;

            _isSelected = selected;
            if (_imgSelected != null)
                _imgSelected.enabled = false;

            setOutline(false);
            tweenToCurrentScale();
        }

        // 播放卖出后的缩小淡出效果
        public void PlaySold(Action onComplete)
        {
            ensureReferences();
            _view?.HideRecycleTooltip(this);
            if (_button != null) _button.interactable = false;
            if (_canvasGroup != null) _canvasGroup.blocksRaycasts = false;

            _isPlayingSold = true;
            _scaleTween?.Kill();
            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOScale(0f, 0.28f).SetEase(Ease.InBack));
            sequence.Join(_canvasGroup.DOFade(0f, 0.24f));
            sequence.OnComplete(() => onComplete?.Invoke());
            _scaleTween = sequence;
        }

        // 鼠标进入时放大材料图标
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_data == null || _isPlayingSold) return;

            _isPointerInside = true;
            setOutline(false);
            tweenToCurrentScale();
            _view?.ShowRecycleTooltip(this, _data, eventData.position);
        }

        // 鼠标移动时同步详情浮层位置
        public void OnPointerMove(PointerEventData eventData)
        {
            if (_data == null || _isPlayingSold) return;

            _view?.MoveRecycleTooltip(eventData.position);
        }

        // 鼠标离开时恢复材料图标
        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isPlayingSold) return;

            _isPointerInside = false;
            setOutline(false);
            tweenToCurrentScale();
            _view?.HideRecycleTooltip(this);
        }

        private void handleClick()
        {
            if (_data == null) return;
            _onClick?.Invoke(_data, this);
        }

        private void ensureReferences()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_button == null) _button = GetComponent<Button>();
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (_imgIcon == null)
            {
                Transform icon = transform.Find("Img_Icon");
                if (icon != null) _imgIcon = icon.GetComponent<Image>();
            }
            if (_imgIcon != null)
            {
                _imgIcon.raycastTarget = false;
                _imgIcon.preserveAspect = true;
            }
            if (_imgSelected == null)
            {
                Transform selected = transform.Find("Img_Selected");
                if (selected != null) _imgSelected = selected.GetComponent<Image>();
            }
            if (_imgSelected != null)
                _imgSelected.raycastTarget = false;
            if (_txtName == null)
            {
                Transform name = transform.Find("Txt_Name");
                if (name != null) _txtName = name.GetComponent<TextMeshProUGUI>();
            }
            if (_txtPrice == null)
            {
                Transform price = transform.Find("Txt_Price");
                if (price != null) _txtPrice = price.GetComponent<TextMeshProUGUI>();
            }
        }

        private void OnDestroy()
        {
            _view?.HideRecycleTooltip(this);
            _scaleTween?.Kill();
            if (_outlineMaterial != null)
            {
                Destroy(_outlineMaterial);
                _outlineMaterial = null;
            }
        }

        // 根据选中和悬停状态刷新缩放
        private void tweenToCurrentScale()
        {
            float targetScale = _isSelected ? SELECTED_SCALE : (_isPointerInside ? HOVER_SCALE : 1f);
            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(targetScale, SCALE_DURATION).SetEase(Ease.OutBack);
        }

        // 切换材料图标描边
        private void setOutline(bool enabled)
        {
            if (_imgIcon == null) return;

            if (!enabled)
            {
                _imgIcon.material = null;
                return;
            }

            if (_outlineMaterial == null)
            {
                Shader shader = Shader.Find("UI/Outline");
                if (shader == null) return;

                _outlineMaterial = new UnityEngine.Material(shader);
                _outlineMaterial.SetColor("_OutlineColor", Color.white);
                _outlineMaterial.SetFloat("_OutlineWidth", OUTLINE_WIDTH);
            }

            _outlineMaterial.SetFloat("_OutlineEnabled", 1f);
            _imgIcon.material = _outlineMaterial;
        }
    }
}
