/*
* ┌──────────────────────────────────┐
* │  描    述: 21 点玩法视图
* │  类    名: BlackjackView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common.Defines;
using DG.Tweening;
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

        private readonly List<Button> _itemButtons = new();
        private readonly List<CardSlot> _smallCards = new();

        private Button _btnBox;

        private CardSlot _bigCard;
        private TextMeshProUGUI _txtBottom;

        private BubbleAnimSlot _devilBubble;
        private BubbleAnimSlot _angelBubble;

        private BlackjackDialogSession _dialogSession;
        private TMP_FontAsset _fontTemplate;

        private class CardSlot
        {
            public GameObject root;
            public TextMeshProUGUI point;
            public Image face;
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
            _devilBubble?.resetInstant();
            _angelBubble?.resetInstant();
        }

        public override void Close(params object[] args)
        {
            _devilBubble?.killTween();
            _angelBubble?.killTween();
            base.Close(args);
        }

        protected override void OnUpdate()
        {
            if (_dialogSession == null) return;

            _dialogSession.Tick();
            applyBubbleVisibility();
        }

        public void Refresh(BlackjackModel model, BlackjackDialogSession dialogSession)
        {
            if (model == null) return;

            _dialogSession = dialogSession;

            if (_bigCard != null)
            {
                setCardFace(_bigCard, true);
                if (_bigCard.point != null) _bigCard.point.text = model.TotalPoint.ToString();
            }

            for (int i = 0; i < _smallCards.Count; i++)
            {
                CardSlot slot = _smallCards[i];
                bool revealed = i < model.Cards.Count && model.Cards[i].revealed;
                setCardFace(slot, revealed);
                if (slot.point != null)
                    slot.point.text = revealed ? model.GetRevealedPoint(i).ToString() : "?";
            }

            foreach (Button btn in _itemButtons)
                if (btn != null) btn.interactable = model.CanDraw;

            if (_txtBottom != null)
                _txtBottom.text = $"累计点数：{model.TotalPoint} / {BlackjackModel.BustLimit}　已翻 {model.RevealedCount}/{model.CardCount}";

            applyBubbleVisibility();
        }

        private void applyBubbleVisibility()
        {
            if (_dialogSession == null) return;

            _devilBubble?.sync(_dialogSession.DevilText, !string.IsNullOrEmpty(_dialogSession.DevilText));
            _angelBubble?.sync(_dialogSession.AngelText, !string.IsNullOrEmpty(_dialogSession.AngelText));
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
            Transform items = Find<Transform>("Items");
            if (items == null) return;

            foreach (Transform child in items)
            {
                Button btn = child.GetComponent<Button>();
                if (btn == null) btn = child.gameObject.AddComponent<Button>();
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
                btn.onClick.AddListener(() => ApplyFunc(EventDefines.BlackjackDraw, itemIndex));
            }
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
            _smallCards.Clear();

            Transform big = transform.Find("BigCard");
            if (big != null) _bigCard = bindCard(big.gameObject);

            Transform smalls = Find<Transform>("SmallCards");
            if (smalls != null)
                foreach (Transform child in smalls)
                    _smallCards.Add(bindCard(child.gameObject));
        }

        private CardSlot bindCard(GameObject root)
        {
            return new CardSlot
            {
                root = root,
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
