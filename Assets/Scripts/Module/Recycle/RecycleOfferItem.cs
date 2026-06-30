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
using UnityEngine.UI;

namespace Module.Recycle
{
    // 回收材料格子，负责展示材料、选中状态与卖出动画
    public class RecycleOfferItem : MonoBehaviour
    {
        private Image _imgIcon;
        private Image _imgSelected;
        private TextMeshProUGUI _txtName;
        private TextMeshProUGUI _txtPrice;
        private Button _button;
        private CanvasGroup _canvasGroup;
        private RecycleOfferData _data;
        private Action<RecycleOfferData, RecycleOfferItem> _onClick;
        private Tween _scaleTween;

        public RecycleOfferData Data => _data;

        // 绑定材料数据和点击回调
        public void Bind(RecycleOfferData data, Action<RecycleOfferData, RecycleOfferItem> onClick)
        {
            ensureReferences();

            _data = data;
            _onClick = onClick;
            _canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one;

            if (_txtName != null) _txtName.text = data != null ? data.name : string.Empty;
            if (_txtPrice != null) _txtPrice.text = data != null ? data.price.ToString() : "0";

            if (_imgIcon != null)
            {
                Sprite sprite = data == null || string.IsNullOrEmpty(data.iconPath)
                    ? null
                    : ArtAssetLoader.LoadSprite(data.iconPath, false);
                _imgIcon.sprite = sprite;
                _imgIcon.enabled = true;
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
            if (_imgSelected != null)
                _imgSelected.enabled = selected;

            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(selected ? 1.08f : 1f, 0.12f).SetEase(Ease.OutBack);
        }

        // 播放卖出后的缩小淡出效果
        public void PlaySold(Action onComplete)
        {
            ensureReferences();
            if (_button != null) _button.interactable = false;

            _scaleTween?.Kill();
            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOScale(0f, 0.28f).SetEase(Ease.InBack));
            sequence.Join(_canvasGroup.DOFade(0f, 0.24f));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        private void handleClick()
        {
            if (_data == null) return;
            _onClick?.Invoke(_data, this);
        }

        private void ensureReferences()
        {
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
            if (_imgSelected == null)
            {
                Transform selected = transform.Find("Img_Selected");
                if (selected != null) _imgSelected = selected.GetComponent<Image>();
            }
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
    }
}
