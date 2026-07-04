/*
* ┌──────────────────────────────────┐
* │  描    述: 21 点玩法视图
* │  类    名: BlackjackView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common.Defines;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Blackjack
{
    public class BlackjackView : BaseView
    {
        private readonly List<Button> _itemButtons = new();
        private readonly List<CardSlot> _smallCards = new();

        private Button _btnBox;

        private CardSlot _bigCard;
        private TextMeshProUGUI _txtBottom;
        private TextMeshProUGUI _bubbleLeft;
        private TextMeshProUGUI _bubbleRight;

        private TMP_FontAsset _fontTemplate;

        // 单张牌的运行时绑定
        private class CardSlot
        {
            public GameObject root;
            public TextMeshProUGUI point;   // 点数文本
            public Image face;              // 牌面（翻开=亮色，未翻=暗色背面）
        }

        public override void InitUI()
        {
            collectItemButtons();
            collectCards();
            collectBoxButton();

            _txtBottom = findText("BottomText/Txt_Bottom");
            _bubbleLeft = findText("BubbleLeft/Txt_Bubble");
            _bubbleRight = findText("BubbleRight/Txt_Bubble");

            if (_txtBottom != null) _fontTemplate = _txtBottom.font;
            else if (_bubbleLeft != null) _fontTemplate = _bubbleLeft.font;
        }

        public override void InitData()
        {
            base.InitData();
            bindItemButtons();
            bindBoxButton();
        }

        public void Refresh(BlackjackModel model)
        {
            if (model == null) return;

            // 大牌显示当前累计点数
            if (_bigCard != null)
            {
                setCardFace(_bigCard, true);
                if (_bigCard.point != null) _bigCard.point.text = model.TotalPoint.ToString();
            }

            // 四张小牌
            for (int i = 0; i < _smallCards.Count; i++)
            {
                CardSlot slot = _smallCards[i];
                bool revealed = i < model.Cards.Count && model.Cards[i].revealed;
                setCardFace(slot, revealed);
                if (slot.point != null)
                    slot.point.text = revealed ? model.GetRevealedPoint(i).ToString() : "?";
            }

            // 顶部按钮：不能继续翻牌时禁用
            foreach (Button btn in _itemButtons)
                if (btn != null) btn.interactable = model.CanDraw;

            // 底部文本
            if (_txtBottom != null)
                _txtBottom.text = $"累计点数：{model.TotalPoint} / {BlackjackModel.BustLimit}　已翻 {model.RevealedCount}/{model.CardCount}";

            if (_bubbleLeft != null)
                _bubbleLeft.text = BlackjackDialogCatalogLoader.GetDevilText(model.IsBusted);
            if (_bubbleRight != null)
                _bubbleRight.text = BlackjackDialogCatalogLoader.GetAngelText(model.IsBusted);
        }

        // ---------------- 道具按钮 ----------------

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

        // 左上魔盒：返回 CookView
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

        // ---------------- 卡牌 ----------------

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

        // 翻开=亮色牌面，未翻=暗色背面
        private static void setCardFace(CardSlot slot, bool revealed)
        {
            if (slot == null) return;
            if (slot.face != null)
                slot.face.color = revealed ? new Color(1f, 1f, 1f, 1f) : new Color(0.45f, 0.3f, 0.55f, 1f);
            if (slot.point != null)
                slot.point.gameObject.SetActive(true);
        }

        // ---------------- 查找工具 ----------------

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
