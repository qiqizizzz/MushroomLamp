/*
* ┌──────────────────────────────────┐
* │  描    述: 21 点玩法视图
* │  类    名: BlackjackView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common.Defines;
using Common.UI;
using DG.Tweening;
using Module.MagicBoxBuff;
using Module.Material;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Blackjack
{
    public class BlackjackView : BaseView
    {
        private const float BubbleAnimDuration = 0.32f;
        private const float BubbleHideScale = 0.08f;

        private const float ItemHoverScale = 1.2f;
        private const float DealStagger = 0.08f;
        private const float DealDuration = 0.42f;

        private static readonly Color ItemNormalColor = Color.white;
        private static readonly Color ItemDisabledColor = new Color(0.78f, 0.78f, 0.78f, 0.55f);

        private enum ItemInteractionMode
        {
            DrawCard,
            PickBuff,
            PickMaterial
        }

        [Header("开场")]
        [SerializeField] private float _openCooldownSeconds = 1.5f;
        [Header("小牌布局")]
        [SerializeField] private float _layoutCardWidth = 140f;
        [SerializeField] private float _layoutSpacing = 20f;
        [Header("翻牌")]
        [SerializeField] private float _flipHalfDuration = 0.18f;

        private readonly List<Button> _itemButtons = new();
        private readonly List<UIButtonHoverItem> _itemHovers = new();
        private readonly List<TextMeshProUGUI> _itemLabels = new();
        private readonly List<CardSlot> _smallCardPool = new();
        private readonly List<CardSlot> _smallCards = new();

        private Button _btnBox;

        private CardSlot _bigCard;
        private RectTransform _smallCardsRoot;
        private TextMeshProUGUI _txtBottom;

        private BubbleAnimSlot _devilBubble;
        private BubbleAnimSlot _angelBubble;

        private BlackjackDialogSession _dialogSession;
        private TMP_FontAsset _fontTemplate;

        private Sequence _introSequence;
        private bool _interactionLocked;
        private Action _onIntroComplete;
        private readonly HashSet<int> _flippingCardIndices = new();
        private ItemInteractionMode _itemMode = ItemInteractionMode.DrawCard;
        private bool _buffSlotPicked;

        private class CardSlot
        {
            public GameObject root;
            public RectTransform rect;
            public TextMeshProUGUI point;
            public Image face;
            public Tween flipTween;
        }

        // 单侧气泡：从角色位置弹出 / 收回
        private class BubbleAnimSlot
        {
            public RectTransform bubble;
            public RectTransform character;
            public TextMeshProUGUI text;
            public Vector2 emitNormalized;
            public Vector3 restLocalPos;
            public Vector3 restLocalScale;
            public bool isShown;
            public bool isHiding;
            public string lastText = string.Empty;
            public Sequence tween;

            public void init(RectTransform bubbleRt, RectTransform characterRt, TextMeshProUGUI label, Vector2 emitNorm)
            {
                bubble = bubbleRt;
                character = characterRt;
                text = label;
                emitNormalized = emitNorm;

                if (bubble == null) return;

                restLocalPos = bubble.localPosition;
                restLocalScale = bubble.localScale;
                killTween();
                bubble.gameObject.SetActive(false);
                isShown = false;
                isHiding = false;
            }

            public void sync(string content, bool shouldShow)
            {
                if (bubble == null) return;

                if (shouldShow)
                {
                    if (isHiding)
                    {
                        killTween();
                        isHiding = false;
                    }

                    if (!isShown)
                    {
                        playShow(content);
                        return;
                    }

                    if (text != null && text.text != content)
                        text.text = content;

                    lastText = content;
                    return;
                }

                if (isShown || isHiding)
                    playHide();
            }

            private void playShow(string content)
            {
                killTween();
                lastText = content ?? string.Empty;
                if (text != null) text.text = lastText;

                bubble.gameObject.SetActive(true);
                bubble.localPosition = getEmitLocalPosition(character, bubble, emitNormalized);
                bubble.localScale = restLocalScale * BubbleHideScale;

                isShown = true;
                isHiding = false;

                tween = DOTween.Sequence()
                    .Join(bubble.DOLocalMove(restLocalPos, BubbleAnimDuration).SetEase(Ease.OutBack))
                    .Join(bubble.DOScale(restLocalScale, BubbleAnimDuration).SetEase(Ease.OutBack));
            }

            private void playHide()
            {
                if (isHiding || !isShown) return;

                killTween();
                isHiding = true;

                Vector3 emitPos = getEmitLocalPosition(character, bubble, emitNormalized);
                Vector3 hideScale = new Vector3(
                    restLocalScale.x >= 0f ? BubbleHideScale : -BubbleHideScale,
                    BubbleHideScale,
                    restLocalScale.z);

                tween = DOTween.Sequence()
                    .Join(bubble.DOLocalMove(emitPos, BubbleAnimDuration).SetEase(Ease.InBack))
                    .Join(bubble.DOScale(hideScale, BubbleAnimDuration).SetEase(Ease.InBack))
                    .OnComplete(() =>
                    {
                        if (bubble != null)
                        {
                            bubble.gameObject.SetActive(false);
                            bubble.localPosition = restLocalPos;
                            bubble.localScale = restLocalScale;
                        }

                        isShown = false;
                        isHiding = false;
                        lastText = string.Empty;
                    });
            }

            public void killTween()
            {
                if (tween == null) return;
                tween.Kill();
                tween = null;
            }

            public void resetInstant()
            {
                killTween();
                if (bubble != null)
                {
                    bubble.localPosition = restLocalPos;
                    bubble.localScale = restLocalScale;
                    bubble.gameObject.SetActive(false);
                }

                isShown = false;
                isHiding = false;
                lastText = string.Empty;
            }
        }

        public override void InitUI()
        {
            collectItemButtons();
            collectCards();
            collectBoxButton();

            _txtBottom = findText("BottomText/Txt_Bottom");

            RectTransform devilTf = transform.Find("Devil") as RectTransform;
            RectTransform angelTf = transform.Find("Angel") as RectTransform;
            RectTransform bubbleLeftRt = transform.Find("BubbleLeft") as RectTransform;
            RectTransform bubbleRightRt = transform.Find("BubbleRight") as RectTransform;

            _devilBubble = new BubbleAnimSlot();
            _devilBubble.init(bubbleLeftRt, devilTf, findText("BubbleLeft/Txt_Bubble"), new Vector2(0.82f, 0.72f));

            _angelBubble = new BubbleAnimSlot();
            _angelBubble.init(bubbleRightRt, angelTf, findText("BubbleRight/Txt_Bubble"), new Vector2(0.18f, 0.72f));

            if (_txtBottom != null) _fontTemplate = _txtBottom.font;
            else if (_devilBubble.text != null) _fontTemplate = _devilBubble.text.font;
        }

        public override void InitData()
        {
            base.InitData();
            bindItemButtons();
            bindBoxButton();
        }

        public override void Open(params object[] args)
        {
            base.Open(args);
            _dialogSession = null;
            killIntroSequence();
            _devilBubble?.resetInstant();
            _angelBubble?.resetInstant();
        }

        public override void Close(params object[] args)
        {
            killIntroSequence();
            killAllCardFlipTweens();
            _devilBubble?.killTween();
            _angelBubble?.killTween();
            base.Close(args);
        }

        public int GetItemSlotCount()
        {
            return _itemButtons.Count;
        }

        public void BeginSession(BlackjackModel model, Action onIntroComplete)
        {
            _onIntroComplete = onIntroComplete;
            killIntroSequence();
            killAllCardFlipTweens();
            _flippingCardIndices.Clear();
            _devilBubble?.resetInstant();
            _angelBubble?.resetInstant();

            syncItemSlots(model.ItemSlotCount);
            syncSmallCards(model);
            setInteractionLocked(true);
            RefreshGameplay(model, applyLayout: false);

            Vector2 flyStart = getBigCardAnchoredInSmallCardsRoot();
            IReadOnlyList<Vector2> targets = model.GetSmallCardLayout(
                _smallCardsRoot != null ? _smallCardsRoot.rect.width : 720f,
                _layoutCardWidth,
                _layoutSpacing);

            prepareSmallCardsAt(flyStart);

            float cooldown = Mathf.Max(0f, _openCooldownSeconds);
            _introSequence = DOTween.Sequence()
                .AppendInterval(cooldown)
                .AppendCallback(() => playSmallCardsFlyIn(targets, () =>
                {
                    setInteractionLocked(false);
                    _onIntroComplete?.Invoke();
                    _onIntroComplete = null;
                }));
        }

        protected override void OnUpdate()
        {
            if (_dialogSession == null || !_dialogSession.DialogEnabled) return;

            _dialogSession.Tick();
            applyBubbleVisibility();
        }

        public void RefreshGameplay(BlackjackModel model, bool applyLayout = true)
        {
            if (model == null) return;

            if (applyLayout)
            {
                syncItemSlots(model.ItemSlotCount);
                syncSmallCards(model);
            }

            refreshBigCardAndBottom(model);

            for (int i = 0; i < _smallCards.Count; i++)
            {
                if (_flippingCardIndices.Contains(i)) continue;

                CardSlot slot = _smallCards[i];
                bool revealed = i < model.Cards.Count && model.Cards[i].revealed;
                setCardFace(slot, revealed);
                if (slot.point != null)
                    slot.point.text = revealed ? model.GetRevealedPoint(i).ToString() : "?";
            }

            for (int i = 0; i < _itemButtons.Count; i++)
            {
                bool available = resolveItemAvailable(model, i);
                applyItemSlotState(i, available);
            }
        }

        private bool resolveItemAvailable(BlackjackModel model, int index)
        {
            if (_interactionLocked) return false;

            switch (_itemMode)
            {
                case ItemInteractionMode.PickBuff:
                    return !_buffSlotPicked && index >= 0;
                case ItemInteractionMode.PickMaterial:
                    return index >= 0;
                default:
                    return model.IsItemSlotAvailable(index);
            }
        }

        // 点击道具后播放小牌翻转；数值与累计点数在动画结束后由 onComplete 触发刷新
        public void PlayCardFlipReveal(int cardIndex, int pointValue, int usedItemSlot, Action onComplete)
        {
            if (cardIndex < 0 || cardIndex >= _smallCards.Count)
            {
                onComplete?.Invoke();
                return;
            }

            CardSlot slot = _smallCards[cardIndex];
            if (slot.rect == null)
            {
                onComplete?.Invoke();
                return;
            }

            killCardFlipTween(slot);
            _flippingCardIndices.Add(cardIndex);

            applyItemSlotState(usedItemSlot, false);
            setInteractionLocked(true);

            setCardFace(slot, false);
            if (slot.point != null)
            {
                slot.point.text = "?";
                slot.point.gameObject.SetActive(true);
            }

            slot.rect.localScale = Vector3.one;
            float half = Mathf.Max(0.05f, _flipHalfDuration);

            slot.flipTween = DOTween.Sequence()
                .Append(slot.rect.DOScaleX(0f, half).SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    setCardFace(slot, true);
                    if (slot.point != null)
                        slot.point.text = pointValue.ToString();
                })
                .Append(slot.rect.DOScaleX(1f, half).SetEase(Ease.OutQuad))
                .OnComplete(() =>
                {
                    slot.flipTween = null;
                    _flippingCardIndices.Remove(cardIndex);
                    if (slot.rect != null)
                        slot.rect.localScale = Vector3.one;
                    setInteractionLocked(false);
                    onComplete?.Invoke();
                });
        }

        private void refreshBigCardAndBottom(BlackjackModel model)
        {
            if (_bigCard != null)
            {
                setCardFace(_bigCard, true);
                if (_bigCard.point != null) _bigCard.point.text = model.TotalPoint.ToString();
            }

            if (_txtBottom != null)
                _txtBottom.text = $"累计点数：{model.TotalPoint} / {model.EffectiveBustLimit}　已翻 {model.RevealedCount}/{model.CardCount}";
        }

        public void SetupBuffPick(IReadOnlyList<MagicBoxBuffJsonData> candidates)
        {
            _itemMode = ItemInteractionMode.PickBuff;
            _buffSlotPicked = false;
            bindItemButtons();
            syncBuffItemSlots(candidates, pickedIndex: -1);
            setInteractionLocked(false);
        }

        public void SetupMaterialPick(IReadOnlyList<MaterialJsonData> candidates)
        {
            _itemMode = ItemInteractionMode.PickMaterial;
            bindItemButtons();
            syncMaterialItemSlots(candidates);
            setInteractionLocked(false);
        }

        public void BindDrawMode()
        {
            _itemMode = ItemInteractionMode.DrawCard;
            bindItemButtons();
            clearItemLabels();
            setInteractionLocked(false);
        }

        public void RefreshDialog(BlackjackDialogSession dialogSession)
        {
            _dialogSession = dialogSession;
            applyBubbleVisibility();
        }

        private void applyItemSlotState(int index, bool available)
        {
            if (index < 0 || index >= _itemButtons.Count) return;

            Button btn = _itemButtons[index];
            if (btn != null)
            {
                btn.interactable = available;
                if (btn.targetGraphic is Image img)
                    img.color = available ? ItemNormalColor : ItemDisabledColor;
            }

            if (index < _itemHovers.Count && _itemHovers[index] != null)
                _itemHovers[index].SetInteractable(available);
        }

        private void applyBubbleVisibility()
        {
            if (_dialogSession == null || !_dialogSession.DialogEnabled) return;

            _devilBubble?.sync(_dialogSession.DevilText, !string.IsNullOrEmpty(_dialogSession.DevilText));
            _angelBubble?.sync(_dialogSession.AngelText, !string.IsNullOrEmpty(_dialogSession.AngelText));
        }

        private void syncItemSlots(int slotCount)
        {
            Transform items = Find<Transform>("Items");
            if (items == null) return;

            int index = 0;
            foreach (Transform child in items)
            {
                bool active = index < slotCount;
                if (child.gameObject.activeSelf != active)
                    child.gameObject.SetActive(active);
                index++;
            }
        }

        private void syncSmallCards(BlackjackModel model)
        {
            if (model == null || _smallCardsRoot == null) return;

            ensureSmallCardPool(model.ItemSlotCount);

            _smallCards.Clear();
            for (int i = 0; i < _smallCardPool.Count; i++)
            {
                CardSlot slot = _smallCardPool[i];
                bool active = i < model.ItemSlotCount;
                if (slot.root != null)
                    slot.root.SetActive(active);
                if (active)
                    _smallCards.Add(slot);
            }

            IReadOnlyList<Vector2> layout = model.GetSmallCardLayout(
                _smallCardsRoot.rect.width,
                _layoutCardWidth,
                _layoutSpacing);

            for (int i = 0; i < _smallCards.Count; i++)
            {
                if (_smallCards[i].rect == null || i >= layout.Count) continue;
                _smallCards[i].rect.anchoredPosition = layout[i];
                _smallCards[i].rect.localScale = Vector3.one;
            }
        }

        private void ensureSmallCardPool(int required)
        {
            if (_smallCardPool.Count == 0) return;

            while (_smallCardPool.Count < required)
            {
                CardSlot template = _smallCardPool[0];
                GameObject clone = Instantiate(template.root, _smallCardsRoot);
                clone.name = $"Card_{_smallCardPool.Count}";
                _smallCardPool.Add(bindCard(clone));
            }
        }

        private void prepareSmallCardsAt(Vector2 anchoredPosition)
        {
            for (int i = 0; i < _smallCards.Count; i++)
            {
                CardSlot slot = _smallCards[i];
                if (slot.rect == null) continue;
                slot.rect.anchoredPosition = anchoredPosition;
                slot.rect.localScale = Vector3.zero;
            }
        }

        private void playSmallCardsFlyIn(IReadOnlyList<Vector2> targets, Action onComplete)
        {
            killIntroSequence();

            int count = Mathf.Min(_smallCards.Count, targets?.Count ?? 0);
            if (count <= 0)
            {
                onComplete?.Invoke();
                return;
            }

            _introSequence = DOTween.Sequence();

            for (int i = 0; i < count; i++)
            {
                CardSlot slot = _smallCards[i];
                if (slot.rect == null) continue;

                Vector2 target = targets[i];
                float delay = i * DealStagger;

                _introSequence.Insert(delay, slot.rect.DOAnchorPos(target, DealDuration).SetEase(Ease.OutCubic));
                _introSequence.Insert(delay, slot.rect.DOScale(Vector3.one, DealDuration).SetEase(Ease.OutBack));
            }

            _introSequence.OnComplete(() =>
            {
                _introSequence = null;
                onComplete?.Invoke();
            });
        }

        private Vector2 getBigCardAnchoredInSmallCardsRoot()
        {
            if (_bigCard?.root == null || _smallCardsRoot == null)
                return Vector2.zero;

            var bigRt = _bigCard.root.transform as RectTransform;
            if (bigRt == null) return Vector2.zero;

            Vector3 world = bigRt.TransformPoint(bigRt.rect.center);
            return _smallCardsRoot.InverseTransformPoint(world);
        }

        private void setInteractionLocked(bool locked)
        {
            _interactionLocked = locked;

            for (int i = 0; i < _itemButtons.Count; i++)
            {
                if (_itemButtons[i] != null)
                    _itemButtons[i].interactable = !locked;
                if (i < _itemHovers.Count && _itemHovers[i] != null)
                    _itemHovers[i].SetInteractable(!locked);
            }

            if (_btnBox != null)
                _btnBox.interactable = !locked;
        }

        private void killIntroSequence()
        {
            if (_introSequence == null) return;
            _introSequence.Kill();
            _introSequence = null;
        }

        private void killCardFlipTween(CardSlot slot)
        {
            if (slot?.flipTween == null) return;
            slot.flipTween.Kill();
            slot.flipTween = null;
            if (slot.rect != null)
                slot.rect.localScale = Vector3.one;
        }

        private void killAllCardFlipTweens()
        {
            _flippingCardIndices.Clear();
            for (int i = 0; i < _smallCardPool.Count; i++)
                killCardFlipTween(_smallCardPool[i]);
        }

        private static Vector3 getEmitLocalPosition(RectTransform character, RectTransform bubble, Vector2 normalizedInCharacter)
        {
            if (bubble == null) return Vector3.zero;
            if (character == null || bubble.parent == null)
                return bubble.localPosition;

            var localOffset = new Vector3(
                (normalizedInCharacter.x - character.pivot.x) * character.rect.width,
                (normalizedInCharacter.y - character.pivot.y) * character.rect.height,
                0f);

            Vector3 world = character.TransformPoint(localOffset);
            return bubble.parent.InverseTransformPoint(world);
        }

        private void collectItemButtons()
        {
            _itemButtons.Clear();
            _itemHovers.Clear();
            Transform items = Find<Transform>("Items");
            if (items == null) return;

            foreach (Transform child in items)
            {
                Button btn = child.GetComponent<Button>();
                if (btn == null) btn = child.gameObject.AddComponent<Button>();

                UIButtonHoverItem hover = child.GetComponent<UIButtonHoverItem>();
                if (hover == null) hover = child.gameObject.AddComponent<UIButtonHoverItem>();
                hover.Setup(btn, null, ItemHoverScale);

                _itemHovers.Add(hover);
                _itemButtons.Add(btn);
            }
        }

        private void bindItemButtons()
        {
            for (int i = 0; i < _itemButtons.Count; i++)
            {
                int itemIndex = i;
                Button btn = _itemButtons[i];
                if (btn == null) continue;
                btn.onClick.RemoveAllListeners();

                switch (_itemMode)
                {
                    case ItemInteractionMode.PickBuff:
                        btn.onClick.AddListener(() => onBuffItemClicked(itemIndex));
                        break;
                    case ItemInteractionMode.PickMaterial:
                        btn.onClick.AddListener(() => ApplyFunc(EventDefines.BlackjackPickMaterial, itemIndex));
                        break;
                    default:
                        btn.onClick.AddListener(() => ApplyFunc(EventDefines.BlackjackDraw, itemIndex));
                        break;
                }
            }
        }

        private void onBuffItemClicked(int itemIndex)
        {
            if (_buffSlotPicked) return;
            _buffSlotPicked = true;
            ApplyFunc(EventDefines.BlackjackPickBuff, itemIndex);
        }

        private void syncBuffItemSlots(IReadOnlyList<MagicBoxBuffJsonData> candidates, int pickedIndex)
        {
            Transform items = Find<Transform>("Items");
            if (items == null) return;

            int childIndex = 0;
            foreach (Transform child in items)
            {
                bool active = candidates != null && childIndex < candidates.Count;
                child.gameObject.SetActive(active);
                if (active)
                {
                    MagicBoxBuffJsonData buff = candidates[childIndex];
                    setItemLabel(childIndex, buff?.name ?? "Buff");
                }

                if (childIndex == pickedIndex)
                    applyItemSlotState(childIndex, false);

                childIndex++;
            }
        }

        private void syncMaterialItemSlots(IReadOnlyList<MaterialJsonData> candidates)
        {
            Transform items = Find<Transform>("Items");
            if (items == null) return;

            int childIndex = 0;
            foreach (Transform child in items)
            {
                bool active = candidates != null && childIndex < candidates.Count;
                child.gameObject.SetActive(active);
                if (active)
                {
                    MaterialJsonData material = candidates[childIndex];
                    setItemLabel(childIndex, material?.name ?? "材料");
                }

                childIndex++;
            }
        }

        private void setItemLabel(int index, string text)
        {
            ensureItemLabel(index);
            if (index < 0 || index >= _itemLabels.Count || _itemLabels[index] == null) return;
            _itemLabels[index].text = text ?? string.Empty;
            _itemLabels[index].gameObject.SetActive(true);
        }

        private void clearItemLabels()
        {
            for (int i = 0; i < _itemLabels.Count; i++)
            {
                if (_itemLabels[i] != null)
                    _itemLabels[i].gameObject.SetActive(false);
            }
        }

        private void ensureItemLabel(int index)
        {
            while (_itemLabels.Count <= index)
                _itemLabels.Add(null);

            if (_itemLabels[index] != null) return;
            if (index >= _itemButtons.Count || _itemButtons[index] == null) return;

            Transform parent = _itemButtons[index].transform;
            var labelGo = new GameObject("Txt_BuffName", typeof(RectTransform));
            labelGo.transform.SetParent(parent, false);

            var rect = labelGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 22f;
            label.color = Color.white;
            label.raycastTarget = false;
            if (_fontTemplate != null) label.font = _fontTemplate;

            _itemLabels[index] = label;
        }

        private void collectBoxButton()
        {
            Transform boxTf = Find<Transform>("Box");
            if (boxTf == null) return;

            _btnBox = boxTf.GetComponent<Button>();
            if (_btnBox == null)
                _btnBox = boxTf.gameObject.AddComponent<Button>();

            Image image = boxTf.GetComponent<Image>();
            if (image != null)
            {
                _btnBox.targetGraphic = image;
                _btnBox.transition = Selectable.Transition.None;
            }
        }

        private void bindBoxButton()
        {
            if (_btnBox == null) return;

            _btnBox.onClick.RemoveAllListeners();
            _btnBox.onClick.AddListener(() => ApplyFunc(EventDefines.BlackjackReturn));
        }

        private void collectCards()
        {
            _smallCardPool.Clear();
            _smallCards.Clear();

            Transform big = transform.Find("BigCard");
            if (big != null) _bigCard = bindCard(big.gameObject);

            _smallCardsRoot = Find<RectTransform>("SmallCards");
            if (_smallCardsRoot == null) return;

            foreach (Transform child in _smallCardsRoot)
                _smallCardPool.Add(bindCard(child.gameObject));
        }

        private CardSlot bindCard(GameObject root)
        {
            return new CardSlot
            {
                root = root,
                rect = root.GetComponent<RectTransform>(),
                face = root.GetComponent<Image>(),
                point = findTextIn(root.transform, "Txt_Point")
            };
        }

        private static void setCardFace(CardSlot slot, bool revealed)
        {
            if (slot == null) return;
            if (slot.face != null)
                slot.face.color = revealed ? new Color(1f, 1f, 1f, 1f) : new Color(0.45f, 0.3f, 0.55f, 1f);
            if (slot.point != null)
                slot.point.gameObject.SetActive(true);
        }

        private TextMeshProUGUI findText(string path)
        {
            Transform t = transform.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        private static TextMeshProUGUI findTextIn(Transform root, string path)
        {
            Transform t = root.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
