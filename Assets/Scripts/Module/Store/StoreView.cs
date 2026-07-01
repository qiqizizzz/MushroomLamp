/*

* ┌──────────────────────────────────┐

* │  描    述: 商店子页面视图

* │           中间：三个购买卡牌 + 各卡牌下方材料介绍框

* │           底部：背包横向滚动列表

* │  类    名: StoreView.cs

* └──────────────────────────────────┘

*/



using System.Collections.Generic;

using Common;

using Common.Defines;

using Common.UI;

using MVC.View;

using TMPro;

using UnityEngine;

using UnityEngine.UI;



namespace Module.Store

{

    public class StoreView : BaseView

    {

        private Button _btnBack;

        private TextMeshProUGUI _txtGold;



        private GameObject _buyItemPrefab;

        private readonly List<Transform> _buyAnchors = new();

        private readonly List<StoreBuyItem> _buyItems = new();



        private const int BagRows = 1;

        private const int BagPoolColumns = 8;

        private static readonly Vector2 BagCellSize = new Vector2(140f, 180f);

        private static readonly Vector2 BagSpacing = new Vector2(20f, 0f);



        private ScrollRect _bagScroll;

        private LoopGridView _bagGrid;

        private GameObject _bagItemPrefab;

        private StoreModel _model;



        public override void InitUI()

        {

            _btnBack = Find<Button>("Btn_Back");

            _txtGold = Find<TextMeshProUGUI>("TopGold/Txt_GoldValue");



            _buyItemPrefab = ResManager.LoadAsset<GameObject>(AddressDefines.UI_StoreBuyItem);

            collectBuyAnchors();

            ensureBuyItems();

            collectBagScroll();

        }



        public override void InitData()

        {

            base.InitData();

            bindButton(_btnBack, () => ApplyFunc(EventDefines.StoreReturn));

        }



        public void Refresh(StoreModel model)

        {

            if (model == null) return;

            _model = model;



            if (_txtGold != null) _txtGold.text = model.Gold.ToString();



            refreshBuyCards(model);

            refreshBag(model);

        }



        private void collectBuyAnchors()

        {

            _buyAnchors.Clear();

            Transform middle = Find<Transform>("Middle");

            if (middle == null) return;



            foreach (Transform child in middle)

            {

                if (child.name.StartsWith("BuyAnchor"))

                    _buyAnchors.Add(child);

            }

        }



        private void ensureBuyItems()

        {

            if (_buyItems.Count == _buyAnchors.Count) return;



            _buyItems.Clear();

            foreach (Transform anchor in _buyAnchors)

            {

                clearChildren(anchor);



                if (_buyItemPrefab == null)

                {

                    _buyItems.Add(null);

                    continue;

                }



                GameObject go = Instantiate(_buyItemPrefab, anchor);

                go.name = "StoreBuyItem";



                RectTransform rt = go.GetComponent<RectTransform>();

                rt.anchorMin = Vector2.zero;

                rt.anchorMax = Vector2.one;

                rt.offsetMin = Vector2.zero;

                rt.offsetMax = Vector2.zero;



                StoreBuyItem item = go.GetComponent<StoreBuyItem>();

                _buyItems.Add(item);

            }

        }



        private void refreshBuyCards(StoreModel model)

        {

            ensureBuyItems();



            for (int i = 0; i < _buyItems.Count; i++)

            {

                StoreBuyItem item = _buyItems[i];

                bool valid = item != null && model.BuySlots != null && i < model.BuySlots.Count;



                if (item != null) item.gameObject.SetActive(valid);

                if (!valid) continue;



                StoreBuySlotData slot = model.BuySlots[i];
                item.Bind(slot);

                bool canPick = !slot.isPurchased
                    && !(model.CardsIncludedInBoxPrice && model.HasBoxPickCompleted());

                if (item.Button != null)
                {
                    item.Button.onClick.RemoveAllListeners();
                    item.Button.interactable = canPick;
                    StoreBuySlotData captured = slot;
                    item.Button.onClick.AddListener(() => ApplyFunc(EventDefines.StoreBuy, captured));
                }

                setupBuyCardHover(item, canPick);

            }

        }



        private static void setupBuyCardHover(StoreBuyItem item, bool canPick)
        {
            StoreBuyHoverItem hover = item.Hover;
            if (hover == null) return;

            hover.Setup(item.Icon != null ? item.Icon.rectTransform : null);
            hover.SetInteractable(canPick);
        }



        private void collectBagScroll()

        {

            _bagScroll = Find<ScrollRect>("BagScrollView");

            _bagItemPrefab = ResManager.LoadAsset<GameObject>(AddressDefines.UI_StoreBagItem);

            if (_bagScroll == null) return;



            _bagScroll.horizontal = true;

            _bagScroll.vertical = false;



            stripLayoutComponents(_bagScroll.content);



            GameObject slotPrefab = _bagItemPrefab != null ? _bagItemPrefab : buildBagSlotTemplate();



            _bagGrid = _bagScroll.gameObject.GetComponent<LoopGridView>();

            if (_bagGrid == null) _bagGrid = _bagScroll.gameObject.AddComponent<LoopGridView>();



            var padding = new RectOffset(20, 20, 20, 20);

            _bagGrid.InitHorizontal(_bagScroll, slotPrefab, BagRows, BagPoolColumns,

                BagCellSize, BagSpacing, onUpdateBagSlot, padding);

        }



        private void refreshBag(StoreModel model)

        {

            if (_bagGrid == null) return;

            _bagGrid.SetTotalCount(model.BagEntries?.Count ?? 0);

        }



        private void onUpdateBagSlot(int dataIndex, GameObject slot)

        {

            if (_model == null || _model.BagEntries == null) return;

            if (dataIndex < 0 || dataIndex >= _model.BagEntries.Count) return;



            StoreBagItem item = slot.GetComponent<StoreBagItem>();

            if (item == null) item = slot.AddComponent<StoreBagItem>();

            item.Bind(_model.BagEntries[dataIndex]);

        }



        private static void clearChildren(Transform parent)

        {

            if (parent == null) return;

            for (int i = parent.childCount - 1; i >= 0; i--)

                Destroy(parent.GetChild(i).gameObject);

        }



        private static void stripLayoutComponents(RectTransform content)

        {

            if (content == null) return;

            var layout = content.GetComponent<LayoutGroup>();

            if (layout != null) Destroy(layout);

            var fitter = content.GetComponent<ContentSizeFitter>();

            if (fitter != null) Destroy(fitter);

        }



        private GameObject buildBagSlotTemplate()

        {

            GameObject root = new GameObject("StoreBagItemTemplate", typeof(RectTransform), typeof(Image), typeof(StoreBagItem));

            RectTransform rt = root.GetComponent<RectTransform>();

            rt.SetParent(transform, false);

            rt.sizeDelta = BagCellSize;



            root.GetComponent<Image>().enabled = false;



            createChildImage(rt, "Img_Icon", Color.white,

                new Vector2(0.1f, 0.26f), new Vector2(0.9f, 0.92f));

            createChildText(rt, "Txt_Name", new Vector2(0.02f, 0.0f), new Vector2(0.98f, 0.2f), 22, "");



            TextMeshProUGUI countTxt = createChildText(rt, "Txt_Count", new Vector2(0.5f, 0.02f), new Vector2(0.98f, 0.28f), 24, "x0");

            countTxt.alignment = TextAlignmentOptions.BottomRight;

            countTxt.color = new Color(0.2f, 0.2f, 0.2f, 1f);



            root.SetActive(false);

            return root;

        }



        private static void bindButton(Button button, UnityEngine.Events.UnityAction action)

        {

            if (button == null || action == null) return;

            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(action);

        }



        private static Image createChildImage(RectTransform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)

        {

            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));

            RectTransform rt = go.GetComponent<RectTransform>();

            rt.SetParent(parent, false);

            rt.anchorMin = anchorMin;

            rt.anchorMax = anchorMax;

            rt.offsetMin = Vector2.zero;

            rt.offsetMax = Vector2.zero;

            Image img = go.GetComponent<Image>();

            img.color = color;

            img.raycastTarget = false;

            return img;

        }



        private TextMeshProUGUI createChildText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float fontSize, string text)

        {

            var fontSource = Find<TextMeshProUGUI>("TopGold/Txt_GoldValue");



            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));

            RectTransform rt = go.GetComponent<RectTransform>();

            rt.SetParent(parent, false);

            rt.anchorMin = anchorMin;

            rt.anchorMax = anchorMax;

            rt.offsetMin = Vector2.zero;

            rt.offsetMax = Vector2.zero;

            TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();

            if (fontSource != null) txt.font = fontSource.font;

            txt.text = text;

            txt.fontSize = fontSize;

            txt.color = new Color(0.2f, 0.15f, 0.1f, 1f);

            txt.alignment = TextAlignmentOptions.Center;

            txt.raycastTarget = false;

            return txt;

        }

    }

}

