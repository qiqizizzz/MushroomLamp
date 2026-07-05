/*
* ┌──────────────────────────────────┐
* │  描    述: 材料三选一弹层视图（半透明遮罩 + 居中候选材料卡）
* │  类    名: MaterialPickView.cs
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common;
using Common.Defines;
using Common.UI;
using DG.Tweening;
using Module.Cook;
using Module.Item;
using Module.Material;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.MaterialPick
{
    public class MaterialPickView : BaseView, IMaterialPickCardHost
    {
        private const string ITEM_TOOLTIP_PATH = "UI/Cook/ItemTooltip";
        private static readonly Vector2 TooltipOffset = new Vector2(18f, -18f);
        private const float CardDisplayScale = 1.5f;
        private const float OpenDuration = 0.35f;
        private const float CloseDuration = 0.28f;
        private const float CardSpacing = 48f;
        private const float OverlayTargetAlpha = 0.9f;

        private Image _overlayImage;
        private CanvasGroup _overlayGroup;
        private RectTransform _panelRoot;
        private TextMeshProUGUI _txtTitle;
        private RectTransform _cardsRoot;
        private HorizontalLayoutGroup _cardsLayout;
        private RectTransform _cardTemplate;

        private readonly List<MaterialPickCardItem> _cardItems = new();
        private readonly List<CookMaterialData> _previewMaterials = new();
        private ItemTooltip _itemTooltip;
        private RectTransform _tooltipCanvasRect;
        private object _tooltipOwner;
        private Sequence _panelSequence;
        private Tween _overlayTween;
        private bool _isClosing;
        private bool _interactionReady;

        public override void InitUI()
        {
            ensureRootLayout();
            buildHierarchy();
        }

        public override void Open(params object[] args)
        {
            SetVisible(true);
            _isClosing = false;
            _interactionReady = false;

            if (args == null || args.Length == 0 || args[0] is not MaterialPickModel model)
                return;

            refresh(model);
            playOpenAnimation();
        }

        public override void Close(params object[] args)
        {
            killAnimations();
            HideCardTooltip();
            clearCards();
            SetVisible(false);
        }

        public void PlayCloseAnimation(Action onComplete)
        {
            if (_isClosing)
                return;

            _isClosing = true;
            _interactionReady = false;
            setCardsInteractable(false);
            HideCardTooltip();
            killAnimations();

            if (_panelRoot != null)
            {
                _panelSequence = DOTween.Sequence()
                    .Append(_panelRoot.DOScale(Vector3.zero, CloseDuration).SetEase(Ease.InBack));
            }

            if (_overlayGroup != null)
            {
                _overlayTween = _overlayGroup
                    .DOFade(0f, CloseDuration)
                    .SetEase(Ease.OutQuad);
            }

            float duration = Mathf.Max(CloseDuration, _panelSequence?.Duration() ?? 0f);
            DOVirtual.DelayedCall(duration, () => onComplete?.Invoke()).SetTarget(this);
        }

        public void ShowCardTooltip(object owner, CookMaterialData materialData, Vector2 screenPosition)
        {
            if (!ensureItemTooltip()) return;

            _tooltipOwner = owner;
            _itemTooltip.transform.SetAsLastSibling();
            _itemTooltip.Bind(materialData);
            MoveCardTooltip(screenPosition);
        }

        public void MoveCardTooltip(Vector2 screenPosition)
        {
            if (_itemTooltip == null) return;
            _itemTooltip.SetScreenPosition(screenPosition, _tooltipCanvasRect, TooltipOffset);
        }

        public void HideCardTooltip(object owner = null)
        {
            if (owner != null && _tooltipOwner != owner)
                return;

            _tooltipOwner = null;
            _itemTooltip?.Hide();
        }

        private void refresh(MaterialPickModel model)
        {
            if (_txtTitle != null)
                _txtTitle.text = string.IsNullOrWhiteSpace(model.title) ? "幸运三选一" : model.title;

            clearCards();
            _previewMaterials.Clear();

            IReadOnlyList<MaterialJsonData> candidates = model.candidates;
            if (candidates == null || candidates.Count == 0)
                return;

            Vector2 cardSize = _cardTemplate != null
                ? _cardTemplate.sizeDelta
                : MaterialPickCardItem.DefaultCardSize * CardDisplayScale;

            for (int i = 0; i < candidates.Count; i++)
            {
                MaterialJsonData cfg = candidates[i];
                if (cfg == null) continue;

                CookMaterialData preview = createPreviewMaterial(i, cfg);
                _previewMaterials.Add(preview);

                GameObject cardObj = createCardObject(i);
                cardObj.transform.SetParent(_cardsRoot, false);
                cardObj.SetActive(true);

                MaterialPickCardItem cardItem = cardObj.GetComponent<MaterialPickCardItem>();
                if (cardItem == null)
                    cardItem = cardObj.AddComponent<MaterialPickCardItem>();

                int capturedIndex = i;
                cardItem.Setup(
                    preview,
                    this,
                    () => onCardClicked(capturedIndex),
                    cardSize.x,
                    cardSize.y);

                _cardItems.Add(cardItem);
            }

            if (_cardsLayout != null)
                _cardsLayout.spacing = CardSpacing;

            resetPanelForOpen();
            setCardsInteractable(false);
        }

        private void onCardClicked(int index)
        {
            if (!_interactionReady || _isClosing) return;
            ApplyFunc(EventDefines.MaterialPickSelect, index);
        }

        private void playOpenAnimation()
        {
            killAnimations();
            resetPanelForOpen();

            if (_overlayGroup != null)
            {
                _overlayGroup.alpha = 0f;
                _overlayTween = _overlayGroup
                    .DOFade(OverlayTargetAlpha, OpenDuration)
                    .SetEase(Ease.OutQuad);
            }

            if (_panelRoot != null)
            {
                _panelRoot.localScale = Vector3.zero;
                _panelSequence = DOTween.Sequence()
                    .Append(_panelRoot.DOScale(Vector3.one, OpenDuration).SetEase(Ease.OutBack))
                    .OnComplete(() =>
                    {
                        _interactionReady = true;
                        setCardsInteractable(true);
                    });
            }
            else
            {
                _interactionReady = true;
                setCardsInteractable(true);
            }
        }

        private void resetPanelForOpen()
        {
            if (_panelRoot != null)
                _panelRoot.localScale = Vector3.zero;

            if (_overlayGroup != null)
                _overlayGroup.alpha = 0f;
        }

        private void setCardsInteractable(bool interactable)
        {
            for (int i = 0; i < _cardItems.Count; i++)
            {
                if (_cardItems[i] != null)
                    _cardItems[i].SetInteractable(interactable);
            }
        }

        private void clearCards()
        {
            HideCardTooltip();
            for (int i = 0; i < _cardItems.Count; i++)
            {
                if (_cardItems[i] != null)
                    Destroy(_cardItems[i].gameObject);
            }

            _cardItems.Clear();
            _previewMaterials.Clear();
        }

        private void killAnimations()
        {
            _panelSequence?.Kill();
            _panelSequence = null;
            _overlayTween?.Kill();
            _overlayTween = null;
            DOTween.Kill(this);
        }

        private GameObject createCardObject(int index)
        {
            if (_cardTemplate != null)
            {
                GameObject clone = Instantiate(_cardTemplate.gameObject, _cardsRoot);
                clone.name = $"PickCard_{index}";
                return clone;
            }

            return new GameObject($"PickCard_{index}", typeof(RectTransform));
        }

        private static CookMaterialData createPreviewMaterial(int index, MaterialJsonData config)
        {
            Sprite icon = ArtAssetLoader.LoadSprite(config.iconPath, logOnFail: false);
            return new CookMaterialData(-1000 - index, config, icon);
        }

        private bool ensureItemTooltip()
        {
            if (_itemTooltip != null)
                return true;

            Transform parent = transform;
            GameObject tooltipObj = ResManager.Instantiate(ITEM_TOOLTIP_PATH, parent);
            if (tooltipObj == null) return false;

            _itemTooltip = tooltipObj.GetComponent<ItemTooltip>();
            if (_itemTooltip == null)
                _itemTooltip = tooltipObj.AddComponent<ItemTooltip>();

            tooltipObj.name = "MaterialPickItemTooltip";
            tooltipObj.transform.SetAsLastSibling();
            _tooltipCanvasRect = parent as RectTransform;
            if (_tooltipCanvasRect == null)
                _tooltipCanvasRect = tooltipObj.GetComponentInParent<Canvas>()?.transform as RectTransform;

            _itemTooltip.SetFontAsset(UIFontHelper.JingnanFont);
            _itemTooltip.Hide();
            return _itemTooltip != null;
        }

        private void ensureRootLayout()
        {
            RectTransform root = transform as RectTransform;
            if (root == null) return;

            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.localScale = Vector3.one;
        }

        private void buildHierarchy()
        {
            if (Find("Overlay") != null)
                bindExistingNodes();
            else
                buildRuntimeHierarchy();
        }

        private void bindExistingNodes()
        {
            _overlayImage = Find<Image>("Overlay");
            _overlayGroup = Find<CanvasGroup>("Overlay");
            if (_overlayGroup == null && _overlayImage != null)
                _overlayGroup = _overlayImage.gameObject.AddComponent<CanvasGroup>();

            if (_overlayImage != null)
                _overlayImage.color = new Color(0f, 0f, 0f, 1f);

            _panelRoot = Find<RectTransform>("Panel");
            _txtTitle = Find<TextMeshProUGUI>("Panel/Text_Title");
            _cardsRoot = Find<RectTransform>("Panel/CardsRoot");
            _cardsLayout = _cardsRoot != null ? _cardsRoot.GetComponent<HorizontalLayoutGroup>() : null;

            Transform templateTf = _cardsRoot != null ? _cardsRoot.Find("CardTemplate") : null;
            _cardTemplate = templateTf as RectTransform;
        }

        private void buildRuntimeHierarchy()
        {
            GameObject overlayObj = createStretchChild("Overlay");
            _overlayImage = overlayObj.AddComponent<Image>();
            _overlayImage.color = new Color(0f, 0f, 0f, 1f);
            _overlayImage.raycastTarget = true;
            _overlayGroup = overlayObj.AddComponent<CanvasGroup>();
            _overlayGroup.alpha = 0f;
            _overlayGroup.blocksRaycasts = true;

            GameObject panelObj = new GameObject("Panel", typeof(RectTransform));
            panelObj.transform.SetParent(transform, false);
            _panelRoot = panelObj.GetComponent<RectTransform>();
            _panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRoot.pivot = new Vector2(0.5f, 0.5f);
            _panelRoot.anchoredPosition = Vector2.zero;
            _panelRoot.localScale = Vector3.zero;

            VerticalLayoutGroup panelLayout = panelObj.AddComponent<VerticalLayoutGroup>();
            panelLayout.childAlignment = TextAnchor.MiddleCenter;
            panelLayout.spacing = 36f;
            panelLayout.padding = new RectOffset(24, 24, 24, 24);
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = false;
            panelLayout.childForceExpandHeight = false;

            GameObject titleObj = new GameObject("Text_Title", typeof(RectTransform));
            titleObj.transform.SetParent(panelObj.transform, false);
            _txtTitle = titleObj.AddComponent<TextMeshProUGUI>();
            _txtTitle.alignment = TextAlignmentOptions.Center;
            _txtTitle.fontSize = 42f;
            _txtTitle.color = Color.white;
            UIFontHelper.ApplyChineseFont(_txtTitle, UIFontHelper.JingnanFont);

            LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 56f;

            GameObject cardsObj = new GameObject("CardsRoot", typeof(RectTransform));
            cardsObj.transform.SetParent(panelObj.transform, false);
            _cardsRoot = cardsObj.GetComponent<RectTransform>();
            _cardsLayout = cardsObj.AddComponent<HorizontalLayoutGroup>();
            _cardsLayout.childAlignment = TextAnchor.MiddleCenter;
            _cardsLayout.spacing = CardSpacing;
            _cardsLayout.childControlWidth = false;
            _cardsLayout.childControlHeight = false;
            _cardsLayout.childForceExpandWidth = false;
            _cardsLayout.childForceExpandHeight = false;

            ContentSizeFitter cardsFitter = cardsObj.AddComponent<ContentSizeFitter>();
            cardsFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            cardsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ContentSizeFitter panelFitter = panelObj.AddComponent<ContentSizeFitter>();
            panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private GameObject createStretchChild(string name)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(transform, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return obj;
        }
    }
}
